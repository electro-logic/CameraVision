// Author: Leonardo Tazzini (http://electro-logic.blogspot.it)

namespace CameraVision
{
    public partial class MainWindow : System.Windows.Window
    {
        MainWindowVM _vm = new MainWindowVM();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = _vm;
        }

        private void Window_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            _vm.ReadRegisters();
        }
    }
}
