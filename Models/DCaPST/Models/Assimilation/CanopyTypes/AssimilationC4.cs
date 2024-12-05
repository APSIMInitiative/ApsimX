using Models.DCAPST.Interfaces;

namespace Models.DCAPST
{
    /// <summary>
    /// Defines the pathway functions for a C4 canopy
    /// </summary>
    public class AssimilationC4 : Assimilation
    {
        private double _alpha;
        private readonly double _canopyAirO2;
        private double _leafGamma;
        private double _leafKc;
        private double _leafKo;

        private Terms _x = new();

        /// <summary>
        /// Initializes the AssimilationC4 class with the given canopy and parameters
        /// </summary>
        /// <param name="canopy">The canopy parameters</param>
        /// <param name="parameters">The pathway parameters</param>
        public AssimilationC4(ICanopyParameters canopy, IPathwayParameters parameters) : base(canopy, parameters)
        {
            _canopyAirO2 = canopy.AirO2;
        }

        /// <inheritdoc/>
        public override void UpdateIntercellularCO2(AssimilationPathway pathway, double gt, double waterUseMolsSecond)
        {
            pathway.IntercellularCO2 = ((gt - waterUseMolsSecond / 2.0) * canopy.AirCO2 - pathway.CO2Rate) / (gt + waterUseMolsSecond / 2.0);
        }

        /// <inheritdoc/>
        protected override void UpdateMesophyllCO2(AssimilationPathway pathway, double leafGmT)
        {
            pathway.MesophyllCO2 = pathway.IntercellularCO2 - pathway.CO2Rate / leafGmT;
        }

        /// <summary>
        /// Helper method to create an AssimilationFunction object
        /// </summary>
        /// <param name="pathway">The assimilation pathway</param>
        /// <param name="leaf">The leaf temperature response</param>
        /// <param name="x">The terms for the function</param>
        /// <returns>An AssimilationFunction object</returns>
        private static AssimilationFunction CreateAssimilationFunction(AssimilationPathway pathway, TemperatureResponse leaf, Terms x)
        {
            return new AssimilationFunction()
            {
                x = x,
                MesophyllRespiration = leaf.GmRd,
                BundleSheathConductance = pathway.Gbs,
                Respiration = leaf.RdT
            };
        }

        /// <inheritdoc/>
        protected override AssimilationFunction GetAc1Function(AssimilationPathway pathway, TemperatureResponse leaf)
        {
            PrecomputeValues(pathway, leaf);

            _x._1 = leaf.VcMaxT;
            _x._2 = _leafKc + _canopyAirO2 * (_leafKc / _leafKo);
            _x._3 = leaf.VpMaxT / (pathway.MesophyllCO2 + leaf.Kp);
            _x._4 = 0.0;
            _x._5 = 1.0;
            _x._6 = 1.0;
            _x._7 = _alpha * _leafGamma;
            _x._8 = _leafGamma * _canopyAirO2;
            _x._9 = (_leafKc / _leafKo) * _alpha;

            return CreateAssimilationFunction(pathway, leaf, _x);
        }

        /// <inheritdoc/>
        protected override AssimilationFunction GetAc2Function(AssimilationPathway pathway, TemperatureResponse leaf)
        {
            PrecomputeValues(pathway, leaf);

            _x._1 = leaf.VcMaxT;
            _x._2 = _leafKc + _canopyAirO2 * (_leafKc / _leafKo);
            _x._3 = 0.0;
            _x._4 = pathway.Vpr;
            _x._5 = 1.0;
            _x._6 = 1.0;
            _x._7 = _alpha * _leafGamma;
            _x._8 = _leafGamma * _canopyAirO2;
            _x._9 = (_leafKc / _leafKo) * _alpha;

            return CreateAssimilationFunction(pathway, leaf, _x);
        }

        /// <inheritdoc/>
        protected override AssimilationFunction GetAjFunction(AssimilationPathway pathway, TemperatureResponse leaf)
        {
            PrecomputeValues(pathway, leaf);

            _x._1 = (1.0 - parameters.MesophyllElectronTransportFraction) * leaf.J / 3.0;
            _x._2 = _canopyAirO2 * (7.0 / 3.0) * _leafGamma;
            _x._3 = 0.0;
            _x._4 = parameters.MesophyllElectronTransportFraction * leaf.J / parameters.ExtraATPCost;
            _x._5 = 1.0;
            _x._6 = 1.0;
            _x._7 = _alpha * _leafGamma;
            _x._8 = _leafGamma * _canopyAirO2;
            _x._9 = (7.0 / 3.0) * _leafGamma * _alpha;

            return CreateAssimilationFunction(pathway, leaf, _x);
        }

        /// <summary>
        /// Precomputes common values for each function
        /// </summary>
        /// <param name="pathway">The assimilation pathway</param>
        /// <param name="leaf">The leaf temperature response</param>
        private void PrecomputeValues(AssimilationPathway pathway, TemperatureResponse leaf)
        {
            _alpha = 0.1 / (canopy.DiffusivitySolubilityRatio * pathway.Gbs);
            _leafGamma = leaf.Gamma;
            _leafKo = leaf.Ko;
            _leafKc = leaf.Kc;
        }
    }
}
