using System;
using System.Linq;
using System.Threading;

namespace FinalPractice
{
    internal class ChatUI
    {
        private readonly NetworkService _netService;
        private string _screen = "MENU";
        private string _activePrivateTarget = "";

        public ChatUI(NetworkService netService)
        {
            _netService = netService;
            _netService.OnMessageReceived += RedrawUI;
        }

        public void Run()
        {
            _netService.StartListening();
            Thread.Sleep(1000); 
            RedrawUI();

            bool inMenu = true;
            while (inMenu)
            {
                string choice = Console.ReadLine()!;
                switch (choice)
                {
                    case "1":
                        _screen = "GLOBAL";
                        RedrawUI();
                        RunGlobalChatLoop();
                        break;

                    case "2":
                        ShowPrivateChatsMenu();
                        break;

                    case "3":
                        StartNewPrivateChat();
                        break;

                    case "4":
                        inMenu = false;
                        break;

                    default:
                        RedrawUI();
                        break;
                }
            }
        }

        private void RunGlobalChatLoop()
        {
            while (_screen == "GLOBAL")
            {
                string text = Console.ReadLine()!;
                if (text.Length == 0)
                {
                    _screen = "MENU";
                    RedrawUI();
                    break;
                }

                ChatMessage globalMsg = new ChatMessage
                {
                    Type = "MESSAGE",
                    Nick = _netService.MyNick,
                    Color = _netService.MyColor,
                    ToNick = "GLOBAL",
                    Message = text
                };

                _netService.SendToServer(globalMsg);
            }
        }

        private void ShowPrivateChatsMenu()
        {
            Console.Clear();
            Console.WriteLine("------- СПИСОК ВАШИХ ПРИВАТНИХ ЧАТІВ --------");

            if (_netService.ChatHistory.Count == 0)
            {
                Console.WriteLine("Ви ще нікому не писали приватних повідомлень.");
                Console.ReadKey();
                _screen = "MENU";
                RedrawUI();
                return;
            }

            for (int i = 0; i < _netService.ChatHistory.Count; i++)
            {
                string userInHistory = _netService.ChatHistory[i];
                int totalMessages = _netService.PrivateMessages.Count(m =>
                    (m.Nick.ToLower() == userInHistory.ToLower() && m.ToNick.ToLower() == _netService.MyNick.ToLower()) ||
                    (m.Nick.ToLower() == _netService.MyNick.ToLower() && m.ToNick.ToLower() == userInHistory.ToLower()));

                int readCount;

                if (_netService.ReadMessagesCount.ContainsKey(userInHistory.ToLower()))
                {
                    readCount = _netService.ReadMessagesCount[userInHistory.ToLower()];
                }
                else
                {
                    readCount = 0;
                }

                int unreadCount = totalMessages - readCount;

                if (unreadCount > 0)
                {
                    Console.Write($"{i + 1}) ");
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"{userInHistory} (+{unreadCount} нових)");
                    Console.ResetColor();
                }
                else
                {
                    Console.WriteLine($"{i + 1}) {userInHistory}");
                }
            }

            Console.Write("\nОберіть номер чату для входу : ");
            string inputTarget = Console.ReadLine()!;

            if (string.IsNullOrEmpty(inputTarget))
            {
                _screen = "MENU";
                RedrawUI();
                return;
            }

            if (int.TryParse(inputTarget, out int index) && index > 0 && index <= _netService.ChatHistory.Count)
            {
                _activePrivateTarget = _netService.ChatHistory[index - 1];
                _screen = "PRIVATE";
                RedrawUI();
                RunPrivateChatLoop();
            }
            else
            {
                Console.WriteLine("Некоректний номер. Повернення в меню...");
                Thread.Sleep(1500);
                _screen = "MENU";
                RedrawUI();
            }
        }

        private void StartNewPrivateChat()
        {
            Console.Write("\nВведіть нік користувача: ");
            string user = Console.ReadLine()!;

            if (string.IsNullOrEmpty(user))
            {
                _screen = "MENU";
                RedrawUI();
                return;
            }

            if (!_netService.ChatHistory.Contains(user) && user.ToUpper() != "GLOBAL")
            {
                _netService.ChatHistory.Add(user);
            }

            _activePrivateTarget = user;
            _screen = "PRIVATE";
            RedrawUI();
            RunPrivateChatLoop();
        }

