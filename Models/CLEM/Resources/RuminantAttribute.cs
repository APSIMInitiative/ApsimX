using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// Interface for all resource attributes
    /// </summary>
    public interface IRuminantAttribute
    {
        /// <summary>
        /// The value of the attrubute
        /// </summary>
        object storedValue { get; set; }

        /// <summary>
        /// The style for inheritance of attribute
        /// </summary>
        RuminantAttributeInheritanceStyle InheritanceStyle { get; set; }
    }


    /// <summary>
    /// A ruminant attribute that stores an associated object
    /// </summary>
    public class RuminantAttribute: IRuminantAttribute
    {
        /// <summary>
        /// Value object of attribute
        /// </summary>
        public object storedValue { get; set; }

        /// <summary>
        /// The style of inheritance of the attribute
        /// </summary>
        public RuminantAttributeInheritanceStyle InheritanceStyle { get; set; }

        /// <summary>
        /// Value as a float
        /// </summary>
        public float Value
        {
            get
            {
                if(float.TryParse(storedValue.ToString(), out float val))
                {
                    return val;
                }
                else
                {
                    return 0;
                }
            }
        }
    }

    /// <summary>
    /// A ruminant attribute that stores an associated geneotype
    /// </summary>
    public class RuminantGenotypeAttribute : IRuminantAttribute
    {
        /// <summary>
        /// Value object of attribute
        /// </summary>
        public object storedValue { get; set; }

        /// <summary>
        /// The style of inheritance of the attribute
        /// </summary>
        public RuminantAttributeInheritanceStyle InheritanceStyle { get; set; }

        /// <summary>
        /// Value as a string (e.g Bb)
        /// </summary>
        public string Value
        {
            get
            {
                return storedValue.ToString();
            }
        }

    }

}
