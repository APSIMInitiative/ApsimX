using System;
using System.Diagnostics.CodeAnalysis;
using Models.Core;

namespace Models.GrazPlan
{

    /// <summary>
    /// Livestock metabolizable energy partition
    /// </summary>
    [Serializable]
    public class EnergyUse
    {
        /// <summary>
        /// Gets or sets the basal maintenance requirement       
        /// </summary>
        [Units("MJ")]
        public double MaintBase { get; set; }

        /// <summary>
        /// Gets or sets the E(graze) + E(move)                  
        /// </summary>
        [Units("MJ")]
        public double MaintMoveGraze { get; set; }

        /// <summary>
        /// Gets or sets the E(cold)         
        /// </summary>
        [Units("MJ")]
        public double MaintCold { get; set; }

        /// <summary>
        /// Gets or sets the ME(c)           
        /// </summary>
        [Units("MJ")]
        public double Conceptus { get; set; }

        /// <summary>
        /// Gets or sets the ME(l) 
        /// </summary>
        [Units("MJ")]
        public double Lactation { get; set; }

        /// <summary>
        /// Gets or sets the ME(w) = NE(w) / k(w)           
        /// </summary>
        [Units("MJ")]
        public double Fleece { get; set; }

        /// <summary>
        /// Gets or sets the ME(g)      
        /// </summary>
        [Units("MJ")]
        public double Gain { get; set; }
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
        [Units("-")]
        public string Paddock { get; set; }

        /// <summary>
        /// Gets or sets the supplement eaten in kg
        /// </summary>
        [Units("kg")]
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
        [Units("kg/d")]
        public double Weight { get; set; }

        /// <summary>
        /// Gets or sets the dry matter pool N amount kg/d
        /// </summary>
        [Units("kg/d")]
        public double N { get; set; }

        /// <summary>
        /// Gets or sets the dry matter pool P amount kg/d
        /// </summary>
        [Units("kg/d")]
        public double P { get; set; }

        /// <summary>
        /// Gets or sets the dry matter pool S amount mol/d
        /// </summary>
        [Units("kg/d")]
        public double S { get; set; }

        /// <summary>
        /// Gets or sets the dry matter pool AshAlk amount mol/d
        /// </summary>
        [Units("mol/d")]
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
        [Units("kg/d")]
        public double N { get; set; }

        /// <summary>
        /// Gets or sets the P amount in kg/d
        /// </summary>
        [Units("kg/d")]
        public double P { get; set; }

        /// <summary>
        /// Gets or sets the S amount in mol/d
        /// </summary>
        [Units("mol/d")]
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
        public const int prpBASE_EMPTY_WT = 43;
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

