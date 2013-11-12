using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;

using System.Reflection;
using System.Collections;
using Models.Plant.Functions;
using Models.Plant.Organs;
using Models.Plant.Phen;


namespace Models.Plant.OldPlant
{
    public class HarvestType
    {
        public Double Plants;
        public Double Remove;
        public Double Height;
        public String Report = "";
    }
    public class RemovedByAnimalType
    {
        public RemovedByAnimalelementType[] element;
    }
    public class RemovedByAnimalelementType
    {
        public String CohortID = "";
        public String Organ = "";
        public String AgeID = "";
        public Double Bottom;
        public Double Top;
        public String Chem = "";
        public Double WeightRemoved;
    }
    public class AvailableToAnimalelementType
    {
        public String CohortID = "";
        public String Organ = "";
        public String AgeID = "";
        public Double Bottom;
        public Double Top;
        public String Chem = "";
        public Double Weight;
        public Double N;
        public Double P;
        public Double S;
        public Double AshAlk;
    }
    public class AvailableToAnimalType
    {
        public AvailableToAnimalelementType[] element;
    }

    public class Plant15: Model
    {
        [Link]
        Phenology Phenology = null;


        public SowPlant2Type SowingData;

        #region Outputs
        public string CropType = "";
        #endregion

        #region Event handlers and publishers
        
        public event NewCropDelegate NewCrop;
        
        public event NullTypeDelegate Sowing;
        
        public event BiomassRemovedDelegate BiomassRemoved;

        [EventSubscribe("Sow")]
        private void OnSow(SowPlant2Type Sow)
        {
            if (Sow.Cultivar == "")
                throw new Exception("Cultivar not specified on sow line.");

            SowingData = Sow;

            // Go through all our children and find all organs.
            Organ1s.Clear();
            foreach (object ChildObject in this.Models)
            {
                Organ1 Child1 = ChildObject as Organ1;
                if (Child1 != null)
                {
                    Organ1s.Add(Child1);
                    if (Child1 is AboveGround)
                        Tops.Add(Child1);
                }

            }

            if (NewCrop != null)
            {
                NewCropType Crop = new NewCropType();
                Crop.crop_type = CropType;
                Crop.sender = Name;
                NewCrop.Invoke(Crop);
            }

            if (Sowing != null)
                Sowing.Invoke();

            WriteSowReport(Sow);
        }
        #endregion


        #region Plant1 functionality

        [Link]
        NStress NStress = null;

        [Link]
        RadiationPartitioning RadiationPartitioning = null;

        [Link]
        Function NFixRate = null;

        [Link]
        CompositeBiomass AboveGroundLive = null;

        [Link]
        CompositeBiomass AboveGround = null;

        [Link]
        CompositeBiomass BelowGround = null;

        [Link]
        SWStress SWStress = null;

        [Link]
        Function TempStress = null;

        [Link]
        Root1 Root = null;

        [Link]
        Stem1 Stem = null;

        [Link]
        Leaf1 Leaf = null;

        [Link]
        Pod Pod = null;

        [Link]
        Grain Grain = null;

        [Link]
        Population1 Population = null;

        [Link]
        PlantSpatial1 PlantSpatial = null;

        [Link]
        GenericArbitratorXY Arbitrator1 = null;

        public double EOCropFactor = 1.5;

        public string NSupplyPreference = "";

        public bool DoRetranslocationBeforeNDemand = false;

        //[Input]
        //double EO = 0;
        [Link]
        Soils.SoilWater SoilWat = null;

        //[Input]
        //public DateTime Today; // for debugging.
        [Link]
        Clock Clock = null;
        
        public event NewCanopyDelegate New_Canopy;

        
        public event NewCropDelegate CropEnding;

        
        public event NewPotentialGrowthDelegate NewPotentialGrowth;

        
        public event NullTypeDelegate Harvesting;

        
        public string plant_status
        {
            get
            {
                // What should be returned here?
                // The old "plant" component returned either "out", "alive"
                // How to determine "dead"?
                return "alive";
            }
        }

        // Used by SWIM
        
        [Units("mm")]
        public double WaterDemand
        {
            get
            {
                double Demand = 0;
                foreach (Organ1 Organ in Organ1s)
                    Demand += Organ.SWDemand;
                return Demand;
            }
        }

        
        [Units("mm")]
        public double EP
        {
            get
            {
                return WaterDemand;
            }
        }

        
        [Units("kg/ha")]
        public double Biomass { get { return AboveGround.Wt * 10; } } // convert to kg/ha

