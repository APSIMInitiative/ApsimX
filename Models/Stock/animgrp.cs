using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Reflection;
using Models.Grazplan;
using StdUnits;

namespace Models.Stock
{
    /// <summary>
    /// Record containing the different sources from which an animal acquires energy, protein etc                                
    /// </summary>
    [Serializable]
    public struct TDietRecord                                                             
    {                                                                                   
        /// <summary>
        /// 
        /// </summary>
        public double Herbage;                                                                                         
        /// <summary>
        /// 
        /// </summary>
        public double Supp;
        /// <summary>
        /// 
        /// </summary>
        public double Milk;
        /// <summary>
        /// "Solid" is herbage and supplement taken together
        /// </summary>
        public double Solid;                                                                
        /// <summary>
        /// 
        /// </summary>
        public double Total;                                                                                          
    }

    /// <summary>
    /// Allocation of energy, protein etc for:
    /// </summary>
    [Serializable]
    public struct TPhysiolRecord
    {                       
        /// <summary>
        /// Basal metab.+movement+digestion+cold
        /// </summary>
        public double Maint;                                                             
        /// <summary>
        /// Pregnancy
        /// </summary>
        public double Preg;                                                              
        /// <summary>
        /// Lactation
        /// </summary>
        public double Lact;                                                                  
        /// <summary>
        /// Wool growth (sheep only)
        /// </summary>
        public double Wool;                                                                                
        /// <summary>
        /// Weight gain (after efficiency losses)
        /// </summary>
        public double Gain;                                                              
        /// <summary>
        /// Basal metabolism
        /// </summary>
        public double Metab;
        /// <summary>
        /// Heat production in the cold
        /// </summary>
        public double Cold;                                                              
        /// <summary>
        /// 
        /// </summary>
        public double Total;
    }

    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class TAnimalOutput                                                                                               
    {
        /// <summary>
        /// Potential intake, after correction for legume content of the diet
        /// </summary>
        public double IntakeLimitLegume;           
        /// <summary>
        /// Intakes for interface with pasture model
        /// </summary>
        public GrazType.TGrazingOutputs IntakePerHead = new GrazType.TGrazingOutputs();
        // ...... Intake-related values - exclude grain that passes the gut undamaged. ......
        /// <summary>
        /// Intakes summarised for use in the nutrition model
        /// </summary>
        public GrazType.IntakeRecord PaddockIntake = new GrazType.IntakeRecord();                  
        /// <summary>
        /// Intakes summarised for use in the nutrition model
        /// </summary>
        public GrazType.IntakeRecord SuppIntake = new GrazType.IntakeRecord();                                        
        /// <summary>
        /// Daily dry matter intake (kg) - not milk
        /// </summary>
        public TDietRecord DM_Intake = new TDietRecord();                                  
        /// <summary>
        /// Daily crude protein intake (kg)
        /// </summary>
        public TDietRecord CP_Intake = new TDietRecord();                                  
        /// <summary>
        /// Daily phosphorus intake (kg)
        /// </summary>
        public TDietRecord Phos_Intake = new TDietRecord();                                
        /// <summary>
        /// Daily sulphur intake (kg)
        /// </summary>
        public TDietRecord Sulf_Intake = new TDietRecord();                                                
        /// <summary>
        /// Metabolizable energy intake (MJ)
        /// </summary>
        public TDietRecord ME_Intake = new TDietRecord();                                  
        /// <summary>
        /// Digestibility of diet components (0-1)
        /// </summary>
        public TDietRecord Digestibility = new TDietRecord();                                 
        /// <summary>
        /// Crude protein concentrations (0-1)
        /// </summary>
        public TDietRecord ProteinConc = new TDietRecord();                                       
        /// <summary>
        /// ME:dry matter ratios (MJ/kg)
        /// </summary>
        public TDietRecord ME_2_DM = new TDietRecord();                                                 
        /// <summary>
        /// Proportion of each component in the diet 
        /// </summary>
        public TDietRecord DietPropn = new TDietRecord();                                  
        /// <summary>
        /// Degradability of protein in diet (0-1), corrected 
        /// </summary>
        public TDietRecord CorrDgProt = new TDietRecord();                                 
        //..................................................................}
        /// <summary>
        /// Microbial crude protein (kg)
        /// </summary>
        public double MicrobialCP;                                                                      
        /// <summary>
        /// Digestible protein leaving the stomach (kg): total
        /// </summary>
        public double DPLS;                                                                   
        /// <summary>
        /// Digestible protein leaving the stomach (kg): from milk
        /// </summary>
        public double DPLS_Milk;                                                             
        /// <summary>
        /// Digestible protein leaving the stomach (kg): from MCP
        /// </summary>
        public double DPLS_MCP;
        /// <summary>
        /// DPLS available for wool growth (kg)
        /// </summary>
        public double DPLS_Avail_Wool;                                                           
        /// <summary>
        /// Intake of undegradable protein (kg)
        /// </summary>
        public double UDP_Intake;                                                                
        /// <summary>
        /// Digestibility of UDP (0-1)
        /// </summary>
        public double UDP_Dig;        
        /// <summary>
        /// Requirement for UDP (kg)
        /// </summary>
        public double UDP_Reqd;                                                                             
        /// <summary>
        /// Daily intake for RDP (kg)
        /// </summary>
        public double RDP_Intake;                                                            
        /// <summary>
        /// Daily requirement for RDP (kg)
        /// </summary>
        public double RDP_Reqd;
        // Allocation of energy and protein to various uses.                          
        /// <summary>
        /// Allocation of energy
        /// </summary>
        public TPhysiolRecord EnergyUse = new TPhysiolRecord();                                   
        /// <summary>
        /// Allocation of protein
        /// </summary>
        public TPhysiolRecord ProteinUse = new TPhysiolRecord();                                                        
        /// <summary>
        /// 
        /// </summary>
        public TPhysiolRecord Phos_Use = new TPhysiolRecord();
        /// <summary>
        /// 
        /// </summary>
        public TPhysiolRecord Sulf_Use = new TPhysiolRecord();
        /// <summary>
        /// Efficiencies of ME use (0-1)
        /// </summary>
        public TPhysiolRecord Efficiency = new TPhysiolRecord();                                        

        /// <summary>
        /// Endogenous faecal losses      (N,S,P)
        /// </summary>
        public GrazType.DM_Pool EndoFaeces = new GrazType.DM_Pool();                           
        /// <summary>
        /// Total organic faecal losses   (DM,N,S,P)
        /// </summary>
        public GrazType.DM_Pool OrgFaeces = new GrazType.DM_Pool();                        
        /// <summary>
        /// Total inorganic faecal losses (N,S,P)
        /// </summary>
        public GrazType.DM_Pool InOrgFaeces = new GrazType.DM_Pool();                      
        /// <summary>
        /// Total urinary losses of       (N,S,P)
        /// </summary>
        public GrazType.DM_Pool Urine = new GrazType.DM_Pool();                            
        /// <summary>
        /// N in dermal losses (kg)
        /// </summary>
        public double DermalNLoss;                                                                           

        /// <summary>
        /// 
        /// </summary>
        public double GainEContent;
        /// <summary>
        /// 
        /// </summary>
        public double GainPContent;
        /// <summary>
        /// Increase in conceptus weight (kg/d)
        /// </summary>
        public double ConceptusGrowth;                                                     
        /// <summary>
        /// Net energy retained in wool (MJ)
        /// </summary>
        public double TotalWoolEnergy;                                                     
        /// <summary>
        /// Thermoneutral heat production (MJ)
        /// </summary>
        public double Therm0HeatProdn;                                                     
        /// <summary>
        /// Lower critical temperature from the chilling submodel (oC)      
        /// </summary>
        public double LowerCritTemp;                                                       

        /// <summary>
        /// 
        /// </summary>
        public double RDP_IntakeEffect;

        /// <summary>
        /// Copy a TAnimalOutput object
        /// </summary>
        /// <returns></returns>
        public TAnimalOutput Copy()
        {
            return ObjectCopier.Clone(this);
        }
    }

    /// <summary>
    /// An age list item
    /// </summary>
    [Serializable]
    public struct TAgeListElement
    {
        /// <summary>
        /// Age in days
        /// </summary>
        public int iAgeDays;
        /// <summary>
        /// Number of males
        /// </summary>
        public int iNoMales;
        /// <summary>
        /// Number of females
        /// </summary>
        public int iNoFemales;
    }
    #region TAgeList
    // ======================================================================
    /// <summary>
    /// An agelist
    /// </summary>
    [Serializable]
    public class TAgeList
    {
        private TAgeListElement[] FData;
        private void setCount(int iValue)
        {
            Array.Resize(ref FData, iValue);
        }

        /// <summary>
        /// Gets rid of empty elements of a TAgeList                                  
        /// </summary>
        public void Pack()
        {
            int Idx, Jdx;

            Idx = 0;
            while (Idx < Count)
            {
                if ((FData[Idx].iNoMales > 0) || (FData[Idx].iNoFemales > 0))
                    Idx++;
                else
                {
                    for (Jdx = Idx + 1; Jdx <= Count - 1; Jdx++)
                        FData[Jdx - 1] = FData[Jdx];
                    setCount(Count - 1);
                }
            }
        }
        /// <summary>
        /// Random number factory instance
        /// </summary>
        public TMyRandom RandFactory;
        /// <summary>
        /// TAgeList constructor
        /// </summary>
        /// <param name="RandomFactory">An instance of a random number object</param>
        public TAgeList(TMyRandom RandomFactory)
        {
            RandFactory = RandomFactory;
            setCount(0);
        }
        /// <summary>
        /// CreateCopy
        /// </summary>
        /// <param name="srcList"></param>
        /// <param name="RandomFactory"></param>
        public TAgeList(TAgeList srcList, TMyRandom RandomFactory)
        {
            int Idx;

            RandFactory = RandomFactory;
            setCount(srcList.Count);
            for (Idx = 0; Idx <= srcList.Count - 1; Idx++)
                FData[Idx] = srcList.FData[Idx];
        }
        /* constructor Load(  Stream    : TStream;
                           bUseSmall : Boolean;
                           RandomFactory: TMyRandom );
        procedure   Store( Stream    : TStream  ); */
        /// <summary>
        /// Items in the age list
        /// </summary>
        public int Count
        {
            get { return FData.Length; }
        }
        /// <summary>
        /// { Used instead of Add or Insert to add data to the age list.  The Input     
        /// method ensures that there are no duplicate ages in the list and that it   
        /// is maintained in increasing order of age                                  
        /// </summary>
        /// <param name="A"></param>
        /// <param name="NM"></param>
        /// <param name="NF"></param>
        public void Input(int A, int NM, int NF)
        {
            int iPos, Idx;

            iPos = 0;
            while ((iPos < Count) && (FData[iPos].iAgeDays < A))
                iPos++;
            if ((iPos < Count) && (FData[iPos].iAgeDays == A))                     // If we find A already in the list, then   
            {                                                                     //   increment the corresponding numbers of 
                FData[iPos].iNoMales += NM;                                      //   animals                                
                FData[iPos].iNoFemales += NF;
            }
            else                                                                      // Otherwise insert a new element in the    
            {                                                                     //   correct place                          
                setCount(Count + 1);
                for (Idx = Count - 1; Idx >= iPos + 1; Idx--)
                    FData[Idx] = FData[Idx - 1];
                FData[iPos].iAgeDays = A;
                FData[iPos].iNoMales = NM;
                FData[iPos].iNoFemales = NF;
            }
        }
        /// <summary>
        /// Change the numbers of male and female animals to new values.              
        /// Parameters:                                                               
        ///   NM   New total number of male animals to place in the list              
        ///   NF   New total number of female animals to place in the list            
        /// </summary>
        /// <param name="NM"></param>
        /// <param name="NF"></param>
        public void Resize(int NM, int NF)
        {
            int CurrM = 0;
            int CurrF = 0;
            int MLeft;
            int FLeft;
            int Idx;

            Pack();                                                                     // Ensure there are no empty list members   }

            if (Count == 0)                                                        // Hard to do anything with no age info     }
                Input(365 * 3, NM, NF);
            else if (Count == 1)
            {
                FData[0].iNoMales = NM;
                FData[0].iNoFemales = NF;
            }
            else
            {
                GetOlder(-1, ref CurrM, ref CurrF);                                           // Work out number of animals currently in  }
                MLeft = NM;                                                            //   the list                               }
                FLeft = NF;
                for (Idx = 0; Idx <= Count - 1; Idx++)
                {
                    if ((NM == 0) || (CurrM > 0))
                        FData[Idx].iNoMales = Convert.ToInt32(Math.Truncate(NM * StdMath.XDiv(FData[Idx].iNoMales, CurrM)));
                    else
                        FData[Idx].iNoMales = Convert.ToInt32(Math.Truncate(NM * StdMath.XDiv(FData[Idx].iNoFemales, CurrF)));
                    if ((NF == 0) || (CurrF > 0))
                        FData[Idx].iNoFemales = Convert.ToInt32(Math.Truncate(NF * StdMath.XDiv(FData[Idx].iNoFemales, CurrF)));
                    else
                        FData[Idx].iNoFemales = Convert.ToInt32(Math.Truncate(NF * StdMath.XDiv(FData[Idx].iNoMales, CurrM)));
                    MLeft -= FData[Idx].iNoMales;
                    FLeft -= FData[Idx].iNoFemales;
                }

                Idx = Count - 1;                                                         // Add the "odd" animals into the oldest    }
                while ((MLeft > 0) || (FLeft > 0))                                      //   groups as evenly as possible           }
                {
                    if (MLeft > 0)
                    {
                        FData[Idx].iNoMales++;
                        MLeft--;
                    }
                    if (FLeft > 0)
                    {
                        FData[Idx].iNoFemales++;
                        FLeft--;
                    }

                    Idx--;
                    if (Idx < 0)
                        Idx = Count - 1;
                }
            }
            Pack();
        }
        /// <summary>
        /// Set the count of items to 0
        /// </summary>
        public void Clear()
        {
            setCount(0);
        }
        /// <summary>
        /// Add all elements of OtherAges into the object.  Unlike TAnimalGroup.Merge,
        /// TAgeList.Merge does not free OtherAges.                                   
        /// </summary>
        /// <param name="OtherAges"></param>
        public void Merge(TAgeList OtherAges)
        {
            int Idx;

            for (Idx = 0; Idx <= OtherAges.Count - 1; Idx++)
            {
                Input(OtherAges.FData[Idx].iAgeDays,
                       OtherAges.FData[Idx].iNoMales,
                       OtherAges.FData[Idx].iNoFemales);
            }
        }
        /// <summary>
        /// Split the age group by age. If ByAge=TRUE, oldest animals are placed in the result.
        /// If ByAge=FALSE, the age structures are made the same as far as possible.
        /// </summary>
        /// <param name="NM"></param>
        /// <param name="NF"></param>
        /// <param name="ByAge">Split by age</param>
        /// <returns></returns>
        public TAgeList Split(int NM, int NF, bool ByAge)
        {
            int[,] TransferNo;                                          // 0,x =male, 1,x =female                         
            int[] TotalNo = new int[2];
            int[] TransfersReqd = new int[2];
            int[] TransfersDone = new int[2];
            double[] TransferPropn = new double[2];
            int iAnimal, iFirst, iLast;
            int Idx, Jdx;

            TAgeList Result = new TAgeList(RandFactory);                // Create a list with the same age          
            for (Idx = 0; Idx <= Count - 1; Idx++)                      //   structure but no animals               
                Result.Input(this.FData[Idx].iAgeDays, 0, 0);

            TransfersReqd[0] = NM;
            TransfersReqd[1] = NF;
            TransferNo = new int[2, Count];                             // Assume that this zeros TransferNo        

            for (Jdx = 0; Jdx <= 1; Jdx++)
                TransfersDone[Jdx] = 0;

            if (ByAge)                                                  // If ByAge=TRUE, oldest animals are placed 
            {                                                           //   in Result                           
                for (Idx = Count - 1; Idx >= 0; Idx--)
                {
                    TransferNo[0, Idx] = Math.Min(TransfersReqd[0] - TransfersDone[0], FData[Idx].iNoMales);
                    TransferNo[1, Idx] = Math.Min(TransfersReqd[1] - TransfersDone[1], FData[Idx].iNoFemales);
                    for (Jdx = 0; Jdx <= 1; Jdx++)
                        TransfersDone[Jdx] += TransferNo[Jdx, Idx];
                }
            }
            else                                                                  // If ByAge=FALSE, the age structures are   
            {                                                                     //   made the same as far as possible       
                GetOlder(-1, ref TotalNo[0], ref TotalNo[1]);

                for (Jdx = 0; Jdx <= 1; Jdx++)
                {
                    TransfersReqd[Jdx] = Math.Min(TransfersReqd[Jdx], TotalNo[Jdx]);
                    TransferPropn[Jdx] = StdMath.XDiv(TransfersReqd[Jdx], TotalNo[Jdx]);
                }

                for (Idx = 0; Idx <= Count - 1; Idx++)
                {
                    TransferNo[0, Idx] = Convert.ToInt32(Math.Round(TransferPropn[0] * FData[Idx].iNoMales));
                    TransferNo[1, Idx] = Convert.ToInt32(Math.Round(TransferPropn[1] * FData[Idx].iNoFemales));
                    for (Jdx = 0; Jdx <= 1; Jdx++)
                        TransfersDone[Jdx] += TransferNo[Jdx, Idx];
                }

                for (Jdx = 0; Jdx <= 1; Jdx++)                                          // Randomly allocate roundoff errors        
                {
                    while (TransfersDone[Jdx] < TransfersReqd[Jdx])                     // Too few transfers                        
                    {
                        iAnimal = Convert.ToInt32(Math.Min(Math.Truncate(RandFactory.MyRandom() * (TotalNo[Jdx] - TransfersDone[Jdx])),
                                        (TotalNo[Jdx] - TransfersDone[Jdx]) - 1));
                        Idx = -1;
                        iLast = 0;
                        do
                        {
                            Idx++;
                            iFirst = iLast;
                            if (Jdx == 0)
                                iLast = iFirst + (FData[Idx].iNoMales - TransferNo[Jdx, Idx]);
                            else
                                iLast = iFirst + (FData[Idx].iNoFemales - TransferNo[Jdx, Idx]);
                            //until (Idx = Count-1) or ((iAnimal >= iFirst) and (iAnimal < iLast));
                        } while ((Idx != Count - 1) && ((iAnimal < iFirst) || (iAnimal >= iLast)));

                        TransferNo[Jdx, Idx]++;
                        TransfersDone[Jdx]++;
                    }

                    while (TransfersDone[Jdx] > TransfersReqd[Jdx])           // Too many transfers                       
                    {
                        iAnimal = Convert.ToInt32(Math.Min(Math.Truncate(RandFactory.MyRandom() * TransfersDone[Jdx]),
                                        TransfersDone[Jdx] - 1));
                        Idx = -1;
                        iLast = 0;
                        do
                        {
                            Idx++;
                            iFirst = iLast;
                            iLast = iFirst + TransferNo[Jdx, Idx];
                            //until (Idx = Count-1) or ((iAnimal >= iFirst) and (iAnimal < iLast));
                        } while ((Idx != Count - 1) && ((iAnimal < iFirst) || (iAnimal >= iLast)));

                        TransferNo[Jdx, Idx]--;
                        TransfersDone[Jdx]--;
                    }
                }
            }

            for (Idx = 0; Idx <= Count - 1; Idx++)                                                // Carry out transfers                      
            {
                this.FData[Idx].iNoMales -= TransferNo[0, Idx];
                Result.FData[Idx].iNoMales += TransferNo[0, Idx];
                this.FData[Idx].iNoFemales -= TransferNo[1, Idx];
                Result.FData[Idx].iNoFemales += TransferNo[1, Idx];
            }

            this.Pack();                                                                // Clear away empty entries in both lists   
            Result.Pack();

            return Result;
        }

        /// <summary>
        /// Increase all ages by the same amount (NoDays)                             
        /// </summary>
        /// <param name="NoDays"></param>
        public void AgeBy(int NoDays)
        {
            int Idx;

            for (Idx = 0; Idx <= Count - 1; Idx++)
                FData[Idx].iAgeDays += NoDays;
        }

        /// <summary>
        /// Compute the mean age of all animals in the list                           
        /// </summary>
        /// <returns></returns>
        public int MeanAge()
        {
            double AxN, N;
            double dN;
            int Idx;
            int Result;

            if (Count == 1)
                Result = FData[0].iAgeDays;
            else if (Count == 0)
                Result = 0;
            else
            {
                AxN = 0;
                N = 0;
                for (Idx = 0; Idx <= Count - 1; Idx++)
                {
                    dN = FData[Idx].iNoMales + FData[Idx].iNoFemales;
                    AxN = AxN + dN * FData[Idx].iAgeDays;
                    N = N + dN;
                }
                if (N > 0.0)
                    Result = Convert.ToInt32(Math.Round(AxN / N));
                else
                    Result = 0;
            }
            return Result;
        }

        /// <summary>
        /// Returns the number of male and female animals (NM and NF, respectively)     
        /// which are aged greater than A days                                        
        /// </summary>
        /// <param name="A"></param>
        /// <param name="NM"></param>
        /// <param name="NF"></param>
        public void GetOlder(int A, ref int NM, ref int NF)
        {
            int Idx;

            NM = 0;
            NF = 0;
            for (Idx = 0; Idx <= Count - 1; Idx++)
            {
                if (FData[Idx].iAgeDays > A)
                {
                    NM += FData[Idx].iNoMales;
                    NF += FData[Idx].iNoFemales;
                }
            }
        }
    }
    #endregion TAgeList

    /// <summary>
    /// Set of differences between two sub-groups of animals.  Used in the Split  
    /// method of AnimalGroup                                                     
    /// </summary>
    [Serializable]
    public struct TDifferenceRecord
    {
        /// <summary>
        /// Standard reference weight
        /// </summary>
        public double StdRefWt;
        /// <summary>
        /// Base weight
        /// </summary>
        public double BaseWeight;
        /// <summary>
        /// Fleece weight
        /// </summary>
        public double FleeceWt;
    }

    /// <summary>
    /// Climatic inputs to the animal model                                       
    /// </summary>
    [Serializable]
    public struct TAnimalWeather
    {
        /// <summary>
        /// Latitude (degrees, +ve=north)
        /// </summary>
        public double Latitude;                             
        /// <summary>
        /// Date at which environment prevails
        /// </summary>
        public int TheDay;                                         
        /// <summary>
        /// Maximum air temperature (deg C)
        /// </summary>
        public double MaxTemp;                                         
        /// <summary>
        /// Minimum air temperature (deg C)
        /// </summary>
        public double MinTemp;                                         
        /// <summary>
        /// Mean of MaxTemp and MinTemp
        /// </summary>
        public double MeanTemp;                                            
        /// <summary>
        /// Precipitation (mm)
        /// </summary>
        public double Precipitation;                                                
        /// <summary>
        /// Average daily windspeed (m/s)
        /// </summary>
        public double WindSpeed;                                         
        /// <summary>
        /// Daylength including civil twilight (hr)
        /// </summary>
        public double DayLength;                               
    }

    /// <summary>
    /// TStateInfo type. Information required to reset the state in the case of RDP insufficiency                                                                
    /// </summary>
    public struct TStateInfo
    {
        /// <summary>
        /// Base weight without wool
        /// </summary>
        public double fBaseWeight;
        /// <summary>
        /// Weight of wool
        /// </summary>
        public double fWoolWt;
        /// <summary>
        /// Wool microns
        /// </summary>
        public double fWoolMicron;
        /// <summary>
        /// Depth of coat
        /// </summary>
        public double fCoatDepth;
        /// <summary>
        /// Foetal weight
        /// </summary>
        public double fFoetalWt;
        /// <summary>
        /// 
        /// </summary>
        public double fLactAdjust;
        /// <summary>
        /// 
        /// </summary>
        public double fLactRatio;
        /// <summary>
        /// 
        /// </summary>
        public double fBasePhos;
        /// <summary>
        /// 
        /// </summary>
        public double fBaseSulf;
    }

    /// <summary>
    /// TExcretionInfo type. Totalled amounts of excretion                           
    /// </summary>
    public class TExcretionInfo
    {
        /// <summary>
        /// Organic faeces pool
        /// </summary>
        public GrazType.DM_Pool OrgFaeces = new GrazType.DM_Pool();
        /// <summary>
        /// Inorganic faeces pool
        /// </summary>
        public GrazType.DM_Pool InOrgFaeces = new GrazType.DM_Pool();
        /// <summary>
        /// Urine pool
        /// </summary>
        public GrazType.DM_Pool Urine = new GrazType.DM_Pool();

        /// <summary>
        /// Number in the time step by all animals (not including unweaned young)
        /// </summary>
        public double dDefaecations;   
        /// <summary>
        /// Volume per defaecation, m^3 (fresh basis)
        /// </summary>
        public double dDefaecationVolume;   
        /// <summary>
        /// Area per defaecation, m^2 (fresh basis)
        /// </summary>
        public double dDefaecationArea;   
        /// <summary>
        /// Eccentricity of faeces
        /// </summary>
        public double dDefaecationEccentricity;   
        /// <summary>
        /// Proportion of faecal inorganic N that is nitrate
        /// </summary>
        public double dFaecalNO3Propn;   
        /// <summary>
        /// Number in the time step by all animals (not including unweaned young)
        /// </summary>
        public double dUrinations;   
        /// <summary>
        /// Fluid volume per urination, m^3
        /// </summary>
        public double dUrinationVolume;   
        /// <summary>
        /// Area covered by each urination at the soil surface, m^2
        /// </summary>
        public double dUrinationArea;   
        /// <summary>
        /// Eccentricity of urinations
        /// </summary>
        public double dUrinationEccentricity;   
    }
    #region TAnimalGroup
    // =============================================================================================
    /// <summary>
    /// TAnimalGroup class
    /// </summary>
    [Serializable]
    public class TAnimalGroup
    {
        /// <summary>
        /// AnimalsDynamicGlb differentiates between the "static" version of the      
        /// model used in GrazFeed and the "dynamic" version used elsewhere           
        /// </summary>
        public const bool AnimalsDynamicGlb = true;
        /// <summary>
        /// Represents no difference
        /// </summary>
        public TDifferenceRecord NODIFF = new TDifferenceRecord() { StdRefWt = 0, BaseWeight = 0, FleeceWt = 0 };
        /// <summary>
        /// 
        /// </summary>
        public const int LatePregLength = 42;
        /// <summary>
        /// Depth of wool left after shearing (cm)
        /// </summary>
        public const double STUBBLE_MM = 0.5;                                                        
        /// <summary>
        /// This animal's parameters
        /// </summary>
        protected TAnimalParamSet AParams;
        /// <summary>
        /// Paramters of the animal mated to
        /// </summary>
        protected TAnimalParamSet FMatedTo;
        /// <summary>
        /// Distribution of ages
        /// </summary>
        protected TAgeList Ages;                                                                 
        /// <summary>
        /// Mean age of all animals (days)
        /// </summary>
        protected int MeanAge;                                                         
        /// <summary>
        /// Number of male animals in the group 
        /// </summary>
        protected int NoMales;                  
        /// <summary>
        /// Number of female animals in the group
        /// </summary>
        protected int NoFemales;                                                                              
        /// <summary>
        /// All weights in kg
        /// </summary>
        protected double TotalWeight;                                                               
        /// <summary>
        /// Greasy fleece weight (including stubble)
        /// </summary>
        protected double WoolWt;                                             
        /// <summary>
        /// Growth of greasy fleece (kg/d)           
        /// </summary>
        protected double DeltaWoolWt;                                       
        /// <summary>
        /// Average fibre diameter (microns)         
        /// </summary>
        protected double WoolMicron;                                        
        /// <summary>
        /// Diameter of new wool (microns)           
        /// </summary>
        protected double DeltaWoolMicron;   
        /// <summary>
        /// Reproduction status
        /// </summary>
        protected GrazType.ReproType ReproStatus;
        /// <summary>
        /// Lactation status
        /// </summary>
        protected GrazType.LactType LactStatus;
        /// <summary>
        /// Number of foetuses
        /// </summary>
        protected int FNoFoetuses;
        /// <summary>
        /// Number of offspring
        /// </summary>
        protected int FNoOffspring;
        /// <summary>
        /// Previous offspring
        /// </summary>
        protected int FPrevOffspring;

        /// <summary>
        /// The mothers animal group
        /// </summary>
        protected TAnimalGroup Mothers;

