using DTO;
using MediaClient.Exceptions;
using MediaClient.WebApi;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MediaClient
{
    class RegisterViewModel:INotifyPropertyChanged
    {
        private CUserInfo _userInfo;
        private StatusUpdate _statusUpdate;

        private ICommand _registerCommand;
        public ICommand RegisterCommand { get => _registerCommand; set => _registerCommand = value; }

        #region CTORs
        public RegisterViewModel(StatusUpdate statusUpdate)
        {
            _userInfo = new CUserInfo();
            _statusUpdate = statusUpdate;
            RegisterCommand = new RelayCommand(new Action<object>(Register));
        }
        #endregion

        private void Register(object obj)
        {
            var sc = SynchronizationContext.Current;

            try
            {
                Task<CUserInfo> loggedUser = Register(_userInfo);
                loggedUser.ContinueWith(delegate
                {
                    if (loggedUser.Result.Id.Equals(Guid.Empty))
                    {
                        System.Windows.MessageBox.Show("Couldn't register with given credentials!");
                        return;
                    }
                    _userInfo.Id = loggedUser.Result.Id;
                    FileCatalogViewModel fcvm = new FileCatalogViewModel(_userInfo, _statusUpdate);
                    sc.Post(delegate
                    {
                        FileCatalogPage fcp = new FileCatalogPage();
                        fcp.DataContext = fcvm;
                        Switcher.Switch(fcp);
                    }, null);
                }); 
                
            }
            catch (WebApiException ex)
            {
                System.Windows.MessageBox.Show("Couldn't register with given credentials!");
                return;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Couldn't register with given credentials!");
                return;
            }

        }

        UserWebApi userWebApi = new UserWebApi();
        internal async Task<CUserInfo> Register(CUserInfo user)
        {
            try
            {
                HttpResponseMessage response = await userWebApi.RegisterAsync(user);
                CUserInfo loggedUser = response.Content.ReadAsAsync<CUserInfo>().Result;
                return loggedUser;
            }
            catch (Exception ex)
            {
                throw new WebApiException($"User {user.Name} couldn't register in MediaServer!", ex);
            }
        }

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

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName]string prop = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }
    }
}
