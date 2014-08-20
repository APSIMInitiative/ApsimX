using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;

using System.Reflection;
using System.Collections;
using Models.PMF.Functions;
using Models.Soils;
using System.Xml.Serialization;
using Models.PMF;


namespace Models.PMF.Slurp
{
    /// <summary>
    /// Slurp is a 'dummy' static crop model.  The user sets very basic input information such as ....  These states will
    /// not change during the simulation (no growth or death) unless the states are reset by the user.  
    /// 
    /// Need to check canopy height and depth units.  Micromet documentation says m but looks like is in mm in the module
    /// 
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class Slurp : Model, ICrop2
    {
        // Deleted list - keep for a bit
        //public string plant_status = "out";
        [Description("Leaf Mass - this will soon be deleted")]         public double LeafMass { get; set; }
        //public event EventHandler StartSlurp;
        //public event NewCanopyDelegate NewCanopy;
        private double PEP;
        private double EP;
        private double FW;
        private double FWexpan;
        private double RootMass = 0.0;
        private double RootN;
        private double[] NUptake = null;
        private double[] PotSWUptake = null;
        private double[] SWUptake;
        private double[] PotNUptake = null;
        private double[] bd = null;
        private double RootNConcentration = 0.0;
        private double KNO3 = 0.0;
        private double[] kl;

        
        /// <summary>
        /// Link to the soil module
        /// </summary>
        [Link] Soils.Soil Soil = null;

        // The variables that are in CanopyProperties
        /// <summary>
        /// Holds the set of crop canopy properties that is used by Arbitrator for light and engergy calculations
        /// </summary>
        public CanopyProperties CanopyProperties { get { return LocalCanopyData; } }
        CanopyProperties LocalCanopyData = new CanopyProperties();

        [Description("Green LAI (m2/m2)")] public double localLAI { get; set; }
        [Description("Total LAI (m2/m2)")] public double localLAItot { get; set; }
        [Description("Green cover (m2/m2)")] public double localCoverGreen { get; set; }
        [Description("Total cover (m2/m2)")] public double localCoverTot { get; set; }
        [Description("Height of the canopy (mm)")] public double localCanopyHeight { get; set; }
        [Description("Depth of the canopy (mm)")] public double localCanopyDepth { get; set; }
        [Description("Maximum stomatal conductance (m/s)")] public double localMaximumStomatalConductance { get; set; }
        [Description("Frgr - effect on stomatal conductance (-)")] public double localFrgr;
        

        // The variables that in RootProperties
        /// <summary>
        /// Holds the set of crop root properties that is used by Arbitrator for water and nutrient calculations
        /// </summary>
        public RootProperties RootProperties { get { return LocalRootData; } }
        RootProperties LocalRootData = new RootProperties();

        [Description("Rooting Depth (mm)")] public double localRootDepth { get; set; }
        [Description("Root length density at the soil surface (mm/mm3)")] public double localSurfaceRootLengthDensity { get; set; }

        public double[] localRootExplorationByLayer { get; set; }
        public double[] localRootLengthDensityByVolume { get; set; }

        private double Ndemand = 0.0;  // wehre does this sit?
        double tempDepthUpper;
        double tempDepthMiddle;
        double tempDepthLower;

        
        // The following event handler will be called once at the beginning of the simulation
        public override void  OnSimulationCommencing()
        {
            CanopyProperties.CoverGreen = localCoverGreen;
            CanopyProperties.CoverTot = localCoverTot;
            CanopyProperties.CanopyDepth = localCanopyDepth;
            CanopyProperties.CanopyHeight = localCanopyHeight;
            CanopyProperties.LAI = localLAI;
            CanopyProperties.LAItot = localLAItot;
            CanopyProperties.MaximumStomatalConductance = localMaximumStomatalConductance;
            CanopyProperties.Frgr = localFrgr;

            RootProperties.KL = Soil.KL(Name);
            RootProperties.LowerLimitDep = Soil.LL(Name);
            RootProperties.RootDepth = localRootDepth;

            localRootExplorationByLayer = new double[Soil.SoilWater.dlayer.Length];
            localRootLengthDensityByVolume = new double[Soil.SoilWater.dlayer.Length];

            tempDepthUpper = 0.0;
            tempDepthMiddle = 0.0;
            tempDepthLower = 0.0;

            for (int i = 0; i < Soil.SoilWater.dlayer.Length; i++)
            {
                tempDepthLower += Soil.SoilWater.dlayer[i];  // increment soil depth thorugh the layers
                tempDepthMiddle = tempDepthLower - Soil.SoilWater.dlayer[i]*0.5;
                tempDepthUpper = tempDepthLower - Soil.SoilWater.dlayer[i];
                if (tempDepthUpper < localRootDepth)        // set the root exploration
                {
                    localRootExplorationByLayer[i] = 1.0;
                }
                else if (tempDepthLower <= localRootDepth)
                {
                    localRootExplorationByLayer[i] = Utility.Math.Divide(localRootDepth - tempDepthUpper, Soil.SoilWater.dlayer[i], 0.0);
                }
                else
                {
                    localRootExplorationByLayer[i] = 0.0;
                }
                // set a triangular root length density by scaling layer depth against maximum rooting depth, constrain the multiplier between 0 and 1
                localRootLengthDensityByVolume[i] = localSurfaceRootLengthDensity * localRootExplorationByLayer[i] * (1.0 - Utility.Math.Constrain(Utility.Math.Divide(tempDepthMiddle, localRootDepth, 0.0), 0.0, 1.0));
            }
            RootProperties.RootExplorationByLayer = localRootExplorationByLayer;
            RootProperties.RootLengthDensityByVolume = localRootLengthDensityByVolume;


            /*
            kl = new double[Soil.SoilWater.sw_dep.Length];
            PotSWUptake = new double[kl.Length];
            PotNUptake = new double[kl.Length];
            SWUptake = new double[kl.Length];
            NUptake = new double[kl.Length];
            for (int i = 0; i < kl.Length; i++)
                kl[i] = 0.5;

            bd = (double[])Soil.Water.Get("BD");

             */
            // Invoke a sowing event. Needed for MicroClimate
            //if (StartSlurp != null)
            //    StartSlurp.Invoke(this, new EventArgs());

            //Send a NewCanopy event to MicroClimate
            //NewCanopyType LocalCanopyData = new NewCanopyType();
            //LocalCanopyData.cover = CoverGreen;
            //LocalCanopyData.cover_tot = CoverTot;
            //LocalCanopyData.depth = Depth;
            //LocalCanopyData.height = Height;
            //LocalCanopyData.lai = LAI;
            //LocalCanopyData.lai_tot = LAItot;
            //LocalCanopyData.sender = "Slurp";
            //if (NewCanopy != null)
            //    NewCanopy.Invoke(LocalCanopyData);
        }

        /// <summary>
        /// MicroClimate needs FRGR
        /// </summary>
        //public double FRGR { get { return 1; } }

        /// <summary>
        /// MicroClimate supplies PotentialEP
        /// </summary>
        [XmlIgnore]
        public double PotentialEP { get; set; }

        /// <summary>
        /// Arbitrator supplies ActualEP
        /// </summary>
        [XmlIgnore]
        public double ActualEP { get; set; }

        /// <summary>
        /// MicroClimate supplies LightProfile
        /// </summary>
        //[XmlIgnore]
        //public CanopyEnergyBalanceInterceptionlayerType[] LightProfile { get; set; }

        //[EventSubscribe("DoPlantGrowth")]
        //private void OnDoPlantGrowth(object sender, EventArgs e)
        //{
        //    DoWaterBalance();
        //    DoNBalance();
       // }


        /*
        private void DoWaterBalance()
        {
            PEP = Soil.SoilWater.eo * localCoverGreen;

            for (int j = 0; j < Soil.SoilWater.ll15_dep.Length; j++)
                PotSWUptake[j] = Math.Max(0.0, RootProportion(j, localRootDepth) * kl[j] * (Soil.SoilWater.sw_dep[j] - Soil.SoilWater.ll15_dep[j]));

            double TotPotSWUptake = Utility.Math.Sum(PotSWUptake);

            EP = 0.0;
            for (int j = 0; j < Soil.SoilWater.ll15_dep.Length; j++)
            {
                SWUptake[j] = PotSWUptake[j] * Math.Min(1.0, PEP / TotPotSWUptake);
                EP += SWUptake[j];
                Soil.SoilWater.sw_dep[j] = Soil.SoilWater.sw_dep[j] - SWUptake[j];

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


            for (int j = 0; j < Soil.SoilWater.ll15_dep.Length; j++)
            {
                double swaf = 0;
                swaf = (Soil.SoilWater.sw_dep[j] - Soil.SoilWater.ll15_dep[j]) / (Soil.SoilWater.dul_dep[j] - Soil.SoilWater.ll15_dep[j]);
                swaf = Math.Max(0.0, Math.Min(swaf, 1.0));
                double no3ppm = Soil.SoilNitrogen.no3[j] * (100.0 / (bd[j] * Soil.SoilWater.dlayer[j]));
                PotNUptake[j] = Math.Max(0.0, RootProportion(j, localRootDepth) * KNO3 * Soil.SoilNitrogen.no3[j] * swaf);
            }

            double TotPotNUptake = Utility.Math.Sum(PotNUptake);
            double Fr = Math.Min(1.0, Ndemand / TotPotNUptake);

            for (int j = 0; j < Soil.SoilWater.ll15_dep.Length; j++)
            {
                NUptake[j] = PotNUptake[j] * Fr;
                Soil.SoilNitrogen.no3[j] = Soil.SoilNitrogen.no3[j] - NUptake[j];
            }

            Fr = Math.Min(1.0, Math.Max(0, Utility.Math.Sum(NUptake) / LeafNDemand));
            double DeltaLeafN = LeafNDemand * Fr;

            LeafN += Math.Max(0.0, LeafMass * LeafNConc / 100.0 - LeafN) * Fr;

            // Calculate fraction of N demand for Vegetative Parts
            if ((Ndemand - DeltaLeafN) > 0)
                Fr = Math.Max(0.0, ((Utility.Math.Sum(NUptake) - DeltaLeafN) / (Ndemand - DeltaLeafN)));
            else
                Fr = 0.0;

            double[] RootNDef = new double[Soil.SoilWater.ll15_dep.Length];
            double TotNDef = 1e-20;
            for (int j = 0; j < Soil.SoilWater.ll15_dep.Length; j++)
            {
                RootNDef[j] = Math.Max(0.0, RootMass * RootNConcentration / 100.0 - RootN);
                TotNDef += RootNDef[j];
            }
            for (int j = 0; j < Soil.SoilWater.ll15_dep.Length; j++)
                RootN += RootNDemand / 10 * Fr * RootNDef[j] / TotNDef;

            double EndN = PlantN;
            double Change = EndN - StartN;
            double Uptake = Utility.Math.Sum(NUptake) / 10.0;
            if (Math.Abs(Change - Uptake) > 0.001)
                throw new Exception("Error in N Allocation");
        }
        */

        // from here
        /*
        private double RootProportion(int layer, double root_depth)
        {
            double depth_to_layer_bottom = 0;   // depth to bottom of layer (mm)
            double depth_to_layer_top = 0;      // depth to top of layer (mm)
            double depth_to_root = 0;           // depth to root in layer (mm)
            double depth_of_root_in_layer = 0;  // depth of root within layer (mm)
            // Implementation Section ----------------------------------
            for (int i = 0; i <= layer; i++)
                depth_to_layer_bottom += Soil.SoilWater.dlayer[i];
            depth_to_layer_top = depth_to_layer_bottom - Soil.SoilWater.dlayer[layer];
            depth_to_root = Math.Min(depth_to_layer_bottom, root_depth);
            depth_of_root_in_layer = Math.Max(0.0, depth_to_root - depth_to_layer_top);

            return depth_of_root_in_layer / Soil.SoilWater.dlayer[layer];
        }

        private int LayerIndex(double depth)
        {
            double CumDepth = 0;
            for (int i = 0; i < Soil.SoilWater.dlayer.Length; i++)
            {
                CumDepth = CumDepth + Soil.SoilWater.dlayer[i];
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
                return localLAI * 1;
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
        // from here
        */
    }   
}