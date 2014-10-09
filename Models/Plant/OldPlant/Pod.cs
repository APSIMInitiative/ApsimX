using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Models.Core;
using Models.PMF.Functions;
using Models.PMF.Organs;
using Models.PMF.Phen;
using System.Xml.Serialization;

namespace Models.PMF.OldPlant
{
    /// <summary>
    /// The pod organ for plant15
    /// </summary>
    [Serializable]
    public class Pod : BaseOrgan1, AboveGround
    {
        #region Parameters read from XML file and links to other functions.

        /// <summary>The environment</summary>
        [Link]
        Environment Environment = null;


        /// <summary>The grain</summary>
        [Link]
        Grain Grain = null;

        /// <summary>The te</summary>
        [Link] Function TE = null;
        /// <summary>The fraction grain in pod</summary>
        [Link] Function FractionGrainInPod = null;
        /// <summary>The n concentration critical</summary>
        [Link] Function NConcentrationCritical = null;
        /// <summary>The n concentration minimum</summary>
        [Link] Function NConcentrationMinimum = null;
        /// <summary>The n concentration maximum</summary>
        [Link] Function NConcentrationMaximum = null;
        /// <summary>The growth structural fraction stage</summary>
        [Link] Function GrowthStructuralFractionStage = null;
        /// <summary>The dm senescence fraction</summary>
        [Link] Function DMSenescenceFraction = null;

        /// <summary>The plant</summary>
        [Link]
        Plant15 Plant = null;

        /// <summary>The total live</summary>
        [Link]
        CompositeBiomass TotalLive = null;


        /// <summary>The population</summary>
        [Link]
        Population1 Population = null;

        /// <summary>Gets or sets the initial wt.</summary>
        /// <value>The initial wt.</value>
        public double InitialWt { get; set; }

        /// <summary>Gets or sets the initial n concentration.</summary>
        /// <value>The initial n concentration.</value>
        public double InitialNConcentration { get; set; }

        /// <summary>Gets or sets the n deficit uptake fraction.</summary>
        /// <value>The n deficit uptake fraction.</value>
        public double NDeficitUptakeFraction { get; set; }

        /// <summary>Gets or sets the n senescence concentration.</summary>
        /// <value>The n senescence concentration.</value>
        public double NSenescenceConcentration { get; set; }

        /// <summary>Gets or sets the senescence detachment fraction.</summary>
        /// <value>The senescence detachment fraction.</value>
        public double SenescenceDetachmentFraction { get; set; }

        #endregion

        #region Private variables
        /// <summary>The dlt_dm_pot_rue</summary>
        private double dlt_dm_pot_rue;
        /// <summary>The dlt_n_senesced_retrans</summary>
        private double dlt_n_senesced_retrans;           // plant N retranslocated to/from (+/-) senesced part to/from <<somewhere else??>> (g/m^2)
        /// <summary>The dlt_n_senesced_trans</summary>
        private double dlt_n_senesced_trans;
        /// <summary>The _ dm green demand</summary>
        private double _DMGreenDemand;
        /// <summary>The _ n demand</summary>
        private double _NDemand;
        /// <summary>The _ soil n demand</summary>
        private double _SoilNDemand;
        /// <summary>The n maximum</summary>
        private double NMax;
        /// <summary>The sw_demand_te</summary>
        private double sw_demand_te;
        /// <summary>The sw_demand</summary>
        private double sw_demand;
        /// <summary>The n_conc_crit</summary>
        private double n_conc_crit = 0;
        /// <summary>The n_conc_max</summary>
        private double n_conc_max = 0;
        /// <summary>The n_conc_min</summary>
        private double n_conc_min = 0;
        /// <summary>The transp eff</summary>
        private double transpEff;
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
        public override void DoSWDemand(double Supply)
        {
            // Return crop water demand from soil by the crop (mm) calculated by
            // dividing biomass production limited by radiation by transpiration efficiency.
            // get potential transpiration from potential
            // carbohydrate production and transpiration efficiency

            // Calculate today's transpiration efficiency from min,max temperatures and co2 level
            // and converting mm water to g dry matter (g dm/m^2/mm water)

            transpEff = TE.Value / Environment.VPD / Conversions.g2mm;

            if (transpEff == 0)
            {
                sw_demand_te = 0;
                sw_demand = 0;
            }
            else
            {
                sw_demand_te = dlt_dm_pot_rue / transpEff;

                // For some reason, the old PLANT module used a CoverGreen of zero in this calculation
                // and not the calculated one from the DoCover method. Is this a bug? - DPH
                double ZeroCoverGreen = 0;

                // Capping of sw demand will create an effective TE- recalculate it here
                // In an ideal world this should NOT be changed here - NIH
                double SWDemandMax = Supply * ZeroCoverGreen;
                sw_demand = Utility.Math.Constrain(sw_demand_te, Double.MinValue, SWDemandMax);
                transpEff = transpEff * Utility.Math.Divide(sw_demand_te, sw_demand, 1.0);
            }
            Util.Debug("Pod.sw_demand=%f", sw_demand);
            Util.Debug("Pod.transpEff=%f", transpEff);
        }
        /// <summary>Does the sw uptake.</summary>
        /// <param name="SWDemand">The sw demand.</param>
        public override void DoSWUptake(double SWDemand) { }

