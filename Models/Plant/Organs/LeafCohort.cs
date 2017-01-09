using System;
using System.Collections.Generic;
using Models.Core;
using System.Xml.Serialization;
using Models.PMF.Interfaces;
using Models.Interfaces;
using APSIM.Shared.Utilities;

namespace Models.PMF.Organs
{
    ///<summary>
    /// A leaf cohort model
    /// </summary>
    /// <remarks>
    /// 
    /// @startuml
    /// Initialized -> Appeared: Appearance 
    /// Appeared -> Expanded: GrowthDuration
    /// Expanded -> Senescing: LagDuration
    /// Senescing -> Senesced: SenescenceDuration
    /// Senesced -> Detaching: DetachmentLagDuration
    /// Detaching -> Detached: DetachmentDuration
    /// Initialized ->Expanded: IsGrowing
    /// Initialized -> Senesced: IsAlive
    /// Initialized -> Senesced: IsGreen
    /// Initialized -> Senescing: IsNotSenescing
    /// Senescing -> Senesced: IsSenescing
    /// Expanded -> Detached: IsFullyExpanded
    /// Senesced -> Detached: ShouldBeDead
    /// Senesced -> Detached: Finished
    /// Appeared -> Detached: IsAppeared
    /// Initialized -> Detached: IsInitialised
    /// @enduml
    /// 
    /// Leaf death
    /// ------------------------
    /// The leaf area, structural biomass and structural nitrogen of 
    /// green (live) parts is subtracted by a fraction.
    /// 
    /// </remarks>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class LeafCohort : Model
    {
        #region Paramater Input Classes

        /// <summary>The plant</summary>
        [Link]
        private Plant Plant = null;

        /// <summary>The structure</summary>
        [Link]
        private Structure Structure = null;

        /// <summary>The leaf</summary>
        [Link]
        private Leaf Leaf = null;

        /// <summary>The clock</summary>
        [Link]
        public Clock Clock = null;

        [Link]
        private ISurfaceOrganicMatter SurfaceOrganicMatter = null;

        /// <summary>The live</summary>
        [XmlIgnore]
        public Biomass Live = new Biomass();

        /// <summary>The dead</summary>
        [XmlIgnore]
        public Biomass Dead = new Biomass();

        /// <summary>The live start</summary>
        private Biomass liveStart;

        #endregion

        #region Class Fields

        /// <summary>The rank</summary>
        [Description("Rank")]
        public int Rank { get; set; } // 1 based ranking

        /// <summary>The area</summary>
        [Description("Area mm2")]
        public double Area { get; set; }

        //Leaf coefficients
        /// <summary>The age</summary>
        [XmlIgnore]
        public double Age;

        /// <summary>The n reallocation factor</summary>
        private double nReallocationFactor;

        /// <summary>The dm reallocation factor</summary>
        private double dmReallocationFactor;

        /// <summary>The n retranslocation factor</summary>
        private double nRetranslocationFactor;

        /// <summary>The dm retranslocation factor</summary>
        private double dmRetranslocationFactor;

        /// <summary>The functional n conc</summary>
        private double functionalNConc;

        /// <summary>The luxary n conc</summary>
        private double luxaryNConc;

        /// <summary>The maximum live area</summary>
        private double maxLiveArea;

        /// <summary>The maximum live area</summary>
        private double maxCohortPopulation;

        /// <summary>The structural fraction</summary>
        [XmlIgnore]
        public double StructuralFraction;

        /// <summary>The non structural fraction</summary>
        [XmlIgnore]
        public double NonStructuralFraction;
        
        /// <summary>The growth duration</summary>
        [XmlIgnore]
        public double GrowthDuration;

        /// <summary>The lag duration</summary>
        [XmlIgnore]
        public double LagDuration;

        /// <summary>The senescence duration</summary>
        [XmlIgnore]
        public double SenescenceDuration;

        /// <summary>The detachment lag duration</summary>
        [XmlIgnore]
        public double DetachmentLagDuration;

        /// <summary>The detachment duration</summary>
        [XmlIgnore]
        public double DetachmentDuration;

        /// <summary>The specific leaf area maximum</summary>
        [XmlIgnore]
        public double SpecificLeafAreaMax;

        /// <summary>The specific leaf area minimum</summary>
        [XmlIgnore]
        public double SpecificLeafAreaMin;

        /// <summary>The maximum n conc</summary>
        [XmlIgnore]
        public double MaximumNConc;

        /// <summary>The minimum n conc</summary>
        [XmlIgnore]
        public double MinimumNConc;

        /// <summary>The initial n conc</summary>
        [XmlIgnore]
        public double InitialNConc;

        /// <summary>The live area</summary>
        [XmlIgnore]
        public double LiveArea;

        /// <summary>The dead area</summary>
        [XmlIgnore]
        public double DeadArea;

        /// <summary>The maximum area</summary>
        [XmlIgnore]
        public double MaxArea;

        /// <summary>The maximum area</summary>
        [XmlIgnore]
        public double LeafSizeShape = 0.01;

        /// <summary>The size of senessing leaves relative to the other leaves in teh cohort</summary>
        [XmlIgnore]
        public double SenessingLeafRelativeSize = 1;

        /// <summary>Gets or sets the cover above.</summary>
        /// <value>The cover above.</value>
        [XmlIgnore]
        public double CoverAbove { get; set; }

        /// <summary>The shade induced sen rate</summary>
        private double shadeInducedSenRate;

        /// <summary>The senesced frac</summary>
        private double senescedFrac;

        /// <summary>The detached frac</summary>
        private double detachedFrac;

        /// <summary>The cohort population</summary>
        [XmlIgnore]
        public double CohortPopulation; //Number of leaves in this cohort

        /// <summary>The cell division stress factor</summary>
        [XmlIgnore]
        public double CellDivisionStressFactor = 1;

        /// <summary>The cell division stress accumulation</summary>
        [XmlIgnore]
        public double CellDivisionStressAccumulation;

        /// <summary>The cell division stress days</summary>
        [XmlIgnore]
        public double CellDivisionStressDays;

        //Leaf Initial status paramaters
        /// <summary>The leaf start n retranslocation supply</summary>
        [XmlIgnore]
        public double LeafStartNRetranslocationSupply;

        /// <summary>The leaf start n reallocation supply</summary>
        [XmlIgnore]
        public double LeafStartNReallocationSupply;

        /// <summary>The leaf start dm retranslocation supply</summary>
        [XmlIgnore]
        public double LeafStartDmRetranslocationSupply;

        /// <summary>The leaf start dm reallocation supply</summary>
        [XmlIgnore]
        public double LeafStartDmReallocationSupply;

        /// <summary>The leaf start area</summary>
        [XmlIgnore]
        public double LeafStartArea;

        /// <summary>
        /// The leaf start metabolic n reallocation supply
        /// </summary>
        [XmlIgnore]
        public double LeafStartMetabolicNReallocationSupply;

        /// <summary>
        /// The leaf start non structural n reallocation supply
        /// </summary>
        [XmlIgnore]
        public double LeafStartNonStructuralNReallocationSupply;

        /// <summary>
        /// The leaf start metabolic n retranslocation supply
        /// </summary>
        [XmlIgnore]
        public double LeafStartMetabolicNRetranslocationSupply;

        /// <summary>
        /// The leaf start non structural n retranslocation supply
        /// </summary>
        [XmlIgnore]
        public double LeafStartNonStructuralNRetranslocationSupply;

        /// <summary>
        /// The leaf start metabolic dm reallocation supply
        /// </summary>
        [XmlIgnore]
        public double LeafStartMetabolicDmReallocationSupply;

        /// <summary>
        /// The leaf start non structural dm reallocation supply
        /// </summary>
        [XmlIgnore]
        public double LeafStartNonStructuralDmReallocationSupply;

        //variables used in calculating daily supplies and deltas
        /// <summary>Gets the DM amount detached (send to surface OM) (g/m2)</summary>
        [XmlIgnore]
        public Biomass Detached { get; set; }

        /// <summary>Gets the DM amount removed from the system (harvested, grazed, etc) (g/m2)</summary>
        [XmlIgnore]
        public Biomass Removed { get; set; }

        /// <summary>The delta potential area</summary>
        private double deltaPotentialArea;

        /// <summary>The delta water constrained area</summary>
        public double DeltaWaterConstrainedArea;

        /// <summary>The delta carbon constrained area</summary>
        public double DeltaCarbonConstrainedArea;

        //private double StructuralDMDemand;
        //private double MetabolicDMDemand;
        /// <summary>The potential structural dm allocation</summary>
        private double potentialStructuralDmAllocation;

        /// <summary>The potential metabolic dm allocation</summary>
        private double potentialMetabolicDmAllocation;

        /// <summary>The metabolic n reallocated</summary>
        private double metabolicNReallocated;

        /// <summary>The metabolic wt reallocated</summary>
        private double metabolicWtReallocated;

        /// <summary>The non structural n reallocated</summary>
        private double nonStructuralNReallocated;

        /// <summary>The non structural wt reallocated</summary>
        private double nonStructuralWtReallocated;

        /// <summary>The metabolic n retranslocated</summary>
        private double metabolicNRetranslocated;

        /// <summary>The non structural n retrasnlocated</summary>
        private double nonStructuralNRetrasnlocated;

        /// <summary>The dm retranslocated</summary>
        private double dmRetranslocated;

        /// <summary>The metabolic n allocation</summary>
        private double metabolicNAllocation;

        /// <summary>The structural dm allocation</summary>
        private double structuralDmAllocation;

        /// <summary>The metabolic dm allocation</summary>
        private double metabolicDmAllocation;

        #endregion

        #region Class Properties

        /// <summary>Has the leaf chort been initialised?</summary>
        [XmlIgnore]
        public bool IsInitialised;

        /// <summary>Gets a value indicating whether this instance has not appeared.</summary>
        /// <value>
        /// <c>true</c> if this instance is not appeared; otherwise, <c>false</c>.
        /// </value>
        public bool IsNotAppeared
        {
            get { return IsInitialised && Age <= 0; }
        }

        /// <summary>Gets a value indicating whether this instance is growing.</summary>
        /// <value>
        /// <c>true</c> if this instance is growing; otherwise, <c>false</c>.
        /// </value>
        public bool IsGrowing
        {
            get { return Age < GrowthDuration; }
        }

        /// <summary>Gets or sets a value indicating whether this instance is appeared.</summary>
        /// <value>
        /// <c>true</c> if this instance is appeared; otherwise, <c>false</c>.
        /// </value>
        [XmlIgnore]
        public bool IsAppeared { get; set; }

        /// <summary>Gets a value indicating whether this instance is fully expanded.</summary>
        /// <value>
        /// <c>true</c> if this instance is fully expanded; otherwise, <c>false</c>.
        /// </value>
        public bool IsFullyExpanded
        {
            get { return IsAppeared && Age > GrowthDuration; }
        }

        /// <summary>Gets a value indicating whether this instance is green.</summary>
        /// <value><c>true</c> if this instance is green; otherwise, <c>false</c>.</value>
        public bool IsGreen
        {
            get { return Age < GrowthDuration + LagDuration + SenescenceDuration; }
        }
        /// <summary>Gets a value indicating whether this instance is senescing.</summary>
        /// <value>
        /// <c>true</c> if this instance is senescing; otherwise, <c>false</c>.
        /// </value>
        public bool IsSenescing
        {
            get { return Age > GrowthDuration + LagDuration; }
        }
        /// <summary>Gets a value indicating whether this instance is not senescing.</summary>
        /// <value>
        /// <c>true</c> if this instance is not senescing; otherwise, <c>false</c>.
        /// </value>
        public bool IsNotSenescing
        {
            get { return Age < GrowthDuration + LagDuration; }
        }
        /// <summary>Gets a value indicating whether this <see cref="LeafCohort"/> is finished.</summary>
        /// <value><c>true</c> if finished; otherwise, <c>false</c>.</value>
        public bool Finished
        {
            get { return IsAppeared && !IsGreen; }
        }
        /// <summary>Gets a value indicating whether this instance is dead.</summary>
        /// <value><c>true</c> if this instance is dead; otherwise, <c>false</c>.</value>
        public bool IsDead
        {
            get
            {
                return MathUtilities.FloatsAreEqual(LiveArea, 0.0) && !MathUtilities.FloatsAreEqual(DeadArea, 0.0);
            }
        }
        /// <summary>Gets the maximum size.</summary>
        /// <value>The maximum size.</value>
        public double MaxSize
        {
            get
            {
                if (maxLiveArea <= 0)
                    return 0;
                return maxLiveArea/maxCohortPopulation;
            }
        }

        /// <summary>Gets the fraction expanded.</summary>
        /// <value>The fraction expanded.</value>
        public double FractionExpanded
        {
            get
            {
                if (Age <= 0)
                    return 0;
                if (Age >= GrowthDuration)
                    return 1;
                return Age/GrowthDuration;
            }
        }

        /// <summary>Gets the size.</summary>
        /// <value>The size.</value>
        public double Size
        {
            get
            {
                if (IsAppeared)
                    return LiveArea / CohortPopulation;
                return 0;
            }
        }

        /// <summary>Gets the specific area.</summary>
        /// <value>The specific area.</value>
        public double SpecificArea
        {
            get
            {
                if (Live.Wt > 0)
                    return LiveArea / Live.Wt;
                return 0;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public double MaintenanceRespiration { get; set; }

        #endregion

        #region Arbitration methods

        /// <summary>Gets the structural dm demand.</summary>
        /// <value>The structural dm demand.</value>
        public double StructuralDMDemand
        {
            get
            {
                if (IsGrowing)
                {
                    double totalDmDemand = Math.Min(deltaPotentialArea/((SpecificLeafAreaMax + SpecificLeafAreaMin)/2),
                        DeltaWaterConstrainedArea/SpecificLeafAreaMin);
                    if (totalDmDemand < 0)
                        throw new Exception("Negative DMDemand in" + this);
                    return totalDmDemand*StructuralFraction;
                }
                return 0;
            }
        }

        /// <summary>Gets the metabolic dm demand.</summary>
        /// <value>The metabolic dm demand.</value>
        public double MetabolicDMDemand
        {
            get
            {
                if (IsGrowing)
                {
                    double totalDmDemand = Math.Min(deltaPotentialArea/((SpecificLeafAreaMax + SpecificLeafAreaMin)/2),
                        DeltaWaterConstrainedArea/SpecificLeafAreaMin);
                    return totalDmDemand*(1 - StructuralFraction);
                }
                return 0;
            }
        }

        /// <summary>Gets the non structural dm demand.</summary>
        /// <value>The non structural dm demand.</value>
        public double NonStructuralDMDemand
        {
            get
            {
                if (IsNotSenescing)
                {
                    double maxNonStructuralDM = (MetabolicDMDemand + StructuralDMDemand + liveStart.MetabolicWt +
                                                 liveStart.StructuralWt)*NonStructuralFraction;
                    return Math.Max(0.0, maxNonStructuralDM - liveStart.NonStructuralWt);
                }
                return 0.0;
            }
        }

        /// <summary>Gets the structural n demand.</summary>
        /// <value>The structural n demand.</value>
        public double StructuralNDemand
        {
            get
            {
                if (IsNotSenescing && shadeInducedSenRate <= 0.0)
                    // Assuming a leaf will have no demand if it is senescing and will have no demand if it is is shaded conditions
                    return MinimumNConc*potentialStructuralDmAllocation;
                return 0.0;
            }
        }

        /// <summary>Gets the non structural n demand.</summary>
        /// <value>The non structural n demand.</value>
        public double NonStructuralNDemand
        {
            get
            {
                if (IsNotSenescing && shadeInducedSenRate <= 0.0 && NonStructuralFraction > 0)
                    // Assuming a leaf will have no demand if it is senescing and will have no demand if it is is shaded conditions.  Also if there is 
                    return Math.Max(0.0, luxaryNConc*(liveStart.StructuralWt + liveStart.MetabolicWt
                                                      + potentialStructuralDmAllocation + potentialMetabolicDmAllocation) -
                                         Live.NonStructuralN);
                return 0.0;
            }
        }

        /// <summary>Gets the metabolic n demand.</summary>
        /// <value>The metabolic n demand.</value>
        public double MetabolicNDemand
        {
            get
            {
                if (IsNotSenescing && shadeInducedSenRate <= 0.0)
                    // Assuming a leaf will have no demand if it is senescing and will have no demand if it is is shaded conditions
                    return functionalNConc*potentialMetabolicDmAllocation;
                return 0.0;
            }
        }

        /// <summary>Sets the dm allocation.</summary>
        /// <value>The dm allocation.</value>
        public BiomassAllocationType DMAllocation
        {
            set { SetDmAllocation(value); }
        }

        /// <summary>Sets the n allocation.</summary>
        /// <value>The n allocation.</value>
        public BiomassAllocationType NAllocation
        {
            set { SetNAllocation(value); }
        }

        /// <summary>Sets the dm potential allocation.</summary>
        /// <value>The dm potential allocation.</value>
        /// <exception cref="System.Exception">
        /// -ve Potential DM Allocation to Leaf Cohort
        /// or
        /// Potential DM Allocation to Leaf Cohortis in excess of its Demand
        /// or
        /// -ve Potential DM Allocation to Leaf Cohort
        /// or
        /// Potential DM Allocation to Leaf Cohortis in excess of its Demand
        /// </exception>
        public BiomassPoolType DMPotentialAllocation
        {
            set
            {
                if (value.Structural < -0.0000000001)
                    throw new Exception("-ve Potential DM Allocation to Leaf Cohort");
                if (value.Structural - StructuralDMDemand > 0.0000000001)
                    throw new Exception("Potential DM Allocation to Leaf Cohortis in excess of its Demand");
                if (value.Metabolic < -0.0000000001)
                    throw new Exception("-ve Potential DM Allocation to Leaf Cohort");
                if (value.Metabolic - MetabolicDMDemand > 0.0000000001)
                    throw new Exception("Potential DM Allocation to Leaf Cohortis in excess of its Demand");

                if (StructuralDMDemand > 0)
                    potentialStructuralDmAllocation = value.Structural;
                if (MetabolicDMDemand > 0)
                    potentialMetabolicDmAllocation = value.Metabolic;
            }
        }
        #endregion

        #region Functions
        /// <summary>Constructor</summary>
        public LeafCohort()
        {
            Detached = new Biomass();
            Removed = new Biomass();
        }
        
        /// <summary>Returns a clone of this object</summary>
        /// <returns></returns>
        public virtual LeafCohort Clone()
        {
            LeafCohort newLeaf = (LeafCohort) MemberwiseClone();
            newLeaf.Live = new Biomass();
            newLeaf.Dead = new Biomass();
            newLeaf.Detached = new Biomass();
            newLeaf.Removed = new Biomass();
            return newLeaf;
        }

        /// <summary>Does the initialisation.</summary>
        public void DoInitialisation()
        {
            IsInitialised = true;
            Age = 0;
        }

        /// <summary>Does the appearance.</summary>
        /// <param name="leafFraction">The leaf fraction.</param>
        /// <param name="leafCohortParameterseafCohortParameters">The leaf cohort parameters.</param>
        public void DoAppearance(double leafFraction, Leaf.LeafCohortParameters leafCohortParameterseafCohortParameters)
        {
            Name = "Leaf" + Rank;
            IsAppeared = true;
            if (CohortPopulation <= 0)
                CohortPopulation = Structure.TotalStemPopn;
            MaxArea = leafCohortParameterseafCohortParameters.MaxArea.Value*CellDivisionStressFactor*leafFraction;
            //Reduce potential leaf area due to the effects of stress prior to appearance on cell number 
            GrowthDuration = leafCohortParameterseafCohortParameters.GrowthDuration.Value*leafFraction;
            LagDuration = leafCohortParameterseafCohortParameters.LagDuration.Value;
            SenescenceDuration = leafCohortParameterseafCohortParameters.SenescenceDuration.Value;
            DetachmentLagDuration = leafCohortParameterseafCohortParameters.DetachmentLagDuration.Value;
            DetachmentDuration = leafCohortParameterseafCohortParameters.DetachmentDuration.Value;
            StructuralFraction = leafCohortParameterseafCohortParameters.StructuralFraction.Value;
            SpecificLeafAreaMax = leafCohortParameterseafCohortParameters.SpecificLeafAreaMax.Value;
            SpecificLeafAreaMin = leafCohortParameterseafCohortParameters.SpecificLeafAreaMin.Value;
        //    MaximumNConc = leafCohortParameterseafCohortParameters.MaximumNConc.Value;
            MinimumNConc = leafCohortParameterseafCohortParameters.MinimumNConc.Value;
            NonStructuralFraction = leafCohortParameterseafCohortParameters.NonStructuralFraction.Value;
            InitialNConc = leafCohortParameterseafCohortParameters.InitialNConc.Value;
            if (Area > 0) //Only set age for cohorts that have an area specified in the xml.
                Age = Area/MaxArea*GrowthDuration;
                    //FIXME.  The size function is not linear so this does not give an exact starting age.  Should re-arange the the size function to return age for a given area to initialise age on appearance.
            LiveArea = Area*CohortPopulation;
            Live.StructuralWt = LiveArea/((SpecificLeafAreaMax + SpecificLeafAreaMin)/2)*StructuralFraction;
            Live.StructuralN = Live.StructuralWt*InitialNConc;
            functionalNConc = (leafCohortParameterseafCohortParameters.CriticalNConc.Value -
                               leafCohortParameterseafCohortParameters.MinimumNConc.Value*StructuralFraction)*
                              (1/(1 - StructuralFraction));
            luxaryNConc = leafCohortParameterseafCohortParameters.MaximumNConc.Value -
                           leafCohortParameterseafCohortParameters.CriticalNConc.Value;
            Live.MetabolicWt = Live.StructuralWt*1/StructuralFraction - Live.StructuralWt;
            Live.NonStructuralWt = 0;
            Live.StructuralN = Live.StructuralWt*MinimumNConc;
            Live.MetabolicN = Live.MetabolicWt*functionalNConc;
            Live.NonStructuralN = 0;
            nReallocationFactor = leafCohortParameterseafCohortParameters.NReallocationFactor.Value;
            dmReallocationFactor = leafCohortParameterseafCohortParameters.DMReallocationFactor.Value;
            nRetranslocationFactor = leafCohortParameterseafCohortParameters.NRetranslocationFactor.Value;
            dmRetranslocationFactor = leafCohortParameterseafCohortParameters.DMRetranslocationFactor.Value;
            LeafSizeShape = leafCohortParameterseafCohortParameters.LeafSizeShapeParameter.Value;
        }

        /// <summary>Does the potential growth.</summary>
        /// <param name="tt">The tt.</param>
        /// <param name="leafCohortParameters">The leaf cohort parameters.</param>
        public void DoPotentialGrowth(double tt, Leaf.LeafCohortParameters leafCohortParameters)
        {
            //Reduce leaf Population in Cohort due to plant mortality
            double startPopulation = CohortPopulation;
            if (Structure.ProportionPlantMortality > 0)
                CohortPopulation -= CohortPopulation*Structure.ProportionPlantMortality;

            //Reduce leaf Population in Cohort  due to branch mortality
            if ((Structure.ProportionBranchMortality > 0) && (CohortPopulation > Structure.MainStemPopn))
                //Ensure we there are some branches.
                CohortPopulation -= CohortPopulation*Structure.ProportionBranchMortality;

            double propnStemMortality = (startPopulation - CohortPopulation)/startPopulation;

            //Calculate Accumulated Stress Factor for reducing potential leaf size
            if (IsNotAppeared)
            {
                CellDivisionStressDays += 1;
                CellDivisionStressAccumulation += leafCohortParameters.CellDivisionStress.Value;
                CellDivisionStressFactor = Math.Max(CellDivisionStressAccumulation / CellDivisionStressDays, 0.01);
            }

            if (IsAppeared)
            {
                //Accelerate thermal time accumulation if crop is water stressed.
                double thermalTime;
                if (IsFullyExpanded)
                    thermalTime = tt*leafCohortParameters.DroughtInducedSenAcceleration.Value;
                else thermalTime = tt;

                //Leaf area growth parameters
                deltaPotentialArea = PotentialAreaGrowthFunction(thermalTime);
                    //Calculate delta leaf area in the absence of water stress
                DeltaWaterConstrainedArea = deltaPotentialArea*leafCohortParameters.ExpansionStress.Value;
                    //Reduce potential growth for water stress

                CoverAbove = Leaf.CoverAboveCohort(Rank); // Calculate cover above leaf cohort (unit??? FIXME-EIT)
                if (leafCohortParameters.ShadeInducedSenescenceRate != null)
                    shadeInducedSenRate = leafCohortParameters.ShadeInducedSenescenceRate.Value;
                SenessingLeafRelativeSize = leafCohortParameters.SenessingLeafRelativeSize.Value;
                senescedFrac = FractionSenescing(thermalTime, propnStemMortality, SenessingLeafRelativeSize);

                // Doing leaf mass growth in the cohort
                Biomass liveBiomass = new Biomass(Live);

                //Set initial leaf status values
                LeafStartArea = LiveArea;
                liveStart = new Biomass(Live);

                //If the model allows reallocation of senescent DM do it.
                if ((dmReallocationFactor > 0) && (senescedFrac > 0))
                {
                    // DM to reallocate.
                    LeafStartMetabolicDmReallocationSupply = liveStart.MetabolicWt*senescedFrac*dmReallocationFactor;
                    LeafStartNonStructuralDmReallocationSupply = liveStart.NonStructuralWt*senescedFrac*
                                                                 dmReallocationFactor;
                    LeafStartDmReallocationSupply = LeafStartMetabolicDmReallocationSupply +
                                                    LeafStartNonStructuralDmReallocationSupply;
                    liveBiomass.MetabolicWt -= LeafStartMetabolicDmReallocationSupply;
                    liveBiomass.NonStructuralWt -= LeafStartNonStructuralDmReallocationSupply;

                }
                else
                    LeafStartMetabolicDmReallocationSupply =
                        LeafStartNonStructuralDmReallocationSupply = LeafStartDmReallocationSupply = 0;

                LeafStartDmRetranslocationSupply = liveBiomass.NonStructuralWt*dmRetranslocationFactor;
                //Nretranslocation is that which occurs before uptake (senessed metabolic N and all non-structuralN)
                LeafStartMetabolicNReallocationSupply = senescedFrac*liveBiomass.MetabolicN*nReallocationFactor;
                LeafStartNonStructuralNReallocationSupply = senescedFrac*liveBiomass.NonStructuralN*nReallocationFactor;
                //Retranslocated N is only that which occurs after N uptake. Both Non-structural and metabolic N are able to be retranslocated but metabolic N will only be moved if remobilisation of non-structural N does not meet demands
                LeafStartMetabolicNRetranslocationSupply = Math.Max(0.0,
                    liveBiomass.MetabolicN*nRetranslocationFactor - LeafStartMetabolicNReallocationSupply);
                LeafStartNonStructuralNRetranslocationSupply = Math.Max(0.0,
                    liveBiomass.NonStructuralN*nRetranslocationFactor - LeafStartNonStructuralNReallocationSupply);
                LeafStartNReallocationSupply = LeafStartNonStructuralNReallocationSupply + LeafStartMetabolicNReallocationSupply;
                LeafStartNRetranslocationSupply = LeafStartNonStructuralNRetranslocationSupply + LeafStartMetabolicNRetranslocationSupply;

                //zero locals variables
                potentialStructuralDmAllocation = 0;
                potentialMetabolicDmAllocation = 0;
                dmRetranslocated = 0;
                metabolicNReallocated = 0;
                nonStructuralNReallocated = 0;
                metabolicWtReallocated = 0;
                nonStructuralWtReallocated = 0;
                metabolicNRetranslocated = 0;
                nonStructuralNRetrasnlocated = 0;
                metabolicNAllocation = 0;
                structuralDmAllocation = 0;
                metabolicDmAllocation = 0;
            }
        }

        /// <summary>Does the actual growth.</summary>
        /// <param name="tt">The tt.</param>
        /// <param name="leafCohortParameters">The leaf cohort parameters.</param>
        public void DoActualGrowth(double tt, Leaf.LeafCohortParameters leafCohortParameters)
        {
            if (!IsAppeared)
                return;

            //Acellerate thermal time accumulation if crop is water stressed.
            double thermalTime;
            if ((leafCohortParameters.DroughtInducedSenAcceleration != null) && IsFullyExpanded)
                thermalTime = tt*leafCohortParameters.DroughtInducedSenAcceleration.Value;
            else thermalTime = tt;

            //Growing leaf area after DM allocated
            DeltaCarbonConstrainedArea = (structuralDmAllocation + metabolicDmAllocation)*SpecificLeafAreaMax;
            //Fixme.  Live.Nonstructural should probably be included in DM supply for leaf growth also
            double deltaActualArea = Math.Min(DeltaWaterConstrainedArea, DeltaCarbonConstrainedArea);
            LiveArea += deltaActualArea;
            // Integrates leaf area at each cohort? FIXME-EIT is this the one integrated at leaf.cs?

            //Senessing leaf area
            double areaSenescing = LiveArea*senescedFrac;
            double areaSenescingN = 0;
            if ((Live.MetabolicNConc <= MinimumNConc) & (metabolicNRetranslocated - metabolicNAllocation > 0.0))
                areaSenescingN = LeafStartArea*(metabolicNRetranslocated - metabolicNAllocation)/liveStart.MetabolicN;

            double leafAreaLoss = Math.Max(areaSenescing, areaSenescingN);
            if (leafAreaLoss > 0)
                senescedFrac = Math.Min(1.0, leafAreaLoss/LeafStartArea);

            double structuralWtSenescing = senescedFrac*liveStart.StructuralWt;
            double structuralNSenescing = senescedFrac*liveStart.StructuralN;
            double metabolicWtSenescing = senescedFrac*liveStart.MetabolicWt;
            double metabolicNSenescing = senescedFrac*liveStart.MetabolicN;
            double nonStructuralWtSenescing = senescedFrac*liveStart.NonStructuralWt;
            double nonStructuralNSenescing = senescedFrac*liveStart.NonStructuralN;

            DeadArea = DeadArea + leafAreaLoss;
            LiveArea = LiveArea - leafAreaLoss;
            // Final leaf area of cohort that will be integrated in Leaf.cs? (FIXME-EIT)

            Live.StructuralWt -= structuralWtSenescing;
            Dead.StructuralWt += structuralWtSenescing;

            Live.StructuralN -= structuralNSenescing;
            Dead.StructuralN += structuralNSenescing;

            Live.MetabolicWt -= Math.Max(0.0, metabolicWtSenescing - metabolicWtReallocated);
            Dead.MetabolicWt += Math.Max(0.0, metabolicWtSenescing - metabolicWtReallocated);


            Live.MetabolicN -= Math.Max(0.0, metabolicNSenescing - metabolicNReallocated - metabolicNRetranslocated);
            //Don't Seness todays N if it has been taken for reallocation
            Dead.MetabolicN += Math.Max(0.0, metabolicNSenescing - metabolicNReallocated - metabolicNRetranslocated);

            Live.NonStructuralN -= Math.Max(0.0,
                nonStructuralNSenescing - nonStructuralNReallocated - nonStructuralNRetrasnlocated);
            //Dont Senesess todays NonStructural N if it was retranslocated or reallocated 
            Dead.NonStructuralN += Math.Max(0.0,
                nonStructuralNSenescing - nonStructuralNReallocated - nonStructuralNRetrasnlocated);

            Live.NonStructuralWt -= Math.Max(0.0, nonStructuralWtSenescing - dmRetranslocated);
            Live.NonStructuralWt = Math.Max(0.0, Live.NonStructuralWt);

            Dead.NonStructuralWt += Math.Max(0.0,
                nonStructuralWtSenescing - dmRetranslocated - nonStructuralWtReallocated);

            MaintenanceRespiration = 0;
            //Do Maintenance respiration
            MaintenanceRespiration += Live.MetabolicWt*leafCohortParameters.MaintenanceRespirationFunction.Value;
            Live.MetabolicWt *= (1 - leafCohortParameters.MaintenanceRespirationFunction.Value);
            MaintenanceRespiration += Live.NonStructuralWt*leafCohortParameters.MaintenanceRespirationFunction.Value;
            Live.NonStructuralWt *= (1 - leafCohortParameters.MaintenanceRespirationFunction.Value);

            Age = Age + thermalTime;

            // Do Detachment of this Leaf Cohort
            // ---------------------------------
            detachedFrac = FractionDetaching(thermalTime);
            if (detachedFrac > 0.0)
            {
                double detachedWt = Dead.Wt*detachedFrac;
                double detachedN = Dead.N*detachedFrac;

                DeadArea *= 1 - detachedFrac;
                Dead.StructuralWt *= 1 - detachedFrac;
                Dead.StructuralN *= 1 - detachedFrac;
                Dead.NonStructuralWt *= 1 - detachedFrac;
                Dead.NonStructuralN *= 1 - detachedFrac;
                Dead.MetabolicWt *= 1 - detachedFrac;
                Dead.MetabolicN *= 1 - detachedFrac;

                if (detachedWt > 0)
                    SurfaceOrganicMatter.Add(detachedWt*10, detachedN*10, 0, Plant.CropType, "Leaf");
            }
        }

        /// <summary>Does the kill.</summary>
        /// <param name="fraction">The fraction.</param>
        public void DoKill(double fraction)
        {
            if (!IsInitialised)
                return;

            double change = LiveArea*fraction;
            LiveArea -= change;
            DeadArea += change;

            change = Live.StructuralWt*fraction;
            Live.StructuralWt -= change;
            Dead.StructuralWt += change;

            change = Live.NonStructuralWt*fraction;
            Live.NonStructuralWt -= change;
            Dead.NonStructuralWt += change;

            change = Live.StructuralN*fraction;
            Live.StructuralN -= change;
            Dead.StructuralN += change;

            change = Live.NonStructuralN*fraction;
            Live.NonStructuralN -= change;
            Dead.NonStructuralN += change;
        }

        /// <summary>Does the frost.</summary>
        /// <param name="fraction">The fraction.</param>
        public void DoFrost(double fraction)
        {
            if (IsAppeared)
                DoKill(fraction);
        }

        /// <summary>Does the zeroing of some varibles.</summary>
        protected void DoDailyCleanup()
        {
            Detached.Clear();
            Removed.Clear();
        }

        /// <summary>Potential delta LAI</summary>
        /// <param name="tt">thermal-time</param>
        /// <returns>(mm2 leaf/cohort position/m2 soil/day)</returns>
        public double PotentialAreaGrowthFunction(double tt)
        {
            double leafSizeDelta = SizeFunction(Age + tt) - SizeFunction(Age);
                //mm2 of leaf expanded in one day at this cohort (Today's minus yesterday's Area/cohort)
            double growth = CohortPopulation*leafSizeDelta;
                // Daily increase in leaf area for that cohort position in a per m2 basis (mm2/m2/day)
            if (growth < 0)
                throw new Exception("Netagive potential leaf area expansion in" + this);
            return growth;
        }

        /// <summary>Potential average leaf size for today per cohort (no stress)</summary>
        /// <param name="tt">Thermal-time accumulation since cohort initiation</param>
        /// <returns>Average leaf size (mm2/leaf)</returns>
        protected double SizeFunction(double tt)
        {
            if (GrowthDuration <= 0)
                throw new Exception(
                    "Trying to calculate leaf size with a growth duration parameter value of zero won't work");
            double oneLessShape = 1 - LeafSizeShape;
            double alpha = -Math.Log((1/oneLessShape - 1)/(MaxArea/(MaxArea*LeafSizeShape) - 1))/GrowthDuration;
            double leafSize = MaxArea/(1 + (MaxArea/(MaxArea*LeafSizeShape) - 1)*Math.Exp(-alpha*tt));
            double y0 = MaxArea/(1 + (MaxArea/(MaxArea*LeafSizeShape) - 1)*Math.Exp(-alpha*0));
            double yDiffprop = y0/(MaxArea/2);
            double scaledLeafSize = (leafSize - y0)/(1 - yDiffprop);
            return scaledLeafSize;
        }

        /// <summary>Fractions the senescing.</summary>
        /// <param name="tt">The tt.</param>
        /// <param name="stemMortality">The stem mortality.</param>
        /// <param name="senessingLeafRelativeSize">The relative size of senessing tillers leaves relative to the other leaves in the cohort</param>
        /// <returns></returns>
        /// <exception cref="System.Exception">Bad Fraction Senescing</exception>
        public double FractionSenescing(double tt, double stemMortality, double senessingLeafRelativeSize)
        {
            //Calculate fraction of leaf area senessing based on age and shading.  This is used to to calculate change in leaf area and Nreallocation supply.
            if (!IsAppeared)
                return 0;

            double fracSenAge;
            double ttInSenPhase = Math.Max(0.0, Age + tt - LagDuration - GrowthDuration);
            if (ttInSenPhase > 0)
            {
                double leafDuration = GrowthDuration + LagDuration + SenescenceDuration;
                double remainingTt = Math.Max(0, leafDuration - Age);

                if (remainingTt <= 0)
                    fracSenAge = 1;
                else
                    fracSenAge = Math.Min(1, Math.Min(tt, ttInSenPhase)/remainingTt);
                if (fracSenAge > 1 || fracSenAge < 0)
                    throw new Exception("Bad Fraction Senescing");
            }
            else
            {
                fracSenAge = 0;
            }

            if (maxLiveArea < LiveArea)
                maxLiveArea = LiveArea;
            if (maxCohortPopulation < CohortPopulation)
                maxCohortPopulation = CohortPopulation;

            double fracSenShade = 0;
            if (LiveArea > 0)
            {
                fracSenShade = Math.Min(maxLiveArea*shadeInducedSenRate, LiveArea)/LiveArea;
                fracSenShade += stemMortality*senessingLeafRelativeSize;
                fracSenShade = Math.Min(fracSenShade, 1.0);
            }

            return Math.Max(fracSenAge, fracSenShade);
        }

        /// <summary>Fractions the detaching.</summary>
        /// <param name="tt">The thermal time.</param>
        /// <returns></returns>
        /// <exception cref="System.Exception">Bad Fraction Detaching</exception>
        public double FractionDetaching(double tt)
        {
            double fracDetach;
            double ttInDetachPhase = Math.Max(0.0,Age + tt - LagDuration - GrowthDuration - SenescenceDuration - DetachmentLagDuration);
            if (ttInDetachPhase > 0)
            {
                double leafDuration = GrowthDuration + LagDuration + SenescenceDuration + DetachmentLagDuration + DetachmentDuration;
                double remainingTt = Math.Max(0, leafDuration - Age);

                if (remainingTt <= 0)
                    fracDetach = 1;
                else
                    fracDetach = Math.Min(1, Math.Min(tt, ttInDetachPhase)/remainingTt);
                if ((fracDetach > 1) || (fracDetach < 0))
                    throw new Exception("Bad Fraction Detaching");
            }
            else
                fracDetach = 0;

            return fracDetach;
        }

        /// <summary>Sets the dm allocation.</summary>
        /// <value>The dm allocation.</value>
        /// <exception cref="System.Exception">
        /// -ve DM Allocation to Leaf Cohort
        /// or
        /// DM Allocated to Leaf Cohort is in excess of its Demand
        /// or
        /// A leaf cohort cannot supply that amount for DM Reallocation
        /// or
        /// Leaf cohort given negative DM Reallocation
        /// or
        /// Negative DM retranslocation from a Leaf Cohort
        /// or
        /// A leaf cohort cannot supply that amount for DM retranslocation
        /// </exception>
        private void SetDmAllocation(BiomassAllocationType value)
        {
            //Firstly allocate DM
            if (value.Structural + value.NonStructural + value.Metabolic < -0.0000000001)
                throw new Exception("-ve DM Allocation to Leaf Cohort");
            if (value.Structural + value.NonStructural + value.Metabolic - (StructuralDMDemand + MetabolicDMDemand + NonStructuralDMDemand) > 0.0000000001)
                throw new Exception("DM Allocated to Leaf Cohort is in excess of its Demand");
            if (StructuralDMDemand + MetabolicDMDemand + NonStructuralDMDemand > 0)
            {
                structuralDmAllocation = value.Structural;
                metabolicDmAllocation = value.Metabolic;
                Live.StructuralWt += value.Structural;
                Live.MetabolicWt += value.Metabolic;
                Live.NonStructuralWt += value.NonStructural;
            }

            //Then remove reallocated DM
            if (value.Reallocation -
                (LeafStartMetabolicDmReallocationSupply + LeafStartNonStructuralDmReallocationSupply) >
                0.00000000001)
                throw new Exception("A leaf cohort cannot supply that amount for DM Reallocation");
            if (value.Reallocation < -0.0000000001)
                throw new Exception("Leaf cohort given negative DM Reallocation");
            if (value.Reallocation > 0.0)
            {
                nonStructuralWtReallocated = Math.Min(LeafStartNonStructuralDmReallocationSupply, value.Reallocation);
                //Reallocate nonstructural first
                metabolicWtReallocated = Math.Max(0.0,
                        Math.Min(value.Reallocation - nonStructuralWtReallocated, LeafStartMetabolicDmReallocationSupply));
                //Then reallocate metabolic DM
                Live.NonStructuralWt -= nonStructuralWtReallocated;
                Live.MetabolicWt -= metabolicWtReallocated;
            }

            //Then remove retranslocated DM
            if (value.Retranslocation < -0.0000000001)
                throw new Exception("Negative DM retranslocation from a Leaf Cohort");
            if (value.Retranslocation > LeafStartDmRetranslocationSupply)
                throw new Exception("A leaf cohort cannot supply that amount for DM retranslocation");
            if ((value.Retranslocation > 0) && (LeafStartDmRetranslocationSupply > 0))
                Live.NonStructuralWt -= value.Retranslocation;
        }

        /// <summary>Sets the n allocation.</summary>
        /// <value>The n allocation.</value>
        /// <exception cref="System.Exception">
        /// A leaf cohort cannot supply that amount for N Reallocation
        /// or
        /// Leaf cohort given negative N Reallocation
        /// or
        /// A leaf cohort cannot supply that amount for N Retranslocation
        /// or
        /// Leaf cohort given negative N Retranslocation
        /// </exception>
        private void SetNAllocation(BiomassAllocationType value)
        {
            //Fresh allocations
            Live.StructuralN += value.Structural;
            Live.MetabolicN += value.Metabolic;
            Live.NonStructuralN += value.NonStructural;
            //Reallocation
            if (value.Reallocation -
                (LeafStartMetabolicNReallocationSupply + LeafStartNonStructuralNReallocationSupply) > 0.00000000001)
                throw new Exception("A leaf cohort cannot supply that amount for N Reallocation");
            if (value.Reallocation < -0.0000000001)
                throw new Exception("Leaf cohort given negative N Reallocation");
            if (value.Reallocation > 0.0)
            {
                nonStructuralNReallocated = Math.Min(LeafStartNonStructuralNReallocationSupply, value.Reallocation);
                //Reallocate nonstructural first
                metabolicNReallocated = Math.Max(0.0, value.Reallocation - LeafStartNonStructuralNReallocationSupply);
                //Then reallocate metabolic N
                Live.NonStructuralN -= nonStructuralNReallocated;
                Live.MetabolicN -= metabolicNReallocated;
            }
            //Retranslocation
            if (value.Retranslocation -
                (LeafStartMetabolicNRetranslocationSupply + LeafStartNonStructuralNRetranslocationSupply) >
                0.00000000001)
                throw new Exception("A leaf cohort cannot supply that amount for N Retranslocation");
            if (value.Retranslocation < -0.0000000001)
                throw new Exception("Leaf cohort given negative N Retranslocation");
            if (value.Retranslocation > 0.0)
            {
                nonStructuralNRetrasnlocated = Math.Min(LeafStartNonStructuralNRetranslocationSupply,
                    value.Retranslocation); //Reallocate nonstructural first
                metabolicNRetranslocated = Math.Max(0.0,
                        value.Retranslocation - LeafStartNonStructuralNRetranslocationSupply);
                //Then reallocate metabolic N
                Live.NonStructuralN -= nonStructuralNRetrasnlocated;
                Live.MetabolicN -= metabolicNRetranslocated;
            }
        }


        /// <summary>Called when [do daily initialisation].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoDailyInitialisation")]
        private void OnDoDailyInitialisation(object sender, EventArgs e)
        {
            if (Plant.IsAlive)
                DoDailyCleanup();
        }
        #endregion

        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        /// <param name="tags">The list of tags to add to.</param>
        /// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
        /// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
        public override void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
        {
            // write memos.
            foreach (IModel memo in Apsim.Children(this, typeof(Memo)))
                memo.Document(tags, -1, indent);

            tags.Add(new AutoDocumentation.Paragraph("Area = " + Area, indent));
        }
    }
}
