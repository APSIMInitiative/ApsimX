namespace UserInterface.Classes
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Represents all properties of an object, as they are to be displayed
    /// in the UI for editing.
    /// </summary>
    public class PropertyGroup
    {
        /// <summary>
        /// Name of the property group.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Properties belonging to the model.
        /// </summary>
        public IEnumerable<Property> Properties { get; private set; }

        /// <summary>
        /// Properties belonging to properties of the model marked with
        /// DisplayType.SubModel.
        /// </summary>
        public IEnumerable<PropertyGroup> SubModelProperties { get; private set; }

        /// <summary>
        /// Constructs a property group.
        /// </summary>
        /// <param name="name">Name of the property group.</param>
        /// <param name="properties">Properties belonging to the model.</param>
        /// <param name="subProperties">Property properties.</param>
        public PropertyGroup(string name, IEnumerable<Property> properties, IEnumerable<PropertyGroup> subProperties)
        {
            Name = name;
            Properties = properties;
            SubModelProperties = subProperties ?? new PropertyGroup[0];
        }

        /// <summary>
        /// Returns the total number of properties in this property group and sub property groups.
        /// </summary>
        public int Count()
        {
            return Properties.Count() + SubModelProperties?.Sum(p => p.Count()) ?? 0;
        }

        public Property Find(Guid id)
        {
            return GetAllProperties().FirstOrDefault(p => p.ID == id);
        }

        public IEnumerable<Property> GetAllProperties()
        {
            foreach (Property property in Properties)
                yield return property;
            foreach (Property property in SubModelProperties.SelectMany(g => g.GetAllProperties()))
                yield return property;
        }
    }
}
