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
        [Description("Leaf Mass")]
        public double LeafMass { get; set; }
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
        
        /// <summary>
        /// Link to the soil module
        /// </summary>
        [Link]
        Soils.Soil Soil = null;

        // The variables that are in CanopyProperties

        /// <summary>
        /// Holds the set of crop canopy properties that is used by Arbitrator for light and engergy calculations
        /// </summary>
        public CanopyProperties CanopyProperties { get { return LocalCanopyData; } }
        CanopyProperties LocalCanopyData = new CanopyProperties();

        /// <summary>
        /// Crop type was used to assign generic types of properties (e.g. maximum stomatal conductance) to crops
        /// Probably not needed now as the crops will have to supply these themselves
        /// ???? delete ????
        /// </summary>
        public string CropType { get { return "Slurp"; } }


        /// <summary>
        /// The name as it appears in the GUI e.g. "Wheat3" 
        /// ???? How is this got?????
        /// </summary>
        //public string Name { get { return "Slurp"; } } this does nto work

        /// <summary>
        /// Greem leaf area index (m2/m2) 
        /// Used in the light and energy arbitration
        /// Set from the interface and will not change unless reset
        /// </summary>
        [Description("LAI")] public double LAI { get; set; }

        /// <summary>
        /// Total (includes dead) leaf area index (m2/m2) 
        /// Used in the light and energy arbitration
        /// Set from the interface and will not change unless reset
        /// </summary>
        [Description("Total LAI")]
        public double LAItot { get; set; }

        /// <summary>
        /// Green cover (m2/m2) - fractional cover resulting from the assigned LAI, 
        /// Calculate this using an assumed light interception coefficient
        /// Used in the light and energy arbitration
        /// Set from the interface and will not change unless reset
        /// </summary>
        [Description("Cover Green")]
        public double CoverGreen { get; set; }

        /// <summary>
        /// Total (green and dead) cover (m2/m2) - fractional cover resulting from the assigned LAItot, 
        /// Calculate this using an assumed light interception coefficient
        /// Used in the light and energy arbitration
        /// Set from the interface and will not change unless reset
        /// </summary>
        [Description("Total Cover")]
        public double CoverTot { get; set; }

        /// <summary>
        /// Height to the top of the canopy (mm) 
        /// Used in the light and energy arbitration
        /// Set from the interface and will not change unless reset
        /// </summary>
        [Description("Canopy Height")]
        public double Height { get; set; }

        /// <summary>
        /// Depth of the canopy (mm).  If the canopy is continuous from the ground to the top of the canopy then 
        /// the depth = height, otherwise depth must be less than the height
        /// Used in the light and energy arbitration
        /// Set from the interface and will not change unless reset
        /// </summary>
        [Description("Canopy Depth")]
        public double Depth { get; set; }


        // The variables that in RootProperties

        /// <summary>
        /// Holds the set of crop root properties that is used by Arbitrator for water and nutrient calculations
        /// </summary>
        public RootProperties RootProperties { get { return LocalRootData; } }
        RootProperties LocalRootData = new RootProperties();

        /// <summary>
        /// Depth of the root system (mm).  
        /// Used in the water and nutrient arbitration
        /// Set from the interface and will not change unless reset
        /// </summary>
        [Description("Root Depth")]
        public double RootDepth { get; set; }

        /// <summary>
        /// The bastardised Passioura/Monteith K*L (/day)
        /// At some point this will be replaced by one soil property and the root length density.
        /// Used in the water and nutrient arbitration
        /// Set from the interface and will not change unless reset
        /// </summary>
        [Description("kl")]
        public double[] kl { get; set; }


        private double Ndemand = 0.0;  // wehre does this sit?

        
        // The following event handler will be called once at the beginning of the simulation
        public override void  OnSimulationCommencing()
        {
            RootProperties.KL = Soil.KL("slurp");
            RootProperties.LLDep = Soil.LL("slurp");
            RootProperties.RootDepth = RootDepth;
            RootProperties.RootExplorationByLayer= new double[] {1.0,1.0,0.5,0.0};
            RootProperties.RootLengthDensityByVolume = new double[] { 0.05, 0.03, 0.0058, 0.0 };

            CanopyProperties.cover = CoverGreen;
            CanopyProperties.cover_tot = CoverTot;
            CanopyProperties.CropType = CropType;
            CanopyProperties.depth = Depth;
            CanopyProperties.height = Height;
            CanopyProperties.lai = LAI;
            CanopyProperties.lai_tot = LAItot;
            CanopyProperties.MaximumStomatalConductance = 0.010;

            kl = new double[Soil.SoilWater.sw_dep.Length];
            PotSWUptake = new double[kl.Length];
            PotNUptake = new double[kl.Length];
            SWUptake = new double[kl.Length];
            NUptake = new double[kl.Length];
            for (int i = 0; i < kl.Length; i++)
                kl[i] = 0.5;

            bd = (double[])Soil.Water.Get("BD");
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
        public double FRGR { get { return 1; } }

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
        [XmlIgnore]
        public CanopyEnergyBalanceInterceptionlayerType[] LightProfile { get; set; }

        [EventSubscribe("DoPlantGrowth")]
        private void OnDoPlantGrowth(object sender, EventArgs e)
        {
            DoWaterBalance();
            DoNBalance();
        }

        private void DoWaterBalance()
        {
            PEP = Soil.SoilWater.eo * CoverGreen;

            for (int j = 0; j < Soil.SoilWater.ll15_dep.Length; j++)
                PotSWUptake[j] = Math.Max(0.0, RootProportion(j, RootDepth) * kl[j] * (Soil.SoilWater.sw_dep[j] - Soil.SoilWater.ll15_dep[j]));

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
                PotNUptake[j] = Math.Max(0.0, RootProportion(j, RootDepth) * KNO3 * Soil.SoilNitrogen.no3[j] * swaf);
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