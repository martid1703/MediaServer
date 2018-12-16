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
using MediaClient.UserControls;
using System.IO;
using DTO;

namespace MediaClient
{
    /// <summary>
    /// Interaction logic for FileCatalog.xaml
    /// </summary>
    public partial class FileCatalogPage : Page
    {
        public FileCatalogPage()
        {
            InitializeComponent();
        }

        private void PasswordChangedEvent(object sender, RoutedEventArgs e)
        {
            if (this.DataContext != null)
            { ((dynamic)this.DataContext).Password = ((PasswordBox)sender).Password; }
        }

    }
}
