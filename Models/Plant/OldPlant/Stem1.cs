using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Models.Core;
using Models.Plant.Functions;
using Models.Plant.Organs;
using Models.Plant.Phen;
using System.Xml.Serialization;

namespace Models.Plant.OldPlant
{
    public class Stem1 : BaseOrgan1, AboveGround
    {
        #region Parameters read from XML file and links to other functions.
        [Link]
        Plant15 Plant = null;

        public Function HeightFunction { get; set; }

        [Link]
        Population1 Population = null;

        public CompositeBiomass TotalLive { get; set; }

        public Function GrowthStructuralFractionStage { get; set; }

        public Function DMSenescenceFraction { get; set; }

        [Link]
        Leaf1 Leaf = null;

        public Function NConcentrationCritical { get; set; }

        public Function NConcentrationMinimum { get; set; }

        public Function NConcentrationMaximum { get; set; }

        public Function RetainFraction { get; set; }

        public double NDeficitUptakeFraction = 1.0;

        public double NSenescenceConcentration = 0;

        public double SenescenceDetachmentFraction = 0;

        public double InitialWt = 0;

        public double InitialNConcentration = 0;
        #endregion

        #region Private variables
        public double dlt_n_senesced_retrans;           // plant N retranslocated to/from (+/-) senesced part to/from <<somewhere else??>> (g/m^2)
        public double dlt_n_senesced_trans;
        public double dlt_height;                       // growth upwards (mm)
        public double dlt_width;                        // growth outwards (mm)

