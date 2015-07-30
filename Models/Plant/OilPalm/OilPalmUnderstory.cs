using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;

using System.Reflection;
using System.Collections;
using APSIM.Shared.Utilities;
using Models.Interfaces;


namespace Models.PMF.OilPalm
{
    /// <summary>
    /// A model of oil palm understory
    /// </summary>
    public class OilPalmUnderstory: Model
    {

        /// <summary>The plant_status</summary>
        public string plant_status = "out";

        /// <summary>The crop_ type</summary>
        public string Crop_Type = "OilPalmUnderstory";
        
        //double height = 300.0;
        
        //double cover_tot = 0.0;

        /// <summary>The cover_green</summary>
        double cover_green = 0.0;

        /// <summary>The root depth</summary>
        double RootDepth = 300.0;
        /// <summary>The soil wat</summary>
        [Link]
        Soils.SoilWater SoilWat = null;
        /// <summary>The soil n</summary>
        [Link]
        Soils.SoilNitrogen SoilN = null;
        /// <summary>The met data</summary>
        [Link]
        IWeather MetData = null;

        /// <summary>The kl</summary>
        double kl = 0.04;

        /// <summary>The pot sw uptake</summary>
        double[] PotSWUptake;

        /// <summary>The sw uptake</summary>
        double[] SWUptake;

        /// <summary>The pep</summary>
        double PEP = 0.0;

        /// <summary>The ep</summary>
        double EP = 0.0;

        /// <summary>The DLT dm</summary>
        double DltDM = 0.0;

        /// <summary>The fw</summary>
        double FW = 0.0;

        //double[] sw_dep;
        /// <summary>The pot n uptake</summary>
        double[] PotNUptake;
        /// <summary>The n uptake</summary>
        double[] NUptake;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Data">The data.</param>
        public delegate void NitrogenChangedDelegate(Soils.NitrogenChangedType Data);
        /// <summary>Occurs when [nitrogen changed].</summary>
        public event NitrogenChangedDelegate NitrogenChanged;

        /// <summary>Gets or sets the n fixation.</summary>
        /// <value>The n fixation.</value>
        public double NFixation { get; set; }

        // The following event handler will be called once at the beginning of the simulation
        /// <summary>Called when [simulation commencing].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            //MyPaddock.Parent.ChildPaddocks
            PotSWUptake = new double[SoilWat.LL15mm.Length];
            SWUptake = new double[SoilWat.LL15mm.Length];
            NUptake = new double[SoilWat.LL15mm.Length];
            PotNUptake = new double[SoilWat.LL15mm.Length];
        }

        /// <summary>Occurs when [biomass removed].</summary>
        public event BiomassRemovedDelegate BiomassRemoved;

        /// <summary>Occurs when [new crop].</summary>
        public event NewCropDelegate NewCrop;

        /// <summary>Occurs when [sowing].</summary>
        public event NullTypeDelegate Sowing;

        /// <summary>Called when [sow].</summary>
        /// <param name="Sow">The sow.</param>
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

        /// <summary>Called when [do plant growth].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
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
            BiomassRemovedData.dlt_dm_n = new float[1] { (float)(NFixation + MathUtilities.Sum(NUptake)) };
            BiomassRemovedData.dlt_dm_p = new float[1] { 0 };
            BiomassRemovedData.fraction_to_residue = new float[1] { 1 };
            BiomassRemoved.Invoke(BiomassRemovedData);


        }

        /// <summary>Does the growth.</summary>
        private void DoGrowth()
        {
            double OPCover = (double)Apsim.Get(this, "OilPalm.cover_green");
            double RUE = 1.3;
            DltDM = RUE * MetData.Radn * cover_green * (1 - OPCover) * FW;

        }

        /// <summary>Does the water balance.</summary>
        private void DoWaterBalance()
        {
            double OPCover = (double)Apsim.Get(this, "OilPalm.cover_green");
            
            cover_green = 0.40 * (1 - OPCover);
            PEP = SoilWat.Eo * cover_green * (1 - OPCover);


            for (int j = 0; j < SoilWat.Thickness.Length; j++)
                PotSWUptake[j] = Math.Max(0.0, RootProportion(j, RootDepth) * kl * (SoilWat.SWmm[j] - SoilWat.LL15mm[j]));

            double TotPotSWUptake = MathUtilities.Sum(PotSWUptake);

            EP = 0.0;
            for (int j = 0; j < SoilWat.Thickness.Length; j++)
            {
                SWUptake[j] = PotSWUptake[j] * Math.Min(1.0, PEP / TotPotSWUptake);
                EP += SWUptake[j];
                SoilWat.SWmm[j] = SoilWat.SWmm[j] - SWUptake[j];

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

        /// <summary>Does the n balance.</summary>
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
                PotNUptake[j] = Math.Max(0.0, RootProportion(j, RootDepth) * SoilN.NO3[j]);
            }

            double TotPotNUptake = MathUtilities.Sum(PotNUptake);
            double Fr = Math.Min(1.0, (Ndemand - NFixation) / TotPotNUptake);

            for (int j = 0; j < SoilWat.Thickness.Length; j++)
            {
                NUptake[j] = PotNUptake[j] * Fr;
                NUptakeType.DeltaNO3[j] = -NUptake[j];
            }

            if (NitrogenChanged != null)
                NitrogenChanged.Invoke(NUptakeType);
        }




        /// <summary>Roots the proportion.</summary>
        /// <param name="layer">The layer.</param>
        /// <param name="root_depth">The root_depth.</param>
        /// <returns></returns>
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
        /// <summary>Layers the index.</summary>
        /// <param name="depth">The depth.</param>
        /// <returns></returns>
        /// <exception cref="System.Exception">Depth deeper than bottom of soil profile</exception>
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