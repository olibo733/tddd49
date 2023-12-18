using ChatApp.Model;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows;
using ChatApp.ViewModel.Command;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Text.Json;
using System.IO;
using System.Linq;


/// <summary>
/// TODO: Override the X-button
/// </summary>
public class ChatWindowViewModel : INotifyPropertyChanged
{
    private bool isConnected;
    private bool isConnectionRequestPending;
    public bool isServer;
    public string userName;
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
                OnPropertyChanged(nameof(IsConnected));
                OnPropertyChanged(nameof(CanSendMessage));
                //(SendMessageCommand as SendMessageCommand)?.OnCanExecuteChanged();

            }
        }
    }

    public string _serverConnectedPartner;
    public string _clientConnectedPartner;

    public string CurrentConnectedPartner
    {
        get => isServer ? _serverConnectedPartner : _clientConnectedPartner;
    }

    public bool CanSendMessage
    {
        get
        {
            var canSend = !string.IsNullOrEmpty(_serverConnectedPartner) || !string.IsNullOrEmpty(_clientConnectedPartner);
            System.Diagnostics.Debug.WriteLine($"CanSendMessage: {canSend}, ServerPartner: {_serverConnectedPartner}, ClientPartner: {_clientConnectedPartner}");
            return canSend;
        }
    }

    private bool _CanSendMessageToOtherUser; // Backing field

    public bool canSendMessageToOtherUser
    {
        get => _CanSendMessageToOtherUser; // Use the backing field
        set
        {
            if (_CanSendMessageToOtherUser != value) // Check against the backing field
            {
                _CanSendMessageToOtherUser = value; // Set the backing field
                OnPropertyChanged(nameof(canSendMessageToOtherUser)); // Notify property change
                System.Diagnostics.Debug.WriteLine($"QuickFic: {canSendMessageToOtherUser}, ServerPartner: {_serverConnectedPartner}, ClientPartner: {_clientConnectedPartner}");
                (SendMessageCommand as SendMessageCommand)?.OnCanExecuteChanged();
                (PlaySoundCommand as PlaySoundCommand)?.OnCanExecuteChanged();

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

    private string _searchQuery;
    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            if (_searchQuery != value)
            {
                _searchQuery = value;
                OnPropertyChanged();
                FilterConversationPartners(); // Call the filter method when the query changes
            }
        }
    }

    private string _messageText;
    public string MessageText
    {
        get => _messageText;
        set
        {
            if (_messageText != value)
            {
                _messageText = value;
                OnPropertyChanged(nameof(MessageText));
            }
        }
    }

    public string _selectedConversationPartner;
    public string SelectedConversationPartner
    {
        get => _selectedConversationPartner;
        set
        {
            if (_selectedConversationPartner != value)
            {
                _selectedConversationPartner = value;
                System.Diagnostics.Debug.WriteLine($"Current selected partner is:{_selectedConversationPartner} from {this.userName}");

                OnPropertyChanged();
                LoadConversationHistory(value); // Load the conversation when a new partner is selected
                (SendMessageCommand as SendMessageCommand)?.OnCanExecuteChanged();

                //(SendMessageCommand as SendMessageCommand)?.OnCanExecuteChanged();
            }
        }
    }

    public ObservableCollection<ChatMessage> Messages { get; private set; }
    public ObservableCollection<string> SearchedConversationPartners { get; private set; }

    public ICommand SendMessageCommand { get; private set; }
    public ICommand DisconnectCommand { get; private set; }
    public ICommand StartServerCommand { get; private set; }
    public ICommand ConnectToServerCommand { get; private set; }
    public ICommand AcceptCommand { get; private set; }
    public ICommand DenyCommand { get; private set; }
    public ICommand PlaySoundCommand { get; private set; }

    public ChatWindowViewModel(NetworkManager networkManager, bool isServer, string userName)
    {
        this.networkManager = networkManager;
        this.networkManager.UserName = userName;
        this.isServer = isServer;
        this.userName = userName;
        Messages = new ObservableCollection<ChatMessage>();
        SearchedConversationPartners = new ObservableCollection<string>();

        DisconnectCommand = new DisconnectCommand(this);
        AcceptCommand = new ConnectionCommand(this, "Accept");
        DenyCommand = new ConnectionCommand(this, "Deny");
        SendMessageCommand = new SendMessageCommand(this);
        PlaySoundCommand = new PlaySoundCommand(this);

        LoadAllConversationPartners();


        this.networkManager.ConnectionRequest += (isPending) => IsConnectionRequestPending = isPending;
        this.networkManager.MessageReceived += ProcessReceivedChatMessage;
        this.networkManager.Disconnected += OnDisconnected;
        this.networkManager.ServerConnectionChanged += OnServerConnectionChanged;
        this.networkManager.ClientConnectionChanged += OnClientConnectionChanged;
    }

    public void Disconnect()
    {
        networkManager.Disconnect(isServer);
        IsConnected = false;

        RequestClose?.Invoke();
    }

    private void ProcessReceivedChatMessage(ChatMessage chatMessage)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            Messages.Add(chatMessage);
            UpdateConversationList();
        });
    }
    private void OnServerConnectionChanged(bool status, string partnerName)
    {
        //IsConnected = isConnected;
        System.Diagnostics.Debug.WriteLine($"Servername is: {partnerName}");

        _serverConnectedPartner = partnerName;
        if (status)
        {
            ResetChat();
        }
        canSendMessageToOtherUser = true;
        //UpdateCanSendMessage();
    }

    private void OnClientConnectionChanged(bool status, string partnerName)
    {
        //IsConnected = isConnected;
        System.Diagnostics.Debug.WriteLine($"Clientname is: {partnerName}");

        _clientConnectedPartner = partnerName;
        if (status)
        {
            ResetChat();
        }
        canSendMessageToOtherUser = true;

        //UpdateCanSendMessage();
    }

    private void ResetChat()
    {
        Messages.Clear(); // Clears the message list
        MessageText = string.Empty; // Clears the message input box
        OnPropertyChanged(nameof(Messages));
        OnPropertyChanged(nameof(MessageText));
    }


    private void OnDisconnected()
    {
        IsConnected = false;
        System.Diagnostics.Debug.WriteLine($"Disconnet request of {isServer}");
        MessageBox.Show("The other party has disconnected.", "Disconnected");
        // Handle additional logic when disconnected
    }


    // Implementation of INotifyPropertyChanged
    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }


    public void LoadConversationHistory(string otherUserName)
    {
        Messages.Clear();

        var sessions = networkManager.ChatHistoryManager.LoadChatHistory(userName);

        foreach (var session in sessions)
        {
            if (session.Participant1 == otherUserName || session.Participant2 == otherUserName)
            {
                foreach (var message in session.Messages)
                {
                    Messages.Add(message);
                }
            }
        }
    }

    private void UpdateConversationList()
    {
        LoadAllConversationPartners(); // Reload the conversation partners list
    }
    private List<string> AllConversationPartners { get; set; }
    private void FilterConversationPartners()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            // If the search query is empty, show all partners
            SearchedConversationPartners = new ObservableCollection<string>(AllConversationPartners);
        }
        else
        {
            // Filter the list based on the search query
            var filteredPartners = AllConversationPartners
                .Where(partner => partner.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase))
                .ToList();

            SearchedConversationPartners = new ObservableCollection<string>(filteredPartners);
        }

        OnPropertyChanged(nameof(SearchedConversationPartners));
    }
    private void LoadAllConversationPartners()
    {
        var partners = networkManager.ChatHistoryManager.GetAllConversationPartners(userName);
        AllConversationPartners = partners.ToList(); // Store all partners
        SearchedConversationPartners.Clear();
        foreach (var partner in partners)
        {
            SearchedConversationPartners.Add(partner);
        }
    }

}


