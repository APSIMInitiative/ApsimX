using System;
using System.Linq;
using APSIM.Core;
using APSIM.Numerics;
using APSIM.Shared.Utilities;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.LinearAlgebra;
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
        [NonSerialized]
        private IStructure structure;
        private readonly SimpleGrazing simpleGrazing;
        private readonly bool pseudoPatches;
        private Random pseudoRandom;
        private int pseudoRandomSeed;
        private readonly ISummary summary;
        private readonly Physical physical;

        // User properties.

        /// <summary>Number of patches or zones to create.</summary>
        private readonly int zoneCount;

        /// <summary>Urine return type</summary>
        private readonly UrineReturnTypes urineReturnType;

        /// <summary>Urine return pattern.</summary>
        private readonly UrineReturnPatterns urineReturnPattern;

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
        /// <param name="structure">Scope instance</param>
        /// <param name="pseudoPatches">Use pseudo patches?</param>
        /// <param name="zoneCount"></param>
        /// <param name="urineReturnType"></param>
        /// <param name="urineReturnPattern"></param>
        /// <param name="pseudoRandomSeed"></param>
        /// <param name="maxEffectiveNConcentration"></param>
        public UrineDungPatches(SimpleGrazing simpleGrazing,
                                IStructure structure,
                                bool pseudoPatches,
                                int zoneCount,
                                UrineReturnTypes urineReturnType,
                                UrineReturnPatterns urineReturnPattern,
                                int pseudoRandomSeed,
                                double maxEffectiveNConcentration)
        {
            this.simpleGrazing = simpleGrazing;
            this.structure = structure;
            this.pseudoPatches = pseudoPatches;
            this.zoneCount = zoneCount;
            this.urineReturnType = urineReturnType;
            this.urineReturnPattern = urineReturnPattern;
            this.pseudoRandomSeed = pseudoRandomSeed;
            this.maxEffectiveNConcentration = maxEffectiveNConcentration;
            summary = structure.Find<ISummary>(relativeTo: simpleGrazing);
            physical = structure.Find<Physical>(relativeTo: simpleGrazing);
        }

        /// <summary>
        /// Invoked by the infrastructure before the simulation gets created in memory.
        /// Use this to create patches.
        /// </summary>
        public void OnPreLink()
        {
            var simulation = simpleGrazing.Node.FindParent<Simulation>(recurse: true) as Simulation;
            var zone = simulation.Node.FindChild<Zone>();

            if (zoneCount == 0)
                throw new Exception("Number of patches/zones in urine patches is zero.");

            if (pseudoPatches)
            {
                zone.Area = 1.0;

                var patchManager = structure.Find<NutrientPatchManager>(relativeTo: simpleGrazing);
                if (patchManager == null)
                    throw new Exception("Cannot find NutrientPatchManager");
                var soilPhysical = structure.Find<Physical>(relativeTo: simpleGrazing);
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

                int nPatchesAdded = 0;
                double NewArea = 1.0 / zoneCount;

                while (nPatchesAdded < zoneCount - 1)
                {
                    AddSoilCNPatchType newPatch = new AddSoilCNPatchType
                    {
                        DepositionType = DepositionTypeEnum.ToNewPatch,
                        AreaFraction = NewArea,
                        AffectedPatches_id = [0],
                        AffectedPatches_nm = ["0"],
                        SuppressMessages = false
                    };
                    patchManager.Add(newPatch);
                    nPatchesAdded += 1;
                }
            }
            else
            {
                // explicit patches
                zone.Area = 1.0 / zoneCount;  // and then this will apply to all the new zones
                for (int i = 0; i < zoneCount - 1; i++)
                {
                    var newZone = Apsim.Clone(zone);
                    Structure.Add(newZone, simulation);
                }
            }
        }

        /// <summary>Invoked at start of simulation.</summary>
        public void OnStartOfSimulation(IStructure structure)
        {
            if (!pseudoPatches)
                summary.WriteMessage(simpleGrazing, "Created " + zoneCount + " identical zones, each of area " + (1.0 / zoneCount) + " ha", MessageType.Diagnostic);

            summary.WriteMessage(simpleGrazing, "Initialising the ZoneManager for grazing, urine return and reporting", MessageType.Diagnostic);

            pseudoRandom = new Random(pseudoRandomSeed);  // sets a constant seed value

            if (pseudoPatches)
                DivisorForReporting = 1.0;
            else
                DivisorForReporting = zoneCount;

            if (pseudoPatches)
            {
                var patchManager = structure.Find<NutrientPatchManager>(relativeTo: simpleGrazing);

                //var patchManager = FindInScope<NutrientPatchManager>();
                summary.WriteMessage(simpleGrazing, patchManager.NumPatches.ToString() + " pseudopatches have been created", MessageType.Diagnostic);
            }
            else
            {
                var simulation = structure.FindParent<Simulation>(relativeTo: simpleGrazing, recurse: true);
                var physical = structure.Find<IPhysical>(relativeTo: simpleGrazing);
                double[] arrayForMaxEffConc = Enumerable.Repeat(maxEffectiveNConcentration, physical.Thickness.Length).ToArray();
                foreach (Zone zone in structure.FindAll<Zone>(relativeTo: simulation))
                {
                    foreach (var patchManager in structure.FindAll<NutrientPatchManager>(relativeTo: zone))
                    {
                        patchManager.MaximumNO3AvailableToPlants = arrayForMaxEffConc;
                        patchManager.MaximumNH4AvailableToPlants = arrayForMaxEffConc;
                    }
                }
            }

            summary.WriteMessage(simpleGrazing, "Finished initialising the Manager for grazing, urine return and reporting", MessageType.Diagnostic);

            NumZonesForUrine = 1;  // in the future this might be > 1
            ZoneNumForUrine = -1;  // this will be incremented to 0 (first zone) below
        }

        /// <summary>Invoked to do urine return</summary>
        public void DoUrineReturn(int numUrinations, double meanLoad)
        {
            // meanLoad is coming in as kg N excreted by the herd
            // convert the herd value to a per urination value in g
            meanLoad = meanLoad  / numUrinations; // to g/urination

            summary.WriteMessage(simpleGrazing, "The Zone for urine return is " + ZoneNumForUrine, MessageType.Diagnostic);

            //(double[] urineLoad, double[] urineVolume) = CalculateLoadVolume(numUrinations, meanLoad);



            double[] ureaToAdd = new double[physical.Thickness.Length];
            double gridArea = 10000 / zoneCount;   // m2/patch
            double gridAreaUsed = 0;
            double totalUrineAdded = 0;  // kg
            for (int i = 0; i < numUrinations; i++)
            {
                //urineLoad[i] = urineLoad[i] * Constants.g2kg;

                // THIS IS TEMPORARY - DELETE !!!!!!!!
                //urineLoad[i] = meanLoad;

                // use the Beatson data for wetted area, convert to radius, add 0.1 m edge and then convert back to an area. Note this is a natural log
                // check 2L should give a area of 0.3866 m2
                //double urinationArea = Math.PI * Math.Pow(Math.Sqrt((0.135 * Math.Log(urineVolume[i]) + 0.104) / Math.PI) + 0.1, 2.0);
                double urinationArea = 0.5;
                gridAreaUsed += urinationArea; // m2
                //double urinationDepth = urineVolume[i] / urinationArea / 0.05;    // 0.05 is the assumed increase in water content from the urineation
                double urinationDepth = 250;

                // convert urineLoad for this urination into a depth profile.
                double[] depthPenetration = UrinePenetration(urinationDepth);
                for (int ii = 0; ii <= (physical.Thickness.Length - 1); ii++)
                    ureaToAdd[ii] += depthPenetration[ii] * meanLoad;   // kg

                if (gridAreaUsed - 0.5 * urinationArea >= gridArea || i == numUrinations-1)
                {
                    GetZoneForUrineReturn();
                    AddUrineToGrid(ureaToAdd, urinationDepth);
                    totalUrineAdded += ureaToAdd.Sum();
                    Array.Clear(ureaToAdd);   // zero out the depth array
                    gridAreaUsed = 0.0;
                }
            }
            if (!MathUtilities.FloatsAreEqual(meanLoad * numUrinations, totalUrineAdded))
                throw new Exception($"The amount of urine added ({totalUrineAdded}) does not equal the amount that should have been added ({meanLoad * numUrinations})");
        }

        /// <summary>
        /// Add urine to grid (patch), pseudo or explicit.
        /// </summary>
        /// <param name="ureaToAddByLayer">Amount of urine to add - layered (kg/ha)</param>
        /// <param name="urineDepthPenetration">Urine depth penetration (mm)</param>
        private void AddUrineToGrid(double[] ureaToAddByLayer, double urineDepthPenetration)
        {
            double ureaToAdd = ureaToAddByLayer.Sum();
            if (pseudoPatches)
            {
                ureaToAddByLayer = MathUtilities.Multiply_Value(ureaToAddByLayer, zoneCount);
                var patchManager = structure.Find<NutrientPatchManager>(relativeTo: simpleGrazing);
                AddSoilCNPatchType patch = new()
                {
                    Sender = "manager",
                    DepositionType = DepositionTypeEnum.ToSpecificPatch,
                    AffectedPatches_id = [ZoneNumForUrine],
                    AffectedPatches_nm = [],
                    Urea = ureaToAddByLayer
                };
                patchManager.Add(patch);
            }
            else
            {
                // Explicit patches
                Zone zone = structure.FindAll<Zone>(relativeTo: simpleGrazing).ToArray()[ZoneNumForUrine];
                Fertiliser thisFert = structure.Find<Fertiliser>(relativeTo: zone) as Fertiliser;

                thisFert.Apply(amount: ureaToAdd * zoneCount,
                        type: "UreaN",
                        depth: 0.0,   // when depthBottom is specified then this means depthTop
                        depthBottom: urineDepthPenetration,
                        doOutput: true);
            }
            summary.WriteMessage(simpleGrazing, ureaToAdd + " urine N added to Zone " + ZoneNumForUrine + ", the local load was " + ureaToAdd + " kg N /ha", MessageType.Diagnostic);
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
        private double[] UrinePenetration(double urineDepthPenetration)
        {
            // note this assumes that all the paddocks are the same
            double tempDepth = 0.0;
            var urineDepthPenetrationArray = new double[physical.Thickness.Length];
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
            }
            return urineDepthPenetrationArray;
        }

        [NonSerialized]
        private Random RandomNumGenerator = new Random(10);

        //////////////////// PARAMETERS ///////////////////////

        /// <summary>Means of the original distributions in log space, mu_i load (gN) first and then mu_j volume (L)</summary>
        public double[] VectorOfMeans { get; set; } = [1.1567018157972, 0.400851532821705];

        /// <summary>Covariance matrix - in order of E_ii, E_ij, E_ij, E_jj</summary>
        public double[] CovarianceMatrix { get; set; } = [0.047123025480434, 0.033361752139528, 0.033361752139528, 0.033598852595467];

        /// <summary>Constrain the sampling?</summary>
        public bool DoConstraints { get; set; } = true;

        /// <summary>Choose constraint probability</summary>
        public double OneMinusAlpha { get; set; } = 0.9;

        //////////////////// OUTPUTS ///////////////////////

        /// <summary>Normal mean load to generate</summary>
        public double NormalMeanLoadToGenerate { get; set; }

        /// <summary>
        /// Calculate load and volume.
        /// </summary>
        /// <param name="numUrinations">Number of animal urinations.</param>
        /// <param name="meanLoad">Mean urination load (g N).</param>
        /// <returns></returns>
        private (double[] load, double[] volume) CalculateLoadVolume(int numUrinations, double meanLoad)
        {
            // I (VOS) don't understand what the subtraction is here but the second version gives an actual mean much closer to the intended mean
            NormalMeanLoadToGenerate = Math.Log10(meanLoad) - 0.5 * CovarianceMatrix[0];
            //NormalMeanLoadToGenerate = Math.Log10(MeanLoadToGenerate) - 1.0 * CovarianceMatrix[0];

            double[,] TransformedMu = { { NormalMeanLoadToGenerate },
                                        { VectorOfMeans[1] }};

            double[,] SigmaRows = { { CovarianceMatrix[0], CovarianceMatrix[1] },
                                    { CovarianceMatrix[2], CovarianceMatrix[3] } };

            double[,] SigmaColumns = { { 1 } };

            // Converts parameters to MathNet matrices.
            Matrix<double> TransformedMuMatrix = Matrix<double>.Build.DenseOfArray(TransformedMu);
            Matrix<double> SigmaRowsMatrix = Matrix<double>.Build.DenseOfArray(SigmaRows);
            Matrix<double> SigmaColumnsMatrix = Matrix<double>.Build.DenseOfArray(SigmaColumns);

            // Initialises transformed distribution.
            var TransformedMVN = new MatrixNormal(TransformedMuMatrix,
                                                  SigmaRowsMatrix,
                                                  SigmaColumnsMatrix,
                                                  RandomNumGenerator);

            // Generates samples and transforms them back to lognormal space.
            double[] LogNormalLoadSamples = new double[numUrinations];
            double[] LogNormalVolumeSamples = new double[numUrinations];

            // This is a value from the chi squared distribution, used to check whether the generated sample is within the confidence interval.
            double ChiSqMax = ChiSquared.InvCDF(2, OneMinusAlpha);

            for (int i = 0; i < numUrinations; i++)
            {

                bool reject = true;
                while (reject)
                {
                    // Calculates the sample's chi squared score.
                    Matrix<double> LoadVolumeSample = TransformedMVN.Sample();
                    Matrix<double> Deviation = LoadVolumeSample.Subtract(TransformedMuMatrix);
                    Matrix<double> DeviationT = Deviation.Transpose();
                    Matrix<double> SigmaRowsMatrixInv = SigmaRowsMatrix.Inverse();
                    Matrix<double> ChiSqScore = (DeviationT.Multiply(SigmaRowsMatrixInv)).Multiply(Deviation);

                    // If constrained and sample is outside confidence interval, discards sample and tries again.
                    if ((ChiSqScore[0,0] > ChiSqMax) && DoConstraints)
                    {
                        reject = true;
                    }
                    else
                    {
                        reject = false;
                        LogNormalLoadSamples[i] = Math.Pow(10.0, LoadVolumeSample[0,0]);
                        LogNormalVolumeSamples[i] = Math.Pow(10.0, LoadVolumeSample[1,0]);
                    }
                }
            }

            // Return load and volume.
            return (load: LogNormalLoadSamples,       // could aim to correct this against the intended??? Not the best solution
                    volume: LogNormalVolumeSamples);
        }
    }
}