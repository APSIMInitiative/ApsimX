﻿using System;
using System.Collections.Generic;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Interfaces;
using Models.Utilities;
using Newtonsoft.Json;

namespace Models.Soils
{
    /// <summary>This class captures chemical soil data</summary>
    [Serializable]
    [ViewName("ApsimNG.Resources.Glade.ProfileView.glade")]
    [PresenterName("UserInterface.Presenters.ProfilePresenter")]
    [ValidParent(ParentType = typeof(Soil))]
    public class Chemical : Model
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
        [Display]
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
        [Display(Format = "N3")]
        [Summary]
        public double[] EC { get; set; }

        /// <summary>Gets or sets the esp.</summary>
        [Display(Format = "N3")]
        [Summary]
        public double[] ESP { get; set; }

        /// <summary>CEC.</summary>
        [Display(Format = "N3")]
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

        /// <summary>Get all solutes with standardised layer structure.</summary>
        /// <returns></returns>
        public static IEnumerable<Solute> GetStandardisedSolutes(Chemical chemical)
        {
            List<Solute> solutes = new List<Solute>();

            // Add in child solutes.
            foreach (Solute solute in chemical.Parent.FindAllChildren<Solute>())
            {
                if (MathUtilities.AreEqual(chemical.Thickness, solute.Thickness))
                    solutes.Add(solute);
                else
                {
                    Solute standardisedSolute = solute.Clone();
                    if (standardisedSolute.Parent == null)
                        standardisedSolute.Parent = solute.Parent;

                    if (solute.InitialValuesUnits == Solute.UnitsEnum.kgha)
                        standardisedSolute.InitialValues = SoilUtilities.MapMass(solute.InitialValues, solute.Thickness, chemical.Thickness, false);
                    else
                        standardisedSolute.InitialValues = SoilUtilities.MapConcentration(solute.InitialValues, solute.Thickness, chemical.Thickness, 1.0);
                    standardisedSolute.Thickness = chemical.Thickness;
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
