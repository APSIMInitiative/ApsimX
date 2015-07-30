using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Models.Core;
using Models.PMF.Functions;
using Models.PMF.Phen;
using Models.PMF.Organs;
using System.Xml.Serialization;
using Models.PMF.Interfaces;
using Models.Interfaces;
using APSIM.Shared.Utilities;

namespace Models.PMF.OldPlant
{
    /// <summary>
    /// A leaf organ for Plant15
    /// </summary>
    [Serializable]
    public class Leaf1 : BaseOrgan1, AboveGround, ICanopy
    {
        #region Canopy interface
        /// <summary>Canopy type</summary>
        public string CanopyType { get { return Plant.CropType; } }

        /// <summary>Gets the lai.</summary>
        /// <value>The lai.</value>
        [Units("m^2/m^2")]
        public double LAI { get { return _LAI; } }

        /// <summary>Gets the lai total.</summary>
        /// <value>The lai total.</value>
        [Units("m^2/m^2")]
        public double LAITotal { get { return LAI + SLAI; } }

        /// <summary>Gets or sets the cover green.</summary>
        /// <value>The cover green.</value>
        [XmlIgnore]
        public override double CoverGreen { get; protected set; } // Required by soilwat for E0 calculation.
        
        /// <summary>Gets the cover total.</summary>
        public double CoverTotal
        {
            get
            {
                return (1.0
                     - (1.0 - CoverGreen)
                     * (1.0 - CoverSen));
            }
        }

        /// <summary>Gets the canopy height (mm)</summary>
        public double Height { get { return Plant.height; } }

        /// <summary>Gets the canopy depth (mm)</summary>
        public double Depth { get { return Plant.height; } }

        /// <summary>Gets  FRGR.</summary>
        public double FRGR { get { return Plant.FRGR; } }

        /// <summary>Sets the potential evapotranspiration.</summary>
        public double PotentialEP { set { Plant.PotentialEP = value; } }

        /// <summary>Sets the light profile.</summary>
        public CanopyEnergyBalanceInterceptionlayerType[] LightProfile { set { Plant.LightProfile = value; } }

        #endregion

        #region Parameters read from XML file and links to other functions.
        /// <summary>The plant</summary>
        [Link]
        Plant15 Plant = null;

        /// <summary>The stem</summary>
        [Link]
        public Stem1 Stem;

        /// <summary>The environment</summary>
        [Link]
        Environment Environment = null;

        /// <summary>The photosynthesis</summary>
        [Link]
        RUEModel1 Photosynthesis = null;

        /// <summary>The population</summary>
        [Link]
        Population1 Population = null;

        /// <summary>The phenology</summary>
        [Link]
        Phenology Phenology = null;

        /// <summary>The te modifier</summary>
        [Link]
        IFunction TEModifier = null;
        /// <summary>The n conc critical modifier</summary>
        [Link]
        IFunction NConcCriticalModifier = null;
        /// <summary>The te</summary>
        [Link]
        IFunction TE = null;
        /// <summary>The leaf size</summary>
        [Link]
        IFunction LeafSize = null;
        /// <summary>The sw stress</summary>
        [Link] SWStress SWStress = null;
        /// <summary>The n stress</summary>
        [Link] NStress NStress = null;
        /// <summary>The p stress</summary>
        [Link] PStress PStress = null;


        /// <summary>The plant spatial</summary>
        [Link]
        PlantSpatial1 PlantSpatial = null;

        /// <summary>The sla maximum</summary>
        [Link]
        IFunction SLAMax = null;
        /// <summary>The leaf number fraction</summary>
        [Link]
        IFunction LeafNumberFraction = null;
        /// <summary>The extinction coefficient</summary>
        [Link]
        IFunction ExtinctionCoefficient = null;
        /// <summary>The extinction coefficient dead</summary>
        [Link]
        IFunction ExtinctionCoefficientDead = null;
        /// <summary>The n concentration critical</summary>
        [Link]
        IFunction NConcentrationCritical = null;
        /// <summary>The n concentration minimum</summary>
        [Link]
        IFunction NConcentrationMinimum = null;
        /// <summary>The n concentration maximum</summary>
        [Link]
        IFunction NConcentrationMaximum = null;

