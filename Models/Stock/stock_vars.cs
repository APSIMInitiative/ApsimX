namespace Models.GrazPlan
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using Models.Core;

    /// <summary>
    /// Livestock metabolizable energy partition
    /// </summary>
    [Serializable]
    public class EnergyUse
    {
        /// <summary>
        /// Gets or sets the basal maintenance requirement       
        /// MJ
        /// </summary>
        public double MaintBase { get; set; }

        /// <summary>
        /// Gets or sets the E(graze) + E(move)                  
        /// MJ
        /// </summary>
        public double MaintMoveGraze { get; set; }
        
        /// <summary>
        /// Gets or sets the E(cold)         
        /// MJ
        /// </summary>
        public double MaintCold { get; set; }
        
        /// <summary>
        /// Gets or sets the ME(c)           
        /// MJ
        /// </summary>
        public double Conceptus { get; set; }
        
        /// <summary>
        /// Gets or sets the ME(l) 
        /// MJ
        /// </summary>
        public double Lactation { get; set; }
        
        /// <summary>
        /// Gets or sets the ME(w) = NE(w) / k(w)           
        /// MJ
        /// </summary>
        public double Fleece { get; set; }
        
        /// <summary>
        /// Gets or sets the ME(g)      
        /// MJ
        /// </summary>
        public double Gain { get; set; }
    } 

    /// <summary>
    /// The stock genotype. The initial values in the stock component.
    /// </summary>
    public class StockGeno
    {
        /// <summary>
        /// Gets or sets the name of the genotype
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the mother breed
        /// </summary>
        public string DamBreed { get; set; }
        
        /// <summary>
        /// Gets or sets the male parent
        /// </summary>
        public string SireBreed { get; set; }
        
        /// <summary>
        /// Gets or sets the generation count
        /// </summary>
        public int Generation { get; set; }
        
        /// <summary>
        /// Gets or sets the standard Reference Weight kg
        /// </summary>
        public double SRW { get; set; }

        /// <summary>
        /// Gets or sets the conception rates
        /// </summary>
        public double[] Conception = new double[4];
        
        /// <summary>
        /// Gets or sets the death rate /y
        /// </summary>
        public double DeathRate { get; set; }
        
        /// <summary>
        /// Gets or sets the reference fleece weight kg
        /// </summary>
        public double RefFleeceWt { get; set; }
        
        /// <summary>
        /// Gets or sets the fibre diameter in um
        /// </summary>
        public double MaxFibreDiam { get; set; }
        
        /// <summary>
        /// Gets or sets the fleece yield kg/kg
        /// </summary>
        public double FleeceYield { get; set; }
        
        /// <summary>
        /// Gets or sets the peak milk production kg
        /// </summary>
        public double PeakMilk { get; set; }
        
        /// <summary>
        /// Gets or sets the weaner death rate /y
        /// </summary>
        public double WnrDeathRate { get; set; }
    }

    /// <summary>
    /// Parent class for the sheep and cattle init classes
    /// </summary>
    public class AnimalInit
    {
        /// <summary>
        /// Gets or sets the genotype name
        /// </summary>
        public string Genotype { get; set; }

        /// <summary>
        /// Gets or sets the count
        /// </summary>
        public int Number { get; set; }
        
        /// <summary>
        /// Gets or sets the animal sex type
        /// </summary>
        public ReproductiveType Sex { get; set; } 
        
        /// <summary>
        /// Gets or sets the in days
        /// </summary>
        public double Age { get; set; }
        
        /// <summary>
        /// Gets or sets the weight in kg
        /// </summary>
        public double Weight { get; set; }
        
        /// <summary>
        /// Gets or sets the maximum previous weight kg
        /// </summary>
        public double MaxPrevWt { get; set; }
        
        /// <summary>
        /// Gets or sets the mated to genotype
        /// </summary>
        public string MatedTo { get; set; }
        
        /// <summary>
        /// Gets or sets the number of days pregnant d
        /// </summary>
        public int Pregnant { get; set; }
        
        /// <summary>
        /// Gets or sets the days lactating d
        /// </summary>
        public int Lactating { get; set; }
        
        /// <summary>
        /// Gets or sets the condition score at birth
        /// </summary>
        public double BirthCS { get; set; }
        
        /// <summary>
        /// Gets or sets the occupied paddock name
        /// </summary>
        public string Paddock { get; set; }
        
        /// <summary>
        /// Gets or sets the tag number
        /// </summary>
        public int Tag { get; set; }
        
        /// <summary>
        /// Gets or sets the priority number
        /// </summary>
        public int Priority { get; set; }
    }

    /// <summary>
    /// The sheep init type
    /// </summary>
    public class SheepInit : AnimalInit
    {
        /// <summary>
        /// Gets or sets the fleece weight in kg
        /// </summary>
        public double FleeceWt { get; set; }
        
        /// <summary>
        /// Gets or sets the fibre diameter in um
        /// </summary>
        public double FibreDiam { get; set; }
        
        /// <summary>
        /// Gets or sets the number of young
        /// </summary>
        public int NumYoung { get; set; }
        
        /// <summary>
        /// Gets or sets the lamb weight kg
        /// </summary>
        public double LambWt { get; set; }
        
        /// <summary>
        /// Gets or sets the lamb fleece weight kg
        /// </summary>
        public double LambFleeceWt { get; set; }
    }

    /// <summary>
    /// The cattle init type
    /// </summary>
    public class CattleInit : AnimalInit
    {
        /// <summary>
        /// Gets or sets the number of foetuses
        /// </summary>
        public int NumFoetuses { get; set; }
        
        /// <summary>
        /// Gets or sets the number of suckling young
        /// </summary>
        public int NumSuckling { get; set; }
        
        /// <summary>
        /// Gets or sets the calf weight in kg
        /// </summary>
        public double CalfWt { get; set; }
    }

    /// <summary>
    /// The paddock init type
    /// </summary>
    public class PaddockInit
    {
        /// <summary>
        /// Gets or sets the name of the paddock that is used for Move events
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Gets or sets the paddock area in ha
        /// </summary>
        [Units("ha")]
        public double Area { get; set; }

        /// <summary>
        /// Gets or sets the paddock slope in deg
        /// </summary>
        [Units("deg")]
        public double Slope { get; set; }

        /// <summary>
        /// Gets or sets the list of forages
        /// </summary>
        public string[] Forages;
        
        /// <summary>
        /// Gets or sets the destination for excreta or faeces
        /// </summary>
        public string Excretion { get; set; }
        
        /// <summary>
        /// Gets or sets the destination for urine
        /// </summary>
        public string Urine { get; set; }
    }

    /// <summary>
    /// Supplement eaten type
    /// </summary>
    [Serializable]
    public class SupplementEaten
    {
        /// <summary>
        /// Gets or sets the paddock name
        /// </summary>
        public string Paddock { get; set; }
        
        /// <summary>
        /// Gets or sets the supplement eaten in kg
        /// </summary>
        public double Eaten { get; set; }
    }

    /// <summary>
    /// Dry matter pool
    /// </summary>
    [Serializable]
    public class DMPoolHead
    {
        /// <summary>
        /// Gets or sets the dry matter pool weight in kg/d
        /// </summary>
        public double Weight { get; set; }
        
        /// <summary>
        /// Gets or sets the dry matter pool N amount kg/d
        /// </summary>
        public double N { get; set; }
        
        /// <summary>
        /// Gets or sets the dry matter pool P amount kg/d
        /// </summary>
        public double P { get; set; }
        
        /// <summary>
        /// Gets or sets the dry matter pool S amount mol/d
        /// </summary>
        public double S { get; set; }
        
        /// <summary>
        /// Gets or sets the dry matter pool AshAlk amount mol/d
        /// </summary>
        public double AshAlk { get; set; }
    }

    /// <summary>
    /// Inorganic faeces type
    /// </summary>
    [Serializable]
    public class InorgFaeces
    {
        /// <summary>
        /// Gets or sets the N amount in kg/d
        /// </summary>
        public double N { get; set; }
        
        /// <summary>
        /// Gets or sets the P amount in kg/d
        /// </summary>
        public double P { get; set; }
        
        /// <summary>
        /// Gets or sets the S amount in mol/d
        /// </summary>
        public double S { get; set; }
    }
    
    /// <summary>
    /// Definitions of many property constants in the Stock component
    /// </summary>
    [SuppressMessage("Microsoft.StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented", Justification = "Reviewed.")]
    [SuppressMessage("Microsoft.StyleCop.CSharp.NamingRules", "SA1310:FieldNamesMustNotContainUnderscore", Justification = "Reviewed.")]
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1303:ConstFieldNamesMustBeginWithUpperCaseLetter", Justification = "Reviewed.")]
    public static class StockProps
    {
#pragma warning disable 1591 //missing xml comment
        // Reporting variables -------------------------------------------------------}
        public const int prpAGE = 1;
        public const int prpAGE_MONTHS = 2;
        public const int prpLIVE_WT = 3;
        public const int prpBASE_WT = 4;
        public const int prpCOND_SCORE = 5;
        public const int prpMAX_PREV_WT = 6;
        public const int prpFLEECE_WT = 7;
        public const int prpCFLEECE_WT = 8;
        public const int prpFIBRE_DIAM = 9;
        public const int prpPREGNANT = 10;
        public const int prpLACTATING = 11;
        public const int prpNO_FOETUSES = 12;
        public const int prpNO_SUCKLING = 13;
        public const int prpBIRTH_CS = 14;

        public const int prpPADDOCK = 15;
        public const int prpTAG = 16;
        public const int prpPRIORITY = 17;

        public const int prpDSE = 18;
        public const int prpWT_CHANGE = 19;
        public const int prpINTAKE = 20;
        public const int prpINTAKE_PAST = 21;
        public const int prpINTAKE_SUPP = 22;
        public const int prpME_INTAKE = 23;
        public const int prpCPI_INTAKE = 24;
        public const int prpCFLEECE_GROWTH = 25;
        public const int prpDAY_FIBRE_DIAM = 26;
        public const int prpMILK_WT = 27;
        public const int prpMILK_ME = 28;
        public const int prpRETAINED_N = 29;
        public const int prpRETAINED_P = 30;
        public const int prpRETAINED_S = 31;
        public const int prpFAECES = 32;
        public const int prpURINE_N = 33;
        public const int prpURINE_P = 34;
        public const int prpURINE_S = 35;
        public const int prpINORG_FAECES = 36;
        public const int prpCH4_OUTPUT = 37;
        public const int prpRDPI = 38;
        public const int prpRDPR = 39;
        public const int prpRDP_EFFECT = 40;
        public const int prpPADD_RANK = 41;
        public const int prpINTAKE_MOD = 42;
#pragma warning restore 1591 //missing xml comment
    }

    /// <summary>
    /// Container for Stock property access
    /// </summary>
    public static class StockVars
    {
        /// <summary>
        /// Convert to days
        /// </summary>
        public const double MONTH2DAY = 365.25 / 12;

        /// <summary>
        /// Copies the parameters into an array of genotype structures
        /// </summary>
        /// <param name="model">The animal model</param>
        /// <param name="genoValues">The genotypes returned</param>
        public static void MakeGenotypesValue(StockList model, ref StockGeno[] genoValues)
        {
            AnimalParamSet parameters;
            string damBreed = string.Empty;
            string sireBreed = string.Empty;
            int generation = 0;
            int idx, jdx;

            Array.Resize(ref genoValues, model.GenotypeCount());
            for (idx = 0; idx <= model.GenotypeCount() - 1; idx++)
            {
                parameters = model.GetGenotype(idx);

                if (parameters.ParentageCount() == 1)
                {
                    damBreed = parameters.ParentageBreed(0);
                    sireBreed = damBreed;
                    generation = 0;
                }
                else if ((parameters.ParentageCount() == 2) && (parameters.ParentagePropn(0) > 0))
                {
                    damBreed = parameters.ParentageBreed(0);
                    sireBreed = parameters.ParentageBreed(1);
                    generation = Convert.ToInt32(Math.Max(0, Math.Round(Math.Log(parameters.ParentagePropn(0)) / Math.Log(0.5))), CultureInfo.InvariantCulture);    // TODO: may need checking
                }
                else if (parameters.ParentageCount() == 2)
                {
                    sireBreed = parameters.ParentageBreed(1);
                    damBreed = sireBreed;
                    generation = 0;
                }
                else
                {
                    damBreed = parameters.ParentageBreed(0);
                    sireBreed = parameters.ParentageBreed(1);
                    generation = 0;
                }

                genoValues[idx].Name = parameters.Name;
                genoValues[idx].DamBreed = damBreed;
                genoValues[idx].SireBreed = sireBreed;
                genoValues[idx].Generation = generation;

                genoValues[idx].SRW = parameters.BreedSRW;
                genoValues[idx].DeathRate = parameters.AnnualDeaths(false);
                genoValues[idx].WnrDeathRate = parameters.AnnualDeaths(true);
                genoValues[idx].RefFleeceWt = parameters.PotentialGFW;
                genoValues[idx].MaxFibreDiam = parameters.MaxMicrons;
                genoValues[idx].FleeceYield = parameters.FleeceYield;
                genoValues[idx].PeakMilk = parameters.PotMilkYield;

                if (parameters.Animal == GrazType.AnimalType.Sheep)
                    Array.Resize(ref genoValues[idx].Conception, 3);
                else
                    Array.Resize(ref genoValues[idx].Conception, 2);
                for (jdx = 0; jdx < genoValues[idx].Conception.Length; jdx++)
                    genoValues[idx].Conception[jdx] = parameters.Conceptions[jdx];
            }
        }

        /// <summary>
        /// Fill an init array with animal details from the model
        /// </summary>
        /// <param name="model"></param>
        /// <param name="initValue"></param>
        public static void MakeAnimalValue(StockList model, ref AnimalInits[] initValue)
        {
            AnimalGroup animalGroup;
            int idx, jdx;

            Array.Resize(ref initValue, model.Count());

            jdx = 0;
            for (idx = 1; idx <= model.Count(); idx++)
            {
                animalGroup = model.At(idx);

                initValue[jdx] = new AnimalInits();

                initValue[jdx].Genotype = animalGroup.Genotype.Name;
                initValue[jdx].Number = animalGroup.NoAnimals;
                initValue[jdx].Sex = animalGroup.ReproState;
                initValue[jdx].AgeDays = animalGroup.AgeDays;
                initValue[jdx].Weight = animalGroup.LiveWeight;
                initValue[jdx].MaxPrevWt = animalGroup.MaxPrevWeight;
                initValue[jdx].Pregnant = animalGroup.Pregnancy;
                initValue[jdx].Lactating = animalGroup.Lactation;

                GrazType.AnimalType animal = model.At(idx).Genotype.Animal;
                if (animal == GrazType.AnimalType.Sheep)
                {
                    initValue[jdx].FleeceWt = animalGroup.FleeceCutWeight;
                    initValue[jdx].FibreDiam = animalGroup.FibreDiam;
                    initValue[jdx].NumFoetuses = Math.Max(animalGroup.NoFoetuses, animalGroup.NoOffspring);
                }
                else if (animal == GrazType.AnimalType.Cattle)
                {
                    initValue[jdx].NumFoetuses = animalGroup.NoFoetuses;
                    initValue[jdx].NumSuckling = animalGroup.NoOffspring;
                }

                if (animalGroup.Lactation > 0)
                    initValue[jdx].BirthCS = animalGroup.BirthCondition;

                if ((animalGroup.Pregnancy > 0) || (animalGroup.Young != null))
                {
                    if (animalGroup.MatedTo != null)
                        initValue[jdx].MatedTo = animalGroup.MatedTo.Name;
                    else
                        initValue[jdx].MatedTo = string.Empty;
                }
                else
                    initValue[jdx].MatedTo = string.Empty;

                if (animalGroup.Young != null)
                {
                    initValue[jdx].YoungWt = animalGroup.Young.LiveWeight;
                    if (animal == GrazType.AnimalType.Sheep)
                    {
                        initValue[jdx].YoungGFW = animalGroup.Young.FleeceCutWeight;
                    }

                    initValue[jdx].Paddock = model.GetInPadd(idx);
                    initValue[jdx].Tag = model.GetTag(idx);
                    initValue[jdx].Priority = model.GetPriority(idx);
                }
                jdx++;
            } // next animal
        }
    
        /// <summary>
        /// Fill the paddock init list
        /// </summary>
        /// <param name="model">The stock model</param>
        /// <param name="initValue">The init value</param>
        public static void MakePaddockList(StockList model, ref PaddockInit[] initValue)
        {
            PaddockInfo paddock;
            int idx, jdx;

            Array.Resize(ref initValue, model.Paddocks.Count());

            for (idx = 0; idx < initValue.Length; idx++)
            {
                paddock = model.Paddocks.ByIndex(idx);
                initValue[idx] = new PaddockInit();
                initValue[idx].Name = paddock.Name;                                       // "name"                                
                initValue[idx].Area = paddock.Area;                                       // "area"                                
                initValue[idx].Slope = paddock.Slope;                                       // "slope"                               
                Array.Resize(ref initValue[idx].Forages, paddock.Forages.Count());
                for (jdx = 0; jdx < paddock.Forages.Count(); jdx++)
                    initValue[idx].Forages[jdx] = paddock.Forages.ByIndex(jdx).Name;
                initValue[idx].Excretion = paddock.ExcretionDest;                              // "excretion"                           
            }
        }

        /// <summary>
        /// Rank the paddocks
        /// </summary>
        /// <param name="model">The animal model</param>
        /// <param name="initValue">The paddock ranks</param>
        public static void MakePaddockRank(StockList model, ref string[] initValue)
        {
            List<string> rankingList = new List<string>();

            model.RankPaddocks(rankingList);
            initValue = rankingList.ToArray();
            /*Array.Resize(ref aValue, sList.Count);
            for (int Idx = 0; Idx < aValue.Length; Idx++)
                aValue[Idx] = sList[Idx];*/
        }

        /// <summary>
        /// The output counts for these type of animals
        /// </summary>
        public enum CountType
        {
            /// <summary>
            /// Both males and females
            /// </summary>
            eBoth,

            /// <summary>
            /// Female animals
            /// </summary>
            eFemale,
            
            /// <summary>
            /// Male animals
            /// </summary>
            eMale,
            
            /// <summary>
            /// Deaths of non suckling animals
            /// </summary>
            eDeaths
        }

        /// <summary>
        /// Populate the numbers array for the type of output required
        /// </summary>
        /// <param name="model">The Stock list model</param>
        /// <param name="code">The count type</param>
        /// <param name="useYoung">Report for young animals</param>
        /// <param name="useAll">Combined value</param>
        /// <param name="useTag">Use tag groups</param>
        /// <param name="numbers">The populated array of numbers</param>
        /// <returns>True if the code type is valid</returns>
        public static bool PopulateNumberValue(StockList model, CountType code, bool useYoung, bool useAll, bool useTag, ref int[] numbers)
        {
            int numPasses;
            AnimalGroup animalGroup;
            int value;
            int total;
            int p, idx;

            bool result = true;
            Array.Clear(numbers, 0, numbers.Length);

            if (useTag)
                numPasses = numbers.Length;
            else
                numPasses = 1;

            for (p = 1; p <= numPasses; p++)
            {
                total = 0;

                for (idx = 1; idx <= model.Count(); idx++)
                    if (!useTag || (model.GetTag(idx) == p))
                    {
                        if (!useYoung)
                            animalGroup = model.At(idx);
                        else
                            animalGroup = model.At(idx).Young;

                        value = 0;
                        if (animalGroup != null)
                        {
                            switch (code)
                            {
                                case CountType.eBoth:
                                    value = animalGroup.NoAnimals;
                                    break;
                                case CountType.eFemale:
                                    value = animalGroup.FemaleNo;
                                    break;
                                case CountType.eMale:
                                    value = animalGroup.MaleNo;
                                    break;
                                case CountType.eDeaths:
                                    value = animalGroup.Deaths;
                                    break;
                                default:
                                    result = false;
                                    break;
                            }
                        }
                        if (!useTag && !useAll)
                            numbers[idx - 1] = value;
                        else
                            total = total + value;
                    } // _ loop over animal groups 

                if (useAll)
                    numbers[0] = total;
                else if (useTag)
                    numbers[p - 1] = total;
            } // _ loop over passes _
            return result;
        }

        /// <summary>
        /// Fill the double[] with values from the model.
        /// </summary>
        /// <param name="model">The animal model</param>
        /// <param name="varCode">The variable code</param>
        /// <param name="useYoung">For young animals</param>
        /// <param name="useAll">For all groups</param>
        /// <param name="useTag">Use the tag number</param>
        /// <param name="arrayValues">The returned double array</param>
        /// <returns>True if the varCode is valid</returns>
        public static bool PopulateRealValue(StockList model, int varCode, bool useYoung, bool useAll, bool useTag, ref double[] arrayValues)
        {
            int numPasses;
            AnimalGroup animalGroup;
            double value;
            double total;
            int denom;
            int passIdx, idx;

            bool result = true;
            Array.Clear(arrayValues, 0, arrayValues.Length);

            if (useTag)
                numPasses = arrayValues.Length;
            else
                numPasses = 1;

            for (passIdx = 1; passIdx <= numPasses; passIdx++)
            {
                total = 0.0;
                denom = 0;

                for (idx = 1; idx <= model.Count(); idx++)
                {
                    if (!useTag || (model.GetTag(idx) == passIdx))
                    {
                        if (!useYoung)
                            animalGroup = model.At(idx);
                        else
                            animalGroup = model.At(idx).Young;

                        value = 0.0;
                        if (animalGroup != null)
                        {
                            int n = (int)GrazType.TOMElement.n;
                            int p = (int)GrazType.TOMElement.p;
                            int s = (int)GrazType.TOMElement.s;
                            switch (varCode)
                            {
                                case StockProps.prpAGE: value = animalGroup.AgeDays;
                                    break;
                                case StockProps.prpAGE_MONTHS: value = animalGroup.AgeDays / MONTH2DAY;
                                    break;
                                case StockProps.prpLIVE_WT: value = animalGroup.LiveWeight;
                                    break;
                                case StockProps.prpBASE_WT: value = animalGroup.BaseWeight;
                                    break;
                                case StockProps.prpCOND_SCORE: value = animalGroup.fConditionScore(AnimalParamSet.Cond_System.csSYSTEM1_5);
                                    break;
                                case StockProps.prpMAX_PREV_WT: value = animalGroup.MaxPrevWeight;
                                    break;
                                case StockProps.prpFLEECE_WT: value = animalGroup.FleeceCutWeight;
                                    break;
                                case StockProps.prpCFLEECE_WT: value = animalGroup.CleanFleeceWeight;
                                    break;
                                case StockProps.prpFIBRE_DIAM: value = animalGroup.FibreDiam;
                                    break;
                                case StockProps.prpPREGNANT: value = animalGroup.Pregnancy;
                                    break;
                                case StockProps.prpLACTATING: value = animalGroup.Lactation;
                                    break;
                                case StockProps.prpNO_FOETUSES: value = animalGroup.NoFoetuses;
                                    break;
                                case StockProps.prpNO_SUCKLING: value = animalGroup.NoOffspring;
                                    break;
                                case StockProps.prpBIRTH_CS: value = animalGroup.BirthCondition;
                                    break;
                                case StockProps.prpDSE: value = animalGroup.DrySheepEquivs;
                                    break;
                                case StockProps.prpWT_CHANGE: value = animalGroup.WeightChange;
                                    break;
                                case StockProps.prpME_INTAKE: value = animalGroup.AnimalState.ME_Intake.Total;
                                    break;
                                case StockProps.prpCPI_INTAKE: value = animalGroup.AnimalState.CP_Intake.Total;
                                    break;
                                case StockProps.prpCFLEECE_GROWTH: value = animalGroup.CleanFleeceGrowth;
                                    break;
                                case StockProps.prpDAY_FIBRE_DIAM: value = animalGroup.DayFibreDiam;
                                    break;
                                case StockProps.prpMILK_WT: value = animalGroup.MilkYield;
                                    break;
                                case StockProps.prpMILK_ME: value = animalGroup.MilkEnergy;
                                    break;
                                case StockProps.prpRETAINED_N:
                                    value = (animalGroup.AnimalState.CP_Intake.Total / GrazType.N2Protein) - (animalGroup.AnimalState.InOrgFaeces.Nu[n] + animalGroup.AnimalState.OrgFaeces.Nu[n] + animalGroup.AnimalState.Urine.Nu[n]);
                                    break;
                                case StockProps.prpRETAINED_P:
                                    value = animalGroup.AnimalState.Phos_Intake.Total - (animalGroup.AnimalState.InOrgFaeces.Nu[p] + animalGroup.AnimalState.OrgFaeces.Nu[p] + animalGroup.AnimalState.Urine.Nu[p]);
                                    break;
                                case StockProps.prpRETAINED_S:
                                    value = animalGroup.AnimalState.Sulf_Intake.Total - (animalGroup.AnimalState.InOrgFaeces.Nu[s] + animalGroup.AnimalState.OrgFaeces.Nu[s] + animalGroup.AnimalState.Urine.Nu[s]);
                                    break;
                                case StockProps.prpURINE_N: value = animalGroup.AnimalState.Urine.Nu[n];
                                    break;
                                case StockProps.prpURINE_P: value = animalGroup.AnimalState.Urine.Nu[p];
                                    break;
                                case StockProps.prpURINE_S: value = animalGroup.AnimalState.Urine.Nu[s];
                                    break;
                                case StockProps.prpCH4_OUTPUT:
                                    value = 0.001 * animalGroup.MethaneWeight;         // Convert g/d to kg/d                  
                                    break;
                                case StockProps.prpRDPI: value = animalGroup.AnimalState.RDP_Intake;
                                    break;
                                case StockProps.prpRDPR: value = animalGroup.AnimalState.RDP_Reqd;
                                    break;
                                case StockProps.prpRDP_EFFECT: value = animalGroup.AnimalState.RDP_IntakeEffect;
                                    break;
                                case StockProps.prpINTAKE_MOD: value = animalGroup.IntakeModifier;
                                    break;
                                default: result = false;
                                    break;
                            }
                        }

                        if (!useTag && !useAll)
                            arrayValues[idx - 1] = value;
                        else if (varCode == StockProps.prpDSE)                                     // Sum DSE's; take a weighted average of 
                            total = total + value;                                                 // all other values                    
                        else if (animalGroup != null)
                        {
                            total = total + (animalGroup.NoAnimals * value);
                            denom = denom + animalGroup.NoAnimals;
                        }
                    } // _ loop over animal groups _
                }

                if ((varCode != StockProps.prpDSE) && (denom > 0))
                    total = total / denom;
                if (useAll)
                    arrayValues[0] = total;
                else if (useTag)
                    arrayValues[passIdx - 1] = total;
            } // _ loop over passes _
            return result;
        }

        /// <summary>
        /// Convert the dry matter pool
        /// </summary>
        /// <param name="pool">The DM pool</param>
        /// <param name="poolValue">The pool data</param>
        /// <param name="onlyNPSVal">The NPS values only</param>
        public static void DMPool2Value(GrazType.DM_Pool pool, ref DMPoolHead poolValue, bool onlyNPSVal = false)
        {
            if (!onlyNPSVal)
            {
                poolValue.Weight = pool.DM;                                          // Item[1] = "weight"   kg/head          
                poolValue.N = pool.Nu[(int)GrazType.TOMElement.n];                   // Item[2] = "n"        kg/head          
                poolValue.P = pool.Nu[(int)GrazType.TOMElement.p];                   // Item[3] = "p"        kg/head          
                poolValue.S = pool.Nu[(int)GrazType.TOMElement.s];                   // Item[4] = "s"        kg/head          
                poolValue.AshAlk = pool.AshAlk;                                     // Item[5] = "ash_alk"  mol/head         
            }
            else
            {
                poolValue.N = pool.Nu[(int)GrazType.TOMElement.n];                   // Item[1] = "n"        kg/head          
                poolValue.P = pool.Nu[(int)GrazType.TOMElement.p];                   // Item[2] = "p"        kg/head          
                poolValue.S = pool.Nu[(int)GrazType.TOMElement.s];                   // Item[3] = "s"        kg/head          
            }
        }

        /// <summary>
        /// Populate the dry matter pool
        /// </summary>
        /// <param name="model">The stock model</param>
        /// <param name="propCode">The property code</param>
        /// <param name="useYoung">For young</param>
        /// <param name="useAll">For all groups</param>
        /// <param name="useTag">For tag number</param>
        /// <param name="poolValues">The DM pool value returned</param>
        /// <returns>True if the propCode is valid</returns>
        public static bool PopulateDMPoolValue(StockList model, int propCode, bool useYoung, bool useAll, bool useTag, ref DMPoolHead[] poolValues)
        {
            int numPasses;
            AnimalGroup animalGroup;
            GrazType.DM_Pool pool = new GrazType.DM_Pool();
            GrazType.DM_Pool totalPool = new GrazType.DM_Pool();
            int denom;
            int passIdx, idx;

            bool result = true;
            for (int i = 0; i < poolValues.Length; i++)
                poolValues[i] = new DMPoolHead();

            if (useTag)
                numPasses = poolValues.Length;
            else
                numPasses = 1;

            for (passIdx = 1; passIdx <= numPasses; passIdx++)
            {
                GrazType.ZeroDMPool(ref totalPool);
                denom = 0;

                for (idx = 1; idx <= model.Count(); idx++)
                {
                    if (!useTag || (model.GetTag(idx) == passIdx))
                    {
                        if (!useYoung)
                            animalGroup = model.At(idx);
                        else
                            animalGroup = model.At(idx).Young;

                        GrazType.ZeroDMPool(ref pool);
                        if (animalGroup != null)
                        {
                            int n = (int)GrazType.TOMElement.n;
                            int p = (int)GrazType.TOMElement.p;
                            int s = (int)GrazType.TOMElement.s;

                            switch (propCode)
                            {
                                case StockProps.prpINTAKE:
                                    pool.DM = animalGroup.AnimalState.DM_Intake.Total;
                                    pool.Nu[n] = animalGroup.AnimalState.CP_Intake.Total / GrazType.N2Protein;
                                    pool.Nu[p] = animalGroup.AnimalState.Phos_Intake.Total;
                                    pool.Nu[s] = animalGroup.AnimalState.Sulf_Intake.Total;
                                    pool.AshAlk = (animalGroup.AnimalState.PaddockIntake.AshAlkalinity * animalGroup.AnimalState.PaddockIntake.Biomass)
                                                   + (animalGroup.AnimalState.SuppIntake.AshAlkalinity * animalGroup.AnimalState.SuppIntake.Biomass);
                                    break;
                                case StockProps.prpINTAKE_PAST:
                                    pool.DM = animalGroup.AnimalState.DM_Intake.Herbage;
                                    pool.Nu[n] = animalGroup.AnimalState.CP_Intake.Herbage / GrazType.N2Protein;
                                    pool.Nu[p] = animalGroup.AnimalState.Phos_Intake.Herbage;
                                    pool.Nu[s] = animalGroup.AnimalState.Sulf_Intake.Herbage;
                                    pool.AshAlk = animalGroup.AnimalState.PaddockIntake.AshAlkalinity * animalGroup.AnimalState.PaddockIntake.Biomass;
                                    break;
                                case StockProps.prpINTAKE_SUPP:
                                    pool.DM = animalGroup.AnimalState.DM_Intake.Supp;
                                    pool.Nu[n] = animalGroup.AnimalState.CP_Intake.Supp / GrazType.N2Protein;
                                    pool.Nu[p] = animalGroup.AnimalState.Phos_Intake.Supp;
                                    pool.Nu[s] = animalGroup.AnimalState.Sulf_Intake.Supp;
                                    pool.AshAlk = animalGroup.AnimalState.SuppIntake.AshAlkalinity * animalGroup.AnimalState.SuppIntake.Biomass;
                                    break;
                                case StockProps.prpFAECES:
                                    GrazType.AddDMPool(animalGroup.AnimalState.OrgFaeces, pool);
                                    GrazType.AddDMPool(animalGroup.AnimalState.InOrgFaeces, pool);
                                    break;
                                case StockProps.prpINORG_FAECES:
                                    GrazType.AddDMPool(animalGroup.AnimalState.InOrgFaeces, pool);
                                    break;
                                default:
                                    result = false;
                                    break;
                            }
                        }

                        if (!useTag && !useAll)
                        {
                            DMPool2Value(pool, ref poolValues[idx - 1], (propCode == StockProps.prpINORG_FAECES));
                        }
                        else if (animalGroup != null)
                        {
                            GrazType.AddDMPool(GrazType.MultiplyDMPool(pool, animalGroup.NoAnimals), totalPool);
                            denom = denom + animalGroup.NoAnimals;
                        }
                    }
                } // _ loop over animal groups _

                if (useTag || useAll)
                {
                    if (denom > 0)
                        totalPool = GrazType.PoolFraction(totalPool, 1.0 / denom);
                    if (useAll)
                    {
                        DMPool2Value(totalPool, ref poolValues[0], (propCode == StockProps.prpINORG_FAECES));
                    }
                    else
                    {
                        DMPool2Value(totalPool, ref poolValues[passIdx - 1], (propCode == StockProps.prpINORG_FAECES));
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Copy the supplement eaten into a SupplementEaten[]
        /// </summary>
        /// <param name="model">The animal model</param>
        /// <param name="suppValues">The supplement data returned</param>
        public static void MakeSuppEaten(StockList model, ref SupplementEaten[] suppValues)
        {
            int count;
            int paddIdx;
            uint idx;

            count = 0;
            for (paddIdx = 0; paddIdx <= model.Paddocks.Count() - 1; paddIdx++)
            {
                if (model.Paddocks.ByIndex(paddIdx).SuppRemovalKG > 0.0)
                    count++;
            }
            
            suppValues = new SupplementEaten[count];
            idx = 0;
            for (paddIdx = 0; paddIdx <= model.Paddocks.Count() - 1; paddIdx++)
                if (model.Paddocks.ByIndex(paddIdx).SuppRemovalKG > 0.0)
                {
                    suppValues[idx] = new SupplementEaten();
                    suppValues[idx].Paddock = model.Paddocks.ByIndex(paddIdx).Name;
                    suppValues[idx].Eaten = model.Paddocks.ByIndex(paddIdx).SuppRemovalKG;
                    idx++;
                }
        }

        /// <summary>
        /// Populate metabolizable energy use array 
        /// Note: these are an* ME* partition                                          
        /// </summary>
        /// <param name="model">The animal model</param>
        /// <param name="energyValues">The energy use returned</param>
        public static void MakeEnergyUse(StockList model, ref EnergyUse[] energyValues)
        {
            double ME_Metab;
            double ME_MoveGraze;
            int idx;

            for (idx = 1; idx <= model.Count(); idx++)
            {
                AnimalGroup group = model.At(idx);
                ME_Metab = group.AnimalState.EnergyUse.Metab / group.AnimalState.Efficiency.Maint;
                ME_MoveGraze = group.AnimalState.EnergyUse.Maint - ME_Metab - group.AnimalState.EnergyUse.Cold;

                energyValues[idx].MaintBase = ME_Metab;
                energyValues[idx].MaintMoveGraze = ME_MoveGraze;  // Separating E(graze) and E(move) requires work in AnimGRP.pas...
                energyValues[idx].MaintCold = group.AnimalState.EnergyUse.Cold;
                energyValues[idx].Conceptus = group.AnimalState.EnergyUse.Preg;
                energyValues[idx].Lactation = group.AnimalState.EnergyUse.Lact;
                energyValues[idx].Fleece  = group.AnimalState.EnergyUse.Wool / group.AnimalState.Efficiency.Gain;
                energyValues[idx].Gain = group.AnimalState.EnergyUse.Gain / group.AnimalState.Efficiency.Gain;
            }
        }
    }
}
