﻿using System;
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
            var nextPictureNumber = Directory.GetFiles(targetFolder, "*.jpg").Length + 1;

            if (Directory.Exists(assetPath))
            {
                var counter = 0;

                foreach (var assetFullPath in Directory.GetFiles(assetPath))
                {
                    var image = Image.FromFile(assetFullPath);
                    if (image.RawFormat.Equals(ImageFormat.Jpeg))
                    {
                        var assetFilename = Path.GetFileName(assetFullPath);
                        var hash = ProcessImage(image);

                        if (!Exists(hash, hashes))
                        {
                            counter++;

                            var targetFilename = $"{nextPictureNumber++:0000}.jpg";
                            var targetFullPath = Path.Combine(targetFolder, targetFilename);
                            while (File.Exists(targetFullPath))
                            {
                                targetFilename = $"{nextPictureNumber++:0000}.jpg";
                                targetFullPath = Path.Combine(targetFolder, targetFilename);
                            }

                            WriteLine($"{assetFilename} seems to be new - copying it to {targetFolder} as {targetFilename}");
                            File.Copy(assetFullPath, targetFullPath, false);
                        }
                    }
                }

                WriteLine($"Number of copied images: {counter}");
            }
            else
            {
                WriteLine($"Path \"{assetPath}\" not found");
            }
        }

        private bool Exists(string hashCode, ISet<string> existingHashCodes)
        {
            bool result = false;

            foreach (var hashCodeToCheck in existingHashCodes)
            {
                if (GenerateScore(hashCodeToCheck, hashCode) > 0.95f)
                {
                    result = true;
                    break;
                }
            }

            return result;
        }

        private HashSet<string> ScanTargetFolder(string targetFolder)
        {
            if (string.IsNullOrWhiteSpace(targetFolder))
            {
                throw new ArgumentException("Invalid target folder specified (null, empty, or whitespaces only)");
            }

            var hashes = new HashSet<string>();

            if (Directory.Exists(targetFolder))
            {
                foreach (var fullPath in Directory.GetFiles(targetFolder, "*.jpg"))
                {
                    var image = Image.FromFile(fullPath);
                    if (image.RawFormat.Equals(ImageFormat.Jpeg))
                    {
                        var hash = ProcessImage(image);
                        hashes.Add(hash);
                    }
                }
            }

            return hashes;
        }

        private float GenerateScore(Image original, Image copy)
        {
            var hashCodeOriginal = ProcessImage(original);
            var hashCodeCopy = ProcessImage(copy);

            return GenerateScore(hashCodeOriginal, hashCodeCopy);
        }

        private float GenerateScore(string hash1, string hash2)
        {
            int score = 0;
            for (int i = 0; i < hash1.Length; i++)
            {
                if (hash1[i] != hash2[i])
                {
                    score++;
                }
            }

            return (hash1.Length - score) / (float)hash1.Length;
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
