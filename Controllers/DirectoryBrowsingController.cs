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

        /// <summary>
        /// Endpoint to upload a file to a specified path within the BrowsableDirectory. The path must be a valid directory within the BrowsableDirectory, and the file will be saved with the same name as the uploaded file.
        /// </summary>
        /// <param name="file">The file that is being uploaded</param>
        /// <param name="path">The full path that the <paramref name="file"/> should be uploaded to</param>
        /// <returns>400 status code if <paramref name="file"/> is <c>null</c> or empty, or if <paramref name="path"/> is empty or is not an allowed path. 200 otherwise.</returns>
        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile(IFormFile file, [FromForm] string path)
        {
            Console.WriteLine("Path: " + path);
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

        /// <summary>
        /// Download a file from the path
        /// </summary>
        /// <param name="path">Full path of the file to download</param>
        /// <returns>400 status code and validation message if <paramref name="path"/> is empty, consists only of whitespace, or is not an allowable path</returns>
        [HttpGet]
        [Route("download")]
        public async Task<IActionResult> DownloadFile(string path)
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
            // Henry Ford said that customers could pick any color they wanted for the Model T, as long as it was black - in this case, users can access any file they want, as long as it's within the BrowsableDirectory
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

        /// <summary>
        /// Get the entire directory structure starting from the BrowsableDirectory, optionally filtered by a query string. If a query is provided, only entities whose paths contain the query string (case-insensitive) will be included in the results, and parent folders will be preserved if any descendant matches.
        /// </summary>
        /// <param name="query">String to filter on</param>
        /// <returns><see cref="Entity"/> populated with the entire directory structure</returns>
        [HttpGet]
        [Route("directories")]
        public Entity GetDirectories(string query = "") {
            Console.WriteLine("Getting directories...");
            // BrowsableDirectory will always be the root of the directory structure that we return, so we can just get the current working directory and append the BrowsableDirectory name to it to get the full path
            string directory = Path.Combine(Directory.GetCurrentDirectory(), GlobalConstants.BrowsableDirectoryName);

            var entity = new Entity
            {
                EntityType = EntityType.Folder,
                Path = directory
            };

            // Recursively populate the entity and its subentities with the contents of the directory at the specified path
            InitEntity(entity);

            // Apply the query if it's not empty or whitespace. This will filter the results to include only entities whose paths contain the query string (case-insensitive), and will preserve parent folders if any descendant matches.
            if (!string.IsNullOrWhiteSpace(query))
            {
                // If a query is provided, filter the results to include only entities whose paths contain the query string (case-insensitive).
                // Apply recursive filtering so parent folders are preserved when any descendant matches.
                filterEntity(entity, query);
            }

            return entity;
        }

        /// <summary>
        /// Implement the filtering logic for the GetDirectories endpoint. This method recursively checks if the entity or any of its subentities match the query string, and removes any entities that do not match from the results.
        /// </summary>
        /// <param name="entity">Root <see cref="Entity"/></param>
        /// <param name="query">String that we're filtering on</param>
        /// <returns><c>true</c> if there are any matches for the <paramref name="query"/>; <c>false</c> otherwise</returns>
        private bool filterEntity(Entity entity, string query)
        {
            // Recursively check to see if the entity or any of its subentities match the query, and if not, remove it from the results
            if (entity == null) return false;
            bool selfMatches = !string.IsNullOrWhiteSpace(entity.Path) &&
                               entity.Path.Contains(query, StringComparison.OrdinalIgnoreCase);

            if (entity.Subentities == null || entity.Subentities.Count == 0)
            {
                return selfMatches;
            }

            var kept = new List<Entity>();
            foreach (var child in entity.Subentities)
            {
                if (filterEntity(child, query))
                {
                    kept.Add(child);
                }
            }

            entity.Subentities = kept;
            return selfMatches || entity.Subentities.Count > 0;
        }

        /// <summary>
        /// Recursively initializes the <see cref="Entity"/> and its subentities by populating the <see cref="Entity.Subentities"/> property with the contents of the directory at <see cref="Entity.Path"/>. If the entity is a folder, it will include both subfolders and files; if it's a file, it will initialize an empty list of subentities.
        /// </summary>
        /// <param name="entity">Root <see cref="Entity"/></param>
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