using ChatApp.ViewModel;
using System;
using System.Windows;

namespace ChatApp
{
    /// <summary>
    /// Interaction logic for ChatWindow.xaml
    /// </summary>
    public partial class ChatWindow : Window
    {
        public ChatWindow(ChatWindowViewModel viewModel)
        {
            InitializeComponent();

            // Set the DataContext of the window to the ChatWindowViewModel
            this.DataContext = viewModel;

            if (viewModel != null)
            {
                viewModel.RequestClose += CloseWindow;
            }
        }

        private void CloseWindow()
        {
            Close(); // Or this.Hide() if you just want to hide the window
        }

        // Ensure to unsubscribe from the event when the window is closed
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            if (DataContext is ChatWindowViewModel viewModel)
            {
                viewModel.RequestClose -= CloseWindow;
            }
        }
    }
}
