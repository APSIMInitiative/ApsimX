using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;


namespace Models.Stock
{
    /// <summary>
    /// Container for many GrazPlan constants
    /// </summary>
    static public class GrazType
    {
        /// <summary>
        /// 
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
        public const double VeryLarge = 1.0E6;
        /// <summary>
        /// Represents a small value
        /// </summary>
        public const double VerySmall = 1.0E-4;

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
        /// Maximum soil layers
        /// </summary>
        public const int MaxSoilLayers = 50;
#pragma warning disable 1591 //missing xml comment
        public const int stSEEDL = 1; public const int ptLEAF = 1; public const int SOFT = 1; public const int EFFR = 1;
        public const int stESTAB = 2; public const int ptSTEM = 2; public const int HARD = 2; public const int OLDR = 2;
        public const int stSENC = 3; public const int ptROOT = 3; public const int UNRIPE = 1;
        public const int stDEAD = 4; public const int ptSEED = 4; public const int RIPE = 2;
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
        public enum TOMElement { 
            /// <summary>
            /// Carbon
            /// </summary>
            C, 
            /// <summary>
            /// Nitrogen
            /// </summary>
            N, 
            /// <summary>
            /// Phosphorous
            /// </summary>
            P, 
            /// <summary>
            /// Sulphur
            /// </summary>
            S };
        /// <summary>
        /// Plant nutrients
        /// </summary>
        public enum TPlantNutrient { pnNO3, pnNH4, pnPOx, pnSO4 };

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
            /// Nutrients in kg element/ha
            /// </summary>
            public double[] Nu = new double[4];                       
            /// <summary>
            /// Ash alkalinity in mol/ha
            /// </summary>
            public double AshAlk;                                                      
        }
        /// <summary>
        /// Zero the DM pool
        /// </summary>
        /// <param name="Pool">Pool to zero</param>
        public static void ZeroDMPool(ref DM_Pool Pool)
        {
            int pe;

            Pool.DM = 0;
            for (pe = (int)TOMElement.N; pe <= (int)TOMElement.S; pe++)
                Pool.Nu[pe] = 0;
            Pool.AshAlk = 0;
        }

        // RootDMArray       = packed array[1..MaxSoilLayers] of DM_Pool;
        /// <summary>
        /// Sheep or Cattle animal type
        /// </summary>
        public enum AnimalType { Sheep, Cattle };
        /// <summary>
        /// Age type of the animal
        /// </summary>
        public enum AgeType { LambCalf, Weaner, Yearling, TwoYrOld, Mature };
        /// <summary>
        /// Text for the age types
        /// </summary>
        static public string[] AgeText = { "Young", "Weaner", "Yearling", "2-3yo", "Mature" };
        /// <summary>
        /// Reproduction type
        /// </summary>
        public enum ReproType { Castrated, Male, Empty, EarlyPreg, LatePreg };
        /// <summary>
        /// Lactation type
        /// </summary>
        public enum LactType { Dry, Lactating, Suckling };
        /// <summary>
        /// Sheep or cattle text
        /// </summary>
        static public string[] AnimalText = { "Sheep", "Cattle" };

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
            /// 
            /// </summary>
            public double Digestibility;
            /// <summary>
            /// 
            /// </summary>
            public double CrudeProtein;
            /// <summary>
            /// 
            /// </summary>
            public double Degradability;
            /// <summary>
            /// 
            /// </summary>
            public double PhosContent;
            /// <summary>
            /// 
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
        public class TGrazingInputs
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
            /// 
            /// </summary>
            public double SelectFactor;
            /// <summary>
            /// "Tropicality" of legumes 0 => temperate; 1 => tropical 
            /// </summary>
            public double LegumeTrop;                                               

            /// <summary>
            /// Construct a TGrazingInputs object
            /// </summary>
            public TGrazingInputs()
            {

            }

