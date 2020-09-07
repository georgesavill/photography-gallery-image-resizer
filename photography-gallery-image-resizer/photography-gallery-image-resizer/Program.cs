using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.Processing;
using StackExchange.Redis;


namespace photography_gallery_image_resizer
{
    class Program
    {
        static int[] imageSizes = { 400, 800, 1600 };
        static string directorySeparator = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "\\" : "/";

        static void Main(string[] args)
        {
            ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("192.168.1.179:6379,allowAdmin=true");
            IDatabase redisDatabase = redis.GetDatabase(9);

            if (args.Length != 2)
            {
                Console.WriteLine("Please provide two arguments - \"INPUT DIRECTORY\" \"OUTPUT DIRECTORY\"");
                Environment.Exit(-1);
            }

            string inputDirectory = args[0];
            string outputDirectory = args[1];

            if (Directory.Exists(inputDirectory) && Directory.Exists(outputDirectory))
            {
                ProcessImages(inputDirectory, outputDirectory, redisDatabase);
            }
            else
            {
                Console.WriteLine("Input or output directory provided (" + inputDirectory + " or " + outputDirectory + ") does not exist");
                Environment.Exit(-1);
            }
        }

        static void ProcessImages(string inputDirectory, string outputDirectory, IDatabase redisDatabase)
        {
            string[] fileList = Directory.GetFiles(inputDirectory, "*.jpg", SearchOption.AllDirectories);
            if (fileList.Length == 0)
            {
                Console.WriteLine("No .jpg files present in input directory (" + inputDirectory + ")");
                Environment.Exit(-1);
            }
            var opts = new ParallelOptions { MaxDegreeOfParallelism = Convert.ToInt32(Math.Ceiling((Environment.ProcessorCount * 0.75) * 1.0)) };
            Parallel.ForEach(fileList, opts, (imagePath) =>
            {
                string uploadedImageFileName = imagePath.Split(".").First().Split(directorySeparator).Last();
                string uploadedImageExtension = imagePath.Split(".").Last();
                string targetDirectory = outputDirectory + GetRelativeImageDirectory(inputDirectory, imagePath);
                string potentialExistingImage = outputDirectory + imagePath.Split(inputDirectory)[1];

                if (File.Exists(potentialExistingImage))
                {

                    if (ImagesAreDifferent(imagePath, potentialExistingImage))
                    {
                        foreach (int size in imageSizes)
                        {
                        ResizeImage(imagePath, size, uploadedImageFileName, targetDirectory, uploadedImageExtension, redisDatabase);
                        }
                        File.Copy(imagePath, targetDirectory + directorySeparator + uploadedImageFileName + "." + uploadedImageExtension, true);
                    }
                }
                else
                {
                    foreach (int size in imageSizes)
                    {
                        Console.WriteLine(imagePath + " is a new image, resizing...");
                        ResizeImage(imagePath, size, uploadedImageFileName, targetDirectory, uploadedImageExtension, redisDatabase);
                    }
                    File.Copy(imagePath, targetDirectory + directorySeparator + uploadedImageFileName + "." + uploadedImageExtension, true);
                }
            });
        }

        static bool ImagesAreDifferent(string imagePath, string potentialExistingImage)
        {
            FileInfo inputImage = new FileInfo(imagePath);
            FileInfo existingImage = new FileInfo(potentialExistingImage);

            if (inputImage.Length != existingImage.Length) { 
                Console.WriteLine(imagePath + " exists in output directory, but the image size has changed, resizing...");
                return true; 
            }
            else if (!File.ReadAllBytes(inputImage.FullName).SequenceEqual(File.ReadAllBytes(existingImage.FullName)))
            {
                Console.WriteLine(imagePath + " exists in output directory, but the image content has changed, resizing...");
                return true;
            } else
            {
                Console.WriteLine(imagePath + " exists in output directory, and is identical to the input image, skipping..."); ;
                return false;
            }

        }

        static void ResizeImage(string imagePath, int newWidth, string uploadedImageFileName, string uploadedImageDirectory, string uploadedImageExtension, IDatabase redisDatabase)
        {
            Directory.CreateDirectory(uploadedImageDirectory);
            using Image image = Image.Load(imagePath);
            image.Mutate(x => x.Resize(newWidth, Convert.ToInt32(newWidth * GetImageRatio(image.Width, image.Height))));
            int imageQuality;
            if (newWidth < 500)
            {
                imageQuality = 75;
            } else
            {
                imageQuality = 65;
            }
            JpegEncoder encoder = new JpegEncoder { Quality = imageQuality };
            image.Save(uploadedImageDirectory + directorySeparator + uploadedImageFileName + "_" + newWidth.ToString() + ".jpg", encoder);
            redisDatabase.HashSet(uploadedImageFileName + "." + uploadedImageExtension, new HashEntry[] {
                new HashEntry("Model",image.Metadata.ExifProfile.GetValue(ExifTag.Model).ToString()),
                new HashEntry("LensModel",image.Metadata.ExifProfile.GetValue(ExifTag.LensModel).ToString()),
                new HashEntry("FNumber",FixFNumber(image.Metadata.ExifProfile.GetValue(ExifTag.FNumber).ToString())),
                new HashEntry("FocalLength",image.Metadata.ExifProfile.GetValue(ExifTag.FocalLength).ToString()),
                new HashEntry("ExposureTime",image.Metadata.ExifProfile.GetValue(ExifTag.ExposureTime).ToString()),
                new HashEntry("Height",image.Height),
                new HashEntry("Width",image.Width),
                new HashEntry("AspectRatio",GetImageRatio(image.Width, image.Height))
            });
        }
        
        static string FixFNumber(string input)
        {
            string[] splitInput = input.Split("/");
            if (splitInput.Length == 1) {
                return input;
            } 
            else
            {
                return (float.Parse(splitInput[0]) / 10).ToString();
            }
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
    }
}
