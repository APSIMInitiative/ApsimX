using System;
using System.Collections.Generic;
using APSIM.Shared.Documentation;
using Models.Core;
using Models.Functions;

namespace Models.PMF.Organs
{
    /// <summary>
    /// Leaf cohort parameters.
    /// </summary>
    [Serializable]
    public class LeafCohortParameters : Model
    {
        /// <summary>The maximum area</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("mm2")]
        public IFunction MaxArea = null;
        /// <summary>The growth duration</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("deg day")]
        public IFunction GrowthDuration = null;
        /// <summary>The lag duration</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("deg day")]
        public IFunction LagDuration = null;
        /// <summary>The senescence duration</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("deg day")]
        public IFunction SenescenceDuration = null;
        /// <summary>The detachment lag duration</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("deg day")]
        public IFunction DetachmentLagDuration = null;
        /// <summary>The detachment duration</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("deg day")]
        public IFunction DetachmentDuration = null;
        /// <summary>The specific leaf area maximum</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction SpecificLeafAreaMax = null;
        /// <summary>The specific leaf area minimum</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction SpecificLeafAreaMin = null;
        /// <summary>The structural fraction</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction StructuralFraction = null;
        /// <summary>The maximum n conc</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction MaximumNConc = null;
        /// <summary>The minimum n conc</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction MinimumNConc = null;
        /// <summary>The initial n conc</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction InitialNConc = null;
        /// <summary>The n reallocation factor</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction NReallocationFactor = null;
        /// <summary>The dm reallocation factor</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction DMReallocationFactor = null;
        /// <summary>The n retranslocation factor</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction NRetranslocationFactor = null;
        /// <summary>The expansion stress</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction ExpansionStress = null;

        /// <summary>The expansion stress</summary>
        public double ExpansionStressValue { get; set; }
        /// <summary>The CellDivisionStressValue</summary>
        public double CellDivisionStressValue { get; set; }
        /// <summary>The LagAccelerationValue</summary>
        public double LagAccelerationValue { get; set; }
        /// <summary>The SenescenceAccelerationValue</summary>
        public double SenescenceAccelerationValue { get; set; }
        /// <summary>The ShadeInducedSenescenceRateValue</summary>
        public double ShadeInducedSenescenceRateValue { get; set; }
        /// <summary>The SenessingLeafRelativeSizeValue</summary>
        public double SenessingLeafRelativeSizeValue { get; set; }


        /// <summary>The critical n conc</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction CriticalNConc = null;
        /// <summary>The dm retranslocation factor</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction DMRetranslocationFactor = null;
        /// <summary>The shade induced senescence rate</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction ShadeInducedSenescenceRate = null;
        /// <summary>The stress induced reduction of lag phase through acceleration of tt accumulation by the cohort during this phase</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction LagAcceleration = null;
        /// <summary>The stress induced reduction of senescence phase through acceleration of tt accumulation by the cohort during this phase</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction SenescenceAcceleration = null;
        /// <summary>The non structural fraction</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction StorageFraction = null;
        /// <summary>The cell division stress</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction CellDivisionStress = null;
        /// <summary>The Shape of the sigmoidal function of leaf area increase</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction LeafSizeShapeParameter = null;
        /// <summary>The size of leaves on senessing tillers relative to the dominant tillers in that cohort</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction SenessingLeafRelativeSize = null;
        /// <summary>The proportion of mass that is respired each day</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction MaintenanceRespirationFunction = null;
        /// <summary>Modify leaf size by age</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction LeafSizeAgeMultiplier = null;
        /// <summary>Modify lag duration by age</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction LagDurationAgeMultiplier = null;
        /// <summary>Modify senescence duration by age</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction SenescenceDurationAgeMultiplier = null;
        /// <summary>The cost for remobilisation</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction RemobilisationCost = null;




