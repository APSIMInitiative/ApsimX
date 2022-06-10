namespace Models.Soils
{
    using APSIM.Shared.APSoil;
    using APSIM.Shared.Utilities;
    using Models.Core;
    using Models.Interfaces;
    using Models.Soils.Nutrients;
    using Models.Soils.Standardiser;
    using Newtonsoft.Json;
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Data;

    /// <summary>This class captures chemical soil data</summary>
    [Serializable]
    [ViewName("ApsimNG.Resources.Glade.NewGridView.glade")]
    [PresenterName("UserInterface.Presenters.NewGridPresenter")]
    [ValidParent(ParentType=typeof(Soil))]
    public class Chemical : Model, ITabularData
    {
        /// <summary>An enumeration for specifying PH units.</summary>
        public enum PHUnitsEnum
        {
            /// <summary>PH as water method.</summary>
            [Description("1:5 water")]
            Water,

            /// <summary>PH as Calcium chloride method.</summary>
            [Description("CaCl2")]
            CaCl2
        }

        /// <summary>Depth strings. Wrapper around Thickness.</summary>
        [Description("Depth")]
        [Units("mm")]
        [JsonIgnore]
        public string[] Depth
        {
            get
            {
                return SoilUtilities.ToDepthStrings(Thickness);
            }
            set
            {
                Thickness = SoilUtilities.ToThickness(value);
            }
        }

        /// <summary>Thickness of each layer.</summary>
        [Summary]
        [Units("mm")]
        public double[] Thickness { get; set; }

        /// <summary>pH</summary>
        [Summary]
        [Description("PH")]
        [Display(Format = "N1")]
        public double[] PH { get; set; }

        /// <summary>The units of pH.</summary>
        public PHUnitsEnum PHUnits { get; set; }

        /// <summary>Gets or sets the ec.</summary>
        [Summary]
        [Description("EC")]
        [Units("1:5 dS/m")]
        public double[] EC { get; set; }

        /// <summary>Gets or sets the esp.</summary>
        [Summary]
        [Description("ESP")]
        [Units("%")]
        public double[] ESP { get; set; }

        /// <summary>EC metadata</summary>
        public string[] ECMetadata { get; set; }

        /// <summary>CL metadata</summary>
        public string[] CLMetadata { get; set; }

        /// <summary>ESP metadata</summary>
        public string[] ESPMetadata { get; set; }

        /// <summary>PH metadata</summary>
        public string[] PHMetadata { get; set; }

        /// <summary>Tabular data. Called by GUI.</summary>
        public TabularData GetTabularData()
        {
            var solutes = GetStandardisedSolutes();

            var columns = new List<TabularData.Column>();

            var depthColumns = new List<VariableProperty>();
            depthColumns.Add(new VariableProperty(this, GetType().GetProperty("Depth")));
            foreach (var solute in solutes)
            {
                if (MathUtilities.AreEqual(solute.Thickness, Thickness))
                    depthColumns.Add(new VariableProperty(solute, solute.GetType().GetProperty("Depth")));
            }
            columns.Add(new TabularData.Column("Depth", depthColumns));

            foreach (var solute in solutes)
                columns.Add(new TabularData.Column(solute.Name, 
                                                   new VariableProperty(solute, solute.GetType().GetProperty("InitialValues"))));

            columns.Add(new TabularData.Column("pH", new VariableProperty(this, GetType().GetProperty("PH"))));
            columns.Add(new TabularData.Column("EC", new VariableProperty(this, GetType().GetProperty("EC"))));
            columns.Add(new TabularData.Column("ESP", new VariableProperty(this, GetType().GetProperty("ESP"))));

            return new TabularData(Name, columns);
        }

        /// <summary>Get all solutes with standardised layer structure.</summary>
        /// <returns></returns>
        private IEnumerable<Solute> GetStandardisedSolutes()
        {
            var solutes = new List<Solute>();

            // Add in child solutes.
            foreach (var solute in FindAllChildren<Solute>())
            {
                if (MathUtilities.AreEqual(Thickness, solute.Thickness))
                    solutes.Add(solute);
                else
                {
                    var standardisedSolute = solute.Clone();
                    if (solute.InitialValuesUnits == Solute.UnitsEnum.kgha)
                        standardisedSolute.InitialValues = Layers.MapMass(solute.InitialValues, solute.Thickness, Thickness, false);
                    else
                        standardisedSolute.InitialValues = Layers.MapConcentration(solute.InitialValues, solute.Thickness, Thickness, 1.0);
                    standardisedSolute.Thickness = Thickness;
                    solutes.Add(standardisedSolute);
                }
            }
            return solutes;
        }


    }
}
