using System.Reflection;
using Models.Core;

namespace UserInterface.Classes
{
    /// <summary>
    /// Stores a property and the object to which it belongs.
    /// </summary>
    public class PropertyObjectPair
    {
        public object Model { get; set; }
        public PropertyInfo Property { get; set; }
        public CategoryAttribute Category { get; set; }

        public PropertyObjectPair()
        {
            Model = null;
            Property = null;
            Category = null;
        }
    }
}