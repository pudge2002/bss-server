namespace WebApplication2.Video
{
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.DependencyInjection;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using WebApplication2.Data;
    using WebApplication2.Models;
    using Microsoft.EntityFrameworkCore;
    using System.Collections.Concurrent;

    public class VideoRecorderHostedService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ConcurrentDictionary<int, Task> _cameraTasks = new();

        public VideoRecorderHostedService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var videoRecorderService = scope.ServiceProvider.GetRequiredService<VideoRecorderService>();

            // Загружаем все камеры из базы данных
            var cameras = await context.Cameras.ToListAsync(cancellationToken);

            // Запускаем запись для каждой камеры
            foreach (var camera in cameras)
            {
                videoRecorderService.StartRecording(camera);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            // Останавливаем все записи при завершении работы приложения
            using var scope = _serviceProvider.CreateScope();
            var videoRecorderService = scope.ServiceProvider.GetRequiredService<VideoRecorderService>();

            foreach (var cameraId in _cameraTasks.Keys)
            {
                videoRecorderService.StopRecording(cameraId);
            }

            return Task.CompletedTask;
        }
    }
}
