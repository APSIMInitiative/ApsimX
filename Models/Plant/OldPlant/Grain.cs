using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Models.Core;
using Models.PMF.Functions;
using Models.PMF.Phen;
using Models.PMF.Organs;
using System.Xml.Serialization;
using System.IO;
using Models.PMF.Interfaces;
using APSIM.Shared.Utilities;

namespace Models.PMF.OldPlant
{
    /// <summary>
    /// A grain organ for Plant15
    /// </summary>
    [Serializable]
    public class Grain : BaseOrgan1, AboveGround, Reproductive
    {
        #region Parameters read from XML file and links to other functions.

        /// <summary>The plant</summary>
        [Link]
        Plant15 Plant = null;

        /// <summary>The sw stress</summary>
        [Link]
        SWStress SWStress = null;

        /// <summary>The n stress</summary>
        [Link]
        NStress NStress = null;

        /// <summary>The temporary stress</summary>
        [Link]
        IFunction TempStress = null;

        /// <summary>The stem</summary>
        [Link]
        Stem1 Stem = null;

        /// <summary>The phenology</summary>
        [Link]
        Phenology Phenology = null;

        /// <summary>The population</summary>
        [Link]
        Population1 Population = null;

        /// <summary>The grain growth period</summary>
        [Link]
        IFunction GrainGrowthPeriod = null;
        /// <summary>The reproductive period</summary>
        [Link]
        IFunction ReproductivePeriod = null;
        /// <summary>The relative grain fill</summary>
        [Link]
        IFunction RelativeGrainFill = null;
        /// <summary>The relative grain n fill</summary>
        [Link]
        IFunction RelativeGrainNFill = null;
        /// <summary>The dm senescence fraction</summary>
        [Link]
        IFunction DMSenescenceFraction = null;
        /// <summary>The n concentration critical</summary>
        [Link]
        IFunction NConcentrationCritical = null;
        /// <summary>The n concentration minimum</summary>
        [Link]
        IFunction NConcentrationMinimum = null;
        /// <summary>The n concentration maximum</summary>
        [Link]
        IFunction NConcentrationMaximum = null;
        /// <summary>The growth structural fraction stage</summary>
        [Link]
        IFunction GrowthStructuralFractionStage = null;

        /// <summary>Gets or sets the initial wt.</summary>
        /// <value>The initial wt.</value>
        public double InitialWt { get; set; }

        /// <summary>Gets or sets the initial n concentration.</summary>
        /// <value>The initial n concentration.</value>
        public double InitialNConcentration { get; set; }

        /// <summary>Gets or sets the grains per gram stem.</summary>
        /// <value>The grains per gram stem.</value>
        public double GrainsPerGramStem { get; set; }

        /// <summary>Gets or sets the potential grain filling rate.</summary>
        /// <value>The potential grain filling rate.</value>
        public double PotentialGrainFillingRate { get; set; }

        /// <summary>Gets or sets the potential grain growth rate.</summary>
        /// <value>The potential grain growth rate.</value>
        public double PotentialGrainGrowthRate { get; set; }

        /// <summary>Gets or sets the minimum grain n filling rate.</summary>
        /// <value>The minimum grain n filling rate.</value>
        public double MinimumGrainNFillingRate { get; set; }

        /// <summary>Gets or sets the critical grain filling rate.</summary>
        /// <value>The critical grain filling rate.</value>
        public double CriticalGrainFillingRate { get; set; }

        /// <summary>Gets or sets the grain maximum daily n conc.</summary>
        /// <value>The grain maximum daily n conc.</value>
        public double GrainMaxDailyNConc { get; set; }

        /// <summary>Gets or sets the potential grain n filling rate.</summary>
        /// <value>The potential grain n filling rate.</value>
        public double PotentialGrainNFillingRate { get; set; }

        /// <summary>Gets or sets the maximum size of the grain.</summary>
        /// <value>The maximum size of the grain.</value>
        public double MaxGrainSize { get; set; }

        /// <summary>Gets or sets the n senescence concentration.</summary>
        /// <value>The n senescence concentration.</value>
        public double NSenescenceConcentration { get; set; }

        /// <summary>Gets or sets the senescence detachment fraction.</summary>
        /// <value>The senescence detachment fraction.</value>
        public double SenescenceDetachmentFraction { get; set; }

        /// <summary>Gets or sets the water content fraction.</summary>
        /// <value>The water content fraction.</value>
        public double WaterContentFraction { get; set; }

        #endregion

