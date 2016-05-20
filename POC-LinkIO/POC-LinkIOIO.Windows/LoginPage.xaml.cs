using link.io.csharp;
using link.io.csharp.exception;
using link.io.csharp.model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

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
        private string room;

        public LoginPage()
        {
            InitializeComponent();
            DataContext = this;
            Room = "abcd";
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

        public string Room
        {
            get
            {
                return room;
            }
            set
            {
                room = value;
                OnPropertyChanged("Room");
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
         
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (Room != null && Room != "")
            {
                LinkIOSetup.Instance.create().connectTo(server).withAPIKey(api_key).withMail(login).withPassword(Password.Password).withErrorHandler((Exception ex) => DisplayErrorMessage(ex)).connect(async (link.io.csharp.LinkIO lio) =>
                {
                    lio.joinRoom(room, (string a, List<User> b) => { });
                    await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        Frame rootFrame = Window.Current.Content as Frame;
                        if (!((Frame)Window.Current.Content).Navigate(typeof(WhiteBoardPage),  lio))
                        {
                            throw new Exception("Failed to go to the next page.");
                        }
                    });
                });
            }
            else
            {
                ErrorMessage.Text = "You have to choose a room.";
            }
        }

        private async void DisplayErrorMessage(Exception e)
        {
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                if (e is AccountNotFoundException)
                {
                    ErrorMessage.Text = e.Message;
                }
                else if (e is WrongPasswordException)
                {
                    ErrorMessage.Text = e.Message;
                }
                else if (e is WrongAPIKeyException)
                {
                    ErrorMessage.Text = e.Message;
                }
            });
        }
    }
}