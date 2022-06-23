using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using static Svero.CopySpotlightPics.ConsoleHelper;

namespace Svero.CopySpotlightPics
{
    ///<summary>
    ///Hashing code from: http://csharphelper.com/blog/2018/07/perform-image-hashing-in-c/
    ///</summary>
    internal static class Program
    {
        private const string DefaultAssetPath =
            @"Packages\Microsoft.Windows.ContentDeliveryManager_cw5n1h2txyewy\LocalState\Assets";

		///<summary>
		///Entry point for the application
		///</summary>
        /// <param name="args">String array with command-line arguments</param>
		[SupportedOSPlatform("Windows7.0")]
        private static void Main(string[] args)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }
            
            if (args.Length < 1)
            {
                WriteMessage("Usage: dotnet run [<source_folder>] <target_folder>", ConsoleColor.Yellow);
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
                WriteMessage($"The source folder {sourceFolder} does not exist", ConsoleColor.Yellow);
            }

            if (!Directory.Exists(targetFolder))
            {
                WriteMessage($"Creating target folder \"{targetFolder}\"", ConsoleColor.Yellow);
                Directory.CreateDirectory(targetFolder);
            }

            var hashes = ImageTools.ScanFolder(targetFolder, "*.jpg");

            var nextPictureNumber = Directory.GetFiles(targetFolder, "*.jpg").Length + 1;

            if (Directory.Exists(sourceFolder))
            {
                var counter = 0;
                
                WriteMessage($"Source folder: {sourceFolder}", ConsoleColor.Gray);
                WriteMessage($"Target folder: {targetFolder}", ConsoleColor.Gray);
                WriteMessage(string.Empty);

                foreach (var candidateFile in Directory.GetFiles(sourceFolder))
                {
                    try
                    {
                        var image = Image.FromFile(candidateFile);
                        
                        if (image.RawFormat.Equals(ImageFormat.Jpeg) && image.Width >= 1600)
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

                                WriteMessage(
                                    $"{fileName} seems to be new - copying it as {targetFilename}", 
                                    ConsoleColor.Green);
                                File.Copy(candidateFile, targetFullPath, false);
                            }
                            else
                            {
                                var pictureId = Path.GetFileNameWithoutExtension(picture.Path);
                                WriteMessage(
                                    $"Skip {fileName} - It already exists with ID {pictureId}", 
                                    ConsoleColor.Gray);
                            }
                        }
                    }
                    catch (OutOfMemoryException ex)
                    {
                        WriteMessage(
                            $"The file {candidateFile} is not a valid image or has an unsupported format: {ex.Message}",
                            ConsoleColor.Red);
                    }
                }

                WriteMessage(string.Empty);
                
                if (counter > 0)
                {
                    WriteMessage($"Number of copied images: {counter}", ConsoleColor.Green);
                }
                else
                {
                    WriteMessage("No new wallpapers were copied", ConsoleColor.Yellow);
                }
                
            }
            else
            {
                WriteMessage($"Path \"{sourceFolder}\" not found", ConsoleColor.Red);
            }
        }
    }
}
