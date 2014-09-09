using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Core;
using Models.Soils;
using System.Xml.Serialization;

namespace Models.PMF
{
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class SimpleTree : Model, ICrop
    {
        /// <summary>
        /// Required for MicroClimate
        /// </summary>
        public NewCanopyType CanopyData { get { return LocalCanopyData; } }
        NewCanopyType LocalCanopyData = new NewCanopyType();

        /// <summary>
        /// Information on the plants root system. One for each plant
        /// </summary>
        public RootSystem RootSystem { get { return rootSystem; } }
        /// <summary>
        /// Cover live
        /// </summary>
        public double CoverLive { get; set; }
        /// <summary>
        /// plant_status
        /// </summary>
        public string plant_status { get; set; }
        // Plant soil water demand
        public double sw_demand { get; set; }

        private int NumPlots;
        private double[] dlayer;
        private Soil Soil;
        private string[] zoneList;
        private RootSystem rootSystem;

        [Units("mm/mm")]
        [Description("What zones will the roots be in? (comma seperated)")]
        public string zones { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public SimpleTree()
        {
            Name = "SimpleTree";
        }

        /// <summary>
        /// Crop type
        /// </summary>
        public string CropType { get { return "SimpleTree"; } }
        /// <summary>
        /// Frogger. Used for MicroClimate I think? 
        /// </summary>
        public double FRGR { get { return 1; } } 
        /// <summary>
        /// Gets a list of cultivar names
        /// </summary>
        public string[] CultivarNames
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// MicroClimate supplies PotentialEP
        /// </summary>
        [XmlIgnore]
        public double PotentialEP { get; set; }

        /// <summary>
        /// MicroClimate supplies LightProfile
        /// </summary>
        [XmlIgnore]
        public CanopyEnergyBalanceInterceptionlayerType[] LightProfile { get; set; }

        /// <summary>
        /// Simulation start
        /// </summary>
        public override void OnSimulationCommencing()
        {

            rootSystem = new RootSystem();
            rootSystem.RootZones = new List<RootZone>();

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

        /// <summary>
        /// Run at start of day
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [EventSubscribe("DoDailyInitialisation")]
        private void OnDoDailyInitialisation(object sender, EventArgs e)
        {
           // GetPotSWUptake();
        }

        /// <summary>
        /// Calculate the potential sw uptake for today
        /// </summary>
        public UptakeInfo GetPotSWUptake(UptakeInfo info)
        {
            return info;
        }

        /// <summary>
        /// Calculate how far through the given layer the roots are
        /// </summary>
        /// <param name="layer">The layer number to check.</param>
        /// <param name="root_depth">Depth of the roots.</param>
        /// <param name="dlayer">An array representing the thickness of the soil layers.</param>
        /// <returns></returns>
        private double RootProportion(int layer, double root_depth, double[] dlayer)
        {
            double depth_to_layer_bottom = 0;   // depth to bottom of layer (mm)
            double depth_to_layer_top = 0;      // depth to top of layer (mm)
            double depth_to_root = 0;           // depth to root in layer (mm)
            double depth_of_root_in_layer = 0;  // depth of root within layer (mm)
            // Implementation Section ----------------------------------
            for (int i = 0; i <= layer; i++)
                depth_to_layer_bottom += dlayer[i];
            depth_to_layer_top = depth_to_layer_bottom - dlayer[layer];
            depth_to_root = Math.Min(depth_to_layer_bottom, root_depth);
            depth_of_root_in_layer = Math.Max(0.0, depth_to_root - depth_to_layer_top);

            return depth_of_root_in_layer / dlayer[layer];
        }
    }
}