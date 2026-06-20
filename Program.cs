using System.Text.Json.Serialization;

namespace TestProject {
    public class Program {
        public static void Main(string[] args) {
            EnsureBrowsableDirectoryExists();

            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                });

            var app = builder.Build();

            // Configure the HTTP request pipeline.

            app.UseHttpsRedirection();

            app.UseStaticFiles();

            app.MapControllers();

            app.Run();
        }

        private static void EnsureBrowsableDirectoryExists()
        {
            // Ensure the browsable directory exists
            if (!Directory.Exists(GlobalConstants.BrowsableDirectoryName))
            {
                // Create the browsable directory if it doesn't exist - this should always happen on the first run of the application
                Directory.CreateDirectory(GlobalConstants.BrowsableDirectoryName);

                // The next step is only relevant to the demo - conditional compilation is used to ensure that this code is only included in debug builds, so it won't affect release builds of the application.
#if DEBUG
                // For demo purposes, create some junk folders inside the browsable directory. We may want to consider removing this in the future, but it helps to demonstrate that the directory is browsable and that the folders are visible when the application is run.
                var random = new Random();

                int maxFolder = 65 + random.Next(3, 10);
                // 65 is the ASCII code for 'A', so this will create folders named "Folder A", "Folder B", etc.
                for (int i = 65; i < maxFolder; i++)
                {
                    string folderPath = Path.Combine(Directory.GetCurrentDirectory(), GlobalConstants.BrowsableDirectoryName, $"Folder {(char)i}");
                    Directory.CreateDirectory(folderPath);

                    // Create some random files inside of it as well, to further demonstrate that the directory is browsable and that the files are visible when the application is run.

                    int maxFile = 65 + random.Next(3, 10);
                    for (int j = 65; j < maxFile; j++)
                    {
                        string filePath = Path.Combine(folderPath, $"File {(char)j}.txt");
                        File.WriteAllText(filePath, $"This is file {(char)j} in folder {(char)i}.");
                    }
                }
#endif
            }
        }
    }
}