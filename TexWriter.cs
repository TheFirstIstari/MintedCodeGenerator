namespace MintedCodeGenerator
{
    public static class TexWriter
    {
        private static readonly int MAX_Y_COORD = 30;
        private static readonly double X_COORDINATE_WRAP = 8;
        private static readonly double FILE_LISTING_SCALAR = 0.85;

        private static void WriteLines(this StreamWriter streamWriter, IEnumerable<string> lines)
        {
            foreach (var line in lines)
            {
                streamWriter.WriteLine(line);
            }
        }

        public static string GetRelativePath(string fullPath, string startDirectory)
        {
            string[] splitPath = startDirectory.Split('\\');
            return splitPath[^1] + fullPath.Remove(0, startDirectory.Length);
        }

        private static string TikzPath(double xStart, double yStart, double xEnd, double yEnd) => $"\\draw ({xStart}, {yStart}) |- ({xEnd}, {yEnd});";
        private static string FileNode(string fileName, double xCoord, double yCoord) => $"\\node[right] at ({xCoord}, {yCoord}) {{\\fileref{{{fileName}}}}};";
        private static string FolderNode(string folderName, double xCoord, double yCoord) => $"\\node[right, fill=white] at ({xCoord - 0.25}, {yCoord}) {{{folderName}}};";

        private static double TreeX(double depth, int wrapNumber)
        {
            return depth * 0.5 + wrapNumber * X_COORDINATE_WRAP;
        }

        private static double TreeY(double nodeNumber, int wrapNumber)
        {
            return -(nodeNumber * 0.5 - wrapNumber * MAX_Y_COORD);
        }

        /// <summary>
        /// Recursively traverses the folder structure and adds to pathCommands and nodeCommands to create the Tikz tree.
        /// </summary>
        /// <param name="pathCommands">The tikz path commands.</param>
        /// <param name="nodeCommands">The tikz node commands.</param>
        /// <param name="filePaths">The list, in pre-order, of files in the directory</param>
        /// <param name="itemNumber">The number item to start at. Internal value: use 0 for non-recursion calls.</param>
        /// <param name="nodeNumber">The number node to start at. Internal value: use 0 for non-recursion calls.</param>
        /// <param name="depth">The number of subfolders from the start. Internal value: use 0 for non-recursion calls.</param>
        /// <param name="wrapNumber">The number of rightward shifts to start from. Internal value: use 0 for non-recursion calls.</param>
        /// <returns>The new itemNumber and nodeNumber. Internal value, but the first value should be equal to the length of <paramref name="filePaths"/> once execution finishes.</returns>
        private static (int, int, int) CreateFileTree(List<string> pathCommands, List<string> nodeCommands, List<FilePath> filePaths, int itemNumber, int nodeNumber, int depth, int wrapNumber)
        {
            // Folder node
            string[] pathSplit = filePaths[itemNumber].FullyQualifiedPath.Split(FilePath.DIRECTORY_SEPARATOR);
            string currentFolder = pathSplit[depth];
            if (depth != 0)
            {
                pathCommands.Add(TikzPath(TreeX(depth - 1, wrapNumber), TreeY(nodeNumber - 1, wrapNumber), TreeX(depth, wrapNumber), TreeY(nodeNumber, wrapNumber)));
                for (int i = 0; i < depth - 1; i++) // Draw horizontal lines for all trees before the current
                {
                    pathCommands.Add(TikzPath(TreeX(i, wrapNumber), TreeY(nodeNumber - 1, wrapNumber), TreeX(i, wrapNumber), TreeY(nodeNumber, wrapNumber)));
                }
            }

            nodeCommands.Add(FolderNode(currentFolder, TreeX(depth, wrapNumber), TreeY(nodeNumber, wrapNumber)));
            depth++;
            nodeNumber++;


            while (itemNumber < filePaths.Count)
            {
                pathSplit = filePaths[itemNumber].FullyQualifiedPath.Split(FilePath.DIRECTORY_SEPARATOR);
                int nodeDepth = pathSplit.Length - 1;
                if (nodeDepth > depth) // Encountered a new folder, recurse.
                {
                    (itemNumber, nodeNumber, wrapNumber) = CreateFileTree(pathCommands, nodeCommands, filePaths, itemNumber, nodeNumber, depth, wrapNumber);
                }
                else if (nodeDepth == depth && pathSplit[depth - 1] == currentFolder) // File node in the current folder
                {
                    for (int i = 0; i < depth - 1; i++) // Draw horizontal lines for all trees before the current
                    {
                        pathCommands.Add(TikzPath(TreeX(i, wrapNumber), TreeY(nodeNumber - 1, wrapNumber), TreeX(i, wrapNumber), TreeY(nodeNumber, wrapNumber)));
                    }
                    pathCommands.Add(TikzPath(TreeX(depth - 1, wrapNumber), TreeY(nodeNumber - 1, wrapNumber), TreeX(depth, wrapNumber), TreeY(nodeNumber, wrapNumber))); // and a |_ for the current item.
                    nodeCommands.Add(FileNode(pathSplit[depth], TreeX(depth, wrapNumber), TreeY(nodeNumber, wrapNumber)));

                    nodeNumber++;
                    itemNumber++;
                    if (-TreeY(nodeNumber, 0) - wrapNumber * MAX_Y_COORD > MAX_Y_COORD) wrapNumber++;
                }
                else // New folder started or depth decreased - tree has ended.
                {
                    return (itemNumber, nodeNumber, wrapNumber);
                }
            }
            return (itemNumber, nodeNumber, wrapNumber);
        }

        private static void CreateFileListing(StreamWriter streamWriter, string startDirectory, IEnumerable<FilePath> fullFilePaths)
        {
            List<FilePath> filePaths = new List<FilePath>();
            foreach (FilePath filePath in fullFilePaths)
            {
                filePaths.Add(new FilePath(GetRelativePath(filePath.FullyQualifiedPath, startDirectory)));
            }

            streamWriter.WriteLines(["\\begin{figure}[ht]", $"\\begin{{tikzpicture}}[scale={FILE_LISTING_SCALAR}]"]);
            List<string> pathCommands = [];
            List<string> nodeCommands = [];

            _ = CreateFileTree(pathCommands, nodeCommands, filePaths, 0, 0, 0, 0);

            streamWriter.WriteLines(pathCommands);
            streamWriter.WriteLines(nodeCommands);
            streamWriter.WriteLines(["\\end{tikzpicture}", "\\end{figure}"]);
        }

        /// <summary>
        /// Filters a list of files by file extension and assigns the files a language based on the file extensions dictionary.
        /// </summary>
        /// <param name="filePaths">A list of fully-qualified file names.</param>
        /// <param name="fileExtensions">A dictionary of file extensions and languages, with the keys as file extensions including the preceding '.', such as .cs or .xaml.cs, and the values as pygments language specifiers.</param>
        /// <returns>A dictionary containing all the files with extensions in the dictionary, assigned their language.</returns>
        private static Dictionary<FilePath, string> FilterAndAssignFiles(List<FilePath> filePaths, Dictionary<string, string> fileExtensions)
        {
            Dictionary<FilePath, string> filteredFiles = new();
            foreach (FilePath filePath in filePaths)
            {
                if (fileExtensions.TryGetValue(filePath.FileExtension, out string? language))
                {
                    filteredFiles.Add(filePath, language);
                }
            }
            return filteredFiles;
        }
        private static List<FilePath> GetFileList(string directoryPath, List<string> ignoredDirectories)
        {
            List<FilePath> fileList = new List<FilePath>();

            // Check if the directory exists
            if (Directory.Exists(directoryPath))
            {
                // Get files in the current directory
                string[] files = Directory.GetFiles(directoryPath);
                foreach (string file in files)
                {
                    fileList.Add(new FilePath(file));
                }

                // Recursively get files from subdirectories
                string[] subdirectories = Directory.GetDirectories(directoryPath);
                foreach (string subdirectory in subdirectories)
                {
                    string[] directoryParts = subdirectory.Split(FilePath.DIRECTORY_SEPARATOR);
                    bool validDirectory = true;
                    foreach (string directoryPart in directoryParts)
                    {
                        validDirectory &= !ignoredDirectories.Contains(directoryPart);
                    }

                    if (validDirectory)
                    {
                        fileList.AddRange(GetFileList(subdirectory, ignoredDirectories));
                    }
                }
            }
            else
            {
                Console.WriteLine("Directory not found: " + directoryPath);
            }

            return fileList;
        }

        public static void CreateOutputFile(string outputFilePath, string sourceFileDirectory, string[] codeBeforeListings, string[] codeAfterListings)
        {
            List<string> ignoredDirectories = ["bin", "Builds", "obj", "Debug", "x64", ".vs", ".git", "Properties"];
            Dictionary<string, string> fileExtensions = new Dictionary<string, string>
            {
                { ".cs", "cs" },
                { ".xaml.cs", "cs" },
                { ".xaml", "xml" },
                {".manifest", "xml" },
                {".appxmanifest", "xml" }
            };

            List<FilePath> filePaths = GetFileList(sourceFileDirectory, ignoredDirectories);
            Dictionary<FilePath, string> filteredPaths = FilterAndAssignFiles(filePaths, fileExtensions);

            using (StreamWriter streamWriter = new StreamWriter(outputFilePath, false))
            {
                foreach (string line in codeBeforeListings)
                {
                    streamWriter.WriteLine(line);
                }

                streamWriter.WriteLine($"% LaTeX automatically generated by {AppDomain.CurrentDomain.FriendlyName}.");
                streamWriter.WriteLine("\\section{Project code structure}");

                CreateFileListing(streamWriter, sourceFileDirectory, filteredPaths.Keys);

                streamWriter.WriteLines(["\\clearpage", "\\section{Source code}"]);

                foreach (KeyValuePair<FilePath, string> pathAndLanguage in filteredPaths)
                {
                    streamWriter.WriteLine("\\subsection*{" + pathAndLanguage.Key.FileName + "}");
                    streamWriter.WriteLine("\\label{" + pathAndLanguage.Key.FileName + "}");
                    streamWriter.WriteLine("\\inputminted[breaklines]{" + pathAndLanguage.Value + "}{{" + pathAndLanguage.Key.GetPathWithSeparator('/') + "}}");
                }

                foreach (string line in codeAfterListings)
                {
                    streamWriter.WriteLine(line);
                }
            }
        }
    }
}