using System;
using System.Collections.Generic;
using System.Linq;
using Models.Core;
using Models.Core.ApsimFile;
using Models.Soils;
using Models.Soils.NutrientPatching;
using Models.Soils.Nutrients;
using Models.Surface;

namespace Models.AgPasture;

/// <summary>
/// This class encapsulates urine deposition to the soil using explicit patches (a separate zone for each patch).
/// </summary>
public class UrineDungReturnPatches
{
    private bool pseudoPatches;
    private IModel model;
    private ISummary summary;
    private int numPatches;
    private int numLayers;
    private Random random;
    private List<Zone> zones;
    private NutrientPatchManager patchManager;  // only used for pseudo patches.
    int patchNumForUrine = -1;  // this will be incremented to 0 (first zone) below 
    bool newlyInitialised = true;
    private double[] urineDepthPenetration;
    private UrineReturnPatterns urineReturnPattern;

    /// <summary>The different methods for urine return</summary>
    public enum UrineReturnPatterns
    {
        /// <summary>Rotating in order</summary>
        RotatingInOrder,
        /// <summary>Not enabled Random</summary>
        Random,
        /// <summary>Not enabled Pseudo-random</summary>
        PseudoRandom
    }

    ///<summary>Initialise the patching system. Call at PreLink stage.</summary>
    ///<param name="pseudoPatches">Pseudo patches?</param>
    ///<param name="model">The model that is going to do the urine deposition.</param>
    ///<param name="numPatches">The number of patches to use.</param>
    ///<param name="maxEffectiveNConcentration">The maximum effective N concentration (kg/ha).</param>
    ///<param name="urineDepthPenetration">The depth of urine penetration (mm)</param>
    ///<param name="urineReturnPattern">The urine return pattern</param>
    public void Initialise(bool pseudoPatches, IModel model, int numPatches, double maxEffectiveNConcentration, double urineDepthPenetration, UrineReturnPatterns urineReturnPattern)
    {
        this.pseudoPatches = pseudoPatches;
        this.model = model;
        this.numPatches = numPatches;
        this.urineReturnPattern = urineReturnPattern;
        
        var simulation = model.FindAncestor<Simulation>();
        summary = simulation.FindInScope<ISummary>();
        var zone = simulation.FindChild<Zone>();
        var physical = zone.FindInScope<IPhysical>();
        numLayers = physical.Thickness.Length;

        this.urineDepthPenetration = UrinePenetration(physical.Thickness, urineDepthPenetration);

        if (pseudoPatches)
            patchManager = InitialisePseudoPatches(numPatches, maxEffectiveNConcentration, zone);
        else
            InitialiseExplicitPatches(numPatches, maxEffectiveNConcentration, zone);

        zones = simulation.FindAllChildren<Zone>().ToList();
    }

    ///<summary>Do urine, dung, trampling</summary>
    ///<param name="urineDungReturn">The amount of urine and dung to return to soil.</param>
    public void DoUrineDungTrampling(UrineDungReturn.UrineDung urineDungReturn)
    {    
        // Patchy has the commented code below to convert from harvested DM and N to amount of urine and dung N.
        // SimpleGrazing uses digestibility to calculate this. The urineDungReturn argument to this method are from SimpleGrazing.
        /* if (UrineReturnType == UrineReturnTypes.FromHarvest)
        {
            AmountUrineNReturned = HarvestedN * 0.50;  // 
            AmountDungNReturned = HarvestedN * 0.35;  // 
            AmountDungCReturned = AmountDungNReturned * 20;
        }
        else if (UrineReturnType == UrineReturnTypes.SetMonthly)
        {
            AmountUrineNReturned = MonthlyUrineNAmt[clock.Today.Month - 1];   //  hardcoded as an input
            AmountDungNReturned = AmountUrineNReturned / 0.50 * 0.35;  // 
            AmountDungCReturned = AmountDungNReturned * 20;
        } */

        if (newlyInitialised)
        {
            if (pseudoPatches)
                summary.WriteMessage(model, "Urine deposition is via explicit patches.", MessageType.Information);
            else
                summary.WriteMessage(model, "Urine deposition is via pseudo patches.", MessageType.Information);
            summary.WriteMessage(model, $"Created {numPatches} patches, each of area {1.0 / numPatches} ha", MessageType.Information);
            for (int i = 0; i < numLayers; i++)
                summary.WriteMessage(model, "The proportion of urine applied to the " + i + "th layer will be " + urineDepthPenetration[i], MessageType.Diagnostic);

            newlyInitialised = false;
        }

        if (urineDungReturn.UrineNToSoil > 0)
        {
            summary.WriteMessage(model, $"The amount of urine N returned to the whole paddock is {urineDungReturn.UrineNToSoil}", MessageType.Diagnostic);

            // Do urine return.
            DeterminePatchForUrineReturn();  
            summary.WriteMessage(model, $"The zone for urine return is {patchNumForUrine}", MessageType.Diagnostic);

            double[] UreaToAdd = new double[numLayers];  
            for (int i = 0; i < numLayers; i++)
                UreaToAdd[i] = urineDepthPenetration[i]  * urineDungReturn.UrineNToSoil * numPatches;

            if (pseudoPatches)
                DoUrineReturnPseudoPatches(UreaToAdd);
            else
                DoUrineReturnExplicitPatches(UreaToAdd);

            foreach (Zone zone in zones)
            {
                var surfaceOrganicMatter = zone.FindInScope<SurfaceOrganicMatter>(); 
                UrineDungReturn.DoDungReturn(urineDungReturn, surfaceOrganicMatter);  // Note that dung is assumed to be spread uniformly over the paddock (patches or zones).
                UrineDungReturn.DoTrampling(surfaceOrganicMatter, fractionResidueIncorporated: 0.1);
            }
        }
    }