        internal List<Organ1> Organ1s = new List<Organ1>();
        internal List<Organ1> Tops = new List<Organ1>();
        private double ext_n_demand;
        private string AverageStressMessage;
        private DateTime FloweringDate;
        private DateTime MaturityDate;
        private double LAIMax = 0;
        private bool PhenologyEventToday = false;

        public double TopsSWDemand
        {
            get
            {
                double SWDemand = 0.0;
                foreach (Organ1 Organ in Tops)
                    SWDemand += Organ.SWDemand;
                return SWDemand;
            }
        }
        private double TopsLiveWt
        {
            get
            {
                double SWDemand = 0.0;
                foreach (Organ1 Organ in Tops)
                    SWDemand += Organ.Live.Wt;
                return SWDemand;
            }
        }
        public double SWSupplyDemandRatio
        {
            get
            {
                double Supply = 0;
                foreach (Organ1 Organ in Organ1s)
                    Supply += Organ.SWSupply;

                double Demand = TopsSWDemand;
                if (Demand > 0)
                    return Supply / Demand;
                else
                    return 1.0;
            }
        }

        /// <summary>
        /// Old PLANT1 compat. eventhandler. Not used in Plant2
        /// </summary>
        [EventSubscribe("StartOfDay")]
        private void OnPrepare(object sender, EventArgs e)
        {
            Util.Debug("\r\nPREPARE=%s", Clock.Today.ToString("d/M/yyyy"));
            Util.Debug("       =%i", Clock.Today.DayOfYear);

            foreach (Organ1 Organ in Organ1s)
                Organ.OnPrepare(null, null);

            NStress.DoPlantNStress();
            RadiationPartitioning.DoRadiationPartition();
            foreach (Organ1 Organ in Organ1s)
                Organ.DoPotentialRUE();

            // Calculate Plant Water Demand
            double SWDemandMaxFactor = EOCropFactor * SoilWat.eo;
            foreach (Organ1 Organ in Organ1s)
                Organ.DoSWDemand(SWDemandMaxFactor);

            DoNDemandEstimate();

            // PUBLISH NewPotentialGrowth event.
            NewPotentialGrowthType NewPotentialGrowthData = new NewPotentialGrowthType();
            NewPotentialGrowthData.frgr = (float)Math.Min(Math.Min(TempStress.Value, NStress.Photo),
                                                            Math.Min(SWStress.OxygenDeficitPhoto, 1.0 /*PStress.Photo*/));  // FIXME
            NewPotentialGrowthData.sender = Name;
            NewPotentialGrowth.Invoke(NewPotentialGrowthData);
            Util.Debug("NewPotentialGrowth.frgr=%f", NewPotentialGrowthData.frgr);
            //Prepare_p();   // FIXME
        }

