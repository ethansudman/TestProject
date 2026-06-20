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

        [HttpGet]
        [Route("file")]
        public async Task<ActionResult> GetFile(string path)
        {
            #region Validation Logic
            if (string.IsNullOrWhiteSpace(path))
            {
                return BadRequest("Path parameter is required.");
            }

            else if (!System.IO.File.Exists(path))
            {
                return NotFound("File not found.");
            }

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