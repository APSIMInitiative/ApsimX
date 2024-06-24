using System;

namespace Models.GrazPlan
{
    /// <summary>
    /// This is nearly the same information as "GrazType.ReproType" (below), but is intended for
    /// use in the GUI with Buy events, where we don't really want to include the
    /// pregnancy state of females. I'm placing it outside the GrazType namespace
    /// for simplicity when entering values in the GUI.
    /// </summary>
    public enum ReproductiveType
    {
        /// <summary>
        /// female
        /// </summary>
        Female,
        /// <summary>
        /// male
        /// </summary>
        Male,
        /// <summary>
        /// castrated male
        /// </summary>
        Castrate
    }

    /// <summary>
    /// Container for many GrazPlan constants
    /// </summary>
    public static class GrazType
    {
        /// <summary>
        /// None item
        /// </summary>
        public const int NONE = 0;

        /// <summary>
        /// Total item
        /// </summary>
        public const int TOTAL = 0;

        /// <summary>
        /// Surface item
        /// </summary>
        public const int SURFACE = 0;

        /// <summary>
        /// Represents a large value
        /// </summary>
        public const double VERYLARGE = 1.0E6;
        /// <summary>
        /// Represents a small value
        /// </summary>
        public const double VERYSMALL = 1.0E-4;

        /// <summary>
        /// Number of digestibility classes
        /// </summary>
        public const int DigClassNo = 6;

        /// <summary>
        /// Total number of herbage classes
        /// </summary>
        public const int HerbClassNo = DigClassNo * 2;

        /// <summary>
        /// Maximum plant species
        /// </summary>
        public const int MaxPlantSpp = 80;

        /// <summary>
        /// The ungrazeable amount of green in a paddock
        /// </summary>
        public const double Ungrazeable = 0.0;  // g/m^2 - setting this to zero because the forages have already removed the ungrazable portion.

        /// <summary>
        /// Maximum soil layers
        /// </summary>
        public const int MaxSoilLayers = 50;
#pragma warning disable 1591 //missing xml comment

        /// <summary>
        /// Plant part leaf
        /// </summary>
        public const int ptLEAF = 1;

        /// <summary>
        /// Plant part stem
        /// </summary>
        public const int ptSTEM = 2;

        /// <summary>
        /// Plant part root
        /// </summary>
        public const int ptROOT = 3;

        /// <summary>
        /// Plant part seed
        /// </summary>
        public const int ptSEED = 4;

        /// <summary>
        /// Seed maturity
        /// </summary>
        public const int SOFT = 1;
        public const int HARD = 2;

        /// <summary>
        /// Seed ripeness
        /// </summary>
        public const int UNRIPE = 1;
        public const int RIPE = 2;

        public const int EFFR = 1;
        public const int OLDR = 2;

        public const int stSEEDL = 1;
        public const int stESTAB = 2;
        public const int stSENC = 3;
        public const int stDEAD = 4;
        public const int stLITT1 = 5;
        public const int stLITT2 = 6;

        public const int sgGREEN = 123;
        public const int sgDRY = 456;
        public const int sgAV_DRY = 45;
        public const int sgEST_SENC = 23;
        public const int sgSTANDING = 1234;
        public const int sgLITTER = 56;

        /// <summary>
        /// Organic material elements
        /// </summary>
        public enum TOMElement
        {
            /// <summary>
            /// Carbon element
            /// </summary>
            c,

            /// <summary>
            /// Nitrogen value
            /// </summary>
            n,

            /// <summary>
            /// Phosphorous element
            /// </summary>
            p,

            /// <summary>
            /// Sulphur element
            /// </summary>
            s
        }
        public enum TPlantElement {N=1, P, S };

        /// <summary>
        /// Plant nutrients
        /// </summary>
        public enum TPlantNutrient
        {
            /// <summary>
            ///
            /// </summary>
            pnNO3,

            /// <summary>
            ///
            /// </summary>
            pnNH4,

            /// <summary>
            ///
            /// </summary>
            pnPOx,

            /// <summary>
            ///
            /// </summary>
            pnSO4
        }

        public enum TSoilNutrient { snNO3, snNH4, snUrea, snPOx, snSO4, snElS, snCations };

