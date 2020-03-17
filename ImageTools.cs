using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using Svero.CopySpotlightPics.Models;

using static System.Console;

namespace Svero.CopySpotlightPics
{
    /// <summary>
    /// Implements several methods for handling images.
    /// </summary>
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public static class ImageTools
    {
        /// <summary>
        /// Checks if a picture with the specified code does exist in the specified collection of pictures. A
        /// picture is assumed to have a similar code if the score is higher then 95%.
        /// </summary>
        /// <param name="pictureCode">Picture code to look up for</param>
        /// <param name="pictures">Set with existing pictures</param>
        /// <returns>True if a similar picture was found, otherwise false</returns>
        public static bool Exists(string pictureCode, IEnumerable<SpotlightPicture> pictures)
        {
            if (string.IsNullOrWhiteSpace(pictureCode))
            {
                throw new ArgumentException("Invalid picture code");
            }

            if (pictures == null)
            {
                throw new ArgumentNullException(nameof(pictures));
            }
            
            return pictures.Any(pictureToCheck => 
                GenerateScore(pictureToCheck.PictureCode, pictureCode) > 0.95f);
        }

        /// <summary>
        /// Tries to find a similar picture in a collection of pictures. A picture is assumed to be similar if the
        /// code score is higher then 95%.
        /// </summary>
        /// <param name="pictureCode">Code of the picture to lookup for (not null or blank).</param>
        /// <param name="pictures">Collection with pictures (not null)</param>
        /// <returns>Picture if found, otherwise null</returns>
        public static SpotlightPicture? SearchByCode(string pictureCode, ISet<SpotlightPicture> pictures)
        {
            if (string.IsNullOrWhiteSpace(pictureCode))
            {
                throw new ArgumentException("Invalid picture code");
            }

            if (pictures == null)
            {
                throw new ArgumentNullException(nameof(pictures));
            }
            
            return pictures.FirstOrDefault(currentPicture => GenerateScore(currentPicture.PictureCode, 
                pictureCode) > 0.95f);
        }

        /// <summary>
        /// Scans the specified path for pictures.
        /// </summary>
        /// <param name="path">Path to scan</param>
        /// <param name="searchPattern">Search pattern (default "*.*")</param>
        /// <returns>Set with found pictures</returns>
        /// <exception cref="ArgumentException">If path is blank or does not exist</exception>
        public static ISet<SpotlightPicture> ScanFolder(string path, string searchPattern = "*.*")
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Invalid target folder specified (null, empty, or whitespaces only)");
            }

            if (!Directory.Exists(path))
            {
                throw new ArgumentException($"The specified folder {path} does not exist");
            }

            if (string.IsNullOrWhiteSpace(searchPattern))
            {
                searchPattern = "*.*";
            }

            var foundPictures = new HashSet<SpotlightPicture>();

            foreach (var pictureFile in Directory.GetFiles(path, searchPattern))
            {
                try
                {
                    var image = Image.FromFile(pictureFile);
                    if (image.RawFormat.Equals(ImageFormat.Jpeg))
                    {
                        var hash = ProcessImage(image);

                        var existingPicture = foundPictures
                            .FirstOrDefault(m => m.PictureCode.Equals(hash));

                        if (existingPicture == null)
                        {
                            foundPictures.Add(new SpotlightPicture(hash, pictureFile));
                        }
                        else
                        {
                            WriteLine($"{existingPicture.Path} has the same hash as {pictureFile}");
                        }
                    }
                }
                catch (OutOfMemoryException ex)
                {
                    WriteLine($"The file {pictureFile} is not a valid image or has an unsupported format: {ex.Message}");
                }
            }

