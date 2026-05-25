using System;
using System.Text;

namespace FinalPractice
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;

            using NetworkService netService = new NetworkService();

            if (!netService.Connect("127.0.0.1", 3000))
            {
                Console.ReadKey();
                return;
            }

            bool isAuthenticated = false;

            while (!isAuthenticated)
            {
                Console.WriteLine(" --------- РЕЄСТРАЦІЯ / ВХІД --------------");
                Console.Write("Введіть свій нік: ");
                string nick = Console.ReadLine()!;

                Console.Write("Введіть свій Email: ");
                string email = Console.ReadLine()!;

                Console.WriteLine("Оберіть колір для підсвічування ніка:");
                Console.WriteLine("1 - Червоний ");
                Console.WriteLine("2 - Зелений");
                Console.WriteLine("3 - Жовтий");
                Console.WriteLine("4 - Блакитний");
                Console.Write("Ваш вибір: ");

                string colorChoice = Console.ReadLine()!;
                string color = colorChoice switch
                {
                    "1" => "Red",
                    "2" => "Green",
                    "3" => "Yellow",
                    "4" => "Cyan",
                    _ => "White"
                };

                var response = netService.TryRegister(nick, email, color);

                if (response != null && response.Type == "REG_SUCCESS")
                {
                    isAuthenticated = true;
                    Console.Clear();
                }
                else if (response != null && response.Type == "REG_ERROR")
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Помилка: {response.Message}");
                    Console.ResetColor();
                    Console.WriteLine("Спробуйте ще раз.\n");
                }
                else
                {
                    Console.WriteLine("Сервер повернув порожню або некоректну відповідь.");
                }
            }

            if (isAuthenticated)
            {
                ChatUI ui = new ChatUI(netService);
                ui.Run();
            }
        }
    }
}