		// SoilArray         = packed array[SURFACE..MaxSoilLayers] of Float;
        // LayerArray        = packed array[1..MaxSoilLayers]       of Float;
        // DigClassArray     = packed array[1..DigClassNo]          of Float; // double[GrazType.DigClassNo+1]
        // PastureArray      = packed array[1..MaxPlantSpp]         of Float;
        // HerbageArray      = packed array[1..HerbClassNo]         of Float;

        /// <summary>
        /// Dry matter pool
        /// </summary>
        [Serializable]
        public class DM_Pool
        {
            /// <summary>
            /// Dry matter in kg/ha
            /// </summary>
            public double DM;

            /// <summary>
            /// Nutrients in kg element/ha [0, N...
            /// </summary>
            public double[] Nu = new double[4];

            /// <summary>
            /// Ash alkalinity in mol/ha
            /// </summary>
            public double AshAlk;
        }

        /// <summary>
        /// Initialise a layer array with default values
        /// </summary>
        /// <param name="values"></param>
        /// <param name="defValue"></param>
        public static void InitLayerArray(ref double[] values, double defValue)
        {
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = defValue;
            }
        }

        /// <summary>
        /// Zero the DM pool used in Animal
        /// </summary>
        /// <param name="pool">Pool to zero</param>
        public static void ZeroDMPool(ref DM_Pool pool)
        {
            int pe;

            pool.DM = 0;
            for (pe = (int)TOMElement.n; pe <= (int)TOMElement.s; pe++)
                pool.Nu[pe] = 0;
            pool.AshAlk = 0;
        }

        // RootDMArray       = packed array[1..MaxSoilLayers] of DM_Pool;

        /// <summary>
        /// Sheep or Cattle animal type
        /// </summary>
        public enum AnimalType
        {
            /// <summary>
            /// Is sheep
            /// </summary>
            Sheep,

            /// <summary>
            /// Is Cattle
            /// </summary>
            Cattle
        }

        /// <summary>
        /// Age type of the animal
        /// </summary>
        public enum AgeType
        {
            /// <summary>
            /// Lamb or calf
            /// </summary>
            LambCalf,

            /// <summary>
            /// A weaner
            /// </summary>
            Weaner,

            /// <summary>
            /// A yearling animal
            /// </summary>
            Yearling,

            /// <summary>
            /// A two year old
            /// </summary>
            TwoYrOld,

            /// <summary>
            /// A mature animal
            /// </summary>
            Mature
        }

        /// <summary>
        /// Text for the age types
        /// </summary>
        public static string[] AgeText = { "Young", "Weaner", "Yearling", "2-3yo", "Mature" };

        /// <summary>
        /// Reproduction type
        /// </summary>
        public enum ReproType
        {
            /// <summary>
            /// Castrated animal
            /// </summary>
            Castrated,

            /// <summary>
            /// Is a male
            /// </summary>
            Male,

            /// <summary>
            /// Is empty
            /// </summary>
            Empty,

            /// <summary>
            /// Early pregnancy
            /// </summary>
            EarlyPreg,

            /// <summary>
            /// Late pregnancy
            /// </summary>
            LatePreg
        }

        /// <summary>
        /// Lactation type
        /// </summary>
        public enum LactType
        {
            /// <summary>
            /// Is dry, not lactating
            /// </summary>
            Dry,

            /// <summary>
            /// Is lactating
            /// </summary>
            Lactating,

            /// <summary>
            /// Has suckling
            /// </summary>
            Suckling
        }

        /// <summary>
        /// Sheep or cattle text
        /// </summary>
        public static string[] AnimalText = { "Sheep", "Cattle" };

        // Records for transferring information between pasture and animal model, & vice versa
        public const int hbGREEN = 0;
        public const int hbDRY = 1;
#pragma warning restore 1591
        /// <summary>
        /// One element of the available feed
        /// </summary>
        [Serializable]
        public struct IntakeRecord
        {
            /// <summary>
            /// Biomass
            /// </summary>
            public double Biomass;

            /// <summary>
            /// Digestibility value
            /// </summary>
            public double Digestibility;

