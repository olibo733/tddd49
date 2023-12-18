using ChatApp.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
// Other using statements if necessary


namespace ChatApp.Model
{
    public class ChatHistoryManager
    {
        private const string HistoryFilePath = "chat_history.json";

        public string SaveChatSession(ChatSession session)
        {
            ChatHistory history;

            if (File.Exists("chat_history.json"))
            {
                string json = File.ReadAllText("chat_history.json");
                history = JsonSerializer.Deserialize<ChatHistory>(json);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("creating file");
                history = new ChatHistory();
            }
            System.Diagnostics.Debug.WriteLine("Does the file already exists?");

            // Check if the session already exists
            var existingSession = history.Sessions.FirstOrDefault(s => s.SessionId == session.SessionId);
            if (existingSession != null)
            {
                // Update existing session if needed
                // For example, update messages or other details
            }
            else
            {
                // Add new session
                history.Sessions.Add(session);
            }

            // Save updated history
            string updatedJson = JsonSerializer.Serialize(history);
            File.WriteAllText("chat_history.json", updatedJson);
            return session.SessionId;
        }


        public List<ChatMessage> GetConversationMessages(string user1, string user2)
        {
            var history = LoadAllChatHistory();
            return history.Sessions
                .Where(session => (session.Participant1 == user1 && session.Participant2 == user2) ||
                                  (session.Participant1 == user2 && session.Participant2 == user1))
                .SelectMany(session => session.Messages)
                .ToList();
        }

        public void AddMessageToChatSession(string sessionId, ChatMessage newMessage)
        {
            var history = LoadAllChatHistory();

            // Find the session by its ID
            var session = history.Sessions.FirstOrDefault(s => s.SessionId == sessionId);
            if (session != null)
            {
                // Add the new message to the session
                session.Messages.Add(newMessage);

                // Save the updated history
                SaveChatHistory(history);
            }
            else
            {
                // Handle the case where the session is not found, if necessary
            }
        }
        private void SaveChatHistory(ChatHistory history)
        {
            string updatedJson = JsonSerializer.Serialize(history);
            File.WriteAllText(HistoryFilePath, updatedJson);
        }

        public List<ChatSession> LoadChatHistory(string userName)
        {
            var history = LoadAllChatHistory();
            return history.Sessions.Where(session => session.Participant1 == userName || session.Participant2 == userName).ToList();
        }

        private ChatHistory LoadAllChatHistory()
        {
            if (File.Exists(HistoryFilePath))
            {
                string json = File.ReadAllText(HistoryFilePath);
                return JsonSerializer.Deserialize<ChatHistory>(json);
            }
            return new ChatHistory();
        }

        public List<string> GetAllConversationPartners(string currentUserName)
        {
            var allPartners = new List<string>();
            var history = LoadAllChatHistory();

            foreach (var session in history.Sessions)
            {
                // Add the other participant of each session to the list
                if (session.Participant1 == currentUserName && !allPartners.Contains(session.Participant2))
                {
                    allPartners.Add(session.Participant2);
                }
                else if (session.Participant2 == currentUserName && !allPartners.Contains(session.Participant1))
                {
                    allPartners.Add(session.Participant1);
                }
            }

            return allPartners;
        }

        // Additional methods...
    }

    public class ChatHistory
    {
        public List<ChatSession> Sessions { get; set; } = new List<ChatSession>();
    }

    public class ChatSession
    {
        public string SessionId { get; set; }
        public List<ChatMessage> Messages { get; set; }
        public string Participant1 { get; set; }
        public string Participant2 { get; set; }

    }
}

