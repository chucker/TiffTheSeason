using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

#nullable enable

namespace TiffTheSeason
{
    class Program
    {
        private const int ImageSize = 64;

        static void Main(string[] args)
        {
            var inputDir = new DirectoryInfo("../../../input");
            var outputDir = new DirectoryInfo("../../../output");

            outputDir.Create();

            // replicate input dir hierarchy into output dir

            foreach (var inputFSItem in inputDir.EnumerateFileSystemInfos("*", new EnumerationOptions
            {
                RecurseSubdirectories = true
            }))
            {
                var relativePath = inputFSItem.GetPathRelativeTo(inputDir);
                string equivalentOutputPath = Path.Combine(outputDir.FullName, relativePath);

                switch (inputFSItem)
                {
                    case DirectoryInfo directory:
                        Directory.CreateDirectory(equivalentOutputPath);
                        break;
                    case FileInfo inputFile:
                        Color color;
                        using (var inBitmap = new Bitmap(inputFile.FullName))
                        {
                            color = inBitmap.GetMostFrequentColor();
                        }

                        using (var outBitmap = new Bitmap(ImageSize, ImageSize))
                        using (var g = Graphics.FromImage(outBitmap))
                        {
                            g.Clear(color);

                            g.Save();

                            outBitmap.Save(equivalentOutputPath, ImageFormat.Tiff);
                        }
                        break;
                }
            }
        }
    }

    public static class FileSystemInfoExtensions
    {
        public static string GetPathRelativeTo(this FileSystemInfo fileSystemInfo, FileSystemInfo baseFsi)
        {
            var pathUri = new Uri(fileSystemInfo.FullName);

            string basePath = baseFsi.FullName;

            // Folders must end in a slash
            if (!basePath.EndsWith(Path.DirectorySeparatorChar.ToString()))
                basePath += Path.DirectorySeparatorChar;

            var folderUri = new Uri(basePath);

            return Uri.UnescapeDataString(folderUri.MakeRelativeUri(pathUri)
                                                   .ToString()
                                                   .Replace('/', Path.DirectorySeparatorChar));
        }
    }

    public static class BitmapExtensions
    {
        public static Color GetMostFrequentColor(this Bitmap bitmap)
        {
            var colors = new List<Color>();

            for (int x = 0; x < bitmap.Width; x++)
            {
                for (int y = 0; y < bitmap.Height; y++)
                {
                    colors.Add(bitmap.GetPixel(x, y));
                }
            }

            return colors.Where(c =>
                         {
                             // this is probably "white"-like, and not part of the real color
                             const int brightestColor = 0xFD;
                             return c.R < brightestColor || c.G < brightestColor || c.B < brightestColor;
                         })
                         .GroupBy(c => c)
                         .OrderByDescending(g => g.Count())
                         .First().Key;
        }
    }
}
