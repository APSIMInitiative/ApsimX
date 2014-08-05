// -----------------------------------------------------------------------
// <copyright file="LinkAttribute.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace Models.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Xml.Serialization;

    /// <summary>
    /// When applied to a field, the infrastructure will locate an object in scope of the 
    /// related field and store a reference to it in the field. If no matching
    /// model is found (and IsOptional is not specified or is false), then an 
    /// exception will be thrown. 
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class LinkAttribute : XmlIgnoreAttribute
    {
        /// <summary>
        /// Stores the optionally specified name path
        /// </summary>
        private string path = null;

        /// <summary>
        /// Stores the value of IsOptional if specified.
        /// </summary>
        private bool isOptional = false;

        /// <summary>
        /// Gets or sets the NamePath of a field. When specified, the infrastructure will locate 
        /// the object using this name and store a reference to it in the field. NamePath 
        /// must conform to the specification in Section 2.5. Will throw an exception if the 
        /// NamePath isn’t valid.
        /// </summary>
        public string NamePath
        {
            get { return this.path; }
            set { this.path = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the link is optional or not. When true, the infrastructure will not throw an exception when an object cannot be found.
        /// </summary>
        public bool IsOptional
        {
            get { return this.isOptional; }
            set { this.isOptional = value; }
        }
    }
}
