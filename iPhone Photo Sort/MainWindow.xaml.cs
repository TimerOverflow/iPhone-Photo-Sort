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
      TextBox_MessageOutput.AppendText(text);
      TextBox_MessageOutput.AppendText("\u2028"); // Linebreak, not paragraph break
      TextBox_MessageOutput.ScrollToEnd();
    } 

    public void button_sort_Click(object sender, RoutedEventArgs e)
    {
      DirectoryInfo iPhone = new DirectoryInfo(Directory + "\\iPhone");
      DirectoryInfo iPhoneOrg = new DirectoryInfo(Directory + "\\iPhone\\Org");
      DirectoryInfo iPhoneFiltered = new DirectoryInfo(Directory + "\\iPhone\\Filtered");
      DirectoryInfo iPhoneAAE = new DirectoryInfo(Directory + "\\iPhone\\AAE");
      DirectoryInfo Others = new DirectoryInfo(Directory + "\\Others");

      if (iPhone.Exists == false)
      {
        iPhone.Create();
        AddLine(iPhone.FullName + " created!");
      }

      if (iPhoneOrg.Exists == false)
      {
        iPhoneOrg.Create();
        AddLine(iPhoneOrg.FullName + " created!");
      }

      if (iPhoneFiltered.Exists == false)
      {
        iPhoneFiltered.Create();
        AddLine(iPhoneFiltered.FullName + " created!");
      }

      if (iPhoneAAE.Exists == false)
      {
        iPhoneAAE.Create();
        AddLine(iPhoneAAE.FullName + " created!");
      }

      if (Others.Exists == false)
      {
        Others.Create();
        AddLine(Others.FullName + " created!");
      }

      for (int i = 0; i < FindFiles; i++)
      {
        string file_name = FileName[i];

        if (file_name.Contains("IMG_E"))
        {
          System.IO.File.Move(System.IO.Path.Combine(Directory, file_name), System.IO.Path.Combine(iPhoneFiltered.FullName, file_name));
          System.IO.File.Move(System.IO.Path.Combine(Directory, file_name.Replace("E", "")), System.IO.Path.Combine(iPhoneOrg.FullName, file_name.Replace("E", "")));

          FileName[i] = "";
          FileName[Array.IndexOf(FileName, file_name.Replace("E", ""))] = "";
        }
      }

      for (int i = 0; i < FindFiles; i++)
      {
        string file_name = FileName[i];

        if (System.IO.Path.GetExtension(file_name) == ".AAE")
        {
          System.IO.File.Move(System.IO.Path.Combine(Directory, file_name), System.IO.Path.Combine(iPhoneAAE.FullName, file_name));
        }
        else if (file_name.Contains("IMG"))
        {
          System.IO.File.Move(System.IO.Path.Combine(Directory, file_name), System.IO.Path.Combine(iPhone.FullName, file_name));
          AddLine("iPhone " + file_name);
        }
        else if(file_name != "")
        {
          System.IO.File.Move(System.IO.Path.Combine(Directory, file_name), System.IO.Path.Combine(Others.FullName, file_name));
          AddLine("Others " + file_name);
        }
      }
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
        foreach (var file in files)
        {
          AddLine(file.Name);
          FileName[FindFiles++] = file.Name;
        }

        AddLine("total files : " + FindFiles.ToString());
      }
    }
  }
}
