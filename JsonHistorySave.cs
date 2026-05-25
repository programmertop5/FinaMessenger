using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace ServerMsg
{
    internal class StorageService
    {
        private readonly string _historyFilePath = "chat_history.json";
        private readonly object _historyLock = new object();

        public List<ChatMessage> AllMessages { get; private set; } = new List<ChatMessage>();

        public void LoadHistory()
        {
            lock (_historyLock)
            {
                try
                {
                    if (File.Exists(_historyFilePath))
                    {
                        string json = File.ReadAllText(_historyFilePath, Encoding.UTF8);
                        AllMessages = JsonSerializer.Deserialize<List<ChatMessage>>(json) ?? new List<ChatMessage>();
                        Console.WriteLine($"Завантажено {AllMessages.Count} повідомлень з історії.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Помилка завантаження історії: {ex.Message}");
                }
            }
        }

        public void SaveMessage(ChatMessage msg)
        {
            lock (_historyLock)
            {
                AllMessages.Add(msg);
                try
                {
                    string json = JsonSerializer.Serialize(AllMessages, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(_historyFilePath, json, Encoding.UTF8);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Помилка збереження історії: {ex.Message}");
                }
            }
        }
    }
}