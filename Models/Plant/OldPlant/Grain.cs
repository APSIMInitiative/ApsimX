using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Models.Core;
using Models.PMF.Functions;
using Models.PMF.Phen;
using Models.PMF.Organs;
using System.Xml.Serialization;

namespace Models.PMF.OldPlant
{
    public class Grain : BaseOrgan1, AboveGround, Reproductive
    {
        [Link]
        Summary Summary = null;

        #region Parameters read from XML file and links to other functions.

        [Link]
        Plant15 Plant = null;

        [Link]
        SWStress SWStress = null;

        [Link]
        NStress NStress = null;

        public Function TempStress { get; set; }

        [Link]
        Stem1 Stem = null;

        [Link]
        Phenology Phenology = null;

        [Link]
        Population1 Population = null;

        public Function GrainGrowthPeriod { get; set; }

        public Function ReproductivePeriod { get; set; }

        public Function RelativeGrainFill { get; set; }

        public Function RelativeGrainNFill { get; set; }

        public Function DMSenescenceFraction { get; set; }

        public Function NConcentrationCritical { get; set; }

        public Function NConcentrationMinimum { get; set; }

        public Function NConcentrationMaximum { get; set; }

        public Function GrowthStructuralFractionStage { get; set; }

        public double InitialWt = 0;

        public double InitialNConcentration = 0;

        public double GrainsPerGramStem = 0;

        public double PotentialGrainFillingRate = 0;

        public double PotentialGrainGrowthRate = 0;

        public double MinimumGrainNFillingRate = 0;

        public double CriticalGrainFillingRate = 0;

        public double GrainMaxDailyNConc = 0;

        public double PotentialGrainNFillingRate = 0;

        public double MaxGrainSize = 0;

        public double NSenescenceConcentration = 0;

        public double SenescenceDetachmentFraction = 0;

        public double WaterContentFraction = 0;

        #endregion

        #region Private variables
        public double dlt_dm_pot_rue;
        public double dlt_n_senesced_retrans;           // plant N retranslocated to/from (+/-) senesced part to/from <<somewhere else??>> (g/m^2)
        public double dlt_n_senesced_trans;
        public double dlt_height;                       // growth upwards (mm)
        public double dlt_width;                        // growth outwards (mm)
        private double _DMGreenDemand;
        private double _NDemand;
        private double _SoilNDemand;
        private double sw_demand;
        private double n_conc_crit = 0;
        private double n_conc_max = 0;
        private double n_conc_min = 0;
        private double DltDMGrainDemand;
        private double N_grain_demand;
        #endregion

        #region Public interface defined by Organ1
        [XmlIgnore]
        public override Biomass Senescing { get; protected set; }
        [XmlIgnore]
        public override Biomass Retranslocation { get; protected set; }
        [XmlIgnore]
        public override Biomass Growth { get; protected set; }
        [XmlIgnore]
        public override Biomass Detaching { get; protected set; }
        [XmlIgnore]
        public override Biomass GreenRemoved { get; protected set; }
        [XmlIgnore]
        public override Biomass SenescedRemoved { get; protected set; }

        // Soil water
        public override double SWSupply { get { return 0; } }
        public override double SWDemand { get { return sw_demand; } }
        public override double SWUptake { get { return 0; } }
        public override void DoSWDemand(double Supply) { }
        public override void DoSWUptake(double SWDemand) { }

