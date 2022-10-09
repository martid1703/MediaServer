using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MediaClient
{
    class MainWindowViewModel

    {
        //----------------------------------------------------------------------
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName]string prop = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }
        //----------------------------------------------------------------------

        // Property to bind status bar to
        private StatusUpdate _statusBarInfo;
        public StatusUpdate StatusBarInfo
        {
            get { return _statusBarInfo; }
            set
            {
                _statusBarInfo = value;
                OnPropertyChanged("StatusBarInfo");
            }
        }

        //----CTOR-------------------------------------------------------
        public MainWindowViewModel()
        {
            _statusBarInfo = new StatusUpdate();

            // todo: get user settings from "UserSettings.xml" and write them to the UserSettings class
            UserSettings.ChunkSize = 500000;// bytes
            UserSettings.UserFolder = System.IO.Path.GetFullPath(
                System.IO.Path.Combine(Environment.CurrentDirectory, "..\\..\\" + "UserFiles\\"));

            LoginCommand = new RelayCommand(Login);

        }

        private ICommand _loginCommand;
        public ICommand LoginCommand { get => _loginCommand; set => _loginCommand = value; }

        public void Login(Object obj)
        {
            LoginViewModel loginViewModel = new LoginViewModel(StatusBarInfo);
            LoginPage loginPage = new LoginPage();
            loginPage.DataContext = loginViewModel;
            Switcher.Switch(loginPage);
        }

   
    }
}
