using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Core;
using Models.Soils;
using System.Xml.Serialization;

namespace Models.PMF
{
    /// <summary>
    /// A model of a simple tree
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class SimpleTree : Model, ICrop
    {
        /// <summary>Required for MicroClimate</summary>
        public NewCanopyType CanopyData { get { return LocalCanopyData; } }
        /// <summary>The local canopy data</summary>
        NewCanopyType LocalCanopyData = new NewCanopyType();

        /// <summary>
        /// Is the plant alive?
        /// </summary>
        public bool IsAlive
        {
            get { return true; }
        }
        /// <summary>Cover live</summary>
        /// <value>The cover live.</value>
        public double CoverLive { get; set; }
        /// <summary>plant_status</summary>
        /// <value>The plant_status.</value>
        public string plant_status { get; set; }
        // Plant soil water demand
        /// <summary>Gets or sets the sw_demand.</summary>
        /// <value>The sw_demand.</value>
        [XmlIgnore]
        public double sw_demand { get; set; }
        /// <summary>A list of uptakes generated for the soil arbitrator</summary>
        [XmlIgnore]
        public List<ZoneWaterAndN> Uptakes;
        /// <summary>The actual uptake of the plant</summary>
        /// <value>The uptake.</value>
        [XmlIgnore]
        public double[] Uptake {get;set;}


        /// <summary>Gets or sets the zones.</summary>
        /// <value>The zones.</value>
        [Units("mm")]
        [Description("Constant value for plant EP")]
        public string ConstantEP { get; set; }

        /// <summary>Constructor</summary>
        public SimpleTree()
        {
            Name = "SimpleTree";
        }

        /// <summary>Crop type</summary>
        public string CropType { get { return "SimpleTree"; } }
        /// <summary>Frogger. Used for MicroClimate I think?</summary>
        public double FRGR { get { return 1; } }
        /// <summary>Gets a list of cultivar names</summary>
        public string[] CultivarNames
        {
            get
            {
                return null;
            }
        }

        /// <summary>MicroClimate supplies PotentialEP</summary>
        [XmlIgnore]
        public double PotentialEP { get; set; }

        /// <summary>MicroClimate supplies LightProfile</summary>
        [XmlIgnore]
        public CanopyEnergyBalanceInterceptionlayerType[] LightProfile { get; set; }

        /// <summary>Simulation start</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            Uptakes = new List<ZoneWaterAndN>();
            CoverLive = 0.5;
            plant_status = "alive";
            sw_demand = 0;
            
            //HEB.  I have put these here so values can be got by interface
            LocalCanopyData.sender = Name;
            LocalCanopyData.lai = 0;
            LocalCanopyData.lai_tot = 0;
            LocalCanopyData.height = 0;             // height effect, mm 
            LocalCanopyData.depth = 0;              // canopy depth 
            LocalCanopyData.cover = CoverLive;
            LocalCanopyData.cover_tot = CoverLive;
        }

        /// <summary>Run at start of day</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoDailyInitialisation")]
        private void OnDoDailyInitialisation(object sender, EventArgs e)
        {
        }

        /// <summary>Calculate the potential sw uptake for today</summary>
        /// <param name="info"></param>
        /// <returns></returns>
        /// <exception cref="ApsimXException">Could not find root zone in Zone  + this.Parent.Name +  for SimpleTree</exception>
        public List<ZoneWaterAndN> GetSWUptakes(List<ZoneWaterAndN> info)
        {
            return null;
        }

        /// <summary>
        /// Set the potential sw uptake for today
        /// </summary>
        public void SetSWUptake(List<Soils.ZoneWaterAndN> info)
        {}



       /// <summary>Sows the plant</summary>
        /// <param name="cultivar">The cultivar.</param>
        /// <param name="population">The population.</param>
        /// <param name="depth">The depth.</param>
        /// <param name="rowSpacing">The row spacing.</param>
        /// <param name="maxCover">The maximum cover.</param>
        /// <param name="budNumber">The bud number.</param>
        public void Sow(string cultivar, double population, double depth, double rowSpacing, double maxCover = 1, double budNumber = 1)
        {

        }
    }
}