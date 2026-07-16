using System;
using System.Linq;
using System.Reflection;
using APSIM.Core;
using APSIM.Numerics;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Interfaces;

namespace Models
{
    /// <summary>
    /// GenericFruitTree-specific microclimate zone that can consume preloaded canopy LightProfile
    /// (per-layer LAI + thickness) before shortwave partitioning.
    /// </summary>
    [Serializable]
    public class PerennialMicroClimateZone : MicroClimateZone
    {
        /// <summary>Create a perennial microclimate zone wrapper for a target zone.</summary>
        /// <param name="clockModel">Clock model.</param>
        /// <param name="zoneModel">Target zone model.</param>
        /// <param name="structure">Structure instance.</param>
        /// <param name="minHeightDiffForNewLayer">Minimum canopy height difference to create a new layer (m).</param>
        public PerennialMicroClimateZone(IClock clockModel, Zone zoneModel, IStructure structure, double minHeightDiffForNewLayer)
            : base(clockModel, zoneModel, structure, minHeightDiffForNewLayer)
        {
        }

        /// <summary>
        /// Replaces the base canopy compartment pipeline so LAI layering can come from a preloaded LightProfile.
        /// </summary>
        public new void DoCanopyCompartments()
        {
            DefineLayersForPerennialCanopy();
            DivideComponentsUsingLightProfile();
            CalculateLightExtinctionVariablesUsingLayeredLai();
        }

        /// <summary>Break the combined canopy into layers (copied from MicroClimateZone).</summary>
        private void DefineLayersForPerennialCanopy()
        {
            double[] nodes = new double[2 * Canopies.Count];
            int numNodes = 1;
            for (int compNo = 0; compNo <= Canopies.Count - 1; compNo++)
            {
                double heightMetres = Math.Round(Canopies[compNo].Canopy.Height, 5) / 1000.0;
                double depthMetres = Math.Round(Canopies[compNo].Canopy.Depth, 5) / 1000.0;
                double canopyBase = heightMetres - depthMetres;
                if (IsNewLayer(nodes, heightMetres, numNodes))
                {
                    nodes[numNodes] = heightMetres;
                    numNodes++;
                }
                if (Array.IndexOf(nodes, canopyBase) == -1)
                {
                    nodes[0] = canopyBase;
                    numNodes++;
                }
            }

            Array.Resize(ref nodes, numNodes);
            Array.Sort(nodes);
            nodes = nodes.Distinct().ToArray();
            numLayers = nodes.Length - 1;

            if (DeltaZ.Length != numLayers)
            {
                Array.Resize(ref DeltaZ, numLayers);
                Array.Resize(ref layerKtot, numLayers);
                Array.Resize(ref LAItotsum, numLayers);

                for (int j = 0; j <= Canopies.Count - 1; j++)
                {
                    Array.Resize(ref Canopies[j].Ftot, numLayers);
                    Array.Resize(ref Canopies[j].Fgreen, numLayers);
                    Array.Resize(ref Canopies[j].Rs, numLayers);
                    Array.Resize(ref Canopies[j].FRs, numLayers);
                    Array.Resize(ref Canopies[j].Rl, numLayers);
                    Array.Resize(ref Canopies[j].Rsoil, numLayers);
                    Array.Resize(ref Canopies[j].Gc, numLayers);
                    Array.Resize(ref Canopies[j].Ga, numLayers);
                    Array.Resize(ref Canopies[j].PET, numLayers);
                    Array.Resize(ref Canopies[j].PETr, numLayers);
                    Array.Resize(ref Canopies[j].PETa, numLayers);
                    Array.Resize(ref Canopies[j].Omega, numLayers);
                    Array.Resize(ref Canopies[j].interception, numLayers);
                }
            }

            for (int i = 0; i <= numLayers - 1; i++)
                DeltaZ[i] = nodes[i + 1] - nodes[i];
        }

        /// <summary>Create a new layer for the specified height?</summary>
        private bool IsNewLayer(double[] nodes, double height, int numNodes)
        {
            bool found = false;
            for (int i = 1; i < numNodes; i++)
                if (Math.Abs(nodes[i] - height) < MinimumHeightDiffForNewLayer)
                    found = true;
            return !found;
        }

