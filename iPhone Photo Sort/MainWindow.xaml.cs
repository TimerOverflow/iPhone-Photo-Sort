using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        public MainWindow()
        {
            InitializeComponent();
            SetBuildVersion();
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
                MessageBox.Show("폴더를 먼저 선택해주세요.");
                return;
            }

            Button_Sort.IsEnabled = false;
            Button_FolderOpen.IsEnabled = false;
            AddLine("카메라 모델별로 사진/영상을 정리합니다...");

            await Task.Run(() => SortFiles());

            AddLine("정리가 완료되었습니다.");
            Dispatcher.Invoke(() =>
            {
                ProgressBar_Status.Visibility = Visibility.Hidden;
                Button_Sort.IsEnabled = true;
                Button_FolderOpen.IsEnabled = true;
            });
        }

        private void SortFiles()
        {
            Dictionary<string, DirectoryInfo> cameraDirectories = new Dictionary<string, DirectoryInfo>();
            int totalFiles = _fileNames.Count;

            // 1. 디렉토리 구조 파악 및 생성
            for (int i = 0; i < totalFiles; i++)
            {
                string filePath = System.IO.Path.Combine(_directory, _fileNames[i]);
                string cameraModel = GetCameraModel(filePath);

                if (!string.IsNullOrEmpty(cameraModel))
                {
                    string cameraDirectoryName = System.IO.Path.Combine(_directory, cameraModel);
                    if (!cameraDirectories.ContainsKey(cameraModel))
                    {
                        DirectoryInfo cameraDirInfo = new DirectoryInfo(cameraDirectoryName);
                        if (!cameraDirInfo.Exists)
                        {
                            cameraDirInfo.Create();
                            AddLine($"{cameraModel} 디렉토리를 생성했습니다: {cameraDirInfo.FullName}");
                        }
                        cameraDirectories.Add(cameraModel, cameraDirInfo);
                    }
                }
                UpdateProgress(i + 1, totalFiles);
            }

            AddLine("디렉토리 준비 완료. 파일 이동을 시작합니다...");

            // IMG_E... 파일이 IMG_... 파일보다 먼저 처리되도록 내림차순 정렬
            // 이렇게 하면 편집된 파일을 처리할 때 원본 파일을 찾아 Org 폴더로 먼저 옮길 수 있습니다.
            _fileNames.Sort((a, b) => string.Compare(b, a, StringComparison.OrdinalIgnoreCase));

            // 2. 파일 이동 처리
            for (int i = 0; i < _fileNames.Count; i++)
            {
                string fileName = _fileNames[i];
                if (string.IsNullOrEmpty(fileName)) continue;

                string filePath = System.IO.Path.Combine(_directory, fileName);
                
                // 파일이 이미 다른 로직(MoveOriginalImage 등)에 의해 이동되었는지 확인
                if (!System.IO.File.Exists(filePath)) continue;

                string cameraModel = GetCameraModel(filePath);
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
                        AddLine("Others 디렉토리를 생성했습니다: " + othersDir.FullName);
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
                            _fileNames[i] = ""; // 현재 파일 처리 완료

                            // 원본 파일(IMG_...)을 찾아 Org 폴더로 이동
                            if (!MoveOriginalImage(fileName, _directory, targetDirectory.FullName, true))
                            {
                                // 이미 기종 폴더 루트로 이동된 경우를 대비해 재시도
                                MoveOriginalImage(fileName, targetDirectory.FullName, targetDirectory.FullName, false);
                            }
                        }
                        else if (System.IO.Path.GetExtension(fileName).Equals(".AAE", StringComparison.OrdinalIgnoreCase))
                        {
                            DirectoryInfo aaeDir = new DirectoryInfo(System.IO.Path.Combine(targetDirectory.FullName, "AAE"));
                            if (!aaeDir.Exists) aaeDir.Create();
                            
                            MoveFileSafe(filePath, System.IO.Path.Combine(aaeDir.FullName, fileName));
                        }
                        else if (fileName.StartsWith("IMG", StringComparison.OrdinalIgnoreCase))
                        {
                            MoveFileSafe(filePath, System.IO.Path.Combine(targetDirectory.FullName, fileName));
                        }
                        else
                        {
                            MoveFileSafe(filePath, System.IO.Path.Combine(targetDirectory.FullName, fileName));
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
                    AddLine($"{System.IO.Path.GetFileName(destPath)} 덮어쓰기 건너뜀.");
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
            // 예: IMG_1234.HEIC, IMG_1234.MOV, IMG_1234.AAE 등
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
                found = true;
            }

            if (!found && !silent)
            {
                AddLine($"{orgBaseName}.* 원본 파일이 없습니다.");
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
                AddLine("파일 목록을 불러오는 중입니다...");

                try
                {
                    // 비동기적으로 파일 목록 로드
                    _fileNames = await Task.Run(() => 
                        System.IO.Directory.EnumerateFiles(_directory)
                        .Where(f => !System.IO.Path.GetFileName(f).StartsWith(".")) // 숨김 파일 제외 (필요시)
                        .Select(f => System.IO.Path.GetFileName(f))
                        .ToList()
                    );

                    AddLine($"{_fileNames.Count}개의 파일을 찾았습니다.");
                }
                catch (Exception ex)
                {
                    AddLine($"파일 로드 중 오류 발생: {ex.Message}");
                }
                finally
                {
                    ProgressBar_Status.IsIndeterminate = false;
                    ProgressBar_Status.Visibility = Visibility.Hidden;
                    Button_FolderOpen.IsEnabled = true;
                    Button_Sort.IsEnabled = true;
                }
            }
        }
    }
}