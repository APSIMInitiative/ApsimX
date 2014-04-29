using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;

using System.Reflection;
using System.Collections;
using Models.PMF.Functions;
using Models.Soils;


namespace Models.PMF.Slurp
{
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class Slurp : Model, ICrop
    {
        public string plant_status = "out";
        [Link]
        Soils.SoilWater SoilWat = null;
        [Link]
        Water water = null;
        [Link]
        Soils.SoilNitrogen SoilN = null;

        [Description ("Cover Green")]
        public double CoverGreen { get; set; }
        [Description("Root Depth")]
        public double RootDepth { get; set; }
        [Description("Leaf Mass")]
        public double LeafMass { get; set; }
        [Description("kl")]
        public double[] kl { get; set; }
        [Description("LAI")]
        public double LAI { get; set; }
        [Description("Total LAI")]
        public double LAItot { get; set; }
        [Description("Total Cover")]
        public double CoverTot { get; set; }
        [Description("Height")]
        public double Height { get; set; }
        [Description("Depth")]
        public double Depth { get; set; }
        
        public string CropType = "Slurp";

        public event EventHandler StartSlurp;
        public event NewCanopyDelegate NewCanopy;
        public event NewPotentialGrowthDelegate NewPotentialGrowth;

        private double PEP;
        private double EP;
        private double FW;
        private double FWexpan;
        private double Ndemand = 0.0;
        private double RootMass = 0.0;
        private double RootN;
        private double[] NUptake = null;
        private double[] PotSWUptake = null;
        private double[] SWUptake;
        private double[] PotNUptake = null;
        private double[] bd = null;
        private double RootNConcentration = 0.0;
        private double KNO3 = 0.0;

        // The following event handler will be called once at the beginning of the simulation
        public override void  OnCommencing()
        {
            kl = new double[SoilWat.sw_dep.Length];
            PotSWUptake = new double[kl.Length];
            PotNUptake = new double[kl.Length];
            SWUptake = new double[kl.Length];
            NUptake = new double[kl.Length];
            for (int i = 0; i < kl.Length; i++)
                kl[i] = 0.5;

            bd = (double[])water.Get("BD");
            // Invoke a sowing event. Needed for MicroClimate
            if (StartSlurp != null)
                StartSlurp.Invoke(this, new EventArgs());

            //Send a NewCanopy event to MicroClimate
            NewCanopyType NewCanopyType = new NewCanopyType();
            NewCanopyType.cover = CoverGreen;
            NewCanopyType.cover_tot = CoverTot;
            NewCanopyType.depth = Depth;
            NewCanopyType.height = Height;
            NewCanopyType.lai = LAI;
            NewCanopyType.lai_tot = LAItot;
            NewCanopyType.sender = "Slurp";
            if (NewCanopy != null)
                NewCanopy.Invoke(NewCanopyType);
        }

        [EventSubscribe("MiddleOfDay")]
        private void OnProcess(object sender, EventArgs e)
        {
            DoWaterBalance();
            DoNBalance();

            //for MicroClimate
            NewPotentialGrowthType NewPotentialGrowthData = new NewPotentialGrowthType();
            NewPotentialGrowthData.frgr = 1;
            NewPotentialGrowthData.sender = Name;
            if (NewPotentialGrowth != null)
                NewPotentialGrowth.Invoke(NewPotentialGrowthData);
        }

        private void DoWaterBalance()
        {
            PEP = SoilWat.eo * CoverGreen;

            for (int j = 0; j < SoilWat.ll15_dep.Length; j++)
                PotSWUptake[j] = Math.Max(0.0, RootProportion(j, RootDepth) * kl[j] * (SoilWat.sw_dep[j] - SoilWat.ll15_dep[j]));

            double TotPotSWUptake = Utility.Math.Sum(PotSWUptake);

            EP = 0.0;
            for (int j = 0; j < SoilWat.ll15_dep.Length; j++)
            {
                SWUptake[j] = PotSWUptake[j] * Math.Min(1.0, PEP / TotPotSWUptake);
                EP += SWUptake[j];
                SoilWat.sw_dep[j] = SoilWat.sw_dep[j] - SWUptake[j];

            }

            if (PEP > 0.0)
            {
                FW = EP / PEP;
                FWexpan = Math.Max(0.0, Math.Min(1.0, (TotPotSWUptake / PEP - 0.5) / 1.0));

            }
            else
            {
                FW = 1.0;
                FWexpan = 1.0;
            }

        }

        private void DoNBalance()
        {
            double StartN = PlantN;

            double RootNDemand = Math.Max(0.0, (RootMass * RootNConcentration / 100.0 - RootN)) * 10.0;  // kg/ha
            double LeafNDemand = Math.Max(0.0, (LeafMass * LeafNConc / 100 - LeafN)) * 10.0;  // kg/ha 

            Ndemand = LeafNDemand + RootNDemand;  //kg/ha


            for (int j = 0; j < SoilWat.ll15_dep.Length; j++)
            {
                double swaf = 0;
                swaf = (SoilWat.sw_dep[j] - SoilWat.ll15_dep[j]) / (SoilWat.dul_dep[j] - SoilWat.ll15_dep[j]);
                swaf = Math.Max(0.0, Math.Min(swaf, 1.0));
                double no3ppm = SoilN.no3[j] * (100.0 / (bd[j] * SoilWat.dlayer[j]));
                PotNUptake[j] = Math.Max(0.0, RootProportion(j, RootDepth) * KNO3 * SoilN.no3[j] * swaf);
            }

            double TotPotNUptake = Utility.Math.Sum(PotNUptake);
            double Fr = Math.Min(1.0, Ndemand / TotPotNUptake);

            for (int j = 0; j < SoilWat.ll15_dep.Length; j++)
            {
                NUptake[j] = PotNUptake[j] * Fr;
                SoilN.no3[j] = SoilN.no3[j] - NUptake[j];
            }

            Fr = Math.Min(1.0, Math.Max(0, Utility.Math.Sum(NUptake) / LeafNDemand));
            double DeltaLeafN = LeafNDemand * Fr;

            LeafN += Math.Max(0.0, LeafMass * LeafNConc / 100.0 - LeafN) * Fr;

            // Calculate fraction of N demand for Vegetative Parts
            if ((Ndemand - DeltaLeafN) > 0)
                Fr = Math.Max(0.0, ((Utility.Math.Sum(NUptake) - DeltaLeafN) / (Ndemand - DeltaLeafN)));
            else
                Fr = 0.0;

            double[] RootNDef = new double[SoilWat.ll15_dep.Length];
            double TotNDef = 1e-20;
            for (int j = 0; j < SoilWat.ll15_dep.Length; j++)
            {
                RootNDef[j] = Math.Max(0.0, RootMass * RootNConcentration / 100.0 - RootN);
                TotNDef += RootNDef[j];
            }
            for (int j = 0; j < SoilWat.ll15_dep.Length; j++)
                RootN += RootNDemand / 10 * Fr * RootNDef[j] / TotNDef;

            double EndN = PlantN;
            double Change = EndN - StartN;
            double Uptake = Utility.Math.Sum(NUptake) / 10.0;
            if (Math.Abs(Change - Uptake) > 0.001)
                throw new Exception("Error in N Allocation");
        }

        private double RootProportion(int layer, double root_depth)
        {
            double depth_to_layer_bottom = 0;   // depth to bottom of layer (mm)
            double depth_to_layer_top = 0;      // depth to top of layer (mm)
            double depth_to_root = 0;           // depth to root in layer (mm)
            double depth_of_root_in_layer = 0;  // depth of root within layer (mm)
            // Implementation Section ----------------------------------
            for (int i = 0; i <= layer; i++)
                depth_to_layer_bottom += SoilWat.dlayer[i];
            depth_to_layer_top = depth_to_layer_bottom - SoilWat.dlayer[layer];
            depth_to_root = Math.Min(depth_to_layer_bottom, root_depth);
            depth_of_root_in_layer = Math.Max(0.0, depth_to_root - depth_to_layer_top);

            return depth_of_root_in_layer / SoilWat.dlayer[layer];
        }

        private int LayerIndex(double depth)
        {
            double CumDepth = 0;
            for (int i = 0; i < SoilWat.dlayer.Length; i++)
            {
                CumDepth = CumDepth + SoilWat.dlayer[i];
                if (CumDepth >= depth) { return i; }
            }
            throw new Exception("Depth deeper than bottom of soil profile");
        }


        public double RootNConc
        {
            get
            {
                return RootN / RootMass * 100.0;
            }

        }

        public double PlantN
        {
            get
            {
                return LeafN + RootN;
            }
        }

        //TODO: get a good function for LeafN
        public double LeafN
        {
            get
            {
                return LAI * 1;
            }
            set
            {
                
            }

        }

        public double LeafNConc
        {
            get
            {
                return LeafN / LeafMass * 100.0;
            }

        }
    }   
}