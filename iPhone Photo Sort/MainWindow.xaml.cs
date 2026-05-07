using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.WindowsAPICodePack.Dialogs;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.QuickTime;

namespace iPhone_Photo_Sort
{
    public partial class MainWindow : Window
    {
        private string _directory = "";
        private List<string> _fileNames = new List<string>();
        private ObservableCollection<FileSortInfo> _fileSortInfos = new ObservableCollection<FileSortInfo>();

        public MainWindow()
        {
            InitializeComponent();
            SetBuildVersion();
            ListView_Files.ItemsSource = _fileSortInfos;
        }

        private void SetBuildVersion()
        {
            try
            {
                var filePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                var lastWriteTime = System.IO.File.GetLastWriteTime(filePath);
                TextBlock_BuildTime.Text = $"Build: {lastWriteTime:yyyy.MM.dd HH:mm:ss}";
            }
            catch
            {
                TextBlock_BuildTime.Text = "Version: Developer Build";
            }
        }

        public void AddLine(string text)
        {
            Dispatcher.Invoke(() =>
            {
                TextBox_MessageOutput.AppendText(text);
                TextBox_MessageOutput.AppendText("\u2028");
                TextBox_MessageOutput.ScrollToEnd();
            });
        }

        private void UpdateProgress(int current, int total)
        {
            Dispatcher.Invoke(() =>
            {
                if (ProgressBar_Status.Visibility != Visibility.Visible)
                    ProgressBar_Status.Visibility = Visibility.Visible;
                
                ProgressBar_Status.Maximum = total;
                ProgressBar_Status.Value = current;
            });
        }

