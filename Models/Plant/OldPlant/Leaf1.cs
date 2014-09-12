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
    [Serializable]
    public class Leaf1 : BaseOrgan1, AboveGround
    {
        #region Parameters read from XML file and links to other functions.
        [Link]
        Plant15 Plant = null;

        [Link]
        public Stem1 Stem;

        [Link]
        Environment Environment = null;

        [Link]
        RUEModel1 Photosynthesis = null;

        [Link]
        Population1 Population = null;

        [Link]
        Phenology Phenology = null;

        [Link] Function TEModifier = null;
        [Link] Function NConcCriticalModifier = null;
        [Link] Function TE = null;
        [Link] Function LeafSize = null;
        [Link] SWStress SWStress = null;
        [Link] NStress NStress = null;
        [Link] PStress PStress = null;


        [Link]
        PlantSpatial1 PlantSpatial = null;

        [Link] Function SLAMax = null;
        [Link] Function LeafNumberFraction = null;
        [Link] Function ExtinctionCoefficient = null;
        [Link] Function ExtinctionCoefficientDead = null;
        [Link] Function NConcentrationCritical = null;
        [Link] Function NConcentrationMinimum = null;
        [Link] Function NConcentrationMaximum = null;

        [Link]
        WeatherFile MetData = null;

        public double NodeNumberCorrection { get; set; }

        public double SLAMin { get; set; }

        public double FractionLeafSenescenceRate { get; set; }

        public double NodeSenescenceRate { get; set; }

        public double NFactLeafSenescenceRate { get; set; }

        public double MinTPLA { get; set; }

        public double NDeficitUptakeFraction { get; set; }

        [Link]
        Function NodeFormationPeriod = null;

        [Link]
        Function NodeAppearanceRate = null;

        [Link]
        LinearInterpolationFunction LeavesPerNode = null;

        [Link]
        Function LeafSenescencePeriod = null;

        [Link]
        Function LeafSenescenceFrost = null;

        [Link]
        Function DMSenescenceFraction = null;

        [Link]
        CompositeBiomass TotalLive = null;

        [Link]
        Function GrowthStructuralFractionStage = null;

        public double InitialWt { get; set; }

        public double InitialNConcentration { get; set; }

        public double InitialTPLA { get; set; }

        public double InitialLeafNumber { get; set; }

        public double LAISenLight { get; set; }

        public double SenLightSlope { get; set; }

        public double SenRateWater { get; set; }

        public double NSenescenceConcentration { get; set; }

        public double SenescenceDetachmentFraction { get; set; }
        #endregion

        #region Variables we need from other modules
        double CO2 = 350;             // The TEModifier and NConcCriticalModifier function's use this.
        #endregion

        #region Private variables
        public double dlt_dm_pot_rue;
        public double dlt_n_senesced_retrans;           // plant N retranslocated to/from (+/-) senesced part to/from <<somewhere else??>> (g/m^2)
        public double dlt_n_senesced_trans;
        public double dlt_height;                       // growth upwards (mm)
        public double dlt_width;                        // growth outwards (mm)
        public double width = 0;
        private double _NDemand = 0;
        private double _SoilNDemand = 0;
        private double NMax = 0;
        private double sw_demand_te = 0;
        private double sw_demand = 0;
        private double n_conc_crit = 0;
        private double n_conc_max = 0;
        private double n_conc_min = 0;
        private double radiationInterceptedGreen;
        private double _LeavesPerNode = 0;
        private double _LAI = 0;
        private double _SLAI = 0;
        private double dltLAI;
        private double dltSLAI;
        private double dltLAI_pot;
        private double dltLAI_stressed;
        private double dltLAI_carbon;
        private double dltSLAI_detached;
        private double dltSLAI_age;
        private double dltSLAI_light;
        private double dltSLAI_water;
        private double dltSLAI_frost;
        private double dltLeafNo;
        private double dltLeafNoPot;
        private double dltLeafNoSen;
        private double dltNodeNoPot;
        private bool ExternalSWDemand = false;
        private double transpEff;

        [XmlIgnore]
        public double NodeNo { get; set; }
        private double[] LeafNo;

        private double[] LeafNoSen;
        private double dltNodeNo;
        private double[] LeafArea;
        private const int max_node = 25;
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
        public override void DoSWDemand(double Supply)
        {
            if (ExternalSWDemand == true)
            {
                transpEff = dlt_dm_pot_rue / sw_demand;
                ExternalSWDemand = false;
            }
            else
            {
                // Return crop water demand from soil by the crop (mm) calculated by
                // dividing biomass production limited by radiation by transpiration efficiency.
                // get potential transpiration from potential
                // carbohydrate production and transpiration efficiency

                // Calculate today's transpiration efficiency from min,max temperatures and co2 level
                // and converting mm water to g dry matter (g dm/m^2/mm water)

                transpEff = TE.Value / Environment.VPD / Conversions.g2mm;
                transpEff = transpEff * TEModifier.Value;

                if (transpEff == 0)
                {
                    sw_demand_te = 0;
                    sw_demand = 0;
                }
                else
                {
                    sw_demand_te = (dlt_dm_pot_rue - Respiration) / transpEff;

                    // Capping of sw demand will create an effective TE- recalculate it here
                    // In an ideal world this should NOT be changed here - NIH
                    double SWDemandMax = Supply * CoverGreen;
                    sw_demand = Utility.Math.Constrain(sw_demand_te, Double.MinValue, SWDemandMax);
                    transpEff = transpEff * Utility.Math.Divide(sw_demand_te, sw_demand, 1.0);
                }
            }
            Util.Debug("Leaf.sw_demand=%f", sw_demand);
            Util.Debug("Leaf.transpEff=%f", transpEff);
        }
        public override void DoSWUptake(double SWDemand) { }

        // dry matter
        public override double DMSupply
        {
            get
            {
                if (Plant.TopsSWDemand > 0)
                    return dlt_dm_pot_rue * SWStress.Photo;
                else
                    return 0.0;
            }
        }
        public override double DMRetransSupply
        {
            get
            {
                return Utility.Math.Constrain(Live.NonStructuralWt, 0.0, double.MaxValue);
            }
        }
        public override double dltDmPotRue { get { return dlt_dm_pot_rue; } }
        public override double DMGreenDemand
        {
            get
            {
                // Maximum DM this part can take today (PFR)
                return Utility.Math.Divide(dltLAI_stressed, SLAMin * Conversions.smm2sm, 0.0);
            }
        }
        public override double DMDemandDifferential { get { return 0; } }
        public override void DoDMDemand(double DMSupply) { }
        public override void DoDmRetranslocate(double dlt_dm_retrans_to_fruit, double demand_differential_begin) { }
        public override void GiveDmGreen(double Delta)
        {
            Growth.StructuralWt += Delta * GrowthStructuralFractionStage.Value;
            Growth.NonStructuralWt += Delta * (1.0 - GrowthStructuralFractionStage.Value);
            Util.Debug("Leaf.Growth.StructuralWt=%f", Growth.StructuralWt);
            Util.Debug("Leaf.Growth.NonStructuralWt=%f", Growth.NonStructuralWt);
        }
        public override void DoSenescence()
        {
            double fraction_senescing = Utility.Math.Constrain(DMSenescenceFraction.Value, 0.0, 1.0);

            Senescing.StructuralWt = (Live.StructuralWt + Growth.StructuralWt + Retranslocation.StructuralWt) * fraction_senescing;
            Senescing.NonStructuralWt = (Live.NonStructuralWt + Growth.NonStructuralWt + Retranslocation.NonStructuralWt) * fraction_senescing;
            Util.Debug("Leaf.Senescing.StructuralWt=%f", Senescing.StructuralWt);
            Util.Debug("Leaf.Senescing.NonStructuralWt=%f", Senescing.NonStructuralWt);

        }
        public override void DoDetachment()
        {
            dltSLAI_detached = SLAI * SenescenceDetachmentFraction;
            double Density = 1.0;
            double area_detached = dltSLAI_detached / Density * Conversions.sm2smm;

            for (int node = 0; node < max_node; node++)
            {
                if (area_detached > LeafArea[node])
                {
                    area_detached = area_detached - LeafArea[node];
                    LeafArea[node] = 0.0;
                }
                else
                {
                    LeafArea[node] = LeafArea[node] - area_detached;
                    break;
                }
            }

            Detaching = Dead * SenescenceDetachmentFraction;
            Util.Debug("leaf.dltSLAI_detached=%f", dltSLAI_detached);
            Util.DebugArray("leaf.LeafArea=%f0", LeafArea, 10);
            Util.Debug("Leaf.Detaching.Wt=%f", Detaching.Wt);
            Util.Debug("Leaf.Detaching.N=%f", Detaching.N);
        }
        public override void RemoveBiomass()
        {
            double chop_fr_green = Utility.Math.Divide(GreenRemoved.Wt, Live.Wt, 0.0);
            double chop_fr_sen = Utility.Math.Divide(SenescedRemoved.Wt, Dead.Wt, 0.0);

            double dlt_lai = LAI * chop_fr_green;
            double dlt_slai = SLAI * chop_fr_sen;

            // keep leaf area above a minimum
            double lai_init = InitialTPLA * Conversions.smm2sm * Population.Density;
            double dlt_lai_max = LAI - lai_init;
            dlt_lai = Utility.Math.Constrain(dlt_lai, double.MinValue, dlt_lai_max);

            _LAI -= dlt_lai;
            _SLAI -= dlt_slai;
            RemoveDetachment(dlt_slai, dlt_lai);

            Live = Live - GreenRemoved;
            Dead = Dead - SenescedRemoved;

            // keep dm above a minimum
            double dm_init = InitialWt * Population.Density;
            double n_init = dm_init * InitialNConcentration;
            if (Live.Wt < dm_init)
            {
                // keep dm above a minimum
                Live.StructuralWt = Live.StructuralWt * dm_init / Live.Wt;
                Live.NonStructuralWt = Live.NonStructuralWt * dm_init / Live.Wt;
            }
            if (Live.N < n_init)
            {
                // keep N above a minimum
                Live.StructuralN = n_init;
            }
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
            Util.Debug("Leaf.NDemand=%f", _NDemand);
            Util.Debug("Leaf.NMax=%f", NMax);
        }
        public override void DoNDemand1Pot(double dltDmPotRue)
        {
            Biomass OldGrowth = Growth;
            Growth.StructuralWt = dltDmPotRue * Utility.Math.Divide(Live.Wt, TotalLive.Wt, 0.0);
            Util.Debug("Leaf.Growth.StructuralWt=%f", Growth.StructuralWt);
            Util.CalcNDemand(dltDmPotRue, dltDmPotRue, n_conc_crit, n_conc_max, Growth, Live, Retranslocation.N, 1.0,
                       ref _NDemand, ref NMax);
            Growth.StructuralWt = 0.0;
            Growth.NonStructuralWt = 0.0;
            Util.Debug("Leaf.NDemand=%f", _NDemand);
            Util.Debug("Leaf.NMax=%f", NMax);
        }
        public override void DoSoilNDemand()
        {
            _SoilNDemand = NDemand - dlt_n_senesced_retrans;
            _SoilNDemand = Utility.Math.Constrain(_SoilNDemand, 0.0, double.MaxValue);
            Util.Debug("Leaf.SoilNDemand=%f", _SoilNDemand);
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
            Util.Debug("Leaf.Retranslocation.N=%f", Retranslocation.N);
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

            Util.Debug("Leaf.SenescingN=%f", SenescingN);
            Util.Debug("Leaf.dlt.n_senesced_trans=%f", dlt_n_senesced_trans);
        }
        public override void DoNSenescedRetranslocation(double navail, double n_demand_tot)
        {
            dlt_n_senesced_retrans = navail * Utility.Math.Divide(NDemand, n_demand_tot, 0.0);
            Util.Debug("Leaf.dlt.n_senesced_retrans=%f", dlt_n_senesced_retrans);
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

            Util.Debug("Leaf.n_conc_crit=%f", n_conc_crit);
            Util.Debug("Leaf.n_conc_min=%f", n_conc_min);
            Util.Debug("Leaf.n_conc_max=%f", n_conc_max);

            n_conc_crit *= NConcCriticalModifier.Value;
            if (n_conc_crit <= n_conc_min)
                throw new Exception("nconc_crit < nconc_min!. What's happened to CO2??");
        }
        public override void ZeroDltNSenescedTrans()
        {
            dlt_n_senesced_trans = 0;
        }
        public override void DoNUptake(double PotNFix) { }

        // cover
        [XmlIgnore]
        public override double CoverGreen { get; protected set; } // Required by soilwat for E0 calculation.
        [XmlIgnore]
        public override double CoverSen { get; protected set; }
        public override void DoPotentialRUE()
        {
            dlt_dm_pot_rue = Photosynthesis.PotentialDM(radiationInterceptedGreen);
            Util.Debug("Leaf.dlt.dm_pot_rue=%f", dlt_dm_pot_rue);
        }
        public override double interceptRadiation(double incomingSolarRadiation)
        {
            radiationInterceptedGreen = CoverGreen * incomingSolarRadiation;
            return CoverTotal * incomingSolarRadiation;
        }
        public override void DoCover()
        {
            CoverGreen = CalculateCover(LAI, ExtinctionCoefficient.Value, PlantSpatial.CanopyFactor);
            CoverSen = CalculateCover(_SLAI, ExtinctionCoefficientDead.Value, PlantSpatial.CanopyFactor);
            Util.Debug("leaf.cover.green=%f", CoverGreen);
            Util.Debug("leaf.cover.sen=%f", CoverSen);
        }

        // update
        public override void Update()
        {
            double TotalDltNSenescedRetrans = 0;
            foreach (Organ1 Organ in Plant.Organ1s)
                TotalDltNSenescedRetrans += Organ.DltNSenescedRetrans;

            Growth.StructuralN -= dlt_n_senesced_trans;
            Growth.StructuralN -= TotalDltNSenescedRetrans;

            Live = Live + Growth - Senescing;

            Dead = Dead - Detaching + Senescing;
            Live = Live + Retranslocation;
            Live.StructuralN = Live.N + dlt_n_senesced_retrans;

            Biomass dying = Live * Population.DyingFractionPlants;
            Live = Live - dying;
            Dead = Dead + dying;
            Senescing = Senescing + dying;

            Util.Debug("Leaf.Green.Wt=%f", Live.Wt);
            Util.Debug("Leaf.Green.N=%f", Live.N);
            Util.Debug("Leaf.Senesced.Wt=%f", Dead.Wt);
            Util.Debug("Leaf.Senesced.N=%f", Dead.N);
            Util.Debug("Leaf.Senescing.Wt=%f", Senescing.Wt);
            Util.Debug("Leaf.Senescing.N=%f", Senescing.N);

            double node_no = 1.0 + NodeNo;

            double dlt_leaf_area = dltLAI * Conversions.sm2smm;
            Util.Accumulate(dlt_leaf_area, LeafArea, node_no - 1.0, dltNodeNo);

            // Area senescence is calculated apart from plant number death
            // so any decrease in plant number will mean an increase in average
            // plant size as far as the leaf size record is concerned.

            // NIH - Don't think this is needed anymore because death goes into SLAI not TLAI_dead now
            //if ((plant->population().Density() /*+ g_dlt_plants*/)<=0.0)   //XXXX FIXME!!
            //    {
            //    fill_real_array(gLeafArea, 0.0, max_node);
            //    }

            Util.Accumulate(dltLeafNo, LeafNo, node_no - 1.0f, dltNodeNo);

            double leaf_no_sen_tot = Utility.Math.Sum(LeafNoSen) + dltLeafNoSen;

            for (int node = 0; node < max_node; node++)
            {
                if (leaf_no_sen_tot > LeafNo[node])
                {
                    leaf_no_sen_tot -= LeafNo[node];
                    LeafNoSen[node] = LeafNo[node];
                }
                else
                {
                    LeafNoSen[node] = leaf_no_sen_tot;
                    leaf_no_sen_tot = 0.0;
                }
            }
            NodeNo += dltNodeNo;

            // transfer plant leaf area
            _LAI += dltLAI - dltSLAI;
            _SLAI += dltSLAI - dltSLAI_detached;

            // Transfer dead leaf areas
            double dying_fract_plants = Population.DyingFractionPlants;

            double dlt_lai_dead = LAI * dying_fract_plants;
            _LAI -= dlt_lai_dead;
            _SLAI += dlt_lai_dead;

            Util.Debug("leaf.LeafNo=%f", Utility.Math.Sum(LeafNo));
            Util.Debug("leaf.LeafNoSen=%f", Utility.Math.Sum(LeafNoSen));
            Util.Debug("leaf.NodeNo=%f", NodeNo);
            Util.Debug("leaf.LAI=%f", _LAI);
            Util.Debug("leaf.SLAI=%f", _SLAI);

        }
        #endregion

        #region Public interface specific to Leaf
        public double NCrit { get { return n_conc_crit * Live.Wt; } }
        public double NMin { get { return n_conc_min * Live.Wt; } }
        public double NSenescedTrans { get { return dlt_n_senesced_trans; } }
        public double CoverTotal
        {
            get
            {
                return (1.0
                     - (1.0 - CoverGreen)
                     * (1.0 - CoverSen));
            }
        }
        [Units("m^2/m^2")]
        public double LAI { get { return _LAI; } }
        [Units("m^2/m^2")]
        public double SLAI { get { return _SLAI; } }
        [Units("m^2/m^2")]
        public double LAITotal { get { return LAI + SLAI; } }
        public double LeafNumber { get { return Utility.Math.Sum(LeafNo); } }
        public double LeafNumberDead { get { return Utility.Math.Sum(LeafNoSen); } }
         public double NodeNumberNow { get { return NodeNo + NodeNumberCorrection; } }
        /// <summary>
        /// Ratio of actual to potential lai
        /// </summary>
        public double LAIRatio
        {
            get
            {
                return Utility.Math.Divide(dltLAI, dltLAI_stressed, 0.0);
            }
        }
        public double FractionCanopySenescing { get { return Utility.Math.Divide(dltSLAI, _LAI + dltLAI, 0.0); } }
        public void DoCanopyExpansion()
        {
            dltNodeNoPot = 0.0;
            if (NodeFormationPeriod.Value == 1)
                dltNodeNoPot = Utility.Math.Divide(Phenology.CurrentPhase.TTForToday, NodeAppearanceRate.Value, 0.0);

            dltLeafNoPot = 0;
            if (Phenology.OnDayOf("Emergence"))
                _LeavesPerNode = LeavesPerNode.Value;

            else if (NodeFormationPeriod.Value == 1)
            {
                double leaves_per_node_now = LeavesPerNode.Value;

                _LeavesPerNode = Math.Min(_LeavesPerNode, leaves_per_node_now);

                double dlt_leaves_per_node = LeavesPerNode.ValueForX(NodeNo + dltNodeNoPot)
                                           - leaves_per_node_now;

                double stressFactor = Math.Min(Math.Pow(Math.Min(NStress.Expansion, 1.0 /*pStress->pFact.expansion*/), 2), SWStress.Expansion);

                _LeavesPerNode = (_LeavesPerNode) + dlt_leaves_per_node * stressFactor;

                dltLeafNoPot = dltNodeNoPot * _LeavesPerNode;
            }


            // Calculate leaf area potential.
            dltLAI_pot = dltLeafNoPot * LeafSize.Value * Conversions.smm2sm * Population.Density;

            // Calculate leaf area stressed.
            double StressFactor = Math.Min(SWStress.Expansion, Math.Min(NStress.Expansion, PStress.Expansion));
            dltLAI_stressed = dltLAI_pot * StressFactor;
            Util.Debug("Leaf.dltLAI_pot=%f", dltLAI_pot);
            Util.Debug("Leaf.dltLAI_stressed=%f", dltLAI_stressed);
        }
        internal void Actual()
        {
            // maximum daily increase in leaf area
            dltLAI_carbon = Growth.Wt * SLAMax.Value * Conversions.smm2sm;

            // index from carbon supply
            dltLAI = Math.Min(dltLAI_carbon, dltLAI_stressed);

            // Simulate actual leaf number increase as limited by dry matter production.

            //ratio of actual to potential leaf appearance
            double leaf_no_frac = LeafNumberFraction.Value;

            dltLeafNo = dltLeafNoPot * leaf_no_frac;

            if (dltLeafNo < dltNodeNoPot)
                dltNodeNo = dltLeafNo;
            else
                dltNodeNo = dltNodeNoPot;
            Util.Debug("Leaf.dltLAI_carbon=%f", dltLAI_carbon);
            Util.Debug("Leaf.dltLAI=%f", dltLAI);
            Util.Debug("Leaf.dltLeafNo=%f", dltLeafNo);
            Util.Debug("Leaf.dltNodeNo=%f", dltNodeNo);
        }
        internal void LeafDeath()
        {
            double leaf_no_sen_now;                       // total number of dead leaves yesterday

            double leaf_no_now = Utility.Math.Sum(LeafNo);

            double leaf_per_node = leaf_no_now * FractionLeafSenescenceRate;

            double node_sen_rate = Utility.Math.Divide(NodeSenescenceRate,
                                                      1.0 + NFactLeafSenescenceRate * (1.0 - NStress.Expansion),
                                                      0.0);

            double leaf_death_rate = Utility.Math.Divide(node_sen_rate, leaf_per_node, 0.0);

            if (Phenology.InPhase("ReadyForHarvesting"))
            {
                // Constrain leaf death to remaining leaves
                //cnh do we really want to do this?;  XXXX
                leaf_no_sen_now = Utility.Math.Sum(LeafNoSen);
                dltLeafNoSen = Utility.Math.Constrain(leaf_no_now - leaf_no_sen_now, 0.0, double.MaxValue);
            }
            else if (LeafSenescencePeriod.Value == 1)
            {
                dltLeafNoSen = Utility.Math.Divide(Phenology.CurrentPhase.TTForToday, leaf_death_rate, 0.0);

                // Ensure minimum leaf area remains
                double tpla_now = Utility.Math.Sum(LeafArea);
                double max_sen_area = Utility.Math.Constrain(tpla_now - MinTPLA, 0.0, double.MaxValue) * Population.Density;
                double max_sleaf_no_now = LeafNumberFromArea(LeafArea, LeafNo, max_node, max_sen_area);

                // Constrain leaf death to remaining leaves
                leaf_no_sen_now = Utility.Math.Sum(LeafNoSen);
                dltLeafNoSen = Utility.Math.Constrain(dltLeafNoSen, double.MinValue, max_sleaf_no_now - leaf_no_sen_now);
            }
            else
            {
                dltLeafNoSen = 0.0;
            }
            Util.Debug("Leaf.dltLeafNoSen=%f", dltLeafNoSen);
        }
        /// <summary>
        /// Calculate todays leaf area senescence
        /// </summary>
        internal void LeafAreaSenescence()
        {
            dltSLAI_age = LeafAreaSenescenceAge();
            dltSLAI_light = LeafAreaSenescenceLight();
            dltSLAI_water = LeafAreaSenescenceWater();
            dltSLAI_frost = LeafAreaSenescencFrost();

            dltSLAI = Math.Max(Math.Max(Math.Max(dltSLAI_age, dltSLAI_light), dltSLAI_water), dltSLAI_frost);
            Util.Debug("Leaf.dltSLAI_age=%f", dltSLAI_age);
            Util.Debug("Leaf.dltSLAI_light=%f", dltSLAI_light);
            Util.Debug("Leaf.dltSLAI_water=%f", dltSLAI_water);
            Util.Debug("Leaf.dltSLAI_frost=%f", dltSLAI_frost);
            Util.Debug("Leaf.dltSLAI=%f", dltSLAI);
        }
        #endregion

        #region Event handlers
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            Senescing = new Biomass();
            Retranslocation = new Biomass();
            Growth = new Biomass();
            Detaching = new Biomass();
            GreenRemoved = new Biomass();
            SenescedRemoved = new Biomass();
            LeafNo = new double[max_node];
            LeafNoSen = new double[max_node];
            LeafArea = new double[max_node];
            if (CO2 != 350 && (TEModifier == null || NConcCriticalModifier == null))
                throw new Exception("CO2 isn't at the default level, and model: " + Plant.Name + " has no CO2 parameterisations.");
        }

        public override void OnPrepare(object sender, EventArgs e)
        {
            if (LeafNo == null)
                OnSimulationCommencing(null, null);

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

            _NDemand = 0.0;
            _SoilNDemand = 0.0;
            NMax = 0.0;
            sw_demand_te = 0.0;
            sw_demand = 0.0;
            dltLAI = 0.0;
            dltSLAI = 0.0;
            dltLAI_pot = 0.0;
            dltLAI_stressed = 0.0;
            dltLAI_carbon = 0.0;  // (PFR)
            dltSLAI_detached = 0.0;
            dltSLAI_age = 0.0;
            dltSLAI_light = 0.0;
            dltSLAI_water = 0.0;
            dltSLAI_frost = 0.0;
            dltLeafNo = 0.0;
            //    g.dlt_node_no              = 0.0; JNGH - need to carry this through for site no next day.
            dltLeafNoPot = 0.0;
            dltNodeNoPot = 0.0;
        }

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

            InitialiseAreas();
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

                InitialiseAreas();
            }
        }
        #endregion

        #region Private functionality

        private void InitialiseAreas()
        {
            // Initialise leaf areas to a newly emerged state.
            NodeNo = InitialLeafNumber;

            Util.ZeroArray(LeafNo);
            Util.ZeroArray(LeafNoSen);
            Util.ZeroArray(LeafArea);

            int leaf_no_emerged = Convert.ToInt32(InitialLeafNumber);
            double leaf_emerging_fract = Math.IEEERemainder(InitialLeafNumber, 1.0);
            for (int leaf = 0; leaf < leaf_no_emerged; leaf++)
                LeafNo[leaf] = 1.0;

            LeafNo[leaf_no_emerged] = leaf_emerging_fract;

            double avg_leaf_area = Utility.Math.Divide(InitialTPLA, InitialLeafNumber, 0.0);
            for (int leaf = 0; leaf < leaf_no_emerged; leaf++)
                LeafArea[leaf] = avg_leaf_area * Population.Density;

            LeafArea[leaf_no_emerged] = leaf_emerging_fract * avg_leaf_area * Population.Density;

            _LAI = InitialTPLA * Conversions.smm2sm * Population.Density;
            _SLAI = 0.0;

            Util.Debug("Leaf.InitGreen.StructuralWt=%f", Live.StructuralWt);
            Util.Debug("Leaf.InitGreen.StructuralN=%f", Live.StructuralN);
            Util.Debug("Leaf.InitLeafNo=%f", Utility.Math.Sum(LeafNo));
            Util.Debug("Leaf.InitLeafArea=%f", Utility.Math.Sum(LeafArea));
            Util.Debug("Leaf.InitLAI=%f", LAI);
            Util.Debug("Leaf.InitSLAI=%f", SLAI);
        }
        private double Respiration
        {
            get
            {
                // Temperature effect
                double Q10 = 2.0;
                double fTempRef = 25.0;
                double fTmpAve = (MetData.MaxT + MetData.MinT) / 2.0;
                double fTempEf = Math.Pow(Q10, (fTmpAve - fTempRef) / 10.0);

                double nfac = 1.0;
                double MaintenanceCoefficient = 0.0;
                return Live.Wt * MaintenanceCoefficient * fTempEf * nfac;
            }
        }
        private static double CalculateCover(double LAI, double ExtinctionCoefficient, double CanopyFactor)
        {
            if (LAI > 0.0)
            {
                // light interception modified to give hedgerow effect with skip row

                // lai transformed to solid canopy
                double lai_canopy = LAI * CanopyFactor;    // lai in hedgerow

                // interception on row area basis
                double cover_green_leaf_canopy = 1.0 - Math.Exp(-ExtinctionCoefficient * lai_canopy);


                // interception on ground area basis
                return Utility.Math.Divide(cover_green_leaf_canopy, CanopyFactor, 0);
            }
            else
                return 0.0;
        }
        /// <summary>
        /// Derives number of leaves to result in given cumulative area
        /// </summary>
        private double LeafNumberFromArea(double[] g_leaf_area, double[] g_leaf_no, int NumNodes, double pla)
        {
            int node_no = 1 + Util.GetCumulativeIndex(pla, g_leaf_area, NumNodes);

            // number of complete nodes
            double node_area_whole = Utility.Math.Sum(g_leaf_area, 0, node_no - 1, 0.0);

            // area from last node (mm^2)
            double node_area_part = pla - node_area_whole;

            // fraction of last node (0-1)
            double node_fract = Utility.Math.Divide(node_area_part, g_leaf_area[node_no - 1], 0.0);

            return Utility.Math.Sum(g_leaf_no, 0, node_no, 0.0) + node_fract * g_leaf_no[node_no - 1];
        }

        /// <summary>
        /// Calculate the leaf senescence
        /// due to normal phenological (phasic, age) development
        /// </summary>
        private double LeafAreaSenescenceAge()
        {
            // get highest leaf no. senescing today
            double leaf_no_dead = Utility.Math.Sum(LeafNoSen) + dltLeafNoSen;
            int dying_node = Util.GetCumulativeIndex(leaf_no_dead, LeafNo, max_node);

            // get area senesced from highest leaf no.
            if (dying_node >= 0)
            {
                // senesced leaf area from current node dying (mm^2)
                double area_sen_dying_node = Utility.Math.Divide(leaf_no_dead - Utility.Math.Sum(LeafNo, 0, dying_node, 0)
                                              , LeafNo[dying_node]
                                              , 0.0) * LeafArea[dying_node];

                // lai senesced by natural ageing
                const double Density = 1.0;  // because LeafArea is on an area basis and not a plant basis
                double slai_age = (Utility.Math.Sum(LeafArea, 0, dying_node, 0)
                              + area_sen_dying_node)
                              * Conversions.smm2sm * Density;

                double min_lai = MinTPLA * Density * Conversions.smm2sm;
                double max_sen = Utility.Math.Constrain(_LAI - min_lai, 0.0, double.MaxValue);
                return Utility.Math.Constrain(slai_age - _SLAI, 0.0, max_sen);
            }
            return 0.0;
        }

        /// <summary>
        /// Return the lai that would senesce on the current day due to shading
        /// </summary>
        private double LeafAreaSenescenceLight()
        {
            // this doesnt account for other growing crops
            // should be based on reduction of intercepted light and k*lai
            // competition for light factor

            double slai_light_fac; // light competition factor (0-1)
            if (_LAI > LAISenLight)
                slai_light_fac = SenLightSlope * (_LAI - LAISenLight);
            else
                slai_light_fac = 0.0;

            double min_lai = MinTPLA * Population.Density * Conversions.smm2sm;
            double max_sen = Utility.Math.Constrain(_LAI - min_lai, 0.0, double.MaxValue);

            return Utility.Math.Constrain(_LAI * slai_light_fac, 0.0, max_sen);
        }

        /// <summary>
        /// Return the lai that would senesce on the current day due to water stress
        /// </summary>
        private double LeafAreaSenescenceWater()
        {
            // drought stress factor
            double slai_water_fac = SenRateWater * (1.0 - SWStress.Photo);
            double dlt_slai_water = _LAI * slai_water_fac;
            double min_lai = MinTPLA * Population.Density * Conversions.smm2sm;
            double max_sen = Utility.Math.Constrain(_LAI - min_lai, 0.0, double.MaxValue);
            return Utility.Math.Constrain(dlt_slai_water, 0.0, max_sen);
        }

        /// <summary>
        /// Return the lai that would senesce on the
        /// current day from low temperatures
        /// </summary>
        private double LeafAreaSenescencFrost()
        {
            double dlt_slai_low_temp = LeafSenescenceFrost.Value * _LAI;
            double min_lai = MinTPLA * Population.Density * Conversions.smm2sm;
            double max_sen = Utility.Math.Constrain(_LAI - min_lai, 0.0, double.MaxValue);
            return Utility.Math.Constrain(dlt_slai_low_temp, 0.0, max_sen);
        }


        /// <summary>
        /// Remove detachment from leaf area record
        /// </summary>
        void RemoveDetachment(double dlt_slai_detached, double dlt_lai_removed)
        {
            // Remove detachment from leaf area record from bottom upwards
            double area_detached = dlt_slai_detached * Conversions.sm2smm;  // (mm2/plant)

            for (int node = 0; node < max_node; node++)
            {
                if (area_detached > LeafArea[node])
                {
                    area_detached = area_detached - LeafArea[node];
                    LeafArea[node] = 0.0;
                }
                else
                {
                    LeafArea[node] = LeafArea[node] - area_detached;
                    break;
                }
            }

            // Remove detachment from leaf area record from top downwards
            double area_removed = dlt_lai_removed * Conversions.sm2smm;  // (mm2/plant)

            for (int node = (int)NodeNo; node >= 0; node--)
            {
                if (area_removed > LeafArea[node])
                {
                    area_removed = area_removed - LeafArea[node];
                    LeafArea[node] = 0.0;
                }
                else
                {
                    LeafArea[node] = LeafArea[node] - area_removed;
                    break;
                }
            }

            // calc new node number
            for (int node = max_node - 1; node >= 0; node--)
            {
                if (!Utility.Math.FloatsAreEqual(LeafArea[node], 0.0, 1.0E-4f))    // Slop?
                {
                    NodeNo = (double)node;  //FIXME - need adjustment for leafs remaining in for this node
                    break;
                }
            }

            // calc new leaf number
            int newNodeNo = (int)(1.0 + NodeNo);
            for (int node = newNodeNo - 1; node < max_node; node++)
            {
                LeafNo[node] = 0.0;
                LeafNoSen[node] = 0.0;
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