            /// <summary>
            /// Crude protein
            /// </summary>
            public double CrudeProtein;

            /// <summary>
            /// Degradability value
            /// </summary>
            public double Degradability;

            /// <summary>
            /// Phosphorous content
            /// </summary>
            public double PhosContent;

            /// <summary>
            /// Sulphur content
            /// </summary>
            public double SulfContent;

            /// <summary>
            /// Average pasture height:default height
            /// </summary>
            public double HeightRatio;

            /// <summary>
            /// Units are moles/kg DM
            /// </summary>
            public double AshAlkalinity;
        }

        /// <summary>
        /// Grazing inputs
        /// </summary>
        [Serializable]
        public class GrazingInputs
        {
            /// <summary>
            /// Available herbage
            /// </summary>
            public IntakeRecord[] Herbage = new IntakeRecord[DigClassNo + 1];       // [1..DigClassNo];

            /// <summary>
            /// Total live + senescing pasture (kg/ha)
            /// </summary>
            public double TotalGreen;

            /// <summary>
            /// Total dead pasture + litter (kg/ha)
            /// </summary>
            public double TotalDead;

            /// <summary>
            /// Proportion of legume
            /// </summary>
            public double LegumePropn;

            /// <summary>
            /// Seeds of various type
            /// </summary>
            public IntakeRecord[,] Seeds = new IntakeRecord[MaxPlantSpp + 1, 3];    // [1..MaxPlantSpp,UNRIPE..RIPE]

            /// <summary>
            ///
            /// </summary>
            public int[,] SeedClass = new int[MaxPlantSpp + 1, RIPE + 1];

            /// <summary>
            /// The selection factor
            /// </summary>
            public double SelectFactor;

            /// <summary>
            /// "Tropicality" of legumes 0 => temperate; 1 => tropical
            /// </summary>
            public double LegumeTrop;

            /// <summary>
            /// Construct a GrazingInputs object
            /// </summary>
            public GrazingInputs()
            {
            }

            /// <summary>
            /// Copy the whole object
            /// </summary>
            /// <param name="src">The source grazing inputs</param>
            public void CopyFrom(GrazingInputs src)
            {
                Array.Copy(src.Herbage, this.Herbage, src.Herbage.Length);
                this.TotalGreen = src.TotalGreen;
                this.TotalDead = src.TotalDead;
                this.LegumePropn = src.LegumePropn;
                Array.Copy(src.Seeds, this.Seeds, src.Seeds.Length);
                Array.Copy(src.SeedClass, this.SeedClass, src.SeedClass.Length);
                this.SelectFactor = src.SelectFactor;
                this.LegumeTrop = src.LegumeTrop;
            }

            /// <summary>
            /// Copy constructor
            /// </summary>
            /// <param name="src">The grazing inputs source object</param>
            public GrazingInputs(GrazingInputs src)
            {
                Array.Copy(src.Herbage, this.Herbage, src.Herbage.Length);
                this.TotalGreen = src.TotalGreen;
                this.TotalDead = src.TotalDead;
                this.LegumePropn = src.LegumePropn;
                Array.Copy(src.Seeds, this.Seeds, src.Seeds.Length);
                Array.Copy(src.SeedClass, this.SeedClass, src.SeedClass.Length);
                this.SelectFactor = src.SelectFactor;
                this.LegumeTrop = src.LegumeTrop;
            }
        }

        /// <summary>
        /// Zero the grazing inputs
        /// </summary>
        /// <param name="inputs">The grazing inputs to clear</param>
        public static void zeroGrazingInputs(ref GrazingInputs inputs)
        {
            inputs.Herbage = new IntakeRecord[DigClassNo + 1];
            inputs.TotalGreen = 0;
            inputs.TotalDead = 0;
            inputs.LegumePropn = 0;
            inputs.Seeds = new IntakeRecord[MaxPlantSpp + 1, RIPE + 1];
            inputs.SeedClass = new int[MaxPlantSpp + 1, RIPE + 1];
            inputs.SelectFactor = 0;
            inputs.LegumeTrop = 0;
        }