        public async void button_sort_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_directory) || _fileNames.Count == 0)
            {
                MessageBox.Show("Please select a folder first.");
                return;
            }

            Button_Sort.IsEnabled = false;
            Button_FolderOpen.IsEnabled = false;
            AddLine("Sorting photos and videos by camera model...");

            await Task.Run(() => SortFiles());

            AddLine("Sorting completed.");

            // 정렬 이력을 파일로 저장
            await Task.Run(() => SaveSortHistory());
            AddLine("Sort history saved.");

            Dispatcher.Invoke(() =>
            {
                ProgressBar_Status.Visibility = Visibility.Hidden;
                Button_Sort.IsEnabled = false; // 이미 정렬 완료되었으므로 비활성화
                Button_FolderOpen.IsEnabled = true;

                // 정렬 완료 후 폴더 트리로 전환
                BuildFolderTree();
            });
        }

        /// <summary>
        /// 정렬 결과를 바탕으로 폴더 트리를 구축하고 TreeView에 표시합니다.
        /// </summary>
        private void BuildFolderTree()
        {
            var rootNodes = new ObservableCollection<FolderTreeNode>();

            // FileSortInfo를 DestinationFolder 기준으로 그룹화
            var sortedFiles = _fileSortInfos.Where(f => f.IsSorted).ToList();
            var unsortedFiles = _fileSortInfos.Where(f => !f.IsSorted).ToList();

            // 정렬된 파일들을 폴더별로 그룹화
            var folderGroups = sortedFiles
                .GroupBy(f => f.DestinationFolder ?? "Others")
                .OrderBy(g => g.Key);

            foreach (var group in folderGroups)
            {
                string fullPath = group.Key; // e.g. "iPhone 17 Pro/Filtered"
                string[] parts = fullPath.Split('/');

                // 최상위 폴더 찾기 또는 생성
                FolderTreeNode currentNode = null;
                ObservableCollection<FolderTreeNode> currentChildren = rootNodes;

                foreach (string part in parts)
                {
                    var existingNode = currentChildren.FirstOrDefault(n => n.IsFolder && n.Name == part);
                    if (existingNode == null)
                    {
                        existingNode = new FolderTreeNode
                        {
                            Name = part,
                            IsFolder = true,
                            IsExpanded = true
                        };
                        currentChildren.Add(existingNode);
                    }
                    currentNode = existingNode;
                    currentChildren = currentNode.Children;
                }

                // 파일 노드 추가
                foreach (var fileInfo in group.OrderBy(f => f.FileName))
                {
                    currentNode.Children.Add(new FolderTreeNode
                    {
                        Name = fileInfo.FileName,
                        IsFolder = false,
                        FileInfo = fileInfo
                    });
                }
            }

            // 미분류 파일이 있으면 별도 노드로 추가
            if (unsortedFiles.Any())
            {
                var unsortedNode = new FolderTreeNode
                {
                    Name = "Unsorted",
                    IsFolder = true,
                    IsExpanded = false
                };
                foreach (var fileInfo in unsortedFiles.OrderBy(f => f.FileName))
                {
                    unsortedNode.Children.Add(new FolderTreeNode
                    {
                        Name = fileInfo.FileName,
                        IsFolder = false,
                        FileInfo = fileInfo
                    });
                }
                rootNodes.Add(unsortedNode);
            }

            // ListView 숨기고 TreeView 표시
            ListView_Files.Visibility = Visibility.Collapsed;
            TreeView_Sorted.ItemsSource = rootNodes;
            TreeView_Sorted.Visibility = Visibility.Visible;
            TextBlock_FileListHeader.Text = "🌳 Sorted Folder Tree";

            int totalSorted = sortedFiles.Count;
            int totalUnsorted = unsortedFiles.Count;
            TextBlock_FileCount.Text = $"{totalSorted} sorted, {totalUnsorted} unsorted";
        }

        private void SortFiles()
        {
            Dictionary<string, DirectoryInfo> cameraDirectories = new Dictionary<string, DirectoryInfo>();
            int totalFiles = _fileNames.Count;

            // 1. 디렉토리 구조 파악 및 생성 + 메타데이터 수집
            for (int i = 0; i < totalFiles; i++)
            {
                string filePath = System.IO.Path.Combine(_directory, _fileNames[i]);
                string cameraModel = null;

                // FileSortInfo 찾기 및 메타데이터 수집
                FileSortInfo info = null;
                Dispatcher.Invoke(() =>
                {
                    info = _fileSortInfos.FirstOrDefault(x => x.FileName == _fileNames[i]);
                });

                if (info != null)
                {
                    CollectMetadata(filePath, info);
                    cameraModel = info.CameraModel;
                }
                else
                {
                    cameraModel = GetCameraModel(filePath);
                }

                if (!string.IsNullOrEmpty(cameraModel))
                {
                    string cameraDirectoryName = System.IO.Path.Combine(_directory, cameraModel);
                    if (!cameraDirectories.ContainsKey(cameraModel))
                    {
                        DirectoryInfo cameraDirInfo = new DirectoryInfo(cameraDirectoryName);
                        if (!cameraDirInfo.Exists)
                        {
                            cameraDirInfo.Create();
                            AddLine($"Created directory for {cameraModel}: {cameraDirInfo.FullName}");
                        }
                        cameraDirectories.Add(cameraModel, cameraDirInfo);
                    }
                }
                UpdateProgress(i + 1, totalFiles);
            }

            AddLine("Directories ready. Starting file migration...");

            // IMG_E... 파일이 IMG_... 파일보다 먼저 처리되도록 내림차순 정렬
            _fileNames.Sort((a, b) => string.Compare(b, a, StringComparison.OrdinalIgnoreCase));

            // 2. 파일 이동 처리
            for (int i = 0; i < _fileNames.Count; i++)
            {
                string fileName = _fileNames[i];
                if (string.IsNullOrEmpty(fileName)) continue;

                string filePath = System.IO.Path.Combine(_directory, fileName);
                
                // 파일이 이미 다른 로직(MoveOriginalImage 등)에 의해 이동되었는지 확인
                if (!System.IO.File.Exists(filePath)) continue;

                // FileSortInfo 찾기
                FileSortInfo info = null;
                Dispatcher.Invoke(() =>
                {
                    info = _fileSortInfos.FirstOrDefault(x => x.FileName == fileName);
                });

                string cameraModel = info?.CameraModel ?? GetCameraModel(filePath);
                DirectoryInfo targetDirectory = null;

                if (!string.IsNullOrEmpty(cameraModel) && cameraDirectories.ContainsKey(cameraModel))
                {
                    targetDirectory = cameraDirectories[cameraModel];
                }
                else
                {
                    DirectoryInfo othersDir = new DirectoryInfo(System.IO.Path.Combine(_directory, "Others"));
                    if (!othersDir.Exists)
                    {
                        othersDir.Create();
                        AddLine("Created 'Others' directory: " + othersDir.FullName);
                    }
                    targetDirectory = othersDir;
                }

                if (targetDirectory != null)
                {
                    try
                    {
                        if (fileName.StartsWith("IMG_E", StringComparison.OrdinalIgnoreCase))
                        {
                            DirectoryInfo filteredDir = new DirectoryInfo(System.IO.Path.Combine(targetDirectory.FullName, "Filtered"));
                            if (!filteredDir.Exists) filteredDir.Create();
                            
                            MoveFileSafe(filePath, System.IO.Path.Combine(filteredDir.FullName, fileName));
                            _fileNames[i] = "";

                            // 분류 정보 기록
                            UpdateSortInfo(info, $"{targetDirectory.Name}/Filtered",
                                $"Edited file (IMG_E prefix) → Filtered folder");

                            // 원본 파일(IMG_...)을 찾아 Org 폴더로 이동
                            if (!MoveOriginalImage(fileName, _directory, targetDirectory.FullName, true))
                            {
                                MoveOriginalImage(fileName, targetDirectory.FullName, targetDirectory.FullName, false);
                            }
                        }
                        else if (System.IO.Path.GetExtension(fileName).Equals(".AAE", StringComparison.OrdinalIgnoreCase))
                        {
                            DirectoryInfo aaeDir = new DirectoryInfo(System.IO.Path.Combine(targetDirectory.FullName, "AAE"));
                            if (!aaeDir.Exists) aaeDir.Create();
                            
                            MoveFileSafe(filePath, System.IO.Path.Combine(aaeDir.FullName, fileName));
                            UpdateSortInfo(info, $"{targetDirectory.Name}/AAE",
                                "Apple AAE sidecar file → AAE folder");
                        }
                        else if (fileName.StartsWith("IMG", StringComparison.OrdinalIgnoreCase))
                        {
                            MoveFileSafe(filePath, System.IO.Path.Combine(targetDirectory.FullName, fileName));
                            string reason = !string.IsNullOrEmpty(cameraModel) 
                                ? $"Camera model: {cameraModel} → {targetDirectory.Name} folder" 
                                : "No camera metadata found → Others folder";
                            UpdateSortInfo(info, targetDirectory.Name, reason);
                        }
                        else
                        {
                            MoveFileSafe(filePath, System.IO.Path.Combine(targetDirectory.FullName, fileName));
                            string reason = !string.IsNullOrEmpty(cameraModel) 
                                ? $"Camera model: {cameraModel} → {targetDirectory.Name} folder" 
                                : "No camera metadata found → Others folder";
                            UpdateSortInfo(info, targetDirectory.Name, reason);
                        }
                    }
                    catch (Exception ex)
                    {
                        AddLine($"Error moving {fileName}: {ex.Message}");
                    }
                }
                UpdateProgress(i + 1, totalFiles);
            }
        }

        private void UpdateSortInfo(FileSortInfo info, string destination, string reason)
        {
            if (info == null) return;
            Dispatcher.Invoke(() =>
            {
                info.DestinationFolder = destination;
                info.SortingReason = reason;
                info.IsSorted = true;
            });
        }

        /// <summary>
        /// 파일에서 주요 메타데이터를 한 번에 수집하여 FileSortInfo에 저장합니다.
        /// </summary>
        private void CollectMetadata(string filePath, FileSortInfo info)
        {
            try
            {
                var directories = ImageMetadataReader.ReadMetadata(filePath);
                
                foreach (var dir in directories)
                {
                    foreach (var tag in dir.Tags)
                    {
                        string tagName = tag.Name;
                        string tagValue = tag.Description;
                        if (string.IsNullOrWhiteSpace(tagValue)) continue;

                        // Camera Model
                        if (string.IsNullOrEmpty(info.CameraModel) &&
                            tagName.IndexOf("Model", StringComparison.OrdinalIgnoreCase) >= 0 &&
                            tagName.IndexOf("Color", StringComparison.OrdinalIgnoreCase) < 0)
                        {
                            info.CameraModel = tagValue.Trim();
                        }

                        // Exposure Time / Shutter Speed
                        if (string.IsNullOrEmpty(info.Exposure) &&
                            (tagName.Equals("Exposure Time", StringComparison.OrdinalIgnoreCase) ||
                             tagName.Equals("Shutter Speed Value", StringComparison.OrdinalIgnoreCase)))
                        {
                            info.Exposure = tagValue.Trim();
                        }

                        // F-Number
                        if (string.IsNullOrEmpty(info.FNumber) &&
                            (tagName.Equals("F-Number", StringComparison.OrdinalIgnoreCase) ||
                             tagName.Equals("Aperture Value", StringComparison.OrdinalIgnoreCase)))
                        {
                            info.FNumber = tagValue.Trim();
                        }

                        // ISO
                        if (string.IsNullOrEmpty(info.ISO) &&
                            tagName.IndexOf("ISO", StringComparison.OrdinalIgnoreCase) >= 0 &&
                            tagName.IndexOf("Offset", StringComparison.OrdinalIgnoreCase) < 0)
                        {
                            info.ISO = tagValue.Trim();
                        }

                        // Focal Length
                        if (string.IsNullOrEmpty(info.FocalLength) &&
                            tagName.Equals("Focal Length", StringComparison.OrdinalIgnoreCase))
                        {
                            info.FocalLength = tagValue.Trim();
                        }

                        // Date/Time Original
                        if (string.IsNullOrEmpty(info.DateTaken) &&
                            (tagName.Equals("Date/Time Original", StringComparison.OrdinalIgnoreCase) ||
                             tagName.Equals("Date/Time", StringComparison.OrdinalIgnoreCase)))
                        {
                            info.DateTaken = tagValue.Trim();
                        }

                        // Lens Model
                        if (string.IsNullOrEmpty(info.LensModel) &&
                            tagName.IndexOf("Lens Model", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            info.LensModel = tagValue.Trim();
                        }

                        // Video Duration
                        if (string.IsNullOrEmpty(info.Duration) &&
                            tagName.Equals("Duration", StringComparison.OrdinalIgnoreCase))
                        {
                            info.Duration = tagValue.Trim();
                        }

                        // Video Frame Rate
                        if (string.IsNullOrEmpty(info.FrameRate) &&
                            tagName.Equals("Frame Rate", StringComparison.OrdinalIgnoreCase))
                        {
                            info.FrameRate = tagValue.Trim();
                        }

                        // GPS Location (간략화된 형식으로 추출)
                        if (string.IsNullOrEmpty(info.Location) &&
                            tagName.StartsWith("GPS Latitude", StringComparison.OrdinalIgnoreCase))
                        {
                            // 위도가 감지되면 위치 정보가 있는 것으로 간주
                            // 실제로는 Latitude/Longitude를 조합해야 하지만 여기서는 존재 여부와 간단한 값 위주로 표시
                            info.Location = "Available";
                        }

                        // Resolution (Image Width x Height)
                        bool isWidth = tagName.Equals("Image Width", StringComparison.OrdinalIgnoreCase) ||
                                       tagName.Equals("Exif Image Width", StringComparison.OrdinalIgnoreCase);
                        bool isHeight = tagName.Equals("Image Height", StringComparison.OrdinalIgnoreCase) ||
                                        tagName.Equals("Exif Image Height", StringComparison.OrdinalIgnoreCase);

                        if (isWidth || isHeight)
                        {
                            string val = tagValue.Trim().Replace(" pixels", "");
                            string currentRes = info.Resolution ?? "";
                            string[] parts = currentRes.Split(new[] { " x " }, StringSplitOptions.None);
                            string w = parts.Length > 0 ? parts[0] : "";
                            string h = parts.Length > 1 ? parts[1] : "";

                            if (isWidth) w = val;
                            if (isHeight) h = val;

                            if (string.IsNullOrEmpty(w)) w = "?";
                            if (string.IsNullOrEmpty(h)) h = "?";

                            info.Resolution = $"{w} x {h}";
                        }
                    }
                }
            }
            catch
            {
                // 메타데이터 파싱 실패 시 무시
            }
        }

        private void MoveFileSafe(string sourcePath, string destPath)
        {
            if (!System.IO.File.Exists(sourcePath)) return;

            if (System.IO.File.Exists(destPath))
            {
                if (sourcePath.Equals(destPath, StringComparison.OrdinalIgnoreCase))
                    return;

                ConflictResolution resolution = ConflictResolution.Skip;
                Dispatcher.Invoke(() =>
                {
                    var dialog = new ConflictResolutionDialog(System.IO.Path.GetFileName(destPath));
                    dialog.Owner = this;
                    if (dialog.ShowDialog() == true)
                    {
                        resolution = dialog.Resolution;
                    }
                });

                if (resolution == ConflictResolution.Skip)
                {
                    AddLine($"Skipped overwriting {System.IO.Path.GetFileName(destPath)}.");
                    return;
                }
                else if (resolution == ConflictResolution.Overwrite)
                {
                    System.IO.File.Delete(destPath);
                    System.IO.File.Move(sourcePath, destPath);
                }
                else if (resolution == ConflictResolution.Rename)
                {
                    string dir = System.IO.Path.GetDirectoryName(destPath);
                    string name = System.IO.Path.GetFileNameWithoutExtension(destPath);
                    string ext = System.IO.Path.GetExtension(destPath);
                    int count = 1;
                    string newDestPath;
                    do
                    {
                        newDestPath = System.IO.Path.Combine(dir, $"{name}({count}){ext}");
                        count++;
                    } while (System.IO.File.Exists(newDestPath));

                    System.IO.File.Move(sourcePath, newDestPath);
                }
            }
            else
            {
                System.IO.File.Move(sourcePath, destPath);
            }
        }

        private bool MoveOriginalImage(string filteredFileName, string sourceDirectory, string DestinationDirectory, bool silent)
        {
            // IMG_E1234.JPG -> "IMG_1234" 추출
            string fileNameWithoutExt = System.IO.Path.GetFileNameWithoutExtension(filteredFileName);
            if (fileNameWithoutExt.Length < 6) return false;
            
            string numberPart = fileNameWithoutExt.Substring(5); // "1234"
            string orgBaseName = "IMG_" + numberPart; // "IMG_1234"
            
            // sourceDirectory에서 orgBaseName으로 시작하는 모든 파일 찾기 (확장자 상관없이)
            DirectoryInfo di = new DirectoryInfo(sourceDirectory);
            if (!di.Exists) return false;

            FileInfo[] files;
            try
            {
                files = di.GetFiles(orgBaseName + ".*");
            }
            catch
            {
                return false;
            }
            
            bool found = false;
            foreach (var file in files)
            {
                // 자기 자신(편집본)은 제외
                if (file.Name.Equals(filteredFileName, StringComparison.OrdinalIgnoreCase)) continue;

                // 확장자에 따라 목적지 폴더 결정 (.AAE 파일은 AAE 폴더, 나머지는 Org 폴더)
                string subFolderName = file.Extension.Equals(".AAE", StringComparison.OrdinalIgnoreCase) ? "AAE" : "Org";

                DirectoryInfo subDir = new DirectoryInfo(System.IO.Path.Combine(DestinationDirectory, subFolderName));
                if (!subDir.Exists) subDir.Create();
                
                string destPath = System.IO.Path.Combine(subDir.FullName, file.Name);
                MoveFileSafe(file.FullName, destPath);
                
                // _fileNames 리스트에서도 처리 완료 표시
                int index = _fileNames.FindIndex(x => x.Equals(file.Name, StringComparison.OrdinalIgnoreCase));
                if (index >= 0)
                {
                    _fileNames[index] = "";
                }

                // FileSortInfo 업데이트
                FileSortInfo orgInfo = null;
                string parentFolderName = new DirectoryInfo(DestinationDirectory).Name;
                Dispatcher.Invoke(() =>
                {
                    orgInfo = _fileSortInfos.FirstOrDefault(x => x.FileName.Equals(file.Name, StringComparison.OrdinalIgnoreCase));
                });

                string reason = subFolderName == "AAE"
                    ? $"AAE sidecar of edited file {filteredFileName} → AAE folder"
                    : $"Original of edited file {filteredFileName} → Org folder";
                UpdateSortInfo(orgInfo, $"{parentFolderName}/{subFolderName}", reason);

                found = true;
            }

            if (!found && !silent)
            {
                AddLine($"Original file {orgBaseName}.* not found.");
            }
            return found;
        }

        private string GetCameraModel(string filePath)
        {
            try
            {
                var directories = ImageMetadataReader.ReadMetadata(filePath);
                
                foreach (var dir in directories)
                {
                    // 1. Exif 표준 태그 확인
                    if (dir.ContainsTag(ExifDirectoryBase.TagModel))
                    {
                        string model = dir.GetString(ExifDirectoryBase.TagModel);
                        if (!string.IsNullOrWhiteSpace(model)) return model.Trim();
                    }

                    // 2. 태그 이름으로 "Model"이 포함된 속성 찾기 (HEIC, 동영상 등 지원 강화)
                    foreach (var tag in dir.Tags)
                    {
                        if (tag.Name.IndexOf("Model", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            string desc = tag.Description;
                            if (!string.IsNullOrWhiteSpace(desc))
                                return desc.Trim();
                        }
                    }
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        private async void Button_Click_2(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                _directory = dialog.FileName;
                TextBox_Path.Text = _directory;

                // UI 초기화
                Button_FolderOpen.IsEnabled = false;
                Button_Sort.IsEnabled = false;
                ProgressBar_Status.Visibility = Visibility.Visible;
                ProgressBar_Status.IsIndeterminate = true;
                _fileSortInfos.Clear();

                // 트리뷰가 표시 중이면 다시 리스트뷰로 전환
                TreeView_Sorted.Visibility = Visibility.Collapsed;
                TreeView_Sorted.ItemsSource = null;
                ListView_Files.Visibility = Visibility.Visible;
                TextBlock_FileListHeader.Text = "📋 File List";

                AddLine("Loading file list...");

                try
                {
                    // 기존 정렬 이력 확인
                    string historyPath = System.IO.Path.Combine(_directory, SortHistory.FileName);
                    bool historyLoaded = false;

                    if (System.IO.File.Exists(historyPath))
                    {
                        AddLine("Found existing sort history. Loading...");
                        historyLoaded = await Task.Run(() => LoadSortHistory(historyPath));
                    }

                    if (historyLoaded)
                    {
                        // 이력 로드 성공: 트리뷰로 표시, SORT 비활성화
                        AddLine($"Sort history loaded. ({_fileSortInfos.Count} files)");
                        TextBlock_FileCount.Text = $"{_fileSortInfos.Count} files (from history)";
                        BuildFolderTree();

                        ProgressBar_Status.IsIndeterminate = false;
                        ProgressBar_Status.Visibility = Visibility.Hidden;
                        Button_FolderOpen.IsEnabled = true;
                        Button_Sort.IsEnabled = false; // 이미 정렬됨
                    }
                    else
                    {
                        // 이력 없음: 기존 로직 - 파일 목록 로드
                        _fileNames = await Task.Run(() => 
                            System.IO.Directory.EnumerateFiles(_directory)
                            .Where(f => !System.IO.Path.GetFileName(f).StartsWith("."))
                            .Select(f => System.IO.Path.GetFileName(f))
                            .ToList()
                        );

                        foreach (var fileName in _fileNames)
                        {
                            string fullPath = System.IO.Path.Combine(_directory, fileName);
                            long fileBytes = 0;
                            try { fileBytes = new FileInfo(fullPath).Length; } catch { }

                            string fileSizeStr;
                            if (fileBytes >= 1024 * 1024)
                                fileSizeStr = $"{fileBytes / (1024.0 * 1024.0):F1} MB";
                            else if (fileBytes >= 1024)
                                fileSizeStr = $"{fileBytes / 1024.0:F1} KB";
                            else
                                fileSizeStr = $"{fileBytes} B";

                            _fileSortInfos.Add(new FileSortInfo
                            {
                                FileName = fileName,
                                OriginalPath = fullPath,
                                FileSize = fileSizeStr,
                                IsSorted = false
                            });
                        }

                        AddLine($"Found {_fileNames.Count} files.");
                        TextBlock_FileCount.Text = $"{_fileNames.Count} files";

                        ProgressBar_Status.IsIndeterminate = false;
                        ProgressBar_Status.Visibility = Visibility.Hidden;
                        Button_FolderOpen.IsEnabled = true;
                        Button_Sort.IsEnabled = true;
                    }
                }
                catch (Exception ex)
                {
                    AddLine($"Error loading files: {ex.Message}");
                    ProgressBar_Status.IsIndeterminate = false;
                    ProgressBar_Status.Visibility = Visibility.Hidden;
                    Button_FolderOpen.IsEnabled = true;
                    Button_Sort.IsEnabled = true;
                }
            }
        }

        /// <summary>
        /// 정렬 결과를 JSON 파일로 저장합니다.
        /// </summary>
        private void SaveSortHistory()
        {
            try
            {
                var history = new SortHistory
                {
                    SourceDirectory = _directory,
                    SortedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    AppVersion = TextBlock_BuildTime.Dispatcher.Invoke(() => TextBlock_BuildTime.Text)
                };

                Dispatcher.Invoke(() =>
                {
                    foreach (var info in _fileSortInfos)
                    {
                        history.Entries.Add(SortHistoryEntry.FromFileSortInfo(info));
                    }
                });

                string historyPath = System.IO.Path.Combine(_directory, SortHistory.FileName);
                var serializer = new DataContractJsonSerializer(typeof(SortHistory),
                    new DataContractJsonSerializerSettings
                    {
                        UseSimpleDictionaryFormat = true
                    });

                using (var stream = System.IO.File.Create(historyPath))
                {
                    serializer.WriteObject(stream, history);
                }
            }
            catch (Exception ex)
            {
                AddLine($"Failed to save sort history: {ex.Message}");
            }
        }

        /// <summary>
        /// JSON 파일에서 정렬 이력을 로드합니다.
        /// </summary>
        private bool LoadSortHistory(string historyPath)
        {
            try
            {
                SortHistory history;
                var serializer = new DataContractJsonSerializer(typeof(SortHistory));

                using (var stream = System.IO.File.OpenRead(historyPath))
                {
                    history = (SortHistory)serializer.ReadObject(stream);
                }

                if (history == null || history.Entries == null || history.Entries.Count == 0)
                    return false;

                Dispatcher.Invoke(() =>
                {
                    _fileSortInfos.Clear();
                    foreach (var entry in history.Entries)
                    {
                        _fileSortInfos.Add(entry.ToFileSortInfo());
                    }
                });

                AddLine($"Sorted at: {history.SortedAt}");
                AddLine($"App version: {history.AppVersion}");
                return true;
            }
            catch (Exception ex)
            {
                AddLine($"Failed to load sort history: {ex.Message}");
                return false;
            }
        }
    }
}