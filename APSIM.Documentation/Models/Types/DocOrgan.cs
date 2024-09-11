using System.Collections.Generic;
using APSIM.Shared.Documentation;
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
        public override List<ITag> Document(int heading = 0)
        {
            List<ITag> tags = base.Document(heading);

            //// List the parameters, properties, and processes from this organ that need to be documented:

            // document DM demands
            tags.Add(new Heading("Dry Matter Demand", heading + 1));
            tags.Add(new Paragraph("The dry matter demand for the organ is calculated as defined in DMDemands, based on the DMDemandFunction and partition fractions for each biomass pool."));
            IModel DMDemand = this.model.FindChild("dmDemands");
            tags.AddRange(AutoDocumentation.Document(DMDemand, heading + 2));

            // document N demands
            tags.Add(new Heading("Nitrogen Demand", heading + 1));
            tags.Add(new Paragraph("The N demand is calculated as defined in NDemands, based on DM demand the N concentration of each biomass pool."));
            IModel NDemand = this.model.FindChild("nDemands");
            tags.AddRange(AutoDocumentation.Document(NDemand, heading + 2));

            // document N concentration thresholds
            IModel MinN = this.model.FindChild("MinimumNConc");
            tags.AddRange(AutoDocumentation.Document(MinN, heading + 2));
            IModel CritN = this.model.FindChild("CriticalNConc");
            tags.AddRange(AutoDocumentation.Document(CritN, heading + 2));
            IModel MaxN = this.model.FindChild("MaximumNConc");
            tags.AddRange(AutoDocumentation.Document(MaxN, heading + 2));
            IModel NDemSwitch = this.model.FindChild("NitrogenDemandSwitch");
            if (NDemSwitch is Constant)
            {
                if ((NDemSwitch as Constant).Value() == 1.0)
                {
                    //Don't bother documenting as is does nothing
                }
                else
                {
                    tags.Add(new Paragraph("The demand for N is reduced by a factor of " + (NDemSwitch as Constant).Value() + " as specified by the NitrogenDemandSwitch"));
                }
            }
            else
            {
                tags.Add(new Paragraph("The demand for N is reduced by a factor specified by the NitrogenDemandSwitch."));
                tags.AddRange(AutoDocumentation.Document(NDemSwitch, heading + 2));
            }

            // document DM supplies
            tags.Add(new Heading("Dry Matter Supply", heading + 1));
            IModel DMReallocFac = this.model.FindChild("DMReallocationFactor");
            if (DMReallocFac is Constant)
            {
                if ((DMReallocFac as Constant).Value() == 0)
                    tags.Add(new Paragraph(this.model.Name + " does not reallocate DM when senescence of the organ occurs."));
                else
                    tags.Add(new Paragraph(this.model.Name + " will reallocate " + (DMReallocFac as Constant).Value() * 100 + "% of DM that senesces each day."));
            }
            else
            {
                tags.Add(new Paragraph("The proportion of senescing DM that is allocated each day is quantified by the DMReallocationFactor."));
                tags.AddRange(AutoDocumentation.Document(DMReallocFac, heading + 2));
            }
            IModel DMRetransFac = this.model.FindChild("DMRetranslocationFactor");
            if (DMRetransFac is Constant)
            {
                if ((DMRetransFac as Constant).Value() == 0)
                    tags.Add(new Paragraph(this.model.Name + " does not retranslocate non-structural DM."));
                else
                    tags.Add(new Paragraph(this.model.Name + " will retranslocate " + (DMRetransFac as Constant).Value() * 100 + "% of non-structural DM each day."));
            }
            else
            {
                tags.Add(new Paragraph("The proportion of non-structural DM that is allocated each day is quantified by the DMReallocationFactor."));
                tags.AddRange(AutoDocumentation.Document(DMRetransFac, heading + 2));
            }

            // document N supplies
            tags.Add(new Heading("Nitrogen Supply", heading + 1));
            IModel NReallocFac = this.model.FindChild("NReallocationFactor");
            if (NReallocFac is Constant)
            {
                if ((NReallocFac as Constant).Value() == 0)
                    tags.Add(new Paragraph(this.model.Name + " does not reallocate N when senescence of the organ occurs."));
                else
                    tags.Add(new Paragraph(this.model.Name + " can reallocate up to " + (NReallocFac as Constant).Value() * 100 + "% of N that senesces each day if required by the plant arbitrator to meet N demands."));
            }
            else
            {
                tags.Add(new Paragraph("The proportion of senescing N that is allocated each day is quantified by the NReallocationFactor."));
                tags.AddRange(AutoDocumentation.Document(NReallocFac, heading + 2));
            }
            IModel NRetransFac = this.model.FindChild("NRetranslocationFactor");
            if (NRetransFac is Constant)
            {
                if ((NRetransFac as Constant).Value() == 0)
                    tags.Add(new Paragraph(this.model.Name + " does not retranslocate non-structural N."));
                else
                    tags.Add(new Paragraph(this.model.Name + " can retranslocate up to " + (NRetransFac as Constant).Value() * 100 + "% of non-structural N each day if required by the plant arbitrator to meet N demands."));
            }
            else
            {
                tags.Add(new Paragraph("The proportion of non-structural N that is allocated each day is quantified by the NReallocationFactor."));
                tags.AddRange(AutoDocumentation.Document(NRetransFac, heading + 2));
            }

            // document senescence and detachment
            tags.Add(new Heading("Senescence and Detachment", heading + 1));
            IModel SenRate = this.model.FindChild("SenescenceRate");
            if (SenRate is Constant)
            {
                if ((SenRate as Constant).Value() == 0)
                    tags.Add(new Paragraph(this.model.Name + " has senescence parameterised to zero so all biomass in this organ will remain alive."));
                else
                    tags.Add(new Paragraph(this.model.Name + " senesces " + (SenRate as Constant).Value() * 100 + "% of its live biomass each day, moving the corresponding amount of biomass from the live to the dead biomass pool."));
            }
            else
            {
                tags.Add(new Paragraph("The proportion of live biomass that senesces and moves into the dead pool each day is quantified by the SenescenceRate."));
                tags.AddRange(AutoDocumentation.Document(SenRate, heading + 2));
            }

            IModel DetRate = this.model.FindChild("DetachmentRateFunction");
            if (DetRate is Constant)
            {
                if ((DetRate as Constant).Value() == 0)
                    tags.Add(new Paragraph(this.model.Name + " has detachment parameterised to zero so all biomass in this organ will remain with the plant until a defoliation or harvest event occurs."));
                else
                    tags.Add(new Paragraph(this.model.Name + " detaches " + (DetRate as Constant).Value() * 100 + "% of its live biomass each day, passing it to the surface organic matter model for decomposition."));
            }
            else
            {
                tags.Add(new Paragraph("The proportion of Biomass that detaches and is passed to the surface organic matter model for decomposition is quantified by the DetachmentRateFunction."));
                tags.AddRange(AutoDocumentation.Document(DetRate, heading + 2));
            }
            return tags;
        }
    }
}
