using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ChatApp.ViewModel.Command
{
    public class ConnectToServerCommand : ICommand
    {
        public event EventHandler? CanExecuteChanged;

        private readonly MainWindowViewModel _viewModel;

        public ConnectToServerCommand(MainWindowViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        public bool CanExecute(object? parameter)
        {
            // Your logic to determine if the command can execute
            return true; // or false based on your conditions
        }

        public async void Execute(object? parameter)
        {
            if (_viewModel.ValidateFields(out string errorMessage))
            {
                if (int.TryParse(_viewModel.PortNumber, out int portNumber))
                {
                    string isConnectionSuccessful = await _viewModel.networkManager.ConnectToServer(_viewModel.IpAddress, portNumber);
                    if (isConnectionSuccessful == "Everything ok")
                    {
                        _viewModel.OpenChatWindow(_viewModel.networkManager, false); // Open the chat window
                        _viewModel.ValidationMessage = "";

                    }
                    else if(isConnectionSuccessful == "Denied")
                    {
                        _viewModel.ValidationMessage = "Server denied the request";
                    }
                    else if (isConnectionSuccessful == "No server found on the given ip")
                    {
                        _viewModel.ValidationMessage = "No server found on the given ip";
                    }
                    else if (isConnectionSuccessful == "Invalid IP address format.")
                    {
                        _viewModel.ValidationMessage = "Invalid IP address format.";
                    }
                } 
            }
            else
            {
                _viewModel.ValidationMessage = errorMessage; // Display validation errors
            }
        }

        // Call this method when you want to reevaluate whether the command can execute
        public void OnCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }

}