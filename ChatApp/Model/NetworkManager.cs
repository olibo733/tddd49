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

        public event PropertyChangedEventHandler PropertyChanged;
        public event Action<bool> ConnectionRequest;
        public event Action<bool> ConnectionChanged;
        public event Action Disconnected;
        public event Action<ChatMessage> MessageReceived;

        public delegate void ConnectionRequestReceivedHandler(TcpClient client);
        public event ConnectionRequestReceivedHandler ConnectionRequestReceived;
        public event Action<string> ServerResponseReceived;

        private void OnPropertyChanged(string propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));

            }
        }

        private void OnConnectionChanged(bool isConnected)
        {
            System.Diagnostics.Debug.WriteLine("Chinging button vis");

            ConnectionChanged?.Invoke(isConnected);
        }

        //                      //
        //  Server logic below  //
        //                      //
        public bool StartServer(string serverIPAddress, int port)
        {
            Task.Factory.StartNew(() =>
            {
                if (!IPAddress.TryParse(serverIPAddress, out IPAddress ipAddress))
                {
                    System.Diagnostics.Debug.WriteLine("Invalid IP address format.");
                    return;
                }

                if (server != null && server.Server.IsBound)
                {
                    System.Diagnostics.Debug.WriteLine("Server is already running.");
                    return; // Server is already started
                }

                var ipEndPoint = new IPEndPoint(ipAddress, port);
                server = new TcpListener(ipEndPoint);
                

                try
                {
                    server.Start();
                    System.Diagnostics.Debug.WriteLine("Start listening...");

                    while (true) // Keep listening for new connections
                    {
                        TcpClient client = server.AcceptTcpClient();
                        this.serverClient = client;
                        System.Diagnostics.Debug.WriteLine("Connection request received!");

                        // Store the pending client and notify the ViewModel
                        pendingClient = client;
                        ConnectionRequest?.Invoke(true); // Notify ViewModel that a request is pending
                        Task.Factory.StartNew(() => ListenForClientResponse(client));

                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Error: " + ex.Message);
                }
            });

            return true;
        }

        public void Disconnect()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Disconnect request received!");

                var disconnectMessage = new ChatMessage
                {
                    Header = "Disconnect",
                    Name = this.UserName,
                    Message = "User has disconnected",
                    Date = DateTime.Now
                };

                string serializedMessage = SerializeChatMessage(disconnectMessage);
                byte[] messageBytes = Encoding.UTF8.GetBytes(serializedMessage);

                // Server Side Disconnect
                if (pendingClient != null)
                {
                    SendMessageFromServer(serializedMessage);
                    StopServer();
                }
                

                // Client Side Disconnect
                if (client != null && client.Connected)
                {
                    SendMessageToServer(serializedMessage);
                    NetworkStream clientStreams = client.GetStream();
                    clientStreams.Write(messageBytes, 0, messageBytes.Length);
                    clientStreams.Flush();
                    clientStreams?.Close();
                    client.Close();
                    client = null;
                    clientStream = null;
                }

                // Invoke the Disconnected event to notify the UI or other components
                Disconnected?.Invoke();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in Disconnect: {ex.Message}");
            }

            System.Diagnostics.Debug.WriteLine("Disconnect process completed.");
        }

        public void StopServer()
        {
            try
            {
                if (server != null)
                {
                    server.Stop();
                    server = null;
                }

                if (pendingClient != null)
                {
                    stream?.Close();
                    pendingClient.Close();
                    pendingClient = null;
                    stream = null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error stopping server: {ex.Message}");
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
                        ChatMessage serverResponse = DeserializeChatMessage(message);
                        System.Diagnostics.Debug.WriteLine($"Message on server side is: {message}");
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            ProcessReceivedChatMessage(serverResponse);
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
                MessageReceived?.Invoke(chatMessage);
            }
            else if (chatMessage.Header == "Disconnect")
            {
                System.Diagnostics.Debug.WriteLine($"Message recieved on client side is: {chatMessage.Message}");
            }
        }

        public void AcceptConnection()
        {
            System.Diagnostics.Debug.WriteLine("Accept the connection without the  pending client");
            if (pendingClient != null && pendingClient.Connected)
            {
                System.Diagnostics.Debug.WriteLine("Accept the connection");
                SendConnectionResponse("Accept");
                this.ConnectedClient = pendingClient;
                // Move the connection handling logic here
                ConnectionRequest?.Invoke(false); // Notify ViewModel that the request has been processed
                OnConnectionChanged(true);
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
                        Name = this.UserName,
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
                        Name = this.UserName,
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

        public bool ConnectToServer(string serverIPAddress, int port)
        {
            Task.Factory.StartNew(() =>
            {
                if (!IPAddress.TryParse(serverIPAddress, out IPAddress ipAddress))
                {
                    System.Diagnostics.Debug.WriteLine("Invalid IP address format.");
                    return false;
                }

                var serverEndpoint = new IPEndPoint(ipAddress, port);
                client = new TcpClient();

                try
                {
                    System.Diagnostics.Debug.WriteLine($"Attempting to connect to server at {serverEndpoint} ");
                    client.Connect(serverEndpoint);
                    System.Diagnostics.Debug.WriteLine("Connection request sent!");
                    
                    if (client.Connected)
                    {
                        clientStream = client.GetStream();
                        ListenForServerResponse(client);
                        return true;  // Indicates successful connection
                    }
                    return false;

                    // Here, you can add additional logic if needed, e.g., waiting for a server response
                    // ...

                    // Indicates the connection attempt was made
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Connection failed: {ex.Message}");
                    return false;
                }
            });

            return false; // Indicates the task to connect was started
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
            if (serverResponse.Header == "ConnectionRequestResponse")
            {
                if (serverResponse.Message == "Accept")
                {
                    ServerResponseReceived?.Invoke(serverResponse.Message);
                }
                else if (serverResponse.Message == "Deny")
                {
                    ServerResponseReceived?.Invoke(serverResponse.Message);
                }
            }else if(serverResponse.Header == "ChatMessage")
            {
                System.Diagnostics.Debug.WriteLine($"Message recieved on client side is: {serverResponse.Message}");
                MessageReceived?.Invoke(serverResponse);

            }
            else if(serverResponse.Header == "Disconnect")
            {
                System.Diagnostics.Debug.WriteLine($"Message recieved on client side is: {serverResponse.Message}");
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
    }

    public class ChatMessage
    {
        public string Header { get; set; }
        public string Name { get; set; }
        public string Message { get; set; }
        public DateTime Date { get; set; }
    }
}
