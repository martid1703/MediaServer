using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace MediaClient
{
    public static class Switcher
    {
        public static MainWindow pageSwitcher;

        public static void Switch(Page newPage)
        {
            pageSwitcher.Navigate(newPage);
        }
    }
}
