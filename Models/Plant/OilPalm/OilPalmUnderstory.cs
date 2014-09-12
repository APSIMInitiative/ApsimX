using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;

using System.Reflection;
using System.Collections;


namespace Models.PMF.OilPalm
{
    public class OilPalmUnderstory: Model
    {
       
        public string plant_status = "out";
        
        public string Crop_Type = "OilPalmUnderstory";
        
        //double height = 300.0;
        
        //double cover_tot = 0.0;
        
        double cover_green = 0.0;
        
        double RootDepth = 300.0;
        [Link]
        Soils.SoilWater SoilWat = null;
        [Link]
        Soils.SoilNitrogen SoilN = null;
        [Link]
        WeatherFile MetData = null;

        double kl = 0.04;
               
        double[] PotSWUptake;
        
        double[] SWUptake;
        
        double PEP = 0.0;
        
        double EP = 0.0;
        
        double DltDM = 0.0;
        
        double FW = 0.0;

        //double[] sw_dep;
        double[] PotNUptake;
        double[] NUptake;

        public delegate void NitrogenChangedDelegate(Soils.NitrogenChangedType Data);
        public event NitrogenChangedDelegate NitrogenChanged;

        public double NFixation { get; set; }

        // The following event handler will be called once at the beginning of the simulation
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            //MyPaddock.Parent.ChildPaddocks
            PotSWUptake = new double[SoilWat.ll15_dep.Length];
            SWUptake = new double[SoilWat.ll15_dep.Length];
            NUptake = new double[SoilWat.ll15_dep.Length];
            PotNUptake = new double[SoilWat.ll15_dep.Length];
        }
        
        public event BiomassRemovedDelegate BiomassRemoved;
        
        public event NewCropDelegate NewCrop;
        
        public event NullTypeDelegate Sowing;

        [EventSubscribe("Sow")]
        private void OnSow(SowPlant2Type Sow)
        {
            plant_status = "alive";

            if (NewCrop != null)
            {
                NewCropType Crop = new NewCropType();
                Crop.crop_type = Crop_Type;
                Crop.sender = Name;
                NewCrop.Invoke(Crop);
            }

            if (Sowing != null)
                Sowing.Invoke();

        }

        [EventSubscribe("DoPlantGrowth")]
        private void OnDoPlantGrowth(object sender, EventArgs e)
        {

            DoWaterBalance();
            DoGrowth();
            DoNBalance();

            // Now add today's growth to the soil - ie assume plants are in steady state.
            BiomassRemovedType BiomassRemovedData = new BiomassRemovedType();
            BiomassRemovedData.crop_type = Crop_Type;
            BiomassRemovedData.dm_type = new string[1] { "litter" };
            BiomassRemovedData.dlt_crop_dm = new float[1] { (float)(DltDM * 10) };
            BiomassRemovedData.dlt_dm_n = new float[1] { (float)(NFixation + Utility.Math.Sum(NUptake)) };
            BiomassRemovedData.dlt_dm_p = new float[1] { 0 };
            BiomassRemovedData.fraction_to_residue = new float[1] { 1 };
            BiomassRemoved.Invoke(BiomassRemovedData);


        }

        private void DoGrowth()
        {
            double OPCover = (double)Apsim.Get(this, "OilPalm.cover_green");
            double RUE = 1.3;
            DltDM = RUE * MetData.Radn * cover_green * (1 - OPCover) * FW;

        }

        private void DoWaterBalance()
        {
            double OPCover = (double)Apsim.Get(this, "OilPalm.cover_green");
            
            cover_green = 0.40 * (1 - OPCover);
            PEP = SoilWat.eo * cover_green * (1 - OPCover);


            for (int j = 0; j < SoilWat.Thickness.Length; j++)
                PotSWUptake[j] = Math.Max(0.0, RootProportion(j, RootDepth) * kl * (SoilWat.sw_dep[j] - SoilWat.ll15_dep[j]));

            double TotPotSWUptake = Utility.Math.Sum(PotSWUptake);

            EP = 0.0;
            for (int j = 0; j < SoilWat.Thickness.Length; j++)
            {
                SWUptake[j] = PotSWUptake[j] * Math.Min(1.0, PEP / TotPotSWUptake);
                EP += SWUptake[j];
                SoilWat.sw_dep[j] = SoilWat.sw_dep[j] - SWUptake[j];

            }
            if (PEP > 0.0)
            {
                FW = EP / PEP;
            }
            else
            {
                FW = 1.0;
            }

        }

        private void DoNBalance()
        {
            Soils.NitrogenChangedType NUptakeType = new Soils.NitrogenChangedType();
            NUptakeType.Sender = Name;
            NUptakeType.SenderType = "Plant";
            NUptakeType.DeltaNO3 = new double[SoilWat.Thickness.Length];
            NUptakeType.DeltaNH4 = new double[SoilWat.Thickness.Length];

            double Ndemand = DltDM * 10 * 0.021;
            NFixation = Math.Max(0.0, Ndemand * .44);

            for (int j = 0; j < SoilWat.Thickness.Length; j++)
            {
                PotNUptake[j] = Math.Max(0.0, RootProportion(j, RootDepth) * SoilN.no3[j]);
            }

            double TotPotNUptake = Utility.Math.Sum(PotNUptake);
            double Fr = Math.Min(1.0, (Ndemand - NFixation) / TotPotNUptake);

            for (int j = 0; j < SoilWat.Thickness.Length; j++)
            {
                NUptake[j] = PotNUptake[j] * Fr;
                NUptakeType.DeltaNO3[j] = -NUptake[j];
            }

            if (NitrogenChanged != null)
                NitrogenChanged.Invoke(NUptakeType);
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

    }
}