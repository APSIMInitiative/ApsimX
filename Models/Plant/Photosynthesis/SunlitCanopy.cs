using System;

namespace Models.PMF.Photosynthesis
{
    /// <summary></summary>
    public class SunlitCanopy : SunlitShadedCanopy
    {
        /// <summary></summary>
        public SunlitCanopy() { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_nLayers"></param>
        /// <param name="type"></param>
        public SunlitCanopy(int _nLayers, SSType type) : base(_nLayers, type) { }
        //---------------------------------------------------------------------------------------------------------
        /// <summary>
        /// 
        /// </summary>
        /// <param name="canopy"></param>
        /// <param name="counterpart"></param>
        public override void calcLAI(LeafCanopy canopy, SunlitShadedCanopy counterpart)
        {
            LAIS = new double[canopy.nLayers];
            for (int i = 0; i < canopy.nLayers; i++)
            {

                LAIS[i] = ((i == 0 ? 1 : Math.Exp(-canopy.beamExtCoeffs[i] * canopy.LAIAccums[i - 1])) -
                    Math.Exp(-canopy.beamExtCoeffs[i] * canopy.LAIAccums[i])) * 1 / canopy.beamExtCoeffs[i];
            }
        }
        //---------------------------------------------------------------------------------------------------------
        /// <summary>
        /// 
        /// </summary>
        /// <param name="EM"></param>
        /// <param name="canopy"></param>
        /// <param name="shaded"></param>
        public override void calcIncidentRadiation(EnvironmentModel EM, LeafCanopy canopy, SunlitShadedCanopy shaded)
        {
            for (int i = 0; i < _nLayers; i++)
            {
                incidentIrradiance[i] = EM.directRadiationPAR * canopy.propnInterceptedRadns[i] / LAIS[i] * LAIS[i] +
                     EM.diffuseRadiationPAR * canopy.propnInterceptedRadns[i] / (LAIS[i] + shaded.LAIS[i]) * LAIS[i];
            }
        }
        //---------------------------------------------------------------------------------------------------------
        /// <summary>
        /// 
        /// </summary>
        /// <param name="EM"></param>
        /// <param name="canopy"></param>
        /// <param name="shaded"></param>
        public override void calcAbsorbedRadiation(EnvironmentModel EM, LeafCanopy canopy, SunlitShadedCanopy shaded)
        {
            calcAbsorbedRadiationDirect(EM, canopy);
            calcAbsorbedRadiationDiffuse(EM, canopy);
            calcAbsorbedRadiationScattered(EM, canopy);

            for (int i = 0; i < _nLayers; i++)
            {
                absorbedIrradiance[i] = absorbedRadiationDirect[i] + absorbedRadiationDiffuse[i] + absorbedRadiationScattered[i];
                absorbedIrradiancePAR[i] = absorbedRadiationDirectPAR[i] + absorbedRadiationDiffusePAR[i] + absorbedRadiationScatteredPAR[i];
                absorbedIrradianceNIR[i] = absorbedRadiationDirectNIR[i] + absorbedRadiationDiffuseNIR[i] + absorbedRadiationScatteredNIR[i];
            }
        }
        //---------------------------------------------------------------------------------------------------------

        void calcAbsorbedRadiationDirect(EnvironmentModel EM, LeafCanopy canopy)
        {
            for (int i = 0; i < _nLayers; i++)
            {

                absorbedRadiationDirect[i] = (1 - canopy.leafScatteringCoeffs[i]) * EM.directRadiationPAR *
                    ((i == 0 ? 1 : Math.Exp(-canopy.beamExtCoeffs[i] * canopy.LAIAccums[i - 1])) -
                    Math.Exp(-canopy.beamExtCoeffs[i] * canopy.LAIAccums[i]));

                absorbedRadiationDirectPAR[i] = (1 - canopy.leafScatteringCoeffs[i]) * canopy.directPAR *
                    ((i == 0 ? 1 : Math.Exp(-canopy.beamExtCoeffs[i] * canopy.LAIAccums[i - 1])) -
                    Math.Exp(-canopy.beamExtCoeffs[i] * canopy.LAIAccums[i]));

                absorbedRadiationDirectNIR[i] = (1 - canopy.leafScatteringCoeffsNIR[i]) * canopy.directNIR *
                    ((i == 0 ? 1 : Math.Exp(-canopy.beamExtCoeffsNIR[i] * canopy.LAIAccums[i - 1])) -
                    Math.Exp(-canopy.beamExtCoeffsNIR[i] * canopy.LAIAccums[i]));
            }
        }
        //---------------------------------------------------------------------------------------------------------
        void calcAbsorbedRadiationDiffuse(EnvironmentModel EM, LeafCanopy canopy)
        {
            for (int i = 0; i < _nLayers; i++)
            {
                absorbedRadiationDiffuse[i] = (1 - canopy.diffuseReflectionCoeffs[i]) * EM.diffuseRadiationPAR *
                    ((i == 0 ? 1 : Math.Exp(-(canopy.diffuseScatteredDiffuses[i] + canopy.beamExtCoeffs[i]) * canopy.LAIAccums[i - 1])) -
                    Math.Exp(-(canopy.diffuseScatteredDiffuses[i] + canopy.beamExtCoeffs[i]) * canopy.LAIAccums[i])) * (canopy.diffuseScatteredDiffuses[i] /
                    (canopy.diffuseScatteredDiffuses[i] + canopy.beamExtCoeffs[i]));

                absorbedRadiationDiffusePAR[i] = (1 - canopy.diffuseReflectionCoeffs[i]) * canopy.diffusePAR *
                    ((i == 0 ? 1 : Math.Exp(-(canopy.diffuseScatteredDiffuses[i] + canopy.beamExtCoeffs[i]) * canopy.LAIAccums[i - 1])) -
                    Math.Exp(-(canopy.diffuseScatteredDiffuses[i] + canopy.beamExtCoeffs[i]) * canopy.LAIAccums[i])) * (canopy.diffuseScatteredDiffuses[i] /
                    (canopy.diffuseScatteredDiffuses[i] + canopy.beamExtCoeffs[i]));

                absorbedRadiationDiffuseNIR[i] = (1 - canopy.diffuseReflectionCoeffsNIR[i]) * canopy.diffuseNIR *
                    ((i == 0 ? 1 : Math.Exp(-(canopy.diffuseScatteredDiffusesNIR[i] + canopy.beamExtCoeffsNIR[i]) * canopy.LAIAccums[i - 1])) -
                    Math.Exp(-(canopy.diffuseScatteredDiffusesNIR[i] + canopy.beamExtCoeffsNIR[i]) * canopy.LAIAccums[i])) * (canopy.diffuseScatteredDiffusesNIR[i] /
                    (canopy.diffuseScatteredDiffusesNIR[i] + canopy.beamExtCoeffsNIR[i]));
            }
        }
        //---------------------------------------------------------------------------------------------------------
        /// <summary>
        /// 
        /// </summary>
        /// <param name="PM"></param>
        /// <param name="canopy"></param>
        public override void calcConductanceResistance(PhotosynthesisModel PM, LeafCanopy canopy)
        {
            for (int i = 0; i < canopy.nLayers; i++)
            {
                gbh[i] = 0.01 * Math.Pow((canopy.us[i] / canopy.leafWidth), 0.5) *
                    (1 - Math.Exp(-1 * (0.5 * canopy.ku + canopy.kb) * canopy.LAI)) / (0.5 * canopy.ku + canopy.kb);
            }

            base.calcConductanceResistance(PM, canopy);
        }
        //---------------------------------------------------------------------------------------------------------
        /// <summary>
        /// 
        /// </summary>
        /// <param name="canopy"></param>
        /// <param name="counterpart"></param>
        /// <param name="PM"></param>
        public override void calcMaxRates(LeafCanopy canopy, SunlitShadedCanopy counterpart, PhotosynthesisModel PM)
        {
            calcRubiscoActivity25(canopy, counterpart, PM);
            calcElectronTransportRate25(canopy, counterpart, PM);
            calcRdActivity25(canopy, counterpart, PM);
            calcPRate25(canopy, counterpart, PM);

        }
        //---------------------------------------------------------------------------------------------------------

        void calcAbsorbedRadiationScattered(EnvironmentModel EM, LeafCanopy canopy)
        {
            for (int i = 0; i < _nLayers; i++)
            {
                if (canopy.beamScatteredBeams[i] + canopy.beamExtCoeffs[i] == 0)
                {
                    absorbedRadiationScattered[i] = 0;
                }
                else
                {
                    absorbedRadiationScattered[i] = EM.directRadiationPAR * (((1 - canopy.beamReflectionCoeffs[i]) *
                         ((i == 0 ? 1 : Math.Exp(-(canopy.beamExtCoeffs[i] + canopy.beamScatteredBeams[i]) * canopy.LAIAccums[i - 1])) -
                         Math.Exp(-(canopy.beamExtCoeffs[i] + canopy.beamScatteredBeams[i]) * canopy.LAIAccums[i])) *
                         (canopy.beamScatteredBeams[i] / (canopy.beamScatteredBeams[i] + canopy.beamExtCoeffs[i]))) -
                         ((1 - canopy.leafScatteringCoeffs[i]) *
                          ((i == 0 ? 1 : Math.Exp(-2 * canopy.beamExtCoeffs[i] * canopy.LAIAccums[i - 1])) -
                          Math.Exp(-2 * canopy.beamExtCoeffs[i] * canopy.LAIAccums[i])) / 2));
                }

                if (canopy.beamScatteredBeams[i] + canopy.beamExtCoeffs[i] == 0)
                {
                    absorbedRadiationScattered[i] = 0;
                }
                else
                {
                    absorbedRadiationScatteredPAR[i] = canopy.directPAR * (((1 - canopy.beamReflectionCoeffs[i]) *
                         ((i == 0 ? 1 : Math.Exp(-(canopy.beamExtCoeffs[i] + canopy.beamScatteredBeams[i]) * canopy.LAIAccums[i - 1])) -
                         Math.Exp(-(canopy.beamExtCoeffs[i] + canopy.beamScatteredBeams[i]) * canopy.LAIAccums[i])) *
                         (canopy.beamScatteredBeams[i] / (canopy.beamScatteredBeams[i] + canopy.beamExtCoeffs[i]))) -
                         ((1 - canopy.leafScatteringCoeffs[i]) *
                          ((i == 0 ? 1 : Math.Exp(-2 * canopy.beamExtCoeffs[i] * canopy.LAIAccums[i - 1])) -
                          Math.Exp(-2 * canopy.beamExtCoeffs[i] * canopy.LAIAccums[i])) / 2));
                }

                if (canopy.beamScatteredBeamsNIR[i] + canopy.beamExtCoeffsNIR[i] == 0)
                {
                    absorbedRadiationScatteredNIR[i] = 0;
                }
                else
                {
                    absorbedRadiationScatteredNIR[i] = canopy.directNIR * (((1 - canopy.beamReflectionCoeffsNIR[i]) *
                         ((i == 0 ? 1 : Math.Exp(-(canopy.beamExtCoeffsNIR[i] + canopy.beamScatteredBeamsNIR[i]) * canopy.LAIAccums[i - 1])) -
                         Math.Exp(-(canopy.beamExtCoeffsNIR[i] + canopy.beamScatteredBeamsNIR[i]) * canopy.LAIAccums[i])) *
                         (canopy.beamScatteredBeamsNIR[i] / (canopy.beamScatteredBeamsNIR[i] + canopy.beamExtCoeffsNIR[i]))) -
                         ((1 - canopy.leafScatteringCoeffsNIR[i]) *
                          ((i == 0 ? 1 : Math.Exp(-2 * canopy.beamExtCoeffsNIR[i] * canopy.LAIAccums[i - 1])) -
                          Math.Exp(-2 * canopy.beamExtCoeffsNIR[i] * canopy.LAIAccums[i])) / 2));
                }
            }
        }
        //----------------------------------------------------------------------
        /// <summary>
        /// 
        /// </summary>
        /// <param name="canopy"></param>
        /// <param name="shaded"></param>
        /// <param name="PM"></param>
        public void calcRubiscoActivity25(LeafCanopy canopy, SunlitShadedCanopy shaded, PhotosynthesisModel PM)
        {
            for (int i = 0; i < _nLayers; i++)
            {
                VcMax25[i] = canopy.LAI * canopy.CPath.psiVc * (canopy.leafNTopCanopy - canopy.CPath.structuralN) *
                   ((i == 0 ? 1 : Math.Exp(-(canopy.beamExtCoeffs[i] + canopy.NAllocationCoeff / canopy.LAI) * canopy.LAIAccums[i - 1])) -
                   Math.Exp(-(canopy.beamExtCoeffs[i] + canopy.NAllocationCoeff / canopy.LAI) * canopy.LAIAccums[i])) /
                   (canopy.NAllocationCoeff + canopy.beamExtCoeffs[i] * canopy.LAI);
            }
        }
        //----------------------------------------------------------------------
        /// <summary>
        /// 
        /// </summary>
        /// <param name="canopy"></param>
        /// <param name="shaded"></param>
        /// <param name="PM"></param>
        public void calcRdActivity25(LeafCanopy canopy, SunlitShadedCanopy shaded, PhotosynthesisModel PM)
        {
            for (int i = 0; i < _nLayers; i++)
            {
                Rd25[i] = canopy.LAI * canopy.CPath.psiRd * (canopy.leafNTopCanopy - canopy.CPath.structuralN) *
                    ((i == 0 ? 1 : Math.Exp(-(canopy.beamExtCoeffs[i] + canopy.NAllocationCoeff / canopy.LAI) * canopy.LAIAccums[i - 1])) -
                    Math.Exp(-(canopy.beamExtCoeffs[i] + canopy.NAllocationCoeff / canopy.LAI) * canopy.LAIAccums[i])) /
                    (canopy.NAllocationCoeff + canopy.beamExtCoeffs[i] * canopy.LAI);
            }
        }
        //---------------------------------------------------------------------------------------------------------
        /// <summary>
        /// 
        /// </summary>
        /// <param name="canopy"></param>
        /// <param name="shaded"></param>
        /// <param name="PM"></param>
        public void calcElectronTransportRate25(LeafCanopy canopy, SunlitShadedCanopy shaded, PhotosynthesisModel PM)
        {
            for (int i = 0; i < _nLayers; i++)
            {
                J2Max25[i] = canopy.LAI * canopy.CPath.psiJ2 * (canopy.leafNTopCanopy - canopy.CPath.structuralN) *
                    ((i == 0 ? 1 : Math.Exp(-(canopy.beamExtCoeffs[i] + canopy.NAllocationCoeff / canopy.LAI) * canopy.LAIAccums[i - 1])) -
                    Math.Exp(-(canopy.beamExtCoeffs[i] + canopy.NAllocationCoeff / canopy.LAI) * canopy.LAIAccums[i])) /
                    (canopy.NAllocationCoeff + canopy.beamExtCoeffs[i] * canopy.LAI);

                JMax25[i] = canopy.LAI * canopy.CPath.psiJ * (canopy.leafNTopCanopy - canopy.CPath.structuralN) *
                    ((i == 0 ? 1 : Math.Exp(-(canopy.beamExtCoeffs[i] + canopy.NAllocationCoeff / canopy.LAI) * canopy.LAIAccums[i - 1])) -
                    Math.Exp(-(canopy.beamExtCoeffs[i] + canopy.NAllocationCoeff / canopy.LAI) * canopy.LAIAccums[i])) /
                    (canopy.NAllocationCoeff + canopy.beamExtCoeffs[i] * canopy.LAI);
            }
        }

        //---------------------------------------------------------------------------------------------------------
        /// <summary>
        /// 
        /// </summary>
        /// <param name="canopy"></param>
        /// <param name="shaded"></param>
        /// <param name="PM"></param>
        public void calcPRate25(LeafCanopy canopy, SunlitShadedCanopy shaded, PhotosynthesisModel PM)
        {
            for (int i = 0; i < _nLayers; i++)
            {
                VpMax25[i] = canopy.LAI * canopy.CPath.psiVp * (canopy.leafNTopCanopy - canopy.CPath.structuralN) *
                    ((i == 0 ? 1 : Math.Exp(-(canopy.beamExtCoeffs[i] + canopy.NAllocationCoeff / canopy.LAI) * canopy.LAIAccums[i - 1])) -
                    Math.Exp(-(canopy.beamExtCoeffs[i] + canopy.NAllocationCoeff / canopy.LAI) * canopy.LAIAccums[i])) /
                    (canopy.NAllocationCoeff + canopy.beamExtCoeffs[i] * canopy.LAI);
            }
        }
    }
}
