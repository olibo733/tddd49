using System.ComponentModel;
using System.Runtime.CompilerServices;
using ChatApp.Model;
using System.Windows.Input;
using ChatApp.ViewModel.Command;


namespace ChatApp.ViewModel
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        public NetworkManager networkManager;

        private string userName;
        private string ipAddress;
        private string portNumber;
        private string validationMessage;

        public ICommand StartServerCommand { get; private set; }
        public ICommand ConnectToServerCommand { get; private set; }


        public MainWindowViewModel(NetworkManager networkManager)
        {
            this.networkManager = networkManager;
            this.networkManager.ServerResponseReceived += OnServerResponseReceived;


            StartServerCommand = new StartServerCommand(this);
            ConnectToServerCommand = new ConnectToServerCommand(this);
        }


        public string UserName
        {
            get { return userName; }
            set
            {
                if (userName != value)
                {
                    userName = value;
                    OnPropertyChanged();

                    // Update the UserName in networkManager when the property changes
                    networkManager.UserName = userName;
                }
            }
        }

        public string IpAddress
        {
            get { return ipAddress; }
            set
            {
                if (ipAddress != value)
                {
                    ipAddress = value;
                    OnPropertyChanged();
                }
            }
        }

        public string PortNumber
        {
            get { return portNumber; }
            set
            {
                if (portNumber != value)
                {
                    portNumber = value;
                    OnPropertyChanged();
                }
            }
        }

        public void OpenChatWindow(NetworkManager networkManager, bool isServer)
        {
            System.Diagnostics.Debug.WriteLine($"Name: {userName}");

            ChatWindowViewModel chatViewModel = new ChatWindowViewModel(networkManager, isServer, userName);
            var chatWindow = new ChatWindow(chatViewModel);
            chatWindow.Show();

        }

        

        


        private void OnServerResponseReceived(string responseMessage)
        {
            if (responseMessage == "Deny")
            {
                ValidationMessage = "Server denied the request.";
            }
            else if (responseMessage == "Accept")
            {
                //OpenChatWindow(this.networkManager, false);
                // You might want to take additional actions here for an accepted connection
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public string ValidationMessage
        {
            get { return validationMessage; }
            set
            {
                if (validationMessage != value)
                {
                    validationMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool ValidateFields(out string errorMessage)
        {
            errorMessage = "";
            if (string.IsNullOrWhiteSpace(UserName))
            {
                errorMessage += "User Name is required.\n";
            }

            if (string.IsNullOrWhiteSpace(IpAddress))
            {
                errorMessage += "A valid IP Address is required.\n";
            }

            if (string.IsNullOrWhiteSpace(PortNumber) || !int.TryParse(PortNumber, out int _))
            {
                errorMessage += "A valid Port Number is required.\n";
            }

            return string.IsNullOrEmpty(errorMessage);
        }

       
    }
}