        /// <summary>The met data</summary>
        [Link]
        IWeather MetData = null;

        /// <summary>Gets or sets the node number correction.</summary>
        /// <value>The node number correction.</value>
        public double NodeNumberCorrection { get; set; }

        /// <summary>Gets or sets the sla minimum.</summary>
        /// <value>The sla minimum.</value>
        public double SLAMin { get; set; }

        /// <summary>Gets or sets the fraction leaf senescence rate.</summary>
        /// <value>The fraction leaf senescence rate.</value>
        public double FractionLeafSenescenceRate { get; set; }

        /// <summary>Gets or sets the node senescence rate.</summary>
        /// <value>The node senescence rate.</value>
        public double NodeSenescenceRate { get; set; }

        /// <summary>Gets or sets the n fact leaf senescence rate.</summary>
        /// <value>The n fact leaf senescence rate.</value>
        public double NFactLeafSenescenceRate { get; set; }

        /// <summary>Gets or sets the minimum tpla.</summary>
        /// <value>The minimum tpla.</value>
        public double MinTPLA { get; set; }

        /// <summary>Gets or sets the n deficit uptake fraction.</summary>
        /// <value>The n deficit uptake fraction.</value>
        public double NDeficitUptakeFraction { get; set; }

        /// <summary>The node formation period</summary>
        [Link]
        IFunction NodeFormationPeriod = null;

        /// <summary>The node appearance rate</summary>
        [Link]
        IFunction NodeAppearanceRate = null;

        /// <summary>The leaves per node</summary>
        [Link]
        LinearInterpolationFunction LeavesPerNode = null;

        /// <summary>The leaf senescence period</summary>
        [Link]
        IFunction LeafSenescencePeriod = null;

        /// <summary>The leaf senescence frost</summary>
        [Link]
        IFunction LeafSenescenceFrost = null;

        /// <summary>The dm senescence fraction</summary>
        [Link]
        IFunction DMSenescenceFraction = null;

        /// <summary>The total live</summary>
        [Link]
        CompositeBiomass TotalLive = null;

        /// <summary>The growth structural fraction stage</summary>
        [Link]
        IFunction GrowthStructuralFractionStage = null;

        /// <summary>Gets or sets the initial wt.</summary>
        /// <value>The initial wt.</value>
        public double InitialWt { get; set; }

        /// <summary>Gets or sets the initial n concentration.</summary>
        /// <value>The initial n concentration.</value>
        public double InitialNConcentration { get; set; }

        /// <summary>Gets or sets the initial tpla.</summary>
        /// <value>The initial tpla.</value>
        public double InitialTPLA { get; set; }

        /// <summary>Gets or sets the initial leaf number.</summary>
        /// <value>The initial leaf number.</value>
        public double InitialLeafNumber { get; set; }

        /// <summary>Gets or sets the lai sen light.</summary>
        /// <value>The lai sen light.</value>
        public double LAISenLight { get; set; }

        /// <summary>Gets or sets the sen light slope.</summary>
        /// <value>The sen light slope.</value>
        public double SenLightSlope { get; set; }

        /// <summary>Gets or sets the sen rate water.</summary>
        /// <value>The sen rate water.</value>
        public double SenRateWater { get; set; }

        /// <summary>Gets or sets the n senescence concentration.</summary>
        /// <value>The n senescence concentration.</value>
        public double NSenescenceConcentration { get; set; }

        /// <summary>Gets or sets the senescence detachment fraction.</summary>
        /// <value>The senescence detachment fraction.</value>
        public double SenescenceDetachmentFraction { get; set; }
        #endregion

