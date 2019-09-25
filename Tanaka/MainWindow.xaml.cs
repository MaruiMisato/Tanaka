using System.Windows;
//using  Entry;
namespace Tanaka {
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window {
        public MainWindow() {
            InitializeComponent();
        }
        private void ExeButton_Click(object sender, RoutedEventArgs e) {
            if (Clipboard.ContainsFileDropList()) {//Check if clipboard has file drop format data.
                new EntryPoint().FileOrFolderAsync(this, Clipboard.GetFileDropList());//Class1にMainWindow自身を渡すことでどのMainWindowにあるかはっきりさせる
            } else {//Check if clipboard has file drop format data.
                MessageBox.Show("Please select folders.");
            }
        }
    }
}
