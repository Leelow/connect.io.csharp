using link.io.csharp;
using link.io.csharp.model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace POC_LinkIO
{
    /// <summary>
    /// Une page vide peut être utilisée seule ou constituer une page de destination au sein d'un frame.
    /// </summary>
    public sealed partial class LoginPage : Page, INotifyPropertyChanged
    {
        private string server = "link-io.insa-rennes.fr:443";
        private string api_key = "BCHY8PwT8foOpn23lJLL";
        private string login;

        public LoginPage()
        {
            InitializeComponent();
            DataContext = this;
        }

        public string Login
        {
            get
            {
                return login;
            }
            set
            {
                login = value;
                OnPropertyChanged("Login");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(String name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            LinkIOSetup.Instance.create().connectTo(server).withAPIKey(api_key).withMail(login).withPassword(Password.Password).connect(async (link.io.csharp.LinkIO lio) =>
            {
                lio.joinRoom("abcd", (string a, List<User> b) => { });
                await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    Frame rootFrame = Window.Current.Content as Frame;
                    if (!((Frame)Window.Current.Content).Navigate(typeof(WhiteBoardPage), lio))
                    {
                        throw new Exception("Failed to go to the next page.");
                    }
                });
                
            });

            
        }
    }
}
