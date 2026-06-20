using Microsoft.AspNetCore.Mvc;
using System.IO;

/* This controller provides endpoints for browsing directories and uploading/downloading files within a specific "browsable" directory on the server.
 * 
 * This implementation is missing several things that we would likely want in a production application, such as authentication/authorization to restrict access to the endpoints, logging of important events (e.g., file uploads/downloads, attempts to access unauthorized paths), and potentially some rate limiting or other measures to prevent abuse of the endpoints.
 */

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

            // In a real production application, we might want to do some additional validation on the file itself, such as checking the file extension against a whitelist of allowed types, scanning the file for viruses/malware, and/or enforcing a maximum file size limit to prevent abuse of the upload functionality
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
            Console.WriteLine("Getting directories...");
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