        #region Private variables
        /// <summary>The dlt_dm_pot_rue</summary>
        public double dlt_dm_pot_rue;
        /// <summary>The dlt_n_senesced_retrans</summary>
        public double dlt_n_senesced_retrans;           // plant N retranslocated to/from (+/-) senesced part to/from <<somewhere else??>> (g/m^2)
        /// <summary>The dlt_n_senesced_trans</summary>
        public double dlt_n_senesced_trans;
        /// <summary>The dlt_height</summary>
        public double dlt_height;                       // growth upwards (mm)
        /// <summary>The dlt_width</summary>
        public double dlt_width;                        // growth outwards (mm)
        /// <summary>The _ dm green demand</summary>
        private double _DMGreenDemand;
        /// <summary>The _ n demand</summary>
        private double _NDemand;
        /// <summary>The _ soil n demand</summary>
        private double _SoilNDemand;
        /// <summary>The sw_demand</summary>
        private double sw_demand;
        /// <summary>The n_conc_crit</summary>
        private double n_conc_crit = 0;
        /// <summary>The n_conc_max</summary>
        private double n_conc_max = 0;
        /// <summary>The n_conc_min</summary>
        private double n_conc_min = 0;
        /// <summary>The DLT dm grain demand</summary>
        private double DltDMGrainDemand;
        /// <summary>The n_grain_demand</summary>
        private double N_grain_demand;
        #endregion

        #region Public interface defined by Organ1
        /// <summary>Gets or sets the senescing.</summary>
        /// <value>The senescing.</value>
        [XmlIgnore]
        public override Biomass Senescing { get; protected set; }
        /// <summary>Gets or sets the retranslocation.</summary>
        /// <value>The retranslocation.</value>
        [XmlIgnore]
        public override Biomass Retranslocation { get; protected set; }
        /// <summary>Gets or sets the growth.</summary>
        /// <value>The growth.</value>
        [XmlIgnore]
        public override Biomass Growth { get; protected set; }
        /// <summary>Gets or sets the detaching.</summary>
        /// <value>The detaching.</value>
        [XmlIgnore]
        public override Biomass Detaching { get; protected set; }
        /// <summary>Gets or sets the green removed.</summary>
        /// <value>The green removed.</value>
        [XmlIgnore]
        public override Biomass GreenRemoved { get; protected set; }
        /// <summary>Gets or sets the senesced removed.</summary>
        /// <value>The senesced removed.</value>
        [XmlIgnore]
        public override Biomass SenescedRemoved { get; protected set; }

        // Soil water
        /// <summary>Gets the sw supply.</summary>
        /// <value>The sw supply.</value>
        public override double SWSupply { get { return 0; } }
        /// <summary>Gets the sw demand.</summary>
        /// <value>The sw demand.</value>
        public override double SWDemand { get { return sw_demand; } }
        /// <summary>Gets the sw uptake.</summary>
        /// <value>The sw uptake.</value>
        public override double SWUptake { get { return 0; } }
        /// <summary>Does the sw demand.</summary>
        /// <param name="Supply">The supply.</param>
        public override void DoSWDemand(double Supply) { }
        /// <summary>Does the sw uptake.</summary>
        /// <param name="SWDemand">The sw demand.</param>
        public override void DoSWUptake(double SWDemand) { }

