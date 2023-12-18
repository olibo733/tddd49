using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ChatApp.ViewModel.Command
{
    public class StartServerCommand : ICommand
    {
        public event EventHandler? CanExecuteChanged;

        private readonly MainWindowViewModel _viewModel;

        public StartServerCommand(MainWindowViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        public bool CanExecute(object? parameter)
        {
            // Your logic to determine if the command can execute
            return true; // or false based on your conditions
        }

        public void Execute(object? parameter)
        {
            // Convert the PortNumber from string to int.
           
            if (_viewModel.ValidateFields(out string errorMessage))
            {
                if (int.TryParse(_viewModel.PortNumber, out int portNumber))
                {
                    string startServerTask = _viewModel.networkManager.StartServer(_viewModel.IpAddress, portNumber);
                    if (startServerTask == "Everything ok")
                    {
                        _viewModel.OpenChatWindow(_viewModel.networkManager, true); // Open the chat window
                    }
                    else if(startServerTask == "Server is already running.")
                    {
                        _viewModel.ValidationMessage = "Server is already running";
                    }else if(startServerTask == "The address is already in use.")
                    {
                        _viewModel.ValidationMessage = "The address is already in use.";
                    }else if(startServerTask == "Error starting server")
                    {
                        _viewModel.ValidationMessage = "Error starting server";
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