        /// <summary>
        ///  Old PLANT1 compat. process eventhandler.
        /// </summary>
        [EventSubscribe("MiddleOfDay")]
        private void OnProcess(object sender, EventArgs e)
        {
            Util.Debug("\r\nPROCESS=%s", Clock.Today.ToString("d/M/yyyy"));
            Util.Debug("       =%i", Clock.Today.DayOfYear);
            foreach (Organ1 Organ in Organ1s)
                Organ.DoSWUptake(TopsSWDemand);

            SWStress.DoPlantWaterStress(TopsSWDemand);
            foreach (Organ1 Organ in Organ1s)
                Organ.DoNSupply();
            Phenology.DoTimeStep();
            Stem.Morphology();
            Leaf.DoCanopyExpansion();

            foreach (Organ1 Organ in Organ1s)   // NIH - WHY IS THIS HERE!!!!?????  Not needed I hope.
                Organ.DoPotentialRUE();         // DPH - It does make a small difference!

            Arbitrator1.PartitionDM(Organ1s);
            Arbitrator1.RetranslocateDM(Organ1s);
            Leaf.Actual();
            Pod.CalcDltPodArea();
            Root.RootLengthGrowth();
            Leaf.LeafDeath();
            Leaf.LeafAreaSenescence();

            foreach (Organ1 Organ in Organ1s)
                Organ.DoSenescence();

            Root.DoSenescenceLength();
            Grain.DoNDemandGrain();

            //  g.n_fix_pot = _fixation->Potential(biomass, swStress->swDef.fixation);
            double n_fix_pot = 0;

            if (DoRetranslocationBeforeNDemand)
                Arbitrator1.DoNRetranslocate(Grain.NDemand, Organ1s);

            bool IncludeRetranslocationInNDemand = !DoRetranslocationBeforeNDemand;
            foreach (Organ1 Organ in Organ1s)
                Organ.DoNDemand(IncludeRetranslocationInNDemand);

            foreach (Organ1 Organ in Organ1s)
                Organ.DoNSenescence();
            Arbitrator1.doNSenescedRetranslocation(Organ1s);

            foreach (Organ1 Organ in Organ1s)
                Organ.DoSoilNDemand();
            // PotNFix = _fixation->NFixPot();
            double PotNFix = 0;
            Root.DoNUptake(PotNFix);

            double n_fix_uptake = Arbitrator1.DoNPartition(n_fix_pot, Organ1s);

            // DoPPartition();
            if (!DoRetranslocationBeforeNDemand)
                Arbitrator1.DoNRetranslocate(Grain.NDemand2, Tops);

            // DoPRetranslocate();
            bool PlantIsDead = Population.PlantDeath();

            foreach (Organ1 Organ in Organ1s)
                Organ.DoDetachment();

            Update();

            CheckBounds();
            SWStress.DoPlantWaterStress(TopsSWDemand);
            NStress.DoPlantNStress();
            if (PhenologyEventToday)
            {
                Console.WriteLine(string.Format("{0}(Day of year = {1}), {2}:",
                                  Clock.Today.ToString("d MMMMMM yyyy"),
                                  Clock.Today.DayOfYear,
                                  Name));
                Console.WriteLine("      " + Phenology.CurrentPhase.Start);
                if (Phenology.CurrentPhase.Start != "Germination")
                {
                    double biomass = 0;

                    double StoverWt = 0;
                    double StoverN = 0;
                    foreach (Organ1 Organ in Tops)
                    {
                        biomass += Organ.Live.Wt + Organ.Dead.Wt;
                        if (!(Organ is Reproductive))
                        {
                            StoverWt += Organ.Live.Wt + Organ.Dead.Wt;
                            StoverN += Organ.Live.N + Organ.Dead.N;
                        }
                    }
                    double StoverNConc = Utility.Math.Divide(StoverN, StoverWt, 0) * Conversions.fract2pcnt;
                    Console.WriteLine(string.Format("           biomass =       {0,8:F2} (g/m^2)   lai          = {1,7:F2} (m^2/m^2)",
                                                    biomass, Leaf.LAI));
                    Console.WriteLine(string.Format("           stover N conc = {0,8:F2} (%)     extractable sw = {1,7:F2} (mm)",
                                                    StoverNConc, Root.ESWInRootZone));
                }
                PhenologyEventToday = false;
            }
            //Root.UpdateWaterBalance();

            LAIMax = Math.Max(LAIMax, Leaf.LAI);
        }

        [EventSubscribe("PhaseChanged")]
        private void OnPhaseChanged(PhaseChangedType Data)
        {
            if (SWStress != null && NStress != null)
            {
                PhenologyEventToday = true;
                AverageStressMessage += String.Format("    {0,40}{1,13:F3}{2,13:F3}{3,13:F3}{4,13:F3}\r\n",
                                                      Data.OldPhaseName,
                                                      1 - SWStress.PhotoAverage, 1 - SWStress.ExpansionAverage,
                                                      1 - NStress.PhotoAverage, 1 - NStress.GrainAverage);
                SWStress.ResetAverage();
                NStress.ResetAverage();
            }

            if (Data.NewPhaseName.Contains("FloweringTo"))
                FloweringDate = Clock.Today;
            else if (Data.NewPhaseName.Contains("MaturityTo"))
                MaturityDate = Clock.Today;
        }

        private void CheckBounds()
        {
            //throw new NotImplementedException();
        }

        private void Update()
        {
            foreach (Organ1 Organ in Organ1s)
                Organ.Update();

            // now update new canopy covers
            PlantSpatial.Density = Population.Density;
            PlantSpatial.CanopyWidth = Stem.Width;

            foreach (Organ1 Organ in Organ1s)
                Organ.DoCover();

            // Update the plant stress observers
            SWStress.Update();
            NStress.Update();
            Population.Update();

            UpdateCanopy();

            foreach (Organ1 Organ in Organ1s)
                Organ.DoNConccentrationLimits();

            // PUBLISH BiomassRemoved event
            DoBiomassRemoved();
        }

