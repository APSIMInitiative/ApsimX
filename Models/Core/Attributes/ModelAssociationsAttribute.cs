using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Core.Attributes
{
    /// <summary>
    /// The style of assocation with the requesting model.
    /// </summary>
    public enum ModelAssociationStyle
    {
        /// <summary>
        /// Model must be in scope
        /// </summary>
        InScope,
        /// <summary>
        /// Model must be a descendant
        /// </summary>
        Descendent,
        /// <summary>
        /// Model must be a child
        /// </summary>
        Child,
        /// <summary>
        /// Model must be a descendent of all ruminant types
        /// </summary>
        DescendentOfRuminantType,
    }


    /// <summary>
    /// Attribute to hold lists of required models for validation
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ModelAssociationsAttribute : System.Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ModelAssociationsAttribute" /> class.
        /// </summary>
        /// <param name="associatedModels">A list of required models</param>
        /// <param name="associationStyles">A list of AssociationStyle matched to the models</param>
        /// <param name="singleInstance">Determines if this model can only have a single instance</param>
        public ModelAssociationsAttribute(Type[] associatedModels = null, ModelAssociationStyle[] associationStyles = null, bool singleInstance = false)
        {
            this.AssociatedModels = associatedModels;
            this.AssociationStyles = associationStyles;
            this.SingleInstance = singleInstance;
        }

        /// <summary>
        /// Array of required model types and their association style
        /// </summary>
        public Type[] AssociatedModels { get; set; }

        /// <summary>
        /// Array of association styles linked to the models
        /// </summary>
        public ModelAssociationStyle[] AssociationStyles { get; set; }

        /// <summary>
        /// Determines if this model can only have a single instance in scope
        /// </summary>
        public bool SingleInstance { get; set; } = false;

    }
}
