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
      richTextBox_MessageOutput.AppendText(text);
      richTextBox_MessageOutput.AppendText("\u2028"); // Linebreak, not paragraph break
      richTextBox_MessageOutput.ScrollToEnd();
    } 

    public void button_Click(object sender, RoutedEventArgs e)
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
        foreach (var file in files)
        {
          AddLine(file.Name);
          FileName[FindFiles++] = file.Name;
        }

        AddLine("total files : " + FindFiles.ToString());
      }
    }

    public void button_sort_Click(object sender, RoutedEventArgs e)
    {
      DirectoryInfo iPhone = new DirectoryInfo(Directory + "\\iPhone");
      DirectoryInfo iPhoneOrg = new DirectoryInfo(Directory + "\\iPhone\\Org");
      DirectoryInfo iPhoneFiltered = new DirectoryInfo(Directory + "\\iPhone\\Filtered");
      DirectoryInfo Others = new DirectoryInfo(Directory + "\\Others");

      if(iPhone.Exists == false)
      {
        iPhone.Create();
        AddLine(iPhone.FullName + " created!");
      }

      if(iPhoneOrg.Exists == false)
      {
        iPhoneOrg.Create();
        AddLine(iPhoneOrg.FullName + " created!");
      }

      if(iPhoneFiltered.Exists == false)
      {
        iPhoneFiltered.Create();
        AddLine(iPhoneFiltered.FullName + " created!");
      }

      if(Others.Exists == false)
      {
        Others.Create();
        AddLine(Others.FullName + " created!");
      }

      for(int i = 0; i < FindFiles; i++)
      {
        string file_name = FileName[i];
        string source_path = Directory;
        string target_path = "";

        if (file_name.Contains("IMG_"))
        {
          target_path = iPhone.FullName;
        }
        else
        {
          target_path = Others.FullName;
        }

        string source_file = System.IO.Path.Combine(source_path, file_name);
        string dest_file = System.IO.Path.Combine(target_path, file_name);

        System.IO.File.Move(source_file, dest_file);
      }
    }
  }
}
