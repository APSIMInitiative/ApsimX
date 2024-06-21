using System.Collections.Generic;
using System.Data;
using System.Linq;
using APSIM.Shared.Documentation;
using Models;
using Models.Core;
using Models.Functions;
using Models.PMF;

namespace APSIM.Documentation.Models.Types
{

    /// <summary>
    /// Base documentation class for models
    /// </summary>
    public class OrganDoc : GenericDoc
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OrganDoc" /> class.
        /// </summary>
        public OrganDoc(IModel model): base(model) {}

        /// <summary>
        /// Document the model.
        /// </summary>
        public override IEnumerable<ITag> Document(List<ITag> tags = null, int headingLevel = 0, int indent = 0)
        {
            if (tags == null)
                tags = new List<ITag>();
            
            // add a heading, the name of this organ
            tags.Add(new Heading(this.model.Name, headingLevel));

            // write the basic description of this class, given in the <summary>
            AutoDocumentation.DocumentModelSummary(this.model, tags, headingLevel, indent, false);

            // write the memos
            foreach (IModel memo in this.model.FindAllChildren<Memo>())
                AutoDocumentation.Document(memo, tags, headingLevel + 1, indent);

            //// List the parameters, properties, and processes from this organ that need to be documented:

            // document DM demands
            tags.Add(new Heading("Dry Matter Demand", headingLevel + 1));
            tags.Add(new Paragraph("The dry matter demand for the organ is calculated as defined in DMDemands, based on the DMDemandFunction and partition fractions for each biomass pool.", indent));
            IModel DMDemand = this.model.FindChild("dmDemands");
            AutoDocumentation.Document(DMDemand, tags, headingLevel + 2, indent);

            // document N demands
            tags.Add(new Heading("Nitrogen Demand", headingLevel + 1));
            tags.Add(new Paragraph("The N demand is calculated as defined in NDemands, based on DM demand the N concentration of each biomass pool.", indent));
            IModel NDemand = this.model.FindChild("nDemands");
            AutoDocumentation.Document(NDemand, tags, headingLevel + 2, indent);

            // document N concentration thresholds
            IModel MinN = this.model.FindChild("MinimumNConc");
            AutoDocumentation.Document(MinN, tags, headingLevel + 2, indent);
            IModel CritN = this.model.FindChild("CriticalNConc");
            AutoDocumentation.Document(CritN, tags, headingLevel + 2, indent);
            IModel MaxN = this.model.FindChild("MaximumNConc");
            AutoDocumentation.Document(MaxN, tags, headingLevel + 2, indent);
            IModel NDemSwitch = this.model.FindChild("NitrogenDemandSwitch");
            if (NDemSwitch is Constant)
            {
                if ((NDemSwitch as Constant).Value() == 1.0)
                {
                    //Don't bother documenting as is does nothing
                }
                else
                {
                    tags.Add(new Paragraph("The demand for N is reduced by a factor of " + (NDemSwitch as Constant).Value() + " as specified by the NitrogenDemandSwitch", indent));
                }
            }
            else
            {
                tags.Add(new Paragraph("The demand for N is reduced by a factor specified by the NitrogenDemandSwitch.", indent));
                AutoDocumentation.Document(NDemSwitch, tags, headingLevel + 2, indent);
            }

            // document DM supplies
            tags.Add(new Heading("Dry Matter Supply", headingLevel + 1));
            IModel DMReallocFac = this.model.FindChild("DMReallocationFactor");
            if (DMReallocFac is Constant)
            {
                if ((DMReallocFac as Constant).Value() == 0)
                    tags.Add(new Paragraph(this.model.Name + " does not reallocate DM when senescence of the organ occurs.", indent));
                else
                    tags.Add(new Paragraph(this.model.Name + " will reallocate " + (DMReallocFac as Constant).Value() * 100 + "% of DM that senesces each day.", indent));
            }
            else
            {
                tags.Add(new Paragraph("The proportion of senescing DM that is allocated each day is quantified by the DMReallocationFactor.", indent));
                AutoDocumentation.Document(DMReallocFac, tags, headingLevel + 2, indent);
            }
            IModel DMRetransFac = this.model.FindChild("DMRetranslocationFactor");
            if (DMRetransFac is Constant)
            {
                if ((DMRetransFac as Constant).Value() == 0)
                    tags.Add(new Paragraph(this.model.Name + " does not retranslocate non-structural DM.", indent));
                else
                    tags.Add(new Paragraph(this.model.Name + " will retranslocate " + (DMRetransFac as Constant).Value() * 100 + "% of non-structural DM each day.", indent));
            }
            else
            {
                tags.Add(new Paragraph("The proportion of non-structural DM that is allocated each day is quantified by the DMReallocationFactor.", indent));
                AutoDocumentation.Document(DMRetransFac, tags, headingLevel + 2, indent);
            }

            // document N supplies
            tags.Add(new Heading("Nitrogen Supply", headingLevel + 1));
            IModel NReallocFac = this.model.FindChild("NReallocationFactor");
            if (NReallocFac is Constant)
            {
                if ((NReallocFac as Constant).Value() == 0)
                    tags.Add(new Paragraph(this.model.Name + " does not reallocate N when senescence of the organ occurs.", indent));
                else
                    tags.Add(new Paragraph(this.model.Name + " can reallocate up to " + (NReallocFac as Constant).Value() * 100 + "% of N that senesces each day if required by the plant arbitrator to meet N demands.", indent));
            }
            else
            {
                tags.Add(new Paragraph("The proportion of senescing N that is allocated each day is quantified by the NReallocationFactor.", indent));
                AutoDocumentation.Document(NReallocFac, tags, headingLevel + 2, indent);
            }
            IModel NRetransFac = this.model.FindChild("NRetranslocationFactor");
            if (NRetransFac is Constant)
            {
                if ((NRetransFac as Constant).Value() == 0)
                    tags.Add(new Paragraph(this.model.Name + " does not retranslocate non-structural N.", indent));
                else
                    tags.Add(new Paragraph(this.model.Name + " can retranslocate up to " + (NRetransFac as Constant).Value() * 100 + "% of non-structural N each day if required by the plant arbitrator to meet N demands.", indent));
            }
            else
            {
                tags.Add(new Paragraph("The proportion of non-structural N that is allocated each day is quantified by the NReallocationFactor.", indent));
                AutoDocumentation.Document(NRetransFac, tags, headingLevel + 2, indent);
            }

            // document senescence and detachment
            tags.Add(new Heading("Senescence and Detachment", headingLevel + 1));
            IModel SenRate = this.model.FindChild("SenescenceRate");
            if (SenRate is Constant)
            {
                if ((SenRate as Constant).Value() == 0)
                    tags.Add(new Paragraph(this.model.Name + " has senescence parameterised to zero so all biomass in this organ will remain alive.", indent));
                else
                    tags.Add(new Paragraph(this.model.Name + " senesces " + (SenRate as Constant).Value() * 100 + "% of its live biomass each day, moving the corresponding amount of biomass from the live to the dead biomass pool.", indent));
            }
            else
            {
                tags.Add(new Paragraph("The proportion of live biomass that senesces and moves into the dead pool each day is quantified by the SenescenceRate.", indent));
                AutoDocumentation.Document(SenRate, tags, headingLevel + 2, indent);
            }

            IModel DetRate = this.model.FindChild("DetachmentRateFunction");
            if (DetRate is Constant)
            {
                if ((DetRate as Constant).Value() == 0)
                    tags.Add(new Paragraph(this.model.Name + " has detachment parameterised to zero so all biomass in this organ will remain with the plant until a defoliation or harvest event occurs.", indent));
                else
                    tags.Add(new Paragraph(this.model.Name + " detaches " + (DetRate as Constant).Value() * 100 + "% of its live biomass each day, passing it to the surface organic matter model for decomposition.", indent));
            }
            else
            {
                tags.Add(new Paragraph("The proportion of Biomass that detaches and is passed to the surface organic matter model for decomposition is quantified by the DetachmentRateFunction.", indent));
                AutoDocumentation.Document(DetRate, tags, headingLevel + 2, indent);
            }
            return tags;
        }
    }
}
