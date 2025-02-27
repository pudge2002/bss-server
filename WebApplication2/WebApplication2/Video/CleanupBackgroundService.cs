using Microsoft.Extensions.Options;

namespace WebApplication2.Video
{
    public class CleanupBackgroundService : BackgroundService
    {
        private readonly CleanupSettingsService _cleanupSettingsService;
        private readonly string _baseOutputPath;

        public CleanupBackgroundService(
            CleanupSettingsService cleanupSettingsService,
            IOptions<VideoRecorderSettings> videoRecorderSettings)
        {
            _cleanupSettingsService = cleanupSettingsService;
            _baseOutputPath = videoRecorderSettings.Value.BaseOutputPath;

            // Проверяем и создаем папку для записи, если она не существует
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Получаем текущие настройки очистки
                    var settings = await _cleanupSettingsService.GetSettingsAsync();
                    var cleanupTime = TimeSpan.Parse(settings.CleanupTime);
                    var now = DateTime.Now.TimeOfDay;

                    // Проверяем, наступило ли время очистки
                    if (now >= cleanupTime)
                    {
                        // Выполняем очистку
                        await PerformCleanupAsync(settings.CleanupIntervalDays, stoppingToken);
                    }

                    // Ждем 1 час перед следующей проверкой
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Задача была отменена
                    break;
                }
                catch (Exception ex)
                {
                    // Логируем ошибку
                    Console.WriteLine($"Ошибка в фоновой задаче очистки: {ex.Message}");
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken); // Ждем 5 минут перед повторной попыткой
                }
            }
        }

        /// <summary>
        /// Выполняет очистку старых файлов.
        /// </summary>
        /// <param name="retentionDays">Количество дней для хранения файлов.</param>
        /// <param name="cancellationToken">Токен отмены.</param>
        private async Task PerformCleanupAsync(int retentionDays, CancellationToken cancellationToken)
        {
            var retentionTime = DateTime.Now.AddDays(-retentionDays);

            // Очищаем записи для всех камер
            foreach (var cameraDirectory in Directory.GetDirectories(_baseOutputPath))
            {
                foreach (var file in Directory.GetFiles(cameraDirectory))
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return; // Прерываем выполнение, если задача отменена
                    }

                    var fileInfo = new FileInfo(file);
                    if (fileInfo.CreationTime < retentionTime)
                    {
                        try
                        {
                            fileInfo.Delete();
                            Console.WriteLine($"Удален файл: {file}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Ошибка при удалении файла {file}: {ex.Message}");
                        }
                    }
                }
            }

            Console.WriteLine("Очистка завершена.");
        }
    }
}