        // dry matter
        /// <summary>Gets the dm supply.</summary>
        /// <value>The dm supply.</value>
        public override double DMSupply { get { return 0; } }
        /// <summary>Gets the dm retrans supply.</summary>
        /// <value>The dm retrans supply.</value>
        public override double DMRetransSupply { get { return 0; } }
        /// <summary>Gets the DLT dm pot rue.</summary>
        /// <value>The DLT dm pot rue.</value>
        public override double dltDmPotRue { get { return 0; } }
        /// <summary>Gets the dm green demand.</summary>
        /// <value>The dm green demand.</value>
        public override double DMGreenDemand { get { return _DMGreenDemand; } }
        /// <summary>Gets the dm demand differential.</summary>
        /// <value>The dm demand differential.</value>
        public override double DMDemandDifferential
        {
            get
            {
                return MathUtilities.Constrain(DMGreenDemand - Growth.Wt, 0.0, Double.MaxValue);
            }
        }
        /// <summary>Does the dm demand.</summary>
        /// <param name="DMSupply">The dm supply.</param>
        public override void DoDMDemand(double DMSupply) { }
        /// <summary>Does the dm retranslocate.</summary>
        /// <param name="DMAvail">The dm avail.</param>
        /// <param name="DMDemandDifferentialTotal">The dm demand differential total.</param>
        public override void DoDmRetranslocate(double DMAvail, double DMDemandDifferentialTotal)
        {
            Retranslocation.NonStructuralWt = DMAvail * MathUtilities.Divide(DMDemandDifferential, DMDemandDifferentialTotal, 0.0);
            Util.Debug("meal.Retranslocation=%f", Retranslocation.NonStructuralWt);
        }
        /// <summary>Gives the dm green.</summary>
        /// <param name="Delta">The delta.</param>
        public override void GiveDmGreen(double Delta)
        {
            Growth.StructuralWt += Delta * GrowthStructuralFractionStage.Value;
            Growth.NonStructuralWt += Delta * (1.0 - GrowthStructuralFractionStage.Value);
            Util.Debug("meal.Growth.StructuralWt=%f", Growth.StructuralWt);
            Util.Debug("meal.Growth.NonStructuralWt=%f", Growth.NonStructuralWt);
        }
        /// <summary>Does the senescence.</summary>
        public override void DoSenescence()
        {
            double fraction_senescing = MathUtilities.Constrain(DMSenescenceFraction.Value, 0.0, 1.0);

            Senescing.StructuralWt = (Live.StructuralWt + Growth.StructuralWt + Retranslocation.StructuralWt) * fraction_senescing;
            Senescing.NonStructuralWt = (Live.NonStructuralWt + Growth.NonStructuralWt + Retranslocation.NonStructuralWt) * fraction_senescing;
            Util.Debug("meal.Senescing.StructuralWt=%f", Senescing.StructuralWt);
            Util.Debug("meal.Senescing.NonStructuralWt=%f", Senescing.NonStructuralWt);

        }
        /// <summary>Does the detachment.</summary>
        public override void DoDetachment()
        {
            Detaching = Dead * SenescenceDetachmentFraction;
            Util.Debug("meal.Detaching.Wt=%f", Detaching.Wt);
            Util.Debug("meal.Detaching.N=%f", Detaching.N);
        }
        /// <summary>Removes the biomass.</summary>
        public override void RemoveBiomass()
        {
            Live = Live - GreenRemoved;
            Dead = Dead - SenescedRemoved;
        }

