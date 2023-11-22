using System;
using System.Collections.Generic;
using Models.CLEM.Interfaces;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// List of attributes for a resource
    /// </summary>
    [Serializable]
    public class IndividualAttributeList
    {
        /// <summary>
        /// List of individual attributes
        /// </summary>
        private Dictionary<string, IIndividualAttribute> attributes { get; set; }

        /// <summary>
        /// The list of available attributes for the individual in a list
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, IIndividualAttribute> Items
        {
            get
            {
                if (attributes is null)
                    attributes = new Dictionary<string, IIndividualAttribute>();
                return attributes;
            }
        }

        /// <summary>
        /// Check if the individual has any attributes added
        /// </summary>
        /// <returns></returns>
        public bool AttributesPresent
        {
            get { return (attributes != null); }
        }

        /// <summary>
        /// Check if the selected attribute exists on this individual
        /// </summary>
        /// <param name="tag">Attribute label</param>
        /// <returns></returns>
        public bool Exists(string tag)
        {
            return (attributes is null) ? false : attributes.ContainsKey(tag);
        }

        /// <summary>
        /// Add an attribute to this individual
        /// </summary>
        /// <param name="tag">Attribute label</param>
        /// <param name="value">Value to set or change</param>
        public void Add(string tag, IIndividualAttribute value = null)
        {
            if (attributes is null)
                attributes = new Dictionary<string, IIndividualAttribute>();

            if (!attributes.ContainsKey(tag))
                attributes.Add(tag, value);
            else
                attributes[tag] = value;
        }

        /// <summary>
        /// Return the value of the selected attribute on this individual else null if not provided
        /// </summary>
        /// <param name="tag">Attribute label</param>
        /// <returns>Value of attribute if found</returns>
        public IIndividualAttribute GetValue(string tag)
        {
            if (attributes is null || !attributes.ContainsKey(tag))
                return null;
            else
                return attributes[tag];
        }

        /// <summary>
        /// Remove the attribute from this individual
        /// </summary>
        /// <param name="tag">Attribute label</param>
        public void Remove(string tag)
        {
            if (attributes != null && attributes.ContainsKey(tag))
                attributes.Remove(tag);
        }

    }
}
