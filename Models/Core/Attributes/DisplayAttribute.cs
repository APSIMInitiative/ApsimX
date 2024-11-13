using System;
namespace Models.Core
{

    /// <summary>
    /// An enumeration for display types.
    /// Used by the Display Attribute.
    /// </summary>
    public enum DisplayType
    {
        /// <summary>
        /// No specific display editor.
        /// </summary>
        None,

        /// <summary>
        /// Allows the user to select from a preset list of values.
        /// If this is used, the Values property of DisplayAttribute
        /// must also be set.
        /// </summary>
        DropDown,

        /// <summary>
        /// Use the table name editor.
        /// </summary>
        TableName,

        /// <summary>
        /// A cultivar name editor.
        /// </summary>
        CultivarName,

        /// <summary>
        /// A stage name selector.
        /// </summary>
        CropStageName,

        /// <summary>
        /// A list of crops parameterised as columns in a csv input file
        /// </summary>
        CSVCrops,

        /// <summary>
        /// A phase name selector.
        /// </summary>
        CropPhaseName,

        /// <summary>
        /// A LifePhase name editor.
        /// </summary>
        LifeCycleName,

        /// <summary>
        /// A LifePhase name editor.
        /// </summary>
        LifePhaseName,

        /// <summary>
        /// A file name editor.
        /// </summary>
        FileName,

        /// <summary>
        /// Allows selection of more than one file name.
        /// </summary>
        FileNames,

        /// <summary>
        /// Allows selection of a directory via a file chooser widget.
        /// </summary>
        DirectoryName,

        /// <summary>
        /// A field name editor.
        /// </summary>
        FieldName,

        /// <summary>
        /// Use a list of known residue types
        /// </summary>
        ResidueName,

        /// <summary>
        /// A model drop down.
        /// </summary>
        Model,

        /// <summary>
        /// This property is an object whose properties
        /// should also be displayed/editable in the GUI.
        /// </summary>
        SubModel,

        /// <summary>
        /// Only valid on an array property. Uses an multi-line
        /// text editor. Each line of input is treated as an
        /// element in the array.
        /// </summary>
        MultiLineText,
            
        /// <summary>
        /// This is a list of SCRUMcrop model parameterisations that 
        /// may be established in a simulation.
        /// </summary>
        SCRUMcropName,

        /// <summary>
        /// This is a list of SCRUMcrop model establishment stages.
        /// </summary>
        ScrumEstablishStages,

        /// <summary>
        /// This is a list of SCRUMcrop model harvest stages.
        /// </summary>
        ScrumHarvestStages,

        /// <summary>
        /// List of plant organs that have damagable organs returned in plant.organ format.
        /// </summary>
        PlantOrganList,

		/// <summary>
        /// Provides a EditorView object for display
        /// </summary>
        Code,

        /// <summary>
        /// Provides a GTK Colour Picker dialog
        /// </summary>
        ColourPicker,

        /// <summary>
        /// Provides a plant name.
        /// </summary>
        PlantName
    }

    /// <summary>
    /// Specifies various user interface display properties.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class DisplayAttribute : System.Attribute
    {
        /// <summary>
        /// Gets or sets the name to display in the grid.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the display format (e.g. 'N3') that the user interface should
        /// use when showing values in the related property.
        /// </summary>
        public string Format { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the user interface should display
        /// a total at the top of the column in a ProfileGrid.
        /// </summary>
        public bool ShowTotal { get; set; }

        /// <summary>
        /// Gets or sets a value denoting the type of model to show in the model drop down.
        /// </summary>
        public Type ModelType { get; set; }

        /// <summary>
        /// Gets or sets the types for the ResourceGroups whose Resource items are valid choices in the Resource name editor.
        /// eg. [Display(CLEMResourceGroups = new Type[] {typeof(AnimalFoodStore), typeof(HumanFoodStore), typeof(ProductStore) } )]"
        /// Will create a dropdown list with all the Resource items from only the AnimalFoodStore, HumanFoodStore and ProductStore.
        /// </summary>
        public Type[] CLEMResourceGroups { get; set; }

        /// <summary>
        /// Gets or sets the display type. 
        /// </summary>
        public DisplayType Type { get; set; }

        /// <summary>
        /// Gets or sets the name of a method which returns a list of valid values for this property.
        /// Methods pointed to by this property can return any generic IEnumerable and must accept no arguments.
        /// </summary>
        public string Values { get; set; }

        /// <summary>
        /// A list of objects to be passed to the values method allowing the user to further specify
        /// functioanlity from the display attributes
        /// </summary>
        public object[] ValuesArgs { get; set; }

        /// <summary>
        /// Specifies a callback method that will be called by GUI to determine if this property is enabled.
        /// </summary>
        public string EnabledCallback { get; set; }

        /// <summary>
        /// Specifies a callback method that will be called by GUI to determine if this property is visible.
        /// </summary>
        public string VisibleCallback { get; set; }

        /// <summary>
        /// Used in conjuction with <see cref="DisplayType.CultivarName"/>.
        /// Specifies the name of a plant whose cultivars should be displayed.
        /// </summary>
        public string PlantName { get; set; }

        /// <summary>
        /// Used in conjuction with <see cref="DisplayType.LifePhaseName"/>.
        /// Specifies the name of a LifeCycle whose phases should be displayed.
        /// </summary>
        public string LifeCycleName { get; set; }

        /// <summary>
        /// Set the primary order of properties for display
        /// Otherwise the line number of Description attribute is used for ordering
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// Specifies the name of a single sub-property of supported type to substitute for a class-based property in the display 
        /// This allows a class-based property to be represented by managed by the user through one of its own properties in the GUI rather than providing all properties using the DisplayType.SubModel approach
        /// </summary>
        public string SubstituteSubPropertyName { get; set; }


    }
}
