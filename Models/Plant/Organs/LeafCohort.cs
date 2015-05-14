using System;
using System.Collections.Generic;
using System.Text;

using Models.Core;
using Models.PMF.Functions;
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
    /// Leaf death
    /// ------------------------
    /// The leaf area, structural biomass and structural nitrogen of 
    /// green (live) parts is subtracted by a fraction.
    ///</remarks>
    [Serializable]
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
        public int Rank = 0;  // 1 based ranking
        /// <summary>The area</summary>
        public double Area = 0;
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
        /// <summary>
        /// The leaf start metabolic dm retranslocation supply
        /// </summary>
        [XmlIgnore]
        public double LeafStartMetabolicDMRetranslocationSupply = 0;
        /// <summary>
        /// The leaf start non structural dm retranslocation supply
        /// </summary>
        [XmlIgnore]
        public double LeafStartNonStructuralDMRetranslocationSupply = 0;
        //variables used in calculating daily supplies and deltas
        /// <summary>The delta wt</summary>
        [XmlIgnore]
        public double DeltaWt = 0;
        //public double StructuralNDemand = 0;
        //public double MetabolicNDemand = 0;
        //public double NonStructuralNDemand = 0;
        /// <summary>The potential area growth</summary>
        [XmlIgnore]
        public double PotentialAreaGrowth = 0;
        /// <summary>The delta potential area</summary>
        private double DeltaPotentialArea = 0;
        /// <summary>The delta water constrained area</summary>
        private double DeltaWaterConstrainedArea = 0;
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
        /// <summary>Gets the node age.</summary>
        /// <value>The node age.</value>
        public double NodeAge
        {
            get { return Age; }
        }
        /// <summary>The is initialised</summary>
        [XmlIgnore]
        public bool IsInitialised = false;
        /// <summary>Gets a value indicating whether this instance is not appeared.</summary>
        /// <value>
        /// <c>true</c> if this instance is not appeared; otherwise, <c>false</c>.
        /// </value>
        public bool IsNotAppeared
        {
            get
            {
                return (IsInitialised && Age == 0);
            }
        }
        /// <summary>Gets a value indicating whether this instance is growing.</summary>
        /// <value>
        /// <c>true</c> if this instance is growing; otherwise, <c>false</c>.
        /// </value>
        public bool IsGrowing
        {
            get { return (Age < GrowthDuration); }
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
            get
            {
                return (IsAppeared && Age > GrowthDuration);
            }
        }
        /// <summary>Gets a value indicating whether this instance is green.</summary>
        /// <value><c>true</c> if this instance is green; otherwise, <c>false</c>.</value>
        public bool IsGreen
        {
            get { return (Age < (GrowthDuration + LagDuration + SenescenceDuration)); }
        }
        /// <summary>Gets a value indicating whether this instance is senescing.</summary>
        /// <value>
        /// <c>true</c> if this instance is senescing; otherwise, <c>false</c>.
        /// </value>
        public bool IsSenescing
        {
            get { return (Age > (GrowthDuration + LagDuration)); }
        }
        /// <summary>Gets a value indicating whether this instance is not senescing.</summary>
        /// <value>
        /// <c>true</c> if this instance is not senescing; otherwise, <c>false</c>.
        /// </value>
        public bool IsNotSenescing
        {
            get { return (Age < (GrowthDuration + LagDuration)); }
        }
        /// <summary>
        /// Gets a value indicating whether [should be dead].
        /// </summary>
        /// <value><c>true</c> if [should be dead]; otherwise, <c>false</c>.</value>
        public bool ShouldBeDead
        {
            get { return !IsGreen; }
        }
        /// <summary>Gets a value indicating whether this <see cref="LeafCohort"/> is finished.</summary>
        /// <value><c>true</c> if finished; otherwise, <c>false</c>.</value>
        public bool Finished
        {
            get
            {
                return IsAppeared && !IsGreen;
            }
        }
        /// <summary>Gets a value indicating whether this instance is alive.</summary>
        /// <value><c>true</c> if this instance is alive; otherwise, <c>false</c>.</value>
        public bool IsAlive
        {
            get { return ((Age >= 0) && (Age < (GrowthDuration + LagDuration + SenescenceDuration))); }
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
                else
                    //Fixme.  This function is not returning the correct values.  Use commented out line
                    //return MaxArea / Population;
                    return MaxLiveArea / CohortPopulation;
            }
        }
        /// <summary>Gets the live population.</summary>
        /// <value>The live population.</value>
        public double LivePopulation
        {
            get
            {
                return CohortPopulation;
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
                else
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
                else if (Age >= GrowthDuration)
                    return 1;
                else
                    return Age / GrowthDuration;
            }
        }
        /// <summary>ns the fac.</summary>
        /// <returns></returns>
        private double NFac()
        {
            if (IsAppeared)
            {
                double Nconc = Live.NConc;
                double value = Math.Min(1.0, Math.Max(0.0, (Nconc - MinimumNConc) / (MaximumNConc - MinimumNConc)));
                return value;
            }
            else
                return 0;
        }
        /// <summary>Gets the specific area.</summary>
        /// <value>The specific area.</value>
        public double SpecificArea
        {
            get
            {
                if (Live.Wt > 0)
                    return LiveArea / Live.Wt;
                else
                    return 0;
            }
        }
        #endregion

        #region Arbitration methods
        /// <summary>Gets the structural dm demand.</summary>
        /// <value>The structural dm demand.</value>
        virtual public double StructuralDMDemand
        {
            get
            {
                if (IsGrowing)
                {
                    double TotalDMDemand = Math.Min(DeltaPotentialArea / ((SpecificLeafAreaMax + SpecificLeafAreaMin) / 2), DeltaWaterConstrainedArea / SpecificLeafAreaMin);
                    return TotalDMDemand * StructuralFraction;
                }
                else return 0;
            }
        }
        /// <summary>Gets the metabolic dm demand.</summary>
        /// <value>The metabolic dm demand.</value>
        virtual public double MetabolicDMDemand
        {
            get
            {
                if (IsGrowing)
                {
                    double TotalDMDemand = Math.Min(DeltaPotentialArea / ((SpecificLeafAreaMax + SpecificLeafAreaMin) / 2), DeltaWaterConstrainedArea / SpecificLeafAreaMin);
                    return TotalDMDemand * (1 - StructuralFraction);
                }
                else return 0;
            }
        }
        /// <summary>Gets the non structural dm demand.</summary>
        /// <value>The non structural dm demand.</value>
        virtual public double NonStructuralDMDemand
        {
            get
            {
                if (IsNotSenescing)
                {
                    double MaxNonStructuralDM = (MetabolicDMDemand + StructuralDMDemand + LiveStart.MetabolicWt + LiveStart.StructuralWt) * NonStructuralFraction;
                    return Math.Max(0.0, MaxNonStructuralDM - LiveStart.NonStructuralWt);
                }
                else
                    return 0.0;
            }
        }
        /// <summary>Gets the total dm demand.</summary>
        /// <value>The total dm demand.</value>
        virtual public double TotalDMDemand
        {
            get
            {
                return StructuralDMDemand + MetabolicDMDemand + NonStructuralDMDemand;
            }
        }
        /// <summary>Gets the structural n demand.</summary>
        /// <value>The structural n demand.</value>
        virtual public double StructuralNDemand
        {
            get
            {
                if ((IsNotSenescing) && (ShadeInducedSenRate == 0.0)) // Assuming a leaf will have no demand if it is senescing and will have no demand if it is is shaded conditions
                    return MinimumNConc * PotentialStructuralDMAllocation;
                else
                    return 0.0;
            }
        }
        /// <summary>Gets the non structural n demand.</summary>
        /// <value>The non structural n demand.</value>
        virtual public double NonStructuralNDemand
        {
            get
            {
                if ((IsNotSenescing) && (ShadeInducedSenRate == 0.0)) // Assuming a leaf will have no demand if it is senescing and will have no demand if it is is shaded conditions
                    return Math.Max(0.0, LuxaryNConc * (LiveStart.StructuralWt + LiveStart.MetabolicWt + PotentialStructuralDMAllocation + PotentialMetabolicDMAllocation) - Live.NonStructuralN);//Math.Max(0.0, MaxN - CritN - LeafStartNonStructuralN); //takes the difference between the two above as the maximum nonstructural N conc and subtracts the current nonstructural N conc to give a value
                else
                    return 0.0;
            }
        }
        /// <summary>Gets the metabolic n demand.</summary>
        /// <value>The metabolic n demand.</value>
        virtual public double MetabolicNDemand
        {
            get
            {
                if ((IsNotSenescing) && (ShadeInducedSenRate == 0.0)) // Assuming a leaf will have no demand if it is senescing and will have no demand if it is is shaded conditions
                    return FunctionalNConc * PotentialMetabolicDMAllocation;
                else
                    return 0.0;
            }
        }
        /// <summary>Gets the total n demand.</summary>
        /// <value>The total n demand.</value>
        virtual public double TotalNDemand
        {
            get
            {
                return StructuralNDemand + NonStructuralNDemand + MetabolicNDemand;
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
        virtual public BiomassAllocationType DMAllocation
        {
            set
            {
                //Firstly allocate DM
                if (value.Structural + value.NonStructural + value.Metabolic < -0.0000000001)
                    throw new Exception("-ve DM Allocation to Leaf Cohort");
                if ((value.Structural + value.NonStructural + value.Metabolic - TotalDMDemand) > 0.0000000001)
                    throw new Exception("DM Allocated to Leaf Cohort is in excess of its Demand");
                if (TotalDMDemand > 0)
                {
                    StructuralDMAllocation = value.Structural;
                    MetabolicDMAllocation = value.Metabolic;
                    Live.StructuralWt += value.Structural;
                    Live.MetabolicWt += value.Metabolic;
                    Live.NonStructuralWt += value.NonStructural;
                }

                //Then remove reallocated DM
                if (value.Reallocation - (LeafStartMetabolicDMReallocationSupply + LeafStartNonStructuralDMReallocationSupply) > 0.00000000001)
                    throw new Exception("A leaf cohort cannot supply that amount for DM Reallocation");
                if (value.Reallocation < -0.0000000001)
                    throw new Exception("Leaf cohort given negative DM Reallocation");
                if (value.Reallocation > 0.0)
                {
                    NonStructuralWtReallocated = Math.Min(LeafStartNonStructuralDMReallocationSupply, value.Reallocation); //Reallocate nonstructural first
                    MetabolicWtReallocated = Math.Max(0.0, Math.Min(value.Reallocation - NonStructuralWtReallocated, LeafStartMetabolicDMReallocationSupply)); //Then reallocate metabolic DM
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
        /*virtual public double NAllocation
        {
            set
            {
                if (value < -0.000000001)
                    throw new Exception("-ve N allocation to Leaf cohort");
                if ((value - TotalNDemand) > 0.0000000001)
                    throw new Exception("N Allocation to leaf cohort in excess of its Demand");
                if (TotalNDemand > 0.0)
                {
                    double StructDemFrac = 0;
                    if (StructuralNDemand > 0)
                        StructDemFrac = StructuralNDemand / (StructuralNDemand + MetabolicNDemand);
                    StructuralNAllocation = Math.Min(value * StructDemFrac, StructuralNDemand);
                    MetabolicNAllocation = Math.Min(value - StructuralNAllocation, MetabolicNDemand);
                    Live.StructuralN += StructuralNAllocation;
                    Live.MetabolicN += MetabolicNAllocation;//Then partition N to Metabolic
                    Live.NonStructuralN += Math.Max(0.0, value - StructuralNAllocation - MetabolicNAllocation); //Then partition N to NonStructural
                }
            }
        }*/
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
        virtual public BiomassAllocationType NAllocation
        {
            set
            {
                //Fresh allocations
                Live.StructuralN += value.Structural;
                Live.MetabolicN += value.Metabolic;
                Live.NonStructuralN += value.NonStructural;
                //Reallocation
                if (value.Reallocation - (LeafStartMetabolicNReallocationSupply + LeafStartNonStructuralNReallocationSupply) > 0.00000000001)
                    throw new Exception("A leaf cohort cannot supply that amount for N Reallocation");
                if (value.Reallocation < -0.0000000001)
                    throw new Exception("Leaf cohort given negative N Reallocation");
                if (value.Reallocation > 0.0)
                {
                    NonStructuralNReallocated = Math.Min(LeafStartNonStructuralNReallocationSupply, value.Reallocation); //Reallocate nonstructural first
                    MetabolicNReallocated = Math.Max(0.0, value.Reallocation - LeafStartNonStructuralNReallocationSupply); //Then reallocate metabolic N
                    Live.NonStructuralN -= NonStructuralNReallocated;
                    Live.MetabolicN -= MetabolicNReallocated;
                }
                //Retranslocation
                if (value.Retranslocation - (LeafStartMetabolicNRetranslocationSupply + LeafStartNonStructuralNRetranslocationSupply) > 0.00000000001)
                    throw new Exception("A leaf cohort cannot supply that amount for N Retranslocation");
                if (value.Retranslocation < -0.0000000001)
                    throw new Exception("Leaf cohort given negative N Retranslocation");
                if (value.Retranslocation > 0.0)
                {
                    NonStructuralNRetrasnlocated = Math.Min(LeafStartNonStructuralNRetranslocationSupply, value.Retranslocation); //Reallocate nonstructural first
                    MetabolicNRetranslocated = Math.Max(0.0, value.Retranslocation - LeafStartNonStructuralNRetranslocationSupply); //Then reallocate metabolic N
                    Live.NonStructuralN -= NonStructuralNRetrasnlocated;
                    Live.MetabolicN -= MetabolicNRetranslocated;
                }
            }
        }
        /// <summary>Gets the n retranslocation supply.</summary>
        /// <value>The n retranslocation supply.</value>
        virtual public double NRetranslocationSupply
        {
            get
            {
                return LeafStartNonStructuralNRetranslocationSupply + LeafStartMetabolicNRetranslocationSupply;
            }
        }
        /*virtual public double NRetranslocation
        {
            set
            {
                if (value - (LeafStartMetabolicNRetranslocationSupply + LeafStartNonStructuralNRetranslocationSupply) > 0.00000000001)
                    throw new Exception("A leaf cohort cannot supply that amount for N Retranslocation");
                if (value < -0.0000000001)
                    throw new Exception("Leaf cohort given negative N Retranslocation");
                if (value > 0.0)
                {
                    NonStructuralNRetrasnlocated = Math.Min(LeafStartNonStructuralNRetranslocationSupply, value); //Reallocate nonstructural first
                    MetabolicNRetranslocated = Math.Max(0.0, value - LeafStartNonStructuralNRetranslocationSupply); //Then reallocate metabolic N
                    Live.NonStructuralN -= NonStructuralNRetrasnlocated;
                    Live.MetabolicN -= MetabolicNRetranslocated;
                }
            }
        }*/
        /// <summary>Gets the dm retranslocation supply.</summary>
        /// <value>The dm retranslocation supply.</value>
        public double DMRetranslocationSupply
        {
            get
            {
                return LeafStartDMRetranslocationSupply;
            }
        }
        /// <summary>Gets the n reallocation supply.</summary>
        /// <value>The n reallocation supply.</value>
        public double NReallocationSupply
        {
            get
            {
                return LeafStartNonStructuralNReallocationSupply + LeafStartMetabolicNReallocationSupply;
            }
        }
        /// <summary>Gets the dm reallocation supply.</summary>
        /// <value>The dm reallocation supply.</value>
        public double DMReallocationSupply
        {
            get
            {
                return LeafStartNonStructuralDMReallocationSupply + LeafStartMetabolicDMReallocationSupply;
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
        virtual public BiomassPoolType DMPotentialAllocation
        {
            set
            {
                if (value.Structural < -0.0000000001)
                    throw new Exception("-ve Potential DM Allocation to Leaf Cohort");
                if ((value.Structural - StructuralDMDemand) > 0.0000000001)
                    throw new Exception("Potential DM Allocation to Leaf Cohortis in excess of its Demand");
                if (StructuralDMDemand > 0)
                {
                    PotentialStructuralDMAllocation = value.Structural;
                    //double StructuralDMDemandFrac = StructuralDMDemand / (StructuralDMDemand + MetabolicDMDemand);
                    //PotentialStructuralDMAllocation = value * StructuralDMDemandFrac;
                    //PotentialMetabolicDMAllocation = value * (1 - StructuralDMDemandFrac);
                }

                if (value.Metabolic < -0.0000000001)
                    throw new Exception("-ve Potential DM Allocation to Leaf Cohort");
                if ((value.Metabolic - MetabolicDMDemand) > 0.0000000001)
                    throw new Exception("Potential DM Allocation to Leaf Cohortis in excess of its Demand");
                if (MetabolicDMDemand > 0)
                {
                    PotentialMetabolicDMAllocation = value.Metabolic;
                    //double StructuralDMDemandFrac = StructuralDMDemand / (StructuralDMDemand + MetabolicDMDemand);
                    //PotentialStructuralDMAllocation = value * StructuralDMDemandFrac;
                    //PotentialMetabolicDMAllocation = value * (1 - StructuralDMDemandFrac);
                }
            }


        }
        /*public double NReallocation
        {
            set
            {
                if (value - (LeafStartMetabolicNReallocationSupply + LeafStartNonStructuralNReallocationSupply) > 0.00000000001)
                    throw new Exception("A leaf cohort cannot supply that amount for N Reallocation");
                if (value < -0.0000000001)
                    throw new Exception("Leaf cohort given negative N Reallocation");
                if (value > 0.0)
                {
                    NonStructuralNReallocated = Math.Min(LeafStartNonStructuralNReallocationSupply, value); //Reallocate nonstructural first
                    MetabolicNReallocated = Math.Max(0.0, value - LeafStartNonStructuralNReallocationSupply); //Then reallocate metabolic N
                    Live.NonStructuralN -= NonStructuralNReallocated;
                    Live.MetabolicN -= MetabolicNReallocated;
                }
            }
        }*/
        #endregion

        #region Functions
        /// <summary>The default constuctor that will be called by the APSIM infrastructure.</summary>
        public LeafCohort()
        {
        }
        /// <summary>Returns a clone of this object</summary>
        /// <returns></returns>
        public virtual LeafCohort Clone()
        {
            LeafCohort NewLeaf = (LeafCohort)this.MemberwiseClone();
            NewLeaf.Live = new Biomass();
            NewLeaf.Dead = new Biomass();
            return NewLeaf;
        }
        /// <summary>Does the initialisation.</summary>
        public void DoInitialisation()
        {
            IsInitialised = true;
            Age = 0;
        }
        /// <summary>Does the appearance.</summary>
        /// <param name="LeafFraction">The leaf fraction.</param>
        /// <param name="LeafCohortParameters">The leaf cohort parameters.</param>
        public void DoAppearance(double LeafFraction, Models.PMF.Organs.Leaf.LeafCohortParameters LeafCohortParameters)
        {
            Name = "Leaf" + Rank.ToString();
            IsAppeared = true;
            if (CohortPopulation == 0)
                CohortPopulation = Structure.TotalStemPopn;
            MaxArea = LeafCohortParameters.MaxArea.Value * CellDivisionStressFactor * LeafFraction;//Reduce potential leaf area due to the effects of stress prior to appearance on cell number 
            GrowthDuration = LeafCohortParameters.GrowthDuration.Value * LeafFraction;
            LagDuration = LeafCohortParameters.LagDuration.Value;
            SenescenceDuration = LeafCohortParameters.SenescenceDuration.Value;
            DetachmentLagDuration = LeafCohortParameters.DetachmentLagDuration.Value;
            DetachmentDuration = LeafCohortParameters.DetachmentDuration.Value;
            StructuralFraction = LeafCohortParameters.StructuralFraction.Value;
            SpecificLeafAreaMax = LeafCohortParameters.SpecificLeafAreaMax.Value;
            SpecificLeafAreaMin = LeafCohortParameters.SpecificLeafAreaMin.Value;
            MaximumNConc = LeafCohortParameters.MaximumNConc.Value;
            MinimumNConc = LeafCohortParameters.MinimumNConc.Value;
            if (LeafCohortParameters.NonStructuralFraction != null)
                NonStructuralFraction = LeafCohortParameters.NonStructuralFraction.Value;
            if (LeafCohortParameters.InitialNConc != null) //FIXME HEB I think this can be removed
                InitialNConc = LeafCohortParameters.InitialNConc.Value;
            //if (Area > MaxArea)  FIXMEE HEB  This error trap should be activated but throws errors in chickpea so that needs to be fixed first.
            //    throw new Exception("Initial Leaf area is greater that the Maximum Leaf Area.  Check set up of initial leaf area values to make sure they are not to large and check MaxArea function and CellDivisionStressFactor Function to make sure the values they are returning will not be too small.");
            Age = Area / MaxArea * GrowthDuration; //FIXME.  The size function is not linear so this does not give an exact starting age.  Should re-arange the the size function to return age for a given area to initialise age on appearance.
            LiveArea = Area * CohortPopulation;
            Live.StructuralWt = LiveArea / ((SpecificLeafAreaMax + SpecificLeafAreaMin) / 2) * StructuralFraction;
            Live.StructuralN = Live.StructuralWt * InitialNConc;
            FunctionalNConc = (LeafCohortParameters.CriticalNConc.Value - (LeafCohortParameters.MinimumNConc.Value * StructuralFraction)) * (1 / (1 - StructuralFraction));
            LuxaryNConc = (LeafCohortParameters.MaximumNConc.Value - LeafCohortParameters.CriticalNConc.Value);
            Live.MetabolicWt = (Live.StructuralWt * 1 / StructuralFraction) - Live.StructuralWt;
            Live.NonStructuralWt = 0;
            Live.StructuralN = Live.StructuralWt * MinimumNConc;
            Live.MetabolicN = Live.MetabolicWt * FunctionalNConc;
            Live.NonStructuralN = 0;
            NReallocationFactor = LeafCohortParameters.NReallocationFactor.Value;
            if (LeafCohortParameters.DMReallocationFactor != null)
                DMReallocationFactor = LeafCohortParameters.DMReallocationFactor.Value;
            NRetranslocationFactor = LeafCohortParameters.NRetranslocationFactor.Value;
            if (LeafCohortParameters.DMRetranslocationFactor != null)
                DMRetranslocationFactor = LeafCohortParameters.DMRetranslocationFactor.Value;
            else DMRetranslocationFactor = 0;
        }
        /// <summary>Does the potential growth.</summary>
        /// <param name="TT">The tt.</param>
        /// <param name="LeafCohortParameters">The leaf cohort parameters.</param>
        virtual public void DoPotentialGrowth(double TT, Models.PMF.Organs.Leaf.LeafCohortParameters LeafCohortParameters)
        {
            //Reduce leaf Population in Cohort due to plant mortality
            double StartPopulation = CohortPopulation;
            if (Structure.ProportionPlantMortality > 0)
            {
                CohortPopulation -= CohortPopulation * Structure.ProportionPlantMortality;
            }
            //Reduce leaf Population in Cohort  due to branch mortality
            if ((Structure.ProportionBranchMortality > 0) && (CohortPopulation > Structure.MainStemPopn))  //Ensure we there are some branches.
            {
                double deltaPopn = Math.Min(CohortPopulation * Structure.ProportionBranchMortality, CohortPopulation - Structure.MainStemPopn); //Ensure we are not killing more branches that the cohort has.
                CohortPopulation -= CohortPopulation * Structure.ProportionBranchMortality;
            }
            double PropnStemMortality = (StartPopulation - CohortPopulation) / StartPopulation;

            //Calculate Accumulated Stress Factor for reducing potential leaf size
            if (IsNotAppeared && (LeafCohortParameters.CellDivisionStress != null))
            {
                CellDivisionStressDays += 1;
                CellDivisionStressAccumulation += LeafCohortParameters.CellDivisionStress.Value;
                //FIXME HEB  The limitation below should be used to avoid zero values for maximum leaf size.
                //CellDivisionStressFactor = Math.Max(CellDivisionStressAccumulation / CellDivisionStressDays, 0.01);
                CellDivisionStressFactor = CellDivisionStressAccumulation / CellDivisionStressDays;
            }

            if (IsAppeared)
            {
                // The following line needs to be CHANGED!!!!!!
                //Leaf.CurrentRank = Rank - 1; //Set currentRank variable in parent leaf for use in experssion functions
                //Acellerate thermal time accumulation if crop is water stressed.
                double _ThermalTime;
                if ((LeafCohortParameters.DroughtInducedSenAcceleration != null) && (IsFullyExpanded))
                    _ThermalTime = TT * LeafCohortParameters.DroughtInducedSenAcceleration.Value;
                else _ThermalTime = TT;

                //Leaf area growth parameters
                DeltaPotentialArea = PotentialAreaGrowthFunction(_ThermalTime); //Calculate delta leaf area in the absence of water stress
                DeltaWaterConstrainedArea = DeltaPotentialArea * LeafCohortParameters.ExpansionStress.Value; //Reduce potential growth for water stress

                CoverAbove = Leaf.CoverAboveCohort(Rank); // Calculate cover above leaf cohort (unit??? FIXME-EIT)
                if (LeafCohortParameters.ShadeInducedSenescenceRate != null)
                    ShadeInducedSenRate = LeafCohortParameters.ShadeInducedSenescenceRate.Value;
                SenescedFrac = FractionSenescing(_ThermalTime, PropnStemMortality);

                // Doing leaf mass growth in the cohort
                Biomass LiveBiomass = new Biomass(Live);

                //Set initial leaf status values
                LeafStartArea = LiveArea;
                LiveStart = new Biomass(Live);

                //If the model allows reallocation of senescent DM do it.
                if ((DMReallocationFactor > 0) && (SenescedFrac > 0))
                {
                    // DM to reallocate.

                    LeafStartMetabolicDMReallocationSupply = LiveStart.MetabolicWt * SenescedFrac * DMReallocationFactor;
                    LeafStartNonStructuralDMReallocationSupply = LiveStart.NonStructuralWt * SenescedFrac * DMReallocationFactor;

                    LeafStartDMReallocationSupply = LeafStartMetabolicDMReallocationSupply + LeafStartNonStructuralDMReallocationSupply;
                    LiveBiomass.MetabolicWt -= LeafStartMetabolicDMReallocationSupply;
                    LiveBiomass.NonStructuralWt -= LeafStartNonStructuralDMReallocationSupply;

                }
                else
                {
                    LeafStartMetabolicDMReallocationSupply = LeafStartNonStructuralDMReallocationSupply = LeafStartDMReallocationSupply = 0;
                }

                LeafStartDMRetranslocationSupply = LiveBiomass.NonStructuralWt * DMRetranslocationFactor;
                //Nretranslocation is that which occurs before uptake (senessed metabolic N and all non-structuralN)
                LeafStartMetabolicNReallocationSupply = SenescedFrac * LiveBiomass.MetabolicN * NReallocationFactor;
                LeafStartNonStructuralNReallocationSupply = SenescedFrac * LiveBiomass.NonStructuralN * NReallocationFactor;
                //Retranslocated N is only that which occurs after N uptake. Both Non-structural and metabolic N are able to be retranslocated but metabolic N will only be moved if remobilisation of non-structural N does not meet demands
                LeafStartMetabolicNRetranslocationSupply = Math.Max(0.0, (LiveBiomass.MetabolicN * NRetranslocationFactor) - LeafStartMetabolicNReallocationSupply);
                LeafStartNonStructuralNRetranslocationSupply = Math.Max(0.0, (LiveBiomass.NonStructuralN * NRetranslocationFactor) - LeafStartNonStructuralNReallocationSupply);
                LeafStartNReallocationSupply = NReallocationSupply;
                LeafStartNRetranslocationSupply = NRetranslocationSupply;

                //zero locals variables
                //StructuralDMDemand = 0;
                //MetabolicDMDemand = 0;
                //StructuralNDemand = 0;
                //MetabolicNDemand = 0;
                //NonStructuralNDemand = 0;
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
        /// <param name="TT">The tt.</param>
        /// <param name="LeafCohortParameters">The leaf cohort parameters.</param>
        virtual public void DoActualGrowth(double TT, Models.PMF.Organs.Leaf.LeafCohortParameters LeafCohortParameters)
        {
            if (IsAppeared)
            {
                //Acellerate thermal time accumulation if crop is water stressed.
                double _ThermalTime;
                if ((LeafCohortParameters.DroughtInducedSenAcceleration != null) && (IsFullyExpanded))
                    _ThermalTime = TT * LeafCohortParameters.DroughtInducedSenAcceleration.Value;
                else _ThermalTime = TT;

                //Growing leaf area after DM allocated
                double DeltaCarbonConstrainedArea = (StructuralDMAllocation + MetabolicDMAllocation) * SpecificLeafAreaMax;  //Fixme.  Live.Nonstructural should probably be included in DM supply for leaf growth also
                double DeltaActualArea = Math.Min(DeltaWaterConstrainedArea, DeltaCarbonConstrainedArea);
                LiveArea += DeltaActualArea; // Integrates leaf area at each cohort? FIXME-EIT is this the one integrated at leaf.cs?

                //Senessing leaf area
                double AreaSenescing = LiveArea * SenescedFrac;
                double AreaSenescingN = 0;
                if ((Live.MetabolicNConc <= MinimumNConc) & ((MetabolicNRetranslocated - MetabolicNAllocation) > 0.0))
                    AreaSenescingN = LeafStartArea * (MetabolicNRetranslocated - MetabolicNAllocation) / LiveStart.MetabolicN;

                double LeafAreaLoss = Math.Max(AreaSenescing, AreaSenescingN);
                if (LeafAreaLoss > 0)
                    SenescedFrac = Math.Min(1.0, LeafAreaLoss / LeafStartArea);


                /* RFZ why variation between using LiveStart and Live
                double StructuralWtSenescing = SenescedFrac * Live.StructuralWt;
                double StructuralNSenescing = SenescedFrac * Live.StructuralN;
                double MetabolicWtSenescing = SenescedFrac * Live.MetabolicWt;
                double MetabolicNSenescing = SenescedFrac * LiveStart.MetabolicN;
                double NonStructuralWtSenescing = SenescedFrac * Live.NonStructuralWt;
                double NonStructuralNSenescing = SenescedFrac * LiveStart.NonStructuralN;
                */


                double StructuralWtSenescing = SenescedFrac * LiveStart.StructuralWt;
                double StructuralNSenescing = SenescedFrac * LiveStart.StructuralN;
                double MetabolicWtSenescing = SenescedFrac * LiveStart.MetabolicWt;
                double MetabolicNSenescing = SenescedFrac * LiveStart.MetabolicN;
                double NonStructuralWtSenescing = SenescedFrac * LiveStart.NonStructuralWt;
                double NonStructuralNSenescing = SenescedFrac * LiveStart.NonStructuralN;

                DeadArea = DeadArea + LeafAreaLoss;
                LiveArea = LiveArea - LeafAreaLoss; // Final leaf area of cohort that will be integrated in Leaf.cs? (FIXME-EIT)

                Live.StructuralWt -= StructuralWtSenescing;
                Dead.StructuralWt += StructuralWtSenescing;

                Live.StructuralN -= StructuralNSenescing;
                Dead.StructuralN += StructuralNSenescing;

                Live.MetabolicWt -= Math.Max(0.0, MetabolicWtSenescing - MetabolicWtReallocated);
                Dead.MetabolicWt += Math.Max(0.0, MetabolicWtSenescing - MetabolicWtReallocated);


                Live.MetabolicN -= Math.Max(0.0, (MetabolicNSenescing - MetabolicNReallocated - MetabolicNRetranslocated));  //Don't Seness todays N if it has been taken for reallocation
                Dead.MetabolicN += Math.Max(0.0, (MetabolicNSenescing - MetabolicNReallocated - MetabolicNRetranslocated));

                Live.NonStructuralN -= Math.Max(0.0, NonStructuralNSenescing - NonStructuralNReallocated - NonStructuralNRetrasnlocated);  //Dont Senesess todays NonStructural N if it was retranslocated or reallocated 
                Dead.NonStructuralN += Math.Max(0.0, NonStructuralNSenescing - NonStructuralNReallocated - NonStructuralNRetrasnlocated);

                Live.NonStructuralWt -= Math.Max(0.0, NonStructuralWtSenescing - DMRetranslocated);
                Live.NonStructuralWt = Math.Max(0.0, Live.NonStructuralWt);

                //RFZ
                //Reallocated gos to to reallocation pool but not into dead pool. 
                Dead.NonStructuralWt += Math.Max(0.0, NonStructuralWtSenescing - DMRetranslocated - NonStructuralWtReallocated);



                Age = Age + _ThermalTime;

                // Do Detachment of this Leaf Cohort
                // ---------------------------------
                DetachedFrac = FractionDetaching(_ThermalTime);
                if (DetachedFrac > 0.0)
                {
                    double DetachedArea = DeadArea * DetachedFrac;
                    double DetachedWt = Dead.Wt * DetachedFrac;
                    double DetachedN = Dead.N * DetachedFrac;

                    DeadArea *= (1 - DetachedFrac);
                    Dead.StructuralWt *= (1 - DetachedFrac);
                    Dead.StructuralN *= (1 - DetachedFrac);
                    Dead.NonStructuralWt *= (1 - DetachedFrac);
                    Dead.NonStructuralN *= (1 - DetachedFrac);
                    Dead.MetabolicWt *= (1 - DetachedFrac);
                    Dead.MetabolicN *= (1 - DetachedFrac);

                    if (DetachedWt > 0)
                        SurfaceOrganicMatter.Add(DetachedWt * 10, DetachedN * 10, 0, Plant.CropType, "Leaf");
                }
            }
        }
        /// <summary>Does the kill.</summary>
        /// <param name="fraction">The fraction.</param>
        virtual public void DoKill(double fraction)
        {
            if (IsInitialised)
            {
                double change;
                change = LiveArea * fraction;
                LiveArea -= change;
                DeadArea += change;

                change = Live.StructuralWt * fraction;
                Live.StructuralWt -= change;
                Dead.StructuralWt += change;

                change = Live.NonStructuralWt * fraction;
                Live.NonStructuralWt -= change;
                Dead.NonStructuralWt += change;

                change = Live.StructuralN * fraction;
                Live.StructuralN -= change;
                Dead.StructuralN += change;

                change = Live.NonStructuralN * fraction;
                Live.NonStructuralN -= change;
                Dead.NonStructuralN += change;
            }
        }
        /// <summary>Does the frost.</summary>
        /// <param name="fraction">The fraction.</param>
        virtual public void DoFrost(double fraction)
        {
            if (IsAppeared)
                DoKill(fraction);
        }
        /// <summary>Potential delta LAI</summary>
        /// <param name="TT">thermal-time</param>
        /// <returns>(mm2 leaf/cohort position/m2 soil/day)</returns>
        virtual public double PotentialAreaGrowthFunction(double TT)
        {
            double BranchNo = Structure.TotalStemPopn - Structure.MainStemPopn;  //Fixme, this line appears redundant
            double leafSizeDelta = SizeFunction(Age + TT) - SizeFunction(Age); //mm2 of leaf expanded in one day at this cohort (Today's minus yesterday's Area/cohort)
            double growth = CohortPopulation * leafSizeDelta; // Daily increase in leaf area for that cohort position in a per m2 basis (mm2/m2/day)
            return growth;                              // FIXME-EIT Unit conversion to m2/m2 could happen here and population could be considered at higher level only (?)
        }
        /// <summary>Potential average leaf size for today per cohort (no stress)</summary>
        /// <param name="TT">Thermal-time accumulation since cohort initiation</param>
        /// <returns>Average leaf size (mm2/leaf)</returns>
        protected double SizeFunction(double TT)
        {
            double alpha = -Math.Log((1 / 0.99 - 1) / (MaxArea / (MaxArea * 0.01) - 1)) / GrowthDuration;
            double leafsize = MaxArea / (1 + (MaxArea / (MaxArea * 0.01) - 1) * Math.Exp(-alpha * TT));
            return leafsize;

        }
        /// <summary>Fractions the senescing.</summary>
        /// <param name="TT">The tt.</param>
        /// <param name="StemMortality">The stem mortality.</param>
        /// <returns></returns>
        /// <exception cref="System.Exception">Bad Fraction Senescing</exception>
        public double FractionSenescing(double TT, double StemMortality)
        {
            //Calculate fraction of leaf area senessing based on age and shading.  This is used to to calculate change in leaf area and Nreallocation supply.
            if (IsAppeared)
            {
                double FracSenAge = 0;
                double TTInSenPhase = Math.Max(0.0, Age + TT - LagDuration - GrowthDuration);
                if (TTInSenPhase > 0)
                {
                    double LeafDuration = GrowthDuration + LagDuration + SenescenceDuration;
                    double RemainingTT = Math.Max(0, LeafDuration - Age);

                    if (RemainingTT == 0)
                        FracSenAge = 1;
                    else
                        FracSenAge = Math.Min(1, Math.Min(TT, TTInSenPhase) / RemainingTT);
                    if ((FracSenAge > 1) || (FracSenAge < 0))
                    {
                        throw new Exception("Bad Fraction Senescing");
                    }
                }
                else
                {
                    FracSenAge = 0;
                }

                if (MaxLiveArea < LiveArea)
                    MaxLiveArea = LiveArea;

                double FracSenShade = 0;
                if (LiveArea > 0)
                {
                    FracSenShade = Math.Min(MaxLiveArea * ShadeInducedSenRate, LiveArea) / LiveArea;
                    FracSenShade += StemMortality;
                    FracSenShade = Math.Min(FracSenShade, 1.0);
                }

                return Math.Max(FracSenAge, FracSenShade);
            }
            else
                return 0;
        }
        /// <summary>Fractions the detaching.</summary>
        /// <param name="TT">The tt.</param>
        /// <returns></returns>
        /// <exception cref="System.Exception">Bad Fraction Detaching</exception>
        public double FractionDetaching(double TT)
        {
            double FracDetach = 0;
            double TTInDetachPhase = Math.Max(0.0, Age + TT - LagDuration - GrowthDuration - SenescenceDuration - DetachmentLagDuration);
            if (TTInDetachPhase > 0)
            {
                double LeafDuration = GrowthDuration + LagDuration + SenescenceDuration + DetachmentLagDuration + DetachmentDuration;
                double RemainingTT = Math.Max(0, LeafDuration - Age);

                if (RemainingTT == 0)
                    FracDetach = 1;
                else
                    FracDetach = Math.Min(1, Math.Min(TT, TTInDetachPhase) / RemainingTT);
                if ((FracDetach > 1) || (FracDetach < 0))
                    throw new Exception("Bad Fraction Detaching");
            }
            else
                FracDetach = 0;

            return FracDetach;

        }
        /// <summary>Called when [simulation commencing].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            //MyPaddock.Subscribe(Structure.InitialiseStage, DoInitialisation);
        }
        #endregion

    }
}
   
