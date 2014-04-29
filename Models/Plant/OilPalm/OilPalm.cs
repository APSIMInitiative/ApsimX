using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;

using System.Reflection;
using System.Collections;
using Models.PMF.Functions;
using Models.Soils;


namespace Models.PMF.OilPalm
{
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class OilPalm: ModelCollection, ICrop
    {
        
        public string plant_status = "out";
        [Link]
        Clock Clock = null;
        [Link]
        WeatherFile MetData = null;
        [Link]
        Soils.Soil Soil = null;
        [Link]
        Soils.SoilWater SoilWat = null;
        [Link]
        Soils.SoilNitrogen SoilN = null;
        [Link]
        ISummary Summary = null;

        
        public string Crop_Type = "";

        public double height = 0.0;

        public double cover_tot = 0.0;
        
        double interception = 0.0;

        public double UnderstoryCoverMax = 0.0;
        public double UnderstoryLegumeFraction = 0;

        double Ndemand = 0.0;
        
        double RootDepth = 0.0;
        public double InitialRootDepth = 1.0;
        public double MaximumRootDepth = 10000.0;
        public double[] kl;
        public double[] ll;
        public double[] xf;
        public double InterceptionFraction = 0.0;
        public double[] bd = null;
        
        double[] PotSWUptake;
        
        double[] SWUptake;
        
        double PEP = 0.0;
        
        double EP = 0.0;
        
        double DltDM = 0.0;
        double Excess = 0.0;
        
        double FW = 0.0;
        
        double FWexpan = 0.0;
        
        double Fn = 1.0;
        
        double CumulativeFrondNumber = 0.0;
        
        double CumulativeBunchNumber = 0.0;
        
        double CumulativeYield = 0.0;
        
        double ReproductiveGrowthFraction = 0.0;

        public double CarbonStress { get; set; }
        
        double HarvestBunches = 0.0;
        
        double HarvestYield = 0.0;
        
        double HarvestFFB = 0.0;
        
        double HarvestBunchSize = 0.0;

        public double Age { get; set; }

        public double Population { get; set; }
        public SowPlant2Type SowingData = new SowPlant2Type();

        double[] PotNUptake;
        double[] NUptake;
        double StemGrowth = 0.0;
        double FrondGrowth = 0.0;
        double RootGrowth = 0.0;

        //FrondType[] Frond;
        public List<FrondType> Fronds = new List<FrondType>();
        public List<BunchType> Bunches = new List<BunchType>();
        public List<RootType> Roots = new List<RootType>();

        //Component MySoilWat;
        //Component MySoilN;


        [Link] Function  FrondAppRate = null;
        [Link] Function  RelativeDevelopmentalRate = null;
        [Link] Function  FrondMaxArea = null;
        [Link] Function  DirectExtinctionCoeff = null;
        [Link] Function  DiffuseExtinctionCoeff = null;
        [Link] Function  ExpandingFronds = null;
        [Link] Function  InitialFrondNumber = null;
        [Link] Function  RUE = null;
        [Link] Function  RootFrontVelocity = null;
        [Link] Function  RootSenescenceRate = null;
        [Link] Function  SpecificLeafArea = null;
        [Link] Function  SpecificLeafAreaMax = null;
        [Link] Function  RootFraction = null;
        [Link] Function  BunchSizeMax = null;
        [Link] Function  FemaleFlowerFraction = null;
        [Link] Function  FFFStressImpact = null;
        [Link] Function  StemToFrondFraction = null;
        [Link] Function  FlowerAbortionFraction = null;
        [Link] Function  BunchFailureFraction = null;
        [Link] Function  KNO3 = null;
        [Link] Function  StemNConcentration = null;
        [Link] Function  BunchNConcentration = null;
        [Link] Function  RootNConcentration = null;
        [Link] Function  BunchOilConversionFactor = null;
        [Link] Function  RipeBunchWaterContent = null;
        [Link] Function  HarvestFrondNumber = null;
        [Link] Function  FrondMaximumNConcentration = null;
        [Link] Function  FrondCriticalNConcentration = null;
        [Link] Function  FrondMinimumNConcentration = null;
        
        public double UnderstoryCoverGreen = 0;
        private double UnderstoryKL = 0.04;
        
        double[] UnderstoryPotSWUptake;
        
        double[] UnderstorySWUptake;
        
        double[] UnderstoryPotNUptake;
        
        double[] UnderstoryNUptake;
        
        public double UnderstoryRootDepth = 0;
        
        public double UnderstoryPEP = 0;
        
        public double UnderstoryEP = 0;
        
        public double UnderstoryFW = 0;
        
        public double UnderstoryDltDM = 0;
        
        public double UnderstoryNFixation = 0;

        public class RootType
        {
            public double Mass = 0;
            public double N = 0;
            public double Length = 0;
        }

        public class FrondType
        {
            public double Mass; // g/frond
            public double N;    // g/frond
            public double Area; // m2/frond
            public double Age;  //days
        }
        public class BunchType
        {
            public double Mass = 0;
            public double N = 0;
            public double Age = 0;
            public double FemaleFraction = 1;
        }

        
        double StemMass = 0.0;
        
        double StemN = 0.0;
        
        double StemNConc
        {
            get
            {
                if (StemMass > 0)
                    return StemN / StemMass * 100;
                else
                    return 0.0;
            }
        }

        // The following event handler will be called once at the beginning of the simulation
        public override void OnCommencing()
        {
            //MyPaddock.Parent.ChildPaddocks
            PotSWUptake = new double[SoilWat.ll15_dep.Length];
            SWUptake = new double[SoilWat.ll15_dep.Length];
            PotNUptake = new double[SoilWat.ll15_dep.Length];
            NUptake = new double[SoilWat.ll15_dep.Length];

            UnderstoryPotSWUptake = new double[SoilWat.ll15_dep.Length];
            UnderstorySWUptake = new double[SoilWat.ll15_dep.Length];
            UnderstoryPotNUptake = new double[SoilWat.ll15_dep.Length];
            UnderstoryNUptake = new double[SoilWat.ll15_dep.Length];

            for (int i = 0; i < SoilWat.ll15_dep.Length; i++)
            {
                RootType R = new RootType();
                Roots.Add(R);
                Roots[i].Mass = 0.1;
                Roots[i].N = Roots[i].Mass * RootNConcentration.Value / 100;
            }

            for (int i = 0; i < (int)InitialFrondNumber.Value; i++)
            {
                FrondType F = new FrondType();
                F.Age = ((int)InitialFrondNumber.Value - i) * FrondAppRate.Value;
                F.Area = SizeFunction(F.Age);
                F.Mass = F.Area / SpecificLeafArea.Value;
                F.N = F.Mass * FrondCriticalNConcentration.Value / 100.0;
                Fronds.Add(F);
                CumulativeFrondNumber += 1;
            }
            for (int i = 0; i < (int)InitialFrondNumber.Value + 60; i++)
            {
                BunchType B = new BunchType();
                B.FemaleFraction = FemaleFlowerFraction.Value;
                Bunches.Add(B);
            }


            RootDepth = InitialRootDepth;


        }

        public void Sow(string Cultivar, double Population, double Depth = 100, double RowSpacing = 150, double MaxCover = 1, double BudNumber = 1, string CropClass = "Plant")
        {
            SowingData = new SowPlant2Type();
            SowingData.Population = Population;
            this.Population = Population;
            SowingData.Depth = Depth;
            SowingData.Cultivar = Cultivar;
            SowingData.MaxCover = MaxCover;
            SowingData.BudNumber = BudNumber;
            SowingData.RowSpacing = RowSpacing;
            SowingData.CropClass = CropClass;

            // Invoke a sowing event.
            if (Sowing != null)
                Sowing.Invoke();

            Summary.WriteMessage(FullPath, string.Format("A crop of OilPalm was sown today at a population of " + Population + " plants/m2 with " + BudNumber + " buds per plant at a row spacing of " + RowSpacing + " and a depth of " + Depth + " mm"));
        }

        /// <summary>
        /// Harvest the crop.
        /// </summary>
        public void Harvest()
        {
            // Invoke a harvesting event.
            if (Harvesting != null)
                Harvesting.Invoke();
        }
        
        public event NewCropDelegate NewCrop;
        
        public event NullTypeDelegate Sowing;
        
        public event NullTypeDelegate Harvesting;
        
        public event FOMLayerDelegate IncorpFOM;
        
        public event BiomassRemovedDelegate BiomassRemoved;

        [EventSubscribe("Sow")]
        private void OnSow(SowPlant2Type Sow)
        {
            SowingData = Sow;
            plant_status = "alive";
            Population = SowingData.Population;

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


        // The following event handler will be called each day at the beginning of the day
        [EventSubscribe("StartOfDay")]
        private void OnPrepare(object sender, EventArgs e)
        {
            interception = MetData.Rain * InterceptionFraction;
        }

        [EventSubscribe("MiddleOfDay")]
        private void OnProcess(object sender, EventArgs e)
        {

            DoWaterBalance();
            DoGrowth();
            DoNBalance();
            DoDevelopment();
            DoFlowerAbortion();
            DoGenderDetermination();
            DoUnderstory();

        }

        private void DoFlowerAbortion()
        {
            // Main abortion stage occurs around frond 11 over 3 plastochrons

            int B = Fronds.Count - 11;
            if (B > 0)
            {
                double AF = (1 - FlowerAbortionFraction.Value);
                Bunches[B - 1].FemaleFraction *= AF;
                Bunches[B].FemaleFraction *= AF;
                Bunches[B + 1].FemaleFraction *= AF;
            }

            // Bunch failure stage occurs around frond 21 over 1 plastochron
            B = Fronds.Count - 21;
            if (B > 0)
            {
                double BFF = (1 - BunchFailureFraction.Value);
                Bunches[B].FemaleFraction *= BFF;
            }

        }

        private void DoGenderDetermination()
        {
            // Main abortion stage occurs 25 plastochroons before spear leaf over 9 plastochrons
            // NH Try 20 as this allows for 26 per year and harvest at 32 - ie 26*2 - 32
            int B = 53; //Fronds.Count + 20;
            Bunches[B - 4].FemaleFraction *= (1.0 - FFFStressImpact.Value);
            Bunches[B - 3].FemaleFraction *= (1.0 - FFFStressImpact.Value);
            Bunches[B - 2].FemaleFraction *= (1.0 - FFFStressImpact.Value);
            Bunches[B - 1].FemaleFraction *= (1.0 - FFFStressImpact.Value);
            Bunches[B + 0].FemaleFraction *= (1.0 - FFFStressImpact.Value);
            Bunches[B + 1].FemaleFraction *= (1.0 - FFFStressImpact.Value);
            Bunches[B + 2].FemaleFraction *= (1.0 - FFFStressImpact.Value);
            Bunches[B + 3].FemaleFraction *= (1.0 - FFFStressImpact.Value);
            Bunches[B + 4].FemaleFraction *= (1.0 - FFFStressImpact.Value);


        }
        private void DoRootGrowth(double Allocation)
        {
            int RootLayer = LayerIndex(RootDepth);
            RootDepth = RootDepth + RootFrontVelocity.Value * Soil.XF("OilPalm")[RootLayer];
            RootDepth = Math.Min(MaximumRootDepth, RootDepth);
            RootDepth = Math.Min(Utility.Math.Sum(SoilWat.dlayer), RootDepth);

            // Calculate Root Activity Values for water and nitrogen
            double[] RAw = new double[SoilWat.dlayer.Length];
            double[] RAn = new double[SoilWat.dlayer.Length];
            double TotalRAw = 0;
            double TotalRAn = 0;

            for (int layer = 0; layer < SoilWat.dlayer.Length; layer++)
            {
                if (layer <= LayerIndex(RootDepth))
                    if (Roots[layer].Mass > 0)
                    {
                        RAw[layer] = SWUptake[layer] / Roots[layer].Mass
                                   * SoilWat.dlayer[layer]
                                   * RootProportion(layer, RootDepth);
                        RAw[layer] = Math.Max(RAw[layer], 1e-20);  // Make sure small numbers to avoid lack of info for partitioning

                        RAn[layer] = NUptake[layer] / Roots[layer].Mass
                                   * SoilWat.dlayer[layer]
                                   * RootProportion(layer, RootDepth);
                        RAn[layer] = Math.Max(RAw[layer], 1e-10);  // Make sure small numbers to avoid lack of info for partitioning

                    }
                    else if (layer > 0)
                    {
                        RAw[layer] = RAw[layer - 1];
                        RAn[layer] = RAn[layer - 1];
                    }
                    else
                    {
                        RAw[layer] = 0;
                        RAn[layer] = 0;
                    }
                TotalRAw += RAw[layer];
                TotalRAn += RAn[layer];
            }
            double allocated = 0;
            for (int layer = 0; layer < SoilWat.dlayer.Length; layer++)
            {
                if (TotalRAw > 0)

                    Roots[layer].Mass += Allocation * RAw[layer] / TotalRAw;
                else if (Allocation > 0)
                    throw new Exception("Error trying to partition root biomass");
                allocated += Allocation * RAw[layer] / TotalRAw;
            }



            // Do Root Senescence
            FOMLayerLayerType[] FOMLayers = new FOMLayerLayerType[SoilWat.dlayer.Length];

            for (int layer = 0; layer < SoilWat.dlayer.Length; layer++)
            {
                double Fr = RootSenescenceRate.Value;
                double DM = Roots[layer].Mass * Fr * 10.0;
                double N = Roots[layer].N * Fr * 10.0;
                Roots[layer].Mass *= (1.0 - Fr);
                Roots[layer].N *= (1.0 - Fr);
                Roots[layer].Length *= (1.0 - Fr);


                FOMType fom = new FOMType();
                fom.amount = (float)DM;
                fom.N = (float)N;
                fom.C = (float)(0.44 * DM);
                fom.P = 0;
                fom.AshAlk = 0;

                FOMLayerLayerType Layer = new FOMLayerLayerType();
                Layer.FOM = fom;
                Layer.CNR = 0;
                Layer.LabileP = 0;

                FOMLayers[layer] = Layer;
            }
            FOMLayerType FomLayer = new FOMLayerType();
            FomLayer.Type = Crop_Type;
            FomLayer.Layer = FOMLayers;
            IncorpFOM.Invoke(FomLayer);


        }
        private void DoGrowth()
        {
            double RUEclear = RUE.Value;
            double RUEcloud = RUE.Value * (1 + 0.33 * cover_green);
            double WF = DiffuseLightFraction;
            double RUEadj = WF * WF * RUEcloud + (1 - WF * WF) * RUEclear;
            DltDM = RUEadj * Fn * MetData.Radn * cover_green * FW;

            double DMAvailable = DltDM;

            RootGrowth = (DltDM * RootFraction.Value);
            DMAvailable -= RootGrowth;
            DoRootGrowth(RootGrowth);

            double[] BunchDMD = new double[Bunches.Count];
            for (int i = 0; i < 6; i++)
                BunchDMD[i] = BunchSizeMax.Value / (6 * FrondAppRate.Value / DeltaT) * Fn * Population * Bunches[i].FemaleFraction * BunchOilConversionFactor.Value;
            double TotBunchDMD = Utility.Math.Sum(BunchDMD);

            double[] FrondDMD = new double[Fronds.Count];
            for (int i = 0; i < Fronds.Count; i++)
                FrondDMD[i] = (SizeFunction(Fronds[i].Age + DeltaT) - SizeFunction(Fronds[i].Age)) / SpecificLeafArea.Value * Population * Fn;
            double TotFrondDMD = Utility.Math.Sum(FrondDMD);

            //double StemDMD = DMAvailable * StemToFrondFraction.Value;
            double StemDMD = TotFrondDMD * StemToFrondFraction.Value;

            double Fr = Math.Min(DMAvailable / (TotBunchDMD + TotFrondDMD + StemDMD), 1.0);
            Excess = 0.0;
            if (Fr > 1.0)
                Excess = DMAvailable - (TotBunchDMD + TotFrondDMD + StemDMD);


            if (Age > 10 && Fr < 1)
            { }

            for (int i = 0; i < 6; i++)
                Bunches[i].Mass += BunchDMD[i] * Fr / Population / BunchOilConversionFactor.Value;
            if (DltDM > 0)
                ReproductiveGrowthFraction = TotBunchDMD * Fr / DltDM;
            else
                ReproductiveGrowthFraction = 0;

            for (int i = 0; i < Fronds.Count; i++)
            {
                FrondGrowth = FrondDMD[i] * Fr / Population;
                Fronds[i].Mass += FrondGrowth;
                if (Fr >= SpecificLeafArea.Value / SpecificLeafAreaMax.Value)
                    Fronds[i].Area += (SizeFunction(Fronds[i].Age + DeltaT) - SizeFunction(Fronds[i].Age)) * Fn;
                else
                    Fronds[i].Area += FrondGrowth * SpecificLeafAreaMax.Value;

            }

            StemGrowth = StemDMD * Fr;// +Excess; 
            StemMass += StemGrowth;

            CarbonStress = Fr;

        }
        private void DoDevelopment()
        {
            Age = Age + 1.0 / 365.0;
            //for (int i = 0; i < Frond.Length; i++)
            //    Frond[i].Age += 1;
            foreach (FrondType F in Fronds)
            {
                F.Age += DeltaT;
                //F.Area = SizeFunction(F.Age);
            }
            if (Fronds[Fronds.Count - 1].Age >= FrondAppRate.Value)
            {
                FrondType F = new FrondType();
                Fronds.Add(F);
                CumulativeFrondNumber += 1;

                BunchType B = new BunchType();
                B.FemaleFraction = FemaleFlowerFraction.Value;
                Bunches.Add(B);
            }

            //if (Fronds[0].Age >= (40 * FrondAppRate.Value))
            if (FrondNumber > Math.Round(HarvestFrondNumber.Value))
            {
                HarvestBunches = Bunches[0].FemaleFraction;
                HarvestYield = Bunches[0].Mass * Population / (1.0 - RipeBunchWaterContent.Value);
                HarvestFFB = HarvestYield / 100;
                HarvestBunchSize = Bunches[0].Mass / (1.0 - RipeBunchWaterContent.Value) / Bunches[0].FemaleFraction;
                if (Harvesting != null)
                    Harvesting.Invoke();
                // Now rezero these outputs - they can only be output non-zero on harvesting event.
                HarvestBunches = 0.0;
                HarvestYield = 0.0;
                HarvestFFB = 0.0;
                HarvestBunchSize = 0.0;


                CumulativeBunchNumber += Bunches[0].FemaleFraction;
                CumulativeYield += Bunches[0].Mass * Population / (1.0 - RipeBunchWaterContent.Value);
                Bunches.RemoveAt(0);

                BiomassRemovedType BiomassRemovedData = new BiomassRemovedType();
                BiomassRemovedData.crop_type = Crop_Type;
                BiomassRemovedData.dm_type = new string[1] { "frond" };
                BiomassRemovedData.dlt_crop_dm = new float[1] { (float)(Fronds[0].Mass * Population * 10) };
                BiomassRemovedData.dlt_dm_n = new float[1] { (float)(Fronds[0].N * Population * 10) };
                BiomassRemovedData.dlt_dm_p = new float[1] { 0 };
                BiomassRemovedData.fraction_to_residue = new float[1] { 1 };
                Fronds.RemoveAt(0);
                BiomassRemoved.Invoke(BiomassRemovedData);
            }
        }
        private void DoWaterBalance()
        {
            PEP = SoilWat.eo * cover_green;


            for (int j = 0; j < SoilWat.ll15_dep.Length; j++)
                PotSWUptake[j] = Math.Max(0.0, RootProportion(j, RootDepth) * Soil.KL("OilPalm")[j] * (SoilWat.sw_dep[j] - SoilWat.ll15_dep[j]));

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
                //FWexpan = Math.Max(0.0, Math.Min(1.0, (TotPotSWUptake / PEP - 0.5) / 0.6));
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

            double StemNDemand = StemGrowth * StemNConcentration.Value / 100.0 * 10.0;  // factor of 10 to convert g/m2 to kg/ha
            double RootNDemand = Math.Max(0.0, (RootMass * RootNConcentration.Value / 100.0 - RootN)) * 10.0;  // kg/ha
            double FrondNDemand = Math.Max(0.0, (FrondMass * FrondMaximumNConcentration.Value / 100.0 - FrondN)) * 10.0;  // kg/ha 
            double BunchNDemand = Math.Max(0.0, (BunchMass * BunchNConcentration.Value / 100.0 - BunchN)) * 10.0;  // kg/ha 

            Ndemand = StemNDemand + FrondNDemand + RootNDemand + BunchNDemand;  //kg/ha


            for (int j = 0; j < SoilWat.ll15_dep.Length; j++)
            {
                double swaf = 0;
                swaf = (SoilWat.sw_dep[j] - SoilWat.ll15_dep[j]) / (SoilWat.dul_dep[j] - SoilWat.ll15_dep[j]);
                swaf = Math.Max(0.0, Math.Min(swaf, 1.0));
                double no3ppm = SoilN.no3[j] * (100.0 / (Soil.BD[j] * SoilWat.dlayer[j]));
                PotNUptake[j] = Math.Max(0.0, RootProportion(j, RootDepth) * KNO3.Value * SoilN.no3[j] * swaf);
            }

            double TotPotNUptake = Utility.Math.Sum(PotNUptake);
            double Fr = Math.Min(1.0, Ndemand / TotPotNUptake);

            for (int j = 0; j < SoilWat.ll15_dep.Length; j++)
            {
                NUptake[j] = PotNUptake[j] * Fr;
                SoilN.no3[j] = SoilN.no3[j] - NUptake[j];
            }
           
            Fr = Math.Min(1.0, Math.Max(0, Utility.Math.Sum(NUptake) / BunchNDemand));
            double DeltaBunchN = BunchNDemand * Fr;

            double Tot = 0;
            foreach (BunchType B in Bunches)
            {
                Tot += Math.Max(0.0, B.Mass * BunchNConcentration.Value / 100.0 - B.N) * Fr / SowingData.Population;
                B.N += Math.Max(0.0, B.Mass * BunchNConcentration.Value / 100.0 - B.N) * Fr;
            }

            // Calculate fraction of N demand for Vegetative Parts
            if ((Ndemand - DeltaBunchN) > 0)
                Fr = Math.Max(0.0, ((Utility.Math.Sum(NUptake) - DeltaBunchN) / (Ndemand - DeltaBunchN)));
            else
                Fr = 0.0;

            StemN += StemNDemand / 10 * Fr;

            double[] RootNDef = new double[SoilWat.ll15_dep.Length];
            double TotNDef = 1e-20;
            for (int j = 0; j < SoilWat.ll15_dep.Length; j++)
            {
                RootNDef[j] = Math.Max(0.0, Roots[j].Mass * RootNConcentration.Value / 100.0 - Roots[j].N);
                TotNDef += RootNDef[j];
            }
            for (int j = 0; j < SoilWat.ll15_dep.Length; j++)
                Roots[j].N += RootNDemand / 10 * Fr * RootNDef[j] / TotNDef;

            foreach (FrondType F in Fronds)
                F.N += Math.Max(0.0, F.Mass * FrondMaximumNConcentration.Value / 100.0 - F.N) * Fr;

            double EndN = PlantN;
            double Change = EndN - StartN;
            double Uptake = Utility.Math.Sum(NUptake) / 10.0;
            if (Math.Abs(Change - Uptake) > 0.001)
                throw new Exception("Error in N Allocation");

            double Nact = FrondNConc;
            double Ncrit = FrondCriticalNConcentration.Value;
            double Nmin = FrondMinimumNConcentration.Value;
            Fn = Math.Min(Math.Max(0.0, (Nact - Nmin) / (Ncrit - Nmin)), 1.0);

        }



        
        public double LAI
        {
            get
            {
                double FrondArea = 0.0;

                //for (int i = 0; i < Frond.Length; i++)
                //   FrondArea = FrondArea + Frond[i].Area;
                foreach (FrondType F in Fronds)
                    FrondArea += F.Area;
                return FrondArea * SowingData.Population;
            }

        }
        
        public double FrondArea
        {
            get
            {
                double A = 0.0;

                foreach (FrondType F in Fronds)
                    A += F.Area;
                return A / Fronds.Count;
            }

        }
        
        public double Frond17Area
        {
            get
            {
                //note frond 17 is 18th frond because they ignore the spear leaf
                if (Fronds.Count > 18)
                    return Fronds[Fronds.Count - 18].Area;
                else
                    return 0;

            }

        }

        
        public double FrondMass
        {
            get
            {
                double FrondMass = 0.0;

                //for (int i = 0; i < Frond.Length; i++)
                //   FrondArea = FrondArea + Frond[i].Area;
                foreach (FrondType F in Fronds)
                    FrondMass += F.Mass;
                return FrondMass * Population;
            }

        }
        
        public double FrondN
        {
            get
            {
                double FrondN = 0.0;

                //for (int i = 0; i < Frond.Length; i++)
                //   FrondArea = FrondArea + Frond[i].Area;
                foreach (FrondType F in Fronds)
                    FrondN += F.N;
                return FrondN * SowingData.Population;
            }

        }
        
        public double FrondNConc
        {
            get
            {
                return FrondN / FrondMass * 100.0;
            }

        }
        
        public double BunchMass
        {
            get
            {
                double BunchMass = 0.0;

                foreach (BunchType B in Bunches)
                    BunchMass += B.Mass;
                return BunchMass * SowingData.Population;
            }

        }
        
        public double BunchN
        {
            get
            {
                double BunchN = 0.0;

                foreach (BunchType B in Bunches)
                    BunchN += B.N * SowingData.Population;
                return BunchN;
            }

        }
        
        public double BunchNConc
        {
            get
            {
                if (BunchMass > 0)
                    return BunchN / BunchMass * 100.0;
                else
                    return 0;
            }

        }

        
        public double RootMass
        {
            get
            {
                double RootMass = 0.0;

                foreach (RootType R in Roots)
                    RootMass += R.Mass;
                return RootMass;
            }

        }
        
        public double RootN
        {
            get
            {
                double RootN = 0.0;

                foreach (RootType R in Roots)
                    RootN += R.N;
                return RootN;
            }

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
                return FrondN + RootN + StemN + BunchN;
            }
        }
        
        public double TotalFrondNumber
        {
            get
            {
                return Fronds.Count;
            }
        }
        
        public double FrondNumber
        {
            get
            {
                return Math.Max(Fronds.Count - ExpandingFronds.Value, 0.0);
            }
        }

        
        public double cover_green
        {
            get
            {
                double DF = DiffuseLightFraction;
                double DirectCover = 1.0 - Math.Exp(-DirectExtinctionCoeff.Value * LAI);
                double DiffuseCover = 1.0 - Math.Exp(-DiffuseExtinctionCoeff.Value * LAI);
                return DF * DiffuseCover + (1 - DF) * DirectCover;
            }
        }
        
        public double SLA
        {
            get { return LAI * 10000.0 / FrondMass; }
        }
        
        public double FFF
        {
            get { return Bunches[0].FemaleFraction; }
        }

        protected double SizeFunction(double Age)
        {
            double GrowthDuration = ExpandingFronds.Value * FrondAppRate.Value;
            double alpha = -Math.Log((1 / 0.99 - 1) / (FrondMaxArea.Value / (FrondMaxArea.Value * 0.01) - 1)) / GrowthDuration;
            double leafsize = FrondMaxArea.Value / (1 + (FrondMaxArea.Value / (FrondMaxArea.Value * 0.01) - 1) * Math.Exp(-alpha * Age));
            return leafsize;

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
        private double DeltaT
        {
            get
            {
                //return Math.Min(Math.Pow(Fn,0.5),1.0);
                //return Math.Min(1.4 * Fn, RelativeDevelopmentalRate.Value);
                //return Math.Min(1.0 * Fn, RelativeDevelopmentalRate.Value);
                return Math.Min(1.25 * Fn, 1.0) * RelativeDevelopmentalRate.Value;
            }
        }


        private void DoUnderstory()
        {
            DoUnderstoryWaterBalance();
            DoUnderstoryGrowth();
            DoUnderstoryNBalance();

            // Now add today's growth to the soil - ie assume plants are in steady state.
            BiomassRemovedType BiomassRemovedData = new BiomassRemovedType();
            BiomassRemovedData.crop_type = "OilPalmUnderstory";
            BiomassRemovedData.dm_type = new string[1] { "litter" };
            BiomassRemovedData.dlt_crop_dm = new float[1] { (float)(UnderstoryDltDM * 10) };
            BiomassRemovedData.dlt_dm_n = new float[1] { (float)(UnderstoryNFixation + Utility.Math.Sum(UnderstoryNUptake)) };
            BiomassRemovedData.dlt_dm_p = new float[1] { 0 };
            BiomassRemovedData.fraction_to_residue = new float[1] { 1 };
            BiomassRemoved.Invoke(BiomassRemovedData);

        }
        private void DoUnderstoryGrowth()
        {
            double RUE = 1.3;
            UnderstoryDltDM = RUE * MetData.Radn * UnderstoryCoverGreen * (1 - cover_green) * FW;
        }

        private void DoUnderstoryWaterBalance()
        {

            UnderstoryCoverGreen = UnderstoryCoverMax * (1 - cover_green);
            UnderstoryPEP = SoilWat.eo * UnderstoryCoverGreen * (1 - cover_green);
            
            for (int j = 0; j < SoilWat.ll15_dep.Length; j++)
                UnderstoryPotSWUptake[j] = Math.Max(0.0, RootProportion(j, UnderstoryRootDepth) * UnderstoryKL * (SoilWat.sw_dep[j] - SoilWat.ll15_dep[j]));

            double TotUnderstoryPotSWUptake = Utility.Math.Sum(UnderstoryPotSWUptake);

            UnderstoryEP = 0.0;
            for (int j = 0; j < SoilWat.ll15_dep.Length; j++)
            {
                UnderstorySWUptake[j] = UnderstoryPotSWUptake[j] * Math.Min(1.0, PEP / TotUnderstoryPotSWUptake);
                UnderstoryEP += UnderstorySWUptake[j];
                SoilWat.sw_dep[j] = SoilWat.sw_dep[j] - UnderstorySWUptake[j];

            }
            if (UnderstoryPEP > 0.0)
                UnderstoryFW = UnderstoryEP / UnderstoryPEP;
            else
                UnderstoryFW = 1.0;

        }
        private void DoUnderstoryNBalance()
        {
            double LegumeNdemand = UnderstoryDltDM * UnderstoryLegumeFraction * 10 * 0.021;
            double NonLegumeNdemand = UnderstoryDltDM * (1 - UnderstoryLegumeFraction) * 10 * 0.005;
            double UnderstoryNdemand = LegumeNdemand + NonLegumeNdemand;
            UnderstoryNFixation = Math.Max(0.0, LegumeNdemand * .44);
            
            for (int j = 0; j < SoilWat.ll15_dep.Length; j++)
            {
                UnderstoryPotNUptake[j] = Math.Max(0.0, RootProportion(j, UnderstoryRootDepth) * SoilN.no3[j]);
            }

            double TotUnderstoryPotNUptake = Utility.Math.Sum(UnderstoryPotNUptake);
            double Fr = Math.Min(1.0, (UnderstoryNdemand - UnderstoryNFixation) / TotUnderstoryPotNUptake);

            for (int j = 0; j < SoilWat.ll15_dep.Length; j++)
            {
                UnderstoryNUptake[j] = UnderstoryPotNUptake[j] * Fr;
                SoilN.no3[j] = SoilN.no3[j] - UnderstoryNUptake[j];
            }
            
            //UnderstoryNFixation += UnderstoryNdemand - Utility.Math.Sum(UnderstoryNUptake);

            //NFixation = Math.Max(0.0, Ndemand - Utility.Math.Sum(NUptake));

        }

        
        public double DefoliationFraction
        {
            get
            {
                return 0;
            }
            set
            {
                FrondType Loss = new FrondType();
                foreach (FrondType F in Fronds)
                {
                    Loss.Mass += F.Mass * value;
                    Loss.N += F.N * value;

                    F.Mass = F.Mass * (1.0 - value);
                    F.N = F.N * (1.0 - value);
                    F.Area = F.Area * (1.0 - value);
                }


                // Now publish today's losses
                BiomassRemovedType BiomassRemovedData = new BiomassRemovedType();
                BiomassRemovedData.crop_type = "OilPalm";
                BiomassRemovedData.dm_type = new string[1] { "fronds" };
                BiomassRemovedData.dlt_crop_dm = new float[1] { (float)(Loss.Mass * SowingData.Population * 10.0) };
                BiomassRemovedData.dlt_dm_n = new float[1] { (float)(Loss.N * SowingData.Population * 10.0) };
                BiomassRemovedData.dlt_dm_p = new float[1] { 0 };
                BiomassRemovedData.fraction_to_residue = new float[1] { 0 };
                if (BiomassRemoved != null)
                    BiomassRemoved.Invoke(BiomassRemovedData);

            }
        }

        
        public double DiffuseLightFraction       // This was originally in the RUEModel class inside "Potential" function (PFR)
        {
            get
            {

                double Q = Q0(MetData.Latitude, Clock.Today.DayOfYear);
                double T = MetData.Radn / Q;
                double X1 = (0.80 - 0.0017 * MetData.Latitude + 0.000044 * MetData.Latitude * MetData.Latitude);
                double A1 = ((0.05 - 0.96) / (X1 - 0.26));
                double A0 = (0.05 - A1 * X1);

                return Math.Min(Math.Max(0.0, A0 + A1 * T), 1.0);  //Taken from Roderick paper Ag For Met(?)

            }
        }

        private double Q0(double lat, int day) 						// (PFR)
        {
            double DEC = (23.45 * Math.Sin(2.0 * 3.14159265 / 365.25 * (day - 79.25)));
            double DECr = (DEC * 2.0 * 3.14159265 / 360.0);
            double LATr = (lat * 2.0 * 3.14159265 / 360.0);
            double HS = Math.Acos(-Math.Tan(LATr) * Math.Tan(DECr));

            return 86400.0 * 1360.0 * (HS * Math.Sin(LATr) * Math.Sin(DECr) + Math.Cos(LATr) * Math.Cos(DECr) * Math.Sin(HS)) / 3.14159265 / 1000000.0;
        }
    }
}