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
        /// Gets the picture hash of the picture. This is NOT the same value as the .NET hash code.
        /// </summary>
        public string Hash { get; }

        /// <summary>
        /// Creates a new instance and sets the picture hash to the specified value.
        /// </summary>
        /// <param name="hash">Picture hash value (not null or blank)</param>
        /// <exception cref="ArgumentException">If hash is invalid (null or blank)</exception>
        public SpotlightPicture(string hash)
        {
            if (string.IsNullOrWhiteSpace(hash))
            {
                throw new ArgumentException("Invalid hash code");
            }
            
            Hash = hash;
        }

        /// <summary>
        /// Creates a new instance and sets the properties to the specified values.
        /// </summary>
        /// <param name="hash">Hash value (not null or blank)</param>
        /// <param name="path">Picture path (maybe null)</param>
        public SpotlightPicture(string hash, string path) : this(hash)
        {
            Path = path?.Trim();
        }

        /// <summary>
        /// Checks if the specified object is equal to this instance.
        /// </summary>
        /// <param name="obj">Object to check with</param>
        /// <returns>True if equal (same hash code), false if not</returns>
        public override bool Equals(object? obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (object.ReferenceEquals(this, obj))
            {
                return true;
            }

            if (!(obj is SpotlightPicture other))
            {
                return false;
            }

            return Hash.Equals(other.Hash);
        }

        /// <summary>
        /// Returns the hash code for this object.
        /// </summary>
        /// <returns>Hash code</returns>
        public override int GetHashCode()
        {
            return Hash.GetHashCode();
        }
    }
}