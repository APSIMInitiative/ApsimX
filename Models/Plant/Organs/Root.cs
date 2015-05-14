using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using Models.PMF.Functions;
using Models.Soils;
using System.Xml.Serialization;
using Models.PMF.Interfaces;
using Models.Soils.Arbitrator;
using APSIM.Shared.Utilities;

namespace Models.PMF.Organs
{
    ///<summary>
    /// The generic root model
    ///</summary>
    /// \param InitialDM <b>(Constant)</b> The initial dry weight of root (\f$g mm^{-2}\f$. CHECK).
    /// \param SpecificRootLength <b>(Constant)</b> The length of the specific root 
    ///     (\f$m g^{-1}\f$. CHECK).
    /// \param KNO3 <b>(Constant)</b> Fraction of extractable soil NO3 (\f$K_{NO3}\f$, unitless).  
    /// \param KNH4 <b>(Constant)</b> Fraction of extractable soil NH4 (\f$K_{NH4}\f$, unitless).  
    /// \param NitrogenDemandSwitch <b>(IFunction)</b> Whether to switch on nitrogen demand 
    ///     when nitrogen deficit is calculated (0 or 1, unitless).
    /// \param RootFrontVelocity <b>(IFunction)</b> The daily growth speed of root depth 
    ///     (\f$mm d^{-1}\f$. CHECK).
    /// \param PartitionFraction <b>(IFunction)</b> The fraction of biomass partitioning 
    ///     into root (0-1, unitless).
    /// \param KLModifier <b>(IFunction)</b> The modifier for KL factor which is defined as 
    ///     the fraction of available water able to be extracted per day, and empirically 
    ///     derived incorporating both plant and soil factors which limit rate of water 
    ///     update (0-1, unitless).
    /// \param TemperatureEffect <b>(IFunction)</b> 
    ///     The temperature effects on root depth growth (0-1, unitless).
    /// \param MaximumNConc <b>(IFunction)</b> 
    ///     Maximum nitrogen concentration (\f$g m^{-2}\f$. CHECK).
    /// \param MinimumNConc <b>(IFunction)</b> 
    ///     Minimum nitrogen concentration (\f$g m^{-2}\f$. CHECK).
    /// \param MaxDailyNUptake <b>(IFunction)</b> 
    ///     Maximum daily nitrogen update (\f$kg ha^{-1}\f$. CHECK).
    /// 
    /// \param SenescenceRate <b>(IFunction, Optional)</b> The daily senescence rate of 
    ///     root length (0-1, unitless).
    ///     
    /// \retval Length Total root length (mm).
    /// \retval Depth Root depth (mm).
    /// 
    ///<remarks>
    /// 
    /// Potential root growth 
    /// ------------------------
    ///  
    /// Actual root growth
    /// ------------------------
    /// 
    /// Nitrogen deficit 
    /// ------------------------
    /// 
    ///</remarks>
    [Serializable]
    public class Root : BaseOrgan, BelowGround
    {
        #region Links
        /// <summary>The plant</summary>
        [Link]
        Plant Plant = null;

        /// <summary>The arbitrator</summary>
        [Link]
        OrganArbitrator Arbitrator = null;

        /// <summary>The soil</summary>
        [Link]
        Soils.Soil Soil = null;
        #endregion
        
        #region Parameters
        /// <summary>Gets or sets the initial dm.</summary>
        /// <value>The initial dm.</value>
        public double InitialDM { get; set; }
        /// <summary>Gets or sets the length of the specific root.</summary>
        /// <value>The length of the specific root.</value>
        public double SpecificRootLength { get; set; }
        /// <summary>The KNO3 for each root layer at a given root lenght density (KNO3_xRootLength).  Actual KNO3 us calculated each day for each layer with current root length density of each layer </summary>
        public double[] KNO3_yProperty { get; set; }
        /// <summary>Rootlength density that KNO3 is related to </summary>
        public double[] KNO3_xRootLength { get; set; }
        /// <summary>The KNH4 for each root layer at a given root lenght density (KNH4_xRootLength).  Actual KNH4 us calculated each day for each layer with current root length density of each layer </summary>
        public double[] KNH4_yProperty { get; set; }
        /// <summary>Rootlength density that KNH4 is related to </summary>
        public double[] KNH4_xRootLength { get; set; }
        
