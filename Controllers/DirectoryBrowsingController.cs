using Microsoft.AspNetCore.Mvc;
using System.IO;

namespace TestProject.Controllers {
    [ApiController]
    [Route("[controller]")]
    public class DirectoryBrowsingController : ControllerBase {

        private readonly ILogger<DirectoryBrowsingController> _logger;

        public DirectoryBrowsingController(ILogger<DirectoryBrowsingController> logger) {
            _logger = logger;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile(IFormFile file, [FromForm] string path)
        {
            #region Validation
            // First couple of checks are for innocuous mistakes, last one is a potential security issue if we allow arbitrary file uploads to arbitrary locations on the server
            if (file == null || file.Length == 0)
            {
                return BadRequest("Must include a file");
            }
            else if (string.IsNullOrWhiteSpace(path))
            {
                return BadRequest("Invalid path");
            }
            else if (!Directory.Exists(Path.GetDirectoryName(path)))
            {
                return BadRequest("The specified directory does not exist.");
            }

            // Must be a path within the BrowsableDirectory to prevent file uploads to arbitrary locations
            // In a real application, we might want to take further action beyond just returning a BadRequest, such as logging an alert or even temporarily blocking the client IP if we see repeated attempts to upload to unauthorized paths
            else if (!path.StartsWith(Path.Combine(Directory.GetCurrentDirectory(), GlobalConstants.BrowsableDirectoryName)))
            {
                return BadRequest("Uploading files to the specified path is not allowed.");
            }
            #endregion

            using (var stream = new FileStream(path, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return Ok();
        }

        [HttpGet]
        [Route("file")]
        public async Task<IActionResult> GetFile(string path)
        {
            #region Validation Logic
            // Check for innocuous mistakes
            if (string.IsNullOrWhiteSpace(path))
            {
                return BadRequest("Path parameter is required.");
            }

            else if (!System.IO.File.Exists(path))
            {
                return NotFound("File not found.");
            }

            // Must be a path within the BrowsableDirectory to prevent access to arbitrary files
            // In a real application, we might want to take further action beyond just returning a BadRequest, such as logging an alert or even temporarily blocking the client IP if we see repeated attempts to access unauthorized paths
            else if (!path.StartsWith(Path.Combine(Directory.GetCurrentDirectory(), GlobalConstants.BrowsableDirectoryName)))
            {
                return BadRequest("Access to the specified path is not allowed.");
            }
            #endregion

            return new FileContentResult(System.IO.File.ReadAllBytes(path), "application/octet-stream")
            {
                FileDownloadName = Path.GetFileName(path)
            };
        }

        [HttpGet]
        [Route("directories")]
        public Entity GetDirectories() {
            // Get one level up
            string directory = Path.Combine(Directory.GetCurrentDirectory(), GlobalConstants.BrowsableDirectoryName);

            var entity = new Entity
            {
                EntityType = EntityType.Folder,
                Path = directory
            };

            InitEntity(entity);

            return entity;
        }

        }
        private void InitEntity(Entity entity)
        {
            switch (entity.EntityType)
            {
                case EntityType.Folder:
                    // TODO: Do I actually need to do this?
                    if (entity.Subentities == null)
                    {
                        entity.Subentities = new List<Entity>();
                    }

                    Directory.GetDirectories(entity.Path).ToList().ForEach(dir =>
                    {
                        var subEntity = new Entity
                        {
                            EntityType = EntityType.Folder,
                            Path = dir
                        };
                        InitEntity(subEntity);
                        entity.Subentities.Add(subEntity);
                    });

                    Directory.GetFiles(entity.Path).ToList().ForEach(file =>
                    {
                        var subEntity = new Entity
                        {
                            EntityType = EntityType.File,
                            Path = file,
                            Subentities = new List<Entity>() // Files won't have subentities, but we still need to initialize it
                        };
                        entity.Subentities.Add(subEntity);
                    });
                    break;

                // TODO: Do I actually need to do this?
                case EntityType.File:
                    // Files won't have subentities, but we still need to initialize it
                    entity.Subentities = new List<Entity>();
                    break;
            }
        }
    }
}