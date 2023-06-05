using System;
using System.Collections.Generic;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Interfaces;
using Newtonsoft.Json;

namespace Models.Soils
{
    /// <summary>This class captures chemical soil data</summary>
    [Serializable]
    [ViewName("ApsimNG.Resources.Glade.ProfileView.glade")]
    [PresenterName("UserInterface.Presenters.ProfilePresenter")]
    [ValidParent(ParentType = typeof(Soil))]
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
        [Summary]
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
        [Units("mm")]
        public double[] Thickness { get; set; }

        /// <summary>pH</summary>
        [Summary]
        [Display(Format = "N1")]
        public double[] PH { get; set; }

        /// <summary>The units of pH.</summary>
        public PHUnitsEnum PHUnits { get; set; }

        /// <summary>Gets or sets the ec.</summary>
        [Summary]
        public double[] EC { get; set; }

        /// <summary>Gets or sets the esp.</summary>
        [Summary]
        public double[] ESP { get; set; }

        /// <summary>CEC.</summary>
        [Summary]
        [Units("cmol+/kg")]
        public double[] CEC { get; set; }

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
            columns.Add(new TabularData.Column("CEC", new VariableProperty(this, GetType().GetProperty("CEC"))));

            return new TabularData(Name, columns);
        }

        /// <summary>Get all solutes with standardised layer structure.</summary>
        /// <returns></returns>
        public IEnumerable<Solute> GetStandardisedSolutes()
        {
            var solutes = new List<Solute>();

            // Add in child solutes.
            foreach (var solute in Parent.FindAllChildren<Solute>())
            {
                if (MathUtilities.AreEqual(Thickness, solute.Thickness))
                    solutes.Add(solute);
                else
                {
                    var standardisedSolute = solute.Clone();
                    if (solute.InitialValuesUnits == Solute.UnitsEnum.kgha)
                        standardisedSolute.InitialValues = SoilUtilities.MapMass(solute.InitialValues, solute.Thickness, Thickness, false);
                    else
                        standardisedSolute.InitialValues = SoilUtilities.MapConcentration(solute.InitialValues, solute.Thickness, Thickness, 1.0);
                    standardisedSolute.Thickness = Thickness;
                    solutes.Add(standardisedSolute);
                }
            }
            return solutes;
        }

        /// <summary>Gets the model ready for running in a simulation.</summary>
        /// <param name="targetThickness">Target thickness.</param>
        public void Standardise(double[] targetThickness)
        {
            SetThickness(targetThickness);
            if (PHUnits == PHUnitsEnum.CaCl2)
            {
                PH = SoilUtilities.PHCaCl2ToWater(PH);
                PHUnits = PHUnitsEnum.Water;
            }

            EC = MathUtilities.FillMissingValues(EC, Thickness.Length, 0);
            ESP = MathUtilities.FillMissingValues(ESP, Thickness.Length, 0);
            PH = MathUtilities.FillMissingValues(PH, Thickness.Length, 7.0);
            CEC = MathUtilities.FillMissingValues(CEC, Thickness.Length, 0);
        }


        /// <summary>Sets the chemical thickness.</summary>
        /// <param name="targetThickness">The thickness to change the chemical to.</param>
        private void SetThickness(double[] targetThickness)
        {
            if (!MathUtilities.AreEqual(targetThickness, Thickness))
            {
                PH = SoilUtilities.MapConcentration(PH, Thickness, targetThickness, 7.0);
                EC = SoilUtilities.MapConcentration(EC, Thickness, targetThickness, MathUtilities.LastValue(EC));
                ESP = SoilUtilities.MapConcentration(ESP, Thickness, targetThickness, MathUtilities.LastValue(ESP));
                CEC = SoilUtilities.MapConcentration(CEC, Thickness, targetThickness, MathUtilities.LastValue(CEC));
                Thickness = targetThickness;
            }
        }

    }
}
