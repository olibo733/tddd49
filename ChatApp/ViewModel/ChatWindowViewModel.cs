using ChatApp.Model;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows;
using ChatApp.ViewModel.Command;
using System;

public class ChatWindowViewModel : INotifyPropertyChanged
{
    private bool isConnected;
    private bool isConnectionRequestPending;
    public bool isServer;
    public NetworkManager networkManager;
    public event Action RequestClose;

    public bool IsConnected
    {
        get => isConnected;
        set
        {
            if (isConnected != value)
            {
                isConnected = value;
                OnPropertyChanged();
            }

        }
    }
    
    public bool IsConnectionRequestPending
    {
        get => isConnectionRequestPending;
        set
        {
            if (isConnectionRequestPending != value)
            {
                isConnectionRequestPending = value;
                OnPropertyChanged();
            }
        }
    }

    public string MessageText { get; set; }

    public ICommand SendMessageCommand { get; private set; }
    public ICommand DisconnectCommand { get; private set; }
    public ICommand StartServerCommand { get; private set; }
    public ICommand ConnectToServerCommand { get; private set; }
    public ICommand AcceptCommand { get; private set; }
    public ICommand DenyCommand { get; private set; }

    public ChatWindowViewModel(NetworkManager networkManager, bool isServer)
    {
        this.networkManager = networkManager;
        this.isServer = isServer;

        DisconnectCommand = new DisconnectCommand(this);
        AcceptCommand = new ConnectionCommand(this, "Accept");
        DenyCommand = new ConnectionCommand(this, "Deny");
        SendMessageCommand = new SendMessageCommand(this);

        this.networkManager.ConnectionRequest += (isPending) => IsConnectionRequestPending = isPending;

        // Subscribe to networkManager events to update IsConnected
        this.networkManager.Disconnected += OnDisconnected;
        this.networkManager.ConnectionChanged += OnConnectionChanged;
        // You might also want to set IsConnected based on initial connection status
    }

    public void Disconnect()
    {
        networkManager.Disconnect();
        IsConnected = false;

        RequestClose?.Invoke();
    }

    private void OnConnectionChanged(bool isConnected)
    {
        IsConnected = isConnected;
    }

    private void OnDisconnected()
    {
        IsConnected = false;
        // Handle additional logic when disconnected
    }


    // Implementation of INotifyPropertyChanged
    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    // ... other members and methods ...
}
