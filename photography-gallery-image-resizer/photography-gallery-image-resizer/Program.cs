using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace photography_gallery_image_resizer
{
    class Program
    {
        static int thumbnailWidth = 315;
        static int previewWidth = 765;
        static string directorySeparator = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "\\" : "/";

        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Please provide two arguments - \"INPUT DIRECTORY\" \"OUTPUT DIRECTORY\"");
                Environment.Exit(-1);
            }

            string inputDirectory = args[0];
            string outputDirectory = args[1];

            if (Directory.Exists(inputDirectory) && Directory.Exists(outputDirectory))
            {
                ProcessImages(inputDirectory, outputDirectory);
            }
            else
            {
                Console.WriteLine("Input or output directory provided (" + inputDirectory + " or " + outputDirectory + ") does not exist");
                Environment.Exit(-1);
            }
        }

        static void ProcessImages(string inputDirectory, string outputDirectory)
        {
            string[] fileList = Directory.GetFiles(inputDirectory, "*.jpg", SearchOption.AllDirectories);
            if (fileList.Length == 0)
            {
                Console.WriteLine("No .jpg files present in input directory (" + inputDirectory + ")");
                Environment.Exit(-1);
            }
            foreach (string imagePath in fileList)
            {
                Console.WriteLine("Resizing " + imagePath);
                string uploadedImageFileName = imagePath.Split(".").First().Split(directorySeparator).Last();
                string uploadedImageExtension = imagePath.Split(".").Last();
                string targetDirectory = outputDirectory + GetRelativeImageDirectory(inputDirectory, imagePath);

                Directory.CreateDirectory(targetDirectory);

                ResizeImage(imagePath, thumbnailWidth, "_thumbnail", uploadedImageFileName, targetDirectory, uploadedImageExtension);
                ResizeImage(imagePath, previewWidth, "_preview", uploadedImageFileName, targetDirectory, uploadedImageExtension);

                File.Move(imagePath, targetDirectory + directorySeparator + uploadedImageFileName + "." + uploadedImageExtension);
            }
            DeleteEmptyDirectories(inputDirectory);
        }

        static void ResizeImage(string imagePath, int newWidth, string outputType, string uploadedImageFileName, string uploadedImageDirectory, string uploadedImageExtension)
        {
            using Image image = Image.Load(imagePath);
            image.Mutate(x => x.Resize(newWidth, Convert.ToInt32(newWidth * GetImageRatio(image.Width, image.Height))));
            JpegEncoder encoder = new JpegEncoder { Quality = 75 };
            image.Save(uploadedImageDirectory + directorySeparator + uploadedImageFileName + outputType + "." + uploadedImageExtension, encoder);
        }

        static string GetRelativeImageDirectory(string inputDirectory, string imagePath)
        {
            string[] splitImagePath = imagePath.Split(inputDirectory).Last().Split(directorySeparator);
            Array.Resize(ref splitImagePath, splitImagePath.Length - 1);
            return string.Join(directorySeparator, splitImagePath);
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
                Directory.Delete(dir, true);
            }
        }
    }
}
