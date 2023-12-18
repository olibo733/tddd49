using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using ChatApp.Model;


namespace ChatApp.Model
{
    public class NetworkManager : INotifyPropertyChanged
    {

        public string UserName { get; set; }
        private NetworkStream stream;
        private TcpClient pendingClient; // Holds the reference to the pending connection
        private TcpClient serverClient;
        private TcpClient client; // Client instance
        private NetworkStream clientStream;
        private TcpListener server;
        private string sessiondID;

        public event PropertyChangedEventHandler PropertyChanged;
        public event Action<bool> ConnectionRequest;
        public event Action<bool, string> ServerConnectionChanged;
        public event Action<bool, string> ClientConnectionChanged;
        public event Action Disconnected;
        public event Action<string> UserDisconnected;
        public event Action<ChatMessage> MessageReceived;

        public delegate void ConnectionRequestReceivedHandler(TcpClient client);
        public event ConnectionRequestReceivedHandler ConnectionRequestReceived;
        public event Action<string> ServerResponseReceived;
        public event Action<string> OnConnectionEstablished;

        public void EstablishConnection(string connectedPartner)
        {
            OnConnectionEstablished?.Invoke(connectedPartner);
        }

        private void OnPropertyChanged(string propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));

            }
        }

        // Call this method when the client's connection status changes
        private void OnClientConnectionChanged(bool isConnected, string partnerName)
        {
            ClientConnectionChanged?.Invoke(isConnected, partnerName);
        }

        // Call this method when the server's connection status changes
        private void OnServerConnectionChanged(bool isConnected, string partnerName)
        {
            ServerConnectionChanged?.Invoke(isConnected, partnerName);
        }


        public ChatHistoryManager ChatHistoryManager { get; private set; }

        public NetworkManager(ChatHistoryManager chatHistoryManager)
        {
            ChatHistoryManager = chatHistoryManager;            
        }

        //                      //
        //  Server logic below  //
        //                      //
        public string StartServer(string serverIPAddress, int port)
        {
            // Check if the server is already running
            if (server != null && server.Server.IsBound)
            {
                System.Diagnostics.Debug.WriteLine("Server is already running.");
                return "Server is already running.";
            }

            if (!IPAddress.TryParse(serverIPAddress, out IPAddress ipAddress))
            {
                System.Diagnostics.Debug.WriteLine("Invalid IP address format.");
                return "Invalid IP address format.";
            }

            try
            {
                var ipEndPoint = new IPEndPoint(ipAddress, port);
                server = new TcpListener(ipEndPoint);

                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        server.Start();
                        System.Diagnostics.Debug.WriteLine("Server started and listening...");

                        while (true)
                        {
                            TcpClient client = server.AcceptTcpClient();
                            this.serverClient = client;
                            System.Diagnostics.Debug.WriteLine("Connection request received!");


                            pendingClient = client;
                            ConnectionRequest?.Invoke(true); // Notify ViewModel that a request is pending
                            Task.Factory.StartNew(() => ListenForClientResponse(client));

                            // Rest of the code to handle client connection
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("Error in server operation: " + ex.Message);
                        // Handle the exception
                    }
                }, TaskCreationOptions.LongRunning); // Use LongRunning for tasks that are expected to run a long time

                return "Everything ok"; // Server started successfully
            }
            catch (SocketException ex)
            {
                if (ex.SocketErrorCode == SocketError.AddressAlreadyInUse)
                {
                    System.Diagnostics.Debug.WriteLine("The address is already in use.");
                    return "The address is already in use.";
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Error starting server: " + ex.Message);
                    return "Error starting server";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error starting server: " + ex.Message);
                return "Error starting server"; // Error occurred while starting the server
            }
        }


        public void Disconnect(bool isServer)
        {
            try
            {
                var disconnectMessage = new ChatMessage
                {
                    Header = "Disconnect",
                    Sender = this.UserName,
                    Message = $"{(isServer ? "Server" : "Client")} has disconnected",
                    Date = DateTime.Now
                };

                string serializedMessage = SerializeChatMessage(disconnectMessage);

                if (isServer)
                {
                    // Notify connected clients about the server disconnecting
                    SendMessageFromServer(serializedMessage);
                    StopServer();
                }
                else // Client disconnecting
                {
                    // Notify the server about the client disconnecting
                    SendMessageToServer(serializedMessage);
                    CloseClientConnection();
                }

                // Invoke the Disconnected event
                //Disconnected?.Invoke();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in Disconnect: {ex.Message}");
            }
        }

        private void CloseClientConnection()
        {
            if (clientStream != null)
            {
                clientStream.Flush();
                clientStream.Close();
                clientStream = null;
            }

            if (client != null)
            {
                client.Close();
                client = null;
            }
        }

        public void StopServer()
        {
            try
            {
                if (server != null)
                {
                    // Optionally notify all connected clients about the server shutdown

                    server.Stop();
                    server = null;
                    System.Diagnostics.Debug.WriteLine("Server stopped successfully.");
                }

                ClosePendingClientConnection();
                
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error stopping server: {ex.Message}");
            }
        }

        private void ClosePendingClientConnection()
        {
            if (pendingClient != null)
            {
                stream?.Close();
                pendingClient.Close();
                pendingClient = null;
                stream = null;
            }
        }


        private void ListenForClientResponse(TcpClient client)
        {
            Task.Run(() =>
            {
                try
                {
                    NetworkStream stream = client.GetStream();
                    byte[] buffer = new byte[1024];

                    while (true)
                    {
                        int bytesRead = stream.Read(buffer, 0, buffer.Length);
                        if (bytesRead == 0)
                            break; // Client has disconnected

                        string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        ChatMessage clientResponse = DeserializeChatMessage(message);
                        System.Diagnostics.Debug.WriteLine($"Message on server side is: {message}");
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            
                            ProcessReceivedChatMessage(clientResponse);
                        });
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("ListenForClientResponse Error: " + ex.Message);
                }
                finally
                {
                    client.Close();
                }
            });
        }

        

        private void ProcessReceivedChatMessage(ChatMessage chatMessage)
        {
            // Add your logic to process the chat message here
            // For example, broadcasting the message to other clients or just logging it
            if(chatMessage.Header == "ChatMessage")
            {
                ChatHistoryManager.AddMessageToChatSession(this.sessiondID, chatMessage);
                MessageReceived?.Invoke(chatMessage);
            }
            else if (chatMessage.Header == "Disconnect")
            {

                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(chatMessage.Message, "Disconnection");
                    // Update UI or perform other actions as needed
                    
                });
                System.Diagnostics.Debug.WriteLine($"Disconnect is: {chatMessage.Message}");
                //UserDisconnected?.Invoke($"User {chatMessage.Sender} has disconnected.");
            }
            else if( chatMessage.Header == "SessionEstablished")
            {
                var currentSession = new ChatSession
                {
                    Participant1 = chatMessage.Sender,
                    Participant2 = chatMessage.Receiver,
                    Messages = new List<ChatMessage>(),
                    SessionId = Guid.NewGuid().ToString() // Create a new session ID
                };
                System.Diagnostics.Debug.WriteLine($"Clients name on server side is: {chatMessage.Sender}");

                //EstablishConnection(chatMessage.Sender);
                OnServerConnectionChanged(true, chatMessage.Sender);

                string SessionID = ChatHistoryManager.SaveChatSession(currentSession);
                this.sessiondID = SessionID;
                System.Diagnostics.Debug.WriteLine($"SessionID: {SessionID}");

                ChatMessage SessionIDResponse = new ChatMessage
                {
                    Header = "SessionIDResponse",
                    Sender = this.UserName,
                    Message = SessionID,
                    Date = DateTime.Now
                };
                string sessionID = SerializeChatMessage(SessionIDResponse);
                System.Diagnostics.Debug.WriteLine($"PendingClients: {pendingClient.Connected}");

                SendMessageFromServer(sessionID);
            }
            else if (chatMessage.Header == "PlaySoundRequest")
            {
                System.Diagnostics.Debug.WriteLine($"play sound on server");
                System.Media.SoundPlayer player = new System.Media.SoundPlayer(@"C:\Users\olive\Documents\Programmering\tddd49\ChatApp\blip.wav");
                player.Play();
            }
        }

        public void AcceptConnection()
        {
            if (pendingClient != null && pendingClient.Connected)
            {
                System.Diagnostics.Debug.WriteLine("Accept the connection");
                SendConnectionResponse("Accept");
                this.ConnectedClient = pendingClient;
                // Move the connection handling logic here
                ConnectionRequest?.Invoke(false); // Notify ViewModel that the request has been processed
            }
        }

        public void DenyConnection()
        {
            System.Diagnostics.Debug.WriteLine("Deny the connection without the pending client");
            if (pendingClient != null)
            {
                SendConnectionResponse("Deny");
                pendingClient.Close();
                pendingClient = null; // Clear the reference
                ConnectionRequest?.Invoke(false); // Notify ViewModel that the request has been processed
            }
        }

        private void SendConnectionResponse(string message)
        {
            string ServerResponse = "";
            if (message != null)
            {

                if (message.Equals("Accept"))
                {
                    ChatMessage ServerConnectionResponse = new ChatMessage
                    {
                        Header = "ConnectionRequestResponse",
                        Sender = this.UserName,
                        Message = "Accept",
                        Date = DateTime.Now
                    };
                    ServerResponse = SerializeChatMessage(ServerConnectionResponse);
                }
                else if (message.Equals("Deny"))
                {
                    ChatMessage ServerConnectionResponse = new ChatMessage
                    {
                        Header = "ConnectionRequestResponse",
                        Sender = this.UserName,
                        Message = "Deny",
                        Date = DateTime.Now
                    };
                    ServerResponse = SerializeChatMessage(ServerConnectionResponse);
                }
                
            }
            if(ServerResponse != "")
            {
                SendMessageFromServer(ServerResponse);
            }
            
        }
       

        public void SendMessageFromServer(string message)
        {
            if (pendingClient != null && pendingClient.Connected)
            {
                NetworkStream stream = pendingClient.GetStream();
                byte[] messageBytes = Encoding.UTF8.GetBytes(message);
                stream.Write(messageBytes, 0, messageBytes.Length);
                System.Diagnostics.Debug.WriteLine($"Sending message {message} to {pendingClient.ToString}");
            }
        }

        //                      //
        //  Client logic below  //
        //                      //

        public async Task<string> ConnectToServer(string serverIPAddress, int port)
        {
            if (!IPAddress.TryParse(serverIPAddress, out IPAddress ipAddress))
            {
                System.Diagnostics.Debug.WriteLine("Invalid IP address format.");
                return "Invalid IP address format.";
            }

            var serverEndpoint = new IPEndPoint(ipAddress, port);
            client = new TcpClient();

            try
            {
                System.Diagnostics.Debug.WriteLine($"Attempting to connect to server at {serverEndpoint}");
                await client.ConnectAsync(serverEndpoint.Address, serverEndpoint.Port);
                System.Diagnostics.Debug.WriteLine("Connection request sent!");

                if (client.Connected)
                {
                    clientStream = client.GetStream();

                    // Optionally, you can add a timeout for the response
                    var response = await ReadResponseFromServer();
                    System.Diagnostics.Debug.WriteLine($"Response is: {response}");
                    ChatMessage chatMessage = DeserializeChatMessage(response);
                    System.Diagnostics.Debug.WriteLine($"Chatmessage.message is: {chatMessage.Message}");

                    // Here, you should include your logic to determine if the response
                    // indicates a successful connection.
                    // For example, you might expect a specific message from the server.
                    if (chatMessage.Header == "ConnectionRequestResponse" && chatMessage.Message == "Accept")
                    {
                        ServerResponseReceived?.Invoke(chatMessage.Message);
                        System.Diagnostics.Debug.WriteLine($"Client is is: {this.UserName}");

                        var currentSession = new ChatSession
                        {
                            Participant1 = chatMessage.Sender,
                            Participant2 = this.UserName,
                            Messages = new List<ChatMessage>(),
                            SessionId = Guid.NewGuid().ToString() // Create a new session ID
                        };

                        // Notify server with both usernames
                        ChatMessage notifyServerWithNames = new ChatMessage
                        {
                            Header = "SessionEstablished",
                            Sender = this.UserName,        // client's username
                            Receiver = chatMessage.Sender, // server's username
                            Message = "Session established between " + this.UserName + " and " + chatMessage.Sender,
                            Date = DateTime.Now
                        };
                        System.Diagnostics.Debug.WriteLine($"Servers name on client side is: {chatMessage.Sender}");
                        //EstablishConnection(chatMessage.Sender);
                        string notifyString = SerializeChatMessage(notifyServerWithNames);
                        SendMessageToServer(notifyString);
                        
                        ListenForServerResponse(client);
                        return "Everything ok";
                    }else if(chatMessage.Header == "ConnectionRequestResponse" && chatMessage.Message == "Deny")
                    {
                        ServerResponseReceived?.Invoke(chatMessage.Message);
                        return "Denied";
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Connection failed: {ex.Message}");
            }

            return "No server found on the given ip";
        }

        private async Task<string> ReadResponseFromServer()
        {
            if (client == null || !client.Connected || clientStream == null)
            {
                throw new InvalidOperationException("Not connected to a server.");
            }

            byte[] buffer = new byte[1024];
            try
            {
                int bytesRead = await clientStream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead > 0)
                {
                    string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    System.Diagnostics.Debug.WriteLine($"Received response: {response}");
                    return response;
                }
                else
                {
                    return string.Empty; // No data received, or connection closed
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error reading response from server: {ex.Message}");
                return string.Empty; // Return empty string or handle the exception as needed
            }
        }



        public void SendMessageToServer(string message)
        {
            if (client != null && client.Connected) // Use client instance
            {
                NetworkStream stream = client.GetStream(); // Get client's own stream
                
                byte[] messageBytes = Encoding.UTF8.GetBytes(message);
                stream.Write(messageBytes, 0, messageBytes.Length);
                stream.Flush();
            }
        }

        private string ReadMessageFromServer(TcpClient client)
        {
            if (client == null || !client.Connected)
            {
                return null; // or handle this situation appropriately
            }

            NetworkStream stream = client.GetStream();
            if (stream.DataAvailable)
            {
                byte[] buffer = new byte[1024]; // Buffer size can be adjusted as needed
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                return Encoding.UTF8.GetString(buffer, 0, bytesRead);
            }

            return null; // No data available to read
        }

        private void ListenForServerResponse(TcpClient client)
        {
            Task.Run(() =>
            {
                try
                {
                    while (true)
                    {
                        string message = ReadMessageFromServer(client);
                        if (!string.IsNullOrEmpty(message))
                        {
                            ChatMessage serverResponse = DeserializeChatMessage(message);
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                ProcessServerMessage(serverResponse);
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Handle exceptions
                    // Update UI with error message if needed
                }
            });
        }

        private void ProcessServerMessage(ChatMessage serverResponse)
        {
           if(serverResponse.Header == "ChatMessage")
            {
                System.Diagnostics.Debug.WriteLine($"Message recieved on client side is: {serverResponse.Message}");
                ChatHistoryManager.AddMessageToChatSession(this.sessiondID, serverResponse);

                MessageReceived?.Invoke(serverResponse);

            }
            else if(serverResponse.Header == "Disconnect")
            {
                System.Diagnostics.Debug.WriteLine($"Message recieved on client side is: {serverResponse.Message}");
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(serverResponse.Message, "Disconnection");
                    // Update UI or perform other actions as needed
                    //IsConnected = false;
                });
                //Disconnect(false);
                //Disconnected?.Invoke();
            }
            else if(serverResponse.Header == "SessionIDResponse")
            {
                this.sessiondID = serverResponse.Message;
                System.Diagnostics.Debug.WriteLine($"serverResponse.sender is: {serverResponse.Sender}");

                OnClientConnectionChanged(true, serverResponse.Sender);
            }
            else if(serverResponse.Header == "PlaySoundRequest")
            {
                System.Diagnostics.Debug.WriteLine($"play sound on client");
                System.Media.SoundPlayer player = new System.Media.SoundPlayer(@"C:\Users\olive\Documents\Programmering\tddd49\ChatApp\blip.wav");
                player.Play();
            }
            // Handle other types of messages as needed
        }

        //
        // Helper functions
        //

        public string SerializeChatMessage(ChatMessage chatMessage)
        {
            return JsonSerializer.Serialize(chatMessage);
        }

        public ChatMessage DeserializeChatMessage(string jsonString)
        {
            return JsonSerializer.Deserialize<ChatMessage>(jsonString);
        }
        public TcpClient ConnectedClient
        {
            get { return this.serverClient; } // Return the private field 'client'
            set
            {
                this.serverClient = value; // Set the private field 'client'
                OnPropertyChanged(nameof(ConnectedClient));
            }
        }

        public TcpClient ServerClient
        {
            get { return pendingClient; } // Return the private field 'pendingClient'
            set
            {
                pendingClient = value; // Set the private field 'pendingClient'
                OnPropertyChanged(nameof(ServerClient));
            }
        }

        public void SendPlaySoundRequest(bool isServer)
        {
            var playSoundMessage = new ChatMessage
            {
                Header = "PlaySoundRequest",
                Sender = this.UserName,
                Message = "Play sound",
                Date = DateTime.Now
            };

            string serializedMessage = SerializeChatMessage(playSoundMessage);
            if (isServer)
            {
                SendMessageFromServer(serializedMessage);
            }
            else
            {
                SendMessageToServer(serializedMessage);
            }
        }
    }

    public class ChatMessage
    {
        public string Header { get; set; }
        public string Sender { get; set; }
        public string Receiver { get; set; }
        public string Message { get; set; }
        public DateTime Date { get; set; }
    }
}