        private void UpdateCanopy()
        {
            // PUBLISH New_Canopy event
            double cover_green = 0;
            double cover_sen = 0;
            foreach (Organ1 Organ in Organ1s)
            {
                cover_green += Organ.CoverGreen;
                cover_sen += Organ.CoverSen;
            }
            double cover_tot = (1.0 - (1.0 - cover_green) * (1.0 - cover_sen));
            NewCanopyType NewCanopy = new NewCanopyType();
            NewCanopy.height = (float)Stem.Height;
            NewCanopy.depth = (float)Stem.Height;
            NewCanopy.lai = (float)Leaf.LAI;
            NewCanopy.lai_tot = (float)(Leaf.LAI + Leaf.SLAI);
            NewCanopy.cover = (float)cover_green;
            NewCanopy.cover_tot = (float)cover_tot;
            NewCanopy.sender = Name;
            New_Canopy.Invoke(NewCanopy);
            Util.Debug("NewCanopy.height=%f", NewCanopy.height);
            Util.Debug("NewCanopy.depth=%f", NewCanopy.depth);
            Util.Debug("NewCanopy.lai=%f", NewCanopy.lai);
            Util.Debug("NewCanopy.lai_tot=%f", NewCanopy.lai_tot);
            Util.Debug("NewCanopy.cover=%f", NewCanopy.cover);
            Util.Debug("NewCanopy.cover_tot=%f", NewCanopy.cover_tot);
        }

        private void DoBiomassRemoved()
        {
            List<Biomass> Detaching = new List<Biomass>();
            List<string> OrganNames = new List<string>();
            foreach (Organ1 Organ in Organ1s)
            {
                if (Organ is AboveGround && !Organ.Detaching.IsEmpty)
                {
                    Detaching.Add(Organ.Detaching);
                    OrganNames.Add(Organ.Name);
                }
            }
            BiomassRemovedType chopped = new BiomassRemovedType();
            chopped.crop_type = CropType;
            chopped.dm_type = new string[Detaching.Count];
            chopped.dlt_crop_dm = new float[Detaching.Count];
            chopped.dlt_dm_n = new float[Detaching.Count];
            chopped.dlt_dm_p = new float[Detaching.Count];
            chopped.fraction_to_residue = new float[Detaching.Count];

            for (int i = 0; i < Detaching.Count; i++)
            {
                chopped.dm_type[i] = OrganNames[i];
                chopped.dlt_crop_dm[i] = (float)Detaching[i].Wt;
                chopped.dlt_dm_n[i] = (float)Detaching[i].N;
                //chopped.dlt_dm_p[i] = (float) Detaching[i].P;
                chopped.fraction_to_residue[i] = 1.0f;
            }
            BiomassRemoved.Invoke(chopped);
        }


        /// <summary>
        ///  Calculate an approximate nitrogen demand for today's growth.
        ///   The estimate basically = n to fill the plant up to maximum
        ///   nitrogen concentration.
        /// </summary>
        void DoNDemandEstimate()
        {
            // Assume that the distribution of plant
            // C will be similar after today and so N demand is that
            // required to raise all plant parts to max N conc.

            double dltDmPotRue = 0;
            foreach (Organ1 Organ in Organ1s)
                dltDmPotRue += Organ.dltDmPotRue;

            foreach (Organ1 Organ in Organ1s)
                Organ.DoNDemand1Pot(dltDmPotRue);

            ext_n_demand = 0;
            foreach (Organ1 Organ in Organ1s)
                ext_n_demand += Organ.NDemand;

            //nh  use zero growth value here so that estimated n fix is always <= actual;
            double n_fix_pot = NFixRate.Value * AboveGroundLive.Wt * SWStress.Fixation;

            if (NSupplyPreference == "active")
            {
                // Nothing extra to do here
            }
            else if (NSupplyPreference == "fixation")
            {
                // Remove potential fixation from demand term
                ext_n_demand = ext_n_demand - n_fix_pot;
                ext_n_demand = Utility.Math.Constrain(ext_n_demand, 0.0, Double.MaxValue);
            }
            else
            {
                throw new Exception("bad n supply preference");
            }
            Util.Debug("Plant.ext_n_demand=%f", ext_n_demand);
        }