    /// <summary>
    /// Do urine return for explicit patches.
    /// </summary>    
    /// <param name="ureaToAdd">The amount of urea to add (kg/ha)</param>
    private void DoUrineReturnExplicitPatches(double[] ureaToAdd)
    {
        SolutePatch urea = zones[patchNumForUrine].FindInScope<SolutePatch>("Urea");
        urea.AddKgHaDelta(SoluteSetterType.Fertiliser, ureaToAdd);
        summary.WriteMessage(model, $"The local load was {ureaToAdd.Sum()} kg N /ha", MessageType.Diagnostic);
    }

    /// <summary>
    /// Do urine return for pseudo patches.
    /// </summary>    
    /// <param name="ureaToAdd">The amount of urea to add (kg/ha)</param>
    private void DoUrineReturnPseudoPatches(double[] ureaToAdd)
    {
        AddSoilCNPatchType CurrentPatch = new()
        {
            Sender = "manager",
            DepositionType = DepositionTypeEnum.ToSpecificPatch,
            AffectedPatches_id = new int[1] { patchNumForUrine },
            AffectedPatches_nm = Array.Empty<string>(),
            Urea = ureaToAdd
        };

        summary.WriteMessage(model, "Patch MinN prior to urine return: " + patchManager.MineralNEachPatch[patchNumForUrine], MessageType.Diagnostic);
        patchManager.Add(CurrentPatch); 
        summary.WriteMessage(model, "Patch MinN after urine return: " + patchManager.MineralNEachPatch[patchNumForUrine], MessageType.Diagnostic);
    }    

    /// <summary>
    /// Determine the patch number to add the urea to.
    /// </summary>    
    private void DeterminePatchForUrineReturn()
    {
        if (urineReturnPattern == UrineReturnPatterns.RotatingInOrder) 
        {
            patchNumForUrine += 1;  //increment the zone number - it was initialised at -1. NOTE, patchNumForUrine is used for both zones and patches
            if (patchNumForUrine >= numPatches)
                patchNumForUrine = 0;  // but reset back to the first patch if needed
        }
        else if (urineReturnPattern == UrineReturnPatterns.Random)
        {
            random ??= new Random();
            patchNumForUrine = random.Next(0, numPatches);  // in C# the maximum value (ZoneCount) will not be selected
        }
        else if (urineReturnPattern == UrineReturnPatterns.PseudoRandom)
        {
            random ??= new Random(666);                     // sets a constant seed value
            patchNumForUrine = random.Next(0, numPatches);  // in C# the maximum value (ZoneCount) will not be selected
        }
    }

