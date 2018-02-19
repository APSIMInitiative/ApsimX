using System;
using System.Collections.Generic;
using System.Linq;
using StdUnits;

namespace Models.GrazPlan
{

    /// <summary>
    /// Used to bundle animal genotype information so it can be passed to            
    /// TStockList.Create.                                                           
    /// N.B. All the numeric fields may be set to DMISSING, and MaleBreedName may     
    ///      be set to the null string, in which case the TStockList class will      
    ///      provide a default.                                                      
    /// </summary>
    [Serializable]
    public class TSingleGenotypeInits
    {
        /// <summary>
        /// Genotype name
        /// </summary>
        public string sGenotypeName;
        /// <summary>
        /// Dam breed name
        /// </summary>
        public string sDamBreed;
        /// <summary>
        /// Sire breed name
        /// </summary>
        public string sSireBreed;
        /// <summary>
        /// 1 = first cross, 2 = second cross etc
        /// </summary>
        public int iGeneration;                         
        /// <summary>
        /// Standard reference weight
        /// </summary>
        public double SRW;
        /// <summary>
        /// Potential fleece weight
        /// </summary>
        public double PotFleeceWt;
        /// <summary>
        /// Maximum wool fibre diameter
        /// </summary>
        public double MaxFibreDiam;
        /// <summary>
        /// Fleece yield
        /// </summary>
        public double FleeceYield;
        /// <summary>
        /// Peak milk production
        /// </summary>
        public double PeakMilk;
        /// <summary>
        /// Death rates
        /// </summary>
        public double[] DeathRate = new double[2];      //array[Boolean]
        /// <summary>
        /// Conception rates
        /// </summary>
        public double[] Conceptions = new double[4];    //array[1..  3]
    }

    /// <summary>
    /// Information required to initialise a single animal group, as a record.      
    /// N.B. the YoungWt and YoungGFW fields may be set to MISSING, in which case    
    ///      TStockList will estimate defaults.                                       
    /// </summary>
    [Serializable]
    public struct TAnimalInits
    {
        /// <summary>
        /// Genotype name
        /// </summary>
        public string sGenotype;
        /// <summary>
        /// Number of animals
        /// </summary>
        public int Number;
        /// <summary>
        /// Sex of animals
        /// </summary>
        public GrazType.ReproType Sex;
        /// <summary>
        /// Age in days
        /// </summary>
        public int AgeDays;
        /// <summary>
        /// Weight of animals
        /// </summary>
        public double Weight;
        /// <summary>
        /// Maximum previous weight
        /// </summary>
        public double MaxPrevWt;
        /// <summary>
        /// Fleece weight
        /// </summary>
        public double Fleece_Wt;
        /// <summary>
        /// Fleece fibre diameter 
        /// </summary>
        public double Fibre_Diam;
        /// <summary>
        /// Mated to animal
        /// </summary>
        public string sMatedTo;
        /// <summary>
        /// Days pregnant
        /// </summary>
        public int Pregnant;
        /// <summary>
        /// Days lactating
        /// </summary>
        public int Lactating;
        /// <summary>
        /// Number of foetuses
        /// </summary>
        public int No_Foetuses;
        /// <summary>
        /// Number of suckling young
        /// </summary>
        public int No_Suckling;
        /// <summary>
        /// Greasy fleece weight of young
        /// </summary>
        public double Young_GFW;
        /// <summary>
        /// Weight of young
        /// </summary>
        public double Young_Wt;
        /// <summary>
        /// Birth Condition score
        /// </summary>
        public double Birth_CS;
        /// <summary>
        /// Paddock location
        /// </summary>
        public string Paddock;
        /// <summary>
        /// Tag of animal group
        /// </summary>
        public int Tag;
        /// <summary>
        /// Priority level
        /// </summary>
        public int Priority;
    }

    /// <summary>
    ///  Abbreviated animal initialisation set, used in TStockList.Buy                
    /// </summary>
    [Serializable]
    public struct TPurchaseInfo
    {
        /// <summary>
        /// Genotype name
        /// </summary>
        public string sGenotype;
        /// <summary>
        /// Number of animals
        /// </summary>
        public int Number;
        /// <summary>
        /// Live weight
        /// </summary>
        public double LiveWt;
        /// <summary>
        /// Greasy fleece weight
        /// </summary>
        public double GFW;
        /// <summary>
        /// Age in days
        /// </summary>
        public int AgeDays;
        /// <summary>
        /// Condition score
        /// </summary>
        public double fCondScore;
        /// <summary>
        /// Reproduction status
        /// </summary>
        public GrazType.ReproType Repro;
        /// <summary>
        /// Mated to animal
        /// </summary>
        public string sMatedTo;
        /// <summary>
        /// Pregnant days
        /// </summary>
        public int Preg;
        /// <summary>
        /// Lactation days
        /// </summary>
        public int Lact;
        /// <summary>
        /// Number of young
        /// </summary>
        public int NYoung;
        /// <summary>
        /// Weight of young
        /// </summary>
        public double YoungWt;
        /// <summary>
        /// Greasy fleece weight of young
        /// </summary>
        public double YoungGFW;
    }

    /// <summary>
    /// Attributes of a set of livstock cohorts, used in TStockList.AddStock         
    /// </summary>
    [Serializable]
    public struct TCohortsInfo
    {
        /// <summary>
        /// Genotype name
        /// </summary>
        public string sGenotype;
        /// <summary>
        /// Total number of animals to enter the simulation. 
        /// The animals will be distributed across the age cohorts, 
        /// taking the genotype-specific death rate into account
        /// </summary>
        public int iNumber;
        /// <summary>
        /// Reproduction status
        /// </summary>
        public GrazType.ReproType ReproClass;
        /// <summary>
        /// Minimum years of the youngest cohort
        /// </summary>
        public int iMinYears;
        /// <summary>
        /// Maximum years of the oldest cohort
        /// </summary>
        public int iMaxYears;
        /// <summary>
        /// 
        /// </summary>
        public int iAgeOffsetDays;
        /// <summary>
        /// Average unfasted live weight of the animals across all age cohorts
        /// </summary>
        public double fMeanLiveWt;
        /// <summary>
        /// Average condition score of the animals 
        /// </summary>
        public double fCondScore;
        /// <summary>
        /// Average greasy fleece weight of the animals across all age cohorts
        /// </summary>
        public double fMeanGFW;
        /// <summary>
        /// Days since shearing
        /// </summary>
        public int iFleeceDays;
        /// <summary>
        /// Genotype of the rams or bulls with which the animals were mated prior to entry
        /// </summary>
        public string sMatedTo;
        /// <summary>
        /// Days pregnant
        /// </summary>
        public int iDaysPreg;
        /// <summary>
        /// Average number of foetuses per animal (including barren animals) across all age classes
        /// </summary>
        public double fFoetuses;
        /// <summary>
        /// The time since parturition in those animals that are lactating
        /// </summary>
        public int iDaysLact;
        /// <summary>
        /// Average number of suckling offspring per animal (including dry animals) across all age classes
        /// </summary>
        public double fOffspring;
        /// <summary>
        /// Average unfasted live weight of any suckling lambs or calves
        /// </summary>
        public double fOffspringWt;
        /// <summary>
        /// Average body condition score of any suckling lambs or calves
        /// </summary>
        public double fOffspringCS;
        /// <summary>
        /// Average greasy fleece weight of any suckling lambs
        /// </summary>
        public double fLambGFW;
    }

    /// <summary>
    /// The container for stock
    /// </summary>
    [Serializable]
    public class TStockContainer
    {
        /// <summary>
        /// The animal group
        /// </summary>
        public TAnimalGroup Animals;
        /// <summary>
        /// Paddock occupied
        /// </summary>
        public TPaddockInfo PaddOccupied;
        /// <summary>
        /// Tag number
        /// </summary>
        public int iTag;
        /// <summary>
        /// Priority level
        /// </summary>
        public int iPriority;

        /// <summary>
        /// 0=mothers, 1=suckling young
        /// </summary>
        public TStateInfo[] initState = new TStateInfo[2];             
        /// <summary>
        /// 
        /// </summary>
        public double[] fRDPFactor = new double[2];      //[0..1] 
        /// <summary>
        /// Index is to forage-within-paddock
        /// </summary>
        public GrazType.TGrazingInputs[] initForageInputs;              
        /// <summary>
        /// Forage inputs
        /// </summary>
        public GrazType.TGrazingInputs[] stepForageInputs;
        /// <summary>
        /// Paddock grazing inputs
        /// </summary>
        public GrazType.TGrazingInputs paddockInputs;
        /// <summary>
        /// Pasture intake
        /// </summary>
        public GrazType.TGrazingOutputs[] pastIntakeRate = new GrazType.TGrazingOutputs[2];
        /// <summary>
        /// Supplement intake
        /// </summary>
        public double[] fSuppIntakeRate = new double[2];

        /// <summary>
        /// Create a stock container
        /// </summary>
        public TStockContainer()
        {
            for (int i = 0; i < 2; i++)
                pastIntakeRate[i] = new GrazType.TGrazingOutputs();
        }
    }

    /// <summary>
    /// TStockList is primarily a list of TAnimalGroups. Each animal group has a     
    /// "current paddock" (function getInPadd() ) and a "group tag" (function getTag()      
    /// associated with it. The correspondences between these and the animal         
    /// groups must be maintained.                                                   
    ///                                                                               
    /// In addition, the class maintains two other lists:                            
    /// FPaddockInfo  holds paddock-specific information.  Animal groups are        
    ///                 related to the members of FPaddockInfo by the FPaddockNos     
    ///                 array.                                                        
    ///   FSwardInfo    holds the herbage availabilities and amounts removed from     
    ///                 each sward (i.e. all components which respond to the          
    ///                 call for "sward2stock").  The animal groups never refer to    
    ///                 this information directly; instead, the TStockList.Dynamics   
    ///                 method (1) aggregates the availability in each sward into     
    ///                 a paddock-level total, and (2) once the grazing logic has     
    ///                 been executed it also allocates the amounts removed between   
    ///                 the various swards.  Swards are allocated to paddocks on      
    ///                 the basis of their FQDN's.                                    
    ///                                                                               
    ///  N.B. The use of a fixed-length array for priorities and paddock numbers      
    ///       limits the number of animal groups that can be stored in this           
    ///       implementation.                                                         
    ///  N.B. The At property is 1-offset.  In many of the management methods, an     
    ///       index of 0 denotes "do to all groups".                                  
    /// </summary>
    [Serializable]
    public class TStockList
    {
        private const double MONTH2DAY = 365.25 / 12;
        private double[] MIN_SRW = { 30.0, 300.0 };                       // [AnimalType] Limits to breed SRW's                 
        private double[] MAX_SRW = { 120.0, 1000.0 };

        private const double WEIGHT2DSE = 0.02;                           // Converts animal mass into "dse"s for trampling purposes                     
        private const int FALSE = 0;
        private const int TRUE = 1;
        private string FParamFile;
        private TAnimalParamSet FBaseParams;
        private TAnimalParamSet[] FGenotypes = new TAnimalParamSet[0];
        private TStockContainer[] FStock = new TStockContainer[0];        //FStock[0] is kept for use as temporary storage         
        private TPaddockList FPaddocks;
        TEnterpriseList FEnterprises;
        TGrazingList FGrazing;
        TForageProviders FForageProviders;

        /// <summary>
        /// Makes a copy of TAnimalParamsGlb and modifies it according to sConstFile     
        /// </summary>
        /// <param name="sConstFile"></param>
        /// <returns></returns>
        private TAnimalParamSet MakeParamSet(string sConstFile)
        {
            TAnimalParamSet Result = new TAnimalParamSet((TAnimalParamSet)null);
            Result.CopyAll(TGAnimalParams.AnimalParamsGlb());
            if (sConstFile != "")
                TGParamFactory.ParamXMLFactory().readFromFile(sConstFile, Result, true);
            Result.sCurrLocale = GrazLocale.sDefaultLocale();
            return Result;
        }
        private void setParamFile(string sFileName)
        {
            FBaseParams = null;
            FBaseParams = MakeParamSet(sFileName);
            FParamFile = sFileName;
        }
        /// <summary>
        /// iPosn is 1-offset; so is FStock
        /// </summary>
        /// <param name="iPosn"></param>
        /// <returns></returns>
        private TAnimalGroup getAt(int iPosn)
        {
            return FStock[iPosn].Animals;
        }
        private void setAt(int iPosn, TAnimalGroup AG)
        {
            if ((iPosn == Count() + 1) && (AG != null))
                Add(AG, Paddocks.byIndex(0), 0, 0);
            else
                FStock[iPosn].Animals = AG;
        }
        /// <summary>
        /// iPosn is 1-offset; so is FStock                                              
        /// </summary>
        /// <param name="iPosn"></param>
        /// <returns></returns>
        private TPaddockInfo getPaddInfo(int iPosn)
        {
            return FStock[iPosn].PaddOccupied;
        }
        /// <summary>
        /// iPosn is 1-offset; so is FStock                                              
        /// </summary>
        /// <param name="iPosn"></param>
        /// <returns></returns>
        public string getInPadd(int iPosn)
        {
            if ((iPosn >= 1) && (iPosn <= Count()))
                return FStock[iPosn].PaddOccupied.sName;
            else
                return String.Empty;
        }
        /// <summary>
        /// iPosn is 1-offset; so is FStock
        /// </summary>
        /// <param name="iPosn"></param>
        /// <param name="sValue"></param>
        public void setInPadd(int iPosn, string sValue)
        {
            TPaddockInfo Paddock;

            Paddock = Paddocks.byName(sValue);
            if (Paddock == null)
                throw new Exception("Stock: attempt to place animals in non-existent paddock: " + sValue);
            else
                FStock[iPosn].PaddOccupied = Paddock;
        }
        /// <summary>
        /// iPosn is 1-offset; so is FStock                                              
        /// </summary>
        /// <param name="iPosn"></param>
        /// <returns></returns>
        public int getPriority(int iPosn)
        {
            if ((iPosn >= 1) && (iPosn <= Count()))
                return FStock[iPosn].iPriority;
            else
                return 0;
        }
        /// <summary>
        /// iPosn is 1-offset; so is FStock
        /// </summary>
        /// <param name="iPosn"></param>
        /// <param name="iValue"></param>
        public void setPriority(int iPosn, int iValue)
        {
            if ((iPosn >= 1) && (iPosn <= Count()))
                FStock[iPosn].iPriority = iValue;
        }

        private void setWeather(TAnimalWeather TheEnv)
        {
            int I;

            for (I = 1; I <= Count(); I++)
            {
                At(I).Weather = TheEnv;
                if (At(I).Young != null)
                    At(I).Young.Weather = TheEnv;
            }
        }
        /// <summary>
        /// These values are paddock-specific and are stored in the FPaddocks list.        
        /// </summary>
        /// <param name="iPaddID"></param>
        /// <param name="fValue"></param>
        private void setWaterLog(int iPaddID, double fValue)
        {
            TPaddockInfo PaddInfo;

            PaddInfo = FPaddocks.byID(iPaddID);
            if (PaddInfo != null)
                PaddInfo.fWaterlog = fValue;
        }
        
        /// <summary>
        /// Combine sufficiently-similar groups of animals and delete empty ones         
        /// </summary>
        private void Merge()
        {
            int Idx, Jdx;
            TAnimalGroup AG;

            for (Idx = 1; Idx <= Count(); Idx++)                                                     // Remove empty groups                   
            {
                if ((At(Idx) != null) && (At(Idx).NoAnimals == 0))
                {
                    setAt(Idx, null);
                }
            }

            for (Idx = 1; Idx <= Count() - 1; Idx++)                                                // Merge similar groups                     
            {
                for (Jdx = Idx + 1; Jdx <= Count(); Jdx++)
                {
                    if ((At(Idx) != null) && (At(Jdx) != null)
                       && At(Idx).Similar(At(Jdx))
                       && (getPaddInfo(Idx) == getPaddInfo(Jdx))
                       && (getTag(Idx) == getTag(Jdx))
                       && (getPriority(Idx) == getPriority(Jdx)))
                    {
                        AG = At(Jdx);
                        setAt(Jdx, null);
                        At(Idx).Merge(ref AG);
                    }
                }
            }
            for (Idx = Count(); Idx >= 1; Idx--)                                              // Pack the lists and priority array.      
            {
                if (At(Idx) == null)
                    Delete(Idx);
            }
        }

        /// <summary>
        /// Records state information prior to the grazing and nutrition calculations      
        /// so that it can be restored if there is an RDP insufficiency.                 
        /// </summary>
        /// <param name="iPosn"></param>
        private void storeInitialState(int iPosn)
        {
            At(iPosn).storeStateInfo(ref FStock[iPosn].initState[0]);
            if (At(iPosn).Young != null)
                At(iPosn).Young.storeStateInfo(ref FStock[iPosn].initState[1]);
        }

