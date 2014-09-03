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
        public NewCanopyType CanopyData { get { return LocalCanopyData; } }
        NewCanopyType LocalCanopyData = new NewCanopyType();

        public RootSystem RootSystem { get { return rootSystem; } }
        public double CoverLive { get; set; }
        public string plant_status { get; set; }
        public double sw_demand { get; set; }

        private int NumPlots;
        private double[] dlayer;
        private Soil Soil;
        private string[] zoneList;
        private RootSystem rootSystem;

        [Units("mm/mm")]
        [Description("What zones will the roots be in? (comma seperated)")]
        public string zones { get; set; }

        public SimpleTree()
        {
            Name = "SimpleTree";
        }

        public string CropType { get { return "Wheat"; } }
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

        public override void OnSimulationCommencing()
        {
            zoneList = zones.Split(',');

            NumPlots = zoneList.Count();
            rootSystem = new RootSystem();
            rootSystem.RootZones = new List<RootZone>();

            foreach (string zone in zoneList)
            {
                RootZone currentZone = new RootZone();
                currentZone.Zone = (Zone)this.Parent.Find(zone.Trim());
                if (currentZone.Zone == null)
                    throw new ApsimXException(this.FullPath, "Could not find zone " + zone);
                currentZone.Soil = (Soil)currentZone.Zone.Find(typeof(Soil));
                if (currentZone.Soil == null)
                    throw new ApsimXException(this.FullPath, "Could not find soil in zone " + zone);
                currentZone.RootDepth = 500;
                rootSystem.RootZones.Add(currentZone);
            }
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

        [EventSubscribe("DoDailyInitialisation")]
        private void OnDoDailyInitialisation(object sender, EventArgs e)
        {
 /*           for (int i = 0; i < NumPlots; i++)
            {
                Zone Currentzone = (Zone)this.Parent.Find(zoneList[i].Trim());
                // removing zone properties for now
                //   Component zoneProps = (Component)MyPaddock.Parent.ChildPaddocks[i].LinkByName("zoneProps");
                rootSystem.Zones[i] = new RootZone();
                rootSystem.Zones[i].Zone.Area = (double)this.Parent.Get("Area"); //get the zone area from parent (zone)
                //   if (!zoneProps.Get("zoneArea", out rootSystem.Zones[i].Zone.Area))
                //       throw new Exception("Could not find zoneProps component in zone " + MyPaddock.Parent.ChildPaddocks[i].Name);
                Soil = (Soil)Currentzone.Find(typeof(Soil));
                rootSystem.Zones[i].Soil.Thickness = (double[])Soil.SoilWater.Get("dlayer");
                rootSystem.Zones[i].Zone.Name = Currentzone.Name;
                rootSystem.Zones[i].RootDepth = 550;
                rootSystem.Zones[i].kl = new double[rootSystem.Zones[i].Soil.Thickness.Length];
                rootSystem.Zones[i].ll = new double[rootSystem.Zones[i].Soil.Thickness.Length];

                for (int j = 0; j < rootSystem.Zones[i].Soil.Thickness.Length; j++)
                {
                    rootSystem.Zones[i].kl[j] = 0.02;
                    rootSystem.Zones[i].ll[j] = 0.15;
                }
            }*/
            GetPotSWUptake();
        }

        private void GetPotSWUptake()
        {
            double TotPotSWUptake = 0;
            foreach (RootZone rz in rootSystem.RootZones)
            {
                rz.PotSWUptake = new double[rz.Soil.Thickness.Length];
                for (int i = 0; i < rz.Soil.Thickness.Length; i++)
                {
                    rz.PotSWUptake[i] = Math.Max(0.0, RootProportion(i, rz.RootDepth, rz.Soil.Thickness) * rz.Soil.KL(Name)[i] * (rz.Soil.SW[i] * rz.Soil.Thickness[i] - rz.Soil.LL15[i] * rz.Soil.Thickness[i])); //* rootSystem.Zones[i].Zone.Area;
                }
                TotPotSWUptake += Utility.Math.Sum(rz.PotSWUptake);
            }

            rootSystem.SWDemand = TotPotSWUptake;
            sw_demand = TotPotSWUptake; //TODO - do we still need this? think another module might want it
        }

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