        /// <summary>
        /// Populate layer LAI using preloaded canopy LightProfile when available; otherwise fallback to stock logic.
        /// </summary>
        private void DivideComponentsUsingLightProfile()
        {
            double top = 0.0;
            double bottom = 0.0;

            for (int i = 0; i <= numLayers - 1; i++)
            {
                bottom = top;
                top += DeltaZ[i];
                LAItotsum[i] = 0.0;

                for (int j = 0; j <= Canopies.Count - 1; j++)
                {
                    Array.Resize(ref Canopies[j].LAI, numLayers);
                    Array.Resize(ref Canopies[j].LAItot, numLayers);

                    Canopies[j].LAI[i] = 0.0;
                    Canopies[j].LAItot[i] = 0.0;

                    double heightMetres = Math.Round(Canopies[j].Canopy.Height, 5) / 1000.0;
                    double depthMetres = Math.Round(Canopies[j].Canopy.Depth, 5) / 1000.0;
                    double canopyBase = heightMetres - depthMetres;

                    bool intersectsLayer = (heightMetres > bottom) && (canopyBase < top);
                    if (!intersectsLayer)
                        continue;

                    if (TryLayerLaiFromPreloadedProfile(Canopies[j], bottom, top, canopyBase, depthMetres,
                        out double laiGreenLayer, out double laiTotalLayer))
                    {
                        Canopies[j].LAI[i] = Math.Max(0.0, laiGreenLayer);
                        Canopies[j].LAItot[i] = Math.Max(Canopies[j].LAI[i], laiTotalLayer);
                    }
                    else
                    {
                        double ld = MathUtilities.Divide(Canopies[j].Canopy.LAITotal, depthMetres, 0.0);
                        Canopies[j].LAItot[i] = ld * DeltaZ[i];
                        Canopies[j].LAI[i] = Canopies[j].LAItot[i] * MathUtilities.Divide(Canopies[j].Canopy.LAI, Canopies[j].Canopy.LAITotal, 0.0);
                    }

                    LAItotsum[i] += Canopies[j].LAItot[i];
                }

                for (int j = 0; j <= Canopies.Count - 1; j++)
                {
                    Canopies[j].Ftot[i] = MathUtilities.Divide(Canopies[j].LAItot[i], LAItotsum[i], 0.0);
                    Canopies[j].Fgreen[i] = MathUtilities.Divide(Canopies[j].LAI[i], LAItotsum[i], 0.0);
                }
            }
        }

        /// <summary>
        /// Keep stock cover-derived K terms, but apply them over layer LAI built from preloaded profiles.
        /// </summary>
        private void CalculateLightExtinctionVariablesUsingLayeredLai()
        {
            for (int j = 0; j <= Canopies.Count - 1; j++)
            {
                if (MathUtilities.FloatsAreEqual(Canopies[j].Canopy.CoverGreen, 1.0, 1E-10))
                    throw new Exception("Unrealistically high cover value in MicroMet i.e. > 0.999999999");

                Canopies[j].K = MathUtilities.Divide(-Math.Log(1.0 - Canopies[j].Canopy.CoverGreen), Canopies[j].Canopy.LAI, 0.0);
                Canopies[j].Ktot = MathUtilities.Divide(-Math.Log(1.0 - Canopies[j].Canopy.CoverTotal), Canopies[j].Canopy.LAITotal, 0.0);
            }

            for (int i = 0; i <= numLayers - 1; i++)
            {
                layerKtot[i] = 0.0;
                for (int j = 0; j <= Canopies.Count - 1; j++)
                    layerKtot[i] += Canopies[j].Ftot[i] * Canopies[j].Ktot;
            }
        }

        private static bool TryLayerLaiFromPreloadedProfile(
            MicroClimateCanopy microClimateCanopy,
            double layerBottom,
            double layerTop,
            double canopyBase,
            double canopyDepth,
            out double laiGreenLayer,
            out double laiTotalLayer)
        {
            laiGreenLayer = 0.0;
            laiTotalLayer = 0.0;

            var profile = GetCanopyLightProfile(microClimateCanopy?.Canopy);
            if (profile == null || profile.Length == 0)
                return false;

            double profileDepth = profile.Sum(p => Math.Max(0.0, p.thickness));
            if (profileDepth <= 0.0)
                return false;

            double targetDepth = Math.Max(1e-9, canopyDepth);
            double depthScale = targetDepth / profileDepth;

            double segmentBottom = canopyBase;
            foreach (var segment in profile)
            {
                double rawThickness = Math.Max(0.0, segment.thickness);
                if (rawThickness <= 0.0)
                    continue;

                double segmentThickness = rawThickness * depthScale;
                double segmentTop = segmentBottom + segmentThickness;

                double overlap = Math.Max(0.0, Math.Min(segmentTop, layerTop) - Math.Max(segmentBottom, layerBottom));
                if (overlap > 0.0)
                {
                    double fraction = MathUtilities.Divide(overlap, segmentThickness, 0.0);
                    double greenAmount = Math.Max(0.0, segment.AmountOnGreen);
                    double deadAmount = Math.Max(0.0, segment.AmountOnDead);

                    laiGreenLayer += greenAmount * fraction;
                    laiTotalLayer += (greenAmount + deadAmount) * fraction;
                }

                segmentBottom = segmentTop;
            }

            return true;
        }

        private static CanopyEnergyBalanceInterceptionlayerType[] GetCanopyLightProfile(ICanopy canopy)
        {
            if (canopy == null)
                return null;

            PropertyInfo lightProfileProperty = canopy.GetType().GetProperty("LightProfile");
            if (lightProfileProperty == null || !lightProfileProperty.CanRead)
                return null;

            return lightProfileProperty.GetValue(canopy) as CanopyEnergyBalanceInterceptionlayerType[];
        }
    }
}
