using System;
using System.Linq;
using Models.Core;
using Models.Core.ApsimFile;
using Models.Soils;
using Models.Soils.NutrientPatching;
using Models.Surface;
using static Models.AgPasture.SimpleGrazing;

namespace Models.AgPasture
{
    /// <summary>
    /// Encapsulates urine patch functionality.
    /// </summary>
    public class UrineDungPatches
    {
        private readonly SimpleGrazing simpleGrazing;
        private readonly bool pseudoPatches;
        private double[] monthlyUrineNAmt;                 // breaks the N balance but useful for testing
        private double[] urineDepthPenetrationArray;
        private Random pseudoRandom;
        private int pseudoRandomSeed;
        private readonly ISummary summary;
        private readonly Clock clock;
        private readonly Physical physical;

        // User properties.

        /// <summary>Number of patches or zones to create.</summary>
        private readonly int zoneCount;

        /// <summary>Urine return type</summary>
        private readonly UrineReturnTypes urineReturnType;

        /// <summary>Urine return pattern.</summary>
        private readonly UrineReturnPatterns urineReturnPattern;

        /// <summary>Depth of urine penetration (mm)</summary>
        private readonly double urineDepthPenetration;

        /// <summary>Maximum effective NO3-N or NH4-N concentration</summary>
        private readonly double maxEffectiveNConcentration;

        // Outputs.

        /// <summary>Zone or patch that urine will be applied to</summary>
        public int ZoneNumForUrine { get; private set; }

        /// <summary>Number of zones for applying urine</summary>
        public int NumZonesForUrine { get; private set; }

