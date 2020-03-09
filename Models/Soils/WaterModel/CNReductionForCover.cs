
namespace Models.WaterModel
{
    using APSIM.Shared.Utilities;
    using Core;
    using Interfaces;
    using Models.Functions;
    using Models.Surface;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>Implements the curve number reduction caused by cover.</summary>
    [Serializable]
    public class CNReductionForCover : Model, IFunction
    {
        // --- Links -------------------------------------------------------------------------

        [Link]
        private WaterBalance waterBalance = null;

        /// <summary>A multiplier to CoverTot to get effective cover for runoff.</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private LinearInterpolationFunction EffectiveCoverMultiplier = null;
        
        /// <summary>A list of all canopies.</summary>
        [Link]
        private List<ICanopy> canopies = null;

        /// <summary>A link to SurfaceOrganicMatter</summary>
        [Link]
        private ISurfaceOrganicMatter surfaceOrganicMatter = null;

        // --- Settable properties -----------------------------------------------------------

        // --- Outputs -----------------------------------------------------------------------

        /// <summary>Canopy heights. Used by EffectiveCoverMultipler.</summary>
        public double[] CanopyHeights
        {
            get
            {
                List<double> canopyHeights = new List<double>();
                foreach (ICanopy canopy in canopies)
                    canopyHeights.Add(canopy.Height);

                return canopyHeights.ToArray();
            }
        }

        /// <summary>Returns the value to subtract from curve number due to cover.</summary>
        public double Value(int arrayIndex = -1)
        {
            double cover_surface_runoff = CalcCoverForRunoff();

            // Reduce CN2 for the day due to the cover effect
            // NB cover_surface_runoff should really be a parameter to this function
            // proportion of maximum cover effect on runoff (0-1)
            double cover_fract = MathUtilities.Divide(cover_surface_runoff, waterBalance.CNCov, 0.0);
            cover_fract = MathUtilities.Bound(cover_fract, 0.0, 1.0);
            double cover_reduction = waterBalance.CNRed * cover_fract;
            return cover_reduction;
        }

        // --- Methods -----------------------------------------------------------------------

        /// <summary>Calculate an effective cover that is used for runoff.</summary>
        /// <returns>The effective cover to use in the runoff calculations.</returns>
        private double CalcCoverForRunoff()
        {
            // Cover CN response from perfect   - ML  & dms 7-7-95
            // nb. PERFECT assumed crop canopy was 1/2 effect of mulch
            // This allows the taller canopies to have less effect on runoff
            // and the cover close to ground to have full effect.
            // weight effectiveness of crop canopies
            //    0 (no effect) to 1 (full effect)

            double coverSurfaceCrop = 0.0;  // efective total cover (0-1)
            for (int canopy = 0; canopy < canopies.Count; canopy++)
            {
                double effectiveCropCover = canopies[canopy].CoverTotal * EffectiveCoverMultiplier.ValueForX(canopies[canopy].Height);
                coverSurfaceCrop = addCover(coverSurfaceCrop, effectiveCropCover);
            }

            // add cover known to affect runoff i.e. residue with canopy shading residue         
            return addCover(coverSurfaceCrop, surfaceOrganicMatter.Cover);
        }

        /// <summary>Combines two cover values.</summary>
        /// <param name="cover1">First cover (0-1).</param>
        /// <param name="cover2">Second cover (0-1).</param>
        /// <returns></returns>
        private static double addCover(double cover1, double cover2)
        {
            // "cover1" and "cover2" are numbers between 0 and 1 which
            // indicate what fraction of sunlight is intercepted by the
            // foliage of plants. This function returns a number between
            // 0 and 1 indicating the fraction of sunlight intercepted
            // when "cover1" is combined with "cover2", i.e. both sets of
            // plants are present.

            double bare = (1.0 - cover1) * (1.0 - cover2);
            return (1.0 - bare);
        }
    }
}
