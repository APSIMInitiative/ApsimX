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
        [Units("cm")]
        [JsonIgnore]
        public string[] Depth
        {
            get
            {
                return SoilUtilities.ToDepthStringsCM(Thickness);
            }
            set
            {
                Thickness = SoilUtilities.ToThicknessCM(value);
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
        public DataTable TabularData
        {
            get
            {
                return GetData();
            }
            set
            {
                SetData(value);
            }
        }

        /// <summary>Get tabular data. Called by GUI.</summary>
        private DataTable GetData()
        {
            var solutes = GetStandardisedSolutes();

            var data = new DataTable("Chemical");
            data.Columns.Add("Depth");
            foreach (var solute in solutes)
            {
                data.Columns.Add(solute.Name);
                bool soluteOnDifferentLayerStructure = !MathUtilities.AreEqual(solute.Thickness,
                                                                               FindChild<Solute>(solute.Name).Thickness);
                if (soluteOnDifferentLayerStructure)
                {
                    // Different layer structure in solute so mark it as readonly.
                    data.Columns[solute.Name].ReadOnly = true;
                }
            }
            data.Columns.Add("pH");
            data.Columns.Add("EC");
            data.Columns.Add("ESP");

            // Add units to row 1.
            var unitsRow = data.NewRow();
            unitsRow["Depth"] = "(mm)";
            unitsRow["pH"] = $"({PHUnits})";
            unitsRow["EC"] = "(1:5 dS/m)";
            unitsRow["ESP"] = "(%)";
            foreach (var solute in solutes)
                unitsRow[solute.Name] = $"({solute.InitialValuesUnits})";
            data.Rows.Add(unitsRow);

            var depthStrings = SoilUtilities.ToDepthStrings(Thickness);
            for (int i = 0; i < Thickness.Length; i++)
            {
                var row = data.NewRow();
                row["Depth"] = depthStrings[i];
                if (PH != null && i < PH.Length)
                    row["pH"] = PH[i].ToString("F3");
                if (EC != null && i < EC.Length)
                    row["EC"] = EC[i].ToString("F3");
                if (ESP != null && i < ESP.Length)
                    row["ESP"] = ESP[i].ToString("F3");
                foreach (var solute in solutes)
                    row[solute.Name] = solute.InitialValues[i].ToString("F3");
                data.Rows.Add(row);
            }

            return data;
        }

        /// <summary>Setting tabular data. Called by GUI.</summary>
        /// <param name="data"></param>
        public void SetData(DataTable data)
        {
            var solutes = GetStandardisedSolutes();
            var depthStrings = DataTableUtilities.GetColumnAsStrings(data, "Depth", 100, 1);
            var numLayers = depthStrings.ToList().FindIndex(value => value == null);
            if (numLayers == -1)
                numLayers = 100;

            Thickness = SoilUtilities.ToThickness(DataTableUtilities.GetColumnAsStrings(data, "Depth", numLayers, 1));
            PH = DataTableUtilities.GetColumnAsDoubles(data, "PH", numLayers, 1);
            EC = DataTableUtilities.GetColumnAsDoubles(data, "EC", numLayers, 1);
            ESP = DataTableUtilities.GetColumnAsDoubles(data, "ESP", numLayers, 1);

            int i = 1;
            foreach (var solute in solutes)
            {
                solute.InitialValues = DataTableUtilities.GetColumnAsDoubles(data, solute.Name, numLayers, 1);
                var units = data.Rows[0][i].ToString();
                if (units == "(kgha)")
                    solute.InitialValuesUnits = Solute.UnitsEnum.kgha;
                else
                    solute.InitialValuesUnits = Solute.UnitsEnum.ppm;
                i++;
            }
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

        /// <summary>
        /// Get possible units for a given column.
        /// </summary>
        /// <param name="columnIndex">The column index.</param>
        /// <returns></returns>
        public IEnumerable<string> GetUnits(int columnIndex)
        {
            var numSolutes = GetStandardisedSolutes().Count();

            if (columnIndex >= 1 && columnIndex <= numSolutes)
                return (string[]) Enum.GetNames(typeof (Solute.UnitsEnum));
            else if (columnIndex == numSolutes + 1)
                return (string[]) Enum.GetNames(typeof(PHUnitsEnum));
            else
                return new string[0];
        }

        /// <summary>
        /// Set the units for a column.
        /// </summary>
        /// <param name="columnIndex"></param>
        /// <param name="units"></param>
        public void SetUnits(int columnIndex, string units)
        {
            var solutes = GetStandardisedSolutes().ToList();
            var numSolutes = solutes.Count;

            if (columnIndex >= 1 && columnIndex <= numSolutes)
                solutes[columnIndex-1].InitialValuesUnits = (Solute.UnitsEnum) Enum.Parse(typeof(Solute.UnitsEnum), units);
            else if (columnIndex == numSolutes + 1)
                PHUnits = (PHUnitsEnum)Enum.Parse(typeof(PHUnitsEnum), units);            
        }
    }
}