        /// <summary>The nitrogen demand switch</summary>
        [Link]
        IFunction NitrogenDemandSwitch = null;
        /// <summary>The senescence rate</summary>
        [Link(IsOptional = true)]
        [Units("/d")]
        IFunction SenescenceRate = null;
        /// <summary>The temperature effect</summary>
        [Link]
        [Units("0-1")]
        IFunction TemperatureEffect = null;
        /// <summary>The root front velocity</summary>
        [Link]
        [Units("mm/d")]
        IFunction RootFrontVelocity = null;
        /// <summary>The partition fraction</summary>
        [Link]
        [Units("0-1")]
        IFunction PartitionFraction = null;
        /// <summary>The maximum n conc</summary>
        [Link]
        [Units("g/g")]
        IFunction MaximumNConc = null;
        /// <summary>The maximum daily n uptake</summary>
        [Link]
        [Units("kg N/ha")]
        IFunction MaxDailyNUptake = null;
        /// <summary>The minimum n conc</summary>
        [Link]
        [Units("g/g")]
        IFunction MinimumNConc = null;
        /// <summary>The kl modifier</summary>
        [Link]
        [Units("0-1")]
        IFunction KLModifier = null;

        #endregion

        #region States
        /// <summary>The kgha2gsm</summary>
        private const double kgha2gsm = 0.1;
        /// <summary>The uptake</summary>
        private double[] Uptake = null;
        /// <summary>The delta n h4</summary>
        private double[] DeltaNH4;
        /// <summary>The delta n o3</summary>
        private double[] DeltaNO3;
        /// <summary>The _ senescence rate</summary>
        private double _SenescenceRate = 0;
        /// <summary>The Nuptake</summary>
        private double[] NitUptake = null;

        /// <summary>Gets or sets the layer live.</summary>
        /// <value>The layer live.</value>
        [XmlIgnore]
        public Biomass[] LayerLive { get; set; }
        /// <summary>Gets or sets the layer dead.</summary>
        /// <value>The layer dead.</value>
        [XmlIgnore]
        public Biomass[] LayerDead { get; set; }
        /// <summary>Gets or sets the length.</summary>
        /// <value>The length.</value>
        [XmlIgnore]
        public double Length { get; set; }

        /// <summary>Gets or sets the depth.</summary>
        /// <value>The depth.</value>
        [XmlIgnore]
        [Units("mm")]
        public double Depth { get; set; }

        /// <summary>Clears this instance.</summary>
        protected override void Clear()
        {
            base.Clear();
            Uptake = null;
            DeltaNH4 = null;
            DeltaNO3 = null;
            _SenescenceRate = 0;
            Length = 0;
            Depth = 0;

            if (LayerLive == null || LayerLive.Length == 0)
            {
                LayerLive = new Biomass[Soil.Thickness.Length];
                LayerDead = new Biomass[Soil.Thickness.Length];
                for (int i = 0; i < Soil.Thickness.Length; i++)
                {
                    LayerLive[i] = new Biomass();
                    LayerDead[i] = new Biomass();
                }
            }
            else
            {
                for (int i = 0; i < Soil.Thickness.Length; i++)
                {
                    LayerLive[i].Clear();
                    LayerDead[i].Clear();
                }
            }


            DeltaNO3 = new double[Soil.Thickness.Length];
            DeltaNH4 = new double[Soil.Thickness.Length];
        }

        #endregion
        
        #region Class Properties
        /// <summary>Gets a value indicating whether this instance is growing.</summary>
        /// <value>
        /// <c>true</c> if this instance is growing; otherwise, <c>false</c>.
        /// </value>
        private bool isGrowing { get { return (Plant.IsAlive && Plant.SowingData.Depth < this.Depth); } }

        /// <summary>The soil crop</summary>
        private SoilCrop soilCrop;

        /// <summary>Gets the l ldep.</summary>
        /// <value>The l ldep.</value>
        [Units("mm")]
        double[] LLdep
        {
            get
            {
                double[] value = new double[Soil.Thickness.Length];
                for (int i = 0; i < Soil.Thickness.Length; i++)
                    value[i] = soilCrop.LL[i] * Soil.Thickness[i];
                return value;
            }
        }

        /// <summary>Gets the length density.</summary>
        /// <value>The length density.</value>
        [Units("??mm/mm3")]
        public double[] LengthDensity
        {
            get
            {
                double[] value = new double[Soil.Thickness.Length];
                for (int i = 0; i < Soil.Thickness.Length; i++)
                    value[i] = LayerLive[i].Wt * SpecificRootLength / 1000000 / Soil.Thickness[i];
                return value;
            }
        }
        /// <summary>Gets the RLV.</summary>
        /// <value>The RLV.</value>
        [Units("??km/mm3")]
        double[] rlv
        {
            get
            {
                return LengthDensity;
            }
        }
        #endregion