        // dry matter
        /// <summary>Gets the dm supply.</summary>
        /// <value>The dm supply.</value>
        public override double DMSupply { get { return 0.0; } }
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
                return Utility.Math.Constrain(DMGreenDemand - Growth.Wt, 0.0, Double.MaxValue);
            }
        }
        /// <summary>Does the dm demand.</summary>
        /// <param name="DMSupply">The dm supply.</param>
        public override void DoDMDemand(double DMSupply)
        {
            Grain.doProcessBioDemand();

            double dlt_dm_supply_by_pod = 0.0;  // FIXME
            DMSupply += dlt_dm_supply_by_pod;

            double dm_grain_demand = Grain.DltDmPotentialGrain;

            if (dm_grain_demand > 0.0)
                _DMGreenDemand = dm_grain_demand * FractionGrainInPod.Value - dlt_dm_supply_by_pod;
            else
                _DMGreenDemand = DMSupply * FractionGrainInPod.Value - dlt_dm_supply_by_pod;
            Util.Debug("Pod.DMGreenDemand=%f", _DMGreenDemand);
        }
        /// <summary>Does the dm retranslocate.</summary>
        /// <param name="DMAvail">The dm avail.</param>
        /// <param name="DMDemandDifferentialTotal">The dm demand differential total.</param>
        public override void DoDmRetranslocate(double DMAvail, double DMDemandDifferentialTotal)
        {
            Retranslocation.NonStructuralWt += DMAvail * Utility.Math.Divide(DMDemandDifferential, DMDemandDifferentialTotal, 0.0);
            Util.Debug("pod.Retranslocation=%f", Retranslocation.NonStructuralWt);
        }
        /// <summary>Gives the dm green.</summary>
        /// <param name="Delta">The delta.</param>
        public override void GiveDmGreen(double Delta)
        {
            Growth.StructuralWt += Delta * GrowthStructuralFractionStage.Value;
            Growth.NonStructuralWt += Delta * (1.0 - GrowthStructuralFractionStage.Value);
            Util.Debug("Pod.Growth.StructuralWt=%f", Growth.StructuralWt);
            Util.Debug("Pod.Growth.NonStructuralWt=%f", Growth.NonStructuralWt);
        }
        /// <summary>Does the senescence.</summary>
        public override void DoSenescence()
        {
            double fraction_senescing = Utility.Math.Constrain(DMSenescenceFraction.Value, 0.0, 1.0);

            Senescing.StructuralWt = (Live.StructuralWt + Growth.StructuralWt + Retranslocation.StructuralWt) * fraction_senescing;
            Senescing.NonStructuralWt = (Live.NonStructuralWt + Growth.NonStructuralWt + Retranslocation.NonStructuralWt) * fraction_senescing;
            Util.Debug("Pod.Senescing.StructuralWt=%f", Senescing.StructuralWt);
            Util.Debug("Pod.Senescing.NonStructuralWt=%f", Senescing.NonStructuralWt);
        }
        /// <summary>Does the detachment.</summary>
        public override void DoDetachment()
        {
            Detaching = Dead * SenescenceDetachmentFraction;
            Util.Debug("Pod.Detaching.Wt=%f", Detaching.Wt);
            Util.Debug("Pod.Detaching.N=%f", Detaching.N);
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
        public override double NCapacity
        {
            get
            {
                return Utility.Math.Constrain(NMax - NDemand, 0.0, double.MaxValue);
            }
        }
        /// <summary>Gets the n demand differential.</summary>
        /// <value>The n demand differential.</value>
        public override double NDemandDifferential { get { return Utility.Math.Constrain(NDemand - Growth.N, 0.0, double.MaxValue); } }
        /// <summary>Gets the available retranslocate n.</summary>
        /// <value>The available retranslocate n.</value>
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
        /// <summary>Gets the DLT n senesced retrans.</summary>
        /// <value>The DLT n senesced retrans.</value>
        public override double DltNSenescedRetrans { get { return dlt_n_senesced_retrans; } }
        /// <summary>Does the n demand.</summary>
        /// <param name="IncludeRetransloation">if set to <c>true</c> [include retransloation].</param>
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
            Util.Debug("Pod.NDemand=%f", _NDemand);
            Util.Debug("Pod.NMax=%f", NMax);
        }
        /// <summary>Does the n demand1 pot.</summary>
        /// <param name="dltDmPotRue">The DLT dm pot rue.</param>
        public override void DoNDemand1Pot(double dltDmPotRue)
        {
            Biomass OldGrowth = Growth;
            Growth.StructuralWt = dltDmPotRue * Utility.Math.Divide(Live.Wt, TotalLive.Wt, 0.0);
            Util.Debug("Pod.Growth.StructuralWt=%f", Growth.StructuralWt);

            Util.CalcNDemand(dltDmPotRue, dltDmPotRue, n_conc_crit, n_conc_max, Growth, Live, Retranslocation.N, 1.0,
                       ref _NDemand, ref NMax);
            Growth.StructuralWt = 0.0;
            Growth.NonStructuralWt = 0.0;
            Util.Debug("Pod.NDemand=%f", _NDemand);
            Util.Debug("Pod.NMax=%f", NMax);
        }
        /// <summary>Does the soil n demand.</summary>
        public override void DoSoilNDemand()
        {
            _SoilNDemand = NDemand - dlt_n_senesced_retrans;
            _SoilNDemand = Utility.Math.Constrain(_SoilNDemand, 0.0, double.MaxValue);
            Util.Debug("Pod.SoilNDemand=%f", _SoilNDemand);
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
                Retranslocation.StructuralN = -AvailableRetranslocateN;
            }
            else
            {
                // supply greater than demand.
                // Retranslocate what is needed
                Retranslocation.StructuralN = -GrainNDemand * Utility.Math.Divide(AvailableRetranslocateN, NSupply, 0.0);
            }
            Util.Debug("Pod.Retranslocation.N=%f", Retranslocation.N);
        }
        /// <summary>Does the n senescence.</summary>
        public override void DoNSenescence()
        {
            double green_n_conc = Utility.Math.Divide(Live.N, Live.Wt, 0.0);
            double dlt_n_in_senescing_part = Senescing.Wt * green_n_conc;
            double sen_n_conc = Math.Min(NSenescenceConcentration, green_n_conc);

            double SenescingN = Senescing.Wt * sen_n_conc;
            Senescing.StructuralN = Utility.Math.Constrain(SenescingN, double.MinValue, Live.N);

            dlt_n_senesced_trans = dlt_n_in_senescing_part - Senescing.N;
            dlt_n_senesced_trans = Utility.Math.Constrain(dlt_n_senesced_trans, 0.0, double.MaxValue);

            Util.Debug("Pod.SenescingN=%f", SenescingN);
            Util.Debug("Pod.dlt.n_senesced_trans=%f", dlt_n_senesced_trans);
        }
        /// <summary>Does the n senesced retranslocation.</summary>
        /// <param name="navail">The navail.</param>
        /// <param name="n_demand_tot">The n_demand_tot.</param>
        public override void DoNSenescedRetranslocation(double navail, double n_demand_tot)
        {
            dlt_n_senesced_retrans = navail * Utility.Math.Divide(NDemand, n_demand_tot, 0.0);
            Util.Debug("Pod.dlt.n_senesced_retrans=%f", dlt_n_senesced_retrans);
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
            Growth.StructuralN += NFixUptake * Utility.Math.Divide(NDemandDifferential, nFixDemandTotal, 0.0);
        }
        /// <summary>Does the n conccentration limits.</summary>
        public override void DoNConccentrationLimits()
        {
            n_conc_crit = NConcentrationCritical.Value;
            n_conc_min = NConcentrationMinimum.Value;
            n_conc_max = NConcentrationMaximum.Value;
            Util.Debug("Pod.n_conc_crit=%f", n_conc_crit);
            Util.Debug("Pod.n_conc_min=%f", n_conc_min);
            Util.Debug("Pod.n_conc_max=%f", n_conc_max);
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
        /// <summary>Gets the cover total.</summary>
        /// <value>The cover total.</value>
        [XmlIgnore]
        public double CoverTotal { get { return 1.0 - (1.0 - CoverGreen) * (1.0 - CoverSen); } }
        /// <summary>Does the potential rue.</summary>
        public override void DoPotentialRUE()
        {
            Util.Debug("Pod.dlt.dm_pot_rue=%f", 0.0);
        }
        /// <summary>Intercepts the radiation.</summary>
        /// <param name="incomingSolarRadiation">The incoming solar radiation.</param>
        /// <returns></returns>
        public override double interceptRadiation(double incomingSolarRadiation) { return 0; }
        /// <summary>Does the cover.</summary>
        public override void DoCover()
        {
            Util.Debug("pod.cover.green=%f", 0.0);
        }

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

            Util.Debug("Pod.Green.Wt=%f", Live.Wt);
            Util.Debug("Pod.Green.N=%f", Live.N);
            Util.Debug("Pod.Senesced.Wt=%f", Dead.Wt);
            Util.Debug("Pod.Senesced.N=%f", Dead.N);
            Util.Debug("Pod.Senescing.Wt=%f", Senescing.Wt);
            Util.Debug("Pod.Senescing.N=%f", Senescing.N);
            Util.Debug("pod.partAI=%f", 0.0);
        }
        #endregion

        #region Public interface specific to Pod
        /// <summary>Calculates the DLT pod area.</summary>
        internal void CalcDltPodArea()
        {
            // The old PLANT model always had a dltDm of zero which results in a dlt_partAI and PartAI of zero.
            // This in turn results in CoverGreen always = 0
            // Is this a bug? - DPH
            Util.Debug("Pod.dlt_partAI=%f", 0.0);
        }
        #endregion

        #region Event handlers
        /// <summary>Called when [simulation commencing].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            Growth = new Biomass();
            Senescing = new Biomass();
            Detaching = new Biomass();
            Retranslocation = new Biomass();
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

            _DMGreenDemand = 0.0;
            _NDemand = 0.0;
            _SoilNDemand = 0.0;
            NMax = 0.0;
            sw_demand_te = 0.0;
            sw_demand = 0.0;
        }
        /// <summary>Called when [harvest].</summary>
        /// <param name="Harvest">The harvest.</param>
        /// <param name="BiomassRemoved">The biomass removed.</param>
        public override void OnHarvest(HarvestType Harvest, BiomassRemovedType BiomassRemoved)
        {
            double dm_init = Utility.Math.Constrain(InitialWt * Population.Density, double.MinValue, Live.Wt);
            double n_init = Utility.Math.Constrain(dm_init * InitialNConcentration, double.MinValue, Live.N);
            //double p_init = Utility.Math.Constrain(dm_init * SimplePart::c.p_init_conc, double.MinValue, Green.P);

            double retain_fr_green = Utility.Math.Divide(dm_init, Live.Wt, 0.0);
            double retain_fr_sen = 0.0;

            double dlt_dm_harvest = Live.Wt + Dead.Wt - dm_init;
            double dlt_n_harvest = Live.N + Dead.N - n_init;
            //double dlt_p_harvest = Green.P + Senesced.P - p_init;

            Dead = Dead * retain_fr_sen;
            Live.StructuralWt = Live.Wt * retain_fr_green;
            Live.StructuralN = n_init;
            //Green.P = p_init;

            int i = Util.IncreaseSizeOfBiomassRemoved(BiomassRemoved);
            BiomassRemoved.dm_type[i] = Name;
            BiomassRemoved.fraction_to_residue[i] = (float)(1.0 - Harvest.Remove);
            BiomassRemoved.dlt_crop_dm[i] = (float)(dlt_dm_harvest * Conversions.gm2kg / Conversions.sm2ha);
            BiomassRemoved.dlt_dm_n[i] = (float)(dlt_n_harvest * Conversions.gm2kg / Conversions.sm2ha);
            //BiomassRemoved.dlt_dm_p[i] = (float)(dlt_p_harvest * Conversions.gm2kg / Conversions.sm2ha);
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
                Util.Debug("Pod.InitGreen.StructuralWt=%f", Live.StructuralWt);
                Util.Debug("Pod.InitGreen.StructuralN=%f", Live.StructuralN);
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


