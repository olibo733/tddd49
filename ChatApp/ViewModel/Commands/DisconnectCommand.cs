using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ChatApp.ViewModel.Command
{
    public class DisconnectCommand : ICommand
    {
        public event EventHandler? CanExecuteChanged;

        public ChatWindowViewModel _viewModel;
        

        public DisconnectCommand(ChatWindowViewModel viewModel)
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
            _viewModel.Disconnect();
        }

        // Call this method when you want to reevaluate whether the command can execute
        public void OnCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }

}