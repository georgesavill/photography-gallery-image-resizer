using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using StackExchange.Redis;
using ImageMagick;

namespace photography_gallery_image_resizer
{
    class Program
    {
        static int[] imageSizes = { 400, 800, 1600 };
        static string directorySeparator = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "\\" : "/";

        static void Main(string[] args)
        {
            ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("10.0.0.20:6379"); // TODO: Read from config file
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

            using (MagickImage image = new MagickImage(imagePath))
            {
                IExifProfile metadata = image.GetExifProfile();
                string redisReference = uploadedImageFileName + "." + uploadedImageExtension;
                redisDatabase.HashSet(redisReference, new HashEntry[] {
                    new HashEntry("Model",metadata.GetValue(ExifTag.Model).ToString()),
                    new HashEntry("LensModel",metadata.GetValue(ExifTag.LensModel).ToString()),
                    new HashEntry("FNumber",FixFNumber(metadata.GetValue(ExifTag.FNumber).ToString())),
                    new HashEntry("FocalLength",metadata.GetValue(ExifTag.FocalLength).ToString()),
                    new HashEntry("ExposureTime",metadata.GetValue(ExifTag.ExposureTime).ToString()),
                    new HashEntry("Height",image.Height),
                    new HashEntry("Width",image.Width),
                    new HashEntry("Dimensions",image.Width.ToString() + "," + image.Height.ToString()),
                    new HashEntry("AspectRatio",GetImageRatio(image.Width, image.Height))
                });
                Console.WriteLine("Redis data added for: " + redisReference);
                image.Resize(newWidth, Convert.ToInt32(newWidth * GetImageRatio(image.Width, image.Height)));
                image.Strip();
                int imageQuality;
                if (newWidth < 500)
                {
                    imageQuality = 75;
                }
                    else
                {
                    imageQuality = 65;
                }
                image.Quality = imageQuality;
                image.Format = MagickFormat.Pjpeg;

                image.Write(uploadedImageDirectory + directorySeparator + uploadedImageFileName + "_" + newWidth.ToString() + ".jpg");
            }
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
