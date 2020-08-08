using System;
using System.IO;
using System.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace photography_gallery_image_resizer
{
    class Program
    {
        static int thumbnailWidth = 315;
        static int previewWidth = 765;
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
            foreach (string imagePath in fileList)
            {
                Console.WriteLine("Resizing " + imagePath);
                string uploadedImageFileName = imagePath.Split(".").First().Split("\\").Last();
                string uploadedImageExtension = imagePath.Split(".").Last();
                string targetDirectory = outputDirectory + GetRelativeImageDirectory(inputDirectory, imagePath);

                Directory.CreateDirectory(targetDirectory);

                ResizeImage(imagePath, thumbnailWidth, "_thumbnail", uploadedImageFileName, targetDirectory, uploadedImageExtension);
                ResizeImage(imagePath, previewWidth, "_preview", uploadedImageFileName, targetDirectory, uploadedImageExtension);

                File.Move(imagePath, targetDirectory + "\\" + uploadedImageFileName + "." + uploadedImageExtension);
            }
            DeleteEmptyDirectories(inputDirectory);
        }

        static void ResizeImage(string imagePath, int newWidth, string outputType, string uploadedImageFileName, string uploadedImageDirectory, string uploadedImageExtension)
        {
            using Image image = Image.Load(imagePath);
            image.Mutate(x => x.Resize(newWidth, Convert.ToInt32(newWidth * GetImageRatio(image.Width, image.Height))));
            image.Save(uploadedImageDirectory + "//" + uploadedImageFileName + outputType + "." + uploadedImageExtension);
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
        static void DeleteEmptyDirectories(string inputDirectory)
        {
            string[] directoryList = Directory.GetDirectories(inputDirectory, "*.*", SearchOption.AllDirectories);
            foreach (string dir in directoryList)
            {
                Directory.Delete(dir, false);
            }
        }
    }
}