        /// <summary>
        /// Event handler for the Harvest event - normally comes from Manager.
        /// </summary>
        [EventSubscribe("Harvest")]
        private void OnHarvest(HarvestType Harvest)
        {
            //WriteHarvestReport();

            // Tell the rest of the system we are about to harvest
            if (Harvesting != null)
                Harvesting.Invoke();

            // Check some bounds
            if (Harvest.Remove < 0 || Harvest.Remove > 1.0)
                throw new Exception("Harvest remove fraction needs to be between 0 and 1");
            if (Harvest.Height < 0 || Harvest.Height > 1000.0)
                throw new Exception("Harvest height needs to be between 0 and 1000");

            // Set the population denisty if one was provided by user.
            if (Harvest.Plants != 0)
                Population.Density = Harvest.Plants;

            // Call each organ's OnHarvest. They fill a BiomassRemoved structure. We then publish a
            // BiomassRemoved event.
            BiomassRemovedType BiomassRemovedData = new BiomassRemovedType();
            foreach (Organ1 Organ in Organ1s)
                Organ.OnHarvest(Harvest, BiomassRemovedData);
            BiomassRemovedData.crop_type = CropType;
            BiomassRemoved.Invoke(BiomassRemovedData);
            WriteBiomassRemovedReport(BiomassRemovedData);

            // now update new canopy covers
            PlantSpatial.Density = Population.Density;
            PlantSpatial.CanopyWidth = Leaf.width;
            foreach (Organ1 Organ in Organ1s)
                Organ.DoCover();
            UpdateCanopy();

            foreach (Organ1 Organ in Organ1s)
                Organ.DoNConccentrationLimits();
        }

        [EventSubscribe("EndCrop")]
        private void OnEndCrop()
        {
            NewCropType Crop = new NewCropType();
            Crop.crop_type = CropType;
            Crop.sender = Name;
            if (CropEnding != null)
                CropEnding.Invoke(Crop);

            // Keep track of some variables for reporting.
            Biomass AboveGroundBiomass = new Biomass(AboveGround);
            Biomass BelowGroundBiomass = new Biomass(BelowGround);

            // Call each organ's OnHarvest. They fill a BiomassRemoved structure. We then publish a
            // BiomassRemoved event.
            BiomassRemovedType BiomassRemovedData = new BiomassRemovedType();
            foreach (Organ1 Organ in Organ1s)
                Organ.OnEndCrop(BiomassRemovedData);
            BiomassRemovedData.crop_type = CropType;
            BiomassRemoved.Invoke(BiomassRemovedData);

            Console.WriteLine("    Organic matter from crop:-      Tops to surface residue      Roots to soil FOM");
            Console.WriteLine(string.Format("                      DM (kg/ha) = {0,21:F1}{1,24:F1}",
                                            AboveGroundBiomass.Wt, BelowGroundBiomass.Wt));
            Console.WriteLine(string.Format("                      N  (kg/ha) = {0,22:F2}{1,24:F2}",
                                            AboveGroundBiomass.N, BelowGroundBiomass.N));
            //Console.WriteLine(string.Format("                      P  (kg/ha) = {0,22:F2}{1,24:F2}",
            //                                AboveGroundBiomass.P, BelowGroundBiomass.P));

        }


        /// <summary>
        /// Write a sowing report to summary file.
        /// </summary>
        void WriteSowReport(SowPlant2Type Sow)
        {
            Console.WriteLine("Crop Sow");

            Console.WriteLine("   ------------------------------------------------");
            Console.WriteLine("   cultivar                   = " + Sow.Cultivar);
            Phenology.WriteSummary();
            Grain.WriteCultivarInfo();
            Console.WriteLine("   ------------------------------------------------\n\n");

            Root.WriteSummary();

            Console.WriteLine(string.Format("    Crop factor for bounding water use is set to {0,5:F1} times eo.", EOCropFactor));

            Console.WriteLine("");
            Console.WriteLine("                 Crop Sowing Data");
            Console.WriteLine("    ------------------------------------------------");
            Console.WriteLine("    Sowing  Depth Plants Spacing Skip  Skip  Cultivar");
            Console.WriteLine("    Day no   mm     m^2     mm   row   plant name");
            Console.WriteLine("    ------------------------------------------------");

            Console.WriteLine(string.Format("   {0,7:D}{1,7:F1}{2,7:F1}{3,7:F1}{4,6:F1}{5,6:F1} {6}", new object[] 
                            {Clock.Today.DayOfYear, 
                             Sow.Depth,
                             Sow.Population, 
                             Sow.RowSpacing,
                             Sow.SkipRow,
                             Sow.SkipPlant,
                             Sow.Cultivar}));
            Console.WriteLine("    ------------------------------------------------\n");
        }


