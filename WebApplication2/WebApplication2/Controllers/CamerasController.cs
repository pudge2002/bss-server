using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using WebApplication2.Data;
using WebApplication2.Models;
using WebApplication2.Video;

[ApiController]
[Route("api/[controller]")]
public class CamerasController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly VideoRecorderService _videoRecorderService;

    public CamerasController(ApplicationDbContext context, VideoRecorderService videoRecorderService)
    {
        _context = context;
        _videoRecorderService = videoRecorderService;
    }

    /// <summary>
    /// Получает список всех камер.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetCameras()
    {
        var cameras = await _context.Cameras.ToListAsync();
        return Ok(cameras);
    }

    /// <summary>
    /// Добавляет новую камеру и запускает запись.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> AddCamera([FromBody] Camera camera)
    {
        if (camera == null)
        {
            return BadRequest("Camera data is null.");
        }

        _context.Cameras.Add(camera);
        await _context.SaveChangesAsync();

        // Запуск записи для новой камеры через хостируемый сервис
        _videoRecorderService.StartRecording(camera);

        return Ok("Camera added successfully.");
    }

    /// <summary>
    /// Удаляет камеру и останавливает запись.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCamera(int id)
    {
        var camera = await _context.Cameras.FindAsync(id);
        if (camera == null)
        {
            return NotFound($"Camera with ID {id} not found.");
        }

        // Останавливаем запись для камеры
        _videoRecorderService.StopRecording(camera.Id);

        // Удаляем камеру из базы данных
        _context.Cameras.Remove(camera);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}