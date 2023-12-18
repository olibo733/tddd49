using ChatApp.ViewModel.Command;
using System;
using System.Windows.Input;

namespace ChatApp.ViewModel.Command
{
    public class PlaySoundCommand : ICommand
    {
        public event EventHandler? CanExecuteChanged;

        private readonly ChatWindowViewModel _viewModel;

        public PlaySoundCommand(ChatWindowViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        public bool CanExecute(object? parameter)
        {
            if (_viewModel._clientConnectedPartner != null || _viewModel._serverConnectedPartner != null)
            {
                if (_viewModel._selectedConversationPartner == null)
                {
                    return true;
                }
                else if (_viewModel._selectedConversationPartner == _viewModel._clientConnectedPartner || _viewModel._selectedConversationPartner == _viewModel._serverConnectedPartner)
                {
                    return true;
                }
                else
                {
                    return false;
                }

            }
            return false;
        }

        public void Execute(object? parameter)
        {
            _viewModel.networkManager.SendPlaySoundRequest(_viewModel.isServer); // Method in ViewModel to handle this action
        }

        public void OnCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
