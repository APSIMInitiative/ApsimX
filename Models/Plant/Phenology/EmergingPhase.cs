using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using System.Xml.Serialization;

namespace Models.PMF.Phen
{
    /// <summary>
    /// Emerging phase in phenology
    /// </summary>
    /// \pre A \ref Models.PMF.Plant "Plant" function has to exist to 
    /// provide the sowing depth (\f$D_{seed}\f$).
    /// \param ShootLag An initial period of fixed thermal time during 
    /// which shoot elongation is slow (the "lag" phase, \f$T_{lag}\f$, deg;Cd)
    /// \param ShootRate The rate of shoot elongation (\f$r_{e}\f$, 
    /// deg;Cd mm<sup>-1</sup>) towards the soil surface is 
    /// linearly related to air temperature.
    /// <remarks>
    /// The thermal time target in the emerging phase includes 
    /// an effect of the depth of sowing (\f$D_{seed}\f$), an 
    /// initial period of fixed thermal time during which 
    /// shoot elongation is slow (the "lag" phase, \f$T_{lag}\f$, \p ShootLag)
    /// and a linear period, where the rate of shoot elongation (\f$r_{e}\f$, 
    /// \p ShootRate) towards the soil surface is linearly related to 
    /// air temperature. Then, the thermal time target (\f$T_{emer}\f$) 
    /// is calculated by 
    /// \f[
    /// T_{emer}=T_{lag}+r_{e}D_{seed}
    /// \f]
    /// </remarks>
    [Serializable]
    public class EmergingPhase : GenericPhase
    {
        /// <summary>The plant</summary>
        [Link]
        Plant Plant = null;

        /// <summary>Gets or sets the shoot lag.</summary>
        /// <value>The shoot lag.</value>
        [Units("oCd")]
       // [XmlIgnore]
        public double ShootLag { get; set; }
        /// <summary>Gets or sets the shoot rate.</summary>
        /// <value>The shoot rate</value>
        [Units("oCd/mm")]
       // [XmlIgnore]
        public double ShootRate { get; set; }

        /// <summary>Return the target to caller. Can be overridden by derived classes.</summary>
        /// <returns></returns>
        protected override double CalcTarget()
        {
            return ShootLag + Plant.SowingData.Depth * ShootRate;
        }

    }

    /// <summary>
    /// The class below is for Plant15. Need to get rid of this eventually.
    /// </summary>
    [Serializable]
    public class EmergingPhase15 : GenericPhase
    {
        /// <summary>The plant</summary>
        [Link]
        Models.PMF.OldPlant.Plant15 Plant = null;

        /// <summary>Gets or sets the shoot lag.</summary>
        /// <value>The shoot lag.</value>
        public double ShootLag { get; set; }
        /// <summary>Gets or sets the shoot rate.</summary>
        /// <value>The shoot rate.</value>
        public double ShootRate { get; set; }

        /// <summary>Return the target to caller. Can be overridden by derived classes.</summary>
        /// <returns></returns>
        protected override double CalcTarget()
        {
            return ShootLag + Plant.SowingData.Depth * ShootRate;
        }

    }
}