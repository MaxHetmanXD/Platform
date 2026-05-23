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

            string accessPath = fileMetadata?.GetAccessPath();

            if (fileMetadata == null || string.IsNullOrEmpty(accessPath) || !System.IO.File.Exists(accessPath))
            {
                return NotFound();
            }

            string contentType = "application/octet-stream";
            if (fileMetadata.Extension == ".jpg" || fileMetadata.Extension == ".jpeg") contentType = "image/jpeg";
            else if (fileMetadata.Extension == ".png") contentType = "image/png";
            else if (fileMetadata.Extension == ".gif") contentType = "image/gif";
            else if (fileMetadata.Extension == ".webp") contentType = "image/webp";

            var fileBytes = await System.IO.File.ReadAllBytesAsync(accessPath);

            if (contentType.StartsWith("image/"))
            {
                return File(fileBytes, contentType);
            }

            return File(fileBytes, contentType, fileMetadata.FileName);
        }
    }
}