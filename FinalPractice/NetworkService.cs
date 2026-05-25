using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace FinalPractice
{
    internal class NetworkService : IDisposable
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private StreamWriter _writer;
        private StreamReader _reader;
        private Thread _receiveThread;

        public List<ChatMessage> GlobalMessages { get; } = new List<ChatMessage>();
        public List<ChatMessage> PrivateMessages { get; } = new List<ChatMessage>();
        public List<string> SystemAlerts { get; } = new List<string>();
        public List<string> ChatHistory { get; } = new List<string>();
        public Dictionary<string, int> ReadMessagesCount { get; } = new Dictionary<string, int>();

        public string MyNick { get; private set; } = "";
        public string MyColor { get; private set; } = "White";

        public event Action OnMessageReceived;

        public bool Connect(string ip, int port)
        {
            try
            {
                _client = new TcpClient(ip, port);
                _stream = _client.GetStream();
                _writer = new StreamWriter(_stream, Encoding.UTF8) { AutoFlush = true };
                _reader = new StreamReader(_stream, Encoding.UTF8);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Не вдалося підключитися до сервера: {ex.Message}");
                return false;
            }
        }

        public ChatMessage TryRegister(string nick, string email, string color)
        {
            MyNick = nick;
            MyColor = color;

            ChatMessage regRequest = new ChatMessage
            {
                Type = "REGISTER",
                Nick = MyNick,
                Color = MyColor,
                Email = email
            };

            SendToServer(regRequest);

            string jsonResponse = _reader.ReadLine()!;
            if (string.IsNullOrEmpty(jsonResponse)) return null;

            return JsonSerializer.Deserialize<ChatMessage>(jsonResponse);
        }

        public void StartListening()
        {
            _receiveThread = new Thread(ReceiveLoop) { IsBackground = true };
            _receiveThread.Start();
        }

        public void SendToServer(ChatMessage message)
        {
            if (_writer != null)
            {
                _writer.WriteLine(JsonSerializer.Serialize(message));
            }
        }

        private void ReceiveLoop()
        {
            try
            {
                while (true)
                {
                    string jsonResponse = _reader.ReadLine()!;
                    if (jsonResponse == null) break;

                    ChatMessage incomingMsg = JsonSerializer.Deserialize<ChatMessage>(jsonResponse)!;

                    if (incomingMsg.Type == "MESSAGE")
                    {
                        if (incomingMsg.ToNick.ToUpper() == "GLOBAL")
                        {
                            GlobalMessages.Add(incomingMsg);
                        }
                        else
                        {
                            string chatPartner = incomingMsg.Nick.ToLower() == MyNick.ToLower()
                                ? incomingMsg.ToNick
                                : incomingMsg.Nick;

                            bool alreadyExists = ChatHistory.Any(x => x.ToLower() == chatPartner.ToLower());
                            if (!string.IsNullOrEmpty(chatPartner) && !alreadyExists)
                            {
                                ChatHistory.Add(chatPartner);
                            }

                            PrivateMessages.Add(incomingMsg);
                        }
                    }
                    else if (incomingMsg.Type == "OFF_MSG")
                    {
                        SystemAlerts.Add(incomingMsg.Message);
                    }

                    OnMessageReceived?.Invoke();
                }
            }
            catch
            {
            }
        }

        public void Dispose()
        {
            _writer?.Dispose();
            _reader?.Dispose();
            _stream?.Dispose();
            _client?.Dispose();
        }
    }
}