                int n = model == null ? 0 : model.Count();
                for (idx = 1; idx <= n; idx++)
                    if (!useTag || (model.Animals[idx].Tag == p))
                    {
                        if (!useYoung)
                            animalGroup = model.Animals[idx];
                        else
                            animalGroup = model.Animals[idx].Young;

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

                int count = model == null ? 0 : model.Count();
                for (idx = 1; idx <= count; idx++)
                {
                    if (!useTag || (model.Animals[idx].Tag == passIdx))
                    {
                        if (!useYoung)
                            animalGroup = model.Animals[idx];
                        else
                            animalGroup = model.Animals[idx].Young;

                        value = 0.0;
                        if (animalGroup != null)
                        {
                            int n = (int)GrazType.TOMElement.n;
                            int p = (int)GrazType.TOMElement.p;
                            int s = (int)GrazType.TOMElement.s;
                            switch (varCode)
                            {
                                case StockProps.prpAGE:
                                    value = animalGroup.AgeDays;
                                    break;
                                case StockProps.prpAGE_MONTHS:
                                    value = animalGroup.AgeDays / MONTH2DAY;
                                    break;
                                case StockProps.prpLIVE_WT:
                                    value = animalGroup.LiveWeight;
                                    break;
                                case StockProps.prpBASE_WT:
                                    value = animalGroup.BaseWeight;
                                    break;
                                case StockProps.prpCOND_SCORE:
                                    value = animalGroup.ConditionScore(StockUtilities.Cond_System.csSYSTEM1_5);
                                    break;
                                case StockProps.prpMAX_PREV_WT:
                                    value = animalGroup.MaxPrevWeight;
                                    break;
                                case StockProps.prpFLEECE_WT:
                                    value = animalGroup.FleeceCutWeight;
                                    break;
                                case StockProps.prpCFLEECE_WT:
                                    value = animalGroup.CleanFleeceWeight;
                                    break;
                                case StockProps.prpFIBRE_DIAM:
                                    value = animalGroup.FibreDiam;
                                    break;
                                case StockProps.prpPREGNANT:
                                    value = animalGroup.Pregnancy;
                                    break;
                                case StockProps.prpLACTATING:
                                    value = animalGroup.Lactation;
                                    break;
                                case StockProps.prpNO_FOETUSES:
                                    value = animalGroup.NoFoetuses;
                                    break;
                                case StockProps.prpNO_SUCKLING:
                                    value = animalGroup.NoOffspring;
                                    break;
                                case StockProps.prpBIRTH_CS:
                                    value = animalGroup.BirthCondition;
                                    break;
                                case StockProps.prpDSE:
                                    value = animalGroup.DrySheepEquivs;
                                    break;
                                case StockProps.prpWT_CHANGE:
                                    value = animalGroup.WeightChange;
                                    break;
                                case StockProps.prpME_INTAKE:
                                    value = animalGroup.AnimalState.ME_Intake.Total;
                                    break;
                                case StockProps.prpCPI_INTAKE:
                                    value = animalGroup.AnimalState.CP_Intake.Total;
                                    break;
                                case StockProps.prpCFLEECE_GROWTH:
                                    value = animalGroup.CleanFleeceGrowth;
                                    break;
                                case StockProps.prpDAY_FIBRE_DIAM:
                                    value = animalGroup.DayFibreDiam;
                                    break;
                                case StockProps.prpMILK_WT:
                                    value = animalGroup.MilkYield;
                                    break;
                                case StockProps.prpMILK_ME:
                                    value = animalGroup.MilkEnergy;
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
                                case StockProps.prpURINE_N:
                                    value = animalGroup.AnimalState.Urine.Nu[n];
                                    break;
                                case StockProps.prpURINE_P:
                                    value = animalGroup.AnimalState.Urine.Nu[p];
                                    break;
                                case StockProps.prpURINE_S:
                                    value = animalGroup.AnimalState.Urine.Nu[s];
                                    break;
                                case StockProps.prpCH4_OUTPUT:
                                    value = 0.001 * animalGroup.MethaneWeight;         // Convert g/d to kg/d                  
                                    break;
                                case StockProps.prpRDPI:
                                    value = animalGroup.AnimalState.RDP_Intake;
                                    break;
                                case StockProps.prpRDPR:
                                    value = animalGroup.AnimalState.RDP_Reqd;
                                    break;
                                case StockProps.prpRDP_EFFECT:
                                    value = animalGroup.AnimalState.RDP_IntakeEffect;
                                    break;
                                case StockProps.prpINTAKE_MOD:
                                    value = animalGroup.IntakeModifier;
                                    break;
                                case StockProps.prpBASE_EMPTY_WT:
                                    value = animalGroup.EmptyBodyWeight;
                                    break;
                                default:
                                    result = false;
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

                int count = model == null ? 0 : model.Count();
                for (idx = 1; idx <= count; idx++)
                {
                    if (!useTag || (model.Animals[idx].Tag == passIdx))
                    {
                        if (!useYoung)
                            animalGroup = model.Animals[idx];
                        else
                            animalGroup = model.Animals[idx].Young;

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
            for (paddIdx = 0; paddIdx <= model.Paddocks.Count - 1; paddIdx++)
            {
                if (model.Paddocks[paddIdx].SuppRemovalKG > 0.0)
                    count++;
            }

            suppValues = new SupplementEaten[count];
            idx = 0;
            for (paddIdx = 0; paddIdx <= model.Paddocks.Count - 1; paddIdx++)
                if (model.Paddocks[paddIdx].SuppRemovalKG > 0.0)
                {
                    suppValues[idx] = new SupplementEaten();
                    suppValues[idx].Paddock = model.Paddocks[paddIdx].Name;
                    suppValues[idx].Eaten = model.Paddocks[paddIdx].SuppRemovalKG;
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
                AnimalGroup group = model.Animals[idx];
                ME_Metab = group.AnimalState.EnergyUse.Metab / group.AnimalState.Efficiency.Maint;
                ME_MoveGraze = group.AnimalState.EnergyUse.Maint - ME_Metab - group.AnimalState.EnergyUse.Cold;

                energyValues[idx].MaintBase = ME_Metab;
                energyValues[idx].MaintMoveGraze = ME_MoveGraze;  // Separating E(graze) and E(move) requires work in AnimGRP.pas...
                energyValues[idx].MaintCold = group.AnimalState.EnergyUse.Cold;
                energyValues[idx].Conceptus = group.AnimalState.EnergyUse.Preg;
                energyValues[idx].Lactation = group.AnimalState.EnergyUse.Lact;
                energyValues[idx].Fleece = group.AnimalState.EnergyUse.Wool / group.AnimalState.Efficiency.Gain;
                energyValues[idx].Gain = group.AnimalState.EnergyUse.Gain / group.AnimalState.Efficiency.Gain;
            }
        }
    }
}
