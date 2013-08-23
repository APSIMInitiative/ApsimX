using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace Model.Core
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
        public Units(string text)
        {
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

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class Description : System.Attribute
    {
        private string Desc;
        public Description(string Description) { Desc = Description; }
        public override string ToString() { return Desc; }
    }
}