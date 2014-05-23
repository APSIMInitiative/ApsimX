using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Core;
using Models.Soils;

namespace Models.PMF
{
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class SimpleTree : Model, ICrop
    {
        public NewCanopyType CanopyData { get { return LocalCanopyData; } }
        NewCanopyType LocalCanopyData = new NewCanopyType();
        
        public RootSystem RootSystem { get; set; }
        public double CoverLive { get; set; }
        public string plant_status { get; set; }
        public double sw_demand { get; set; }

        private int NumPlots;
        private double[] dlayer;
        private Soil Soil;
        private string[] fieldList;

        [Units("mm/mm")]
        [Description("What fields will the roots be in? (comma seperated)")]
        public string fields { get; set; }

        public SimpleTree()
        {
            fields = "Field, Field1, Field2";
        }

        public override void OnSimulationCommencing()
        {
            fieldList = fields.Split(',');
            //check field names are valid
            foreach (string s in fieldList)
                if (this.Parent.Find(s.Trim()) == null)
                    throw new Exception("Error: Could not find field with name " + s);

            NumPlots = fieldList.Count();
            RootSystem = new RootSystem();
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

        [EventSubscribe("StartOfDay")]
        private void OnPrepare(object sender, EventArgs e)
        {
            RootSystem.Zones = new RootZone[NumPlots];

            for (int i = 0; i < NumPlots; i++)
            {
                Zone CurrentField = (Zone)this.Parent.Find(fieldList[i].Trim());
                // removing field properties for now
                //   Component fieldProps = (Component)MyPaddock.Parent.ChildPaddocks[i].LinkByName("FieldProps");
                RootSystem.Zones[i] = new RootZone();
                RootSystem.Zones[i].ZoneArea = (double)this.Parent.Get("Area"); //get the zone area from parent (field)
                //   if (!fieldProps.Get("fieldArea", out RootSystem.Zones[i].ZoneArea))
                //       throw new Exception("Could not find FieldProps component in field " + MyPaddock.Parent.ChildPaddocks[i].Name);
                Soil = (Soil)CurrentField.Find(typeof(Soil));
                RootSystem.Zones[i].dlayer = (double[])Soil.SoilWater.Get("dlayer");
                RootSystem.Zones[i].ZoneName = CurrentField.Name;
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
                SWDep = (double[])Soil.SoilWater.Get("sw_dep");
                LL15Dep = (double[])Soil.SoilWater.Get("ll15_dep");
                for (int j = 0; j < SWDep.Length; j++)
                {
                    //only use 1 paddock to calculate sw_demand for testing
//                    if (i == 0)
                        PotSWUptake[i][j] = Math.Max(0.0, RootProportion(j, RootSystem.Zones[i].RootDepth, RootSystem.Zones[i].dlayer) * RootSystem.Zones[i].kl[j] * (SWDep[j] - LL15Dep[j])); //* RootSystem.Zones[i].ZoneArea;
//                    else
//                        PotSWUptake[i][j] = 0;
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