        /// <summary>
        ///  Write a harvest report to summary file.
        /// </summary>
        /// //Fixme, this class should be active
        /*void WriteHarvestReport()
        {
            //+  Constant Values
            const double plant_c_frac = 0.4;    // fraction of c in resiudes


            //+  Local Variables
            double grain_wt;                     // grain dry weight (g/kernel)
            double plant_grain_no;               // final grains /head
            double n_grain;                      // total grain N uptake (kg/ha)
            double n_green = 0;                  // above ground green plant N (kg/ha)
            double n_stover = 0;                 // nitrogen content of stover (kg\ha)
            double n_total;                      // total gross nitrogen content (kg/ha)
            double n_grain_conc_percent;         // grain nitrogen %
            double yield;                        // grain yield dry wt (kg/ha)
            double yield_wet;                    // grain yield including moisture (kg/ha)

            // crop harvested. Report status
            yield = (Grain.Live.Wt + Grain.Dead.Wt) * Conversions.gm2kg / Conversions.sm2ha;
            yield_wet = yield * Grain.WaterContentFraction * Conversions.gm2kg / Conversions.sm2ha;
            grain_wt = Utility.Math.Divide(Grain.Live.Wt + Grain.Dead.Wt, Grain.GrainNo, 0);
            plant_grain_no = Utility.Math.Divide(Grain.GrainNo, Population.Density, 0.0);
            n_grain = (Grain.Live.N + Grain.Dead.N) * Conversions.gm2kg / Conversions.sm2ha;


            double dmRoot = (Root.Live.Wt + Root.Dead.Wt) * Conversions.gm2kg / Conversions.sm2ha;
            double nRoot = (Root.Live.N + Root.Dead.N) * Conversions.gm2kg / Conversions.sm2ha;

            n_grain_conc_percent = (Grain.Live.NConc + Grain.Dead.NConc) * Conversions.fract2pcnt;

            double stoverTot = 0;
            double TopsTotalWt = 0;
            double TopsGreenWt = 0;
            double TopsSenescedWt = 0;
            double TopsSenescedN = 0;
            foreach (Organ1 Organ in Tops)
            {
                TopsTotalWt += Organ.Live.Wt + Organ.Dead.Wt;
                TopsGreenWt += Organ.Live.Wt;
                TopsSenescedWt += Organ.Dead.Wt;
                TopsSenescedN += Organ.Dead.N;
                if (Organ is Reproductive)
                { }
                else
                {
                    stoverTot += Organ.Live.Wt + Organ.Dead.Wt;
                    n_stover += (Organ.Live.N + Organ.Dead.N) * Conversions.gm2kg / Conversions.sm2ha;
                    n_green += Organ.Live.N * Conversions.gm2kg / Conversions.sm2ha;
                }
            }
            n_total = n_grain + n_stover;

            double DMRrootShootRatio = Utility.Math.Divide(dmRoot, TopsTotalWt * Conversions.gm2kg / Conversions.sm2ha, 0.0);
            double HarvestIndex = Utility.Math.Divide(yield, TopsTotalWt * Conversions.gm2kg / Conversions.sm2ha, 0.0);
            double StoverCNRatio = Utility.Math.Divide(stoverTot * Conversions.gm2kg / Conversions.sm2ha * plant_c_frac, n_stover, 0.0);
            double RootCNRatio = Utility.Math.Divide(dmRoot * plant_c_frac, nRoot, 0.0);

            Console.WriteLine("");

            Console.WriteLine(string.Format("{0}{1,4:D}{2,26}{3}{4,10:F1}", new object[] {
                        " flowering day          = ",
                        FloweringDate.DayOfYear, 
                        " ", 
                        " stover (kg/ha)         = ",
                        stoverTot * Conversions.gm2kg / Conversions.sm2ha}));

            Console.WriteLine(string.Format("{0}{1,4:D}{2,26}{3}{4,10:F1}", new object[] {
                        " maturity day           = ",
                        MaturityDate.DayOfYear, 
                        " ", 
                        " grain yield (kg/ha)    = ",
                        yield}));

            Console.WriteLine(string.Format("{0}{1,6:F1}{2,24}{3}{4,10:F1}", new object[] {
                        " grain % water content  = ",
                        Grain.WaterContentFraction * Conversions.fract2pcnt, 
                        " ", 
                        " grain yield wet (kg/ha)= ",
                        yield_wet}));

            Console.WriteLine(string.Format("{0}{1,8:F3}{2,22}{3}{4,10:F1}", new object[] {
                        " grain wt (g)           = ",
                        grain_wt, 
                        " ", 
                        " grains/m^2             = ",
                        Grain.GrainNo}));

            Console.WriteLine(string.Format("{0}{1,6:F1}{2,24}{3}{4,10:F3}", new object[] {
                        " grains/plant           = ",
                        plant_grain_no, 
                        " ", 
                        " maximum lai            = ",
                        LAIMax}));

            Console.WriteLine(string.Format("{0}{1,10:F1}", " total above ground biomass (kg/ha)    = ", TopsTotalWt * Conversions.gm2kg / Conversions.sm2ha));
            Console.WriteLine(string.Format("{0}{1,10:F1}", " live above ground biomass (kg/ha)     = ", TopsTotalWt * Conversions.gm2kg / Conversions.sm2ha));
            Console.WriteLine(string.Format("{0}{1,10:F1}", " green above ground biomass (kg/ha)    = ", TopsGreenWt * Conversions.gm2kg / Conversions.sm2ha));
            Console.WriteLine(string.Format("{0}{1,10:F1}", " senesced above ground biomass (kg/ha) = ", TopsSenescedWt * Conversions.gm2kg / Conversions.sm2ha));
            Console.WriteLine(string.Format("{0}{1,8:F1}", " number of leaves       = ", Leaf.LeafNumber));

            Console.WriteLine(string.Format("{0}{1,8:F2}{2,22}{3}{4,10:F2}", new object[] {
                        " DM Root:Shoot ratio    = ",
                        DMRrootShootRatio, 
                        " ", 
                        " Harvest Index          = ",
                        HarvestIndex}));

            Console.WriteLine(string.Format("{0}{1,8:F2}{2,22}{3}{4,10:F2}", new object[] {
                        " Stover C:N ratio       = ",
                        StoverCNRatio, 
                        " ", 
                        " Root C:N ratio         = ",
                        RootCNRatio}));

            Console.WriteLine(string.Format("{0}{1,8:F2}{2,22}{3}{4,10:F2}", new object[] {
                        " grain N percent        = ",
                        n_grain_conc_percent, 
                        " ", 
                        " total N content (kg/ha)= ",
                        n_total}));

            Console.WriteLine(string.Format("{0}{1,8:F2}{2,22}{3}{4,8:F2}", new object[] {
                        " grain N uptake (kg/ha) = ",
                        n_grain, 
                        " ", 
                        " senesced N content (kg/ha)=",
                        TopsSenescedN * Conversions.gm2kg / Conversions.sm2ha}));

            Console.WriteLine(string.Format("{0}{1,8:F2}", " green N content (kg/ha)= ", n_green));


            //summary_p ();

            Console.WriteLine("");

            Console.WriteLine(" Average Stress Indices:                          Water Photo  Water Expan  N Photo      N grain conc");

            Console.WriteLine(AverageStressMessage);
            AverageStressMessage = "";
        }*/

