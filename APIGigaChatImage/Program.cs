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
    class Program
    {
        static string ClientId = "019b0341-e893-71fe-983a-cf99db3031f1";
        static string AuthorizationKey = "MDE5YjAzNDEtZTg5My03MWZlLTk4M2EtY2Y5OWRiMzAzMWYxOjYwNzRhYTdhLTcwOTctNDBmMy04ZjVkLTBkZDIwN2YxZGM3Yw==";
        static List<Request.Message> DialogHistory =
       new List<Request.Message>()
        {
            new Request.Message
            {
                role = "system",
                content = "Ты — Пользовтаель"
            }
        };

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


        static async Task GenerateWallpaperMode(string token)
        {
            Console.WriteLine("\n=== РЕЖИМ ГЕНЕРАЦИИ ОБОЕВ ===");
            Console.WriteLine("Введите описание для обоев рабочего стола:");
            Console.Write("Промпт: ");
            string prompt = Console.ReadLine();

            if (string.IsNullOrEmpty(prompt))
            {
                prompt = "красивые обои для рабочего стола, высокое качество, HD";
            }

            var wallpaperHistory = new List<Request.Message>
            {
                new Request.Message
                {
                    role = "system",
                    content = "Ты генерируешь изображения для обоев рабочего стола. Всегда возвращай изображение в ответ."
                },
                new Request.Message
                {
                    role = "user",
                    content = $"Сгенерируй обои рабочего стола: {prompt}. Соотношение сторон 16:9, высокое разрешение."
                }
            };

            ResponseMessage answer = await GetAnswer(token, wallpaperHistory);

            string assistantText = answer.choices[0].message.content;
            Console.WriteLine("Сгенерировано изображение обоев");

            string fileId = ExtractImageId(assistantText);

            if (!string.IsNullOrEmpty(fileId))
            {
                Console.WriteLine($"\nНайдено изображение обоев с ID: {fileId}");
                Console.WriteLine("Скачивание обоев...");

                byte[] imageData = await DownloadImage(token, fileId);
                if (imageData != null && imageData.Length > 0)
                {
                    string savedPath = SaveImage(imageData, "wallpaper");
                    Console.WriteLine($" Обои сохранены: {savedPath}");


                    Console.WriteLine("\nУстанавливаю обои на рабочий стол...");
                    WallpaperSetter.SetWallpaper(savedPath);

                    Console.WriteLine($" Обои успешно установлены!");


                    Console.WriteLine($"\nФайл обоев: {Path.GetFullPath(savedPath)}");

                    Console.Write("\nОткрыть папку с обоями? (да/нет): ");
                    string openFolder = Console.ReadLine();
                    if (openFolder.ToLower() == "да")
                    {
                        string folder = Path.GetDirectoryName(savedPath);
                        System.Diagnostics.Process.Start("explorer.exe", folder);
                    }
                }
            }
            else
            {
                Console.WriteLine(" Не удалось сгенерировать обои");
            }

            Console.WriteLine("\nНажмите любую клавишу для продолжения...");
            Console.ReadKey();
        }

        public static async Task<ResponseMessage> GetAnswer(string token, List<Request.Message> messages)
        {
            ResponseMessage responseMessage = null;
            string Url = "https://gigachat.devices.sberbank.ru/api/v1/chat/completions";

            using (HttpClientHandler Handler = new HttpClientHandler())
            {
                Handler.ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true;

                using (HttpClient Client = new HttpClient(Handler))
                {
                    HttpRequestMessage Request = new HttpRequestMessage(HttpMethod.Post, Url);

                    Request.Headers.Add("Accept", "application/json");
                    Request.Headers.Add("Authorization", $"Bearer {token}");

                    Request DataRequest = new Request()
                    {
                        model = "GigaChat:2.0.28.2",
                        messages = messages,
                        function_call = "auto",
                        temperature = 0.3,
                        max_tokens = 1500
                    };

                    string JsonContent = JsonConvert.SerializeObject(DataRequest);
                    Request.Content = new StringContent(JsonContent, Encoding.UTF8, "application/json");

                    HttpResponseMessage Response = await Client.SendAsync(Request);

                    if (Response.IsSuccessStatusCode)
                    {
                        string ResponseContent = await Response.Content.ReadAsStringAsync();
                        responseMessage = JsonConvert.DeserializeObject<ResponseMessage>(ResponseContent);
                    }
                    else
                    {
                        Console.WriteLine($"Ошибка запроса: {Response.StatusCode}");
                    }
                }
            }
            return responseMessage;
        }

        public static string ExtractImageId(string content)
        {
            if (content.Contains("<img") && content.Contains("src=\""))
            {
                int start = content.IndexOf("src=\"") + 5;
                int end = content.IndexOf("\"", start);

                if (start > 5 && end > start)
                {
                    string imageId = content.Substring(start, end - start);
                    Console.WriteLine($"Извлечен ID изображения: {imageId}");
                    return imageId;
                }
            }


            Console.WriteLine("В ответе не найдено изображение");
            return null;
        }

        public static async Task<byte[]> DownloadImage(string token, string fileId)
        {
            try
            {
                Console.WriteLine($"Скачивание изображения по ID: {fileId}");
                string downloadUrl = $"https://gigachat.devices.sberbank.ru/api/v1/files/{fileId}/content";

                Console.WriteLine($"Используется URL: {downloadUrl}");

                using (HttpClientHandler handler = new HttpClientHandler())
                {
                    handler.ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true;

                    using (HttpClient client = new HttpClient(handler))
                    {
                        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
                        client.DefaultRequestHeaders.Add("Accept", "image/jpeg,image/png,*/*");

                        HttpResponseMessage response = await client.GetAsync(downloadUrl);

                        if (response.IsSuccessStatusCode)
                        {
                            byte[] imageData = await response.Content.ReadAsByteArrayAsync();

                            Console.WriteLine($" Изображение успешно скачано: {imageData.Length} байт");

                            return imageData;
                        }
                        else
                        {
                            Console.WriteLine($" Ошибка скачивания: {response.StatusCode}");
                            return null;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($" Ошибка в методе DownloadImage: {ex.Message}");
                return null;
            }
        }

        public static string SaveImage(byte[] data, string prefix = "image")
        {
            try
            {
                string saveFolder = "GeneratedImages";
                if (!Directory.Exists(saveFolder))
                {
                    Directory.CreateDirectory(saveFolder);
                    Console.WriteLine($"Создана папка: {saveFolder}");
                }

                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

                string extension = ".jpg";

                if (data.Length > 2)
                {
                    if (data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47)
                        extension = ".png";
                    else if (data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF)
                        extension = ".jpg";
                }

                string fileName = $"{prefix}_{timestamp}{extension}";
                string filePath = Path.Combine(saveFolder, fileName);
                File.WriteAllBytes(filePath, data);
                Console.WriteLine($" Изображение сохранено по пути: {filePath}");

                return filePath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($" Ошибка сохранения изображения: {ex.Message}");
                return null;
            }
        }

        public static async Task<string> GetToken(string rqUID, string bearer)
        {
            string ReturnToken = null;
            string Url = "https://ngw.devices.sberbank.ru:9443/api/v2/oauth";

            using (HttpClientHandler Handler = new HttpClientHandler())
            {
                Handler.ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true;
                using (HttpClient Client = new HttpClient(Handler))
                {
                    HttpRequestMessage Request = new HttpRequestMessage(HttpMethod.Post, Url);
                    Request.Headers.Add("Accept", "application/json");
                    Request.Headers.Add("RqUID", rqUID);
                    Request.Headers.Add("Authorization", $"Bearer {bearer}");

                    var Data = new List<KeyValuePair<string, string>>
                    {
                        new KeyValuePair<string, string>("scope","GIGACHAT_API_PERS")
                    };

                    Request.Content = new FormUrlEncodedContent(Data);
                    HttpResponseMessage Response = await Client.SendAsync(Request);

                    if (Response.IsSuccessStatusCode)
                    {
                        string ResponseContent = await Response.Content.ReadAsStringAsync();
                        ResponseToken Token = JsonConvert.DeserializeObject<ResponseToken>(ResponseContent);
                        ReturnToken = Token.access_token;
                    }
                    else
                    {
                        Console.WriteLine($"Ошибка при получении токена: {Response.StatusCode}");
                        string errorContent = await Response.Content.ReadAsStringAsync();
                        Console.WriteLine($"Детали ошибки: {errorContent}");
                    }
                }
            }
            return ReturnToken;
        }
    }
}