        // dry matter
        public override double DMSupply { get { return 0; } }
        public override double DMRetransSupply { get { return 0; } }
        public override double dltDmPotRue { get { return 0; } }
        public override double DMGreenDemand { get { return _DMGreenDemand; } }
        public override double DMDemandDifferential
        {
            get
            {
                return Utility.Math.Constrain(DMGreenDemand - Growth.Wt, 0.0, Double.MaxValue);
            }
        }
        public override void DoDMDemand(double DMSupply) { }
        public override void DoDmRetranslocate(double DMAvail, double DMDemandDifferentialTotal)
        {
            Retranslocation.NonStructuralWt = DMAvail * Utility.Math.Divide(DMDemandDifferential, DMDemandDifferentialTotal, 0.0);
            Util.Debug("meal.Retranslocation=%f", Retranslocation.NonStructuralWt);
        }
        public override void GiveDmGreen(double Delta)
        {
            Growth.StructuralWt += Delta * GrowthStructuralFractionStage.Value;
            Growth.NonStructuralWt += Delta * (1.0 - GrowthStructuralFractionStage.Value);
            Util.Debug("meal.Growth.StructuralWt=%f", Growth.StructuralWt);
            Util.Debug("meal.Growth.NonStructuralWt=%f", Growth.NonStructuralWt);
        }
        public override void DoSenescence()
        {
            double fraction_senescing = Utility.Math.Constrain(DMSenescenceFraction.Value, 0.0, 1.0);

            Senescing.StructuralWt = (Live.StructuralWt + Growth.StructuralWt + Retranslocation.StructuralWt) * fraction_senescing;
            Senescing.NonStructuralWt = (Live.NonStructuralWt + Growth.NonStructuralWt + Retranslocation.NonStructuralWt) * fraction_senescing;
            Util.Debug("meal.Senescing.StructuralWt=%f", Senescing.StructuralWt);
            Util.Debug("meal.Senescing.NonStructuralWt=%f", Senescing.NonStructuralWt);

        }
        public override void DoDetachment()
        {
            Detaching = Dead * SenescenceDetachmentFraction;
            Util.Debug("meal.Detaching.Wt=%f", Detaching.Wt);
            Util.Debug("meal.Detaching.N=%f", Detaching.N);
        }
        public override void RemoveBiomass()
        {
            Live = Live - GreenRemoved;
            Dead = Dead - SenescedRemoved;
        }

        // nitrogen
        public override double NDemand { get { return _NDemand; } }
        public override double NSupply { get { return 0; } }
        public override double NUptake { get { return 0; } }
        public override double SoilNDemand { get { return _SoilNDemand; } }
        public override double NCapacity { get { return 0.0; } }
        public override double NDemandDifferential { get { return Utility.Math.Constrain(NDemand - Growth.N, 0.0, double.MaxValue); } }
        public override double AvailableRetranslocateN { get { return 0.0; } }
        public override double DltNSenescedRetrans { get { return dlt_n_senesced_retrans; } }
        public override void DoNDemand(bool IncludeRetranslocation)
        {
            _NDemand = N_grain_demand;
            Util.Debug("Grain.NDemand=%f", _NDemand);
        }
        public override void DoNDemand1Pot(double dltDmPotRue)
        {
            // no n demand for grain
        }
        public override void DoSoilNDemand()
        {
            _SoilNDemand = NDemand - dlt_n_senesced_retrans;
            _SoilNDemand = Utility.Math.Constrain(_SoilNDemand, 0.0, double.MaxValue);
            Util.Debug("meal.SoilNDemand=%f", _SoilNDemand);
        }
        public override void DoNSupply() { }
        public override void DoNRetranslocate(double NSupply, double GrainNDemand)
        {
            if (GrainNDemand >= NSupply)
            {
                // demand greater than or equal to supply
                // retranslocate all available N
                Retranslocation.StructuralN = NSupply;
            }
            else
            {
                // supply greater than demand. Retranslocate what is needed
                Retranslocation.StructuralN = GrainNDemand;
            }
            Util.Debug("meal.Retranslocation.N=%f", Retranslocation.N);
        }
        public override void DoNSenescence()
        {
            double green_n_conc = Utility.Math.Divide(Live.N, Live.Wt, 0.0);
            double dlt_n_in_senescing_part = Senescing.Wt * green_n_conc;
            double sen_n_conc = Math.Min(NSenescenceConcentration, green_n_conc);

            double SenescingN = Senescing.Wt * sen_n_conc;
            Senescing.StructuralN = Utility.Math.Constrain(SenescingN, double.MinValue, Live.N);

            dlt_n_senesced_trans = dlt_n_in_senescing_part - Senescing.N;
            dlt_n_senesced_trans = Utility.Math.Constrain(dlt_n_senesced_trans, 0.0, double.MaxValue);

            Util.Debug("meal.SenescingN=%f", SenescingN);
            Util.Debug("meal.dlt.n_senesced_trans=%f", dlt_n_senesced_trans);
        }
        public override void DoNSenescedRetranslocation(double navail, double n_demand_tot)
        {
            dlt_n_senesced_retrans = navail * Utility.Math.Divide(NDemand, n_demand_tot, 0.0);
            Util.Debug("meal.dlt.n_senesced_retrans=%f", dlt_n_senesced_retrans);
        }
        public override void DoNPartition(double GrowthN)
        {
            Growth.StructuralN = GrowthN;
        }
        public override void DoNFixRetranslocate(double NFixUptake, double nFixDemandTotal)
        {
            Growth.StructuralN += NFixUptake * Utility.Math.Divide(NDemandDifferential, nFixDemandTotal, 0.0);
        }
        public override void DoNConccentrationLimits()
        {
            n_conc_crit = NConcentrationCritical.Value;
            n_conc_min = NConcentrationMinimum.Value;
            n_conc_max = NConcentrationMaximum.Value;
            Util.Debug("meal.n_conc_crit=%f", n_conc_crit);
            Util.Debug("meal.n_conc_min=%f", n_conc_min);
            Util.Debug("meal.n_conc_max=%f", n_conc_max);
        }
        public override void ZeroDltNSenescedTrans()
        {
            dlt_n_senesced_trans = 0;
        }
        public override void DoNUptake(double PotNFix) { }

