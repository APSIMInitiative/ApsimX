﻿using System;
using Models.DCAPST.Interfaces;

namespace Models.DCAPST
{
    /// <summary>
    /// Defines the pathway functions for a C3 canopy
    /// </summary>
    public class AssimilationC3 : Assimilation
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="canopy"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public AssimilationC3(ICanopyParameters canopy, IPathwayParameters parameters) : base(canopy, parameters)
        { }

        /// <inheritdoc/>
        protected override AssimilationFunction GetAc1Function(AssimilationPathway pathway, TemperatureResponse leaf)
        {
            var x = new Terms()
            {
                _1 = leaf.VcMaxT,
                _2 = leaf.Kc + canopy.AirO2 * leaf.Kc / leaf.Ko,
                _3 = 0.0,
                _4 = 0.0,
                _5 = 0.0,
                _6 = 0.0,
                _7 = 0.0,
                _8 = canopy.AirO2 * leaf.Gamma,
                _9 = 0.0
            };

            var param = new AssimilationFunction()
            {
                x = x,
                MesophyllRespiration = leaf.GmRd,
                BundleSheathConductance = 1.0,
                Respiration = leaf.RdT
            };

            return param;
        }

        /// <inheritdoc/>
        protected override AssimilationFunction GetAc2Function(AssimilationPathway pathway, TemperatureResponse leaf)
        {
            throw new Exception("The C3 model does not use the Ac2 pathway");
        }

        /// <inheritdoc/>
        protected override AssimilationFunction GetAjFunction(AssimilationPathway pathway, TemperatureResponse leaf)
        {
            var x = new Terms()
            {
                _1 = leaf.J / 4.0,
                _2 = 2.0 * leaf.Gamma * canopy.AirO2,
                _3 = 0.0,
                _4 = 0.0,
                _5 = 0.0,
                _6 = 0.0,
                _7 = 0.0,
                _8 = canopy.AirO2 * leaf.Gamma,
                _9 = 0.0
            };

            var func = new AssimilationFunction()
            {
                x = x,

                MesophyllRespiration = leaf.GmRd,
                BundleSheathConductance = 1.0,
                Respiration = leaf.RdT
            };

            return func;
        }
    }
}
