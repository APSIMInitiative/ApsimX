using System.Collections.Generic;
using APSIM.Documentation.Tag;
using Models.Core;
using Models.Functions;

namespace APSIM.Documentation.Models.Types
{

    /// <summary>
    /// Base documentation class for models
    /// </summary>
    public class DocOrgan : DocGeneric
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DocOrgan" /> class.
        /// </summary>
        public DocOrgan(IModel model): base(model) {}

        /// <summary>
        /// Document the model.
        /// </summary>
        public override List<ITag> Document(int none = 0)
        {
            Section section = GetSummaryAndRemarksSection(model);

            //// List the parameters, properties, and processes from this organ that need to be documented:
            List<ITag> subTags = new List<ITag>();

            // document DM demands
            subTags.Add(new Paragraph("The dry matter demand for the organ is calculated as defined in DMDemands, based on the DMDemandFunction and partition fractions for each biomass pool.", model));
            IModel DMDemand = this.model.Node.FindChild<IModel>("dmDemands");
            subTags.AddRange(AutoDocumentation.DocumentModel(DMDemand));
            section.Add(new Section("Dry Matter Demand", subTags, model));

            // document N demands
            subTags = new List<ITag>();
            subTags.Add(new Paragraph("The N demand is calculated as defined in NDemands, based on DM demand the N concentration of each biomass pool.", model));
            IModel NDemand = this.model.Node.FindChild<IModel>("nDemands");
            subTags.AddRange(AutoDocumentation.DocumentModel(NDemand));

            // document N concentration thresholds
            IModel MinN = this.model.Node.FindChild<IModel>("MinimumNConc");
            subTags.AddRange(AutoDocumentation.DocumentModel(MinN));
            IModel CritN = this.model.Node.FindChild<IModel>("CriticalNConc");
            subTags.AddRange(AutoDocumentation.DocumentModel(CritN));
            IModel MaxN = this.model.Node.FindChild<IModel>("MaximumNConc");
            subTags.AddRange(AutoDocumentation.DocumentModel(MaxN));
            IModel NDemSwitch = this.model.Node.FindChild<IModel>("NitrogenDemandSwitch");
            if (NDemSwitch is Constant)
            {
                if ((NDemSwitch as Constant).Value() == 1.0)
                {
                    //Don't bother documenting as is does nothing
                }
                else
                {
                    subTags.Add(new Paragraph("The demand for N is reduced by a factor of " + (NDemSwitch as Constant).Value() + " as specified by the NitrogenDemandSwitch", model));
                }
            }
            else
            {
                subTags.Add(new Paragraph("The demand for N is reduced by a factor specified by the NitrogenDemandSwitch.", model));
                subTags.AddRange(AutoDocumentation.DocumentModel(NDemSwitch));
            }
            section.Add(new Section("Nitrogen Demand", subTags, model));

            // document DM supplies
            subTags = new List<ITag>();
            IModel DMReallocFac = this.model.Node.FindChild<IModel>("DMReallocationFactor");
            if (DMReallocFac is Constant)
            {
                if ((DMReallocFac as Constant).Value() == 0)
                    subTags.Add(new Paragraph(this.model.Name + " does not reallocate DM when senescence of the organ occurs.", model));
                else
                    subTags.Add(new Paragraph(this.model.Name + " will reallocate " + (DMReallocFac as Constant).Value() * 100 + "% of DM that senesces each day.", model));
            }
            else
            {
                subTags.Add(new Paragraph("The proportion of senescing DM that is allocated each day is quantified by the DMReallocationFactor.", model));
                subTags.AddRange(AutoDocumentation.DocumentModel(DMReallocFac));
            }
            IModel DMRetransFac = this.model.Node.FindChild<IModel>("DMRetranslocationFactor");
            if (DMRetransFac is Constant)
            {
                if ((DMRetransFac as Constant).Value() == 0)
                    subTags.Add(new Paragraph(this.model.Name + " does not retranslocate non-structural DM.", model));
                else
                    subTags.Add(new Paragraph(this.model.Name + " will retranslocate " + (DMRetransFac as Constant).Value() * 100 + "% of non-structural DM each day.", model));
            }
            else
            {
                subTags.Add(new Paragraph("The proportion of non-structural DM that is allocated each day is quantified by the DMReallocationFactor.", model));
                subTags.AddRange(AutoDocumentation.DocumentModel(DMRetransFac));
            }
            section.Add(new Section("Dry Matter Supply", subTags, model));

            // document N supplies
            subTags = new List<ITag>();
            IModel NReallocFac = this.model.Node.FindChild<IModel>("NReallocationFactor");
            if (NReallocFac is Constant)
            {
                if ((NReallocFac as Constant).Value() == 0)
                    subTags.Add(new Paragraph(this.model.Name + " does not reallocate N when senescence of the organ occurs.", model));
                else
                    subTags.Add(new Paragraph(this.model.Name + " can reallocate up to " + (NReallocFac as Constant).Value() * 100 + "% of N that senesces each day if required by the plant arbitrator to meet N demands.", model));
            }
            else
            {
                subTags.Add(new Paragraph("The proportion of senescing N that is allocated each day is quantified by the NReallocationFactor.", model));
                subTags.AddRange(AutoDocumentation.DocumentModel(NReallocFac));
            }
            IModel NRetransFac = this.model.Node.FindChild<IModel>("NRetranslocationFactor");
            if (NRetransFac is Constant)
            {
                if ((NRetransFac as Constant).Value() == 0)
                    subTags.Add(new Paragraph(this.model.Name + " does not retranslocate non-structural N.", model));
                else
                    subTags.Add(new Paragraph(this.model.Name + " can retranslocate up to " + (NRetransFac as Constant).Value() * 100 + "% of non-structural N each day if required by the plant arbitrator to meet N demands.", model));
            }
            else
            {
                subTags.Add(new Paragraph("The proportion of non-structural N that is allocated each day is quantified by the NReallocationFactor.", model));
                subTags.AddRange(AutoDocumentation.DocumentModel(NRetransFac));
            }
            section.Add(new Section("Nitrogen Supply", subTags, model));

            // document senescence and detachment
            subTags = new List<ITag>();
            IModel SenRate = this.model.Node.FindChild<IModel>("SenescenceRate");
            if (SenRate is Constant)
            {
                if ((SenRate as Constant).Value() == 0)
                    subTags.Add(new Paragraph(this.model.Name + " has senescence parameterised to zero so all biomass in this organ will remain alive.", model));
                else
                    subTags.Add(new Paragraph(this.model.Name + " senesces " + (SenRate as Constant).Value() * 100 + "% of its live biomass each day, moving the corresponding amount of biomass from the live to the dead biomass pool.", model));
            }
            else
            {
                subTags.Add(new Paragraph("The proportion of live biomass that senesces and moves into the dead pool each day is quantified by the SenescenceRate.", model));
                subTags.AddRange(AutoDocumentation.DocumentModel(SenRate));
            }

            IModel DetRate = this.model.Node.FindChild<IModel>("DetachmentRateFunction");
            if (DetRate is Constant)
            {
                if ((DetRate as Constant).Value() == 0)
                    subTags.Add(new Paragraph(this.model.Name + " has detachment parameterised to zero so all biomass in this organ will remain with the plant until a defoliation or harvest event occurs.", model));
                else
                    subTags.Add(new Paragraph(this.model.Name + " detaches " + (DetRate as Constant).Value() * 100 + "% of its live biomass each day, passing it to the surface organic matter model for decomposition.", model));
            }
            else
            {
                subTags.Add(new Paragraph("The proportion of Biomass that detaches and is passed to the surface organic matter model for decomposition is quantified by the DetachmentRateFunction.", model));
                subTags.AddRange(AutoDocumentation.DocumentModel(DetRate));
            }
            section.Add(new Section("Senescence and Detachment", subTags, model));

            return new List<ITag>() {section};
        }
    }
}
