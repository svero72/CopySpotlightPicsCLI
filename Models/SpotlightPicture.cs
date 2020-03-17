using System;

namespace Svero.CopySpotlightPics.Models
{
    public class SpotlightPicture
    {
        /// <summary>
        /// Gets or sets the path of the picture
        /// </summary>
        public string? Path { get; set; }

        /// <summary>
        /// Gets the code of the picture.
        /// </summary>
        public string PictureCode { get; }

        /// <summary>
        /// Creates a new instance and sets the picture code to the specified value.
        /// </summary>
        /// <param name="pictureCode">Picture code (not null or blank)</param>
        /// <exception cref="ArgumentException">If hash is invalid (null or blank)</exception>
        public SpotlightPicture(string pictureCode)
        {
            if (string.IsNullOrWhiteSpace(pictureCode))
            {
                throw new ArgumentException("Invalid hash code");
            }
            
            PictureCode = pictureCode;
        }

        /// <summary>
        /// Creates a new instance and sets the properties to the specified values.
        /// </summary>
        /// <param name="pictureCode">Code value (not null or blank)</param>
        /// <param name="path">Picture path (maybe null)</param>
        public SpotlightPicture(string pictureCode, string path) : this(pictureCode)
        {
            Path = path?.Trim();
        }

        /// <summary>
        /// Checks if the specified object is equal to this instance.
        /// </summary>
        /// <param name="obj">Object to check with</param>
        /// <returns>True if equal (same picture code), false if not</returns>
        public override bool Equals(object? obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (!(obj is SpotlightPicture other))
            {
                return false;
            }

            return PictureCode.Equals(other.PictureCode);
        }

        /// <summary>
        /// Returns the hash code for this object.
        /// </summary>
        /// <returns>Hash code</returns>
        public override int GetHashCode()
        {
            return PictureCode.GetHashCode();
        }
    }
}