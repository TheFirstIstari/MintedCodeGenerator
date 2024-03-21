namespace MintedCodeGenerator
{
    public class FilePath
    {
        public static readonly char DIRECTORY_SEPARATOR = '\\'; // Windows
        private static readonly char EXTENSION_SEPARATOR = '.';

        private readonly string fullyQualifiedPath;
        private readonly string fileName;
        private readonly string fileExtension;

        public string FullyQualifiedPath => fullyQualifiedPath;
        public string FileName => fileName;
        public string FileExtension => fileExtension;

        public string GetPathWithSeparator(char separator)
        {
            return fullyQualifiedPath.Replace(DIRECTORY_SEPARATOR, separator);
        }

        private static string ExtractFileName(string path)
        {
            string[] splitPath = path.Split(DIRECTORY_SEPARATOR);
            return splitPath[^1];
        }

        private static string ExtractFileExtension(string fileName)
        {
            string[] fileAndExtension = fileName.Split(EXTENSION_SEPARATOR, 2); // Allow compound extensions like .xaml.cs by only splitting once.
            return EXTENSION_SEPARATOR + fileAndExtension[1];
        }

        public FilePath(string fullyQualifiedPath)
        {
            this.fullyQualifiedPath = fullyQualifiedPath;
            fileName = ExtractFileName(fullyQualifiedPath);
            fileExtension = ExtractFileExtension(fileName);
        }
    }
}