        /// <summary>
        ///  Write a biomass removed report.
        /// </summary>
        void WriteBiomassRemovedReport(BiomassRemovedType Data)
        {
            double dm_residue = 0.0;
            double dm_root_residue = 0.0;
            double n_residue = 0.0;
            double n_root_residue = 0.0;
            double p_residue = 0.0;
            double p_root_residue = 0.0;
            double dm_chopped = 0.0;
            double dm_root_chopped = 0.0;
            double n_chopped = 0.0;
            double n_root_chopped = 0.0;
            double p_chopped = 0.0;
            double p_root_chopped = 0.0;

            for (int part = 0; part < Data.dm_type.Length; part++)
            {
                dm_chopped += Data.dlt_crop_dm[part];
                n_chopped += Data.dlt_dm_n[part];
                p_chopped += Data.dlt_dm_p[part];
                dm_residue += Data.dlt_crop_dm[part] * Data.fraction_to_residue[part];
                n_residue += Data.dlt_dm_n[part] * Data.fraction_to_residue[part];
                p_residue += Data.dlt_dm_p[part] * Data.fraction_to_residue[part];
                if (Data.dm_type[part] == "root")
                {
                    dm_root_residue += Data.dlt_crop_dm[part] * Data.fraction_to_residue[part];
                    n_root_residue += Data.dlt_dm_n[part] * Data.fraction_to_residue[part];
                    p_root_residue += Data.dlt_dm_p[part] * Data.fraction_to_residue[part];
                    dm_root_chopped += Data.dlt_crop_dm[part];
                    n_root_chopped += Data.dlt_dm_n[part];
                    p_root_chopped += Data.dlt_dm_p[part];
                }
            }

            double dm_tops_chopped = dm_chopped - dm_root_chopped;
            double n_tops_chopped = n_chopped - n_root_chopped;
            double p_tops_chopped = p_chopped - p_root_chopped;

            double dm_tops_residue = dm_residue - dm_root_residue;
            double n_tops_residue = n_residue - n_root_residue;
            double p_tops_residue = p_residue - p_root_residue;

            Console.WriteLine("\r\nCrop harvested.");

            Console.WriteLine("    Organic matter from crop:-      Tops to surface residue      Roots to soil FOM");

            Console.WriteLine(string.Format("                      DM (kg/ha) = {0,21:F1}{1,24:F1}",
                                            dm_tops_residue, dm_root_residue));
            Console.WriteLine(string.Format("                      N  (kg/ha) = {0,22:F2}{1,24:F2}",
                                            n_tops_residue, n_root_residue));
            //Console.WriteLine(string.Format("{0,48}{1,55:F1}{2,24:F1}", "P (kg/ha) = ", p_tops_residue, p_root_residue));

            Console.WriteLine("");

            double dm_removed_tops = dm_tops_chopped - dm_tops_residue;
            double dm_removed_root = dm_root_chopped - dm_root_residue;
            double n_removed_tops = n_tops_chopped - n_tops_residue;
            double n_removed_root = n_root_chopped - n_root_residue;
            double p_removed_tops = p_tops_chopped - p_tops_residue;
            double p_removed_root = p_root_chopped - p_root_residue;

            Console.WriteLine("    Organic matter removed from system:-      From Tops               From Roots");

            Console.WriteLine(string.Format("                      DM (kg/ha) = {0,21:F1}{1,24:F1}",
                              dm_removed_tops, dm_removed_root));
            Console.WriteLine(string.Format("                      N  (kg/ha) = {0,22:F2}{1,24:F2}",
                              n_removed_tops, n_removed_root));
            //Console.WriteLine(string.Format("{0,48}{1,55:F3}{2,24:F1}", "DM (kg/ha) = ", p_removed_tops, p_removed_root));

            Console.WriteLine("");
        }
        #endregion

