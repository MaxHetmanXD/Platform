using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Platform.Data;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Platform.Controllers
{
    public class FilesController : Controller
    {
        private readonly PlatformDbContext _context;

        public FilesController(PlatformDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Download(Guid id)
        {
            var fileMetadata = await _context.Files.FirstOrDefaultAsync(f => f.Id == id);

            if (fileMetadata == null || !System.IO.File.Exists(fileMetadata.LocalPath))
            {
                return NotFound();
            }

            string contentType = "application/octet-stream";
            if (fileMetadata.Extension == ".jpg" || fileMetadata.Extension == ".jpeg") contentType = "image/jpeg";
            else if (fileMetadata.Extension == ".png") contentType = "image/png";
            else if (fileMetadata.Extension == ".gif") contentType = "image/gif";
            else if (fileMetadata.Extension == ".webp") contentType = "image/webp";

            var fileBytes = await System.IO.File.ReadAllBytesAsync(fileMetadata.LocalPath);

            return File(fileBytes, contentType, fileMetadata.FileName);
        }
    }
}