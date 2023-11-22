using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ChatApp.ViewModel.Command
{
    public class ConnectionCommand : ICommand
    {
        public event EventHandler? CanExecuteChanged;

        private readonly ChatWindowViewModel _viewModel;
        private string _choosenCommand;

        public ConnectionCommand(ChatWindowViewModel viewModel, string ChoosenCommand)
        {
            _viewModel = viewModel;
            _choosenCommand = ChoosenCommand;
        }

        public bool CanExecute(object? parameter)
        {
            // Your logic to determine if the command can execute
            return true; // or false based on your conditions
        }

        public void Execute(object? parameter)
        {
            System.Diagnostics.Debug.WriteLine($"choosen command is: {_choosenCommand}");
            if (_choosenCommand != null)
            {
                if (_choosenCommand.Equals("Accept"))
                {
                    _viewModel.networkManager.AcceptConnection();
                }else if (_choosenCommand.Equals("Deny")){
                    _viewModel.networkManager.DenyConnection() ;
                }
            }
        }

        // Call this method when you want to reevaluate whether the command can execute
        public void OnCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }

}