        #region Variables we need from other modules
        /// <summary>The c o2</summary>
        double CO2 = 350;             // The TEModifier and NConcCriticalModifier function's use this.
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
        /// <summary>The width</summary>
        public double width = 0;
        /// <summary>The _ n demand</summary>
        private double _NDemand = 0;
        /// <summary>The _ soil n demand</summary>
        private double _SoilNDemand = 0;
        /// <summary>The n maximum</summary>
        private double NMax = 0;
        /// <summary>The sw_demand_te</summary>
        private double sw_demand_te = 0;
        /// <summary>The sw_demand</summary>
        private double sw_demand = 0;
        /// <summary>The n_conc_crit</summary>
        private double n_conc_crit = 0;
        /// <summary>The n_conc_max</summary>
        private double n_conc_max = 0;
        /// <summary>The n_conc_min</summary>
        private double n_conc_min = 0;
        /// <summary>The radiation intercepted green</summary>
        private double radiationInterceptedGreen;
        /// <summary>The _ leaves per node</summary>
        private double _LeavesPerNode = 0;
        /// <summary>The _ lai</summary>
        private double _LAI = 0;
        /// <summary>The maximum lai</summary>
        private double maxLAI = 0;
        /// <summary>The _ slai</summary>
        private double _SLAI = 0;
        /// <summary>The DLT lai</summary>
        private double dltLAI;
        /// <summary>The DLT slai</summary>
        private double dltSLAI;
        /// <summary>The DLT la i_pot</summary>
        private double dltLAI_pot;
        /// <summary>The DLT la i_stressed</summary>
        private double dltLAI_stressed;
        /// <summary>The DLT la i_carbon</summary>
        private double dltLAI_carbon;
        /// <summary>The DLT sla i_detached</summary>
        private double dltSLAI_detached;
        /// <summary>The DLT sla i_age</summary>
        private double dltSLAI_age;
        /// <summary>The DLT sla i_light</summary>
        private double dltSLAI_light;
        /// <summary>The DLT sla i_water</summary>
        private double dltSLAI_water;
        /// <summary>The DLT sla i_frost</summary>
        private double dltSLAI_frost;
        /// <summary>The DLT leaf no</summary>
        private double dltLeafNo;
        /// <summary>The DLT leaf no pot</summary>
        private double dltLeafNoPot;
        /// <summary>The DLT leaf no sen</summary>
        private double dltLeafNoSen;
        /// <summary>The DLT node no pot</summary>
        private double dltNodeNoPot;
        /// <summary>The external sw demand</summary>
        private bool ExternalSWDemand = false;
        /// <summary>The transp eff</summary>
        private double transpEff;

        /// <summary>Gets or sets the node no.</summary>
        /// <value>The node no.</value>
        [XmlIgnore]
        public double NodeNo { get; set; }
        /// <summary>The leaf no</summary>
        private double[] LeafNo;