        /// <summary>
        /// Day in the mating cycle; -1 if not mating
        /// </summary>
        protected int MateCycle;                                        
        /// <summary>
        /// Days left in joining period
        /// </summary>
        protected int DaysToMate;                                                      
        /// <summary>
        /// Days since conception
        /// </summary>
        protected int FoetalAge;                                                            
        /// <summary>
        /// Weight of foetus 
        /// </summary>
        protected double FoetalWt;                                                              
        /// <summary>
        /// Base weight 42 days before parturition   
        /// </summary>
        protected double MidLatePregWt;                                 

        /// <summary>
        /// Fleece-free, conceptus-free weight (kg)
        /// </summary>
        protected double BasalWeight;                                     
        /// <summary>
        /// Change in BaseWeight (kg/d)
        /// </summary>
        protected double DeltaBaseWeight;                                              
        /// <summary>
        /// Highest previous weight (kg)
        /// </summary>
        protected double MaxPrevWt;                                                   
        /// <summary>
        /// Hair or fleece depth (cm)
        /// </summary>
        protected double FCoatDepth;                                                    
        /// <summary>
        /// Value of Condition at parturition
        /// </summary>
        protected double ConditionAtBirthing;                                    
        /// <summary>
        /// Phosphorus in base weight (kg)
        /// </summary>
        protected double BasePhos;                                                 
        /// <summary>
        /// Sulphur in base weight (kg)
        /// </summary>
        protected double BaseSulf;                                                    
        /// <summary>
        /// Relative size
        /// </summary>
        protected double Size;                                                                      
        /// <summary>
        /// Relative condition
        /// </summary>
        protected double Condition;                                                            
        /// <summary>
        /// Weight of these animals at birth (kg)
        /// </summary>
        protected double BirthWt;                                            
        /// <summary>
        /// Standard reference weight of the group
        /// </summary>
        protected double StdRefWt;                                          
        /// <summary>
        /// Normal weight (kg)
        /// </summary>
        protected double NormalWt;                                                              
        /// <summary>
        /// Days since parturition (if lactating)
        /// </summary>
        protected int DaysLactating;                                         
        /// <summary>
        /// Milk production (MJ)
        /// </summary>
        protected double Milk_MJProdn;                                                       
        /// <summary>
        /// Protein in milk production (kg)
        /// </summary>
        protected double Milk_ProtProdn;                                          
        /// <summary>
        /// 
        /// </summary>
        protected double Milk_PhosProdn;
        /// <summary>
        /// 
        /// </summary>
        protected double Milk_SulfProdn;
        /// <summary>
        /// Weight of milk (4% fat equiv.)
        /// </summary>
        protected double Milk_Weight;                                               
        /// <summary>
        /// Proportion of potential milk production  
        /// </summary>
        protected double PropnOfMaxMilk;                                
        /// <summary>
        /// Scales max. intake etc for underweight in lactating animals  
        /// </summary>
        protected double LactAdjust;                                    
        /// <summary>
        /// 
        /// </summary>
        protected double LactRatio;                                                          
        /// <summary>
        /// 
        /// </summary>
        protected double DryOffTime;
        /// <summary>
        /// Potential intake (uncorrected for legume)
        /// </summary>
        protected double IntakeLimit;                                   
        /// <summary>
        /// 
        /// </summary>
        protected double FeedingLevel;
        /// <summary>
        /// 
        /// </summary>
        protected double Start_FU;
        /// <summary>
        /// Fraction of base weight gain from solid intake. 
        /// </summary>
        protected double BWGain_Solid;                                  
        /// <summary>
        /// Additional distance walked -dairy cattle
        /// </summary>
        protected double FDistanceWalked;                                
        /// <summary>
        /// Overall stocking pressure
        /// </summary>
        protected double FAnimalsPerHa;                                                 
        /// <summary>
        /// 
        /// </summary>
        protected double Steepness;
        /// <summary>
        /// The grazing inputs
        /// </summary>
        protected GrazType.TGrazingInputs Inputs = new GrazType.TGrazingInputs();
        /// <summary>
        /// The animal's environment
        /// </summary>
        protected TAnimalWeather TheEnv;
        /// <summary>
        /// 
        /// </summary>
        protected double WaterLog;
        /// <summary>
        /// The ration being fed
        /// </summary>
        protected TSupplementRation TheRation;
        /// <summary>
        /// 
        /// </summary>
        protected TSupplement FIntakeSupp;
        /// <summary>
        /// 
        /// </summary>
        protected double Supp_FWI;
        /// <summary>
        /// 
        /// </summary>
        protected double[] NetSupp_DMI;
        /// <summary>
        /// Sub time step value
        /// </summary>
        protected double[] TimeStepNetSupp_DMI;
        /// <summary>
        /// Chill index
        /// </summary>
        protected double ChillIndex;
        /// <summary>
        /// 
        /// </summary>
        protected double ImplantEffect;
        /// <summary>
        /// 
        /// </summary>
        protected double FIntakeModifier;

