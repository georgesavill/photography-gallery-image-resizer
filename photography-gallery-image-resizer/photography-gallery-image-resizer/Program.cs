using System;
using System.IO;

namespace photography_gallery_image_resizer
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Please provide two arguments - \"INPUT DIRECTORY\" \"OUTPUT DIRECTORY\"");
                Environment.ExitCode = -1;
            }

            string inputDirectory = args[0];
            string outputDirectory = args[1];
            Console.WriteLine("Resizing images in " + inputDirectory);
            if (Directory.Exists(inputDirectory))
            {
                ProcessImages(inputDirectory, outputDirectory);
            }
            else
            {
                Console.WriteLine("Input directory provided (" + inputDirectory + ") does not exist");
                Environment.ExitCode = -1;
            }
        }

        static void ProcessImages(string inputDirectory, string outputDirectory)
        {
            string[] fileList = Directory.GetFiles(inputDirectory, "*.jpg", SearchOption.AllDirectories);
            foreach (string entry in fileList) { Console.WriteLine(entry); }
        }
    }
}