        // cover
        [XmlIgnore]
        public override double CoverGreen { get { return 0; } protected set { } }
        [XmlIgnore]
        public override double CoverSen { get { return 0; } protected set { } }
        public override void DoPotentialRUE() { }
        public override double interceptRadiation(double incomingSolarRadiation) { return 0; }
        public override void DoCover() { }

        // update
        public override void Update()
        {
            Live = Live + Growth - Senescing;

            Dead = Dead - Detaching + Senescing;
            Live = Live + Retranslocation;
            Live.StructuralN = Live.N + dlt_n_senesced_retrans;

            Biomass dying = Live * Population.DyingFractionPlants;
            Live = Live - dying;
            Dead = Dead + dying;
            Senescing = Senescing + dying;

            //if (HIStressSensitivePeriod.Value == 1)
            //{

            //}

            double dlt_grain_no_lost = GrainNo * Population.DyingFractionPlants;
            GrainNo -= dlt_grain_no_lost;

            Util.Debug("meal.Green.Wt=%f", Live.Wt);
            Util.Debug("meal.Green.N=%f", Live.N);
            Util.Debug("meal.Senesced.Wt=%f", Dead.Wt);
            Util.Debug("meal.Senesced.N=%f", Dead.N);
            Util.Debug("meal.Senescing.Wt=%f", Senescing.Wt);
            Util.Debug("meal.Senescing.N=%f", Senescing.N);
            Util.Debug("meal.GrainNo=%f", GrainNo);
        }

        #endregion

        #region Public interface specific to Grain
        [Units("kg/ha")]
        public double Yield { get { return Live.Wt * 10; } }  // convert to kg/ha

        [XmlIgnore]
        public double GrainNo { get; private set; }

        
        [Units("%")]
        public double Protein
        {
            get
            {
                return NConc * 5.71;
            }
        }

        
        [Units("%")]
        private double NConc
        {
            get
            {
                return Utility.Math.Divide(Live.N + Dead.N,
                                          Live.Wt + Dead.Wt,
                                          0.0) * Conversions.fract2pcnt;
            }
        }

        
        public double Wt { get { return Live.Wt; } }

        
        public double N { get { return Live.N; } }

        
        [Units("g")]
        public double Size
        {
            get
            {
                return Utility.Math.Divide(Live.Wt + Dead.Wt,
                                          GrainNo,
                                          0.0);
            }
        }

