using System;
using System.Windows;

namespace iPhone_Photo_Sort
{
    public enum ConflictResolution
    {
        Overwrite,
        Skip,
        Rename
    }

    public partial class ConflictResolutionDialog : Window
    {
        public ConflictResolution Resolution { get; private set; }

        public ConflictResolutionDialog(string fileName)
        {
            InitializeComponent();
            MessageTextBlock.Text = $"대상 폴더에 이미 동일한 이름의 파일이 존재합니다:\n{fileName}\n\n어떻게 처리하시겠습니까?";
        }

        private void OverwriteButton_Click(object sender, RoutedEventArgs e)
        {
            Resolution = ConflictResolution.Overwrite;
            DialogResult = true;
        }

        private void SkipButton_Click(object sender, RoutedEventArgs e)
        {
            Resolution = ConflictResolution.Skip;
            DialogResult = true;
        }

        private void RenameButton_Click(object sender, RoutedEventArgs e)
        {
            Resolution = ConflictResolution.Rename;
            DialogResult = true;
        }
    }
}
