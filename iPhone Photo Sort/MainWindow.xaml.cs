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

namespace iPhone_Photo_Sort
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    public MainWindow()
    {
      InitializeComponent();
    }

    public void AddLine(string text)
    {
      richTextBox_MessageOutput.AppendText(text);
      richTextBox_MessageOutput.AppendText("\u2028"); // Linebreak, not paragraph break
      richTextBox_MessageOutput.ScrollToEnd();
    } 

    private void button_Click(object sender, RoutedEventArgs e)
    {
      CommonOpenFileDialog openFileDialog = new CommonOpenFileDialog();

      openFileDialog.IsFolderPicker = true;

      if (openFileDialog.ShowDialog() == CommonFileDialogResult.Ok)
      {
        TextBox_Path.Text = openFileDialog.FileName;

        var di = new DirectoryInfo(TextBox_Path.Text);
        var files = di.EnumerateFiles();
        var cnt = 0;

        foreach(var file in files)
        {
          AddLine(file.Name);
          cnt++;
        }

        AddLine("total files : " + cnt.ToString());
      }
    }

    private void button_sort_Click(object sender, RoutedEventArgs e)
    {

    }
  }
}
