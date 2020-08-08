using System;
using System.IO;
using System.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

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
            foreach (string imagePath in fileList) {
                Console.WriteLine("Resizing " + imagePath);
                ResizeImage(imagePath, 315, "_thumbnail", inputDirectory, outputDirectory);
                ResizeImage(imagePath, 765, "_preview", inputDirectory, outputDirectory);
            }
        }

        static void ResizeImage(string imagePath, int newWidth, string outputType, string inputDirectory, string outputDirectory)
        {
            using Image image = Image.Load(imagePath);

            image.Mutate(x => x.Resize(newWidth, Convert.ToInt32(newWidth * GetImageRatio(image.Width, image.Height))));

            string uploadedImageFileName = imagePath.Split(".").First().Split("\\").Last();
            string uploadedImageDirectory = GetRelativeImageDirectory(inputDirectory, imagePath);
            string uploadedImageExtension = imagePath.Split(".").Last();

            Directory.CreateDirectory(outputDirectory + uploadedImageDirectory);

            image.Save(outputDirectory + uploadedImageDirectory + "//" + uploadedImageFileName + outputType + "." + uploadedImageExtension);
        }

        static string GetRelativeImageDirectory(string inputDirectory, string imagePath)
        {
            string[] splitImagePath = imagePath.Split(inputDirectory).Last().Split("\\");
            Array.Resize(ref splitImagePath, splitImagePath.Length - 1);
            return string.Join("\\", splitImagePath);
        }

        static float GetImageRatio(int width, int height)
        {
            float w = width;
            float h = height;
            return h / w;
        }
    }
}
