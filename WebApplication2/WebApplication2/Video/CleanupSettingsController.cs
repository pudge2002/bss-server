using Microsoft.AspNetCore.Mvc;

namespace WebApplication2.Video
{
    [ApiController]
    [Route("api/[controller]")]
    public class CleanupSettingsController : ControllerBase
    {
        private readonly CleanupSettingsService _cleanupSettingsService;

        public CleanupSettingsController(CleanupSettingsService cleanupSettingsService)
        {
            _cleanupSettingsService = cleanupSettingsService;
        }

        [HttpGet]
        public async Task<ActionResult<CleanupSettings>> GetCleanupSettings()
        {
            var settings = await _cleanupSettingsService.GetSettingsAsync();
            return Ok(settings);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateCleanupSettings([FromBody] CleanupSettingsDto settings)
        {
            if (settings == null)
            {
                return BadRequest("Настройки не указаны.");
            }

            var cleanupSettings = new CleanupSettings
            {
                CleanupTime = settings.CleanupTime,
                CleanupIntervalDays = settings.CleanupIntervalDays
            };

            await _cleanupSettingsService.SaveSettingsAsync(cleanupSettings);
            return Ok("Настройки очистки сохранены успешно.");
        }
    }

    public class CleanupSettingsDto
    {
        public string CleanupTime { get; set; }
        public int CleanupIntervalDays { get; set; }
    }
}