    /// <summary>
    /// Calculate urine penetration into each layer (0-1)
    /// </summary>    
    /// <param name="thickness">Layer thicknesses.</param>
    /// <param name="urinePenetration">The depth of urine penetration (mm).</param>
    private static double[] UrinePenetration(double[] thickness, double urinePenetration)
    {
        double tempDepth = 0.0;
        double[] urineDepthPenetration = new double[thickness.Length];
        for (int i = 0; i <= (thickness.Length - 1); i++)
        {
            tempDepth += thickness[i];
            if (tempDepth <= urinePenetration)
            {
                urineDepthPenetration[i] = thickness[i] / urinePenetration;
            }
            else
            {
                urineDepthPenetration[i] = (urinePenetration - (tempDepth - thickness[i])) / 
                                                (tempDepth - (tempDepth - thickness[i])) * thickness[i] / urinePenetration;
                urineDepthPenetration[i] = Math.Max(0.0, Math.Min(1.0, urineDepthPenetration[i]));
            }
        }
        return urineDepthPenetration;
    } 


    /// <summary>
    /// Initialise explicit patches
    /// </summary>    
    /// <param name="numPatches">The number of patches to create.</param>
    /// <param name="maxEffectiveNConcentration">The maximum effective N concentration.</param>
    /// <param name="zone">The zone in the simulation.</param>
    private static void InitialiseExplicitPatches(int numPatches, double maxEffectiveNConcentration, Zone zone)
    {
        ModifyZoneForPatches(zone, maxEffectiveNConcentration);

        // Clone the first zone as many times as needed to get the required number of patches.
        zone.Area = 1.0 / numPatches;  // this will apply to all the new zones
        for (int i = 0; i < numPatches - 1; i++)
            Structure.Add(Apsim.Clone(zone), zone.Parent as Simulation);
    }

    /// <summary>
    /// Initialise pseudo patches
    /// </summary>    
    /// <param name="numPatches">The number of patches to create.</param>
    /// <param name="maxEffectiveNConcentration">The maximum effective N concentration.</param>
    /// <param name="zone">The zone in the simulation.</param>
    private static NutrientPatchManager InitialisePseudoPatches(int numPatches, double maxEffectiveNConcentration, Zone zone)
    {
        var patchManager = ModifyZoneForPatches(zone, maxEffectiveNConcentration);

        // Clone the first zone as many times as needed to get the required number of patches.
        zone.Area = 1.0;

        patchManager.NPartitionApproach = PartitionApproachEnum.BasedOnConcentrationAndDelta;
        patchManager.AutoAmalgamationApproach = AutoAmalgamationApproachEnum.None;
        patchManager.basePatchApproach = BaseApproachEnum.IDBased;
        patchManager.AllowPatchAmalgamationByAge = false;
        patchManager.PatchAgeForForcedMerge = 1000000.0;  // ie don't merge                                

        AddSoilCNPatchType NewPatch = new()
        {
            DepositionType = DepositionTypeEnum.ToNewPatch,
            AreaFraction = 1.0 / numPatches,
            AffectedPatches_id = new int[] { 0 },
            AffectedPatches_nm = new string[1] { "0" },
            SuppressMessages = false
        };
        for (int i = 0; i < numPatches - 1; i++)
            patchManager.Add(NewPatch);

        return patchManager;
    }  

    /// <summary>
    /// Modify a zone to make it ready for patching.
    /// </summary>    
    /// <param name="zone">The zone in the simulation.</param>
    /// <param name="maxEffectiveNConcentration">The maximum effective N concentration.</param>
    private static NutrientPatchManager ModifyZoneForPatches(Zone zone, double maxEffectiveNConcentration)
    {
        Soil soil = zone.FindChild<Soil>();

        // Remove nutrient.
        Nutrient nutrient = soil.FindChild<Nutrient>();
        Structure.Delete(nutrient);

        // Replace all solutes with SolutePatch instances.
        foreach (var solute in soil.FindAllChildren<Solute>())
        {
            SolutePatch newSolute = new()
            {
                Name = solute.Name,
                InitialValues = solute.InitialValues,
                InitialValuesUnits = solute.InitialValuesUnits
            };
            Structure.Delete(solute);
            Structure.Add(newSolute, soil);
        }

        // Add NutrientPatchManager.
        var physical = zone.FindInScope<Physical>();
        double[] maxEffectiveNConcentrationByLayer = Enumerable.Repeat(maxEffectiveNConcentration, physical.Thickness.Length).ToArray();
        NutrientPatchManager patchManager = new()
        {
            MaximumNO3AvailableToPlants = maxEffectiveNConcentrationByLayer,
            MaximumNH4AvailableToPlants = maxEffectiveNConcentrationByLayer
        };

        Structure.Add(patchManager, soil);
        return patchManager;
    }
}