        // nitrogen
        /// <summary>Gets the n demand.</summary>
        /// <value>The n demand.</value>
        public override double NDemand { get { return _NDemand; } }
        /// <summary>Gets the n supply.</summary>
        /// <value>The n supply.</value>
        public override double NSupply { get { return 0; } }
        /// <summary>Gets the n uptake.</summary>
        /// <value>The n uptake.</value>
        public override double NUptake { get { return 0; } }
        /// <summary>Gets the soil n demand.</summary>
        /// <value>The soil n demand.</value>
        public override double SoilNDemand { get { return _SoilNDemand; } }
        /// <summary>Gets the n capacity.</summary>
        /// <value>The n capacity.</value>
        public override double NCapacity { get { return 0.0; } }
        /// <summary>Gets the n demand differential.</summary>
        /// <value>The n demand differential.</value>
        public override double NDemandDifferential { get { return MathUtilities.Constrain(NDemand - Growth.N, 0.0, double.MaxValue); } }
        /// <summary>Gets the available retranslocate n.</summary>
        /// <value>The available retranslocate n.</value>
        public override double AvailableRetranslocateN { get { return 0.0; } }
        /// <summary>Gets the DLT n senesced retrans.</summary>
        /// <value>The DLT n senesced retrans.</value>
        public override double DltNSenescedRetrans { get { return dlt_n_senesced_retrans; } }
        /// <summary>Does the n demand.</summary>
        /// <param name="IncludeRetranslocation">if set to <c>true</c> [include retranslocation].</param>
        public override void DoNDemand(bool IncludeRetranslocation)
        {
            _NDemand = N_grain_demand;
            Util.Debug("Grain.NDemand=%f", _NDemand);
        }
        /// <summary>Does the n demand1 pot.</summary>
        /// <param name="dltDmPotRue">The DLT dm pot rue.</param>
        public override void DoNDemand1Pot(double dltDmPotRue)
        {
            // no n demand for grain
        }
        /// <summary>Does the soil n demand.</summary>
        public override void DoSoilNDemand()
        {
            _SoilNDemand = NDemand - dlt_n_senesced_retrans;
            _SoilNDemand = MathUtilities.Constrain(_SoilNDemand, 0.0, double.MaxValue);
            Util.Debug("meal.SoilNDemand=%f", _SoilNDemand);
        }
        /// <summary>Does the n supply.</summary>
        public override void DoNSupply() { }
        /// <summary>Does the n retranslocate.</summary>
        /// <param name="NSupply">The n supply.</param>
        /// <param name="GrainNDemand">The grain n demand.</param>
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
        /// <summary>Does the n senescence.</summary>
        public override void DoNSenescence()
        {
            double green_n_conc = MathUtilities.Divide(Live.N, Live.Wt, 0.0);
            double dlt_n_in_senescing_part = Senescing.Wt * green_n_conc;
            double sen_n_conc = Math.Min(NSenescenceConcentration, green_n_conc);

            double SenescingN = Senescing.Wt * sen_n_conc;
            Senescing.StructuralN = MathUtilities.Constrain(SenescingN, double.MinValue, Live.N);

            dlt_n_senesced_trans = dlt_n_in_senescing_part - Senescing.N;
            dlt_n_senesced_trans = MathUtilities.Constrain(dlt_n_senesced_trans, 0.0, double.MaxValue);

            Util.Debug("meal.SenescingN=%f", SenescingN);
            Util.Debug("meal.dlt.n_senesced_trans=%f", dlt_n_senesced_trans);
        }
        /// <summary>Does the n senesced retranslocation.</summary>
        /// <param name="navail">The navail.</param>
        /// <param name="n_demand_tot">The n_demand_tot.</param>
        public override void DoNSenescedRetranslocation(double navail, double n_demand_tot)
        {
            dlt_n_senesced_retrans = navail * MathUtilities.Divide(NDemand, n_demand_tot, 0.0);
            Util.Debug("meal.dlt.n_senesced_retrans=%f", dlt_n_senesced_retrans);
        }
        /// <summary>Does the n partition.</summary>
        /// <param name="GrowthN">The growth n.</param>
        public override void DoNPartition(double GrowthN)
        {
            Growth.StructuralN = GrowthN;
        }
        /// <summary>Does the n fix retranslocate.</summary>
        /// <param name="NFixUptake">The n fix uptake.</param>
        /// <param name="nFixDemandTotal">The n fix demand total.</param>
        public override void DoNFixRetranslocate(double NFixUptake, double nFixDemandTotal)
        {
            Growth.StructuralN += NFixUptake * MathUtilities.Divide(NDemandDifferential, nFixDemandTotal, 0.0);
        }
        /// <summary>Does the n conccentration limits.</summary>
        public override void DoNConccentrationLimits()
        {
            n_conc_crit = NConcentrationCritical.Value;
            n_conc_min = NConcentrationMinimum.Value;
            n_conc_max = NConcentrationMaximum.Value;
            Util.Debug("meal.n_conc_crit=%f", n_conc_crit);
            Util.Debug("meal.n_conc_min=%f", n_conc_min);
            Util.Debug("meal.n_conc_max=%f", n_conc_max);
        }
        /// <summary>Zeroes the DLT n senesced trans.</summary>
        public override void ZeroDltNSenescedTrans()
        {
            dlt_n_senesced_trans = 0;
        }
        /// <summary>Does the n uptake.</summary>
        /// <param name="PotNFix">The pot n fix.</param>
        public override void DoNUptake(double PotNFix) { }

        // cover
        /// <summary>Gets or sets the cover green.</summary>
        /// <value>The cover green.</value>
        [XmlIgnore]
        public override double CoverGreen { get { return 0; } protected set { } }
        /// <summary>Gets or sets the cover sen.</summary>
        /// <value>The cover sen.</value>
        [XmlIgnore]
        public override double CoverSen { get { return 0; } protected set { } }
        /// <summary>Does the potential rue.</summary>
        public override void DoPotentialRUE() { }
        /// <summary>Intercepts the radiation.</summary>
        /// <param name="incomingSolarRadiation">The incoming solar radiation.</param>
        /// <returns></returns>
        public override double interceptRadiation(double incomingSolarRadiation) { return 0; }
        /// <summary>Does the cover.</summary>
        public override void DoCover() { }

        // update
        /// <summary>Updates this instance.</summary>
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
        /// <summary>Gets the yield.</summary>
        /// <value>The yield.</value>
        [Units("kg/ha")]
        public double Yield { get { return Live.Wt * 10; } }  // convert to kg/ha

        /// <summary>Gets the grain no.</summary>
        /// <value>The grain no.</value>
        [XmlIgnore]
        public double GrainNo { get; private set; }


        /// <summary>Gets the protein.</summary>
        /// <value>The protein.</value>
        [Units("%")]
        public double Protein
        {
            get
            {
                return NConc * 5.71;
            }
        }


        /// <summary>Gets the n conc.</summary>
        /// <value>The n conc.</value>
        [Units("%")]
        private double NConc
        {
            get
            {
                return MathUtilities.Divide(Live.N + Dead.N,
                                          Live.Wt + Dead.Wt,
                                          0.0) * Conversions.fract2pcnt;
            }
        }