        /// <summary>The leaf no sen</summary>
        private double[] LeafNoSen;
        /// <summary>The DLT node no</summary>
        private double dltNodeNo;
        /// <summary>The leaf area</summary>
        private double[] LeafArea;
        /// <summary>The max_node</summary>
        private const int max_node = 25;
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
                    sw_demand = MathUtilities.Constrain(sw_demand_te, Double.MinValue, SWDemandMax);
                    transpEff = transpEff * MathUtilities.Divide(sw_demand_te, sw_demand, 1.0);
                }
            }
            Util.Debug("Leaf.sw_demand=%f", sw_demand);
            Util.Debug("Leaf.transpEff=%f", transpEff);
        }
        /// <summary>Does the sw uptake.</summary>
        /// <param name="SWDemand">The sw demand.</param>
        public override void DoSWUptake(double SWDemand) { }

        // dry matter
        /// <summary>Gets the dm supply.</summary>
        /// <value>The dm supply.</value>
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
        /// <summary>Gets the dm retrans supply.</summary>
        /// <value>The dm retrans supply.</value>
        public override double DMRetransSupply
        {
            get
            {
                return MathUtilities.Constrain(Live.NonStructuralWt, 0.0, double.MaxValue);
            }
        }
        /// <summary>Gets the DLT dm pot rue.</summary>
        /// <value>The DLT dm pot rue.</value>
        public override double dltDmPotRue { get { return dlt_dm_pot_rue; } }
        /// <summary>Gets the dm green demand.</summary>
        /// <value>The dm green demand.</value>
        public override double DMGreenDemand
        {
            get
            {
                // Maximum DM this part can take today (PFR)
                return MathUtilities.Divide(dltLAI_stressed, SLAMin * Conversions.smm2sm, 0.0);
            }
        }
        /// <summary>Gets the dm demand differential.</summary>
        /// <value>The dm demand differential.</value>
        public override double DMDemandDifferential { get { return 0; } }
        /// <summary>Does the dm demand.</summary>
        /// <param name="DMSupply">The dm supply.</param>
        public override void DoDMDemand(double DMSupply) { }
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
            Util.Debug("Leaf.Growth.StructuralWt=%f", Growth.StructuralWt);
            Util.Debug("Leaf.Growth.NonStructuralWt=%f", Growth.NonStructuralWt);
        }
        /// <summary>Does the senescence.</summary>
        public override void DoSenescence()
        {
            double fraction_senescing = MathUtilities.Constrain(DMSenescenceFraction.Value, 0.0, 1.0);

            Senescing.StructuralWt = (Live.StructuralWt + Growth.StructuralWt + Retranslocation.StructuralWt) * fraction_senescing;
            Senescing.NonStructuralWt = (Live.NonStructuralWt + Growth.NonStructuralWt + Retranslocation.NonStructuralWt) * fraction_senescing;
            Util.Debug("Leaf.Senescing.StructuralWt=%f", Senescing.StructuralWt);
            Util.Debug("Leaf.Senescing.NonStructuralWt=%f", Senescing.NonStructuralWt);

        }
        /// <summary>Does the detachment.</summary>
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
        /// <summary>Removes the biomass.</summary>
        public override void RemoveBiomass()
        {
            double chop_fr_green = MathUtilities.Divide(GreenRemoved.Wt, Live.Wt, 0.0);
            double chop_fr_sen = MathUtilities.Divide(SenescedRemoved.Wt, Dead.Wt, 0.0);

            double dlt_lai = LAI * chop_fr_green;
            double dlt_slai = SLAI * chop_fr_sen;

            // keep leaf area above a minimum
            double lai_init = InitialTPLA * Conversions.smm2sm * Population.Density;
            double dlt_lai_max = LAI - lai_init;
            dlt_lai = MathUtilities.Constrain(dlt_lai, double.MinValue, dlt_lai_max);

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
            Util.Debug("Leaf.NDemand=%f", _NDemand);
            Util.Debug("Leaf.NMax=%f", NMax);
        }
        /// <summary>Does the n demand1 pot.</summary>
        /// <param name="dltDmPotRue">The DLT dm pot rue.</param>
        public override void DoNDemand1Pot(double dltDmPotRue)
        {
            Biomass OldGrowth = Growth;
            Growth.StructuralWt = dltDmPotRue * MathUtilities.Divide(Live.Wt, TotalLive.Wt, 0.0);
            Util.Debug("Leaf.Growth.StructuralWt=%f", Growth.StructuralWt);
            Util.CalcNDemand(dltDmPotRue, dltDmPotRue, n_conc_crit, n_conc_max, Growth, Live, Retranslocation.N, 1.0,
                       ref _NDemand, ref NMax);
            Growth.StructuralWt = 0.0;
            Growth.NonStructuralWt = 0.0;
            Util.Debug("Leaf.NDemand=%f", _NDemand);
            Util.Debug("Leaf.NMax=%f", NMax);
        }
        /// <summary>Does the soil n demand.</summary>
        public override void DoSoilNDemand()
        {
            _SoilNDemand = NDemand - dlt_n_senesced_retrans;
            _SoilNDemand = MathUtilities.Constrain(_SoilNDemand, 0.0, double.MaxValue);
            Util.Debug("Leaf.SoilNDemand=%f", _SoilNDemand);
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
                Retranslocation.StructuralN = -GrainNDemand * MathUtilities.Divide(AvailableRetranslocateN, NSupply, 0.0);
            }
            Util.Debug("Leaf.Retranslocation.N=%f", Retranslocation.N);
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

            Util.Debug("Leaf.SenescingN=%f", SenescingN);
            Util.Debug("Leaf.dlt.n_senesced_trans=%f", dlt_n_senesced_trans);
        }
        /// <summary>Does the n senesced retranslocation.</summary>
        /// <param name="navail">The navail.</param>
        /// <param name="n_demand_tot">The n_demand_tot.</param>
        public override void DoNSenescedRetranslocation(double navail, double n_demand_tot)
        {
            dlt_n_senesced_retrans = navail * MathUtilities.Divide(NDemand, n_demand_tot, 0.0);
            Util.Debug("Leaf.dlt.n_senesced_retrans=%f", dlt_n_senesced_retrans);
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

            Util.Debug("Leaf.n_conc_crit=%f", n_conc_crit);
            Util.Debug("Leaf.n_conc_min=%f", n_conc_min);
            Util.Debug("Leaf.n_conc_max=%f", n_conc_max);

            n_conc_crit *= NConcCriticalModifier.Value;
            if (n_conc_crit <= n_conc_min)
                throw new Exception("nconc_crit < nconc_min!. What's happened to CO2??");
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
        /// <summary>Gets or sets the cover sen.</summary>
        /// <value>The cover sen.</value>
        [XmlIgnore]
        public override double CoverSen { get; protected set; }
        /// <summary>Does the potential rue.</summary>
        public override void DoPotentialRUE()
        {
            dlt_dm_pot_rue = Photosynthesis.PotentialDM(radiationInterceptedGreen);
            Util.Debug("Leaf.dlt.dm_pot_rue=%f", dlt_dm_pot_rue);
        }
        /// <summary>Intercepts the radiation.</summary>
        /// <param name="incomingSolarRadiation">The incoming solar radiation.</param>
        /// <returns></returns>
        public override double interceptRadiation(double incomingSolarRadiation)
        {
            radiationInterceptedGreen = CoverGreen * incomingSolarRadiation;
            return CoverTotal * incomingSolarRadiation;
        }
        /// <summary>Does the cover.</summary>
        public override void DoCover()
        {
            CoverGreen = CalculateCover(LAI, ExtinctionCoefficient.Value, PlantSpatial.CanopyFactor);
            CoverSen = CalculateCover(_SLAI, ExtinctionCoefficientDead.Value, PlantSpatial.CanopyFactor);
            Util.Debug("leaf.cover.green=%f", CoverGreen);
            Util.Debug("leaf.cover.sen=%f", CoverSen);
        }

        // update
        /// <summary>Updates this instance.</summary>
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

            double leaf_no_sen_tot = MathUtilities.Sum(LeafNoSen) + dltLeafNoSen;

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

            maxLAI = Math.Max(maxLAI, _LAI);

            Util.Debug("leaf.LeafNo=%f", MathUtilities.Sum(LeafNo));
            Util.Debug("leaf.LeafNoSen=%f", MathUtilities.Sum(LeafNoSen));
            Util.Debug("leaf.NodeNo=%f", NodeNo);
            Util.Debug("leaf.LAI=%f", _LAI);
            Util.Debug("leaf.SLAI=%f", _SLAI);

        }
        #endregion

        #region Public interface specific to Leaf
        /// <summary>Gets the n crit.</summary>
        /// <value>The n crit.</value>
        public double NCrit { get { return n_conc_crit * Live.Wt; } }
        /// <summary>Gets the n minimum.</summary>
        /// <value>The n minimum.</value>
        public double NMin { get { return n_conc_min * Live.Wt; } }
        /// <summary>Gets the n senesced trans.</summary>
        /// <value>The n senesced trans.</value>
        public double NSenescedTrans { get { return dlt_n_senesced_trans; } }

        /// <summary>Gets the slai.</summary>
        /// <value>The slai.</value>
        [Units("m^2/m^2")]
        public double SLAI { get { return _SLAI; } }

        /// <summary>Gets the leaf number.</summary>
        /// <value>The leaf number.</value>
        public double LeafNumber { get { return MathUtilities.Sum(LeafNo); } }
        /// <summary>Gets the leaf number dead.</summary>
        /// <value>The leaf number dead.</value>
        public double LeafNumberDead { get { return MathUtilities.Sum(LeafNoSen); } }
        /// <summary>Gets the node number now.</summary>
        /// <value>The node number now.</value>
         public double NodeNumberNow { get { return NodeNo + NodeNumberCorrection; } }
         /// <summary>Ratio of actual to potential lai</summary>
         /// <value>The lai ratio.</value>
        public double LAIRatio
        {
            get
            {
                return MathUtilities.Divide(dltLAI, dltLAI_stressed, 0.0);
            }
        }
        /// <summary>Gets the fraction canopy senescing.</summary>
        /// <value>The fraction canopy senescing.</value>
        public double FractionCanopySenescing { get { return MathUtilities.Divide(dltSLAI, _LAI + dltLAI, 0.0); } }
        /// <summary>Does the canopy expansion.</summary>
        public void DoCanopyExpansion()
        {
            dltNodeNoPot = 0.0;
            if (NodeFormationPeriod.Value == 1)
                dltNodeNoPot = MathUtilities.Divide(Phenology.CurrentPhase.TTForToday, NodeAppearanceRate.Value, 0.0);

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
        /// <summary>Actuals this instance.</summary>
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
        /// <summary>Leafs the death.</summary>
        internal void LeafDeath()
        {
            double leaf_no_sen_now;                       // total number of dead leaves yesterday

            double leaf_no_now = MathUtilities.Sum(LeafNo);

            double leaf_per_node = leaf_no_now * FractionLeafSenescenceRate;

            double node_sen_rate = MathUtilities.Divide(NodeSenescenceRate,
                                                      1.0 + NFactLeafSenescenceRate * (1.0 - NStress.Expansion),
                                                      0.0);

            double leaf_death_rate = MathUtilities.Divide(node_sen_rate, leaf_per_node, 0.0);

            if (Phenology.InPhase("ReadyForHarvesting"))
            {
                // Constrain leaf death to remaining leaves
                //cnh do we really want to do this?;  XXXX
                leaf_no_sen_now = MathUtilities.Sum(LeafNoSen);
                dltLeafNoSen = MathUtilities.Constrain(leaf_no_now - leaf_no_sen_now, 0.0, double.MaxValue);
            }
            else if (LeafSenescencePeriod.Value == 1)
            {
                dltLeafNoSen = MathUtilities.Divide(Phenology.CurrentPhase.TTForToday, leaf_death_rate, 0.0);

                // Ensure minimum leaf area remains
                double tpla_now = MathUtilities.Sum(LeafArea);
                double max_sen_area = MathUtilities.Constrain(tpla_now - MinTPLA, 0.0, double.MaxValue) * Population.Density;
                double max_sleaf_no_now = LeafNumberFromArea(LeafArea, LeafNo, max_node, max_sen_area);

                // Constrain leaf death to remaining leaves
                leaf_no_sen_now = MathUtilities.Sum(LeafNoSen);
                dltLeafNoSen = MathUtilities.Constrain(dltLeafNoSen, double.MinValue, max_sleaf_no_now - leaf_no_sen_now);
            }
            else
            {
                dltLeafNoSen = 0.0;
            }
            Util.Debug("Leaf.dltLeafNoSen=%f", dltLeafNoSen);
        }
        /// <summary>Calculate todays leaf area senescence</summary>
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
        /// <summary>Gets the maximum lai.</summary>
        public double MaximumLAI { get { return maxLAI; } }
        #endregion

        #region Event handlers
        /// <summary>Called when [simulation commencing].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        /// <exception cref="System.Exception">CO2 isn't at the default level, and model:  + Plant.Name +  has no CO2 parameterisations.</exception>
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

        /// <summary>Called when [prepare].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
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

        /// <summary>Called when [harvest].</summary>
        /// <param name="Harvest">The harvest.</param>
        /// <param name="BiomassRemoved">The biomass removed.</param>
        public override void OnHarvest(HarvestType Harvest, BiomassRemovedType BiomassRemoved)
        {
            double dm_init = MathUtilities.Constrain(InitialWt * Population.Density, double.MinValue, Live.Wt);
            double n_init = MathUtilities.Constrain(dm_init * InitialNConcentration, double.MinValue, Live.N);
            //double p_init = MathUtilities.Constrain(dm_init * SimplePart::c.p_init_conc, double.MinValue, Green.P);

            double retain_fr_green = MathUtilities.Divide(dm_init, Live.Wt, 0.0);
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

                InitialiseAreas();
            }
        }
        #endregion

        #region Private functionality

        /// <summary>Initialises the areas.</summary>
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

            double avg_leaf_area = MathUtilities.Divide(InitialTPLA, InitialLeafNumber, 0.0);
            for (int leaf = 0; leaf < leaf_no_emerged; leaf++)
                LeafArea[leaf] = avg_leaf_area * Population.Density;

            LeafArea[leaf_no_emerged] = leaf_emerging_fract * avg_leaf_area * Population.Density;

            _LAI = InitialTPLA * Conversions.smm2sm * Population.Density;
            _SLAI = 0.0;
            maxLAI = 0.0;

            Util.Debug("Leaf.InitGreen.StructuralWt=%f", Live.StructuralWt);
            Util.Debug("Leaf.InitGreen.StructuralN=%f", Live.StructuralN);
            Util.Debug("Leaf.InitLeafNo=%f", MathUtilities.Sum(LeafNo));
            Util.Debug("Leaf.InitLeafArea=%f", MathUtilities.Sum(LeafArea));
            Util.Debug("Leaf.InitLAI=%f", LAI);
            Util.Debug("Leaf.InitSLAI=%f", SLAI);
        }
        /// <summary>Gets the respiration.</summary>
        /// <value>The respiration.</value>
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
        /// <summary>Calculates the cover.</summary>
        /// <param name="LAI">The lai.</param>
        /// <param name="ExtinctionCoefficient">The extinction coefficient.</param>
        /// <param name="CanopyFactor">The canopy factor.</param>
        /// <returns></returns>
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
                return MathUtilities.Divide(cover_green_leaf_canopy, CanopyFactor, 0);
            }
            else
                return 0.0;
        }
        /// <summary>Derives number of leaves to result in given cumulative area</summary>
        /// <param name="g_leaf_area">The g_leaf_area.</param>
        /// <param name="g_leaf_no">The g_leaf_no.</param>
        /// <param name="NumNodes">The number nodes.</param>
        /// <param name="pla">The pla.</param>
        /// <returns></returns>
        private double LeafNumberFromArea(double[] g_leaf_area, double[] g_leaf_no, int NumNodes, double pla)
        {
            int node_no = 1 + Util.GetCumulativeIndex(pla, g_leaf_area, NumNodes);

            // number of complete nodes
            double node_area_whole = MathUtilities.Sum(g_leaf_area, 0, node_no - 1, 0.0);

            // area from last node (mm^2)
            double node_area_part = pla - node_area_whole;

            // fraction of last node (0-1)
            double node_fract = MathUtilities.Divide(node_area_part, g_leaf_area[node_no - 1], 0.0);

            return MathUtilities.Sum(g_leaf_no, 0, node_no, 0.0) + node_fract * g_leaf_no[node_no - 1];
        }

        /// <summary>
        /// Calculate the leaf senescence
        /// due to normal phenological (phasic, age) development
        /// </summary>
        /// <returns></returns>
        private double LeafAreaSenescenceAge()
        {
            // get highest leaf no. senescing today
            double leaf_no_dead = MathUtilities.Sum(LeafNoSen) + dltLeafNoSen;
            int dying_node = Util.GetCumulativeIndex(leaf_no_dead, LeafNo, max_node);

            // get area senesced from highest leaf no.
            if (dying_node >= 0)
            {
                // senesced leaf area from current node dying (mm^2)
                double area_sen_dying_node = MathUtilities.Divide(leaf_no_dead - MathUtilities.Sum(LeafNo, 0, dying_node, 0)
                                              , LeafNo[dying_node]
                                              , 0.0) * LeafArea[dying_node];

                // lai senesced by natural ageing
                const double Density = 1.0;  // because LeafArea is on an area basis and not a plant basis
                double slai_age = (MathUtilities.Sum(LeafArea, 0, dying_node, 0)
                              + area_sen_dying_node)
                              * Conversions.smm2sm * Density;

                double min_lai = MinTPLA * Density * Conversions.smm2sm;
                double max_sen = MathUtilities.Constrain(_LAI - min_lai, 0.0, double.MaxValue);
                return MathUtilities.Constrain(slai_age - _SLAI, 0.0, max_sen);
            }
            return 0.0;
        }

        /// <summary>Return the lai that would senesce on the current day due to shading</summary>
        /// <returns></returns>
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
            double max_sen = MathUtilities.Constrain(_LAI - min_lai, 0.0, double.MaxValue);

            return MathUtilities.Constrain(_LAI * slai_light_fac, 0.0, max_sen);
        }

        /// <summary>Return the lai that would senesce on the current day due to water stress</summary>
        /// <returns></returns>
        private double LeafAreaSenescenceWater()
        {
            // drought stress factor
            double slai_water_fac = SenRateWater * (1.0 - SWStress.Photo);
            double dlt_slai_water = _LAI * slai_water_fac;
            double min_lai = MinTPLA * Population.Density * Conversions.smm2sm;
            double max_sen = MathUtilities.Constrain(_LAI - min_lai, 0.0, double.MaxValue);
            return MathUtilities.Constrain(dlt_slai_water, 0.0, max_sen);
        }

        /// <summary>
        /// Return the lai that would senesce on the
        /// current day from low temperatures
        /// </summary>
        /// <returns></returns>
        private double LeafAreaSenescencFrost()
        {
            double dlt_slai_low_temp = LeafSenescenceFrost.Value * _LAI;
            double min_lai = MinTPLA * Population.Density * Conversions.smm2sm;
            double max_sen = MathUtilities.Constrain(_LAI - min_lai, 0.0, double.MaxValue);
            return MathUtilities.Constrain(dlt_slai_low_temp, 0.0, max_sen);
        }


        /// <summary>Remove detachment from leaf area record</summary>
        /// <param name="dlt_slai_detached">The dlt_slai_detached.</param>
        /// <param name="dlt_lai_removed">The dlt_lai_removed.</param>
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
                if (!MathUtilities.FloatsAreEqual(LeafArea[node], 0.0, 1.0E-4f))    // Slop?
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