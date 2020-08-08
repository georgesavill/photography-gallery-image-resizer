using System;

namespace photography_gallery_image_resizer
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Please provide two arguments - \"INPUT DIRECTORY\" \"OUTPUT DIRECTORY\"");
                Environment.ExitCode = 0xA0;
            } 
            

        }
    }
}
