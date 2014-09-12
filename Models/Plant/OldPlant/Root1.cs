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

namespace Models.PMF.OldPlant
{
    [Serializable]
    public class Root1 : BaseOrgan1, BelowGround
    {

        [Link]
        ISummary Summary = null;

        #region Parameters read from XML file and links to other functions.
        [Link]
        public Plant15 Plant;

        [Link] Function RootAdvanceFactorTemp = null;
        [Link] Function RootAdvanceFactorWaterStress = null;
        [Link] Function SWFactorRootDepth = null;
        [Link] Function SWFactorRootLength = null;
        [Link] Function RootDepthRate = null;

        [Link]
        Population1 Population = null;

        [Link] Function RelativeRootRate = null;
        [Link] Function DMSenescenceFraction = null;
        [Link] Function GrowthStructuralFractionStage = null;

        [Link]
        object NUptakeFunction = null;

        [Link]
        Soil Soil = null;

        public double NConcentrationCritical { get; set; }

        public double NConcentrationMinimum { get; set; }

        public double NConcentrationMaximum { get; set; }

        public double InitialRootDepth { get; set; }

        public double DieBackFraction { get; set; }

        public double[] cl { get; set; }

        public double[] ll { get; set; }

        public double[] kl { get; set; }

        public double[] xf { get; set; }

        public bool ModifyKL { get; set; }

        public double ClA { get; set; }

        public double ClB { get; set; }

        public double ESPA { get; set; }

        public double ESPB { get; set; }

        public double ECA { get; set; }

        public double ECB { get; set; }

        public double NDeficitUptakeFraction { get; set; }

        public double NSenescenceConcentration { get; set; }

        public string NSupplyPreference { get; set; }

        public double SenescenceDetachmentFraction { get; set; }

        public double InitialWt { get; set; }

        public double InitialNConcentration { get; set; }

        public double SpecificRootLength { get; set; }

        #endregion

        #region Variables we need from other modules
        
        //Fixme, this needs to talk to swim
        double swim3 = double.MinValue;
        #endregion

        #region Events we're going to publish at some point.
        
        public event FOMLayerDelegate IncorpFOM;

        
        public event WaterChangedDelegate WaterChanged;

        
        public event NitrogenChangedDelegate NitrogenChanged;
        #endregion

        #region Private variables
        private bool SwimIsPresent = false;
        private double[] dlt_sw_dep;
        private double[] sw_avail;
        private double[] sw_avail_pot;
        private double[] sw_supply;
        private double[] dlt_no3gsm;
        private double[] dlt_nh4gsm;
        private double[] no3gsm_uptake_pot;
        private double[] nh4gsm_uptake_pot;
        private double dltRootDepth;
        private double[] dltRootLength;
        private double[] dltRootLengthSenesced;
        private double[] dltRootLengthDead;
        private double[] ll_dep;
        private double[] RootLength;
        private double[] no3gsm_min;
        private double[] nh4gsm_min;
        private bool HaveModifiedKLValues = false;
        private double[] RootLengthSenesced;
        private double dlt_n_senesced_retrans;           // plant N retranslocated to/from (+/-) senesced part to/from <<somewhere else??>> (g/m^2)
        private double dlt_n_senesced_trans;


        private double _DMGreenDemand;
        private double _NDemand;
        private double _SoilNDemand;
        private double NMax;
        private double sw_demand;
        private double n_conc_crit = 0;
        private double n_conc_max = 0;
        private double n_conc_min = 0;
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
        public override double SWSupply { get { return Utility.Math.Sum(sw_supply); } }
        public override double SWDemand { get { return sw_demand; } }
        public override double SWUptake { get { return -Utility.Math.Sum(dlt_sw_dep); } }
        public override void DoSWDemand(double Supply) { }
        public override void DoSWUptake(double SWDemand)
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
            double RootDepthMax = Utility.Math.Sum(Soil.SoilWater.dlayer, 0, deepest_layer + 1, 0.0);
            dltRootDepth = Utility.Math.Constrain(dltRootDepth, double.MinValue, RootDepthMax - RootDepth);

