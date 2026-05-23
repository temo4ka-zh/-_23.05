using System;
using System.IO;
namespace NotesReminderApp.Services
{
    public static class Logger
    {
        private static readonly string LogPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Data",
            "log.txt"
        );

        public static void Log(string message)
        {
            try
            {
                string? directory = Path.GetDirectoryName(LogPath);

                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string logLine = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";

                File.AppendAllText(LogPath, logLine + Environment.NewLine);
            }
            catch
            {
                // Если логирование не удалось, приложение не должно аварийно завершаться
            }
        }
    }
}