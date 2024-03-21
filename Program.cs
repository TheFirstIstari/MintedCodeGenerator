namespace MintedCodeGenerator
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length > 0 && (args[1] == "-h") || args[1] == "--help")
            {
                Console.WriteLine($"{AppDomain.CurrentDomain.FriendlyName} usage:");
                Console.WriteLine($"Command is: \"{AppDomain.CurrentDomain.FriendlyName} <outfile> <sourcedir>\", where <outfile> is the tex file to output to, and <sourcedir> is the directory to search.");
                return;
            }
            if (args.Length != 2)
            {
                throw new ArgumentException("The program must be run with 2 command-line arguments. The first should be the file path to place the LaTeX output, and the second should be the directory containing the source code to document.");
            }
            string outputFilePath = args[0];
            string sourceFileDirectory = args[1];
            //string[] testHeader = ["\\documentclass{article}", "", "\\usepackage{tikz}", "\\usepackage{minted}", "\\usepackage{hyperref}", "\\begin{document}"];
            string[] subfileHeader = ["\\documentclass[../main.tex]{subfiles}", "", "\\begin{document}", "\\chapter{Source code}", "\\label{sourceCode}"];
            string[] trailer = ["\\end{document}"];
            TexWriter.CreateOutputFile(outputFilePath, sourceFileDirectory, subfileHeader, trailer);
        }
    }
}
