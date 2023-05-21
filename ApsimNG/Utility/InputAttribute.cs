using System;

namespace Utility
{
    [AttributeUsage(AttributeTargets.Property)]
    internal class InputAttribute : Attribute
    {
        public string Name { get; set; }
        public string OnChanged { get; set; }
        public InputAttribute(string name)
        {
            Name = name;
        }
    }
}