        private double _DMGreenDemand;
        private double _NDemand;
        private double _SoilNDemand;
        private double NMax;
        private double sw_demand;
        private double n_conc_crit = 0;
        private double n_conc_max = 0;
        private double n_conc_min = 0;
        private double DeltaHeight;

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
        public override double DMSupply { get { return 0.0; } }
        public override double DMRetransSupply
        {
            get
            {
                return Utility.Math.Constrain(Live.NonStructuralWt, 0.0, double.MaxValue);
            }
        }
        public override double dltDmPotRue { get { return 0; } }
        public override double DMGreenDemand { get { return _DMGreenDemand; } }
        public override double DMDemandDifferential { get { return 0; } }
        public override void DoDMDemand(double DMSupply) { }
        public override void DoDmRetranslocate(double dlt_dm_retrans_to_fruit, double demand_differential_begin) { }
        public override void GiveDmGreen(double Delta)
        {
            Growth.StructuralWt += Delta * GrowthStructuralFractionStage.FunctionValue;
            Growth.NonStructuralWt += Delta * (1.0 - GrowthStructuralFractionStage.FunctionValue);
            Util.Debug("Stem.Growth.StructuralWt=%f", Growth.StructuralWt);
            Util.Debug("Stem.Growth.NonStructuralWt=%f", Growth.NonStructuralWt);
        }
        public override void DoSenescence()
        {
            double fraction_senescing = Utility.Math.Constrain(DMSenescenceFraction.FunctionValue, 0.0, 1.0);

            Senescing.StructuralWt = (Live.StructuralWt + Growth.StructuralWt + Retranslocation.StructuralWt) * fraction_senescing;
            Senescing.NonStructuralWt = (Live.NonStructuralWt + Growth.NonStructuralWt + Retranslocation.NonStructuralWt) * fraction_senescing;
            Util.Debug("Stem.Senescing.StructuralWt=%f", Senescing.StructuralWt);
            Util.Debug("Stem.Senescing.NonStructuralWt=%f", Senescing.NonStructuralWt);
        }
        public override void DoDetachment()
        {
            Detaching = Dead * SenescenceDetachmentFraction;
            Util.Debug("Stem.Detaching.Wt=%f", Detaching.Wt);
            Util.Debug("Stem.Detaching.N=%f", Detaching.N);
        }
        public override void RemoveBiomass()
        {
            Live = Live - GreenRemoved;
            Dead = Dead - SenescedRemoved;
        }
        internal void AdjustMorphologyAfterARemoveBiomass()
        {
            double dm_plant = Utility.Math.Divide(Live.Wt, Population.Density, 0.0);

            if (HeightFunction != null)
                Height = HeightFunction.FunctionValue;
        }
        // nitrogen
        public override double NDemand { get { return _NDemand; } }
        public override double NSupply { get { return 0; } }
        public override double NUptake { get { return 0; } }
        public override double SoilNDemand { get { return _SoilNDemand; } }
        public override double NCapacity
        {
            get
            {
                return Utility.Math.Constrain(NMax - NDemand, 0.0, double.MaxValue);
            }
        }
        public override double NDemandDifferential { get { return Utility.Math.Constrain(NDemand - Growth.N, 0.0, double.MaxValue); } }
        public override double AvailableRetranslocateN
        {
            get
            {
                double N_min = n_conc_min * Live.Wt;
                double N_avail = Utility.Math.Constrain(Live.N - N_min, 0.0, double.MaxValue);
                double n_retrans_fraction = 1.0;
                return (N_avail * n_retrans_fraction);
            }
        }
        public override double DltNSenescedRetrans { get { return dlt_n_senesced_retrans; } }
        public override void DoNDemand(bool IncludeRetransloation)
        {

            double TopsDMSupply = 0;
            double TopsDltDmPotRue = 0;
            foreach (Organ1 Organ in Plant.Tops)
            {
                TopsDMSupply += Organ.DMSupply;
                TopsDltDmPotRue += Organ.dltDmPotRue;
            }

            if (IncludeRetransloation)
                Util.CalcNDemand(TopsDMSupply, TopsDltDmPotRue, n_conc_crit, n_conc_max, Growth, Live, Retranslocation.N, NDeficitUptakeFraction,
                          ref _NDemand, ref NMax);
            else
                Util.CalcNDemand(TopsDMSupply, TopsDltDmPotRue, n_conc_crit, n_conc_max, Growth, Live, 0.0, NDeficitUptakeFraction,
                          ref _NDemand, ref NMax);
            Util.Debug("Stem.NDemand=%f", _NDemand);
            Util.Debug("Stem.NMax=%f", NMax);
        }
        public override void DoNDemand1Pot(double dltDmPotRue)
        {
            Biomass OldGrowth = Growth;
            Growth.StructuralWt = dltDmPotRue * Utility.Math.Divide(Live.Wt, TotalLive.Wt, 0.0);
            Util.Debug("Stem.Growth.StructuralWt=%f", Growth.StructuralWt);

            Util.CalcNDemand(dltDmPotRue, dltDmPotRue, n_conc_crit, n_conc_max, Growth, Live, Retranslocation.N, 1.0,
                       ref _NDemand, ref NMax);
            Growth.StructuralWt = 0.0;
            Growth.NonStructuralWt = 0.0;
            Util.Debug("Stem.NDemand=%f", _NDemand);
            Util.Debug("Stem.NMax=%f", NMax);
        }
        public override void DoSoilNDemand()
        {
            _SoilNDemand = NDemand - dlt_n_senesced_retrans;
            _SoilNDemand = Utility.Math.Constrain(_SoilNDemand, 0.0, double.MaxValue);
            Util.Debug("Stem.SoilNDemand=%f", _SoilNDemand);
        }
        public override void DoNSupply() { }
        public override void DoNRetranslocate(double NSupply, double GrainNDemand)
        {
            if (GrainNDemand >= NSupply)
            {
                // demand greater than or equal to supply
                // retranslocate all available N
                Retranslocation.StructuralN = -AvailableRetranslocateN;
            }
            else
            {
                // supply greater than demand.
                // Retranslocate what is needed
                Retranslocation.StructuralN = -GrainNDemand * Utility.Math.Divide(AvailableRetranslocateN, NSupply, 0.0);
            }
            Util.Debug("Stem.Retranslocation.N=%f", Retranslocation.N);
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

            Util.Debug("Stem.SenescingN=%f", SenescingN);
            Util.Debug("Stem.dlt.n_senesced_trans=%f", dlt_n_senesced_trans);
        }
        public override void DoNSenescedRetranslocation(double navail, double n_demand_tot)
        {
            dlt_n_senesced_retrans = navail * Utility.Math.Divide(NDemand, n_demand_tot, 0.0);
            Util.Debug("Stem.dlt.n_senesced_retrans=%f", dlt_n_senesced_retrans);
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
            n_conc_crit = NConcentrationCritical.FunctionValue;
            n_conc_min = NConcentrationMinimum.FunctionValue;
            n_conc_max = NConcentrationMaximum.FunctionValue;
            Util.Debug("Stem.n_conc_crit=%f", n_conc_crit);
            Util.Debug("Stem.n_conc_min=%f", n_conc_min);
            Util.Debug("Stem.n_conc_max=%f", n_conc_max);
        }
        public override void ZeroDltNSenescedTrans()
        {
            dlt_n_senesced_trans = 0;
        }
        public override void DoNUptake(double PotNFix) { }