        #region Functions

        /// <summary>
        /// Gets or sets the nuptake supply.
        /// </summary>
        public double NuptakeSupply { get; set; }

        /// <summary>Event from sequencer telling us to do our potential growth.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoPotentialPlantGrowth")]
        private void OnDoPotentialPlantGrowth(object sender, EventArgs e)
        {
            if (Plant.IsEmerged)
            {
                _SenescenceRate = 0;
                if (SenescenceRate != null) //Default of zero means no senescence
                    _SenescenceRate = SenescenceRate.Value;

                /*  if (Live.Wt == 0)
                  {
                      //determine how many layers to put initial DM into.
                      Depth = Plant.SowingData.Depth;
                      double AccumulatedDepth = 0;
                      double InitialLayers = 0;
                      for (int layer = 0; layer < Soil.Thickness.Length; layer++)
                      {
                          if (AccumulatedDepth < Depth)
                              InitialLayers += 1;
                          AccumulatedDepth += Soil.SoilWater.Thickness[layer];
                      }
                      for (int layer = 0; layer < Soil.Thickness.Length; layer++)
                      {
                          if (layer <= InitialLayers - 1)
                          {
                              //dirstibute root biomass evently through root depth
                              LayerLive[layer].StructuralWt = InitialDM / InitialLayers * Plant.Population;
                              LayerLive[layer].StructuralN = InitialDM / InitialLayers * MaxNconc * Plant.Population;
                          }
                      }
               
                  }
                  */
                Length = 0;
                for (int layer = 0; layer < Soil.Thickness.Length; layer++)
                    Length += LengthDensity[layer];
            }
        }

        /// <summary>Does the nutrient allocations.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoActualPlantGrowth")]
        private void OnDoActualPlantGrowth(object sender, EventArgs e)
        {
            
            // Do Root Front Advance
            int RootLayer = LayerIndex(Depth);
            double TEM = (TemperatureEffect == null) ? 1 : TemperatureEffect.Value;

            Depth = Depth + RootFrontVelocity.Value * soilCrop.XF[RootLayer] * TEM;
            double MaxDepth = 0;
            for (int i = 0; i < Soil.Thickness.Length; i++)
                if (soilCrop.XF[i] > 0)
                    MaxDepth += Soil.Thickness[i];

            Depth = Math.Min(Depth, MaxDepth);

            // Do Root Senescence
            FOMLayerLayerType[] FOMLayers = new FOMLayerLayerType[Soil.Thickness.Length];

            for (int layer = 0; layer < Soil.Thickness.Length; layer++)
            {
                double DM = LayerLive[layer].Wt * _SenescenceRate * 10.0;
                double N = LayerLive[layer].StructuralN * _SenescenceRate * 10.0;
                LayerLive[layer].StructuralWt *= (1.0 - _SenescenceRate);
                LayerLive[layer].NonStructuralWt *= (1.0 - _SenescenceRate);
                LayerLive[layer].StructuralN *= (1.0 - _SenescenceRate);
                LayerLive[layer].NonStructuralN *= (1.0 - _SenescenceRate);



                FOMType fom = new FOMType();
                fom.amount = (float)DM;
                fom.N = (float)N;
                fom.C = (float)(0.40 * DM);
                fom.P = 0;
                fom.AshAlk = 0;

                FOMLayerLayerType Layer = new FOMLayerLayerType();
                Layer.FOM = fom;
                Layer.CNR = 0;
                Layer.LabileP = 0;

                FOMLayers[layer] = Layer;
            }
            FOMLayerType FomLayer = new FOMLayerType();
            FomLayer.Type = Plant.CropType;
            FomLayer.Layer = FOMLayers;
            IncorpFOM.Invoke(FomLayer);
        }

        /// <summary>Does the water uptake.</summary>
        /// <param name="Amount">The amount.</param>
        public override void DoWaterUptake(double[] Amount)
        {
            // Send the delta water back to SoilWat that we're going to uptake.
            WaterChangedType WaterUptake = new WaterChangedType();
            WaterUptake.DeltaWater = MathUtilities.Multiply_Value(Amount, -1.0);

            Uptake = WaterUptake.DeltaWater;
            if (WaterChanged != null)
                WaterChanged.Invoke(WaterUptake);
        }

