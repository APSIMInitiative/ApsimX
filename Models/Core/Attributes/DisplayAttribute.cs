

namespace Models.Core
{
    using System;

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
        /// Use the table name editor.
        /// </summary>
        TableName,

        /// <summary>
        /// A cultivar name editor.
        /// </summary>
        CultivarName,

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
        /// A CLEM Resource name editor.
        /// </summary>
        CLEMResourceName,

        /// <summary>
        /// A CLEM Crop file reader editor.
        /// </summary>
        CLEMCropFileName,

        /// <summary>
        /// A CLEM Grasp file reader editor.
        /// </summary>
        CLEMGraspFileName
    }

    /// <summary>
    /// Specifies various user interface display properties.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class DisplayAttribute : System.Attribute
    {
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
        /// eg. [Display(CLEMResourceNameResourceGroups = new Type[] {typeof(AnimalFoodStore), typeof(HumanFoodStore), typeof(ProductStore) } )]"
        /// Will create a dropdown list with all the Resource items from only the AnimalFoodStore, HumanFoodStore and ProductStore.
        /// </summary>
        public Type[] CLEMResourceNameResourceGroups { get; set; }

        /// <summary>
        /// Gets or sets strings that are manually added to the Resource name editor.
        /// eg. [Display(CLEMExtraEntries = new string[] {"None", "All"}  )]"
        /// Will add these strings to the dropdown list created by CLEMResourceNameResourceGroups. 
        /// </summary>
        public string[] CLEMExtraEntries { get; set; }

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
        /// Specifies a callback method that will be called by GUI to determine if this property is enabled.
        /// </summary>
        public string EnabledCallback { get; set; }

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
    }
}
