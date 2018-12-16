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
using System.Windows.Shapes;
using System.IO;

namespace MediaClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        MainWindowViewModel mainWindowVM;
        public MainWindow()
        {
            InitializeComponent();

            mainWindowVM = new MainWindowViewModel();
            this.DataContext = mainWindowVM;
            Switcher.pageSwitcher = this;

        }

        public void Navigate(Page nextPage)
        {
            this.mainFrame.Content = nextPage;
        }

        private void menuExitClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            mainWindowVM.Login(sender);
        }
    }
}