        internal double NDemand2 { get { return Utility.Math.Constrain(NDemand - dlt_n_senesced_retrans - Growth.N, 0.0, double.MaxValue); } }
        internal void doProcessBioDemand()
        {
            DoDMDemandStress();
            DoGrainNumber();
            DoDMDemandGrain();
        }
        private void DoGrainNumber()
        {
            if (Phenology.OnDayOf("emergence"))
            {
                // seedling has just emerged.
                GrainNo = 0.0;
            }
            else if (Phenology.OnDayOf("flowering"))
            {
                // we are at first day of grainfill.
                GrainNo = GrainsPerGramStem * Stem.Live.Wt;
            }
            else
            {
                // no changes
            }
            Util.Debug("Grian.GrainNumber=%f", GrainNo);
        }
        void DoDMDemandStress()
        {
            double RueReduction;          // Effect of non-optimal N and Temp conditions on RUE (0-1)

            RueReduction = Math.Min(TempStress.Value, NStress.Photo);
            double Dlt_dm_stress_max = SWStress.Photo * RueReduction;
            Util.Debug("Grain.Dlt_dm_stress_max=%f", Dlt_dm_stress_max);
        }
        void DoDMDemandGrain()
        {
            if (GrainGrowthPeriod.Value == 1)
            {
                // Perform grain filling calculations

                if (Phenology.InPhase("StartGrainFillToEndGrainFill"))
                    DltDMGrainDemand = GrainNo * PotentialGrainFillingRate * RelativeGrainFill.Value;
                else
                {
                    // we are in the flowering to grainfill phase
                    DltDMGrainDemand = GrainNo * PotentialGrainGrowthRate * RelativeGrainFill.Value;
                }
                // check that grain growth will not result in daily n conc below minimum conc
                // for daily grain growth
                double nfact_grain_fill = Math.Min(1.0, NStress.Grain * PotentialGrainNFillingRate / MinimumGrainNFillingRate);
                DltDMGrainDemand = DltDMGrainDemand * nfact_grain_fill;



                // Check that growth does not exceed maximum grain size
                double max_grain = GrainNo * MaxGrainSize;

                double max_dlt = Math.Max(max_grain - Live.Wt, 0.0);
                DltDMGrainDemand = Math.Min(DltDMGrainDemand, max_dlt);
                _DMGreenDemand = Math.Max(DltDMGrainDemand, 0.0);
            }
            else
                DltDMGrainDemand = 0.0;
            Util.Debug("Grain.Dlt_dm_grain_demand=%f", DltDMGrainDemand);
        }
        public double DltDmPotentialGrain
        {
            get
            {
                return DltDMGrainDemand; // oilPart->removeEnergy(DltDMGrainDemand);
            }
        }
        internal void DoNDemandGrain()
        {
            double grain_growth;

            // default case
            double gN_grain_demand1 = 0.0;
            double gN_grain_demand2 = 0.0;
            N_grain_demand = 0.0;
            if (ReproductivePeriod.Value == 1)
            {
                // we are in grain filling stage

                gN_grain_demand1 = GrainNo
                               * PotentialGrainNFillingRate * NStress.Grain
                               * RelativeGrainNFill.Value;

                // calculate total N supply
                double NSupply = 0;
                foreach (Organ1 Organ in Plant.Organ1s)
                    NSupply += Organ.NSupply;

                gN_grain_demand2 = Math.Min(GrainNo * PotentialGrainNFillingRate * RelativeGrainNFill.Value, NSupply);
                N_grain_demand = Math.Max(gN_grain_demand1, gN_grain_demand2);
                N_grain_demand = gN_grain_demand1;

            }

            if (GrainGrowthPeriod.Value == 1)
            {
                // during grain C filling period so make sure that C filling is still
                // going on otherwise stop putting N in now

                grain_growth = Utility.Math.Divide(Growth.Wt + Retranslocation.Wt, GrainNo, 0.0);
                if (grain_growth < CriticalGrainFillingRate)
                {
                    //! grain filling has stopped - stop n flow as well
                    N_grain_demand = 0.0;
                }
                double dailyNconc = Utility.Math.Divide(N_grain_demand, (Growth.Wt + Retranslocation.Wt), 1.0);
                if (dailyNconc > GrainMaxDailyNConc)
                    N_grain_demand = (Growth.Wt + Retranslocation.Wt) * GrainMaxDailyNConc;
            }
            Util.Debug("Grain.N_grain_demand=%f", N_grain_demand);
        }
        internal void WriteCultivarInfo()
        {
            Summary.WriteMessage(FullPath, string.Format("   grains_per_gram_stem           = {0,10:F1} (/g)", GrainsPerGramStem));
            Summary.WriteMessage(FullPath, string.Format("   potential_grain_filling_rate   = {0,10:F4} (g/grain/day)", PotentialGrainFillingRate));
            Summary.WriteMessage(FullPath, string.Format("   potential_grain_growth_rate    = {0,10:F4} (g/grain/day)", PotentialGrainGrowthRate));
            Summary.WriteMessage(FullPath, string.Format("   max_grain_size                 = {0,10:F4} (g)", MaxGrainSize));
        }
        #endregion