            return foundPictures;
        }

        /// <summary>
        /// Generates the score of similarity between the both specified images.
        /// </summary>
        /// <param name="imageA">Image A</param>
        /// <param name="imageB">Image B</param>
        /// <returns>Similarity score (between 0.0 and 1.0)</returns>
        /// <exception cref="ArgumentNullException">If one of the specified instances is null</exception>
        public static float GenerateScore(Image imageA, Image imageB)
        {
            if (imageA == null)
            {
                throw new ArgumentNullException(nameof(imageA));
            }

            if (imageB == null)
            {
                throw new ArgumentNullException(nameof(imageB));
            }
            
            var hashCodeOriginal = ProcessImage(imageA);
            var hashCodeCopy = ProcessImage(imageB);

            return GenerateScore(hashCodeOriginal, hashCodeCopy);
        }

        /// <summary>
        /// Generates the similarity score between the both picture codes.
        /// </summary>
        /// <param name="pictureCodeA">Picture code A</param>
        /// <param name="pictureCodeB">Picture code B</param>
        /// <returns>Similarity score (between 0.0 and 1.0)</returns>
        /// <exception cref="ArgumentException">If one of the both parameters is invalid (blank)</exception>
        public static float GenerateScore(string pictureCodeA, string pictureCodeB)
        {
            if (string.IsNullOrWhiteSpace(pictureCodeA))
            {
                throw new ArgumentException("Invalid value for picture code A (blank)");
            }

            if (string.IsNullOrWhiteSpace(pictureCodeB))
            {
                throw new ArgumentException("Invalid value for picture code B (blank)");
            }
            
            var score = pictureCodeA.Where((t, i) => t != pictureCodeB[i]).Count();

            return (pictureCodeA.Length - score) / (float)pictureCodeA.Length;
        }

        /// <summary>
        /// Processes the specified image by scaling and converting it. Finally the picture code is generated.
        /// </summary>
        /// <param name="image">Image to process</param>
        /// <returns>Picture code</returns>
        /// <exception cref="ArgumentNullException">If the image is null</exception>
        public static string ProcessImage(Image image)
        {
            if (image == null)
            {
                throw new ArgumentNullException(nameof(image));
            }
            
            var scaled = ScaleTo(image, 9, 9, InterpolationMode.High);
            var monochrome = ToMonochrome(scaled);
            var pictureCode = GeneratePictureCode(monochrome);

            return pictureCode;
        }

        /// <summary>
        /// Scales the specified original picture to the specified value using the specified mode and
        /// returns the result as a new instance.
        /// </summary>
        /// <param name="original">Original image (not null)</param>
        /// <param name="targetWidth">Target width in pixel (>= 1)</param>
        /// <param name="targetHeight">Target height in pixel (>= 1)</param>
        /// <param name="mode">Interpolation mode</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">If the specified original image is null</exception>
        /// <exception cref="ArgumentOutOfRangeException">If the height or width is outside the allowed range)</exception>
        public static Bitmap ScaleTo(Image original, int targetWidth, int targetHeight, InterpolationMode mode)
        {
            if (original == null)
            {
                throw new ArgumentNullException(nameof(original));
            }

            if (targetWidth < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(targetWidth), "Invalid target width specified (less then 1)");
            }

            if (targetHeight < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(targetHeight),"Invalid target height specified (less then 1)");
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

        /// <summary>
        /// Converts the specified image to a monochrome image.
        /// </summary>
        /// <param name="image">Image to convert</param>
        /// <returns>Converted image</returns>
        /// <exception cref="ArgumentNullException">If the image is null</exception>
        public static Bitmap ToMonochrome(Image image)
        {
            if (image == null)
            {
                throw new ArgumentNullException(nameof(image));
            }
            
            var colorMatrix = new ColorMatrix(new[]
            {
                new[] {0.299f, 0.299f, 0.299f, 0.000f, 0.000f},
                new[] {0.587f, 0.587f, 0.587f, 0.000f, 0.000f},
                new[] {0.114f, 0.114f, 0.114f, 0.000f, 0.000f},
                new[] {0.000f, 0.000f, 0.000f, 1.000f, 0.000f},
                new[] {0.000f, 0.000f, 0.000f, 0.000f, 1.000f}
            });

            var attributes = new ImageAttributes();
            attributes.SetColorMatrix(colorMatrix);

            Point[] points =
            {
                new Point(0, 0),
                new Point(image.Width, 0),
                new Point(0, image.Height)
            };

            var rectangle = new Rectangle(0, 0, image.Width, image.Height);

            var result = new Bitmap(image.Width, image.Height);
            using (var graphics = Graphics.FromImage(result))
            {
                graphics.DrawImage(image, points, rectangle, GraphicsUnit.Pixel, attributes);
            }

            return result;
        }
    
        /// <summary>
        /// Generates the picture code. Only the first 9x9 pixel matrix is used to generate the code!
        /// </summary>
        /// <param name="bitmap">Bitmap</param>
        /// <returns>Picture code</returns>
        /// <exception cref="ArgumentNullException">If the specified bitmap is null</exception>
        public static string GeneratePictureCode(Bitmap bitmap)
        {
            if (bitmap == null)
            {
                throw new ArgumentNullException(nameof(bitmap));
            }
            
            var rowHashCode = string.Empty;
            var colHashCode = string.Empty;

            for (var r = 0; r < 8; r++)
            {
                for (var c = 0; c < 8; c++)
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

            for (var c = 0; c < 8; c++)
            {
                for (var r = 0; r < 8; r++)
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