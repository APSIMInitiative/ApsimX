using System;
namespace Models.Plant
{

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]

    public class Comment : Attribute
    {
        public double MaxVal;
        public double MinVal;
        public string Name;
        public string units = "";

        public Comment()
        {
        }

        public Comment(string Name)
        {
            this.Name = Name;
        }

        public Comment(string Name, string units)
        {
            this.Name = Name;
            this.units = units;
        }
    }
}