        /// <summary>Divisor for reporting</summary>
        public double DivisorForReporting { get; private set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="simpleGrazing">Parent SimpleGrazing model</param>
        /// <param name="pseudoPatches">Use pseudo patches?</param>
        /// <param name="zoneCount"></param>
        /// <param name="urineReturnType"></param>
        /// <param name="urineReturnPattern"></param>
        /// <param name="pseudoRandomSeed"></param>
        /// <param name="urineDepthPenetration"></param>
        /// <param name="maxEffectiveNConcentration"></param>
        public UrineDungPatches(SimpleGrazing simpleGrazing, bool pseudoPatches,
                                int zoneCount,
                                UrineReturnTypes urineReturnType,
                                UrineReturnPatterns urineReturnPattern,
                                int pseudoRandomSeed,
                                double urineDepthPenetration,
                                double maxEffectiveNConcentration)
        {
            this.simpleGrazing = simpleGrazing;
            this.pseudoPatches = pseudoPatches;
            this.zoneCount = zoneCount;
            this.urineReturnType = urineReturnType;
            this.urineReturnPattern = urineReturnPattern;
            this.pseudoRandomSeed = pseudoRandomSeed;
            this.urineDepthPenetration = urineDepthPenetration;
            this.maxEffectiveNConcentration = maxEffectiveNConcentration;
            summary = simpleGrazing.FindInScope<ISummary>();
            clock = simpleGrazing.FindInScope<Clock>();
            physical = simpleGrazing.FindInScope<Physical>();
        }

        /// <summary>
        /// Invoked by the infrastructure before the simulation gets created in memory.
        /// Use this to create patches.
        /// </summary>
        public void OnPreLink()
        {
            var simulation = simpleGrazing.FindAncestor<Simulation>() as Simulation;
            var zone = simulation.FindChild<Zone>();

            if (zoneCount == 0)
                throw new Exception("Number of patches/zones in urine patches is zero.");

            if (pseudoPatches)
            {
                zone.Area = 1.0;

                var patchManager = simpleGrazing.FindInScope<NutrientPatchManager>();
                if (patchManager == null)
                    throw new Exception("Cannot find NutrientPatchManager");
                var soilPhysical = simpleGrazing.FindInScope<Physical>();
                if (patchManager == null)
                    throw new Exception("Cannot find Physical");

                double[] ArrayForMaxEffConc = new double[soilPhysical.Thickness.Length];
                for (int i = 0; i <= (soilPhysical.Thickness.Length - 1); i++)
                    ArrayForMaxEffConc[i] = maxEffectiveNConcentration;

                patchManager.MaximumNO3AvailableToPlants = ArrayForMaxEffConc;
                patchManager.MaximumNH4AvailableToPlants = ArrayForMaxEffConc;

                patchManager.NPartitionApproach = PartitionApproachEnum.BasedOnConcentrationAndDelta;
                patchManager.AutoAmalgamationApproach = AutoAmalgamationApproachEnum.None;
                patchManager.basePatchApproach = BaseApproachEnum.IDBased;
                patchManager.AllowPatchAmalgamationByAge = false;
                patchManager.PatchAgeForForcedMerge = 1000000.0;  // ie don't merge

                int[] PatchToAddTo = new int[1];  //need an array variable for this
                string[] PatchNmToAddTo = new string[1];
                int nPatchesAdded = 0;
                double NewArea = 1.0 / zoneCount;

                while (nPatchesAdded < zoneCount - 1)
                {
                    AddSoilCNPatchType NewPatch = new AddSoilCNPatchType();
                    NewPatch.DepositionType = DepositionTypeEnum.ToNewPatch;
                    NewPatch.AreaFraction = NewArea;
                    PatchToAddTo[0] = 0;
                    PatchNmToAddTo[0] = "0";
                    NewPatch.AffectedPatches_id = PatchToAddTo;
                    NewPatch.AffectedPatches_nm = PatchNmToAddTo;
                    NewPatch.SuppressMessages = false;
                    patchManager.Add(NewPatch);
                    nPatchesAdded += 1;
                }

            }
            else //(!PseudoPatches)  // so now this is zones - possibly multiple zones
            {
                zone.Area = 1.0 / zoneCount;  // and then this will apply to all the new zones
                for (int i = 0; i < zoneCount-1; i++)
                {
                    var newZone = Apsim.Clone(zone);
                    Structure.Add(newZone, simulation);
                }
            }
        }

        /// <summary>Invoked at start of simulation.</summary>
        public void OnStartOfSimulation()
        {
            if (!pseudoPatches)
                summary.WriteMessage(simpleGrazing, "Created " + zoneCount + " identical zones, each of area " + (1.0 / zoneCount) + " ha", MessageType.Diagnostic);

            summary.WriteMessage(simpleGrazing, "Initialising the ZoneManager for grazing, urine return and reporting", MessageType.Diagnostic);

            pseudoRandom = new Random(pseudoRandomSeed);  // sets a constant seed value

            if (pseudoPatches)
                DivisorForReporting = 1.0;
            else
                DivisorForReporting = zoneCount;

            monthlyUrineNAmt = new double[] { 24, 19, 17, 12, 8, 5, 5, 10, 16, 19, 23, 25 }; //This is to get a pattern of return that varies with month but removes the variation that might be caused by small changes in herbage growth
            //MonthlyUrineNAmt = new double[] { 25, 25, 25, 25, 25, 25, 25, 25, 25, 25, 25, 25 }; //This is to get a pattern of return that varies with month but removes the variation that might be caused by small changes in herbage growth
            //MonthlyUrineNAmt = new double[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }; //This is to get a pattern of return that varies with month but removes the variation that might be caused by small changes in herbage growth

            if (pseudoPatches)
            {
                var patchManager = simpleGrazing.FindInScope<NutrientPatchManager>();

                //var patchManager = FindInScope<NutrientPatchManager>();
                summary.WriteMessage(simpleGrazing, patchManager.NumPatches.ToString() + " pseudopatches have been created", MessageType.Diagnostic);
            }
            else
            {
                var simulation = simpleGrazing.FindAncestor<Simulation>();
                var physical = simpleGrazing.FindInScope<IPhysical>();
                double[] arrayForMaxEffConc = Enumerable.Repeat(maxEffectiveNConcentration, physical.Thickness.Length).ToArray();
                foreach (Zone zone in simulation.FindAllInScope<Zone>())
                {
                    foreach (var patchManager in zone.FindAllInScope<NutrientPatchManager>())
                    {
                        patchManager.MaximumNO3AvailableToPlants = arrayForMaxEffConc;
                        patchManager.MaximumNH4AvailableToPlants = arrayForMaxEffConc;
                    }
                }
            }

            summary.WriteMessage(simpleGrazing, "Finished initialising the Manager for grazing, urine return and reporting", MessageType.Diagnostic);

            NumZonesForUrine = 1;  // in the future this might be > 1
            ZoneNumForUrine = -1;  // this will be incremented to 0 (first zone) below

            UrinePenetration();
        }

        /// <summary>Invoked to do trampling and dung return.</summary>
        public void DoTramplingAndDungReturn(double amountDungCReturned, double amountDungNReturned)
        {
            // Note that dung is assumed to be spread uniformly over the paddock (patches or sones).
            // There is no need to bring zone area into the calculations here but zone area must be included for variables reported FROM the zone to the upper level

            int i = -1;  // patch or paddock counter
            foreach (Zone zone in simpleGrazing.FindAllInScope<Zone>())
            {
                i += 1;
                SurfaceOrganicMatter surfaceOM = zone.FindInScope<SurfaceOrganicMatter>() as SurfaceOrganicMatter;

                // do some trampling of litter
                // accelerate the movement of surface litter into the soil - do this before the dung is added
                double temp = surfaceOM.Wt * 0.1;

                surfaceOM.Incorporate(fraction: (double) 0.1, depth: (double)100.0, doOutput: true);

                summary.WriteMessage(simpleGrazing, "For patch " + i + " the amount of litter trampled was " + temp + " and the remaining litter is " + (surfaceOM.Wt), MessageType.Diagnostic);

                // move the dung to litter
                AddFaecesType dung = new()
                {
                    OMWeight = amountDungCReturned / 0.4,  //assume dung C is 40% of OM
                    OMN = amountDungNReturned
                };
                surfaceOM.Add(dung.OMWeight, dung.OMN, 0.0, "RuminantDung_PastureFed", null);
                summary.WriteMessage(simpleGrazing, "For patch " + i + " the amount of dung DM added to the litter was " + (amountDungCReturned / 0.4) + " and the amount of N added in the dung was " + (amountDungNReturned), MessageType.Diagnostic);

            }
        }

        /// <summary>Invoked to do urine return</summary>
        public void DoUrineReturn(double amountUrineNReturned)
        {
            GetZoneForUrineReturn();

            summary.WriteMessage(simpleGrazing, "The Zone for urine return is " + ZoneNumForUrine, MessageType.Diagnostic);

            if (!pseudoPatches)
            {
                Zone zone = simpleGrazing.FindAllInScope<Zone>().ToArray()[ZoneNumForUrine];
                Fertiliser thisFert = zone.FindInScope<Fertiliser>() as Fertiliser;

                thisFert.Apply(amount: amountUrineNReturned * zoneCount,
                        type: "UreaN",
                        depth: 0.0,   // when depthBottom is specified then this means depthTop
                        depthBottom: urineDepthPenetration,
                        doOutput: true);

                summary.WriteMessage(simpleGrazing, amountUrineNReturned + " urine N added to Zone " + ZoneNumForUrine + ", the local load was " + amountUrineNReturned / zone.Area + " kg N /ha", MessageType.Diagnostic);
            }
            else // PseudoPatches
            {
                int[] PatchToAddTo = new int[1];  //because need an array variable for this
                string[] PatchNmToAddTo = new string[0];  //need an array variable for this
                double[] UreaToAdd = new double[physical.Thickness.Length];

                for (int ii = 0; ii <= (physical.Thickness.Length - 1); ii++)
                    UreaToAdd[ii] = urineDepthPenetrationArray[ii]  * amountUrineNReturned * zoneCount;

                // needed??   UreaReturned += AmountFertNReturned;

                AddSoilCNPatchType CurrentPatch = new();
                CurrentPatch.Sender = "manager";
                CurrentPatch.DepositionType = DepositionTypeEnum.ToSpecificPatch;
                PatchToAddTo[0] = ZoneNumForUrine;
                CurrentPatch.AffectedPatches_id = PatchToAddTo;
                CurrentPatch.AffectedPatches_nm = PatchNmToAddTo;
                CurrentPatch.Urea = UreaToAdd;

                var patchManager = simpleGrazing.FindInScope<NutrientPatchManager>();

                summary.WriteMessage(simpleGrazing, "Patch MinN prior to urine return: " + patchManager.MineralNEachPatch[ZoneNumForUrine], MessageType.Diagnostic);
                patchManager.Add(CurrentPatch);
                summary.WriteMessage(simpleGrazing, "Patch MinN after urine return: " + patchManager.MineralNEachPatch[ZoneNumForUrine], MessageType.Diagnostic);
            }
        }

        /// <summary>Determine and return the zone for urine return.</summary>
        private void GetZoneForUrineReturn()
        {
            if (urineReturnPattern == UrineReturnPatterns.RotatingInOrder)
            {
                ZoneNumForUrine += 1;  //increment the zone number - it was initialised at -1. NOTE, ZoneNumForUrine is used for both zones and patches
                if (ZoneNumForUrine >= zoneCount)
                    ZoneNumForUrine = 0;  // but reset back to the first patch if needed
            }
            else if (urineReturnPattern == UrineReturnPatterns.Random)
            {
                Random rnd = new Random();
                ZoneNumForUrine = rnd.Next(0, zoneCount); // in C# the maximum value (ZoneCount) will not be selected
            }
            else if (urineReturnPattern == UrineReturnPatterns.PseudoRandom)
            {
                ZoneNumForUrine = pseudoRandom.Next(0, zoneCount); // in C# the maximum value (ZoneCount) will not be selected
            }
            else
                throw new Exception("UrineResturnPattern not recognised");

            summary.WriteMessage(simpleGrazing, "The next zone/patch for urine return is " + ZoneNumForUrine, MessageType.Diagnostic);
        }

        /// <summary>Calculate the urine penetration array.</summary>
        private void UrinePenetration()
        {
            // note this assumes that all the paddocks are the same
            double tempDepth = 0.0;
            urineDepthPenetrationArray = new double[physical.Thickness.Length];
            for (int i = 0; i <= (physical.Thickness.Length - 1); i++)
            {
                tempDepth += physical.Thickness[i];
                if (tempDepth <= urineDepthPenetration)
                {
                    urineDepthPenetrationArray[i] = physical.Thickness[i] / urineDepthPenetration;
                }
                else
                {
                    urineDepthPenetrationArray[i] = (urineDepthPenetration - (tempDepth - physical.Thickness[i])) / (tempDepth - (tempDepth - physical.Thickness[i])) * physical.Thickness[i] / urineDepthPenetration;
                    urineDepthPenetrationArray[i] = Math.Max(0.0, Math.Min(1.0, urineDepthPenetrationArray[i]));
                }
                summary.WriteMessage(simpleGrazing, "The proportion of urine applied to the " + i + "th layer will be " + urineDepthPenetrationArray[i], MessageType.Diagnostic);
            }
        }
    }
}