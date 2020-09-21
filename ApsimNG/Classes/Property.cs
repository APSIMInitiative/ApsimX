namespace UserInterface.Classes
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using APSIM.Shared.Utilities;
    using Models.Core;

    /// <summary>
    /// Represents a property which can be displayed/edited by the user.
    /// </summary>
    public class Property
    {
        /// <summary>
        /// A unique ID.
        /// </summary>
        public Guid ID { get; private set; }

        /// <summary>
        /// Name of the property, as it appears in the source code.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The type of the property. The determines the sort of input
        /// widget created by the view.
        /// </summary>
        /// <value></value>
        public Type DataType { get; private set; }

        /// <summary>
        /// Name of the property, as it will be displayed to the user.
        /// Often this is the same as <see cref="Name" />, with spaces
        /// - e.g. "Pascal Case" instead of "PascalCase".
        /// </summary>
        public string DisplayName { get; private set; }

        /// <summary>
        /// A tooltip which provides more detailed information about the
        /// property.
        /// </summary>
        public string Tooltip { get; private set; }

        /// <summary>
        /// Value of the property.
        /// </summary>
        public object Value { get; private set; }

        /// <summary>
        /// Determines how the property will be editable. E.g. via a drop-
        /// down, checkbox, file selector, colour, etc.
        /// </summary>
        public DisplayType DisplayMethod { get; private set; }

        /// <summary>
        /// Options to be shown in a dropdown. This will always be null
        /// unless <see cref="DisplayMethod" /> is set to DisplayType.DropDown.
        /// </summary>
        public string[] DropDownOptions { get; private set; }

        /// <summary>
        /// Separators to be shown above the property. May be null.
        /// </summary>
        public List<string> Separators { get; private set; }

        /// <summary>
        /// Instantiates a Property object by reading metadata about
        /// the given property.
        /// </summary>
        /// <param name="obj">The object reference used to fetch the current value of the property.</param>
        /// <param name="metadata">Property metadata.</param>
        public Property(object obj, PropertyInfo metadata)
        {
            ID = Guid.NewGuid();
            Name = metadata.Name;

            DisplayName = metadata.GetCustomAttribute<DescriptionAttribute>().ToString();
            Tooltip = metadata.GetCustomAttribute<TooltipAttribute>()?.Tooltip;

            DisplayMethod = metadata.GetCustomAttribute<DisplayAttribute>()?.Type ?? DisplayType.None;
            if (DisplayMethod == DisplayType.DropDown)
            {
                string methodName = metadata.GetCustomAttribute<DisplayAttribute>().Values;
                MethodInfo method = obj.GetType().GetMethod(methodName);
                DropDownOptions = ((IEnumerable<object>)method.Invoke(obj, null))?.Select(v => v?.ToString())?.ToArray();
            }

            Value = metadata.GetValue(obj);
            DataType = metadata.PropertyType;
            Separators = metadata.GetCustomAttributes<SeparatorAttribute>()?.Select(s => s.ToString())?.ToList();

            if (metadata.PropertyType.IsEnum)
            {
                DisplayMethod = DisplayType.DropDown;
                DropDownOptions = Enum.GetValues(metadata.PropertyType).Cast<Enum>()
                                      .Select(e => VariableProperty.GetEnumDescription(e))
                                      .ToArray();
            }
        }
    }
}
