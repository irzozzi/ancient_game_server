using Microsoft.AspNetCore.Mvc;
using System.IO;

namespace AncientServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LegalController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;

        public LegalController(IWebHostEnvironment env)
        {
            _env = env;
        }

        private string LoadTextFile(string fileName)
        {
            var path = Path.Combine(_env.ContentRootPath, "Content", fileName);
            if (!System.IO.File.Exists(path))
                return $"Документ {fileName} временно недоступен.";
            return System.IO.File.ReadAllText(path);
        }

        // GET: api/legal/privacy — политика конфиденциальности
        [HttpGet("privacy")]
        public IActionResult GetPrivacyPolicy()
        {
            var text = LoadTextFile("privacy_policy.md");
            return Ok(new
            {
                version = "1.0",
                updatedAt = "2026-01-01",
                content = text
            });
        }

        // GET: api/legal/terms — пользовательское соглашение
        [HttpGet("terms")]
        public IActionResult GetTermsOfService()
        {
            var text = LoadTextFile("terms_of_service.md");
            return Ok(new
            {
                version = "1.0",
                updatedAt = "2026-01-01",
                content = text
            });
        }

        // GET: api/legal/versions — список версий документов
        [HttpGet("versions")]
        public IActionResult GetVersions()
        {
            return Ok(new
            {
                privacy = new { version = "1.0", updatedAt = "2026-01-01" },
                terms = new { version = "1.0", updatedAt = "2026-01-01" }
            });
        }
    }
}