using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Models.Core;
using Models.PMF.Functions;
using Models.PMF.Organs;
using Models.Soils;
using Models.PMF.Phen;
using System.Xml.Serialization;
using System.IO;
using Models.PMF.Interfaces;
using APSIM.Shared.Utilities;

namespace Models.PMF.OldPlant
{
    /// <summary>
    /// A root model for plant15
    /// </summary>
    [Serializable]
    public class Root1 : BaseOrgan1, BelowGround
    {

        /// <summary>The summary</summary>
        [Link]
        ISummary Summary = null;

        #region Parameters read from XML file and links to other functions.
        /// <summary>The plant</summary>
        [Link]
        public Plant15 Plant;

        /// <summary>The root advance factor temporary</summary>
        [Link]
        IFunction RootAdvanceFactorTemp = null;
        /// <summary>The root advance factor water stress</summary>
        [Link]
        IFunction RootAdvanceFactorWaterStress = null;
        /// <summary>The sw factor root depth</summary>
        [Link]
        IFunction SWFactorRootDepth = null;
        /// <summary>The sw factor root length</summary>
        [Link] IFunctionArray SWFactorRootLength = null;
        /// <summary>The root depth rate</summary>
        [Link]
        IFunction RootDepthRate = null;

        /// <summary>The population</summary>
        [Link]
        Population1 Population = null;

        /// <summary>The relative root rate</summary>
        [Link] IFunctionArray RelativeRootRate = null;
        /// <summary>The dm senescence fraction</summary>
        [Link]
        IFunction DMSenescenceFraction = null;
        /// <summary>The growth structural fraction stage</summary>
        [Link]
        IFunction GrowthStructuralFractionStage = null;

        /// <summary>The n uptake function</summary>
        [Link]
        object NUptakeFunction = null;

        /// <summary>The soil</summary>
        [Link]
        Soil Soil = null;

        /// <summary>Gets or sets the n concentration critical.</summary>
        /// <value>The n concentration critical.</value>
        public double NConcentrationCritical { get; set; }

        /// <summary>Gets or sets the n concentration minimum.</summary>
        /// <value>The n concentration minimum.</value>
        public double NConcentrationMinimum { get; set; }

        /// <summary>Gets or sets the n concentration maximum.</summary>
        /// <value>The n concentration maximum.</value>
        public double NConcentrationMaximum { get; set; }

        /// <summary>Gets or sets the initial root depth.</summary>
        /// <value>The initial root depth.</value>
        public double InitialRootDepth { get; set; }

        /// <summary>Gets or sets the die back fraction.</summary>
        /// <value>The die back fraction.</value>
        public double DieBackFraction { get; set; }

        /// <summary>Gets or sets the cl.</summary>
        /// <value>The cl.</value>
        public double[] cl { get; set; }

        /// <summary>Gets or sets the ll.</summary>
        /// <value>The ll.</value>
        public double[] ll { get; set; }

        /// <summary>Gets or sets the kl.</summary>
        /// <value>The kl.</value>
        public double[] kl { get; set; }

