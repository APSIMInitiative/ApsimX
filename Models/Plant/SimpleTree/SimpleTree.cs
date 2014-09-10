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
        /// Root system information
        /// </summary>
        [XmlIgnore]
        public RootSystem RootSystem
        {
            get
            {
                return rootSystem;
            }
            set
            {
                rootSystem = value;
            }
        }
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
           //GetPotSWUptake();
        }

        /// <summary>
        /// Calculate the potential sw uptake for today
        /// </summary>
        public List<UptakeInfo> GetPotSWUptake(List<UptakeInfo> info)
        {
            //get the uptake information applicable to this crop
            List<UptakeInfo> thisCrop = info.AsEnumerable().Where(x => x.Plant.Equals(this)).ToList();
            thisCrop = CalcSWSourceStrength(thisCrop);

            for (int i = 0; i < thisCrop.Count; i++)
            {
                List<RootZone> thisRZ = RootSystem.RootZones.AsEnumerable().Where(x => x.Zone.Name.Equals(this.Parent.Name)).ToList();
                if (thisRZ.Count == 0)
                    throw new ApsimXException(this.FullPath, "Could not find root zone in Zone " + this.Parent.Name + " for SimpleTree");

                thisCrop[i].Uptake = new double[thisCrop[i].SWDep.Length];
                for (int j = 0; j < thisCrop[i].SWDep.Length; j++)
                {
                    thisCrop[i].Uptake[j] = Math.Max(0.0, RootProportion(j, RootSystem.RootZones[0].RootDepth, RootSystem.RootZones[0].Soil.Thickness) *
                                                      RootSystem.RootZones[0].Soil.KL("SimpleTree")[j] * (thisCrop[i].SWDep[j] - RootSystem.RootZones[0].Soil.SoilWater.ll15_dep[j]) *
                                                      thisCrop[i].Strength);
                }
            }
            return thisCrop;
        }

        /// <summary>
        /// Calculate the best paddocks to take water from when a crop is in multiple root zones.
        /// This method calculates the relative source strength of each field/zone.
        /// </summary>
        /// <param name="info">A list of Uptake data from fields the current crop is in.</param>
        /// <returns>A Dictionary containing the paddock names and relative strengths</returns>
        private List<UptakeInfo> CalcSWSourceStrength(List<UptakeInfo> info) 
        {
            double[] ESWDeps = new double[info.Count];
            double TotalESW;

            for (int i = 0; i < info.Count; i++)
            {
                Soil Soil = (Soil)info[i].Zone.Find(typeof(Soil));
                double[] ESWlayers;

                ESWlayers = Utility.Math.Subtract(info[i].SWDep, Utility.Math.Multiply(Soil.LL(CropType), Soil.Thickness));
                ESWDeps[i] = (double)Utility.Math.Sum(ESWlayers);
            }

            TotalESW = (double)Utility.Math.Sum(ESWDeps);
            for (int i = 0; i < info.Count; i++)
                info[i].Strength =  ESWDeps[i] / TotalESW; // * RootData.RootZones[i].Zone.Area); //ignore area for now; need to find a better way of implementing it

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