        #region Event handlers
        [EventSubscribe("Initialised")]
        private void OnInitialised(object sender, EventArgs e)
        {
            Senescing = new Biomass();
            Retranslocation = new Biomass();
            Growth = new Biomass();
            Detaching = new Biomass();
            GreenRemoved = new Biomass();
            SenescedRemoved = new Biomass();
        }

        public override void OnPrepare(object sender, EventArgs e)
        {
            Growth.Clear();
            Senescing.Clear();
            Detaching.Clear();
            Retranslocation.Clear();
            GreenRemoved.Clear();
            SenescedRemoved.Clear();

            dlt_dm_pot_rue = 0.0;
            dlt_n_senesced_retrans = 0.0;
            dlt_n_senesced_trans = 0.0;
            dlt_height = 0.0;
            dlt_width = 0.0;

            _DMGreenDemand = 0.0;
            _NDemand = 0.0;
            _SoilNDemand = 0.0;
            sw_demand = 0.0;
        }
        public override void OnHarvest(HarvestType Harvest, BiomassRemovedType BiomassRemoved)
        {
            int i = Util.IncreaseSizeOfBiomassRemoved(BiomassRemoved);
            BiomassRemoved.dm_type[i] = "meal";
            BiomassRemoved.fraction_to_residue[i] = 0.0F;
            BiomassRemoved.dlt_crop_dm[i] = (float)((Live.Wt + Dead.Wt) * Conversions.gm2kg / Conversions.sm2ha);
            BiomassRemoved.dlt_dm_n[i] = (float)((Live.N + Dead.N) * Conversions.gm2kg / Conversions.sm2ha);
            //BiomassRemoved.dlt_dm_p[i] = (float)((Green.P + Senesced.P) * Conversions.gm2kg / Conversions.sm2ha);

            Live.Clear();
            Dead.Clear();
        }
        public override void OnEndCrop(BiomassRemovedType BiomassRemoved)
        {
            int i = Util.IncreaseSizeOfBiomassRemoved(BiomassRemoved);
            BiomassRemoved.dm_type[i] = Name;
            BiomassRemoved.fraction_to_residue[i] = 1.0F;
            BiomassRemoved.dlt_crop_dm[i] = (float)((Live.Wt + Dead.Wt) * Conversions.gm2kg / Conversions.sm2ha);
            BiomassRemoved.dlt_dm_n[i] = (float)((Live.N + Dead.N) * Conversions.gm2kg / Conversions.sm2ha);
            //BiomassRemoved.dlt_dm_p[i] = (float)((Green.P + Senesced.P) * Conversions.gm2kg / Conversions.sm2ha);

            Dead.Clear();
            Live.Clear();
        }
        [EventSubscribe("PhaseChanged")]
        private void OnPhaseChanged(PhaseChangedType PhenologyChange)
        {
            if (PhenologyChange.NewPhaseName == "EmergenceToEndOfJuvenile")
            {
                Live.StructuralWt = InitialWt * Population.Density;
                Live.StructuralN = InitialNConcentration * Live.StructuralWt;
                Util.Debug("meal.InitGreen.StructuralWt=%f", Live.StructuralWt);
                Util.Debug("meal.InitGreen.StructuralN=%f", Live.StructuralN);
            }
        }

        #endregion

        #region Grazing
        public override AvailableToAnimalelementType[] AvailableToAnimal
        { get { return Util.AvailableToAnimal(Plant.Name, this.Name, 0.0, Live, Dead); } }
        public override RemovedByAnimalType RemovedByAnimal
        {
            set
            {
                foreach (RemovedByAnimalelementType Cohort in value.element)
                {
                    if (Cohort.Organ.Equals(this.Name, StringComparison.CurrentCultureIgnoreCase))
                    {
                        if (Cohort.AgeID.Equals("live", StringComparison.CurrentCultureIgnoreCase))
                            GreenRemoved = Util.RemoveDM(Cohort.WeightRemoved * Conversions.kg2gm / Conversions.ha2sm, Live, this.Name);
                        else if (Cohort.AgeID.Equals("dead", StringComparison.CurrentCultureIgnoreCase))
                            SenescedRemoved = Util.RemoveDM(Cohort.WeightRemoved * Conversions.kg2gm / Conversions.ha2sm, Dead, this.Name);
                    }
                }
            }
        }
        #endregion
    }

}