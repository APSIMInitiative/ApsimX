using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Reflection;
using APSIM.Shared.Extensions.Collections;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.LifeCycle;
using Models.PMF;
using Models.Storage;
using Models.Surface;
namespace UserInterface.Classes
{
    public enum PropertyType
    {
        SingleLineText,
        MultiLineText,
        DropDown,
        Checkbox,
        Colour,
        ColourPicker,
        File,
        Files,
        Directory,
        //Directories,
        Font,
        Numeric,
        Code
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
        /// If false, the widget shown in the GUI for this property will be disabled.
        /// </summary>
        public bool Enabled { get; private set; }

        /// <summary>
        /// If false, the widget will not be shown in the GUI for this property.
        /// </summary>
        public bool Visible { get; private set; }

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
            Enabled = true;
            Visible = true;
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
            string units = metadata.GetCustomAttribute<UnitsAttribute>()?.ToString();
            if (!string.IsNullOrEmpty(units))
            {
                units = "(" + units + ")";
                if (!Name.Contains(units))
                    Name += " " + units;
            }

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

            if (attrib != null && !string.IsNullOrEmpty(attrib.VisibleCallback))
            {
                BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;
                MethodInfo method = metadata.DeclaringType.GetMethod(attrib.VisibleCallback, flags);
                if (method == null)
                {
                    // Try a property with this name.
                    PropertyInfo visibleProperty = metadata.DeclaringType.GetProperty(attrib.VisibleCallback, flags);
                    if (visibleProperty == null)
                        throw new InvalidOperationException($"Unable to evaluate visible callback {attrib.VisibleCallback} for property {metadata.Name} on type {metadata.DeclaringType.FullName} - method or property does not exist");

                    if (visibleProperty.PropertyType != typeof(bool))
                        throw new InvalidOperationException($"Property {visibleProperty.Name} is not a valid enabled callback, because it has a return type of {visibleProperty.PropertyType}. It should have a bool return type.");
                    if (!visibleProperty.CanRead)
                        throw new InvalidOperationException($"Property {visibleProperty.Name} is not a valid enabled callback, because it does not have a get accessor.");

                    Visible = (bool)visibleProperty.GetValue(obj);
                }
                else
                {
                    if (method.ReturnType != typeof(bool))
                        throw new InvalidOperationException($"Method {metadata.Name} is not a valid enabled callback, because it has a return type of {method.ReturnType}. It should have a bool return type.");
                    ParameterInfo[] parameters = method.GetParameters();
                    List<ParameterInfo> nonOptionalParameters = parameters.Where(p => !p.IsOptional).ToList();
                    if (nonOptionalParameters.Count != 0)
                        throw new InvalidOperationException($"Method {metadata.Name} is not a valid enabled callback, because it takes {nonOptionalParameters.Count} non-optional arguments ({string.Join(", ", nonOptionalParameters.Select(p => p.Name))}). It should take 0 arguments");

                    Visible = (bool)method.Invoke(obj, null);
                }
            }
            else
                Visible = true;

