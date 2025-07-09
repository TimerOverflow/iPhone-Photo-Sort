using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Windows.Media.Imaging; // BitmapMetadata 사용을 위한 네임스페이스 추가

namespace iPhone_Photo_Sort
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        static string Directory = "";
        static int MaxFiles = 2000;
        static string[] FileName = new string[MaxFiles];
        static int FindFiles = 0;

        public MainWindow()
        {
            InitializeComponent();
        }

        public void AddLine(string text)
        {
            TextBox_MessageOutput.AppendText(text);
            TextBox_MessageOutput.AppendText("\u2028"); // Linebreak, not paragraph break
            TextBox_MessageOutput.ScrollToEnd();
        }

        public void button_sort_Click(object sender, RoutedEventArgs e)
        {
            DirectoryInfo rootDir = new DirectoryInfo(Directory);
            Dictionary<string, DirectoryInfo> cameraDirectories = new Dictionary<string, DirectoryInfo>();

            AddLine("카메라 모델별로 사진을 정리합니다...");

            // 1. 모든 사진 파일을 순회하며 카메라 모델 정보를 얻고 디렉토리를 생성합니다.
            for (int i = 0; i < FindFiles; i++)
            {
                string filePath = System.IO.Path.Combine(Directory, FileName[i]);
                string cameraModel = GetCameraModel(filePath);

                if (!string.IsNullOrEmpty(cameraModel))
                {
                    string cameraDirectoryName = System.IO.Path.Combine(Directory, cameraModel);
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
            }

            // 2. 기존 규칙에 따라 파일을 카메라 모델 디렉토리 하위에 정리합니다.
            for (int i = 0; i < FindFiles; i++)
            {
                string fileName = FileName[i];
                string filePath = System.IO.Path.Combine(Directory, fileName);
                string cameraModel = GetCameraModel(filePath);
                DirectoryInfo targetDirectory = null;

                if (!string.IsNullOrEmpty(cameraModel) && cameraDirectories.ContainsKey(cameraModel))
                {
                    targetDirectory = cameraDirectories[cameraModel];
                }
                else
                {
                    // 카메라 모델 정보를 얻을 수 없거나 해당 모델의 디렉토리가 없는 경우 "Others" 디렉토리로 이동
                    DirectoryInfo othersDir = new DirectoryInfo(System.IO.Path.Combine(Directory, "Others"));
                    if (!othersDir.Exists)
                    {
                        othersDir.Create();
                        AddLine("Others 디렉토리를 생성했습니다: " + othersDir.FullName);
                    }
                    targetDirectory = othersDir;
                }

                if (targetDirectory != null)
                {
                    if (fileName.Contains("IMG_E"))
                    {
                        DirectoryInfo filteredDir = new DirectoryInfo(System.IO.Path.Combine(targetDirectory.FullName, "Filtered"));
                        if (!filteredDir.Exists) filteredDir.Create();
                        try
                        {
                            System.IO.File.Move(filePath, System.IO.Path.Combine(filteredDir.FullName, fileName));
                            FileName[i] = "";
                            // Move Filtered Image

                            if(MoveOriginalImage(fileName, Directory, targetDirectory.FullName) == false)
                            {
                                MoveOriginalImage(fileName, targetDirectory.FullName, targetDirectory.FullName);
                            }
                        }
                        catch (Exception ex)
                        {
                            AddLine($"Error moving {fileName}: {ex.Message}");
                        }
                    }
                    else if (System.IO.Path.GetExtension(fileName) == ".AAE")
                    {
                        DirectoryInfo aaeDir = new DirectoryInfo(System.IO.Path.Combine(targetDirectory.FullName, "AAE"));
                        if (!aaeDir.Exists) aaeDir.Create();
                        try
                        {
                            System.IO.File.Move(filePath, System.IO.Path.Combine(aaeDir.FullName, fileName));
                        }
                        catch (Exception ex)
                        {
                            AddLine($"Error moving {fileName}: {ex.Message}");
                        }
                    }
                    else if (fileName.Contains("IMG"))
                    {
                        try
                        {
                            System.IO.File.Move(filePath, System.IO.Path.Combine(targetDirectory.FullName, fileName));
                            AddLine($"{cameraModel} - iPhone: {fileName}");
                        }
                        catch (Exception ex)
                        {
                            AddLine($"Error moving {fileName}: {ex.Message}");
                        }
                    }
                    else if (fileName != "")
                    {
                        try
                        {
                            System.IO.File.Move(filePath, System.IO.Path.Combine(targetDirectory.FullName, fileName));
                            AddLine($"{cameraModel} - Others: {fileName}");
                        }
                        catch (Exception ex)
                        {
                            AddLine($"Error moving {fileName}: {ex.Message}");
                        }
                    }
                }
            }

            AddLine("사진 정리가 완료되었습니다.");
        }

        private bool MoveOriginalImage(string filteredFileName, string sourceDirectory, string DestinationDirectory)
        {
            string orgFileName = filteredFileName.Replace("E", "");
            string orgFilePath = System.IO.Path.Combine(sourceDirectory, orgFileName);
            if (System.IO.File.Exists(orgFilePath))
            {
                DirectoryInfo orgDir = new DirectoryInfo(System.IO.Path.Combine(DestinationDirectory, "Org"));
                if (!orgDir.Exists) orgDir.Create();
                System.IO.File.Move(orgFilePath, System.IO.Path.Combine(orgDir.FullName, orgFileName));
                int index = Array.IndexOf(FileName, orgFileName);
                if (index >= 0)
                {
                    FileName[index] = "";
                }

                return true;
            }
            else
            {
                AddLine($"{orgFileName} 파일이 없습니다.");
                return false;
            }
        }

        private string GetCameraModel(string filePath)
        {
            try
            {
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    BitmapFrame bitmapFrame = BitmapFrame.Create(fs, BitmapCreateOptions.DelayCreation, BitmapCacheOption.None);
                    BitmapMetadata metadata = bitmapFrame.Metadata as BitmapMetadata;

                    if (metadata != null && metadata.ContainsQuery("System.Photo.CameraModel"))
                    {
                        return metadata.GetQuery("System.Photo.CameraModel").ToString().Trim();
                    }
                }
            }
            catch (Exception ex)
            {
                AddLine($"Error reading metadata from {filePath}: {ex.Message}");
            }
            return null;
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog openFileDialog = new CommonOpenFileDialog();

            openFileDialog.IsFolderPicker = true;

            if (openFileDialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                TextBox_Path.Text = openFileDialog.FileName;
                Directory = openFileDialog.FileName;

                var di = new DirectoryInfo(TextBox_Path.Text);
                var files = di.EnumerateFiles();

                FindFiles = 0;
                FileName = new string[MaxFiles]; // Reset FileName array when a new directory is selected
                foreach (var file in files)
                {
                    AddLine(file.Name);
                    FileName[FindFiles++] = file.Name;
                }

                AddLine(FindFiles.ToString() + "개의 파일을 찾았습니다.");
            }
        }
    }
}