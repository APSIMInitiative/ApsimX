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
        private Biomass LiveStart = null;

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
        public double Age = 0;

        /// <summary>The n reallocation factor</summary>
        private double NReallocationFactor = 0;

        /// <summary>The dm reallocation factor</summary>
        private double DMReallocationFactor = 0;

        /// <summary>The n retranslocation factor</summary>
        private double NRetranslocationFactor = 0;

        /// <summary>The dm retranslocation factor</summary>
        private double DMRetranslocationFactor = 0;

        /// <summary>The functional n conc</summary>
        private double FunctionalNConc = 0;

        /// <summary>The luxary n conc</summary>
        private double LuxaryNConc = 0;

        /// <summary>The structural fraction</summary>
        [XmlIgnore]
        public double StructuralFraction = 0;

        /// <summary>The non structural fraction</summary>
        [XmlIgnore]
        public double NonStructuralFraction = 0;

        /// <summary>The maximum live area</summary>
        [XmlIgnore]
        public double MaxLiveArea = 0;

        /// <summary>The maximum live area</summary>
        [XmlIgnore]
        public double MaxCohortPopulation = 0;

        /// <summary>The growth duration</summary>
        [XmlIgnore]
        public double GrowthDuration = 0;

        /// <summary>The lag duration</summary>
        [XmlIgnore]
        public double LagDuration = 0;

        /// <summary>The senescence duration</summary>
        [XmlIgnore]
        public double SenescenceDuration = 0;

        /// <summary>The detachment lag duration</summary>
        [XmlIgnore]
        public double DetachmentLagDuration = 0;

        /// <summary>The detachment duration</summary>
        [XmlIgnore]
        public double DetachmentDuration = 0;

        /// <summary>The specific leaf area maximum</summary>
        [XmlIgnore]
        public double SpecificLeafAreaMax = 0;

        /// <summary>The specific leaf area minimum</summary>
        [XmlIgnore]
        public double SpecificLeafAreaMin = 0;

        /// <summary>The maximum n conc</summary>
        [XmlIgnore]
        public double MaximumNConc = 0;

        /// <summary>The minimum n conc</summary>
        [XmlIgnore]
        public double MinimumNConc = 0;

        /// <summary>The initial n conc</summary>
        [XmlIgnore]
        public double InitialNConc = 0;

        /// <summary>The live area</summary>
        [XmlIgnore]
        public double LiveArea = 0;

        /// <summary>The dead area</summary>
        [XmlIgnore]
        public double DeadArea = 0;

        /// <summary>The maximum area</summary>
        [XmlIgnore]
        public double MaxArea = 0;

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
        private double ShadeInducedSenRate = 0;

        /// <summary>The senesced frac</summary>
        private double SenescedFrac = 0;

        /// <summary>The detached frac</summary>
        private double DetachedFrac = 0;

        /// <summary>The cohort population</summary>
        [XmlIgnore]
        public double CohortPopulation = 0; //Number of leaves in this cohort

        /// <summary>The cell division stress factor</summary>
        [XmlIgnore]
        public double CellDivisionStressFactor = 1;

        /// <summary>The cell division stress accumulation</summary>
        [XmlIgnore]
        public double CellDivisionStressAccumulation = 0;

        /// <summary>The cell division stress days</summary>
        [XmlIgnore]
        public double CellDivisionStressDays = 0;

        //Leaf Initial status paramaters
        /// <summary>The leaf start n retranslocation supply</summary>
        [XmlIgnore]
        public double LeafStartNRetranslocationSupply = 0;

        /// <summary>The leaf start n reallocation supply</summary>
        [XmlIgnore]
        public double LeafStartNReallocationSupply = 0;

        /// <summary>The leaf start dm retranslocation supply</summary>
        [XmlIgnore]
        public double LeafStartDMRetranslocationSupply = 0;

        /// <summary>The leaf start dm reallocation supply</summary>
        [XmlIgnore]
        public double LeafStartDMReallocationSupply = 0;

        /// <summary>The leaf start area</summary>
        [XmlIgnore]
        public double LeafStartArea = 0;

        /// <summary>
        /// The leaf start metabolic n reallocation supply
        /// </summary>
        [XmlIgnore]
        public double LeafStartMetabolicNReallocationSupply = 0;

        /// <summary>
        /// The leaf start non structural n reallocation supply
        /// </summary>
        [XmlIgnore]
        public double LeafStartNonStructuralNReallocationSupply = 0;

        /// <summary>
        /// The leaf start metabolic n retranslocation supply
        /// </summary>
        [XmlIgnore]
        public double LeafStartMetabolicNRetranslocationSupply = 0;

        /// <summary>
        /// The leaf start non structural n retranslocation supply
        /// </summary>
        [XmlIgnore]
        public double LeafStartNonStructuralNRetranslocationSupply = 0;

        /// <summary>
        /// The leaf start metabolic dm reallocation supply
        /// </summary>
        [XmlIgnore]
        public double LeafStartMetabolicDMReallocationSupply = 0;

        /// <summary>
        /// The leaf start non structural dm reallocation supply
        /// </summary>
        [XmlIgnore]
        public double LeafStartNonStructuralDMReallocationSupply = 0;

        //variables used in calculating daily supplies and deltas
        /// <summary>Gets the DM amount detached (send to surface OM) (g/m2)</summary>
        [XmlIgnore]
        public Biomass Detached { get; set; }

        /// <summary>Gets the DM amount removed from the system (harvested, grazed, etc) (g/m2)</summary>
        [XmlIgnore]
        public Biomass Removed { get; set; }

        /// <summary>The delta potential area</summary>
        private double DeltaPotentialArea = 0;

        /// <summary>The delta water constrained area</summary>
        public double DeltaWaterConstrainedArea = 0;

        /// <summary>The delta carbon constrained area</summary>
        public double DeltaCarbonConstrainedArea = 0;

        //private double StructuralDMDemand = 0;
        //private double MetabolicDMDemand = 0;
        /// <summary>The potential structural dm allocation</summary>
        private double PotentialStructuralDMAllocation = 0;

        /// <summary>The potential metabolic dm allocation</summary>
        private double PotentialMetabolicDMAllocation = 0;

        /// <summary>The metabolic n reallocated</summary>
        private double MetabolicNReallocated = 0;

        /// <summary>The metabolic wt reallocated</summary>
        private double MetabolicWtReallocated = 0;

        /// <summary>The non structural n reallocated</summary>
        private double NonStructuralNReallocated = 0;

        /// <summary>The non structural wt reallocated</summary>
        private double NonStructuralWtReallocated = 0;

        /// <summary>The metabolic n retranslocated</summary>
        private double MetabolicNRetranslocated = 0;

        /// <summary>The non structural n retrasnlocated</summary>
        private double NonStructuralNRetrasnlocated = 0;

        /// <summary>The dm retranslocated</summary>
        private double DMRetranslocated = 0;

        /// <summary>The metabolic n allocation</summary>
        private double MetabolicNAllocation = 0;

        /// <summary>The structural dm allocation</summary>
        private double StructuralDMAllocation = 0;

        /// <summary>The metabolic dm allocation</summary>
        private double MetabolicDMAllocation = 0;

        #endregion

        #region Class Properties

        /// <summary>Has the leaf chort been initialised?</summary>
        [XmlIgnore]
        public bool IsInitialised = false;

        /// <summary>Gets a value indicating whether this instance has not appeared.</summary>
        /// <value>
        /// <c>true</c> if this instance is not appeared; otherwise, <c>false</c>.
        /// </value>
        public bool IsNotAppeared
        {
            get { return IsInitialised && Age == 0; }
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
                if (MaxLiveArea == 0)
                    return 0;
                return MaxLiveArea/MaxCohortPopulation;
            }
        }

        /// <summary>Gets the size.</summary>
        /// <value>The size.</value>
        public double Size
        {
            get
            {
                if (IsAppeared)
                    return LiveArea/CohortPopulation;
                return 0;
            }
        }

        /// <summary>Gets the fraction expanded.</summary>
        /// <value>The fraction expanded.</value>
        public double FractionExpanded
        {
            get
            {
                if (Age == 0)
                    return 0;
                if (Age >= GrowthDuration)
                    return 1;
                return Age/GrowthDuration;
            }
        }

        /// <summary>Gets the specific area.</summary>
        /// <value>The specific area.</value>
        public double SpecificArea
        {
            get
            {
                if (Live.Wt > 0)
                    return LiveArea/Live.Wt;
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
                    double TotalDMDemand = Math.Min(DeltaPotentialArea/((SpecificLeafAreaMax + SpecificLeafAreaMin)/2),
                        DeltaWaterConstrainedArea/SpecificLeafAreaMin);
                    if (TotalDMDemand < 0)
                        throw new Exception("Negative DMDemand in" + this);
                    return TotalDMDemand*StructuralFraction;

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
                    double TotalDMDemand = Math.Min(DeltaPotentialArea/((SpecificLeafAreaMax + SpecificLeafAreaMin)/2),
                        DeltaWaterConstrainedArea/SpecificLeafAreaMin);
                    return TotalDMDemand*(1 - StructuralFraction);
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
                    double MaxNonStructuralDM = (MetabolicDMDemand + StructuralDMDemand + LiveStart.MetabolicWt +
                                                 LiveStart.StructuralWt)*NonStructuralFraction;
                    return Math.Max(0.0, MaxNonStructuralDM - LiveStart.NonStructuralWt);
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
                if ((IsNotSenescing) && (ShadeInducedSenRate == 0.0))
                    // Assuming a leaf will have no demand if it is senescing and will have no demand if it is is shaded conditions
                    return MinimumNConc*PotentialStructuralDMAllocation;
                return 0.0;
            }
        }

        /// <summary>Gets the non structural n demand.</summary>
        /// <value>The non structural n demand.</value>
        public double NonStructuralNDemand
        {
            get
            {
                if (IsNotSenescing && (ShadeInducedSenRate == 0.0) && (NonStructuralFraction > 0))
                    // Assuming a leaf will have no demand if it is senescing and will have no demand if it is is shaded conditions.  Also if there is 
                    return Math.Max(0.0, LuxaryNConc*(LiveStart.StructuralWt + LiveStart.MetabolicWt
                                                      + PotentialStructuralDMAllocation + PotentialMetabolicDMAllocation) -
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
                if (IsNotSenescing && (ShadeInducedSenRate == 0.0))
                    // Assuming a leaf will have no demand if it is senescing and will have no demand if it is is shaded conditions
                    return FunctionalNConc*PotentialMetabolicDMAllocation;

                return 0.0;
            }
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
        public BiomassAllocationType DMAllocation
        {
            set
            {
                //Firstly allocate DM
                if (value.Structural + value.NonStructural + value.Metabolic < -0.0000000001)
                    throw new Exception("-ve DM Allocation to Leaf Cohort");
                if ((value.Structural + value.NonStructural + value.Metabolic - (StructuralDMDemand + MetabolicDMDemand + NonStructuralDMDemand)) > 0.0000000001)
                    throw new Exception("DM Allocated to Leaf Cohort is in excess of its Demand");
                if ((StructuralDMDemand + MetabolicDMDemand + NonStructuralDMDemand) > 0)
                {
                    StructuralDMAllocation = value.Structural;
                    MetabolicDMAllocation = value.Metabolic;
                    Live.StructuralWt += value.Structural;
                    Live.MetabolicWt += value.Metabolic;
                    Live.NonStructuralWt += value.NonStructural;
                }

                //Then remove reallocated DM
                if (value.Reallocation -
                    (LeafStartMetabolicDMReallocationSupply + LeafStartNonStructuralDMReallocationSupply) >
                    0.00000000001)
                    throw new Exception("A leaf cohort cannot supply that amount for DM Reallocation");
                if (value.Reallocation < -0.0000000001)
                    throw new Exception("Leaf cohort given negative DM Reallocation");
                if (value.Reallocation > 0.0)
                {
                    NonStructuralWtReallocated = Math.Min(LeafStartNonStructuralDMReallocationSupply, value.Reallocation);
                        //Reallocate nonstructural first
                    MetabolicWtReallocated = Math.Max(0.0,
                            Math.Min(value.Reallocation - NonStructuralWtReallocated, LeafStartMetabolicDMReallocationSupply));
                        //Then reallocate metabolic DM
                    Live.NonStructuralWt -= NonStructuralWtReallocated;
                    Live.MetabolicWt -= MetabolicWtReallocated;
                }

                //Then remove retranslocated DM
                if (value.Retranslocation < -0.0000000001)
                    throw new Exception("Negative DM retranslocation from a Leaf Cohort");
                if (value.Retranslocation > LeafStartDMRetranslocationSupply)
                    throw new Exception("A leaf cohort cannot supply that amount for DM retranslocation");
                if ((value.Retranslocation > 0) && (LeafStartDMRetranslocationSupply > 0))
                    Live.NonStructuralWt -= value.Retranslocation;
            }
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
        public BiomassAllocationType NAllocation
        {
            set
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
                    NonStructuralNReallocated = Math.Min(LeafStartNonStructuralNReallocationSupply, value.Reallocation);
                        //Reallocate nonstructural first
                    MetabolicNReallocated = Math.Max(0.0, value.Reallocation - LeafStartNonStructuralNReallocationSupply);
                        //Then reallocate metabolic N
                    Live.NonStructuralN -= NonStructuralNReallocated;
                    Live.MetabolicN -= MetabolicNReallocated;
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
                    NonStructuralNRetrasnlocated = Math.Min(LeafStartNonStructuralNRetranslocationSupply,
                        value.Retranslocation); //Reallocate nonstructural first
                    MetabolicNRetranslocated = Math.Max(0.0,
                            value.Retranslocation - LeafStartNonStructuralNRetranslocationSupply);
                        //Then reallocate metabolic N
                    Live.NonStructuralN -= NonStructuralNRetrasnlocated;
                    Live.MetabolicN -= MetabolicNRetranslocated;
                }
            }
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
                if ((value.Structural - StructuralDMDemand) > 0.0000000001)
                    throw new Exception("Potential DM Allocation to Leaf Cohortis in excess of its Demand");
                if (value.Metabolic < -0.0000000001)
                    throw new Exception("-ve Potential DM Allocation to Leaf Cohort");
                if ((value.Metabolic - MetabolicDMDemand) > 0.0000000001)
                    throw new Exception("Potential DM Allocation to Leaf Cohortis in excess of its Demand");

                if (StructuralDMDemand > 0)
                    PotentialStructuralDMAllocation = value.Structural;
                if (MetabolicDMDemand > 0)
                    PotentialMetabolicDMAllocation = value.Metabolic;
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
            Name = "Leaf" + Rank.ToString();
            IsAppeared = true;
            if (CohortPopulation == 0)
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
            MaximumNConc = leafCohortParameterseafCohortParameters.MaximumNConc.Value;
            MinimumNConc = leafCohortParameterseafCohortParameters.MinimumNConc.Value;
            NonStructuralFraction = leafCohortParameterseafCohortParameters.NonStructuralFraction.Value;
            InitialNConc = leafCohortParameterseafCohortParameters.InitialNConc.Value;
            if (Area > 0) //Only set age for cohorts that have an area specified in the xml.
                Age = Area/MaxArea*GrowthDuration;
                    //FIXME.  The size function is not linear so this does not give an exact starting age.  Should re-arange the the size function to return age for a given area to initialise age on appearance.
            LiveArea = Area*CohortPopulation;
            Live.StructuralWt = LiveArea/((SpecificLeafAreaMax + SpecificLeafAreaMin)/2)*StructuralFraction;
            Live.StructuralN = Live.StructuralWt*InitialNConc;
            FunctionalNConc = (leafCohortParameterseafCohortParameters.CriticalNConc.Value -
                               leafCohortParameterseafCohortParameters.MinimumNConc.Value*StructuralFraction)*
                              (1/(1 - StructuralFraction));
            LuxaryNConc = leafCohortParameterseafCohortParameters.MaximumNConc.Value -
                           leafCohortParameterseafCohortParameters.CriticalNConc.Value;
            Live.MetabolicWt = Live.StructuralWt*1/StructuralFraction - Live.StructuralWt;
            Live.NonStructuralWt = 0;
            Live.StructuralN = Live.StructuralWt*MinimumNConc;
            Live.MetabolicN = Live.MetabolicWt*FunctionalNConc;
            Live.NonStructuralN = 0;
            NReallocationFactor = leafCohortParameterseafCohortParameters.NReallocationFactor.Value;
            DMReallocationFactor = leafCohortParameterseafCohortParameters.DMReallocationFactor.Value;
            NRetranslocationFactor = leafCohortParameterseafCohortParameters.NRetranslocationFactor.Value;
            DMRetranslocationFactor = leafCohortParameterseafCohortParameters.DMRetranslocationFactor.Value;
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
            if (IsNotAppeared && (leafCohortParameters.CellDivisionStress != null))
            {
                CellDivisionStressDays += 1;
                CellDivisionStressAccumulation += leafCohortParameters.CellDivisionStress.Value;
                //FIXME HEB  The limitation below should be used to avoid zero values for maximum leaf size.
                //CellDivisionStressFactor = Math.Max(CellDivisionStressAccumulation / CellDivisionStressDays, 0.01);
                CellDivisionStressFactor = CellDivisionStressAccumulation/CellDivisionStressDays;
            }

            if (IsAppeared)
            {
                //Accelerate thermal time accumulation if crop is water stressed.
                double thermalTime;
                if ((leafCohortParameters.DroughtInducedSenAcceleration != null) && (IsFullyExpanded))
                    thermalTime = tt*leafCohortParameters.DroughtInducedSenAcceleration.Value;
                else thermalTime = tt;

                //Leaf area growth parameters
                DeltaPotentialArea = PotentialAreaGrowthFunction(thermalTime);
                    //Calculate delta leaf area in the absence of water stress
                DeltaWaterConstrainedArea = DeltaPotentialArea*leafCohortParameters.ExpansionStress.Value;
                    //Reduce potential growth for water stress

                CoverAbove = Leaf.CoverAboveCohort(Rank); // Calculate cover above leaf cohort (unit??? FIXME-EIT)
                if (leafCohortParameters.ShadeInducedSenescenceRate != null)
                    ShadeInducedSenRate = leafCohortParameters.ShadeInducedSenescenceRate.Value;
                SenessingLeafRelativeSize = leafCohortParameters.SenessingLeafRelativeSize.Value;
                SenescedFrac = FractionSenescing(thermalTime, propnStemMortality, SenessingLeafRelativeSize);

                // Doing leaf mass growth in the cohort
                Biomass liveBiomass = new Biomass(Live);

                //Set initial leaf status values
                LeafStartArea = LiveArea;
                LiveStart = new Biomass(Live);

                //If the model allows reallocation of senescent DM do it.
                if ((DMReallocationFactor > 0) && (SenescedFrac > 0))
                {
                    // DM to reallocate.

                    LeafStartMetabolicDMReallocationSupply = LiveStart.MetabolicWt*SenescedFrac*DMReallocationFactor;
                    LeafStartNonStructuralDMReallocationSupply = LiveStart.NonStructuralWt*SenescedFrac*
                                                                 DMReallocationFactor;

                    LeafStartDMReallocationSupply = LeafStartMetabolicDMReallocationSupply +
                                                    LeafStartNonStructuralDMReallocationSupply;
                    liveBiomass.MetabolicWt -= LeafStartMetabolicDMReallocationSupply;
                    liveBiomass.NonStructuralWt -= LeafStartNonStructuralDMReallocationSupply;

                }
                else
                {
                    LeafStartMetabolicDMReallocationSupply =
                        LeafStartNonStructuralDMReallocationSupply = LeafStartDMReallocationSupply = 0;
                }

                LeafStartDMRetranslocationSupply = liveBiomass.NonStructuralWt*DMRetranslocationFactor;
                //Nretranslocation is that which occurs before uptake (senessed metabolic N and all non-structuralN)
                LeafStartMetabolicNReallocationSupply = SenescedFrac*liveBiomass.MetabolicN*NReallocationFactor;
                LeafStartNonStructuralNReallocationSupply = SenescedFrac*liveBiomass.NonStructuralN*NReallocationFactor;
                //Retranslocated N is only that which occurs after N uptake. Both Non-structural and metabolic N are able to be retranslocated but metabolic N will only be moved if remobilisation of non-structural N does not meet demands
                LeafStartMetabolicNRetranslocationSupply = Math.Max(0.0,
                    (liveBiomass.MetabolicN*NRetranslocationFactor) - LeafStartMetabolicNReallocationSupply);
                LeafStartNonStructuralNRetranslocationSupply = Math.Max(0.0,
                    (liveBiomass.NonStructuralN*NRetranslocationFactor) - LeafStartNonStructuralNReallocationSupply);
                LeafStartNReallocationSupply = LeafStartNonStructuralNReallocationSupply + LeafStartMetabolicNReallocationSupply;
                LeafStartNRetranslocationSupply = LeafStartNonStructuralNRetranslocationSupply + LeafStartMetabolicNRetranslocationSupply;

                //zero locals variables
                PotentialStructuralDMAllocation = 0;
                PotentialMetabolicDMAllocation = 0;
                DMRetranslocated = 0;
                MetabolicNReallocated = 0;
                NonStructuralNReallocated = 0;
                MetabolicWtReallocated = 0;
                NonStructuralWtReallocated = 0;
                MetabolicNRetranslocated = 0;
                NonStructuralNRetrasnlocated = 0;
                MetabolicNAllocation = 0;
                StructuralDMAllocation = 0;
                MetabolicDMAllocation = 0;
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
            if ((leafCohortParameters.DroughtInducedSenAcceleration != null) && (IsFullyExpanded))
                thermalTime = tt*leafCohortParameters.DroughtInducedSenAcceleration.Value;
            else thermalTime = tt;

            //Growing leaf area after DM allocated
            DeltaCarbonConstrainedArea = (StructuralDMAllocation + MetabolicDMAllocation)*SpecificLeafAreaMax;
            //Fixme.  Live.Nonstructural should probably be included in DM supply for leaf growth also
            double deltaActualArea = Math.Min(DeltaWaterConstrainedArea, DeltaCarbonConstrainedArea);
            LiveArea += deltaActualArea;
            // Integrates leaf area at each cohort? FIXME-EIT is this the one integrated at leaf.cs?

            //Senessing leaf area
            double areaSenescing = LiveArea*SenescedFrac;
            double areaSenescingN = 0;
            if ((Live.MetabolicNConc <= MinimumNConc) & ((MetabolicNRetranslocated - MetabolicNAllocation) > 0.0))
                areaSenescingN = LeafStartArea*(MetabolicNRetranslocated - MetabolicNAllocation)/LiveStart.MetabolicN;

            double leafAreaLoss = Math.Max(areaSenescing, areaSenescingN);
            if (leafAreaLoss > 0)
                SenescedFrac = Math.Min(1.0, leafAreaLoss/LeafStartArea);

            double structuralWtSenescing = SenescedFrac*LiveStart.StructuralWt;
            double structuralNSenescing = SenescedFrac*LiveStart.StructuralN;
            double metabolicWtSenescing = SenescedFrac*LiveStart.MetabolicWt;
            double metabolicNSenescing = SenescedFrac*LiveStart.MetabolicN;
            double nonStructuralWtSenescing = SenescedFrac*LiveStart.NonStructuralWt;
            double nonStructuralNSenescing = SenescedFrac*LiveStart.NonStructuralN;

            DeadArea = DeadArea + leafAreaLoss;
            LiveArea = LiveArea - leafAreaLoss;
            // Final leaf area of cohort that will be integrated in Leaf.cs? (FIXME-EIT)

            Live.StructuralWt -= structuralWtSenescing;
            Dead.StructuralWt += structuralWtSenescing;

            Live.StructuralN -= structuralNSenescing;
            Dead.StructuralN += structuralNSenescing;

            Live.MetabolicWt -= Math.Max(0.0, metabolicWtSenescing - MetabolicWtReallocated);
            Dead.MetabolicWt += Math.Max(0.0, metabolicWtSenescing - MetabolicWtReallocated);


            Live.MetabolicN -= Math.Max(0.0, (metabolicNSenescing - MetabolicNReallocated - MetabolicNRetranslocated));
            //Don't Seness todays N if it has been taken for reallocation
            Dead.MetabolicN += Math.Max(0.0, (metabolicNSenescing - MetabolicNReallocated - MetabolicNRetranslocated));

            Live.NonStructuralN -= Math.Max(0.0,
                nonStructuralNSenescing - NonStructuralNReallocated - NonStructuralNRetrasnlocated);
            //Dont Senesess todays NonStructural N if it was retranslocated or reallocated 
            Dead.NonStructuralN += Math.Max(0.0,
                nonStructuralNSenescing - NonStructuralNReallocated - NonStructuralNRetrasnlocated);

            Live.NonStructuralWt -= Math.Max(0.0, nonStructuralWtSenescing - DMRetranslocated);
            Live.NonStructuralWt = Math.Max(0.0, Live.NonStructuralWt);

            Dead.NonStructuralWt += Math.Max(0.0,
                nonStructuralWtSenescing - DMRetranslocated - NonStructuralWtReallocated);

            MaintenanceRespiration = 0;
            //Do Maintenance respiration
            MaintenanceRespiration += Live.MetabolicWt*leafCohortParameters.MaintenanceRespirationFunction.Value;
            Live.MetabolicWt *= (1 - leafCohortParameters.MaintenanceRespirationFunction.Value);
            MaintenanceRespiration += Live.NonStructuralWt*leafCohortParameters.MaintenanceRespirationFunction.Value;
            Live.NonStructuralWt *= (1 - leafCohortParameters.MaintenanceRespirationFunction.Value);

            Age = Age + thermalTime;

            // Do Detachment of this Leaf Cohort
            // ---------------------------------
            DetachedFrac = FractionDetaching(thermalTime);
            if (DetachedFrac > 0.0)
            {
                double detachedWt = Dead.Wt*DetachedFrac;
                double detachedN = Dead.N*DetachedFrac;

                DeadArea *= (1 - DetachedFrac);
                Dead.StructuralWt *= (1 - DetachedFrac);
                Dead.StructuralN *= (1 - DetachedFrac);
                Dead.NonStructuralWt *= (1 - DetachedFrac);
                Dead.NonStructuralN *= (1 - DetachedFrac);
                Dead.MetabolicWt *= (1 - DetachedFrac);
                Dead.MetabolicN *= (1 - DetachedFrac);

                if (detachedWt > 0)
                    SurfaceOrganicMatter.Add(detachedWt*10, detachedN*10, 0, Plant.CropType, "Leaf");
            }
        }

        /// <summary>Removes leaves for cohort due to thin event.  </summary>
        /// <param name="proportionRemoved">The fraction.</param>
        public void DoThin(double proportionRemoved)
        {
            CohortPopulation *= 1 - proportionRemoved;
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

                if (remainingTt == 0)
                    fracSenAge = 1;
                else
                    fracSenAge = Math.Min(1, Math.Min(tt, ttInSenPhase)/remainingTt);
                if ((fracSenAge > 1) || (fracSenAge < 0))
                    throw new Exception("Bad Fraction Senescing");
            }
            else
            {
                fracSenAge = 0;
            }

            if (MaxLiveArea < LiveArea)
                MaxLiveArea = LiveArea;
            if (MaxCohortPopulation < CohortPopulation)
                MaxCohortPopulation = CohortPopulation;

            double fracSenShade = 0;
            if (LiveArea > 0)
            {
                fracSenShade = Math.Min(MaxLiveArea*ShadeInducedSenRate, LiveArea)/LiveArea;
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
            double ttInDetachPhase = Math.Max(0.0,
                Age + tt - LagDuration - GrowthDuration - SenescenceDuration - DetachmentLagDuration);
            if (ttInDetachPhase > 0)
            {
                double leafDuration = GrowthDuration + LagDuration + SenescenceDuration + DetachmentLagDuration +
                                      DetachmentDuration;
                double remainingTt = Math.Max(0, leafDuration - Age);

                if (remainingTt == 0)
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
