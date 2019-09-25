using System.Windows;
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
                new EntryPoint().FileOrFolder(this, Clipboard.GetFileDropList());//Class1にMainWindow自身を渡すことでどのMainWindowにあるかはっきりさせる
            } else {//Check if clipboard has file drop format data.
                MessageBox.Show("Please select folders.");
            }
        }
        private void MarginRemove_Click(object sender, RoutedEventArgs e) {
            Shaving.Visibility = (bool)MarginRemove.IsChecked ? Visibility.Visible : Visibility.Hidden;
        }
        private void NotArchive_Click(object sender, RoutedEventArgs e) {
            CompressLevel.Visibility = Visibility.Hidden;
        }
        private void Rar_Click(object sender, RoutedEventArgs e) {
            CompressLevel.Visibility = Visibility.Visible;
        }
        private void SevenZip_Click(object sender, RoutedEventArgs e) {
            CompressLevel.Visibility = Visibility.Visible;
        }
        private void Zip_Click(object sender, RoutedEventArgs e) {
            CompressLevel.Visibility = Visibility.Visible;
        }
    }
}
