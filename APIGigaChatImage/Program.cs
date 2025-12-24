using APIGigaChatImage.Models.Response;
using APIGigaChatImage.Classes; // Добавьте эту строку
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace APIGigaChatImage
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== Запуск программы генерации изображений ===");

            string Token = await GetToken(ClientId, AuthorizationKey);
            if (Token == null)
            {
                Console.WriteLine("Не удалось получить токен");
                Console.WriteLine("Нажмите любую клавишу для выхода...");
                Console.ReadKey();
                return;
            }



            while (true)
            {
                Console.Write("\nСообщение: ");
                string message = Console.ReadLine();


                if (message.ToLower() == "выход" || message.ToLower() == "exit")
                {
                    Console.WriteLine("Выход из программы...");
                    break;
                }


                if (message.ToLower() == "обои" || message.ToLower() == "wallpaper")
                {
                    await GenerateWallpaperMode(Token);
                    continue;
                }

                DialogHistory.Add(new Request.Message
                {
                    role = "user",
                    content = message
                });

                ResponseMessage answer = await GetAnswer(Token, DialogHistory);

                string assistantText = answer.choices[0].message.content;
                Console.WriteLine("Ответ: " + assistantText);

                DialogHistory.Add(new Request.Message
                {
                    role = "assistant",
                    content = assistantText
                });

                string fileId = ExtractImageId(assistantText);


                if (!string.IsNullOrEmpty(fileId))
                {
                    Console.WriteLine($"\nНайдено изображение с ID: {fileId}");
                    Console.WriteLine("Скачивание изображения...");

                    byte[] imageData = await DownloadImage(Token, fileId);
                    if (imageData != null && imageData.Length > 0)
                    {
                        string savedPath = SaveImage(imageData);
                        Console.WriteLine($" Изображение сохранено: {savedPath}");


                        Console.Write("\nУстановить как обои рабочего стола? (да/нет): ");
                        string setWallpaper = Console.ReadLine();
                        if (setWallpaper.ToLower() == "да" || setWallpaper.ToLower() == "yes")
                        {
                            WallpaperSetter.SetWallpaper(savedPath);
                        }


                        Console.Write("Открыть папку с изображением? (да/нет): ");
                        string openFolder = Console.ReadLine();
                        if (openFolder.ToLower() == "да")
                        {
                            string folder = Path.GetDirectoryName(savedPath);
                            System.Diagnostics.Process.Start("explorer.exe", folder);
                        }
                    }
                    else
                    {
                        Console.WriteLine(" Не удалось скачать изображение");
                    }
                }
            }

            Console.WriteLine("\nНажмите любую клавишу для выхода...");
            Console.ReadKey();
        }
    }
}
