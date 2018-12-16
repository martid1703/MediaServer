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

namespace MediaClient
{
    /// <summary>
    /// Interaction logic for WatchVideo.xaml
    /// </summary>
    public partial class WatchVideoPage : Page
    {
        public WatchVideoPage(WatchVideoViewModel wvvm)
        {
            InitializeComponent();

            DataContext = wvvm;
            wvvm.PlayRequested += (sender, e) =>
              {
                  this.myMediaElement.Play();
              };
            wvvm.PauseRequested += (sender, e) =>
            {
                this.myMediaElement.Pause();
            };
            wvvm.StopRequested += (sender, e) =>
            {
                this.myMediaElement.Stop();
            };
        }

        private void myMediaElement_Loaded(object sender, RoutedEventArgs e)
        {
            this.myMediaElement.Play();
        }
    }
}