        private void RunPrivateChatLoop()
        {
            while (_screen == "PRIVATE")
            {
                string text = Console.ReadLine()!;
                if (text.Length == 0)
                {
                    int totalCurrentMessages = _netService.PrivateMessages.Count(m =>
                        (m.Nick.ToLower() == _activePrivateTarget.ToLower() && m.ToNick.ToLower() == _netService.MyNick.ToLower()) ||
                        (m.Nick.ToLower() == _netService.MyNick.ToLower() && m.ToNick.ToLower() == _activePrivateTarget.ToLower()));

                    _netService.ReadMessagesCount[_activePrivateTarget.ToLower()] = totalCurrentMessages;

                    _screen = "MENU";
                    _activePrivateTarget = "";
                    RedrawUI();
                    break;
                }

                ChatMessage privateMsg = new ChatMessage
                {
                    Type = "MESSAGE",
                    Nick = _netService.MyNick,
                    Color = _netService.MyColor,
                    ToNick = _activePrivateTarget,
                    Message = text
                };

                _netService.SendToServer(privateMsg);
            }
        }

        private void RedrawUI()
        {
            Console.Clear();

            if (_screen == "MENU")
            {
                if (_netService.SystemAlerts.Count > 0)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    foreach (var alert in _netService.SystemAlerts)
                    {
                        Console.WriteLine($"[Система]: {alert}");
                    }
                    Console.ResetColor();
                    Console.WriteLine();
                }

                Console.WriteLine("------- ГОЛОВНЕ МЕНЮ --------");
                Console.WriteLine("1) УВІЙТИ В ГЛОБАЛЬНИЙ ЧАТ");
                Console.WriteLine("2) СПИСОК ПРИВАТНИХ ЧАТІВ");
                Console.WriteLine("3) НАПИСАТИ КОРИСТУВАЧЕВІ ЗА НІКОМ");
                Console.WriteLine("4) ВИХІД");
                Console.Write("Ваш вибір: ");
            }
            else if (_screen == "GLOBAL")
            {
                Console.WriteLine("--------- ГЛОБАЛЬНИЙ ЧАТ (Порожній рядок + Enter для виходу) ------\n");
                foreach (var m in _netService.GlobalMessages)
                {
                    ApplyConsoleColor(m.Color);
                    Console.Write($"[{m.Nick}]: ");
                    Console.ResetColor();
                    Console.WriteLine(m.Message);
                }
                Console.Write("Я: ");
            }
            else if (_screen == "PRIVATE")
            {
                Console.WriteLine($"---- Приватний чат з {_activePrivateTarget} (Порожній рядок + Enter для виходу) -----\n");
                foreach (var m in _netService.PrivateMessages)
                {
                    if (m.Nick.ToLower() == _activePrivateTarget.ToLower() || m.ToNick.ToLower() == _activePrivateTarget.ToLower())
                    {
                        ApplyConsoleColor(m.Color);
                        if (m.Nick.ToLower() == _netService.MyNick.ToLower())
                        {
                            Console.Write($"(Ви -> {m.ToNick}): ");
                        }
                        else
                        {
                            Console.Write($"[{m.Nick}]: ");
                        }
                        Console.ResetColor();
                        Console.WriteLine(m.Message);
                    }
                }
                Console.Write("Повідомлення: ");
            }
        }

        private void ApplyConsoleColor(string colorName)
        {
            switch (colorName)
            {
                case "Red": Console.ForegroundColor = ConsoleColor.Red; break;
                case "Green": Console.ForegroundColor = ConsoleColor.Green; break;
                case "Yellow": Console.ForegroundColor = ConsoleColor.Yellow; break;
                case "Cyan": Console.ForegroundColor = ConsoleColor.Cyan; break;
                default: Console.ForegroundColor = ConsoleColor.White; break;
            }
        }
    }
}