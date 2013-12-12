using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UserInterface.Views;
using System.Reflection;
using System.Data;
using Models.Soils;
using Models.Core;
using System.Drawing;
using Models.Graph;
using System.IO;
using System.Xml;

namespace UserInterface.Presenters
{
    /// <summary>
    /// This presenter talks to a ProfileView to display profile (layered) data in a grid.
    /// It uses reflection to look for public properties that are read/write, don't have an
    /// [XmlIgnore] attribute and return either double[] or string[].
    /// 
    /// For each property found it will
    ///   1. optionally look for units via a units attribute:
    ///         [Units("kg/ha")]
    ///   2. optionally look for changable units via the presense of these properties/  methods:
    ///         public Enum {Property}Units { get; set; }
    ///         public string {Property}UnitsToString(Enum Units)
    ///         public void   {Property}UnitsSet(Enum ToUnits)
    ///     
    ///         where {Property} is the name of the property being examined.
    ///   3. optionally look for a metadata property named:
    ///     {Property}Metadata { get; set; }
    /// </summary>
    class ProfilePresenter : IPresenter
    {
        /// <summary>
        /// Encapsulates a discovered property of a model. Provides properties to ProfilePresenter
        /// returning information about the property.
        /// </summary>
        class ProfileProperty
        {
            public Model Model;
            public PropertyInfo Property;

            /// <summary>
            /// Return name to caller.
            /// </summary>
            public string Name 
            { 
                get 
                {
                    if (Model is SoilCrop)
                        return (Model as SoilCrop).Name + " " + Property.Name;
                    return Property.Name; 
                } 
            }

            /// <summary>
            /// Return units to caller.
            /// </summary>
            public string Units
            {
                get
                {
                    // Get units from property
                    string UnitString = null;
                    Units UnitsAttribute = Utility.Reflection.GetAttribute(Property, typeof(Units), false) as Units;
                    PropertyInfo UnitsInfo = Model.GetType().GetProperty(Property.Name + "Units");
                    MethodInfo UnitsToStringInfo = Model.GetType().GetMethod(Property.Name + "UnitsToString");
                    if (UnitsAttribute != null)
                        UnitString = UnitsAttribute.UnitsString;
                    else if (UnitsToStringInfo != null)
                        UnitString = (string)UnitsToStringInfo.Invoke(Model, new object[] { null });
                    return UnitString;
                }
            }

            /// <summary>
            /// Return true if this property is readonly.
            /// </summary>
            public bool IsReadOnly
            {
                get
                {
                    if (!Property.CanWrite)
                        return true;
                    if (Metadata.Contains("Estimated") || Metadata.Contains("Calculated"))
                        return true;
                    return false;
                }
            }

            /// <summary>
            /// Return metadata for each layer. Returns new string[0] if none available.
            /// </summary>
            public string[] Metadata
            {
                get
                {
                    PropertyInfo metadataInfo = Model.GetType().GetProperty(Property.Name + "Metadata");
                    if (metadataInfo != null)
                    {
                        string[] metadata = metadataInfo.GetValue(Model, null) as string[];
                        if (metadata != null)
                            return metadata;
                    }
                    return new string[0];
                }
            }

            /// <summary>
            ///  Return true if the property is of type double[]
            /// </summary>
            public Type DataType { get { return Property.PropertyType.GetElementType(); } }

            /// <summary>
            /// Return the property value as an array of double.s
            /// </summary>
            public object[] Values 
            { 
                get 
                { 
                    Array A = Property.GetValue(Model, null) as Array;
                    if (A == null)
                        return null;
                    else
                    {
                        object[] values = new object[A.Length];
                        A.CopyTo(values, 0);
                        return values;
                    }
                } 
            }

            /// <summary>
            /// Return the number of decimal places to be used to display this property.
            /// </summary>
            public string Format
            {
                get
                {
                    DisplayFormat displayFormatAttribute = Utility.Reflection.GetAttribute(Property, typeof(DisplayFormat), false) as DisplayFormat;
                    if (displayFormatAttribute != null)
                        return displayFormatAttribute.Format;
                    return "N3";
                }
            }

            /// <summary>
            /// Return the crop name or null if this property isn't a crop one.
            /// </summary>
            public string CropName
            {
                get
                {
                    if (Model is SoilCrop)
                        return Model.Name;
                    return null;
                }
            }

            /// <summary>
            /// If the column has been marked as [DisplayTotal] then return the sum of all values.
            /// Otherwise return Double.Nan
            /// </summary>
            public double Total
            {
                get
                {
                    if (Utility.Reflection.GetAttribute(Property, typeof(DisplayTotal), false) != null 
                        && DataType == typeof(double))
                    {
                        double sum = 0.0;
                        foreach (double doubleValue in Values)
                            sum += doubleValue;
                        return sum;
                    }
                    return Double.NaN;
                }
            }

        }

        // Privates
        private Model Model;
        private IProfileView View;
        private CommandHistory CommandHistory;
        private GraphPresenter GraphPresenter;
        private PropertyPresenter PropertyPresenter;
        private List<ProfileProperty> PropertiesInGrid = new List<ProfileProperty>();
        private Graph g;
        private Zone ParentZone;

