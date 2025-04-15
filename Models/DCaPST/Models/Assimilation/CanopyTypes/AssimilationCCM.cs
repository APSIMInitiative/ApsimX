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
        public AssimilationCCM(CanopyParameters canopy, PathwayParameters parameters, double ambientCO2) : base(canopy, parameters, ambientCO2)
        {
        }

        /// <inheritdoc/>
        public override void UpdateIntercellularCO2(AssimilationPathway pathway, double gt, double waterUseMolsSecond)
        {
            pathway.IntercellularCO2 = ((gt - waterUseMolsSecond / 2.0) * ambientCO2 - pathway.CO2Rate) / (gt + waterUseMolsSecond / 2.0);
        }

        /// <inheritdoc/>
        protected override void UpdateMesophyllCO2(AssimilationPathway pathway, double leafGmT)
        {
            pathway.MesophyllCO2 = pathway.IntercellularCO2 - pathway.CO2Rate / leafGmT;
        }

        /// <inheritdoc/>
        protected override void UpdateChloroplasticO2(AssimilationPathway pathway)
        {
            pathway.ChloroplasticO2 = 
                parameters.PS2ActivityFraction * pathway.CO2Rate / (canopy.DiffusivitySolubilityRatio * pathway.Gbs) + canopy.AirO2;
        }

        /// <inheritdoc/>
        protected override void UpdateChloroplasticCO2(AssimilationPathway pathway, AssimilationFunction func)
        {
            var pathwayMesophyllCO2 = pathway.MesophyllCO2;

            var a = pathwayMesophyllCO2 * func.x._3 
                + func.x._4
                - func.x._5 * pathway.CO2Rate 
                - func.MesophyllRespiration;

            pathway.ChloroplasticCO2 = pathwayMesophyllCO2 + a * func.x._6 / pathway.Gbs;
        }

        /// <inheritdoc/>
        protected override AssimilationFunction GetAc1Function(AssimilationPathway pathway, TemperatureResponse leaf)
        {
            var alpha = 0.1 / (canopy.DiffusivitySolubilityRatio * pathway.Gbs);
            var leafKc = leaf.Kc;
            var leafKo = leaf.Ko;
            var kcko = leafKc / leafKo;
            var cc = pathway.ChloroplasticCO2;
            var oc = pathway.ChloroplasticO2;
            var leafGamma = leaf.Gamma;
            var leafVcMaxT = leaf.VcMaxT;
            var canopyAirO2 = canopy.AirO2;

            var x = new Terms()
            {
                _1 = leafVcMaxT,
                _2 = leafKc + canopyAirO2 * kcko,
                _3 = leafVcMaxT / (pathway.MesophyllCO2 + leaf.Kp),
                _4 = -cc * leafVcMaxT / (cc + leafKc * (1 + oc / leafKo)),
                _5 = 0.0,
                _6 = 1.0,
                _7 = alpha * leafGamma,
                _8 = leafGamma * canopyAirO2,
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
            var leafGamma = leaf.Gamma;
            var leafKc = leaf.Kc;
            var leafVcMaxT = leaf.VcMaxT;
            var canopyAirO2 = canopy.AirO2;
            var leafKo = leaf.Ko;

            var x = new Terms()
            {
                _1 = leafVcMaxT,
                _2 = leafKc + canopyAirO2 * (leafKc / leafKo),
                _3 = 0.0,
                _4 = pathway.Vpr - cc * leafVcMaxT / (cc + leafKc * (1 + oc / leafKo)),
                _5 = 0.0,
                _6 = 1.0,
                _7 = a * leafGamma,
                _8 = leafGamma * canopyAirO2,
                _9 = (leafKc / leafKo) * a
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
            var a = 0.1 / (canopy.DiffusivitySolubilityRatio * pathway.Gbs);
            var y = parameters.MesophyllElectronTransportFraction;
            var z = parameters.ATPProductionElectronTransportFactor;
            var cc = pathway.ChloroplasticCO2;
            var oc = pathway.ChloroplasticO2;
            var leafGamma = leaf.Gamma;
            var leafJ = leaf.J;
            var canopyAirO2 = canopy.AirO2;

            var x = new Terms()
            {
                _1 = (1 - y) * z * leafJ / 3.0,
                _2 = canopyAirO2 * (7.0 / 3.0) * leafGamma,
                _3 = 0.0,
                _4 = (y * z * leafJ / parameters.ExtraATPCost) - cc * (1 - y) * z * leafJ / (3 * cc + 7 * leafGamma * oc),
                _5 = 0.0,
                _6 = 1.0,
                _7 = a * leafGamma,
                _8 = leafGamma * canopyAirO2,
                _9 = (7.0 / 3.0) * leafGamma * a 
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
