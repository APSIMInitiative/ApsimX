using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using APSIM.Shared.Utilities;

namespace Models.PMF
{
    /// <summary>Contains the variables need for arbitration</summary>
    [Serializable]
    public class BiomassArbitrationType
    {
        //Biomass Demand Variables
        /// <summary>Gets or sets the structural demand.</summary>
        /// <value>Demand for structural biomass from each organ</value>
        public double[] StructuralDemand { get; set; }
        /// <summary>Gets or sets the total structural demand.</summary>
        /// <value>Demand for structural biomass from the crop</value>
        public double TotalStructuralDemand { get { return MathUtilities.Sum(StructuralDemand); } }
        /// <summary>Gets or sets the metabolic demand.</summary>
        /// <value>Demand for metabolic biomass from each organ</value>
        public double[] MetabolicDemand { get; set; }
        /// <summary>Gets or sets the total metabolic demand.</summary>
        /// <value>Demand for metabolic biomass from the crop</value>
        public double TotalMetabolicDemand { get { return MathUtilities.Sum(MetabolicDemand); } }
        /// <summary>Gets or sets the non structural demand.</summary>
        /// <value>Demand for non-structural biomass from each organ</value>
        public double[] NonStructuralDemand { get; set; }
        /// <summary>Gets or sets the total non structural demand.</summary>
        /// <value>Demand for non-structural biomass from the crop</value>
        public double TotalNonStructuralDemand { get { return MathUtilities.Sum(NonStructuralDemand); } }
        /// <summary>Gets or sets the total crop demand.</summary>
        /// <value>crop demand for biomass, structural, non-sturctural and metabolic</value>
        public double TotalPlantDemand { get { return TotalStructuralDemand + TotalMetabolicDemand + TotalNonStructuralDemand; } }
        //Biomass Supply Variables
        /// <summary>Gets or sets the reallocation supply.</summary>
        /// <value>Biomass available for reallocation for each organ as it dies</value>
        public double[] ReallocationSupply { get; set; }
        /// <summary>Gets or sets the total reallocation supply.</summary>
        /// <value>Biomass available for reallocation from the entire crop</value>
        public double TotalReallocationSupply { get { return MathUtilities.Sum(ReallocationSupply); } }
        /// <summary>Gets or sets the uptake supply.</summary>
        /// <value>Biomass available for uptake from each absorbing organ, generally limited to ntrient uptake in roots</value>
        public double[] UptakeSupply { get; set; }
        /// <summary>Gets or sets the total uptake supply.</summary>
        /// <value>Biomass available for uptake by the crop</value>
        public double TotalUptakeSupply { get { return MathUtilities.Sum(UptakeSupply); } }
        /// <summary>Gets or sets the fixation supply.</summary>
        /// <value>Biomass that may be fixed by the crop, eg DM fixed by photosynhesis in the leaves of N fixed by nodules</value>
        public double[] FixationSupply { get; set; }
        /// <summary>Gets or sets the total fixation supply.</summary>
        /// <value>Total fixation by the crop</value>
        public double TotalFixationSupply { get { return MathUtilities.Sum(FixationSupply); } }
        /// <summary>Gets or sets the retranslocation supply.</summary>
        /// <value>Supply of labile biomass that can be retranslocated from each oragn</value>
        public double[] RetranslocationSupply { get; set; }
        /// <summary>Gets or sets the total retranslocation supply.</summary>
        /// <value>The total supply of labile biomass in the crop</value>
        public double TotalRetranslocationSupply { get { return MathUtilities.Sum(RetranslocationSupply); } }
        /// <summary>Gets or sets the total crop supply.</summary>
        /// <value>crop supply from uptake, fixation, reallocation and remobilisation</value>
        public double TotalPlantSupply { get { return TotalReallocationSupply + TotalUptakeSupply + TotalFixationSupply + TotalRetranslocationSupply; } }
        //Biomass Allocation Variables
        /// <summary>Gets or sets the reallocation.</summary>
        /// <value>The amount of biomass reallocated from each organ as it dies</value>
        public double[] Reallocation { get; set; }
        /// <summary>Gets or sets the total reallocation.</summary>
        /// <value>The total amount of biomass reallocated by the crop</value>
        public double TotalReallocation { get; set; }
        /// <summary>Gets or sets the uptake.</summary>
        /// <value>The actual uptake of biomass by each organ, generally limited to nutrients in the roots</value>
        public double[] Uptake { get; set; }
        /// <summary>Gets or sets the fixation.</summary>
        /// <value>The actual uptake of biomass by the whole crop</value>
        public double[] Fixation { get; set; }
        /// <summary>Gets or sets the retranslocation.</summary>
        /// <value>The actual retranslocation or biomass from each oragan</value>
        public double[] Retranslocation { get; set; }
        /// <summary>Gets or sets the total retranslocation.</summary>
        /// <value>The total amount of biomass retranslocated by the crop</value>
        public double TotalRetranslocation { get; set; }
        /// <summary>Gets or sets the respiration.</summary>
        /// <value>The amount of biomass respired by each organ</value>
        public double[] Respiration { get; set; }
        /// <summary>Gets or sets the total respiration.</summary>
        /// <value>Total respiration by the crop</value>
        public double TotalRespiration { get; set; }
        /// <summary>Gets or sets the constrained growth.</summary>
        /// <value>Biomass growth that is possible given nutrient availability and minimum N concentratins of organs</value>
        public double[] ConstrainedGrowth { get; set; }
        /// <summary>Gets or sets the structural allocation.</summary>
        /// <value>The actual amount of structural biomass allocated to each organ</value>
        public double[] StructuralAllocation { get; set; }
        /// <summary>Gets or sets the total structural allocation.</summary>
        /// <value>The total structural biomass allocation to the whole crop</value>
        public double TotalStructuralAllocation { get { return MathUtilities.Sum(StructuralAllocation); } }
        /// <summary>Gets or sets the metabolic allocation.</summary>
        /// <value>The actual meatabilic biomass allocation to each organ</value>
        public double[] MetabolicAllocation { get; set; }
        /// <summary>Gets or sets the total metabolic allocation.</summary>
        /// <value>The metabolic biomass allocation to each organ</value>
        public double TotalMetabolicAllocation { get { return MathUtilities.Sum(MetabolicAllocation); } }
        /// <summary>Gets or sets the non structural allocation.</summary>
        /// <value>The actual non-structural biomass allocation to each organ</value>
        public double[] NonStructuralAllocation { get; set; }
        /// <summary>Gets or sets the total non structural allocation.</summary>
        /// <value>The total non-structural allocationed to the crop</value>
        public double TotalNonStructuralAllocation { get { return MathUtilities.Sum(NonStructuralAllocation); } }
        /// <summary>Gets or sets the total allocation.</summary>
        /// <value>The actual biomass allocation to each organ, structural, non-structural and metabolic</value>
        public double[] TotalAllocation { get; set; }
        /// <summary>Gets or sets the total allocated.</summary>
        /// <value>The amount of biomass allocated to the whole crop</value>
        public double Allocated { get; set; }
        /// <summary>Gets or sets the not allocated.</summary>
        /// <value>The biomass available that was not allocated.</value>
        public double NotAllocated { get; set; }
        /// <summary>Gets or sets the sink limitation.</summary>
        /// <value>The amount of biomass that could have been assimilated but was not because the demand from organs was insufficient.</value>
        public double SinkLimitation { get; set; }
        /// <summary>Gets or sets the limitation due to nutrient shortage</summary>
        /// <value>The amount of biomass that could have been assimilated but was not becasue nutrient supply was insufficient to meet organs minimunn N concentrations</value>
        public double NutrientLimitation { get; set; }
        //Error checking variables
        /// <summary>Gets or sets the start.</summary>
        /// <value>The start.</value>
        public double Start { get; set; }
        /// <summary>Gets or sets the end.</summary>
        /// <value>The end.</value>
        public double End { get; set; }
        /// <summary>Gets or sets the balance error.</summary>
        /// <value>The balance error.</value>
        public double BalanceError { get; set; }
        /// <summary>the type of biomass being arbitrated</summary>
        /// <value>The balance error.</value>
        public string BiomassType { get; set; }

        //Constructor for Array variables
        /// <summary>Initializes a new instance of the <see cref="BiomassArbitrationType"/> class.</summary>
        public BiomassArbitrationType()
        { }

        /// <summary>Initializes a new instance of the <see cref="BiomassArbitrationType"/> class.</summary>
        /// <param name="Size">The size.</param>
        public BiomassArbitrationType(int Size)
        {
            StructuralDemand = new double[Size];
            MetabolicDemand = new double[Size];
            NonStructuralDemand = new double[Size];
            ReallocationSupply = new double[Size];
            UptakeSupply = new double[Size];
            FixationSupply = new double[Size];
            RetranslocationSupply = new double[Size];
            Reallocation = new double[Size];
            Uptake = new double[Size];
            Fixation = new double[Size];
            Retranslocation = new double[Size];
            Respiration = new double[Size];
            ConstrainedGrowth = new double[Size];
            StructuralAllocation = new double[Size];
            MetabolicAllocation = new double[Size];
            NonStructuralAllocation = new double[Size];
            TotalAllocation = new double[Size];
        }
    }
}
