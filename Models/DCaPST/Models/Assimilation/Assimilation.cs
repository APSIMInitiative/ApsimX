using Models.DCAPST.Interfaces;
using System;

namespace Models.DCAPST
{
    /// <summary>
    /// Tracks the state of an assimilation type
    /// </summary>
    public abstract class Assimilation : IAssimilation
    {
        /// <inheritdoc/>
        public virtual int Iterations { get; set; } = 3;

        /// <summary>
        /// The parameters describing the canopy
        /// </summary>
        protected readonly CanopyParameters canopy;

        /// <summary>
        /// The parameters describing the pathways
        /// </summary>
        protected readonly PathwayParameters parameters;

        /// <summary>
        /// The amount of CO2 in the air.
        /// </summary>
        protected double ambientCO2;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="canopy"></param>
        /// <param name="parameters"></param>
        /// <param name="ambientCO2"></param>
        public Assimilation(CanopyParameters canopy, PathwayParameters parameters, double ambientCO2)
        {
            this.canopy = canopy;
            this.parameters = parameters;
            this.ambientCO2 = ambientCO2;
        }       

        /// <summary>
        /// Factory method for accessing the different possible terms for assimilation
        /// </summary>
        public AssimilationFunction GetFunction(AssimilationPathway pathway, TemperatureResponse leaf)
        {
            return pathway.Type switch
            {
                PathwayType.Ac1 => GetAc1Function(pathway, leaf),
                PathwayType.Ac2 => GetAc2Function(pathway, leaf),
                PathwayType.Aj => GetAjFunction(pathway, leaf),
                _ => throw new ArgumentException($"Unsupported pathway type: {pathway.Type}"),
            };
        }

        /// <inheritdoc/>
        public void UpdatePartialPressures(AssimilationPathway pathway, double leafGmT, AssimilationFunction function)
        {
            var cm = pathway.MesophyllCO2;
            var cc = pathway.ChloroplasticCO2;
            var oc = pathway.ChloroplasticO2;

            UpdateMesophyllCO2(pathway, leafGmT);
            UpdateChloroplasticO2(pathway);
            UpdateChloroplasticCO2(pathway, function);

            pathway.MesophyllCO2 = (pathway.MesophyllCO2 + cm) / 2.0;
            pathway.ChloroplasticCO2 = (pathway.ChloroplasticCO2 + cc) / 2.0;
            pathway.ChloroplasticO2 = (pathway.ChloroplasticO2 + oc) / 2.0;
        }

        /// <inheritdoc/>
        public virtual void UpdateIntercellularCO2(AssimilationPathway pathway, double gt, double waterUseMolsSecond) 
        { /*C4 & CCM overwrite this.*/ }

        /// <summary>
        /// Updates the mesophyll CO2 parameter
        /// </summary>
        protected virtual void UpdateMesophyllCO2(AssimilationPathway pathway, double leafGmT) 
        { /*C4 & CCM overwrite this.*/ }

        /// <summary>
        /// Updates the chloroplastic O2 parameter
        /// </summary>
        protected virtual void UpdateChloroplasticO2(AssimilationPathway pathway) 
        { /*CCM overwrites this.*/ }

        /// <summary>
        /// Updates the chloroplastic CO2 parameter
        /// </summary>
        protected virtual void UpdateChloroplasticCO2(AssimilationPathway pathway, AssimilationFunction func) 
        { /*CCM overwrites this.*/ }

        /// <summary>
        /// Retrieves a function describing assimilation along the Ac1 pathway
        /// </summary>
        protected abstract AssimilationFunction GetAc1Function(AssimilationPathway pathway, TemperatureResponse leaf);

        /// <summary>
        /// Retrieves a function describing assimilation along the Ac2 pathway
        /// </summary>
        protected abstract AssimilationFunction GetAc2Function(AssimilationPathway pathway, TemperatureResponse leaf);

        /// <summary>
        /// Retrieves a function describing assimilation along the Aj pathway
        /// </summary>
        protected abstract AssimilationFunction GetAjFunction(AssimilationPathway pathway, TemperatureResponse leaf);
    }
}