        /// <summary>
        /// Restores state information about animal groups if there is an RDP            
        /// insufficiency. Also alters the intake limit.                                 
        /// * Assumes that FStock[*].fRDPFactor[] has ben populated - see the            
        ///   computeNutritiion() method.                                                
        /// </summary>
        /// <param name="iPosn"></param>
        private void revertInitialState(int iPosn)
        {
            At(iPosn).revertStateInfo(FStock[iPosn].initState[0]);
            At(iPosn).PotIntake = At(iPosn).PotIntake * FStock[iPosn].fRDPFactor[0];

            if (At(iPosn).Young != null)
            {
                At(iPosn).Young.revertStateInfo(FStock[iPosn].initState[1]);
                At(iPosn).Young.PotIntake = At(iPosn).Young.PotIntake * FStock[iPosn].fRDPFactor[1];
            }
        }
        /// <summary>
        /// 1. Sets the livestock inputs (other than forage and supplement amounts) for    
        ///    animal groups occupying the paddock denoted by aPaddock.                  
        /// 2. Sets up the amounts of herbage available to each animal group from each   
        ///    forage (for animal groups and forages in the paddock denoted by aPaddock).  
        /// </summary>
        /// <param name="iPosn"></param>
        private void setInitialStockInputs(int iPosn)
        {
            TAnimalGroup aGroup;
            TPaddockInfo aPaddock;
            int Jdx;

            aGroup = At(iPosn);
            aPaddock = getPaddInfo(iPosn);

            aGroup.PaddSteep = aPaddock.Steepness;
            aGroup.WaterLogging = aPaddock.fWaterlog;
            aGroup.RationFed.Assign(aPaddock.SuppInPadd);                              // fTotalAmount will be overridden       

            //ensure young are fed
            if (aGroup.Young != null)
            {
                aGroup.Young.PaddSteep = aPaddock.Steepness;
                aGroup.Young.WaterLogging = aPaddock.fWaterlog;
                aGroup.Young.RationFed.Assign(aPaddock.SuppInPadd);
            }

            Array.Resize(ref FStock[iPosn].initForageInputs, aPaddock.Forages.Count());
            Array.Resize(ref FStock[iPosn].stepForageInputs, aPaddock.Forages.Count());
            for (Jdx = 0; Jdx <= aPaddock.Forages.Count() - 1; Jdx++)
            {
                if (FStock[iPosn].stepForageInputs[Jdx] == null)
                    FStock[iPosn].stepForageInputs[Jdx] = new GrazType.TGrazingInputs();

                FStock[iPosn].initForageInputs[Jdx] = aPaddock.Forages.byIndex(Jdx).availForage(aGroup.Genotype.GrazeC[17],
                                                                                                aGroup.Genotype.GrazeC[18],
                                                                                                aGroup.Genotype.GrazeC[19]);
                FStock[iPosn].stepForageInputs[Jdx].CopyFrom(FStock[iPosn].initForageInputs[Jdx]);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="iPosn"></param>
        public void computeStepAvailability(int iPosn)
        {
            TPaddockInfo aPaddock;
            TAnimalGroup aGroup;
            double fPropn;
            int Jdx;

            aPaddock = getPaddInfo(iPosn);
            aGroup = At(iPosn);

            FStock[iPosn].paddockInputs = new GrazType.TGrazingInputs();
            for (Jdx = 0; Jdx <= aPaddock.Forages.Count() - 1; Jdx++)
                GrazType.addGrazingInputs(Jdx + 1,
                                  FStock[iPosn].stepForageInputs[Jdx],
                                  ref FStock[iPosn].paddockInputs);

            aGroup.Herbage.CopyFrom(FStock[iPosn].paddockInputs);
            aGroup.RationFed.Assign(aPaddock.SuppInPadd);                              // fTotalAmount will be overridden       

            if (aPaddock.fSummedPotIntake > 0.0)
                fPropn = aGroup.PotIntake / aPaddock.fSummedPotIntake;                     // This is the proportion of the total   
            else                                                                         //   supplement that one animal gets     
                fPropn = 0.0;

            aGroup.RationFed.TotalAmount = fPropn * StdMath.DIM(aPaddock.SuppInPadd.TotalAmount,
                                                           aPaddock.SuppRemovalKG);
            if (aGroup.Young != null)
            {
                aGroup.Young.Herbage.CopyFrom(FStock[iPosn].paddockInputs);
                aGroup.Young.RationFed.Assign(aPaddock.SuppInPadd);

                if (aPaddock.fSummedPotIntake > 0.0)
                    fPropn = aGroup.Young.PotIntake / aPaddock.fSummedPotIntake;
                else
                    fPropn = 0.0;
                aGroup.Young.RationFed.TotalAmount = fPropn * StdMath.DIM(aPaddock.SuppInPadd.TotalAmount,
                                                                     aPaddock.SuppRemovalKG);
            }
        }

        /// <summary>
        /// Limits the length of a grazing sub-step so that no more than MAX_CONSUMPTION 
        /// of the herbage is consumed.                                                  
        /// </summary>
        /// <param name="aPaddock"></param>
        /// <returns></returns>
        private double computeStepLength(TPaddockInfo aPaddock)
        {
            double Result;
            const double MAX_CONSUMPTION = 0.20;

            int iPosn;
            double[] fHerbageRI = new double[GrazType.DigClassNo + 1];
            double[,] fSeedRI = new double[GrazType.MaxPlantSpp + 1, 3];
            double fSuppRelIntake = 0.0;
            double fRemovalRate;
            double fRemovalTime;
            int iClass;

            iPosn = 1;                                                                  // Find the first animal group occupying 
            while ((iPosn <= Count()) && (getPaddInfo(iPosn) != aPaddock))                //   ths paddock                         
                iPosn++;

            if ((iPosn > Count()) || (aPaddock.fArea <= 0.0))
                Result = 1.0;
            else
            {
                At(iPosn).CalculateRelIntake(At(iPosn), 1.0, false, 1.0, ref fHerbageRI, ref fSeedRI, ref fSuppRelIntake);

                fRemovalTime = 9999.9;
                for (iClass = 1; iClass <= GrazType.DigClassNo; iClass++)
                    if (FStock[iPosn].paddockInputs.Herbage[iClass].Biomass > 0.0)
                    {
                        fRemovalRate = aPaddock.fSummedPotIntake * fHerbageRI[iClass] / aPaddock.fArea;
                        if (fRemovalRate > 0.0)
                            fRemovalTime = Math.Min(fRemovalTime, FStock[iPosn].paddockInputs.Herbage[iClass].Biomass / fRemovalRate);
                    }
				
				Result = Math.Max( 0.01, Math.Min( 1.0, MAX_CONSUMPTION * fRemovalTime ) );
            }

            return Result;
        }
        /// <summary>
        /// Calculate the intake limit
        /// </summary>
        /// <param name="aGroup"></param>
        public void computeIntakeLimit(TAnimalGroup aGroup)
        {
            aGroup.Calc_IntakeLimit();
            if (aGroup.Young != null)
                aGroup.Young.Calc_IntakeLimit();
        }

        private void computeGrazing(int iPosn, double dStartTime, double dDeltaTime)
        {
            At(iPosn).Grazing(dDeltaTime, (dStartTime == 0.0), false,
                               ref FStock[iPosn].pastIntakeRate[0], ref FStock[iPosn].fSuppIntakeRate[0]);
            if (At(iPosn).Young != null)
                At(iPosn).Young.Grazing(dDeltaTime, (dStartTime == 0.0), false,
                                        ref FStock[iPosn].pastIntakeRate[1], ref FStock[iPosn].fSuppIntakeRate[1]);
        }
        private void computeRemoval(TPaddockInfo aPaddock, double dDeltaTime)
        {
            TAnimalGroup aGroup;
            TForageInfo aForage;
            double fPropn;
            int iPosn;
            int iForage;
            int iClass;
            int iRipe;

            if (aPaddock.fArea > 0.0)
            {
                for (iPosn = 1; iPosn <= Count(); iPosn++)
                {
                    if (getPaddInfo(iPosn) == aPaddock)
                    {
                        aGroup = At(iPosn);

                        for (iForage = 0; iForage <= aPaddock.Forages.Count() - 1; iForage++)
                        {
                            aForage = aPaddock.Forages.byIndex(iForage);

                            for (iClass = 1; iClass <= GrazType.DigClassNo; iClass++)
                            {
                                if (FStock[iPosn].paddockInputs.Herbage[iClass].Biomass > 0.0)
                                {
                                    fPropn = FStock[iPosn].stepForageInputs[iForage].Herbage[iClass].Biomass
                                              / FStock[iPosn].paddockInputs.Herbage[iClass].Biomass;
                                    aForage.RemovalKG.Herbage[iClass] =
                                      aForage.RemovalKG.Herbage[iClass]
                                      + fPropn * dDeltaTime * aGroup.NoAnimals * FStock[iPosn].pastIntakeRate[0].Herbage[iClass];
                                    if (aGroup.Young != null)
                                        aForage.RemovalKG.Herbage[iClass] =
                                          aForage.RemovalKG.Herbage[iClass]
                                          + fPropn * dDeltaTime * aGroup.Young.NoAnimals * FStock[iPosn].pastIntakeRate[1].Herbage[iClass];
                                }
                            }

                            for (iRipe = GrazType.UNRIPE; iRipe <= GrazType.RIPE; iRipe++)
                                aForage.RemovalKG.Seed[1, iRipe] =
                                  aForage.RemovalKG.Seed[1, iRipe]
                                  + dDeltaTime * aGroup.NoAnimals * FStock[iPosn].pastIntakeRate[0].Seed[iForage + 1, iRipe];
                            if (aGroup.Young != null)
                                for (iRipe = GrazType.UNRIPE; iRipe <= GrazType.RIPE; iRipe++)
                                    aForage.RemovalKG.Seed[1, iRipe] =
                                      aForage.RemovalKG.Seed[1, iRipe]
                                      + dDeltaTime * aGroup.Young.NoAnimals * FStock[iPosn].pastIntakeRate[1].Seed[iForage + 1, iRipe];
                        } //_ loop over forages within paddock _

                        aPaddock.SuppRemovalKG = aPaddock.SuppRemovalKG + dDeltaTime * aGroup.NoAnimals * FStock[iPosn].fSuppIntakeRate[0];
                        if (aGroup.Young != null)
                            aPaddock.SuppRemovalKG = aPaddock.SuppRemovalKG + dDeltaTime * aGroup.Young.NoAnimals * FStock[iPosn].fSuppIntakeRate[1];
                    } //_ loop over animal groups within paddock _
                }

                for (iPosn = 1; iPosn <= Count(); iPosn++)
                {
                    if (getPaddInfo(iPosn) == aPaddock)
                    {
                        for (iForage = 0; iForage <= aPaddock.Forages.Count() - 1; iForage++)
                        {
                            aForage = aPaddock.Forages.byIndex(iForage);

                            for (iClass = 1; iClass <= GrazType.DigClassNo; iClass++)
                                FStock[iPosn].stepForageInputs[iForage].Herbage[iClass].Biomass =
                                  StdMath.DIM(FStock[iPosn].initForageInputs[iForage].Herbage[iClass].Biomass,
                                       aForage.RemovalKG.Herbage[iClass] / aPaddock.fArea);

                            for (iRipe = GrazType.UNRIPE; iRipe <= GrazType.RIPE; iRipe++)
                                FStock[iPosn].stepForageInputs[iForage].Seeds[iForage + 1, iRipe].Biomass =
                                  StdMath.DIM(FStock[iPosn].initForageInputs[iForage].Seeds[iForage + 1, iRipe].Biomass,
                                       aForage.RemovalKG.Seed[1, iRipe] / aPaddock.fArea);
                        } //_ loop over forages within paddock _
                    }
                }
            } //_ if aPaddock.fArea > 0.0 _
        }
        private void computeNutrition(int iPosn, ref double fRDP)
        {
            At(iPosn).Nutrition();
            FStock[iPosn].fRDPFactor[0] = At(iPosn).RDP_IntakeFactor();
            fRDP = Math.Min(fRDP, FStock[iPosn].fRDPFactor[0]);
            if (At(iPosn).Young != null)
            {
                At(iPosn).Young.Nutrition();
                FStock[iPosn].fRDPFactor[1] = At(iPosn).Young.RDP_IntakeFactor();
                fRDP = Math.Min(fRDP, FStock[iPosn].fRDPFactor[1]);
            }
        }

        private void completeGrowth(int iPosn)
        {
            At(iPosn).completeGrowth(FStock[iPosn].fRDPFactor[0]);
            if (At(iPosn).Young != null)
                At(iPosn).Young.completeGrowth(FStock[iPosn].fRDPFactor[1]);

        }

        private double getPaddockRank(TPaddockInfo aPaddock,
                                      TAnimalGroup aGroup)
        {
            double Result;
            GrazType.TGrazingInputs forageInputs;
            GrazType.TGrazingInputs paddockInputs;
            double[] fHerbageRI = new double[GrazType.DigClassNo + 1];
            double[,] fSeedRI = new double[GrazType.MaxPlantSpp + 1, GrazType.RIPE + 1];
            double fDummy = 0.0;
            int Jdx;
            int iClass;

            aGroup.PaddSteep = aPaddock.Steepness;
            aGroup.WaterLogging = aPaddock.fWaterlog;
            aGroup.RationFed.Assign(aPaddock.SuppInPadd);
            aGroup.RationFed.TotalAmount = 0.0;                                        // No supplementary feed here            

            paddockInputs = new GrazType.TGrazingInputs();
            for (Jdx = 0; Jdx <= aPaddock.Forages.Count() - 1; Jdx++)
            {
                forageInputs = aPaddock.Forages.byIndex(Jdx).availForage(aGroup.Genotype.GrazeC[17],
                                                                           aGroup.Genotype.GrazeC[18],
                                                                           aGroup.Genotype.GrazeC[19]);
                GrazType.addGrazingInputs(Jdx + 1, forageInputs, ref paddockInputs);
            }
            aGroup.Herbage = paddockInputs;

            aGroup.CalculateRelIntake(aGroup, 1.0, false, 1.0, ref fHerbageRI, ref fSeedRI, ref fDummy);

            Result = 0.0;
            for (iClass = 1; iClass <= GrazType.DigClassNo; iClass++)                                             // Function result is DMDI/pot. intake   
                Result = Result + fHerbageRI[iClass] * GrazType.ClassDig[iClass];
            return Result;
        }
        //management events described by the livestock dialog

        /// <summary>
        /// If the stock are to be managed by this component then there are tasks to
        /// do such as ageing animals and re-tagging them if required.
        /// </summary>
        /// <param name="currentDay"></param>
        /// <param name="curEnt"></param>
        protected void manageDailyTasks(int currentDay, TEnterpriseInfo curEnt)
        {
            int g;
            int mob;

            if (curEnt.TagUpdateDay == currentDay)      //if this is a re-tagging day
            {
                //only work on the groups in this enterprise
                g = 1;
                while (g <= Count())                       //for each group
                {
                    if (curEnt.ContainsTag(getTag(g)))        //if this group belongs to this ent
                    {
                        //modify the tag value for this age group
                        for (mob = 0; mob <= MOBS - 1; mob++)
                        {
                            if (ENTMOBS[(int)curEnt.EntTypeFromName(curEnt.EntClass), mob].MobName != "")  //for each mob in this enterprise type
                                TagGroup(curEnt, g, mob);        //tag the group
                        }
                    }
                    g++;
                }
            }
        }

        /// <summary>
        /// Execute on any of the shearing dates
        /// Shears all on a single date OR each specified tagged group on it's day of year.
        /// </summary>
        /// <param name="currentDay"></param>
        /// <param name="curEnt"></param>
        protected void manageShearing(int currentDay, TEnterpriseInfo curEnt)
        {
            int idx, g;
            TShearByTag shearInfo = new TShearByTag();

            if (curEnt.ShearingTagCount == 0)
            {
                if (curEnt.ShearingDate == currentDay)          //if shear all
                {
                    //only shear the groups in this enterprise
                    g = 1;
                    while (g <= Count())                       //for each group
                    {
                        if (curEnt.ContainsTag(getTag(g)))       //if this group belongs to this ent
                        {
                            Shear(g, true, true);                  //shear this group
                        }
                        g++;
                    }
                }
            }
            else //shear each group on it's specified day
            {
                for (idx = 1; idx <= curEnt.ShearingTagCount; idx++)
                {
                    curEnt.getShearingTag(idx, ref shearInfo);
                    if (shearInfo.doy == currentDay)
                    {
                        if (curEnt.ContainsTag(shearInfo.tagNo))         //if this tag belongs to this ent
                        {
                            g = 1;
                            while (g <= Count())                       //for each group
                            {
                                if (getTag(g) == shearInfo.tagNo)         //if this group matches the tag
                                {
                                    Shear(g, true, true);                  //shear this group
                                }
                                g++;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Execute for CFA selling dates
        /// </summary>
        /// <param name="currentDay"></param>
        /// <param name="curEnt"></param>
        protected void manageCFA(int currentDay, TEnterpriseInfo curEnt)
        {
            int g;
            int number;
            int groups;

            if (curEnt.getFirstSaleDay(1) == currentDay)
            {
                //only sell from the groups in this enterprise
                g = 1;
                groups = Count();                     //store this count because it changes
                while (g <= groups)                       //for each group
                {
                    if (curEnt.ContainsTag(getTag(g)))        //if this group belongs to this ent
                    {
                        SplitAge(g, 365 * curEnt.getFirstSaleYears(1)); //split this group based on age
                    }
                    g++;
                }
                //loop through the groups again to pick up the new groups of aged animals
                g = 1;
                while (g <= Count())                      //for each group
                {
                    if (curEnt.ContainsTag(getTag(g)))        //if this group belongs to this ent
                    {
                        //remove animals that match the age criteria
                        if (At(g).AgeDays >= 365 * curEnt.getFirstSaleYears(1))
                        {
                            number = At(g).NoAnimals;
                            Sell(g, number); //sell the aged animals
                        }
                    }
                    g++;
                }
            }
        }

        /// <summary>
        /// Process the reproduction logic specified by the dialog.
        /// Mating, Castrating, Weaning.
        /// </summary>
        /// <param name="currentDay"></param>
        /// <param name="curEnt"></param>
        protected void manageReproduction(int currentDay, TEnterpriseInfo curEnt)
        {
            int g;
            int t;
            int tagNo;
            int groups;
            int BirthDOY;
            int gestation;
            bool found;

            //Mating day
            if (curEnt.MateDay == currentDay)
            {
                //only mate the groups in this enterprise
                g = 1;
                groups = Count();
                while (g <= groups)                       //for each group
                {
                    for (t = 1; t <= curEnt.MateTagCount; t++)      //for each tag that needs to be mated
                    {
                        tagNo = curEnt.getMateTag(t);
                        if ((tagNo == getTag(g)) && (curEnt.ContainsTag(getTag(g))))  //if mate this group and this group belongs to this ent
                        {
                            if (At(g).AgeDays >= (365 * curEnt.MateYears))
                            {
                                Join(g, curEnt.MateWith, 42);
                                setTag(g, curEnt.JoinedTag);        //retag the ewes that are mated into a ewe tag group
                            }
                        }
                    }
                    g++;
                }
            }

            //Castrate day
            if (curEnt.Castrate)
            {
                if (curEnt.IsCattle)
                    gestation = TEnterpriseInfo.COWGESTATION;
                else
                    gestation = TEnterpriseInfo.EWEGESTATION;
                BirthDOY = StdDate.DateShift(curEnt.MateDay, gestation, 0, 0);

                if (StdDate.DateShift(BirthDOY, 30, 0, 0) == currentDay)     //if 30 days after birth (??)
                {
                    g = 1;
                    groups = Count();
                    while (g <= groups)                       //for each group
                    {
                        if (curEnt.ContainsTag(getTag(g)))        //if this group belongs to this ent
                        {
                            Castrate(g, At(g).NoAnimals);   //castrate all male young in the group
                        }
                        g++;
                    }
                }
            }

            //Weaning day
            if (curEnt.WeanDay == currentDay)
            {
                g = 1;
                groups = Count();                     //store the group count because it changes
                while (g <= groups)                       //for each group
                {
                    if (curEnt.ContainsTag(getTag(g)))        //if this group belongs to this ent
                    {
                        Wean(g, At(g).NoAnimals, true, true);   //wean all young in the group
                        if (curEnt.IsCattle)
                            DryOff(g, At(g).NoAnimals);           //## may be possible to include option in user interface
                        //retag the mothers into dry ewes tag group
                        setTag(g, curEnt.DryTag);
                    }
                    g++;
                }
                //go through all the new groups and determine the new weaner tag for them
                for (g = groups + 1; g <= Count(); g++)
                {
                    found = false;
                    t = 1;
                    while (!found && (t <= curEnt.MateTagCount))
                    {
                        if (getTag(g) == curEnt.getMateTag(t))
                            found = true;                    //this tag belongs to a mated group
                        t++;
                    }
                    if (found)
                    {
                        //retag the weaners
                        if (At(g).MaleNo > 0)
                        {
                            setTag(g, curEnt.WeanerMTag);
                        }
                        //the new group will be retagged M/F
                        if (At(g).FemaleNo > 0)
                        {
                            setTag(g, curEnt.WeanerFTag);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Manage the initialisation of the animal groups for the enterprise.
        /// Called at the end of evtINITSTEP
        /// </summary>
        /// <param name="currentDay"></param>
        /// <param name="curEnt"></param>
        /// <param name="latitude"></param>
        protected void manageInitialiseStock(int currentDay, TEnterpriseInfo curEnt, double latitude)
        {
            double area;
            int numToBuy;
            TCohortsInfo CohortsInfo = new TCohortsInfo();
            int birthDay;
            int replaceAge;
            int ageOffset;
            int yngYrs = 0, oldYrs = 0;
            int i;
            TAnimalParamSet genoprms;
            int iBuyYears;
            int dtBuy;
            List<int> newGroups;
            int groupIdx;
            TEnterpriseInfo.TStockEnterprise stockEnt;
            double survivalRate;

            area = calcStockArea(curEnt);
            if (area > 0)
            {
                newGroups = new List<int>();
                numToBuy = Convert.ToInt32(Math.Truncate(curEnt.StockRateFemale * area));
                stockEnt = curEnt.EntTypeFromName(curEnt.EntClass);

                //calc the birth date based on age at replacement
                replaceAge = Convert.ToInt32(Math.Truncate(MONTH2DAY * curEnt.ReplaceAge + 0.5));
                ageOffset = (replaceAge + DaysFromDOY(curEnt.ReplacementDay, currentDay)) % 366;

                //get age range - from calc'd birth to CFA age
                GetAgeRange(curEnt.ReplacementDay, replaceAge, curEnt.getFirstSaleYears(1), curEnt.getFirstSaleDay(1), currentDay, ref yngYrs, ref oldYrs);
                genoprms = getGenotype(curEnt.BaseGenoType);
                if (curEnt.getPurchase(1))
                {
                    iBuyYears = curEnt.ReplaceAge / 12;
                    dtBuy = StdDate.DateVal(StdDate.DayOf(curEnt.ReplacementDay), StdDate.MonthOf(curEnt.ReplacementDay), 1900 + (iBuyYears + 1));
                    birthDay = StdDate.DateShift(dtBuy, 0, -curEnt.ReplaceAge, 0) & 0xFFFF;
                }
                else
                    birthDay = 1 + (curEnt.MateDay + 12 + genoprms.Gestation) % 365;

                survivalRate = Math.Min(1.0 - genoprms.AnnualDeaths(false), 0.999999);

                CohortsInfo.sGenotype = curEnt.BaseGenoType;
                CohortsInfo.iNumber = Convert.ToInt32(Math.Truncate(numToBuy * Math.Pow(survivalRate, ((currentDay - curEnt.ReplacementDay + 365) % 365) / 365)));
                CohortsInfo.iMinYears = yngYrs;
                CohortsInfo.iMaxYears = oldYrs;
                CohortsInfo.iAgeOffsetDays = StdDate.Interval(birthDay, currentDay);
                CohortsInfo.fMeanLiveWt = curEnt.ReplaceWeight;
                CohortsInfo.fCondScore = curEnt.ReplaceCond;

                switch (stockEnt)
                {
                    case TEnterpriseInfo.TStockEnterprise.entEweWether:

                        //setup cohorts for the females
                        CohortsInfo.ReproClass = GrazType.ReproType.Empty;   // or preg
                        CohortsInfo.fMeanGFW = genoprms.PotentialGFW * DaysFromDOY(curEnt.ShearingDate, currentDay) / 365.0;
                        CohortsInfo.iFleeceDays = DaysFromDOY365(curEnt.ShearingDate, currentDay);
                        if (curEnt.ManageReproduction)
                            CohortsInfo.sMatedTo = curEnt.MateWith;
                        else
                            CohortsInfo.sMatedTo = curEnt.BaseGenoType;
                        //        CohortsInfo.iDaysPreg =
                        /*          CohortsInfo.fFoetuses = eventData.member("foetuses").asDouble();
                                  CohortsInfo.iDaysLact = eventData.member("lactating").asInteger();
                                  CohortsInfo.fOffspring = eventData.member("offspring").asDouble();
                                  CohortsInfo.fOffspringWt = eventData.member("young_wt").asDouble();
                                  CohortsInfo.fOffspringCS = eventData.member("young_cond_score").asDouble();
                                  CohortsInfo.fLambGFW = eventData.member("young_fleece_wt").asDouble();
                          */
                        if (CohortsInfo.iNumber > 0)
                            AddCohorts(CohortsInfo, 1 + DaysFromDOY365(1, currentDay), latitude, null);

                        //tag the groups
                        for (i = 0; i <= newGroups.Count - 1; i++)
                        {
                            groupIdx = newGroups[i];
                            TagGroup(curEnt, groupIdx, 1);          //tag the group
                        }
                        DraftToOpenPaddocks(curEnt, area);        //move the groups to paddocks

                        //setup wether cohorts
                        break;
                    case TEnterpriseInfo.TStockEnterprise.entLamb:
                    case TEnterpriseInfo.TStockEnterprise.entWether:
                    case TEnterpriseInfo.TStockEnterprise.entSteer:
                        CohortsInfo.ReproClass = GrazType.ReproType.Castrated;
                        //birthday offset from the current date
                        CohortsInfo.fMeanGFW = 0;
                        CohortsInfo.iFleeceDays = 0;
                        CohortsInfo.sMatedTo = "";
                        CohortsInfo.iDaysPreg = 0;
                        CohortsInfo.fFoetuses = 0;
                        CohortsInfo.iDaysLact = 0;
                        CohortsInfo.fOffspring = 0;
                        CohortsInfo.fOffspringWt = 0;
                        CohortsInfo.fOffspringCS = 0;
                        CohortsInfo.fLambGFW = 0;
                        if ((stockEnt == TEnterpriseInfo.TStockEnterprise.entWether) || (stockEnt == TEnterpriseInfo.TStockEnterprise.entLamb))
                        {
                            CohortsInfo.fMeanGFW = genoprms.PotentialGFW * DaysFromDOY(curEnt.ShearingDate, currentDay) / 365.0;
                        }
                        if (CohortsInfo.iNumber > 0)
                            AddCohorts(CohortsInfo, currentDay, latitude, newGroups);
                        //tag the groups
                        for (i = 0; i <= newGroups.Count - 1; i++)
                        {
                            groupIdx = newGroups[i];
                            TagGroup(curEnt, groupIdx, 1);          //tag the group
                        }
                        DraftToOpenPaddocks(curEnt, area);        //move the groups to paddocks
                        break;
                    case TEnterpriseInfo.TStockEnterprise.entBeefCow:
                        break;
                    default:
                        break;
                }
                newGroups = null;
            }
        }

        /// <summary>
        /// Manage the replacement of adults by purchasing or ageing the existing stock.
        /// </summary>
        /// <param name="currentDay"></param>
        /// <param name="curEnt"></param>
        protected void manageReplacement(int currentDay, TEnterpriseInfo curEnt)
        {
            double area;
            int numToBuy;
            int totalStock;
            int g, groups;
            int groupIdx;
            TPurchaseInfo AnimalInfo = new TPurchaseInfo();
            int yrs;
            TAnimalParamSet genoprms;
            TEnterpriseInfo.TStockEnterprise stockEnt;

            if (curEnt.ReplacementDay == currentDay)
            {
                area = calcStockArea(curEnt);
                if (area > 0)
                {
                    totalStock = 0;
                    g = 1;
                    groups = Count();
                    while (g <= groups)                       //for each group
                    {
                        if (curEnt.ContainsTag(getTag(g)))        //if this group belongs to this ent
                            totalStock = totalStock + At(g).NoAnimals;
                        g++;
                    }
                    numToBuy = Convert.ToInt32(Math.Truncate(curEnt.StockRateFemale * area) - totalStock);  //calc how many to purchase to maintain stocking rate
                    if (!curEnt.getPurchase(1))       //if self replacing
                    {
                        // tag enough young ewes as replacements
                        // sell excess young ewes
                    }
                    else
                    {
                        stockEnt = curEnt.EntTypeFromName(curEnt.EntClass);
                        genoprms = getGenotype(curEnt.BaseGenoType);
                        switch (stockEnt)
                        {
                            case TEnterpriseInfo.TStockEnterprise.entLamb:
                            case TEnterpriseInfo.TStockEnterprise.entWether:
                            case TEnterpriseInfo.TStockEnterprise.entSteer:
                                AnimalInfo.sGenotype = curEnt.BaseGenoType;
                                AnimalInfo.Number = numToBuy;
                                AnimalInfo.Repro = GrazType.ReproType.Castrated;
                                AnimalInfo.AgeDays = Convert.ToInt32(Math.Truncate(MONTH2DAY * curEnt.ReplaceAge + 0.5));
                                AnimalInfo.LiveWt = curEnt.ReplaceWeight;
                                AnimalInfo.GFW = 0;
                                AnimalInfo.fCondScore = TAnimalParamSet.Condition2CondScore(curEnt.ReplaceCond);
                                AnimalInfo.sMatedTo = "";
                                AnimalInfo.Preg = 0;
                                AnimalInfo.Lact = 0;
                                AnimalInfo.NYoung = 0;
                                AnimalInfo.NYoung = 0;
                                AnimalInfo.YoungWt = 0;
                                AnimalInfo.YoungGFW = 0;
                                if ((stockEnt == TEnterpriseInfo.TStockEnterprise.entWether) || (stockEnt == TEnterpriseInfo.TStockEnterprise.entLamb))
                                {
                                    AnimalInfo.GFW = genoprms.PotentialGFW * DaysFromDOY(curEnt.ShearingDate, currentDay) / 365.0;
                                }
                                if (AnimalInfo.Number > 0)
                                {
                                    groupIdx = Buy(AnimalInfo);
                                    yrs = AnimalInfo.AgeDays / 365;
                                    TagGroup(curEnt, groupIdx, 1);        //tag the group

                                    // {TODO: before drafting, a request of forages should be done}

                                    DraftToOpenPaddocks(curEnt, area);
                                    /*
                                    //find the first paddock to be stocked for this enterprise
                                    p = 0;
                                    while (p <= paddocks.Count - 1) do
                                    begin
                                      if curEnt.StockedPaddock[p+1]    //if paddock to be stocked
                                      begin
                                        InPadd[groupIdx] = paddocks.byIndex(p).sName;  //move into correct paddock
                                        p = paddocks.Count; //terminate loop
                                      end;
                                      inc(p);
                                    end;
                                    //if none found default to the first paddock
                                    if p = paddocks.Count
                                      InPadd[groupIdx] = paddocks.byIndex(0).sName;
                                      */
                                }
                                break;
                        }
                    }

                }
            }
        }

        /// <summary>
        /// Selling of young stock
        /// </summary>
        /// <param name="currentDay"></param>
        /// <param name="curEnt"></param>
        protected void manageSelling(int currentDay, TEnterpriseInfo curEnt)
        {
            int g;
            int groups;
            int number;
            bool saleDatesInRange;

            if (curEnt.getYoungSaleWeight(1) <= 0.0)  //if sell on a fixed date
            {
                if (curEnt.getYoungSaleFirstDay(1) == currentDay)
                {
                    switch (curEnt.EntTypeFromName(curEnt.EntClass))
                    {
                        case TEnterpriseInfo.TStockEnterprise.entEweWether:
                            //self replacing
                            //not self replacing
                            break;
                        case TEnterpriseInfo.TStockEnterprise.entWether:
                        case TEnterpriseInfo.TStockEnterprise.entSteer:
                            g = 1;
                            groups = Count();                     //store this count
                            while (g <= groups)                       //for each group
                            {
                                if (curEnt.ContainsTag(getTag(g)))        //if this group belongs to this ent
                                {
                                    //if this group is old enough
                                    if (At(g).AgeDays >= (365.25 * curEnt.getYoungSaleFirstYears(1)))
                                    {
                                        //SplitAge(g, 365 * curEnt.YoungSaleFirstYears[1]); //split this group based on age
                                        number = At(g).NoAnimals;
                                        Sell(g, number); //sell the animals
                                    }
                                }
                                g++;
                            }  //next group
                            break;
                    }
                }
            }
            else
            {
                if (curEnt.getYoungSaleWtGain(1) > TEnterpriseInfo.INVALID_WTGAIN)
                {
                    //sell by weight gain
                    //for each young animal (?)
                    //calc d_wt
                    //
                    //calc ave weight change
                    //At[g].WeightChange
                    //for each animal group
                    //if animal group is young
                    //sell group
                    //endif
                    //next animal group
                }
                else
                {
                    //sell by weight within a period
                    switch (curEnt.EntTypeFromName(curEnt.EntClass))
                    {
                        case TEnterpriseInfo.TStockEnterprise.entEweWether:
                            //self replacing
                            //not self replacing
                            break;
                        case TEnterpriseInfo.TStockEnterprise.entWether:
                        case TEnterpriseInfo.TStockEnterprise.entSteer:

                            //check if today is in the selling date range
                            if ((curEnt.getYoungSaleFirstDay(1) <= curEnt.getYoungSaleLastDay(1)))
                                saleDatesInRange = ((curEnt.getYoungSaleFirstDay(1) <= currentDay) && (currentDay <= curEnt.getYoungSaleLastDay(1)));
                            else
                                saleDatesInRange = ((curEnt.getYoungSaleFirstDay(1) <= currentDay) || (currentDay <= curEnt.getYoungSaleLastDay(1)));

                            if (saleDatesInRange)
                            {
                                g = 1;
                                groups = Count();
                                while (g <= groups)                      //for each group
                                {
                                    if (curEnt.ContainsTag(getTag(g)))        //if this group belongs to this ent
                                    {
                                        //if this group is old enough
                                        if (At(g).AgeDays >= (365.25 * curEnt.getYoungSaleFirstYears(1)))
                                        {
                                            //check for sale condition
                                            if ((At(g).LiveWeight >= curEnt.getYoungSaleWeight(1)) || (currentDay == curEnt.getYoungSaleLastDay(1)))
                                            {
                                                number = At(g).NoAnimals;
                                                Sell(g, number); //sell the animals
                                            }
                                        }
                                    }
                                    g++;
                                }
                            }  //in date range
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// There can be a number of grazing periods. Each of these can include the
        /// movement of any number of tag groups to any paddocks. There are two types
        /// of grazing period, Fixed and Flexible.
        /// </summary>
        /// <param name="currentDate"></param>
        /// <param name="currentDay"></param>
        /// <param name="curEnt"></param>
        protected void manageGrazing(int currentDate, int currentDay, TEnterpriseInfo curEnt)
        {
            int p;
            int iPaddock, iTag;
            int tagno;
            int paddockIter, tagIter;
            int stockedIdx;
            List<string> exclPaddocks;
            bool found;
            int index;
            //int paddchosen;
            //TPaddockInfo ThePadd;

            for (p = 1; p <= GrazingPeriods.Count(); p++)   //for each grazing period
            {
                //if this period applies - within dates or within wrapped dates
                if (TodayIsInPeriod(currentDay, GrazingPeriods.getStartDay(p), GrazingPeriods.getFinishDay(p)))
                {
                    if ((GrazingPeriods.getPeriodType(p).ToLower()) == TEnterpriseInfo.PERIOD_TEXT[TEnterpriseInfo.FIXEDPERIOD].ToLower()) //if fixed period
                    {
                        //move the tag groups to their paddocks
                        //(they may already be there, although it is possible they may not be due to starting part way through the period)
                        for (paddockIter = 1; paddockIter <= GrazingPeriods.getFixedPaddCount(p); paddockIter++)  //for each paddock in this grazing period
                        {
                            iPaddock = GrazingPeriods.getFixedPadd(p, paddockIter);                 //test this paddock index
                            for (tagIter = 1; tagIter <= GrazingPeriods.getFixedPaddTagCount(p, paddockIter); tagIter++)   //for each tag group that is planned for this paddock
                            {
                                iTag = GrazingPeriods.getFixedPaddTag(p, paddockIter, tagIter);
                                if (curEnt.ContainsTag(iTag))
                                {
                                    stockedIdx = PaddockIndexStockedByTagNo(iTag);
                                    if (iPaddock != stockedIdx)
                                        MoveTagToPaddock(iTag, iPaddock);
                                }
                            }
                        }
                    }
                    else if (GrazingPeriods.getPeriodType(p).ToLower() == TEnterpriseInfo.PERIOD_TEXT[TEnterpriseInfo.FLEXIBLEPERIOD].ToLower())     //else if flexible
                    {
                        // X day intervals from the start of the period
                        if ((GrazingPeriods.getMoveCheck(p) > 0) && (StdDate.Interval(GrazingPeriods.getStartDay(p), currentDay) % GrazingPeriods.getMoveCheck(p) == 0)) //is this the check day or day one?
                        {
                            if (GrazingPeriods.getCriteria(p) == CRITERIA[DRAFT_MOVE])   //else if drafting for this period then
                            {
                                //get the list of excluded paddocks
                                exclPaddocks = new List<string>();
                                //for each tag in the GrazingPeriod
                                for (tagIter = 1; tagIter <= GrazingPeriods.getTagCount(p); tagIter++)       //for each tag
                                {
                                    for (index = 1; index <= Paddocks.Count() - 1; index++)
                                    {
                                        found = false;
                                        paddockIter = 1;
                                        while (paddockIter <= GrazingPeriods.getTagPaddocks(p, tagIter)) //for each paddock in the GrazingPeriod
                                        {
                                            iPaddock = GrazingPeriods.getPaddock(p, tagIter, paddockIter);
                                            if (iPaddock == index)
                                                found = true;
                                            paddockIter++;
                                        }
                                        if (!found)
                                            exclPaddocks.Add(Paddocks.byIndex(index).sName); //add to the exclude list
                                    }
                                    tagno = GrazingPeriods.getTag(p, tagIter);
                                    Draft(tagno, exclPaddocks); //now do the draft only for this tagno
                                } //next tag
                            }
                        }
                    }
                }
            } //next period
        }

        /// <summary>
        /// Calculate the area that is permitted to be stocked for the given enterprise.
        /// </summary>
        /// <param name="curEnt"></param>
        /// <returns></returns>
        protected double calcStockArea(TEnterpriseInfo curEnt)
        {
            int Idx;
            TPaddockInfo PaddInfo;

            double result = 0;
            for (Idx = 0; Idx <= Paddocks.Count() - 1; Idx++)
            {
                PaddInfo = Paddocks.byIndex(Idx);
                if ((Idx < curEnt.StockedPaddocks) && (PaddInfo.iPaddID >= 0) && (curEnt.getStockedPad(Idx)))
                    result = result + PaddInfo.fArea;
            }
            //if no area from chosen paddocks then assume it was given as a composite value
            if (result == 0)
                result = curEnt.GrazedArea;
            return result;
        }

        /// <summary>
        ///   For a given day of year, obtains the ages (in years, rounded down) of   
        ///   the youngest and oldest animals in a flock/herd from the policy for     
        ///   additions to and sales from it.                                         
        /// </summary>
        /// <param name="EnterDOY">Day of year for entry to the flock/herd</param>
        /// <param name="iEnterDays">Age in days at entry</param>
        /// <param name="sale_yrs"></param>
        /// <param name="sale_day"></param>
        /// <param name="Today"></param>
        /// <param name="iYoungYrs"></param>
        /// <param name="iOldYrs"></param>
        protected void GetAgeRange(int EnterDOY, int iEnterDays, int sale_yrs,
                              int sale_day, int Today, ref int iYoungYrs, ref int iOldYrs)
        {
            int iAgeAtSale;                                                 // Age of animals at sale (in days)         
            int iTimeSinceEntry;                                            // Time since last entry of animals (days)  
            int iTimeSinceSale;                                             // Time since last sale                     

            iAgeAtSale = 366 * sale_yrs
                          + (iEnterDays + DaysFromDOY(EnterDOY, sale_day)) % 366;

            iTimeSinceEntry = DaysFromDOY(EnterDOY, Today);                 // If Today is the same day-of-year as      
            if (iTimeSinceEntry == 0)                                       //   EnterDOY, then the entry hasn't        
                iTimeSinceEntry = 366;                                      //   happened yet                           

            iTimeSinceSale = DaysFromDOY(sale_day, Today);                  // Ditto for the sale day-of-year           
            if (iTimeSinceSale == 0)
                iTimeSinceSale = 366;

            iYoungYrs = (iEnterDays + iTimeSinceEntry) / 366;
            iOldYrs = (iAgeAtSale + iTimeSinceSale) / 366 - 1;              // Oldest animals left were AgeAtSale-366   
            //   days old on the last Sales.DOY         
        }

        /// <summary>
        /// Apply the tag to the group based on the age group and mob specified for this enterprise.
        /// </summary>
        /// <param name="curEnt"></param>
        /// <param name="groupIdx"></param>
        /// <param name="mob"></param>
        /// <returns></returns>
        public int TagGroup(TEnterpriseInfo curEnt, int groupIdx, int mob)
        {
            int age;
            bool tagChanged;
            //TODO: revise this code. Mobs need to be able to merge by assigning the same tag
            tagChanged = false;
            age = AGEGRPS - 1;
            while (!tagChanged && (age >= 0))  //for each tagged age group for this enterprise
            {
                //if the new group is in this range
                if (At(groupIdx).AgeDays >= (ENTAGEGRPS[(int)curEnt.EntTypeFromName(curEnt.EntClass), age].age * 365.25))
                {
                    if (curEnt.getTag(mob, age + 1) > 0)
                    {
                        setTag(groupIdx, curEnt.getTag(mob, age + 1));  //tag it
                        setPriority(groupIdx, AGEGRPS - age);      //set priority (for drafting)
                        tagChanged = true;
                    }
                }
                age--;
            }
            return getTag(groupIdx);
        }

        /// <summary>
        /// Draft the animals to the paddocks selected in the initialisation OR
        /// draft them to any paddock.
        /// </summary>
        /// <param name="curEnt"></param>
        /// <param name="area"></param>
        protected void DraftToOpenPaddocks(TEnterpriseInfo curEnt, double area)
        {
            int p;
            List<string> exclPaddocks;

            exclPaddocks = new List<string>();
            for (p = 1; p <= Paddocks.Count() - 1; p++)
            {
                if (!curEnt.getStockedPad(p))  //if paddock not stocked then
                    exclPaddocks.Add(Paddocks.byIndex(p).sName);
            }
            //if no paddocks selected to be stocked then just draft anywhere
            if ((area > 0) && ((Paddocks.Count() - 1) == exclPaddocks.Count()))
                exclPaddocks.Clear();
            Draft(exclPaddocks);                   //moves animals in stocked paddocks by animal priority
        }

        /// <summary>
        /// Find the index of the paddock that this tag group is currently grazing
        /// </summary>
        /// <param name="tagno"></param>
        /// <returns></returns>
        protected int PaddockIndexStockedByTagNo(int tagno)
        {

            int i;
            int iPosn;
            bool found;

            int result = -1;
            found = false;
            for (iPosn = 1; iPosn <= Count(); iPosn++)
            {
                if (FStock[iPosn].iTag == tagno)
                {
                    i = 1;
                    while (!found && (i < Paddocks.Count()))
                    {
                        if (Paddocks.byIndex(i).sName == FStock[iPosn].PaddOccupied.sName)
                        {
                            result = i;
                            found = true;
                        }
                        i++;
                    }
                }
            }  //next animal index
            return result;
        }

        /// <summary>
        /// Move a tagged group of animals to a paddock by index.
        /// </summary>
        /// <param name="tagno"></param>
        /// <param name="paddockidx"></param>
        protected void MoveTagToPaddock(int tagno, int paddockidx)
        {
            int g, groups;

            g = 1;
            groups = Count();
            while (g <= groups)                                 //for each group
            {
                if (tagno == getTag(g))                            //if this group belongs to this ent
                {
                    setInPadd(g, Paddocks.byIndex(paddockidx).sName);  //move them to this paddock
                }
                g++;
            }
        }

        /// <summary>
        /// Check date to see if it is in this range - handles 1 Jan wrapping.
        /// </summary>
        /// <param name="currentDay"></param>
        /// <param name="periodstart"></param>
        /// <param name="periodfinish"></param>
        /// <returns></returns>
        public bool TodayIsInPeriod(int currentDay, int periodstart, int periodfinish)
        {
            bool result = false;
            if (((periodstart <= currentDay) && (periodfinish >= currentDay))
              || ((periodstart > periodfinish) && ((periodfinish >= currentDay) || (periodstart <= currentDay))))
                result = true;
            return result;
        }
        /// <summary>
        /// ptr to the hosts random number generator
        /// </summary>
        public TMyRandom RandFactory;                                                 
        /// <summary>
        /// start of the simulation
        /// </summary>
        public int StartRun;       

        /// <summary>
        /// Create a TStockList
        /// </summary>
        /// <param name="RandomFactory"></param>
        public TStockList(TMyRandom RandomFactory)
        {
            StartRun = 0;
            RandFactory = RandomFactory;                                               //store the ptr
            setParamFile("");                                                          // Creates a default FBaseParams         
            Array.Resize(ref FStock, 1);                                               // Set aside temporary storage           
            FPaddocks = new TPaddockList();
            FPaddocks.Add(-1, String.Empty);                                                     // The "null" paddock is added here      
            //  FForages  := TForageList.Create( TRUE );
            FForageProviders = new TForageProviders();
            FEnterprises = new TEnterpriseList();
            FGrazing = new TGrazingList();
        }
        /// <summary>
        /// Parameter file name
        /// </summary>
        public string sParamFile
        {
            get { return FParamFile; }
            set { setParamFile(value); }
        }
        /// <summary>
        /// Add more genotypes
        /// </summary>
        /// <param name="BreedInits"></param>
        public void addGenotypes(TSingleGenotypeInits[] BreedInits)
        {
            int Idx, Jdx;

            Idx = FGenotypes.Length;
            Array.Resize(ref FGenotypes, Idx + BreedInits.Length);
            for (Jdx = 0; Jdx <= BreedInits.Length - 1; Jdx++)
                FGenotypes[Idx + Jdx] = ParamsFromGenotypeInits(FBaseParams, BreedInits, Jdx);
        }
        /// <summary>
        /// Get the genotype count
        /// </summary>
        /// <returns></returns>
        public int iGenotypeCount()
        {
            return FGenotypes.Length;
        }
        /// <summary>
        /// Get the genotype at Idx
        /// </summary>
        /// <param name="Idx"></param>
        /// <returns></returns>
        public TAnimalParamSet getGenotype(int Idx)
        {
            return FGenotypes[Idx];
        }

        /// <summary>
        /// Locate a genotype in FGenotypes. If this fails, try searching for it in the  
        /// main parameter set and adding it to FGenotypes.                            
        /// </summary>
        /// <param name="sName"></param>
        /// <returns></returns>
        public TAnimalParamSet getGenotype(string sName)
        {
            int Idx;
            TAnimalParamSet srcParamSet;

            TAnimalParamSet Result = null;
            if ((sName == "") && (FGenotypes.Length >= 1))                           // Null string is a special case         
                Result = FGenotypes[0];
            else
            {
                Idx = 0;
                while ((Idx < FGenotypes.Length) && (sName.ToLower() != FGenotypes[Idx].sName.ToLower()))
                    Idx++;

                if (Idx < FGenotypes.Length)
                    Result = FGenotypes[Idx];
                else
                {
                    srcParamSet = FBaseParams.Match(sName);
                    if (srcParamSet != null)
                    {
                        Result = new TAnimalParamSet(null, srcParamSet);
                        Idx = FGenotypes.Length;
                        Array.Resize(ref FGenotypes, Idx + 1);
                        FGenotypes[Idx] = Result;
                    }
                }
            }

            if (Result == null)
                throw new Exception("Genotype name \"" + sName + "\" not recognised");

            return Result;
        }
        /// <summary>
        /// Add a group of animals to the list                                           
        /// Returns the group index of the group that was added. 0->n                    
        /// </summary>
        /// <param name="aGroup"></param>
        /// <param name="PaddInfo"></param>
        /// <param name="iTagVal"></param>
        /// <param name="iPriority"></param>
        /// <returns></returns>
        public int Add(TAnimalGroup aGroup, TPaddockInfo PaddInfo, int iTagVal, int iPriority)
        {
            int Idx;

            aGroup.Calc_IntakeLimit();

            Idx = FStock.Length;
            Array.Resize(ref FStock, Idx + 1);
            FStock[Idx] = new TStockContainer();
            FStock[Idx].Animals = aGroup.Copy();
            FStock[Idx].PaddOccupied = PaddInfo;
            FStock[Idx].iTag = iTagVal;
            FStock[Idx].iPriority = iPriority;

            setInitialStockInputs(Idx);
            return Idx;
        }

        /// <summary>
        /// Returns the group index of the group that was added. 0->n                    
        /// </summary>
        /// <param name="Inits"></param>
        /// <returns></returns>
        public int Add(TAnimalInits Inits)
        {
            TAnimalGroup NewGroup;
            TPaddockInfo Paddock;

            NewGroup = new TAnimalGroup(getGenotype(Inits.sGenotype),
                                             Inits.Sex,
                                             Inits.Number,
                                             Inits.AgeDays,
                                             Inits.Weight,
                                             Inits.Fleece_Wt,
                                             RandFactory);
            if (bIsGiven(Inits.MaxPrevWt))
                NewGroup.MaxPrevWeight = Inits.MaxPrevWt;
            if (bIsGiven(Inits.Fibre_Diam))
                NewGroup.FibreDiam = Inits.Fibre_Diam;

            if (Inits.sMatedTo != "")
                NewGroup.MatedTo = getGenotype(Inits.sMatedTo);
            if ((NewGroup.ReproState == GrazType.ReproType.Empty) && (Inits.Pregnant > 0))
            {
                NewGroup.Pregnancy = Inits.Pregnant;
                if (Inits.No_Foetuses > 0)
                    NewGroup.NoFoetuses = Inits.No_Foetuses;
            }
            if ((NewGroup.ReproState == GrazType.ReproType.Empty) || (NewGroup.ReproState == GrazType.ReproType.EarlyPreg) || (NewGroup.ReproState == GrazType.ReproType.LatePreg) && (Inits.Lactating > 0))
            {
                NewGroup.Lactation = Inits.Lactating;
                if (Inits.No_Suckling > 0)
                    NewGroup.NoOffspring = Inits.No_Suckling;
                else if ((NewGroup.Animal == GrazType.AnimalType.Cattle) && (Inits.No_Suckling == 0))
                {
                    NewGroup.Young = null;
                }
                if (bIsGiven(Inits.Birth_CS))
                    NewGroup.BirthCondition = TAnimalParamSet.CondScore2Condition(Inits.Birth_CS, TAnimalParamSet.TCond_System.csSYSTEM1_5);
            }

            if (NewGroup.Young != null)
            {
                if (bIsGiven(Inits.Young_Wt))
                    NewGroup.Young.LiveWeight = Inits.Young_Wt;
                if (bIsGiven(Inits.Young_GFW))
                    NewGroup.Young.FleeceCutWeight = Inits.Young_GFW;
            }

            Paddock = FPaddocks.byName(Inits.Paddock.ToLower());
            if (Paddock == null)
                Paddock = FPaddocks.byIndex(0);

            return Add(NewGroup, Paddock, Inits.Tag, Inits.Priority);
        }

        /// <summary>
        ///  * N.B. iPosn is 1-offset; FStock is effectively also a 1-offset array        
        /// </summary>
        /// <param name="iPosn">In all methods, iPosn is 1-offset</param>
        public void Delete(int iPosn)
        {
            int iCount;
            int Idx;

            iCount = Count();
            if ((iPosn >= 1) && (iPosn <= iCount))
            {
                FStock[iPosn].Animals = null;
                FStock[iPosn].initForageInputs = null;
                FStock[iPosn].stepForageInputs = null;

                for (Idx = iPosn + 1; Idx <= iCount; Idx++)
                    FStock[Idx - 1] = FStock[Idx];
                Array.Resize(ref FStock, iCount);                                               // Leave FStock[0] as temporary storage  
            }
        }
        /// <summary>
        /// Clear the list
        /// </summary>
        public void Clear()
        {
            while (Count() > 0)
                Delete(Count());
        }

        /// <summary>
        /// Remove empty groups                   
        /// </summary>
        public void Pack()
        {
            int Idx;

            for (Idx = 1; Idx <= Count(); Idx++)
            {
                if ((At(Idx) != null) && (At(Idx).NoAnimals == 0))
                {
                    setAt(Idx, null);
                }
            }

            for (Idx = Count(); Idx >= 1; Idx++)
                if (At(Idx) == null)
                    Delete(Idx);
        }

        /// <summary>
        /// Only groups 1 to Length()-1 are counted                                    
        /// </summary>
        /// <returns></returns>
        public int Count()
        {
            return FStock.Length - 1;
        }
        /// <summary>
        /// Get the animal group at the position
        /// </summary>
        /// <param name="Posn"></param>
        /// <returns></returns>
        public TAnimalGroup At(int Posn)
        {
            return getAt(Posn);
        }

        /// <summary>
        /// iPosn is 1-offset; so is FStock                                              
        /// </summary>
        /// <param name="iPosn"></param>
        /// <returns></returns>
        public int getTag(int iPosn)
        {
            if ((iPosn >= 1) && (iPosn <= Count()))
                return FStock[iPosn].iTag;
            else
                return 0;
        }
        /// <summary>
        /// Set the tag value
        /// </summary>
        /// <param name="iPosn"></param>
        /// <param name="iValue"></param>
        public void setTag(int iPosn, int iValue)
        {
            if ((iPosn >= 1) && (iPosn <= Count()))
                FStock[iPosn].iTag = iValue;
        }
        /// <summary>
        /// Get the highest tag number
        /// </summary>
        /// <returns></returns>
        public int iHighestTag()
        {
            int Idx;

            int Result = 0;
            for (Idx = 1; Idx <= Count(); Idx++)
                Result = Math.Max(Result, getTag(Idx));
            return Result;
        }
        /// <summary>
        /// Get the list of paddocks
        /// </summary>
        public TPaddockList Paddocks
        {
            get { return FPaddocks; }
        }
        /// <summary>
        /// Get the enterprise list
        /// </summary>
        public TEnterpriseList Enterprises
        {
            get { return FEnterprises; }
        }
        /// <summary>
        /// Get the grazing periods
        /// </summary>
        public TGrazingList GrazingPeriods
        {
            get { return FGrazing; }
        }
        /// <summary>
        /// Get all the forage providers
        /// </summary>
        public TForageProviders ForagesAll
        {
            get { return FForageProviders; }
        }

        // Inputs for model dynamics ................................................
        /// <summary>
        /// The animals weather
        /// </summary>
        public TAnimalWeather Weather { set { setWeather(value); } }
        /*property    WaterLogging[iPaddID:Integer] : Float          write SetWaterLog;
        procedure   passForageValues(provider: TForageProvider; aValue: TTypedValue);
    (*
        procedure   passGrazingInputs( ID            : Integer;
                                       const Grazing : TGrazingInputs;
                                       sUnit         : string );
    *)
        procedure   passHerbageData(   iForageID     : Integer;
                                       const Data    : TPopnHerbageData;
                                       sUnit         : string ); */
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sPaddName"></param>
        /// <param name="fSuppKG"></param>
        /// <param name="Supplement"></param>
        public void PlaceSuppInPadd(string sPaddName, double fSuppKG, TSupplement Supplement)
        {
            TPaddockInfo ThePadd;

            ThePadd = Paddocks.byName(sPaddName);
            if (ThePadd == null)
                throw new Exception("Stock: attempt to feed supplement into non-existent paddock");
            else
                ThePadd.FeedSupplement(fSuppKG, Supplement);
        }

        // Model execution routines ................................................
        /// <summary>
        /// Initiate the time step for the paddocks
        /// </summary>
        public void beginTimeStep()
        {
            Paddocks.beginTimeStep();
        }

        /// <summary>
        /// Advance the list by one time step.  All the input properties should be set first                                                                        
        /// </summary>
        public void Dynamics()
        {

            const double EPS = 1.0E-6;

            double TotPotIntake;
            TAnimalList NewGroups;
            TPaddockInfo ThePaddock;
            double dTime;
            double dDelta;
            double fRDP;
            int Padd, Idx, N;
            int iIter;
            bool bDone;

            for (Padd = 0; Padd <= Paddocks.Count() - 1; Padd++)
            {
                ThePaddock = Paddocks.byIndex(Padd);
                ThePaddock.computeTotals();
            }

            for (Idx = 1; Idx <= Count(); Idx++)
                setInitialStockInputs(Idx);

            N = Count();
            for (Idx = 1; Idx <= N; Idx++)                                                  // Aging,birth,deaths etc. Animal groups 
            {                                                                               //   may appear in the NewGroups list as 
                NewGroups = null;                                                           //   a result of processes such as lamb  
                At(Idx).Age(1, ref NewGroups);                                              //   deaths                              
                if (At(Idx).Young != null)                                                  // Ensure the new young have climate data}
                    At(Idx).Young.Weather = At(Idx).Weather;
                Add(NewGroups, getPaddInfo(Idx), getTag(Idx), getPriority(Idx));            // The new groups are added back onto    
                NewGroups = null;                                                           //   the main list                       
            }

            Merge();                                                                        // Merge any similar animal groups       

            for (Idx = 1; Idx <= Count(); Idx++)                                            // Now run the grazing and nutrition     
            {                                                                               //   models. This process is quite       
                storeInitialState(Idx);                                                     //   involved...                         
                computeIntakeLimit(At(Idx));
                At(Idx).Reset_Grazing();
            }

            for (Padd = 0; Padd <= Paddocks.Count() - 1; Padd++)                            // Compute the total potential intake    
            {                                                                               //    (used to distribute supplement     
                ThePaddock = Paddocks.byIndex(Padd);                                        //    between groups of animals)         
                TotPotIntake = 0.0;

                for (Idx = 1; Idx <= Count(); Idx++)
                    if (getPaddInfo(Idx) == ThePaddock)
                    {
                        TotPotIntake = TotPotIntake + At(Idx).NoAnimals * At(Idx).PotIntake;
                        if (At(Idx).Young != null)
                            TotPotIntake = TotPotIntake + At(Idx).Young.NoAnimals * At(Idx).Young.PotIntake;
                    }
                ThePaddock.fSummedPotIntake = TotPotIntake;
            }

            for (Padd = 0; Padd <= Paddocks.Count() - 1; Padd++)                            // We loop over paddocks and then over   
            {                                                                               //   animal groups within a paddock so   
                ThePaddock = Paddocks.byIndex(Padd);                                        //   that we can take account of herbage 
                //   removal & its effect on intake      
                iIter = 1;                                                                  // This loop handles RDP insufficiency   
                bDone = false;
                while (!bDone)
                {
                    dTime = 0.0;                                                            // Variable-length substeps for grazing  

                    while (dTime < 1.0 - EPS)
                    {
                        for (Idx = 1; Idx <= Count(); Idx++)
                            if (getPaddInfo(Idx) == ThePaddock)
                                computeStepAvailability(Idx);

                        dDelta = Math.Min(computeStepLength(ThePaddock), 1.0 - dTime);

                        for (Idx = 1; Idx <= Count(); Idx++)                                // Compute rate of grazing for this      
                            if (getPaddInfo(Idx) == ThePaddock)                             //   substep                             
                                computeGrazing(Idx, dTime, dDelta);

                        computeRemoval(ThePaddock, dDelta);

                        dTime = dTime + dDelta;
                    } //_ grazing sub-steps loop _

                    fRDP = 1.0;
                    for (Idx = 1; Idx <= Count(); Idx++)                                    // Nutrition submodel here...            
                        if (getPaddInfo(Idx) == ThePaddock)
                            computeNutrition(Idx, ref fRDP);

                    if (iIter == 2)                                                         // Maximum of 2 iterations in the RDP    
                        bDone = true;                                                       //   loop                                
                    else
                    {
                        bDone = (fRDP == 1.0);                                              // Is there an animal group in this      
                        if (!bDone)                                                         //   paddock with an RDP insufficiency?  
                        {
                            ThePaddock.zeroRemoval();
                            for (Idx = 1; Idx <= Count(); Idx++)                            // If so, we have to revert the state of 
                                if (getPaddInfo(Idx) == ThePaddock)                         //   the animal group ready for the      
                                    revertInitialState(Idx);                                //   second iteration.                   
                        }
                    }

                    iIter++;
                } //_ RDP loop _
            } //_ loop over paddocks _

            for (Idx = 1; Idx <= Count(); Idx++)
                completeGrowth(Idx);
        }

        // Outputs to other models .................................................
        /// <summary>
        /// Get the mass for the area
        /// </summary>
        /// <param name="paddID"></param>
        /// <param name="provider"></param>
        /// <param name="sUnit"></param>
        /// <returns></returns>
        public double returnMassPerArea(int paddID, TForageProvider provider, string sUnit)
        {
            double Result;
            TPaddockInfo ThePadd;
            double fMassKGHA;
            int Idx;

            if (provider != null)
                ThePadd = provider.OwningPaddock;
            else
                ThePadd = FPaddocks.byID(paddID);

            fMassKGHA = 0.0;
            if (ThePadd != null)
            {
                for (Idx = 1; Idx <= Count(); Idx++)
                    if (getPaddInfo(Idx) == ThePadd)
                    {
                        fMassKGHA = fMassKGHA + At(Idx).NoAnimals * At(Idx).LiveWeight;
                        if (At(Idx).Young != null)
                            fMassKGHA = fMassKGHA + At(Idx).Young.NoAnimals * At(Idx).Young.LiveWeight;
                    }
                fMassKGHA = fMassKGHA / ThePadd.fArea;
            }

            if (sUnit == "kg/ha")
                Result = fMassKGHA;
            else if (sUnit == "kg/m^2")
                Result = fMassKGHA * 0.0001;
            else if (sUnit == "dse/ha")
                Result = fMassKGHA * WEIGHT2DSE;
            else if (sUnit == "g/m^2")
                Result = fMassKGHA * 0.1;
            else
                throw new Exception("Stock: Unit (" + sUnit + ") not recognised");

            return Result;
        }
        //    function    returnRemoval(     iForageID : Integer; sUnit : string   ) : TGrazingOutputs;

        private double dWeightedMean(double dY1, double dY2, double dX1, double dX2)
        {
            if (dX1 + dX2 > 0.0)
                return (dX1 * dY1 + dX2 * dY2) / (dX1 + dX2);
            else
                return 0;
        }

        /// <summary>
        /// Used by returnExcretion()
        /// </summary>
        /// <param name="destExcretion"></param>
        /// <param name="srcExcretion"></param>
        private void addExcretions(ref TExcretionInfo destExcretion, TExcretionInfo srcExcretion)
        {
            if (srcExcretion.dDefaecations > 0.0)
            {
                destExcretion.dDefaecationVolume = dWeightedMean(destExcretion.dDefaecationVolume, srcExcretion.dDefaecationVolume,
                                                                         destExcretion.dDefaecations, srcExcretion.dDefaecations);
                destExcretion.dDefaecationArea = dWeightedMean(destExcretion.dDefaecationArea, srcExcretion.dDefaecationArea,
                                                                         destExcretion.dDefaecations, srcExcretion.dDefaecations);
                destExcretion.dDefaecationEccentricity = dWeightedMean(destExcretion.dDefaecationEccentricity, srcExcretion.dDefaecationEccentricity,
                                                                         destExcretion.dDefaecations, srcExcretion.dDefaecations);
                destExcretion.dFaecalNO3Propn = dWeightedMean(destExcretion.dFaecalNO3Propn, srcExcretion.dFaecalNO3Propn,
                                                                         destExcretion.InOrgFaeces.Nu[(int)GrazType.TOMElement.N], srcExcretion.InOrgFaeces.Nu[(int)GrazType.TOMElement.N]);
                destExcretion.dDefaecations = destExcretion.dDefaecations + srcExcretion.dDefaecations;

                destExcretion.OrgFaeces = AddDMPool(destExcretion.OrgFaeces, srcExcretion.OrgFaeces);
                destExcretion.InOrgFaeces = AddDMPool(destExcretion.InOrgFaeces, srcExcretion.InOrgFaeces);
            }

            if (srcExcretion.dUrinations > 0.0)
            {
                destExcretion.dUrinationVolume = dWeightedMean(destExcretion.dUrinationVolume, srcExcretion.dUrinationVolume,
                                                                       destExcretion.dUrinations, srcExcretion.dUrinations);
                destExcretion.dUrinationArea = dWeightedMean(destExcretion.dUrinationArea, srcExcretion.dUrinationArea,
                                                                       destExcretion.dUrinations, srcExcretion.dUrinations);
                destExcretion.dUrinationEccentricity = dWeightedMean(destExcretion.dUrinationEccentricity, srcExcretion.dUrinationEccentricity,
                                                                       destExcretion.dUrinations, srcExcretion.dUrinations);
                destExcretion.dUrinations = destExcretion.dUrinations + srcExcretion.dUrinations;

                destExcretion.Urine = AddDMPool(destExcretion.Urine, srcExcretion.Urine);
            }
        }

        /// <summary>
        /// Parameters:                                                               
        /// OrgFaeces    kg/ha  Excretion of organic matter in faeces                 
        /// InorgFaeces  kg/ha  Excretion of inorganic nutrients in faeces            
        /// Urine        kg/ha  Excretion of nutrients in urine                       
        ///                                                                           
        /// Note:  TAnimalGroup.OrgFaeces returns the OM faecal excretion in kg, and  
        ///        is the total of mothers and young where appropriate; similarly for   
        ///        TAnimalGroup.InorgFaeces and TAnimalGroup.Urine.                   
        ///        TAnimalGroup.FaecalAA and TAnimalGroup.UrineAAN return weighted    
        ///        averages over mothers and young where appropriate. As a result we  
        ///        don't need to concern ourselves with unweaned young in this        
        ///        particular calculation except when computing PatchFract.           
        /// </summary>
        /// <param name="iPaddID"></param>
        /// <param name="Excretion"></param>
        public void ReturnExcretion(int iPaddID, out TExcretionInfo Excretion)
        {
            TPaddockInfo ThePadd;
            double fArea;
            int Idx;

            ThePadd = FPaddocks.byID(iPaddID);

            if (ThePadd != null)
                fArea = ThePadd.fArea;
            else if (FPaddocks.Count() == 0)
                fArea = 1.0;
            else
            {
                fArea = 0.0;
                for (Idx = 0; Idx <= FPaddocks.Count() - 1; Idx++)
                    fArea = fArea + FPaddocks.byIndex(Idx).fArea;
            }

            Excretion = new TExcretionInfo();
            for (Idx = 1; Idx <= Count(); Idx++)
            {
                if ((ThePadd == null) || (getPaddInfo(Idx) == ThePadd))
                {
                    addExcretions(ref Excretion, At(Idx).Excretion);
                    if (At(Idx).Young != null)
                        addExcretions(ref Excretion, At(Idx).Young.Excretion);
                }
            }

            // Convert values in kg to kg/ha
            Excretion.OrgFaeces = MultiplyDMPool(Excretion.OrgFaeces, 1.0 / fArea);
            Excretion.InOrgFaeces = MultiplyDMPool(Excretion.InOrgFaeces, 1.0 / fArea);
            Excretion.Urine = MultiplyDMPool(Excretion.Urine, 1.0 / fArea);
        }

        /// <summary>
        /// Return the reproductive status of the group as a string.  These strings   
        /// are compatible with the ParseRepro routine.                               
        /// </summary>
        /// <param name="Idx"></param>
        /// <param name="UseYoung"></param>
        /// <returns></returns>
        public string SexString(int Idx, bool UseYoung)
        {
            string[,] MaleNames = { { "wether", "ram" }, { "steer", "bull" } };   //[AnimalType,Castrated..Male] of String =

            string Result;
            TAnimalGroup TheGroup;

            if (UseYoung)
                TheGroup = At(Idx).Young;
            else
                TheGroup = At(Idx);
            if (TheGroup == null)
                Result = "";
            else
            {
                if ((TheGroup.ReproState == GrazType.ReproType.Male) || (TheGroup.ReproState == GrazType.ReproType.Castrated))
                    Result = MaleNames[(int)TheGroup.Animal, (int)TheGroup.ReproState];
                else if (TheGroup.Animal == GrazType.AnimalType.Sheep)
                    Result = "ewe";
                else if (TheGroup.AgeDays < 2 * 365)
                    Result = "heifer";
                else
                    Result = "cow";
            }
            return Result;
        }

        /// <summary>
        /// GrowthCurve calculates MaxNormalWt (see below) for an animal with the   
        /// default birth weight.                                                   
        /// </summary>
        /// <param name="SRW"></param>
        /// <param name="BW"></param>
        /// <param name="AgeDays"></param>
        /// <param name="Params"></param>
        /// <returns></returns>
        private double MaxNormWtFunc(double SRW, double BW, int AgeDays, TAnimalParamSet Params)
        {
            double GrowthRate;

            GrowthRate = Params.GrowthC[1] / Math.Pow(SRW, Params.GrowthC[2]);
            return SRW - (SRW - BW) * Math.Exp(-GrowthRate * AgeDays);
        }
        /// <summary>
        /// Calculate the growth from the standard growth curve
        /// </summary>
        /// <param name="iAgeDays"></param>
        /// <param name="Repr"></param>
        /// <param name="Params"></param>
        /// <returns></returns>
        public double GrowthCurve(int iAgeDays, GrazType.ReproType Repr, TAnimalParamSet Params)
        {
            double SRW;

            SRW = Params.BreedSRW;
            if ((Repr == GrazType.ReproType.Male) || (Repr == GrazType.ReproType.Castrated))
                SRW = SRW * Params.SRWScalars[(int)Repr];
            return MaxNormWtFunc(SRW, Params.StdBirthWt(1), iAgeDays, Params);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="CohortsInfo"></param>
        /// <param name="mainGenotype"></param>
        /// <param name="AgeInfo"></param>
        /// <param name="fLatitude"></param>
        /// <param name="iMateDOY"></param>
        /// <param name="fCondition"></param>
        /// <param name="fChill"></param>
        /// <returns></returns>
        private double getReproRate(TCohortsInfo CohortsInfo, TAnimalParamSet mainGenotype, TAgeInfo[] AgeInfo, double fLatitude, int iMateDOY, double fCondition, double fChill)
        {
            double Result = 0.0;
            double[] fPregRate = new double[4];
            int iCohort;
            int N;

            for (iCohort = CohortsInfo.iMinYears; iCohort <= CohortsInfo.iMaxYears; iCohort++)
            {
                fPregRate = getOffspringRates(mainGenotype, fLatitude, iMateDOY,
                                                AgeInfo[iCohort].iAgeAtMating,
                                                AgeInfo[iCohort].fSizeAtMating,
                                                fCondition, fChill);
                for (N = 1; N <= 3; N++)
                    Result = Result + AgeInfo[iCohort].fPropn * fPregRate[N];
            }

            return Result;
        }

        internal class TAgeInfo
        {
            public double fPropn;
            public double[] fPropnPreg = new double[4];
            public double[] fPropnLact = new double[4];
            public int[,] iNumbers = new int[4, 4];
            public int iAgeDays;
            public double fNormalBaseWt;
            public double fBaseWeight;
            public double fFleeceWt;
            public int iAgeAtMating;
            public double fSizeAtMating;
        }

        // Management events .......................................................
        /// <summary>
        /// Add animal cohorts
        /// </summary>
        /// <param name="CohortsInfo"></param>
        /// <param name="iDOY"></param>
        /// <param name="fLatitude"></param>
        /// <param name="newGroups"></param>
        public void AddCohorts(TCohortsInfo CohortsInfo, int iDOY, double fLatitude, List<int> newGroups)
        {
            TAnimalParamSet mainGenotype;
            TAgeInfo[] AgeInfo;

            TAnimalInits AnimalInits;
            int iNoCohorts;
            double fSurvival;
            int iDaysSinceShearing;
            double fMeanNormalWt;
            double fMeanFleeceWt;
            double fBaseWtScalar;
            double fFleeceWtScalar;
            int iTotalAnimals;
            int iMateDOY;
            double fLowCondition;
            double fLowFoetuses;
            double fHighCondition;
            double fHighFoetuses;
            double fCondition;
            double fTrialFoetuses;
            double[] fPregRate = new double[4];     //TConceptionArray;
            int[] iShiftNumber = new int[4];
            bool bLactationDone;
            double fLowChill;
            double fLowOffspring;
            double fHighChill;
            double fHighOffspring;
            double fChillIndex;
            double fTrialOffspring;
            double[] fLactRate = new double[4];
            int iCohort;
            int iPreg;
            int iLact;
            int groupIndex;

            if (CohortsInfo.iNumber > 0)
            {
                mainGenotype = getGenotype(CohortsInfo.sGenotype);

                AgeInfo = new TAgeInfo[CohortsInfo.iMaxYears + 1];
                for (int i = 0; i < CohortsInfo.iMaxYears + 1; i++)
                    AgeInfo[i] = new TAgeInfo();
                iNoCohorts = CohortsInfo.iMaxYears - CohortsInfo.iMinYears + 1;
                fSurvival = 1.0 - mainGenotype.AnnualDeaths(false);

                if (mainGenotype.Animal == GrazType.AnimalType.Cattle)
                    iDaysSinceShearing = 0;
                else if (bIsGiven(CohortsInfo.fMeanGFW) && (CohortsInfo.iFleeceDays == 0))
                    iDaysSinceShearing = Convert.ToInt32(Math.Truncate(365.25 * CohortsInfo.fMeanGFW / mainGenotype.PotentialGFW));
                else
                    iDaysSinceShearing = CohortsInfo.iFleeceDays;

                for (iCohort = CohortsInfo.iMinYears; iCohort <= CohortsInfo.iMaxYears; iCohort++)
                {
                    //Proportion of all stock in this age cohort
                    if (fSurvival >= 1.0)
                        AgeInfo[iCohort].fPropn = 1.0 / iNoCohorts;
                    else
                        AgeInfo[iCohort].fPropn = (1.0 - fSurvival) * Math.Pow(fSurvival, iCohort - CohortsInfo.iMinYears)
                                                        / (1.0 - Math.Pow(fSurvival, iNoCohorts));
                    AgeInfo[iCohort].iAgeDays = Convert.ToInt32(Math.Truncate(365.25 * iCohort) + CohortsInfo.iAgeOffsetDays);

                    // Normal weight for age
                    AgeInfo[iCohort].fNormalBaseWt = GrowthCurve(AgeInfo[iCohort].iAgeDays, CohortsInfo.ReproClass, mainGenotype);

                    // Estimate a default fleece weight based on time since shearing
                    AgeInfo[iCohort].fFleeceWt = TAnimalParamSet.fDefaultFleece(mainGenotype,
                                                                  AgeInfo[iCohort].iAgeDays,
                                                                  CohortsInfo.ReproClass,
                                                                  iDaysSinceShearing);
                }

                // Re-scale the fleece-free and fleece weights
                fMeanNormalWt = 0.0;
                fMeanFleeceWt = 0.0;
                for (iCohort = CohortsInfo.iMinYears; iCohort <= CohortsInfo.iMaxYears; iCohort++)
                {
                    fMeanNormalWt = fMeanNormalWt + AgeInfo[iCohort].fPropn * AgeInfo[iCohort].fNormalBaseWt;
                    fMeanFleeceWt = fMeanFleeceWt + AgeInfo[iCohort].fPropn * AgeInfo[iCohort].fFleeceWt;
                }

                if ((CohortsInfo.fMeanGFW > 0.0) && (fMeanFleeceWt > 0.0))
                    fFleeceWtScalar = CohortsInfo.fMeanGFW / fMeanFleeceWt;
                else
                    fFleeceWtScalar = 1.0;

                if (!bIsGiven(CohortsInfo.fMeanGFW))
                    CohortsInfo.fMeanGFW = fMeanFleeceWt;
                if (bIsGiven(CohortsInfo.fMeanLiveWt))
                    fBaseWtScalar = (CohortsInfo.fMeanLiveWt - CohortsInfo.fMeanGFW) / fMeanNormalWt;
                else if (bIsGiven(CohortsInfo.fCondScore))
                    fBaseWtScalar = TAnimalParamSet.CondScore2Condition(CohortsInfo.fCondScore);
                else
                    fBaseWtScalar = 1.0;

                for (iCohort = CohortsInfo.iMinYears; iCohort <= CohortsInfo.iMaxYears; iCohort++)
                {
                    AgeInfo[iCohort].fBaseWeight = AgeInfo[iCohort].fNormalBaseWt * fBaseWtScalar;
                    AgeInfo[iCohort].fFleeceWt = AgeInfo[iCohort].fFleeceWt * fFleeceWtScalar;
                }

                // Numbers in each age cohort
                iTotalAnimals = 0;
                for (iCohort = CohortsInfo.iMinYears; iCohort <= CohortsInfo.iMaxYears; iCohort++)
                {
                    AgeInfo[iCohort].iNumbers[0, 0] = Convert.ToInt32(Math.Truncate(AgeInfo[iCohort].fPropn * CohortsInfo.iNumber));
                    iTotalAnimals += AgeInfo[iCohort].iNumbers[0, 0];
                }
                for (iCohort = CohortsInfo.iMinYears; iCohort <= CohortsInfo.iMaxYears; iCohort++)
                    if (iTotalAnimals < CohortsInfo.iNumber)
                    {
                        AgeInfo[iCohort].iNumbers[0, 0]++;
                        iTotalAnimals++;
                    }

                // Pregnancy and lactation
                if ((CohortsInfo.ReproClass == GrazType.ReproType.Empty) || (CohortsInfo.ReproClass == GrazType.ReproType.EarlyPreg) || (CohortsInfo.ReproClass == GrazType.ReproType.LatePreg))
                {
                    // Numbers with each number of foetuses
                    if ((CohortsInfo.iDaysPreg > 0) && (CohortsInfo.fFoetuses > 0.0))
                    {
                        iMateDOY = 1 + (iDOY - CohortsInfo.iDaysPreg + 364) % 365;
                        for (iCohort = CohortsInfo.iMinYears; iCohort <= CohortsInfo.iMaxYears; iCohort++)
                        {
                            AgeInfo[iCohort].iAgeAtMating = AgeInfo[iCohort].iAgeDays - CohortsInfo.iDaysPreg;
                            AgeInfo[iCohort].fSizeAtMating = GrowthCurve(AgeInfo[iCohort].iAgeAtMating,
                                                                           CohortsInfo.ReproClass,
                                                                           mainGenotype) / mainGenotype.fSexStdRefWt(CohortsInfo.ReproClass);
                        }

                        // binary search for the body condition at mating that yields the desired pregnancy rate
                        fLowCondition = 0.60;
                        fLowFoetuses = getReproRate(CohortsInfo, mainGenotype, AgeInfo, fLatitude, iMateDOY, fLowCondition, 0);
                        fHighCondition = 1.40;
                        fHighFoetuses = getReproRate(CohortsInfo, mainGenotype, AgeInfo, fLatitude, iMateDOY, fHighCondition, 0);

                        if (fLowFoetuses > CohortsInfo.fFoetuses)
                            fCondition = fLowCondition;
                        else if (fHighFoetuses < CohortsInfo.fFoetuses)
                            fCondition = fHighCondition;
                        else
                        {
                            do
                            {
                                fCondition = 0.5 * (fLowCondition + fHighCondition);
                                fTrialFoetuses = getReproRate(CohortsInfo, mainGenotype, AgeInfo, fLatitude, iMateDOY, fCondition, 0);

                                if (fTrialFoetuses < CohortsInfo.fFoetuses)
                                    fLowCondition = fCondition;
                                else
                                    fHighCondition = fCondition;
                            }
                            while (Math.Abs(fTrialFoetuses - CohortsInfo.fFoetuses) >= 1.0E-5); //until (Abs(fTrialFoetuses-CohortsInfo.fFoetuses) < 1.0E-5);
                        }

                        // Compute final pregnancy rates and numbers
                        for (iCohort = CohortsInfo.iMinYears; iCohort <= CohortsInfo.iMaxYears; iCohort++)
                        {
                            fPregRate = getOffspringRates(mainGenotype, fLatitude, iMateDOY,
                                                            AgeInfo[iCohort].iAgeAtMating,
                                                            AgeInfo[iCohort].fSizeAtMating,
                                                            fCondition);
                            for (iPreg = 1; iPreg <= 3; iPreg++)
                                iShiftNumber[iPreg] = Convert.ToInt32(Math.Round(fPregRate[iPreg] * AgeInfo[iCohort].iNumbers[0, 0]));
                            for (iPreg = 1; iPreg <= 3; iPreg++)
                            {
                                AgeInfo[iCohort].iNumbers[iPreg, 0] += iShiftNumber[iPreg];
                                AgeInfo[iCohort].iNumbers[0, 0] -= iShiftNumber[iPreg];
                            }
                        }
                    } // if (iDaysPreg > 0) and (fFoetuses > 0.0)  

                    // Numbers with each number of suckling young
                    // Different logic for sheep and cattle:
                    // - for sheep, first assume average body condition at conception and vary
                    //   the chill index. If that doesn't work, fix the chill index & vary the
                    //   body condition
                    // - for cattle, fix the chill index & vary the body condition

                    if ((CohortsInfo.iDaysLact > 0) && (CohortsInfo.fOffspring > 0.0))
                    {
                        bLactationDone = false;
                        fCondition = 1.0;
                        fChillIndex = 0;
                        iMateDOY = 1 + (iDOY - CohortsInfo.iDaysLact - mainGenotype.Gestation + 729) % 365;
                        for (iCohort = CohortsInfo.iMinYears; iCohort <= CohortsInfo.iMaxYears; iCohort++)
                        {
                            AgeInfo[iCohort].iAgeAtMating = AgeInfo[iCohort].iAgeDays - CohortsInfo.iDaysLact - mainGenotype.Gestation;
                            AgeInfo[iCohort].fSizeAtMating = GrowthCurve(AgeInfo[iCohort].iAgeAtMating,
                                                                           CohortsInfo.ReproClass,
                                                                           mainGenotype) / mainGenotype.fSexStdRefWt(CohortsInfo.ReproClass);
                        }

                        if (mainGenotype.Animal == GrazType.AnimalType.Sheep)
                        {
                            // binary search for the chill index at birth that yields the desired proportion of lambs
                            fLowChill = 500.0;
                            fLowOffspring = getReproRate(CohortsInfo, mainGenotype, AgeInfo, fLatitude, iMateDOY, 1.0, fLowChill);
                            fHighChill = 2500.0;
                            fHighOffspring = getReproRate(CohortsInfo, mainGenotype, AgeInfo, fLatitude, iMateDOY, 1.0, fHighChill);

                            // this is a monotonically decreasing function...
                            if ((fHighOffspring < CohortsInfo.fOffspring) && (fLowOffspring > CohortsInfo.fOffspring))
                            {
                                do
                                {
                                    fChillIndex = 0.5 * (fLowChill + fHighChill);
                                    fTrialOffspring = getReproRate(CohortsInfo, mainGenotype, AgeInfo, fLatitude, iMateDOY, 1.0, fChillIndex);

                                    if (fTrialOffspring > CohortsInfo.fOffspring)
                                        fLowChill = fChillIndex;
                                    else
                                        fHighChill = fChillIndex;
                                } while (Math.Abs(fTrialOffspring - CohortsInfo.fOffspring) >= 1.0E-5); //until (Abs(fTrialOffspring-CohortsInfo.fOffspring) < 1.0E-5);

                                bLactationDone = true;
                            }
                        } // fitting lactation rate to a chill index

                        if (!bLactationDone)
                        {
                            fChillIndex = 800.0;
                            // binary search for the body condition at mating that yields the desired proportion of lambs or calves
                            fLowCondition = 0.60;
                            fLowOffspring = getReproRate(CohortsInfo, mainGenotype, AgeInfo, fLatitude, iMateDOY, fLowCondition, fChillIndex);
                            fHighCondition = 1.40;
                            fHighOffspring = getReproRate(CohortsInfo, mainGenotype, AgeInfo, fLatitude, iMateDOY, fHighCondition, fChillIndex);

                            if (fLowOffspring > CohortsInfo.fOffspring)
                                fCondition = fLowCondition;
                            else if (fHighOffspring < CohortsInfo.fOffspring)
                                fCondition = fHighCondition;
                            else
                            {
                                do
                                {
                                    fCondition = 0.5 * (fLowCondition + fHighCondition);
                                    fTrialOffspring = getReproRate(CohortsInfo, mainGenotype, AgeInfo, fLatitude, iMateDOY, fCondition, fChillIndex);

                                    if (fTrialOffspring < CohortsInfo.fOffspring)
                                        fLowCondition = fCondition;
                                    else
                                        fHighCondition = fCondition;
                                } while (Math.Abs(fTrialOffspring - CohortsInfo.fOffspring) >= 1.0E-5); //until (Abs(fTrialOffspring-CohortsInfo.fOffspring) < 1.0E-5);
                            }
                        } // fitting lactation rate to a condition 

                        // Compute final offspring rates and numbers
                        for (iCohort = CohortsInfo.iMinYears; iCohort <= CohortsInfo.iMaxYears; iCohort++)
                        {
                            fLactRate = getOffspringRates(mainGenotype, fLatitude, iMateDOY,
                                                            AgeInfo[iCohort].iAgeAtMating,
                                                            AgeInfo[iCohort].fSizeAtMating,
                                                            fCondition, fChillIndex);
                            for (iPreg = 0; iPreg <= 3; iPreg++)
                            {
                                for (iLact = 1; iLact <= 3; iLact++)
                                    iShiftNumber[iLact] = Convert.ToInt32(Math.Round(fLactRate[iLact] * AgeInfo[iCohort].iNumbers[iPreg, 0]));
                                for (iLact = 1; iLact <= 3; iLact++)
                                {
                                    AgeInfo[iCohort].iNumbers[iPreg, iLact] += iShiftNumber[iLact];
                                    AgeInfo[iCohort].iNumbers[iPreg, 0] -= iShiftNumber[iLact];
                                }
                            }
                        }
                    } //_ lactating animals _
                } //_ female animals 

                // Construct the animal groups from the numbers and cohort-specific information
                AnimalInits.sGenotype = CohortsInfo.sGenotype;
                AnimalInits.sMatedTo = CohortsInfo.sMatedTo;
                AnimalInits.Sex = CohortsInfo.ReproClass;
                AnimalInits.Birth_CS = StdMath.DMISSING;
                AnimalInits.Paddock = "";
                AnimalInits.Tag = 0;
                AnimalInits.Priority = 0;

                for (iCohort = CohortsInfo.iMinYears; iCohort <= CohortsInfo.iMaxYears; iCohort++)
                {
                    for (iPreg = 0; iPreg <= 3; iPreg++)
                    {
                        for (iLact = 0; iLact <= 3; iLact++)
                        {
                            if (AgeInfo[iCohort].iNumbers[iPreg, iLact] > 0)
                            {
                                AnimalInits.Number = AgeInfo[iCohort].iNumbers[iPreg, iLact];
                                AnimalInits.AgeDays = AgeInfo[iCohort].iAgeDays;
                                AnimalInits.Weight = AgeInfo[iCohort].fBaseWeight + AgeInfo[iCohort].fFleeceWt;
                                AnimalInits.MaxPrevWt = StdMath.DMISSING; // compute from cond_score
                                AnimalInits.Fleece_Wt = AgeInfo[iCohort].fFleeceWt;
                                AnimalInits.Fibre_Diam = TAnimalParamSet.fDefaultMicron(mainGenotype,
                                                                             AnimalInits.AgeDays,
                                                                             AnimalInits.Sex,
                                                                             iDaysSinceShearing,
                                                                             AnimalInits.Fleece_Wt);
                                if (iPreg > 0)
                                {
                                    AnimalInits.Pregnant = CohortsInfo.iDaysPreg;
                                    AnimalInits.No_Foetuses = iPreg;
                                }
                                else
                                {
                                    AnimalInits.Pregnant = 0;
                                    AnimalInits.No_Foetuses = 0;
                                }

                                if ((iLact > 0)
                                   || ((mainGenotype.Animal == GrazType.AnimalType.Cattle) && (CohortsInfo.iDaysLact > 0) && (CohortsInfo.fOffspring == 0.0)))
                                {
                                    AnimalInits.Lactating = CohortsInfo.iDaysLact;
                                    AnimalInits.No_Suckling = iLact;
                                    AnimalInits.Young_GFW = CohortsInfo.fLambGFW;
                                    AnimalInits.Young_Wt = CohortsInfo.fOffspringWt;
                                }
                                else
                                {
                                    AnimalInits.Lactating = 0;
                                    AnimalInits.No_Suckling = 0;
                                    AnimalInits.Young_GFW = 0.0;
                                    AnimalInits.Young_Wt = 0.0;
                                }

                                groupIndex = Add(AnimalInits);
                                if (newGroups != null)
                                {
                                    newGroups.Add(groupIndex);
                                }
                            }
                        }
                    }
                }
            } // if CohortsInfo.iNumber > 0 
        }

        /// <summary>
        /// Executes a "buy" event
        /// </summary>
        /// <param name="AnimalInfo"></param>
        /// <returns></returns>
        protected int Buy(TPurchaseInfo AnimalInfo)
        {
            TAnimalParamSet aGenotype;
            TAnimalGroup NewGroup;
            double fBodyCondition;
            double fLiveWeight;
            double fLowBaseWeight = 0.0;
            double fHighBaseWeight = 0.0;
            TAnimalList WeanList;
            int PaddNo;

            int result = 0;

            if (AnimalInfo.Number > 0)
            {
                aGenotype = getGenotype(AnimalInfo.sGenotype);

                if (AnimalInfo.LiveWt > 0.0)
                    fLiveWeight = AnimalInfo.LiveWt;
                else
                {
                    fLiveWeight = GrowthCurve(AnimalInfo.AgeDays, AnimalInfo.Repro, aGenotype);
                    if (AnimalInfo.fCondScore > 0.0)
                        fLiveWeight = fLiveWeight * TAnimalParamSet.CondScore2Condition(AnimalInfo.fCondScore);
                    if (aGenotype.Animal == GrazType.AnimalType.Sheep)
                        fLiveWeight = fLiveWeight + AnimalInfo.GFW;
                }

                // Construct a new group of animals.     
                NewGroup = new TAnimalGroup(aGenotype,
                                                 AnimalInfo.Repro,                         //   Repro should be Empty, Castrated or 
                                                 AnimalInfo.Number,                        //   Male; pregnancy is handled with the 
                                                 AnimalInfo.AgeDays,                       //   Preg  field.                        
                                                 fLiveWeight,
                                                 AnimalInfo.GFW,
                                                 RandFactory);

                if ((AnimalInfo.fCondScore > 0.0) && (AnimalInfo.LiveWt > 0.0))        // Adjust the condition score if it has  
                {                                                                      //   been given                          
                    fBodyCondition = TAnimalParamSet.CondScore2Condition(AnimalInfo.fCondScore);
                    NewGroup.WeightRangeForCond(AnimalInfo.Repro, AnimalInfo.AgeDays,
                                                fBodyCondition, NewGroup.Genotype,
                                                ref fLowBaseWeight, ref fHighBaseWeight);

                    if ((NewGroup.BaseWeight >= fLowBaseWeight) && (NewGroup.BaseWeight <= fHighBaseWeight))
                        NewGroup.setConditionAtWeight(fBodyCondition);
                    else
                    {
                        NewGroup = null;
                        throw new Exception("Purchased animals with condition score "
                                                + AnimalInfo.fCondScore.ToString() + "\n"
                                                + " must have a base weight in the range "
                                                + fLowBaseWeight.ToString()
                                                + "-" + fHighBaseWeight.ToString() + " kg");
                    }
                }

                if (NewGroup.ReproState == GrazType.ReproType.Empty)
                {
                    if (AnimalInfo.sMatedTo != "")                                      // Use TAnimalGroup's property interface 
                        NewGroup.MatedTo = getGenotype(AnimalInfo.sMatedTo);            //   to set up pregnancy and lactation.  
                    NewGroup.Pregnancy = AnimalInfo.Preg;
                    NewGroup.Lactation = AnimalInfo.Lact;
                    if ((NewGroup.Animal == GrazType.AnimalType.Cattle)
                       && (AnimalInfo.Lact > 0) && (AnimalInfo.NYoung == 0))            // NYoung denotes the number of          
                    {                                                                   //   *suckling* young in lactating cows, 
                        WeanList = null;                                                //   which isn't quite the same as the   
                        NewGroup.Wean(true, true, ref WeanList, ref WeanList);          //   YoungNo property                    
                        WeanList = null; ;
                    }
                    else if (AnimalInfo.NYoung > 0)
                    {
                        // if the animals are pregnant then they need feotuses
                        if (NewGroup.Pregnancy > 0)
                        {
                            if ((AnimalInfo.Lact > 0) && (NewGroup.Animal == GrazType.AnimalType.Cattle))
                            {
                                NewGroup.NoOffspring = 1;
                                NewGroup.NoFoetuses = Math.Min(2, Math.Max(0, AnimalInfo.NYoung - 1));
                            }
                            else
                            {
                                NewGroup.NoFoetuses = Math.Min(3, AnimalInfo.NYoung);                 // recalculates livewt
                            }
                        }
                        else
                            NewGroup.NoOffspring = AnimalInfo.NYoung;
                    }

                    if (NewGroup.Young != null)                                         // Lamb/calf weights and lamb fleece     
                    {                                                                   //   weights are optional.               
                        if (bIsGiven(AnimalInfo.YoungWt))
                            NewGroup.Young.LiveWeight = AnimalInfo.YoungWt;
                        if (bIsGiven(AnimalInfo.YoungGFW))
                            NewGroup.Young.FleeceCutWeight = AnimalInfo.YoungGFW;
                    }
                } //if (ReproState = Empty) 


                PaddNo = 0;                                                             // Newly bought animals have tag # zero  
                while ((PaddNo < Paddocks.Count()) && (Paddocks.byIndex(PaddNo).sName == ""))  // and go in the first named paddock.  
                    PaddNo++;
                if (PaddNo >= Paddocks.Count())
                    PaddNo = 0;
                result = Add(NewGroup, Paddocks.byIndex(PaddNo), 0, 0);
            } // if AnimalInfo.Number > 0 
            return result;
        }

        /// <summary>
        /// If GroupIdx=0, work through all groups, removing animals until Number        
        /// animals (not including unweaned lambs/calves) have been removed.  If         
        /// GroupIdx>0, then remove the lesser of Number animals and all animals in      
        /// the group                                                                    
        /// </summary>
        /// <param name="GroupIdx"></param>
        /// <param name="Number"></param>
        public void Sell(int GroupIdx, int Number)
        {
            int iNoToSell;
            int Idx;

            Idx = 1;
            while ((Idx <= Count()) && (Number > 0))                                     // A negative number is construed as zero
            {
                if (((GroupIdx == 0) || (GroupIdx == Idx)) && (At(Idx) != null))          // Does this call apply to group I?      
                {
                    iNoToSell = Math.Min(Number, At(Idx).NoAnimals);
                    At(Idx).NoAnimals = At(Idx).NoAnimals - iNoToSell;
                    if (GroupIdx == 0)
                        Number = Number - iNoToSell;
                    else
                        Number = 0;
                }
                Idx++;
            }
        }

        /// <summary>
        /// Sell the animals that have this tag. Sells firstly from the group with the
        /// smallest index.
        /// </summary>
        /// <param name="iTag"></param>
        /// <param name="Number"></param>
        public void SellTag(int iTag, int Number)
        {
            int iNoToSell;
            int Idx;
            int iRemainToSell;

            iRemainToSell = Number;                                                      //count down the numbers for sale in a group
            Idx = 1;
            while ((Idx <= Count()) && (iRemainToSell > 0))                                // A negative number is construed as zero
            {
                if ((iTag == getTag(Idx)) && (At(Idx) != null))                             // Does this call apply to group I?      
                {
                    iNoToSell = Math.Min(iRemainToSell, At(Idx).NoAnimals);                     //only sell what is possible from this group
                    At(Idx).NoAnimals = At(Idx).NoAnimals - iNoToSell;
                    iRemainToSell = iRemainToSell - iNoToSell;
                }
                Idx++;
            }
        }

        /// <summary>
        /// If GroupIdx=0, shear all groups; otherwise shear the nominated group.        
        /// Unweaned lambs are not shorn.                                                
        /// </summary>
        /// <param name="GroupIdx"></param>
        /// <param name="bAdults"></param>
        /// <param name="bLambs"></param>
        public void Shear(int GroupIdx, bool bAdults, bool bLambs)
        {
            double fDummy = 0;
            int I;

            for (I = 1; I <= Count(); I++)
            {
                if (((GroupIdx == 0) || (GroupIdx == I)) && (At(I) != null))
                {
                    if (bAdults)
                        At(I).Shear(ref fDummy);
                    if (bLambs && (At(I).Young != null))
                        At(I).Young.Shear(ref fDummy);
                }
            }
        }
        /// <summary>
        /// If GroupIdx=0, commence joining of all groups; otherwise commence joining    
        /// of the nominated group.                                                      
        /// </summary>
        /// <param name="GroupIdx"></param>
        /// <param name="sMateTo"></param>
        /// <param name="MateDays"></param>
        public void Join(int GroupIdx, string sMateTo, int MateDays)
        {
            int I;

            for (I = 1; I <= Count(); I++)
                if (((GroupIdx == 0) || (GroupIdx == I)) && (At(I) != null))
                    At(I).Join(getGenotype(sMateTo), MateDays);
        }
        /// <summary>
        /// The castration routine is complicated somewhat by the fact that the          
        /// parameter refers to the number of male lambs or calves to castrate.          
        /// When this number is less than the number of male lambs or calves in a        
        /// group, the excess must be split off.                                         
        /// </summary>
        /// <param name="GroupIdx"></param>
        /// <param name="Number"></param>
        public void Castrate(int GroupIdx, int Number)
        {
            int NoToCastrate;
            int I, N;

            N = Count();                                                               // Store the initial list size so that     
            for (I = 1; I <= N; I++)                                                        //   groups which are split off aren't      
            {
                if (((GroupIdx == 0) || (GroupIdx == I)) && (At(I) != null))           //   processed twice                        
                {
                    if ((At(I).Young != null) && (At(I).Young.MaleNo > 0) && (Number > 0))
                    {
                        NoToCastrate = Math.Min(Number, At(I).Young.MaleNo);
                        if (NoToCastrate < At(I).Young.MaleNo)
                            this.Split(I, Convert.ToInt32(Math.Round((double)Number / NoToCastrate * At(I).NoAnimals)));        //TODO: check this conversion
                        At(I).Young.Castrate();
                        Number = Number - NoToCastrate;
                    }
                }
            }
        }

        /// <summary>
        /// See the notes to the Castrate method; but weaning is even further         
        /// complicated because males and/or females may be weaned.                   
        /// </summary>
        /// <param name="GroupIdx"></param>
        /// <param name="Number"></param>
        /// <param name="WeanFemales"></param>
        /// <param name="WeanMales"></param>
        public void Wean(int GroupIdx, int Number, bool WeanFemales, bool WeanMales)
        {
            int NoToWean;
            int MothersToWean;
            TAnimalList NewGroups;
            int Idx, N;

            Number = Math.Max(Number, 0);
            N = Count();                                                                    // Only iterate through groups present at
            for (Idx = 1; Idx <= N; Idx++)                                                  //   the start of the routine            
            {
                if (((GroupIdx == 0) || (GroupIdx == Idx)) && (At(Idx) != null))            // Group Idx, or all groups if 0         
                {
                    if (At(Idx).Young != null)
                    {
                        if (WeanMales && WeanFemales)                                       // Establish the number of lambs/calves  
                            NoToWean = Math.Min(Number, At(Idx).Young.NoAnimals);           //   to wean from this group of mothers  
                        else if (WeanMales)
                            NoToWean = Math.Min(Number, At(Idx).Young.MaleNo);
                        else if (WeanFemales)
                            NoToWean = Math.Min(Number, At(Idx).Young.FemaleNo);
                        else
                            NoToWean = 0;

                        if (NoToWean > 0)
                        {
                            if (NoToWean == Number)
                            {
                                if (WeanMales && WeanFemales)                                       // If there are more lambs/calves present
                                    MothersToWean = Convert.ToInt32(Math.Round((double)NoToWean / At(Idx).NoOffspring));     //   than are to be weaned, split the 
                                else                                                                //   excess off                       
                                    MothersToWean = Convert.ToInt32(Math.Round(NoToWean / (At(Idx).NoOffspring / 2.0)));
                                if (MothersToWean < At(Idx).NoAnimals)
                                    this.Split(Idx, MothersToWean);
                            }
                            NewGroups = null;                                                       // Carry out the weaning process. N.B.   
                            At(Idx).Wean(WeanFemales, WeanMales, ref NewGroups, ref NewGroups);             //   the weaners appear in the same      
                            Add(NewGroups, getPaddInfo(Idx), getTag(Idx), getPriority(Idx));        //   paddock as their mothers and with   
                            NewGroups = null;                                                       //   the same tag and priority value     
                        }

                        Number = Number - NoToWean;
                    } //_ if (Young <> NIL) 
                }
            }
        }

        /// <summary>
        /// If GroupIdx=0, end lactation of all groups; otherwise end lactation of    
        /// of the nominated group.                                                   
        /// </summary>
        /// <param name="GroupIdx"></param>
        /// <param name="Number"></param>
        public void DryOff(int GroupIdx, int Number)
        {
            int NoToDryOff;
            int I, N;

            Number = Math.Max(Number, 0);                                               // Only iterate through groups present at   
            N = Count();                                                          //   the start of the routine               
            for (I = 1; I <= N; I++)                                                         // Group I, or all groups if I=0            
            {
                if (((GroupIdx == 0) || (GroupIdx == I)) && (At(I) != null) && (At(I).Lactation > 0))
                {
                    NoToDryOff = Math.Min(Number, At(I).FemaleNo);
                    if (NoToDryOff > 0)
                    {
                        if (NoToDryOff < At(I).FemaleNo)
                            this.Split(I, NoToDryOff);
                        At(I).DryOff();
                    }
                    Number = Number - NoToDryOff;
                }
            }
        }

        /// <summary>
        /// Break an animal group up in various ways; by number, by age, by weight    
        /// or by sex of lambs/calves.  The new group(s) have the same priority and   
        /// paddock as the original.  SplitWeight assumes a distribution of weights   
        /// around the group average.                                                 
        /// </summary>
        /// <param name="GroupIdx"></param>
        /// <param name="NoToKeep"></param>
        public void Split(int GroupIdx, int NoToKeep)
        {
            TAnimalGroup srcGroup;
            int iNoToSplit;

            srcGroup = getAt(GroupIdx);
            if (srcGroup != null)
            {
                iNoToSplit = Math.Max(0, srcGroup.NoAnimals - Math.Max(NoToKeep, 0));
                if (iNoToSplit > 0)
                    Add(srcGroup.Split(iNoToSplit, false, srcGroup.NODIFF, srcGroup.NODIFF),
                         getPaddInfo(GroupIdx), getTag(GroupIdx), getPriority(GroupIdx));
            }
        }
        /// <summary>
        /// Split the group by age
        /// </summary>
        /// <param name="GroupIdx"></param>
        /// <param name="Age_Days"></param>
        public void SplitAge(int GroupIdx, int Age_Days)
        {
            TAnimalGroup srcGroup;
            int NM = 0;
            int NF = 0;

            srcGroup = getAt(GroupIdx);
            if (srcGroup != null)
            {
                srcGroup.GetOlder(Age_Days, ref NM, ref NF);
                if (NM + NF > 0)
                    Add(srcGroup.Split(NM + NF, true, srcGroup.NODIFF, srcGroup.NODIFF),
                         getPaddInfo(GroupIdx), getTag(GroupIdx), getPriority(GroupIdx));
            }
        }
        /// <summary>
        /// Split the group by weight
        /// </summary>
        /// <param name="GroupIdx"></param>
        /// <param name="SplitWt"></param>
        public void SplitWeight(int GroupIdx, double SplitWt)
        {
            double VarRatio = 0.10;                                                 // Coefficient of variation of LW (0-1)       
            int NOSTEPS = 20;

            TAnimalGroup srcGroup;
            double SplitSD;                                                         // Position of the threshold on the live wt   
            //   distribution of the group, in S.D. units 
            double RemovePropn;                                                     // Proportion of animals lighter than SplitWt 
            int NoToRemove;                                                         // Number to transfer to TempAnimals          
            int iNoAnimals;
            double fLiveWt;
            TDifferenceRecord Diffs;
            double RightSD;
            double StepWidth;
            double PrevCum;
            double CurrCum;
            double RemoveLW;
            double DiffRatio;
            int Idx;

            srcGroup = getAt(GroupIdx);
            if (srcGroup != null)
            {
                iNoAnimals = srcGroup.NoAnimals;
                fLiveWt = srcGroup.LiveWeight;
                SplitSD = (SplitWt - fLiveWt) / (VarRatio * fLiveWt);               // NB SplitWt is a live weight value     

                RemovePropn = StdMath.CumNormal(SplitSD);                           // Normal distribution of weights assumed
                NoToRemove = Convert.ToInt32(Math.Round(iNoAnimals * RemovePropn));

                if (NoToRemove > 0)
                {
                    Diffs = new TDifferenceRecord() { StdRefWt = srcGroup.NODIFF.StdRefWt, BaseWeight = srcGroup.NODIFF.BaseWeight, FleeceWt = srcGroup.NODIFF.FleeceWt };            
                    if (NoToRemove < iNoAnimals)
                    {                                                               // This computation gives us the mean    
                        RightSD = -5.0;                                             //   live weight of animals which are    
                        StepWidth = (SplitSD - RightSD) / NOSTEPS;                  //   lighter than the weight threshold.  
                        RemoveLW = 0.0;                                             //   We are integrating over a truncated 
                        PrevCum = 0.0;                                              //   normal distribution, using the      
                        for (Idx = 1; Idx <= NOSTEPS; Idx++)                        //   differences between successive      
                        {                                                           //   evaluations of the CumNormal        
                            RightSD = RightSD + StepWidth;                          //   function                            
                            CurrCum = StdMath.CumNormal(RightSD);
                            RemoveLW = RemoveLW + (CurrCum - PrevCum) * fLiveWt * (1.0 + VarRatio * (RightSD - 0.5 * StepWidth));
                            PrevCum = CurrCum;
                        }
                        RemoveLW = RemoveLW / RemovePropn;

                        DiffRatio = iNoAnimals / (iNoAnimals - NoToRemove) * (RemoveLW / fLiveWt - 1.0);
                        Diffs.BaseWeight = DiffRatio * srcGroup.BaseWeight;
                        Diffs.StdRefWt = DiffRatio * srcGroup.StdReferenceWt;               // Weight diffs within a group are       
                        Diffs.FleeceWt = DiffRatio * srcGroup.FleeceCutWeight;              //   assumed genetic!                    
                    } //_ if (NoToRemove < NoAnimals) _

                    Add(srcGroup.Split(NoToRemove, false, Diffs, srcGroup.NODIFF),          // Now we have computed Diffs, we split  
                         getPaddInfo(GroupIdx), getTag(GroupIdx), getPriority(GroupIdx));   //   up the animals                      
                } //_ if (NoToRemove > 0) _
            }
        }
        /// <summary>
        /// Split off the young
        /// </summary>
        /// <param name="GroupIdx"></param>
        public void SplitYoung(int GroupIdx)
        {
            TAnimalGroup srcGroup;
            TAnimalList NewGroups;

            srcGroup = getAt(GroupIdx);
            if (srcGroup != null)
            {
                NewGroups = null;
                srcGroup.SplitYoung(ref NewGroups);
                this.Add(NewGroups, getPaddInfo(GroupIdx), getTag(GroupIdx), getPriority(GroupIdx));
                NewGroups = null;
            }

        }

        /// <summary>
        /// Sorting is done using the one-offset FStock array                            
        /// </summary>
        public void Sort()
        {
            int Idx, Jdx;

            for (Idx = 1; Idx <= Count() - 1; Idx++)
            {
                for (Jdx = Idx + 1; Jdx <= Count(); Jdx++)
                {
                    if (FStock[Idx].iTag > FStock[Jdx].iTag)
                    {
                        FStock[0] = FStock[Idx];                                            // FStock[0] is temporary storage        
                        FStock[Idx] = FStock[Jdx];
                        FStock[Jdx] = FStock[0];
                    }
                }
            }
        }
        /// <summary>
        /// Perform a drafting operation
        /// </summary>
        /// <param name="sClosed"></param>
        public void Draft(List<string> sClosed)
        {
            double[] fPaddockRank;
            bool[] bAvailable;
            TAnimalGroup tempAnimals;
            int iPrevPadd;
            int iBestPadd;
            double fBestRank;
            int iPrevPriority;
            int iBestPriority;
            int iPadd, Idx;

            if ((Count() > 0) && (Paddocks.Count() > 0))
            {
                fPaddockRank = new double[Paddocks.Count()];
                bAvailable = new bool[Paddocks.Count()];

                for (iPadd = 0; iPadd <= Paddocks.Count() - 1; iPadd++)                                      // Only draft into pasture paddocks     
                    bAvailable[iPadd] = (Paddocks.byIndex(iPadd).Forages.Count() > 0);

                for (Idx = 1; Idx <= this.Count(); Idx++)                                              // Paddocks occupied by groups that are  
                {
                    if (getPriority(Idx) <= 0)                                                //not to be drafted                   
                    {
                        iPadd = Paddocks.IndexOf(getInPadd(Idx));
                        if (iPadd >= 0)
                            bAvailable[iPadd] = false;
                    }
                }

                for (Idx = 0; Idx <= sClosed.Count() - 1; Idx++)                                        // Paddocks closed by the manager        
                {
                    iPadd = Paddocks.IndexOf(sClosed[Idx]);
                    if (iPadd >= 0)
                        bAvailable[iPadd] = false;
                }

                tempAnimals = At(1).Copy();
                for (iPadd = 0; iPadd <= Paddocks.Count() - 1; iPadd++)                                 // Rank order for open, unoccupied paddocks                            
                {
                    if (bAvailable[iPadd])
                        fPaddockRank[iPadd] = getPaddockRank(Paddocks.byIndex(iPadd), tempAnimals);
                    else
                        fPaddockRank[iPadd] = 0.0;
                }
                tempAnimals = null;

                iPrevPadd = 0;                                                                          // Fallback paddock if none available    
                while ((iPrevPadd < Paddocks.Count() - 1) && (Paddocks.byIndex(iPrevPadd).sName == ""))
                    iPrevPadd++;

                iPrevPriority = 0;
                do
                {
                    iBestPadd = -1;                                                                     // Locate the best available paddock     
                    fBestRank = -1.0;
                    for (iPadd = 0; iPadd <= Paddocks.Count() - 1; iPadd++)
                    {
                        if (bAvailable[iPadd] && (fPaddockRank[iPadd] > fBestRank))
                        {
                            iBestPadd = iPadd;
                            fBestRank = fPaddockRank[iPadd];
                        }
                    }
                    if (iBestPadd == -1)                                                                // No unoccupied paddocks - use the      
                        iBestPadd = iPrevPadd;                                                          //lowest-ranked unoccupied paddock    

                    iBestPriority = Int32.MaxValue;                                                     // Locate the next-smallest priority score 
                    for (Idx = 1; Idx <= this.Count(); Idx++)
                    {
                        if ((getPriority(Idx) < iBestPriority) && (getPriority(Idx) > iPrevPriority))
                            iBestPriority = getPriority(Idx);
                    }
                    for (Idx = 1; Idx <= this.Count(); Idx++)                                           // Move animals with that priority score 
                    {
                        if (getPriority(Idx) == iBestPriority)
                            setInPadd(Idx, Paddocks.byIndex(iBestPadd).sName);
                    }
                    bAvailable[iBestPadd] = false;

                    iPrevPadd = iBestPadd;
                    iPrevPriority = iBestPriority;
                }
                while (iBestPriority != Int32.MaxValue);

            }
        }

        /// <summary>
        /// Perform a drafting operation
        /// </summary>
        /// <param name="tagNo"></param>
        /// <param name="sClosed"></param>
        public void Draft(int tagNo, List<string> sClosed)
        {
            double[] fPaddockRank;
            bool[] bAvailable;
            TAnimalGroup tempAnimals;
            int iPrevPadd;
            int iBestPadd;
            double fBestRank;
            int iPrevPriority;
            int iBestPriority;
            int iPadd, Idx;

            if ((Count() > 0) && (Paddocks.Count() > 0))
            {
                fPaddockRank = new double[Paddocks.Count()];
                bAvailable = new bool[Paddocks.Count()];

                for (iPadd = 0; iPadd <= Paddocks.Count() - 1; iPadd++)                                      // Only draft into pasture paddocks      
                    bAvailable[iPadd] = (Paddocks.byIndex(iPadd).Forages.Count() > 0);

                for (Idx = 1; Idx <= this.Count(); Idx++)                                              // Paddocks occupied by groups that are  
                {
                    if (getPriority(Idx) <= 0)                                              //   not to be drafted                   
                    {
                        iPadd = Paddocks.IndexOf(getInPadd(Idx));
                        if (iPadd >= 0)
                            bAvailable[iPadd] = false;
                    }
                }

                for (Idx = 0; Idx <= sClosed.Count() - 1; Idx++)                                         // Paddocks closed by the manager        
                {
                    iPadd = Paddocks.IndexOf(sClosed[Idx]);
                    if (iPadd >= 0)
                        bAvailable[iPadd] = false;
                }

                tempAnimals = At(1).Copy();
                for (iPadd = 0; iPadd <= Paddocks.Count() - 1; iPadd++)                                      // Rank order for open, unoccupied       
                {
                    if (bAvailable[iPadd])                                                 //   paddocks                            
                        fPaddockRank[iPadd] = getPaddockRank(Paddocks.byIndex(iPadd), tempAnimals);
                    else
                        fPaddockRank[iPadd] = 0.0;
                }
                tempAnimals = null;

                iPrevPadd = 0;                                                            // Fallback paddock if none available    
                while ((iPrevPadd < Paddocks.Count() - 1) && (Paddocks.byIndex(iPrevPadd).sName == ""))
                    iPrevPadd++;

                iPrevPriority = 0;
                do
                {
                    iBestPadd = -1;                                                         // Locate the best available paddock     
                    fBestRank = -1.0;
                    for (iPadd = 0; iPadd <= Paddocks.Count() - 1; iPadd++)
                    {
                        if (bAvailable[iPadd] && (fPaddockRank[iPadd] > fBestRank))
                        {
                            iBestPadd = iPadd;
                            fBestRank = fPaddockRank[iPadd];
                        }
                    }
                    if (iBestPadd == -1)                                                  // No unoccupied paddocks - use the      
                        iBestPadd = iPrevPadd;                                                //   lowest-ranked unoccupied paddock    

                    iBestPriority = Int32.MaxValue;                                                 // Locate the next-smallest priority score 
                    for (Idx = 1; Idx <= this.Count(); Idx++)
                    {
                        if ((getPriority(Idx) < iBestPriority) && (getPriority(Idx) > iPrevPriority))
                            iBestPriority = getPriority(Idx);
                    }

                    for (Idx = 1; Idx <= this.Count(); Idx++)                                            // Move animals with that priority score 
                    {
                        if ((this.getTag(Idx) == tagNo) && (getPriority(Idx) == iBestPriority))
                            setInPadd(Idx, Paddocks.byIndex(iBestPadd).sName);
                    }
                    bAvailable[iBestPadd] = false;

                    iPrevPadd = iBestPadd;
                    iPrevPriority = iBestPriority;
                }
                while (iBestPriority != Int32.MaxValue);

            }
        }

        // ==============================================================================
        // Execute user's internally defined tasks for this day
        // ==============================================================================

        /// <summary>
        /// Setup the stock groups using the internal criteria the user has defined
        /// for this component.
        /// </summary>
        /// <param name="currentDay"></param>
        /// <param name="latitude"></param>
        public void ManageInternalInit(int currentDay, double latitude)
        {
            int i;
            TEnterpriseInfo curEnt;

            for (i = 0; i <= Enterprises.Count - 1; i++)   //for each enterprise
            {
                curEnt = Enterprises.byIndex(i);
                if (curEnt.ManageInit)
                    manageInitialiseStock(currentDay, curEnt, latitude);

                if (curEnt.ManageGrazing)
                    manageGrazing(currentDay, currentDay, curEnt);
            }
        }

        /// <summary>
        /// Follow the management events described by the user for this stock component.
        /// </summary>
        /// <param name="currentDate"></param>
        public void ManageInternalTasks(int currentDate)
        {
            int i;
            TEnterpriseInfo curEnt;
            int currentDay;

            currentDay = StdDate.DateVal(StdDate.DayOf(currentDate), StdDate.MonthOf(currentDate), 0);
            for (i = 0; i <= Enterprises.Count - 1; i++)   //for each enterprise
            {
                curEnt = Enterprises.byIndex(i);
                manageDailyTasks(currentDay, curEnt);   //correct order?

                if (curEnt.ManageGrazing)
                    manageGrazing(currentDate, currentDay, curEnt);

                //manage: supplementary feeding

                if (curEnt.ManageShearing)
                    manageShearing(currentDay, curEnt);
                if (curEnt.ManageCFA)
                    manageCFA(currentDay, curEnt);
                if (curEnt.ManageSelling)
                    manageSelling(currentDay, curEnt);
                if (curEnt.ManageReplacement)
                    manageReplacement(currentDay, curEnt);
                if (curEnt.ManageReproduction)
                    manageReproduction(currentDay, curEnt);
            } //next enterprise
        }

        // Paddock rank order ......................................................
        /// <summary>
        /// Rank the paddocks
        /// </summary>
        /// <param name="sList"></param>
        public void rankPaddocks(List<string> sList)
        {
            double[] fPaddockRank = new double[Paddocks.Count()];
            TAnimalGroup tempAnimals;
            int iBestPadd;
            double fBestRank;
            int iPadd, Idx;

            if (Count() > 0)
                tempAnimals = At(1).Copy();
            else
                tempAnimals = new TAnimalGroup(getGenotype("Medium Merino"), GrazType.ReproType.Empty, 1, 365 * 4, 50.0, 0.0, RandFactory);
            for (iPadd = 0; iPadd <= Paddocks.Count() - 1; iPadd++)
                fPaddockRank[iPadd] = getPaddockRank(Paddocks.byIndex(iPadd), tempAnimals);

            sList.Clear();
            for (Idx = 0; Idx <= Paddocks.Count() - 1; Idx++)
            {
                fBestRank = -1.0;
                iBestPadd = -1;
                for (iPadd = 0; iPadd <= Paddocks.Count() - 1; iPadd++)
                {
                    if (fPaddockRank[iPadd] > fBestRank)
                    {
                        iBestPadd = iPadd;
                        fBestRank = fPaddockRank[iPadd];
                    }
                }
                sList.Add(Paddocks.byIndex(iBestPadd).sName);
                fPaddockRank[iBestPadd] = -999.9;
            }
        }

        /// <summary>
        /// Convert the Stock geno object to TSingleGenotypeInits
        /// </summary>
        /// <param name="aValue"></param>
        /// <param name="Inits"></param>
        public void Value2GenotypeInits(TStockGeno aValue, ref TSingleGenotypeInits Inits)
        {
            int Idx;

            Inits.sGenotypeName = aValue.name;
            Inits.sDamBreed = aValue.dam_breed;
            Inits.sSireBreed = aValue.sire_breed;
            Inits.iGeneration = aValue.generation;
            Inits.SRW = aValue.srw;
            Inits.PotFleeceWt = aValue.ref_fleece_wt;
            Inits.MaxFibreDiam = aValue.max_fibre_diam;
            Inits.FleeceYield = aValue.fleece_yield;
            Inits.PeakMilk = aValue.peak_milk;
            Inits.DeathRate[FALSE] = aValue.death_rate;
            Inits.DeathRate[TRUE] = aValue.wnr_death_rate;

            // Catch weaner death rates that are missing from v1.3 input data...
            if (Inits.DeathRate[TRUE] == 0.0)
                Inits.DeathRate[TRUE] = Inits.DeathRate[FALSE];

            for (int i = 0; i < Inits.Conceptions.Length; i++)
                Inits.Conceptions[0] = 0.0;
            for (Idx = 1; Idx <= aValue.conception.Length; Idx++)
                Inits.Conceptions[Idx-1] = aValue.conception[Idx-1];
        }

        private struct ReproRecord
        {
            public string Name;
            public GrazType.ReproType Repro;
            public ReproRecord(string name, GrazType.ReproType repro)
            {
                Name = name;
                Repro = repro;
            }
        }
        /// <summary>
        /// Converts a keyword to a ReproType.  Allows plurals in the keyword. 
        /// N.B. The routine is animal-insensitive, i.e. if 'COW' is passed in,      
        ///      Empty will be returned regardless of whether sheep or cattle are    
        ///      under consideration                                                 
        /// </summary>
        /// <param name="Keyword"></param>
        /// <param name="repro"></param>
        /// <returns></returns>
        private bool ParseRepro(string Keyword, ref GrazType.ReproType repro)
        {
            ReproRecord[] SexKeywords = new ReproRecord[8] {
                        new ReproRecord( name : "ram",    repro : GrazType.ReproType.Male ),
                        new ReproRecord( name : "crypto",  repro : GrazType.ReproType.Male ),
                        new ReproRecord( name : "wether", repro : GrazType.ReproType.Castrated ),
                        new ReproRecord( name : "ewe",    repro : GrazType.ReproType.Empty     ),
                        new ReproRecord( name : "bull",   repro : GrazType.ReproType.Male      ),
                        new ReproRecord( name : "steer",  repro : GrazType.ReproType.Castrated ),
                        new ReproRecord( name : "heifer", repro : GrazType.ReproType.Empty     ),
                        new ReproRecord( name : "cow",    repro : GrazType.ReproType.Empty     ) };

            int Idx;

            bool result = true;
            Keyword = Keyword.ToLower().Trim();
            if ((Keyword != "") && (Keyword[Keyword.Length - 1] == 's'))                 // Plurals are allowed                   
                Keyword = Keyword.Substring(0, Keyword.Length - 1);
            Idx = 0;
            while ((Idx <= 7) && (Keyword != SexKeywords[Idx].Name))
                Idx++;
            if (Idx <= 7)
                repro = SexKeywords[Idx].Repro;
            else
                repro = GrazType.ReproType.Castrated;
            if ((Idx > 7) && (Keyword.Length > 0))
                result = false;

            return result;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="aValue"></param>
        /// <param name="Inits"></param>
        public void SheepValue2AnimalInits(TSheepInit aValue, ref TAnimalInits Inits)
        {
            Inits = new TAnimalInits();

            Inits.sGenotype = aValue.genotype;
            Inits.Number = aValue.number;
            if (aValue.sex.Length > 0)
                ParseRepro(aValue.sex.ToLower(), ref Inits.Sex);
            else
                Inits.Sex = GrazType.ReproType.Castrated;
            Inits.AgeDays = Convert.ToInt32(aValue.age);
            Inits.Weight = aValue.weight;
            Inits.MaxPrevWt = aValue.max_prev_wt;
            Inits.Fleece_Wt = aValue.fleece_wt;
            Inits.Fibre_Diam = aValue.fibre_diam;
            Inits.sMatedTo = aValue.mated_to;
            Inits.Pregnant = aValue.pregnant;
            Inits.Lactating = aValue.lactating;

            if (Inits.Pregnant > 0)
            {
                Inits.No_Foetuses = aValue.no_young;
            }

            if (Inits.Lactating > 0)
            {
                Inits.No_Suckling = aValue.no_young;
                Inits.Birth_CS = aValue.birth_cs;
                Inits.Young_Wt = aValue.lamb_wt;
                Inits.Young_GFW = aValue.lamb_fleece_wt;
            }
            Inits.Paddock = aValue.paddock;
            Inits.Tag = aValue.tag;
            Inits.Priority = aValue.priority;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="aValue"></param>
        /// <param name="Inits"></param>
        public void CattleValue2AnimalInits(TCattleInit aValue, ref TAnimalInits Inits)
        {
            Inits = new TAnimalInits();

            Inits.sGenotype = aValue.genotype;
            Inits.Number = aValue.number;
            if (aValue.sex.Length > 0)
                ParseRepro(aValue.sex.ToLower(), ref Inits.Sex);
            else
                Inits.Sex = GrazType.ReproType.Castrated;
            Inits.AgeDays = Convert.ToInt32(aValue.age);
            Inits.Weight = aValue.weight;
            Inits.MaxPrevWt = aValue.max_prev_wt;
            Inits.sMatedTo = aValue.mated_to;
            Inits.Pregnant = aValue.pregnant;
            Inits.Lactating = aValue.lactating;
            if (Inits.Pregnant > 0)
            {
                Inits.No_Foetuses = aValue.no_foetuses;
            }

            if (Inits.Lactating > 0)
            {
                Inits.No_Suckling = aValue.no_suckling;
                Inits.Birth_CS = aValue.birth_cs;
                Inits.Young_Wt = aValue.calf_wt;
            }
            Inits.Paddock = aValue.paddock;
            Inits.Tag = aValue.tag;
            Inits.Priority = aValue.priority;
        }
        

        /// <summary>
        /// These functions return the number of days from the first date to the  
        /// second.  PosInterval assumes that its arguments are days-of-year, i.e.    
        /// YearOf(DOY1)=YearOf(DOY2)=0, while DaysFromDOY treats its first argument  
        /// as though it is a day-of-year and computes the number of days from that   
        /// day-of-year to the second date.                                           
        /// </summary>
        /// <param name="DOY1"></param>
        /// <param name="DOY2"></param>
        /// <returns></returns>
        private int PosInterval(int DOY1, int DOY2)
        {
            int Result = StdDate.Interval(DOY1, DOY2);
            if (Result < 0)
                Result = 366 + Result;
            return Result;
        }

        private int DaysFromDOY(int DOY, int Dt)
        {
            int DOY_MASK = 0xFFFF;
            int Result;
            if (StdDate.YearOf(Dt) == 0)
                Result = PosInterval(DOY & DOY_MASK, Dt);
            else
            {
                DOY = StdDate.DateVal(StdDate.DayOf(DOY), StdDate.MonthOf(DOY), StdDate.YearOf(Dt));
                if (DOY > Dt)
                    DOY = StdDate.DateShift(DOY, 0, 0, -1);
                Result = StdDate.Interval(DOY, Dt);
            }
            return Result;
        }
        /// <summary>
        /// Tests for a non-MISSING, non-zero value                                      
        /// </summary>
        /// <param name="X"></param>
        /// <returns></returns>
        public bool bIsGiven(double X)
        {
            return ((X != 0.0) && (Math.Abs(X - StdMath.DMISSING) > Math.Abs(0.0001 * StdMath.DMISSING)));
        }
        /// <summary>
        /// Calculate the days from the day of year in a non leap year
        /// </summary>
        /// <param name="iDOY"></param>
        /// <param name="aDay"></param>
        /// <returns></returns>
        public int DaysFromDOY365(int iDOY, int aDay)
        {
            int dtDOY;
            int Result;

            if (iDOY == 0)
                Result = 0;
            else
            {
                dtDOY = StdDate.DateShift(StdDate.DateVal(31, 12, StdDate.YearOf(aDay) - 1), iDOY % 365, 0, 0);
                if ((StdDate.YearOf(aDay) > 0) && (dtDOY > aDay))
                    dtDOY = StdDate.DateShift(dtDOY, 0, 0, -1);
                Result = StdDate.Interval(dtDOY, aDay);
            }
            return Result;
        }

        /// <summary>
        /// Stock age record
        /// </summary>
        [Serializable]
        protected struct TAgeRec
        {
            /// <summary>
            /// Group name
            /// </summary>
            public string GroupName;
            /// <summary>
            /// Minimum age in years
            /// </summary>
            public int age;       
        }

        /// <summary>
        /// Mob record
        /// </summary>
        [Serializable]
        protected struct TMobRec
        {
            /// <summary>
            /// Mob name
            /// </summary>
            public string MobName;
            /// <summary>
            /// Is mob male
            /// </summary>
            public bool MobMale;
            /// <summary>
            /// Is mob pure bred
            /// </summary>
            public bool MobPure;
        }

        private const int AGEGRPS = 7;

        // array[entWether..entLamb, 0..AGEGRPS-1]
        private TAgeRec[,] ENTAGEGRPS = {
            {new TAgeRec() {GroupName="Weaner", age=0},    //wether
             new TAgeRec() {GroupName="1y", age=1},
             new TAgeRec() {GroupName="2y", age=2},
             new TAgeRec() {GroupName="3y", age=3},
             new TAgeRec() {GroupName="4y", age=4},
             new TAgeRec() {GroupName="5y", age=5},
             new TAgeRec() {GroupName="6y", age=6}},
            {new TAgeRec() {GroupName="Weaner", age=0},    //ewe & wether
             new TAgeRec() {GroupName="1y", age=1},
             new TAgeRec() {GroupName="2y", age=2},
             new TAgeRec() {GroupName="3y", age=3},
             new TAgeRec() {GroupName="4y", age=4},
             new TAgeRec() {GroupName="5y", age=5},
             new TAgeRec() {GroupName="6y", age=6}},
            {new TAgeRec() {GroupName="Weaner", age=0},    //steer
             new TAgeRec() {GroupName="1y", age=1},
             new TAgeRec() {GroupName="2y", age=2},
             new TAgeRec() {GroupName="3y", age=3},
             new TAgeRec() {GroupName="4y", age=4},
             new TAgeRec() {GroupName="5y", age=5},
             new TAgeRec() {GroupName="6y", age=6}},
            {new TAgeRec() {GroupName="Weaner", age=0},    //beef
             new TAgeRec() {GroupName="1y", age=1},
             new TAgeRec() {GroupName="2y", age=2},
             new TAgeRec() {GroupName="3y", age=3},
             new TAgeRec() {GroupName="4y", age=4},
             new TAgeRec() {GroupName="5y", age=5},
             new TAgeRec() {GroupName="6y", age=6}},
            {new TAgeRec() {GroupName="Weaner", age=0},    //lamb
             new TAgeRec() {GroupName="1y", age=1},
             new TAgeRec() {GroupName="2y", age=2},
             new TAgeRec() {GroupName="3y", age=3},
             new TAgeRec() {GroupName="4y", age=4},
             new TAgeRec() {GroupName="5y", age=5},
             new TAgeRec() {GroupName="6y", age=6}}
                                              };

        //array to hold the list of mob names and easy access to their specification
        private const int MOBS = 4;

        //[entWether..entLamb, 0..MOBS-1]  TStockEnterprise.entWether..
        private TMobRec[,] ENTMOBS = {
                {new TMobRec(){MobName="Male",         MobMale=true,  MobPure=true},    //wethers
                 new TMobRec(){MobName="",             MobMale=true,  MobPure=true},
                 new TMobRec(){MobName="",             MobMale=true,  MobPure=true},
                 new TMobRec(){MobName="",             MobMale=true,  MobPure=true}},
                {new TMobRec(){MobName="Female",       MobMale=false, MobPure=true},    //ewe & wether
                 new TMobRec(){MobName="Male",         MobMale=true,  MobPure=true},
                 new TMobRec(){MobName="Young Female", MobMale=false, MobPure=true},
                 new TMobRec(){MobName="Young Male",   MobMale=true,  MobPure=true}},
                {new TMobRec(){MobName="Male",         MobMale=true,  MobPure=true},    //steer
                 new TMobRec(){MobName="",             MobMale=true,  MobPure=true},
                 new TMobRec(){MobName="",             MobMale=true,  MobPure=true},
                 new TMobRec(){MobName="",             MobMale=true,  MobPure=true}},
                {new TMobRec(){MobName="Female",       MobMale=false, MobPure=true},    //beef
                 new TMobRec(){MobName="Male",         MobMale=true,  MobPure=true},
                 new TMobRec(){MobName="Young Female", MobMale=false, MobPure=true},
                 new TMobRec(){MobName="Young Male",   MobMale=true,  MobPure=true}},
                {new TMobRec(){MobName="Female",       MobMale=false, MobPure=true},    //lamb
                 new TMobRec(){MobName="Male",         MobMale=true,  MobPure=true},
                 new TMobRec(){MobName="",             MobMale=true,  MobPure=true},
                 new TMobRec(){MobName="",             MobMale=true,  MobPure=true}} 
                                       };

        //checking paddock for grazing move
        private const int MAX_CRITERIA = 1;
        private const int DRAFT_MOVE = 0;
        private string[] CRITERIA = new string[MAX_CRITERIA] { "draft" };   //used in radiogroup on dialog

        /// <summary>
        /// Initialises a TGenotypeInits so that most parameters revert to their         
        /// defaults.  Can't be done as a constant because MISSING is a typed value      
        /// </summary>
        /// <param name="Genotype"></param>
        public void MakeEmptyGenotype(ref TSingleGenotypeInits Genotype)
        {
            Genotype.sGenotypeName = "Medium Merino";
            Genotype.sDamBreed = "";
            Genotype.sSireBreed = "";
            Genotype.iGeneration = 0;
            Genotype.SRW = StdMath.DMISSING;
            Genotype.PotFleeceWt = StdMath.DMISSING;
            Genotype.MaxFibreDiam = StdMath.DMISSING;
            Genotype.FleeceYield = StdMath.DMISSING;
            Genotype.PeakMilk = StdMath.DMISSING;
            Genotype.DeathRate[FALSE] = StdMath.DMISSING;
            Genotype.DeathRate[TRUE] = StdMath.DMISSING;
            Genotype.Conceptions[1] = StdMath.DMISSING;
            Genotype.Conceptions[2] = 0.0;
            Genotype.Conceptions[3] = 0.0;
        }

        /// <summary>
        /// Utility routines for manipulating the DM_Pool type.  AddDMPool adds the   
        /// contents of two pools together
        /// </summary>
        /// <param name="Pool1"></param>
        /// <param name="Pool2"></param>
        /// <returns></returns>
        protected GrazType.DM_Pool AddDMPool(GrazType.DM_Pool Pool1, GrazType.DM_Pool Pool2)
        {
            int N = (int)GrazType.TOMElement.N;
            int P = (int)GrazType.TOMElement.P;
            int S = (int)GrazType.TOMElement.S;
            GrazType.DM_Pool Result = new GrazType.DM_Pool();
            Result.DM = Pool1.DM + Pool2.DM;
            Result.Nu[N] = Pool1.Nu[N] + Pool2.Nu[N];
            Result.Nu[P] = Pool1.Nu[P] + Pool2.Nu[P];
            Result.Nu[S] = Pool1.Nu[S] + Pool2.Nu[S];
            Result.AshAlk = Pool1.AshAlk + Pool2.AshAlk;

            return Result;
        }

        /// <summary>
        /// MultiplyDMPool scales the contents of a pool                                                                 
        /// </summary>
        /// <param name="Src"></param>
        /// <param name="X"></param>
        /// <returns></returns>
        protected GrazType.DM_Pool MultiplyDMPool(GrazType.DM_Pool Src, double X)
        {
            int N = (int)GrazType.TOMElement.N;
            int P = (int)GrazType.TOMElement.P;
            int S = (int)GrazType.TOMElement.S;
            GrazType.DM_Pool Result = new GrazType.DM_Pool();
            Result.DM = Src.DM * X;
            Result.Nu[N] = Src.Nu[N] * X;
            Result.Nu[P] = Src.Nu[P] * X;
            Result.Nu[S] = Src.Nu[S] * X;
            Result.AshAlk = Src.AshAlk * X;

            return Result;
        }

        private double[] getOffspringRates(TAnimalParamSet Params,
                                    double fLatitude,
                                    int iMateDOY,
                                    int iAgeDays,
                                    double fMatingSize,
                                    double fCondition,
                                    double fChillIndex = 0.0)
        {
            const double NO_CYCLES = 2.5;
            const double STD_LATITUDE = -35.0;                                                      // Latitude (in degrees) for which the      
            //   DayLengthConst[] parameters are set    
            double[] Result;
            double[] Conceptions = new double[4];
            double fEmptyPropn;
            double fDLFactor;
            double fPropn;
            double fExposureOdds;
            double fDeathRate;
            int N;

            fDLFactor = (1.0 - Math.Sin(GrazEnv.DAY2RAD * (iMateDOY + 10)))
                         * Math.Sin(GrazEnv.DEG2RAD * fLatitude) / Math.Sin(GrazEnv.DEG2RAD * STD_LATITUDE);
            for (N = 1; N <= Params.MaxYoung; N++)
            {
                if ((iAgeDays > Params.Puberty[0]) && (Params.ConceiveSigs[N][0] < 5.0))     //Puberty[false]
                    fPropn = StdMath.DIM(1.0, Params.DayLengthConst[N] * fDLFactor)
                              * StdMath.SIG(fMatingSize * fCondition, Params.ConceiveSigs[N]);
                else
                    fPropn = 0.0;

                if (N == 1)
                    Conceptions[N] = fPropn;
                else
                {
                    Conceptions[N] = fPropn * Conceptions[N - 1];
                    Conceptions[N - 1] = Conceptions[N - 1] - Conceptions[N];
                }
            }

            fEmptyPropn = 1.0;
            for (N = 1; N <= Params.MaxYoung; N++)
                fEmptyPropn = fEmptyPropn - Conceptions[N];

            Result = new double[4];
            if (fEmptyPropn < 1.0)
                for (N = 1; N <= Params.MaxYoung; N++)
                    Result[N] = Conceptions[N] * (1.0 - Math.Pow(fEmptyPropn, NO_CYCLES)) / (1.0 - fEmptyPropn);

            if ((fChillIndex > 0) && (Params.Animal == GrazType.AnimalType.Sheep))
            {
                for (N = 1; N <= Params.MaxYoung; N++)
                {
                    fExposureOdds = Params.ExposureConsts[0] - Params.ExposureConsts[1] * fCondition + Params.ExposureConsts[2] * fChillIndex;
                    if (N > 1)
                        fExposureOdds = fExposureOdds + Params.ExposureConsts[3];
                    fDeathRate = Math.Exp(fExposureOdds) / (1.0 + Math.Exp(fExposureOdds));

                    Result[N] = (1.0 - fDeathRate) * Result[N];
                }
            }
            return Result;
        }

        /// <summary>
        /// Add( TAnimalList, TPaddockInfo, integer, integer )                           
        /// Private variant. Adds all members of a TAnimalList back into the stock list  
        /// </summary>
        /// <param name="AnimList"></param>
        /// <param name="PaddInfo"></param>
        /// <param name="iTagVal"></param>
        /// <param name="iPriority"></param>
        public void Add(TAnimalList AnimList,
                                  TPaddockInfo PaddInfo,
                                  int iTagVal,
                                  int iPriority)
        {
            int Idx;

            if (AnimList != null)
                for (Idx = 0; Idx <= AnimList.Count - 1; Idx++)
                {
                    Add(AnimList.At(Idx), PaddInfo, iTagVal, iPriority);
                    AnimList.SetAt(Idx, null);                           // Detach the animal group from the TAnimalList                              
                }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mainParams"></param>
        /// <param name="Inits"></param>
        /// <param name="sName"></param>
        /// <param name="iSearchBefore"></param>
        /// <returns></returns>
        private TAnimalParamSet findGenotype(TAnimalParamSet mainParams, TSingleGenotypeInits[] Inits, string sName, int iSearchBefore)
        {
            TAnimalParamSet Result;
            TAnimalParamSet foundParams;
            int Idx;

            Idx = 0;
            while ((Idx < iSearchBefore) && (sName.ToLower() != Inits[Idx].sGenotypeName.ToLower()))
                Idx++;
            if (Idx < iSearchBefore)
                Result = ParamsFromGenotypeInits(mainParams, Inits, Idx);
            else
            {
                foundParams = mainParams.Match(sName);
                if (foundParams != null)
                    Result = new TAnimalParamSet(null, foundParams);
                else
                    throw new Exception("Breed name \"" + sName + "\" not recognised");
            }
            return Result;
        }

        /// <summary>
        /// Always makes a copy
        /// </summary>
        /// <param name="mainParams"></param>
        /// <param name="Inits"></param>
        /// <param name="iGenotype"></param>
        /// <returns></returns>
        public TAnimalParamSet ParamsFromGenotypeInits(TAnimalParamSet mainParams,
                                          TSingleGenotypeInits[] Inits,
                                          int iGenotype)
        {
            TAnimalParamSet Result;
            //bool bWeaner;

            if (Inits[iGenotype].sDamBreed == String.Empty)
                Result = findGenotype(mainParams, Inits, Inits[iGenotype].sGenotypeName, 0);
            else if (Inits[iGenotype].iGeneration == 0)
                Result = findGenotype(mainParams, Inits, Inits[iGenotype].sDamBreed, 0);
            else
                Result = TAnimalParamSet.CreateFactory(Inits[iGenotype].sGenotypeName,
                                                  findGenotype(mainParams, Inits, Inits[iGenotype].sDamBreed, iGenotype),
                                                  findGenotype(mainParams, Inits, Inits[iGenotype].sSireBreed, iGenotype));

            Result.sName = Inits[iGenotype].sGenotypeName;

            if (bIsGiven(Inits[iGenotype].SRW))
                Result.BreedSRW = Inits[iGenotype].SRW;
            if (bIsGiven(Inits[iGenotype].PotFleeceWt))
                Result.PotentialGFW = Inits[iGenotype].PotFleeceWt;
            if (bIsGiven(Inits[iGenotype].MaxFibreDiam))
                Result.MaxMicrons = Inits[iGenotype].MaxFibreDiam;
            if (bIsGiven(Inits[iGenotype].FleeceYield))
                Result.FleeceYield = Inits[iGenotype].FleeceYield;
            if (bIsGiven(Inits[iGenotype].PeakMilk))
                Result.PotMilkYield = Inits[iGenotype].PeakMilk;
            for (int iWeaner = 0; iWeaner <= 1; iWeaner++)
            {
                if (Inits[iGenotype].DeathRate[iWeaner] != StdMath.DMISSING)                    // A zero death rate is permissible      
                    Result.SetAnnualDeaths(iWeaner == 1, Inits[iGenotype].DeathRate[iWeaner]);
            }
            if (bIsGiven(Inits[iGenotype].Conceptions[1]))
                Result.Conceptions = Inits[iGenotype].Conceptions;

            return Result;
        }
        
        /// <summary>
        /// The main stock management function that handles a number of events.
        /// </summary>
        /// <param name="Model"></param>
        /// <param name="stockEvent">The event parameters</param>
        /// <param name="dtToday"></param>
        /// <param name="fLatitude"></param>
        public void doStockManagement(TStockList Model, IStockEvent stockEvent, int dtToday = 0, double fLatitude = -35.0)
        {
            TCohortsInfo CohortsInfo = new TCohortsInfo();
            TPurchaseInfo PurchaseInfo = new TPurchaseInfo();
            List<string> sClosed;
            string sParam;
            int iParam1;
            int iParam3;
            double fValue;
            int iTag;
            int iGroups;

            if (stockEvent != null)
            {
                if (stockEvent.GetType() == typeof(TStockAdd))          // add_animals
                {
                    TStockAdd stockInfo = (TStockAdd)stockEvent;
                    CohortsInfo.sGenotype = stockInfo.genotype;
                    CohortsInfo.iNumber = Math.Max(0, stockInfo.number);
                    if (!ParseRepro(stockInfo.sex, ref CohortsInfo.ReproClass))
                        throw new Exception("Event ADD does not support sex='" + stockInfo.sex + "'");
                    if (dtToday > 0)
                        CohortsInfo.iAgeOffsetDays = DaysFromDOY365(stockInfo.birth_day, dtToday);
                    else
                        CohortsInfo.iAgeOffsetDays = 0;
                    CohortsInfo.iMinYears = stockInfo.min_years;
                    CohortsInfo.iMaxYears = stockInfo.max_years;
                    CohortsInfo.fMeanLiveWt = stockInfo.mean_weight;
                    CohortsInfo.fCondScore = stockInfo.cond_score;
                    CohortsInfo.fMeanGFW = stockInfo.mean_fleece_wt;
                    if (dtToday > 0)
                        CohortsInfo.iFleeceDays = DaysFromDOY365(stockInfo.shear_day, dtToday);
                    else
                        CohortsInfo.iFleeceDays = 0;
                    CohortsInfo.sMatedTo = stockInfo.mated_to;
                    CohortsInfo.iDaysPreg = stockInfo.pregnant;
                    CohortsInfo.fFoetuses = stockInfo.foetuses;
                    CohortsInfo.iDaysLact = stockInfo.lactating;
                    CohortsInfo.fOffspring = stockInfo.offspring;
                    CohortsInfo.fOffspringWt = stockInfo.young_wt;
                    CohortsInfo.fOffspringCS = stockInfo.young_cond_score;
                    CohortsInfo.fLambGFW = stockInfo.young_fleece_wt;

                    if (CohortsInfo.iNumber > 0)
                        Model.AddCohorts(CohortsInfo, 1 + DaysFromDOY365(1, dtToday), fLatitude, null);
                }

                else if (stockEvent.GetType() == typeof(TStockBuy))
                {
                    TStockBuy stockInfo = (TStockBuy)stockEvent;
                    PurchaseInfo.sGenotype = stockInfo.genotype;
                    PurchaseInfo.Number = Math.Max(0, stockInfo.number);
                    if (!ParseRepro(stockInfo.sex, ref PurchaseInfo.Repro))
                        throw new Exception("Event BUY does not support sex='" + stockInfo.sex + "'");
                    PurchaseInfo.AgeDays = Convert.ToInt32(Math.Round(MONTH2DAY * stockInfo.age));  // Age in months                
                    PurchaseInfo.LiveWt = stockInfo.weight;
                    PurchaseInfo.GFW = stockInfo.fleece_wt;
                    PurchaseInfo.fCondScore = stockInfo.cond_score;
                    PurchaseInfo.sMatedTo = stockInfo.mated_to;
                    PurchaseInfo.Preg = stockInfo.pregnant;
                    PurchaseInfo.Lact = stockInfo.lactating;
                    PurchaseInfo.NYoung = stockInfo.no_young;
                    if ((PurchaseInfo.Preg > 0) || (PurchaseInfo.Lact > 0))
                        PurchaseInfo.NYoung = Math.Max(1, PurchaseInfo.NYoung);
                    PurchaseInfo.YoungWt = stockInfo.young_wt;
                    if ((PurchaseInfo.Lact == 0) || (PurchaseInfo.YoungWt == 0.0))                              // Can't use MISSING as default owing    
                        PurchaseInfo.YoungWt = StdMath.DMISSING;                                                //   to double-to-single conversion      
                    PurchaseInfo.YoungGFW = stockInfo.young_fleece_wt;
                    iTag = stockInfo.usetag;

                    if (PurchaseInfo.Number > 0)
                    {
                        Model.Buy(PurchaseInfo);
                        if (iTag > 0)
                            Model.setTag(Model.Count(), iTag);
                    }
                } //_ buy _

                //sell a number from a group of animals
                else if (stockEvent.GetType() == typeof(TStockSell))
                {
                    TStockSell stockInfo = (TStockSell)stockEvent;
                    Model.Sell(stockInfo.group, stockInfo.number);
                }
                //sell a number of animals tagged with a specific tag 
                else if (stockEvent.GetType() == typeof(TStockSellTag))
                {
                    TStockSellTag stockInfo = (TStockSellTag)stockEvent;
                    Model.SellTag(stockInfo.tag, stockInfo.number);
                }

                else if (stockEvent.GetType() == typeof(TStockShear))
                {
                    TStockShear stockInfo = (TStockShear)stockEvent;
                    sParam = stockInfo.sub_group.ToLower();
                    Model.Shear(stockInfo.group, ((sParam == "adults") || (sParam == "both") || (sParam == "")), ((sParam == "lambs") || (sParam == "both")));
                }

                else if (stockEvent.GetType() == typeof(TStockMove))
                {
                    TStockMove stockInfo = (TStockMove)stockEvent;
                    iParam1 = stockInfo.group;
                    if ((iParam1 >= 1) && (iParam1 <= Model.Count()))
                        Model.setInPadd(iParam1, stockInfo.paddock);
                    else
                        throw new Exception("Invalid group number in MOVE event");
                }

                else if (stockEvent.GetType() == typeof(TStockJoin))
                {
                    TStockJoin stockInfo = (TStockJoin)stockEvent;
                    Model.Join(stockInfo.group, stockInfo.mate_to, stockInfo.mate_days);
                }
                else if (stockEvent.GetType() == typeof(TStockCastrate))
                {
                    TStockCastrate stockInfo = (TStockCastrate)stockEvent;
                    Model.Castrate(stockInfo.group, stockInfo.number);
                }
                else if (stockEvent.GetType() == typeof(TStockWean))
                {
                    TStockWean stockInfo = (TStockWean)stockEvent;
                    iParam1 = stockInfo.group;
                    sParam = stockInfo.sex.ToLower();
                    iParam3 = stockInfo.number;

                    if (sParam == "males")
                        Model.Wean(iParam1, iParam3, false, true);
                    else if (sParam == "females")
                        Model.Wean(iParam1, iParam3, true, false);
                    else if ((sParam == "all") || (sParam == ""))
                        Model.Wean(iParam1, iParam3, true, true);
                    else
                        throw new Exception("Invalid offspring type \"" + sParam + "\" in WEAN event");
                }

                else if (stockEvent.GetType() == typeof(TStockDryoff))
                {
                    TStockDryoff stockInfo = (TStockDryoff)stockEvent;
                    Model.DryOff(stockInfo.group, stockInfo.number);
                }
                //split off the requested animals from all groups
                else if (stockEvent.GetType() == typeof(TStockSplitAll))
                {
                    TStockSplitAll stockInfo = (TStockSplitAll)stockEvent;
                    iGroups = Model.Count(); //get pre-split count of groups
                    for (iParam1 = 1; iParam1 <= iGroups; iParam1++)
                    {
                        sParam = stockInfo.type.ToLower();
                        fValue = stockInfo.value;
                        iTag = stockInfo.othertag;

                        if (sParam == "age")
                            Model.SplitAge(iParam1, Convert.ToInt32(Math.Round(fValue)));
                        else if (sParam == "weight")
                            Model.SplitWeight(iParam1, fValue);
                        else if (sParam == "young")
                            Model.SplitYoung(iParam1);
                        else if (sParam == "number")
                            Model.Split(iParam1, Convert.ToInt32(Math.Round(fValue)));
                        else
                            throw new Exception("Stock: invalid keyword (" + sParam + ") in \"split\" event");
                        if ((iTag > 0) && (Model.Count() > iGroups))     //if a tag for any new group is given
                            Model.setTag(Model.Count(), iTag);
                    }
                }

                //split off the requested animals from one group
                else if (stockEvent.GetType() == typeof(TStockSplit))
                {
                    TStockSplit stockInfo = (TStockSplit)stockEvent;
                    iGroups = Model.Count(); //get pre-split count of groups
                    iParam1 = stockInfo.group;
                    sParam = stockInfo.type.ToLower();
                    fValue = stockInfo.value;
                    iTag = stockInfo.othertag;

                    if ((iParam1 < 1) && (iParam1 > Model.Count()))
                        throw new Exception("Invalid group number in SPLIT event");
                    else if (sParam == "age")
                        Model.SplitAge(iParam1, Convert.ToInt32(Math.Round(fValue)));
                    else if (sParam == "weight")
                        Model.SplitWeight(iParam1, fValue);
                    else if (sParam == "young")
                        Model.SplitYoung(iParam1);
                    else if (sParam == "number")
                        Model.Split(iParam1, Convert.ToInt32(Math.Round(fValue)));
                    else
                        throw new Exception("Stock: invalid keyword (" + sParam + ") in \"split\" event");
                    if ((iTag > 0) && (Model.Count() > iGroups))     //if a tag for the new group is given
                        Model.setTag(Model.Count(), iTag);
                }

                else if (stockEvent.GetType() == typeof(TStockTag))
                {
                    TStockTag stockInfo = (TStockTag)stockEvent;
                    iParam1 = stockInfo.group;
                    if ((iParam1 >= 1) && (iParam1 <= Model.Count()))
                        Model.setTag(iParam1, stockInfo.value);
                    else
                        throw new Exception("Invalid group number in TAG event");
                }

                else if (stockEvent.GetType() == typeof(TStockSort))
                {
                    Model.Sort();
                }

                else if (stockEvent.GetType() == typeof(TStockPrioritise))
                {
                    TStockPrioritise stockInfo = (TStockPrioritise)stockEvent;
                    iParam1 = stockInfo.group;
                    if ((iParam1 >= 1) && (iParam1 <= Model.Count()))
                        Model.setPriority(iParam1, stockInfo.value);
                    else
                        throw new Exception("Invalid group number in PRIORITISE event");
                }

                else if (stockEvent.GetType() == typeof(TStockDraft))
                {
                    TStockDraft stockInfo = (TStockDraft)stockEvent;
                    sClosed = new List<string>(stockInfo.closed);
                    
                    Model.Draft(sClosed);
                }

                else
                    throw new Exception("Event not recognised in STOCK");
            }
        }
    }
}