        /// <summary>
        /// Add grazing inputs to total inputs
        /// </summary>
        /// <param name="iPopn">The seed population</param>
        /// <param name="partInputs">Partial inputs</param>
        /// <param name="totalInputs">Total inputs</param>
        public static void addGrazingInputs(int iPopn, GrazingInputs partInputs, ref GrazingInputs totalInputs)
        {
            int iClass;
            // IntakeRecord intakeRec;
            {
                for (iClass = 1; iClass <= DigClassNo; iClass++)
                {
                    IntakeRecord intake = totalInputs.Herbage[iClass];

                    intake.HeightRatio = WeightAverage(
                                                     intake.HeightRatio,
                                                     intake.Biomass,
                                                     partInputs.Herbage[iClass].HeightRatio,
                                                     partInputs.Herbage[iClass].Biomass);
                    intake.Degradability = WeightAverage(
                                                     intake.Degradability,
                                                     intake.Biomass * intake.CrudeProtein,
                                                     partInputs.Herbage[iClass].Degradability,
                                                     partInputs.Herbage[iClass].Biomass * partInputs.Herbage[iClass].CrudeProtein);
                    intake.Digestibility = WeightAverage(
                                                     intake.Digestibility,
                                                     intake.Biomass,
                                                     partInputs.Herbage[iClass].Digestibility,
                                                     partInputs.Herbage[iClass].Biomass);
                    intake.CrudeProtein = WeightAverage(
                                                     intake.CrudeProtein,
                                                     intake.Biomass,
                                                     partInputs.Herbage[iClass].CrudeProtein,
                                                     partInputs.Herbage[iClass].Biomass);
                    intake.PhosContent = WeightAverage(
                                                     intake.PhosContent,
                                                     intake.Biomass,
                                                     partInputs.Herbage[iClass].PhosContent,
                                                     partInputs.Herbage[iClass].Biomass);
                    intake.SulfContent = WeightAverage(
                                                     intake.SulfContent,
                                                     intake.Biomass,
                                                     partInputs.Herbage[iClass].SulfContent,
                                                     partInputs.Herbage[iClass].Biomass);
                    intake.AshAlkalinity = WeightAverage(
                                                     intake.AshAlkalinity,
                                                     intake.Biomass,
                                                     partInputs.Herbage[iClass].AshAlkalinity,
                                                     partInputs.Herbage[iClass].Biomass);
                    intake.Biomass = intake.Biomass + partInputs.Herbage[iClass].Biomass;
                    totalInputs.Herbage[iClass] = intake;
                }
                totalInputs.LegumePropn = WeightAverage(
                                                        totalInputs.LegumePropn,
                                                        totalInputs.TotalGreen + totalInputs.TotalDead,
                                                        partInputs.LegumePropn,
                                                        partInputs.TotalGreen + partInputs.TotalDead);
                totalInputs.SelectFactor = WeightAverage(
                                                            totalInputs.SelectFactor,
                                                            totalInputs.TotalGreen + totalInputs.TotalDead,
                                                            partInputs.SelectFactor,
                                                            partInputs.TotalGreen + partInputs.TotalDead);
                totalInputs.TotalGreen = totalInputs.TotalGreen + partInputs.TotalGreen;
                totalInputs.TotalDead = totalInputs.TotalDead + partInputs.TotalDead;
                for (int i = 0; i <= 2; i++)
                {
                    totalInputs.Seeds[iPopn, i] = partInputs.Seeds[1, i];
                    totalInputs.SeedClass[iPopn, i] = partInputs.SeedClass[1, i];
                }
                totalInputs.LegumeTrop = WeightAverage(
                                                        totalInputs.LegumeTrop,
                                                        totalInputs.TotalGreen + totalInputs.TotalDead,
                                                        partInputs.LegumeTrop,
                                                        partInputs.TotalGreen + partInputs.TotalDead);
            }
        }
        /// <summary>
        ///
        /// </summary>
        public class TPopnHerbageAttr
        {
            /// <summary>
            /// kg/ha
            /// </summary>
            public double fMass_DM;
            /// <summary>
            /// kg/kg
            /// </summary>
            public double fDM_Digestibility;
            /// <summary>
            /// kg/kg
            /// </summary>
            public double[] fNutrientConc = new double[3]; // TODO: Check this!! [N..S]
            /// <summary>
            /// kg/kg
            /// </summary>
            public double fNDegradability;
            /// <summary>
            /// mol/kg
            /// </summary>
            public double fAshAlkalinity;
            /// <summary>
            /// kg/m^3
            /// </summary>
            public double fBulkDensity;
            /// <summary>
            /// 0-1, bite-size scale
            /// </summary>
            public double fGroundAreaFract;
        }
        /// <summary>
        ///
        /// </summary>
        public class TPopnHerbageData
        {
            /// <summary>
            /// Is a legume
            /// </summary>
            public bool bIsLegume;
            /// <summary>
            ///
            /// </summary>
            public double fSelectFactor;
            /// <summary>
            ///
            /// </summary>
            public TPopnHerbageAttr[,] Herbage = new TPopnHerbageAttr[2, HerbClassNo + 1];
            /// <summary>
            ///
            /// </summary>
            public TPopnHerbageAttr[] Seeds = new TPopnHerbageAttr[RIPE + 1];
            /// <summary>
            ///
            /// </summary>
            public int[] iSeedClass = new int[RIPE + 1];
        }
        /* TSppSeedArray => double[MaxPlantSpp + 1, 3]  //1..50(1..MaxPlantSpp), 1..2(UNRIPE..RIPE)     */