        //cover
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
            Growth.StructuralN += Leaf.NSenescedTrans;
            Live = Live + Growth - Senescing;

            Dead = Dead - Detaching + Senescing;
            Live = Live + Retranslocation;
            Live.StructuralN = Live.N + dlt_n_senesced_retrans;

            Biomass dying = Live * Population.DyingFractionPlants;
            Live = Live - dying;
            Dead = Dead + dying;
            Senescing = Senescing + dying;
            Height += DeltaHeight;

            Util.Debug("Stem.Green.Wt=%f", Live.Wt);
            Util.Debug("Stem.Green.N=%f", Live.N);
            Util.Debug("Stem.Senesced.Wt=%f", Dead.Wt);
            Util.Debug("Stem.Senesced.N=%f", Dead.N);
            Util.Debug("Stem.Senescing.Wt=%f", Senescing.Wt);
            Util.Debug("Stem.Senescing.N=%f", Senescing.N);
        }
        #endregion

        #region Public interface specific to Stem
        public double NCrit { get { return n_conc_crit * Live.Wt; } }
        public double NMin { get { return n_conc_min * Live.Wt; } }
        [XmlIgnore]
        [Units("mm")]
        public double Height { get; private set; }  // soilwat needs height for its E0 calculation.
        [XmlIgnore]
        public double Width { get; private set; }

        [XmlIgnore]
        public double FractionHeightRemoved { get; private set; }
        public double GreenWtPerPlant { get { return Utility.Math.Divide(Live.Wt, Population.Density, 0.0); } }

        internal void Morphology()
        {
            DeltaHeight = Utility.Math.Constrain(HeightFunction.FunctionValue - Height, 0.0, double.MaxValue);
            Util.Debug("Stem.DeltaHeight=%f", DeltaHeight);
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

            dlt_n_senesced_retrans = 0.0;
            dlt_n_senesced_trans = 0.0;
            dlt_height = 0.0;
            dlt_width = 0.0;

            _DMGreenDemand = 0.0;
            _NDemand = 0.0;
            _SoilNDemand = 0.0;
            NMax = 0.0;
            sw_demand = 0.0;
        }
        public override void OnHarvest(HarvestType Harvest, BiomassRemovedType BiomassRemoved)
        {
            // Some biomass is removed according to harvest height
            FractionHeightRemoved = Utility.Math.Divide(Harvest.Height, Height, 0.0);

            double chop_fr_green = (1.0 - RetainFraction.FunctionValue);
            double chop_fr_sen = (1.0 - RetainFraction.FunctionValue);

            double dlt_dm_harvest = Live.Wt * chop_fr_green
                                 + Dead.Wt * chop_fr_sen;

            double dlt_n_harvest = Live.N * chop_fr_green
                                + Dead.N * chop_fr_sen;

            //double dlt_p_harvest = Green.P * chop_fr_green
            //                    + Senesced.P * chop_fr_sen;

            Dead = Dead * RetainFraction.FunctionValue;
            Live = Live * RetainFraction.FunctionValue;

            Height = Utility.Math.Constrain(Harvest.Height, 1.0, double.MaxValue);

            int i = Util.IncreaseSizeOfBiomassRemoved(BiomassRemoved);
            BiomassRemoved.dm_type[i] = Name;
            BiomassRemoved.fraction_to_residue[i] = (float)(1.0 - Harvest.Remove);
            BiomassRemoved.dlt_crop_dm[i] = (float)(dlt_dm_harvest * Conversions.gm2kg / Conversions.sm2ha);
            BiomassRemoved.dlt_dm_n[i] = (float)(dlt_n_harvest * Conversions.gm2kg / Conversions.sm2ha);
            //BiomassRemoved.dlt_dm_p[i] = (float)(dlt_p_harvest * Conversions.gm2kg / Conversions.sm2ha);
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
                Util.Debug("Stem.InitGreen.StructuralWt=%f", Live.StructuralWt);
                Util.Debug("Stem.InitGreen.StructuralN=%f", Live.StructuralN);
            }
        }

        #endregion

        #region Grazing
        public override AvailableToAnimalelementType[] AvailableToAnimal
        { get { return Util.AvailableToAnimal(Plant.Name, this.Name, Height, Live, Dead); } }
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
