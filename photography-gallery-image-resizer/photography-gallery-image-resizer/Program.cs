﻿using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.Processing;
using StackExchange.Redis;


namespace photography_gallery_image_resizer
{
    class Program
    {
        static int thumbnailWidth = 357;
        static int previewWidth = 766;
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
            Parallel.ForEach(fileList, (imagePath) =>
            {
                string uploadedImageFileName = imagePath.Split(".").First().Split(directorySeparator).Last();
                string uploadedImageExtension = imagePath.Split(".").Last();
                string targetDirectory = outputDirectory + GetRelativeImageDirectory(inputDirectory, imagePath);
                string potentialExistingImage = outputDirectory + imagePath.Split(inputDirectory)[1];

                if (File.Exists(potentialExistingImage))
                {
                    Console.WriteLine(imagePath + " exists in output directory, checking if changed.");

                    if (ImagesAreDifferent(imagePath, potentialExistingImage))
                    {
                        ResizeImage(imagePath, thumbnailWidth, "_thumbnail", uploadedImageFileName, targetDirectory, uploadedImageExtension, redisDatabase);
                        File.Copy(imagePath, targetDirectory + directorySeparator + uploadedImageFileName + "." + uploadedImageExtension, true);
                    }
                }
                else
                {
                    ResizeImage(imagePath, thumbnailWidth, "_thumbnail", uploadedImageFileName, targetDirectory, uploadedImageExtension, redisDatabase);
                    File.Copy(imagePath, targetDirectory + directorySeparator + uploadedImageFileName + "." + uploadedImageExtension, true);
                }
            });
        }

        static bool ImagesAreDifferent(string imagePath, string potentialExistingImage)
        {
            FileInfo inputImage = new FileInfo(imagePath);
            FileInfo existingImage = new FileInfo(potentialExistingImage);

            if (inputImage.Length != existingImage.Length) { 
                Console.WriteLine("Image size has changed, resizing new image.");
                return true; 
            }
            else if (!File.ReadAllBytes(inputImage.FullName).SequenceEqual(File.ReadAllBytes(existingImage.FullName)))
            {
                Console.WriteLine("Image is different, resizing new image.");
                return true;
            } else
            {
                Console.WriteLine("Image is identical, skipping.");
                return false;
            }

        }

        static void ResizeImage(string imagePath, int newWidth, string outputType, string uploadedImageFileName, string uploadedImageDirectory, string uploadedImageExtension, IDatabase redisDatabase)
        {
            Console.WriteLine("Resizing " + imagePath);
            Directory.CreateDirectory(uploadedImageDirectory);
            using Image image = Image.Load(imagePath);
            image.Mutate(x => x.Resize(newWidth, Convert.ToInt32(newWidth * GetImageRatio(image.Width, image.Height))));
            JpegEncoder encoder = new JpegEncoder { Quality = 80 };
            image.Save(uploadedImageDirectory + directorySeparator + uploadedImageFileName + outputType + "." + uploadedImageExtension, encoder);
            redisDatabase.HashSet(uploadedImageFileName + "." + uploadedImageExtension, new HashEntry[] {
                new HashEntry("Model",image.Metadata.ExifProfile.GetValue(ExifTag.Model).ToString()),
                new HashEntry("LensModel",image.Metadata.ExifProfile.GetValue(ExifTag.LensModel).ToString()),
                new HashEntry("FNumber",FixFNumber(image.Metadata.ExifProfile.GetValue(ExifTag.FNumber).ToString())),
                new HashEntry("FocalLength",image.Metadata.ExifProfile.GetValue(ExifTag.FocalLength).ToString()),
                new HashEntry("ExposureTime",image.Metadata.ExifProfile.GetValue(ExifTag.ExposureTime).ToString())
            });

            if (outputType == "_thumbnail") { ResizeImage(imagePath, previewWidth, "_preview", uploadedImageFileName, uploadedImageDirectory, uploadedImageExtension, redisDatabase); }
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
