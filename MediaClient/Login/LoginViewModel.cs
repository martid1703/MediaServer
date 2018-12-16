using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;
using MediaClient.Models;
using MediaClient.WebApi;
using MediaClient.Exceptions;
using System.Net.Http;
using DTO;
using System.Windows.Controls;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Threading;
using System.Linq;

namespace MediaClient
{
    class LoginViewModel : INotifyPropertyChanged
    {
        //----------------------------------------------------------------------
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName]string prop = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }

        //----------------------------------------------------------------------
        private StatusUpdate _statusUpdate;
        //----------------------------------------------------------------------
        private CUserInfo _userInfo;
        public CUserInfo UserInfo
        {
            get { return _userInfo; }
            set
            {
                _userInfo = value;
                OnPropertyChanged("UserInfo");
            }
        }

        public String Password
        {
            get { return _userInfo.Password; }
            set
            {
                _userInfo.Password = value;
            }
        }
        //----------------------------------------------------------------------



        private ICommand _loginCommand;
        public ICommand LoginCommand { get => _loginCommand; set => _loginCommand = value; }

        private ICommand _registerCommand;
        public ICommand RegisterCommand { get => _registerCommand; set => _registerCommand = value; }



        #region CTORs
        public LoginViewModel(StatusUpdate statusUpdate)
        {
            _statusUpdate = statusUpdate;

            _userInfo = new CUserInfo();
            UserInfo.Name = "testUser";
            UserInfo.Password = "123";

            LoginCommand = new RelayCommand(new Action<object>(Login));
            RegisterCommand = new RelayCommand(new Action<object>(Register));


        }


        #endregion
        private void Login(object obj)
        {
            var sc = SynchronizationContext.Current;
            Task<Guid> guid = LoginAsync(_userInfo);
            guid.ContinueWith(delegate
            {
                if (guid.Result.Equals(Guid.Empty))
                {
                    System.Windows.MessageBox.Show("Couldn't login with given credentials!");
                    return;
                }
                // set userId after successul Login
                _userInfo.Id = guid.Result;
                FileCatalogViewModel fcvm = new FileCatalogViewModel(_userInfo,_statusUpdate);

                sc.Post(delegate 
                {
                    FileCatalogPage fcp = new FileCatalogPage();
                    fcp.DataContext = fcvm;
                    Switcher.Switch(fcp);
                    
                }, null);
            });
        }

        private void Register(object obj)
        {
            var sc = SynchronizationContext.Current;

                RegisterViewModel registerViewModel = new RegisterViewModel(_statusUpdate);

                sc.Post(delegate
                {
                    //System.Windows.MessageBox.Show(_userInfo.ToString());
                    RegisterPage registerPage = new RegisterPage();
                    registerPage.DataContext = registerViewModel;
                    Switcher.Switch(registerPage);
                }, null);
        }



   

        UserWebApi userWebApi = new UserWebApi();

        // Check credentials of the user = Login to the MediaServer
        internal async Task<Guid> LoginAsync(CUserInfo user)
        {
            try
            {
                HttpResponseMessage loginResponse = await userWebApi.LoginAsync(user);
                if (loginResponse.IsSuccessStatusCode)
                {
                Guid userId = loginResponse.Content.ReadAsAsync<CUserInfo>().Result.Id;
                return userId;
                }
                return Guid.Empty;
            }
            catch (Exception ex)
            {
                throw new WebApiException($"User {user.Name} couldn't login to MediaServer!", ex);
            }

        }

        
    }
}
