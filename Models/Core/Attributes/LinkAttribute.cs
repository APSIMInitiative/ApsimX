// -----------------------------------------------------------------------
// <copyright file="LinkAttribute.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace Models.Core
{
    using PMF;
    using Functions;
    using System;
    using System.Reflection;
    using System.Xml.Serialization;

    /// <summary>
    /// Enumeration of all possible types of links.
    /// </summary>
    public enum LinkType
    {
        /// <summary>
        /// A link to the first matching model in scope.
        /// </summary>
        Scoped,

        /// <summary>
        /// A link to a model via an absolute path.
        /// </summary>
        Path,

        /// <summary>
        /// A link to a child model.
        /// </summary>
        Child,

        /// <summary>
        /// A link to an ancestor model.
        /// </summary>
        Ancestor
    }

    /// <summary>
    /// When applied to a field, the infrastructure will locate an object that matches the 
    /// related field and store a reference to it in the field (dependency injection). 
    /// If no matching model is found (and IsOptional is not specified or is false), then an 
    /// exception will be thrown. 
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class LinkAttribute : XmlIgnoreAttribute
    {
        /// <summary>Iff true, an exception will not be thrown if an object cannot be found.</summary>
        public bool IsOptional { get; set; }

        /// <summary>Absolute path to the link target. Only used if Type is set to LinkType.Path.</summary>
        public string Path { get; set; }

        /// <summary>Controls how the link will be resolved. The values are mutually exclusive. Default value is <see cref="LinkType.Scoped"/>.</summary>
        public LinkType Type { get; set; } = LinkType.Scoped;

        /// <summary>Iff true, target model must have the same name as the field/property to which this link is applied. Defaults to true.</summary>
        public bool ByName { get; set; } = true;

        /// <summary>Is this link a scoped link</summary>
        public virtual bool IsScoped(IVariable field)
        {
            if (typeof(IFunction).IsAssignableFrom(field.DataType) ||
                typeof(Biomass).IsAssignableFrom(field.DataType) ||
                field.DataType.Name == "Object")
                return false;
            else
                return Type == LinkType.Scoped;
        }

        /// <summary>Should the fields name be used when matching?</summary>
        public virtual bool UseNameToMatch(IVariable field)
        {
            return ByName;
        }
    }
    
    /// <summary>
    /// When applied to a field, the infrastructure will locate a child object of the 
    /// related fields type and store a reference to it in the field. If no matching
    /// model is found (and IsOptional is not specified or is false), then an 
    /// exception will be thrown. 
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class ChildLinkAttribute : LinkAttribute
    {
        /// <summary>Is this link a scoped link</summary>
        public override bool IsScoped(IVariable fieldInfo) { return false; }

        /// <summary>Should the fields name be used when matching?</summary>
        public override bool UseNameToMatch(IVariable field) { return false; }
    }

    /// <summary>
    /// When applied to a field, the infrastructure will locate a child object of the 
    /// related fields type and name and store a reference to it in the field. If no matching
    /// model is found (and IsOptional is not specified or is false), then an 
    /// exception will be thrown. 
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class ChildLinkByNameAttribute : LinkAttribute
    {
        /// <summary>Is this link a scoped link</summary>
        public override bool IsScoped(IVariable fieldInfo) { return false; }

        /// <summary>Should the fields name be used when matching?</summary>
        public override bool UseNameToMatch(IVariable field) { return true; }
    }
}