        /// <summary>Does the Nitrogen uptake.</summary>
        /// <param name="NO3NAmount">The NO3NAmount.</param>
        /// <param name="NH4NAmount">The NH4NAmount.</param>
        public override void DoNitrogenUptake(double[] NO3NAmount, double[] NH4NAmount)
        {
            // Send the delta water back to SoilN that we're going to uptake.
            NitrogenChangedType NitrogenUptake = new NitrogenChangedType();
            NitrogenUptake.DeltaNO3 = MathUtilities.Multiply_Value(NO3NAmount, -1.0);
            NitrogenUptake.DeltaNH4 = MathUtilities.Multiply_Value(NH4NAmount, -1.0);

            NitUptake = MathUtilities.Add(NitrogenUptake.DeltaNO3, NitrogenUptake.DeltaNH4);
            if (NitrogenChanged != null)
                NitrogenChanged.Invoke(NitrogenUptake);
        }
        /// <summary>Layers the index.</summary>
        /// <param name="depth">The depth.</param>
        /// <returns></returns>
        /// <exception cref="System.Exception">Depth deeper than bottom of soil profile</exception>
        private int LayerIndex(double depth)
        {
            double CumDepth = 0;
            for (int i = 0; i < Soil.Thickness.Length; i++)
            {
                CumDepth = CumDepth + Soil.Thickness[i];
                if (CumDepth >= depth) { return i; }
            }
            throw new Exception("Depth deeper than bottom of soil profile");
        }
        /// <summary>Roots the proportion.</summary>
        /// <param name="layer">The layer.</param>
        /// <param name="root_depth">The root_depth.</param>
        /// <returns></returns>
        private double RootProportion(int layer, double root_depth)
        {
            double depth_to_layer_bottom = 0;   // depth to bottom of layer (mm)
            double depth_to_layer_top = 0;      // depth to top of layer (mm)
            double depth_to_root = 0;           // depth to root in layer (mm)
            double depth_of_root_in_layer = 0;  // depth of root within layer (mm)
            // Implementation Section ----------------------------------
            for (int i = 0; i <= layer; i++)
                depth_to_layer_bottom += Soil.Thickness[i];
            depth_to_layer_top = depth_to_layer_bottom - Soil.Thickness[layer];
            depth_to_root = Math.Min(depth_to_layer_bottom, root_depth);
            depth_of_root_in_layer = Math.Max(0.0, depth_to_root - depth_to_layer_top);

            return depth_of_root_in_layer / Soil.Thickness[layer];
        }
        /// <summary>Soils the n supply.</summary>
        /// <param name="NO3Supply">The n o3 supply.</param>
        /// <param name="NH4Supply">The n h4 supply.</param>
        private void SoilNSupply(double[] NO3Supply, double[] NH4Supply)
        {
            double[] no3ppm = new double[Soil.Thickness.Length];
            double[] nh4ppm = new double[Soil.Thickness.Length];

            for (int layer = 0; layer < Soil.Thickness.Length; layer++)
            {
                if (LayerLive[layer].Wt > 0)
                {
                    bool DidInterpolate = false;
                    double kno3 = MathUtilities.LinearInterpReal(LengthDensity[layer], KNO3_xRootLength, KNO3_yProperty, out DidInterpolate);
                    double knh4 = MathUtilities.LinearInterpReal(LengthDensity[layer], KNH4_xRootLength, KNH4_yProperty, out DidInterpolate); 
                    double swaf = 0;
                    swaf = (Soil.Water[layer] - Soil.SoilWater.LL15mm[layer]) / (Soil.SoilWater.DULmm[layer] - Soil.SoilWater.LL15mm[layer]);
                    swaf = Math.Max(0.0, Math.Min(swaf, 1.0));
                    no3ppm[layer] = Soil.NO3N[layer] * (100.0 / (Soil.BD[layer] * Soil.Thickness[layer]));
                    NO3Supply[layer] = Soil.NO3N[layer] * kno3 * no3ppm[layer] * swaf;
                    nh4ppm[layer] = Soil.NH4N[layer] * (100.0 / (Soil.BD[layer] * Soil.Thickness[layer]));
                    NH4Supply[layer] = Soil.NH4N[layer] * knh4 * nh4ppm[layer] * swaf;
                }
                else
                {
                    NO3Supply[layer] = 0;
                    NH4Supply[layer] = 0;
                }
            }
        }

        /// <summary>Called when [simulation commencing].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        /// <exception cref="ApsimXException">Cannot find a soil crop parameterisation for  + Name</exception>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            soilCrop = this.Soil.Crop(this.Plant.Name) as SoilCrop;
            if (soilCrop == null)
                throw new ApsimXException(this, "Cannot find a soil crop parameterisation for " + Name);
            Clear();
        }