            if (dltRootDepth < 0.0)
                throw new Exception("negative root growth??");

            Util.Debug("Root.dltRootDepth=%f", dltRootDepth);
            Util.Debug("Root.root_layer_max=%i", RootLayerMax);
            Util.Debug("Root.root_depth_max=%f", RootDepthMax);

            // potential extractable sw
            DoPotentialExtractableSW();

            // actual extractable sw (sw-ll)
            DoSWAvailable();

            DoSWSupply();

            if (SwimIsPresent)
            {
                dlt_sw_dep = (double[])Apsim.Get(this, "uptake_water_" + Plant.CropType);
                dlt_sw_dep = Utility.Math.Multiply_Value(dlt_sw_dep, -1);   // make them negative numbers.
            }
            else
                DoWaterUptakeInternal(SWDemand);
            Util.Debug("Root.dlt_sw_dep=%f", Utility.Math.Sum(dlt_sw_dep));
        }


        // dry matter
        public override double DMSupply { get { return 0.0; } }
        public override double DMRetransSupply { get { return 0; } }
        public override double dltDmPotRue { get { return 0.0; } }
        public override double DMGreenDemand { get { return _DMGreenDemand; } }
        public override double DMDemandDifferential { get { return 0; } }
        public override void DoDMDemand(double DMSupply)
        {
            _DMGreenDemand = Math.Max(0.0, DMSupply);   //Just ask for all you can get for now - NIH.
        }
        public override void DoDmRetranslocate(double dlt_dm_retrans_to_fruit, double demand_differential_begin) { }
        public override void GiveDmGreen(double Delta)
        {
            Growth.StructuralWt += Delta * GrowthStructuralFractionStage.Value;
            Growth.NonStructuralWt += Delta * (1.0 - GrowthStructuralFractionStage.Value);
            Util.Debug("Root.Growth.StructuralWt=%f", Growth.StructuralWt);
            Util.Debug("Root.Growth.NonStructuralWt=%f", Growth.NonStructuralWt);
        }
        public override void DoSenescence()
        {
            double fraction_senescing = Utility.Math.Constrain(DMSenescenceFraction.Value, 0.0, 1.0);

            Senescing.StructuralWt = (Live.StructuralWt + Growth.StructuralWt + Retranslocation.StructuralWt) * fraction_senescing;
            Senescing.NonStructuralWt = (Live.NonStructuralWt + Growth.NonStructuralWt + Retranslocation.NonStructuralWt) * fraction_senescing;
            Util.Debug("Root.Senescing.StructuralWt=%f", Senescing.StructuralWt);
            Util.Debug("Root.Senescing.NonStructuralWt=%f", Senescing.NonStructuralWt);
        }
        public override void DoDetachment()
        {
            Detaching = Dead * SenescenceDetachmentFraction;
            Util.Debug("Root.Detaching.Wt=%f", Detaching.Wt);
            Util.Debug("Root.Detaching.N=%f", Detaching.N);
        }
        public override void RemoveBiomass()
        {
            Live = Live - GreenRemoved;
            Dead = Dead - SenescedRemoved;
        }

        // nitrogen
        
        public override double NDemand { get { return _NDemand; } }
        
