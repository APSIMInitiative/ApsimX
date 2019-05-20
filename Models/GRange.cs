using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models
{
    using System.Xml.Serialization;
    using Models.Core;
    using Models.Soils;
    using Models.Soils.Arbitrator;
    using Models.Interfaces;
    using APSIM.Shared.Utilities;

    /// <summary>
    /// Implements the plant growth model logic abstracted from G-Range
    /// Currently this is just an empty stub
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Zone))]
    public class GRange : Model, IPlant, ICanopy, IUptake
    {
        #region Links

        //[Link]
        //private Clock Clock = null;

        //[Link]
        //private IWeather Weather = null;

        //[Link]
        //private MicroClimate MicroClim; //added for fr_intc_radn_ , but don't know what the corresponding variable is in MicroClimate.

        //[Link]
        //private ISummary Summary = null;

        //[Link]
        //Soils.Soil Soil = null;

        //[ScopedLinkByName]
        //private ISolute NO3 = null;

        //[ScopedLinkByName]
        //private ISolute NH4 = null;

        #endregion

        #region IPlant interface

        /// <summary>Gets a value indicating how leguminous a plant is</summary>
        public double Legumosity { get { return 0; } }

        /// <summary>Gets a value indicating whether the biomass is from a c4 plant or not</summary>
        public bool IsC4 { get { return false; } }

        /// <summary> Is the plant alive?</summary>
        public bool IsAlive { get { return true; } }

        /// <summary>Gets a list of cultivar names</summary>
        public string[] CultivarNames { get { return null; } }

        /// <summary>Get above ground biomass</summary>
        public PMF.Biomass AboveGround { get { return new PMF.Biomass(); } }

        /// <summary>Sows the plant</summary>
        /// <param name="cultivar">The cultivar.</param>
        /// <param name="population">The population.</param>
        /// <param name="depth">The depth.</param>
        /// <param name="rowSpacing">The row spacing.</param>
        /// <param name="maxCover">The maximum cover.</param>
        /// <param name="budNumber">The bud number.</param>
        /// <param name="rowConfig">The bud number.</param>
        public void Sow(string cultivar, double population, double depth, double rowSpacing, double maxCover = 1, double budNumber = 1, double rowConfig = 1) { }

        /// <summary>Returns true if the crop is ready for harvesting</summary>
        public bool IsReadyForHarvesting { get { return false; } }

        /// <summary>Harvest the crop</summary>
        public void Harvest() { }

        /// <summary>End the crop</summary>
        public void EndCrop() { }

        #endregion

        #region ICanopy interface

        /// <summary>Albedo.</summary>
        public double Albedo { get { return 0.15; } }

        /// <summary>Gets or sets the gsmax.</summary>
        public double Gsmax { get { return 0.01; } }

        /// <summary>Gets or sets the R50.</summary>
        public double R50 { get { return 200; } }

        /// <summary>Gets the LAI</summary>
        [Description("Leaf Area Index (m^2/m^2)")]
        [Units("m^2/m^2")]
        public double LAI { get; set; }

        /// <summary>Gets the LAI live + dead (m^2/m^2)</summary>
        public double LAITotal { get { return LAI; } }

        /// <summary>Gets the cover green.</summary>
        [Units("0-1")]
        public double CoverGreen { get { return Math.Min(1.0 - Math.Exp(-0.5 * LAI), 0.999999999); } }

        /// <summary>Gets the cover total.</summary>
        [Units("0-1")]
        public double CoverTotal { get { return 1.0 - (1 - CoverGreen) * (1 - 0); } }

        /// <summary>Gets the canopy height (mm)</summary>
        [Units("mm")]
        public double Height { get; set; }

        /// <summary>Gets the canopy depth (mm)</summary>
        [Units("mm")]
        public double Depth { get { return Height; } }//  Fixme.  This needs to be replaced with something that give sensible numbers for tree crops

        /// <summary>Sets the potential evapotranspiration. Set by MICROCLIMATE.</summary>
        [XmlIgnore]
        public double PotentialEP { get; set; }

        /// <summary>Sets the actual water demand.</summary>
        [XmlIgnore]
        [Units("mm")]
        public double WaterDemand { get; set; }

        /// <summary>Sets the light profile. Set by MICROCLIMATE.</summary>
        public CanopyEnergyBalanceInterceptionlayerType[] LightProfile { get; set; }

        #endregion

        #region IUptake

        /// <summary>
        /// Calculate the potential sw uptake for today. Should return null if crop is not in the ground.
        /// </summary>
        public List<ZoneWaterAndN> GetWaterUptakeEstimates(SoilState soilstate)
        {
            return null; // Needs to be implemented
        }

        /// <summary>
        /// Calculate the potential sw uptake for today. Should return null if crop is not in the ground.
        /// </summary>
        public List<ZoneWaterAndN> GetNitrogenUptakeEstimates(SoilState soilstate)
        {
            return null; // Needs to be implemented
        }

        /// <summary>
        /// Set the sw uptake for today.
        /// </summary>
        public void SetActualWaterUptake(List<ZoneWaterAndN> info)
        {
            // Needs to be implemented
        }

        /// <summary>
        /// Set the sw uptake for today
        /// </summary>
        public void SetActualNitrogenUptakes(List<ZoneWaterAndN> info)
        {
            // Needs to be implemented
        }

        #endregion

        /// <summary>Constructor</summary>
        public GRange()
        {
            Name = "GRange";
        }

        //[EventHandler]
        /// <summary>
        /// Called when [start of simulation].
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("StartOfSimulation")]
        private void OnStartOfSimulation(object sender, EventArgs e)
        { }

        /// <summary>EventHandler - preparation before the main daily processes.</summary>
        /// <param name="sender">The sender model</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data</param>
        [EventSubscribe("DoDailyInitialisation")]
        private void OnDoDailyInitialisation(object sender, EventArgs e)
        {
        }

        /// <summary>Performs the calculations for potential growth.</summary>
        /// <param name="sender">The sender model</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data</param>
        [EventSubscribe("DoPotentialPlantGrowth")]
        private void OnDoPotentialPlantGrowth(object sender, EventArgs e)
        { }

        /// <summary>Performs the calculations for actual growth.</summary>
        /// <param name="sender">The sender model</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data</param>
        [EventSubscribe("DoActualPlantGrowth")]
        private void OnDoActualPlantGrowth(object sender, EventArgs e)
        { }

    }
}