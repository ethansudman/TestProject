namespace TestProject
{
    // We could also use appsettings.json for this, but let's keep it simple for now.
    // There's an example of how to do this here: https://stackoverflow.com/questions/69390676/how-to-use-appsettings-json-in-asp-net-core-6-program-cs-file
    public class GlobalConstants
    {
        /// <summary>
        /// We need to impose at least SOME limit on the directory that users can browse to prevent them from accessing arbitrary files on the server. This is the name of the directory within the current working directory that users will be allowed to browse. For example, if this is set to "BrowsableDirectory", then users will only be able to access files and folders within the "BrowsableDirectory" folder located in the current working directory of the application.
        /// </summary>
        public const string BrowsableDirectoryName = "BrowsableDirectory";
    }
}
