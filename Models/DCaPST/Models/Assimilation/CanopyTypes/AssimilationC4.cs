using Models.DCAPST.Interfaces;

namespace Models.DCAPST
{
    /// <summary>
    /// Defines the pathway functions for a C4 canopy
    /// </summary>
    public class AssimilationC4 : Assimilation
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="canopy"></param>
        /// <param name="parameters"></param>
        /// <param name="ambientCO2"></param>
        /// <returns></returns>
        public AssimilationC4(ICanopyParameters canopy, IPathwayParameters parameters, double ambientCO2) : base(canopy, parameters, ambientCO2)
        { }

        /// <inheritdoc/>
        public override void UpdateIntercellularCO2(AssimilationPathway pathway, double gt, double waterUseMolsSecond)
        {
            pathway.IntercellularCO2 = ((gt - waterUseMolsSecond / 2.0) * ambientCO2 - pathway.CO2Rate) / (gt + waterUseMolsSecond / 2.0);
        }

        /// <inheritdoc/>
        protected override void UpdateMesophyllCO2(AssimilationPathway pathway, TemperatureResponse leaf)
        {
            pathway.MesophyllCO2 = pathway.IntercellularCO2 - pathway.CO2Rate / leaf.GmT;
        }

        /// <inheritdoc/>
        protected override AssimilationFunction GetAc1Function(AssimilationPathway pathway, TemperatureResponse leaf)
        {
            var alpha = 0.1 / (canopy.DiffusivitySolubilityRatio * pathway.Gbs);
            var x = new Terms()
            {
                _1 = leaf.VcMaxT,
                _2 = leaf.Kc + canopy.AirO2 * (leaf.Kc / leaf.Ko),
                _3 = leaf.VpMaxT / (pathway.MesophyllCO2 + leaf.Kp),
                _4 = 0.0,
                _5 = 1.0,
                _6 = 1.0,
                _7 = alpha * leaf.Gamma,
                _8 = leaf.Gamma * canopy.AirO2,
                _9 = (leaf.Kc / leaf.Ko) * alpha
            };

            var func = new AssimilationFunction()
            {
                x = x,

                MesophyllRespiration = leaf.GmRd,
                BundleSheathConductance = pathway.Gbs,
                Respiration = leaf.RdT
            };

            return func;
        }

        /// <inheritdoc/>
        protected override AssimilationFunction GetAc2Function(AssimilationPathway pathway, TemperatureResponse leaf)
        {
            var alpha = 0.1 / (canopy.DiffusivitySolubilityRatio * pathway.Gbs);
            var x = new Terms()
            {
                _1 = leaf.VcMaxT,
                _2 = leaf.Kc + canopy.AirO2 * (leaf.Kc / leaf.Ko),
                _3 = 0.0,
                _4 = pathway.Vpr,
                _5 = 1.0,
                _6 = 1.0,
                _7 = alpha * leaf.Gamma,
                _8 = leaf.Gamma * canopy.AirO2,
                _9 = (leaf.Kc / leaf.Ko) * alpha
            };

            var func = new AssimilationFunction()
            {
                x = x,

                MesophyllRespiration = leaf.GmRd,
                BundleSheathConductance = pathway.Gbs,
                Respiration = leaf.RdT
            };

            return func;
        }
        
        /// <inheritdoc/>
        protected override AssimilationFunction GetAjFunction(AssimilationPathway pathway, TemperatureResponse leaf)
        {
            var alpha = 0.1 / (canopy.DiffusivitySolubilityRatio * pathway.Gbs);
            var x = new Terms()
            {
                _1 = (1.0 - parameters.MesophyllElectronTransportFraction) * leaf.J / 3.0,
                _2 = canopy.AirO2 * (7.0 / 3.0) * leaf.Gamma,
                _3 = 0.0,
                _4 = parameters.MesophyllElectronTransportFraction * leaf.J / parameters.ExtraATPCost,
                _5 = 1.0,
                _6 = 1.0,
                _7 = alpha * leaf.Gamma,
                _8 = leaf.Gamma * canopy.AirO2,
                _9 = (7.0 / 3.0) * leaf.Gamma * alpha
            };

            var func = new AssimilationFunction()
            {
                x = x,

                MesophyllRespiration = leaf.GmRd,
                BundleSheathConductance = pathway.Gbs,
                Respiration = leaf.RdT
            };

            return func;
        }
    }
}