        /// <summary>
        /// Attach the model to the view.
        /// </summary>
        public void Attach(object Model, object View, CommandHistory CommandHistory)
        {
            this.Model = Model as Model;
            this.View = View as IProfileView;
            this.CommandHistory = CommandHistory;

            // Setup the property presenter and view. Hide the view if there are no properties to show.
            PropertyPresenter = new PropertyPresenter();
            PropertyPresenter.Attach(Model, this.View.PropertyGrid, CommandHistory);
            this.View.ShowPropertyGrid(!PropertyPresenter.IsEmpty);

            // Create a list of profile (array) properties. Create a table from them and 
            // hand the table to the profile grid.
            FindAllProperties(this.Model);
            DataTable Table = CreateTable();
            this.View.ProfileGrid.DataSource = Table;
            
            // Format the profile grid.
            FormatGrid(Table);

            // Populate the graph.
            g = Utility.Graph.CreateGraphFromResource(Model.GetType().Name + "Graph");
            if (g == null)
                this.View.ShowGraph(false);
            else
            {
                ParentZone = this.Model.Find(typeof(Zone)) as Zone;
                if (ParentZone != null)
                {
                    ParentZone.AddModel(g);
                    this.View.ShowGraph(true);
                    GraphPresenter = new GraphPresenter();
                    GraphPresenter.Attach(g, this.View.Graph, CommandHistory);
                }
            }
        }

        /// <summary>
        /// Detach the model from the view.
        /// </summary>
        public void Detach()
        {
            if (ParentZone != null && g != null)
                this.Model.RemoveModel(g);
        }

        /// <summary>
        /// Format the grid based on the data in the specified table.
        /// </summary>
        private void FormatGrid(DataTable Table)
        {
            Color[] CropColors = { Color.FromArgb(173, 221, 142), Color.FromArgb(247, 252, 185) };
            Color[] PredictedCropColors = { Color.FromArgb(233, 191, 255), Color.FromArgb(244, 226, 255) };

            int CropIndex = 0;
            int PredictedCropIndex = 0;

            Color ForegroundColour = Color.Black;
            Color BackgroundColour = Color.White;

            for (int Col = 0; Col < PropertiesInGrid.Count; Col++)
            {
                ProfileProperty Property = PropertiesInGrid[Col];

                string ColumnName = Property.Name;

                // crop colours
                if (Property.CropName != null)
                {
                    if (Property.Metadata.Contains("Estimated"))
                    {
                        BackgroundColour = PredictedCropColors[PredictedCropIndex];
                        ForegroundColour = Color.Gray;
                        if (ColumnName.Contains("XF"))
                            PredictedCropIndex++;
                        if (PredictedCropIndex >= PredictedCropColors.Length)
                            PredictedCropIndex = 0;
                    }
                    else
                    {
                        BackgroundColour = CropColors[CropIndex];
                        if (ColumnName.Contains("XF"))
                            CropIndex++;
                        if (CropIndex >= CropColors.Length)
                            CropIndex = 0;
                    }
                }
                // tool tips
                string[] ToolTips = null;
                if (Property.IsReadOnly)
                {
                    ForegroundColour = Color.Gray;
                    ToolTips = Utility.String.CreateStringArray("Calculated", View.ProfileGrid.RowCount);
                }
                else
                {
                    ForegroundColour = Color.Black;
                    ToolTips = Property.Metadata;
                }

                View.ProfileGrid.SetColumnFormat(Col, Property.Format, BackgroundColour, ForegroundColour, Property.IsReadOnly, ToolTips);

                // colour the column headers of total columns.
                if (!Double.IsNaN(Property.Total))
                    View.ProfileGrid.SetColumnHeaderColours(Col, null, Color.Red);
            }

            View.ProfileGrid.RowCount = 100;
        }

        #region DataTable creation methods.

        /// <summary>
        /// Setup the profile grid based on the properties in the model.
        /// </summary>
        private void FindAllProperties(Model Model)
        {
            // Properties must be public with a getter and a setter. They must also
            // be either double[] or string[] type.
            foreach (PropertyInfo Property in Model.GetType().GetProperties())
            {
                bool Ignore = Property.IsDefined(typeof(UserInterfaceIgnore), false);
                if (!Ignore && Property.CanRead)
                {
                    if ((Property.PropertyType == typeof(double[]) || Property.PropertyType == typeof(string[])) &&
                        !Property.Name.Contains("Metadata"))
                    {
                        PropertiesInGrid.Add(new ProfileProperty() { Model = Model, Property = Property });
                    }
                    else if (Property.PropertyType.FullName.Contains("SoilCrop"))
                    {
                        List<SoilCrop> Crops = Property.GetValue(Model, null) as List<SoilCrop>;
                        if (Crops != null)
                            foreach (SoilCrop Crop in Crops)
                                FindAllProperties(Crop);
                    }
                }
            }
        }


        /// <summary>
        /// Setup the profile grid based on the properties in the model.
        /// </summary>
        private DataTable CreateTable()
        {
            DataTable Table = new DataTable();

            foreach (ProfileProperty Property in PropertiesInGrid)
            {
                string ColumnName = Property.Name;
                if (Property.Units != null)
                    ColumnName += "\r\n(" + Property.Units + ")";

                // add a total to the column header if necessary.
                double total = Property.Total;
                if (!Double.IsNaN(total))
                {
                    ColumnName = ColumnName + "\r\n" + total.ToString("N1") + " mm";

                }

                object[] Values = Property.Values;

                if (Table.Columns.IndexOf(ColumnName) == -1)
                    Table.Columns.Add(ColumnName, Property.DataType);

                Utility.DataTable.AddColumnOfObjects(Table, ColumnName, Values);
            }
            return Table;
        }

        #endregion

    }
}

