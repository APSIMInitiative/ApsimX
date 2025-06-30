using System;
using System.Collections.Generic;

namespace Models.Interfaces
{

    /// <summary>This interface describes MicroClimate / canopy comms.</summary>
    public interface ICanopy
    {
        /// <summary>Albedo.</summary>
        string CanopyType { get; }

        /// <summary>Albedo.</summary>
        double Albedo { get; }

        /// <summary>Gets or sets the gsmax.</summary>
        double Gsmax { get; }

        /// <summary>Gets or sets the R50.</summary>
        double R50 { get; }

        /// <summary>Gets the LAI (m^2/m^2)</summary>
        double LAI { get; set; }

        /// <summary>Gets the maximum LAI (m^2/m^2)</summary>
        double LAITotal { get; }

        /// <summary>Gets the cover green (0-1)</summary>
        double CoverGreen { get; }

        /// <summary>Gets the cover total (0-1)</summary>
        double CoverTotal { get; }

        /// <summary>Gets the canopy height (mm)</summary>
        double Height { get; }

        /// <summary>Gets the canopy depth (mm)</summary>
        double Depth { get; }

        /// <summary>Gets the canopy depth (mm)</summary>
        double Width { get; }

        /// <summary>Sets the potential evapotranspiration.</summary>
        double PotentialEP { get; set; }

        /// <summary>Sets the actual water demand.</summary>
        double WaterDemand { get; set; }

        /// <summary>Sets the min canopy temperature.</summary>
        double MinCanopyTemperature { get; set; }

        /// <summary>Sets the max canopy temperature.</summary>
        double MaxCanopyTemperature { get; set; }

        /// <summary>Sets the mean canopy temperature.</summary>
        double MeanCanopyTemperature { get; set; }

        /// <summary>Sets the light profile.</summary>
        CanopyEnergyBalanceInterceptionlayerType[] LightProfile { set; }
    }

    /// <summary>This interface describes a model that has a list of canopies.</summary>
    public interface IHaveCanopy
    {
        /// <summary>List of canopies that MicroClimate will use.</summary>
        List<ICanopy> Canopies { get; }
    }

    /// <summary>A canopy energy balance type</summary>
    [Serializable]
    public class CanopyEnergyBalanceInterceptionlayerType
    {
        /// <summary>The thickness</summary>
        public double thickness;

        /// <summary>The amount or radiation on green area</summary>
        public double AmountOnGreen;

        ///  <summary>The amount of radiation on dead area</summary>
        public double AmountOnDead;
    }
}
