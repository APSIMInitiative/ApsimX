namespace Models.WaterModel
{
    using APSIM.Shared.Utilities;
    using Core;
    using Functions;
    using System;

    /// <summary>
    /// Runoff from rainfall is calculated using the USDA-Soil Conservation Service procedure known as the curve number technique. 
    /// The procedure uses total precipitation from one or more storms occurring on a given day to estimate runoff.
    /// The relation excludes duration of rainfall as an explicit variable, and so rainfall intensity is ignored.
    /// When irrigation is applied it can optionally be included in the runoff calculation. This flag (willRunoff) can be set
    /// when applying irrigation.
    /// 
    /// ![Alt Text](RunoffRainfallCurves.png)
    /// Figure: Runoff response curves (ie runoff as a function of total daily rainfall) are specified by numbers from 0 (no runoff) to 100 (all runoff). 
    /// Response curves for three runoff curve numbers for rainfall varying between 0 and 100 mm per day.
    /// 
    /// The user supplies a curve number for average antecedent rainfall conditions (CN2Bare). 
    /// From this value the wet (high runoff potential) response curve and the dry (low runoff potential) 
    /// response curve are calculated. The SoilWater module will then use the family of curves between these 
    /// two extremes for calculation of runoff depending on the daily moisture status of the soil. 
    /// The effect of soil moisture on runoff is confined to the effective hydraulic depth as specified in the 
    /// module's ini file and is calculated to give extra weighting to layers closer to the soil surface.
    /// ![Alt Text](RunoffResponseCurve.png)
    /// Figure: Runoff response curves (ie runoff as a function of total daily rainfall) are specified by numbers from 0 (no runoff) to 100 (all runoff). 
    ///
    /// ![Alt Text](CurveNumberCover.png) 
    /// Figure: Residue cover effect on runoff curve number where bare soil curve number is 75 and total reduction in 
    /// curve number is 20 at 80% cover. 
    /// 
    /// Surface residues inhibit the transport of water across the soil surface during runoff events and so different 
    /// families of response curves are used according to the amount of crop and residue cover.The extent of the effect 
    /// on runoff is specified by a threshold surface cover (CNCov), above which there is no effect, and the corresponding 
    /// curve number reduction (CNRed). 
    ///
    /// Tillage of the soil surface also reduces runoff potential, and a similar modification of Curve Number is used to 
    /// represent this process. A tillage event is directed to the module, specifying cn_red, the CN reduction, and cn_rain, 
    /// the rainfall amount required to remove the tillage roughness. CN2 is immediately reduced and increases linearly with 
    /// cumulative rain, ie.roughness is smoothed out by rain. 
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.ProfileView")]
    [PresenterName("UserInterface.Presenters.ProfilePresenter")]
    [ValidParent(ParentType = typeof(WaterBalance))]
    public class RunoffModel : Model, IFunction
    {
        /// <summary>The water movement model.</summary>
        [Link]
        private WaterBalance soil = null;

        /// <summary>The summary file model.</summary>
        [Link]
        private ISummary summary = null;

        /// <summary>A function for reducing CN due to cover.</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction cnReductionForCover = null;

        /// <summary>A function for reducing CN due to tillage.</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction cnReductionForTillage = null;

        /// <summary>Effective hydraulic depth (mm)</summary>
        private double hydrolEffectiveDepth = 450;

        /// <summary>Cumulative rainfall below which tillage reduces CN (mm).</summary>
        public double TillageCnCumWater { get; set; }

        /// <summary>Reduction in CN due to tillage()</summary>
        public double TillageCnRed { get; set; }

        /// <summary>Running total of cumulative rainfall since last tillage event. Used for tillage CN reduction (mm).</summary>
        public double CumWaterSinceTillage { get; set; }


        /// <summary>Calculate and return the runoff (mm).</summary>
        public double Value(int arrayIndex = -1)
        {
            double runoff = 0.0;

            if (soil.PotentialRunoff > 0.0)
            {
                double cn2New = soil.CN2Bare - cnReductionForCover.Value(arrayIndex) - cnReductionForTillage.Value(arrayIndex);

                // Tillage reduction on cn
                if (TillageCnCumWater > 0.0)
                {
                    // We minus 1 because we want the opposite fraction. 
                    // Tillage Reduction is biggest (CnRed value) straight after Tillage and gets smaller and becomes 0 when reaches CumWater.
                    // unlike the Cover Reduction, where the reduction starts out smallest (0) and gets bigger and becomes (CnRed value) when you hit CnCover.
                    var tillageFract = MathUtilities.Divide(CumWaterSinceTillage, TillageCnCumWater, 0.0) - 1.0;
                    var tillageReduction = TillageCnRed * tillageFract;
                    cn2New = cn2New + tillageReduction;
                }

                // Cut off response to cover at high covers
                cn2New = MathUtilities.Bound(cn2New, 0.0, 100.0);

                // Calculate CN proportional in dry range (dul to ll15)
                double[] runoff_wf = RunoffWeightingFactor();
                double[] SW = soil.Water;
                double[] LL15 = MathUtilities.Multiply(soil.Properties.LL15, soil.Properties.Thickness);
                double[] DUL = MathUtilities.Multiply(soil.Properties.DUL, soil.Properties.Thickness);
                double cnpd = 0.0;
                for (int i = 0; i < soil.Properties.Thickness.Length; i++)
                {
                    double DULFraction = MathUtilities.Divide((SW[i] - LL15[i]), (DUL[i] - LL15[i]), 0.0);
                    cnpd = cnpd + DULFraction * runoff_wf[i];
                }
                cnpd = MathUtilities.Bound(cnpd, 0.0, 1.0);

                // curve no. for dry soil (antecedant) moisture
                double cn1 = MathUtilities.Divide(cn2New, (2.334 - 0.01334 * cn2New), 0.0);

                // curve no. for wet soil (antecedant) moisture
                double cn3 = MathUtilities.Divide(cn2New, (0.4036 + 0.005964 * cn2New), 0.0);

                // scs curve number
                double cn = cn1 + (cn3 - cn1) * cnpd;

                // curve number will be decided from scs curve number table ??dms
                // s is potential max retention (surface ponding + infiltration)
                double s = 254.0 * (MathUtilities.Divide(100.0, cn, 1000000.0) - 1.0);
                double xpb = soil.PotentialRunoff - 0.2 * s;
                xpb = Math.Max(xpb, 0.0);

                // assign the output variable
                runoff = MathUtilities.Divide(xpb * xpb, (soil.PotentialRunoff + 0.8 * s), 0.0);

                CumWaterSinceTillage += soil.PotentialRunoff;
                ShouldIStopTillageCNReduction();  //NB. this needs to be done _after_ cn calculation.

                // bound check the ouput variable
                return MathUtilities.Bound(runoff, 0.0, soil.PotentialRunoff);
            }

            return 0.0;
        }

        // --- Private methods ---------------------------------------------------------------

        /// <summary>
        /// Calculate the weighting factor hydraulic effectiveness used
        /// to weight the effect of soil moisture on runoff.
        /// </summary>
        /// <returns>Weighting factor for runoff</returns>
        private double[] RunoffWeightingFactor()
        {
            double cumRunoffWeightingFactor = 0.0;

            // Get sumulative depth (mm)
            double[] cumThickness = APSIM.Shared.APSoil.SoilUtilities.ToCumThickness(soil.Properties.Thickness);

            // Ensure hydro effective depth doesn't go below bottom of soil.
            hydrolEffectiveDepth = Math.Min(hydrolEffectiveDepth, MathUtilities.Sum(soil.Properties.Thickness));

            // Scaling factor for wf function to sum to 1
            double scaleFactor = 1.0 / (1.0 - Math.Exp(-4.16));

            // layer number that the effective depth occurs
            int hydrolEffectiveLayer =soil.Properties.LayerIndexOfDepth(hydrolEffectiveDepth);

            double[] runoffWeightingFactor = new double[soil.Properties.Thickness.Length];
            for (int i = 0; i <= hydrolEffectiveLayer; i++)
            {
                double cumDepth = cumThickness[i];
                cumDepth = Math.Min(cumDepth, hydrolEffectiveDepth);

                // assume water content to hydrol_effective_depth affects runoff
                // sum of wf should = 1 - may need to be bounded? <dms 7-7-95>
                runoffWeightingFactor[i] = scaleFactor * (1.0 - Math.Exp(-4.16 * MathUtilities.Divide(cumDepth, hydrolEffectiveDepth, 0.0)));
                runoffWeightingFactor[i] = runoffWeightingFactor[i] - cumRunoffWeightingFactor;  
                cumRunoffWeightingFactor += runoffWeightingFactor[i];
            }

            // Ensure total runoff weighting factor equals 1.
            if (!MathUtilities.FloatsAreEqual(MathUtilities.Sum(runoffWeightingFactor), 1.0))
                throw new Exception("Internal error. Total runoff weighting factor must be equal to one.");

            return runoffWeightingFactor;
        }

        /// <summary>
        /// Accumulate rainfall for tillage cn reduction.
        /// The reduction in the runoff as a result of doing a tillage (tillage_cn_red) ceases after a set amount of rainfall (tillage_cn_rain).
        /// This function works out the accumulated rainfall since last tillage event, and turns off the reduction if it is over the amount of rain specified.
        /// </summary>
        private void ShouldIStopTillageCNReduction()
        {
            if (TillageCnCumWater > 0.0 && CumWaterSinceTillage > TillageCnCumWater)
            {
                // This tillage has lost all effect on cn. CN reduction
                // due to tillage is off until the next tillage operation.
                TillageCnCumWater = 0.0; 
                TillageCnRed = 0.0;

                summary.WriteMessage(this, "Tillage CN reduction finished");
            }
        }
    }
}
