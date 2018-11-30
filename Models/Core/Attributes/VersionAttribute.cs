using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Core.Attributes
{
    /// <summary>
    /// Model version attribute
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = true)]
    public class VersionAttribute : Attribute
    {
        private string author;
        private string company;
        private int major;
        private int minor;
        private int increment;
        private string comments;

        /// <summary>
        /// Attribute constructor
        /// </summary>
        /// <param name="Major">Version major component</param>
        /// <param name="Minor">Version minor component</param>
        /// <param name="Increment">Minor increment</param>
        /// <param name="Author">Author of model</param>
        /// <param name="Company">Company of author</param>
        /// <param name="Comments">Version comments</param>
        public VersionAttribute(int Major, int Minor, int Increment, string Author, string Company, string Comments)
        {
            author = Author;
            company = Company;
            comments = Comments;
            major = Major;
            minor = Minor;
            increment = Increment;
        }

        /// <summary>
        /// Get this version author
        /// </summary>
        /// <returns>Author name</returns>
        public string Author
        {
            get { return author; }
        }

        /// <summary>
        /// Get the company of the author of this version author
        /// </summary>
        /// <returns>Company name</returns>
        public string Company
        {
            get { return company; }
        }

        ///// <summary>
        ///// Get the version number
        ///// </summary>
        ///// <returns>Version number</returns>
        //public decimal Version
        //{
        //    get { return Convert.ToDecimal(major) + Convert.ToDecimal(Convert.ToDouble(minor)/Math.Pow(10,(((Convert.ToDecimal(minor) > 0) ? Convert.ToInt16(Math.Log10(Convert.ToDouble(minor))) + 1 : 1))))  ;}
        //}

        /// <summary>
        /// Gets the uri
        /// </summary>
        /// <returns>help link uri</returns>
        public override string ToString()
        {
            return major.ToString() + "." + minor.ToString() + "." + increment.ToString();
        }

        /// <summary>
        /// Get the comments associated with this version
        /// </summary>
        /// <returns>Comments</returns>
        public string Comments()
        {
            return comments;
        }

    }
}
