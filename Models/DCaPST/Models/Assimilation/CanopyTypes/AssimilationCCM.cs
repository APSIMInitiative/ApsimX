using Models.DCAPST.Interfaces;

namespace Models.DCAPST
{
    /// <summary>
    /// Defines the pathway functions for a CCM canopy
    /// </summary>
    public class AssimilationCCM : Assimilation
    {
        /// <summary>
        /// 
        /// </summary>
        public override int Iterations { get; set; } = 10;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="canopy"></param>
        /// <param name="parameters"></param>
        /// <param name="ambientCO2"></param>
        /// <returns></returns>
        public AssimilationCCM(ICanopyParameters canopy, IPathwayParameters parameters, double ambientCO2) : base(canopy, parameters, ambientCO2)
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
        protected override void UpdateChloroplasticO2(AssimilationPathway pathway)
        {
            pathway.ChloroplasticO2 = parameters.PS2ActivityFraction * pathway.CO2Rate / (canopy.DiffusivitySolubilityRatio * pathway.Gbs) + canopy.AirO2;
        }

        /// <inheritdoc/>
        protected override void UpdateChloroplasticCO2(AssimilationPathway pathway, AssimilationFunction func)
        {
            var a = pathway.MesophyllCO2 * func.x._3 
                + func.x._4
                - func.x._5 * pathway.CO2Rate 
                - func.MesophyllRespiration;

            pathway.ChloroplasticCO2 = pathway.MesophyllCO2 + a * func.x._6 / pathway.Gbs;
        }

        /// <inheritdoc/>
        protected override AssimilationFunction GetAc1Function(AssimilationPathway pathway, TemperatureResponse leaf)
        {
            var alpha = 0.1 / (canopy.DiffusivitySolubilityRatio * pathway.Gbs);
            var kcko = leaf.Kc / leaf.Ko;
            var cc = pathway.ChloroplasticCO2;
            var oc = pathway.ChloroplasticO2;

            var x = new Terms()
            {
                _1 = leaf.VcMaxT,
                _2 = leaf.Kc + canopy.AirO2 * kcko,
                _3 = leaf.VpMaxT / (pathway.MesophyllCO2 + leaf.Kp),
                _4 = -cc * leaf.VcMaxT / (cc + leaf.Kc * (1 + oc / leaf.Ko)),
                _5 = 0.0,
                _6 = 1.0,
                _7 = alpha * leaf.Gamma,
                _8 = leaf.Gamma * canopy.AirO2,
                _9 = kcko * alpha
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
            var a = 0.1 / (canopy.DiffusivitySolubilityRatio * pathway.Gbs);
            var cc = pathway.ChloroplasticCO2;
            var oc = pathway.ChloroplasticO2;

            var x = new Terms()
            {
                _1 = leaf.VcMaxT,
                _2 = leaf.Kc + canopy.AirO2 * (leaf.Kc / leaf.Ko),
                _3 = 0.0,
                _4 = pathway.Vpr - cc * leaf.VcMaxT / (cc + leaf.Kc * (1 + oc / leaf.Ko)),
                _5 = 0.0,
                _6 = 1.0,
                _7 = a * leaf.Gamma,
                _8 = leaf.Gamma * canopy.AirO2,
                _9 = (leaf.Kc / leaf.Ko) * a
            };


            var func = new AssimilationFunction()
            {
                x = x,

                MesophyllRespiration = leaf.GmRd,
                BundleSheathConductance = pathway.Gbs,
                Respiration =leaf.RdT
            };

            return func;
        }

        /// <inheritdoc/>
        protected override AssimilationFunction GetAjFunction(AssimilationPathway pathway, TemperatureResponse leaf)
        {
            var a = 0.1 / (canopy.DiffusivitySolubilityRatio * pathway.Gbs);
            var y = parameters.MesophyllElectronTransportFraction;
            var z = parameters.ATPProductionElectronTransportFactor;
            var cc = pathway.ChloroplasticCO2;
            var oc = pathway.ChloroplasticO2;
            
            var x = new Terms()
            {
                _1 = (1 - y) * z * leaf.J / 3.0,
                _2 = canopy.AirO2 * (7.0 / 3.0) * leaf.Gamma,
                _3 = 0.0,
                _4 = (y * z * leaf.J / parameters.ExtraATPCost) - cc * (1 - y) * z * leaf.J / (3 * cc + 7 * leaf.Gamma * oc),
                _5 = 0.0,
                _6 = 1.0,
                _7 = a * leaf.Gamma,
                _8 = leaf.Gamma * canopy.AirO2,
                _9 = (7.0 / 3.0) * leaf.Gamma * a 
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