        /// <summary>
        ///
        /// </summary>
        [Serializable]
        public class GrazingOutputs // Quantities grazed from a pasture
        {
            /// <summary>
            /// Herbage classes
            /// </summary>
            public double[] Herbage = new double[DigClassNo + 1];   // kg/ha [1..

            /// <summary>
            /// The seed pools
            /// </summary>
            public double[,] Seed = new double[MaxPlantSpp + 1, 3]; // kg/ha  TODO: Fix this [1.., ripe..unripe]

            /// <summary>
            /// Copy from grazing outputs
            /// </summary>
            public void CopyFrom(GrazingOutputs src)
            {
                Array.Copy(src.Herbage, this.Herbage, src.Herbage.Length);
                Array.Copy(src.Seed, this.Seed, src.Seed.Length);
            }
        }

        /// <summary>Nutrient areas</summary>
        public const int MAXNUTRAREAS = 5;

        /// <summary>
        /// Soil nutrient distribution in each layer for each area.
        /// </summary>
        [Serializable]
        public class TSoilNutrientDistn
        {
            /// <summary>Number of nutrient areas</summary>
            public int NoAreas;

            /// <summary></summary>
            public double[] RelAreas = new double[MAXNUTRAREAS - 1];

            /// <summary>
            /// Solution soil nutrient conc. (mg/l)
            /// </summary>
            public double[][] SolnPPM = { new double[GrazType.MaxSoilLayers+1],
                                          new double[GrazType.MaxSoilLayers+1],
                                          new double[GrazType.MaxSoilLayers+1],
                                          new double[GrazType.MaxSoilLayers+1],
                                          new double[GrazType.MaxSoilLayers+1]
            };

            /// <summary>
            /// These are in kg/(total ha), not kg/(patch ha)
            /// </summary>
            public double[][] AvailKgHa = { new double[GrazType.MaxSoilLayers+1],
                                          new double[GrazType.MaxSoilLayers+1],
                                          new double[GrazType.MaxSoilLayers+1],
                                          new double[GrazType.MaxSoilLayers+1],
                                          new double[GrazType.MaxSoilLayers+1]
            };
        }

        // Various constants with biological meanings
        /// <summary>
        /// Default class digestibilites
        /// </summary>
        static public readonly double[] ClassDig = { 0.0, 0.8, 0.7, 0.6, 0.5, 0.4, 0.3, 0.3 };
        /// <summary>
        /// Carbon content of dry matter
        /// </summary>
        public const double DM2Carbon = 0.4;

        /// <summary>
        /// Conversion from N content to protein
        /// </summary>
        public const double N2Protein = 6.25;

        /// <summary>
        /// Default conversion:  kg/ha -> cm height
        /// </summary>
        public const double DM2Height = 0.003;

