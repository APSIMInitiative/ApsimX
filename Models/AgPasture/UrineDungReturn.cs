using System;
using System.Collections.Generic;
using System.Linq;
using APSIM.Shared.Utilities;
using Models.ForageDigestibility;
using Models.Soils;
using Models.Surface;

namespace Models.AgPasture;

/// <summary>
/// This class encapsulates urine and dung deposition to the soil and lost to the simulation.
/// </summary>
public class UrineDungReturn
{
    /// <summary>
    /// Perform urine return to soil.
    /// </summary>
    /// <param name="grazedForages">Grazed forages.</param>
    /// <param name="thickness">Thickness of each layer.</param>
    /// <param name="urea">Urea solute</param>
    /// <param name="fractionDefoliatedBiomassToSoil"></param>
    /// <param name="fractionDefoliatedNToSoil"></param>
    /// <param name="fractionExcretedNToDung"></param>
    /// <param name="CNRatioDung"></param>
    /// <param name="depthUrineIsAdded"></param>
    /// <param name="fractionUrineLostToSimulation">Fraction of the urine lost to simulation.</param>
    /// <param name="fractionDungLostToSimulation">Fraction of the dung lost to simulation.</param>
    /// <returns>The amount of urine and dung added to soil and lost from the simulation</returns>
    public static UrineDung DoUrineReturn(List<DigestibleBiomass> grazedForages,
                                          double[] thickness,
                                          ISolute urea,
                                          double fractionDefoliatedBiomassToSoil,
                                          double fractionDefoliatedNToSoil,
                                          double fractionExcretedNToDung,
                                          double CNRatioDung,
                                          double depthUrineIsAdded,
                                          double fractionUrineLostToSimulation,
                                          double fractionDungLostToSimulation)
    {
        if (grazedForages.Any())
        {
            double returnedToSoilWt = 0;
            double returnedToSoilN = 0;
            foreach (var grazedForage in grazedForages)
            {
                returnedToSoilWt += fractionDefoliatedBiomassToSoil *
                                    (1 - grazedForage.Digestibility) * grazedForage.Total.Wt * 10;  // g/m2 to kg/ha
                returnedToSoilN += fractionDefoliatedNToSoil * grazedForage.Total.N * 10;           // g/m2 to kg/ha
            }

            double dungNReturned;
            if (double.IsNaN(fractionExcretedNToDung))
            {
                const double CToDMRatio = 0.4; // 0.4 is C:DM ratio.
                dungNReturned = Math.Min(returnedToSoilN, returnedToSoilWt * CToDMRatio / CNRatioDung);
            }
            else
                dungNReturned = fractionExcretedNToDung * returnedToSoilN;

            UrineDung deposition = new()
            {
                DungWtToSoil = returnedToSoilWt * (1.0 - fractionDungLostToSimulation),
                DungNToSoil = dungNReturned * (1.0 - fractionDungLostToSimulation),
                UrineNToSoil = (returnedToSoilN - dungNReturned) * (1 - fractionUrineLostToSimulation),
                DungWtLostFromSimulation = returnedToSoilWt * fractionDungLostToSimulation,
                DungNLostFromSimulation = dungNReturned * fractionDungLostToSimulation,
                UrineNLostFromSimulation = (returnedToSoilN - dungNReturned) * fractionUrineLostToSimulation
            };

            // We will do the urine and dung return.
            // find the layer that the urea is to be added to.
            int layer = SoilUtilities.LayerIndexOfDepth(thickness, depthUrineIsAdded);
            var ureaDelta = new double[thickness.Length];
            ureaDelta[layer] = deposition.UrineNToSoil;
            urea.AddKgHaDelta(SoluteSetterType.Fertiliser, ureaDelta);

            return deposition;
        }
        return null;
    }

    /// <summary>
    /// Perform dung return.
    /// </summary>
    /// <param name="deposition">The urine, dung deposition to the soil.</param>
    /// <param name="surfaceOrganicMatter">The SurfaceOrganicMatter model.</param>
    public static void DoDungReturn(UrineDung deposition,
                                    SurfaceOrganicMatter surfaceOrganicMatter)   
    {
        if (deposition.DungWtToSoil > 0)
        {
            var cropType = "RuminantDung_PastureFed";
            surfaceOrganicMatter.Add(deposition.DungWtToSoil, deposition.DungNToSoil, 0, cropType, null);
        }
    }

    /// <summary>
    /// Perform trampling.
    /// </summary>
    /// <param name="surfaceOrganicMatter">The surface organic matter model.</param>
    /// <param name="fractionResidueIncorporated">The fraction of residues incorporated by trampling.</param>
    public static void DoTrampling(SurfaceOrganicMatter surfaceOrganicMatter, double fractionResidueIncorporated = 0.1)
    {
        surfaceOrganicMatter.Incorporate(fractionResidueIncorporated, depth: 100);
    }


    /// <summary>Encapsulates calculated urine and dung return to soil</summary>
    public class UrineDung
    {
        /// <summary>The amount of N in dung returned to soil (kg/ha)</summary>
        public double DungNToSoil { get; set; }
        /// <summary>The amount of biomass in dung returned to soil (kg/ha)</summary>
        public double DungWtToSoil { get; set; }
        /// <summary>The amount of urine N returned to soil (kg/ha)</summary>
        public  double UrineNToSoil { get; set; }

        /// <summary>The amount of N lost to the simulation (kg/ha)</summary>
        public double DungNLostFromSimulation { get; set; }
        /// <summary>The amount of biomass lost to the simulation (kg/ha)</summary>
        public double DungWtLostFromSimulation { get; set; }
        /// <summary>The amount of urine N  lost to the simulation (kg/ha)</summary>
        public  double UrineNLostFromSimulation { get; set; }
    }

}