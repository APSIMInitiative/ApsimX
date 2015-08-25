using System;
using System.Collections.Generic;
using System.Text;

using Models.Core;
using Models.PMF.Functions;
using Models.PMF.Functions.SupplyFunctions;
using Models.PMF.Phen;
using System.Xml.Serialization;
using Models.PMF.Interfaces;
using Models.Interfaces;
using APSIM.Shared.Utilities;

namespace Models.PMF.Organs
{
    /// <summary>
    /// A generic leaf model
    /// </summary>
    /// \param KDead The extinction coefficient for dead leaf (\f$k_d\f$).
    /// \param FrostFraction The fraction of leaf death caused by frost event.
    /// \param GsMax The maximum stomatal conductance (m/s).
    /// \param R50 SolRad at which stomatal conductance decreases to 50% (W/m2).
    /// \retval MaxCover The maximum coverage (\f$C_{max}\f$) with default value 1, 
    ///     which is set by manager sowing. 
    /// \retval LAI Leaf area index for green leaf (\f$\text{LAI}_{g}\f$, \f$m^2 m^{-2}\f$)
    /// \retval LAIDead Leaf area index for dead leaf  (\f$\text{LAI}_{d}\f$, \f$m^2 m^{-2}\f$)
    /// \retval LAITotal Total LAI including live and dead parts (\f$m^2 m^{-2}\f$)
    ///     \f[
    ///     LAI = \text{LAI}_{g} + \text{LAI}_{d}
    ///     \f]
    /// \retval CoverGreen Cover for green leaf (\f$C_g\f$, unitless). 
    ///     \f$C_g\f$ is calculated according to
    ///     extinction coefficient of green leaf (\f$k_{g}\f$).
    ///     \f[
    ///     C_{g}=C_{max}(1-\exp(-k_{g}\frac{\text{LAI}_{g}}{C_{max}}))
    ///     \f]
    ///     where, \f$k\f$ is the extinction coefficient which calculates 
    ///     by parameter "ExtinctionCoeff". 
    ///     As the default value of \f$C_{max}\f$ is 1, the function is reduced to
    ///     \f[
    ///      C_{g}=1-\exp(-k_{g}\text{LAI}_{g})
    ///     \f]
    /// \retval CoverDead Cover for dead leaf (\f$C_d\f$, unitless).
    ///     \f$C_d\f$ is calculated according to
    ///     extinction coefficient of dead leaf (\f$k_{d}\f$).
    ///     \f[
    ///     C_{d}=1-\exp(-k_{d}\text{LAI}_{d})
    ///     \f]
    /// \retval CoverTotal Total cover for green and dead leaves (\f$C_t\f$, unitless).
    ///     \f[
    ///     C_{t} = 1 - (1 - C_{g})(1 - C_{d})
    ///     \f]
    /// 
    /// \retval Height Plant height from Structure (mm). 
    /// \retval Depth Plant height from Structure (mm). Equal to plant height (not sure its function?)
    /// \retval FRGR Fractional relative growth rate (unitless, 0-1)
    ///     with 1.0 at full growth rate and 0.0 at no growth.
    /// \retval PotentialEP Potential evapotranspiration. Set by MICROCLIMATE.
    /// 
    /// <remarks>
    /// The organ "Leaf" consists of a series of \ref LeafCohort "cohort leaves", which 
    /// is identified by leaf rank (1 based ranking).
    /// 
    /// 
    /// On commencing simulation
    /// ------------------------
    /// OnSimulationCommencing is called on commencing simulation. 
    /// The leaves in the seed are initialized from all children with model 
    /// \ref Models.PMF.Organs.LeafCohort "LeafCohort". 
    /// 
    /// Potential growth 
    /// ------------------------
    /// 
    /// Senescence
    /// ------------------------
    /// Frost impact on leaf death
    /// ------------------------
    /// Each cohort leaf is killed by a fraction if value of FrostFraction is more than 0. 
    /// The frost fraction could be calculated through daily minimum temperature and
    /// growth stages. See \ref Models.PMF.Organs.LeafCohort "LeafCohort" model for details.
    /// See \subpage parameter "tutorial" about how to set up a parameter in APSIM. 
    /// </remarks>
    [Serializable]
    [Description("Leaf Class")]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class Leaf : BaseOrgan, AboveGround, ICanopy
    {
        #region Canopy interface

        /// <summary>Gets the canopy. Should return null if no canopy present.</summary>
        public string CanopyType { get { return Plant.CropType; } }

        /// <summary>Gets the LAI</summary>
        [Units("m^2/m^2")]
        public double LAI
        {
            get
            {
                int MM2ToM2 = 1000000; // Conversion of mm2 to m2
                double value = 0;
                foreach (LeafCohort L in Leaves)
                    value = value + L.LiveArea / MM2ToM2;
                return value;
            }
        }

        /// <summary>Gets the LAI live + dead (m^2/m^2)</summary>
        public double LAITotal { get { return LAI + LAIDead; } }

        /// <summary>Gets the cover green.</summary>
        [Units("0-1")]
        public double CoverGreen
        {
            get
            {
                return MaxCover * (1.0 - Math.Exp(-ExtinctionCoeff.Value * LAI / MaxCover));
            }
        }

        /// <summary>Gets the cover total.</summary>
        [Units("0-1")]
        public double CoverTotal { get { return 1.0 - (1 - CoverGreen) * (1 - CoverDead); } }

        /// <summary>Gets the height.</summary>
        [Units("mm")]
        public double Height { get { return Structure.Height; } }

        /// <summary>Gets the depth.</summary>
        [Units("mm")]
        public double Depth { get { return Structure.Height; } }//  Fixme.  This needs to be replaced with something that give sensible numbers for tree crops

        /// <summary>Gets  FRGR.</summary>
        [Units("0-1")]
        public double FRGR { get { return Photosynthesis.FRGR; } }
        
        /// <summary>Sets the potential evapotranspiration. Set by MICROCLIMATE.</summary>
        [Units("mm")]
        public double PotentialEP { get;  set; }

        /// <summary>Sets the light profile. Set by MICROCLIMATE.</summary>
        public CanopyEnergyBalanceInterceptionlayerType[] LightProfile { get; set; } 
        #endregion


        #region Links
        /// <summary>The plant</summary>
        [Link]
        public Plant Plant = null;
        /// <summary>The summary</summary>
        [Link]
        ISummary Summary = null;
        /// <summary>The arbitrator</summary>
        [Link]
        public OrganArbitrator Arbitrator = null;
        /// <summary>The structure</summary>
        [Link]
        public Structure Structure = null;
        /// <summary>The phenology</summary>
        [Link]
        public Phenology Phenology = null;
        [Link]
        ISurfaceOrganicMatter SurfaceOrganicMatter = null;
        #endregion

        #region Structures
        /// <summary>
        /// 
        /// </summary>
        [Serializable]
        public class LeafCohortParameters : Model
        {
            /// <summary>The maximum area</summary>
            [Link]
            [Units("mm2")]
            public IFunction MaxArea = null;
            /// <summary>The growth duration</summary>
            [Link]
            public IFunction GrowthDuration = null;
            /// <summary>The lag duration</summary>
            [Link]
            public IFunction LagDuration = null;
            /// <summary>The senescence duration</summary>
            [Link]
            public IFunction SenescenceDuration = null;
            /// <summary>The detachment lag duration</summary>
            [Link]
            public IFunction DetachmentLagDuration = null;
            /// <summary>The detachment duration</summary>
            [Link]
            public IFunction DetachmentDuration = null;
            /// <summary>The specific leaf area maximum</summary>
            [Link]
            public IFunction SpecificLeafAreaMax = null;
            /// <summary>The specific leaf area minimum</summary>
            [Link]
            public IFunction SpecificLeafAreaMin = null;
            /// <summary>The structural fraction</summary>
            [Link]
            public IFunction StructuralFraction = null;
            /// <summary>The maximum n conc</summary>
            [Link]
            public IFunction MaximumNConc = null;
            /// <summary>The minimum n conc</summary>
            [Link]
            public IFunction MinimumNConc = null;
            /// <summary>The structural n conc</summary>
            [Link(IsOptional = true)]
            public IFunction StructuralNConc = null;
            /// <summary>The initial n conc</summary>
            [Link]
            public IFunction InitialNConc = null;
            /// <summary>The n reallocation factor</summary>
            [Link]
            public IFunction NReallocationFactor = null;
            /// <summary>The dm reallocation factor</summary>
            [Link(IsOptional = true)]
            public IFunction DMReallocationFactor = null;
            /// <summary>The n retranslocation factor</summary>
            [Link]
            public IFunction NRetranslocationFactor = null;
            /// <summary>The expansion stress</summary>
            [Link]
            public IFunction ExpansionStress = null;
            /// <summary>The critical n conc</summary>
            [Link]
            public IFunction CriticalNConc = null;
            /// <summary>The dm retranslocation factor</summary>
            [Link]
            public IFunction DMRetranslocationFactor = null;
            /// <summary>The shade induced senescence rate</summary>
            [Link]
            public IFunction ShadeInducedSenescenceRate = null;
            /// <summary>The drought induced sen acceleration</summary>
            [Link(IsOptional = true)]
            public IFunction DroughtInducedSenAcceleration = null;
            /// <summary>The non structural fraction</summary>
            [Link]
            public IFunction NonStructuralFraction = null;
            /// <summary>The cell division stress</summary>
            [Link(IsOptional = true)]
            public IFunction CellDivisionStress = null;
        }
        #endregion

        #region Parameters
        // DeanH: I have removed DroughtInducedSenAcceleration - it can be incorported into the ThermalTime function
        // in the XML. No need for it to be in leaf.
        
        // Hamish:  We need to put this back in.  putting it in tt will acellerate development.  
        // the response it was capturing in leaf was where leaf area senescence is acellerated but other development processes are not.

        /// <summary>The initial leaves</summary>
        [DoNotDocument]
        private LeafCohort[] InitialLeaves;
        /// <summary>The leaf cohort parameters</summary>
        [Link] LeafCohortParameters CohortParameters = null;
        /// <summary>The photosynthesis</summary>
        [Link] RUEModel Photosynthesis = null;
        /// <summary>The thermal time</summary>
        [Link]
        IFunction ThermalTime = null;
        /// <summary>The extinction coeff</summary>
        [Link]
        IFunction ExtinctionCoeff = null;
        /// <summary>The frost fraction</summary>
        [Link]
        IFunction FrostFraction = null;
        //[Link] Function ExpansionStress = null;
        //[Link] Function CriticalNConc = null;
        //[Link] Function MaximumNConc = null;
        //[Link] Function MinimumNConc = null;
        /// <summary>The structural fraction</summary>
        [Link]
        IFunction StructuralFraction = null;
        /// <summary>The dm demand function</summary>
        [Link(IsOptional = true)]
        IFunction DMDemandFunction = null;
        //[Link] Biomass Total = null;
        //[Link] ArrayBiomass CohortArrayLive = null;
        //[Link] ArrayBiomass CohortArrayDead = null;

        /// <summary>Gets or sets the k dead.</summary>
        /// <value>The k dead.</value>
        [Description("Extinction Coefficient (Dead)")]
        public double KDead { get; set; }
        /// <summary>Gets or sets the gs maximum.</summary>
        /// <value>The gs maximum.</value>
        [Description("GsMax")]
        public double GsMax { get; set; }
        /// <summary>Gets or sets the R50.</summary>
        /// <value>The R50.</value>
        [Description("R50")]
        public double R50 { get; set; }
        /// <summary>Gets or sets the emissivity.</summary>
        /// <value>The emissivity.</value>
        [Description("Emissivity")]
        public double Emissivity { get; set; }
        /// <summary>Gets or sets the albido.</summary>
        /// <value>The albido.</value>
        [Description("Albido")]
        public double Albido { get; set; }
        
        #endregion


        #region States

        /// <summary>The leaves</summary>
        private List<LeafCohort> Leaves = new List<LeafCohort>();

        /// <summary>Initialise all state variables.</summary>
        public double CurrentExpandingLeaf = 0;
        /// <summary>The start fraction expanded</summary>
        public double StartFractionExpanded = 0;
        /// <summary>The fraction nextleaf expanded</summary>
        public double FractionNextleafExpanded = 0;
        /// <summary>The _ expanded node no</summary>
        public double _ExpandedNodeNo = 0;
        /// <summary>The dead nodes yesterday</summary>
        public double DeadNodesYesterday = 0;//Fixme This needs to be set somewhere
        #endregion

        #region Outputs
        //Note on naming convention.  
        //Variables that represent the number of units per meter of area these are called population (Popn suffix) variables 
        //Variables that represent the number of leaf cohorts (integer) in a particular state on an individual main-stem are cohort variables (CohortNo suffix)
        //Variables that represent the number of primordia or nodes (double) in a particular state on an individual mainstem are called number variables (e.g NodeNo or PrimordiaNo suffix)
        //Variables that the number of leaves on a plant or a primary bud have Plant or Primary bud prefixes

        /// <summary>Return the</summary>
        /// <value>The cohort current rank cover above.</value>
        public double CohortCurrentRankCoverAbove
        {
            get
            {
                if (CurrentRank - 1 < Leaves.Count || CurrentRank - 1 >= Leaves.Count)
                    return 0;
                else
                    return Leaves[CurrentRank-1].CoverAbove;
            }
        }


        /// <summary>Gets or sets the fraction died.</summary>
        /// <value>The fraction died.</value>
        public double FractionDied { get; set; }
        /// <summary>
        /// Gets a value indicating whether [cohorts initialised].
        /// </summary>
        /// <value><c>true</c> if [cohorts initialised]; otherwise, <c>false</c>.</value>
        public bool CohortsInitialised
        {
            get
            {
                return Leaves.Count > 0;
            }
        }

        /// <summary>The maximum cover</summary>
        [Description("Max cover")]
        [Units("max units")]
        public double MaxCover;

        /// <summary>Gets the initialised cohort no.</summary>
        /// <value>The initialised cohort no.</value>
        [Description("Number of leaf cohort objects that have been initialised")] //Note:  InitialisedCohortNo is an interger of Primordia Number, increasing every time primordia increses by one and a new cohort is initialised
        public double InitialisedCohortNo { get { return CohortCounter("IsInitialised"); } }

        /// <summary>Gets the appeared cohort no.</summary>
        /// <value>The appeared cohort no.</value>
        [Description("Number of leaf cohort that have appeared")] //Note:  AppearedCohortNo is an interger of AppearedNodeNo, increasing every time AppearedNodeNo increses by one and a new cohort is appeared
        public double AppearedCohortNo
        {
            get
            {
                int Count = CohortCounter("IsAppeared");
                if (FinalLeafAppeared)
                    return Count - (1 - FinalLeafFraction);
                else
                    return Count;
            }
        }

        /// <summary>Gets the final leaf fraction.</summary>
        /// <value>The final leaf fraction.</value>
        [Description("If last leaf has appeared, return the fraction of the final part leaf")]
        public double FinalLeafFraction
        {
            get
            {
                double FLF = 1;
                //int Count = CohortCounter("IsIniated");
                if (InitialisedCohortNo < Structure.MainStemFinalNodeNo)
                    FLF = Math.Min(Structure.MainStemFinalNodeNo - InitialisedCohortNo, 1);
                else
                    FLF = 1 - Math.Min(InitialisedCohortNo - Structure.MainStemFinalNodeNo, 1);
                return FLF;
                    
                //int Count = CohortCounter("IsAppeared");
                // DeanH: I don't think this next if statement will ever be true. Isn't MaximumNodeNumber
                // always equal to MainStemFinalNodeNo?
                //if (Count == (int)Structure.MainStemFinalNodeNo && Count < Structure.MaximumNodeNumber) 
                //    return Leaves[Count-1].FractionExpanded;
                //else
                    //return 1.0;
            }
        }

        /// <summary>
        /// Gets a value indicating whether [final leaf appeared].
        /// </summary>
        /// <value><c>true</c> if [final leaf appeared]; otherwise, <c>false</c>.</value>
        [Description("Returns true if the final leaf has appeared")]
        public bool FinalLeafAppeared
        {
            get
            {
                if (Structure.MainStemNodeNo >= Structure.MainStemFinalNodeNo)
                    return true;
                else
                    return false;
            }
        }

        /// <summary>Gets the expanding cohort no.</summary>
        /// <value>The expanding cohort no.</value>
        [Description("Number of leaf cohorts that have appeared but not yet fully expanded")]
        public double ExpandingCohortNo { get { return CohortCounter("IsGrowing"); } }

        //FIXME ExpandedNodeNo and Expanded Cohort need to be merged
        /// <summary>Gets the expanded node no.</summary>
        /// <value>The expanded node no.</value>
        [Description("Number of leaf cohorts that are fully expanded")]
        public double ExpandedNodeNo
        {
            get
            {
                //HamishB  I have had to change this back because it was not returning the correct values
                //foreach (LeafCohort L in Leaves)
                //    if (!L.IsFullyExpanded)
                //        return ExpandedCohortNo + L.FractionExpanded;
                return _ExpandedNodeNo;
            }
        }

        /// <summary>Gets the expanded cohort no.</summary>
        /// <value>The expanded cohort no.</value>
        [Description("Number of leaf cohorts that are fully expanded")]
        public double ExpandedCohortNo { get { return Math.Min(CohortCounter("IsFullyExpanded"), Structure.MainStemFinalNodeNo); } }

        /// <summary>Gets the green cohort no.</summary>
        /// <value>The green cohort no.</value>
        [Description("Number of leaf cohorts that are have expanded but not yet fully senesced")]
        public double GreenCohortNo
        {
            get
            {
                int Count = CohortCounter("IsGreen");
                if (FinalLeafAppeared)
                    return Count - (1 - FinalLeafFraction);
                else
                    return Count;
            }
        }

        /// <summary>Gets the senescing cohort no.</summary>
        /// <value>The senescing cohort no.</value>
        [Description("Number of leaf cohorts that are Senescing")]
        public double SenescingCohortNo { get { return CohortCounter("IsSenescing"); } }

        /// <summary>Gets the dead cohort no.</summary>
        /// <value>The dead cohort no.</value>
        [Description("Number of leaf cohorts that have fully Senesced")]
        public double DeadCohortNo { get { return Math.Min(CohortCounter("IsDead"), Structure.MainStemFinalNodeNo); } }

        /// <summary>Gets the plant appeared green leaf no.</summary>
        /// <value>The plant appeared green leaf no.</value>
        [Units("/plant")]
        [Description("Number of appeared leaves per plant that have appeared but not yet fully senesced on each plant")]
        public double PlantAppearedGreenLeafNo
        {
            get
            {
                double n = 0;
                foreach (LeafCohort L in Leaves)
                    if ((L.IsAppeared) && (!L.Finished))
                        n += L.CohortPopulation;
                return n / Plant.Population;
            }
        }

        /// <summary>Gets the plant appeared leaf no.</summary>
        /// <value>The plant appeared leaf no.</value>
        [Units("/plant")]
        [Description("Number of leaves per plant that have appeared")]
        public double PlantAppearedLeafNo
        {
            get
            {
                double n = 0;
                foreach (LeafCohort L in Leaves)
                    if (L.IsAppeared)
                        n += L.CohortPopulation;
                return n;
            }
        }

        //Canopy State variables


        /// <summary>Gets the lai dead.</summary>
        /// <value>The lai dead.</value>
        [Units("m^2/m^2")]
        public double LAIDead
        {
            get
            {
                double value = 0;
                foreach (LeafCohort L in Leaves)
                    value = value + L.DeadArea / 1000000;
                return value;
            }
        }

        /// <summary>Gets the cohort live.</summary>
        /// <value>The cohort live.</value>
        [XmlIgnore]
        [Units("g/m^2")]
        public Biomass CohortLive
        {
            get
            {
                Biomass Biomass = new Biomass();
                foreach (LeafCohort L in Leaves)
                    Biomass = Biomass + L.Live;
                return Biomass;
            }
            
        }

        /// <summary>Gets the cohort dead.</summary>
        /// <value>The cohort dead.</value>
        [XmlIgnore]
        [Units("g/m^2")]
        public Biomass CohortDead
        {
            get
            {
                Biomass Biomass = new Biomass();
                foreach (LeafCohort L in Leaves)
                    Biomass = Biomass + L.Dead;
                return Biomass;
            }
        }

        /// <summary>Gets the cover dead.</summary>
        /// <value>The cover dead.</value>
        [Units("0-1")]
        public double CoverDead { get { return 1.0 - Math.Exp(-KDead * LAIDead); } }

        /// <summary>Gets the RAD int tot.</summary>
        /// <value>The RAD int tot.</value>
        [Units("MJ/m^2/day")]
        [Description("This is the intercepted radiation value that is passed to the RUE class to calculate DM supply")]
        public double RadIntTot { get { return CoverGreen * MetData.Radn; } }

        /// <summary>Gets the specific area.</summary>
        /// <value>The specific area.</value>
        [Units("mm^2/g")]
        public double SpecificArea
        {
            get
            {
                if (Live.Wt > 0)
                    return LAI / Live.Wt * 1000000;
                else
                    return 0;
            }
        }
        //Cohort State variable outputs

        /// <summary>Gets the size of the cohort.</summary>
        /// <value>The size of the cohort.</value>
        [XmlIgnore]
        [Units("mm3")]
        public double[] CohortSize
        {
            get
            {
                int i = 0;

                double[] values = new double[Structure.MaximumNodeNumber];
                for (i = 0; i <= (Structure.MaximumNodeNumber - 1); i++)
                    values[i] = 0;
                i = 0;
                foreach (LeafCohort L in Leaves)
                {
                    values[i] = L.Size;
                    i++;
                }
                return values;
            }
        }

        /// <summary>Returns the area of the largest leaf.</summary>
        /// <value>The area of the largest leaf</value>
        [Units("mm2")]
        public double AreaLargestLeaf
        {
            get
            {
                double LLA = 0;
                foreach (LeafCohort L in Leaves)
                {
                    LLA = Math.Max(LLA, L.MaxArea);
                }

                return LLA;
            }
        }

        /// <summary>Gets the maximum leaf area.</summary>
        /// <value>The maximum leaf area.</value>
        [Units("mm2")]
        public double[] MaxLeafArea
        {
            get
            {
                int i = 0;

                double[] values = new double[Structure.MaximumNodeNumber];
                for (i = 0; i <= (Structure.MaximumNodeNumber - 1); i++)
                    values[i] = 0;
                i = 0;
                foreach (LeafCohort L in Leaves)
                {
                    values[i] = L.MaxArea;
                    i++;
                }

                return values;
            }
        }

        /// <summary>Gets the cohort area.</summary>
        /// <value>The cohort area.</value>
        [Units("mm2")]
        public double[] CohortArea
        {
            get
            {
                int i = 0;

                double[] values = new double[Structure.MaximumNodeNumber];
                for (i = 0; i <= (Structure.MaximumNodeNumber - 1); i++)
                    values[i] = 0;
                i = 0;
                foreach (LeafCohort L in Leaves)
                {
                    values[i] = L.LiveArea;
                    i++;
                }

                return values;
            }
        }

        /// <summary>Gets the cohort age.</summary>
        /// <value>The cohort age.</value>
        [Units("mm2")]
        public double[] CohortAge
        {
            get
            {
                int i = 0;

                double[] values = new double[Structure.MaximumNodeNumber];
                for (i = 0; i <= (Structure.MaximumNodeNumber - 1); i++)
                    values[i] = 0;
                i = 0;
                foreach (LeafCohort L in Leaves)
                {
                    values[i] = L.Age;
                    i++;
                }

                return values;
            }
        }

        /// <summary>Gets the maximum size of the cohort.</summary>
        /// <value>The maximum size of the cohort.</value>
        [Units("mm2")]
        public double[] CohortMaxSize
        {
            get
            {
                int i = 0;

                double[] values = new double[Structure.MaximumNodeNumber];
                for (i = 0; i <= (Structure.MaximumNodeNumber - 1); i++)
                    values[i] = 0;
                i = 0;
                foreach (LeafCohort L in Leaves)
                {
                    values[i] = L.MaxSize;
                    i++;
                }

                return values;
            }
        }

        /// <summary>Gets the cohort maximum area.</summary>
        /// <value>The cohort maximum area.</value>
        [Units("mm2")]
        public double[] CohortMaxArea
        {
            get
            {
                int i = 0;

                double[] values = new double[Structure.MaximumNodeNumber];
                for (i = 0; i <= (Structure.MaximumNodeNumber - 1); i++)
                    values[i] = 0;
                i = 0;
                foreach (LeafCohort L in Leaves)
                {
                    values[i] = L.MaxArea;
                    i++;
                }

                return values;
            }
        }

        /// <summary>Gets the cohort sla.</summary>
        /// <value>The cohort sla.</value>
        [Units("mm2/g")]
        public double[] CohortSLA
        {
            get
            {
                int i = 0;

                double[] values = new double[Structure.MaximumNodeNumber];
                for (i = 0; i <= (Structure.MaximumNodeNumber - 1); i++)
                    values[i] = 0;
                i = 0;
                foreach (LeafCohort L in Leaves)
                {
                    values[i] = L.SpecificArea;
                    i++;
                }

                return values;
            }
        }

        /// <summary>Gets the cohort structural frac.</summary>
        /// <value>The cohort structural frac.</value>
        [Units("0-1")]
        public double[] CohortStructuralFrac
        {
            get
            {
                int i = 0;

                double[] values = new double[Structure.MaximumNodeNumber];
                for (i = 0; i <= (Structure.MaximumNodeNumber - 1); i++)
                    values[i] = 0;
                i = 0;
                foreach (LeafCohort L in Leaves)
                {
                    if ((L.Live.StructuralWt + L.Live.MetabolicWt + L.Live.NonStructuralWt) > 0.0)
                    {
                        values[i] = L.Live.StructuralWt / (L.Live.StructuralWt + L.Live.MetabolicWt + L.Live.NonStructuralWt);
                        i++;
                    }
                    else
                    {
                        values[i] = 0;
                        i++;
                    }
                }

                return values;
            }
        }

        //General Leaf State variables

        /// <summary>Gets the live n conc.</summary>
        /// <value>The live n conc.</value>
        [Units("g/g")]
        public double LiveNConc
        {
            get
            {
                return Live.NConc;
            }
        }

        /// <summary>Gets the potential growth.</summary>
        /// <value>The potential growth.</value>
        [Units("g/m^2")]
        public double PotentialGrowth { get { return DMDemand.Structural; } }

        /// <summary>Gets the transpiration.</summary>
        /// <value>The transpiration.</value>
        [Units("mm")]
        public double Transpiration { get { return WaterAllocation; } }

        /// <summary>Gets the fw.</summary>
        /// <value>The fw.</value>
        [Units("0-1")]
        public double Fw
        {
            get
            {
                double F = 0;
                if (WaterDemand > 0)
                    F = WaterAllocation / WaterDemand;
                else
                    F = 1;
                return F;
            }
        }

        /// <summary>Gets the function.</summary>
        /// <value>The function.</value>
        [Units("0-1")]
        public double Fn
        {
            get
            {
                double F = 1;
                double FunctionalNConc = (CohortParameters.CriticalNConc.Value - (CohortParameters.MinimumNConc.Value * CohortParameters.StructuralFraction.Value)) * (1 / (1 - CohortParameters.StructuralFraction.Value));
                if (FunctionalNConc == 0)
                    F = 1;
                else
                {
                    F = Live.MetabolicNConc / FunctionalNConc;
                    F = Math.Max(0.0, Math.Min(F, 1.0));
                }
                return F;
            }
        }
        #endregion

        #region Functions
        /// <summary>1 based rank of the current leaf.</summary>
        /// <value>The current rank.</value>
        private int CurrentRank { get; set; }
        /*private int CurrentRank
        {
            get
            {
                if (Leaves.Count == 0)
                    return 0;

                // Find the first non appeared leaf.
                int i = 0;
                while (i < Leaves.Count && Leaves[i].IsAppeared)
                    i++;

                if (i == 0)
                    throw new ApsimXException(this.FullPath, "No leaves have appeared. Cannot calculate Leaf.CurrentRank");

                return Leaves[i - 1].Rank;
            }
        }*/
        /// <summary>Cohorts the counter.</summary>
        /// <param name="Condition">The condition.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        private int CohortCounter(string Condition)
        {
            int Count = 0;
            foreach (LeafCohort L in Leaves)
            {
                object o = ReflectionUtilities.GetValueOfFieldOrProperty(Condition, L);
                if (o == null)
                    throw new NotImplementedException();
                bool ok = (bool)o;
                if (ok)
                    Count++;
            }
            return Count;
        }
        /// <summary>Copies the leaves.</summary>
        /// <param name="From">From.</param>
        /// <param name="To">To.</param>
        public void CopyLeaves(LeafCohort[] From, List<LeafCohort> To)
        {
            foreach (LeafCohort Leaf in From)
                To.Add(Leaf.Clone());
        }
        /// <summary>Event from sequencer telling us to do our potential growth.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoPotentialPlantGrowth")]
        private void OnDoPotentialPlantGrowth(object sender, EventArgs e)
        {
            if (Plant.IsEmerged)
            {
                if (FrostFraction.Value > 0)
                    foreach (LeafCohort L in Leaves)
                        L.DoFrost(FrostFraction.Value);

                // On the initial day set up initial cohorts and set their properties
                if (Phenology.OnDayOf(Structure.InitialiseStage))
                    InitialiseCohorts();

                //When primordia number is 1 more than current cohort number produce a new cohort
                if (Structure.MainStemPrimordiaNo >= Leaves.Count + FinalLeafFraction)
                {
                    if (CohortsInitialised == false)
                        throw new Exception("Trying to initialse new cohorts prior to InitialStage.  Check the InitialStage parameter on the leaf object and the parameterisation of NodeInitiationRate.  Your NodeInitiationRate is triggering a new leaf cohort before leaves have been initialised.");

                    LeafCohort NewLeaf = InitialLeaves[0].Clone();
                    NewLeaf.CohortPopulation = 0;
                    NewLeaf.Age = 0;
                    NewLeaf.Rank = (int)Math.Truncate(Structure.MainStemNodeNo);
                    NewLeaf.Area = 0.0;
                    NewLeaf.DoInitialisation();
                    Leaves.Add(NewLeaf);
                }

                //When Node number is 1 more than current appeared leaf number make a new leaf appear and start growing
                double FinalFraction = 1;
                if (Structure.MainStemFinalNodeNo - AppearedCohortNo <= 1)
                    FinalFraction = FinalLeafFraction;
                if ((Structure.MainStemNodeNo >= AppearedCohortNo + FinalFraction) && (FinalFraction > 0.0))
                {

                    if (CohortsInitialised == false)
                        throw new Exception("Trying to initialse new cohorts prior to InitialStage.  Check the InitialStage parameter on the leaf object and the parameterisation of NodeAppearanceRate.  Your NodeAppearanceRate is triggering a new leaf cohort before the initial leaves have been triggered.");
                    int AppearingNode = (int)(Structure.MainStemNodeNo + (1 - FinalFraction));
                    double CohortAge = (Structure.MainStemNodeNo - AppearingNode) * Structure.MainStemNodeAppearanceRate.Value * FinalFraction;
                    if (AppearingNode > InitialisedCohortNo)
                        throw new Exception("MainStemNodeNumber exceeds the number of leaf cohorts initialised.  Check primordia parameters to make sure primordia are being initiated fast enough and for long enough");
                    int i = AppearingNode - 1;
                    Leaves[i].Rank = AppearingNode;
                    Leaves[i].CohortPopulation = Structure.TotalStemPopn;
                    Leaves[i].Age = CohortAge;
                    Leaves[i].DoAppearance(FinalFraction, CohortParameters);
                    if (NewLeaf != null)
                        NewLeaf.Invoke();
                }

                bool NextExpandingLeaf = false;
                foreach (LeafCohort L in Leaves)
                {
                    CurrentRank = L.Rank;
                    L.DoPotentialGrowth(ThermalTime.Value, CohortParameters);
                    if ((L.IsFullyExpanded == false) && (NextExpandingLeaf == false))
                    {
                        NextExpandingLeaf = true;
                        if (CurrentExpandingLeaf != L.Rank)
                        {
                            CurrentExpandingLeaf = L.Rank;
                            StartFractionExpanded = L.FractionExpanded;
                        }
                        FractionNextleafExpanded = (L.FractionExpanded - StartFractionExpanded) / (1 - StartFractionExpanded);
                    }
                }
                _ExpandedNodeNo = ExpandedCohortNo + FractionNextleafExpanded;
            }
        }
        /// <summary>Clears this instance.</summary>
        protected override void Clear()
        {
            Leaves = new List<LeafCohort>();
            WaterDemand = 0;
            WaterAllocation = 0;
        }
        /// <summary>Initialises the cohorts.</summary>
        public virtual void InitialiseCohorts() //This sets up cohorts on the day growth starts (eg at emergence)
        {
            Leaves = new List<LeafCohort>();
            CopyLeaves(InitialLeaves, Leaves);
            foreach (LeafCohort Leaf in Leaves)
            {
                if (Leaf.Area > 0)//If initial cohorts have an area set the are considered to be appeared on day of emergence so we do appearance and count up the appeared nodes on the first day
                {
                    Leaf.CohortPopulation = Structure.TotalStemPopn;

                    Leaf.DoInitialisation();
                    Structure.MainStemNodeNo += 1.0;
                    Leaf.DoAppearance(1.0, CohortParameters);
                }
                else //Leaves are primordia and have not yet emerged, initialise but do not set appeared values yet
                    Leaf.DoInitialisation();
                Structure.MainStemPrimordiaNo += 1.0;
            }
        }
        /// <summary>Does the nutrient allocations.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoActualPlantGrowth")]
        private void OnDoActualPlantGrowth(object sender, EventArgs e)
        {
           // WaterAllocation = 0;
            if (Plant.IsAlive)
            {
                foreach (LeafCohort L in Leaves)
                    L.DoActualGrowth(ThermalTime.Value, CohortParameters);

                Structure.UpdateHeight();

                //Work out what proportion of the canopy has died today.  This variable is addressed by other classes that need to perform senescence proces at the same rate as leaf senescnce
                FractionDied = 0;
                if (DeadCohortNo > 0 && GreenCohortNo > 0)
                {
                    double DeltaDeadLeaves = DeadCohortNo - DeadNodesYesterday; //Fixme.  DeadNodesYesterday is never given a value as far as I can see.
                    FractionDied = DeltaDeadLeaves / GreenCohortNo;
                }
            }
        }
        /// <summary>Zeroes the leaves.</summary>
        public virtual void ZeroLeaves()
        {
            Structure.MainStemNodeNo = 0;
            Structure.Clear();
            Leaves.Clear();
            Summary.WriteMessage(this, "Removing leaves from plant");
        }
        /// <summary>Fractional interception "above" a given node position</summary>
        /// <param name="cohortno">cohort position</param>
        /// <returns>fractional interception (0-1)</returns>
        public double CoverAboveCohort(double cohortno)
        {
            int MM2ToM2 = 1000000; // Conversion of mm2 to m2
            double LAIabove = 0;
            for (int i = Leaves.Count - 1; i > cohortno - 1; i--)
                LAIabove += Leaves[i].LiveArea / MM2ToM2;
            return 1 - Math.Exp(-ExtinctionCoeff.Value * LAIabove);
        }

        #endregion

        #region Arbitrator methods

        /// <summary>Gets or sets the dm demand.</summary>
        /// <value>The dm demand.</value>
        [Units("g/m^2")]
        public override BiomassPoolType DMDemand
        {
            get
            {
                double StructuralDemand = 0.0;
                double NonStructuralDemand = 0.0;
                double MetabolicDemand = 0.0;

                if (DMDemandFunction != null)
                {
                    StructuralDemand = DMDemandFunction.Value * StructuralFraction.Value;
                    NonStructuralDemand = DMDemandFunction.Value * (1 - StructuralFraction.Value);
                }
                else
                {
                    foreach (LeafCohort L in Leaves)
                    {
                        StructuralDemand += L.StructuralDMDemand;
                        MetabolicDemand += L.MetabolicDMDemand;
                        NonStructuralDemand += L.NonStructuralDMDemand;
                    }
                }
                return new BiomassPoolType { Structural = StructuralDemand, Metabolic = MetabolicDemand, NonStructural = NonStructuralDemand };
            }

        }
        /// <summary>Daily photosynthetic "net" supply of dry matter for the whole plant (g DM/m2/day)</summary>
        /// <value>The dm supply.</value>
        [Units("g/m^2")]
        public override BiomassSupplyType DMSupply
        {
            get
            {
                double Retranslocation = 0;
                double Reallocation = 0;

                foreach (LeafCohort L in Leaves)
                {
                    Retranslocation += L.LeafStartDMRetranslocationSupply;
                    Reallocation += L.LeafStartDMReallocationSupply;
                }


                return new BiomassSupplyType { Fixation = Photosynthesis.Growth(RadIntTot), Retranslocation = Retranslocation, Reallocation = Reallocation };
            }
        }
        /// <summary>Sets the dm potential allocation.</summary>
        /// <value>The dm potential allocation.</value>
        /// <exception cref="System.Exception">
        /// Invalid allocation of potential DM in + Name
        /// or
        /// the sum of poteitial DM allocation to leaf cohorts is more that that allocated to leaf organ
        /// or
        /// Invalid allocation of potential DM in + Name
        /// or
        /// the sum of poteitial DM allocation to leaf cohorts is more that that allocated to leaf organ
        /// </exception>
        [Units("g/m^2")]
        public override BiomassPoolType DMPotentialAllocation
        {
            set
            {
                //Allocate Potential Structural DM
                if (DMDemand.Structural == 0)
                    if (value.Structural < 0.000000000001) { }//All OK
                    else
                        throw new Exception("Invalid allocation of potential DM in" + Name);

                double[] CohortPotentialStructualDMAllocation = new double[Leaves.Count + 2];

                if (value.Structural == 0.0)
                { }// do nothing
                else
                {
                    double DMPotentialsupply = value.Structural;
                    double DMPotentialallocated = 0;
                    double TotalPotentialDemand = 0;
                    foreach (LeafCohort L in Leaves)
                        TotalPotentialDemand += L.StructuralDMDemand;
                    int i = 0;
                    foreach (LeafCohort L in Leaves)
                    {
                        i++;
                        double fraction = L.StructuralDMDemand / TotalPotentialDemand;
                        double PotentialAllocation = Math.Min(L.StructuralDMDemand, DMPotentialsupply * fraction);
                        CohortPotentialStructualDMAllocation[i] = PotentialAllocation;
                        DMPotentialallocated += PotentialAllocation;
                    }
                    if ((DMPotentialallocated - value.Structural) > 0.000000001)
                        throw new Exception("the sum of poteitial DM allocation to leaf cohorts is more that that allocated to leaf organ");
                }

                //Allocate Metabolic DM
                if (DMDemand.Metabolic == 0)
                    if (value.Metabolic < 0.000000000001) { }//All OK
                    else
                        throw new Exception("Invalid allocation of potential DM in" + Name);

                double[] CohortPotentialMetabolicDMAllocation = new double[Leaves.Count + 2];

                if (value.Metabolic == 0.0)
                { }// do nothing
                else
                {
                    double DMPotentialsupply = value.Metabolic;
                    double DMPotentialallocated = 0;
                    double TotalPotentialDemand = 0;
                    foreach (LeafCohort L in Leaves)
                        TotalPotentialDemand += L.MetabolicDMDemand;
                    int i = 0;
                    foreach (LeafCohort L in Leaves)
                    {
                        i++;
                        double fraction = L.MetabolicDMDemand / TotalPotentialDemand;
                        double PotentialAllocation = Math.Min(L.MetabolicDMDemand, DMPotentialsupply * fraction);
                        CohortPotentialMetabolicDMAllocation[i] = PotentialAllocation;
                        DMPotentialallocated += PotentialAllocation;
                    }
                    if ((DMPotentialallocated - value.Metabolic) > 0.000000001)
                        throw new Exception("the sum of poteitial DM allocation to leaf cohorts is more that that allocated to leaf organ");
                }

                //Send allocations to cohorts
                int a = 0;
                foreach (LeafCohort L in Leaves)
                {
                    a++;
                    L.DMPotentialAllocation = new BiomassPoolType
                    {
                        Structural = CohortPotentialStructualDMAllocation[a],
                        Metabolic = CohortPotentialMetabolicDMAllocation[a],
                    };
                }
            }
        }
        /// <summary>Sets the dm allocation.</summary>
        /// <value>The dm allocation.</value>
        /// <exception cref="System.Exception">
        /// Invalid allocation of DM in Leaf
        /// or
        /// DM allocated to Leaf left over after allocation to leaf cohorts
        /// or
        /// the sum of DM allocation to leaf cohorts is more that that allocated to leaf organ
        /// or
        /// Invalid allocation of DM in Leaf
        /// or
        /// Metabolic DM allocated to Leaf left over after allocation to leaf cohorts
        /// or
        /// the sum of Metabolic DM allocation to leaf cohorts is more that that allocated to leaf organ
        /// or
        /// or
        /// or
        /// or
        /// or
        /// </exception>
        [Units("g/m^2")]
        public override BiomassAllocationType DMAllocation
        {
            set
            {
                double[] StructuralDMAllocationCohort = new double[Leaves.Count + 2];

                double check = Live.StructuralWt;
                //Structural DM allocation
                if (DMDemand.Structural == 0)
                    if (value.Structural < 0.000000000001) { }//All OK
                    else
                        throw new Exception("Invalid allocation of DM in Leaf");
                if (value.Structural == 0.0)
                { }// do nothing
                else
                {
                    double DMsupply = value.Structural;
                    double DMallocated = 0;
                    double TotalDemand = 0;
                    foreach (LeafCohort L in Leaves)
                        TotalDemand += L.StructuralDMDemand;
                    double DemandFraction = (value.Structural) / TotalDemand;//
                    int i = 0;
                    foreach (LeafCohort L in Leaves)
                    {
                        i++;
                        double Allocation = Math.Min(L.StructuralDMDemand * DemandFraction, DMsupply);
                        StructuralDMAllocationCohort[i] = Allocation;
                        DMallocated += Allocation;
                        DMsupply -= Allocation;
                    }
                    if (DMsupply > 0.0000000001)
                        throw new Exception("DM allocated to Leaf left over after allocation to leaf cohorts");
                    if ((DMallocated - value.Structural) > 0.000000001)
                        throw new Exception("the sum of DM allocation to leaf cohorts is more that that allocated to leaf organ");
                }

                //Metabolic DM allocation
                double[] MetabolicDMAllocationCohort = new double[Leaves.Count + 2];

                if (DMDemand.Metabolic == 0)
                    if (value.Metabolic < 0.000000000001) { }//All OK
                    else
                        throw new Exception("Invalid allocation of DM in Leaf");
                if (value.Metabolic == 0.0)
                { }// do nothing
                else
                {
                    double DMsupply = value.Metabolic;
                    double DMallocated = 0;
                    double TotalDemand = 0;
                    foreach (LeafCohort L in Leaves)
                        TotalDemand += L.MetabolicDMDemand;
                    double DemandFraction = (value.Metabolic) / TotalDemand;//
                    int i = 0;
                    foreach (LeafCohort L in Leaves)
                    {
                        i++;
                        double Allocation = Math.Min(L.MetabolicDMDemand * DemandFraction, DMsupply);
                        MetabolicDMAllocationCohort[i] = Allocation;
                        DMallocated += Allocation;
                        DMsupply -= Allocation;
                    }
                    if (DMsupply > 0.0000000001)
                        throw new Exception("Metabolic DM allocated to Leaf left over after allocation to leaf cohorts");
                    if ((DMallocated - value.Metabolic) > 0.000000001)
                        throw new Exception("the sum of Metabolic DM allocation to leaf cohorts is more that that allocated to leaf organ");
                }

                // excess allocation
                double[] NonStructuralDMAllocationCohort = new double[Leaves.Count + 2];
                double TotalSinkCapacity = 0;
                foreach (LeafCohort L in Leaves)
                    TotalSinkCapacity += L.NonStructuralDMDemand;
                if (value.NonStructural > TotalSinkCapacity)
                //Fixme, this exception needs to be turned on again
                { }
                    //throw new Exception("Allocating more excess DM to Leaves then they are capable of storing");
                if (TotalSinkCapacity > 0.0)
                {
                    double SinkFraction = value.NonStructural / TotalSinkCapacity;
                    int i = 0;
                    foreach (LeafCohort L in Leaves)
                    {
                        i++;
                        double Allocation = Math.Min(L.NonStructuralDMDemand * SinkFraction, value.NonStructural);
                        NonStructuralDMAllocationCohort[i] = Allocation;
                    }
                }

                // retranslocation
                double[] DMRetranslocationCohort = new double[Leaves.Count + 2];

                if (value.Retranslocation - DMSupply.Retranslocation > 0.0000000001)
                    throw new Exception(Name + " cannot supply that amount for DM retranslocation");
                if (value.Retranslocation > 0)
                {
                    double remainder = value.Retranslocation;
                    int i = 0;
                    foreach (LeafCohort L in Leaves)
                    {
                        i++;
                        double Supply = Math.Min(remainder, L.DMRetranslocationSupply);
                        DMRetranslocationCohort[i] = Supply;
                        remainder -= Supply;
                    }
                    if (remainder > 0.0000000001)
                        throw new Exception(Name + " DM Retranslocation demand left over after processing.");
                }

                // Reallocation
                double[] DMReAllocationCohort = new double[Leaves.Count + 2];
                if (value.Reallocation - DMSupply.Reallocation > 0.000000001)
                    throw new Exception(Name + " cannot supply that amount for DM Reallocation");
                if (value.Reallocation < -0.000000001)
                    throw new Exception(Name + " recieved -ve DM reallocation");
                if (value.Reallocation > 0)
                {
                    double remainder = value.Reallocation;
                    int i = 0;
                    foreach (LeafCohort L in Leaves)
                    {
                        i++;
                        double ReAlloc = Math.Min(remainder, L.LeafStartDMReallocationSupply);
                        remainder = Math.Max(0.0, remainder - ReAlloc);
                        DMReAllocationCohort[i] = ReAlloc;
                    }
                    if (!MathUtilities.FloatsAreEqual(remainder, 0.0))
                        throw new Exception(Name + " DM Reallocation demand left over after processing.");
                }

                //Send allocations to cohorts
                int a = 0;
                foreach (LeafCohort L in Leaves)
                {
                    a++;
                    L.DMAllocation = new BiomassAllocationType
                    {
                        Structural = StructuralDMAllocationCohort[a],
                        Metabolic = MetabolicDMAllocationCohort[a],
                        NonStructural = NonStructuralDMAllocationCohort[a],
                        Retranslocation = DMRetranslocationCohort[a],
                        Reallocation = DMReAllocationCohort[a],
                    };
                }
            }
        }
        /// <summary>Gets or sets the water demand.</summary>
        /// <value>The water demand.</value>
        [XmlIgnore]
        [Units("mm")]
        public override double WaterDemand
        {
           get
            {
                return PotentialEP;
            }
        }
        /// <summary>Gets or sets the water allocation.</summary>
        /// <value>The water allocation.</value>
        [XmlIgnore]
        public override double WaterAllocation { get; set;}

        /// <summary>Gets or sets the n demand.</summary>
        /// <value>The n demand.</value>
        [Units("g/m^2")]
        public override BiomassPoolType NDemand
        {
            get
            {
                double StructuralDemand = 0.0;
                double MetabolicDemand = 0.0;
                double NonStructuralDemand = 0.0;
                foreach (LeafCohort L in Leaves)
                {
                    StructuralDemand += L.StructuralNDemand;
                    MetabolicDemand += L.MetabolicNDemand;
                    NonStructuralDemand += L.NonStructuralNDemand;
                }
                return new BiomassPoolType { Structural = StructuralDemand, Metabolic = MetabolicDemand, NonStructural = NonStructuralDemand };
            }
        }
        /// <summary>Sets the n allocation.</summary>
        /// <value>The n allocation.</value>
        /// <exception cref="System.Exception">
        /// Invalid allocation of N
        /// or
        /// or
        /// or
        /// or
        /// or
        /// or
        /// </exception>
        [Units("g/m^2")]
        public override BiomassAllocationType NAllocation
        {
            set
            {

                if (NDemand.Structural == 0)
                    if (value.Structural == 0) { }//All OK  FIXME this needs to be seperated into compoents
                    else
                        throw new Exception("Invalid allocation of N");

                double[] StructuralNAllocationCohort = new double[Leaves.Count + 2];
                double[] MetabolicNAllocationCohort = new double[Leaves.Count + 2];
                double[] NonStructuralNAllocationCohort = new double[Leaves.Count + 2];
                double[] NReallocationCohort = new double[Leaves.Count + 2];
                double[] NRetranslocationCohort = new double[Leaves.Count + 2];
                if ((value.Structural + value.Metabolic + value.NonStructural) == 0.0)
                { }// do nothing
                else
                {


                    //setup allocation variables
                    double[] CohortNAllocation = new double[Leaves.Count + 2];
                    double[] StructuralNDemand = new double[Leaves.Count + 2];
                    double[] MetabolicNDemand = new double[Leaves.Count + 2];
                    double[] NonStructuralNDemand = new double[Leaves.Count + 2];
                    double TotalStructuralNDemand = 0;
                    double TotalMetabolicNDemand = 0;
                    double TotalNonStructuralNDemand = 0;

                    int i = 0;
                    foreach (LeafCohort L in Leaves)
                    {
                        {
                            i++;
                            CohortNAllocation[i] = 0;
                            StructuralNDemand[i] = L.StructuralNDemand;
                            TotalStructuralNDemand += L.StructuralNDemand;
                            MetabolicNDemand[i] = L.MetabolicNDemand;
                            TotalMetabolicNDemand += L.MetabolicNDemand;
                            NonStructuralNDemand[i] = L.NonStructuralNDemand;
                            TotalNonStructuralNDemand += L.NonStructuralNDemand;
                        }
                    }
                    double NSupplyValue = value.Structural;
                    //double LeafNAllocated = 0;

                    // first make sure each cohort gets the structural N requirement for growth (includes MinNconc for structural growth and MinNconc for nonstructural growth)
                    if ((NSupplyValue > 0) & (TotalStructuralNDemand > 0))
                    {
                        i = 0;
                        foreach (LeafCohort L in Leaves)
                        {
                            i++;
                            //double allocation = 0;
                            //allocation = Math.Min(StructuralNDemand[i], NSupplyValue * (StructuralNDemand[i] / TotalStructuralNDemand));
                            StructuralNAllocationCohort[i] = Math.Min(StructuralNDemand[i], NSupplyValue * (StructuralNDemand[i] / TotalStructuralNDemand));
                            //LeafNAllocated += allocation;
                        }

                    }
                    // then allocate additional N relative to leaves metabolic demands
                    NSupplyValue = value.Metabolic;
                    if ((NSupplyValue > 0) & (TotalMetabolicNDemand > 0))
                    {
                        i = 0;
                        foreach (LeafCohort L in Leaves)
                        {
                            i++;
                            //double allocation = 0;
                            //allocation = Math.Min(MetabolicNDemand[i], NSupplyValue * (MetabolicNDemand[i] / TotalMetabolicNDemand));
                            MetabolicNAllocationCohort[i] = Math.Min(MetabolicNDemand[i], NSupplyValue * (MetabolicNDemand[i] / TotalMetabolicNDemand));
                            //LeafNAllocated += allocation;
                        }
                    }
                    // then allocate excess N relative to leaves N sink capacity
                    NSupplyValue = value.NonStructural;
                    if ((NSupplyValue > 0) & (TotalNonStructuralNDemand > 0))
                    {
                        i = 0;
                        foreach (LeafCohort L in Leaves)
                        {
                            i++;
                            //double allocation = 0;
                            //allocation = Math.Min(NonStructuralNDemand[i], NSupplyValue * (NonStructuralNDemand[i] / TotalNonStructuralNDemand));
                            NonStructuralNAllocationCohort[i] += Math.Min(NonStructuralNDemand[i], NSupplyValue * (NonStructuralNDemand[i] / TotalNonStructuralNDemand));
                            //LeafNAllocated += allocation;
                        }
                        //NSupplyValue = value.Structural - LeafNAllocated;
                    }

                    //if (NSupplyValue > 0.0000000001)
                    //    throw new Exception("N allocated to Leaf left over after allocation to leaf cohorts");
                    //if ((LeafNAllocated - value.Structural) > 0.000000001)
                    //    throw new Exception("the sum of N allocation to leaf cohorts is more that that allocated to leaf organ");

                    //send N allocations to each cohort
                    //i = 0;
                    //foreach (LeafCohort L in Leaves)
                    //{
                    //    i++;
                    //    L.NAllocation = CohortNAllocation[i];
                    //}
                }

                // Retranslocation
                if (value.Retranslocation - NSupply.Retranslocation > 0.000000001)
                    throw new Exception(Name + " cannot supply that amount for N retranslocation");
                if (value.Retranslocation < -0.000000001)
                    throw new Exception(Name + " recieved -ve N retranslocation");
                if (value.Retranslocation > 0)
                {
                    int i = 0;
                    double remainder = value.Retranslocation;
                    foreach (LeafCohort L in Leaves)
                    {
                        i++;
                        double Retrans = Math.Min(remainder, L.LeafStartNRetranslocationSupply);
                        //L.NRetranslocation = Retrans;
                        NRetranslocationCohort[i] = Retrans;
                        remainder = Math.Max(0.0, remainder - Retrans);
                    }
                    if (!MathUtilities.FloatsAreEqual(remainder, 0.0))
                        throw new Exception(Name + " N Retranslocation demand left over after processing.");
                }

                // Reallocation
                if (value.Reallocation - NSupply.Reallocation > 0.000000001)
                    throw new Exception(Name + " cannot supply that amount for N Reallocation");
                if (value.Reallocation < -0.000000001)
                    throw new Exception(Name + " recieved -ve N reallocation");
                if (value.Reallocation > 0)
                {
                    int i = 0;
                    double remainder = value.Reallocation;
                    foreach (LeafCohort L in Leaves)
                    {
                        i++;
                        double ReAlloc = Math.Min(remainder, L.LeafStartNReallocationSupply);
                        //L.NReallocation = ReAlloc;
                        NReallocationCohort[i] = ReAlloc;
                        remainder = Math.Max(0.0, remainder - ReAlloc);
                    }
                    if (!MathUtilities.FloatsAreEqual(remainder, 0.0))
                        throw new Exception(Name + " N Reallocation demand left over after processing.");
                }

                //Send allocations to cohorts
                int a = 0;
                foreach (LeafCohort L in Leaves)
                {
                    a++;
                    L.NAllocation = new BiomassAllocationType
                    {
                        Structural = StructuralNAllocationCohort[a],
                        Metabolic = MetabolicNAllocationCohort[a],
                        NonStructural = NonStructuralNAllocationCohort[a],
                        Retranslocation = NRetranslocationCohort[a],
                        Reallocation = NReallocationCohort[a],
                    };
                }
            }
        }
        /// <summary>Gets or sets the n supply.</summary>
        /// <value>The n supply.</value>
        [Units("g/m^2")]
        public override BiomassSupplyType NSupply
        {
            get
            {
                double RetransSupply = 0;
                double ReallocationSupply = 0;
                foreach (LeafCohort L in Leaves)
                {
                    RetransSupply += Math.Max(0, L.LeafStartNRetranslocationSupply);
                    ReallocationSupply += L.LeafStartNReallocationSupply;
                }

                return new BiomassSupplyType { Retranslocation = RetransSupply, Reallocation = ReallocationSupply };
            }
        }

        /// <summary>Gets or sets the maximum nconc.</summary>
        /// <value>The maximum nconc.</value>
        public override double MaxNconc
        {
            get
            {
                return CohortParameters.MaximumNConc.Value;
            }
        }
        /// <summary>Gets or sets the minimum nconc.</summary>
        /// <value>The minimum nconc.</value>
        public override double MinNconc
        {
            get
            {
                return CohortParameters.CriticalNConc.Value;
            }
        }
        #endregion

        #region Event handlers and publishers

        /// <summary>Occurs when [new leaf].</summary>
        public event NullTypeDelegate NewLeaf;

        /// <summary>Called when [prune].</summary>
        /// <param name="Prune">The prune.</param>
        [EventSubscribe("Prune")]
        private void OnPrune(PruneType Prune)
        {
            Structure.PrimaryBudNo = Prune.BudNumber;
            ZeroLeaves();
        }

        /// <summary>Called when [remove lowest leaf].</summary>
        [EventSubscribe("RemoveLowestLeaf")]
        private void OnRemoveLowestLeaf()
        {
            Summary.WriteMessage(this, "Removing lowest Leaf");
            Leaves.RemoveAt(0);
        }


        /// <summary>Called when crop is ending</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="data">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantSowing")]
        private void OnPlantSowing(object sender, SowPlant2Type data)
        {
            if (data.Plant == Plant)
            {
                Clear();
                if (data.MaxCover <= 0.0)
                    throw new Exception("MaxCover must exceed zero in a Sow event.");
                MaxCover = data.MaxCover;
            }
        }

        /// <summary>Called when [kill leaf].</summary>
        /// <param name="KillLeaf">The kill leaf.</param>
        [EventSubscribe("KillLeaf")]
        private void OnKillLeaf(KillLeafType KillLeaf)
        {
            Summary.WriteMessage(this, "Killing " + KillLeaf.KillFraction + " of leaves on plant");

            foreach (LeafCohort L in Leaves)
                L.DoKill(KillLeaf.KillFraction);

        }

        /// <summary>Called when crop is being cut.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Cutting")]
        private void OnCutting(object sender, EventArgs e)
        {
            if (sender == Plant)
            {
                Summary.WriteMessage(this, "Cutting " + Name + " from " + Plant.Name);

                if (TotalDM > 0)
                    SurfaceOrganicMatter.Add(TotalDM * 10, TotalN * 10, 0, Plant.CropType, Name);

                Structure.MainStemNodeNo = 0;
                Structure.Clear();
                Live.Clear();
                Dead.Clear();
                Leaves.Clear();
                Structure.ResetStemPopn();
                InitialiseCohorts();
            }
        }

        /// <summary>Called when [simulation commencing].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            List<LeafCohort> initialLeaves = new List<LeafCohort>();
            foreach (LeafCohort initialLeaf in Apsim.Children(this, typeof(LeafCohort)))
                initialLeaves.Add(initialLeaf);
            InitialLeaves = initialLeaves.ToArray();
        }

        /// <summary>Called when crop is ending</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantEnding")]
        private void OnPlantEnding(object sender, EventArgs e)
        {
            if (sender == Plant)
            {
                if (TotalDM > 0)
                    SurfaceOrganicMatter.Add(TotalDM * 10, TotalN * 10, 0, Plant.CropType, Name);
                Clear();
            }
        }
        #endregion


    }
}