        /// <summary>Gets the wt.</summary>
        /// <value>The wt.</value>
        public double Wt { get { return Live.Wt; } }


        /// <summary>Gets the n.</summary>
        /// <value>The n.</value>
        public double N { get { return Live.N; } }


        /// <summary>Gets the size.</summary>
        /// <value>The size.</value>
        [Units("g")]
        public double Size
        {
            get
            {
                return MathUtilities.Divide(Live.Wt + Dead.Wt,
                                          GrainNo,
                                          0.0);
            }
        }

        /// <summary>Gets the n demand2.</summary>
        /// <value>The n demand2.</value>
        internal double NDemand2 { get { return MathUtilities.Constrain(NDemand - dlt_n_senesced_retrans - Growth.N, 0.0, double.MaxValue); } }
        /// <summary>Does the process bio demand.</summary>
        internal void doProcessBioDemand()
        {
            DoDMDemandStress();
            DoGrainNumber();
            DoDMDemandGrain();
        }
        /// <summary>Does the grain number.</summary>
        private void DoGrainNumber()
        {
            if (Phenology.OnDayOf("Emergence"))
            {
                // seedling has just emerged.
                GrainNo = 0.0;
            }
            else if (Phenology.OnDayOf("Flowering"))
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
        /// <summary>Does the dm demand stress.</summary>
        void DoDMDemandStress()
        {
            double RueReduction;          // Effect of non-optimal N and Temp conditions on RUE (0-1)

            RueReduction = Math.Min(TempStress.Value, NStress.Photo);
            double Dlt_dm_stress_max = SWStress.Photo * RueReduction;
            Util.Debug("Grain.Dlt_dm_stress_max=%f", Dlt_dm_stress_max);
        }
        /// <summary>Does the dm demand grain.</summary>
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
        /// <summary>Gets the DLT dm potential grain.</summary>
        /// <value>The DLT dm potential grain.</value>
        public double DltDmPotentialGrain
        {
            get
            {
                return DltDMGrainDemand; // oilPart->removeEnergy(DltDMGrainDemand);
            }
        }
        /// <summary>Does the n demand grain.</summary>
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

                grain_growth = MathUtilities.Divide(Growth.Wt + Retranslocation.Wt, GrainNo, 0.0);
                if (grain_growth < CriticalGrainFillingRate)
                {
                    //! grain filling has stopped - stop n flow as well
                    N_grain_demand = 0.0;
                }
                double dailyNconc = MathUtilities.Divide(N_grain_demand, (Growth.Wt + Retranslocation.Wt), 1.0);
                if (dailyNconc > GrainMaxDailyNConc)
                    N_grain_demand = (Growth.Wt + Retranslocation.Wt) * GrainMaxDailyNConc;
            }
            Util.Debug("Grain.N_grain_demand=%f", N_grain_demand);
        }
        /// <summary>Writes the cultivar information.</summary>
        /// <param name="writer">The writer.</param>
        internal void WriteCultivarInfo(TextWriter writer)
        {
            string message = string.Format("grains_per_gram_stem           = {0,10:F1} (/g)\r\n" +
                                           "potential_grain_filling_rate   = {1,10:F4} (g/grain/day)\r\n" +
                                           "potential_grain_growth_rate    = {2,10:F4} (g/grain/day)\r\n" +
                                           "max_grain_size                 = {3,10:F4} (g)", 
                    GrainsPerGramStem, PotentialGrainFillingRate, PotentialGrainGrowthRate,  MaxGrainSize);

            writer.WriteLine(message);
        }
        #endregion

        #region Event handlers
        /// <summary>Called when [simulation commencing].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            Senescing = new Biomass();
            Retranslocation = new Biomass();
            Growth = new Biomass();
            Detaching = new Biomass();
            GreenRemoved = new Biomass();
            SenescedRemoved = new Biomass();
        }

        /// <summary>Called when [prepare].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
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
        /// <summary>Called when [harvest].</summary>
        /// <param name="Harvest">The harvest.</param>
        /// <param name="BiomassRemoved">The biomass removed.</param>
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
        /// <summary>Called when [end crop].</summary>
        /// <param name="BiomassRemoved">The biomass removed.</param>
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
        /// <summary>Called when [phase changed].</summary>
        /// <param name="PhenologyChange">The phenology change.</param>
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
        /// <summary>Gets the available to animal.</summary>
        /// <value>The available to animal.</value>
        public override AvailableToAnimalelementType[] AvailableToAnimal
        { get { return Util.AvailableToAnimal(Plant.Name, this.Name, 0.0, Live, Dead); } }
        /// <summary>Sets the removed by animal.</summary>
        /// <value>The removed by animal.</value>
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