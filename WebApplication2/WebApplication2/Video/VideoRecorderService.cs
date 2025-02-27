using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OpenCvSharp;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using WebApplication2.Data;
using WebApplication2.Models;
using WebApplication2.Video;

public class VideoRecorderService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
    private readonly string _baseOutputPath;
    private readonly ConcurrentDictionary<int, CancellationTokenSource> _cancellationTokenSources;

    public VideoRecorderService(IDbContextFactory<ApplicationDbContext> dbContextFactory, IOptions<VideoRecorderSettings> settings)
    {
        _dbContextFactory = dbContextFactory;
        _baseOutputPath = settings.Value.BaseOutputPath;
        _cancellationTokenSources = new ConcurrentDictionary<int, CancellationTokenSource>();
        
    }

    public async Task StartRecordingForAllCamerasAsync()
    {
        using (var context = _dbContextFactory.CreateDbContext())
        {
            var cameras = await context.Cameras.ToListAsync();

            foreach (var camera in cameras)
            {
                StartRecording(camera);
            }
        }
    }
    /// <summary>
    /// Запускает запись для конкретной камеры.
    /// </summary>
    public void StartRecording(Camera camera)
    {
        if (_cancellationTokenSources.ContainsKey(camera.Id))
        {
            Console.WriteLine($"Recording is already running for camera {camera.Id}.");
            return;
        }

        var cancellationTokenSource = new CancellationTokenSource();
        _cancellationTokenSources[camera.Id] = cancellationTokenSource;

        Task.Run(async () => await RecordCameraAsync(camera, cancellationTokenSource.Token), cancellationTokenSource.Token);

        Console.WriteLine($"Recording started for camera {camera.Id}.");
    }

    /// <summary>
    /// Останавливает запись для конкретной камеры.
    /// </summary>
    public void StopRecording(int cameraId)
    {
        if (_cancellationTokenSources.TryRemove(cameraId, out var cancellationTokenSource))
        {
            cancellationTokenSource.Cancel();
            cancellationTokenSource.Dispose();
            Console.WriteLine($"Recording stopped for camera {cameraId}.");
        }
        else
        {
            Console.WriteLine($"No recording found for camera {cameraId}.");
        }
    }

    /// <summary>
    /// Основной метод записи видео для камеры.
    /// </summary>
    private async Task RecordCameraAsync(Camera camera, CancellationToken cancellationToken)
    {
        var rtspUrl = $"rtsp://{camera.Login}:{camera.Password}@{camera.Url}";

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                string cameraOutputPath = Path.Combine(_baseOutputPath, $"camera_{camera.Id}");
                Directory.CreateDirectory(cameraOutputPath);

                string outputFilePath = Path.Combine(cameraOutputPath, $"{DateTime.Now:yyyyMMdd_HHmmss}.avi");

                using (var capture = new VideoCapture(rtspUrl))
                {
                    if (!capture.IsOpened())
                    {
                        Console.WriteLine($"Error: Could not open video stream for camera {camera.Id}.");
                        await Task.Delay(5000, cancellationToken);
                        continue;
                    }

                    var fps = capture.Fps;
                    var frameWidth = (int)capture.FrameWidth;
                    var frameHeight = (int)capture.FrameHeight;

                    using (var writer = new VideoWriter(outputFilePath, FourCC.MJPG, fps, new Size(frameWidth, frameHeight), true))
                    {
                        Mat frame = new Mat();
                        var endTime = DateTime.Now.AddMinutes(5);

                        while (DateTime.Now < endTime && !cancellationToken.IsCancellationRequested)
                        {
                            capture.Read(frame);
                            if (frame.Empty())
                                break;

                            writer.Write(frame);
                            await Task.Delay(40, cancellationToken);
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine($"Recording for camera {camera.Id} was canceled.");
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error recording for camera {camera.Id}: {ex.Message}");
                await Task.Delay(5000, cancellationToken);
            }
        }

        Console.WriteLine($"Recording stopped for camera {camera.Id}.");
    }
}