        /// <summary>
        /// Herbage bulk density for HR=1 (kg/m^3)
        /// </summary>
        public const double REF_HERBAGE_BD = 0.01 / DM2Height;

        /// <summary>
        /// Energy content of herbage (MJ/kg DM)
        /// </summary>
        public const double HerbageE2DM = 17.0;

        /// <summary>
        /// Conversion factor
        /// </summary>
        public const double FatE2DM = 36.0;

        /// <summary>
        /// Conversion factor
        /// </summary>
        public const double ProteinE2DM = 14.0;

        // Various constants with biological meanings

        /// <summary>
        /// Get a weighted average
        /// </summary>
        /// <param name="x1">The x1 value</param>
        /// <param name="y1">The y1 value</param>
        /// <param name="x2">The x2 value</param>
        /// <param name="y2">The y2 value</param>
        /// <returns>The weighted average</returns>
        public static double WeightAverage(double x1, double y1, double x2, double y2)
        {
            double result;
            if ((y1 != 0.0) && (y2 != 0.0))
                result = (x1 * y1 + x2 * y2) / (y1 + y2);
            else if (y1 != 0.0)
                result = x1;
            else
                result = x2;

            return result;
        }

        /// <summary>
        /// Scale the grazing inputs
        /// </summary>
        /// <param name="inputs">The grazing inputs</param>
        /// <param name="scale">The scale value</param>
        /// <returns>The scaled grazing input</returns>
        public static GrazingInputs ScaleGrazingInputs(GrazingInputs inputs, double scale)
        {
            int iClass;
            int iSpecies;
            int iRipe;

            GrazingInputs Result = new GrazingInputs(inputs);

            if (scale != 1.0)
            {
                for (iClass = 1; iClass <= DigClassNo; iClass++)
                    Result.Herbage[iClass].Biomass = scale * inputs.Herbage[iClass].Biomass;

                Result.TotalGreen = scale * inputs.TotalGreen;
                Result.TotalDead = scale * inputs.TotalDead;

                for (iSpecies = 1; iSpecies <= MaxPlantSpp; iSpecies++)
                    for (iRipe = UNRIPE; iRipe <= RIPE; iRipe++)
                        Result.Seeds[iSpecies, iRipe].Biomass = scale * inputs.Seeds[iSpecies, iRipe].Biomass;
            }

            return Result;
        }

        /// <summary>
        /// Rescale the DM pool
        /// </summary>
        /// <param name="aPool">The pool to scale</param>
        /// <param name="proportion">The proportion amount</param>
        /// <returns></returns>
        public static DM_Pool PoolFraction(DM_Pool aPool, double proportion)
        {
            return MultiplyDMPool(aPool, proportion);
        }

        /// <summary>
        /// Multiply the DM pool
        /// </summary>
        /// <param name="pool">Dry matter pool</param>
        /// <param name="scale">Scale value</param>
        /// <returns>The scaled dry matter pool</returns>
        public static DM_Pool MultiplyDMPool(DM_Pool pool, double scale)
        {
            DM_Pool result = new DM_Pool();

            result.DM = scale * pool.DM;
            for (int elem = (int)TOMElement.n; elem <= (int)TOMElement.s; elem++)
                result.Nu[elem] = scale * pool.Nu[elem];
            result.AshAlk = scale * pool.AshAlk;

            return result;
        }

        /// <summary>
        /// Add dry matter pool to the total pool
        /// </summary>
        /// <param name="partPool">Part pool to add</param>
        /// <param name="totPool">Total pool</param>
        public static void AddDMPool(DM_Pool partPool, DM_Pool totPool)
        {
            int n = (int)GrazType.TOMElement.n;
            int p = (int)GrazType.TOMElement.p;
            int s = (int)GrazType.TOMElement.s;

            totPool.DM = totPool.DM + partPool.DM;
            totPool.Nu[n] = totPool.Nu[n] + partPool.Nu[n];
            totPool.Nu[p] = totPool.Nu[p] + partPool.Nu[p];
            totPool.Nu[s] = totPool.Nu[s] + partPool.Nu[s];
            totPool.AshAlk = totPool.AshAlk + partPool.AshAlk;
        }
    }
}