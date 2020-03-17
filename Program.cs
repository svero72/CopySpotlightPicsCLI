using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using Svero.CopySpotlightPics.Models;
using static System.Console;

namespace Svero.CopySpotlightPics
{
    ///<summary>
    ///Hashing code from: http://csharphelper.com/blog/2018/07/perform-image-hashing-in-c/
    ///</summary>
    internal static class Program
    {
        private const string DefaultAssetPath =
            @"Packages\Microsoft.Windows.ContentDeliveryManager_cw5n1h2txyewy\LocalState\Assets";

        private static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                WriteLine("Usage: dotnet run [<source_folder>] <target_folder>");
                return;
            }

            string targetFolder, sourceFolder;

            if (args.Length == 1)
            {
                sourceFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), DefaultAssetPath);
                targetFolder = args[0];
            }
            else
            {
                sourceFolder = args[0];
                targetFolder = args[1];
            }

            if (!Directory.Exists(sourceFolder))
            {
                WriteLine($"The source folder {sourceFolder} does not exist");
            }

            if (!Directory.Exists(targetFolder))
            {
                WriteLine($"Creating target folder \"{targetFolder}\"");
                Directory.CreateDirectory(targetFolder);
            }

            var hashes = ImageTools.ScanFolder(targetFolder, "*.jpg");

            var nextPictureNumber = Directory.GetFiles(targetFolder, "*.jpg").Length + 1;

            if (Directory.Exists(sourceFolder))
            {
                var counter = 0;

                foreach (var candidateFile in Directory.GetFiles(sourceFolder))
                {
                    try
                    {
                        var image = Image.FromFile(candidateFile);
                        if (!image.RawFormat.Equals(ImageFormat.Jpeg))
                        {
                            WriteLine($"Skip {candidateFile} - Wrong format ({image.RawFormat})");
                        }
                        else if (image.Width < 1600)
                        {
                            WriteLine($"Skip {candidateFile} - Wrong width ({image.Width})");
                        }
                        else
                        {
                            var fileName = Path.GetFileName(candidateFile);
                            var hash = ImageTools.ProcessImage(image);

                            var picture = ImageTools.SearchByCode(hash, hashes);

                            if (picture == null)
                            {
                                counter++;

                                var targetFilename = $"{nextPictureNumber++:0000}.jpg";
                                var targetFullPath = Path.Combine(targetFolder, targetFilename);
                                while (File.Exists(targetFullPath))
                                {
                                    targetFilename = $"{nextPictureNumber++:0000}.jpg";
                                    targetFullPath = Path.Combine(targetFolder, targetFilename);
                                }

                                WriteLine(
                                    $"{fileName} seems to be new - copying it to {targetFolder} " +
                                    $"as {targetFilename}");
                                File.Copy(candidateFile, targetFullPath, false);
                            }
                            else
                            {
                                WriteLine($"Skip {candidateFile} - It already exists as {picture.Path}");
                            }
                        }
                    }
                    catch (OutOfMemoryException ex)
                    {
                        WriteLine($"The file {candidateFile} is not a valid image or has an " +
                                  $"unsupported format: {ex.Message}");
                    }
                }

                WriteLine($"Number of copied images: {counter}");
            }
            else
            {
                WriteLine($"Path \"{sourceFolder}\" not found");
            }
        }
    }
}