        public override double NSupply
        {
            get
            {
                int deepest_layer = FindLayerNo(RootDepth);
                return Utility.Math.Sum(no3gsm_uptake_pot, 0, deepest_layer + 1, 0) +
                       Utility.Math.Sum(nh4gsm_uptake_pot, 0, deepest_layer + 1, 0);
            }
        }
        public override double NUptake
        {
            get
            {
                int deepest_layer = FindLayerNo(RootDepth);
                return -Utility.Math.Sum(dlt_no3gsm, 0, deepest_layer + 1, 0)
                        - Utility.Math.Sum(dlt_nh4gsm, 0, deepest_layer + 1, 0);
            }
        }
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
            Util.Debug("Root.NDemand=%f", _NDemand);
            Util.Debug("Root.NMax=%f", NMax);
        }
        public override void DoNDemand1Pot(double dltDmPotRue)
        {
            Biomass OldGrowth = Growth;
            Growth.StructuralWt = dltDmPotRue * Utility.Math.Divide(Live.Wt, Plant.TotalLive.Wt, 0.0);
            Util.Debug("Root.Growth.StructuralWt=%f", Growth.StructuralWt);
            Util.CalcNDemand(dltDmPotRue, dltDmPotRue, n_conc_crit, n_conc_max, Growth, Live, Retranslocation.N, 1.0,
                       ref _NDemand, ref NMax);
            Growth.StructuralWt = 0.0;
            Growth.NonStructuralWt = 0.0;
            Util.Debug("Root.NDemand=%f", _NDemand);
            Util.Debug("Root.NMax=%f", NMax);
        }
        public override void DoSoilNDemand()
        {
            _SoilNDemand = NDemand - dlt_n_senesced_retrans;
            _SoilNDemand = Utility.Math.Constrain(_SoilNDemand, 0.0, double.MaxValue);
            Util.Debug("Root.SoilNDemand=%f", _SoilNDemand);
        }
        public override void DoNSupply()
        {
            if (NUptakeFunction is NUptake3)
            {
                double[] no3gsm = Utility.Math.Multiply_Value(Soil.SoilNitrogen.no3, Conversions.kg2gm / Conversions.ha2sm);
                double[] nh4gsm = Utility.Math.Multiply_Value(Soil.SoilNitrogen.nh4, Conversions.kg2gm / Conversions.ha2sm);

                (NUptakeFunction as NUptake3).DoNUptake(RootDepth, no3gsm, nh4gsm,
                                                 Soil.BD, Soil.SoilWater.dlayer, sw_avail, sw_avail_pot, no3gsm_min, nh4gsm_min,
                                                 ref no3gsm_uptake_pot, ref nh4gsm_uptake_pot);
            }
            else
                throw new NotImplementedException();
        }

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
                Retranslocation.StructuralN = GrainNDemand * Utility.Math.Divide(AvailableRetranslocateN, NSupply, 0.0);
            }
            Util.Debug("Root.Retranslocation.N=%f", Retranslocation.N);
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

            Util.Debug("Root.SenescingN=%f", SenescingN);
            Util.Debug("Root.dlt.n_senesced_trans=%f", dlt_n_senesced_trans);
        }
        public override void DoNSenescedRetranslocation(double navail, double n_demand_tot)
        {
            dlt_n_senesced_retrans = navail * Utility.Math.Divide(NDemand, n_demand_tot, 0.0);
            Util.Debug("Root.dlt.n_senesced_retrans=%f", dlt_n_senesced_retrans);
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
            n_conc_crit = NConcentrationCritical;
            n_conc_min = NConcentrationMinimum;
            n_conc_max = NConcentrationMaximum;
            Util.Debug("Root.n_conc_crit=%f", n_conc_crit);
            Util.Debug("Root.n_conc_min=%f", n_conc_min);
            Util.Debug("Root.n_conc_max=%f", n_conc_max);
        }
        public override void ZeroDltNSenescedTrans()
        {
            dlt_n_senesced_trans = 0;
        }
        public override void DoNUptake(double PotNFix)
        {
            //if (SwimIsPresent)
            //{
            //    My.Get("uptake_no3_" + Plant.CropType, out dlt_no3gsm);
            //    Utility.Math.Multiply_Value(dlt_no3gsm, -Conversions.kg2gm/Conversions.ha2sm);   // convert units and make them negative.
            //}
            //else

            double n_demand = 0.0;
            foreach (Organ1 Organ in Plant.Organ1s)
                n_demand += Organ.SoilNDemand;

            int deepest_layer = FindLayerNo(RootDepth);

            double ngsm_supply = Utility.Math.Sum(no3gsm_uptake_pot, 0, deepest_layer + 1, 0)
                               + Utility.Math.Sum(nh4gsm_uptake_pot, 0, deepest_layer + 1, 0);


            if (NSupplyPreference == "fixation")
                n_demand = Utility.Math.Constrain(n_demand - PotNFix, 0.0, double.MaxValue);

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
                scalef = Utility.Math.Divide(n_demand, ngsm_supply, 0.0);

            for (int layer = 0; layer <= deepest_layer; layer++)
            {
                // allocate nitrate
                double no3gsm_uptake = no3gsm_uptake_pot[layer] * scalef;
                dlt_no3gsm[layer] = -no3gsm_uptake;

                // allocate ammonium
                double nh4gsm_uptake = nh4gsm_uptake_pot[layer] * scalef;
                dlt_nh4gsm[layer] = -nh4gsm_uptake;
            }

            Util.Debug("Root.dlt_no3gsm=%f", Utility.Math.Sum(dlt_no3gsm));
            Util.Debug("Root.dlt_nh4gsm=%f", Utility.Math.Sum(dlt_nh4gsm));
        }


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

            for (int layer = 0; layer < Soil.SoilWater.dlayer.Length; layer++)
                RootLength[layer] += dltRootLength[layer];

            for (int layer = 0; layer < Soil.SoilWater.dlayer.Length; layer++)
            {
                RootLength[layer] -= dltRootLengthSenesced[layer];
                RootLengthSenesced[layer] += dltRootLengthSenesced[layer];
            }
            // Note that movement and detachment of C is already done, just
            // need to maintain relationship between length and mass
            // Note that this is not entirely accurate.  It links live root
            // weight with root length and so thereafter dead(and detaching)
            // root is assumed to have the same distribution as live roots.
            for (int layer = 0; layer < Soil.SoilWater.dlayer.Length; layer++)
            {
                dltRootLengthDead[layer] = RootLength[layer] * Population.DyingFractionPlants;
                RootLength[layer] -= dltRootLengthDead[layer];
                RootLengthSenesced[layer] += dltRootLengthDead[layer];
            }

            double CumDepth = Utility.Math.Sum(Soil.SoilWater.dlayer);
            if (RootDepth < 0 || RootDepth > CumDepth)
                throw new Exception("Invalid root depth: " + RootDepth.ToString());

            Util.Debug("root.RootDepth=%f", RootDepth);
            Util.Debug("root.RootLength=%f", Utility.Math.Sum(RootLength));
            Util.Debug("root.RootLengthSenesced=%f", Utility.Math.Sum(RootLengthSenesced));

            UpdateWaterAndNBalance();
        }

        #endregion

        #region Public interface specific to Root
        [XmlIgnore]
        [Units("mm")]
        public double RootDepth { get; set; }

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

        public double SWAvailRatio
        {
            get
            {
                bool valuesFound = false;
                double ratio = 0.0;
                for (int i = 0; i < sw_avail_pot.Length; i++)
                {
                    if (sw_avail_pot[i] > 0)
                    {
                        ratio += sw_avail[i] / sw_avail_pot[i];
                        valuesFound = true;
                    }
                }
                if (valuesFound)
                    return ratio;
                else
                    return 1.0;
            }
        }
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
        public double[] FASWLayered
        {
            get
            {
                double[] FASW = new double[Soil.SoilWater.dlayer.Length];
                for (int i = 0; i < Soil.SoilWater.dlayer.Length; i++)
                {
                    FASW[i] = Utility.Math.Divide(Soil.SoilWater.sw_dep[i] - ll_dep[i], Soil.SoilWater.dul_dep[i] - ll_dep[i], 0.0);
                    FASW[i] = Utility.Math.Constrain(FASW[i], 0.0, 1.0);
                }
                return FASW;
            }
        }
        public double FASW
        {
            get
            {
                //  the layer with root front
                int layer = FindLayerNo(RootDepth);
                int deepest_layer = Soil.SoilWater.dlayer.Length - 1;

                double CumDepth = Utility.Math.Sum(Soil.SoilWater.dlayer, 0, layer + 1, 0.0);

                double rootdepth_in_layer = Soil.SoilWater.dlayer[layer] - (CumDepth - RootDepth);
                rootdepth_in_layer = Utility.Math.Constrain(rootdepth_in_layer, 0.0, Soil.SoilWater.dlayer[layer]);

                double weighting_factor = Utility.Math.Divide(rootdepth_in_layer, Soil.SoilWater.dlayer[layer], 0.0);
                int next_layer = Math.Min(layer + 1, deepest_layer);

                double fasw1 = FASWLayered[layer];
                double fasw2 = FASWLayered[next_layer];

                fasw1 = Math.Min(1.0, Math.Max(0.0, fasw1));
                fasw2 = Math.Min(1.0, Math.Max(0.0, fasw2));

                return weighting_factor * fasw2 + (1.0 - weighting_factor) * fasw1;
            }
        }
        /// <summary>
        /// Root length density - needed by SWIM
        /// </summary>
        [Units("mm/mm^3")]
        public double[] RootLengthDensity { get { return Utility.Math.Divide(RootLength, Soil.SoilWater.dlayer); } }
        /// <summary>
        /// Calculate the extractable soil water in the root zone (mm).
        /// </summary>
        internal double ESWInRootZone
        {
            get
            {
                double ESW = 0;
                int deepest_layer = FindLayerNo(RootDepth);
                for (int layer = 0; layer <= deepest_layer; layer++)
                    ESW += Utility.Math.Constrain(Soil.SoilWater.sw_dep[layer] - ll_dep[layer], 0.0, double.MaxValue);
                return ESW;
            }
        }

        /// <summary>
        /// Return the index of the layer corresponding to the given depth
        /// </summary>
        internal int FindLayerNo(double depth)
        {
            int i;
            double progressive_sum = 0.0;

            for (i = 0; i < Soil.SoilWater.dlayer.Length; i++)
            {
                progressive_sum = progressive_sum + Soil.SoilWater.dlayer[i];
                if (progressive_sum >= depth)
                    break;
            }
            if (i != 0 && i == Soil.SoilWater.dlayer.Length)
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

            double[] rlv_factor = new double[Soil.SoilWater.dlayer.Length];    // relative rooting factor for all layers

            double[] relativeRootRate = RelativeRootRate.Values;
            double[] sWFactorRootLength = SWFactorRootLength.Values;

            double rlv_factor_tot = 0.0;
            for (int layer = 0; layer <= deepest_layer; layer++)
            {
                double branching_factor = relativeRootRate[layer];

                rlv_factor[layer] = sWFactorRootLength[layer] *
                                    branching_factor *                                   // branching factor
                                    xf[layer] *                                          // growth factor
                                    Utility.Math.Divide(Soil.SoilWater.dlayer[layer], RootDepth, 0.0);   // space weighting factor

                rlv_factor[layer] = Utility.Math.Constrain(rlv_factor[layer], 1e-6, double.MaxValue);
                rlv_factor_tot += rlv_factor[layer];
            }

            double dlt_length_tot = Growth.Wt / Conversions.sm2smm * SpecificRootLength;

            for (int layer = 0; layer <= deepest_layer; layer++)
                dltRootLength[layer] = dlt_length_tot * Utility.Math.Divide(rlv_factor[layer], rlv_factor_tot, 0.0);
            Util.Debug("Root.dltRootLength=%f", Utility.Math.Sum(dltRootLength));
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
            Util.Debug("Root.dltRootLengthSenesced=%f", Utility.Math.Sum(dltRootLengthSenesced));
        }

        /// <summary>
        /// Write a summary to the summary file.
        /// </summary>
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
            for (int layer = 0; layer < Soil.SoilWater.dlayer.Length; layer++)
            {
                writer.WriteLine(string.Format("{0,9:F1}{1,10:F3}{2,15:F3}{3,12:F3}",
                                  Soil.SoilWater.dlayer[layer],
                                  getModifiedKL(layer),
                                  Utility.Math.Divide(ll_dep[layer], Soil.SoilWater.dlayer[layer], 0.0),
                                  xf[layer]));
                dep_tot += Soil.SoilWater.dlayer[layer];
                esw_tot += Soil.SoilWater.dul_dep[layer] - ll_dep[layer];
            }
             writer.WriteLine("-----------------------------------------------");
            if (HaveModifiedKLValues)
                writer.WriteLine("**** KL's have been modified using either CL, EC or ESP values.");

            writer.WriteLine("Extractable SW: {0,5:F0}mm in {1,5:F0}mm total depth ({2,3:F0}%).",
                                            esw_tot,
                                            dep_tot,
                                            Conversions.fract2pcnt * Utility.Math.Divide(esw_tot, dep_tot, 0.0));
        }

        /// <summary>
        /// Remove biomass from the root system due to senescence or plant death
        /// </summary>
        internal void RemoveBiomassFraction(double Fraction)
        {
            Biomass Dead;
            Dead = Live * DieBackFraction * Fraction;
            // however dead roots have a given N concentration
            Dead.StructuralN = Dead.Wt * NSenescenceConcentration;

            Live = Live - Dead;
            Dead = Dead + Dead;

            // do root_length
            double[] dltRootLengthDie = new double[Soil.SoilWater.dlayer.Length];
            double Die_length = Dead.Wt / Conversions.sm2smm * SpecificRootLength;
            RootDist(Die_length, dltRootLengthDie);
            for (int layer = 0; layer < Soil.SoilWater.dlayer.Length; layer++)
                RootLength[layer] -= dltRootLengthDie[layer];
        }

        #endregion

        #region Event handlers
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

            dlt_sw_dep = new double[Soil.SoilWater.dlayer.Length];
            sw_avail = new double[Soil.SoilWater.dlayer.Length];
            sw_avail_pot = new double[Soil.SoilWater.dlayer.Length];
            sw_supply = new double[Soil.SoilWater.dlayer.Length];

            dlt_no3gsm = new double[Soil.SoilWater.dlayer.Length];
            dlt_nh4gsm = new double[Soil.SoilWater.dlayer.Length];
            no3gsm_uptake_pot = new double[Soil.SoilWater.dlayer.Length];
            nh4gsm_uptake_pot = new double[Soil.SoilWater.dlayer.Length];
            dltRootLength = new double[Soil.SoilWater.dlayer.Length];
            dltRootLengthSenesced = new double[Soil.SoilWater.dlayer.Length];
            dltRootLengthDead = new double[Soil.SoilWater.dlayer.Length];
            no3gsm_min = new double[Soil.SoilWater.dlayer.Length];
            nh4gsm_min = new double[Soil.SoilWater.dlayer.Length];
            RootLength = new double[Soil.SoilWater.dlayer.Length];
            RootLengthSenesced = new double[Soil.SoilWater.dlayer.Length];

            ll = Soil.Crop(Plant.Name).LL;
            kl = Soil.Crop(Plant.Name).KL;
            xf = Soil.Crop(Plant.Name).XF;

            ll_dep = Utility.Math.Multiply(ll, Soil.SoilWater.dlayer);
            Util.ZeroArray(no3gsm_min);
            Util.ZeroArray(nh4gsm_min);
        }
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
        public override void OnEndCrop(BiomassRemovedType BiomassRemoved)
        {
            DisposeDetachedMaterial(Live, RootLength);
            DisposeDetachedMaterial(Dead, RootLengthSenesced);
            Dead.Clear();
            Live.Clear();
        }

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
                double rld = Utility.Math.Divide(initial_root_length, RootDepth, 0.0);

                int deepest_layer = FindLayerNo(RootDepth);

                for (int layer = 0; layer <= deepest_layer; layer++)
                    RootLength[layer] = rld * Soil.SoilWater.dlayer[layer] * RootProportion(layer, RootDepth);

                Util.Debug("Root.InitGreen.StructuralWt=%f", Live.StructuralWt);
                Util.Debug("Root.InitGreen.StructuralN=%f", Live.StructuralN);
                Util.Debug("Root.InitRootLength=%f", Utility.Math.Sum(RootLength));
            }
        }

        #endregion

        #region Private functionality

        /// <summary>
        /// Dispose of detached material from dead & senesced roots into FOM pool
        /// </summary>
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
        ///    Hochman et. al. (2007) Simulating the effects of saline and sodic subsoils on wheat
        ///       crops growing on Vertosols. Australian Journal of Agricultural Research, 58, 802–810
        /// Will use one of CL, ESP and EC in that order to modified KL.
        /// </summary>
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

        /// <summary>
        /// Return potential available soil water from each layer in the root zone.
        /// </summary>
        private void DoPotentialExtractableSW()
        {
            Util.ZeroArray(sw_avail_pot);

            int deepest_layer = FindLayerNo(RootDepth);
            for (int layer = 0; layer <= deepest_layer; layer++)
                sw_avail_pot[layer] = Soil.SoilWater.dul_dep[layer] - ll_dep[layer];

            // correct bottom layer for actual root penetration
            sw_avail_pot[deepest_layer] = sw_avail_pot[deepest_layer] * RootProportion(deepest_layer, RootDepth);

            Util.Debug("Root.deepest_layer=%i", deepest_layer);
            Util.Debug("Root.sw_avail_pot[deepest_layer]=%f", sw_avail_pot[deepest_layer]);
        }

        /// <summary>
        /// Return actual water available for extraction from each layer in the
        /// soil profile by the crop (mm water)
        /// </summary>
        private void DoSWAvailable()
        {
            Util.ZeroArray(sw_avail);

            int deepest_layer = FindLayerNo(RootDepth);
            for (int layer = 0; layer <= deepest_layer; layer++)
            {
                sw_avail[layer] = Soil.SoilWater.sw_dep[layer] - ll_dep[layer];
                sw_avail[layer] = Utility.Math.Constrain(sw_avail[layer], 0.0, double.MaxValue);
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
        private void DoSWSupply()
        {
            Util.ZeroArray(sw_supply);

            int deepest_layer = FindLayerNo(RootDepth);
            double sw_avail;
            for (int i = 0; i <= deepest_layer; i++)
            {
                sw_avail = (Soil.SoilWater.sw_dep[i] - ll_dep[i]);
                sw_supply[i] = sw_avail * getModifiedKL(i);
                sw_supply[i] = Utility.Math.Constrain(sw_supply[i], 0.0, double.MaxValue);
            }
            //now adjust bottom layer for depth of root
            sw_supply[deepest_layer] = sw_supply[deepest_layer] * RootProportion(deepest_layer, RootDepth);
            Util.Debug("Root.sw_supply[deepest_layer]=%f", sw_supply[deepest_layer]);
        }

        /// <summary>
        /// Calculate todays daily water uptake by this root system
        /// </summary>
        private void DoWaterUptakeInternal(double sw_demand)
        {
            int deepest_layer = FindLayerNo(RootDepth);
            double sw_supply_sum = Utility.Math.Sum(sw_supply, 0, deepest_layer + 1, 0.0);

            if ((sw_supply_sum < 0.0) || (sw_demand < 0.0))
            {
                //we have no uptake - there is no demand or potential
                Util.ZeroArray(dlt_sw_dep);
            }
            else
            {
                // get actual uptake
                Util.ZeroArray(dlt_sw_dep);
                if (sw_demand < sw_supply_sum)
                {
                    // demand is less than what roots could take up.
                    // water is non-limiting.
                    // distribute demand proportionately in all layers.
                    for (int layer = 0; layer <= deepest_layer; layer++)
                    {
                        dlt_sw_dep[layer] = -1.0 * Utility.Math.Divide(sw_supply[layer], sw_supply_sum, 0.0) * sw_demand;
                    }
                }
                else
                {
                    // water is limiting - not enough to meet demand so take
                    // what is available (potential)
                    for (int layer = 0; layer <= deepest_layer; layer++)
                    {
                        dlt_sw_dep[layer] = -1 * sw_supply[layer];
                    }
                }
            }
        }

        /// <summary>
        /// Returns the proportion of layer that has roots in it (0-1).
        ///     Each element of "dlayr" holds the height of  the
        ///     corresponding soil layer.  The height of the top layer is
        ///     held in "dlayr"(1), and the rest follow in sequence down
        ///     into the soil profile.  Given a root depth of "root_depth",
        ///     this function will return the proportion of "dlayr"("layer")
        ///     which has roots in it  (a value in the range 0..1).
        /// </summary>
        private double RootProportion(int layer, double RootDepth)
        {
            double depth_to_layer_bottom = Utility.Math.Sum(Soil.SoilWater.dlayer, 0, layer + 1, 0.0);
            double depth_to_layer_top = depth_to_layer_bottom - Soil.SoilWater.dlayer[layer];
            double depth_to_root = Math.Min(depth_to_layer_bottom, RootDepth);
            double depth_of_root_in_layer = Math.Max(0.0, depth_to_root - depth_to_layer_top);
            return (Utility.Math.Divide(depth_of_root_in_layer, Soil.SoilWater.dlayer[layer], 0.0));
        }

        /// <summary>
        /// Distribute root material over profile based upon root
        /// length distribution.
        /// </summary>
        private double[] RootDist(double root_sum, double[] RootLength)
        {
            int deepest_layer = FindLayerNo(RootDepth);

            double root_length_sum = Utility.Math.Sum(RootLength, 0, deepest_layer + 1, 0.0);

            double[] RootArray = new double[RootLength.Length];
            for (int layer = 0; layer <= deepest_layer; layer++)
                RootArray[layer] = root_sum * Utility.Math.Divide(RootLength[layer], root_length_sum, 0.0);
            return RootArray;
        }

        /// <summary>
        /// 
        /// </summary>
        private double WFPS(int layer)
        {
            double wfps = Utility.Math.Divide(Soil.SoilWater.sw_dep[layer] - Soil.SoilWater.ll15_dep[layer],
                                            Soil.SoilWater.sat_dep[layer] - Soil.SoilWater.ll15_dep[layer], 0.0);
            return Utility.Math.Constrain(wfps, 0.0, 1.0);
        }

        /// <summary>
        /// Update the water and N balance.
        /// </summary>
        private void UpdateWaterAndNBalance()
        {
            NitrogenChangedType NitrogenUptake = new NitrogenChangedType();
            NitrogenUptake.Sender = "Plant";
            NitrogenUptake.SenderType = "Plant";
            NitrogenUptake.DeltaNO3 = Utility.Math.Multiply_Value(dlt_no3gsm, Conversions.gm2kg / Conversions.sm2ha);
            NitrogenUptake.DeltaNH4 = Utility.Math.Multiply_Value(dlt_nh4gsm, Conversions.gm2kg / Conversions.sm2ha);
            Util.Debug("Root.NitrogenUptake.DeltaNO3=%f", Utility.Math.Sum(NitrogenUptake.DeltaNO3));
            Util.Debug("Root.NitrogenUptake.DeltaNH4=%f", Utility.Math.Sum(NitrogenUptake.DeltaNH4));
            NitrogenChanged.Invoke(NitrogenUptake);

            // Send back delta water and nitrogen back to APSIM.
            if (!SwimIsPresent)
            {
                WaterChangedType WaterUptake = new WaterChangedType();
                WaterUptake.DeltaWater = dlt_sw_dep;
                Util.Debug("Root.WaterUptake=%f", Utility.Math.Sum(WaterUptake.DeltaWater));
                WaterChanged.Invoke(WaterUptake);

            }
        }

        #endregion


        #region Grazing
        public override AvailableToAnimalelementType[] AvailableToAnimal { get { return null; } }
        public override RemovedByAnimalType RemovedByAnimal { set { } }
        #endregion
    }
}