            if (attrib != null && !string.IsNullOrEmpty(attrib.EnabledCallback))
            {
                BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;
                MethodInfo method = metadata.DeclaringType.GetMethod(attrib.EnabledCallback, flags);
                if (method == null)
                {
                    // Try a property with this name.
                    PropertyInfo enabledProperty = metadata.DeclaringType.GetProperty(attrib.EnabledCallback, flags);
                    if (enabledProperty == null)
                        throw new InvalidOperationException($"Unable to evaluate enabled callback {attrib.EnabledCallback} for property {metadata.Name} on type {metadata.DeclaringType.FullName} - method or property does not exist");

                    if (enabledProperty.PropertyType != typeof(bool))
                        throw new InvalidOperationException($"Property {enabledProperty.Name} is not a valid enabled callback, because it has a return type of {enabledProperty.PropertyType}. It should have a bool return type.");
                    if (!enabledProperty.CanRead)
                        throw new InvalidOperationException($"Property {enabledProperty.Name} is not a valid enabled callback, because it does not have a get accessor.");

                    Enabled = (bool)enabledProperty.GetValue(obj);
                }
                else
                {
                    if (method.ReturnType != typeof(bool))
                        throw new InvalidOperationException($"Method {metadata.Name} is not a valid enabled callback, because it has a return type of {method.ReturnType}. It should have a bool return type.");
                    ParameterInfo[] parameters = method.GetParameters();
                    List<ParameterInfo> nonOptionalParameters = parameters.Where(p => !p.IsOptional).ToList();
                    if (nonOptionalParameters.Count != 0)
                        throw new InvalidOperationException($"Method {metadata.Name} is not a valid enabled callback, because it takes {nonOptionalParameters.Count} non-optional arguments ({string.Join(", ", nonOptionalParameters.Select(p => p.Name))}). It should take 0 arguments");

                    Enabled = (bool)method.Invoke(obj, null);
                }
            }
            else
                Enabled = true;

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
                case DisplayType.Code:
                    DisplayMethod = PropertyType.Code;
                    break;
                case DisplayType.ColourPicker:
                    DisplayMethod = PropertyType.ColourPicker;
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

                    object[] args = metadata.GetCustomAttribute<DisplayAttribute>().ValuesArgs;

                    // Attempt to resolve links - populating the dropdown may
                    // require access to linked models.
                    Simulations sims = model.FindAncestor<Simulations>();
                    if (sims != null)
                        sims.Links.Resolve(model, allLinks: true, throwOnFail: false);

                    DropDownOptions = ((IEnumerable<object>)method.Invoke(model, args))?.Select(v => v?.ToString())?.ToArray();
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
                    else
                        DropDownOptions = new string[] { };
                    break;
                case DisplayType.SCRUMcropName:
                    DisplayMethod = PropertyType.DropDown;
                    Zone zoney = model.FindInScope<Zone>();
                    if (zoney != null)
                        DropDownOptions = PropertyPresenterHelpers.GetSCRUMcropNames(zoney);
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
                case DisplayType.CropStageName:
                    DisplayMethod = PropertyType.DropDown;
                    Plant planty = model.FindInScope<Plant>();
                    if (planty != null)
                        DropDownOptions = PropertyPresenterHelpers.GetCropStageNames(planty);
                    break;
                case DisplayType.CSVCrops:
                    DisplayMethod = PropertyType.DropDown;
                    PropertyInfo namesPropInfo = model.GetType().GetProperty("CropNames");
                    string[] names = namesPropInfo?.GetValue(model) as string[] ;
                    if (names != null)
                        DropDownOptions = names;
                    break;
                case DisplayType.CropPhaseName:
                    DisplayMethod = PropertyType.DropDown;
                    Plant plantyy = model.FindInScope<Plant>();
                    if (plantyy != null)
                        DropDownOptions = PropertyPresenterHelpers.GetCropPhaseNames(plantyy);
                    break;
                case DisplayType.PlantOrganList:
                    DisplayMethod = PropertyType.DropDown;
                    Zone zone1 = model.FindAncestor<Zone>();
                    List<Plant> plants = zone1.FindAllChildren<Plant>().ToList();
                    if (plants != null)
                        DropDownOptions = PropertyPresenterHelpers.GetPlantOrgans(plants);
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
                case DisplayType.ScrumEstablishStages:
                    DisplayMethod = PropertyType.DropDown;
                    DropDownOptions = new string[3] { "Seed", "Emergence", "Seedling" };
                    break;
                case DisplayType.ScrumHarvestStages: 
                    DisplayMethod = PropertyType.DropDown;
                    DropDownOptions = new string[6] { "Vegetative", "EarlyReproductive", "MidReproductive", "LateReproductive", "Maturity", "Ripe" };
                    break;
                case DisplayType.PlantName:
                    DisplayMethod = PropertyType.DropDown;
                    var plantModels = model.FindAllInScope<Plant>();
                    if (plantModels != null)
                        DropDownOptions = plantModels.Select(plant => plant.Name).ToArray();
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
