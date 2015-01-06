using System;
namespace Models.PMF
{

    /// <summary>
    /// A comment attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]

    [Serializable]
    public class Comment : Attribute
    {
        /// <summary>The maximum value</summary>
        public double MaxVal;
        /// <summary>The minimum value</summary>
        public double MinVal;
        /// <summary>The name</summary>
        public string Name;
        /// <summary>The units</summary>
        public string units = "";

        /// <summary>Initializes a new instance of the <see cref="Comment"/> class.</summary>
        public Comment()
        {
        }

        /// <summary>Initializes a new instance of the <see cref="Comment"/> class.</summary>
        /// <param name="Name">The name.</param>
        public Comment(string Name)
        {
            this.Name = Name;
        }

        /// <summary>Initializes a new instance of the <see cref="Comment"/> class.</summary>
        /// <param name="Name">The name.</param>
        /// <param name="units">The units.</param>
        public Comment(string Name, string units)
        {
            this.Name = Name;
            this.units = units;
        }
    }
}
