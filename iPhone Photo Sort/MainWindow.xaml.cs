using System;
using System.Collections.Generic;
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

    private void button_Click(object sender, RoutedEventArgs e)
    {
      CommonOpenFileDialog dialog = new CommonOpenFileDialog();
      dialog.IsFolderPicker = true;
      if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
      {
        //label.Text = dialog.FileName; // 테스트용, 폴더 선택이 완료되면 선택된 폴더를 label에 출력
      }
    }
  }
}
