using System.Windows.Input;
using System;
using ChatApp.Model;

namespace ChatApp.ViewModel.Command
{
    public class SendMessageCommand : ICommand
    {
        public event EventHandler? CanExecuteChanged;

        private readonly ChatWindowViewModel _viewModel;

        public SendMessageCommand(ChatWindowViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        public bool CanExecute(object? parameter)
        {
            return true; // Command can execute if there's text
        }

        public void Execute(object? parameter)
        {
            ChatMessage chatMessage = new ChatMessage
            {
                Header = "ChatMessage",
                Name = _viewModel.networkManager.UserName,
                Message = _viewModel.MessageText,
                Date = DateTime.Now
            };
            System.Diagnostics.Debug.WriteLine($"Am I server?: {_viewModel.isServer}");

            _viewModel.Messages.Add( chatMessage );

            string serializedMessage = _viewModel.networkManager.SerializeChatMessage(chatMessage);

            if (_viewModel.isServer)
            {
                _viewModel.networkManager.SendMessageFromServer(serializedMessage);
            }
            else
            {
                _viewModel.networkManager.SendMessageToServer(serializedMessage);
            }
            _viewModel.MessageText = ""; // Optionally clear the message after sending
        }

        public void OnCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