            /// <summary>
            /// Copy the whole object
            /// </summary>
            /// <param name="src"></param>
            public void CopyFrom(TGrazingInputs src)
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
            /// <param name="src"></param>
            public TGrazingInputs(TGrazingInputs src)
            {
                Array.Copy(src.Herbage, this.Herbage, src.Herbage.Length);
                TotalGreen = src.TotalGreen;
                TotalDead = src.TotalDead;
                this.LegumePropn = src.LegumePropn;
                Array.Copy(src.Seeds, Seeds, src.Seeds.Length);
                Array.Copy(src.SeedClass, SeedClass, src.SeedClass.Length);
                SelectFactor = src.SelectFactor;
                LegumeTrop = src.LegumeTrop;
            }
        }
        /// <summary>
        /// Zero the grazing inputs
        /// </summary>
        /// <param name="Inputs"></param>
        static public void zeroGrazingInputs(ref TGrazingInputs Inputs)
        {
            Inputs.Herbage = new IntakeRecord[DigClassNo + 1];
            Inputs.TotalGreen = 0;
            Inputs.TotalDead = 0;
            Inputs.LegumePropn = 0;
            Inputs.Seeds = new IntakeRecord[MaxPlantSpp + 1, RIPE + 1];
            Inputs.SeedClass = new int[MaxPlantSpp + 1, RIPE + 1];
            Inputs.SelectFactor = 0;
            Inputs.LegumeTrop = 0;
        }
        /// <summary>
        /// Add grazing inputs to total inputs
        /// </summary>
        /// <param name="iPopn"></param>
        /// <param name="partInputs"></param>
        /// <param name="totalInputs"></param>
        static public void addGrazingInputs(int iPopn, TGrazingInputs partInputs, ref TGrazingInputs totalInputs)
        {
            int iClass;
            // IntakeRecord intakeRec;
            {
                for (iClass = 1; iClass <= DigClassNo; iClass++)
                {
                    IntakeRecord intake = totalInputs.Herbage[iClass];

                    intake.HeightRatio = fWeightAverage(intake.HeightRatio,
                                                     intake.Biomass,
                                                     partInputs.Herbage[iClass].HeightRatio,
                                                     partInputs.Herbage[iClass].Biomass);
                    intake.Degradability = fWeightAverage(intake.Degradability,
                                                     intake.Biomass * intake.CrudeProtein,
                                                     partInputs.Herbage[iClass].Degradability,
                                                     partInputs.Herbage[iClass].Biomass
                                                     * partInputs.Herbage[iClass].CrudeProtein);
                    intake.Digestibility = fWeightAverage(intake.Digestibility,
                                                     intake.Biomass,
                                                     partInputs.Herbage[iClass].Digestibility,
                                                     partInputs.Herbage[iClass].Biomass);
                    intake.CrudeProtein = fWeightAverage(intake.CrudeProtein,
                                                     intake.Biomass,
                                                     partInputs.Herbage[iClass].CrudeProtein,
                                                     partInputs.Herbage[iClass].Biomass);
                    intake.PhosContent = fWeightAverage(intake.PhosContent,
                                                     intake.Biomass,
                                                     partInputs.Herbage[iClass].PhosContent,
                                                     partInputs.Herbage[iClass].Biomass);
                    intake.SulfContent = fWeightAverage(intake.SulfContent,
                                                     intake.Biomass,
                                                     partInputs.Herbage[iClass].SulfContent,
                                                     partInputs.Herbage[iClass].Biomass);
                    intake.AshAlkalinity = fWeightAverage(intake.AshAlkalinity,
                                                     intake.Biomass,
                                                     partInputs.Herbage[iClass].AshAlkalinity,
                                                     partInputs.Herbage[iClass].Biomass);
                    intake.Biomass = intake.Biomass + partInputs.Herbage[iClass].Biomass;
                    totalInputs.Herbage[iClass] = intake;
                }
                totalInputs.LegumePropn = fWeightAverage(totalInputs.LegumePropn,
                                                                totalInputs.TotalGreen + totalInputs.TotalDead,
                                                                partInputs.LegumePropn,
                                                                partInputs.TotalGreen + partInputs.TotalDead);
                totalInputs.SelectFactor = fWeightAverage(totalInputs.SelectFactor,
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
                totalInputs.LegumeTrop = fWeightAverage(totalInputs.LegumeTrop,
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
        public class TGrazingOutputs // Quantities grazed from a pasture         
        {
            /// <summary>
            /// 
            /// </summary>
            public double[] Herbage = new double[DigClassNo + 1];   // [1..
            /// <summary>
            /// 
            /// </summary>
            public double[,] Seed = new double[MaxPlantSpp + 1, 3];     // TODO: Fix this [1.., ripe..unripe]

            /// <summary>
            /// Copy from grazing outputs
            /// </summary>
            public void CopyFrom(TGrazingOutputs src)
            {
                Array.Copy(src.Herbage, this.Herbage, src.Herbage.Length);
                Array.Copy(src.Seed, this.Seed, src.Seed.Length);
            }
        }


        // Various constants with biological meanings      
        /// <summary>
        /// Default class digestibilities
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
        /// 
        /// </summary>
        public const double FatE2DM = 36.0;
        /// <summary>
        /// 
        /// </summary>
        public const double ProteinE2DM = 14.0;

        /// <summary>
        /// Height below which herbage is unavailable for grazing, GH (metres). From GRAZGRZE.PAS
        /// </summary>
        /// <param name="fHeight">Height of herbage, H                  (metres)</param>
        /// <param name="fMaxGH">Maximum value of GH                    (metres)</param>
        /// <param name="fCurvature">Curvature                          (0-1)</param>
        /// <param name="fSlope">Initial slope of the GH-H relationship (0-1)</param>
        /// <returns></returns>
        static public double fGrazingHeight(double fHeight, double fMaxGH, double fCurvature, double fSlope)
        {
            double Result;

            if (fSlope <= 0.0)                                                    // fSlope=0 => all herbage available        
                Result = 0.0;
            else if (fCurvature <= 0.0)                                           // fCurvature=0 => rectangular hyperbola    
                Result = fMaxGH * fHeight / (fHeight + fMaxGH / fSlope);
            else if (fCurvature >= 1.0)                                           // fCurvature=1 => piecewise linear         
                Result = Math.Min(fSlope * fHeight, fMaxGH);
            else                                                                      // Otherwise, a non-rectangular hyperbola   
                Result = (fSlope * fHeight + fMaxGH
                           - Math.Sqrt(Math.Pow(fSlope * fHeight + fMaxGH, 2) - 4.0 * fCurvature * fMaxGH * fSlope * fHeight))
                          / (2.0 * fCurvature);
            return Result;
        }
        /// <summary>
        /// Get a weighted average
        /// </summary>
        /// <param name="X1"></param>
        /// <param name="Y1"></param>
        /// <param name="X2"></param>
        /// <param name="Y2"></param>
        /// <returns></returns>
        static public double fWeightAverage(double X1, double Y1, double X2, double Y2)
        {
            double Result;
            if ((Y1 != 0.0) && (Y2 != 0.0))
                Result = (X1 * Y1 + X2 * Y2) / (Y1 + Y2);
            else if (Y1 != 0.0)
                Result = X1;
            else
                Result = X2;
            return Result;
        }
        /// <summary>
        /// Scale the grazing inputs
        /// </summary>
        /// <param name="Inputs"></param>
        /// <param name="fScale"></param>
        /// <returns></returns>
        static public TGrazingInputs scaleGrazingInputs(TGrazingInputs Inputs, double fScale)
        {
            int iClass;
            int iSpecies;
            int iRipe;

            TGrazingInputs Result = new TGrazingInputs(Inputs);

            if (fScale != 1.0)
            {
                for (iClass = 1; iClass <= DigClassNo; iClass++)
                    Result.Herbage[iClass].Biomass = fScale * Inputs.Herbage[iClass].Biomass;

                Result.TotalGreen = fScale * Inputs.TotalGreen;
                Result.TotalDead = fScale * Inputs.TotalDead;

                for (iSpecies = 1; iSpecies <= MaxPlantSpp; iSpecies++)
                    for (iRipe = UNRIPE; iRipe <= RIPE; iRipe++)
                        Result.Seeds[iSpecies, iRipe].Biomass = fScale * Inputs.Seeds[iSpecies, iRipe].Biomass;
            }
            return Result;
        }
        /// <summary>
        /// Rescale the DM pool
        /// </summary>
        /// <param name="aPool"></param>
        /// <param name="fPropn"></param>
        /// <returns></returns>
        static public DM_Pool PoolFraction(DM_Pool aPool, double fPropn)
        {
            return MultiplyDMPool(aPool, fPropn);
        }
        /// <summary>
        /// Multiply the DM pool
        /// </summary>
        /// <param name="aPool"></param>
        /// <param name="fScale"></param>
        /// <returns></returns>
        static public DM_Pool MultiplyDMPool(DM_Pool aPool, double fScale)
        {
            DM_Pool Result = new DM_Pool();
            Result.DM = fScale * aPool.DM;
            for (int Elem = (int)TOMElement.N; Elem <= (int)TOMElement.S; Elem++)
                Result.Nu[Elem] = fScale * aPool.Nu[Elem];
            Result.AshAlk = fScale * aPool.AshAlk;
            return Result;
        }
        /// <summary>
        /// Add dry matter pool to the total pool
        /// </summary>
        /// <param name="PartPool"></param>
        /// <param name="TotPool"></param>
        static public void AddDMPool(DM_Pool PartPool, DM_Pool TotPool)
        {
            int N = (int)GrazType.TOMElement.N;
            int P = (int)GrazType.TOMElement.P;
            int S = (int)GrazType.TOMElement.S;

            TotPool.DM = TotPool.DM + PartPool.DM;
            TotPool.Nu[N] = TotPool.Nu[N] + PartPool.Nu[N];
            TotPool.Nu[P] = TotPool.Nu[P] + PartPool.Nu[P];
            TotPool.Nu[S] = TotPool.Nu[S] + PartPool.Nu[S];
            TotPool.AshAlk = TotPool.AshAlk + PartPool.AshAlk;
        }

    }

    sealed class PreMergeToMergedDeserializationBinder : SerializationBinder
    {
        public override Type BindToType(string assemblyName, string typeName)
        {
            Type typeToDeserialize = null;

            // For each assemblyName/typeName that you want to deserialize to
            // a different type, set typeToDeserialize to the desired type.
            String exeAssembly = Assembly.GetExecutingAssembly().FullName;

            // The following line of code returns the type.
            typeToDeserialize = Type.GetType(String.Format("{0}, {1}", typeName, exeAssembly));

            return typeToDeserialize;
        }
    }

    /// <summary>
    /// Reference Article http://www.codeproject.com/KB/tips/SerializedObjectCloner.aspx
    /// Provides a method for performing a deep copy of an object.
    /// Binary Serialization is used to perform the copy.
    /// </summary>
    public static class ObjectCopier
    {
        /// <summary>
        /// Perform a deep Copy of the object.
        /// </summary>
        /// <typeparam name="T">The type of object being copied.</typeparam>
        /// <param name="source">The object instance to copy.</param>
        /// <returns>The copied object.</returns>
        public static T Clone<T>(T source)
        {
            if (!typeof(T).IsSerializable)
            {
                throw new ArgumentException(typeof(T).Name + " type must be serializable.", "source");
            }

            // Don't serialize a null object, simply return the default for that object
            if (Object.ReferenceEquals(source, null))
            {
                return default(T);
            }

            IFormatter formatter = new BinaryFormatter();
            Stream stream = new MemoryStream();
            using (stream)
            {
                formatter.Serialize(stream, source);
                stream.Seek(0, SeekOrigin.Begin);
                formatter.Binder = new PreMergeToMergedDeserializationBinder();
                return (T)formatter.Deserialize(stream);
            } 
        }
    }
}

