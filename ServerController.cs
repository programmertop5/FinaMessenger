using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace ServerMsg
{
    internal class ServerController
    {
        private readonly int _port;
        private TcpListener _tcpListener;

        public Dictionary<string, string> PendingConfirmations { get; } = new Dictionary<string, string>();
        public Dictionary<string, (TcpClient Client, StreamWriter Writer)> ActiveClients { get; } = new Dictionary<string, (TcpClient, StreamWriter)>();
        public List<ChatMessage> OfflineMessages { get; } = new List<ChatMessage>();

        public EmailService EmailService { get; } = new EmailService();
        public StorageService StorageService { get; } = new StorageService();

        public ServerController(int port)
        {
            _port = port;
        }

        public void Start()
        {
            StorageService.LoadHistory();

            _tcpListener = new TcpListener(IPAddress.Parse("127.0.0.1"), _port);
            _tcpListener.Start();
            Console.WriteLine($"Сервер запущено на порту {_port}. Очікування підключень...");

            while (true)
            {
                try
                {
                    TcpClient client = _tcpListener.AcceptTcpClient();
                    Console.WriteLine($"Підключився новий клієнт: {client.Client.RemoteEndPoint}");

                    ClientHandler handler = new ClientHandler(client, this);
                    Thread clientThread = new Thread(handler.Start) { IsBackground = true };
                    clientThread.Start();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Помилка підключення: {ex.Message}");
                }
            }
        }

        public void DisconnectClient(string nick)
        {
            if (string.IsNullOrEmpty(nick)) return;

            lock (ActiveClients)
            {
                if (ActiveClients.Remove(nick.ToLower()))
                {
                    Console.WriteLine($"Користувач {nick} покинув чат.");
                }
            }
        }

        public void BroadcastMessage(ChatMessage message)
        {
            string jsonResponse = JsonSerializer.Serialize(message);
            lock (ActiveClients)
            {
                foreach (var clientPair in ActiveClients)
                {
                    try { clientPair.Value.Writer.WriteLine(jsonResponse); } catch { }
                }
            }
        }

        public void SendPrivateMessage(ChatMessage message)
        {
            string jsonResponse = JsonSerializer.Serialize(message);
            string receiverNick = message.ToNick.ToLower();
            string senderNick = message.Nick.ToLower();

            lock (ActiveClients)
            {
                if (ActiveClients.ContainsKey(receiverNick))
                {
                    try
                    {
                        ActiveClients[senderNick].Writer.WriteLine(jsonResponse);
                        if (receiverNick != senderNick && ActiveClients.ContainsKey(receiverNick))
                        {
                            ActiveClients[receiverNick].Writer.WriteLine(jsonResponse);
                        }
                        Console.WriteLine($"{message.Nick} -> {message.ToNick}: {message.Message}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Помилка доставки привату: {ex.Message}");
                    }
                }
                else
                {
                    lock (OfflineMessages) { OfflineMessages.Add(message); }
                    Console.WriteLine($"Отримувач офлайн, повідомлення від {message.Nick} збережено");
                }
            }
        }

        public void SendChatHistoryToClient(StreamWriter writer, string clientNick)
        {
            string userNick = clientNick.ToLower();
            var allMsgs = StorageService.AllMessages;

            foreach (var oldMsg in allMsgs)
            {
                if (string.IsNullOrEmpty(oldMsg.ToNick) || oldMsg.ToNick.ToUpper() == "GLOBAL" ||
                    oldMsg.ToNick.ToLower() == userNick || oldMsg.Nick.ToLower() == userNick)
                {
                    writer.WriteLine(JsonSerializer.Serialize(oldMsg));
                }
            }
        }

        public void SendOfflineMessagesToClient(StreamWriter writer, string clientNick)
        {
            string userNick = clientNick.ToLower();
            List<ChatMessage> userOfflineMsg;

            lock (OfflineMessages)
            {
                userOfflineMsg = OfflineMessages.Where(m => m.ToNick.ToLower() == userNick).ToList();
                OfflineMessages.RemoveAll(m => m.ToNick.ToLower() == userNick);
            }

            if (userOfflineMsg.Count > 0)
            {
                var senders = userOfflineMsg.Select(m => m.Nick).Distinct();
                string senderList = string.Join(", ", senders);

                ChatMessage alertMessage = new ChatMessage
                {
                    Type = "OFF_MSG",
                    Message = $"У вас +{userOfflineMsg.Count} повідомлень від: {senderList}"
                };

                writer.WriteLine(JsonSerializer.Serialize(alertMessage));

                foreach (var offlineMsg in userOfflineMsg)
                {
                    writer.WriteLine(JsonSerializer.Serialize(offlineMsg));
                }
            }
        }
    }
}