        /// <summary>Gets or sets the xf.</summary>
        /// <value>The xf.</value>
        public double[] xf { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [modify kl].
        /// </summary>
        /// <value><c>true</c> if [modify kl]; otherwise, <c>false</c>.</value>
        public bool ModifyKL { get; set; }

        /// <summary>Gets or sets the cl a.</summary>
        /// <value>The cl a.</value>
        public double ClA { get; set; }

        /// <summary>Gets or sets the cl b.</summary>
        /// <value>The cl b.</value>
        public double ClB { get; set; }

        /// <summary>Gets or sets the espa.</summary>
        /// <value>The espa.</value>
        public double ESPA { get; set; }

        /// <summary>Gets or sets the espb.</summary>
        /// <value>The espb.</value>
        public double ESPB { get; set; }

        /// <summary>Gets or sets the eca.</summary>
        /// <value>The eca.</value>
        public double ECA { get; set; }

        /// <summary>Gets or sets the ecb.</summary>
        /// <value>The ecb.</value>
        public double ECB { get; set; }

        /// <summary>Gets or sets the n deficit uptake fraction.</summary>
        /// <value>The n deficit uptake fraction.</value>
        public double NDeficitUptakeFraction { get; set; }

        /// <summary>Gets or sets the n senescence concentration.</summary>
        /// <value>The n senescence concentration.</value>
        public double NSenescenceConcentration { get; set; }

        /// <summary>Gets or sets the n supply preference.</summary>
        /// <value>The n supply preference.</value>
        public string NSupplyPreference { get; set; }

        /// <summary>Gets or sets the senescence detachment fraction.</summary>
        /// <value>The senescence detachment fraction.</value>
        public double SenescenceDetachmentFraction { get; set; }

        /// <summary>Gets or sets the initial wt.</summary>
        /// <value>The initial wt.</value>
        public double InitialWt { get; set; }

        /// <summary>Gets or sets the initial n concentration.</summary>
        /// <value>The initial n concentration.</value>
        public double InitialNConcentration { get; set; }

        /// <summary>Gets or sets the length of the specific root.</summary>
        /// <value>The length of the specific root.</value>
        public double SpecificRootLength { get; set; }

        /// <summary>Gets or sets the soil water uptake from the arbitrator.</summary>
        [XmlIgnore]
        public double[] ArbitratorSWUptake { get; set; }
        /// <summary>Gets or sets the no3 uptake from the arbitrator.</summary>
        [XmlIgnore]
        public double[] ArbitratorNO3Uptake { get; set; }
        /// <summary>Gets or sets the nh4 uptake from the arbitrator.</summary>
        [XmlIgnore]
        public double[] ArbitratorNH4Uptake { get; set; }


        #endregion

        #region Variables we need from other modules
        
        //Fixme, this needs to talk to swim
        /// <summary>The swim3</summary>
        double swim3 = double.MinValue;
        #endregion

        #region Events we're going to publish at some point.

        /// <summary>Occurs when [incorp fom].</summary>
        public event FOMLayerDelegate IncorpFOM;


        /// <summary>Occurs when [water changed].</summary>
        public event WaterChangedDelegate WaterChanged;


        /// <summary>Occurs when [nitrogen changed].</summary>
        public event NitrogenChangedDelegate NitrogenChanged;
        #endregion

        #region Private variables
        /// <summary>The swim is present</summary>
        private bool SwimIsPresent = false;
        /// <summary>The dlt_sw_dep</summary>
        private double[] dlt_sw_dep;
        /// <summary>The sw_avail</summary>
        private double[] sw_avail;
        /// <summary>The sw_avail_pot</summary>
        private double[] sw_avail_pot;
        /// <summary>The sw_supply</summary>
        private double[] sw_supply;
        /// <summary>The dlt_no3gsm</summary>
        private double[] dlt_no3gsm;
        /// <summary>The DLT_NH4GSM</summary>
        private double[] dlt_nh4gsm;
        /// <summary>The no3gsm_uptake_pot</summary>
        private double[] no3gsm_uptake_pot;
        /// <summary>The nh4gsm_uptake_pot</summary>
        private double[] nh4gsm_uptake_pot;

        /// <summary>The DLT root depth</summary>
        private double dltRootDepth;
        /// <summary>The DLT root length</summary>
        private double[] dltRootLength;
        /// <summary>The DLT root length senesced</summary>
        private double[] dltRootLengthSenesced;
        /// <summary>The DLT root length dead</summary>
        private double[] dltRootLengthDead;
        /// <summary>The ll_dep</summary>
        private double[] ll_dep;
        /// <summary>The root length</summary>
        private double[] RootLength;
        /// <summary>The no3gsm_min</summary>
        private double[] no3gsm_min;
        /// <summary>The nh4gsm_min</summary>
        private double[] nh4gsm_min;
        /// <summary>The have modified kl values</summary>
        private bool HaveModifiedKLValues = false;
        /// <summary>The root length senesced</summary>
        private double[] RootLengthSenesced;
        /// <summary>The dlt_n_senesced_retrans</summary>
        private double dlt_n_senesced_retrans;           // plant N retranslocated to/from (+/-) senesced part to/from <<somewhere else??>> (g/m^2)
        /// <summary>The dlt_n_senesced_trans</summary>
        private double dlt_n_senesced_trans;

        /// <summary>The drained upper limit (mm)</summary>
        private double[] DULmm;


        /// <summary>The _ dm green demand</summary>
        private double _DMGreenDemand;
        /// <summary>The _ n demand</summary>
        private double _NDemand;
        /// <summary>The _ soil n demand</summary>
        private double _SoilNDemand;
        /// <summary>The n maximum</summary>
        private double NMax;
        /// <summary>The sw_demand</summary>
        private double sw_demand;
        /// <summary>The n_conc_crit</summary>
        private double n_conc_crit = 0;
        /// <summary>The n_conc_max</summary>
        private double n_conc_max = 0;
        /// <summary>The n_conc_min</summary>
        private double n_conc_min = 0;
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
        public override double SWSupply 
        { 
            get 
            {
                if (sw_supply != null)
                    return MathUtilities.Sum(sw_supply);
                else
                    return 0;
            } 
        }
        /// <summary>Gets the sw demand.</summary>
        /// <value>The sw demand.</value>
        public override double SWDemand { get { return sw_demand; } }
        /// <summary>Gets the sw uptake.</summary>
        /// <value>The sw uptake.</value>
        public override double SWUptake { get { return -MathUtilities.Sum(dlt_sw_dep); } }
        /// <summary>Does the sw demand.</summary>
        /// <param name="Supply">The supply.</param>
        public override void DoSWDemand(double Supply) { }
        /// <summary>Does the sw uptake.</summary>
        /// <param name="SWDemand">The sw demand.</param>
        /// <exception cref="System.Exception">negative root growth??</exception>
        public override void DoSWUptake(double SWDemand)
        {


            dlt_sw_dep = CalculateWaterUptake(SWDemand, Soil.Water);

            if (SwimIsPresent)
            {
                dlt_sw_dep = (double[])Apsim.Get(this, "uptake_water_" + Plant.CropType);
                dlt_sw_dep = MathUtilities.Multiply_Value(dlt_sw_dep, -1);   // make them negative numbers.
            }
            else if (ArbitratorSWUptake != null)
            {
                //dlt_sw_dep = MathUtilities.Multiply_Value(AribtratorSWUptake, -1);   // make them negative numbers.
                dlt_sw_dep = ArbitratorSWUptake;   // make them negative numbers.
                //Util.ZeroArray(AribtratorSWUptake);
            }
            else { }
 
            Util.Debug("Root.dlt_sw_dep=%f", MathUtilities.Sum(dlt_sw_dep));
        }

        /// <summary>Calculate SW uptake for a given demand and soil water content</summary>
        public double[] CalculateWaterUptake(double SWDemand, double[] SW)
        {
            // potential extractable sw
            DoPotentialExtractableSW();

            // actual extractable sw (sw-ll)
            DoSWAvailable(SW);
            DoSWSupply(SW);
            return DoWaterUptakeInternal(SWDemand);

        }
        /// <summary>Calculate SW uptake for a given demand and soil water content</summary>
        public void CalculateNUptake(double[] NO3N, double[] NH4N, ref double[] NO3NUp, ref double[] NH4NUp)
        {
            // In the case of water we just moved the normal code into a method so that it could be recalled

            CalculateNSupply(NO3N, NH4N, ref no3gsm_uptake_pot, ref nh4gsm_uptake_pot);
            DoNUptakeCalculation(0.0);
            NO3NUp = MathUtilities.Multiply_Value(dlt_no3gsm, -10);
            NH4NUp = MathUtilities.Multiply_Value(dlt_nh4gsm, -10);
            
        }

        /// <summary>Calculate change in rooting depth.</summary>
        public void DoRootDepth()
        {
            // Firstly grow roots.
            //  the layer with root front
            int layer = FindLayerNo(RootDepth);

            dltRootDepth = RootDepthRate.Value * RootAdvanceFactorTemp.Value *
                            Math.Min(RootAdvanceFactorWaterStress.Value, SWFactorRootDepth.Value) *
                            xf[layer];

            // prevent roots partially entering layers where xf == 0
            int deepest_layer;
            for (deepest_layer = xf.Length - 1;
                deepest_layer >= 0 &&
                (xf[deepest_layer] <= 0.0 || getModifiedKL(deepest_layer) <= 0.0);
                deepest_layer--)
                ; /* nothing */

            int RootLayerMax = deepest_layer + 1;
            double RootDepthMax = MathUtilities.Sum(Soil.Thickness, 0, deepest_layer + 1, 0.0);
            dltRootDepth = MathUtilities.Constrain(dltRootDepth, double.MinValue, RootDepthMax - RootDepth);

            if (dltRootDepth < 0.0)
                throw new Exception("negative root growth??");

            Util.Debug("Root.dltRootDepth=%f", dltRootDepth);
            Util.Debug("Root.root_layer_max=%i", RootLayerMax);
            Util.Debug("Root.root_depth_max=%f", RootDepthMax);
        }


        // dry matter
        /// <summary>Gets the dm supply.</summary>
        /// <value>The dm supply.</value>
        public override double DMSupply { get { return 0.0; } }
        /// <summary>Gets the dm retrans supply.</summary>
        /// <value>The dm retrans supply.</value>
        public override double DMRetransSupply { get { return 0; } }
        /// <summary>Gets the DLT dm pot rue.</summary>
        /// <value>The DLT dm pot rue.</value>
        public override double dltDmPotRue { get { return 0.0; } }
        /// <summary>Gets the dm green demand.</summary>
        /// <value>The dm green demand.</value>
        public override double DMGreenDemand { get { return _DMGreenDemand; } }
        /// <summary>Gets the dm demand differential.</summary>
        /// <value>The dm demand differential.</value>
        public override double DMDemandDifferential { get { return 0; } }
        /// <summary>Does the dm demand.</summary>
        /// <param name="DMSupply">The dm supply.</param>
        public override void DoDMDemand(double DMSupply)
        {
            _DMGreenDemand = Math.Max(0.0, DMSupply);   //Just ask for all you can get for now - NIH.
        }
        /// <summary>Does the dm retranslocate.</summary>
        /// <param name="dlt_dm_retrans_to_fruit">The dlt_dm_retrans_to_fruit.</param>
        /// <param name="demand_differential_begin">The demand_differential_begin.</param>
        public override void DoDmRetranslocate(double dlt_dm_retrans_to_fruit, double demand_differential_begin) { }
        /// <summary>Gives the dm green.</summary>
        /// <param name="Delta">The delta.</param>
        public override void GiveDmGreen(double Delta)
        {
            Growth.StructuralWt += Delta * GrowthStructuralFractionStage.Value;
            Growth.NonStructuralWt += Delta * (1.0 - GrowthStructuralFractionStage.Value);
            Util.Debug("Root.Growth.StructuralWt=%f", Growth.StructuralWt);
            Util.Debug("Root.Growth.NonStructuralWt=%f", Growth.NonStructuralWt);
        }
        /// <summary>Does the senescence.</summary>
        public override void DoSenescence()
        {
            double fraction_senescing = MathUtilities.Constrain(DMSenescenceFraction.Value, 0.0, 1.0);

            Senescing.StructuralWt = (Live.StructuralWt + Growth.StructuralWt + Retranslocation.StructuralWt) * fraction_senescing;
            Senescing.NonStructuralWt = (Live.NonStructuralWt + Growth.NonStructuralWt + Retranslocation.NonStructuralWt) * fraction_senescing;
            Util.Debug("Root.Senescing.StructuralWt=%f", Senescing.StructuralWt);
            Util.Debug("Root.Senescing.NonStructuralWt=%f", Senescing.NonStructuralWt);
        }
        /// <summary>Does the detachment.</summary>
        public override void DoDetachment()
        {
            Detaching = Dead * SenescenceDetachmentFraction;
            Util.Debug("Root.Detaching.Wt=%f", Detaching.Wt);
            Util.Debug("Root.Detaching.N=%f", Detaching.N);
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
        public override double NSupply
        {
            get
            {
                int deepest_layer = FindLayerNo(RootDepth);
                return MathUtilities.Sum(no3gsm_uptake_pot, 0, deepest_layer + 1, 0) +
                       MathUtilities.Sum(nh4gsm_uptake_pot, 0, deepest_layer + 1, 0);
            }
        }
        /// <summary>Gets the n uptake.</summary>
        /// <value>The n uptake.</value>
        public override double NUptake
        {
            get
            {
                int deepest_layer = FindLayerNo(RootDepth);
                return -MathUtilities.Sum(dlt_no3gsm, 0, deepest_layer + 1, 0)
                        - MathUtilities.Sum(dlt_nh4gsm, 0, deepest_layer + 1, 0);
            }
        }
        /// <summary>Gets the soil n demand.</summary>
        /// <value>The soil n demand.</value>
        public override double SoilNDemand { get { return _SoilNDemand; } }
        /// <summary>Gets the n capacity.</summary>
        /// <value>The n capacity.</value>
        public override double NCapacity
        {
            get
            {
                return MathUtilities.Constrain(NMax - NDemand, 0.0, double.MaxValue);
            }
        }
        /// <summary>Gets the n demand differential.</summary>
        /// <value>The n demand differential.</value>
        public override double NDemandDifferential { get { return MathUtilities.Constrain(NDemand - Growth.N, 0.0, double.MaxValue); } }
        /// <summary>Gets the available retranslocate n.</summary>
        /// <value>The available retranslocate n.</value>
        public override double AvailableRetranslocateN
        {
            get
            {
                double N_min = n_conc_min * Live.Wt;
                double N_avail = MathUtilities.Constrain(Live.N - N_min, 0.0, double.MaxValue);
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
            Util.Debug("Root.NDemand=%f", _NDemand);
            Util.Debug("Root.NMax=%f", NMax);
        }
        /// <summary>Does the n demand1 pot.</summary>
        /// <param name="dltDmPotRue">The DLT dm pot rue.</param>
        public override void DoNDemand1Pot(double dltDmPotRue)
        {
            Biomass OldGrowth = Growth;
            Growth.StructuralWt = dltDmPotRue * MathUtilities.Divide(Live.Wt, Plant.TotalLive.Wt, 0.0);
            Util.Debug("Root.Growth.StructuralWt=%f", Growth.StructuralWt);
            Util.CalcNDemand(dltDmPotRue, dltDmPotRue, n_conc_crit, n_conc_max, Growth, Live, Retranslocation.N, 1.0,
                       ref _NDemand, ref NMax);
            Growth.StructuralWt = 0.0;
            Growth.NonStructuralWt = 0.0;
            Util.Debug("Root.NDemand=%f", _NDemand);
            Util.Debug("Root.NMax=%f", NMax);
        }
        /// <summary>Does the soil n demand.</summary>
        public override void DoSoilNDemand()
        {
            _SoilNDemand = NDemand - dlt_n_senesced_retrans;
            _SoilNDemand = MathUtilities.Constrain(_SoilNDemand, 0.0, double.MaxValue);
            Util.Debug("Root.SoilNDemand=%f", _SoilNDemand);
        }
        /// <summary>Does the n supply.</summary>
        /// <exception cref="System.NotImplementedException"></exception>
        public override void DoNSupply()
        {
            if (NUptakeFunction is NUptake3)
            {
                CalculateNSupply(Soil.NO3N, Soil.NH4N, ref no3gsm_uptake_pot, ref nh4gsm_uptake_pot);
            }
            else
                throw new NotImplementedException();
        }

        private void CalculateNSupply(double[] NO3N, double[] NH4N, ref double[] no3gsm_uptake_pot, ref double[] nh4gsm_uptake_pot)
        {
            double[] no3gsm = MathUtilities.Multiply_Value(NO3N, Conversions.kg2gm / Conversions.ha2sm);
            double[] nh4gsm = MathUtilities.Multiply_Value(NH4N, Conversions.kg2gm / Conversions.ha2sm);

            (NUptakeFunction as NUptake3).DoNUptake(RootDepth, no3gsm, nh4gsm,
                                             Soil.BD, Soil.Thickness, sw_avail, sw_avail_pot, no3gsm_min, nh4gsm_min,
                                             ref no3gsm_uptake_pot, ref nh4gsm_uptake_pot);
        }

        /// <summary>Does the n retranslocate.</summary>
        /// <param name="NSupply">The n supply.</param>
        /// <param name="GrainNDemand">The grain n demand.</param>
        public override void DoNRetranslocate(double NSupply, double GrainNDemand)
        {
            if (GrainNDemand >= NSupply)
            {
                // demand greater than or equal to supply
                // retranslocate all available N
                Retranslocation.StructuralN = AvailableRetranslocateN;
            }
            else
            {
                // supply greater than demand.
                // Retranslocate what is needed
                Retranslocation.StructuralN = GrainNDemand * MathUtilities.Divide(AvailableRetranslocateN, NSupply, 0.0);
            }
            Util.Debug("Root.Retranslocation.N=%f", Retranslocation.N);
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

            Util.Debug("Root.SenescingN=%f", SenescingN);
            Util.Debug("Root.dlt.n_senesced_trans=%f", dlt_n_senesced_trans);
        }
        /// <summary>Does the n senesced retranslocation.</summary>
        /// <param name="navail">The navail.</param>
        /// <param name="n_demand_tot">The n_demand_tot.</param>
        public override void DoNSenescedRetranslocation(double navail, double n_demand_tot)
        {
            dlt_n_senesced_retrans = navail * MathUtilities.Divide(NDemand, n_demand_tot, 0.0);
            Util.Debug("Root.dlt.n_senesced_retrans=%f", dlt_n_senesced_retrans);
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
            n_conc_crit = NConcentrationCritical;
            n_conc_min = NConcentrationMinimum;
            n_conc_max = NConcentrationMaximum;
            Util.Debug("Root.n_conc_crit=%f", n_conc_crit);
            Util.Debug("Root.n_conc_min=%f", n_conc_min);
            Util.Debug("Root.n_conc_max=%f", n_conc_max);
        }
        /// <summary>Zeroes the DLT n senesced trans.</summary>
        public override void ZeroDltNSenescedTrans()
        {
            dlt_n_senesced_trans = 0;
        }

        /// <summary>Does the n uptake.</summary>
        /// <param name="PotNFix">The pot n fix.</param>
        public override void DoNUptake(double PotNFix)
        {
            if (ArbitratorNO3Uptake!=null)
            {
                dlt_no3gsm = MathUtilities.Multiply_Value(ArbitratorNO3Uptake, -0.1);  // need to make it -ve and change from kg/ha to g/m2
                dlt_nh4gsm = MathUtilities.Multiply_Value(ArbitratorNH4Uptake, -0.1);
                }
             else
                DoNUptakeCalculation(PotNFix);

        }
        /// <summary>Does the n uptake.</summary>
        /// <param name="PotNFix">The pot n fix.</param>
        private void DoNUptakeCalculation(double PotNFix)
        {
            //if (SwimIsPresent)
            //{
            //    My.Get("uptake_no3_" + Plant.CropType, out dlt_no3gsm);
            //    MathUtilities.Multiply_Value(dlt_no3gsm, -Conversions.kg2gm/Conversions.ha2sm);   // convert units and make them negative.
            //}
            //else

            double n_demand = 0.0;
            foreach (Organ1 Organ in Plant.Organ1s)
                n_demand += Organ.SoilNDemand;

            int deepest_layer = FindLayerNo(RootDepth);


            double ngsm_supply = MathUtilities.Sum(no3gsm_uptake_pot, 0, deepest_layer + 1, 0)
                               + MathUtilities.Sum(nh4gsm_uptake_pot, 0, deepest_layer + 1, 0);


            if (NSupplyPreference == "fixation")
                n_demand = MathUtilities.Constrain(n_demand - PotNFix, 0.0, double.MaxValue);

            // get actual change in N contents
            Util.ZeroArray(dlt_no3gsm);
            Util.ZeroArray(dlt_nh4gsm);

            double scalef;
            if (n_demand > ngsm_supply)
            {
                scalef = 0.99999f;      // avoid taking it all up as it can
                // cause rounding errors to take
                // no3 below zero.
            }
            else
                scalef = MathUtilities.Divide(n_demand, ngsm_supply, 0.0);

            for (int layer = 0; layer <= deepest_layer; layer++)
            {
                // allocate nitrate
                double no3gsm_uptake = no3gsm_uptake_pot[layer] * scalef;
                dlt_no3gsm[layer] = -no3gsm_uptake;

                // allocate ammonium
                double nh4gsm_uptake = nh4gsm_uptake_pot[layer] * scalef;
                dlt_nh4gsm[layer] = -nh4gsm_uptake;
            }

            Util.Debug("Root.dlt_no3gsm=%f", MathUtilities.Sum(dlt_no3gsm));
            Util.Debug("Root.dlt_nh4gsm=%f", MathUtilities.Sum(dlt_nh4gsm));

                
        }


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
        /// <exception cref="System.Exception">Invalid root depth:  + RootDepth.ToString()</exception>
        public override void Update()
        {
            // send off detached roots before root structure is updated by plant death
            DisposeDetachedMaterial(Detaching, RootLength);

            Live = Live + Growth - Senescing;

            Dead = Dead - Detaching + Senescing;
            Live = Live + Retranslocation;
            Live.StructuralN = Live.N + dlt_n_senesced_retrans;

            Biomass dying = Live * Population.DyingFractionPlants;
            Live = Live - dying;
            Dead = Dead + dying;
            Senescing = Senescing + dying;

            Util.Debug("Root.Green.Wt=%f", Live.Wt);
            Util.Debug("Root.Green.N=%f", Live.N);
            Util.Debug("Root.Senesced.Wt=%f", Dead.Wt);
            Util.Debug("Root.Senesced.N=%f", Dead.N);
            Util.Debug("Root.Senescing.Wt=%f", Senescing.Wt);
            Util.Debug("Root.Senescing.N=%f", Senescing.N);

            RootDepth += dltRootDepth;

            for (int layer = 0; layer < Soil.Thickness.Length; layer++)
                RootLength[layer] += dltRootLength[layer];

            for (int layer = 0; layer < Soil.Thickness.Length; layer++)
            {
                RootLength[layer] -= dltRootLengthSenesced[layer];
                RootLengthSenesced[layer] += dltRootLengthSenesced[layer];
            }
            // Note that movement and detachment of C is already done, just
            // need to maintain relationship between length and mass
            // Note that this is not entirely accurate.  It links live root
            // weight with root length and so thereafter dead(and detaching)
            // root is assumed to have the same distribution as live roots.
            for (int layer = 0; layer < Soil.Thickness.Length; layer++)
            {
                dltRootLengthDead[layer] = RootLength[layer] * Population.DyingFractionPlants;
                RootLength[layer] -= dltRootLengthDead[layer];
                RootLengthSenesced[layer] += dltRootLengthDead[layer];
            }

            double CumDepth = MathUtilities.Sum(Soil.Thickness);
            if (RootDepth < 0 || RootDepth > CumDepth)
                throw new Exception("Invalid root depth: " + RootDepth.ToString());

            Util.Debug("root.RootDepth=%f", RootDepth);
            Util.Debug("root.RootLength=%f", MathUtilities.Sum(RootLength));
            Util.Debug("root.RootLengthSenesced=%f", MathUtilities.Sum(RootLengthSenesced));

            UpdateWaterAndNBalance();
        }

        #endregion

        #region Public interface specific to Root
        /// <summary>Gets or sets the root depth.</summary>
        /// <value>The root depth.</value>
        [XmlIgnore]
        [Units("mm")]
        public double RootDepth { get; set; }

        /// <summary>Gets the root sw uptake.</summary>
        /// <value>The root sw uptake.</value>
        [Units("mm")]
        public double[] RootSWUptake
        {
            get
            {
                double[] Uptake = dlt_sw_dep;
                for (int i = 0; i < Uptake.Length; i++)
                    Uptake[i] = Math.Abs(Uptake[i]);
                return Uptake;
            }
        }

        /// <summary>Gets the sw avail ratio.</summary>
        /// <value>The sw avail ratio.</value>
        public double SWAvailRatio
        {
            get
            {
                bool valuesFound = false;
                double ratio = 0.0;
                if (sw_avail_pot != null)
                {
                    
                    for (int i = 0; i < sw_avail_pot.Length; i++)
                    {
                        if (sw_avail_pot[i] > 0)
                        {
                            ratio += sw_avail[i] / sw_avail_pot[i];
                            valuesFound = true;
                        }
                    }
                }
                if (valuesFound)
                    return ratio;
                else
                    return 1.0;
            }
        }
        /// <summary>Gets the wet root fraction.</summary>
        /// <value>The wet root fraction.</value>
        public double WetRootFraction
        {
            get
            {
                if (RootDepth > 0.0)
                {
                    double[] RootFr = RootDist(1.0, RootLength);

                    double wet_root_fr = 0.0;
                    for (int layer = 0; layer != RootFr.Length; layer++)
                        wet_root_fr = wet_root_fr + WFPS(layer) * RootFr[layer];
                    return wet_root_fr;
                }
                else
                    return 0.0;
            }
        }
        /// <summary>Gets the fasw layered.</summary>
        /// <value>The fasw layered.</value>
        public double[] FASWLayered
        {
            get
            {
                if (ll_dep != null && DULmm != null)
                {
                    double[] FASW = new double[Soil.Thickness.Length];
                    for (int i = 0; i < Soil.Thickness.Length; i++)
                    {
                        FASW[i] = MathUtilities.Divide(Soil.Water[i] - ll_dep[i], DULmm[i] - ll_dep[i], 0.0);
                        FASW[i] = MathUtilities.Constrain(FASW[i], 0.0, 1.0);
                    }
                    return FASW;
                }
                else
                    return new double[0];
            }
        }
        /// <summary>Gets the fasw.</summary>
        /// <value>The fasw.</value>
        public double FASW
        {
            get
            {
                //  the layer with root front
                int layer = FindLayerNo(RootDepth);
                int deepest_layer = Soil.Thickness.Length - 1;

                double CumDepth = MathUtilities.Sum(Soil.Thickness, 0, layer + 1, 0.0);

                double rootdepth_in_layer = Soil.Thickness[layer] - (CumDepth - RootDepth);
                rootdepth_in_layer = MathUtilities.Constrain(rootdepth_in_layer, 0.0, Soil.Thickness[layer]);

                double weighting_factor = MathUtilities.Divide(rootdepth_in_layer, Soil.Thickness[layer], 0.0);
                int next_layer = Math.Min(layer + 1, deepest_layer);

                double[] faswlayered = FASWLayered;
                if (faswlayered.Length > 0)
                {
                    double fasw1 = FASWLayered[layer];
                    double fasw2 = FASWLayered[next_layer];

                    fasw1 = Math.Min(1.0, Math.Max(0.0, fasw1));
                    fasw2 = Math.Min(1.0, Math.Max(0.0, fasw2));
                    return weighting_factor * fasw2 + (1.0 - weighting_factor) * fasw1;
                }
                return 0;
            }
        }
        /// <summary>Root length density - needed by SWIM</summary>
        /// <value>The root length density.</value>
        [Units("mm/mm^3")]
        public double[] RootLengthDensity 
        { 
            get 
            {
                if (RootLength != null)
                    return MathUtilities.Divide(RootLength, Soil.Thickness);
                else
                    return new double[0];
            } 
        }
        /// <summary>Calculate the extractable soil water in the root zone (mm).</summary>
        /// <value>The esw in root zone.</value>
        internal double ESWInRootZone
        {
            get
            {
                double ESW = 0;
                int deepest_layer = FindLayerNo(RootDepth);
                for (int layer = 0; layer <= deepest_layer; layer++)
                    ESW += MathUtilities.Constrain(Soil.Water[layer] - ll_dep[layer], 0.0, double.MaxValue);
                return ESW;
            }
        }

        /// <summary>Return the index of the layer corresponding to the given depth</summary>
        /// <param name="depth">The depth.</param>
        /// <returns></returns>
        internal int FindLayerNo(double depth)
        {
            int i;
            double progressive_sum = 0.0;

            for (i = 0; i < Soil.Thickness.Length; i++)
            {
                progressive_sum = progressive_sum + Soil.Thickness[i];
                if (progressive_sum >= depth)
                    break;
            }
            if (i != 0 && i == Soil.Thickness.Length)
                return (i - 1); // last element in array
            return i;
        }

        /// <summary>
        /// Calculate the increase in root length density in each rooted
        /// layer based upon soil hospitality, moisture and fraction of
        /// layer explored by roots.
        /// </summary>
        internal void RootLengthGrowth()
        {
            Util.ZeroArray(dltRootLength);

            double depth_today = RootDepth + dltRootDepth;
            int deepest_layer = FindLayerNo(depth_today);

            double[] rlv_factor = new double[Soil.Thickness.Length];    // relative rooting factor for all layers

            double[] relativeRootRate = RelativeRootRate.Values;
            double[] sWFactorRootLength = SWFactorRootLength.Values;

            double rlv_factor_tot = 0.0;
            for (int layer = 0; layer <= deepest_layer; layer++)
            {
                double branching_factor = relativeRootRate[layer];

                rlv_factor[layer] = sWFactorRootLength[layer] *
                                    branching_factor *                                   // branching factor
                                    xf[layer] *                                          // growth factor
                                    MathUtilities.Divide(Soil.Thickness[layer], RootDepth, 0.0);   // space weighting factor

                rlv_factor[layer] = MathUtilities.Constrain(rlv_factor[layer], 1e-6, double.MaxValue);
                rlv_factor_tot += rlv_factor[layer];
            }

            double dlt_length_tot = Growth.Wt / Conversions.sm2smm * SpecificRootLength;

            for (int layer = 0; layer <= deepest_layer; layer++)
                dltRootLength[layer] = dlt_length_tot * MathUtilities.Divide(rlv_factor[layer], rlv_factor_tot, 0.0);
            Util.Debug("Root.dltRootLength=%f", MathUtilities.Sum(dltRootLength));
        }

        /// <summary>
        /// Calculate root length senescence based upon changes in senesced root
        /// biomass and the specific root length.
        /// </summary>
        internal void DoSenescenceLength()
        {
            Util.ZeroArray(dltRootLengthSenesced);
            double senesced_length = Senescing.Wt / Conversions.sm2smm * SpecificRootLength;
            dltRootLengthSenesced = RootDist(senesced_length, RootLength);
            Util.Debug("Root.dltRootLengthSenesced=%f", MathUtilities.Sum(dltRootLengthSenesced));
        }

        /// <summary>Write a summary to the summary file.</summary>
        /// <param name="writer">The writer.</param>
        internal void WriteSummary(TextWriter writer)
        {
            writer.WriteLine();
            writer.WriteLine("               Root Profile");
            writer.WriteLine("-----------------------------------------------");
            writer.WriteLine(" Layer       Kl           Lower    Exploration");
            writer.WriteLine(" Depth     Factor         Limit      Factor");
            writer.WriteLine(" (mm)         ()        (mm/mm)       (0-1)");
            writer.WriteLine("-----------------------------------------------");

            double dep_tot, esw_tot;                      // total depth of soil & ll

            dep_tot = esw_tot = 0.0;
            for (int layer = 0; layer < Soil.Thickness.Length; layer++)
            {
                writer.WriteLine(string.Format("{0,9:F1}{1,10:F3}{2,15:F3}{3,12:F3}",
                                  Soil.Thickness[layer],
                                  getModifiedKL(layer),
                                  MathUtilities.Divide(ll_dep[layer], Soil.Thickness[layer], 0.0),
                                  xf[layer]));
                dep_tot += Soil.Thickness[layer];
                esw_tot += DULmm[layer] - ll_dep[layer];
            }
             writer.WriteLine("-----------------------------------------------");
            if (HaveModifiedKLValues)
                writer.WriteLine("**** KL's have been modified using either CL, EC or ESP values.");

            writer.WriteLine("Extractable SW: {0,5:F0}mm in {1,5:F0}mm total depth ({2,3:F0}%).",
                                            esw_tot,
                                            dep_tot,
                                            Conversions.fract2pcnt * MathUtilities.Divide(esw_tot, dep_tot, 0.0));
        }

        /// <summary>Remove biomass from the root system due to senescence or plant death</summary>
        /// <param name="Fraction">The fraction.</param>
        internal void RemoveBiomassFraction(double Fraction)
        {
            Biomass Dead;
            Dead = Live * DieBackFraction * Fraction;
            // however dead roots have a given N concentration
            Dead.StructuralN = Dead.Wt * NSenescenceConcentration;

            Live = Live - Dead;
            Dead = Dead + Dead;

            // do root_length
            double[] dltRootLengthDie = new double[Soil.Thickness.Length];
            double Die_length = Dead.Wt / Conversions.sm2smm * SpecificRootLength;
            RootDist(Die_length, dltRootLengthDie);
            for (int layer = 0; layer < Soil.Thickness.Length; layer++)
                RootLength[layer] -= dltRootLengthDie[layer];
        }

        #endregion

        #region Event handlers
        /// <summary>Called when [simulation commencing].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            SwimIsPresent = swim3 > 0;
            if (SwimIsPresent)
                Summary.WriteMessage(this, "Using SWIM3 for Soil Water Uptake.");

            Senescing = new Biomass();
            Retranslocation = new Biomass();
            Growth = new Biomass();
            Detaching = new Biomass();
            GreenRemoved = new Biomass();
            SenescedRemoved = new Biomass();

            dlt_sw_dep = new double[Soil.Thickness.Length];
            sw_avail = new double[Soil.Thickness.Length];
            sw_avail_pot = new double[Soil.Thickness.Length];
            sw_supply = new double[Soil.Thickness.Length];

            dlt_no3gsm = new double[Soil.Thickness.Length];
            dlt_nh4gsm = new double[Soil.Thickness.Length];
            no3gsm_uptake_pot = new double[Soil.Thickness.Length];
            nh4gsm_uptake_pot = new double[Soil.Thickness.Length];
            dltRootLength = new double[Soil.Thickness.Length];
            dltRootLengthSenesced = new double[Soil.Thickness.Length];
            dltRootLengthDead = new double[Soil.Thickness.Length];
            no3gsm_min = new double[Soil.Thickness.Length];
            nh4gsm_min = new double[Soil.Thickness.Length];
            RootLength = new double[Soil.Thickness.Length];
            RootLengthSenesced = new double[Soil.Thickness.Length];

            SoilCrop soilCrop = Soil.Crop(Plant.Name) as SoilCrop;
            ll = soilCrop.LL;
            kl = soilCrop.KL;
            xf = soilCrop.XF;
            DULmm = MathUtilities.Multiply(Soil.DUL, Soil.Thickness);

            ll_dep = MathUtilities.Multiply(ll, Soil.Thickness);
            Util.ZeroArray(no3gsm_min);
            Util.ZeroArray(nh4gsm_min);
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

            dlt_n_senesced_retrans = 0.0;
            dlt_n_senesced_trans = 0.0;

            _DMGreenDemand = 0.0;
            _NDemand = 0.0;
            _SoilNDemand = 0.0;
            NMax = 0.0;
            sw_demand = 0.0; dltRootDepth = 0.0;
            Util.ZeroArray(dltRootLength);
            Util.ZeroArray(dltRootLengthSenesced);
            Util.ZeroArray(dltRootLengthDead);
            Util.ZeroArray(dlt_sw_dep);
            Util.ZeroArray(sw_avail);
            Util.ZeroArray(sw_avail_pot);
            Util.ZeroArray(sw_supply);
            Util.ZeroArray(dlt_no3gsm);
            Util.ZeroArray(dlt_nh4gsm);
            Util.ZeroArray(no3gsm_uptake_pot);
            Util.ZeroArray(nh4gsm_uptake_pot);
        }
        /// <summary>Called when [harvest].</summary>
        /// <param name="Harvest">The harvest.</param>
        /// <param name="BiomassRemoved">The biomass removed.</param>
        public override void OnHarvest(HarvestType Harvest, BiomassRemovedType BiomassRemoved)
        {
            Biomass Dead;
            Dead = Live * DieBackFraction;

            // however dead roots have a given N concentration
            Dead.StructuralN = Dead.Wt * NSenescenceConcentration;

            Live = Live - Dead;
            Dead = Dead + Dead;

            int i = Util.IncreaseSizeOfBiomassRemoved(BiomassRemoved);

            // Unlike above ground parts, no roots go to surface residue module.
            BiomassRemoved.dm_type[i] = Name;
            BiomassRemoved.fraction_to_residue[i] = 0.0F;
            BiomassRemoved.dlt_crop_dm[i] = 0.0F;
            BiomassRemoved.dlt_dm_n[i] = 0.0F;
            BiomassRemoved.dlt_dm_p[i] = 0.0F;
        }
        /// <summary>Called when [end crop].</summary>
        /// <param name="BiomassRemoved">The biomass removed.</param>
        public override void OnEndCrop(BiomassRemovedType BiomassRemoved)
        {
            DisposeDetachedMaterial(Live, RootLength);
            DisposeDetachedMaterial(Dead, RootLengthSenesced);
            Dead.Clear();
            Live.Clear();
        }

        /// <summary>Called when [phase changed].</summary>
        /// <param name="PhenologyChange">The phenology change.</param>
        [EventSubscribe("PhaseChanged")]
        private void OnPhaseChanged(PhaseChangedType PhenologyChange)
        {
            if (PhenologyChange.NewPhaseName == "GerminationToEmergence")
                RootDepth = InitialRootDepth;
            else if (PhenologyChange.NewPhaseName == "EmergenceToEndOfJuvenile")
            {
                Live.StructuralWt = InitialWt * Population.Density;
                Live.StructuralN = InitialNConcentration * Live.StructuralWt;

                // initial root length (mm/mm^2)
                double initial_root_length = Live.Wt / Conversions.sm2smm * SpecificRootLength;

                // initial root length density (mm/mm^3)
                double rld = MathUtilities.Divide(initial_root_length, RootDepth, 0.0);

                int deepest_layer = FindLayerNo(RootDepth);

                for (int layer = 0; layer <= deepest_layer; layer++)
                    RootLength[layer] = rld * Soil.Thickness[layer] * RootProportion(layer, RootDepth);

                Util.Debug("Root.InitGreen.StructuralWt=%f", Live.StructuralWt);
                Util.Debug("Root.InitGreen.StructuralN=%f", Live.StructuralN);
                Util.Debug("Root.InitRootLength=%f", MathUtilities.Sum(RootLength));
            }
        }

        #endregion

        #region Private functionality

        /// <summary>Disposes the detached material.</summary>
        /// <param name="BiomassToDisposeOf">The biomass to dispose of.</param>
        /// <param name="RootLength">Length of the root.</param>
        private void DisposeDetachedMaterial(Biomass BiomassToDisposeOf, double[] RootLength)
        {
            if (BiomassToDisposeOf.Wt > 0.0)
            {
                // DM
                double[] dlt_dm_incorp = RootDist(BiomassToDisposeOf.Wt * Conversions.gm2kg / Conversions.sm2ha, RootLength);

                // Nitrogen
                double[] dlt_N_incorp = RootDist(BiomassToDisposeOf.N * Conversions.gm2kg / Conversions.sm2ha, RootLength);

                // Phosporous
                //double[] dlt_P_incorp = RootDist(BiomassToDisposeOf.P * Conversions.gm2kg / Conversions.sm2ha);

                FOMLayerType IncorpFOMData = new FOMLayerType();
                IncorpFOMData.Type = Plant.CropType;
                Util.Debug("Root.IncorpFOM.Type=%s", IncorpFOMData.Type.ToLower());
                IncorpFOMData.Layer = new FOMLayerLayerType[dlt_dm_incorp.Length];
                for (int i = 0; i != dlt_dm_incorp.Length; i++)
                {
                    IncorpFOMData.Layer[i] = new FOMLayerLayerType();
                    IncorpFOMData.Layer[i].FOM = new FOMType();
                    IncorpFOMData.Layer[i].FOM.amount = (float)dlt_dm_incorp[i];
                    IncorpFOMData.Layer[i].FOM.N = (float)dlt_N_incorp[i];
                    //IncorpFOMData.Layer[i].FOM.P = (float)dlt_P_incorp[i];
                    IncorpFOMData.Layer[i].FOM.C = (float)0.0;
                    IncorpFOMData.Layer[i].FOM.AshAlk = (float)0.0;
                    IncorpFOMData.Layer[i].CNR = 0;
                    IncorpFOMData.Layer[i].LabileP = 0;
                    Util.Debug("Root.IncorpFOM.FOM.amount=%f2", IncorpFOMData.Layer[i].FOM.amount);
                    Util.Debug("Root.IncorpFOM.FOM.N=%f", IncorpFOMData.Layer[i].FOM.N);

                }
                IncorpFOM.Invoke(IncorpFOMData);
            }
            else
            {
                // no roots to incorporate
            }
        }

        /// <summary>
        /// Calculate a modified KL value as per:
        /// Hochman et. al. (2007) Simulating the effects of saline and sodic subsoils on wheat
        /// crops growing on Vertosols. Australian Journal of Agricultural Research, 58, 802–810
        /// Will use one of CL, ESP and EC in that order to modified KL.
        /// </summary>
        /// <param name="i">The i.</param>
        /// <returns></returns>
        private double getModifiedKL(int i)
        {
            if (ModifyKL)
            {
                double KLFactor = 1.0;
                if (cl != null && cl.Length > 1)
                    KLFactor = Math.Min(1.0, ClA * Math.Exp(ClB * cl[i]));

                else if (Soil.ESP.Length > 1)
                    KLFactor = Math.Min(1.0, ESPA * Math.Exp(ESPB * Soil.ESP[i]));

                else if (Soil.EC.Length > 1)
                    KLFactor = Math.Min(1.0, ECA * Math.Exp(ECB * Soil.EC[i]));

                if (KLFactor != 1.0)
                    HaveModifiedKLValues = true;

                return kl[i] * KLFactor;
            }
            else
                return kl[i];
        }

        /// <summary>Return potential available soil water from each layer in the root zone.</summary>
        private void DoPotentialExtractableSW()
        {
            Util.ZeroArray(sw_avail_pot);

            int deepest_layer = FindLayerNo(RootDepth);
            for (int layer = 0; layer <= deepest_layer; layer++)
                sw_avail_pot[layer] = DULmm[layer] - ll_dep[layer];

            // correct bottom layer for actual root penetration
            sw_avail_pot[deepest_layer] = sw_avail_pot[deepest_layer] * RootProportion(deepest_layer, RootDepth);

            Util.Debug("Root.deepest_layer=%i", deepest_layer);
            Util.Debug("Root.sw_avail_pot[deepest_layer]=%f", sw_avail_pot[deepest_layer]);
        }

        /// <summary>
        /// Return actual water available for extraction from each layer in the
        /// soil profile by the crop (mm water)
        /// </summary>
        private void DoSWAvailable(double[] SW)
        {
            Util.ZeroArray(sw_avail);

            int deepest_layer = FindLayerNo(RootDepth);
            for (int layer = 0; layer <= deepest_layer; layer++)
            {
                sw_avail[layer] = SW[layer] - ll_dep[layer];
                sw_avail[layer] = MathUtilities.Constrain(sw_avail[layer], 0.0, double.MaxValue);
            }
            // correct bottom layer for actual root penetration
            sw_avail[deepest_layer] = sw_avail[deepest_layer] * RootProportion(deepest_layer, RootDepth);
            Util.Debug("Root.sw_avail[deepest_layer]=%f", sw_avail[deepest_layer]);
        }

        /// <summary>
        /// Return potential water uptake from each layer of the soil profile
        /// by the crop (mm water). This represents the maximum amount in each
        /// layer regardless of lateral root distribution but takes account of
        /// root depth in bottom layer.
        /// </summary>
        private void DoSWSupply(double[] SW)
        {
            Util.ZeroArray(sw_supply);

            int deepest_layer = FindLayerNo(RootDepth);
            double sw_avail;
            for (int i = 0; i <= deepest_layer; i++)
            {
                sw_avail = (SW[i] - ll_dep[i]);
                sw_supply[i] = sw_avail * getModifiedKL(i);
                sw_supply[i] = MathUtilities.Constrain(sw_supply[i], 0.0, double.MaxValue);
            }
            //now adjust bottom layer for depth of root
            sw_supply[deepest_layer] = sw_supply[deepest_layer] * RootProportion(deepest_layer, RootDepth);
            Util.Debug("Root.sw_supply[deepest_layer]=%f", sw_supply[deepest_layer]);
        }

        /// <summary>Calculate todays daily water uptake by this root system</summary>
        /// <param name="sw_demand">The sw_demand.</param>
        private double[] DoWaterUptakeInternal(double sw_demand)
        {
            double[] dlt = new double[sw_supply.Length];
            int deepest_layer = FindLayerNo(RootDepth);
            double sw_supply_sum = MathUtilities.Sum(sw_supply, 0, deepest_layer + 1, 0.0);

            if ((sw_supply_sum < 0.0) || (sw_demand < 0.0))
            {
                //we have no uptake - there is no demand or potential
                Util.ZeroArray(dlt);
            }
            else
            {
                // get actual uptake
                Util.ZeroArray(dlt);
                if (sw_demand < sw_supply_sum)
                {
                    // demand is less than what roots could take up.
                    // water is non-limiting.
                    // distribute demand proportionately in all layers.
                    for (int layer = 0; layer <= deepest_layer; layer++)
                    {
                        dlt[layer] = -1.0 * MathUtilities.Divide(sw_supply[layer], sw_supply_sum, 0.0) * sw_demand;
                    }
                }
                else
                {
                    // water is limiting - not enough to meet demand so take
                    // what is available (potential)
                    for (int layer = 0; layer <= deepest_layer; layer++)
                    {
                        dlt[layer] = -1 * sw_supply[layer];
                    }
                }
            }
            return dlt;
        }

        /// <summary>
        /// Returns the proportion of layer that has roots in it (0-1).
        /// Each element of "dlayr" holds the height of  the
        /// corresponding soil layer.  The height of the top layer is
        /// held in "dlayr"(1), and the rest follow in sequence down
        /// into the soil profile.  Given a root depth of "root_depth",
        /// this function will return the proportion of "dlayr"("layer")
        /// which has roots in it  (a value in the range 0..1).
        /// </summary>
        /// <param name="layer">The layer.</param>
        /// <param name="RootDepth">The root depth.</param>
        /// <returns></returns>
        private double RootProportion(int layer, double RootDepth)
        {
            double depth_to_layer_bottom = MathUtilities.Sum(Soil.Thickness, 0, layer + 1, 0.0);
            double depth_to_layer_top = depth_to_layer_bottom - Soil.Thickness[layer];
            double depth_to_root = Math.Min(depth_to_layer_bottom, RootDepth);
            double depth_of_root_in_layer = Math.Max(0.0, depth_to_root - depth_to_layer_top);
            return (MathUtilities.Divide(depth_of_root_in_layer, Soil.Thickness[layer], 0.0));
        }

        /// <summary>
        /// Distribute root material over profile based upon root
        /// length distribution.
        /// </summary>
        /// <param name="root_sum">The root_sum.</param>
        /// <param name="RootLength">Length of the root.</param>
        /// <returns></returns>
        private double[] RootDist(double root_sum, double[] RootLength)
        {
            int deepest_layer = FindLayerNo(RootDepth);

            double root_length_sum = MathUtilities.Sum(RootLength, 0, deepest_layer + 1, 0.0);

            double[] RootArray = new double[RootLength.Length];
            for (int layer = 0; layer <= deepest_layer; layer++)
                RootArray[layer] = root_sum * MathUtilities.Divide(RootLength[layer], root_length_sum, 0.0);
            return RootArray;
        }

        /// <summary>WFPSs the specified layer.</summary>
        /// <param name="layer">The layer.</param>
        /// <returns></returns>
        private double WFPS(int layer)
        {
            double wfps = MathUtilities.Divide(Soil.Water[layer] - Soil.SoilWater.LL15mm[layer],
                                            Soil.SoilWater.SATmm[layer] - Soil.SoilWater.LL15mm[layer], 0.0);
            return MathUtilities.Constrain(wfps, 0.0, 1.0);
        }

        /// <summary>Update the water and N balance.</summary>
        private void UpdateWaterAndNBalance()
        {
            NitrogenChangedType NitrogenUptake = new NitrogenChangedType();
            NitrogenUptake.Sender = "Plant";
            NitrogenUptake.SenderType = "Plant";
            NitrogenUptake.DeltaNO3 = MathUtilities.Multiply_Value(dlt_no3gsm, Conversions.gm2kg / Conversions.sm2ha);
            NitrogenUptake.DeltaNH4 = MathUtilities.Multiply_Value(dlt_nh4gsm, Conversions.gm2kg / Conversions.sm2ha);
            Util.Debug("Root.NitrogenUptake.DeltaNO3=%f", MathUtilities.Sum(NitrogenUptake.DeltaNO3));
            Util.Debug("Root.NitrogenUptake.DeltaNH4=%f", MathUtilities.Sum(NitrogenUptake.DeltaNH4));
            NitrogenChanged.Invoke(NitrogenUptake);

            // Send back delta water and nitrogen back to APSIM.
            if (!SwimIsPresent)
            {
                WaterChangedType WaterUptake = new WaterChangedType();
                WaterUptake.DeltaWater = dlt_sw_dep;
                Util.Debug("Root.WaterUptake=%f", MathUtilities.Sum(WaterUptake.DeltaWater));
                WaterChanged.Invoke(WaterUptake);

            }
        }

        #endregion


        #region Grazing
        /// <summary>Gets the available to animal.</summary>
        /// <value>The available to animal.</value>
        public override AvailableToAnimalelementType[] AvailableToAnimal { get { return null; } }
        /// <summary>Sets the removed by animal.</summary>
        /// <value>The removed by animal.</value>
        public override RemovedByAnimalType RemovedByAnimal { set { } }
        #endregion
    }
}