        /// <summary>Called when crop is ending</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantEnding")]
        private void OnPlantEnding(object sender, EventArgs e)
        {
            if (sender == Plant)
            {
                FOMLayerLayerType[] FOMLayers = new FOMLayerLayerType[Soil.Thickness.Length];

                for (int layer = 0; layer < Soil.Thickness.Length; layer++)
                {
                    double DM = (LayerLive[layer].Wt + LayerDead[layer].Wt) * 10.0;
                    double N = (LayerLive[layer].N + LayerDead[layer].N) * 10.0;

                    FOMType fom = new FOMType();
                    fom.amount = (float)DM;
                    fom.N = (float)N;
                    fom.C = (float)(0.40 * DM);
                    fom.P = 0;
                    fom.AshAlk = 0;

                    FOMLayerLayerType Layer = new FOMLayerLayerType();
                    Layer.FOM = fom;
                    Layer.CNR = 0;
                    Layer.LabileP = 0;

                    FOMLayers[layer] = Layer;
                }
                FOMLayerType FomLayer = new FOMLayerType();
                FomLayer.Type = Plant.CropType;
                FomLayer.Layer = FOMLayers;
                IncorpFOM.Invoke(FomLayer);
            }
        }


        /// <summary>Called when crop is ending</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="data">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantSowing")]
        private void OnPlantSowing(object sender, SowPlant2Type data)
        {
            if (data.Plant == Plant)
            {
                //Fixme, this can be deleted when arbitrator calculates uptake ?????
                Uptake = new double[Soil.Thickness.Length];

                Depth = Plant.SowingData.Depth;
                double AccumulatedDepth = 0;
                double InitialLayers = 0;
                for (int layer = 0; layer < Soil.Thickness.Length; layer++)
                {
                    if (AccumulatedDepth < Depth)
                        InitialLayers += 1;
                    AccumulatedDepth += Soil.SoilWater.Thickness[layer];
                }
                for (int layer = 0; layer < Soil.Thickness.Length; layer++)
                {
                    if (layer <= InitialLayers - 1)
                    {
                        //dirstibute root biomass evently through root depth
                        LayerLive[layer].StructuralWt = InitialDM / InitialLayers * Plant.Population;
                        LayerLive[layer].StructuralN = InitialDM / InitialLayers * MaxNconc * Plant.Population;
                    }
                }
            }
        }
        #endregion

        #region Arbitrator method calls
        /// <summary>Gets or sets the dm demand.</summary>
        /// <value>The dm demand.</value>
        public override BiomassPoolType DMDemand
        {
            get
            {
                double Demand = 0;
                if (isGrowing)
                    Demand = Arbitrator.DMSupply * PartitionFraction.Value;
                return new BiomassPoolType { Structural = Demand };
            }
        }

