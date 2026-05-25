using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace ServerMsg
{
    internal class ClientHandler
    {
        private readonly TcpClient _client;
        private readonly ServerController _server;
        private string _currentClientNick = "";

        public ClientHandler(TcpClient client, ServerController server)
        {
            _client = client;
            _server = server;
        }

        public void Start()
        {
            try
            {
                using NetworkStream stream = _client.GetStream();
                using StreamReader reader = new StreamReader(stream, Encoding.UTF8);
                using StreamWriter writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

                while (true)
                {
                    string jsonRequest = reader.ReadLine()!;
                    if (jsonRequest == null) return;

                    ChatMessage clientMsg = JsonSerializer.Deserialize<ChatMessage>(jsonRequest)!;

                    if (clientMsg.Type == "REGISTER")
                    {
                        HandleRegistration(clientMsg, writer);
                    }
                    else if (clientMsg.Type == "MESSAGE")
                    {
                        HandleMessage(clientMsg);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка клієнта [{_currentClientNick}]: {ex.Message}");
            }
            finally
            {
                _server.DisconnectClient(_currentClientNick);
                _client.Close();
            }
        }

        private void HandleRegistration(ChatMessage clientMsg, StreamWriter writer)
        {
            ChatMessage response = new ChatMessage();
            bool isSuccess = false;

            lock (_server.ActiveClients)
            {
                if (string.IsNullOrEmpty(clientMsg.Nick) || clientMsg.Nick.Trim() == "")
                {
                    response.Type = "REG_ERROR";
                    response.Message = "Нік не має бути порожній";
                }
                else if (_server.ActiveClients.ContainsKey(clientMsg.Nick.ToLower()))
                {
                    response.Type = "REG_ERROR";
                    response.Message = "Цей нік вже використовується";
                }
                else
                {
                    response.Type = "REG_SUCCESS";
                    _currentClientNick = clientMsg.Nick;
                    _server.ActiveClients.Add(_currentClientNick.ToLower(), (_client, writer));
                    isSuccess = true;

                    Console.WriteLine($"Користувач {_currentClientNick} увійшов. Пошта: {clientMsg.Email}");
                }
            }

            writer.WriteLine(JsonSerializer.Serialize(response));

            if (isSuccess)
            {
                if (!string.IsNullOrEmpty(clientMsg.Email))
                {
                    string verificationCode = new Random().Next(100000, 999999).ToString();
                    lock (_server.PendingConfirmations)
                    {
                        _server.PendingConfirmations[clientMsg.Nick.ToLower()] = verificationCode;
                    }
                    _server.EmailService.SendVerificationEmailAsync(clientMsg.Email, verificationCode);
                }

                _server.SendChatHistoryToClient(writer, _currentClientNick);

                _server.SendOfflineMessagesToClient(writer, _currentClientNick);
            }
        }

        private void HandleMessage(ChatMessage clientMsg)
        {
            if (string.IsNullOrEmpty(clientMsg.ToNick) || clientMsg.ToNick.ToUpper() == "GLOBAL")
            {
                Console.WriteLine($"Загальний чат [{clientMsg.Nick}]: {clientMsg.Message}");
                _server.StorageService.SaveMessage(clientMsg);
                _server.BroadcastMessage(clientMsg);
            }
            else
            {
                _server.StorageService.SaveMessage(clientMsg);
                _server.SendPrivateMessage(clientMsg);
            }
        }
    }
}