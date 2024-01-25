using System;
using Models.Core;
using Models.DCAPST.Interfaces;

namespace Models.DCAPST
{
    /// <summary>
    /// Pathway Parameters.
    /// </summary>
    [Serializable]
    public class PathwayParameters : IPathwayParameters
    {
        /// <inheritdoc/>
        [Description("Ratio of intercellular CO2 to air CO2")]
        [Units("")]
        public double IntercellularToAirCO2Ratio { get; set; }

        /// <summary>
        /// Fraction of cyclic electron flow
        /// </summary>
        [Description("Fraction of cyclic electron flow")]
        [Units("")]
        public double FractionOfCyclicElectronFlow => 0.25 * ExtraATPCost;

        /// <summary>
        /// Ratio of respiration to SLN
        /// </summary>
        [Description("Ratio of SLN to respiration")]
        [Units("")]
        public double RespirationSLNRatio { get; set; }        

        /// <summary>
        /// Ratio of Rubisco activity to SLN
        /// </summary>
        [Description("Ratio of SLN to max Rubisco activity")]
        [Units("")]
        public double MaxRubiscoActivitySLNRatio { get; set; }        

        /// <summary>
        /// Ratio of electron transport to SLN
        /// </summary>
        [Description("Ratio of SLN to max electron transport")]
        [Units("")]
        public double MaxElectronTransportSLNRatio { get; set; }        

        /// <summary>
        /// Ratio of PEPc Activity to SLN
        /// </summary>
        [Description("Ratio of SLN to max PEPc activity")]
        [Units("")]
        public double MaxPEPcActivitySLNRatio { get; set; }

        /// <summary>
        /// Ratio of Mesophyll CO2 conductance to SLN
        /// </summary>
        [Description("Ratio of SLN to Mesophyll CO2 conductance")]
        [Units("")]
        public double MesophyllCO2ConductanceSLNRatio { get; set; }

        /// <summary>
        /// Mesophyll electron transport fraction
        /// </summary>
        public double MesophyllElectronTransportFraction => ExtraATPCost / (3.0 + ExtraATPCost);

        /// <summary>
        /// ATP production electron transport factor
        /// </summary>
        [Description("ATP production electron transport factor")]
        [Units("")]
        public double ATPProductionElectronTransportFactor => (3.0 - FractionOfCyclicElectronFlow) / (4.0 * (1.0 - FractionOfCyclicElectronFlow));

        /// <summary>
        /// Extra ATP cost
        /// </summary>
        [Description("Extra ATP cost")]
        [Units("")]
        public double ExtraATPCost { get; set; }

        /// <summary>
        /// Rubisco carboxylation temperature response
        /// </summary>
        [Description("Rubisco carboxylation temperature response")]
        [Units("")]
        [Display(Type = DisplayType.SubModel)]
        public TemperatureResponseValues RubiscoCarboxylation { get; set; }

        /// <summary>
        /// Rubisco oxygenation temperature response
        /// </summary>
        [Description("Rubisco oxygenation temperature response")]
        [Units("")]
        [Display(Type = DisplayType.SubModel)]
        public TemperatureResponseValues RubiscoOxygenation { get; set; }

        /// <summary>
        /// Rubisco carboxylation to oxygenation temperature response factor
        /// </summary>
        [Description("Rubisco carboxylation to oxygenation temperature response factor")]
        [Units("")]
        [Display(Type = DisplayType.SubModel)]
        public TemperatureResponseValues RubiscoCarboxylationToOxygenation { get; set; }

        /// <summary>
        /// Describes how Rubisco activity changes with temperature
        /// </summary>
        [Description("Rubisco activity temperature response")]
        [Units("")]
        [Display(Type = DisplayType.SubModel)]
        public TemperatureResponseValues RubiscoActivity { get; set; }

        /// <summary>
        /// Rubisco carboxylation temperature response factor
        /// </summary>
        [Description("PEPc temperature response factor")]
        [Units("")]
        [Display(Type = DisplayType.SubModel)]
        public TemperatureResponseValues PEPc { get; set; }

        /// <summary>
        /// Describes how PEPc activity changes with temperature
        /// </summary>
        [Description("PEPc activity temperature response")]
        [Units("")]
        [Display(Type = DisplayType.SubModel)]
        public TemperatureResponseValues PEPcActivity { get; set; }

        /// <summary>
        /// Describes how Respiration changes with temperature
        /// </summary>
        [Description("Respiration temperature response")]
        [Units("")]
        [Display(Type = DisplayType.SubModel)]
        public TemperatureResponseValues Respiration { get; set; }

        /// <summary>
        /// Describes how electron transport rate changes with temperature
        /// </summary>
        [Description("Electron transport rate temperature response")]
        [Units("")]
        [Display(Type = DisplayType.SubModel)]
        public LeafTemperatureParameters ElectronTransportRateParams { get; set; }

        /// <summary>
        /// Describes how mesophyll CO2 conductance changes with temperature
        /// </summary>
        [Description("Mesophyll CO2 conductance temperature response")]
        [Display(Type = DisplayType.SubModel)]
        public LeafTemperatureParameters MesophyllCO2ConductanceParams { get; set; }

        /// <summary>
        /// Spectral correction factor
        /// </summary>
        [Description("Spectral correction factor")]
        [Units("")]
        public double SpectralCorrectionFactor { get; set; }

        /// <summary>
        /// Fraction of photosystem II activity in the bundle sheath
        /// </summary>
        [Description("Photosystem II activity fraction")]
        [Units("")]
        public double PS2ActivityFraction { get; set; }
        
        /// <inheritdoc/>
        [Description("PEP regeneration")]
        [Units("")]
        public double PEPRegeneration { get; set; }

        /// <inheritdoc/>
        [Description("Bundle sheath conductance")]
        [Units("")]
        public double BundleSheathConductance { get; set; }       
    }

    /// <summary>
    /// Describes a temperature response.
    /// </summary>
    [Serializable]
    public class TemperatureResponseValues
    {
        /// <summary>
        /// The value of the temperature response factor for a given parameter
        /// </summary>
        [Description("The value of the temperature response factor for a given parameter")]
        public double Factor { get; set; }

        /// <summary>
        /// The value of the temperature response factor at 25 degrees
        /// </summary>
        [Description("The value of the temperature response factor at 25 degrees")]
        [Units("")]
        public double At25 { get; set; }
    }
}
