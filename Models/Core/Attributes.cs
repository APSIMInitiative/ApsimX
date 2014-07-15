//-----------------------------------------------------------------------
// <copyright file="Attributes.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models.Core
{
    using System;
    using System.Xml.Serialization;

    /// <summary>
    /// When applied to a field, ApsimX will locate an object in scope of the 
    /// specified type and store a reference to it in the field. Will throw an 
    /// exception if not found. 
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class Link : XmlIgnoreAttribute
    {
        /// <summary>
        /// Stores the optionally specified name path
        /// </summary>
        private string _Path = null;

        /// <summary>
        /// Stores the value of IsOptional if specified.
        /// </summary>
        private bool _IsOptional = false;

        /// <summary>
        /// Indicates that the link can only be a child.
        /// </summary>
        private bool _MustBeChild = false;

        /// <summary>
        /// Gets or sets the NamePath of a field. When specified, ApsimX will locate 
        /// the object using this name and store a reference to it in the field. NamePath 
        /// must conform to the specification in Section 2.5. Will throw an exception if the 
        /// NamePath isn’t valid.
        /// </summary>
        public string NamePath
        {
            get { return _Path; }
            set { _Path = value; }
        }

        /// <summary>
        /// When IsOptional = true, ApsimX will not throw an exception when an object cannot be found.
        /// </summary>
        public bool IsOptional
        {
            get { return _IsOptional; }
            set { _IsOptional = value; }
        }

        /// <summary>
        /// When MustBeChild is 
        /// </summary>
        public bool MustBeChild
        {
            get { return _MustBeChild; }
            set { _MustBeChild = value; }
        }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class Units : System.Attribute
    {
        public string UnitsString;
        public Units(string text)
        {
            UnitsString = text;
        }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class Bounds : System.Attribute
    {
        public double Lower;
        public double Upper;

    }

    [AttributeUsage(AttributeTargets.Method)]
    public class EventSubscribe : System.Attribute
    {
        public string Name;
        public EventSubscribe(string name)
        {
            Name = name;
        }
    }


    [AttributeUsage(AttributeTargets.Class)]
    public class PresenterName : Attribute
    {
        public string Name { get; set; }
        public PresenterName(string Name) { this.Name = Name; }
        public override string ToString() { return Name; }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class ViewName : Attribute
    {
        public string Name { get; set; }
        public ViewName(string Name) { this.Name = Name; }
        public override string ToString() { return Name; }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Class)]
    public class Description : System.Attribute
    {
        private string Desc;
        public Description(string Description) { Desc = Description; }
        public override string ToString() { return Desc; }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class MainMenuName : System.Attribute
    {
        public string MenuName { get; set; }
        public MainMenuName(string MenuName) { this.MenuName = MenuName; }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class ContextMenuName : System.Attribute
    {
        public string MenuName { get; set; }
        public ContextMenuName(string MenuName) { this.MenuName = MenuName; }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class ContextModelType : System.Attribute
    {
        public Type ModelType { get; set; }
        public ContextModelType(Type ModelType) { this.ModelType = ModelType; }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class AllowDropOn : System.Attribute
    {
        public string ModelTypeName { get; set; }
        public AllowDropOn(string ModelTypeName) { this.ModelTypeName = ModelTypeName; }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class DisplayFormat : System.Attribute
    {
        public string Format;

        public DisplayFormat(string format)
        {
            Format = format;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class DisplayTotal : System.Attribute
    {
    }
}