        // Model logic ...................................................
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ClssAttr"></param>
        /// <param name="NetClassIntake"></param>
        /// <param name="SummaryIntake"></param>
        private void AddDietElement(ref GrazType.IntakeRecord ClssAttr, double NetClassIntake, ref GrazType.IntakeRecord SummaryIntake)
        {
            if (NetClassIntake > 0.0)
            {
                SummaryIntake.Biomass = SummaryIntake.Biomass + NetClassIntake;
                SummaryIntake.Digestibility = SummaryIntake.Digestibility + NetClassIntake * ClssAttr.Digestibility;
                SummaryIntake.CrudeProtein = SummaryIntake.CrudeProtein + NetClassIntake * ClssAttr.CrudeProtein;
                SummaryIntake.Degradability = SummaryIntake.Degradability + NetClassIntake * ClssAttr.CrudeProtein * ClssAttr.Degradability;
                SummaryIntake.PhosContent = SummaryIntake.PhosContent + NetClassIntake * ClssAttr.PhosContent;
                SummaryIntake.SulfContent = SummaryIntake.SulfContent + NetClassIntake * ClssAttr.SulfContent;
                SummaryIntake.AshAlkalinity = SummaryIntake.AshAlkalinity + NetClassIntake * ClssAttr.AshAlkalinity;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="SummaryIntake"></param>
        private void SummariseIntakeRecord(ref GrazType.IntakeRecord SummaryIntake)
        {
            double fTrivialIntake = 1.0E-6; // (kg/head)

            if (SummaryIntake.Biomass < fTrivialIntake)
                SummaryIntake = new GrazType.IntakeRecord();
            else
            {
                SummaryIntake.Digestibility = SummaryIntake.Digestibility / SummaryIntake.Biomass;
                if (SummaryIntake.CrudeProtein > 0.0)
                    SummaryIntake.Degradability = SummaryIntake.Degradability / SummaryIntake.CrudeProtein;
                else
                    SummaryIntake.Degradability = 0.75;

                SummaryIntake.CrudeProtein = SummaryIntake.CrudeProtein / SummaryIntake.Biomass;
                SummaryIntake.PhosContent = SummaryIntake.PhosContent / SummaryIntake.Biomass;
                SummaryIntake.SulfContent = SummaryIntake.SulfContent / SummaryIntake.Biomass;
                SummaryIntake.AshAlkalinity = SummaryIntake.AshAlkalinity / SummaryIntake.Biomass;
            }
        }

        /// <summary>
        ///  DescribeTheDiet                                                           
        /// Calculate the following for each applicable component of the diet         
        /// (herbage, supplement and milk):                                             
        ///   - Dry weight of intake             - Intake of ME                       
        ///   - Weight of protein in the intake  - Intake of P                        
        ///   - Digestibility                    - Intake of S                        
        ///   - Digestible organic matter (DOM)  - Concentration of protein           
        ///   - ME:DM ratio                                                           
        /// These results are all stored in the TimeStepState static variable for     
        /// reference by other routines.                                              
        /// </summary>
        /// <param name="HerbageRI"></param>
        /// <param name="SeedRI"></param>
        /// <param name="SuppRI"></param>
        /// <param name="timeStepState"></param>
        protected void DescribeTheDiet(ref double[] HerbageRI,      // "Relative intakes" of each herbage       
                                   ref double[,] SeedRI,            //   digestibility class, seeds &           
                                   ref double SuppRI,               //   supplement                             
                                   ref TAnimalOutput timeStepState)
        {

            GrazType.IntakeRecord suppInput = new GrazType.IntakeRecord();
            double fGutPassage;
            double fSupp_ME2DM;                                              // Used to compute ME_2_DM.Supp          
            int Species, Clss, Rp, Idx;

            for (Clss = 1; Clss <= GrazType.DigClassNo; Clss++)
                timeStepState.IntakePerHead.Herbage[Clss] = IntakeLimit * HerbageRI[Clss];
            for (Species = 1; Species <= GrazType.MaxPlantSpp; Species++)
            {
                for (Rp = GrazType.UNRIPE; Rp <= GrazType.RIPE; Rp++)
                    timeStepState.IntakePerHead.Seed[Species, Rp] = IntakeLimit * SeedRI[Species, Rp];
            }
            timeStepState.PaddockIntake = new GrazType.IntakeRecord();                        // Summarise herbage+seed intake         
            for (Clss = 1; Clss <= GrazType.DigClassNo; Clss++)
                AddDietElement(ref Inputs.Herbage[Clss], timeStepState.IntakePerHead.Herbage[Clss], ref timeStepState.PaddockIntake);
            for (Species = 1; Species <= GrazType.MaxPlantSpp; Species++)
            {
                for (Rp = GrazType.UNRIPE; Rp <= GrazType.RIPE; Rp++)
                    AddDietElement(ref Inputs.Seeds[Species, Rp], timeStepState.IntakePerHead.Seed[Species, Rp], ref timeStepState.PaddockIntake);
            }

            SummariseIntakeRecord(ref timeStepState.PaddockIntake);
            if (timeStepState.PaddockIntake.Biomass == 0.0) // i.e. less than fTrivialIntake
                timeStepState.IntakePerHead = new GrazType.TGrazingOutputs();

            timeStepState.SuppIntake = new GrazType.IntakeRecord();                             // Summarise supplement intake           
            fSupp_ME2DM = 0.0;
            if ((TheRation.TotalAmount > 0.0) && (SuppRI * IntakeLimit > 0.0))
            {
                for (Idx = 0; Idx <= TheRation.Count - 1; Idx++)                                      // The supplements must be treated       
                {                                                                    //   separately because of the non-      
                    suppInput.Digestibility = TheRation[Idx].DM_Digestibility;            //   linearity in the gut passage term   
                    suppInput.CrudeProtein = TheRation[Idx].CrudeProt;
                    suppInput.Degradability = TheRation[Idx].DgProt;
                    suppInput.PhosContent = TheRation[Idx].Phosphorus;
                    suppInput.SulfContent = TheRation[Idx].Sulphur;
                    suppInput.AshAlkalinity = TheRation[Idx].AshAlkalinity;

                    if (Animal == GrazType.AnimalType.Cattle)
                        fGutPassage = TheRation[Idx].MaxPassage * StdMath.RAMP(TheRation.TotalAmount / IntakeLimit, 0.20, 0.75);
                    else
                        fGutPassage = 0.0;
                    TimeStepNetSupp_DMI[Idx] = (1.0 - fGutPassage) * TheRation.getFWFract(Idx) * (IntakeLimit * SuppRI);

                    AddDietElement(ref suppInput, TimeStepNetSupp_DMI[Idx], ref timeStepState.SuppIntake);
                    fSupp_ME2DM = fSupp_ME2DM + TimeStepNetSupp_DMI[Idx] * TheRation[Idx].ME_2_DM;
                }

                SummariseIntakeRecord(ref timeStepState.SuppIntake);
                if (timeStepState.SuppIntake.Biomass == 0.0) // i.e. less than fTrivialIntake
                {
                    for (Idx = 0; Idx <= TheRation.Count - 1; Idx++)
                        TimeStepNetSupp_DMI[Idx] = 0.0;
                    fSupp_ME2DM = 0.0;
                }
                else
                    fSupp_ME2DM = StdMath.XDiv(fSupp_ME2DM, timeStepState.SuppIntake.Biomass);
            }
            else
                for (Idx = 0; Idx <= TheRation.Count - 1; Idx++)
                    TimeStepNetSupp_DMI[Idx] = 0.0;


            timeStepState.DM_Intake.Herbage = timeStepState.PaddockIntake.Biomass;                              // Dry matter intakes                    
            timeStepState.DM_Intake.Supp = timeStepState.SuppIntake.Biomass;
            timeStepState.DM_Intake.Solid = timeStepState.DM_Intake.Herbage + timeStepState.DM_Intake.Supp;
            timeStepState.DM_Intake.Total = timeStepState.DM_Intake.Solid;                                    // Milk doesn't count for DM intake      

            timeStepState.Digestibility.Herbage = timeStepState.PaddockIntake.Digestibility;                      // Digestibilities                       
            timeStepState.Digestibility.Supp = timeStepState.SuppIntake.Digestibility;
            timeStepState.Digestibility.Solid = StdMath.XDiv(timeStepState.Digestibility.Supp * timeStepState.DM_Intake.Supp +
                                           timeStepState.Digestibility.Herbage * timeStepState.DM_Intake.Herbage,
                                           timeStepState.DM_Intake.Solid);

            if (LactStatus == GrazType.LactType.Suckling)                                             // Milk terms                            
            {
                timeStepState.CP_Intake.Milk = Mothers.Milk_ProtProdn / NoOffspring;
                timeStepState.Phos_Intake.Milk = Mothers.Milk_PhosProdn / NoOffspring;
                timeStepState.Sulf_Intake.Milk = Mothers.Milk_SulfProdn / NoOffspring;
                timeStepState.ME_Intake.Milk = Mothers.Milk_MJProdn / NoOffspring;
            }
            else
            {
                timeStepState.CP_Intake.Milk = 0.0;
                timeStepState.Phos_Intake.Milk = 0.0;
                timeStepState.Sulf_Intake.Milk = 0.0;
                timeStepState.ME_Intake.Milk = 0.0;
            }

            timeStepState.CP_Intake.Herbage = timeStepState.PaddockIntake.Biomass * timeStepState.PaddockIntake.CrudeProtein; // Crude protein intakes and contents    
            timeStepState.CP_Intake.Supp = timeStepState.SuppIntake.Biomass * timeStepState.SuppIntake.CrudeProtein;
            timeStepState.CP_Intake.Solid = timeStepState.CP_Intake.Herbage + timeStepState.CP_Intake.Supp;
            timeStepState.CP_Intake.Total = timeStepState.CP_Intake.Solid + timeStepState.CP_Intake.Milk;
            timeStepState.ProteinConc.Herbage = timeStepState.PaddockIntake.CrudeProtein;
            timeStepState.ProteinConc.Supp = timeStepState.SuppIntake.CrudeProtein;
            timeStepState.ProteinConc.Solid = StdMath.XDiv(timeStepState.CP_Intake.Solid, timeStepState.DM_Intake.Solid);

            timeStepState.Phos_Intake.Herbage = timeStepState.PaddockIntake.Biomass * timeStepState.PaddockIntake.PhosContent;  // Phosphorus intakes                    
            timeStepState.Phos_Intake.Supp = 0.0;
            timeStepState.Phos_Intake.Solid = timeStepState.Phos_Intake.Herbage + timeStepState.Phos_Intake.Supp;
            timeStepState.Phos_Intake.Total = timeStepState.Phos_Intake.Solid + timeStepState.Phos_Intake.Milk;

            timeStepState.Sulf_Intake.Herbage = timeStepState.PaddockIntake.Biomass * timeStepState.PaddockIntake.SulfContent;  // Sulphur intakes                       
            timeStepState.Sulf_Intake.Supp = 0.0;
            timeStepState.Sulf_Intake.Solid = timeStepState.Sulf_Intake.Herbage + timeStepState.Sulf_Intake.Supp;
            timeStepState.Sulf_Intake.Total = timeStepState.Sulf_Intake.Solid + timeStepState.Sulf_Intake.Milk;

            timeStepState.ME_2_DM.Herbage = GrazType.HerbageE2DM * timeStepState.Digestibility.Herbage - 2.0;          // Metabolizable energy intakes and      
            timeStepState.ME_2_DM.Supp = fSupp_ME2DM;                                        //   contents                            
            timeStepState.ME_Intake.Supp = timeStepState.ME_2_DM.Supp * timeStepState.DM_Intake.Supp;
            timeStepState.ME_Intake.Herbage = timeStepState.ME_2_DM.Herbage * timeStepState.DM_Intake.Herbage;
            timeStepState.ME_Intake.Solid = timeStepState.ME_Intake.Herbage + timeStepState.ME_Intake.Supp;
            timeStepState.ME_Intake.Total = timeStepState.ME_Intake.Solid + timeStepState.ME_Intake.Milk;
            timeStepState.ME_2_DM.Solid = StdMath.XDiv(timeStepState.ME_Intake.Solid, timeStepState.DM_Intake.Solid);
        }

        /// <summary>
        /// Compute RDP intake and requirement for a given MEI and feeding level      
        /// </summary>
        /// <param name="Latitude"></param>
        /// <param name="Day"></param>
        /// <param name="IntakeScale"></param>
        /// <param name="FL"></param>
        /// <param name="CorrDg"></param>
        /// <param name="RDPI"></param>
        /// <param name="RDPR"></param>
        /// <param name="UDPIs"></param>
        protected void ComputeRDP(double Latitude,
                                     int Day,
                                     double IntakeScale,            // Assumed scaling factor for intake        
                                     double FL,                     // Assumed feeding level                    
                                     ref TDietRecord CorrDg,
                                     ref double RDPI, ref double RDPR,
                                     ref TDietRecord UDPIs)
        {
            TDietRecord RDPIs;
            double SuppFME_Intake;                                                 // Fermentable ME intake of supplement      
            int Idx;

            CorrDg.Herbage = AnimalState.PaddockIntake.Degradability                           // Correct the protein degradability        
                              * (1.0 - (AParams.DgProtC[1] - AParams.DgProtC[2] * AnimalState.Digestibility.Herbage)// for feeding level                     
                                       * Math.Max(FL, 0.0));
            CorrDg.Supp = AnimalState.SuppIntake.Degradability
                              * (1.0 - AParams.DgProtC[3] * Math.Max(FL, 0.0));

            RDPIs.Herbage = IntakeScale * AnimalState.CP_Intake.Herbage * AnimalState.CorrDgProt.Herbage;
            RDPIs.Supp = IntakeScale * AnimalState.CP_Intake.Supp * AnimalState.CorrDgProt.Supp;
            RDPIs.Solid = RDPIs.Herbage + RDPIs.Supp;
            RDPIs.Milk = 0.0;                                                   // This neglects any degradation of milk    
            UDPIs.Herbage = IntakeScale * AnimalState.CP_Intake.Herbage - RDPIs.Herbage;       //   CPI late in lactation when the rumen   
            UDPIs.Supp = IntakeScale * AnimalState.CP_Intake.Supp - RDPIs.Supp;          //   has begun to develop                   
            UDPIs.Milk = AnimalState.CP_Intake.Milk;
            UDPIs.Solid = UDPIs.Herbage + UDPIs.Supp;
            RDPI = RDPIs.Solid + RDPIs.Milk;

            SuppFME_Intake = StdMath.DIM(IntakeScale * AnimalState.ME_Intake.Supp,                    // Fermentable ME intake of supplement      
                                   GrazType.ProteinE2DM * UDPIs.Supp);                      //   leaves out the ME derived from         
            for (Idx = 0; Idx <= TheRation.Count - 1; Idx++)                                    //   undegraded protein and oils            
                SuppFME_Intake = StdMath.DIM(SuppFME_Intake,
                                       GrazType.FatE2DM * TheRation[Idx].EtherExtract * IntakeScale * NetSupp_DMI[Idx]);

            RDPR = (AParams.DgProtC[4] + AParams.DgProtC[5] * (1.0 - Math.Exp(-AParams.DgProtC[6] * (FL + 1.0))))       // RDP requirement                          
                    * (IntakeScale * AnimalState.ME_Intake.Herbage
                        * (1.0 + AParams.DgProtC[7] * (Latitude / 40.0)
                                            * Math.Sin(GrazEnv.DAY2RAD * StdDate.DOY(Day, true))) + SuppFME_Intake);
        }
        /// <summary>
        /// Set the standard reference weight of a group of animals based on breed  
        /// and sex                                                                   
        /// </summary>
        protected void ComputeSRW()
        {
            double SRW;                                                             // Breed standard reference weight (i.e.    
            //  normal weight of a mature, empty female)

            if (Mothers != null)                                                    // For lambs and calves, take both parents' 
                SRW = AParams.BreedSRW;     // 0.5 * (BreedSRW + MaleSRW)           //   breed SRW's into account               
            else
                SRW = AParams.BreedSRW;

            if (NoMales == 0)                                                       // Now take into account different SRWs of  
                StdRefWt = SRW;                                                     //   males and females and different        
            else                                                                    //   scalars for entire and castrated males 
                StdRefWt = SRW * StdUnits.StdMath.XDiv(NoFemales + NoMales * AParams.SRWScalars[(int)ReproStatus],       //TODO: check this
                                        NoFemales + NoMales);
        }

        /// <summary>
        /// Reference birth weight, adjusted for number of foetuses and relative size 
        /// </summary>
        /// <returns></returns>
        protected double fBirthWtForSize()
        {
            return AParams.StdBirthWt(NoFoetuses) * ((1.0 - AParams.PregC[4]) + AParams.PregC[4] * Size);
        }
        /// <summary>
        ///  "Normal weight" of the foetus and the weight of the conceptus in pregnant }
        /// animals.                                                                  }
        /// </summary>
        /// <returns></returns>
        protected double FoetalNormWt()
        {
            if ((ReproStatus == GrazType.ReproType.EarlyPreg) || (ReproStatus == GrazType.ReproType.LatePreg))
                return fBirthWtForSize() * fGompertz(FoetalAge, AParams.PregC[1], AParams.PregC[2], AParams.PregC[3]);
            else
                return 0.0;
        }

        /// <summary>
        /// Gompertz function, constrained to give f(A)=1.0                              
        /// </summary>
        /// <param name="T"></param>
        /// <param name="A"></param>
        /// <param name="B"></param>
        /// <param name="C"></param>
        /// <returns></returns>
        protected double fGompertz(double T, double A, double B, double C)
        {
            return Math.Exp(B * (1.0 - Math.Exp(C * (1.0 - T / A))));
        }

        /// <summary>
        /// Weight of the conceptus, i.e. foetus(es) plus uterus etc                  
        /// </summary>
        /// <returns></returns>
        protected double ConceptusWt()
        {
            if ((ReproStatus == GrazType.ReproType.EarlyPreg) || (ReproStatus == GrazType.ReproType.LatePreg))
                return NoFoetuses
                          * (AParams.PregC[5] * fBirthWtForSize() * fGompertz(FoetalAge, AParams.PregC[1], AParams.PregC[6], AParams.PregC[7])
                             + FoetalWt - FoetalNormWt());
            else
                return 0.0;
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
        protected double MaxNormWtFunc(double SRW, double BW,
                                int AgeDays,
                                TAnimalParamSet Params)
        {
            double GrowthRate;

            GrowthRate = Params.GrowthC[1] / Math.Pow(SRW, Params.GrowthC[2]);
            return SRW - (SRW - BW) * Math.Exp(-GrowthRate * AgeDays);
        }
        /// <summary>
        /// Normal weight equation                                                 
        /// </summary>
        /// <param name="iAgeDays"></param>
        /// <param name="fMaxOldWt"></param>
        /// <param name="fWeighting"></param>
        /// <returns></returns>
        protected double NormalWeightFunc(int iAgeDays, double fMaxOldWt, double fWeighting)
        {
            double fMaxNormWt;

            fMaxNormWt = MaxNormWtFunc(StdRefWt, BirthWt, iAgeDays, AParams);
            if (fMaxOldWt < fMaxNormWt)                                           // Delayed deveopment of frame size         
                return fWeighting * fMaxNormWt + (1.0 - fWeighting) * fMaxOldWt;
            else
                return fMaxNormWt;
        }
        /// <summary>
        /// Calculate normal weight, size and condition of a group of animals.      
        /// </summary>
        protected void Calc_Weights()
        {
            MaxPrevWt = Math.Max(BaseWeight, MaxPrevWt);                             // Store the highest weight reached to date 
            NormalWt = NormalWeightFunc(MeanAge, MaxPrevWt, AParams.GrowthC[3]);
            Size = NormalWt / StdRefWt;
            Condition = BaseWeight / NormalWt;
        }
        /// <summary>
        /// Compute coat depth from GFW and fibre diameter                              
        /// </summary>
        protected void Calc_CoatDepth()
        {
            double FibreCount;
            double FibreArea;

            //WITH AParams DO
            if (Animal == GrazType.AnimalType.Cattle)
                FCoatDepth = 1.0;
            else
            {
                FibreCount = AParams.WoolC[11] * AParams.ChillC[1] * Math.Pow(NormalWt, 2.0 / 3.0);
                FibreArea = Math.PI / 4.0 * Math.Pow(WoolMicron * 1E-6, 2.0);
                FCoatDepth = 100.0 * AParams.WoolC[3] * WoolWt / (FibreCount * AParams.WoolC[10] * FibreArea);
            }
        }

        /// <summary>
        /// In sheep, the coat depth is used to set the total wool weight (this is the  
        /// way that shearing is done)                                                  
        /// Parameter:                                                                  
        ///   CM  Coat depth for which a greasy wool weight is to be calculated (cm)    
        /// </summary>
        /// <param name="CM"></param>
        /// <returns></returns>
        protected double CoatDepth2Wool(double CM)
        {
            double FibreCount;
            double FibreArea;

            if (Animal == GrazType.AnimalType.Sheep)
            {
                FibreCount = AParams.WoolC[11] * AParams.ChillC[1] * Math.Pow(NormalWt, 2.0 / 3.0);
                FibreArea = Math.PI / 4.0 * Math.Pow(WoolMicron * 1E-6, 2);
                return (FibreCount * AParams.WoolC[10] * FibreArea) * CM / (100.0 * AParams.WoolC[3]);
            }
            else
                return 0.0;
        }

        /// <summary>
        /// Get the conception rates array
        /// </summary>
        /// <returns></returns>
        protected double[] getConceptionRates()
        {
            const double STD_LATITUDE = -35.0;      // Latitude (in degrees) for which the DayLengthConst[] parameters are set    
            int iDOY;
            double fDLFactor;
            double fPropn;
            int N;

            double[] Result = new double[4];        //TConceptionArray

            iDOY = StdDate.DOY(TheEnv.TheDay, true);
            fDLFactor = (1.0 - Math.Sin(GrazEnv.DAY2RAD * (iDOY + 10)))
                         * Math.Sin(GrazEnv.DEG2RAD * TheEnv.Latitude) / Math.Sin(GrazEnv.DEG2RAD * STD_LATITUDE);
            for (N = 1; N <= AParams.MaxYoung; N++)                              // First we calculate the proportion of   
            {                                                                    // females with at least N young          
                if (AParams.ConceiveSigs[N][0] < 5.0)
                    fPropn = StdMath.DIM(1.0, AParams.DayLengthConst[N] * fDLFactor)
                              * StdMath.SIG(Size * Condition, AParams.ConceiveSigs[N]);
                else
                    fPropn = 0.0;

                if (N == 1)
                    Result[N] = fPropn;
                else
                {
                    Result[N] = fPropn * Result[N - 1];
                    Result[N - 1] = Result[N - 1] - Result[N];
                }
            }

            for (N = 1; N <= AParams.MaxYoung - 1; N++)
            {
                Result[N] = StdMath.DIM(Result[N], Result[N + 1]);
            }

            return Result;
        }
        /// <summary>
        /// Make the animals pregnant
        /// </summary>
        /// <param name="ConceptionRate"></param>
        /// <param name="NewGroups"></param>
        protected void makePregnantAnimals(double[] ConceptionRate, ref TAnimalList NewGroups)
        {
            int iInitialNumber;
            TDifferenceRecord FertileDiff;
            TAnimalGroup PregGroup;
            int NPreg, N;

            // A weight differential between conceiving and barren animals
            FertileDiff = new TDifferenceRecord() { StdRefWt = NODIFF.StdRefWt, BaseWeight = NODIFF.BaseWeight, FleeceWt = NODIFF.FleeceWt };            
            FertileDiff.BaseWeight = AParams.FertWtDiff;

            iInitialNumber = NoAnimals;
            for (N = 1; N <= AParams.MaxYoung; N++)
            {
                NPreg = Math.Min(NoAnimals, RandFactory.RndPropn(iInitialNumber, ConceptionRate[N]));
                PregGroup = Split(NPreg, false, FertileDiff, NODIFF);
                if (PregGroup != null)
                {
                    PregGroup.Pregnancy = 1;
                    PregGroup.NoFoetuses = N;
                    CheckAnimList(ref NewGroups);
                    NewGroups.Add(PregGroup);
                }
            }
        }
        /// <summary>
        /// Used in createYoung() to set up the genotypic parameters of the lambs     
        /// or calves that are about to be born/created.                              
        /// </summary>
        /// <returns></returns>
        protected TAnimalParamSet constructOffspringParams()
        {
            TAnimalParamBlend[] mateBlend = new TAnimalParamBlend[1];

            if (FMatedTo != null)
            {
                Array.Resize(ref mateBlend, 2);
                mateBlend[0].Breed = AParams;
                mateBlend[0].fPropn = 0.5;
                mateBlend[1].Breed = FMatedTo;
                mateBlend[1].fPropn = 0.5;

                return TAnimalParamSet.CreateFactory("", mateBlend);
            }
            else
                return new TAnimalParamSet(null, AParams);
        }

        /// <summary>
        ///  Carry out one cycle's worth of conceptions                                
        /// </summary>
        /// <param name="NewGroups"></param>
        private void Conceive(ref TAnimalList NewGroups)
        {
            if ((ReproStatus == GrazType.ReproType.Empty)
               && (!((AParams.Animal == GrazType.AnimalType.Sheep) && (LactStatus == GrazType.LactType.Lactating)))
               && (MateCycle == 0))
                makePregnantAnimals(getConceptionRates(), ref NewGroups);
        }
        /// <summary>
        /// Death rate calculation
        /// </summary>
        /// <returns></returns>
        private double DeathRateFunc()
        {
            double GrowthRate;
            double DeltaNormalWt;
            double Result;

            GrowthRate = AParams.GrowthC[1] / Math.Pow(StdRefWt, AParams.GrowthC[2]);
            DeltaNormalWt = (StdRefWt - BirthWt) * (Math.Exp(-GrowthRate * (MeanAge - 1)) - Math.Exp(-GrowthRate * MeanAge));

            Result = 1.0 - fExpectedSurvival(1);
            if ((LactStatus != GrazType.LactType.Suckling) && (Condition < AParams.MortCondConst) && (DeltaBaseWeight < 0.2 * DeltaNormalWt))
                Result = Result + AParams.MortIntensity * (AParams.MortCondConst - Condition);
            return Result;
        }
        /// <summary>
        /// Exposure calculations
        /// </summary>
        /// <returns></returns>
        private double ExposureFunc()
        {
            double ExposureOdds;
            double Exp_ExpOdds;
            double Result;

            ExposureOdds = AParams.ExposureConsts[0] - AParams.ExposureConsts[1] * Condition + AParams.ExposureConsts[2] * ChillIndex;
            if (NoOffspring > 1)
                ExposureOdds = ExposureOdds + AParams.ExposureConsts[3];
            Exp_ExpOdds = Math.Exp(ExposureOdds);
            Result = Exp_ExpOdds / (1.0 + Exp_ExpOdds);
            return Result;
        }

        /// <summary>
        /// Mortality submodel                                                        
        /// </summary>
        /// <param name="Chill"></param>
        /// <param name="NewGroups"></param>
        protected void Kill(double Chill, ref  TAnimalList NewGroups)
        {
            double DeathRate;
            TDifferenceRecord Diffs;
            int MaleLosses;
            int FemaleLosses;
            int NoLosses;
            int YoungLosses;
            int YoungToKill;
            TAnimalGroup DeadGroup;
            TAnimalGroup SplitGroup;

            Diffs = new TDifferenceRecord() { StdRefWt = NODIFF.StdRefWt, BaseWeight = NODIFF.BaseWeight, FleeceWt = NODIFF.FleeceWt };            
            Diffs.BaseWeight = -AParams.MortWtDiff * BaseWeight;

            DeathRate = DeathRateFunc();
            FemaleLosses = RandFactory.RndPropn(NoFemales, DeathRate);
            MaleLosses = RandFactory.RndPropn(NoMales, DeathRate);
            NoLosses = MaleLosses + FemaleLosses;
            if ((Animal == GrazType.AnimalType.Sheep) && (Young != null) && (Young.MeanAge == 1))
                YoungLosses = RandFactory.RndPropn(Young.NoAnimals, ExposureFunc());
            else
                YoungLosses = 0;

            if ((Young == null) && (NoLosses > 0))
                SplitSex(MaleLosses, FemaleLosses, false, Diffs);

            else if ((Young != null) && (FemaleLosses + YoungLosses > 0))
            {
                if (FemaleLosses > 0)                                               // For now, unweaned young of dying animals 
                {                                                                   //   die with them                       
                    DeadGroup = Split(FemaleLosses, false, Diffs, NODIFF);
                    YoungToKill = StdMath.IDIM(YoungLosses, DeadGroup.Young.NoAnimals);
                    DeadGroup = null;
                }
                else
                    YoungToKill = YoungLosses;

                if (YoungToKill > 0)                                                // Any further young to kill are removed as 
                {                                                                   //   evenly as possible from their mothers  
                    if (NoFemales > 0)
                        LoseYoung(this, YoungToKill / NoFemales);
                    if (YoungToKill % NoFemales > 0)
                    {
                        SplitGroup = Split(YoungToKill % NoFemales, false, NODIFF, NODIFF);
                        LoseYoung(SplitGroup, 1);
                        CheckAnimList(ref NewGroups);
                        NewGroups.Add(SplitGroup);
                    }
                } //_ IF (YoungToKill > 0) 
            } // ELSE IF (Young <> NIL) and (NoLosses + YoungLosses > 0) 
        }

        /// <summary>
        /// Decrease the number of young by N per mother                               
        /// </summary>
        /// <param name="aGroup"></param>
        /// <param name="N"></param>
        protected void LoseYoung(TAnimalGroup aGroup, int N)
        {
            TDifferenceRecord YoungDiffs;
            TAnimalGroup LoseGroup;
            int iMaleYoung;
            int iFemaleYoung;
            int iYoungToLose;
            int iMalesToLose;
            int iFemalesToLose;

            if (N == aGroup.NoOffspring)
            {
                aGroup.Young = null;
                aGroup.SetNoOffspring(0);
            }
            else if (N > 0)
            {
                YoungDiffs = new TDifferenceRecord() { StdRefWt = NODIFF.StdRefWt, BaseWeight = NODIFF.BaseWeight, FleeceWt = NODIFF.FleeceWt };            
                YoungDiffs.BaseWeight = -aGroup.Young.AParams.MortWtDiff * aGroup.Young.BaseWeight;

                iMaleYoung = aGroup.Young.NoMales;
                iFemaleYoung = aGroup.Young.NoFemales;
                iYoungToLose = N * aGroup.NoFemales;

                iMalesToLose = Convert.ToInt32(Math.Round(iYoungToLose * StdMath.XDiv(iMaleYoung, iMaleYoung + iFemaleYoung)));
                iMalesToLose = Math.Min(iMalesToLose, iMaleYoung);

                iFemalesToLose = iYoungToLose - iMalesToLose;
                if (iFemalesToLose > iFemaleYoung)
                {
                    iMalesToLose += iFemalesToLose - iFemaleYoung;
                    iFemalesToLose = iFemaleYoung;
                }

                LoseGroup = aGroup.Young.SplitSex(iMalesToLose, iFemalesToLose, false, YoungDiffs);
                LoseGroup = null;
                aGroup.FNoOffspring -= N;
                aGroup.Young.FNoOffspring -= N;
            }
        }

        /// <summary>
        /// Pregnancy toxaemia and dystokia                                           
        /// </summary>
        /// <param name="NewGroups"></param>
        protected void KillEndPreg(ref TAnimalList NewGroups)
        {
            double DystokiaRate;
            double ToxaemiaRate;
            TAnimalGroup DystGroup;
            int NoLosses;

            if ((Animal == GrazType.AnimalType.Sheep) && (FoetalAge == AParams.Gestation - 1))
                if (NoFoetuses == 1)                                                // Calculate loss of young due to           
                {                                                                  // dystokia and move the corresponding      
                    DystokiaRate = StdMath.SIG((FoetalWt / AParams.StdBirthWt(1)) *                    // number of mothers into a new animal      
                                           Math.Max(Size, 1.0),                            // group                                    
                                         AParams.DystokiaSigs);
                    NoLosses = RandFactory.RndPropn(NoFemales, DystokiaRate);
                    if (NoLosses > 0)
                    {
                        DystGroup = Split(NoLosses, false, NODIFF, NODIFF);
                        DystGroup.Pregnancy = 0;
                        CheckAnimList(ref NewGroups);
                        NewGroups.Add(DystGroup);
                    } // IF (NoLosses > 0)
                } //IF (NoYoung = 1) 

                else if (NoFoetuses >= 2)                                          // Deaths of sheep with multiple young      
                {                                                                  //   due to pregnancy toxaemia              
                    ToxaemiaRate = StdMath.SIG((MidLatePregWt - BaseWeight) / NormalWt,
                                         AParams.ToxaemiaSigs);
                    NoLosses = RandFactory.RndPropn(NoFemales, ToxaemiaRate);
                    if (NoLosses > 0)
                        Split(NoLosses, false, NODIFF, NODIFF);
                } // ELSE IF (NoFoetuses >= 2) 
        }

        /// <summary>
        /// Automatic end to lactation in response to reduced milk production         
        /// </summary>
        /// <returns></returns>
        protected bool YoungStopSuckling()
        {
            return ((Young != null)
                      && (Young.LactStatus == GrazType.LactType.Suckling)
                      && (Young.MeanAge >= 7)
                      && (Milk_Weight / NoSuckling() < AParams.SelfWeanPropn
                                                    * (Young.AnimalState.PaddockIntake.Biomass
                                                       + Young.AnimalState.SuppIntake.Biomass)));
        }

        /// <summary>
        /// Number of offspring that are actually suckling
        /// </summary>
        /// <returns></returns>
        protected int NoSuckling()
        {
            if ((Young != null) && (Young.LactStatus == GrazType.LactType.Suckling))
                return NoOffspring;
            else
                return 0;
        }

        //TODO: check that this function returns changed values
        private void AdjustRecords(TAnimalGroup AG, double X, TDifferenceRecord Diffs)
        {
            AG.BaseWeight = AG.BaseWeight + X * Diffs.BaseWeight;
            if (AParams.Animal == GrazType.AnimalType.Sheep)
                AG.WoolWt = AG.WoolWt + X * Diffs.FleeceWt;
            AG.StdRefWt = AG.StdRefWt + X * Diffs.StdRefWt;
            AG.Calc_Weights();
            AG.TotalWeight = AG.BaseWeight + AG.ConceptusWt();                            // TotalWeight is meant to be the weight  
            if (AParams.Animal == GrazType.AnimalType.Sheep)                                     // "on the scales", including conceptus   
                AG.TotalWeight = AG.TotalWeight + AG.WoolWt;                              // and/or fleece.                         
        }

        //  .............................
        /// <summary>
        /// Used by the public Split function
        /// </summary>
        /// <param name="NMale"></param>
        /// <param name="NFemale"></param>
        /// <param name="ByAge"></param>
        /// <param name="Diffs"></param>
        /// <returns></returns>
        protected TAnimalGroup SplitSex(int NMale, int NFemale, bool ByAge, TDifferenceRecord Diffs)
        {
            double PropnGoing;

            if ((NMale > NoMales) || (NFemale > NoFemales))
                throw new Exception("TAnimalGroup: Error in SplitSex method");

            TAnimalGroup Result = Copy();                                                           // Create the new animal group              
            if ((NMale == NoMales) && (NFemale == NoFemales))
            {
                NoMales = 0;
                NoFemales = 0;
                Ages.Clear();
            }
            else
            {
                PropnGoing = StdMath.XDiv(NMale + NFemale, NoMales + NoFemales);                 // Adjust weights etc                       
                AdjustRecords(this, -PropnGoing, Diffs);
                AdjustRecords(Result, 1.0 - PropnGoing, Diffs);

                Result.NoMales = NMale;                                              // Set up numbers in the two groups and     
                Result.NoFemales = NFemale;                                            //   split up the age list                  
                Result.Ages = Ages.Split(NMale, NFemale, ByAge);
                Result.MeanAge = Result.Ages.MeanAge();

                NoMales = NoMales - NMale;
                NoFemales = NoFemales - NFemale;
                MeanAge = Ages.MeanAge();
            }
            return Result;
        }

        // Property methods ..............................................
        /// <summary>
        /// Set the genotype
        /// </summary>
        /// <param name="aValue"></param>
        protected void setGenotype(TAnimalParamSet aValue)
        {
            AParams = new TAnimalParamSet(null, aValue);
        }
        /// <summary>
        /// Get the total number of females and males
        /// </summary>
        /// <returns></returns>
        protected int GetNoAnimals()
        {
            return NoMales + NoFemales;
        }
        /// <summary>
        /// Set the number of animals
        /// </summary>
        /// <param name="N"></param>
        protected void SetNoAnimals(int N)
        {
            if (Mothers != null)
            {
                NoMales = N / 2;
                NoFemales = N - NoMales;
            }
            else if ((ReproStatus == GrazType.ReproType.Male) || (ReproStatus == GrazType.ReproType.Castrated))
            {
                NoMales = N;
                NoFemales = 0;
            }
            else
            {
                NoMales = 0;
                NoFemales = N;
            }

            if (Ages.Count == 0)
                Ages.Input(MeanAge, NoMales, NoFemales);
            else
                Ages.Resize(NoMales, NoFemales);
        }
        /// <summary>
        /// Set the live weight
        /// </summary>
        /// <param name="LW"></param>
        protected void SetLiveWt(double LW)
        {
            BaseWeight = LW - ConceptusWt() - WoolWt;
            TotalWeight = LW;
            Calc_Weights();
        }

        /// <summary>
        /// Weight of fleece that would be cut if the animals were shorn (kg greasy) 
        /// </summary>
        /// <returns></returns>
        protected double GetFleeceCutWt()
        {
            return StdUnits.StdMath.DIM(WoolWt, CoatDepth2Wool(STUBBLE_MM));
        }
        /// <summary>
        /// Set the weight of fleece
        /// </summary>
        /// <param name="GFW"></param>
        protected void SetFleeceCutWt(double GFW)
        {
            SetWoolWt(CoatDepth2Wool(STUBBLE_MM) + Math.Max(GFW, 0.0));
        }

        /// <summary>
        /// Total weight of wool including stubble (kg greasy)                        
        /// </summary>
        /// <param name="WWt"></param>
        protected void SetWoolWt(double WWt)
        {
            WoolWt = WWt;
            BaseWeight = TotalWeight - ConceptusWt() - WoolWt;
            Calc_Weights();
        }
        /// <summary>
        /// Set the maximum previous weight
        /// </summary>
        /// <param name="MPW"></param>
        protected void SetMaxPrevWt(double MPW)
        {
            MaxPrevWt = MPW;
            Calc_Weights();
        }

        /// <summary>
        /// In sheep, the coat depth is used to set the total wool weight 
        /// Parameter:                                                                  
        /// CM  New coat depth (cm)                                                     
        /// </summary>
        /// <param name="CM"></param>
        protected void SetCoatDepth(double CM)
        {
            FCoatDepth = CM;
            SetWoolWt(CoatDepth2Wool(CM));
        }
        /// <summary>
        /// Set the animal to be mated to
        /// </summary>
        /// <param name="aValue"></param>
        protected void setMatedTo(TAnimalParamSet aValue)
        {
            FMatedTo = null;
            if (aValue == null)
                FMatedTo = null;
            else
                FMatedTo = new TAnimalParamSet(aValue);
        }
        /// <summary>
        /// Set the pregnancy progress
        /// </summary>
        /// <param name="P"></param>
        protected void SetPregnancy(int P)
        {
            double ConditionFactor;
            double OldLiveWt;
            int Idx;

            if (P != FoetalAge)
            {
                if (P == 0)
                {
                    ReproStatus = GrazType.ReproType.Empty;                         // Don't re-set the base weight here as     
                    FoetalAge = 0;                                                  //   this is usually used at birth, where   
                    FoetalWt = 0.0;                                                 //   the conceptus is lost                  
                    MidLatePregWt = 0.0;
                    SetNoFoetuses(0);
                    MateCycle = -1;
                }
                else if (P != 0)
                {
                    OldLiveWt = LiveWeight;                                              // Store live weight                        
                    if (P >= AParams.Gestation - LatePregLength)
                        ReproStatus = GrazType.ReproType.LatePreg;
                    else
                        ReproStatus = GrazType.ReproType.EarlyPreg;
                    FoetalAge = P;
                    if (NoFoetuses == 0)
                        SetNoFoetuses(1);
                    MateCycle = -1;
                    DaysToMate = 0;
                    for (Idx = 1; Idx <= 3; Idx++)                                         // This piece of code estimates the weight  
                    {                                                                      //   of the foetus and implicitly the       
                        ConditionFactor = (Condition - 1.0)                                //   conceptus while keeping the live       
                                           * FoetalNormWt() / AParams.StdBirthWt(NoFoetuses);   //   weight constant                        
                        if (Condition >= 1.0)
                            FoetalWt = FoetalNormWt() * (1.0 + ConditionFactor);
                        else
                            FoetalWt = FoetalNormWt() * (1.0 + AParams.PregScale[NoFoetuses] * ConditionFactor);
                        LiveWeight = OldLiveWt;
                        Calc_Weights();
                    }
                    if (P >= AParams.Gestation - LatePregLength / 2) 
                        MidLatePregWt = BaseWeight;
                }
            }

        }

        /// <summary>
        /// Normal weight as a function of age and sex                                
        /// </summary>
        /// <param name="iAgeDays"></param>
        /// <param name="Repr"></param>
        /// <param name="Params"></param>
        /// <returns></returns>
        protected double GrowthCurve(int iAgeDays, GrazType.ReproType Repr, TAnimalParamSet Params)
        {
            double SRW;

            SRW = Params.BreedSRW;
            if ((Repr == GrazType.ReproType.Male) || (Repr == GrazType.ReproType.Castrated))
                SRW = SRW * Params.SRWScalars[(int)Repr];                                           //TODO: check indexing here
            return MaxNormWtFunc(SRW, Params.StdBirthWt(1), iAgeDays, Params);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="L"></param>
        protected void SetLactation(int L)
        {
            //TAnimalGroup MyClass;

            if (L != DaysLactating)
            {
                if (L == 0)
                {
                    LactStatus = GrazType.LactType.Dry;                                                    // Set this before calling setDryoffTime()  
                    if (Young == null)
                        setDryoffTime(DaysLactating, 0, FPrevOffspring);
                    else                                                                  // This happens when self-weaning occurs    
                    {
                        setDryoffTime(DaysLactating, 0, NoOffspring);
                        Young.LactStatus = GrazType.LactType.Dry;
                    }
                    DaysLactating = 0;                                                   // ConditionAtBirthing, PropnOfMaxMilk and  
                }                                                                     // LactAdjust are left at their final values
                else
                {
                    LactStatus = GrazType.LactType.Lactating;
                    if (NoOffspring == 0)
                        SetNoOffspring(1);
                    ConditionAtBirthing = Condition;
                    DaysLactating = L;
                    DryOffTime = 0.0;
                    LactAdjust = 1.0;
                    PropnOfMaxMilk = 1.0;
                    FPrevOffspring = 0;
                    Young = null;
                    //MyClass = this;                                                         //TODO: not 100% sure this is right
                    Young = new TAnimalGroup(this, 0.5 * (GrowthCurve(L, GrazType.ReproType.Male, AParams)
                                                        + GrowthCurve(L, GrazType.ReproType.Empty, AParams)));
                }
                Milk_MJProdn = 0.0;
                Milk_ProtProdn = 0.0;
                Milk_Weight = 0.0;
                LactRatio = 1.0;
            }
        }
        /// <summary>
        /// Set the number of foetuses
        /// </summary>
        /// <param name="iValue"></param>
        protected void SetNoFoetuses(int iValue)
        {
            int iDaysPreg;

            if (iValue == 0)
            {
                Pregnancy = 0;
                FNoFoetuses = 0;
            }
            else if ((iValue <= AParams.MaxYoung) && (iValue != NoFoetuses))
            {
                iDaysPreg = Pregnancy;
                Pregnancy = 0;
                FNoFoetuses = iValue;
                Pregnancy = iDaysPreg;
            }
        }
        /// <summary>
        ///  On creation, lambs and calves are always suckling their mothers. This may 
        /// change in the course of a simulation (see the YoungStopSuckling function) 
        /// </summary>
        /// <param name="iValue"></param>
        protected void SetNoOffspring(int iValue)
        {
            int iDaysLact;

            if (iValue != NoOffspring)
            {
                iDaysLact = Lactation;                                                 // Store the current stage of lactation     
                Lactation = 0;
                if (Young != null)
                {
                    Young = null;
                    Young = null;
                }

                FNoOffspring = iValue;

                if (iValue == 0)
                    Young = null;
                else if (iValue <= AParams.MaxYoung)
                    SetLactation(iDaysLact);                                            // This creates a new group of (suckling)   
            }                                                                     //   lambs or calves                        
        }

        /// <summary>
        /// Return the total faecal carbon and nitrogen an urine nitrogen produced by 
        /// a group of animals.  The values are in kilograms, not kg/head (i.e. they  
        /// are totalled over all animals in the group)                               
        /// </summary>
        /// <returns></returns>
        protected GrazType.DM_Pool GetOrgFaeces()
        {
            GrazType.DM_Pool Result = new GrazType.DM_Pool();

            Result = MultiplyDMPool(AnimalState.OrgFaeces, NoAnimals);
            if (Young != null)
                Result = AddDMPool(Result, Young.OrgFaeces);
            return Result;
        }
        /// <summary>
        /// Get the inorganic faeces amount
        /// </summary>
        /// <returns></returns>
        protected GrazType.DM_Pool GetInOrgFaeces()
        {
            GrazType.DM_Pool Result = new GrazType.DM_Pool();

            Result = MultiplyDMPool(AnimalState.InOrgFaeces, NoAnimals);
            if (Young != null)
                Result = AddDMPool(Result, Young.InOrgFaeces);
            return Result;
        }
        /// <summary>
        /// Get the urine amount
        /// </summary>
        /// <returns></returns>
        protected GrazType.DM_Pool GetUrine()
        {
            GrazType.DM_Pool Result = MultiplyDMPool(AnimalState.Urine, NoAnimals);
            if (Young != null)
                Result = AddDMPool(Result, Young.Urine);
            return Result;
        }

        /// <summary>
        /// Get excretion parameters
        /// </summary>
        /// <returns></returns>
        protected TExcretionInfo getExcretion()
        {
            // these will have to go into the parameter set eventually...
            double[] dFaecesDensity = { 1000.0, 1000.0 };       // kg/m^3
            double[] dFaecesMoisture = { 4.0, 5.0 };            // kg water/kg DM
            double[] dRefNormalWt = { 50.0, 600.0 };            // kg
            double[] dFaecesRefLength = { 0.012, 0.30 };        // m
            double[] dFaecesPower = { 0.00, 1.0 / 3.0 };
            double[] dFaecesWidthToLength = { 0.80, 1.00 };
            double[] dFaecesHeightToLength = { 0.70, 0.12 };
            double[] dFaecalMoistureHerbageMin = { 6.0, 7.5 };  // kg water/kg DM
            double[] dFaecalMoistureSuppMin = { 3.0, 3.0 };
            double[] dFaecalMoistureMax = { 0.0, 0.0 };
            double[] dFaecesNO3Propn = { 0.25, 0.25 };
            double[] dUrineRefLength = { 0.20, 0.60 };          // m
            double[] dUrineWidthToLength = { 1.00, 1.00 };
            double[] dUrineRefVolume = { 0.00015, 0.00200 };    // m^3
            double[] dDailyUrineRefVol = { 0.0003, 0.0250 };    // m^3/head/d

            double dFaecalLongAxis;         // metres
            double dFaecalHeight;           // metres
            double dFaecalMoistureHerbage;
            double dFaecalMoistureSupp;
            double dFaecalFreshWeight;      // kg/head
            double dUrineLongAxis;          // metres
            double dVolumePerUrination;     // m^3
            double dDailyUrineVolume;       // m^3
            TSupplement tempSuppt;

            TExcretionInfo Result = new TExcretionInfo();

            Result.OrgFaeces = MultiplyDMPool(AnimalState.OrgFaeces, NoAnimals);
            Result.InOrgFaeces = MultiplyDMPool(AnimalState.InOrgFaeces, NoAnimals);
            Result.Urine = MultiplyDMPool(AnimalState.Urine, NoAnimals);

            // In sheep, we treat each faecal pellet as a separate defaecation.
            // Sheep pellets are assumed to have constant size; cattle pats vary with
            // linear dimension of the animal

            dFaecalLongAxis = dFaecesRefLength[(int)Animal] * Math.Pow(NormalWt / dRefNormalWt[(int)Animal], dFaecesPower[(int)Animal]);
            dFaecalHeight = dFaecalLongAxis * dFaecesHeightToLength[(int)Animal];

            // Faecal moisture content seems to be lower when animals are not at pasture,
            // so estimate it separately for herbage and supplement components of the diet
            tempSuppt = new TSupplement();
            TheRation.AverageSuppt(out tempSuppt);
            dFaecalMoistureHerbage = dFaecalMoistureHerbageMin[(int)Animal] + (dFaecalMoistureMax[(int)Animal] - dFaecalMoistureHerbageMin[(int)Animal]) * AnimalState.Digestibility.Herbage;
            dFaecalMoistureSupp = dFaecalMoistureSuppMin[(int)Animal] + (dFaecalMoistureMax[(int)Animal] - dFaecalMoistureSuppMin[(int)Animal]) * (1.0 - tempSuppt.DM_Propn);
            dFaecalFreshWeight = AnimalState.DM_Intake.Herbage * (1.0 - AnimalState.Digestibility.Herbage) * (1.0 + dFaecalMoistureHerbage)
                                      + AnimalState.DM_Intake.Supp * (1.0 - AnimalState.Digestibility.Supp) * (1.0 + dFaecalMoistureSupp);
            tempSuppt = null;

            // Defaecations are assumed to be ellipsoidal prisms:
            Result.dDefaecationEccentricity = Math.Sqrt(1.0 - StdMath.Sqr(dFaecesWidthToLength[(int)AParams.Animal]));
            Result.dDefaecationArea = Math.PI / 4.0 * StdMath.Sqr(dFaecalLongAxis) * dFaecesWidthToLength[(int)AParams.Animal];
            Result.dDefaecationVolume = Result.dDefaecationArea * dFaecalHeight;
            Result.dDefaecations = NoAnimals * (dFaecalFreshWeight / dFaecesDensity[(int)AParams.Animal]) / Result.dDefaecationVolume;
            Result.dFaecalNO3Propn = dFaecesNO3Propn[(int)AParams.Animal];

            dUrineLongAxis = dUrineRefLength[(int)Animal] * Math.Pow(NormalWt / dRefNormalWt[(int)Animal], 1.0 / 3.0);
            dVolumePerUrination = dUrineRefVolume[(int)Animal] * Math.Pow(NormalWt / dRefNormalWt[(int)Animal], 1.0);
            dDailyUrineVolume = dDailyUrineRefVol[(int)Animal] * Math.Pow(NormalWt / dRefNormalWt[(int)Animal], 1.0);

            // Urinations are assumed to be ellipsoidal
            Result.dUrinationEccentricity = Math.Sqrt(1.0 - StdMath.Sqr(dUrineWidthToLength[(int)AParams.Animal]));
            Result.dUrinationArea = Math.PI / 4.0 * StdMath.Sqr(dUrineLongAxis) * dUrineWidthToLength[(int)AParams.Animal];
            Result.dUrinationVolume = dVolumePerUrination;
            Result.dUrinations = NoAnimals * dDailyUrineVolume / Result.dUrinationVolume;

            return Result;
        }

        /// <summary>
        /// Get the animal type
        /// </summary>
        /// <returns></returns>
        protected GrazType.AnimalType GetAnimal()
        {
            return AParams.Animal;
        }
        /// <summary>
        /// Get the breed name
        /// </summary>
        /// <returns></returns>
        protected string GetBreed()
        {
            return AParams.sName;
        }

        //TODO: Test this function
        /// <summary>
        /// Get the age class 
        /// </summary>
        /// <returns></returns>
        protected GrazType.AgeType GetAgeClass()
        {
            //Array[AnimalType,0..3] of AgeType
            GrazType.AgeType[,] AgeClassMap = new GrazType.AgeType[2, 4] 
                                { {GrazType.AgeType.Weaner, GrazType.AgeType.Yearling, GrazType.AgeType.Mature, GrazType.AgeType.Mature},         //sheep
                                {GrazType.AgeType.Weaner, GrazType.AgeType.Yearling, GrazType.AgeType.TwoYrOld, GrazType.AgeType.Mature} };       //cattle
            if ((Mothers != null) || (LactStatus == GrazType.LactType.Suckling))
                return GrazType.AgeType.LambCalf;
            else
                return AgeClassMap[(int)AParams.Animal, Math.Min(MeanAge / 365, 3)]; 
        }
        /// <summary>
        /// Get the weight of the male
        /// </summary>
        /// <returns></returns>
        protected double GetMaleWeight()
        {
            double SRWMale;
            double SRWFemale;
            double MaleNWt;
            double FemaleNWt;
            double GroupNWt;
            double Male2Fem;
            double Result;

            if (MaleNo == 0)
                Result = 0.0;
            else if (FemaleNo == 0)
                Result = LiveWeight;
            else
            {
                SRWFemale = StdRefWt * StdMath.XDiv(NoAnimals, AParams.SRWScalars[(int)ReproStatus] * MaleNo + FemaleNo);
                SRWMale = AParams.SRWScalars[(int)ReproStatus] * SRWFemale;
                MaleNWt = MaxNormWtFunc(SRWMale, BirthWt, MeanAge, AParams);
                FemaleNWt = MaxNormWtFunc(SRWFemale, BirthWt, MeanAge, AParams);
                GroupNWt = StdMath.XDiv(MaleNWt * MaleNo + FemaleNWt * FemaleNo, NoAnimals);
                Male2Fem = 1.0 + (MaleNWt / FemaleNWt - 1.0) * Math.Min(1.0, BaseWeight / GroupNWt) * BWGain_Solid;
                Result = LiveWeight * (double)NoAnimals / ((double)MaleNo + (double)FemaleNo / Male2Fem);
            }
            return Result;
        }
        /// <summary>
        /// Get the weight of the female
        /// </summary>
        /// <returns></returns>
        protected double GetFemaleWeight()
        {
            if (FemaleNo == 0)
                return 0.0;
            else
                return LiveWeight + (double)MaleNo / (double)FemaleNo * (LiveWeight - MaleWeight);
        }

        /// <summary>
        /// Herbage ME intake corresponding to 1 dry sheep equivalent (MJ/d)          
        /// </summary>
        private const double DSE_REF_MEI = 8.8;                     // ME intake corresponding to 1.0 dry sheep 

        /// <summary>
        /// Get the animal DSE's
        /// </summary>
        /// <returns></returns>
        protected double GetDSEs()
        {
            double Result;
            double MEIPerHead;

            MEIPerHead = AnimalState.ME_Intake.Solid;
            if (Young != null)
                MEIPerHead = MEIPerHead + NoOffspring * Young.AnimalState.ME_Intake.Solid;
            Result = NoAnimals * MEIPerHead / DSE_REF_MEI;

            return Result;
        }
        /// <summary>
        /// Get the clean fleece weight
        /// </summary>
        /// <returns></returns>
        protected double GetCFW() { return FleeceCutWeight * AParams.WoolC[3]; }
        /// <summary>
        /// CleanFleeceGrowth
        /// </summary>
        /// <returns></returns>
        protected double GetDeltaCFW()
        {
            return DeltaWoolWt * AParams.WoolC[3];
        }
        /// <summary>
        /// Get the maximum milk yield
        /// </summary>
        /// <returns></returns>
        protected double GetMaxMilkYield()
        {
            if (Lactation == 0)
                return 0.0;
            else
                return StdMath.XDiv(Milk_Weight, PropnOfMaxMilk);
        }
        /// <summary>
        /// Get the milk volume
        /// </summary>
        /// <returns></returns>
        protected double GetMilkVolume()
        {
            if (Lactation == 0)
                return 0.0;
            else
                return StdMath.XDiv(Milk_Weight, AParams.LactC[25]);
        }
        /// <summary>
        /// Get the methane energy
        /// </summary>
        /// <returns></returns>
        protected double GetMethaneEnergy()
        {
            return AParams.MethC[1] * AnimalState.DM_Intake.Solid
                      * (AParams.MethC[2] + AParams.MethC[3] * AnimalState.ME_2_DM.Solid
                          + (FeedingLevel + 1.0) * (AParams.MethC[4] - AParams.MethC[5] * AnimalState.ME_2_DM.Solid));
        }
        /// <summary>
        /// Get the methane weight
        /// </summary>
        /// <returns></returns>
        protected double GetMethaneWeight() { return AParams.MethC[6] * MethaneEnergy; }
        /// <summary>
        /// Get the methane volume
        /// </summary>
        /// <returns></returns>
        protected double GetMethaneVolume() { return AParams.MethC[7] * MethaneEnergy; }

        /// <summary>
        /// ptr to the hosts random number factory
        /// </summary>
        public TMyRandom RandFactory;  
        /// <summary>
        /// Pointers to the young of lactating animals, or the mothers of suckling ones
        /// </summary>
        public TAnimalGroup Young;                                      
        /// <summary>
        /// Animal output
        /// </summary>
        public TAnimalOutput AnimalState = new TAnimalOutput();

        /// <summary>
        /// Animal group constructor
        /// </summary>
        /// <param name="Params"></param>
        /// <param name="Repro"></param>
        /// <param name="Number"></param>
        /// <param name="AgeD"></param>
        /// <param name="LiveWt"></param>
        /// <param name="GFW"></param>
        /// <param name="RandomFactory"></param>
        /// <param name="bTakeParams"></param>
        public TAnimalGroup(TAnimalParamSet Params,
                                 GrazType.ReproType Repro,
                                 int Number,
                                 int AgeD,
                                 double LiveWt,
                                 double GFW,                   // NB this is a *fleece* weight             
                                 TMyRandom RandomFactory,
                                 bool bTakeParams = false)
        {
            Construct(Params, Repro, Number, AgeD, LiveWt, GFW, RandomFactory, bTakeParams);
        }

        /// <summary>
        /// Used during construction
        /// </summary>
        /// <param name="Params"></param>
        /// <param name="Repro"></param>
        /// <param name="Number"></param>
        /// <param name="AgeD"></param>
        /// <param name="LiveWt"></param>
        /// <param name="GFW"></param>
        /// <param name="RandomFactory"></param>
        /// <param name="bTakeParams"></param>
        public void Construct(TAnimalParamSet Params,
                                 GrazType.ReproType Repro,
                                 int Number,
                                 int AgeD,
                                 double LiveWt,
                                 double GFW,                   // NB this is a *fleece* weight             
                                 TMyRandom RandomFactory,
                                 bool bTakeParams = false)
        {
            double fWoolAgeFactor;

            RandFactory = RandomFactory;

            if (bTakeParams)
                AParams = Params;
            else
                AParams = new TAnimalParamSet(null, Params);

            if ((Repro == GrazType.ReproType.Male) || (Repro == GrazType.ReproType.Castrated))
            {
                ReproStatus = Repro;
                NoMales = Number;
            }
            else
            {
                ReproStatus = GrazType.ReproType.Empty;
                NoFemales = Number;
            }
            ComputeSRW();
            ImplantEffect = 1.0;
            FIntakeModifier = 1.0;

            MeanAge = AgeD;                                                          // Age of the animals                       
            Ages = new TAgeList(RandFactory);
            Ages.Input(AgeD, NoMales, NoFemales);

            TheRation = new TSupplementRation();
            FIntakeSupp = new TSupplement();

            MateCycle = -1;                                                          // Not recently mated                       

            LiveWeight = LiveWt;
            BirthWt = Math.Min(AParams.StdBirthWt(1), BaseWeight);
            Calc_Weights();

            if (Animal == GrazType.AnimalType.Sheep)
            {
                WoolMicron = AParams.MaxFleeceDiam;                              // Calculation of FleeceCutWeight depends   
                FleeceCutWeight = GFW;                                                //   on the values of NormalWt & WoolMicron 

                fWoolAgeFactor = AParams.WoolC[5] + (1.0 - AParams.WoolC[5]) * (1.0 - Math.Exp(-AParams.WoolC[12] * AgeDays));
                DeltaWoolWt = AParams.FleeceRatio * StdRefWt * fWoolAgeFactor / 365.0;
            }

            Calc_CoatDepth();
            TotalWeight = BaseWeight + WoolWt;

            if (AgeClass == GrazType.AgeType.Mature)                                                // This will re-calculate size and condition
                SetMaxPrevWt(Math.Max(StdRefWt, BaseWeight));
            else
                SetMaxPrevWt(BaseWeight);

            ConditionAtBirthing = Condition;                                         // These terms affect the calculation of  
            PropnOfMaxMilk = 1.0;                                               //   potential intake                     
            LactAdjust = 1.0;

            BasePhos = BaseWeight * AParams.PhosC[9];
            BaseSulf = BaseWeight * AParams.GainC[12] / GrazType.N2Protein * AParams.SulfC[1];

        }

        /// <summary>
        /// CreateYoung
        /// </summary>
        /// <param name="Parents"></param>
        /// <param name="LiveWt"></param>
        public TAnimalGroup(TAnimalGroup Parents, double LiveWt)
        {
            int Number, iAgeDays;
            double YoungWoolWt;
            TAnimalParamSet youngParams;

            RandFactory = Parents.RandFactory;
            youngParams = Parents.constructOffspringParams();
            Number = Parents.NoOffspring * Parents.FemaleNo;
            iAgeDays = Parents.DaysLactating;
            YoungWoolWt = 0.5 * (TAnimalParamSet.fDefaultFleece(Parents.AParams, iAgeDays, GrazType.ReproType.Male, iAgeDays)
                                  + TAnimalParamSet.fDefaultFleece(Parents.AParams, iAgeDays, GrazType.ReproType.Empty, iAgeDays));

            Construct(youngParams, GrazType.ReproType.Male, Number, iAgeDays, LiveWt, YoungWoolWt, RandFactory, true);

            NoMales = Number / 2;
            NoFemales = Number - NoMales;

            Ages = null;
            Ages = new TAgeList(RandFactory);
            Ages.Input(iAgeDays, NoMales, NoFemales);

            LactStatus = GrazType.LactType.Suckling;
            FNoOffspring = Parents.NoOffspring;
            Mothers = Parents;

            ComputeSRW();                                                              // Must do this after assigning a value to   
            Calc_Weights();                                                            //   Mothers                                 */
        }

        /// <summary>
        /// Copy a TAnimalGroup
        /// </summary>
        /// <returns></returns>
        public TAnimalGroup Copy()
        {
            TAnimalGroup theCopy = ObjectCopier.Clone(this);
            theCopy.RandFactory = this.RandFactory;
            if (this.Ages != null)
               theCopy.Ages.RandFactory = this.RandFactory;
            if (this.Young != null)
            {
                theCopy.Young.RandFactory = this.RandFactory;
                theCopy.Young.Mothers = theCopy;
            }
            return theCopy;
        }

        /// <summary>
        /// Weighted average of corresponding fields in the two TAnimalGroups.    }
        /// </summary>
        /// <param name="Total1"></param>
        /// <param name="Total2"></param>
        /// <param name="Field1"></param>
        /// <param name="Field2"></param>
        private void AverageField(int Total1, int Total2, ref double Field1, double Field2)
        {
            if (Total1 + Total2 > 0)
                Field1 = (Field1 * Total1 + Field2 * Total2) / (Total1 + Total2);  // The result of the averaging process      
        }                                                                          //   goes "into place"                      

        /// <summary>
        /// Merge two animal groups
        /// </summary>
        /// <param name="OtherGrp"></param>
        public void Merge(ref TAnimalGroup OtherGrp)
        {
            double fWoodFactor;
            double fWoodOther;
            int Total1;
            int Total2;


            if ((NoFoetuses != OtherGrp.NoFoetuses)                                   // Necessary conditions for merging         
               || (NoOffspring != OtherGrp.NoOffspring)
               || ((Mothers == null) && (ReproStatus != OtherGrp.ReproStatus))
               || (LactStatus != OtherGrp.LactStatus))
                throw new Exception("TAnimalGroup: Error in Merge method");

            Total1 = NoAnimals;
            Total2 = OtherGrp.NoAnimals;

            NoMales += OtherGrp.NoMales;                                       // Take weighted averages of all            
            NoFemales += OtherGrp.NoFemales;                                     // appropriate fields                       
            Ages.Merge(OtherGrp.Ages);
            MeanAge = Ages.MeanAge();

            AverageField(Total1, Total2, ref TotalWeight, OtherGrp.TotalWeight);
            AverageField(Total1, Total2, ref WoolWt, OtherGrp.WoolWt);
            AverageField(Total1, Total2, ref DeltaWoolWt, OtherGrp.DeltaWoolWt);
            AverageField(Total1, Total2, ref WoolMicron, OtherGrp.WoolMicron);
            AverageField(Total1, Total2, ref FCoatDepth, OtherGrp.FCoatDepth);
            AverageField(Total1, Total2, ref BasalWeight, OtherGrp.BasalWeight);
            AverageField(Total1, Total2, ref DeltaBaseWeight, OtherGrp.DeltaBaseWeight);
            AverageField(Total1, Total2, ref MaxPrevWt, OtherGrp.MaxPrevWt);
            AverageField(Total1, Total2, ref BirthWt, OtherGrp.BirthWt);
            AverageField(Total1, Total2, ref StdRefWt, OtherGrp.StdRefWt);
            AverageField(Total1, Total2, ref IntakeLimit, OtherGrp.IntakeLimit);
            Calc_Weights();

            if ((ReproStatus == GrazType.ReproType.EarlyPreg) || (ReproStatus == GrazType.ReproType.LatePreg))
            {
                FoetalAge = (FoetalAge * Total1 + OtherGrp.FoetalAge * Total2)
                             / (Total1 + Total2);
                AverageField(Total1, Total2, ref  FoetalWt, OtherGrp.FoetalWt);
                AverageField(Total1, Total2, ref MidLatePregWt, OtherGrp.MidLatePregWt);
            }

            if (LactStatus == GrazType.LactType.Lactating)
            {
                DaysLactating = (DaysLactating * Total1 + OtherGrp.DaysLactating * Total2)
                                 / (Total1 + Total2);
                AverageField(Total1, Total2, ref Milk_MJProdn, OtherGrp.Milk_MJProdn);
                AverageField(Total1, Total2, ref Milk_ProtProdn, OtherGrp.Milk_ProtProdn);
                AverageField(Total1, Total2, ref Milk_Weight, OtherGrp.Milk_Weight);
                AverageField(Total1, Total2, ref LactRatio, OtherGrp.LactRatio);
            }
            else if ((FPrevOffspring == 0) && (OtherGrp.FPrevOffspring == 0))
            {
                FPrevOffspring = 0;
                DryOffTime = 0;
                ConditionAtBirthing = 0.0;
                OtherGrp.ConditionAtBirthing = 0.0;
            }
            else
            {
                if ((FPrevOffspring == 0)
                   || ((OtherGrp.FPrevOffspring > 0) && (OtherGrp.NoFemales > NoFemales)))
                    FPrevOffspring = OtherGrp.FPrevOffspring;

                fWoodFactor = WOOD(DryOffTime, AParams.IntakeC[8], AParams.IntakeC[9]);
                fWoodOther = WOOD(OtherGrp.DryOffTime, AParams.IntakeC[8], AParams.IntakeC[9]);
                AverageField(Total1, Total2, ref fWoodFactor, fWoodOther);
                DryOffTime = InverseWOOD(fWoodFactor, AParams.IntakeC[8], AParams.IntakeC[9], true);

                if (ConditionAtBirthing == 0.0)
                    ConditionAtBirthing = 1.0;
                if (OtherGrp.ConditionAtBirthing == 0.0)
                    OtherGrp.ConditionAtBirthing = 1.0;
            }
            AverageField(Total1, Total2, ref ConditionAtBirthing, OtherGrp.ConditionAtBirthing);
            AverageField(Total1, Total2, ref PropnOfMaxMilk, OtherGrp.PropnOfMaxMilk);
            AverageField(Total1, Total2, ref LactAdjust, OtherGrp.LactAdjust);

            if (Young != null)
                Young.Merge(ref OtherGrp.Young);
            OtherGrp = null;
        }

        /// <summary>
        /// Split the animal group
        /// </summary>
        /// <param name="Number"></param>
        /// <param name="ByAge"></param>
        /// <param name="Diffs"></param>
        /// <param name="YngDiffs"></param>
        /// <returns></returns>
        public TAnimalGroup Split(int Number, bool ByAge, TDifferenceRecord Diffs, TDifferenceRecord YngDiffs)
        {
            TAnimalGroup Result;
            int SplitM, SplitF;
            string msg = "";

            if ((Number < 0) || (Number > NoAnimals))
            {
                if (Number < 0)
                    msg = "Number of animals to split off should be > 0";
                if (Number > NoAnimals)
                    msg = "Trying to split off more than " + NoAnimals.ToString() + " animals that exist in the " + GrazType.AgeText[(int)this.AgeClass] + " age class";
                throw new Exception("TAnimalGroup: Error in Split method: " + msg);
            }

            if (Mothers != null)
            {
                SplitM = Convert.ToInt32(Math.Round(StdMath.XDiv(Number * 1.0 * NoMales, NoAnimals)));
                SplitF = Number - SplitM;
            }
            else if ((ReproStatus == GrazType.ReproType.Male) || (ReproStatus == GrazType.ReproType.Castrated))
            {
                SplitM = Number;
                SplitF = 0;
            }
            else
            {
                SplitF = Number;
                SplitM = 0;
            }

            Result = SplitSex(SplitM, SplitF, ByAge, Diffs);
            if (Young != null)
            {
                Result.Young = Young.Split(Number * NoOffspring, false, YngDiffs, NODIFF);
                Result.Young.Mothers = Result.Copy();
            }
            return Result;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="MaleScale"></param>
        /// <param name="NM"></param>
        /// <param name="NF"></param>
        /// <returns></returns>
        private double SexAve(double MaleScale, int NM, int NF)
        {
            return StdMath.XDiv(MaleScale * NM + NF, NM + NF);
        }
        /// <summary>
        /// Split the numbers off the group
        /// </summary>
        /// <param name="NewGroups"></param>
        /// <param name="NF"></param>
        /// <param name="NYM"></param>
        /// <param name="NYF"></param>
        private void SplitNumbers(ref TAnimalList NewGroups, int NF, int NYM, int NYF)
        {
            TAnimalGroup TempYoung;
            TAnimalGroup SplitGroup;
            TAnimalGroup SplitYoung;
            double DiffRatio;
            TDifferenceRecord YngDiffs;

            YngDiffs = new TDifferenceRecord() { StdRefWt = NODIFF.StdRefWt, BaseWeight = NODIFF.BaseWeight, FleeceWt = NODIFF.FleeceWt };            
            //WITH Young DO
            if ((Young.MaleNo > 0) && (Young.FemaleNo > 0))
            {
                DiffRatio = (SexAve(AParams.SRWScalars[(int)ReproStatus], NYM, NYF)
                              - SexAve(AParams.SRWScalars[(int)ReproStatus], Young.NoMales - NYM, Young.NoFemales - NYF))
                            / SexAve(AParams.SRWScalars[(int)ReproStatus], Young.NoMales, Young.NoFemales);
                YngDiffs.StdRefWt = StdRefWt * DiffRatio;
                YngDiffs.BaseWeight = BaseWeight * DiffRatio;
                YngDiffs.FleeceWt = WoolWt * DiffRatio;
            }

            TempYoung = Young;
            Young = null;
            SplitGroup = SplitSex(0, NF, false, NODIFF);
            SplitYoung = TempYoung.SplitSex(NYM, NYF, false, YngDiffs);
            Young = TempYoung;
            Young.Mothers = this;
            SplitGroup.Young = SplitYoung;
            SplitGroup.Young.Mothers = SplitGroup;
            CheckAnimList(ref NewGroups);
            NewGroups.Add(SplitGroup);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="NewGroups"></param>
        public void SplitYoung(ref TAnimalList NewGroups)
        {
            int NoToSplit;

            if (Young != null)
            {
                if (NoOffspring == 1)
                {
                    NoToSplit = Young.FemaleNo;
                    SplitNumbers(ref NewGroups, NoToSplit, 0, NoToSplit);
                }
                else if (NoOffspring == 2)
                {
                    NoToSplit = Convert.ToInt32(Math.Min(Young.MaleNo, Young.FemaleNo)) / 2;    // One male, one female                     
                    if (((Young.FemaleNo - NoToSplit) % 2) != 0)  //if odd                      // Ensures Young.FemaleNo (and hence        
                        NoToSplit++;                                                            //   Young.MaleNo) is even after the call   
                    SplitNumbers(ref NewGroups, NoToSplit, NoToSplit, NoToSplit);               //   to SplitBySex                          
                    NoToSplit = Young.FemaleNo / 2;                                             // Twin females                             
                    SplitNumbers(ref NewGroups, NoToSplit, 0, 2 * NoToSplit);
                }
            }
        }

        /// <summary>
        /// Is an animal group similar enough to another for them to be merged?       
        /// </summary>
        /// <param name="AG"></param>
        /// <returns></returns>
        public bool Similar(TAnimalGroup AG)
        {
            bool Result = ((Genotype.sName == AG.Genotype.sName)
                  && (ReproStatus == AG.ReproStatus)
                  && (NoFoetuses == AG.NoFoetuses)
                  && (NoOffspring == AG.NoOffspring)
                  && (MateCycle == AG.MateCycle)
                  && (DaysToMate == AG.DaysToMate)
                  && (Pregnancy == AG.Pregnancy)
                  && (LactStatus == AG.LactStatus)
                  && (Math.Abs(Lactation - AG.Lactation) < 7)
                  && ((Young == null) == (AG.Young == null))
                  && (ImplantEffect == AG.ImplantEffect));
            if (MeanAge < 365)
                Result = (Result && (MeanAge == AG.MeanAge));
            else
                Result = (Result && (Math.Min(MeanAge / 30, 37) == Math.Min(AG.MeanAge / 30, 37)));
            if (Young != null)
                Result = (Result && (Young.ReproStatus == AG.Young.ReproStatus));

            return Result;
        }

        // Initialisation properties .....................................
        /// <summary>
        /// The animals genotype
        /// </summary>
        public TAnimalParamSet Genotype
        {
            get { return AParams; }
            set { setGenotype(value); }
        }
        /// <summary>
        /// Number of animals in the group
        /// </summary>
        public int NoAnimals
        {
            get { return GetNoAnimals(); }
            set { SetNoAnimals(value); }
        }
        /// <summary>
        /// Number of males
        /// </summary>
        public int MaleNo
        {
            get { return NoMales; }
            set { NoMales = value; }
        }
        /// <summary>
        /// Number of females
        /// </summary>
        public int FemaleNo
        {
            get { return NoFemales; }
            set { NoFemales = value; }
        }
        /// <summary>
        /// Mean age of the group
        /// </summary>
        public int AgeDays
        {
            get { return MeanAge; }
            set { MeanAge = value; }
        }
        /// <summary>
        /// Libe weight of the group
        /// </summary>
        public double LiveWeight
        {
            get { return TotalWeight; }
            set { SetLiveWt(value); }
        }
        /// <summary>
        /// Animal base weight
        /// </summary>
        public double BaseWeight
        {
            get { return BasalWeight; }
            set { BasalWeight = value; }
        }
        /// <summary>
        /// Fleece-free, conceptus-free weight, but including the wool stubble        
        /// </summary>
        public double EmptyShornWeight
        {
            get { return BaseWeight + CoatDepth2Wool(STUBBLE_MM); }
            set
            {
                BaseWeight = value - CoatDepth2Wool(STUBBLE_MM);
                Calc_Weights();
            }
        }
        /// <summary>
        /// Cut weight of fleece
        /// </summary>
        public double FleeceCutWeight
        {
            get { return GetFleeceCutWt(); }
            set { SetFleeceCutWt(value); }
        }
        /// <summary>
        /// Wool weight
        /// </summary>
        public double WoolWeight
        {
            get { return WoolWt; }
            set { SetWoolWt(value); }
        }
        /// <summary>
        /// Depth of coat
        /// </summary>
        public double CoatDepth { get { return FCoatDepth; } set { SetCoatDepth(value); } }
        /// <summary>
        /// Maximum previous weight
        /// </summary>
        public double MaxPrevWeight { get { return MaxPrevWt; } set { SetMaxPrevWt(value); } }
        /// <summary>
        /// Wool fibre diameter
        /// </summary>
        public double FibreDiam { get { return WoolMicron; } set { WoolMicron = value; } }
        /// <summary>
        /// Animal parameters for the animal mated to
        /// </summary>
        public TAnimalParamSet MatedTo { get { return FMatedTo; } set { setMatedTo(value); } }
        /// <summary>
        /// Stage of pregnancy
        /// </summary>
        public int Pregnancy
        {
            get { return FoetalAge; }
            set { SetPregnancy(value); }
        }
        /// <summary>
        /// Days lactating
        /// </summary>
        public int Lactation
        {
            get { return DaysLactating; }
            set { SetLactation(value); }
        }
        /// <summary>
        /// Number of foetuses
        /// </summary>
        public int NoFoetuses { get { return FNoFoetuses; } set { SetNoFoetuses(value); } }
        /// <summary>
        /// Number of offspring
        /// </summary>
        public int NoOffspring { get { return FNoOffspring; } set { SetNoOffspring(value); } }
        /// <summary>
        /// Condition at birth
        /// </summary>
        public double BirthCondition { get { return ConditionAtBirthing; } set { ConditionAtBirthing = value; } }
        /// <summary>
        /// Condition score
        /// </summary>
        /// <param name="System"></param>
        /// <returns></returns>
        public double fConditionScore(TAnimalParamSet.TCond_System System) { return TAnimalParamSet.Condition2CondScore(Condition, System); }
        /// <summary>
        /// Set the condition score
        /// </summary>
        /// <param name="fValue"></param>
        /// <param name="System"></param>
        public void setConditionScore(double fValue, TAnimalParamSet.TCond_System System)
        {
            BaseWeight = NormalWt * TAnimalParamSet.CondScore2Condition(fValue, System);
            Calc_Weights();
        }

        /// <summary>
        /// Sets the value of MaxPrevWeight using current base weight, age and a      
        /// (relative) body condition. Intended for use with young animals.           
        /// </summary>
        /// <param name="fBodyCond"></param>
        public void setConditionAtWeight(double fBodyCond)
        {
            double fMaxNormWt;
            double fNewMaxPrevWt;

            fMaxNormWt = MaxNormWtFunc(StdRefWt, BirthWt, MeanAge, AParams);
            if (BaseWeight >= fMaxNormWt)
                fNewMaxPrevWt = BaseWeight;
            else
            {
                fNewMaxPrevWt = (BaseWeight - fBodyCond * AParams.GrowthC[3] * fMaxNormWt)
                                 / (fBodyCond * (1.0 - AParams.GrowthC[3]));
                fNewMaxPrevWt = Math.Max(BaseWeight, Math.Min(fNewMaxPrevWt, fMaxNormWt));
            }

            SetMaxPrevWt(fNewMaxPrevWt);
        }

        /// <summary>
        /// Wood-type function, scaled to give a maximum of 1.0 at time Tmax          
        /// </summary>
        /// <param name="T"></param>
        /// <param name="Tmax"></param>
        /// <param name="B"></param>
        /// <returns></returns>
        public double WOOD(double T, double Tmax, double B)
        {
            return Math.Pow(T / Tmax, B) * Math.Exp(B * (1 - T / Tmax));
        }

        /// <summary>
        /// Inverse of the WOOD function, evaluated iteratively                       
        /// </summary>
        public double InverseWOOD(double Y, double Tmax, double B, bool bDeclining)
        {
            double X0, X1;
            double Result;

            if (Y <= 0.0)
                Result = 0.0;
            else if (Y >= 1.0)
                Result = Tmax;
            else
            {
                if (!bDeclining)                                                   //Initial guess                            
                    X1 = Math.Min(0.99, Y);
                else
                    X1 = Math.Max(1.01, Math.Exp(B * (1.0 - Y)));

                bool more = true;
                do                                                                 //Newton-Raphson solution                   
                {
                    X0 = X1;
                    X1 = X0 - (1.0 - Y / WOOD(X0, 1.0, B)) * X0 / (B * (1.0 - X0));
                    more = !(Math.Abs(X0 - X1) < 1.0E-5);
                } while (more);
                Result = (X0 + X1) / 2.0 * Tmax;
            }
            return Result;
        }

        /// <summary>
        /// Set the drying off time
        /// </summary>
        /// <param name="iDaysSinceBirth"></param>
        /// <param name="iDaysSinceDryoff"></param>
        /// <param name="iPrevSuckling"></param>
        public void setDryoffTime(int iDaysSinceBirth, int iDaysSinceDryoff, int iPrevSuckling = 1)
        {
            double fLactLength;
            double fWoodFunc;

            FPrevOffspring = iPrevSuckling;

            fLactLength = iDaysSinceBirth - iDaysSinceDryoff;
            if ((LactStatus != GrazType.LactType.Dry) || (fLactLength <= 0))
                DryOffTime = 0.0;
            else if (fLactLength >= AParams.IntakeC[8])
                DryOffTime = fLactLength + AParams.IntakeC[19] * iDaysSinceDryoff;
            else
            {
                fWoodFunc = WOOD(fLactLength, AParams.IntakeC[8], AParams.IntakeC[9]);
                fLactLength = InverseWOOD(fWoodFunc, AParams.IntakeC[8], AParams.IntakeC[9], true);
                DryOffTime = fLactLength + AParams.IntakeC[19] * iDaysSinceDryoff;
            }
        }

        // Inputs for model dynamics .....................................
        /// <summary>
        /// Steepness of the paddock
        /// </summary>
        public double PaddSteep { get { return Steepness; } set { Steepness = value; } }
        /// <summary>
        /// The animals environment
        /// </summary>
        public TAnimalWeather Weather { get { return TheEnv; } set { TheEnv = value; } }
        /// <summary>
        /// The herbage being eaten
        /// </summary>
        public GrazType.TGrazingInputs Herbage { get { return Inputs; } set { Inputs = value; } }
        /// <summary>
        /// 
        /// </summary>
        public double WaterLogging { get { return WaterLog; } set { WaterLog = value; } }

        /// <summary>
        /// 
        /// </summary>
        public TSupplementRation RationFed { get { return TheRation; } }
        /// <summary>
        /// Animals per hectare
        /// </summary>
        public double AnimalsPerHa { get { return FAnimalsPerHa; } set { FAnimalsPerHa = value; } }
        /// <summary>
        /// Distance walked
        /// </summary>
        public double DistanceWalked { get { return FDistanceWalked; } set { FDistanceWalked = value; } }
        /// <summary>
        /// 
        /// </summary>
        public double IntakeModifier { get { return FIntakeModifier; } set { FIntakeModifier = value; } }

        /// <summary>
        /// Used in GrazFeed to initialise the state variables for which yesterday's  
        /// value must be known in order to get today's calculation                   
        /// </summary>
        /// <param name="PrevGroup"></param>
        public void SetUpForYesterday(TAnimalGroup PrevGroup)
        {
            IntakeLimit = PrevGroup.IntakeLimit;
            FeedingLevel = PrevGroup.FeedingLevel;
            Milk_MJProdn = PrevGroup.Milk_MJProdn;
            Milk_ProtProdn = PrevGroup.Milk_ProtProdn;
            PropnOfMaxMilk = PrevGroup.PropnOfMaxMilk;
            AnimalState.LowerCritTemp = PrevGroup.AnimalState.LowerCritTemp;
            if ((Young != null) && (PrevGroup.Young != null))
                Young.SetUpForYesterday(PrevGroup.Young);
        }

        // Daily simulation logic ........................................
        /// <summary>
        /// Advance the age of the animals
        /// </summary>
        /// <param name="AG"></param>
        /// <param name="NoDays"></param>
        /// <param name="NewGroups"></param>
        private void AdvanceAge(TAnimalGroup AG, int NoDays, ref TAnimalList NewGroups)
        {
            AG.MeanAge += NoDays;
            AG.Ages.AgeBy(NoDays);
            if (AG.Young != null)
                AG.Young.Age(NoDays, ref NewGroups);
            if (AG.LactStatus == GrazType.LactType.Lactating)
                AG.DaysLactating += NoDays;
            else if (AG.DryOffTime > 0.0)
                AG.DryOffTime = AG.DryOffTime + AParams.IntakeC[19] * NoDays;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="NoDays"></param>
        /// <param name="NewGroups"></param>
        public void Age(int NoDays, ref TAnimalList NewGroups)
        {
            int NewOffset, I;

            if (ChillIndex == StdMath.DMISSING)
                ChillIndex = ChillFunc(TheEnv.MeanTemp, TheEnv.WindSpeed, TheEnv.Precipitation);
            else
                ChillIndex = 16.0 / 17.0 * ChillIndex + 1.0 / 17.0 * ChillFunc(TheEnv.MeanTemp, TheEnv.WindSpeed, TheEnv.Precipitation);

            if (NewGroups != null)
                NewOffset = NewGroups.Count;
            else
                NewOffset = 0;
            if (Mothers == null)                                                    // Deaths must be done before age is        
                Kill(ChillIndex, ref NewGroups);                                          //   incremented                           

            if (YoungStopSuckling())
                Lactation = 0;

            AdvanceAge(this, NoDays, ref NewGroups);
            if (NewGroups != null)
                for (I = NewOffset; I <= NewGroups.Count - 1; I++)
                    AdvanceAge(NewGroups.At(I), NoDays, ref NewGroups);

            switch (ReproStatus)
            {
                case GrazType.ReproType.Empty: if (MateCycle >= 0)
                    {
                        DaysToMate--;
                        if (DaysToMate <= 0)
                            MateCycle = -1;
                        else
                            MateCycle = (MateCycle + 1) % AParams.OvulationPeriod;
                        if (MateCycle == 0)
                            Conceive(ref NewGroups);
                    }
                    break;
                case GrazType.ReproType.EarlyPreg:
                    FoetalAge++;
                    if (FoetalAge >= AParams.Gestation - LatePregLength)
                        ReproStatus = GrazType.ReproType.LatePreg;
                    break;
                case GrazType.ReproType.LatePreg:
                    FoetalAge++;
                    if (AnimalsDynamicGlb)
                        if ((Animal == GrazType.AnimalType.Sheep) && (FoetalAge == AParams.Gestation - LatePregLength / 2))
                            MidLatePregWt = BaseWeight;
                        else if (FoetalAge == AParams.Gestation - 1)
                            KillEndPreg(ref NewGroups);
                        else if (FoetalAge >= AParams.Gestation)
                        {
                            Lactation = 1;                                // Create lambs or calves                   
                            NoOffspring = NoFoetuses;
                            Young.BaseWeight = FoetalWt - Young.WoolWt;
                            Young.MaxPrevWt = Young.BaseWeight;
                            Pregnancy = 0;                                // End pregnancy                            
                        }
                    break;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void Grow()
        {
            double fRDPScalar;
            TStateInfo initState = new TStateInfo();

            storeStateInfo(ref initState);
            Calc_IntakeLimit();
            Grazing(1.0, true, false);
            Nutrition();
            fRDPScalar = RDP_IntakeFactor();
            if (fRDPScalar < 1.0)
            {
                revertStateInfo(initState);
                IntakeLimit = IntakeLimit * fRDPScalar;
                Grazing(1.0, true, false);                                            // This call resets AnimalState, hence the  
                Nutrition();                                                              //   separate variable for the RDP factor   
            }
            completeGrowth(fRDPScalar);
        }

        /// <summary>
        /// { Routine to compute the potential intake of a group of animals.  The       
        /// result is stored as TheAnimals^.IntakeLimit.  A variety of other fields   
        /// of TheAnimals^ are also updated: the normal weight, mature normal weight, 
        /// highest previous weight (in young animals), relative size and relative    
        /// condition.                                                                
        /// </summary>
        public void Calc_IntakeLimit()
        {
            double fLactTime;                                                              // Scaled days since birth of young         
            double WeightLoss;                                                            // Mean daily weight loss during lactation  
            //   as a proportion of SRW                 
            double CriticalLoss;                                                           // Threshold value of WeightLoss            
            double fTempDiff;
            double fTempAmpl;
            double BelowLCT;
            double X;
            double fCondFactor;
            double fYoungFactor;
            double fHeatFactor;
            double fLactFactor;
            int iLactNo;

            Calc_Weights();                                                             // Compute size and condition               

            if (Condition > 1.0)  // and (LactStatus <> Lactating) then  { No longer exclude lactating females. See bug#2223 }
                fCondFactor = Condition * (AParams.IntakeC[20] - Condition) / (AParams.IntakeC[20] - 1.0);
            else
                fCondFactor = 1.0;

            if (LactStatus == GrazType.LactType.Suckling)
                fYoungFactor = (1.0 - Mothers.PropnOfMaxMilk)
                                / (1.0 + Math.Exp(-AParams.IntakeC[3] * (MeanAge - AParams.IntakeC[4])));
            else
                fYoungFactor = 1.0;

            if (TheEnv.MinTemp < AnimalState.LowerCritTemp)                       // Integrate sinusoidal temperature         }
            {                                                                     //   function over the part below LCT       }
                fTempDiff = TheEnv.MeanTemp - AnimalState.LowerCritTemp;
                fTempAmpl = 0.5 * (TheEnv.MaxTemp - TheEnv.MinTemp);
                X = Math.Acos(Math.Max(-1.0, Math.Min(1.0, fTempDiff / fTempAmpl)));
                BelowLCT = (-fTempDiff * X + fTempAmpl * Math.Sin(X)) / Math.PI;
                fHeatFactor = 1.0 + AParams.IntakeC[17] * BelowLCT * StdMath.DIM(1.0, TheEnv.Precipitation / AParams.IntakeC[18]);
            }
            else
                fHeatFactor = 1.0;

            if (TheEnv.MinTemp >= AParams.IntakeC[7])                             // High temperatures depress intake         }
                fHeatFactor = fHeatFactor * (1.0 - AParams.IntakeC[5] * StdMath.DIM(TheEnv.MeanTemp, AParams.IntakeC[6]));
            if (LactStatus != GrazType.LactType.Lactating)
                LactAdjust = 1.0;
#pragma warning disable 162 //unreachable code
            else if (!AnimalsDynamicGlb)                                        // In the dynamic model, LactAdjust is a    }
            {                                                                     //   state variable computed in the         }
                WeightLoss = Size * StdMath.XDiv(ConditionAtBirthing - Condition,           //   lactation routine; for GrazFeed, it is }
                                             DaysLactating);                           //   estimated with these equations         }
                CriticalLoss = AParams.IntakeC[14] * Math.Exp(-Math.Pow(AParams.IntakeC[13] * DaysLactating, 2));
                if (WeightLoss > CriticalLoss)
                    LactAdjust = (1.0 - AParams.IntakeC[12] * WeightLoss / AParams.IntakeC[13]);
                else
                    LactAdjust = 1.0;
            }
#pragma warning restore 162
            if (LactStatus == GrazType.LactType.Lactating)
            {
                fLactTime = DaysLactating;
                iLactNo = NoSuckling();
            }
            else
            {
                fLactTime = DryOffTime;
                iLactNo = FPrevOffspring;
            }

            if (((ReproStatus == GrazType.ReproType.Male || ReproStatus == GrazType.ReproType.Castrated)) || (Mothers != null))
                fLactFactor = 1.0;
            else
                fLactFactor = 1.0 + AParams.IntakeLactC[iLactNo]
                                     * ((1.0 - AParams.IntakeC[15]) + AParams.IntakeC[15] * ConditionAtBirthing)
                                     * WOOD(fLactTime, AParams.IntakeC[8], AParams.IntakeC[9])
                                     * LactAdjust;

            IntakeLimit = AParams.IntakeC[1] * StdRefWt * Size * (AParams.IntakeC[2] - Size)
                           * fCondFactor * fYoungFactor * fHeatFactor * fLactFactor * FIntakeModifier;

        }
        /// <summary>
        /// Reset the grazing values
        /// </summary>
        public void Reset_Grazing()
        {
            AnimalState = new TAnimalOutput();
            Supp_FWI = 0.0;
            Start_FU = 1.0;
            Array.Resize(ref NetSupp_DMI, TheRation.Count);
            Array.Resize(ref TimeStepNetSupp_DMI, TheRation.Count);
            for (int Idx = 0; Idx < NetSupp_DMI.Length; Idx++)
                NetSupp_DMI[Idx] = 0.0;
        }

        /// <summary>
        /// Output at this step
        /// </summary>
        public TAnimalOutput TimeStepState;

        /// <summary>
        /// Update the value for the timestep
        /// </summary>
        /// <param name="TimeStep"></param>
        /// <param name="full"></param>
        /// <param name="TS"></param>
        private void UpdateFloat(double TimeStep, ref double full, double TS)
        {
            full = full + TimeStep * TS;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="full"></param>
        /// <param name="FullDenom"></param>
        /// <param name="TS"></param>
        /// <param name="TSDenom"></param>
        /// <param name="DT"></param>
        private void UpdateAve(ref double full, double FullDenom, double TS, double TSDenom, double DT)
        {
            full = StdMath.XDiv(full * FullDenom + TS * (DT * TSDenom), FullDenom + (DT * TSDenom));
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="TimeStep"></param>
        /// <param name="full"></param>
        /// <param name="TS"></param>
        private void UpdateGrazingOutputs(double TimeStep, ref GrazType.TGrazingOutputs full, GrazType.TGrazingOutputs TS)
        {
            int I, Sp;

            for (I = 1; I <= GrazType.DigClassNo; I++)
                full.Herbage[I] = full.Herbage[I] + TimeStep * TS.Herbage[I];
            for (Sp = 1; Sp <= GrazType.MaxPlantSpp; Sp++)
            {
                for (I = GrazType.UNRIPE; I <= GrazType.RIPE; I++)
                    full.Seed[Sp, I] = full.Seed[Sp, I] + TimeStep * TS.Seed[Sp, I];
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="full"></param>
        /// <param name="TS"></param>
        /// <param name="DT"></param>
        private void UpdateIntakeRecord(ref GrazType.IntakeRecord full, GrazType.IntakeRecord TS, double DT)
        {
            full.Digestibility = StdMath.XDiv(full.Digestibility * full.Biomass +
                                       TS.Digestibility * (DT * TS.Biomass),
                                     full.Biomass + DT * TS.Biomass);
            full.CrudeProtein = StdMath.XDiv(full.CrudeProtein * full.Biomass +
                                       TS.CrudeProtein * (DT * TS.Biomass),
                                     full.Biomass + DT * TS.Biomass);
            full.Degradability = StdMath.XDiv(full.Degradability * (full.CrudeProtein * full.Biomass) +
                                       TS.Degradability * (TS.CrudeProtein * DT * TS.Biomass),
                                     full.CrudeProtein * full.Biomass +
                                       DT * TS.CrudeProtein * TS.Biomass);
            full.HeightRatio = StdMath.XDiv(full.HeightRatio * full.Biomass +
                                       TS.HeightRatio * (DT * TS.Biomass),
                                     full.Biomass + DT * TS.Biomass);
            full.Biomass = full.Biomass + DT * TS.Biomass;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="TimeStep"></param>
        /// <param name="SuppFullDay"></param>
        /// <param name="full"></param>
        /// <param name="TS"></param>
        private void UpdateDietRecord(double TimeStep, bool SuppFullDay, ref TDietRecord full, TDietRecord TS)
        {
            full.Herbage = full.Herbage + TimeStep * TS.Herbage;
            if (SuppFullDay)
                full.Supp = full.Supp + TS.Supp;
            else
                full.Supp = full.Supp + TimeStep * TS.Supp;
            full.Milk = full.Milk + TimeStep * TS.Milk;
            full.Solid = full.Herbage + full.Supp;
            full.Total = full.Solid + full.Milk;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="full"></param>
        /// <param name="FullDenom"></param>
        /// <param name="TS"></param>
        /// <param name="TSDenom"></param>
        /// <param name="HerbDT"></param>
        /// <param name="SuppDT"></param>
        private void UpdateDietAve(ref TDietRecord full, TDietRecord FullDenom, TDietRecord TS, TDietRecord TSDenom, double HerbDT, double SuppDT)
        {
            UpdateAve(ref full.Herbage, FullDenom.Herbage, TS.Herbage, TSDenom.Herbage, HerbDT);
            UpdateAve(ref full.Supp, FullDenom.Supp, TS.Supp, TSDenom.Supp, SuppDT);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="TimeStep"></param>
        /// <param name="SuppFullDay"></param>
        /// <param name="SuppRI"></param>
        private void UpdateAnimalState(double TimeStep, bool SuppFullDay, double SuppRI)
        {
            double SuppTS;

            if (SuppFullDay)
                SuppTS = 1.0;
            else
                SuppTS = TimeStep;

            if (TimeStep == 1)
                AnimalState = this.TimeStepState.Copy(); 
            else
            {
                UpdateGrazingOutputs(TimeStep, ref AnimalState.IntakePerHead, this.TimeStepState.IntakePerHead);
                UpdateIntakeRecord(ref AnimalState.PaddockIntake, this.TimeStepState.PaddockIntake, TimeStep);
                UpdateIntakeRecord(ref AnimalState.SuppIntake, this.TimeStepState.SuppIntake, SuppTS);

                // compute these averages *before* cumulating DM_Intake & CP_Intake 

                UpdateDietAve(ref AnimalState.Digestibility, AnimalState.DM_Intake,
                               this.TimeStepState.Digestibility, this.TimeStepState.DM_Intake,
                               TimeStep, SuppTS);
                UpdateDietAve(ref AnimalState.ProteinConc, AnimalState.DM_Intake,
                               this.TimeStepState.ProteinConc, this.TimeStepState.DM_Intake,
                               TimeStep, SuppTS);
                UpdateDietAve(ref AnimalState.ME_2_DM, AnimalState.DM_Intake,
                               this.TimeStepState.ME_2_DM, this.TimeStepState.DM_Intake,
                               TimeStep, SuppTS);
                UpdateDietAve(ref AnimalState.CorrDgProt, AnimalState.CP_Intake,
                               this.TimeStepState.CorrDgProt, this.TimeStepState.CP_Intake,
                               TimeStep, SuppTS);

                UpdateDietRecord(TimeStep, SuppFullDay, ref AnimalState.CP_Intake, this.TimeStepState.CP_Intake);
                UpdateDietRecord(TimeStep, SuppFullDay, ref AnimalState.ME_Intake, this.TimeStepState.ME_Intake);
                UpdateDietRecord(TimeStep, SuppFullDay, ref AnimalState.DM_Intake, this.TimeStepState.DM_Intake);

                // compute these averages *after* cumulating DM_Intake & CP_Intake 

                AnimalState.Digestibility.Solid = StdMath.XDiv(AnimalState.Digestibility.Supp * AnimalState.DM_Intake.Supp +
                                             AnimalState.Digestibility.Herbage * AnimalState.DM_Intake.Herbage,
                                             AnimalState.DM_Intake.Solid);
                AnimalState.ProteinConc.Solid = StdMath.XDiv(AnimalState.CP_Intake.Solid, AnimalState.DM_Intake.Solid);
                AnimalState.ME_2_DM.Solid = StdMath.XDiv(AnimalState.ME_Intake.Solid, AnimalState.DM_Intake.Solid);
            }   //_ WITH FullState _

            Supp_FWI = Supp_FWI + SuppTS * StdMath.XDiv(IntakeLimit * SuppRI, IntakeSuppt.DM_Propn);
            for (int Idx = 0; Idx < NetSupp_DMI.Length; Idx++)
                NetSupp_DMI[Idx] = NetSupp_DMI[Idx] + SuppTS * TimeStepNetSupp_DMI[Idx];
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="DeltaT">Fraction of an animal's active day</param>
        /// <param name="Reset">TRUE at the start of the day</param>
        /// <param name="FeedSuppFirst"></param>
        /// <param name="pastIntakeRate"></param>
        /// <param name="fSuppIntakeRate"></param>
        public void Grazing(double DeltaT,
                             bool Reset,
                             bool FeedSuppFirst,
                             ref GrazType.TGrazingOutputs pastIntakeRate,
                             ref double fSuppIntakeRate)
        {
            double[] HerbageRI = new double[GrazType.DigClassNo + 1];
            double MaintMEIScalar;
            double WaterLogScalar;
            double[,] SeedRI = new double[GrazType.MaxPlantSpp + 1, 3];
            double SuppRI = 0;


            // Do this before resetting AnimalState!    
            if ((!AnimalsDynamicGlb || (AnimalState.ME_Intake.Total == 0.0) || (WaterLog == 0.0) || (Steepness > 1.0)))                       // Waterlogging effect only on level ground 
                WaterLogScalar = 1.0;                                                 // The published model assumes WaterLog=0   
            else if ((AnimalState.EnergyUse.Gain == 0.0) || (AnimalState.Efficiency.Gain == 0.0))
                WaterLogScalar = 1.0;
            else
            {
                MaintMEIScalar = Math.Max(0.0, (AnimalState.EnergyUse.Gain / AnimalState.Efficiency.Gain) / AnimalState.ME_Intake.Total);
                WaterLogScalar = StdMath.DIM(1.0, MaintMEIScalar * WaterLog);
            }


            if (Reset)                                                             // First time step of the day?              
                Reset_Grazing();

            this.TimeStepState = new TAnimalOutput();

            CalculateRelIntake(this, DeltaT, FeedSuppFirst,
                                WaterLogScalar,                                       // The published model assumes WaterLog=0   
                                ref HerbageRI, ref SeedRI, ref SuppRI);
            DescribeTheDiet(ref HerbageRI, ref SeedRI, ref SuppRI, ref this.TimeStepState);
            UpdateAnimalState(DeltaT, FeedSuppFirst, SuppRI);

            pastIntakeRate.CopyFrom(this.TimeStepState.IntakePerHead);

            fSuppIntakeRate = StdMath.XDiv(IntakeLimit * SuppRI, IntakeSuppt.DM_Propn);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="DeltaT"></param>
        /// <param name="Reset"></param>
        /// <param name="FeedSuppFirst"></param>
        public void Grazing(double DeltaT, bool Reset, bool FeedSuppFirst)
        {
            GrazType.TGrazingOutputs dummyPastIntake = new GrazType.TGrazingOutputs();
            double fDummySuppIntake = 0.0;

            Grazing(DeltaT, Reset, FeedSuppFirst, ref dummyPastIntake, ref fDummySuppIntake);
        }

        /// <summary>
        /// Compute proportional contribution of diet components (milk, fodder and      
        /// supplement) and the efficiencies of energy use                            
        /// This procedure corresponds to section 5 of the model specification        
        /// </summary>
        private void Efficiencies()
        {
            double HerbageEfficiency;                                                      // Efficiencies for gain from herbage &     
            double SuppEfficiency;                                              //   supplement intake                      

            this.AnimalState.DietPropn.Milk = StdMath.XDiv(AnimalState.ME_Intake.Milk, AnimalState.ME_Intake.Total);
            AnimalState.DietPropn.Solid = 1.0 - AnimalState.DietPropn.Milk;
            AnimalState.DietPropn.Supp = AnimalState.DietPropn.Solid * StdMath.XDiv(AnimalState.ME_Intake.Supp, AnimalState.ME_Intake.Solid);
            AnimalState.DietPropn.Herbage = AnimalState.DietPropn.Solid - AnimalState.DietPropn.Supp;

            if (AnimalState.ME_Intake.Total < GrazType.VerySmall)                                    // Efficiencies of various uses of ME       
            {
                AnimalState.Efficiency.Maint = AParams.EfficC[4];
                AnimalState.Efficiency.Lact = AParams.EfficC[7];
                AnimalState.Efficiency.Preg = AParams.EfficC[8];
            }
            else
            {
                AnimalState.Efficiency.Maint = AnimalState.DietPropn.Solid * (AParams.EfficC[1] + AParams.EfficC[2] * AnimalState.ME_2_DM.Solid) +
                                    AnimalState.DietPropn.Milk * AParams.EfficC[3];
                AnimalState.Efficiency.Lact = AParams.EfficC[5] + AParams.EfficC[6] * AnimalState.ME_2_DM.Solid;
                AnimalState.Efficiency.Preg = AParams.EfficC[8];
            }

            HerbageEfficiency = AParams.EfficC[13]
                                 * (1.0 + AParams.EfficC[14] * Inputs.LegumePropn)
                                 * (1.0 + AParams.EfficC[15] * (TheEnv.Latitude / 40.0) * Math.Sin(GrazEnv.DAY2RAD * StdDate.DOY(TheEnv.TheDay, true)))
                                 * AnimalState.ME_2_DM.Herbage;
            SuppEfficiency = AParams.EfficC[16] * AnimalState.ME_2_DM.Supp;
            AnimalState.Efficiency.Gain = AnimalState.DietPropn.Herbage * HerbageEfficiency
                                 + AnimalState.DietPropn.Supp * SuppEfficiency
                                 + AnimalState.DietPropn.Milk * AParams.EfficC[12];

        }

        /// <summary>
        /// Basal metabolism routine.  Outputs (EnergyUse.Metab,EnergyUse.Maint,      
        /// ProteinUse.Maint) are stored in AnimalState.                              
        /// </summary>
        private void Compute_Maintenance()
        {
            double MetabScale;
            double GrazeMoved_KM;       // Distance walked during grazing (km)      
            double EatingEnergy;        // Energy requirement for grazing           
            double MovingEnergy;        // Energy requirement for movement          
            double EndoUrineN;

            if (LactStatus == GrazType.LactType.Suckling)
                MetabScale = 1.0 + AParams.MaintC[5] * AnimalState.DietPropn.Milk;
            else if ((ReproStatus == GrazType.ReproType.Male) && (MeanAge >= AParams.Puberty[1]))  //Puberty[true]
                MetabScale = 1.0 + AParams.MaintC[15];
            else
                MetabScale = 1.0;
            AnimalState.EnergyUse.Metab = MetabScale * AParams.MaintC[2] * Math.Pow(BaseWeight, 0.75)     // Basal metabolism                         
                               * Math.Max(Math.Exp(-AParams.MaintC[3] * MeanAge), AParams.MaintC[4]);

            EatingEnergy = AParams.MaintC[6] * BaseWeight * AnimalState.DM_Intake.Herbage           // Work of eating fibrous diets             
                                         * StdMath.DIM(AParams.MaintC[7], AnimalState.Digestibility.Herbage);

            if (Inputs.TotalGreen > 100.0)                                     // Energy requirement for movement          
                GrazeMoved_KM = 1.0 / (AParams.MaintC[8] * Inputs.TotalGreen + AParams.MaintC[9]);
            else if (Inputs.TotalDead > 100.0)
                GrazeMoved_KM = 1.0 / (AParams.MaintC[8] * Inputs.TotalDead + AParams.MaintC[9]);
            else
                GrazeMoved_KM = 0.0;
            if (AnimalsPerHa > AParams.MaintC[17])
                GrazeMoved_KM = GrazeMoved_KM * (AParams.MaintC[17] / AnimalsPerHa);

            MovingEnergy = AParams.MaintC[16] * LiveWeight * Steepness * (GrazeMoved_KM + DistanceWalked);

            AnimalState.EnergyUse.Maint = (AnimalState.EnergyUse.Metab + EatingEnergy + MovingEnergy) / AnimalState.Efficiency.Maint
                               + AParams.MaintC[1] * AnimalState.ME_Intake.Total;
            FeedingLevel = AnimalState.ME_Intake.Total / AnimalState.EnergyUse.Maint - 1.0;

            //...........................................................................  MAINTENANCE PROTEIN REQUIREMENT          

            AnimalState.EndoFaeces.Nu[(int)GrazType.TOMElement.N] = (AParams.MaintC[10] * AnimalState.DM_Intake.Solid + AParams.MaintC[11] * AnimalState.ME_Intake.Milk) / GrazType.N2Protein;

            if (Animal == GrazType.AnimalType.Cattle)
            {
                EndoUrineN = (AParams.MaintC[12] * Math.Log(BaseWeight) - AParams.MaintC[13]) / GrazType.N2Protein;
                AnimalState.DermalNLoss = AParams.MaintC[14] * Math.Pow(BaseWeight, 0.75) / GrazType.N2Protein;
            }
            else // sheep 
            {
                EndoUrineN = (AParams.MaintC[12] * BaseWeight + AParams.MaintC[13]) / GrazType.N2Protein;
                AnimalState.DermalNLoss = 0.0;
            }
            AnimalState.ProteinUse.Maint = GrazType.N2Protein * (AnimalState.EndoFaeces.Nu[(int)GrazType.TOMElement.N] + EndoUrineN + AnimalState.DermalNLoss);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="IsRoughage"></param>
        /// <param name="CP"></param>
        /// <param name="DG"></param>
        /// <param name="ADIP_2_CP"></param>
        /// <returns></returns>
        private double DUDPFunc(bool IsRoughage, double CP, double DG, double ADIP_2_CP)
        {
            double Result;
            if (IsRoughage)
                Result = Math.Max(AParams.ProtC[1], Math.Min(AParams.ProtC[3] * CP - AParams.ProtC[4], AParams.ProtC[2]));
            else if (DG >= 1.0)
                Result = 0.0;
            else
                Result = AParams.ProtC[9] * (1.0 - ADIP_2_CP / (1.0 - DG));
            return Result;
        }

        /// <summary>
        /// Compute microbial crude protein and DPLS
        /// </summary>
        private void Compute_DPLS()
        {
            TDietRecord UDPIntakes = new TDietRecord();
            TDietRecord DUDP = new TDietRecord();
            double dgCorrect;
            int Idx;


            ComputeRDP(TheEnv.Latitude, TheEnv.TheDay, 1.0, FeedingLevel,
                        ref AnimalState.CorrDgProt, ref AnimalState.RDP_Intake, ref AnimalState.RDP_Reqd, ref UDPIntakes);
            AnimalState.UDP_Intake = UDPIntakes.Solid + UDPIntakes.Milk;
            dgCorrect = StdMath.XDiv(AnimalState.CorrDgProt.Supp, AnimalState.SuppIntake.Degradability);

            AnimalState.MicrobialCP = AParams.ProtC[6] * AnimalState.RDP_Reqd;                                      // Microbial crude protein synthesis        

            DUDP.Milk = AParams.ProtC[5];
            DUDP.Herbage = DUDPFunc(true, AnimalState.ProteinConc.Herbage, AnimalState.CorrDgProt.Herbage, 0.0);
            DUDP.Supp = 0.0;
            for (Idx = 0; Idx <= TheRation.Count - 1; Idx++)
                DUDP.Supp = DUDP.Supp + StdMath.XDiv(NetSupp_DMI[Idx], AnimalState.DM_Intake.Supp)                  // Fraction of net supplement intake        
                                          * DUDPFunc(TheRation[Idx].IsRoughage,                                     // DUDP of this part of the ration          
                                                      TheRation[Idx].CrudeProt,
                                                      TheRation[Idx].DgProt * dgCorrect,
                                                      TheRation[Idx].ADIP_2_CP);

            AnimalState.DPLS_MCP = AParams.ProtC[7] * AnimalState.MicrobialCP;                                      // DPLS from microbial crude protein        
            AnimalState.DPLS_Milk = DUDP.Milk * UDPIntakes.Milk;                                                    // Store DPLS from milk separately          
            AnimalState.DPLS = DUDP.Herbage * UDPIntakes.Herbage
                            + DUDP.Supp * UDPIntakes.Supp
                            + AnimalState.DPLS_Milk
                            + AnimalState.DPLS_MCP;
            if (UDPIntakes.Solid > 0.0)
                AnimalState.UDP_Dig = (DUDP.Herbage * UDPIntakes.Herbage + DUDP.Supp * UDPIntakes.Supp) / UDPIntakes.Solid;
            else
                AnimalState.UDP_Dig = DUDP.Herbage;

            AnimalState.OrgFaeces.DM = AnimalState.DM_Intake.Solid * (1.0 - AnimalState.Digestibility.Solid);       // Faecal DM & N:                           
            AnimalState.OrgFaeces.Nu[(int)GrazType.TOMElement.N] = ((1.0 - DUDP.Herbage) * UDPIntakes.Herbage       //   Undigested UDP                         
                               + (1.0 - DUDP.Supp) * UDPIntakes.Supp
                               + (1.0 - DUDP.Milk) * UDPIntakes.Milk
                               + AParams.ProtC[8] * AnimalState.MicrobialCP)                                        //   Undigested MCP                         
                               / GrazType.N2Protein + AnimalState.EndoFaeces.Nu[(int)GrazType.TOMElement.N];        //   Endogenous component                   
            AnimalState.InOrgFaeces.Nu[(int)GrazType.TOMElement.N] = 0.0;
        }

        private double fDeltaGompertz(double T, double A, double B, double C)
        {
            return B * C / A * Math.Exp(C * (1.0 - T / A) + B * (1.0 - Math.Exp(C * (1.0 - T / A))));
        }

        /// <summary>
        /// Requirements for pregnancy:                                               
        ///   'Normal' weight of foetus is calculated from its age, maturity of       
        ///   the mother and her no. of young and is adjusted for mother's            
        ///   condition. The "FoetalWt" field of TheAnimals^ is updated here, as      
        ///   are the "EnergyUse.Preg" and "ProteinUse.Preg" fields of TimeStepState   
        /// </summary>
        private void Compute_Pregnancy()
        {
            double fBirthWt;                                                  // Reference birth weight (kg)              
            double fBirthConceptus;                                                  // Reference value of conceptus weight at birth  
            double fFoetalNWt;                                                  // Normal weight of foetus (kg)             
            double fFoetalNGrowth;                                                  // Normal growth rate of foetus (kg/day)    
            double fConditionFactor;                                                  // Effect of maternal condition on foetal growth 
            double fFoetalCondition;                                                  // Foetal body condition                    
            double fPrevConceptusWt;

            fPrevConceptusWt = ConceptusWt();
            fBirthWt = fBirthWtForSize();
            fBirthConceptus = NoFoetuses * AParams.PregC[5] * fBirthWt;

            fFoetalNWt = FoetalNormWt();
            fFoetalNGrowth = fBirthWt * fDeltaGompertz(FoetalAge, AParams.PregC[1], AParams.PregC[2], AParams.PregC[3]);
            fConditionFactor = (Condition - 1.0) * fFoetalNWt / AParams.StdBirthWt(NoFoetuses);
            if (Condition >= 1.0)
                FoetalWt = FoetalWt + fFoetalNGrowth * (1.0 + fConditionFactor);
            else
                FoetalWt = FoetalWt + fFoetalNGrowth * (1.0 + AParams.PregScale[NoFoetuses] * fConditionFactor);
            fFoetalCondition = FoetalWeight / fFoetalNWt;

            // ConceptusWt is a function of foetal age. Advance the age temporarily for this calucation.
            FoetalAge++;
            AnimalState.ConceptusGrowth = ConceptusWt() - fPrevConceptusWt;
            FoetalAge--;

            AnimalState.EnergyUse.Preg = AParams.PregC[8] * fBirthConceptus * fFoetalCondition
                                * fDeltaGompertz(FoetalAge, AParams.PregC[1], AParams.PregC[9], AParams.PregC[10])
                                / AnimalState.Efficiency.Preg;
            AnimalState.ProteinUse.Preg = AParams.PregC[11] * fBirthConceptus * fFoetalCondition
                                * fDeltaGompertz(FoetalAge, AParams.PregC[1], AParams.PregC[12], AParams.PregC[13]);

        }

        /// <summary>
        /// Requirements for lactation:                                             
        ///   The potential production of milk on the particular day of lactation,  
        ///   expressed as the ME value of the milk for the young, is predicted     
        ///   from a Wood-type function, scaled for the absolute and relative size  
        ///   of the mother, her condition at parturition and the no. of young.     
        ///   If ME intake is inadequate for potential production, yield is reduced 
        ///   by a proportion of the energy deficit.                                
        /// </summary>
        private void Compute_Lactation()
        {
            double PotMilkMJ;                                                              // Potential production of milk (MP')       
            double MaxMilkMJ;                                                              // Milk prodn after energy deficit (MP'')   
            double EnergySurplus;
            double AvailMJ;
            double AvailRatio;
            double AvailDays;
            double CondFactor;                                                             // Function of condition affecting milk     
            //   vs body reserves partition (CFlact)    
            double MilkLimit;                                                              // Max. milk consumption by young (kg/hd)   
            double DayRatio;                                                 // Today's value of Milk_MJProd:PotMilkMJ   

            CondFactor = 1.0 - AParams.IntakeC[15] + AParams.IntakeC[15] * ConditionAtBirthing;
            if (!AParams.bUseDairyCurve)                                              // Potential milk production in MJ          
                PotMilkMJ = AParams.PeakLactC[NoSuckling()]
                             * Math.Pow(StdRefWt, 0.75) * Size
                             * CondFactor * LactAdjust
                             * WOOD(DaysLactating + AParams.LactC[1], AParams.LactC[2], AParams.LactC[3]);
            else
                PotMilkMJ = AParams.LactC[5] * AParams.LactC[6] * AParams.PeakMilk
                             * CondFactor * LactAdjust
                             * WOOD(DaysLactating + AParams.LactC[1], AParams.LactC[2], AParams.LactC[4]);

            EnergySurplus = AnimalState.ME_Intake.Total - AnimalState.EnergyUse.Maint - AnimalState.EnergyUse.Preg;
            AvailMJ = AParams.LactC[5] * AnimalState.Efficiency.Lact * EnergySurplus;
            AvailRatio = AvailMJ / PotMilkMJ;                                   // Effects of available energy, stage of    
            AvailDays = Math.Max(DaysLactating, AvailRatio / (2.0 * AParams.LactC[22]));    //   lactation and body condition on        
            MaxMilkMJ = PotMilkMJ * AParams.LactC[7]                                   //   milk production                        
                                         / (1.0 + Math.Exp(AParams.LactC[19] - AParams.LactC[20] * AvailRatio
                                                       - AParams.LactC[21] * AvailDays * (AvailRatio - AParams.LactC[22] * AvailDays)
                                                       + AParams.LactC[23] * Condition * (AvailRatio - AParams.LactC[24] * Condition)));
            if (NoSuckling() > 0)
            {
                MilkLimit = AParams.LactC[6]
                             * NoSuckling()
                             * Math.Pow(Young.BaseWeight, 0.75)
                             * (AParams.LactC[12] + AParams.LactC[13] * Math.Exp(-AParams.LactC[14] * DaysLactating));
                Milk_MJProdn = Math.Min(MaxMilkMJ, MilkLimit);                       // Milk_MJ_Prodn becomes less than MaxMilkMJ
                PropnOfMaxMilk = Milk_MJProdn / MilkLimit;                           //   when the young are not able to consume 
            }                                                                     //   the amount of milk the mothers are     
            else                                                                    //   capable of producing                   
            {
                Milk_MJProdn = MaxMilkMJ;
                PropnOfMaxMilk = 1.0;
            }

            AnimalState.EnergyUse.Lact = Milk_MJProdn / (AParams.LactC[5] * AnimalState.Efficiency.Lact);
            AnimalState.ProteinUse.Lact = AParams.LactC[15] * Milk_MJProdn / AParams.LactC[6];

            if (AnimalsDynamicGlb)
                if (DaysLactating < AParams.LactC[16] * AParams.LactC[2])
                {
                    LactAdjust = 1.0;
                    LactRatio = 1.0;
                }
                else
                {
                    DayRatio = StdMath.XDiv(Milk_MJProdn, PotMilkMJ);
                    if (DayRatio < LactRatio)
                    {
                        LactAdjust = LactAdjust - AParams.LactC[17] * (LactRatio - DayRatio);
                        LactRatio = AParams.LactC[18] * DayRatio + (1.0 - AParams.LactC[18]) * LactRatio;
                    }
                }
        }

        /// <summary>
        /// Wool production is calculated from the intake of ME, except that used   
        /// for pregnancy and lactation, and from the intake of undegraded dietary  
        /// protein. N.B. that the stored fleece weights are on a greasy basis      
        /// </summary>
        /// <param name="DPLS_Adjust"></param>
        private void Compute_Wool(double DPLS_Adjust)
        {
            double AgeFactor;
            double DayLenFactor;
            double ME_Avail_Wool;
            double DPLS_To_CFW;                   // kg CFW grown per kg available DPLS       
            double ME_To_CFW;                     // kg CFW grown per kg available ME         
            double DayCFWGain;

            AgeFactor = AParams.WoolC[5] + (1.0 - AParams.WoolC[5]) * (1.0 - Math.Exp(-AParams.WoolC[12] * MeanAge));
            DayLenFactor = 1.0 + AParams.WoolC[6] * (TheEnv.DayLength - 12);
            DPLS_To_CFW = AParams.WoolC[7] * AParams.FleeceRatio * AgeFactor * DayLenFactor;
            ME_To_CFW = AParams.WoolC[8] * AParams.FleeceRatio * AgeFactor * DayLenFactor;
            AnimalState.DPLS_Avail_Wool = StdMath.DIM(AnimalState.DPLS + DPLS_Adjust,
                                    AParams.WoolC[9] * (AnimalState.ProteinUse.Lact + AnimalState.ProteinUse.Preg));
            ME_Avail_Wool = StdMath.DIM(AnimalState.ME_Intake.Total, AnimalState.EnergyUse.Lact + AnimalState.EnergyUse.Preg);
            DayCFWGain = Math.Min(DPLS_To_CFW * AnimalState.DPLS_Avail_Wool, ME_To_CFW * ME_Avail_Wool);
#pragma warning disable 162 //unreachable code
            if (AnimalsDynamicGlb)
                AnimalState.ProteinUse.Wool = (1 - AParams.WoolC[4]) * (AParams.WoolC[3] * DeltaWoolWt) +            // Smoothed wool growth                     
                                   AParams.WoolC[4] * DayCFWGain;
            else
                AnimalState.ProteinUse.Wool = DayCFWGain;
#pragma warning restore 162
            AnimalState.EnergyUse.Wool = AParams.WoolC[1] * StdMath.DIM(AnimalState.ProteinUse.Wool, AParams.WoolC[2] * Size) /      // Energy use for fleece                    
                               AParams.WoolC[3];
        }

        private void Apply_WoolGrowth()
        {
            double AgeFactor;
            double PotCleanGain;
            double fDiamPower;
            double Gain_Length;

            DeltaWoolWt = AnimalState.ProteinUse.Wool / AParams.WoolC[3];                          // Convert clean to greasy fleece           
            WoolWt = WoolWt + DeltaWoolWt;
            AnimalState.TotalWoolEnergy = AParams.WoolC[1] * DeltaWoolWt;                              // Reporting only                           

            // Changed to always TRUE for use with AgLab API, since we want to
            // be able to report the change in coat depth
            if (true) // AnimalsDynamicGlb  then                                               // This section deals with fibre diameter   
            {
                AgeFactor = AParams.WoolC[5] + (1.0 - AParams.WoolC[5]) * (1.0 - Math.Exp(-AParams.WoolC[12] * MeanAge));
                PotCleanGain = (AParams.WoolC[3] * AParams.FleeceRatio * StdRefWt) * AgeFactor / 365;
                if (AnimalState.EnergyUse.Gain >= 0.0)
                    fDiamPower = AParams.WoolC[13];
                else
                    fDiamPower = AParams.WoolC[14];
                DeltaWoolMicron = AParams.MaxFleeceDiam * Math.Pow(AnimalState.ProteinUse.Wool / PotCleanGain, fDiamPower);
                if (BaseWeight <= 0)
                    throw new Exception("Base weight is zero or less for " + NoAnimals.ToString() + " " + sBreed + " animals aged " + AgeDays.ToString() + " days");
                if (DeltaWoolMicron > 0.0)
                    Gain_Length = 100.0 * 4.0 / Math.PI * AnimalState.ProteinUse.Wool /                   // Computation of fibre diameter assumes    
                                           (AParams.WoolC[10] * AParams.WoolC[11] *                    //   that the day's growth is cylindrical   
                                             AParams.ChillC[1] * Math.Pow(BaseWeight, 2.0 / 3.0) *          //   in shape                               
                                             StdMath.Sqr(DeltaWoolMicron * 1E-6));
                else
                    Gain_Length = 0.0;
                WoolMicron = StdMath.XDiv(FCoatDepth * WoolMicron +                      // Running average fibre diameter           
                                           Gain_Length * DeltaWoolMicron,
                                         FCoatDepth + Gain_Length);
                FCoatDepth = FCoatDepth + Gain_Length;

            }
        }

        /// <summary>
        /// Chilling routine.                                                       
        /// Energy use in maintaining body temperature is computed in 2-hour blocks.
        /// Although the "day" in the animal model runs from 9 am, we first compute 
        /// the value of the insulation and the lower critical temperature in the   
        /// middle of the night (i.e. at the time of minimum temperature).  Even    
        /// though wind increases during the day, the minimum value of the          
        /// Insulation variable will be no less than half the value of Insulation   
        /// at this time for any reasonable value of wind speed; we can therefore   
        /// put a bound on LCT.                                                     
        /// </summary>
        private void Compute_Chilling()
        {
            const double Sin60 = 0.8660254;
            double[] HourSines = { 0, 0.5, Sin60, 1.0, Sin60, 0.5, 0.0, -0.5, -Sin60, -1.0, -Sin60, -0.5, 0.0 }; //[1..12]
            double SurfaceArea;                                                            // Surface area of animal, sq m             
            double BodyRadius;                                                             // Radius of body, cm                       
            double Factor1, Factor2;                                                       // Function of body radius and coat depth   
            double Factor3, WetFactor;
            double HeatPerArea;
            double LCT_Base;
            double PropnClearSky;                                                          // Proportion of night with clear sky       
            double TissueInsulation;
            double Insulation;
            double LCT;                                                                    // Lower critical temp. for a 2-hour period 
            double EnergyRate;

            double AveTemp, TempRange;
            double AveWind, WindRange;
            double Temp2Hr, Wind2Hr;
            int Time;

            AnimalState.Therm0HeatProdn = AnimalState.ME_Intake.Total                      // Thermoneutral heat production            
                           - AnimalState.Efficiency.Preg * AnimalState.EnergyUse.Preg
                           - AnimalState.Efficiency.Lact * AnimalState.EnergyUse.Lact
                           - AnimalState.Efficiency.Gain * (AnimalState.ME_Intake.Total - AnimalState.EnergyUse.Maint - AnimalState.EnergyUse.Preg - AnimalState.EnergyUse.Lact)
                         + AParams.ChillC[16] * ConceptusWt();
            SurfaceArea = AParams.ChillC[1] * Math.Pow(BaseWeight, 2.0 / 3.0);
            BodyRadius = AParams.ChillC[2] * Math.Pow(NormalWt, 1.0 / 3.0);


            // Means and amplitudes for temperature     
            AveTemp = 0.5 * (TheEnv.MaxTemp + TheEnv.MinTemp);                              //   and windrun                            
            TempRange = (TheEnv.MaxTemp - TheEnv.MinTemp) / 2.0;
            AveWind = 0.4 * TheEnv.WindSpeed;                                               // 0.4 corrects wind to animal height       
            WindRange = 0.35 * AveWind;
            PropnClearSky = 0.7 * Math.Exp(-0.25 * TheEnv.Precipitation);                   // Equation J.4                             


            TissueInsulation = AParams.ChillC[3] * Math.Min(1.0, 0.4 + 0.02 * MeanAge) *    // Reduce tissue insulation for animals under 1 month old
                                            (AParams.ChillC[4] + (1.0 - AParams.ChillC[4]) * Condition);  // Tissue insulation calculated as a fn     
            //   of species and body condition          
            Factor1 = BodyRadius / (BodyRadius + CoatDepth);                                // These factors are used in equation J.8   
            Factor2 = BodyRadius * Math.Log(1.0 / Factor1);
            WetFactor = AParams.ChillC[5] + (1.0 - AParams.ChillC[5]) *
                                            Math.Exp(-AParams.ChillC[6] * TheEnv.Precipitation / CoatDepth);
            HeatPerArea = AnimalState.Therm0HeatProdn / SurfaceArea;                        // These factors are used in equation J.10  
            LCT_Base = AParams.ChillC[11] - HeatPerArea * TissueInsulation;
            Factor3 = HeatPerArea / (HeatPerArea - AParams.ChillC[12]);

            AnimalState.EnergyUse.Cold = 0.0;
            AnimalState.LowerCritTemp = 0.0;
            for (Time = 1; Time <= 12; Time++)
            {
                Temp2Hr = AveTemp + TempRange * HourSines[Time];
                Wind2Hr = AveWind + WindRange * HourSines[Time];

                Insulation = WetFactor *                                                    // External insulation due to hair cover or 
                              (Factor1 / (AParams.ChillC[7] + AParams.ChillC[8] * Math.Sqrt(Wind2Hr)) + //   fleece is calculated from Blaxter      
                               Factor2 * (AParams.ChillC[9] - AParams.ChillC[10] * Math.Sqrt(Wind2Hr))); //   (1977)                                 

                LCT = LCT_Base + (AParams.ChillC[12] - HeatPerArea) * Insulation;
                if ((Time >= 7) && (Time <= 11) && (Temp2Hr > 10.0))                        // Night-time, i.e. 7 pm to 5 am            
                    LCT = LCT + PropnClearSky * AParams.ChillC[13] * Math.Exp(-AParams.ChillC[14] * StdMath.Sqr(StdMath.DIM(Temp2Hr, AParams.ChillC[15])));

                EnergyRate = SurfaceArea * StdMath.DIM(LCT, Temp2Hr)
                                                / (Factor3 * TissueInsulation + Insulation);
                AnimalState.EnergyUse.Cold = AnimalState.EnergyUse.Cold + 1.0 / 12.0 * EnergyRate;
                AnimalState.LowerCritTemp = AnimalState.LowerCritTemp + 1.0 / 12.0 * LCT;

            } //_ FOR Time _

            AnimalState.EnergyUse.Maint = AnimalState.EnergyUse.Maint + AnimalState.EnergyUse.Cold;
        }

        /// <summary>
        /// Computes the efficiency of energy use for weight change.  This routine  
        /// is called twice if chilling energy use is computed                      
        /// </summary>
        private void Adjust_K_Gain()
        {
            if (AnimalState.ME_Intake.Total < AnimalState.EnergyUse.Maint + AnimalState.EnergyUse.Preg + AnimalState.EnergyUse.Lact)
            {                                                                                                           // Efficiency of energy use for weight      
                if (LactStatus == GrazType.LactType.Lactating)                                                          //   change                                 
                    AnimalState.Efficiency.Gain = AnimalState.Efficiency.Lact / AParams.EfficC[10];                     // Lactating animals in -ve energy balance 
                else
                    AnimalState.Efficiency.Gain = AnimalState.Efficiency.Maint / AParams.EfficC[11];                    // Dry animals in -ve energy balance        
            }
            else if (LactStatus == GrazType.LactType.Lactating)
                AnimalState.Efficiency.Gain = AParams.EfficC[9] * AnimalState.Efficiency.Lact;                          // Lactating animals in +ve energy balance  
        }

        /// <summary>
        /// The remaining surplus of net energy is converted to weight gain in a      
        /// logistic function dependent on the relative size of the animal.           
        /// </summary>
        private void Compute_Gain()
        {
            double Eff_DPLS;                                                               // Efficiency of DPLS use                   
            double DPLS_Used;
            double[] GainSigs = new double[2];
            double fGainSize;
            double SizeFactor1;
            double SizeFactor2;
            double NetProtein;
            double PrevWoolEnergy;
            double MilkScalar;
            double EmptyBodyGain;


            AnimalState.EnergyUse.Gain = AnimalState.Efficiency.Gain * (AnimalState.ME_Intake.Total - (AnimalState.EnergyUse.Maint + AnimalState.EnergyUse.Preg + AnimalState.EnergyUse.Lact))
                             - AnimalState.EnergyUse.Wool;

            Eff_DPLS = AParams.GainC[2] / (1.0 + (AParams.GainC[2] / AParams.GainC[3] - 1.0) *           // Efficiency of use of protein from milk   
                                                StdMath.XDiv(AnimalState.DPLS_Milk, AnimalState.DPLS));            //   is higher than from solid sources      
            DPLS_Used = (AnimalState.ProteinUse.Maint + AnimalState.ProteinUse.Preg + AnimalState.ProteinUse.Lact) / Eff_DPLS;
            if (Animal == GrazType.AnimalType.Sheep)                                               // Efficiency of use of protein for wool is 
                DPLS_Used = DPLS_Used + AnimalState.ProteinUse.Wool / AParams.GainC[1];                         //   0.6 regardless of source               
            AnimalState.ProteinUse.Gain = Eff_DPLS * (AnimalState.DPLS - DPLS_Used);


            fGainSize = NormalWeightFunc(MeanAge, MaxPrevWt, 0.0) / StdRefWt;
            GainSigs[0] = AParams.GainC[5];
            GainSigs[1] = AParams.GainC[4];
            SizeFactor1 = StdMath.SIG(fGainSize, GainSigs);
            SizeFactor2 = StdMath.RAMP(fGainSize, AParams.GainC[6], AParams.GainC[7]);

            AnimalState.GainEContent = AParams.GainC[8]                                             // Generalization of the SCA equations      
                               - SizeFactor1 * (AParams.GainC[9] - AParams.GainC[10] * (FeedingLevel - 1.0))
                               + SizeFactor2 * AParams.GainC[11] * (Condition - 1.0);
            AnimalState.GainPContent = AParams.GainC[12]
                               + SizeFactor1 * (AParams.GainC[13] - AParams.GainC[14] * (FeedingLevel - 1.0))
                               - SizeFactor2 * AParams.GainC[15] * (Condition - 1.0);

            AnimalState.UDP_Reqd = StdMath.DIM(DPLS_Used +
                                     (AnimalState.EnergyUse.Gain / AnimalState.GainEContent) * AnimalState.GainPContent / Eff_DPLS,
                                    AnimalState.DPLS_MCP)
                               / AnimalState.UDP_Dig;

            NetProtein = AnimalState.ProteinUse.Gain - AnimalState.GainPContent * AnimalState.EnergyUse.Gain / AnimalState.GainEContent;

            if ((NetProtein < 0) && (AnimalState.ProteinUse.Lact > GrazType.VerySmall))               // Deficiency of protein, i.e. protein is   
            {                                                                   //   more limiting than ME                  
                MilkScalar = Math.Max(0.0, 1.0 + AParams.GainC[16] * NetProtein /          // Redirect protein from milk to weight     
                                                                AnimalState.ProteinUse.Lact);    //   change                                 
                AnimalState.EnergyUse.Gain = AnimalState.EnergyUse.Gain + (1.0 - MilkScalar) * Milk_MJProdn;
                AnimalState.ProteinUse.Gain = AnimalState.ProteinUse.Gain + (1.0 - MilkScalar) * AnimalState.ProteinUse.Lact;
                NetProtein = AnimalState.ProteinUse.Gain - AnimalState.GainPContent * AnimalState.EnergyUse.Gain / AnimalState.GainEContent;

                Milk_MJProdn = MilkScalar * Milk_MJProdn;
                AnimalState.EnergyUse.Lact = MilkScalar * AnimalState.EnergyUse.Lact;
                AnimalState.ProteinUse.Lact = MilkScalar * AnimalState.ProteinUse.Lact;
            }
            Milk_ProtProdn = AnimalState.ProteinUse.Lact;
            Milk_Weight = Milk_MJProdn / (AParams.LactC[5] * AParams.LactC[6]);

            if (NetProtein >= 0)
                AnimalState.ProteinUse.Gain = AnimalState.GainPContent * AnimalState.EnergyUse.Gain / AnimalState.GainEContent;
            else
                AnimalState.EnergyUse.Gain = AnimalState.EnergyUse.Gain + AParams.GainC[17] * AnimalState.GainEContent *
                                                    NetProtein / AnimalState.GainPContent;

            if ((AnimalState.ProteinUse.Gain < 0) && (Animal == GrazType.AnimalType.Sheep))                      // If protein is being catabolised, it can  
            {                                                                   //   be utilized to increase wool growth    
                PrevWoolEnergy = AnimalState.EnergyUse.Wool;                                     // Maintain the energy balance by           
                Compute_Wool(Math.Abs(AnimalState.ProteinUse.Gain));                                 //   transferring any extra energy use for  
                AnimalState.EnergyUse.Gain = AnimalState.EnergyUse.Gain - (AnimalState.EnergyUse.Wool - PrevWoolEnergy);  //   wool out of weight change              
            }

            EmptyBodyGain = AnimalState.EnergyUse.Gain / AnimalState.GainEContent;

            DeltaBaseWeight = AParams.GainC[18] * EmptyBodyGain;
            BaseWeight = BaseWeight + DeltaBaseWeight;

            AnimalState.ProteinUse.Total = AnimalState.ProteinUse.Maint + AnimalState.ProteinUse.Gain +
                                 AnimalState.ProteinUse.Preg + AnimalState.ProteinUse.Lact +
                                 AnimalState.ProteinUse.Wool;
            AnimalState.Urine.Nu[(int)GrazType.TOMElement.N] = StdMath.DIM(AnimalState.CP_Intake.Total / GrazType.N2Protein,                    // Urinary loss of N                        
                                      (AnimalState.ProteinUse.Total - AnimalState.ProteinUse.Maint) / GrazType.N2Protein //   This is retention of N                 
                                      + AnimalState.OrgFaeces.Nu[(int)GrazType.TOMElement.N]                             //   This is other excretion                
                                      + AnimalState.InOrgFaeces.Nu[(int)GrazType.TOMElement.N]
                                      + AnimalState.DermalNLoss);
        }

        /// <summary>
        /// Usage of and mass balance for phosphorus                                  
        /// * Only a proportion of the phosphorus intake is absorbed (available).     
        /// * There are endogenous losses of P which will appear in the excreta       
        ///   regardless of intake.                                                   
        /// * P content of the day's conceptus growth varies with stage of pregnancy. 
        /// * P contents of milk and wool are constants.                              
        /// * P usage in liveweight change is computed to try and maintain body P     
        ///   content at PhosC[9].                                                    
        /// * All P is excreted in faeces, but some is organic and the rest is        
        ///   inorganic.  Organic P excretion is a constant proportion of DMI.        
        /// </summary>
        private void Compute_Phosphorus()
        {
            double AvailPhos;
            double ExcretePhos;
            int P = (int)GrazType.TOMElement.P;

            AvailPhos = AParams.PhosC[1] * AnimalState.Phos_Intake.Solid + AParams.PhosC[2] * AnimalState.Phos_Intake.Milk;
            AnimalState.EndoFaeces.Nu[P] = AParams.PhosC[3] * BaseWeight;

            if (((ReproStatus == GrazType.ReproType.EarlyPreg) || (ReproStatus == GrazType.ReproType.LatePreg)) || (LactStatus == GrazType.LactType.Lactating))
                AnimalState.EndoFaeces.Nu[P] = AParams.PhosC[11] * AnimalState.DM_Intake.Total + AParams.PhosC[12] * BaseWeight;
            else
                AnimalState.EndoFaeces.Nu[P] = AParams.PhosC[9] * AnimalState.DM_Intake.Total + AParams.PhosC[10] * BaseWeight;

            AnimalState.Phos_Use.Maint = Math.Min(AvailPhos, AnimalState.EndoFaeces.Nu[P]);
            AnimalState.Phos_Use.Preg = Math.Max(AParams.PhosC[4], AParams.PhosC[5] * FoetalAge - AParams.PhosC[6]) * AnimalState.ConceptusGrowth;
            AnimalState.Phos_Use.Lact = AParams.PhosC[7] * Milk_Weight;
            AnimalState.Phos_Use.Wool = AParams.PhosC[8] * DeltaWoolWt;
            AnimalState.Phos_Use.Gain = DeltaBaseWeight *
                                 (AParams.PhosC[13] + AParams.PhosC[14] * Math.Pow(StdRefWt / BaseWeight, AParams.PhosC[15]));
            AnimalState.Phos_Use.Gain = Math.Min(AvailPhos - (AnimalState.Phos_Use.Maint + AnimalState.Phos_Use.Preg + AnimalState.Phos_Use.Lact + AnimalState.Phos_Use.Wool),
                                    AnimalState.Phos_Use.Gain);
            //WITH AnimalState.Phos_Use DO
            AnimalState.Phos_Use.Total = AnimalState.Phos_Use.Maint + AnimalState.Phos_Use.Preg + AnimalState.Phos_Use.Lact + AnimalState.Phos_Use.Wool + AnimalState.Phos_Use.Gain;
            BasePhos = BasePhos - AnimalState.EndoFaeces.Nu[P] + AnimalState.Phos_Use.Maint + AnimalState.Phos_Use.Gain;
            Milk_PhosProdn = AnimalState.Phos_Use.Lact;

            ExcretePhos = AnimalState.EndoFaeces.Nu[P] + AnimalState.Phos_Intake.Total - AnimalState.Phos_Use.Total;
            AnimalState.OrgFaeces.Nu[P] = 0.0;
            AnimalState.InOrgFaeces.Nu[P] = ExcretePhos - AnimalState.OrgFaeces.Nu[P];
            AnimalState.Urine.Nu[P] = 0.0;
        }

        /// <summary>
        /// Usage of and mass balance for sulphur                                     
        /// </summary>
        private void Compute_Sulfur()
        {
            double ExcreteSulf;
            int S = (int)GrazType.TOMElement.S;

            AnimalState.EndoFaeces.Nu[S] = AParams.SulfC[1] * AnimalState.EndoFaeces.Nu[(int)GrazType.TOMElement.N];

            AnimalState.Sulf_Use.Maint = AnimalState.EndoFaeces.Nu[S];
            AnimalState.Sulf_Use.Preg = AParams.SulfC[1] * AnimalState.ProteinUse.Preg / GrazType.N2Protein;
            AnimalState.Sulf_Use.Lact = AParams.SulfC[2] * AnimalState.ProteinUse.Lact / GrazType.N2Protein;
            AnimalState.Sulf_Use.Wool = AParams.SulfC[3] * AnimalState.ProteinUse.Wool / GrazType.N2Protein;
            //WITH AnimalState.Sulf_Use DO
            AnimalState.Sulf_Use.Gain = Math.Min(AParams.SulfC[1] * AnimalState.ProteinUse.Gain / GrazType.N2Protein,
                                    AnimalState.Sulf_Intake.Total - (AnimalState.Sulf_Use.Maint + AnimalState.Sulf_Use.Preg + AnimalState.Sulf_Use.Lact + AnimalState.Sulf_Use.Wool));
            //WITH AnimalState.Sulf_Use DO
            AnimalState.Sulf_Use.Total = AnimalState.Sulf_Use.Maint + AnimalState.Sulf_Use.Preg + AnimalState.Sulf_Use.Lact + AnimalState.Sulf_Use.Wool + AnimalState.Sulf_Use.Gain;

            ExcreteSulf = AnimalState.EndoFaeces.Nu[S] + AnimalState.Sulf_Intake.Total - AnimalState.Sulf_Use.Total;
            AnimalState.OrgFaeces.Nu[S] = Math.Min(ExcreteSulf, AParams.SulfC[4] * AnimalState.DM_Intake.Total);
            AnimalState.InOrgFaeces.Nu[S] = 0;
            AnimalState.Urine.Nu[S] = ExcreteSulf - AnimalState.OrgFaeces.Nu[S];
            BaseSulf = BaseSulf + AnimalState.Sulf_Use.Gain;
            Milk_SulfProdn = AnimalState.Sulf_Use.Lact;

        }

        /// <summary>
        /// Proton balance                                                            
        /// </summary>
        private void Compute_AshAlk()
        {
            double fIntakeMoles;                                                             // These are all on a per-head basis        
            double fAccumMoles;

            fIntakeMoles = AnimalState.PaddockIntake.AshAlkalinity * AnimalState.PaddockIntake.Biomass
                             + AnimalState.SuppIntake.AshAlkalinity * AnimalState.SuppIntake.Biomass;
            fAccumMoles = AParams.AshAlkC[1] * (WeightChange + AnimalState.ConceptusGrowth);
            if (Animal == GrazType.AnimalType.Sheep)
                fAccumMoles = fAccumMoles + AParams.AshAlkC[2] * GreasyFleeceGrowth;

            AnimalState.OrgFaeces.AshAlk = AParams.AshAlkC[3] * AnimalState.OrgFaeces.DM;
            AnimalState.Urine.AshAlk = fIntakeMoles - fAccumMoles - AnimalState.OrgFaeces.AshAlk;
        }

        /// <summary>
        /// 
        /// </summary>
        public void Nutrition()
        {
            Efficiencies();
            Compute_Maintenance();
            Compute_DPLS();

            if ((ReproStatus == GrazType.ReproType.EarlyPreg) || (ReproStatus == GrazType.ReproType.LatePreg))
                Compute_Pregnancy();

            if (LactStatus == GrazType.LactType.Lactating)
                Compute_Lactation();

            if (Animal == GrazType.AnimalType.Sheep)
                Compute_Wool(0.0);

            Adjust_K_Gain();
            Compute_Chilling();

            Adjust_K_Gain();
            Compute_Gain();

            if (Animal == GrazType.AnimalType.Sheep)
                Apply_WoolGrowth();
            Compute_Phosphorus();                                                     // These must be done after DeltaFleeceWt   
            Compute_Sulfur();                                                         //   is known                               
            Compute_AshAlk();

            TotalWeight = BaseWeight + ConceptusWt();                                // TotalWeight is meant to be the weight    
            if (Animal == GrazType.AnimalType.Sheep)                                                //   "on the scales", including conceptus   
                TotalWeight = TotalWeight + WoolWt;                                  //   and/or fleece.                         
            AnimalState.IntakeLimitLegume = IntakeLimit * (1.0 + AParams.GrazeC[2] * Inputs.LegumePropn);
        }

        /// <summary>
        /// Test whether intake of RDP matches the requirement for RDP.               
        /// </summary>
        /// <returns></returns>
        public double RDP_IntakeFactor()
        {
            TDietRecord TempCorrDg = new TDietRecord();
            TDietRecord TempUDP = new TDietRecord();
            double TempRDPI = 0.0;
            double TempRDPR = 0.0;
            double TempFL;
            double OldResult, TempResult;
            int Idx;
            //    testResult                    : float;
            double Result;

            if ((AnimalState.DM_Intake.Solid < GrazType.VerySmall) || (AnimalState.RDP_Intake >= AnimalState.RDP_Reqd))
                Result = 1.0;
            else
            {
                Result = AnimalState.RDP_Intake / AnimalState.RDP_Reqd;
                if ((AParams.IntakeC[16] > 0.0) && (AParams.IntakeC[16] < 1.0))
                    Result = 1.0 + AParams.IntakeC[16] * (Result - 1.0);
                Idx = 0;
                do
                {
                    OldResult = Result;
                    TempFL = (OldResult * AnimalState.ME_Intake.Total) / AnimalState.EnergyUse.Maint - 1.0;
                    ComputeRDP(TheEnv.Latitude, TheEnv.TheDay, OldResult, TempFL,
                                ref TempCorrDg, ref TempRDPI, ref TempRDPR, ref TempUDP);
                    TempResult = StdMath.XDiv(TempRDPI, TempRDPR);
                    if ((AParams.IntakeC[16] > 0.0) && (AParams.IntakeC[16] < 1.0))
                        TempResult = 1.0 + AParams.IntakeC[16] * (TempResult - 1.0);
                    Result = Math.Max(0.0, Math.Min(1.0 - 0.5 * (1.0 - OldResult), TempResult));
                    Idx++;
                }
                while ((Idx < 5) && (Math.Abs(Result - OldResult) >= 0.001));  //UNTIL (Idx >= 5) or (Abs(Result-OldResult) < 0.001);
            }
            return Result;
        }

        /* FUNCTION  Phos_IntakeFactor : Float; */

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fRDPFactor"></param>
        public void completeGrowth(double fRDPFactor)
        {
            double fLifeWG, fDayWG;

            AnimalState.RDP_IntakeEffect = fRDPFactor;

            if ((NoMales == 0) || (NoFemales == 0))
                BWGain_Solid = 0.0;
            else
            {
                fLifeWG = StdMath.DIM(BaseWeight - WeightChange, BirthWt);
                fDayWG = Math.Max(WeightChange, 0.0);
                BWGain_Solid = StdMath.XDiv(fLifeWG * BWGain_Solid + fDayWG * StdMath.XDiv(AnimalState.ME_Intake.Solid, AnimalState.ME_Intake.Total), fLifeWG + fDayWG);
            }
        }

        /// <summary>
        /// Records state information prior to the grazing and nutrition calculations     
        /// so that it can be restored if there is an RDP insufficiency.                
        /// </summary>
        /// <param name="Info"></param>
        public void storeStateInfo(ref TStateInfo Info)
        {
            Info.fBaseWeight = BasalWeight;
            Info.fWoolWt = WoolWt;
            Info.fWoolMicron = WoolMicron;
            Info.fCoatDepth = FCoatDepth;
            Info.fFoetalWt = FoetalWt;
            Info.fLactAdjust = LactAdjust;
            Info.fLactRatio = LactRatio;
            Info.fBasePhos = BasePhos;
            Info.fBaseSulf = BaseSulf;
        }

        /// <summary>
        /// Restores state information about animal groups if there is an RDP insufficiency.                                                              
        /// </summary>
        /// <param name="Info"></param>
        public void revertStateInfo(TStateInfo Info)
        {
            BasalWeight = Info.fBaseWeight;
            WoolWt = Info.fWoolWt;
            WoolMicron = Info.fWoolMicron;
            FCoatDepth = Info.fCoatDepth;
            FoetalWt = Info.fFoetalWt;
            LactAdjust = Info.fLactAdjust;
            LactRatio = Info.fLactRatio;
            BasePhos = Info.fBasePhos;
            BaseSulf = Info.fBaseSulf;
        }
        /*function  YoungSuppIntakePropn  : Float;
        function  MotherSuppIntakePropn : Float; */
        /// <summary>
        /// Test to see whether urea intake in the supplement has exceeded the limit of 
        /// 3 g per 10 kg liveweight.                                                   
        /// </summary>
        /// <returns></returns>
        public bool ExceededUreaLimit()
        {
            int i;

            bool Result = false;
            if (TheRation.TotalAmount > 0.0)
            {
                for (i = 0; i <= TheRation.Count - 1; i++)
                {
                    // If there's that much CP, it must be urea...
                    if ((TheRation[i].CrudeProt > 1.5) && (TheRation[i].Amount > 3.0e-4 * LiveWeight))
                        Result = true;
                }
            }
            return Result;
        }

        // Outputs to other models .......................................
        /// <summary>
        /// 
        /// </summary>
        /// <param name="GO"></param>
        public void AddGrazingOutputs(ref GrazType.TGrazingOutputs GO)
        {
            int Clss, Sp, Rp;

            for (Clss = 1; Clss <= GrazType.DigClassNo; Clss++)
                GO.Herbage[Clss] = GO.Herbage[Clss] + NoAnimals * AnimalState.IntakePerHead.Herbage[Clss];
            for (Sp = 1; Sp <= GrazType.MaxPlantSpp; Sp++)
            {
                for (Rp = GrazType.UNRIPE; Rp <= GrazType.RIPE; Rp++)
                    GO.Seed[Sp, Rp] = GO.Seed[Sp, Rp] + NoAnimals * AnimalState.IntakePerHead.Seed[Sp, Rp];
            }
        }
        /// <summary>
        /// Organic faeces
        /// </summary>
        public GrazType.DM_Pool OrgFaeces { get { return GetOrgFaeces(); } }
        /// <summary>
        /// Inorganic faeces
        /// </summary>
        public GrazType.DM_Pool InOrgFaeces { get { return GetInOrgFaeces(); } }
        /// <summary>
        /// Urine
        /// </summary>
        public GrazType.DM_Pool Urine { get { return GetUrine(); } }
        /// <summary>
        /// Excretion information
        /// </summary>
        public TExcretionInfo Excretion
        {
            get { return getExcretion(); }
        }

        // Management events .............................................

        /// <summary>
        ///  Commence joining                                                          
        /// </summary>
        /// <param name="MaleParams"></param>
        /// <param name="MatingPeriod"></param>
        public void Join(TAnimalParamSet MaleParams, int MatingPeriod)
        {
            if ((ReproStatus == GrazType.ReproType.Empty) && (MeanAge > AParams.Puberty[0]))
            {
                if (MaleParams.Animal != AParams.Animal)
                    throw new Exception("Attempt to mate female " + GrazType.AnimalText[(int)AParams.Animal].ToLower() + " with male " + GrazType.AnimalText[(int)MaleParams.Animal].ToLower());

                FMatedTo = new TAnimalParamSet(null, MaleParams);
                DaysToMate = MatingPeriod;
                if (DaysToMate > 0)
                    MateCycle = AParams.OvulationPeriod / 2;
                else
                    MateCycle = -1;
            }
        }
        /*procedure Mate(     MaleParams    : TAnimalParamSet;
                            fPregRate     : Single;
                            var NewGroups : TAnimalList ); */
        /// <summary>
        /// 
        /// </summary>
        /// <param name="AL"></param>
        private void CheckAnimList(ref TAnimalList AL)
        {
            if (AL == null)
                AL = new TAnimalList();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="WeanedGroup"></param>
        /// <param name="WeanedOff"></param>
        private void ExportWeaners(ref TAnimalGroup WeanedGroup, ref TAnimalList WeanedOff)
        {
            if (WeanedGroup != null)
            {
                WeanedGroup.LactStatus = GrazType.LactType.Dry;
                WeanedGroup.FNoOffspring = 0;
                WeanedGroup.Mothers = null;
                CheckAnimList(ref WeanedOff);
                WeanedOff.Add(WeanedGroup);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="MotherGroup"></param>
        /// <param name="YoungGroup"></param>
        /// <param name="NYoung"></param>
        /// <param name="NewGroups"></param>
        private void ExportWithYoung(ref TAnimalGroup MotherGroup, ref TAnimalGroup YoungGroup, int NYoung, ref TAnimalList NewGroups)
        {
            MotherGroup.Young = YoungGroup;
            MotherGroup.FNoOffspring = NYoung;
            YoungGroup.Mothers = MotherGroup;
            YoungGroup.FNoOffspring = NYoung;
            CheckAnimList(ref NewGroups);
            NewGroups.Add(MotherGroup);
        }

        /// <summary>
        /// In the case where only one sex of lambs has been weaned, re-constitute
        /// groups of mothers with unweaned lambs or calves.                      
        /// For example, if male lambs have been weaned (bDoFemales=TRUE), then:  
        /// - if pre-weaning lambs/ewe = 1, 100% of the ewe lambs become singles  
        /// - if pre-weaning lambs/ewe = 2, 50% of the ewe lambs become singles   
        ///                                 50% remain as twins                   
        /// - if pre-weaning lambs/ewe = 3, 25% of the ewe lambs become singles   
        ///                                 50% become twins                      
        ///                                 25% remain as triplets                
        /// * We then have to round the numbers of lambs (or calves) that remain  
        ///   twins or triplets down so that they have an integer number of       
        ///   mothers.                                                            
        /// * In order to conserve animals numbers, the number remaining as       
        ///   singles is done by difference                                       
        /// * The re-constituted groups of mothers are sent off to the NewGroups  
        ///   list, leaving Self as the group of mothers whach has had all its    
        ///   offspring weaned                                                    
        /// </summary>
        /// <param name="YoungGroup"></param>
        /// <param name="iTotalYoung"></param>
        /// <param name="GroupPropn"></param>
        /// <param name="NewGroups"></param>
        protected void SplitMothers(ref TAnimalGroup YoungGroup, int iTotalYoung, double GroupPropn, ref TAnimalList NewGroups)
        {
            // becoming : single twin triplet

            //[0..3,1..3] first element [0] in 2nd dimension is a dummy
            double[,] PropnRemainingLambsAs = new double[4, 4]  {  {0, 0,    0,     0      },               // starting out: empty
                                                                   {0, 1,    0,     0      },               //               single
                                                                   {0, 1.0/2.0,  1.0/2.0,   0      },       //               twin
                                                                   {0, 1.0/4.0,  1.0/2.0,   1.0/4.0    }};  //               triplet

            bool bDoFemales;
            int iKeptLambs;
            int[] iLambsByParity = new int[4];
            int[] iEwesByParity = new int[4];
            TAnimalGroup StillMothers;
            TAnimalGroup StillYoung;
            int NY;

            if ((this.NoOffspring > 3) || (this.NoOffspring < 0))
                throw new Exception("Weaning-by-sex logic can only cope with triplets");

            if (YoungGroup != null)
            {
                if ((YoungGroup.NoMales > 0) && (YoungGroup.NoFemales > 0))
                    throw new Exception("Weaning-by-sex logic: only one sex at a time");
                bDoFemales = (YoungGroup.ReproStatus == GrazType.ReproType.Empty);

                // Compute numbers of mothers & offspring that remain feeding/suckling
                // with each parity
                iKeptLambs = YoungGroup.NoAnimals;
                for (NY = 3; NY >= 2; NY--)
                {
                    iLambsByParity[NY] = Convert.ToInt32(Math.Truncate((PropnRemainingLambsAs[this.NoOffspring, NY] * iKeptLambs) + 0.5));
                    iEwesByParity[NY] = (iLambsByParity[NY] / NY);
                    iLambsByParity[NY] = NY * iEwesByParity[NY];
                }
                iLambsByParity[1] = iKeptLambs - iLambsByParity[2] - iLambsByParity[3];
                iEwesByParity[1] = Math.Min(iLambsByParity[1], this.NoFemales - iEwesByParity[2] - iEwesByParity[3]); //allow for previous rounding

                // Split off the mothers & offspring that remain feeding/suckling
                for (NY = 3; NY >= 1; NY--)
                {
                    if (iEwesByParity[NY] > 0)
                    {
                        StillMothers = this.Split(iEwesByParity[NY], false, NODIFF, NODIFF);
                        if (bDoFemales)
                            StillYoung = YoungGroup.SplitSex(0, iLambsByParity[NY], false, NODIFF);
                        else
                            StillYoung = YoungGroup.SplitSex(iLambsByParity[NY], 0, false, NODIFF);
                        ExportWithYoung(ref StillMothers, ref StillYoung, NY, ref NewGroups);
                    }
                }
                if (YoungGroup.NoAnimals != 0)
                    throw new Exception("Weaning-by-sex logic failed");

                YoungGroup = null;
            }
        }

        /// <summary>
        /// Wean male or female lambs/calves
        /// </summary>
        /// <param name="WeanFemales"></param>
        /// <param name="WeanMales"></param>
        /// <param name="NewGroups"></param>
        /// <param name="WeanedOff"></param>
        public void Wean(bool WeanFemales, bool WeanMales, ref TAnimalList NewGroups, ref TAnimalList WeanedOff)
        {
            int TotalYoung;
            int MalePropn;
            double FemaleDiff;
            TDifferenceRecord Diffs;
            TAnimalGroup MaleYoung;
            TAnimalGroup FemaleYoung;


            if (NoAnimals == 0)
            {
                Young = null;
                Lactation = 0;
            }

            else if ((Young != null) && ((WeanMales && (Young.NoMales > 0))
                                       || (WeanFemales && (Young.NoFemales > 0))))
            {
                TotalYoung = Young.NoAnimals;
                MalePropn = Young.NoMales / TotalYoung;

                if (Young.NoMales == 0)                                              // Divide the male from the female lambs or 
                {                                                                   //    calves                              
                    FemaleYoung = Young;
                    MaleYoung = null;
                }
                else if (Young.NoFemales == 0)
                {
                    MaleYoung = Young;
                    FemaleYoung = null;
                }
                else
                {
                    // TODO: this code had a nasty With block. It may need testing
                    FemaleDiff = StdMath.XDiv(Young.FemaleWeight - Young.MaleWeight, Young.LiveWeight);
                    Diffs = new TDifferenceRecord() { StdRefWt = NODIFF.StdRefWt, BaseWeight = NODIFF.BaseWeight, FleeceWt = NODIFF.FleeceWt };            
                    Diffs.BaseWeight = FemaleDiff * Young.BaseWeight;
                    Diffs.FleeceWt = FemaleDiff * Young.WoolWt;
                    Diffs.StdRefWt = Young.StdRefWt * StdMath.XDiv(Young.NoAnimals, AParams.SRWScalars[(int)Young.ReproStatus] * Young.MaleNo + Young.FemaleNo)
                                                 * (1.0 - AParams.SRWScalars[(int)Young.ReproStatus]);

                    MaleYoung = Young;
                    FemaleYoung = MaleYoung.SplitSex(0, Young.NoFemales, false, Diffs);
                }
                if (FemaleYoung != null)
                    FemaleYoung.ReproStatus = GrazType.ReproType.Empty;

                Young = null;                                                    // Detach weaners from their mothers        
                FPrevOffspring = FNoOffspring;
                FNoOffspring = FPrevOffspring;

                if (WeanMales)                                                       // Export the weaned lambs or calves        
                    ExportWeaners(ref MaleYoung, ref WeanedOff);
                if (WeanFemales)
                    ExportWeaners(ref FemaleYoung, ref WeanedOff);

                if (!WeanMales)                                                   // Export ewes or cows which still have     
                    SplitMothers(ref MaleYoung, TotalYoung, MalePropn, ref NewGroups);               //   lambs or calves                        
                if (!WeanFemales)
                    SplitMothers(ref FemaleYoung, TotalYoung, 1.0 - MalePropn, ref NewGroups);

                if (AParams.Animal == GrazType.AnimalType.Sheep)                                        // Sheep don't continue lactation           
                    SetLactation(0);

                FNoOffspring = 0;
            } //_ IF (Young <> NIL) etc _
        }

        /// <summary>
        /// Shear the animals and return the cfw per head
        /// </summary>
        /// <param name="CFW_Head"></param>
        public void Shear(ref double CFW_Head)
        {
            double GreasyFleece;

            GreasyFleece = FleeceCutWeight;
            WoolWt = WoolWt - GreasyFleece;
            TotalWeight = TotalWeight - GreasyFleece;
            Calc_CoatDepth();
            CFW_Head = AParams.WoolC[3] * GreasyFleece;
        }

        /// <summary>
        /// End lactation in cows whose calves have already been weaned               
        /// </summary>
        public void DryOff()
        {
            if ((Young == null) && (LactStatus == GrazType.LactType.Lactating))
                SetLactation(0);
        }
        /// <summary>
        /// Castrate the animals
        /// </summary>
        public void Castrate()
        {
            if (ReproStatus == GrazType.ReproType.Male)
            {
                ReproStatus = GrazType.ReproType.Castrated;
                ComputeSRW();
                Calc_Weights();
            }
        }
        /// <summary>
        /// Implant hormones
        /// </summary>
        /// <param name="bInsert"></param>
        public void ImplantHormone(bool bInsert)
        {
            double fOldEffect;

            fOldEffect = ImplantEffect;
            if (bInsert)
                ImplantEffect = AParams.GrowthC[4];
            else
                ImplantEffect = 1.0;
            if (ImplantEffect != fOldEffect)
                StdRefWt = StdRefWt * ImplantEffect / fOldEffect;
        }

        // Information properties ........................................
        /// <summary>
        /// Get the animal
        /// </summary>
        public GrazType.AnimalType Animal { get { return GetAnimal(); } }
        /// <summary>
        /// The breed name
        /// </summary>
        public string sBreed { get { return GetBreed(); } }
        /// <summary>
        /// Standard reference weight
        /// </summary>
        public double StdReferenceWt { get { return StdRefWt; } }
        /// <summary>
        /// Age class of the animals
        /// </summary>
        public GrazType.AgeType AgeClass { get { return GetAgeClass(); } }
        /// <summary>
        /// Reproductive state
        /// </summary>
        public GrazType.ReproType ReproState { get { return ReproStatus; } }
        /// <summary>
        /// The mother's group
        /// </summary>
        public TAnimalGroup MotherGroup { get { return Mothers; } }
        /// <summary>
        /// Relative size of the animal
        /// </summary>
        public double RelativeSize { get { return Size; } }
        /// <summary>
        /// Body condition
        /// </summary>
        public double BodyCondition { get { return Condition; } }
        /// <summary>
        /// Weight change
        /// </summary>
        public double WeightChange { get { return DeltaBaseWeight; } }

        /// <summary>
        /// Owing to the requirements of the calculation order, the stored value of   
        /// Condition is that at the start of the previous time step. We have to      
        /// compute tomorrow's value of Condition before we can compute the rate of   
        /// change in condition score                                                 
        /// </summary>
        /// <param name="System"></param>
        /// <returns></returns>
        public double ConditionScoreChange(TAnimalParamSet.TCond_System System = TAnimalParamSet.TCond_System.csSYSTEM1_5)
        {
            double fNewCondition;

            fNewCondition = BaseWeight / NormalWeightFunc(MeanAge + 1, Math.Max(BaseWeight, MaxPrevWt), AParams.GrowthC[3]);
            return TAnimalParamSet.Condition2CondScore(fNewCondition, System) - TAnimalParamSet.Condition2CondScore(Condition, System);
        }

        /// <summary>
        /// Clean fleece weight
        /// </summary>
        public double CleanFleeceWeight { get { return GetCFW(); } }
        /// <summary>
        /// Clean fleece growth
        /// </summary>
        public double CleanFleeceGrowth { get { return GetDeltaCFW(); } }
        /// <summary>
        /// Greasy fleece growth
        /// </summary>
        public double GreasyFleeceGrowth { get { return DeltaWoolWt; } }
        /// <summary>
        /// The days fibre diameter
        /// </summary>
        public double DayFibreDiam { get { return DeltaWoolMicron; } }
        /// <summary>
        /// Milk yield
        /// </summary>
        public double MilkYield { get { return Milk_Weight; } }
        /// <summary>
        /// Milk volume
        /// </summary>
        public double MilkVolume { get { return GetMilkVolume(); } }
        /// <summary>
        /// Milk yield
        /// </summary>
        public double MaxMilkYield { get { return GetMaxMilkYield(); } }
        /// <summary>
        /// Milk energy
        /// </summary>
        public double MilkEnergy { get { return Milk_MJProdn; } }
        /// <summary>
        /// Milk protein
        /// </summary>
        public double MilkProtein { get { return Milk_ProtProdn; } }
        /// <summary>
        /// Foetal weight
        /// </summary>
        public double FoetalWeight { get { return FoetalWt; } }
        /// <summary>
        /// Conceptus weight
        /// </summary>
        public double ConceptusWeight { get { return ConceptusWt(); } }
        /// <summary>
        /// Male weight
        /// </summary>
        public double MaleWeight { get { return GetMaleWeight(); } }
        /// <summary>
        /// Female weight
        /// </summary>
        public double FemaleWeight { get { return GetFemaleWeight(); } }
        /// <summary>
        /// DSE
        /// </summary>
        public double DrySheepEquivs { get { return GetDSEs(); } }
        /// <summary>
        /// Potential intake
        /// </summary>
        public double PotIntake { get { return IntakeLimit; } set { IntakeLimit = value; } }
        /// <summary>
        /// Fresh weight supplement intake
        /// </summary>
        public double SupptFW_Intake { get { return Supp_FWI; } }
        /// <summary>
        /// Intake of supplement
        /// </summary>
        public TSupplement IntakeSuppt { get { return FIntakeSupp; } }
        /// <summary>
        /// Methane energy
        /// </summary>
        public double MethaneEnergy { get { return GetMethaneEnergy(); } }
        /// <summary>
        /// Methane weight
        /// </summary>
        public double MethaneWeight { get { return GetMethaneWeight(); } }
        /// <summary>
        /// Methane volume
        /// </summary>
        public double MethaneVolume { get { return GetMethaneVolume(); } }
        /// <summary>
        /// Exceeded urea warning
        /// </summary>
        public bool UreaWarning { get { return ExceededUreaLimit(); } }

        /// <summary>
        ///  Returns the weight change required for these animals to have a given      
        ///  change in body condition                                                  
        /// </summary>
        /// <param name="fDeltaBC">desired rate of change in body condition (/d)</param>
        /// <returns></returns>
        public double WeightChangeAtCondition(double fDeltaBC)
        {
            double fMaxPrevW;
            double[] fBC = new double[2];
            double fMaxN1;
            double fA, fB;

            fMaxPrevW = Math.Max(BaseWeight, MaxPrevWt);
            fBC[0] = BaseWeight / NormalWeightFunc(MeanAge, fMaxPrevW, AParams.GrowthC[3]); // Today's value of body condition          
            fBC[1] = fBC[0] + fDeltaBC;                                           // Desired body condition tomorrow          
            fMaxN1 = MaxNormWtFunc(StdRefWt, BirthWt, MeanAge + 1, AParams);      // Maximum normal weight tomorrow           

            fA = fBC[1] * AParams.GrowthC[3] * fMaxN1;
            fB = fBC[1] * (1.0 - AParams.GrowthC[3]);

            return Math.Min(fBC[1] * fMaxN1, Math.Max(fA / (1.0 - fB), fA + fB * fMaxPrevW)) - BaseWeight;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="iAgeDays"></param>
        /// <param name="NM"></param>
        /// <param name="NF"></param>
        public void GetOlder(int iAgeDays, ref int NM, ref int NF)
        {
            Ages.GetOlder(iAgeDays, ref NM, ref NF);
        }

        /// <summary>
        /// Integration of the age-dependent mortality function                       
        /// </summary>
        /// <param name="iOverDays"></param>
        /// <returns></returns>
        public double fExpectedSurvival(int iOverDays)
        {
            double fDayDeath;
            int iDayCount;
            int iAge;

            iAge = MeanAge;
            double Result = 1.0;

            while (iOverDays > 0)
            {
                if ((LactStatus == GrazType.LactType.Suckling) || (iAge >= Math.Round(AParams.MortAge[2])))
                {
                    fDayDeath = AParams.MortRate[1];
                    iDayCount = iOverDays;
                }
                else if (iAge < Math.Round(AParams.MortAge[1]))
                {
                    fDayDeath = AParams.MortRate[2];
                    iDayCount = Convert.ToInt32(Math.Min(iOverDays, Math.Round(AParams.MortAge[1]) - iAge));
                }
                else
                {
                    fDayDeath = AParams.MortRate[1] + (AParams.MortRate[2] - AParams.MortRate[1])
                                                       * StdMath.RAMP(iAge, AParams.MortAge[2], AParams.MortAge[1]);
                    iDayCount = 1;
                }

                Result = Result * Math.Pow(1.0 - fDayDeath, iDayCount);
                iOverDays -= iDayCount;
                iAge += iDayCount;
            }
            return Result;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="Pool1"></param>
        /// <param name="Pool2"></param>
        /// <returns></returns>
        public GrazType.DM_Pool AddDMPool(GrazType.DM_Pool Pool1, GrazType.DM_Pool Pool2)
        {
            int N = (int)GrazType.TOMElement.N;
            int P = (int)GrazType.TOMElement.P;
            int S = (int)GrazType.TOMElement.S;

            GrazType.DM_Pool Result = new GrazType.DM_Pool();
            Result.DM = Pool1.DM + Pool2.DM;
            Result.Nu[N] = Pool1.Nu[N] + Pool2.Nu[N];
            Result.Nu[S] = Pool1.Nu[S] + Pool2.Nu[S];
            Result.Nu[P] = Pool1.Nu[P] + Pool2.Nu[P];
            Result.AshAlk = Pool1.AshAlk + Pool2.AshAlk;

            return Result;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="Src"></param>
        /// <param name="X"></param>
        /// <returns></returns>
        public GrazType.DM_Pool MultiplyDMPool(GrazType.DM_Pool Src, double X)
        {
            int N = (int)GrazType.TOMElement.N;
            int P = (int)GrazType.TOMElement.P;
            int S = (int)GrazType.TOMElement.S;

            GrazType.DM_Pool Result = new GrazType.DM_Pool();
            Result.DM = Src.DM * X;
            Result.Nu[N] = Src.Nu[N] * X;
            Result.Nu[S] = Src.Nu[S] * X;
            Result.Nu[P] = Src.Nu[P] * X;
            Result.AshAlk = Src.AshAlk * X;

            return Result;
        }

        /// <summary>
        /// Supplement relative intake.
        /// </summary>
        /// <param name="TheAnimals"></param>
        /// <param name="fTimeStepLength"></param>
        /// <param name="fSuppDWPerHead"></param>
        /// <param name="aSupp"></param>
        /// <param name="fSuppRQ"></param>
        /// <param name="bEatenFirst"></param>
        /// <param name="fSuppRI"></param>
        /// <param name="fFracUnsat"></param>
        private void EatSupplement(TAnimalGroup TheAnimals,
                                    double fTimeStepLength,
                                    double fSuppDWPerHead,
                                    TSupplement aSupp,
                                    double fSuppRQ,
                                    bool bEatenFirst,
                                    ref double fSuppRI,
                                    ref double fFracUnsat)
        {
            double fSuppRelFill;

            if (TheAnimals.IntakeLimit < GrazType.VerySmall)
                fSuppRelFill = 0.0;
            else
            {
                if (bEatenFirst)                                                     // Relative fill of supplement           
                    fSuppRelFill = Math.Min(fFracUnsat,
                                          fSuppDWPerHead / (TheAnimals.IntakeLimit * fSuppRQ));
                else
                    fSuppRelFill = Math.Min(fFracUnsat,
                                          fSuppDWPerHead / (TheAnimals.IntakeLimit * fTimeStepLength * fSuppRQ));

                if ((aSupp.ME_2_DM > 0.0) && (!aSupp.IsRoughage))
                {
                    if (TheAnimals.LactStatus == GrazType.LactType.Lactating)
                        fSuppRelFill = Math.Min(fSuppRelFill, TheAnimals.AParams.GrazeC[20] / aSupp.ME_2_DM);
                    else
                        fSuppRelFill = Math.Min(fSuppRelFill, TheAnimals.AParams.GrazeC[11] / aSupp.ME_2_DM);
                }
            }

            fSuppRI = fSuppRQ * fSuppRelFill;
            fFracUnsat = StdMath.DIM(fFracUnsat, fSuppRelFill);
        }

        /// <summary>
        /// "Relative fill" of pasture [F(d)]                                     
        /// </summary>
        /// <param name="TheAnimals"></param>
        /// <param name="FU"></param>
        /// <param name="ClassFeed"></param>
        /// <param name="TotalFeed"></param>
        /// <param name="HR"></param>
        /// <returns></returns>
        private double fRelativeFill(TAnimalGroup TheAnimals, double FU, double ClassFeed, double TotalFeed, double HR)
        {

            double fHeightFactor,
            fSizeFactor,
            fScaledFeed,
            fPropnFactor,
            fRateTerm,
            fTimeTerm;

            double Result;

            // Equation numbers refer to June 2008 revision of Freer, Moore, and Donnelly 
            fHeightFactor = Math.Max(0.0, (1.0 - TheAnimals.AParams.GrazeC[12]) + TheAnimals.AParams.GrazeC[12] * HR);          // Eq. 18 : HF 
            fSizeFactor = 1.0 + StdMath.DIM(TheAnimals.AParams.GrazeC[7], TheAnimals.Size);                                     // Eq. 19 : ZF }
            fScaledFeed = fHeightFactor * fSizeFactor * ClassFeed;                                                              // Part of Eqs. 16, 16 : HF * ZF * B }
            fPropnFactor = 1.0 + TheAnimals.AParams.GrazeC[13] * StdMath.XDiv(ClassFeed, TotalFeed);                            // Part of Eqs. 16, 17 : 1 + Cr13 * Phi }
            fRateTerm = 1.0 - Math.Exp(-fPropnFactor * TheAnimals.AParams.GrazeC[4] * fScaledFeed);                             // Eq. 16 }
            fTimeTerm = 1.0 + TheAnimals.AParams.GrazeC[5] * Math.Exp(-fPropnFactor * Math.Pow(TheAnimals.AParams.GrazeC[6] * fScaledFeed, 2)); // Eq. 17 }
            Result = FU * fRateTerm * fTimeTerm;                                                                                // Eq. 14 }

            return Result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="TheAnimals"></param>
        /// <param name="ClassFeed"></param>
        /// <param name="TotalFeed"></param>
        /// <param name="HR"></param>
        /// <param name="RelQ"></param>
        /// <param name="RI"></param>
        /// <param name="FU"></param>
        private void EatPasture(TAnimalGroup TheAnimals, double ClassFeed,
                                    double TotalFeed,
                                    double HR,
                                    double RelQ,
                                    ref double RI,
                                    ref double FU)
        {
            double fRelFill;

            fRelFill = fRelativeFill(TheAnimals, FU, ClassFeed, TotalFeed, HR);
            RI = RI + fRelFill * RelQ;
            FU = StdMath.DIM(FU, fRelFill);
        }

        /// <summary>
        /// Weighted average of two values                                            
        /// </summary>
        /// <param name="X1"></param>
        /// <param name="Y1"></param>
        /// <param name="X2"></param>
        /// <param name="Y2"></param>
        private void fWeightAverage(ref double X1, double Y1, double X2, double Y2)
        {
            X1 = StdMath.XDiv(X1 * Y1 + X2 * Y2, Y1 + Y2);
        }
        /// <summary>
        /// Calculate relative intake
        /// </summary>
        /// <param name="TheAnimals"></param>
        /// <param name="fTimeStepLength"></param>
        /// <param name="bFeedSuppFirst"></param>
        /// <param name="fWaterLogScalar"></param>
        /// <param name="fHerbageRI"></param>
        /// <param name="fSeedRI"></param>
        /// <param name="fSuppRelIntake"></param>
        public void CalculateRelIntake(TAnimalGroup TheAnimals,
                              double fTimeStepLength,
                              bool bFeedSuppFirst,
                              double fWaterLogScalar,
                              ref double[] fHerbageRI,
                              ref double[,] fSeedRI,
                              ref double fSuppRelIntake)
        {
            const double CLASSWIDTH = 0.1;

            double[] fAvailFeed = new double[GrazType.DigClassNo + 2]; //1..DigClassNo+1    // Grazeable DM in each quality class    
            double[] fHeightRatio = new double[GrazType.DigClassNo + 2];                    // "Height ratio"                        
            double fLegume;                                                                 // Legume fraction                       
            double fLegumeTrop;                                                             // Legume tropicality }
            double fSelectFactor;                                                           // SF, adjusted for legume content       
            double[] fRelQ = new double[GrazType.DigClassNo + 2];                           // Function of herbage class digestib'ty 
            double fSuppRelQ;                                                               // Function of supplement digestibility  
            double fSuppFWPerHead;
            double fSuppDWPerHead;
            double fTotalFeed;

            double fOMD_Supp;
            double fProteinFactor;                                                      // DOM/protein and lactation factors for 
            double fMilkFactor;                                                         //   modifying substitution rate         
            double fSubstSuppRelQ;

            double[] fRelIntake = new double[GrazType.DigClassNo + 2];
            double fSuppEntry;
            double fFillRemaining;                                                      // Proportion of maximum relative fill  that is yet to be satisfied         
            bool bSuppRemains;                                                          // TRUE if the animals have yet to select a supplement that is present        
            double fLegumeAdjust;
            int iSpecies,
            iClass,
            iRipe;


            for (iClass = 1; iClass <= GrazType.DigClassNo; iClass++)                   // Start by aggregating herbage and seed into selection classes              
            {
                fAvailFeed[iClass] = TheAnimals.Inputs.Herbage[iClass].Biomass;
                fHeightRatio[iClass] = TheAnimals.Inputs.Herbage[iClass].HeightRatio;
            }
            fAvailFeed[GrazType.DigClassNo + 1] = 0.0;
            fHeightRatio[GrazType.DigClassNo + 1] = 1.0;

            for (iSpecies = 1; iSpecies <= GrazType.MaxPlantSpp; iSpecies++)
            {
                for (iRipe = GrazType.UNRIPE; iRipe <= GrazType.RIPE; iRipe++)
                {
                    iClass = TheAnimals.Inputs.SeedClass[iSpecies, iRipe];
                    if ((iClass > 0) && (TheAnimals.Inputs.Seeds[iSpecies, iRipe].Biomass > GrazType.VerySmall))
                    {
                        fWeightAverage(ref fHeightRatio[iClass],
                                        fAvailFeed[iClass],
                                        TheAnimals.Inputs.Seeds[iSpecies, iRipe].HeightRatio,
                                        TheAnimals.Inputs.Seeds[iSpecies, iRipe].Biomass);
                        fAvailFeed[iClass] = fAvailFeed[iClass] + TheAnimals.Inputs.Seeds[iSpecies, iRipe].Biomass;
                    }
                }
            }

            fTotalFeed = 0.0;
            for (iClass = 1; iClass <= GrazType.DigClassNo + 1; iClass++)
                fTotalFeed = fTotalFeed + fAvailFeed[iClass];

            fLegume = TheAnimals.Inputs.LegumePropn;
            fLegumeTrop = TheAnimals.Inputs.LegumeTrop;

            TheAnimals.TheRation.AverageSuppt(out TheAnimals.FIntakeSupp);
            fSuppFWPerHead = TheAnimals.TheRation.TotalAmount;
            fSuppDWPerHead = fSuppFWPerHead * TheAnimals.FIntakeSupp.DM_Propn;

            fHerbageRI = new double[GrazType.DigClassNo + 1];                               // Sundry initializations                
            fSeedRI = new double[GrazType.MaxPlantSpp + 1, GrazType.RIPE + 1];
            fSuppRelIntake = 0.0;
            fRelIntake = new double[GrazType.DigClassNo + 2];

            fSelectFactor = (1.0 - fLegume * (1.0 - fLegumeTrop)) * TheAnimals.Inputs.SelectFactor;         // Herbage relative quality calculation  
            for (iClass = 1; iClass <= GrazType.DigClassNo; iClass++)
                fRelQ[iClass] = 1.0 - TheAnimals.AParams.GrazeC[3] * StdMath.DIM(TheAnimals.AParams.GrazeC[1] - fSelectFactor, TheAnimals.Inputs.Herbage[iClass].Digestibility); // Eq. 21 
            fRelQ[GrazType.DigClassNo + 1] = 1; //fixes range check error. Set this to the value that was calc'd when range check error was in place

            bSuppRemains = (fSuppFWPerHead > GrazType.VerySmall);                              // Compute relative quality of           
            if (bSuppRemains)                                                        //    supplement (if present)            
            {
                fSuppRelQ = Math.Min(TheAnimals.AParams.GrazeC[14],
                                   1.0 - TheAnimals.AParams.GrazeC[3] * (TheAnimals.AParams.GrazeC[1] - TheAnimals.FIntakeSupp.DM_Digestibility));

                if (TheAnimals.LactStatus == GrazType.LactType.Lactating)
                    fMilkFactor = TheAnimals.AParams.GrazeC[15] * Math.Exp(-StdMath.Sqr(TheAnimals.DaysLactating / TheAnimals.AParams.GrazeC[8]));
                else
                    fMilkFactor = 0.0;

                fOMD_Supp = Math.Min(1.0, 1.05 * TheAnimals.FIntakeSupp.DM_Digestibility - 0.01);
                if (fOMD_Supp > 0.0)
                    fProteinFactor = TheAnimals.AParams.GrazeC[16] * StdMath.RAMP(TheAnimals.FIntakeSupp.CrudeProt / fOMD_Supp, TheAnimals.AParams.GrazeC[9], TheAnimals.AParams.GrazeC[10]);
                else
                    fProteinFactor = 0.0;

                fSubstSuppRelQ = fSuppRelQ - fMilkFactor - fProteinFactor;
            }
            else
            {
                fSuppRelQ = 0.0;
                fSubstSuppRelQ = 0.0;
            }

            fFillRemaining = TheAnimals.Start_FU;

            if (bSuppRemains && (bFeedSuppFirst || (fTotalFeed <= GrazType.VerySmall)))     // Case where supplement is fed first    
            {
                EatSupplement(TheAnimals, fTimeStepLength, fSuppDWPerHead, TheAnimals.FIntakeSupp, fSuppRelQ, true, ref fSuppRelIntake, ref fFillRemaining);
                TheAnimals.Start_FU = fFillRemaining;
                bSuppRemains = false;
            }

            if (fTotalFeed > GrazType.VerySmall)                                            // Case where there is pasture available 
            {                                                                      //   to the animals                      
                iClass = 1;
                while ((iClass <= GrazType.DigClassNo + 1) && (fFillRemaining >= GrazType.VerySmall))
                {
                    fSuppEntry = Math.Min(1.0, 0.5 + (fSubstSuppRelQ - fRelQ[iClass])
                                                   / (CLASSWIDTH * TheAnimals.AParams.GrazeC[3]));
                    if (bSuppRemains && (fSuppEntry > 0.0))
                    {
                        // This gives a continuous response to changes in supplement DMD
                        EatPasture(TheAnimals, (1.0 - fSuppEntry) * fAvailFeed[iClass], fTotalFeed, fHeightRatio[iClass], fRelQ[iClass], ref fRelIntake[iClass], ref fFillRemaining);
                        EatSupplement(TheAnimals, fTimeStepLength, fSuppDWPerHead, TheAnimals.FIntakeSupp, fSuppRelQ, false, ref fSuppRelIntake, ref fFillRemaining);
                        EatPasture(TheAnimals, fSuppEntry * fAvailFeed[iClass], fTotalFeed, fHeightRatio[iClass], fRelQ[iClass], ref fRelIntake[iClass], ref fFillRemaining);

                        bSuppRemains = false;
                    }
                    else
                        EatPasture(TheAnimals, fAvailFeed[iClass], fTotalFeed,
                                    fHeightRatio[iClass], fRelQ[iClass],
                                    ref fRelIntake[iClass], ref fFillRemaining);
                    iClass++;
                }

                if (bSuppRemains)                                                     // Still supplement left?                
                    EatSupplement(TheAnimals, fTimeStepLength, fSuppDWPerHead, TheAnimals.FIntakeSupp, fSuppRelQ, false, ref fSuppRelIntake, ref fFillRemaining);

                fLegumeAdjust = TheAnimals.AParams.GrazeC[2] * StdMath.Sqr(1.0 - fFillRemaining) * fLegume;        // Adjustment to intake rate for         
                for (iClass = 1; iClass <= GrazType.DigClassNo; iClass++)                                         //   waterlogging and legume content     
                    fRelIntake[iClass] = fRelIntake[iClass] * fWaterLogScalar * (1.0 + fLegumeAdjust);
            }


            for (iClass = 1; iClass <= GrazType.DigClassNo; iClass++)                                             // Distribute relative intakes between herbage and seed                     
            {
                fHerbageRI[iClass] = fRelIntake[iClass] * StdMath.XDiv(TheAnimals.Inputs.Herbage[iClass].Biomass, fAvailFeed[iClass]);
            }

            for (iSpecies = 1; iSpecies <= GrazType.MaxPlantSpp; iSpecies++)
            {
                for (iRipe = GrazType.UNRIPE; iRipe <= GrazType.RIPE; iRipe++)
                {
                    iClass = TheAnimals.Inputs.SeedClass[iSpecies, iRipe];
                    if ((iClass > 0) && (TheAnimals.Inputs.Seeds[iSpecies, iRipe].Biomass > GrazType.VerySmall))
                        fSeedRI[iSpecies, iRipe] = fRelIntake[iClass] * TheAnimals.Inputs.Seeds[iSpecies, iRipe].Biomass / fAvailFeed[iClass];
                }
            }
        }

        /// <summary>
        /// Feasible range of weights for a given age and (relative) body condition   
        /// This weight range is a consequence of the normal weight function          
        /// (TAnimalGroup.NormalWeightFunc)                                           
        /// </summary>
        /// <param name="Repr"></param>
        /// <param name="iAgeDays"></param>
        /// <param name="fBodyCond"></param>
        /// <param name="Params"></param>
        /// <param name="fLowBaseWt"></param>
        /// <param name="fHighBaseWt"></param>
        public void WeightRangeForCond(GrazType.ReproType Repr,
                                      int iAgeDays,
                                      double fBodyCond,
                                      TAnimalParamSet Params,
                                      ref double fLowBaseWt,
                                      ref double fHighBaseWt)
        {
            double fMaxNormWt;

            fMaxNormWt = GrowthCurve(iAgeDays, Repr, Params);
            fHighBaseWt = fBodyCond * fMaxNormWt;
            if (fBodyCond >= 1.0)
                fLowBaseWt = fHighBaseWt;
            else
                fLowBaseWt = fHighBaseWt * Params.GrowthC[3] / (1.0 - fBodyCond * (1.0 - Params.GrowthC[3]));
        }

        /// <summary>
        /// Chill index
        /// </summary>
        /// <param name="T"></param>
        /// <param name="W"></param>
        /// <param name="R"></param>
        /// <returns></returns>
        private double ChillFunc(double T, double W, double R)
        {
            return 481.0 + (11.7 + 3.1 * Math.Sqrt(W)) * (40.0 - T)
                               + 418 * (1.0 - Math.Exp(-0.04 * Math.Min(80, R)));
        }
    }
    #endregion TAnimalGroup

    #region TAnimalList
    /// <summary>
    /// The animal list of animal groups
    /// </summary>
    public class TAnimalList : List<TAnimalGroup>
    {
        /// <summary>
        /// Days of weight gain
        /// </summary>
        public const int GAINDAYCOUNT = 28;       //for the TAnimalList
        /// <summary>
        /// keep count of how many valid entries have been made
        /// </summary>
        private int FValidGainsCount; 
        private double[] FGains = new double[GAINDAYCOUNT - 1];
        private TAnimalGroup GetAt(int Posn)
        {
            return base[Posn];
        }
        /// <summary>
        /// Random number container
        /// </summary>
        public TMyRandom RandFactory;
        /// <summary>
        /// Copy an TAnimalList
        /// </summary>
        /// <returns></returns>
        public TAnimalList Copy()
        {
            int I;

            TAnimalList Result = new TAnimalList();
            for (I = 0; I <= Count - 1; I++)
                Result.Add(At(I).Copy());
            return Result;
        }
        /// <summary>
        /// Remove all animals
        /// </summary>
        public void ClearOut()
        {
            int i;

            for (i = 0; i <= Count - 1; i++)
            {
                SetAt(i, null);
            }
            Clear();
            for (i = 0; i <= GAINDAYCOUNT - 1; i++)
                FGains[i] = StdMath.DMISSING;
            FValidGainsCount = 0; //reset
        }

        /// <summary>
        /// Remove empty TAnimalGroups and unite similar ones
        /// </summary>
        public void Merge()
        {
            TAnimalGroup AG;
            int I, J;

            for (I = 0; I <= Count - 1; I++)                                                  // Remove empty groups                      
            {
                if ((At(I) != null) && (At(I).NoAnimals == 0))
                {
                    SetAt(I, null);
                }
                else
                {
                    for (J = I + 1; J <= Count - 1; J++)
                        if ((At(I) != null) && (At(J) != null) && At(I).Similar(At(J))) // Merge similar groups                     
                        {
                            AG = At(J);
                            this[J] = null;
                            At(I).Merge(ref AG);
                        }
                }
            }
            throw new Exception("Pack() not implemented int TAnimalList.Merge() yet!");
        }
        /// <summary>
        /// Get the animal group at this position
        /// </summary>
        /// <param name="Posn"></param>
        /// <returns></returns>
        public TAnimalGroup At(int Posn)
        {
            return GetAt(Posn);
        }
        /// <summary>
        /// Set the animal group at this position
        /// </summary>
        /// <param name="Posn"></param>
        /// <param name="AG"></param>
        public void SetAt(int Posn, TAnimalGroup AG)
        {
            base[Posn] = AG;
        }
        /// <summary>
        /// Days of weight gain
        /// </summary>
        public int ValidGainDays
        {
            get { return FValidGainsCount; }
        }
        /// <summary>
        /// Add a daily weight gain value in kg. Uses an array as a cheap fifo queue.
        /// Use gain = MISSING when a value is unavailable.
        /// </summary>
        /// <param name="gain"></param>
        public void addWtGain(double gain)
        {
            //shuffle values down and drop the last one off
            for (int i = GAINDAYCOUNT - 1; i >= 1; i--)
                FGains[i] = FGains[i - 1];
            FGains[0] = gain;                   //most recent is at [0]
            if (gain == StdMath.DMISSING)
                FValidGainsCount = 0;           //reset
            else
                FValidGainsCount++;
        }
        /// <summary>
        /// Calc the average weight gain over the last number of days.
        /// </summary>
        /// <param name="days"></param>
        /// <returns></returns>
        public double avGainOver(int days)
        {
            int i;
            double sum;
            int iCount;

            sum = 0;
            if (days > GAINDAYCOUNT)
                days = GAINDAYCOUNT;
            iCount = 0;                         //keep iCount in case there are less days available
            for (i = 0; i <= days - 1; i++)
            {
                if (FGains[i] != StdMath.DMISSING)
                {
                    sum = sum + FGains[i];
                    iCount++;
                }
            }
            return sum / iCount;
        }
    }
    #endregion TAnimalList
}