        /// <summary>Sets the dm potential allocation.</summary>
        /// <value>The dm potential allocation.</value>
        /// <exception cref="System.Exception">
        /// Invalid allocation of potential DM in + Name
        /// or
        /// Error trying to partition potential root biomass
        /// </exception>
        public override BiomassPoolType DMPotentialAllocation
        {
            set
            {
                if (Depth <= 0)
                    return; //cannot allocate growth where no length

                if (DMDemand.Structural == 0)
                    if (value.Structural < 0.000000000001) { }//All OK
                    else
                        throw new Exception("Invalid allocation of potential DM in" + Name);
                // Calculate Root Activity Values for water and nitrogen
                double[] RAw = new double[Soil.Thickness.Length];
                double[] RAn = new double[Soil.Thickness.Length];
                double TotalRAw = 0;
                double TotalRAn = 0; ;

                for (int layer = 0; layer < Soil.Thickness.Length; layer++)
                {
                    if (layer <= LayerIndex(Depth))
                        if (LayerLive[layer].Wt > 0)
                        {
                            RAw[layer] = Uptake[layer] / LayerLive[layer].Wt
                                       * Soil.Thickness[layer]
                                       * RootProportion(layer, Depth);
                            RAw[layer] = Math.Max(RAw[layer], 1e-20);  // Make sure small numbers to avoid lack of info for partitioning

                            RAn[layer] = (DeltaNO3[layer] + DeltaNH4[layer]) / LayerLive[layer].Wt
                                           * Soil.Thickness[layer]
                                           * RootProportion(layer, Depth);
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
                for (int layer = 0; layer < Soil.Thickness.Length; layer++)
                {
                    if (TotalRAw > 0)

                        LayerLive[layer].PotentialDMAllocation = value.Structural * RAw[layer] / TotalRAw;
                    else if (value.Structural > 0)
                        throw new Exception("Error trying to partition potential root biomass");
                    allocated += (TotalRAw > 0) ? value.Structural * RAw[layer] / TotalRAw : 0;
                }
            }
        }
        /// <summary>Sets the dm allocation.</summary>
        /// <value>The dm allocation.</value>
        /// <exception cref="System.Exception">Error trying to partition root biomass</exception>
        public override BiomassAllocationType DMAllocation
        {
            set
            {
                // Calculate Root Activity Values for water and nitrogen
                double[] RAw = new double[Soil.Thickness.Length];
                double[] RAn = new double[Soil.Thickness.Length];
                double TotalRAw = 0;
                double TotalRAn = 0;

                if (Depth <= 0)
                    return; // cannot do anything with no depth
                for (int layer = 0; layer < Soil.Thickness.Length; layer++)
                {
                    if (layer <= LayerIndex(Depth))
                        if (LayerLive[layer].Wt > 0)
                        {
                            RAw[layer] = Uptake[layer] / LayerLive[layer].Wt
                                       * Soil.Thickness[layer]
                                       * RootProportion(layer, Depth);
                            RAw[layer] = Math.Max(RAw[layer], 1e-20);  // Make sure small numbers to avoid lack of info for partitioning

                            RAn[layer] = (DeltaNO3[layer] + DeltaNH4[layer]) / LayerLive[layer].Wt
                                       * Soil.Thickness[layer]
                                       * RootProportion(layer, Depth);
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
                for (int layer = 0; layer < Soil.Thickness.Length; layer++)
                {
                    if (TotalRAw > 0)
                    {
                        LayerLive[layer].StructuralWt += value.Structural * RAw[layer] / TotalRAw;
                        allocated += value.Structural * RAw[layer] / TotalRAw;
                    }
                    else if (value.Structural > 0)
                        throw new Exception("Error trying to partition root biomass");
                        
                }
            }
        }

        /// <summary>Gets or sets the n demand.</summary>
        /// <value>The n demand.</value>
        [Units("g/m2")]
        public override BiomassPoolType NDemand
        {
            get
            {
                //Calculate N demand based on amount of N needed to bring root N content in each layer up to maximum
                double TotalDeficit = 0.0;
                double _NitrogenDemandSwitch = 1;
                if (NitrogenDemandSwitch != null) //Default of 1 means demand is always truned on!!!!
                    _NitrogenDemandSwitch = NitrogenDemandSwitch.Value;
                foreach (Biomass Layer in LayerLive)
                {
                    double NDeficit = Math.Max(0.0, MaximumNConc.Value * (Layer.Wt + Layer.PotentialDMAllocation) - Layer.N);
                    TotalDeficit += NDeficit;
                }
                TotalDeficit *= _NitrogenDemandSwitch;
                return new BiomassPoolType { Structural = TotalDeficit };
            }
        }

        /// <summary>Gets or sets the n supply.</summary>
        /// <value>The n supply.</value>
        public override BiomassSupplyType NSupply
        {
            get
            {
                if (Soil.Thickness != null)
                {
                    double[] no3supply = new double[Soil.Thickness.Length];
                    double[] nh4supply = new double[Soil.Thickness.Length];
                    SoilNSupply(no3supply, nh4supply);
                    double NSupply = (Math.Min(MathUtilities.Sum(no3supply), MaxDailyNUptake.Value) + Math.Min(MathUtilities.Sum(nh4supply), MaxDailyNUptake.Value)) * kgha2gsm;
                    NuptakeSupply = NSupply;
                    return new BiomassSupplyType { Uptake = NSupply };
                    
                }
                else
                    return new BiomassSupplyType();
            }
        }

        /// <summary>Gets the nitrogne supply.</summary>
        /// <value>The water supply.</value>
        public override double[] NO3NSupply(List<ZoneWaterAndN> zones)
        {
            if (zones.Count != 1)
                throw new Exception("PMF can only deal with one soil arbitrator zone at the moment");

            double[] NO3 = zones[0].NO3N;

            double[] NO3Supply = new double[Soil.Thickness.Length];

            double[] no3ppm = new double[Soil.Thickness.Length];

            double NO3uptake = 0;
           
            for (int layer = 0; layer < Soil.Thickness.Length; layer++)
            {
                if (LayerLive[layer].Wt > 0)
                {
                    double swaf = 0;
					swaf = (Soil.Water[layer] - Soil.SoilWater.LL15mm[layer]) / (Soil.SoilWater.DULmm[layer] - Soil.SoilWater.LL15mm[layer]);
                    swaf = Math.Max(0.0, Math.Min(swaf, 1.0));
                    bool DidInterpolate = false;
                    double kno3 = MathUtilities.LinearInterpReal(LengthDensity[layer], KNO3_xRootLength, KNO3_yProperty, out DidInterpolate);
					no3ppm[layer] = NO3[layer] * (100.0 / (Soil.BD[layer] * Soil.Thickness[layer]));
                    NO3Supply[layer] = Math.Min(NO3[layer] * kno3 * no3ppm[layer] * swaf, (MaxDailyNUptake.Value - NO3uptake));
                    NO3uptake += NO3Supply[layer];
                }
                else
                {
                    NO3Supply[layer] = 0;
                }
            }

            return NO3Supply;
        }
        /// <summary>Gets the nitrogne supply.</summary>
        /// <value>The water supply.</value>
        public override double[] NH4NSupply(List<ZoneWaterAndN> zones)
        {
            if (zones.Count != 1)
                throw new Exception("PMF can only deal with one soil arbitrator zone at the moment");

            double[] NH4 = zones[0].NH4N;

            double[] NH4Supply = new double[Soil.Thickness.Length];

            double[] NH4ppm = new double[Soil.Thickness.Length];

            double NH4uptake = 0;

            for (int layer = 0; layer < Soil.Thickness.Length; layer++)
            {
                if (LayerLive[layer].Wt > 0)
                {
                    double swaf = 0;
                    swaf = (Soil.Water[layer] - Soil.SoilWater.LL15mm[layer]) / (Soil.SoilWater.DULmm[layer] - Soil.SoilWater.LL15mm[layer]);
                    swaf = Math.Max(0.0, Math.Min(swaf, 1.0));
                    bool DidInterpolate = false;
                    double knh4 = MathUtilities.LinearInterpReal(LengthDensity[layer], KNH4_xRootLength, KNH4_yProperty, out DidInterpolate); swaf = (Soil.Water[layer] - Soil.SoilWater.LL15mm[layer]) / (Soil.SoilWater.DULmm[layer] - Soil.SoilWater.LL15mm[layer]);
                    NH4ppm[layer] = NH4Supply[layer] * (100.0 / (Soil.BD[layer] * Soil.Thickness[layer]));
                    NH4Supply[layer] = Math.Min(NH4[layer] * knh4 * NH4ppm[layer] * swaf, (MaxDailyNUptake.Value - NH4uptake));
                    NH4uptake += NH4Supply[layer]; 
                }
                else
                {
                    NH4Supply[layer] = 0;
                }
            }

            return NH4Supply;
        }
        /// <summary>Sets the n allocation.</summary>
        /// <value>The n allocation.</value>
        /// <exception cref="System.Exception">
        /// Cannot Allocate N to roots in layers when demand is zero
        /// or
        /// Error in N Allocation:  + Name
        /// or
        /// Request for N uptake exceeds soil N supply
        /// </exception>
        public override BiomassAllocationType NAllocation
        {
            set
            {
                // Recalculate N defict following DM allocation for checking N allocation and partitioning N between layers   
                double Demand = 0.0;
                foreach (Biomass Layer in LayerLive)
                {
                    double NDeficit = Math.Max(0.0, MaximumNConc.Value * Layer.Wt - Layer.N);
                    Demand += NDeficit;
                }
                double Supply = value.Structural;
                double NAllocated = 0;
                if ((Demand == 0) && (Supply > 0.0000000001))
                { throw new Exception("Cannot Allocate N to roots in layers when demand is zero"); }

                // Allocate N to each layer
                if (Demand > 0)
                {
                    foreach (Biomass Layer in LayerLive)
                    {
                        double NDeficit = Math.Max(0.0, MaximumNConc.Value * Layer.Wt - Layer.N);
                        double fraction = NDeficit / Demand;
                        double Allocation = fraction * Supply;
                        Layer.StructuralN += Allocation;
                        NAllocated += Allocation;
                    }
                }
                if (!MathUtilities.FloatsAreEqual(NAllocated - Supply, 0.0))
                {
                    throw new Exception("Error in N Allocation: " + Name);
                }


                //letting arbitrator do uptake now
                /*
                // uptake_gsm
                _Nuptake = value.Uptake;
                double Uptake = value.Uptake / kgha2gsm;
                NitrogenChangedType NitrogenUptake = new NitrogenChangedType();
                NitrogenUptake.Sender = "Plant2";
                NitrogenUptake.SenderType = "Plant";
                NitrogenUptake.DeltaNO3 = new double[Soil.Thickness.Length];
                NitrogenUptake.DeltaNH4 = new double[Soil.Thickness.Length];

                double[] no3supply = new double[Soil.Thickness.Length];
                double[] nh4supply = new double[Soil.Thickness.Length];
                SoilNSupply(no3supply, nh4supply);
                double NSupply = MathUtilities.Sum(no3supply) + MathUtilities.Sum(nh4supply);
                if (Uptake > 0)
                {
                    if (Uptake > NSupply + 0.001)
                        throw new Exception("Request for N uptake exceeds soil N supply");
                    double fraction = 0;
                    if (NSupply > 0) fraction = Uptake / NSupply;

                    for (int layer = 0; layer <= Soil.Thickness.Length - 1; layer++)
                    {
                        DeltaNO3[layer] = -no3supply[layer] * fraction;
                        DeltaNH4[layer] = -nh4supply[layer] * fraction;
                        NitrogenUptake.DeltaNO3[layer] = DeltaNO3[layer];
                        NitrogenUptake.DeltaNH4[layer] = DeltaNH4[layer];
                    }
                    if (NitrogenChanged != null)
                      NitrogenChanged.Invoke(NitrogenUptake); 

                }*/

            }
        }
        /// <summary>Gets or sets the maximum nconc.</summary>
        /// <value>The maximum nconc.</value>
        public override double MaxNconc
        {
            get
            {
                return MaximumNConc.Value;
            }
        }
        /// <summary>Gets or sets the minimum nconc.</summary>
        /// <value>The minimum nconc.</value>
        public override double MinNconc
        {
            get
            {
                return MinimumNConc.Value;
            }
        }


        /// <summary>Gets or sets the water supply.</summary>
        /// <value>The water supply.</value>
        public override double[] WaterSupply(List<ZoneWaterAndN> zones)
        {
            if (zones.Count != 1)
                throw new Exception("PMF can only deal with one soil arbitrator zone at the moment");

            double[] SW = zones[0].Water;

            double[] supply = new double[Soil.Thickness.Length];
            for (int layer = 0; layer < Soil.Thickness.Length; layer++)
                if (layer <= LayerIndex(Depth))
                    supply[layer] = Math.Max(0.0, soilCrop.KL[layer] * KLModifier.Value *
                        (SW[layer] - soilCrop.LL[layer] * Soil.Thickness[layer]) * RootProportion(layer, Depth));
                else
                    supply[layer] = 0;

            return supply;
        }

        /// <summary>Gets or sets the water uptake.</summary>
        /// <value>The water uptake.</value>
        [Units("mm")]
        public override double WaterUptake
        {
            get { return Uptake == null ? 0.0 : -MathUtilities.Sum(Uptake); }
        }
        
        /// <summary>Gets or sets the water uptake.</summary>
        /// <value>The water uptake.</value>
        [Units("mm")]
        public override double NUptake
        {
            get {return NitUptake == null ? 0.0 : -MathUtilities.Sum(NitUptake);}
        }
        #endregion

        #region Event handlers


        /// <summary>Called when [water uptakes calculated].</summary>
        /// <param name="SoilWater">The soil water.</param>
        [EventSubscribe("WaterUptakesCalculated")]
        private void OnWaterUptakesCalculated(WaterUptakesCalculatedType SoilWater)
        {
        
            // Gets the water uptake for each layer as calculated by an external module (SWIM)

            Uptake = new double[Soil.Thickness.Length];

            for (int i = 0; i != SoilWater.Uptakes.Length; i++)
            {
                string UName = SoilWater.Uptakes[i].Name;
                if (UName == Plant.Name)
                {
                    int length = SoilWater.Uptakes[i].Amount.Length;
                    for (int layer = 0; layer < length; layer++)
                    {
                        Uptake[layer] = -(float)SoilWater.Uptakes[i].Amount[layer];
                    }
                }
            }
        }

        /// <summary>Occurs when [incorp fom].</summary>
        public event FOMLayerDelegate IncorpFOM;

        /// <summary>Occurs when [nitrogen changed].</summary>
        public event NitrogenChangedDelegate NitrogenChanged;

        /// <summary>Occurs when [nitrogen changed].</summary>
        public event WaterChangedDelegate WaterChanged;
        #endregion


    }
}