        #region Grazing

        
        public AvailableToAnimalType AvailableToAnimal
        {
            get
            {
                List<AvailableToAnimalelementType> All = new List<AvailableToAnimalelementType>();
                foreach (Organ1 Organ in Organ1s)
                {
                    AvailableToAnimalelementType[] Available = Organ.AvailableToAnimal;
                    if (Available != null)
                        All.AddRange(Available);
                }
                AvailableToAnimalType AvailableToAnimal = new AvailableToAnimalType();
                AvailableToAnimal.element = All.ToArray();
                return AvailableToAnimal;
            }
        }


        
        public RemovedByAnimalType RemovedByAnimal
        {
            get
            {
                return new RemovedByAnimalType();
            }
            set
            {
                if (value == null || value.element == null)
                    return;

                double dmRemovedGreenTops = 0.0;
                foreach (Organ1 Organ in Organ1s)
                {
                    Organ.RemovedByAnimal = value;

                    // BUG? The old plant model always had dmRemovedGreenTops = 0.0;
                    //dmRemovedGreenTops += Organ.GreenRemoved.Wt;  
                }

                // Update biomass and N pools. Different types of plant pools are affected in different ways.
                // Calculate Root Die Back
                double chop_fr_green_leaf = Utility.Math.Divide(Leaf.GreenRemoved.Wt, Leaf.Live.Wt, 0.0);

                Root.RemoveBiomassFraction(chop_fr_green_leaf);
                double biomassGreenTops = AboveGroundLive.Wt;

                foreach (Organ1 Organ in Organ1s)
                    Organ.RemoveBiomass();

                Stem.AdjustMorphologyAfterARemoveBiomass(); // the values calculated here are overwritten in plantPart::morphology(void)

                // now update new canopy covers
                PlantSpatial.Density = Population.Density;
                PlantSpatial.CanopyWidth = 0.0; //Leaf.Width;

                foreach (Organ1 Organ in Organ1s)
                    Organ.DoCover();

                UpdateCanopy();

                double remove_biom_pheno = Utility.Math.Divide(dmRemovedGreenTops, biomassGreenTops, 0.0);
                Phenology.OnRemoveBiomass(remove_biom_pheno);

                foreach (Organ1 Organ in Organ1s)
                    Organ.DoNConccentrationLimits();

                //protocol::ExternalMassFlowType EMF;
                //EMF.PoolClass = "crop";
                //EMF.FlowType = "loss";
                //EMF.DM = (Tops().GreenRemoved.DM() + Tops().SenescedRemoved.DM()) * gm2kg / sm2ha;
                //EMF.N = (Tops().GreenRemoved.N() + Tops().SenescedRemoved.N()) * gm2kg / sm2ha;
                //EMF.P = (Tops().GreenRemoved.P() + Tops().SenescedRemoved.P()) * gm2kg / sm2ha;
                //EMF.C = 0.0; // ?????
                //EMF.SW = 0.0;
                //scienceAPI.publish("ExternalMassFlow", EMF);

            }
        }

        #endregion


    }
}
