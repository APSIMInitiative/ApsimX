namespace UserInterface.Classes
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using APSIM.Shared.Extensions.Collections;
    using APSIM.Shared.Utilities;
    using Models.Core;
    using Models.LifeCycle;
    using Models.Storage;
    using Models.Surface;

    public enum PropertyType
    {
        SingleLineText,
        MultiLineText,
        DropDown,
        Checkbox,
        Colour,
        File,
        Files,
        Directory,
        //Directories,
        Font,
        Numeric
    }

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

    /// <summary>
    /// Represents a property which can be displayed/edited by the user.
    /// </summary>
    /// <remarks>
    /// todo : convert DisplayType to a class with no constructor but a bunch of static instances?
    /// </remarks>
    public class Property
    {
        /// <summary>
        /// A unique ID.
        /// </summary>
        public Guid ID { get; private set; }

        /// <summary>
        /// Name of the property, as it will be displayed to the user.
        /// Often this is the same as property name in the source code,
        /// but not always - e.g. "Pascal Case" instead of "PascalCase".
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// A tooltip which provides more detailed information about the
        /// property.
        /// </summary>
        public string Tooltip { get; private set; }

        /// <summary>
        /// Separators to be shown above the property. May be null.
        /// </summary>
        public List<string> Separators { get; private set; }

        /// <summary>
        /// Value of the property.
        /// </summary>
        public object Value { get; private set; }

        /// <summary>
        /// This determines how the property is editable by the user.
        /// </summary>
        public PropertyType DisplayMethod { get; private set; }

        /// <summary>
        /// Options to be shown in a dropdown. This will always be null
        /// unless <see cref="DisplayMethod" /> is set to PropertyType.DropDown.
        /// </summary>
        public string[] DropDownOptions { get; private set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        public Property(string name, string tooltip, object value, PropertyType displayType, IEnumerable<string> dropDownOptions = null, IEnumerable<string> separators = null)
        {
            ID = Guid.NewGuid();
            Name = name;
            Tooltip = tooltip;
            Value = value;
            DisplayMethod = displayType;
            DropDownOptions = dropDownOptions?.ToArray();
            Separators = separators?.ToList();
        }

        /// <summary>
        /// Instantiates a Property object by reading metadata about
        /// the given property.
        /// </summary>
        /// <param name="obj">The object reference used to fetch the current value of the property.</param>
        /// <param name="metadata">Property metadata.</param>
        public Property(object obj, PropertyInfo metadata)
        {
            IModel model = obj as IModel;
            ID = Guid.NewGuid();
            Name = metadata.GetCustomAttribute<DescriptionAttribute>()?.ToString();
            if (string.IsNullOrEmpty(Name))
                Name = metadata.Name;

            Tooltip = metadata.GetCustomAttribute<TooltipAttribute>()?.Tooltip;
            Separators = metadata.GetCustomAttributes<SeparatorAttribute>()?.Select(s => s.ToString())?.ToList();

            Value = metadata.GetValue(obj);
            if (metadata.PropertyType == typeof(DateTime) || (metadata.PropertyType == typeof(DateTime?) && Value != null))
                // Note: ToShortDateString() uses the current culture, which is what we want in this case.
                Value = ((DateTime)Value).ToShortDateString();
            // ?else if property type isn't a struct?
            else if (Value != null && typeof(IModel).IsAssignableFrom(Value.GetType()))
                Value = ((IModel)Value).Name;
            else if (metadata.PropertyType.IsEnum)
                Value = VariableProperty.GetEnumDescription((Enum)Enum.Parse(metadata.PropertyType, Value?.ToString()));
            else if (metadata.PropertyType != typeof(bool) && metadata.PropertyType != typeof(System.Drawing.Color))
                Value = ReflectionUtilities.ObjectToString(Value, CultureInfo.CurrentCulture);

            // fixme - need to fix this unmaintainable mess brought across from the old PropertyPresenter
            DisplayAttribute attrib = metadata.GetCustomAttribute<DisplayAttribute>();
            DisplayType displayType = attrib?.Type ?? DisplayType.None;

            // For compatibility with the old PropertyPresenter, assume a default of
            // DisplayType.DropDown if the Values property is specified.
            if (displayType == DisplayType.None && !string.IsNullOrEmpty(attrib?.Values))
                displayType = DisplayType.DropDown;

            switch (displayType)
            {
                case DisplayType.None:
                    if (metadata.PropertyType.IsEnum)
                    {
                        // Enums use dropdown
                        DropDownOptions = Enum.GetValues(metadata.PropertyType).Cast<Enum>()
                                      .Select(e => VariableProperty.GetEnumDescription(e))
                                      .ToArray();
                        DisplayMethod = PropertyType.DropDown;
                    }
                    else if (typeof(IModel).IsAssignableFrom(metadata.PropertyType))
                    {
                        // Model selector - use a dropdown containing names of all models in scope.
                        DisplayMethod = PropertyType.DropDown;
                        DropDownOptions = model.FindAllInScope()
                                               .Where(m => metadata.PropertyType.IsAssignableFrom(m.GetType()))
                                               .Select(m => m.Name)
                                               .ToArray();

                    }
                    else if (metadata.PropertyType == typeof(bool))
                        DisplayMethod = PropertyType.Checkbox;
                    else if (metadata.PropertyType == typeof(System.Drawing.Color))
                        DisplayMethod = PropertyType.Colour;
                    else
                        DisplayMethod = PropertyType.SingleLineText;
                    break;
                case DisplayType.FileName:
                    DisplayMethod = PropertyType.File;
                    break;
                case DisplayType.FileNames:
                    DisplayMethod = PropertyType.Files;
                    break;
                case DisplayType.DirectoryName:
                    DisplayMethod = PropertyType.Directory;
                    break;
                case DisplayType.DropDown:
                    string methodName = metadata.GetCustomAttribute<DisplayAttribute>().Values;
                    if (methodName == null)
                        throw new ArgumentNullException($"When using DisplayType.DropDown, the Values property must be specified.");
                    BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy;
                    MethodInfo method = model.GetType().GetMethod(methodName, flags);

                    // Attempt to resolve links - populating the dropdown may
                    // require access to linked models.
                    Simulations sims = model.FindAncestor<Simulations>();
                    if (sims != null)
                        sims.Links.Resolve(model, allLinks: true, throwOnFail: false);

                    DropDownOptions = ((IEnumerable<object>)method.Invoke(model, null))?.Select(v => v?.ToString())?.ToArray();
                    DisplayMethod = PropertyType.DropDown;
                    break;
                case DisplayType.CultivarName:
                    DisplayMethod = PropertyType.DropDown;
                    IPlant plant = null;
                    PropertyInfo plantProperty = model.GetType().GetProperties().FirstOrDefault(p => typeof(IPlant).IsAssignableFrom(p.PropertyType));
                    if (plantProperty != null)
                        plant = plantProperty.GetValue(model) as IPlant;
                    else
                        plant = model.FindInScope<IPlant>();
                    if (plant != null)
                        DropDownOptions = PropertyPresenterHelpers.GetCultivarNames(plant);
                    break;
                case DisplayType.TableName:
                    DisplayMethod = PropertyType.DropDown;
                    DropDownOptions = model.FindInScope<IDataStore>()?.Reader?.TableNames?.ToArray();
                    break;
                case DisplayType.FieldName:
                    DisplayMethod = PropertyType.DropDown;
                    IDataStore storage = model.FindInScope<IDataStore>();
                    PropertyInfo tableNameProperty = model.GetType().GetProperties().FirstOrDefault(p => p.GetCustomAttribute<DisplayAttribute>()?.Type == DisplayType.TableName);
                    string tableName = tableNameProperty?.GetValue(model) as string;
                    if (storage != null && storage.Reader.TableNames.Contains(tableName))
                        DropDownOptions = storage.Reader.ColumnNames(tableName).ToArray();
                    break;
                case DisplayType.LifeCycleName:
                    DisplayMethod = PropertyType.DropDown;
                    Zone zone = model.FindInScope<Zone>();
                    if (zone != null)
                        DropDownOptions = PropertyPresenterHelpers.GetLifeCycleNames(zone);
                    break;
                case DisplayType.LifePhaseName:
                    DisplayMethod = PropertyType.DropDown;
                    LifeCycle lifeCycle = null;
                    if (attrib.LifeCycleName != null)
                        lifeCycle = model.FindInScope<LifeCycle>(attrib.LifeCycleName);
                    else
                    {
                        foreach (PropertyInfo property in model.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
                        {
                            if (property.PropertyType == typeof(string))
                            {
                                string value = property.GetValue(model) as string;
                                LifeCycle match = model.FindInScope<LifeCycle>(value);
                                if (match != null)
                                {
                                    lifeCycle = match;
                                    break;
                                }
                            }
                        }
                    }
                    if (lifeCycle != null)
                        DropDownOptions = PropertyPresenterHelpers.GetPhaseNames(lifeCycle).ToArray();
                    break;
                case DisplayType.Model:
                    DisplayMethod = PropertyType.DropDown;
                    DropDownOptions = model.FindAllInScope().Where(m => metadata.PropertyType.IsAssignableFrom(m.GetType()))
                                           .Select(m => m.Name)
                                           .ToArray();
                    break;
                case DisplayType.ResidueName:
                    if (model is SurfaceOrganicMatter surfaceOM)
                    {
                        DisplayMethod = PropertyType.DropDown;
                        DropDownOptions = surfaceOM.ResidueTypeNames().ToArray();
                        break;
                    }
                    else
                        throw new NotImplementedException($"Display type {displayType} is only supported on models of type {typeof(SurfaceOrganicMatter).Name}, but model is of type {model.GetType().Name}.");
                case DisplayType.MultiLineText:
                    DisplayMethod = PropertyType.MultiLineText;
                    if (Value is IEnumerable enumerable && metadata.PropertyType != typeof(string))
                        Value = string.Join(Environment.NewLine, ((IEnumerable)metadata.GetValue(obj)).ToGenericEnumerable());
                    break;
                // Should never happen - presenter should handle this(?)
                //case DisplayType.SubModel:
                default:
                    throw new NotImplementedException($"Unknown display type {displayType}");
            }

            // If the list of dropdown options doesn't contain the actual value of the
            // property, add that value to the list of valid options.
            if (DisplayMethod == PropertyType.DropDown && Value != null)
            {
                if (DropDownOptions == null)
                    DropDownOptions = new string[1] { Value.ToString() };
                else if (!DropDownOptions.Contains(Value.ToString()))
                {
                    List<string> values = DropDownOptions.ToList();
                    values.Add(Value.ToString());
                    DropDownOptions = values.ToArray();
                }
            }
        }
    }
}
