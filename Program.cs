using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

using static System.Console;

namespace HelloWorld
{
	///<summary>
	///Hashing code from: http://csharphelper.com/blog/2018/07/perform-image-hashing-in-c/
	///</summary>
    class Program
    {
        static String subPath = @"Packages\Microsoft.Windows.ContentDeliveryManager_cw5n1h2txyewy\LocalState\Assets";

        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                WriteLine("Usage: dotnet run <target_folder>");
                return;
            }

            var targetFolder = args[0];
            var app = new Program();
            app.Run(targetFolder);
        }

        private void Run(string targetFolder)
        {
            if (!Directory.Exists(targetFolder))
            {
                WriteLine($"The specified target folder \"{targetFolder}\" does not exist");
                return;
            }

            var hashes = ScanTargetFolder(targetFolder);

            var assetPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), subPath);
            WriteLine($"Path: {assetPath}");

            var count = Directory.GetFiles(targetFolder, "*.jpg").Length;

            if (Directory.Exists(assetPath))
            {
                foreach (var fullPath in Directory.GetFiles(assetPath))
                {
                    var image = Image.FromFile(fullPath);
                    if (image.RawFormat.Equals(ImageFormat.Jpeg))
                    {
                        var assetFilename = Path.GetFileName(fullPath);

                        var hash = ProcessImage(image);
                        if (hashes.Contains(hash))
                        {
                            WriteLine($"It seems that {assetFilename} does exist already at the target folder -> skipped");
                        }
                        else
                        {
                            count++;

                            var targetFilename = Path.Combine(targetFolder, $"{count:0000}.jpg");

                            WriteLine($"Copy {assetFilename} to {targetFilename}");
                            File.Copy(fullPath, targetFilename);
                        }
                    }
                }
            }
            else
            {
                WriteLine($"Path \"{assetPath}\" not found");
            }
        }

        private ISet<string> ScanTargetFolder(string targetFolder)
        {
            if (string.IsNullOrWhiteSpace(targetFolder))
            {
                throw new ArgumentException("Invalid target folder specified (null, empty, or whitespaces only)");
            }

            var hashSet = new HashSet<string>();

            if (Directory.Exists(targetFolder))
            {
                foreach (var fullPath in Directory.GetFiles(targetFolder, "*.jpg"))
                {
                    var image = Image.FromFile(fullPath);
                    if (image.RawFormat.Equals(ImageFormat.Jpeg))
                    {
                        var hash = ProcessImage(image);
                        hashSet.Add(hash);
                    }
                }
            }

            return hashSet;
        }

        private string ProcessImage(Image original)
        {
            var scaled = ScaleTo(original, 9, 9, InterpolationMode.High);
            var monochrome = ToMonochrome(scaled);
            var hashCode = GetHashCode(monochrome);

            return hashCode;
        }

        private Bitmap ScaleTo(Image original, int targetWidth, int targetHeight, InterpolationMode mode)
        {
            if (original == null)
            {
                throw new ArgumentException("Invalid bitmap specified (null)");
            }

            if (targetWidth < 1)
            {
                throw new ArgumentOutOfRangeException("Invalid target width specified (less then 1)");
            }

            if (targetHeight < 1)
            {
                throw new ArgumentOutOfRangeException("Invalid target height specified (less then 1)");
            }

            Bitmap target = new Bitmap(targetWidth, targetHeight);

            using (Graphics graphics = Graphics.FromImage(target))
            {
                RectangleF sourceRect = new RectangleF(-0.5f, -0.5f, original.Width, original.Height);
                Rectangle targetRect = new Rectangle(0, 0, targetWidth, targetHeight);
                graphics.InterpolationMode = mode;
                graphics.DrawImage(original, targetRect, sourceRect, GraphicsUnit.Pixel);
            }

            return target;
        }

        private Bitmap ToMonochrome(Image image)
        {
            var colorMatrix = new ColorMatrix(new float[][] 
            {
                new float[] {0.299f, 0.299f, 0.299f, 0, 0},
                new float[] {0.587f, 0.587f, 0.587f, 0, 0},
                new float[] {0.114f, 0.114f, 0.114f, 0, 0},
                new float[] { 0, 0, 0, 1, 0},
                new float[] { 0, 0, 0, 0, 1}
            });

            var attributes = new ImageAttributes();
            attributes.SetColorMatrix(colorMatrix);

            Point[] points =
            {
                new Point(0, 0),
                new Point(image.Width, 0),
                new Point(0, image.Height)
            };

            Rectangle rectangle = new Rectangle(0, 0, image.Width, image.Height);

            Bitmap result = new Bitmap(image.Width, image.Height);
            using (Graphics graphics = Graphics.FromImage(result))
            {
                graphics.DrawImage(image, points, rectangle, GraphicsUnit.Pixel, attributes);
            }

            return result;
        }
    
        private string GetHashCode(Bitmap bitmap)
        {
            var rowHashCode = string.Empty;
            var colHashCode = string.Empty;

            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    if (bitmap.GetPixel(c + 1, r).R >= bitmap.GetPixel(c, r).R)
                    {
                        rowHashCode += "1";
                    }
                    else
                    {
                        rowHashCode += "0";
                    }
                }
            }

            for (int c = 0; c < 8; c++)
            {
                for (int r = 0; r < 8; r++)
                {
                    if (bitmap.GetPixel(c, r + 1).R >= bitmap.GetPixel(c, r).R)
                    {
                        colHashCode += "1";
                    }
                    else
                    {
                        colHashCode += "0";
                    }
                }
            }

            return $"{rowHashCode},{colHashCode}";
        }
    }
}