        /// <summary>Document this model.</summary>
        public override IEnumerable<ITag> Document()
        {
            var laiTags = new List<ITag>();
            laiTags.Add(new Paragraph("Leaf area index is calculated as the sum of the area of each cohort of leaves. " +
                                      "The appearance of a new cohort of leaves occurs each time Structure.LeafTipsAppeared increases by one. " +
                                      "From tip appearance the area of each cohort will increase for a certian number of degree days defined by the *GrowthDuration*"));
            laiTags.AddRange(GrowthDuration.Document());

            laiTags.Add(new Paragraph("If no stress occurs the leaves will reach a Maximum area (*MaxArea*) at the end of the *GrowthDuration*. " +
                                      "The *MaxArea* is defined by: "));
            laiTags.AddRange(MaxArea.Document());
            laiTags.Add(new Paragraph("In the absence of stress the leaf will remain at *MaxArea* for a number of degree days " +
                                      "set by the *LagDuration* and then area will senesce to zero at the end of the *SenescenceDuration*"));
            laiTags.AddRange(LagDuration.Document());
            laiTags.AddRange(SenescenceDuration.Document());
            laiTags.Add(new Paragraph("Mutual shading can cause premature senescence of cohorts if the leaf area above them becomes too great. Each cohort models the proportion of its area that is lost to shade induced senescence each day as:"));
            laiTags.AddRange(ShadeInducedSenescenceRate.Document());
            yield return new Section("Potential Leaf Area index", laiTags);

            var stressTags = new List<ITag>();
            stressTags.Add(new Paragraph("Stress reduces leaf area in a number of ways. Firstly, stress occuring prior to the appearance of the cohort can reduce cell division, so reducing the maximum leaf size. Leaf captures this by multiplying the *MaxSize* of each cohort by a *CellDivisionStress* factor which is calculated as:"));
            stressTags.AddRange(CellDivisionStress.Document());
            stressTags.Add(new Paragraph("Leaf.FN quantifys the N stress status of the plant and represents the concentration of metabolic N relative the maximum potentil metabolic N content of the leaf calculated as (*Leaf.NConc - MinimumNConc*)/(*CriticalNConc - MinimumNConc*)."));
            stressTags.Add(new Paragraph("Leaf.FW quantifies water stress and is calculated as *Leaf.Transpiration*/*Leaf.WaterDemand*, where *Leaf.Transpiration* is the minimum of *Leaf.WaterDemand* and *Root.WaterUptake*"));
            stressTags.Add(new Paragraph("Stress during the <i>GrowthDuration* of the cohort reduces the size increase of the cohort by multiplying the potential increase by a *ExpansionStress* factor:"));
            stressTags.AddRange(ExpansionStress.Document());
            stressTags.Add(new Paragraph("Stresses can also acellerate the onset and rate of senescence in a number of ways. Nitrogen shortage will cause N to be retranslocated out of lower order leaves to support the expansion of higher order leaves and other organs When this happens the lower order cohorts will have their area reduced in proportion to the amount of N that is remobilised out of them."));
            stressTags.Add(new Paragraph("Water stress hastens senescence by increasing the rate of thermal time accumulation in the lag and senescence phases. This is done by multiplying thermal time accumulation by *DroughtInducedLagAcceleration* and *DroughtInducedSenescenceAcceleration* factors, respectively"));
            yield return new Section("Stress effects on Leaf Area Index", stressTags);

            var dmDemandTags = new List<ITag>();
            dmDemandTags.Add(new Paragraph("Leaf calculates the DM demand from each cohort as a function of the potential size increment (DeltaPotentialArea) an specific leaf area bounds. " +
                                           "Under non stressed conditions the demand for non-storage DM is calculated as *DeltaPotentialArea* divided by the mean of *SpecificLeafAreaMax* and *SpecificLeafAreaMin*. " +
                                           "Under stressed conditions it is calculated as *DeltaWaterConstrainedArea* divided by *SpecificLeafAreaMin*."));
            dmDemandTags.AddRange(SpecificLeafAreaMax.Document());
            dmDemandTags.AddRange(SpecificLeafAreaMin.Document());
            dmDemandTags.Add(new Paragraph("Non-storage DM Demand is then seperated into structural and metabolic DM demands using the *StructuralFraction*:"));
            dmDemandTags.AddRange(StructuralFraction.Document());
            dmDemandTags.Add(new Paragraph("The storage DM demand is calculated from the sum of metabolic and structural DM (including todays demands) multiplied by a *NonStructuralFraction*"));
            yield return new Section("Dry matter Demand", dmDemandTags);

            var nDemandTags = new List<ITag>();
            nDemandTags.Add(new Paragraph("Leaf calculates the N demand from each cohort as a function of the potential DM increment and N concentration bounds."));
            nDemandTags.Add(new Paragraph("Structural N demand = *PotentialStructuralDMAllocation* * *MinimumNConc* where:"));
            nDemandTags.AddRange(MinimumNConc.Document());
            nDemandTags.Add(new Paragraph("Metabolic N demand is calculated as *PotentialMetabolicDMAllocation* * (*CriticalNConc* - *MinimumNConc*) where:"));
            nDemandTags.AddRange(CriticalNConc.Document());
            nDemandTags.Add(new Paragraph("Storage N demand is calculated as the sum of metabolic and structural wt (including todays demands) multiplied by *LuxaryNconc* (*MaximumNConc* - *CriticalNConc*) less the amount of storage N already present.  *MaximumNConc* is given by:"));
            nDemandTags.AddRange(MaximumNConc.Document());
            yield return new Section("Nitrogen Demand", nDemandTags);

            var dmSupplyTags = new List<ITag>();
            dmSupplyTags.Add(new Paragraph("In additon to photosynthesis, the leaf can also supply DM by reallocation of senescing DM and retranslocation of storgage DM:" +
                                           "Reallocation supply is a proportion of the metabolic and non-structural DM that would be senesced each day where the proportion is set by:"));
            dmSupplyTags.AddRange(DMReallocationFactor.Document());
            dmSupplyTags.Add(new Paragraph("Retranslocation supply is calculated as a proportion of the amount of storage DM in each cohort where the proportion is set by :"));
            dmSupplyTags.AddRange(DMRetranslocationFactor.Document());
            yield return new Section("Drymatter supply", dmSupplyTags);

            var nSupplyTags = new List<ITag>();
            nSupplyTags.Add(new Paragraph("Nitrogen supply from the leaf comes from the reallocation of metabolic and storage N in senescing material " +
                                           "and the retranslocation of metabolic and storage N.  Reallocation supply is a proportion of the Metabolic and Storage DM that would be senesced each day where the proportion is set by:"));
            nSupplyTags.AddRange(NReallocationFactor.Document());
            nSupplyTags.Add(new Paragraph("Retranslocation supply is calculated as a proportion of the amount of storage and metabolic N in each cohort where the proportion is set by :"));
            nSupplyTags.AddRange(NRetranslocationFactor.Document());
            yield return new Section("Nitrogen supply", nSupplyTags);

            // Document Constants
            var constantTags = new List<ITag>();
            foreach (var constant in FindAllChildren<Constant>())
                foreach (var tag in constant.Document())
                    constantTags.Add(tag);
            yield return new Section("Constants", constantTags);
        }
    }
}