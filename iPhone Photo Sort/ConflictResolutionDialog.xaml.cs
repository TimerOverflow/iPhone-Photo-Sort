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
            MessageTextBlock.Text = $"The file already exists in the destination folder:\n{fileName}\n\nHow would you like to proceed?";
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
