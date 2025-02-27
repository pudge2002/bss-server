using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using WebApplication2.Data;

namespace WebApplication2.Video
{
    public class CleanupSettingsService
    {
        private readonly string _filePath;

        public CleanupSettingsService(string filePath)
        {
            _filePath = filePath;
            EnsureFileExists().Wait(); // Проверяем и создаем файл при инициализации
        }

        private async Task EnsureFileExists()
        {
            if (!File.Exists(_filePath))
            {
                // Создаем файл с настройками по умолчанию
                var defaultSettings = new CleanupSettings
                {
                    CleanupTime = "02:00",
                    CleanupIntervalDays = 7
                };
                await SaveSettingsAsync(defaultSettings);
            }
        }

        public async Task<CleanupSettings> GetSettingsAsync()
        {
            var json = await File.ReadAllTextAsync(_filePath);
            return JsonSerializer.Deserialize<CleanupSettings>(json);
        }

        public async Task SaveSettingsAsync(CleanupSettings settings)
        {
            var json = JsonSerializer.Serialize(settings);
            await File.WriteAllTextAsync(_filePath, json);
        }
    }

    public class CleanupSettings
    {
        public string CleanupTime { get; set; }
        public int CleanupIntervalDays { get; set; }
    }
}