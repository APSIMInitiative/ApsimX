using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace Models.Core
{

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class Link : XmlIgnoreAttribute
    {
        private string _Path = null;
        private bool _IsOptional = false;

        public string NamePath
        {
            get { return _Path; }
            set { _Path = value; }
        }
        public bool IsOptional
        {
            get { return _IsOptional; }
            set { _IsOptional = value; }
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

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
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

    [AttributeUsage(AttributeTargets.Method)]
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
    public class UserInterfaceIgnore : System.Attribute
    {
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