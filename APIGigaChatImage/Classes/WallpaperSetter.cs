using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace APIGigaChatImage.Classes
{
    public class WallpaperSetter
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);

        private const int SPI_SETDESKWALLPAPER = 20;
        private const int SPIF_UPDATEINIFILE = 0x01;
        private const int SPIF_SENDWININICHANGE = 0x02;

        public static bool SetWallpaper(string imagePath)
        {
            try
            {
                if (string.IsNullOrEmpty(imagePath) || !System.IO.File.Exists(imagePath))
                {
                    Console.WriteLine($"Файл не найден: {imagePath}");
                    return false;
                }

                // Используем правильный путь
                string fullPath = System.IO.Path.GetFullPath(imagePath);

                int result = SystemParametersInfo(
                    SPI_SETDESKWALLPAPER,
                    0,
                    fullPath,
                    SPIF_UPDATEINIFILE | SPIF_SENDWININICHANGE
                );

                if (result == 0)
                {
                    Console.WriteLine("Не удалось установить обои");
                    return false;
                }

                Console.WriteLine($"Обои установлены: {System.IO.Path.GetFileName(imagePath)}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
                return false;
            }
        }

        public static void SetWallpaperModern(string imagePath)
        {
            try
            {
                if (!System.IO.File.Exists(imagePath))
                {
                    Console.WriteLine($"Файл не найден: {imagePath}");
                    return;
                }

                Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                    @"Control Panel\Desktop", true);

                if (key != null)
                {
                    key.SetValue("WallpaperStyle", "10");
                    key.SetValue("TileWallpaper", "0");
                    key.Close();
                }

                SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, imagePath,
                    SPIF_UPDATEINIFILE | SPIF_SENDWININICHANGE);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка (Modern): {ex.Message}");
            }
        }
    }
}
