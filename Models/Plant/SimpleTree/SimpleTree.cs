using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Core;
using Models.Soils;

namespace Models.PMF
{
    public class SimpleTree : Model
    {
        public RootSystem RootSystem { get; set; }
        public double CoverLive { get; set; }
        public string plant_status { get; set; }
        public double sw_demand { get; set; }

        private int NumPlots;
        private double[] dlayer;
        private SoilWater SoilWat;

        public override void OnCommencing()
        {
            NumPlots = this.Parent.FindAll(typeof(Zone)).Count() - 1; //will evetually be a list of zones to include
            RootSystem = new RootSystem();
            CoverLive = 0.5;
            plant_status = "alive";
            sw_demand = 0;
        }

        [EventSubscribe("StartOfDay")]
        private void OnPrepare(object sender, EventArgs e)
        {
            RootSystem.Zones = new RootZone[NumPlots];

            for (int i = 0; i < NumPlots; i++)
            {
                //   Component fieldProps = (Component)MyPaddock.Parent.ChildPaddocks[i].LinkByName("FieldProps");
                RootSystem.Zones[i] = new RootZone();
                RootSystem.Zones[i].ZoneArea = (double)this.Parent.Get("Area"); //get the zone area from parent (field)
                //   if (!fieldProps.Get("fieldArea", out RootSystem.Zones[i].ZoneArea))
                //       throw new Exception("Could not find FieldProps component in field " + MyPaddock.Parent.ChildPaddocks[i].Name);
                SoilWat = (SoilWater)this.Find(typeof(SoilWater));
                RootSystem.Zones[i].dlayer = (double[])SoilWat.Get("dlayer");
                RootSystem.Zones[i].ZoneName = this.Parent.Name;
                RootSystem.Zones[i].RootDepth = 550;
                RootSystem.Zones[i].kl = new double[RootSystem.Zones[i].dlayer.Length];
                RootSystem.Zones[i].ll = new double[RootSystem.Zones[i].dlayer.Length];

                for (int j = 0; j < RootSystem.Zones[i].dlayer.Length; j++)
                {
                    RootSystem.Zones[i].kl[j] = 0.02;
                    RootSystem.Zones[i].ll[j] = 0.15;
                }
            }
            GetPotSWUptake();
        }

        private void GetPotSWUptake()
        {
            double TotPotSWUptake = 0;
            double[] SWDep;
            double[] LL15Dep;
            double[][] PotSWUptake = new double[RootSystem.Zones.Length][];

            for (int i = 0; i < RootSystem.Zones.Length; i++)
            {
                PotSWUptake[i] = new double[RootSystem.Zones[i].dlayer.Length];
                SWDep = (double[])SoilWat.Get("sw_dep");
                LL15Dep = (double[])SoilWat.Get("ll15_dep");
                for (int j = 0; j < SWDep.Length; j++)
                {
                    //only use 1 paddock to calculate sw_demand for testing
                    if (i == 0)
                        PotSWUptake[i][j] = Math.Max(0.0, RootProportion(j, RootSystem.Zones[i].RootDepth, RootSystem.Zones[i].dlayer) * RootSystem.Zones[i].kl[j] * (SWDep[j] - LL15Dep[j])); //* RootSystem.Zones[i].ZoneArea;
                    else
                        PotSWUptake[i][j] = 0;
                }
            }

            foreach (double[] i in PotSWUptake)
                foreach (double d in i)
                    TotPotSWUptake += d;
            RootSystem.SWDemand = TotPotSWUptake;
            sw_demand = TotPotSWUptake;
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