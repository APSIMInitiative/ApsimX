namespace Models.GrazPlan
{
    using StdUnits;
    using System;
    using System.Collections.Generic;
    using System.Globalization;

    /// <summary>
    /// Record containing the different sources from which an animal acquires energy, protein etc                                
    /// </summary>
    [Serializable]
    public struct DietRecord
    {
        /// <summary>
        /// Herbage value
        /// </summary>
        public double Herbage;
        
        /// <summary>
        /// Supplement value
        /// </summary>
        public double Supp;
        
        /// <summary>
        /// Milk value
        /// </summary>
        public double Milk;
        
        /// <summary>
        /// "Solid" is herbage and supplement taken together
        /// </summary>
        public double Solid;
        
        /// <summary>
        /// Total value
        /// </summary>
        public double Total;
    }

    /// <summary>
    /// Allocation of energy, protein etc for:
    /// </summary>
    [Serializable]
    public struct PhysiolRecord
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
        /// Total value
        /// </summary>
        public double Total;
    }

    /// <summary>
    /// The Animal outputs object
    /// </summary>
    [Serializable]
    public class AnimalOutput
    {
        /// <summary>
        /// Potential intake, after correction for legume content of the diet
        /// </summary>
        public double IntakeLimitLegume;
        
        /// <summary>
        /// Intakes for interface with pasture model
        /// </summary>
        public GrazType.GrazingOutputs IntakePerHead = new GrazType.GrazingOutputs();
        
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
        public DietRecord DM_Intake = new DietRecord();
        
        /// <summary>
        /// Daily crude protein intake (kg)
        /// </summary>
        public DietRecord CP_Intake = new DietRecord();
        
        /// <summary>
        /// Daily phosphorus intake (kg)
        /// </summary>
        public DietRecord Phos_Intake = new DietRecord();
        
        /// <summary>
        /// Daily sulphur intake (kg)
        /// </summary>
        public DietRecord Sulf_Intake = new DietRecord();
        
        /// <summary>
        /// Metabolizable energy intake (MJ)
        /// </summary>
        public DietRecord ME_Intake = new DietRecord();
        
        /// <summary>
        /// Digestibility of diet components (0-1)
        /// </summary>
        public DietRecord Digestibility = new DietRecord();
        
        /// <summary>
        /// Crude protein concentrations (0-1)
        /// </summary>
        public DietRecord ProteinConc = new DietRecord();
        
        /// <summary>
        /// ME:dry matter ratios (MJ/kg)
        /// </summary>
        public DietRecord ME_2_DM = new DietRecord();
        
        /// <summary>
        /// Proportion of each component in the diet 
        /// </summary>
        public DietRecord DietPropn = new DietRecord();
        
        /// <summary>
        /// Degradability of protein in diet (0-1), corrected 
        /// </summary>
        public DietRecord CorrDgProt = new DietRecord();
        
        // ..................................................................
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
        
        //// Allocation of energy and protein to various uses.                          
        /// <summary>
        /// Allocation of energy
        /// </summary>
        public PhysiolRecord EnergyUse = new PhysiolRecord();
        
        /// <summary>
        /// Allocation of protein
        /// </summary>
        public PhysiolRecord ProteinUse = new PhysiolRecord();
        
        /// <summary>
        /// Physiology record
        /// </summary>
        public PhysiolRecord Phos_Use = new PhysiolRecord();
        
        /// <summary>
        /// Sulphur use
        /// </summary>
        public PhysiolRecord Sulf_Use = new PhysiolRecord();
        
        /// <summary>
        /// Efficiencies of ME use (0-1)
        /// </summary>
        public PhysiolRecord Efficiency = new PhysiolRecord();

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
        /// Copy a AnimalOutput object
        /// </summary>
        /// <returns>The clone of an animal output</returns>
        public AnimalOutput Copy()
        {
            return ObjectCopier.Clone(this);
        }
    }

    /// <summary>
    /// An age list item
    /// </summary>
    [Serializable]
    public struct AgeListElement
    {
        /// <summary>
        /// Age in days
        /// </summary>
        public int AgeDays;
        
        /// <summary>
        /// Number of males
        /// </summary>
        public int NumMales;
        
        /// <summary>
        /// Number of females
        /// </summary>
        public int NumFemales;
    }

    #region AgeList
    
    /// <summary>
    /// An agelist
    /// </summary>
    [Serializable]
    public class AgeList
    {
        /// <summary>
        /// Array of agelist objects  
        /// </summary>
        private AgeListElement[] FData;

        /// <summary>
        /// Set the count of age lists
        /// </summary>
        /// <param name="value">The count</param>
        private void SetCount(int value)
        {
            Array.Resize(ref this.FData, value);
        }

        /// <summary>
        /// Gets rid of empty elements of a AgeList                                  
        /// </summary>
        public void Pack()
        {
            int idx, jdx;

            idx = 0;
            while (idx < this.Count)
            {
                if ((this.FData[idx].NumMales > 0) || (this.FData[idx].NumFemales > 0))
                    idx++;
                else
                {
                    for (jdx = idx + 1; jdx <= this.Count - 1; jdx++)
                        this.FData[jdx - 1] = this.FData[jdx];
                    this.SetCount(this.Count - 1);
                }
            }
        }
        
        /// <summary>
        /// Random number factory instance
        /// </summary>
        public MyRandom RandFactory;
        
        /// <summary>
        /// AgeList constructor
        /// </summary>
        /// <param name="randomFactory">An instance of a random number object</param>
        public AgeList(MyRandom randomFactory)
        {
            this.RandFactory = randomFactory;
            this.SetCount(0);
        }
        
        /// <summary>
        /// Create a copy
        /// </summary>
        /// <param name="srcList">The source agelist</param>
        /// <param name="randomFactory">The random number object</param>
        public AgeList(AgeList srcList, MyRandom randomFactory)
        {
            int idx;

            this.RandFactory = randomFactory;
            this.SetCount(srcList.Count);
            for (idx = 0; idx <= srcList.Count - 1; idx++)
                this.FData[idx] = srcList.FData[idx];
        }

        /* constructor Load(  Stream    : TStream;
                           bUseSmall : Boolean;
                           RandomFactory: MyRandom );
        procedure   Store( Stream    : TStream  ); */

        /// <summary>
        /// Gets the count of items in the age list
        /// </summary>
        public int Count
        {
            get { return this.FData.Length; }
        }
        
        /// <summary>
        /// Used instead of Add or Insert to add data to the age list.  The Input     
        /// method ensures that there are no duplicate ages in the list and that it   
        /// is maintained in increasing order of age                                  
        /// </summary>
        /// <param name="ageDays">Age in days</param>
        /// <param name="numMales">Number of males</param>
        /// <param name="numFemales">Number of females</param>
        public void Input(int ageDays, int numMales, int numFemales)
        {
            int posIdx, idx;

            posIdx = 0;
            while ((posIdx < this.Count) && (this.FData[posIdx].AgeDays < ageDays))
                posIdx++;
            if ((posIdx < this.Count) && (this.FData[posIdx].AgeDays == ageDays)) 
            {
                // If we find A already in the list, then increment the corresponding numbers of animals 
                this.FData[posIdx].NumMales += numMales;                                                      
                this.FData[posIdx].NumFemales += numFemales;
            }
            else                                                           
            {
                // Otherwise insert a new element in the correct place
                this.SetCount(this.Count + 1);
                for (idx = this.Count - 1; idx >= posIdx + 1; idx--)
                    this.FData[idx] = this.FData[idx - 1];
                this.FData[posIdx].AgeDays = ageDays;
                this.FData[posIdx].NumMales = numMales;
                this.FData[posIdx].NumFemales = numFemales;
            }
        }
        
        /// <summary>
        /// Change the numbers of male and female animals to new values.              
        /// </summary>
        /// <param name="numMales">New total number of male animals to place in the list</param>
        /// <param name="numFemales">New total number of female animals to place in the list</param>
        public void Resize(int numMales, int numFemales)
        {
            int CurrM = 0;
            int CurrF = 0;
            int MLeft;
            int FLeft;
            int idx;

            this.Pack();                                                                // Ensure there are no empty list members   

            if (this.Count == 0)                                                        // Hard to do anything with no age info     
                Input(365 * 3, numMales, numFemales);
            else if (this.Count == 1)
            {
                this.FData[0].NumMales = numMales;
                this.FData[0].NumFemales = numFemales;
            }
            else
            {
                this.GetOlder(-1, ref CurrM, ref CurrF);                                // Work out number of animals currently in the list 
                MLeft = numMales;                                                                                      
                FLeft = numFemales;
                for (idx = 0; idx <= this.Count - 1; idx++)
                {
                    if ((numMales == 0) || (CurrM > 0))
                        this.FData[idx].NumMales = Convert.ToInt32(Math.Truncate(numMales * StdMath.XDiv(this.FData[idx].NumMales, CurrM)), CultureInfo.InvariantCulture);
                    else
                        this.FData[idx].NumMales = Convert.ToInt32(Math.Truncate(numMales * StdMath.XDiv(this.FData[idx].NumFemales, CurrF)), CultureInfo.InvariantCulture);
                    if ((numFemales == 0) || (CurrF > 0))
                        this.FData[idx].NumFemales = Convert.ToInt32(Math.Truncate(numFemales * StdMath.XDiv(this.FData[idx].NumFemales, CurrF)), CultureInfo.InvariantCulture);
                    else
                        this.FData[idx].NumFemales = Convert.ToInt32(Math.Truncate(numFemales * StdMath.XDiv(this.FData[idx].NumMales, CurrM)), CultureInfo.InvariantCulture);
                    MLeft -= this.FData[idx].NumMales;
                    FLeft -= this.FData[idx].NumFemales;
                }

                idx = this.Count - 1;                                                   // Add the "odd" animals into the oldest groups as evenly as possible   
                while ((MLeft > 0) || (FLeft > 0))                                                      
                {
                    if (MLeft > 0)
                    {
                        this.FData[idx].NumMales++;
                        MLeft--;
                    }
                    if (FLeft > 0)
                    {
                        this.FData[idx].NumFemales++;
                        FLeft--;
                    }

                    idx--;
                    if (idx < 0)
                        idx = this.Count - 1;
                }
            }
            this.Pack();
        }
        
        /// <summary>
        /// Set the count of items to 0
        /// </summary>
        public void Clear()
        {
            this.SetCount(0);
        }
        
        /// <summary>
        /// Add all elements of OtherAges into the object.  Unlike AnimalGroup.Merge,
        /// AgeList.Merge does not free otherAges.                                   
        /// </summary>
        /// <param name="otherAges">The other agelist</param>
        public void Merge(AgeList otherAges)
        {
            int idx;

            for (idx = 0; idx <= otherAges.Count - 1; idx++)
            {
                Input(otherAges.FData[idx].AgeDays,
                       otherAges.FData[idx].NumMales,
                       otherAges.FData[idx].NumFemales);
            }
        }
        
        /// <summary>
        /// Split the age group by age. If ByAge=TRUE, oldest animals are placed in the result.
        /// If ByAge=FALSE, the age structures are made the same as far as possible.
        /// </summary>
        /// <param name="numMale">Number of male</param>
        /// <param name="numFemale">Number of female</param>
        /// <param name="ByAge">Split by age</param>
        /// <returns></returns>
        public AgeList Split(int numMale, int numFemale, bool ByAge)
        {
            int[,] TransferNo;                                          // 0,x =male, 1,x =female                         
            int[] TotalNo = new int[2];
            int[] TransfersReqd = new int[2];
            int[] TransfersDone = new int[2];
            double[] TransferPropn = new double[2];
            int iAnimal, iFirst, iLast;
            int idx, jdx;

            AgeList result = new AgeList(RandFactory);                  // Create a list with the same age          
            for (idx = 0; idx <= this.Count - 1; idx++)                 // structure but no animals               
                result.Input(this.FData[idx].AgeDays, 0, 0);

            TransfersReqd[0] = numMale;
            TransfersReqd[1] = numFemale;
            TransferNo = new int[2, this.Count];                        // Assume that this zeros TransferNo        

            for (jdx = 0; jdx <= 1; jdx++)
                TransfersDone[jdx] = 0;

            if (ByAge)                                                  
            {
                // If ByAge=TRUE, oldest animals are placed in Result
                for (idx = this.Count - 1; idx >= 0; idx--)
                {
                    TransferNo[0, idx] = Math.Min(TransfersReqd[0] - TransfersDone[0], this.FData[idx].NumMales);
                    TransferNo[1, idx] = Math.Min(TransfersReqd[1] - TransfersDone[1], this.FData[idx].NumFemales);
                    for (jdx = 0; jdx <= 1; jdx++)
                        TransfersDone[jdx] += TransferNo[jdx, idx];
                }
            }
            else                                                                      
            {
                // If ByAge=FALSE, the age structures are made the same as far as possible
                this.GetOlder(-1, ref TotalNo[0], ref TotalNo[1]);

                for (jdx = 0; jdx <= 1; jdx++)
                {
                    TransfersReqd[jdx] = Math.Min(TransfersReqd[jdx], TotalNo[jdx]);
                    TransferPropn[jdx] = StdMath.XDiv(TransfersReqd[jdx], TotalNo[jdx]);
                }

                for (idx = 0; idx <= this.Count - 1; idx++)
                {
                    TransferNo[0, idx] = Convert.ToInt32(Math.Round(TransferPropn[0] * this.FData[idx].NumMales), CultureInfo.InvariantCulture);
                    TransferNo[1, idx] = Convert.ToInt32(Math.Round(TransferPropn[1] * this.FData[idx].NumFemales), CultureInfo.InvariantCulture);
                    for (jdx = 0; jdx <= 1; jdx++)
                        TransfersDone[jdx] += TransferNo[jdx, idx];
                }

                for (jdx = 0; jdx <= 1; jdx++)                                                  
                {
                    // Randomly allocate roundoff errors
                    while (TransfersDone[jdx] < TransfersReqd[jdx])                     // Too few transfers                        
                    {
                        iAnimal = Convert.ToInt32(Math.Min(Math.Truncate(this.RandFactory.RandomValue() * (TotalNo[jdx] - TransfersDone[jdx])),
                                        (TotalNo[jdx] - TransfersDone[jdx]) - 1), CultureInfo.InvariantCulture);
                        idx = -1;
                        iLast = 0;
                        do
                        {
                            idx++;
                            iFirst = iLast;
                            if (jdx == 0)
                                iLast = iFirst + (this.FData[idx].NumMales - TransferNo[jdx, idx]);
                            else
                                iLast = iFirst + (this.FData[idx].NumFemales - TransferNo[jdx, idx]);
                            //// until (Idx = Count-1) or ((iAnimal >= iFirst) and (iAnimal < iLast));
                        }
                        while ((idx != this.Count - 1) && ((iAnimal < iFirst) || (iAnimal >= iLast)));

                        TransferNo[jdx, idx]++;
                        TransfersDone[jdx]++;
                    }

                    while (TransfersDone[jdx] > TransfersReqd[jdx])                                  
                    {
                        // Too many transfers
                        iAnimal = Convert.ToInt32(Math.Min(Math.Truncate(this.RandFactory.RandomValue() * TransfersDone[jdx]),
                                        TransfersDone[jdx] - 1), CultureInfo.InvariantCulture);
                        idx = -1;
                        iLast = 0;
                        do
                        {
                            idx++;
                            iFirst = iLast;
                            iLast = iFirst + TransferNo[jdx, idx];
                            //// until (Idx = Count-1) or ((iAnimal >= iFirst) and (iAnimal < iLast));
                        }
                        while ((idx != this.Count - 1) && ((iAnimal < iFirst) || (iAnimal >= iLast)));

                        TransferNo[jdx, idx]--;
                        TransfersDone[jdx]--;
                    }
                }
            }

            for (idx = 0; idx <= this.Count - 1; idx++)                                                // Carry out transfers                      
            {
                this.FData[idx].NumMales -= TransferNo[0, idx];
                result.FData[idx].NumMales += TransferNo[0, idx];
                this.FData[idx].NumFemales -= TransferNo[1, idx];
                result.FData[idx].NumFemales += TransferNo[1, idx];
            }

            this.Pack();                                                                // Clear away empty entries in both lists   
            result.Pack();

            return result;
        }

        /// <summary>
        /// Increase all ages by the same amount (NoDays)                             
        /// </summary>
        /// <param name="NoDays"></param>
        public void AgeBy(int NoDays)
        {
            for (int idx = 0; idx <= this.Count - 1; idx++)
                this.FData[idx].AgeDays += NoDays;
        }

        /// <summary>
        /// Compute the mean age of all animals in the list                           
        /// </summary>
        /// <returns>The mean age</returns>
        public int MeanAge()
        {
            double AxN, N;
            double dN;
            int idx;
            int result;

            if (this.Count == 1)
                result = this.FData[0].AgeDays;
            else if (this.Count == 0)
                result = 0;
            else
            {
                AxN = 0;
                N = 0;
                for (idx = 0; idx <= this.Count - 1; idx++)
                {
                    dN = this.FData[idx].NumMales + this.FData[idx].NumFemales;
                    AxN = AxN + dN * this.FData[idx].AgeDays;
                    N = N + dN;
                }
                if (N > 0.0)
                    result = Convert.ToInt32(Math.Round(AxN / N), CultureInfo.InvariantCulture);
                else
                    result = 0;
            }
            return result;
        }

        /// <summary>
        /// Returns the number of male and female animals      
        /// which are aged greater than ageDays days                                        
        /// </summary>
        /// <param name="ageDays">Age in days</param>
        /// <param name="numMale">Number of male</param>
        /// <param name="numFemale">Number of female</param>
        public void GetOlder(int ageDays, ref int numMale, ref int numFemale)
        {
            int idx;

            numMale = 0;
            numFemale = 0;
            for (idx = 0; idx <= Count - 1; idx++)
            {
                if (this.FData[idx].AgeDays > ageDays)
                {
                    numMale += this.FData[idx].NumMales;
                    numFemale += this.FData[idx].NumFemales;
                }
            }
        }
    }
    #endregion AgeList

    /// <summary>
    /// Set of differences between two sub-groups of animals.  Used in the Split  
    /// method of AnimalGroup                                                     
    /// </summary>
    [Serializable]
    public struct DifferenceRecord
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
    public struct AnimalWeather
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
    /// AnimalStateInfo type. Information required to reset the state in the case of RDP insufficiency                                                                
    /// </summary>
    [Serializable]
    public struct AnimalStateInfo
    {
        /// <summary>
        /// Base weight without wool
        /// </summary>
        public double BaseWeight;
        
        /// <summary>
        /// Weight of wool
        /// </summary>
        public double WoolWt;
        
        /// <summary>
        /// Wool microns
        /// </summary>
        public double WoolMicron;
        
        /// <summary>
        /// Depth of coat
        /// </summary>
        public double CoatDepth;
        
        /// <summary>
        /// Foetal weight
        /// </summary>
        public double FoetalWt;
        
        /// <summary>
        /// Lactation adjustment
        /// </summary>
        public double LactAdjust;
        
        /// <summary>
        /// Lactation ratio
        /// </summary>
        public double LactRatio;
        
        /// <summary>
        /// Phosphorous value
        /// </summary>
        public double BasePhos;
        
        /// <summary>
        /// Sulphur value
        /// </summary>
        public double BaseSulf;
    }

    /// <summary>
    /// ExcretionInfo type. Totalled amounts of excretion                           
    /// </summary>
    [Serializable]
    public class ExcretionInfo
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
        public double Defaecations;
        
        /// <summary>
        /// Volume per defaecation, m^3 (fresh basis)
        /// </summary>
        public double DefaecationVolume;
        
        /// <summary>
        /// Area per defaecation, m^2 (fresh basis)
        /// </summary>
        public double DefaecationArea;
        
        /// <summary>
        /// Eccentricity of faeces
        /// </summary>
        public double DefaecationEccentricity;
        
        /// <summary>
        /// Proportion of faecal inorganic N that is nitrate
        /// </summary>
        public double FaecalNO3Propn;
        
        /// <summary>
        /// Number in the time step by all animals (not including unweaned young)
        /// </summary>
        public double Urinations;
        
        /// <summary>
        /// Fluid volume per urination, m^3
        /// </summary>
        public double UrinationVolume;
        
        /// <summary>
        /// Area covered by each urination at the soil surface, m^2
        /// </summary>
        public double UrinationArea;
        
        /// <summary>
        /// Eccentricity of urinations
        /// </summary>
        public double dUrinationEccentricity;
    }

    #region AnimalGroup
    // =============================================================================================
    /// <summary>
    /// AnimalGroup class
    /// </summary>
    [Serializable]
    public class AnimalGroup
    {
        /// <summary>
        /// AnimalsDynamicGlb differentiates between the "static" version of the      
        /// model used in GrazFeed and the "dynamic" version used elsewhere           
        /// </summary>
        public const bool AnimalsDynamicGlb = true;
        
        /// <summary>
        /// Represents no difference
        /// </summary>
        public DifferenceRecord NODIFF = new DifferenceRecord() { StdRefWt = 0, BaseWeight = 0, FleeceWt = 0 };
        
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
        protected AnimalParamSet AParams;
        
        /// <summary>
        /// Paramters of the animal mated to
        /// </summary>
        protected AnimalParamSet FMatedTo;
        
        /// <summary>
        /// Distribution of ages
        /// </summary>
        protected AgeList Ages;
        
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
        /// The daily deaths
        /// </summary>
        protected int FDeaths;

        /// <summary>
        /// The mothers animal group
        /// </summary>
        protected AnimalGroup Mothers;

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
        protected GrazType.GrazingInputs Inputs = new GrazType.GrazingInputs();
        
        /// <summary>
        /// The animal's environment
        /// </summary>
        protected AnimalWeather TheEnv;
        
        /// <summary>
        /// 
        /// </summary>
        protected double WaterLog;
        
        /// <summary>
        /// The ration being fed
        /// </summary>
        protected SupplementRation TheRation;
        
        /// <summary>
        /// 
        /// </summary>
        protected FoodSupplement FIntakeSupp;
        
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

        //// Model logic ...................................................
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ClssAttr"></param>
        /// <param name="NetClassIntake"></param>
        /// <param name="summaryIntake"></param>
        private void AddDietElement(ref GrazType.IntakeRecord ClssAttr, double NetClassIntake, ref GrazType.IntakeRecord summaryIntake)
        {
            if (NetClassIntake > 0.0)
            {
                summaryIntake.Biomass = summaryIntake.Biomass + NetClassIntake;
                summaryIntake.Digestibility = summaryIntake.Digestibility + NetClassIntake * ClssAttr.Digestibility;
                summaryIntake.CrudeProtein = summaryIntake.CrudeProtein + NetClassIntake * ClssAttr.CrudeProtein;
                summaryIntake.Degradability = summaryIntake.Degradability + NetClassIntake * ClssAttr.CrudeProtein * ClssAttr.Degradability;
                summaryIntake.PhosContent = summaryIntake.PhosContent + NetClassIntake * ClssAttr.PhosContent;
                summaryIntake.SulfContent = summaryIntake.SulfContent + NetClassIntake * ClssAttr.SulfContent;
                summaryIntake.AshAlkalinity = summaryIntake.AshAlkalinity + NetClassIntake * ClssAttr.AshAlkalinity;
            }
        }

        /// <summary>
        /// Summarise the intake record
        /// </summary>
        /// <param name="summaryIntake">The intake record</param>
        private void SummariseIntakeRecord(ref GrazType.IntakeRecord summaryIntake)
        {
            double trivialIntake = 1.0E-6; // (kg/head)

            if (summaryIntake.Biomass < trivialIntake)
                summaryIntake = new GrazType.IntakeRecord();
            else
            {
                summaryIntake.Digestibility = summaryIntake.Digestibility / summaryIntake.Biomass;
                if (summaryIntake.CrudeProtein > 0.0)
                    summaryIntake.Degradability = summaryIntake.Degradability / summaryIntake.CrudeProtein;
                else
                    summaryIntake.Degradability = 0.75;

                summaryIntake.CrudeProtein = summaryIntake.CrudeProtein / summaryIntake.Biomass;
                summaryIntake.PhosContent = summaryIntake.PhosContent / summaryIntake.Biomass;
                summaryIntake.SulfContent = summaryIntake.SulfContent / summaryIntake.Biomass;
                summaryIntake.AshAlkalinity = summaryIntake.AshAlkalinity / summaryIntake.Biomass;
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
        /// <param name="herbageRI">"Relative intakes" of each herbage digestibility class</param>
        /// <param name="seedRI">"Relative intakes" of seeds</param>
        /// <param name="suppRI">"Relative intakes" of supplement</param>
        /// <param name="timeStepState"></param>
        protected void DescribeTheDiet(
                                   ref double[] herbageRI,         
                                   ref double[,] seedRI,           
                                   ref double suppRI,                                             
                                   ref AnimalOutput timeStepState)
        {
            GrazType.IntakeRecord suppInput = new GrazType.IntakeRecord();
            double gutPassage;
            double supp_ME2DM;                                              // Used to compute ME_2_DM.Supp          
            int species, classIdx, ripeIdx, idx;

            for (classIdx = 1; classIdx <= GrazType.DigClassNo; classIdx++)
                timeStepState.IntakePerHead.Herbage[classIdx] = this.IntakeLimit * herbageRI[classIdx];
            for (species = 1; species <= GrazType.MaxPlantSpp; species++)
            {
                for (ripeIdx = GrazType.UNRIPE; ripeIdx <= GrazType.RIPE; ripeIdx++)
                    timeStepState.IntakePerHead.Seed[species, ripeIdx] = this.IntakeLimit * seedRI[species, ripeIdx];
            }
            timeStepState.PaddockIntake = new GrazType.IntakeRecord();              // Summarise herbage+seed intake         
            for (classIdx = 1; classIdx <= GrazType.DigClassNo; classIdx++)
                this.AddDietElement(ref this.Inputs.Herbage[classIdx], timeStepState.IntakePerHead.Herbage[classIdx], ref timeStepState.PaddockIntake);
            for (species = 1; species <= GrazType.MaxPlantSpp; species++)
            {
                for (ripeIdx = GrazType.UNRIPE; ripeIdx <= GrazType.RIPE; ripeIdx++)
                    this.AddDietElement(ref this.Inputs.Seeds[species, ripeIdx], timeStepState.IntakePerHead.Seed[species, ripeIdx], ref timeStepState.PaddockIntake);
            }

            this.SummariseIntakeRecord(ref timeStepState.PaddockIntake);
            if (timeStepState.PaddockIntake.Biomass == 0.0) // i.e. less than fTrivialIntake
                timeStepState.IntakePerHead = new GrazType.GrazingOutputs();

            timeStepState.SuppIntake = new GrazType.IntakeRecord();                 // Summarise supplement intake           
            supp_ME2DM = 0.0;
            if ((this.TheRation.TotalAmount > 0.0) && (suppRI * this.IntakeLimit > 0.0))
            {
                // The supplements must be treated separately because of the non-linearity in the gut passage term
                for (idx = 0; idx <= this.TheRation.Count - 1; idx++)                                 
                {                                                                   
                    suppInput.Digestibility = this.TheRation[idx].DMDigestibility;           
                    suppInput.CrudeProtein = this.TheRation[idx].CrudeProt;
                    suppInput.Degradability = this.TheRation[idx].DegProt;
                    suppInput.PhosContent = this.TheRation[idx].Phosphorus;
                    suppInput.SulfContent = this.TheRation[idx].Sulphur;
                    suppInput.AshAlkalinity = this.TheRation[idx].AshAlkalinity;

                    if (this.Animal == GrazType.AnimalType.Cattle)
                        gutPassage = this.TheRation[idx].MaxPassage * StdMath.RAMP(this.TheRation.TotalAmount / IntakeLimit, 0.20, 0.75);
                    else
                        gutPassage = 0.0;
                    this.TimeStepNetSupp_DMI[idx] = (1.0 - gutPassage) * this.TheRation.GetFWFract(idx) * (IntakeLimit * suppRI);

                    this.AddDietElement(ref suppInput, this.TimeStepNetSupp_DMI[idx], ref timeStepState.SuppIntake);
                    supp_ME2DM = supp_ME2DM + this.TimeStepNetSupp_DMI[idx] * this.TheRation[idx].ME2DM;
                }

                this.SummariseIntakeRecord(ref timeStepState.SuppIntake);
                if (timeStepState.SuppIntake.Biomass == 0.0) 
                {
                    // i.e. less than fTrivialIntake
                    for (idx = 0; idx <= this.TheRation.Count - 1; idx++)
                        this.TimeStepNetSupp_DMI[idx] = 0.0;
                    supp_ME2DM = 0.0;
                }
                else
                    supp_ME2DM = StdMath.XDiv(supp_ME2DM, timeStepState.SuppIntake.Biomass);
            }
            else
                for (idx = 0; idx <= this.TheRation.Count - 1; idx++)
                    this.TimeStepNetSupp_DMI[idx] = 0.0;

            timeStepState.DM_Intake.Herbage = timeStepState.PaddockIntake.Biomass;                                  // Dry matter intakes                    
            timeStepState.DM_Intake.Supp = timeStepState.SuppIntake.Biomass;
            timeStepState.DM_Intake.Solid = timeStepState.DM_Intake.Herbage + timeStepState.DM_Intake.Supp;
            timeStepState.DM_Intake.Total = timeStepState.DM_Intake.Solid;                                          // Milk doesn't count for DM intake      

            timeStepState.Digestibility.Herbage = timeStepState.PaddockIntake.Digestibility;                        // Digestibilities                       
            timeStepState.Digestibility.Supp = timeStepState.SuppIntake.Digestibility;
            timeStepState.Digestibility.Solid = StdMath.XDiv(
                                           timeStepState.Digestibility.Supp * timeStepState.DM_Intake.Supp +
                                           timeStepState.Digestibility.Herbage * timeStepState.DM_Intake.Herbage,
                                           timeStepState.DM_Intake.Solid);

            if (this.LactStatus == GrazType.LactType.Suckling)                                                                         
            {
                // Milk terms
                timeStepState.CP_Intake.Milk = this.Mothers.Milk_ProtProdn / this.NoOffspring;
                timeStepState.Phos_Intake.Milk = this.Mothers.Milk_PhosProdn / this.NoOffspring;
                timeStepState.Sulf_Intake.Milk = this.Mothers.Milk_SulfProdn / this.NoOffspring;
                timeStepState.ME_Intake.Milk = this.Mothers.Milk_MJProdn / this.NoOffspring;
            }
            else
            {
                timeStepState.CP_Intake.Milk = 0.0;
                timeStepState.Phos_Intake.Milk = 0.0;
                timeStepState.Sulf_Intake.Milk = 0.0;
                timeStepState.ME_Intake.Milk = 0.0;
            }

            timeStepState.CP_Intake.Herbage = timeStepState.PaddockIntake.Biomass * timeStepState.PaddockIntake.CrudeProtein;   // Crude protein intakes and contents    
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

            timeStepState.ME_2_DM.Herbage = GrazType.HerbageE2DM * timeStepState.Digestibility.Herbage - 2.0;                   // Metabolizable energy intakes and contents     
            timeStepState.ME_2_DM.Supp = supp_ME2DM;                                                                                                        
            timeStepState.ME_Intake.Supp = timeStepState.ME_2_DM.Supp * timeStepState.DM_Intake.Supp;
            timeStepState.ME_Intake.Herbage = timeStepState.ME_2_DM.Herbage * timeStepState.DM_Intake.Herbage;
            timeStepState.ME_Intake.Solid = timeStepState.ME_Intake.Herbage + timeStepState.ME_Intake.Supp;
            timeStepState.ME_Intake.Total = timeStepState.ME_Intake.Solid + timeStepState.ME_Intake.Milk;
            timeStepState.ME_2_DM.Solid = StdMath.XDiv(timeStepState.ME_Intake.Solid, timeStepState.DM_Intake.Solid);
        }

        /// <summary>
        /// Compute RDP intake and requirement for a given MEI and feeding level      
        /// </summary>
        /// <param name="latitude">The latitude</param>
        /// <param name="Day">Day</param>
        /// <param name="IntakeScale"></param>
        /// <param name="FL"></param>
        /// <param name="CorrDg"></param>
        /// <param name="RDPI"></param>
        /// <param name="RDPR"></param>
        /// <param name="UDPIs"></param>
        protected void ComputeRDP(double latitude,
                                     int Day,
                                     double IntakeScale,            // Assumed scaling factor for intake        
                                     double FL,                     // Assumed feeding level                    
                                     ref DietRecord CorrDg,
                                     ref double RDPI, ref double RDPR,
                                     ref DietRecord UDPIs)
        {
            DietRecord RDPIs;
            double suppFME_Intake;                                                                      // Fermentable ME intake of supplement      
            int idx;

            CorrDg.Herbage = this.AnimalState.PaddockIntake.Degradability                               // Correct the protein degradability        
                              * (1.0 - (AParams.DgProtC[1] - this.AParams.DgProtC[2] * this.AnimalState.Digestibility.Herbage)// for feeding level                     
                                       * Math.Max(FL, 0.0));
            CorrDg.Supp = this.AnimalState.SuppIntake.Degradability
                              * (1.0 - this.AParams.DgProtC[3] * Math.Max(FL, 0.0));

            RDPIs.Herbage = IntakeScale * this.AnimalState.CP_Intake.Herbage * this.AnimalState.CorrDgProt.Herbage;
            RDPIs.Supp = IntakeScale * this.AnimalState.CP_Intake.Supp * this.AnimalState.CorrDgProt.Supp;
            RDPIs.Solid = RDPIs.Herbage + RDPIs.Supp;
            RDPIs.Milk = 0.0;                                                                           // This neglects any degradation of milk    
            UDPIs.Herbage = IntakeScale * this.AnimalState.CP_Intake.Herbage - RDPIs.Herbage;           // CPI late in lactation when the rumen   
            UDPIs.Supp = IntakeScale * this.AnimalState.CP_Intake.Supp - RDPIs.Supp;                    // has begun to develop                   
            UDPIs.Milk = this.AnimalState.CP_Intake.Milk;
            UDPIs.Solid = UDPIs.Herbage + UDPIs.Supp;
            RDPI = RDPIs.Solid + RDPIs.Milk;

            suppFME_Intake = StdMath.DIM(IntakeScale * this.AnimalState.ME_Intake.Supp,                 // Fermentable ME intake of supplement      
                                   GrazType.ProteinE2DM * UDPIs.Supp);                                  // leaves out the ME derived from         
            for (idx = 0; idx <= TheRation.Count - 1; idx++)                                            // undegraded protein and oils            
                suppFME_Intake = StdMath.DIM(suppFME_Intake,
                                       GrazType.FatE2DM * TheRation[idx].EtherExtract * IntakeScale * NetSupp_DMI[idx]);

            RDPR = (this.AParams.DgProtC[4] + this.AParams.DgProtC[5] * (1.0 - Math.Exp(-this.AParams.DgProtC[6] * (FL + 1.0))))       // RDP requirement                          
                    * (IntakeScale * this.AnimalState.ME_Intake.Herbage
                        * (1.0 + AParams.DgProtC[7] * (latitude / 40.0)
                                            * Math.Sin(GrazEnv.DAY2RAD * StdDate.DOY(Day, true))) + suppFME_Intake);
        }
        
        /// <summary>
        /// Set the standard reference weight of a group of animals based on breed  
        /// and sex                                                                   
        /// </summary>
        protected void ComputeSRW()
        {
            double SRW;                                                             // Breed standard reference weight (i.e.    
                                                                                    // normal weight of a mature, empty female)

            if (Mothers != null)                                                    // For lambs and calves, take both parents' 
                SRW = AParams.BreedSRW;     // 0.5 * (BreedSRW + MaleSRW)           // breed SRW's into account               
            else
                SRW = AParams.BreedSRW;

            if (NoMales == 0)                                                       // Now take into account different SRWs of  
                StdRefWt = SRW;                                                     // males and females and different        
            else                                                                    // scalars for entire and castrated males 
                StdRefWt = SRW * StdUnits.StdMath.XDiv(NoFemales + NoMales * AParams.SRWScalars[(int)ReproStatus],       // TODO: check this
                                        NoFemales + NoMales);
        }

        /// <summary>
        /// Reference birth weight, adjusted for number of foetuses and relative size 
        /// </summary>
        /// <returns></returns>
        protected double fBirthWtForSize()
        {
            return this.AParams.StdBirthWt(NoFoetuses) * ((1.0 - this.AParams.PregC[4]) + this.AParams.PregC[4] * Size);
        }
        
        /// <summary>
        ///  "Normal weight" of the foetus and the weight of the conceptus in pregnant animals.         
        /// </summary>
        /// <returns>The normal weight</returns>
        protected double FoetalNormWt()
        {
            if ((this.ReproStatus == GrazType.ReproType.EarlyPreg) || (this.ReproStatus == GrazType.ReproType.LatePreg))
                return fBirthWtForSize() * fGompertz(FoetalAge, this.AParams.PregC[1], this.AParams.PregC[2], this.AParams.PregC[3]);
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
        /// <returns>Conceptus weight</returns>
        protected double ConceptusWt()
        {
            if ((ReproStatus == GrazType.ReproType.EarlyPreg) || (ReproStatus == GrazType.ReproType.LatePreg))
                return NoFoetuses
                          * (this.AParams.PregC[5] * fBirthWtForSize() * fGompertz(FoetalAge, this.AParams.PregC[1], this.AParams.PregC[6], this.AParams.PregC[7])
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
        /// <param name="ageDays"></param>
        /// <param name="parameters"></param>
        /// <returns>Maximum normal weight</returns>
        public static double MaxNormWtFunc(double SRW, double BW,
                                int ageDays,
                                AnimalParamSet parameters)
        {
            double GrowthRate;

            GrowthRate = parameters.GrowthC[1] / Math.Pow(SRW, parameters.GrowthC[2]);
            return SRW - (SRW - BW) * Math.Exp(-GrowthRate * ageDays);
        }
        
        /// <summary>
        /// Normal weight equation                                                 
        /// </summary>
        /// <param name="ageDays"></param>
        /// <param name="maxOldWt"></param>
        /// <param name="weighting"></param>
        /// <returns></returns>
        protected double NormalWeightFunc(int ageDays, double maxOldWt, double weighting)
        {
            double fMaxNormWt;

            fMaxNormWt = MaxNormWtFunc(StdRefWt, BirthWt, ageDays, AParams);
            if (maxOldWt < fMaxNormWt)                                           // Delayed deveopment of frame size         
                return weighting * fMaxNormWt + (1.0 - weighting) * maxOldWt;
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
            double fibreCount;
            double fibreArea;

            // WITH AParams DO
            if (this.Animal == GrazType.AnimalType.Cattle)
                this.FCoatDepth = 1.0;
            else
            {
                fibreCount = this.AParams.WoolC[11] * this.AParams.ChillC[1] * Math.Pow(this.NormalWt, 2.0 / 3.0);
                fibreArea = Math.PI / 4.0 * Math.Pow(this.WoolMicron * 1E-6, 2.0);
                this.FCoatDepth = 100.0 * this.AParams.WoolC[3] * this.WoolWt / (fibreCount * this.AParams.WoolC[10] * fibreArea);
            }
        }

        /// <summary>
        /// In sheep, the coat depth is used to set the total wool weight (this is the  
        /// way that shearing is done)                                                  
        /// </summary>
        /// <param name="coatDepth">Coat depth for which a greasy wool weight is to be calculated (cm)</param>
        /// <returns>Wool weight</returns>
        protected double CoatDepth2Wool(double coatDepth)
        {
            double fibreCount;
            double fibreArea;

            if (this.Animal == GrazType.AnimalType.Sheep)
            {
                fibreCount = AParams.WoolC[11] * AParams.ChillC[1] * Math.Pow(this.NormalWt, 2.0 / 3.0);
                fibreArea = Math.PI / 4.0 * Math.Pow(WoolMicron * 1E-6, 2);
                return (fibreCount * AParams.WoolC[10] * fibreArea) * coatDepth / (100.0 * AParams.WoolC[3]);
            }
            else
                return 0.0;
        }

        /// <summary>
        /// Get the conception rates array
        /// </summary>
        /// <returns>Conception rates</returns>
        protected double[] getConceptionRates()
        {
            const double STD_LATITUDE = -35.0;      // Latitude (in degrees) for which the DayLengthConst[] parameters are set    
            int iDOY;
            double fDLFactor;
            double fPropn;
            int N;

            double[] result = new double[4];        // TConceptionArray

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
                    result[N] = fPropn;
                else
                {
                    result[N] = fPropn * result[N - 1];
                    result[N - 1] = result[N - 1] - result[N];
                }
            }

            for (N = 1; N <= AParams.MaxYoung - 1; N++)
            {
                result[N] = StdMath.DIM(result[N], result[N + 1]);
            }

            return result;
        }

        /// <summary>
        /// Make the animals pregnant
        /// </summary>
        /// <param name="conceptionRate">Conception rates</param>
        /// <param name="newGroups">The new animal groups</param>
        protected void makePregnantAnimals(double[] conceptionRate, ref AnimalList newGroups)
        {
            int initialNumber;
            DifferenceRecord fertileDiff;
            AnimalGroup pregGroup;
            int numPreg, n;

            // A weight differential between conceiving and barren animals
            fertileDiff = new DifferenceRecord() { StdRefWt = NODIFF.StdRefWt, BaseWeight = NODIFF.BaseWeight, FleeceWt = NODIFF.FleeceWt };
            fertileDiff.BaseWeight = AParams.FertWtDiff;

            initialNumber = NoAnimals;
            for (n = 1; n <= AParams.MaxYoung; n++)
            {
                numPreg = Math.Min(NoAnimals, RandFactory.RndPropn(initialNumber, conceptionRate[n]));
                pregGroup = Split(numPreg, false, fertileDiff, NODIFF);
                if (pregGroup != null)
                {
                    pregGroup.Pregnancy = 1;
                    pregGroup.NoFoetuses = n;
                    CheckAnimList(ref newGroups);
                    newGroups.Add(pregGroup);
                }
            }
        }

        /// <summary>
        /// Used in createYoung() to set up the genotypic parameters of the lambs     
        /// or calves that are about to be born/created.                              
        /// </summary>
        /// <returns></returns>
        protected AnimalParamSet constructOffspringParams()
        {
            AnimalParamBlend[] mateBlend = new AnimalParamBlend[1];

            if (FMatedTo != null)
            {
                Array.Resize(ref mateBlend, 2);
                mateBlend[0].Breed = this.AParams;
                mateBlend[0].fPropn = 0.5;
                mateBlend[1].Breed = FMatedTo;
                mateBlend[1].fPropn = 0.5;

                return AnimalParamSet.CreateFactory(string.Empty, mateBlend);
            }
            else
                return new AnimalParamSet(null, AParams);
        }

        /// <summary>
        ///  Carry out one cycle's worth of conceptions                                
        /// </summary>
        /// <param name="newGroups"></param>
        private void Conceive(ref AnimalList newGroups)
        {
            if ((ReproStatus == GrazType.ReproType.Empty)
               && (!((this.AParams.Animal == GrazType.AnimalType.Sheep) && (LactStatus == GrazType.LactType.Lactating)))
               && (MateCycle == 0))
                makePregnantAnimals(getConceptionRates(), ref newGroups);
        }

        /// <summary>
        /// Death rate calculation
        /// </summary>
        /// <returns>The death rate</returns>
        private double DeathRateFunc()
        {
            double growthRate;
            double deltaNormalWt;
            double result;

            growthRate = this.AParams.GrowthC[1] / Math.Pow(StdRefWt, AParams.GrowthC[2]);
            deltaNormalWt = (StdRefWt - BirthWt) * (Math.Exp(-growthRate * (MeanAge - 1)) - Math.Exp(-growthRate * MeanAge));

            result = 1.0 - ExpectedSurvival(1);
            if ((LactStatus != GrazType.LactType.Suckling) && (this.Condition < this.AParams.MortCondConst) && (DeltaBaseWeight < 0.2 * deltaNormalWt))
                result = result + this.AParams.MortIntensity * (this.AParams.MortCondConst - this.Condition);
            return result;
        }

        /// <summary>
        /// Exposure calculations
        /// </summary>
        /// <returns>Exposure value</returns>
        private double ExposureFunc()
        {
            double exposureOdds;
            double exp_ExpOdds;
            double result;

            exposureOdds = this.AParams.ExposureConsts[0] - this.AParams.ExposureConsts[1] * Condition + this.AParams.ExposureConsts[2] * ChillIndex;
            if (NoOffspring > 1)
                exposureOdds = exposureOdds + this.AParams.ExposureConsts[3];
            exp_ExpOdds = Math.Exp(exposureOdds);
            result = exp_ExpOdds / (1.0 + exp_ExpOdds);
            return result;
        }

        /// <summary>
        /// Mortality submodel                                                        
        /// </summary>
        /// <param name="chill"></param>
        /// <param name="newGroups"></param>
        protected void Kill(double chill, ref AnimalList newGroups)
        {
            double deathRate;
            DifferenceRecord Diffs;
            int maleLosses;
            int femaleLosses;
            int NoLosses;
            int YoungLosses;
            int YoungToKill;
            AnimalGroup DeadGroup;
            AnimalGroup SplitGroup;

            Diffs = new DifferenceRecord() { StdRefWt = NODIFF.StdRefWt, BaseWeight = NODIFF.BaseWeight, FleeceWt = NODIFF.FleeceWt };
            Diffs.BaseWeight = -AParams.MortWtDiff * BaseWeight;

            deathRate = DeathRateFunc();
            femaleLosses = RandFactory.RndPropn(NoFemales, deathRate);
            maleLosses = RandFactory.RndPropn(NoMales, deathRate);
            NoLosses = maleLosses + femaleLosses;
            if ((Animal == GrazType.AnimalType.Sheep) && (Young != null) && (Young.MeanAge == 1))
                YoungLosses = RandFactory.RndPropn(Young.NoAnimals, ExposureFunc());
            else
                YoungLosses = 0;
            FDeaths = NoLosses;
            if ((Young == null) && (NoLosses > 0))
                SplitSex(maleLosses, femaleLosses, false, Diffs);

            else if ((Young != null) && (femaleLosses + YoungLosses > 0))
            {
                if (femaleLosses > 0)                                               // For now, unweaned young of dying animals 
                {                                                                   //   die with them                       
                    DeadGroup = Split(femaleLosses, false, Diffs, NODIFF);
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
                        CheckAnimList(ref newGroups);
                        newGroups.Add(SplitGroup);
                    }
                } //// _ IF (YoungToKill > 0) 
            } //// ELSE IF (Young <> NIL) and (NoLosses + YoungLosses > 0) 
        }

        /// <summary>
        /// Decrease the number of young by N per mother                               
        /// </summary>
        /// <param name="animalGrp"></param>
        /// <param name="number">Number of animals</param>
        protected void LoseYoung(AnimalGroup animalGrp, int number)
        {
            DifferenceRecord YoungDiffs;
            int iMaleYoung;
            int iFemaleYoung;
            int iYoungToLose;
            int iMalesToLose;
            int iFemalesToLose;

            if (number == animalGrp.NoOffspring)
            {
                animalGrp.Young = null;
                animalGrp.SetNoOffspring(0);
            }
            else if (number > 0)
            {
                YoungDiffs = new DifferenceRecord() { StdRefWt = NODIFF.StdRefWt, BaseWeight = NODIFF.BaseWeight, FleeceWt = NODIFF.FleeceWt };
                YoungDiffs.BaseWeight = -animalGrp.Young.AParams.MortWtDiff * animalGrp.Young.BaseWeight;

                iMaleYoung = animalGrp.Young.NoMales;
                iFemaleYoung = animalGrp.Young.NoFemales;
                iYoungToLose = number * animalGrp.NoFemales;

                iMalesToLose = Convert.ToInt32(Math.Round(iYoungToLose * StdMath.XDiv(iMaleYoung, iMaleYoung + iFemaleYoung)), CultureInfo.InvariantCulture);
                iMalesToLose = Math.Min(iMalesToLose, iMaleYoung);

                iFemalesToLose = iYoungToLose - iMalesToLose;
                if (iFemalesToLose > iFemaleYoung)
                {
                    iMalesToLose += iFemalesToLose - iFemaleYoung;
                    iFemalesToLose = iFemaleYoung;
                }

                animalGrp.Young.SplitSex(iMalesToLose, iFemalesToLose, false, YoungDiffs);
                animalGrp.FNoOffspring -= number;
                animalGrp.Young.FNoOffspring -= number;
            }
        }

        /// <summary>
        /// Pregnancy toxaemia and dystokia                                           
        /// </summary>
        /// <param name="newGroups">The new groups</param>
        protected void KillEndPreg(ref AnimalList newGroups)
        {
            double DystokiaRate;
            double ToxaemiaRate;
            AnimalGroup DystGroup;
            int numLosses;

            if ((Animal == GrazType.AnimalType.Sheep) && (FoetalAge == AParams.Gestation - 1))
                if (NoFoetuses == 1)                                                    // Calculate loss of young due to           
                {                                                                       // dystokia and move the corresponding      
                    DystokiaRate = StdMath.SIG((FoetalWt / AParams.StdBirthWt(1)) *     // number of mothers into a new animal      
                                           Math.Max(Size, 1.0),                         // group                                    
                                         AParams.DystokiaSigs);
                    numLosses = RandFactory.RndPropn(NoFemales, DystokiaRate);
                    if (numLosses > 0)
                    {
                        DystGroup = Split(numLosses, false, NODIFF, NODIFF);
                        DystGroup.Pregnancy = 0;
                        CheckAnimList(ref newGroups);
                        newGroups.Add(DystGroup);
                    } ////  IF (NoLosses > 0)
                } //// IF (NoYoung = 1) 

                else if (NoFoetuses >= 2)                                          // Deaths of sheep with multiple young      
                {                                                                  // due to pregnancy toxaemia              
                    ToxaemiaRate = StdMath.SIG((MidLatePregWt - BaseWeight) / NormalWt,
                                         AParams.ToxaemiaSigs);
                    numLosses = RandFactory.RndPropn(NoFemales, ToxaemiaRate);
                    FDeaths += numLosses;
                    if (numLosses > 0)
                        Split(numLosses, false, NODIFF, NODIFF);
                } //// ELSE IF (NoFoetuses >= 2) 
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
        /// <returns>The number of suckling young</returns>
        protected int NoSuckling()
        {
            if ((Young != null) && (Young.LactStatus == GrazType.LactType.Suckling))
                return NoOffspring;
            else
                return 0;
        }

        
        /// <summary>
        /// TODO: check that this function returns changed values
        /// </summary>
        /// <param name="AG"></param>
        /// <param name="X"></param>
        /// <param name="Diffs"></param>
        private void AdjustRecords(AnimalGroup AG, double X, DifferenceRecord Diffs)
        {
            AG.BaseWeight = AG.BaseWeight + X * Diffs.BaseWeight;
            if (AParams.Animal == GrazType.AnimalType.Sheep)
                AG.WoolWt = AG.WoolWt + X * Diffs.FleeceWt;
            AG.StdRefWt = AG.StdRefWt + X * Diffs.StdRefWt;
            AG.Calc_Weights();
            AG.TotalWeight = AG.BaseWeight + AG.ConceptusWt();                            // TotalWeight is meant to be the weight  
            if (AParams.Animal == GrazType.AnimalType.Sheep)                              // "on the scales", including conceptus   
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
        protected AnimalGroup SplitSex(int NMale, int NFemale, bool ByAge, DifferenceRecord Diffs)
        {
            double PropnGoing;

            if ((NMale > NoMales) || (NFemale > NoFemales))
                throw new Exception("AnimalGroup: Error in SplitSex method");

            AnimalGroup Result = Copy();                                                 // Create the new animal group              
            if ((NMale == NoMales) && (NFemale == NoFemales))
            {
                NoMales = 0;
                NoFemales = 0;
                Ages.Clear();
            }
            else
            {
                PropnGoing = StdMath.XDiv(NMale + NFemale, NoMales + NoFemales);        // Adjust weights etc                       
                AdjustRecords(this, -PropnGoing, Diffs);
                AdjustRecords(Result, 1.0 - PropnGoing, Diffs);

                Result.NoMales = NMale;                                                 // Set up numbers in the two groups and     
                Result.NoFemales = NFemale;                                             // split up the age list                  
                Result.Ages = this.Ages.Split(NMale, NFemale, ByAge);
                Result.MeanAge = Result.Ages.MeanAge();

                this.NoMales = this.NoMales - NMale;
                this.NoFemales = this.NoFemales - NFemale;
                this.MeanAge = this.Ages.MeanAge();
            }
            return Result;
        }

        // Property methods ..............................................
        /// <summary>
        /// Set the genotype
        /// </summary>
        /// <param name="value"></param>
        protected void setGenotype(AnimalParamSet value)
        {
            this.AParams = new AnimalParamSet(null, value);
        }

        /// <summary>
        /// Get the total number of females and males
        /// </summary>
        /// <returns>Total number of animals</returns>
        protected int GetNoAnimals()
        {
            return this.NoMales + this.NoFemales;
        }

        /// <summary>
        /// Set the number of animals
        /// </summary>
        /// <param name="count">Number of animals</param>
        protected void SetNoAnimals(int count)
        {
            if (this.Mothers != null)
            {
                this.NoMales = count / 2;
                this.NoFemales = count - this.NoMales;
            }
            else if ((this.ReproStatus == GrazType.ReproType.Male) || (this.ReproStatus == GrazType.ReproType.Castrated))
            {
                this.NoMales = count;
                this.NoFemales = 0;
            }
            else
            {
                this.NoMales = 0;
                this.NoFemales = count;
            }

            if (this.Ages.Count == 0)
                Ages.Input(this.MeanAge, this.NoMales, this.NoFemales);
            else
                Ages.Resize(this.NoMales, this.NoFemales);
        }

        /// <summary>
        /// Set the live weight
        /// </summary>
        /// <param name="liveWeight">Live weight</param>
        protected void SetLiveWt(double liveWeight)
        {
            BaseWeight = liveWeight - ConceptusWt() - WoolWt;
            TotalWeight = liveWeight;
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
        /// <param name="woolWeight"></param>
        protected void SetWoolWt(double woolWeight)
        {
            WoolWt = woolWeight;
            BaseWeight = TotalWeight - ConceptusWt() - WoolWt;
            Calc_Weights();
        }

        /// <summary>
        /// Set the maximum previous weight
        /// </summary>
        /// <param name="maxPrevWeight"></param>
        protected void SetMaxPrevWt(double maxPrevWeight)
        {
            MaxPrevWt = maxPrevWeight;
            Calc_Weights();
        }

        /// <summary>
        /// In sheep, the coat depth is used to set the total wool weight 
        /// </summary>
        /// <param name="newCoatDepth">New coat depth (cm)</param>
        protected void SetCoatDepth(double newCoatDepth)
        {
            FCoatDepth = newCoatDepth;
            SetWoolWt(CoatDepth2Wool(newCoatDepth));
        }

        /// <summary>
        /// Set the animal to be mated to
        /// </summary>
        /// <param name="value"></param>
        protected void setMatedTo(AnimalParamSet value)
        {
            FMatedTo = null;
            if (value == null)
                FMatedTo = null;
            else
                FMatedTo = new AnimalParamSet(value);
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
                    FoetalAge = 0;                                                  // this is usually used at birth, where   
                    FoetalWt = 0.0;                                                 // the conceptus is lost                  
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
                    {                                                                      // of the foetus and implicitly the       
                        ConditionFactor = (Condition - 1.0)                                // conceptus while keeping the live       
                                           * FoetalNormWt() / AParams.StdBirthWt(NoFoetuses);   // weight constant                        
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
        /// <param name="ageDays"></param>
        /// <param name="reprdType"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static double GrowthCurve(int ageDays, GrazType.ReproType reprdType, AnimalParamSet parameters)
        {
            double SRW;

            SRW = parameters.BreedSRW;
            if ((reprdType == GrazType.ReproType.Male) || (reprdType == GrazType.ReproType.Castrated))
                SRW = SRW * parameters.SRWScalars[(int)reprdType];                                           // TODO: check indexing here
            return MaxNormWtFunc(SRW, parameters.StdBirthWt(1), ageDays, parameters);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="L"></param>
        protected void SetLactation(int L)
        {
            // AnimalGroup MyClass;

            if (L != DaysLactating)
            {
                if (L == 0)
                {
                    LactStatus = GrazType.LactType.Dry;                                     // Set this before calling setDryoffTime()  
                    if (Young == null)
                        SetDryoffTime(DaysLactating, 0, FPrevOffspring);
                    else                                                                    // This happens when self-weaning occurs    
                    {
                        SetDryoffTime(DaysLactating, 0, NoOffspring);
                        Young.LactStatus = GrazType.LactType.Dry;
                    }
                    DaysLactating = 0;                                                      // ConditionAtBirthing, PropnOfMaxMilk and  
                }                                                                           // LactAdjust are left at their final values
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
                    Young = new AnimalGroup(this, 0.5 * (GrowthCurve(L, GrazType.ReproType.Male, AParams)
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
        /// <param name="value"></param>
        protected void SetNoFoetuses(int value)
        {
            int iDaysPreg;

            if (value == 0)
            {
                Pregnancy = 0;
                FNoFoetuses = 0;
            }
            else if ((value <= AParams.MaxYoung) && (value != NoFoetuses))
            {
                iDaysPreg = Pregnancy;
                Pregnancy = 0;
                FNoFoetuses = value;
                Pregnancy = iDaysPreg;
            }
        }

        /// <summary>
        ///  On creation, lambs and calves are always suckling their mothers. This may 
        /// change in the course of a simulation (see the YoungStopSuckling function) 
        /// </summary>
        /// <param name="value"></param>
        protected void SetNoOffspring(int value)
        {
            int iDaysLact;

            if (value != NoOffspring)
            {
                iDaysLact = Lactation;                                                 // Store the current stage of lactation     
                Lactation = 0;
                if (Young != null)
                {
                    Young = null;
                    Young = null;
                }

                FNoOffspring = value;

                if (value == 0)
                    Young = null;
                else if (value <= AParams.MaxYoung)
                    SetLactation(iDaysLact);                                            // This creates a new group of (suckling) lambs or calves  
            }                                                                                                   
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

            Result = MultiplyDMPool(this.AnimalState.OrgFaeces, NoAnimals);
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

            Result = MultiplyDMPool(this.AnimalState.InOrgFaeces, NoAnimals);
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
            GrazType.DM_Pool Result = MultiplyDMPool(this.AnimalState.Urine, NoAnimals);
            if (Young != null)
                Result = AddDMPool(Result, Young.Urine);
            return Result;
        }

        /// <summary>
        /// Get excretion parameters
        /// </summary>
        /// <returns></returns>
        protected ExcretionInfo getExcretion()
        {
            // these will have to go into the parameter set eventually...
            double[] faecesDensity = { 1000.0, 1000.0 };       // kg/m^3
            // double[] dFaecesMoisture = { 4.0, 5.0 };         // kg water/kg DM
            double[] refNormalWt = { 50.0, 600.0 };            // kg
            double[] faecesRefLength = { 0.012, 0.30 };        // m
            double[] faecesPower = { 0.00, 1.0 / 3.0 };
            double[] faecesWidthToLength = { 0.80, 1.00 };
            double[] faecesHeightToLength = { 0.70, 0.12 };
            double[] faecalMoistureHerbageMin = { 6.0, 7.5 };  // kg water/kg DM
            double[] faecalMoistureSuppMin = { 3.0, 3.0 };
            double[] faecalMoistureMax = { 0.0, 0.0 };
            double[] faecesNO3Propn = { 0.25, 0.25 };
            double[] urineRefLength = { 0.20, 0.60 };          // m
            double[] urineWidthToLength = { 1.00, 1.00 };
            double[] urineRefVolume = { 0.00015, 0.00200 };    // m^3
            double[] dailyUrineRefVol = { 0.0030, 0.0250 };    // m^3/head/d

            double faecalLongAxis;         // metres
            double faecalHeight;           // metres
            double faecalMoistureHerbage;
            double faecalMoistureSupp;
            double faecalFreshWeight;      // kg/head
            double urineLongAxis;          // metres
            double volumePerUrination;     // m^3
            double dailyUrineVolume;       // m^3
            FoodSupplement tempSuppt;

            ExcretionInfo result = new ExcretionInfo();

            result.OrgFaeces = MultiplyDMPool(this.AnimalState.OrgFaeces, NoAnimals);
            result.InOrgFaeces = MultiplyDMPool(this.AnimalState.InOrgFaeces, NoAnimals);
            result.Urine = MultiplyDMPool(this.AnimalState.Urine, NoAnimals);

            // In sheep, we treat each faecal pellet as a separate defaecation.
            // Sheep pellets are assumed to have constant size; cattle pats vary with
            // linear dimension of the animal

            faecalLongAxis = faecesRefLength[(int)Animal] * Math.Pow(NormalWt / refNormalWt[(int)Animal], faecesPower[(int)Animal]);
            faecalHeight = faecalLongAxis * faecesHeightToLength[(int)Animal];

            // Faecal moisture content seems to be lower when animals are not at pasture,
            // so estimate it separately for herbage and supplement components of the diet
            tempSuppt = new FoodSupplement();
            TheRation.AverageSuppt(out tempSuppt);
            faecalMoistureHerbage = faecalMoistureHerbageMin[(int)Animal] + (faecalMoistureMax[(int)Animal] - faecalMoistureHerbageMin[(int)Animal]) * this.AnimalState.Digestibility.Herbage;
            faecalMoistureSupp = faecalMoistureSuppMin[(int)Animal] + (faecalMoistureMax[(int)Animal] - faecalMoistureSuppMin[(int)Animal]) * (1.0 - tempSuppt.DMPropn);
            faecalFreshWeight = this.AnimalState.DM_Intake.Herbage * (1.0 - this.AnimalState.Digestibility.Herbage) * (1.0 + faecalMoistureHerbage)
                                      + this.AnimalState.DM_Intake.Supp * (1.0 - this.AnimalState.Digestibility.Supp) * (1.0 + faecalMoistureSupp);
            tempSuppt = null;

            // Defaecations are assumed to be ellipsoidal prisms:
            result.DefaecationEccentricity = Math.Sqrt(1.0 - StdMath.Sqr(faecesWidthToLength[(int)AParams.Animal]));
            result.DefaecationArea = Math.PI / 4.0 * StdMath.Sqr(faecalLongAxis) * faecesWidthToLength[(int)AParams.Animal];
            result.DefaecationVolume = result.DefaecationArea * faecalHeight;
            result.Defaecations = NoAnimals * (faecalFreshWeight / faecesDensity[(int)AParams.Animal]) / result.DefaecationVolume;
            result.FaecalNO3Propn = faecesNO3Propn[(int)AParams.Animal];

            urineLongAxis = urineRefLength[(int)Animal] * Math.Pow(NormalWt / refNormalWt[(int)Animal], 1.0 / 3.0);
            volumePerUrination = urineRefVolume[(int)Animal] * Math.Pow(NormalWt / refNormalWt[(int)Animal], 1.0);
            dailyUrineVolume = dailyUrineRefVol[(int)Animal] * Math.Pow(NormalWt / refNormalWt[(int)Animal], 1.0);

            // Urinations are assumed to be ellipsoidal
            result.dUrinationEccentricity = Math.Sqrt(1.0 - StdMath.Sqr(urineWidthToLength[(int)AParams.Animal]));
            result.UrinationArea = Math.PI / 4.0 * StdMath.Sqr(urineLongAxis) * urineWidthToLength[(int)AParams.Animal];
            result.UrinationVolume = volumePerUrination;
            result.Urinations = NoAnimals * dailyUrineVolume / result.UrinationVolume;

            return result;
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
            return AParams.Name;
        }

        //TODO: Test this function
        /// <summary>
        /// Get the age class 
        /// </summary>
        /// <returns></returns>
        protected GrazType.AgeType GetAgeClass()
        {
            // Array[AnimalType,0..3] of AgeType
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

            MEIPerHead = this.AnimalState.ME_Intake.Solid;
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
            return this.AParams.MethC[1] * this.AnimalState.DM_Intake.Solid
                      * (this.AParams.MethC[2] + this.AParams.MethC[3] * this.AnimalState.ME_2_DM.Solid
                          + (FeedingLevel + 1.0) * (this.AParams.MethC[4] - this.AParams.MethC[5] * this.AnimalState.ME_2_DM.Solid));
        }

        /// <summary>
        /// Get the methane weight
        /// </summary>
        /// <returns></returns>
        protected double GetMethaneWeight()
        {
            return AParams.MethC[6] * MethaneEnergy;
        }
        
        /// <summary>
        /// Get the methane volume
        /// </summary>
        /// <returns></returns>
        protected double GetMethaneVolume()
        {
            return AParams.MethC[7] * MethaneEnergy;
        }

        /// <summary>
        /// ptr to the hosts random number factory
        /// </summary>
        public MyRandom RandFactory;
        
        /// <summary>
        /// Pointers to the young of lactating animals, or the mothers of suckling ones
        /// </summary>
        public AnimalGroup Young;
        
        /// <summary>
        /// Animal output
        /// </summary>
        public AnimalOutput AnimalState = new AnimalOutput();

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
        public AnimalGroup(AnimalParamSet Params,
                                 GrazType.ReproType Repro,
                                 int Number,
                                 int AgeD,
                                 double LiveWt,
                                 double GFW,                   // NB this is a *fleece* weight             
                                 MyRandom RandomFactory,
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
        public void Construct(AnimalParamSet Params,
                                 GrazType.ReproType Repro,
                                 int Number,
                                 int AgeD,
                                 double LiveWt,
                                 double GFW,                   // NB this is a *fleece* weight             
                                 MyRandom RandomFactory,
                                 bool bTakeParams = false)
        {
            double fWoolAgeFactor;

            RandFactory = RandomFactory;

            if (bTakeParams)
                AParams = Params;
            else
                AParams = new AnimalParamSet(null, Params);

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
            Ages = new AgeList(RandFactory);
            Ages.Input(AgeD, NoMales, NoFemales);

            TheRation = new SupplementRation();
            FIntakeSupp = new FoodSupplement();

            MateCycle = -1;                                                          // Not recently mated                       

            LiveWeight = LiveWt;
            BirthWt = Math.Min(AParams.StdBirthWt(1), BaseWeight);
            Calc_Weights();

            if (Animal == GrazType.AnimalType.Sheep)
            {
                WoolMicron = AParams.MaxFleeceDiam;                                   // Calculation of FleeceCutWeight depends   
                FleeceCutWeight = GFW;                                                // on the values of NormalWt & WoolMicron 

                fWoolAgeFactor = AParams.WoolC[5] + (1.0 - AParams.WoolC[5]) * (1.0 - Math.Exp(-AParams.WoolC[12] * AgeDays));
                DeltaWoolWt = AParams.FleeceRatio * StdRefWt * fWoolAgeFactor / 365.0;
            }

            Calc_CoatDepth();
            TotalWeight = BaseWeight + WoolWt;

            if (AgeClass == GrazType.AgeType.Mature)                                  // This will re-calculate size and condition
                SetMaxPrevWt(Math.Max(StdRefWt, BaseWeight));
            else
                SetMaxPrevWt(BaseWeight);

            ConditionAtBirthing = Condition;                                         // These terms affect the calculation of  
            PropnOfMaxMilk = 1.0;                                                    // potential intake                     
            LactAdjust = 1.0;

            BasePhos = BaseWeight * AParams.PhosC[9];
            BaseSulf = BaseWeight * AParams.GainC[12] / GrazType.N2Protein * AParams.SulfC[1];

        }

        /// <summary>
        /// CreateYoung
        /// </summary>
        /// <param name="Parents"></param>
        /// <param name="LiveWt"></param>
        public AnimalGroup(AnimalGroup Parents, double LiveWt)
        {
            int Number, iAgeDays;
            double YoungWoolWt;
            AnimalParamSet youngParams;

            RandFactory = Parents.RandFactory;
            youngParams = Parents.constructOffspringParams();
            Number = Parents.NoOffspring * Parents.FemaleNo;
            iAgeDays = Parents.DaysLactating;
            YoungWoolWt = 0.5 * (AnimalParamSet.fDefaultFleece(Parents.AParams, iAgeDays, GrazType.ReproType.Male, iAgeDays)
                                  + AnimalParamSet.fDefaultFleece(Parents.AParams, iAgeDays, GrazType.ReproType.Empty, iAgeDays));

            Construct(youngParams, GrazType.ReproType.Male, Number, iAgeDays, LiveWt, YoungWoolWt, RandFactory, true);

            NoMales = Number / 2;
            NoFemales = Number - NoMales;

            Ages = null;
            Ages = new AgeList(RandFactory);
            Ages.Input(iAgeDays, NoMales, NoFemales);

            LactStatus = GrazType.LactType.Suckling;
            FNoOffspring = Parents.NoOffspring;
            Mothers = Parents;

            ComputeSRW();                                                              // Must do this after assigning a value to Mothers  
            Calc_Weights();                                                                                            
        }

        /// <summary>
        /// Copy a AnimalGroup
        /// </summary>
        /// <returns></returns>
        public AnimalGroup Copy()
        {
            AnimalGroup theCopy = ObjectCopier.Clone(this);
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
        }                                                                          // goes "into place"                      

        /// <summary>
        /// Merge two animal groups
        /// </summary>
        /// <param name="otherGrp"></param>
        public void Merge(ref AnimalGroup otherGrp)
        {
            double fWoodFactor;
            double fWoodOther;
            int Total1;
            int Total2;


            if ((NoFoetuses != otherGrp.NoFoetuses)                                   // Necessary conditions for merging         
               || (NoOffspring != otherGrp.NoOffspring)
               || ((Mothers == null) && (ReproStatus != otherGrp.ReproStatus))
               || (LactStatus != otherGrp.LactStatus))
                throw new Exception("AnimalGroup: Error in Merge method");

            Total1 = NoAnimals;
            Total2 = otherGrp.NoAnimals;

            NoMales += otherGrp.NoMales;                                       // Take weighted averages of all            
            NoFemales += otherGrp.NoFemales;                                     // appropriate fields                       
            Ages.Merge(otherGrp.Ages);
            MeanAge = Ages.MeanAge();

            AverageField(Total1, Total2, ref TotalWeight, otherGrp.TotalWeight);
            AverageField(Total1, Total2, ref WoolWt, otherGrp.WoolWt);
            AverageField(Total1, Total2, ref DeltaWoolWt, otherGrp.DeltaWoolWt);
            AverageField(Total1, Total2, ref WoolMicron, otherGrp.WoolMicron);
            AverageField(Total1, Total2, ref FCoatDepth, otherGrp.FCoatDepth);
            AverageField(Total1, Total2, ref BasalWeight, otherGrp.BasalWeight);
            AverageField(Total1, Total2, ref DeltaBaseWeight, otherGrp.DeltaBaseWeight);
            AverageField(Total1, Total2, ref MaxPrevWt, otherGrp.MaxPrevWt);
            AverageField(Total1, Total2, ref BirthWt, otherGrp.BirthWt);
            AverageField(Total1, Total2, ref StdRefWt, otherGrp.StdRefWt);
            AverageField(Total1, Total2, ref IntakeLimit, otherGrp.IntakeLimit);
            Calc_Weights();

            if ((ReproStatus == GrazType.ReproType.EarlyPreg) || (ReproStatus == GrazType.ReproType.LatePreg))
            {
                FoetalAge = (FoetalAge * Total1 + otherGrp.FoetalAge * Total2)
                             / (Total1 + Total2);
                AverageField(Total1, Total2, ref FoetalWt, otherGrp.FoetalWt);
                AverageField(Total1, Total2, ref MidLatePregWt, otherGrp.MidLatePregWt);
            }

            if (LactStatus == GrazType.LactType.Lactating)
            {
                DaysLactating = (DaysLactating * Total1 + otherGrp.DaysLactating * Total2)
                                 / (Total1 + Total2);
                AverageField(Total1, Total2, ref Milk_MJProdn, otherGrp.Milk_MJProdn);
                AverageField(Total1, Total2, ref Milk_ProtProdn, otherGrp.Milk_ProtProdn);
                AverageField(Total1, Total2, ref Milk_Weight, otherGrp.Milk_Weight);
                AverageField(Total1, Total2, ref LactRatio, otherGrp.LactRatio);
            }
            else if ((FPrevOffspring == 0) && (otherGrp.FPrevOffspring == 0))
            {
                FPrevOffspring = 0;
                DryOffTime = 0;
                ConditionAtBirthing = 0.0;
                otherGrp.ConditionAtBirthing = 0.0;
            }
            else
            {
                if ((FPrevOffspring == 0)
                   || ((otherGrp.FPrevOffspring > 0) && (otherGrp.NoFemales > NoFemales)))
                    FPrevOffspring = otherGrp.FPrevOffspring;

                fWoodFactor = WOOD(DryOffTime, AParams.IntakeC[8], AParams.IntakeC[9]);
                fWoodOther = WOOD(otherGrp.DryOffTime, AParams.IntakeC[8], AParams.IntakeC[9]);
                AverageField(Total1, Total2, ref fWoodFactor, fWoodOther);
                DryOffTime = InverseWOOD(fWoodFactor, AParams.IntakeC[8], AParams.IntakeC[9], true);

                if (ConditionAtBirthing == 0.0)
                    ConditionAtBirthing = 1.0;
                if (otherGrp.ConditionAtBirthing == 0.0)
                    otherGrp.ConditionAtBirthing = 1.0;
            }
            AverageField(Total1, Total2, ref ConditionAtBirthing, otherGrp.ConditionAtBirthing);
            AverageField(Total1, Total2, ref PropnOfMaxMilk, otherGrp.PropnOfMaxMilk);
            AverageField(Total1, Total2, ref LactAdjust, otherGrp.LactAdjust);

            if (Young != null)
                Young.Merge(ref otherGrp.Young);
            otherGrp = null;
        }

        /// <summary>
        /// Split the animal group
        /// </summary>
        /// <param name="Number"></param>
        /// <param name="ByAge"></param>
        /// <param name="Diffs"></param>
        /// <param name="YngDiffs"></param>
        /// <returns>Animal group</returns>
        public AnimalGroup Split(int Number, bool ByAge, DifferenceRecord Diffs, DifferenceRecord YngDiffs)
        {
            AnimalGroup Result;
            int SplitM, SplitF;
            string msg = string.Empty;

            if ((Number < 0) || (Number > NoAnimals))
            {
                if (Number < 0)
                    msg = "Number of animals to split off should be > 0";
                if (Number > NoAnimals)
                    msg = "Trying to split off more than " + NoAnimals.ToString() + " animals that exist in the " + GrazType.AgeText[(int)this.AgeClass] + " age class";
                throw new Exception("AnimalGroup: Error in Split method: " + msg);
            }

            if (Mothers != null)
            {
                SplitM = Convert.ToInt32(Math.Round(StdMath.XDiv(Number * 1.0 * NoMales, NoAnimals)), CultureInfo.InvariantCulture);
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
        /// <param name="numMale"></param>
        /// <param name="numFemale"></param>
        /// <returns></returns>
        private double SexAve(double MaleScale, int numMale, int numFemale)
        {
            return StdMath.XDiv(MaleScale * numMale + numFemale, numMale + numFemale);
        }

        /// <summary>
        /// Split the numbers off the group
        /// </summary>
        /// <param name="NewGroups"></param>
        /// <param name="NF"></param>
        /// <param name="NYM"></param>
        /// <param name="NYF"></param>
        private void SplitNumbers(ref AnimalList NewGroups, int NF, int NYM, int NYF)
        {
            AnimalGroup TempYoung;
            AnimalGroup SplitGroup;
            AnimalGroup SplitYoung;
            double DiffRatio;
            DifferenceRecord YngDiffs;

            YngDiffs = new DifferenceRecord() { StdRefWt = this.NODIFF.StdRefWt, BaseWeight = this.NODIFF.BaseWeight, FleeceWt = this.NODIFF.FleeceWt };
            //// WITH Young DO
            if ((this.Young.MaleNo > 0) && (this.Young.FemaleNo > 0))
            {
                DiffRatio = (this.SexAve(this.AParams.SRWScalars[(int)this.ReproStatus], NYM, NYF)
                              - this.SexAve(this.AParams.SRWScalars[(int)this.ReproStatus], this.Young.NoMales - NYM, this.Young.NoFemales - NYF))
                            / this.SexAve(this.AParams.SRWScalars[(int)this.ReproStatus], this.Young.NoMales, this.Young.NoFemales);
                YngDiffs.StdRefWt = this.StdRefWt * DiffRatio;
                YngDiffs.BaseWeight = this.BaseWeight * DiffRatio;
                YngDiffs.FleeceWt = this.WoolWt * DiffRatio;
            }

            TempYoung = this.Young;
            this.Young = null;
            SplitGroup = this.SplitSex(0, NF, false, this.NODIFF);
            SplitYoung = TempYoung.SplitSex(NYM, NYF, false, YngDiffs);
            this.Young = TempYoung;
            this.Young.Mothers = this;
            SplitGroup.Young = SplitYoung;
            SplitGroup.Young.Mothers = SplitGroup;
            this.CheckAnimList(ref NewGroups);
            NewGroups.Add(SplitGroup);
        }

        /// <summary>
        /// Split young
        /// </summary>
        /// <param name="newGroups">New animal groups</param>
        public void SplitYoung(ref AnimalList newGroups)
        {
            int numToSplit;

            if (this.Young != null)
            {
                if (NoOffspring == 1)
                {
                    numToSplit = this.Young.FemaleNo;
                    this.SplitNumbers(ref newGroups, numToSplit, 0, numToSplit);
                }
                else if (this.NoOffspring == 2)
                {
                    numToSplit = Convert.ToInt32(Math.Min(this.Young.MaleNo, this.Young.FemaleNo), CultureInfo.InvariantCulture) / 2;   // One male, one female                     
                    if (((this.Young.FemaleNo - numToSplit) % 2) != 0)  //if odd                                                        // Ensures Young.FemaleNo (and hence        
                        numToSplit++;                                                                                                   // Young.MaleNo) is even after the call   
                    this.SplitNumbers(ref newGroups, numToSplit, numToSplit, numToSplit);                                               // to SplitBySex                          
                    numToSplit = this.Young.FemaleNo / 2;                                                                               // Twin females                             
                    this.SplitNumbers(ref newGroups, numToSplit, 0, 2 * numToSplit);
                }
            }
        }

        /// <summary>
        /// Is an animal group similar enough to another for them to be merged?       
        /// </summary>
        /// <param name="animalGrp">An animal group</param>
        /// <returns></returns>
        public bool Similar(AnimalGroup animalGrp)
        {
            bool Result = ((Genotype.Name == animalGrp.Genotype.Name)
                  && (ReproStatus == animalGrp.ReproStatus)
                  && (NoFoetuses == animalGrp.NoFoetuses)
                  && (NoOffspring == animalGrp.NoOffspring)
                  && (MateCycle == animalGrp.MateCycle)
                  && (DaysToMate == animalGrp.DaysToMate)
                  && (Pregnancy == animalGrp.Pregnancy)
                  && (LactStatus == animalGrp.LactStatus)
                  && (Math.Abs(Lactation - animalGrp.Lactation) < 7)
                  && ((Young == null) == (animalGrp.Young == null))
                  && (ImplantEffect == animalGrp.ImplantEffect));
            if (MeanAge < 365)
                Result = (Result && (MeanAge == animalGrp.MeanAge));
            else
                Result = (Result && (Math.Min(MeanAge / 30, 37) == Math.Min(animalGrp.MeanAge / 30, 37)));
            if (Young != null)
                Result = (Result && (Young.ReproStatus == animalGrp.Young.ReproStatus));

            return Result;
        }

        // Initialisation properties .....................................
        /// <summary>
        /// The animals genotype
        /// </summary>
        public AnimalParamSet Genotype
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
        /// Gets or sets the number of males
        /// </summary>
        public int MaleNo
        {
            get { return NoMales; }
            set { NoMales = value; }
        }

        /// <summary>
        /// Gets or sets the number of females
        /// </summary>
        public int FemaleNo
        {
            get { return NoFemales; }
            set { NoFemales = value; }
        }

        /// <summary>
        /// Gets or sets the mean age of the group
        /// </summary>
        public int AgeDays
        {
            get { return MeanAge; }
            set { MeanAge = value; }
        }

        /// <summary>
        /// Gets or sets the live weight of the group
        /// </summary>
        public double LiveWeight
        {
            get { return TotalWeight; }
            set { SetLiveWt(value); }
        }

        /// <summary>
        /// Gets or sets the animal base weight
        /// </summary>
        public double BaseWeight
        {
            get { return BasalWeight; }
            set { BasalWeight = value; }
        }

        /// <summary>
        /// Gets or sets the fleece-free, conceptus-free weight, but including the wool stubble        
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
        /// Gets or sets the cut weight of fleece
        /// </summary>
        public double FleeceCutWeight
        {
            get { return GetFleeceCutWt(); }
            set { SetFleeceCutWt(value); }
        }

        /// <summary>
        /// Gets or sets the wool weight
        /// </summary>
        public double WoolWeight
        {
            get { return WoolWt; }
            set { SetWoolWt(value); }
        }

        /// <summary>
        /// Gets or sets the depth of coat
        /// </summary>
        public double CoatDepth
        {
            get { return FCoatDepth; }
            set { SetCoatDepth(value); }
        }
        
        /// <summary>
        /// Gets or sets the maximum previous weight
        /// </summary>
        public double MaxPrevWeight
        {
            get { return MaxPrevWt; }
            set { SetMaxPrevWt(value); }
        }
        
        /// <summary>
        /// Gets or sets the wool fibre diameter
        /// </summary>
        public double FibreDiam
        {
            get { return WoolMicron; }
            set { WoolMicron = value; }
        }
        
        /// <summary>
        /// Gets or sets the animal parameters for the animal mated to
        /// </summary>
        public AnimalParamSet MatedTo
        {
            get { return FMatedTo; }
            set { setMatedTo(value); }
        }
        
        /// <summary>
        /// Gets or sets the stage of pregnancy
        /// </summary>
        public int Pregnancy
        {
            get { return FoetalAge; }
            set { SetPregnancy(value); }
        }
        
        /// <summary>
        /// Gets or sets the days lactating
        /// </summary>
        public int Lactation
        {
            get { return DaysLactating; }
            set { SetLactation(value); }
        }
        
        /// <summary>
        /// Gets or sets the number of foetuses
        /// </summary>
        public int NoFoetuses
        {
            get { return FNoFoetuses; }
            set { SetNoFoetuses(value); }
        }
        
        /// <summary>
        /// Gets or sets the number of offspring
        /// </summary>
        public int NoOffspring
        {
            get { return FNoOffspring; }
            set { SetNoOffspring(value); }
        }
        
        /// <summary>
        /// Gets or sets the condition at birth
        /// </summary>
        public double BirthCondition
        {
            get { return ConditionAtBirthing; }
            set { ConditionAtBirthing = value; }
        }

        /// <summary>
        /// Gets or sets the daily deaths
        /// </summary>
        public int Deaths
        {
            get { return FDeaths; }
            set { FDeaths = value; }
        }

        /// <summary>
        /// Condition score
        /// </summary>
        /// <param name="System"></param>
        /// <returns></returns>
        public double fConditionScore(AnimalParamSet.Cond_System System) { return AnimalParamSet.Condition2CondScore(Condition, System); }
        
        /// <summary>
        /// Set the condition score
        /// </summary>
        /// <param name="fValue"></param>
        /// <param name="System"></param>
        public void setConditionScore(double fValue, AnimalParamSet.Cond_System System)
        {
            BaseWeight = NormalWt * AnimalParamSet.CondScore2Condition(fValue, System);
            Calc_Weights();
        }

        /// <summary>
        /// Sets the value of MaxPrevWeight using current base weight, age and a      
        /// (relative) body condition. Intended for use with young animals.           
        /// </summary>
        /// <param name="bodyCond"></param>
        public void setConditionAtWeight(double bodyCond)
        {
            double fMaxNormWt;
            double fNewMaxPrevWt;

            fMaxNormWt = MaxNormWtFunc(StdRefWt, BirthWt, MeanAge, AParams);
            if (BaseWeight >= fMaxNormWt)
                fNewMaxPrevWt = BaseWeight;
            else
            {
                fNewMaxPrevWt = (BaseWeight - bodyCond * AParams.GrowthC[3] * fMaxNormWt)
                                 / (bodyCond * (1.0 - AParams.GrowthC[3]));
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
        /// <param name="Y"></param>
        /// <param name="Tmax"></param>
        /// <param name="B"></param>
        /// <param name="declining"></param>
        public double InverseWOOD(double Y, double Tmax, double B, bool declining)
        {
            double x0, x1;
            double result;

            if (Y <= 0.0)
                result = 0.0;
            else if (Y >= 1.0)
                result = Tmax;
            else
            {
                if (!declining)                                                   // Initial guess                            
                    x1 = Math.Min(0.99, Y);
                else
                    x1 = Math.Max(1.01, Math.Exp(B * (1.0 - Y)));

                bool more = true;
                do                                                                 // Newton-Raphson solution                   
                {
                    x0 = x1;
                    x1 = x0 - (1.0 - Y / WOOD(x0, 1.0, B)) * x0 / (B * (1.0 - x0));
                    more = !(Math.Abs(x0 - x1) < 1.0E-5);
                }
                while (more);
                result = (x0 + x1) / 2.0 * Tmax;
            }
            return result;
        }

        /// <summary>
        /// Set the drying off time
        /// </summary>
        /// <param name="daysSinceBirth">Days since birth</param>
        /// <param name="daysSinceDryoff">Days since drying off</param>
        /// <param name="prevSuckling"></param>
        public void SetDryoffTime(int daysSinceBirth, int daysSinceDryoff, int prevSuckling = 1)
        {
            double lactLength;
            double woodFunc;

            FPrevOffspring = prevSuckling;

            lactLength = daysSinceBirth - daysSinceDryoff;
            if ((LactStatus != GrazType.LactType.Dry) || (lactLength <= 0))
                DryOffTime = 0.0;
            else if (lactLength >= AParams.IntakeC[8])
                DryOffTime = lactLength + this.AParams.IntakeC[19] * daysSinceDryoff;
            else
            {
                woodFunc = WOOD(lactLength, this.AParams.IntakeC[8], this.AParams.IntakeC[9]);
                lactLength = InverseWOOD(woodFunc, AParams.IntakeC[8], this.AParams.IntakeC[9], true);
                DryOffTime = lactLength + this.AParams.IntakeC[19] * daysSinceDryoff;
            }
        }

        // Inputs for model dynamics .....................................
        /// <summary>
        /// Gets or sets the steepness of the paddock
        /// </summary>
        public double PaddSteep
        {
            get { return Steepness; }
            set { Steepness = value; }
        }
        
        /// <summary>
        /// Gets or sets the animals environment
        /// </summary>
        public AnimalWeather Weather
        {
            get { return TheEnv; }
            set { TheEnv = value; }
        }
        
        /// <summary>
        /// Gets or sets the herbage being eaten
        /// </summary>
        public GrazType.GrazingInputs Herbage
        {
            get { return Inputs; }
            set { Inputs = value; }
        }
        
        /// <summary>
        /// Gets or sets the water logging value
        /// </summary>
        public double WaterLogging
        {
            get { return WaterLog; }
            set { WaterLog = value; }
        }

        /// <summary>
        /// Gets the supplement ration used
        /// </summary>
        public SupplementRation RationFed
        {
            get { return TheRation; }
        }
        
        /// <summary>
        /// Gets or sets the number of animals per hectare
        /// </summary>
        public double AnimalsPerHa
        {
            get { return FAnimalsPerHa; }
            set { FAnimalsPerHa = value; }
        }
        
        /// <summary>
        /// Gets or sets the distance walked
        /// </summary>
        public double DistanceWalked
        {
            get { return FDistanceWalked; }
            set { FDistanceWalked = value; }
        }
        
        /// <summary>
        /// Gets or sets the intake modifier value
        /// </summary>
        public double IntakeModifier
        {
            get { return FIntakeModifier; }
            set { FIntakeModifier = value; }
        }

        /// <summary>
        /// Used in GrazFeed to initialise the state variables for which yesterday's  
        /// value must be known in order to get today's calculation                   
        /// </summary>
        /// <param name="prevGroup"></param>
        public void SetUpForYesterday(AnimalGroup prevGroup)
        {
            IntakeLimit = prevGroup.IntakeLimit;
            FeedingLevel = prevGroup.FeedingLevel;
            Milk_MJProdn = prevGroup.Milk_MJProdn;
            Milk_ProtProdn = prevGroup.Milk_ProtProdn;
            PropnOfMaxMilk = prevGroup.PropnOfMaxMilk;
            this.AnimalState.LowerCritTemp = prevGroup.AnimalState.LowerCritTemp;
            if ((Young != null) && (prevGroup.Young != null))
                Young.SetUpForYesterday(prevGroup.Young);
        }

        // Daily simulation logic ........................................
        /// <summary>
        /// Advance the age of the animals
        /// </summary>
        /// <param name="amimalGrp"></param>
        /// <param name="numDays"></param>
        /// <param name="newGroups"></param>
        private void AdvanceAge(AnimalGroup amimalGrp, int numDays, ref AnimalList newGroups)
        {
            amimalGrp.MeanAge += numDays;
            amimalGrp.Ages.AgeBy(numDays);
            if (amimalGrp.Young != null)
                amimalGrp.Young.Age(numDays, ref newGroups);
            if (amimalGrp.LactStatus == GrazType.LactType.Lactating)
                amimalGrp.DaysLactating += numDays;
            else if (amimalGrp.DryOffTime > 0.0)
                amimalGrp.DryOffTime = amimalGrp.DryOffTime + AParams.IntakeC[19] * numDays;
        }

        /// <summary>
        /// Age the animals
        /// </summary>
        /// <param name="numDays"></param>
        /// <param name="newGroups"></param>
        public void Age(int numDays, ref AnimalList newGroups)
        {
            int newOffset, i;

            if (ChillIndex == StdMath.DMISSING)
                ChillIndex = ChillFunc(TheEnv.MeanTemp, TheEnv.WindSpeed, TheEnv.Precipitation);
            else
                ChillIndex = 16.0 / 17.0 * ChillIndex + 1.0 / 17.0 * ChillFunc(TheEnv.MeanTemp, TheEnv.WindSpeed, TheEnv.Precipitation);

            if (newGroups != null)
                newOffset = newGroups.Count;
            else
                newOffset = 0;
            if (Mothers == null)                                                    // Deaths must be done before age is        
                Kill(ChillIndex, ref newGroups);                                          //   incremented                           

            if (YoungStopSuckling())
                Lactation = 0;

            AdvanceAge(this, numDays, ref newGroups);
            if (newGroups != null)
                for (i = newOffset; i <= newGroups.Count - 1; i++)
                    AdvanceAge(newGroups.At(i), numDays, ref newGroups);

            switch (ReproStatus)
            {
                case GrazType.ReproType.Empty:
                    if (MateCycle >= 0)
                    {
                        DaysToMate--;
                        if (DaysToMate <= 0)
                            MateCycle = -1;
                        else
                            MateCycle = (MateCycle + 1) % AParams.OvulationPeriod;
                        if (MateCycle == 0)
                            Conceive(ref newGroups);
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
                            KillEndPreg(ref newGroups);
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
        /// Make the animal grow
        /// </summary>
        public void Grow()
        {
            double RDPScalar;
            AnimalStateInfo initState = new AnimalStateInfo();

            storeStateInfo(ref initState);
            Calc_IntakeLimit();
            Grazing(1.0, true, false);
            Nutrition();
            RDPScalar = RDP_IntakeFactor();
            if (RDPScalar < 1.0)
            {
                revertStateInfo(initState);
                IntakeLimit = IntakeLimit * RDPScalar;
                Grazing(1.0, true, false);                                                  // This call resets AnimalState, hence the  
                Nutrition();                                                                // separate variable for the RDP factor   
            }
            completeGrowth(RDPScalar);
        }

        /// <summary>
        /// Routine to compute the potential intake of a group of animals.  The       
        /// result is stored as TheAnimals.IntakeLimit.  A variety of other fields   
        /// of TheAnimals are also updated: the normal weight, mature normal weight, 
        /// highest previous weight (in young animals), relative size and relative    
        /// condition.                                                                
        /// </summary>
        public void Calc_IntakeLimit()
        {
            double lactTime;                                                               // Scaled days since birth of young         
            double weightLoss;                                                              // Mean daily weight loss during lactation  
                                                                                            // as a proportion of SRW                 
            double criticalLoss;                                                            // Threshold value of WeightLoss            
            double tempDiff;
            double tempAmpl;
            double belowLCT;
            double X;
            double condFactor;
            double youngFactor;
            double heatFactor;
            double lactFactor;
            int lactNum;
            double shapeParam;

            Calc_Weights();                                                             // Compute size and condition               

            if (Condition > 1.0)  // and (LactStatus <> Lactating) then  // No longer exclude lactating females. See bug#2223 
                condFactor = Condition * (this.AParams.IntakeC[20] - Condition) / (this.AParams.IntakeC[20] - 1.0);
            else
                condFactor = 1.0;

            if (this.LactStatus == GrazType.LactType.Suckling)
                youngFactor = (1.0 - Mothers.PropnOfMaxMilk)
                                / (1.0 + Math.Exp(-this.AParams.IntakeC[3] * (MeanAge - this.AParams.IntakeC[4])));
            else
                youngFactor = 1.0;

            if (this.TheEnv.MinTemp < this.AnimalState.LowerCritTemp)                                
            {
                // Integrate sinusoidal temperature function over the part below LCT       
                tempDiff = this.TheEnv.MeanTemp - this.AnimalState.LowerCritTemp;
                tempAmpl = 0.5 * (this.TheEnv.MaxTemp - this.TheEnv.MinTemp);
                X = Math.Acos(Math.Max(-1.0, Math.Min(1.0, tempDiff / tempAmpl)));
                belowLCT = (-tempDiff * X + tempAmpl * Math.Sin(X)) / Math.PI;
                heatFactor = 1.0 + this.AParams.IntakeC[17] * belowLCT * StdMath.DIM(1.0, this.TheEnv.Precipitation / this.AParams.IntakeC[18]);
            }
            else
                heatFactor = 1.0;

            if (this.TheEnv.MinTemp >= this.AParams.IntakeC[7])                                 // High temperatures depress intake         
                heatFactor = heatFactor * (1.0 - this.AParams.IntakeC[5] * StdMath.DIM(this.TheEnv.MeanTemp, this.AParams.IntakeC[6]));
            if (this.LactStatus != GrazType.LactType.Lactating)
                this.LactAdjust = 1.0;
#pragma warning disable 162 // unreachable code
            else if (!AnimalsDynamicGlb)                                                        // In the dynamic model, LactAdjust is a    
            {                                                                                   // state variable computed in the         
                weightLoss = Size * StdMath.XDiv(ConditionAtBirthing - Condition,               // lactation routine; for GrazFeed, it is 
                                             DaysLactating);                                    // estimated with these equations         
                criticalLoss = AParams.IntakeC[14] * Math.Exp(-Math.Pow(AParams.IntakeC[13] * DaysLactating, 2));
                if (weightLoss > criticalLoss)
                    this.LactAdjust = (1.0 - AParams.IntakeC[12] * weightLoss / AParams.IntakeC[13]);
                else
                    this.LactAdjust = 1.0;
            }
#pragma warning restore 162
            if (this.LactStatus == GrazType.LactType.Lactating)
            {
                lactTime = this.DaysLactating;
                lactNum = this.NoSuckling();
            }
            else
            {
                lactTime = this.DryOffTime;
                lactNum = this.FPrevOffspring;
            }

            if (((this.ReproStatus == GrazType.ReproType.Male || this.ReproStatus == GrazType.ReproType.Castrated)) || (this.Mothers != null))
                lactFactor = 1.0;
            else
            {
                if (this.NoSuckling() > 0)
                {
                    shapeParam = AParams.IntakeC[9];
                }
                else
                {
                    shapeParam = AParams.IntakeC[21];
                }
                lactFactor = 1.0 + this.AParams.IntakeLactC[lactNum]
                                     * ((1.0 - this.AParams.IntakeC[15]) + this.AParams.IntakeC[15] * this.ConditionAtBirthing)
                                     * this.WOOD(lactTime, this.AParams.IntakeC[8], shapeParam)
                                     * this.LactAdjust;
            }
            this.IntakeLimit = this.AParams.IntakeC[1] * this.StdRefWt * this.Size * (this.AParams.IntakeC[2] - this.Size)
                           * condFactor * youngFactor * heatFactor * lactFactor * this.FIntakeModifier;
        }

        /// <summary>
        /// Reset the grazing values
        /// </summary>
        public void Reset_Grazing()
        {
            this.AnimalState = new AnimalOutput();
            this.Supp_FWI = 0.0;
            this.Start_FU = 1.0;
            Array.Resize(ref this.NetSupp_DMI, this.TheRation.Count);
            Array.Resize(ref this.TimeStepNetSupp_DMI, this.TheRation.Count);
            for (int Idx = 0; Idx < this.NetSupp_DMI.Length; Idx++)
                this.NetSupp_DMI[Idx] = 0.0;
        }

        /// <summary>
        /// Output at this step
        /// </summary>
        public AnimalOutput TimeStepState;

        /// <summary>
        /// Update the value for the timestep
        /// </summary>
        /// <param name="timeStep">The timestep fraction</param>
        /// <param name="full"></param>
        /// <param name="ts"></param>
        private void UpdateFloat(double timeStep, ref double full, double ts)
        {
            full = full + timeStep * ts;
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
        /// Update the grazing outputs
        /// </summary>
        /// <param name="TimeStep"></param>
        /// <param name="full"></param>
        /// <param name="TS"></param>
        private void UpdateGrazingOutputs(double TimeStep, ref GrazType.GrazingOutputs full, GrazType.GrazingOutputs TS)
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
        /// Update the intake record
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
        /// Update the diet record
        /// </summary>
        /// <param name="timeStep">The fraction of the timestep</param>
        /// <param name="suppFullDay"></param>
        /// <param name="full">The full diet</param>
        /// <param name="TS">The extra diet</param>
        private void UpdateDietRecord(double timeStep, bool suppFullDay, ref DietRecord full, DietRecord TS)
        {
            full.Herbage = full.Herbage + timeStep * TS.Herbage;
            if (suppFullDay)
                full.Supp = full.Supp + TS.Supp;
            else
                full.Supp = full.Supp + timeStep * TS.Supp;
            full.Milk = full.Milk + timeStep * TS.Milk;
            full.Solid = full.Herbage + full.Supp;
            full.Total = full.Solid + full.Milk;
        }

        /// <summary>
        /// Update the diet
        /// </summary>
        /// <param name="full">The full diet</param>
        /// <param name="FullDenom"></param>
        /// <param name="TS"></param>
        /// <param name="TSDenom"></param>
        /// <param name="HerbDT"></param>
        /// <param name="SuppDT"></param>
        private void UpdateDietAve(ref DietRecord full, DietRecord FullDenom, DietRecord TS, DietRecord TSDenom, double HerbDT, double SuppDT)
        {
            this.UpdateAve(ref full.Herbage, FullDenom.Herbage, TS.Herbage, TSDenom.Herbage, HerbDT);
            this.UpdateAve(ref full.Supp, FullDenom.Supp, TS.Supp, TSDenom.Supp, SuppDT);
        }

        /// <summary>
        /// Update the animal state
        /// </summary>
        /// <param name="timeStep"></param>
        /// <param name="suppFullDay"></param>
        /// <param name="suppRI"></param>
        private void UpdateAnimalState(double timeStep, bool suppFullDay, double suppRI)
        {
            double suppTS;

            if (suppFullDay)
                suppTS = 1.0;
            else
                suppTS = timeStep;

            if (timeStep == 1)
                this.AnimalState = this.TimeStepState.Copy();
            else
            {
                this.UpdateGrazingOutputs(timeStep, ref this.AnimalState.IntakePerHead, this.TimeStepState.IntakePerHead);
                this.UpdateIntakeRecord(ref this.AnimalState.PaddockIntake, this.TimeStepState.PaddockIntake, timeStep);
                this.UpdateIntakeRecord(ref this.AnimalState.SuppIntake, this.TimeStepState.SuppIntake, suppTS);

                // compute these averages *before* cumulating DM_Intake & CP_Intake 

                this.UpdateDietAve(ref this.AnimalState.Digestibility, this.AnimalState.DM_Intake,
                               this.TimeStepState.Digestibility, this.TimeStepState.DM_Intake,
                               timeStep, suppTS);
                this.UpdateDietAve(ref this.AnimalState.ProteinConc, this.AnimalState.DM_Intake,
                               this.TimeStepState.ProteinConc, this.TimeStepState.DM_Intake,
                               timeStep, suppTS);
                this.UpdateDietAve(ref this.AnimalState.ME_2_DM, this.AnimalState.DM_Intake,
                               this.TimeStepState.ME_2_DM, this.TimeStepState.DM_Intake,
                               timeStep, suppTS);
                this.UpdateDietAve(ref this.AnimalState.CorrDgProt, this.AnimalState.CP_Intake,
                               this.TimeStepState.CorrDgProt, this.TimeStepState.CP_Intake,
                               timeStep, suppTS);

                this.UpdateDietRecord(timeStep, suppFullDay, ref this.AnimalState.CP_Intake, this.TimeStepState.CP_Intake);
                this.UpdateDietRecord(timeStep, suppFullDay, ref this.AnimalState.ME_Intake, this.TimeStepState.ME_Intake);
                this.UpdateDietRecord(timeStep, suppFullDay, ref this.AnimalState.DM_Intake, this.TimeStepState.DM_Intake);

                // compute these averages *after* cumulating DM_Intake & CP_Intake
                this.AnimalState.Digestibility.Solid = StdMath.XDiv(this.AnimalState.Digestibility.Supp * this.AnimalState.DM_Intake.Supp +
                                             this.AnimalState.Digestibility.Herbage * this.AnimalState.DM_Intake.Herbage,
                                             this.AnimalState.DM_Intake.Solid);
                this.AnimalState.ProteinConc.Solid = StdMath.XDiv(this.AnimalState.CP_Intake.Solid, this.AnimalState.DM_Intake.Solid);
                this.AnimalState.ME_2_DM.Solid = StdMath.XDiv(this.AnimalState.ME_Intake.Solid, this.AnimalState.DM_Intake.Solid);
            }   ////_ WITH FullState _

            this.Supp_FWI = this.Supp_FWI + suppTS * StdMath.XDiv(this.IntakeLimit * suppRI, this.IntakeSuppt.DMPropn);
            for (int idx = 0; idx < this.NetSupp_DMI.Length; idx++)
                this.NetSupp_DMI[idx] = this.NetSupp_DMI[idx] + suppTS * this.TimeStepNetSupp_DMI[idx];
        }

        /// <summary>
        /// Do grazing
        /// </summary>
        /// <param name="deltaT">Fraction of an animal's active day</param>
        /// <param name="reset">TRUE at the start of the day</param>
        /// <param name="feedSuppFirst">Feed supplement first</param>
        /// <param name="pastIntakeRate"></param>
        /// <param name="suppIntakeRate"></param>
        public void Grazing(double deltaT,
                             bool reset,
                             bool feedSuppFirst,
                             ref GrazType.GrazingOutputs pastIntakeRate,
                             ref double suppIntakeRate)
        {
            double[] herbageRI = new double[GrazType.DigClassNo + 1];
            double maintMEIScalar;
            double waterLogScalar;
            double[,] seedRI = new double[GrazType.MaxPlantSpp + 1, 3];
            double suppRI = 0;

            // Do this before resetting AnimalState!    
            if ((!AnimalsDynamicGlb || (this.AnimalState.ME_Intake.Total == 0.0) || (this.WaterLog == 0.0) || (this.Steepness > 1.0)))    // Waterlogging effect only on level ground 
                waterLogScalar = 1.0;                                                 // The published model assumes WaterLog=0   
            else if ((this.AnimalState.EnergyUse.Gain == 0.0) || (this.AnimalState.Efficiency.Gain == 0.0))
                waterLogScalar = 1.0;
            else
            {
                maintMEIScalar = Math.Max(0.0, (this.AnimalState.EnergyUse.Gain / this.AnimalState.Efficiency.Gain) / this.AnimalState.ME_Intake.Total);
                waterLogScalar = StdMath.DIM(1.0, maintMEIScalar * WaterLog);
            }

            if (reset)                                                                  // First time step of the day?              
                this.Reset_Grazing();

            this.TimeStepState = new AnimalOutput();

            this.CalculateRelIntake(this, deltaT, feedSuppFirst,
                                waterLogScalar,                                       // The published model assumes WaterLog=0   
                                ref herbageRI, ref seedRI, ref suppRI);
            this.DescribeTheDiet(ref herbageRI, ref seedRI, ref suppRI, ref this.TimeStepState);
            this.UpdateAnimalState(deltaT, feedSuppFirst, suppRI);

            pastIntakeRate.CopyFrom(this.TimeStepState.IntakePerHead);

            suppIntakeRate = StdMath.XDiv(this.IntakeLimit * suppRI, this.IntakeSuppt.DMPropn);
        }

        /// <summary>
        /// Do grazing
        /// </summary>
        /// <param name="deltaT">Fraction of an animal's active day</param>
        /// <param name="reset">TRUE at the start of the day</param>
        /// <param name="feedSuppFirst">Feed supplement first</param>
        public void Grazing(double deltaT, bool reset, bool feedSuppFirst)
        {
            GrazType.GrazingOutputs dummyPastIntake = new GrazType.GrazingOutputs();
            double dummySuppIntake = 0.0;

            this.Grazing(deltaT, reset, feedSuppFirst, ref dummyPastIntake, ref dummySuppIntake);
        }

        /// <summary>
        /// Compute proportional contribution of diet components (milk, fodder and      
        /// supplement) and the efficiencies of energy use                            
        /// This procedure corresponds to section 5 of the model specification        
        /// </summary>
        private void Efficiencies()
        {
            double herbageEfficiency;                                                       // Efficiencies for gain from herbage &     
            double suppEfficiency;                                                          // supplement intake                      

            this.AnimalState.DietPropn.Milk = StdMath.XDiv(this.AnimalState.ME_Intake.Milk, this.AnimalState.ME_Intake.Total);
            this.AnimalState.DietPropn.Solid = 1.0 - this.AnimalState.DietPropn.Milk;
            this.AnimalState.DietPropn.Supp = this.AnimalState.DietPropn.Solid * StdMath.XDiv(this.AnimalState.ME_Intake.Supp, this.AnimalState.ME_Intake.Solid);
            this.AnimalState.DietPropn.Herbage = this.AnimalState.DietPropn.Solid - this.AnimalState.DietPropn.Supp;

            if (this.AnimalState.ME_Intake.Total < GrazType.VerySmall)                             
            {
                // Efficiencies of various uses of ME
                this.AnimalState.Efficiency.Maint = this.AParams.EfficC[4];
                this.AnimalState.Efficiency.Lact = this.AParams.EfficC[7];
                this.AnimalState.Efficiency.Preg = this.AParams.EfficC[8];
            }
            else
            {
                this.AnimalState.Efficiency.Maint = this.AnimalState.DietPropn.Solid * (this.AParams.EfficC[1] + this.AParams.EfficC[2] * this.AnimalState.ME_2_DM.Solid) +
                                    this.AnimalState.DietPropn.Milk * this.AParams.EfficC[3];
                this.AnimalState.Efficiency.Lact = this.AParams.EfficC[5] + this.AParams.EfficC[6] * this.AnimalState.ME_2_DM.Solid;
                this.AnimalState.Efficiency.Preg = this.AParams.EfficC[8];
            }

            herbageEfficiency = this.AParams.EfficC[13]
                                 * (1.0 + this.AParams.EfficC[14] * this.Inputs.LegumePropn)
                                 * (1.0 + this.AParams.EfficC[15] * (this.TheEnv.Latitude / 40.0) * Math.Sin(GrazEnv.DAY2RAD * StdDate.DOY(this.TheEnv.TheDay, true)))
                                 * this.AnimalState.ME_2_DM.Herbage;
            suppEfficiency = this.AParams.EfficC[16] * this.AnimalState.ME_2_DM.Supp;
            this.AnimalState.Efficiency.Gain = this.AnimalState.DietPropn.Herbage * herbageEfficiency
                                 + this.AnimalState.DietPropn.Supp * suppEfficiency
                                 + this.AnimalState.DietPropn.Milk * this.AParams.EfficC[12];
        }

        /// <summary>
        /// Basal metabolism routine.  Outputs (EnergyUse.Metab,EnergyUse.Maint,      
        /// ProteinUse.Maint) are stored in AnimalState.                              
        /// </summary>
        private void Compute_Maintenance()
        {
            double metabScale;
            double grazeMoved_KM;       // Distance walked during grazing (km)      
            double eatingEnergy;        // Energy requirement for grazing           
            double movingEnergy;        // Energy requirement for movement          
            double endoUrineN;

            if (this.LactStatus == GrazType.LactType.Suckling)
                metabScale = 1.0 + this.AParams.MaintC[5] * this.AnimalState.DietPropn.Milk;
            else if ((this.ReproStatus == GrazType.ReproType.Male) && (this.MeanAge >= this.AParams.Puberty[1]))               // Puberty[true]
                metabScale = 1.0 + this.AParams.MaintC[15];
            else
                metabScale = 1.0;
            this.AnimalState.EnergyUse.Metab = metabScale * this.AParams.MaintC[2] * Math.Pow(this.BaseWeight, 0.75) // Basal metabolism                         
                               * Math.Max(Math.Exp(-this.AParams.MaintC[3] * this.MeanAge), this.AParams.MaintC[4]);

            eatingEnergy = this.AParams.MaintC[6] * this.BaseWeight * this.AnimalState.DM_Intake.Herbage             // Work of eating fibrous diets             
                                         * StdMath.DIM(this.AParams.MaintC[7], this.AnimalState.Digestibility.Herbage);

            if (Inputs.TotalGreen > 100.0)                                                                      // Energy requirement for movement          
                grazeMoved_KM = 1.0 / (this.AParams.MaintC[8] * this.Inputs.TotalGreen + this.AParams.MaintC[9]);
            else if (Inputs.TotalDead > 100.0)
                grazeMoved_KM = 1.0 / (this.AParams.MaintC[8] * this.Inputs.TotalDead + this.AParams.MaintC[9]);
            else
                grazeMoved_KM = 0.0;
            if (this.AnimalsPerHa > this.AParams.MaintC[17])
                grazeMoved_KM = grazeMoved_KM * (this.AParams.MaintC[17] / this.AnimalsPerHa);

            movingEnergy = this.AParams.MaintC[16] * this.LiveWeight * this.Steepness * (grazeMoved_KM + this.DistanceWalked);

            this.AnimalState.EnergyUse.Maint = (this.AnimalState.EnergyUse.Metab + eatingEnergy + movingEnergy) / this.AnimalState.Efficiency.Maint
                               + this.AParams.MaintC[1] * this.AnimalState.ME_Intake.Total;
            this.FeedingLevel = this.AnimalState.ME_Intake.Total / this.AnimalState.EnergyUse.Maint - 1.0;

            //// ...........................................................................  MAINTENANCE PROTEIN REQUIREMENT          

            this.AnimalState.EndoFaeces.Nu[(int)GrazType.TOMElement.n] = (this.AParams.MaintC[10] * this.AnimalState.DM_Intake.Solid + this.AParams.MaintC[11] * this.AnimalState.ME_Intake.Milk) / GrazType.N2Protein;

            if (this.Animal == GrazType.AnimalType.Cattle)
            {
                endoUrineN = (this.AParams.MaintC[12] * Math.Log(this.BaseWeight) - this.AParams.MaintC[13]) / GrazType.N2Protein;
                this.AnimalState.DermalNLoss = this.AParams.MaintC[14] * Math.Pow(this.BaseWeight, 0.75) / GrazType.N2Protein;
            }
            else  
            {
                // sheep
                endoUrineN = (this.AParams.MaintC[12] * this.BaseWeight + this.AParams.MaintC[13]) / GrazType.N2Protein;
                this.AnimalState.DermalNLoss = 0.0;
            }
            this.AnimalState.ProteinUse.Maint = GrazType.N2Protein * (this.AnimalState.EndoFaeces.Nu[(int)GrazType.TOMElement.n] + endoUrineN + this.AnimalState.DermalNLoss);
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
            double result;
            if (IsRoughage)
                result = Math.Max(this.AParams.ProtC[1], Math.Min(this.AParams.ProtC[3] * CP - this.AParams.ProtC[4], this.AParams.ProtC[2]));
            else if (DG >= 1.0)
                result = 0.0;
            else
                result = this.AParams.ProtC[9] * (1.0 - ADIP_2_CP / (1.0 - DG));
            return result;
        }

        /// <summary>
        /// Compute microbial crude protein and DPLS
        /// </summary>
        private void Compute_DPLS()
        {
            DietRecord UDPIntakes = new DietRecord();
            DietRecord DUDP = new DietRecord();
            double dgCorrect;
            int idx;

            this.ComputeRDP(this.TheEnv.Latitude, this.TheEnv.TheDay, 1.0, this.FeedingLevel,
                        ref this.AnimalState.CorrDgProt, ref this.AnimalState.RDP_Intake, ref this.AnimalState.RDP_Reqd, ref UDPIntakes);
            this.AnimalState.UDP_Intake = UDPIntakes.Solid + UDPIntakes.Milk;
            dgCorrect = StdMath.XDiv(this.AnimalState.CorrDgProt.Supp, this.AnimalState.SuppIntake.Degradability);

            this.AnimalState.MicrobialCP = this.AParams.ProtC[6] * this.AnimalState.RDP_Reqd;                       // Microbial crude protein synthesis        

            DUDP.Milk = AParams.ProtC[5];
            DUDP.Herbage = DUDPFunc(true, this.AnimalState.ProteinConc.Herbage, this.AnimalState.CorrDgProt.Herbage, 0.0);
            DUDP.Supp = 0.0;
            for (idx = 0; idx <= TheRation.Count - 1; idx++)
                DUDP.Supp = DUDP.Supp + StdMath.XDiv(this.NetSupp_DMI[idx], this.AnimalState.DM_Intake.Supp)        // Fraction of net supplement intake        
                                          * this.DUDPFunc(this.TheRation[idx].IsRoughage,                           // DUDP of this part of the ration          
                                                      this.TheRation[idx].CrudeProt,
                                                      this.TheRation[idx].DegProt * dgCorrect,
                                                      this.TheRation[idx].ADIP2CP);

            this.AnimalState.DPLS_MCP = this.AParams.ProtC[7] * this.AnimalState.MicrobialCP;                            // DPLS from microbial crude protein        
            this.AnimalState.DPLS_Milk = DUDP.Milk * UDPIntakes.Milk;                                                    // Store DPLS from milk separately          
            this.AnimalState.DPLS = DUDP.Herbage * UDPIntakes.Herbage
                            + DUDP.Supp * UDPIntakes.Supp
                            + this.AnimalState.DPLS_Milk
                            + this.AnimalState.DPLS_MCP;
            if (UDPIntakes.Solid > 0.0)
                this.AnimalState.UDP_Dig = (DUDP.Herbage * UDPIntakes.Herbage + DUDP.Supp * UDPIntakes.Supp) / UDPIntakes.Solid;
            else
                this.AnimalState.UDP_Dig = DUDP.Herbage;

            this.AnimalState.OrgFaeces.DM = this.AnimalState.DM_Intake.Solid * (1.0 - this.AnimalState.Digestibility.Solid);       // Faecal DM & N:                           
            this.AnimalState.OrgFaeces.Nu[(int)GrazType.TOMElement.n] = ((1.0 - DUDP.Herbage) * UDPIntakes.Herbage       //   Undigested UDP                         
                               + (1.0 - DUDP.Supp) * UDPIntakes.Supp
                               + (1.0 - DUDP.Milk) * UDPIntakes.Milk
                               + AParams.ProtC[8] * this.AnimalState.MicrobialCP)                                        //   Undigested MCP                         
                               / GrazType.N2Protein + this.AnimalState.EndoFaeces.Nu[(int)GrazType.TOMElement.n];        //   Endogenous component                   
            this.AnimalState.InOrgFaeces.Nu[(int)GrazType.TOMElement.n] = 0.0;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="T"></param>
        /// <param name="A"></param>
        /// <param name="B"></param>
        /// <param name="C"></param>
        /// <returns></returns>
        private double DeltaGompertz(double T, double A, double B, double C)
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
            double birthWt;                                         // Reference birth weight (kg)              
            double birthConceptus;                                  // Reference value of conceptus weight at birth  
            double foetalNWt;                                       // Normal weight of foetus (kg)             
            double foetalNGrowth;                                   // Normal growth rate of foetus (kg/day)    
            double conditionFactor;                                 // Effect of maternal condition on foetal growth 
            double foetalCondition;                                 // Foetal body condition                    
            double prevConceptusWt;

            prevConceptusWt = this.ConceptusWt();
            birthWt = this.fBirthWtForSize();
            birthConceptus = NoFoetuses * this.AParams.PregC[5] * birthWt;

            foetalNWt = this.FoetalNormWt();
            foetalNGrowth = birthWt * this.DeltaGompertz(this.FoetalAge, this.AParams.PregC[1], this.AParams.PregC[2], this.AParams.PregC[3]);
            conditionFactor = (this.Condition - 1.0) * foetalNWt / AParams.StdBirthWt(this.NoFoetuses);
            if (this.Condition >= 1.0)
                this.FoetalWt = this.FoetalWt + foetalNGrowth * (1.0 + conditionFactor);
            else
                this.FoetalWt = this.FoetalWt + foetalNGrowth * (1.0 + this.AParams.PregScale[NoFoetuses] * conditionFactor);
            foetalCondition = FoetalWeight / foetalNWt;

            // ConceptusWt is a function of foetal age. Advance the age temporarily for this calucation.
            FoetalAge++;
            this.AnimalState.ConceptusGrowth = ConceptusWt() - prevConceptusWt;
            FoetalAge--;

            this.AnimalState.EnergyUse.Preg = this.AParams.PregC[8] * birthConceptus * foetalCondition
                                * DeltaGompertz(FoetalAge, this.AParams.PregC[1], this.AParams.PregC[9], this.AParams.PregC[10])
                                / this.AnimalState.Efficiency.Preg;
            this.AnimalState.ProteinUse.Preg = this.AParams.PregC[11] * birthConceptus * foetalCondition
                                * DeltaGompertz(FoetalAge, this.AParams.PregC[1], this.AParams.PregC[12], this.AParams.PregC[13]);

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
            double potMilkMJ;                                                               // Potential production of milk (MP')       
            double maxMilkMJ;                                                               // Milk prodn after energy deficit (MP'')   
            double energySurplus;
            double availMJ;
            double availRatio;
            double availDays;
            double condFactor;                                                              // Function of condition affecting milk     
                                                                                            // vs body reserves partition (CFlact)    
            double milkLimit;                                                               // Max. milk consumption by young (kg/hd)   
            double dayRatio;                                                                // Today's value of Milk_MJProd:PotMilkMJ   

            condFactor = 1.0 - this.AParams.IntakeC[15] + this.AParams.IntakeC[15] * ConditionAtBirthing;
            if (this.NoSuckling() > 0)                                                      // Potential milk production in MJ          
                potMilkMJ = this.AParams.PeakLactC[this.NoSuckling()]
                             * Math.Pow(this.StdRefWt, 0.75) * this.Size
                             * condFactor * this.LactAdjust
                             * this.WOOD(this.DaysLactating + this.AParams.LactC[1], this.AParams.LactC[2], this.AParams.LactC[3]);
            else
                potMilkMJ = this.AParams.LactC[5] * this.AParams.LactC[6] * this.AParams.PeakMilk // peakmilk must have a value
                             * condFactor * this.LactAdjust
                             * this.WOOD(this.DaysLactating + this.AParams.LactC[1], this.AParams.LactC[2], this.AParams.LactC[4]); 

            energySurplus = this.AnimalState.ME_Intake.Total - this.AnimalState.EnergyUse.Maint - this.AnimalState.EnergyUse.Preg;
            availMJ = AParams.LactC[5] * this.AnimalState.Efficiency.Lact * energySurplus;
            availRatio = availMJ / potMilkMJ;                                               // Effects of available energy, stage of    
            availDays = Math.Max(DaysLactating, availRatio / (2.0 * AParams.LactC[22]));    // lactation and body condition on        
            maxMilkMJ = potMilkMJ * this.AParams.LactC[7]                                        // milk production                        
                                         / (1.0 + Math.Exp(this.AParams.LactC[19] - this.AParams.LactC[20] * availRatio
                                                       - this.AParams.LactC[21] * availDays * (availRatio - this.AParams.LactC[22] * availDays)
                                                       + this.AParams.LactC[23] * Condition * (availRatio - this.AParams.LactC[24] * Condition)));
            if (this.NoSuckling() > 0)
            {
                milkLimit = this.AParams.LactC[6]
                             * this.NoSuckling()
                             * Math.Pow(this.Young.BaseWeight, 0.75)
                             * (this.AParams.LactC[12] + this.AParams.LactC[13] * Math.Exp(-this.AParams.LactC[14] * DaysLactating));
                Milk_MJProdn = Math.Min(maxMilkMJ, milkLimit);                              // Milk_MJ_Prodn becomes less than MaxMilkMJ
                PropnOfMaxMilk = Milk_MJProdn / milkLimit;                                  // when the young are not able to consume 
            }                                                                               // the amount of milk the mothers are     
            else                                                                            // capable of producing                   
            {
                Milk_MJProdn = maxMilkMJ;
                PropnOfMaxMilk = 1.0;
            }

            this.AnimalState.EnergyUse.Lact = Milk_MJProdn / (AParams.LactC[5] * this.AnimalState.Efficiency.Lact);
            this.AnimalState.ProteinUse.Lact = AParams.LactC[15] * Milk_MJProdn / AParams.LactC[6];

            if (AnimalsDynamicGlb)
                if (DaysLactating < AParams.LactC[16] * AParams.LactC[2])
                {
                    LactAdjust = 1.0;
                    LactRatio = 1.0;
                }
                else
                {
                    dayRatio = StdMath.XDiv(Milk_MJProdn, potMilkMJ);
                    if (dayRatio < LactRatio)
                    {
                        LactAdjust = LactAdjust - AParams.LactC[17] * (LactRatio - dayRatio);
                        LactRatio = AParams.LactC[18] * dayRatio + (1.0 - AParams.LactC[18]) * LactRatio;
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

            AgeFactor = this.AParams.WoolC[5] + (1.0 - this.AParams.WoolC[5]) * (1.0 - Math.Exp(-this.AParams.WoolC[12] * this.MeanAge));
            DayLenFactor = 1.0 + this.AParams.WoolC[6] * (TheEnv.DayLength - 12);
            DPLS_To_CFW = this.AParams.WoolC[7] * this.AParams.FleeceRatio * AgeFactor * DayLenFactor;
            ME_To_CFW = this.AParams.WoolC[8] * this.AParams.FleeceRatio * AgeFactor * DayLenFactor;
            this.AnimalState.DPLS_Avail_Wool = StdMath.DIM(this.AnimalState.DPLS + DPLS_Adjust,
                                    this.AParams.WoolC[9] * (this.AnimalState.ProteinUse.Lact + this.AnimalState.ProteinUse.Preg));
            ME_Avail_Wool = StdMath.DIM(this.AnimalState.ME_Intake.Total, this.AnimalState.EnergyUse.Lact + this.AnimalState.EnergyUse.Preg);
            DayCFWGain = Math.Min(DPLS_To_CFW * this.AnimalState.DPLS_Avail_Wool, ME_To_CFW * ME_Avail_Wool);
#pragma warning disable 162 //unreachable code
            if (AnimalsDynamicGlb)
                this.AnimalState.ProteinUse.Wool = (1 - this.AParams.WoolC[4]) * (this.AParams.WoolC[3] * this.DeltaWoolWt) +            // Smoothed wool growth                     
                                   this.AParams.WoolC[4] * DayCFWGain;
            else
                this.AnimalState.ProteinUse.Wool = DayCFWGain;
#pragma warning restore 162
            this.AnimalState.EnergyUse.Wool = this.AParams.WoolC[1] * StdMath.DIM(this.AnimalState.ProteinUse.Wool, this.AParams.WoolC[2] * this.Size) /      // Energy use for fleece                    
                               this.AParams.WoolC[3];
        }

        /// <summary>
        /// Apply the wool growth
        /// </summary>
        private void Apply_WoolGrowth()
        {
            double ageFactor;
            double potCleanGain;
            double diamPower;
            double gain_Length;

            this.DeltaWoolWt = this.AnimalState.ProteinUse.Wool / this.AParams.WoolC[3];                // Convert clean to greasy fleece           
            this.WoolWt = this.WoolWt + this.DeltaWoolWt;
            this.AnimalState.TotalWoolEnergy = this.AParams.WoolC[1] * this.DeltaWoolWt;                // Reporting only                           

            // Changed to always TRUE for use with AgLab API, since we want to
            // be able to report the change in coat depth
            if (true) // AnimalsDynamicGlb  then                                                        // This section deals with fibre diameter   
            {
                ageFactor = this.AParams.WoolC[5] + (1.0 - this.AParams.WoolC[5]) * (1.0 - Math.Exp(-this.AParams.WoolC[12] * this.MeanAge));
                potCleanGain = (this.AParams.WoolC[3] * this.AParams.FleeceRatio * this.StdRefWt) * ageFactor / 365;
                if (this.AnimalState.EnergyUse.Gain >= 0.0)
                    diamPower = this.AParams.WoolC[13];
                else
                    diamPower = this.AParams.WoolC[14];
                this.DeltaWoolMicron = this.AParams.MaxFleeceDiam * Math.Pow(this.AnimalState.ProteinUse.Wool / potCleanGain, diamPower);
                if (BaseWeight <= 0)
                    throw new Exception("Base weight is zero or less for " + this.NoAnimals.ToString() + " " + this.Breed + " animals aged " + this.AgeDays.ToString() + " days");
                if (this.DeltaWoolMicron > 0.0)
                    gain_Length = 100.0 * 4.0 / Math.PI * this.AnimalState.ProteinUse.Wool /              // Computation of fibre diameter assumes    
                                           (this.AParams.WoolC[10] * this.AParams.WoolC[11] *             // that the day's growth is cylindrical   
                                             this.AParams.ChillC[1] * Math.Pow(this.BaseWeight, 2.0 / 3.0) *   // in shape                               
                                             StdMath.Sqr(this.DeltaWoolMicron * 1E-6));
                else
                    gain_Length = 0.0;
                this.WoolMicron = StdMath.XDiv(this.FCoatDepth * this.WoolMicron +                      // Running average fibre diameter           
                                           gain_Length * this.DeltaWoolMicron,
                                         FCoatDepth + gain_Length);
                this.FCoatDepth = this.FCoatDepth + gain_Length;

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

            this.AnimalState.Therm0HeatProdn = this.AnimalState.ME_Intake.Total            // Thermoneutral heat production            
                           - this.AnimalState.Efficiency.Preg * this.AnimalState.EnergyUse.Preg
                           - this.AnimalState.Efficiency.Lact * this.AnimalState.EnergyUse.Lact
                           - this.AnimalState.Efficiency.Gain * (this.AnimalState.ME_Intake.Total - this.AnimalState.EnergyUse.Maint - this.AnimalState.EnergyUse.Preg - this.AnimalState.EnergyUse.Lact)
                         + this.AParams.ChillC[16] * this.ConceptusWt();
            SurfaceArea = this.AParams.ChillC[1] * Math.Pow(this.BaseWeight, 2.0 / 3.0);
            BodyRadius = this.AParams.ChillC[2] * Math.Pow(this.NormalWt, 1.0 / 3.0);


            // Means and amplitudes for temperature and windrun     
            AveTemp = 0.5 * (TheEnv.MaxTemp + TheEnv.MinTemp);                                                          
            TempRange = (TheEnv.MaxTemp - TheEnv.MinTemp) / 2.0;
            AveWind = 0.4 * TheEnv.WindSpeed;                                               // 0.4 corrects wind to animal height       
            WindRange = 0.35 * AveWind;
            PropnClearSky = 0.7 * Math.Exp(-0.25 * TheEnv.Precipitation);                   // Equation J.4                             


            TissueInsulation = AParams.ChillC[3] * Math.Min(1.0, 0.4 + 0.02 * MeanAge) *    // Reduce tissue insulation for animals under 1 month old
                                            (AParams.ChillC[4] + (1.0 - AParams.ChillC[4]) * Condition);    // Tissue insulation calculated as a fn     
                                                                                                            // of species and body condition          
            Factor1 = BodyRadius / (BodyRadius + CoatDepth);                                // These factors are used in equation J.8   
            Factor2 = BodyRadius * Math.Log(1.0 / Factor1);
            WetFactor = this.AParams.ChillC[5] + (1.0 - this.AParams.ChillC[5]) *
                                            Math.Exp(-this.AParams.ChillC[6] * TheEnv.Precipitation / CoatDepth);
            HeatPerArea = this.AnimalState.Therm0HeatProdn / SurfaceArea;                   // These factors are used in equation J.10  
            LCT_Base = this.AParams.ChillC[11] - HeatPerArea * TissueInsulation;
            Factor3 = HeatPerArea / (HeatPerArea - this.AParams.ChillC[12]);

            this.AnimalState.EnergyUse.Cold = 0.0;
            this.AnimalState.LowerCritTemp = 0.0;
            for (Time = 1; Time <= 12; Time++)
            {
                Temp2Hr = AveTemp + TempRange * HourSines[Time];
                Wind2Hr = AveWind + WindRange * HourSines[Time];

                Insulation = WetFactor *                                                    // External insulation due to hair cover or 
                              (Factor1 / (AParams.ChillC[7] + AParams.ChillC[8] * Math.Sqrt(Wind2Hr)) +     // fleece is calculated from Blaxter (1977)      
                               Factor2 * (AParams.ChillC[9] - AParams.ChillC[10] * Math.Sqrt(Wind2Hr)));                                     

                LCT = LCT_Base + (this.AParams.ChillC[12] - HeatPerArea) * Insulation;
                if ((Time >= 7) && (Time <= 11) && (Temp2Hr > 10.0))                        // Night-time, i.e. 7 pm to 5 am            
                    LCT = LCT + PropnClearSky * this.AParams.ChillC[13] * Math.Exp(-AParams.ChillC[14] * StdMath.Sqr(StdMath.DIM(Temp2Hr, AParams.ChillC[15])));

                EnergyRate = SurfaceArea * StdMath.DIM(LCT, Temp2Hr)
                                                / (Factor3 * TissueInsulation + Insulation);
                this.AnimalState.EnergyUse.Cold = this.AnimalState.EnergyUse.Cold + 1.0 / 12.0 * EnergyRate;
                this.AnimalState.LowerCritTemp = this.AnimalState.LowerCritTemp + 1.0 / 12.0 * LCT;

            } ////_ FOR Time _

            this.AnimalState.EnergyUse.Maint = this.AnimalState.EnergyUse.Maint + this.AnimalState.EnergyUse.Cold;
        }

        /// <summary>
        /// Computes the efficiency of energy use for weight change.  This routine  
        /// is called twice if chilling energy use is computed                      
        /// </summary>
        private void Adjust_K_Gain()
        {
            if (this.AnimalState.ME_Intake.Total < this.AnimalState.EnergyUse.Maint + this.AnimalState.EnergyUse.Preg + this.AnimalState.EnergyUse.Lact)
            {                                                                                                           // Efficiency of energy use for weight change     
                if (LactStatus == GrazType.LactType.Lactating)
                    this.AnimalState.Efficiency.Gain = this.AnimalState.Efficiency.Lact / this.AParams.EfficC[10];                     // Lactating animals in -ve energy balance 
                else
                    this.AnimalState.Efficiency.Gain = this.AnimalState.Efficiency.Maint / this.AParams.EfficC[11];                    // Dry animals in -ve energy balance        
            }
            else if (LactStatus == GrazType.LactType.Lactating)
                this.AnimalState.Efficiency.Gain = this.AParams.EfficC[9] * this.AnimalState.Efficiency.Lact;                          // Lactating animals in +ve energy balance  
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


            this.AnimalState.EnergyUse.Gain = this.AnimalState.Efficiency.Gain * (this.AnimalState.ME_Intake.Total - (this.AnimalState.EnergyUse.Maint + this.AnimalState.EnergyUse.Preg + this.AnimalState.EnergyUse.Lact))
                             - this.AnimalState.EnergyUse.Wool;

            Eff_DPLS = this.AParams.GainC[2] / (1.0 + (this.AParams.GainC[2] / this.AParams.GainC[3] - 1.0) *               // Efficiency of use of protein from milk   
                                                StdMath.XDiv(this.AnimalState.DPLS_Milk, this.AnimalState.DPLS));           // is higher than from solid sources      
            DPLS_Used = (this.AnimalState.ProteinUse.Maint + this.AnimalState.ProteinUse.Preg + this.AnimalState.ProteinUse.Lact) / Eff_DPLS;
            if (Animal == GrazType.AnimalType.Sheep)                                                                        // Efficiency of use of protein for wool is 
                DPLS_Used = DPLS_Used + this.AnimalState.ProteinUse.Wool / AParams.GainC[1];                                // 0.6 regardless of source               
            this.AnimalState.ProteinUse.Gain = Eff_DPLS * (this.AnimalState.DPLS - DPLS_Used);


            fGainSize = this.NormalWeightFunc(this.MeanAge, this.MaxPrevWt, 0.0) / this.StdRefWt;
            GainSigs[0] = AParams.GainC[5];
            GainSigs[1] = AParams.GainC[4];
            SizeFactor1 = StdMath.SIG(fGainSize, GainSigs);
            SizeFactor2 = StdMath.RAMP(fGainSize, this.AParams.GainC[6], this.AParams.GainC[7]);

            this.AnimalState.GainEContent = this.AParams.GainC[8]                                             // Generalization of the SCA equations      
                               - SizeFactor1 * (this.AParams.GainC[9] - this.AParams.GainC[10] * (this.FeedingLevel - 1.0))
                               + SizeFactor2 * this.AParams.GainC[11] * (this.Condition - 1.0);
            this.AnimalState.GainPContent = this.AParams.GainC[12]
                               + SizeFactor1 * (this.AParams.GainC[13] - this.AParams.GainC[14] * (this.FeedingLevel - 1.0))
                               - SizeFactor2 * this.AParams.GainC[15] * (Condition - 1.0);

            this.AnimalState.UDP_Reqd = StdMath.DIM(DPLS_Used +
                                     (this.AnimalState.EnergyUse.Gain / this.AnimalState.GainEContent) * this.AnimalState.GainPContent / Eff_DPLS,
                                    this.AnimalState.DPLS_MCP)
                               / this.AnimalState.UDP_Dig;

            NetProtein = this.AnimalState.ProteinUse.Gain - this.AnimalState.GainPContent * this.AnimalState.EnergyUse.Gain / this.AnimalState.GainEContent;

            if ((NetProtein < 0) && (this.AnimalState.ProteinUse.Lact > GrazType.VerySmall))                // Deficiency of protein, i.e. protein is   
            {                                                                                               //  more limiting than ME                  
                MilkScalar = Math.Max(0.0, 1.0 + AParams.GainC[16] * NetProtein /                           // Redirect protein from milk to weight change    
                                                                this.AnimalState.ProteinUse.Lact);                                           
                this.AnimalState.EnergyUse.Gain = this.AnimalState.EnergyUse.Gain + (1.0 - MilkScalar) * Milk_MJProdn;
                this.AnimalState.ProteinUse.Gain = this.AnimalState.ProteinUse.Gain + (1.0 - MilkScalar) * this.AnimalState.ProteinUse.Lact;
                NetProtein = this.AnimalState.ProteinUse.Gain - this.AnimalState.GainPContent * this.AnimalState.EnergyUse.Gain / this.AnimalState.GainEContent;

                Milk_MJProdn = MilkScalar * Milk_MJProdn;
                this.AnimalState.EnergyUse.Lact = MilkScalar * this.AnimalState.EnergyUse.Lact;
                this.AnimalState.ProteinUse.Lact = MilkScalar * this.AnimalState.ProteinUse.Lact;
            }
            Milk_ProtProdn = this.AnimalState.ProteinUse.Lact;
            Milk_Weight = Milk_MJProdn / (AParams.LactC[5] * AParams.LactC[6]);

            if (NetProtein >= 0)
                this.AnimalState.ProteinUse.Gain = this.AnimalState.GainPContent * this.AnimalState.EnergyUse.Gain / this.AnimalState.GainEContent;
            else
                this.AnimalState.EnergyUse.Gain = this.AnimalState.EnergyUse.Gain + AParams.GainC[17] * this.AnimalState.GainEContent *
                                                    NetProtein / this.AnimalState.GainPContent;

            if ((this.AnimalState.ProteinUse.Gain < 0) && (Animal == GrazType.AnimalType.Sheep))                    // If protein is being catabolised, it can  
            {                                                                                                       // be utilized to increase wool growth    
                PrevWoolEnergy = this.AnimalState.EnergyUse.Wool;                                                   // Maintain the energy balance by           
                Compute_Wool(Math.Abs(this.AnimalState.ProteinUse.Gain));                                           // transferring any extra energy use for  
                this.AnimalState.EnergyUse.Gain = this.AnimalState.EnergyUse.Gain - (this.AnimalState.EnergyUse.Wool - PrevWoolEnergy);  // wool out of weight change              
            }

            EmptyBodyGain = this.AnimalState.EnergyUse.Gain / this.AnimalState.GainEContent;

            DeltaBaseWeight = AParams.GainC[18] * EmptyBodyGain;
            BaseWeight = BaseWeight + DeltaBaseWeight;

            this.AnimalState.ProteinUse.Total = this.AnimalState.ProteinUse.Maint + this.AnimalState.ProteinUse.Gain +
                                 this.AnimalState.ProteinUse.Preg + this.AnimalState.ProteinUse.Lact +
                                 this.AnimalState.ProteinUse.Wool;
            this.AnimalState.Urine.Nu[(int)GrazType.TOMElement.n] = StdMath.DIM(this.AnimalState.CP_Intake.Total / GrazType.N2Protein,   // Urinary loss of N                        
                                      (this.AnimalState.ProteinUse.Total - this.AnimalState.ProteinUse.Maint) / GrazType.N2Protein       // This is retention of N                 
                                      + this.AnimalState.OrgFaeces.Nu[(int)GrazType.TOMElement.n]                                   // This is other excretion                
                                      + this.AnimalState.InOrgFaeces.Nu[(int)GrazType.TOMElement.n]
                                      + this.AnimalState.DermalNLoss);
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
            double availPhos;
            double excretePhos;
            int p = (int)GrazType.TOMElement.p;

            availPhos = this.AParams.PhosC[1] * this.AnimalState.Phos_Intake.Solid + this.AParams.PhosC[2] * this.AnimalState.Phos_Intake.Milk;
            this.AnimalState.EndoFaeces.Nu[p] = this.AParams.PhosC[3] * BaseWeight;

            if (((ReproStatus == GrazType.ReproType.EarlyPreg) || (ReproStatus == GrazType.ReproType.LatePreg)) || (LactStatus == GrazType.LactType.Lactating))
                this.AnimalState.EndoFaeces.Nu[p] = this.AParams.PhosC[11] * this.AnimalState.DM_Intake.Total + this.AParams.PhosC[12] * BaseWeight;
            else
                this.AnimalState.EndoFaeces.Nu[p] = this.AParams.PhosC[9] * this.AnimalState.DM_Intake.Total + this.AParams.PhosC[10] * BaseWeight;

            this.AnimalState.Phos_Use.Maint = Math.Min(availPhos, this.AnimalState.EndoFaeces.Nu[p]);
            this.AnimalState.Phos_Use.Preg = Math.Max(AParams.PhosC[4], this.AParams.PhosC[5] * FoetalAge - this.AParams.PhosC[6]) * this.AnimalState.ConceptusGrowth;
            this.AnimalState.Phos_Use.Lact = this.AParams.PhosC[7] * Milk_Weight;
            this.AnimalState.Phos_Use.Wool = this.AParams.PhosC[8] * DeltaWoolWt;
            this.AnimalState.Phos_Use.Gain = DeltaBaseWeight *
                                 (this.AParams.PhosC[13] + this.AParams.PhosC[14] * Math.Pow(StdRefWt / BaseWeight, this.AParams.PhosC[15]));
            this.AnimalState.Phos_Use.Gain = Math.Min(availPhos - (this.AnimalState.Phos_Use.Maint + this.AnimalState.Phos_Use.Preg + this.AnimalState.Phos_Use.Lact + this.AnimalState.Phos_Use.Wool),
                                    this.AnimalState.Phos_Use.Gain);
            //// WITH AnimalState.Phos_Use DO
            this.AnimalState.Phos_Use.Total = this.AnimalState.Phos_Use.Maint + this.AnimalState.Phos_Use.Preg + this.AnimalState.Phos_Use.Lact + this.AnimalState.Phos_Use.Wool + this.AnimalState.Phos_Use.Gain;
            BasePhos = BasePhos - this.AnimalState.EndoFaeces.Nu[p] + this.AnimalState.Phos_Use.Maint + this.AnimalState.Phos_Use.Gain;
            Milk_PhosProdn = this.AnimalState.Phos_Use.Lact;

            excretePhos = this.AnimalState.EndoFaeces.Nu[p] + this.AnimalState.Phos_Intake.Total - this.AnimalState.Phos_Use.Total;
            this.AnimalState.OrgFaeces.Nu[p] = 0.0;
            this.AnimalState.InOrgFaeces.Nu[p] = excretePhos - this.AnimalState.OrgFaeces.Nu[p];
            this.AnimalState.Urine.Nu[p] = 0.0;
        }

        /// <summary>
        /// Usage of and mass balance for sulphur                                     
        /// </summary>
        private void Compute_Sulfur()
        {
            double excreteSulf;
            int s = (int)GrazType.TOMElement.s;

            this.AnimalState.EndoFaeces.Nu[s] = this.AParams.SulfC[1] * this.AnimalState.EndoFaeces.Nu[(int)GrazType.TOMElement.n];

            this.AnimalState.Sulf_Use.Maint = this.AnimalState.EndoFaeces.Nu[s];
            this.AnimalState.Sulf_Use.Preg = this.AParams.SulfC[1] * this.AnimalState.ProteinUse.Preg / GrazType.N2Protein;
            this.AnimalState.Sulf_Use.Lact = this.AParams.SulfC[2] * this.AnimalState.ProteinUse.Lact / GrazType.N2Protein;
            this.AnimalState.Sulf_Use.Wool = this.AParams.SulfC[3] * this.AnimalState.ProteinUse.Wool / GrazType.N2Protein;
            ////WITH AnimalState.Sulf_Use DO
            this.AnimalState.Sulf_Use.Gain = Math.Min(this.AParams.SulfC[1] * this.AnimalState.ProteinUse.Gain / GrazType.N2Protein,
                                    this.AnimalState.Sulf_Intake.Total - (this.AnimalState.Sulf_Use.Maint + this.AnimalState.Sulf_Use.Preg + this.AnimalState.Sulf_Use.Lact + this.AnimalState.Sulf_Use.Wool));
            ////WITH AnimalState.Sulf_Use DO
            this.AnimalState.Sulf_Use.Total = this.AnimalState.Sulf_Use.Maint + this.AnimalState.Sulf_Use.Preg + this.AnimalState.Sulf_Use.Lact + this.AnimalState.Sulf_Use.Wool + this.AnimalState.Sulf_Use.Gain;

            excreteSulf = this.AnimalState.EndoFaeces.Nu[s] + this.AnimalState.Sulf_Intake.Total - this.AnimalState.Sulf_Use.Total;
            this.AnimalState.OrgFaeces.Nu[s] = Math.Min(excreteSulf, this.AParams.SulfC[4] * this.AnimalState.DM_Intake.Total);
            this.AnimalState.InOrgFaeces.Nu[s] = 0;
            this.AnimalState.Urine.Nu[s] = excreteSulf - this.AnimalState.OrgFaeces.Nu[s];
            BaseSulf = BaseSulf + this.AnimalState.Sulf_Use.Gain;
            Milk_SulfProdn = this.AnimalState.Sulf_Use.Lact;

        }

        /// <summary>
        /// Proton balance                                                            
        /// </summary>
        private void Compute_AshAlk()
        {
            double intakeMoles;                                                             // These are all on a per-head basis        
            double accumMoles;

            intakeMoles = this.AnimalState.PaddockIntake.AshAlkalinity * this.AnimalState.PaddockIntake.Biomass
                             + this.AnimalState.SuppIntake.AshAlkalinity * this.AnimalState.SuppIntake.Biomass;
            accumMoles = AParams.AshAlkC[1] * (WeightChange + this.AnimalState.ConceptusGrowth);
            if (Animal == GrazType.AnimalType.Sheep)
                accumMoles = accumMoles + this.AParams.AshAlkC[2] * GreasyFleeceGrowth;

            this.AnimalState.OrgFaeces.AshAlk = this.AParams.AshAlkC[3] * this.AnimalState.OrgFaeces.DM;
            this.AnimalState.Urine.AshAlk = intakeMoles - accumMoles - this.AnimalState.OrgFaeces.AshAlk;
        }

        /// <summary>
        /// Nutrition function
        /// </summary>
        public void Nutrition()
        {
            this.Efficiencies();
            this.Compute_Maintenance();
            this.Compute_DPLS();

            if ((this.ReproStatus == GrazType.ReproType.EarlyPreg) || (this.ReproStatus == GrazType.ReproType.LatePreg))
                this.Compute_Pregnancy();

            if (this.LactStatus == GrazType.LactType.Lactating)
                this.Compute_Lactation();

            if (Animal == GrazType.AnimalType.Sheep)
                this.Compute_Wool(0.0);

            this.Adjust_K_Gain();
            this.Compute_Chilling();

            this.Adjust_K_Gain();
            this.Compute_Gain();

            if (this.Animal == GrazType.AnimalType.Sheep)
                this.Apply_WoolGrowth();
            this.Compute_Phosphorus();                                                       // These must be done after DeltaFleeceWt   
            this.Compute_Sulfur();                                                           // is known                               
            this.Compute_AshAlk();

            this.TotalWeight = this.BaseWeight + this.ConceptusWt();                    // TotalWeight is meant to be the weight    
            if (this.Animal == GrazType.AnimalType.Sheep)                               // "on the scales", including conceptus   
                this.TotalWeight = this.TotalWeight + this.WoolWt;                      // and/or fleece.                         
            this.AnimalState.IntakeLimitLegume = this.IntakeLimit * (1.0 + this.AParams.GrazeC[2] * this.Inputs.LegumePropn);
        }

        /// <summary>
        /// Test whether intake of RDP matches the requirement for RDP.               
        /// </summary>
        /// <returns></returns>
        public double RDP_IntakeFactor()
        {
            DietRecord tempCorrDg = new DietRecord();
            DietRecord tempUDP = new DietRecord();
            double tempRDPI = 0.0;
            double tempRDPR = 0.0;
            double tempFL;
            double oldResult, tempResult;
            int idx;
            
            // testResult : float;
            double result;

            if ((this.AnimalState.DM_Intake.Solid < GrazType.VerySmall) || (this.AnimalState.RDP_Intake >= this.AnimalState.RDP_Reqd))
                result = 1.0;
            else
            {
                result = this.AnimalState.RDP_Intake / this.AnimalState.RDP_Reqd;
                if ((AParams.IntakeC[16] > 0.0) && (this.AParams.IntakeC[16] < 1.0))
                    result = 1.0 + this.AParams.IntakeC[16] * (result - 1.0);
                idx = 0;
                do
                {
                    oldResult = result;
                    tempFL = (oldResult * this.AnimalState.ME_Intake.Total) / this.AnimalState.EnergyUse.Maint - 1.0;
                    this.ComputeRDP(this.TheEnv.Latitude, this.TheEnv.TheDay, oldResult, tempFL,
                                ref tempCorrDg, ref tempRDPI, ref tempRDPR, ref tempUDP);
                    tempResult = StdMath.XDiv(tempRDPI, tempRDPR);
                    if ((this.AParams.IntakeC[16] > 0.0) && (this.AParams.IntakeC[16] < 1.0))
                        tempResult = 1.0 + this.AParams.IntakeC[16] * (tempResult - 1.0);
                    result = Math.Max(0.0, Math.Min(1.0 - 0.5 * (1.0 - oldResult), tempResult));
                    idx++;
                }
                while ((idx < 5) && (Math.Abs(result - oldResult) >= 0.001));  //UNTIL (Idx >= 5) or (Abs(Result-OldResult) < 0.001);
            }
            return result;
        }

        /* FUNCTION  Phos_IntakeFactor : Float; */

        /// <summary>
        /// Complete growth function
        /// </summary>
        /// <param name="RDPFactor"></param>
        public void completeGrowth(double RDPFactor)
        {
            double lifeWG, dayWG;

            this.AnimalState.RDP_IntakeEffect = RDPFactor;

            if ((this.NoMales == 0) || (this.NoFemales == 0))
                this.BWGain_Solid = 0.0;
            else
            {
                lifeWG = StdMath.DIM(BaseWeight - WeightChange, this.BirthWt);
                dayWG = Math.Max(WeightChange, 0.0);
                BWGain_Solid = StdMath.XDiv(lifeWG * BWGain_Solid + dayWG * StdMath.XDiv(this.AnimalState.ME_Intake.Solid, this.AnimalState.ME_Intake.Total), lifeWG + dayWG);
            }
        }

        /// <summary>
        /// Records state information prior to the grazing and nutrition calculations     
        /// so that it can be restored if there is an RDP insufficiency.                
        /// </summary>
        /// <param name="animalInfo"></param>
        public void storeStateInfo(ref AnimalStateInfo animalInfo)
        {
            animalInfo.BaseWeight = BasalWeight;
            animalInfo.WoolWt = WoolWt;
            animalInfo.WoolMicron = WoolMicron;
            animalInfo.CoatDepth = FCoatDepth;
            animalInfo.FoetalWt = FoetalWt;
            animalInfo.LactAdjust = LactAdjust;
            animalInfo.LactRatio = LactRatio;
            animalInfo.BasePhos = BasePhos;
            animalInfo.BaseSulf = BaseSulf;
        }

        /// <summary>
        /// Restores state information about animal groups if there is an RDP insufficiency.                                                              
        /// </summary>
        /// <param name="animalInfo"></param>
        public void revertStateInfo(AnimalStateInfo animalInfo)
        {
            BasalWeight = animalInfo.BaseWeight;
            WoolWt = animalInfo.WoolWt;
            WoolMicron = animalInfo.WoolMicron;
            FCoatDepth = animalInfo.CoatDepth;
            FoetalWt = animalInfo.FoetalWt;
            LactAdjust = animalInfo.LactAdjust;
            LactRatio = animalInfo.LactRatio;
            BasePhos = animalInfo.BasePhos;
            BaseSulf = animalInfo.BaseSulf;
        }

        /*function  YoungSuppIntakePropn  : Float;
        function  MotherSuppIntakePropn : Float; */
        /// <summary>
        /// Test to see whether urea intake in the supplement has exceeded the limit of 
        /// 3 g per 10 kg liveweight.                                                   
        /// </summary>
        /// <returns>True if exceeded</returns>
        public bool ExceededUreaLimit()
        {
            int i;

            bool result = false;
            if (TheRation.TotalAmount > 0.0)
            {
                for (i = 0; i <= TheRation.Count - 1; i++)
                {
                    // If there's that much CP, it must be urea...
                    if ((TheRation[i].CrudeProt > 1.5) && (TheRation[i].Amount > 3.0e-4 * LiveWeight))
                        result = true;
                }
            }
            return result;
        }

        // Outputs to other models .......................................
        /// <summary>
        /// Add grazing outputs
        /// </summary>
        /// <param name="grazingOutputs"></param>
        public void AddGrazingOutputs(ref GrazType.GrazingOutputs grazingOutputs)
        {
            int Clss, Sp, Rp;

            for (Clss = 1; Clss <= GrazType.DigClassNo; Clss++)
                grazingOutputs.Herbage[Clss] = grazingOutputs.Herbage[Clss] + NoAnimals * this.AnimalState.IntakePerHead.Herbage[Clss];
            for (Sp = 1; Sp <= GrazType.MaxPlantSpp; Sp++)
            {
                for (Rp = GrazType.UNRIPE; Rp <= GrazType.RIPE; Rp++)
                    grazingOutputs.Seed[Sp, Rp] = grazingOutputs.Seed[Sp, Rp] + NoAnimals * this.AnimalState.IntakePerHead.Seed[Sp, Rp];
            }
        }

        /// <summary>
        /// Organic faeces
        /// </summary>
        public GrazType.DM_Pool OrgFaeces
        {
            get { return this.GetOrgFaeces(); }
        }
        
        /// <summary>
        /// Gets the inorganic faeces
        /// </summary>
        public GrazType.DM_Pool InOrgFaeces
        {
            get { return this.GetInOrgFaeces(); }
        }
        
        /// <summary>
        /// Gets the urine value
        /// </summary>
        public GrazType.DM_Pool Urine
        {
            get { return this.GetUrine(); }
        }
        
        /// <summary>
        /// Gets the excretion information
        /// </summary>
        public ExcretionInfo Excretion
        {
            get { return this.getExcretion(); }
        }

        // Management events .............................................

        /// <summary>
        ///  Commence joining                                                          
        /// </summary>
        /// <param name="maleParams"></param>
        /// <param name="matingPeriod"></param>
        public void Join(AnimalParamSet maleParams, int matingPeriod)
        {
            if ((this.ReproStatus == GrazType.ReproType.Empty) && (this.MeanAge > this.AParams.Puberty[0]))
            {
                if (maleParams.Animal != this.AParams.Animal)
                    throw new Exception("Attempt to mate female " + GrazType.AnimalText[(int)this.AParams.Animal].ToLower() + " with male " + GrazType.AnimalText[(int)maleParams.Animal].ToLower());

                this.FMatedTo = new AnimalParamSet(null, maleParams);
                this.DaysToMate = matingPeriod;
                if (this.DaysToMate > 0)
                    this.MateCycle = this.AParams.OvulationPeriod / 2;
                else
                    this.MateCycle = -1;
            }
        }
        /*procedure Mate(     MaleParams    : AnimalParamSet;
                            fPregRate     : Single;
                            var NewGroups : AnimalList ); */
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="animalList"></param>
        private void CheckAnimList(ref AnimalList animalList)
        {
            if (animalList == null)
                animalList = new AnimalList();
        }
        
        /// <summary>
        /// Export the animal group
        /// </summary>
        /// <param name="weanedGroup"></param>
        /// <param name="weanedOff"></param>
        private void ExportWeaners(ref AnimalGroup weanedGroup, ref AnimalList weanedOff)
        {
            if (weanedGroup != null)
            {
                weanedGroup.LactStatus = GrazType.LactType.Dry;
                weanedGroup.FNoOffspring = 0;
                weanedGroup.Mothers = null;
                this.CheckAnimList(ref weanedOff);
                weanedOff.Add(weanedGroup);
            }
        }
        
        /// <summary>
        /// Export the group with young
        /// </summary>
        /// <param name="MotherGroup"></param>
        /// <param name="YoungGroup"></param>
        /// <param name="NYoung"></param>
        /// <param name="NewGroups"></param>
        private void ExportWithYoung(ref AnimalGroup MotherGroup, ref AnimalGroup YoungGroup, int NYoung, ref AnimalList NewGroups)
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
        protected void SplitMothers(ref AnimalGroup YoungGroup, int iTotalYoung, double GroupPropn, ref AnimalList NewGroups)
        {
            // becoming : single twin triplet

            // [0..3,1..3] first element [0] in 2nd dimension is a dummy
            double[,] PropnRemainingLambsAs = new double[4, 4]  {  {0, 0,    0,     0      }, // starting out: empty
                                                                   {0, 1,    0,     0      },               // single
                                                                   {0, 1.0/2.0,  1.0/2.0,   0      },       // twin
                                                                   {0, 1.0/4.0,  1.0/2.0,   1.0/4.0    }};  // triplet

            bool doFemales;
            int keptLambs;
            int[] lambsByParity = new int[4];
            int[] ewesByParity = new int[4];
            AnimalGroup stillMothers;
            AnimalGroup stillYoung;
            int NY;

            if ((this.NoOffspring > 3) || (this.NoOffspring < 0))
                throw new Exception("Weaning-by-sex logic can only cope with triplets");

            if (YoungGroup != null)
            {
                if ((YoungGroup.NoMales > 0) && (YoungGroup.NoFemales > 0))
                    throw new Exception("Weaning-by-sex logic: only one sex at a time");
                doFemales = (YoungGroup.ReproStatus == GrazType.ReproType.Empty);

                // Compute numbers of mothers & offspring that remain feeding/suckling
                // with each parity
                keptLambs = YoungGroup.NoAnimals;
                for (NY = 3; NY >= 2; NY--)
                {
                    lambsByParity[NY] = Convert.ToInt32(Math.Truncate((PropnRemainingLambsAs[this.NoOffspring, NY] * keptLambs) + 0.5), CultureInfo.InvariantCulture);
                    ewesByParity[NY] = (lambsByParity[NY] / NY);
                    lambsByParity[NY] = NY * ewesByParity[NY];
                }
                lambsByParity[1] = keptLambs - lambsByParity[2] - lambsByParity[3];
                ewesByParity[1] = Math.Min(lambsByParity[1], this.NoFemales - ewesByParity[2] - ewesByParity[3]); // allow for previous rounding

                // Split off the mothers & offspring that remain feeding/suckling
                for (NY = 3; NY >= 1; NY--)
                {
                    if (ewesByParity[NY] > 0)
                    {
                        stillMothers = this.Split(ewesByParity[NY], false, this.NODIFF, this.NODIFF);
                        if (doFemales)
                            stillYoung = YoungGroup.SplitSex(0, lambsByParity[NY], false, this.NODIFF);
                        else
                            stillYoung = YoungGroup.SplitSex(lambsByParity[NY], 0, false, this.NODIFF);
                        this.ExportWithYoung(ref stillMothers, ref stillYoung, NY, ref NewGroups);
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
        /// <param name="weanFemales"></param>
        /// <param name="weanMales"></param>
        /// <param name="newGroups"></param>
        /// <param name="weanedOff"></param>
        public void Wean(bool weanFemales, bool weanMales, ref AnimalList newGroups, ref AnimalList weanedOff)
        {
            int totalYoung;
            int malePropn;
            double femaleDiff;
            DifferenceRecord diffs;
            AnimalGroup maleYoung;
            AnimalGroup femaleYoung;

            if (this.NoAnimals == 0)
            {
                this.Young = null;
                this.Lactation = 0;
            }

            else if ((Young != null) && ((weanMales && (this.Young.NoMales > 0))
                                       || (weanFemales && (this.Young.NoFemales > 0))))
            {
                totalYoung = this.Young.NoAnimals;
                malePropn = this.Young.NoMales / totalYoung;

                if (this.Young.NoMales == 0)
                {                                                                   
                    // Divide the male from the female lambs or calves                              
                    femaleYoung = this.Young;
                    maleYoung = null;
                }
                else if (this.Young.NoFemales == 0)
                {
                    maleYoung = this.Young;
                    femaleYoung = null;
                }
                else
                {
                    // TODO: this code had a nasty With block. It may need testing
                    femaleDiff = StdMath.XDiv(this.Young.FemaleWeight - this.Young.MaleWeight, this.Young.LiveWeight);
                    diffs = new DifferenceRecord() { StdRefWt = this.NODIFF.StdRefWt, BaseWeight = this.NODIFF.BaseWeight, FleeceWt = this.NODIFF.FleeceWt };
                    diffs.BaseWeight = femaleDiff * this.Young.BaseWeight;
                    diffs.FleeceWt = femaleDiff * this.Young.WoolWt;
                    diffs.StdRefWt = this.Young.StdRefWt * StdMath.XDiv(this.Young.NoAnimals, this.AParams.SRWScalars[(int)this.Young.ReproStatus] * this.Young.MaleNo + this.Young.FemaleNo)
                                                 * (1.0 - this.AParams.SRWScalars[(int)this.Young.ReproStatus]);

                    maleYoung = this.Young;
                    femaleYoung = maleYoung.SplitSex(0, this.Young.NoFemales, false, diffs);
                }
                if (femaleYoung != null)
                    femaleYoung.ReproStatus = GrazType.ReproType.Empty;

                this.Young = null;                                                                      // Detach weaners from their mothers        
                this.FPrevOffspring = this.FNoOffspring;
                this.FNoOffspring = this.FPrevOffspring;

                if (weanMales)                                                                          // Export the weaned lambs or calves        
                    this.ExportWeaners(ref maleYoung, ref weanedOff);
                if (weanFemales)
                    this.ExportWeaners(ref femaleYoung, ref weanedOff);

                if (!weanMales)                                                                         // Export ewes or cows which still have     
                    this.SplitMothers(ref maleYoung, totalYoung, malePropn, ref newGroups);             // lambs or calves                        
                if (!weanFemales)
                    this.SplitMothers(ref femaleYoung, totalYoung, 1.0 - malePropn, ref newGroups);

                if (this.AParams.Animal == GrazType.AnimalType.Sheep)                                   // Sheep don't continue lactation           
                    this.SetLactation(0);

                this.FNoOffspring = 0;
            } //// _ IF (Young <> NIL) etc _
        }

        /// <summary>
        /// Shear the animals and return the cfw per head
        /// </summary>
        /// <param name="CFW_Head"></param>
        public void Shear(ref double CFW_Head)
        {
            double greasyFleece;

            greasyFleece = this.FleeceCutWeight;
            this.WoolWt = this.WoolWt - greasyFleece;
            this.TotalWeight = this.TotalWeight - greasyFleece;
            this.Calc_CoatDepth();
            CFW_Head = this.AParams.WoolC[3] * greasyFleece;
        }

        /// <summary>
        /// End lactation in cows whose calves have already been weaned               
        /// </summary>
        public void DryOff()
        {
            if ((this.Young == null) && (this.LactStatus == GrazType.LactType.Lactating))
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
        /// <param name="doInsert"></param>
        public void ImplantHormone(bool doInsert)
        {
            double fOldEffect;

            fOldEffect = ImplantEffect;
            if (doInsert)
                ImplantEffect = this.AParams.GrowthC[4];
            else
                ImplantEffect = 1.0;
            if (ImplantEffect != fOldEffect)
                StdRefWt = StdRefWt * ImplantEffect / fOldEffect;
        }

        // Information properties ........................................
        /// <summary>
        /// Gets the animal
        /// </summary>
        public GrazType.AnimalType Animal
        {
            get { return this.GetAnimal(); }
        }
        
        /// <summary>
        /// Gets the breed name
        /// </summary>
        public string Breed
        {
            get { return this.GetBreed(); }
        }
        
        /// <summary>
        /// Gets the standard reference weight
        /// </summary>
        public double StdReferenceWt
        {
            get { return this.StdRefWt; }
        }
        
        /// <summary>
        /// Gets the age class of the animals
        /// </summary>
        public GrazType.AgeType AgeClass
        {
            get { return this.GetAgeClass(); }
        }
        
        /// <summary>
        /// Gets the reproductive state
        /// </summary>
        public GrazType.ReproType ReproState
        {
            get { return this.ReproStatus; }
        }
        
        /// <summary>
        /// Gets the mother's group
        /// </summary>
        public AnimalGroup MotherGroup
        {
            get { return this.Mothers; }
        }
        
        /// <summary>
        /// Gets the relative size of the animal
        /// </summary>
        public double RelativeSize
        {
            get { return this.Size; }
        }
        
        /// <summary>
        /// Body condition
        /// </summary>
        public double BodyCondition
        {
            get { return this.Condition; }
        }
        
        /// <summary>
        /// Gets the weight change
        /// </summary>
        public double WeightChange
        {
            get { return this.DeltaBaseWeight; }
        }

        /// <summary>
        /// Owing to the requirements of the calculation order, the stored value of   
        /// Condition is that at the start of the previous time step. We have to      
        /// compute tomorrow's value of Condition before we can compute the rate of   
        /// change in condition score                                                 
        /// </summary>
        /// <param name="condSystem"></param>
        /// <returns>Condition score change</returns>
        public double ConditionScoreChange(AnimalParamSet.Cond_System condSystem = AnimalParamSet.Cond_System.csSYSTEM1_5)
        {
            double newCondition;

            newCondition = this.BaseWeight / this.NormalWeightFunc(this.MeanAge + 1, Math.Max(this.BaseWeight, this.MaxPrevWt), this.AParams.GrowthC[3]);
            return AnimalParamSet.Condition2CondScore(newCondition, condSystem) - AnimalParamSet.Condition2CondScore(this.Condition, condSystem);
        }

        /// <summary>
        /// Gets the clean fleece weight
        /// </summary>
        public double CleanFleeceWeight
        {
            get { return this.GetCFW(); }
        }
        
        /// <summary>
        /// Gets the clean fleece growth
        /// </summary>
        public double CleanFleeceGrowth
        {
            get { return this.GetDeltaCFW(); }
        }
        
        /// <summary>
        /// Gets the greasy fleece growth
        /// </summary>
        public double GreasyFleeceGrowth
        {
            get { return this.DeltaWoolWt; }
        }
        
        /// <summary>
        /// Gets the days fibre diameter
        /// </summary>
        public double DayFibreDiam
        {
            get { return this.DeltaWoolMicron; }
        }
        
        /// <summary>
        /// Gets the milk yield
        /// </summary>
        public double MilkYield
        {
            get { return this.Milk_Weight; }
        }
        
        /// <summary>
        /// Gets the milk volume
        /// </summary>
        public double MilkVolume
        {
            get { return this.GetMilkVolume(); }
        }
        
        /// <summary>
        /// Gets the milk yield
        /// </summary>
        public double MaxMilkYield
        {
            get { return this.GetMaxMilkYield(); }
        }
        
        /// <summary>
        /// Gets the milk energy
        /// </summary>
        public double MilkEnergy
        {
            get { return this.Milk_MJProdn; }
        }
        
        /// <summary>
        /// Gets the milk protein
        /// </summary>
        public double MilkProtein
        {
            get { return this.Milk_ProtProdn; }
        }
        
        /// <summary>
        /// Gets the foetal weight
        /// </summary>
        public double FoetalWeight
        {
            get { return this.FoetalWt; }
        }
        
        /// <summary>
        /// Gets the conceptus weight
        /// </summary>
        public double ConceptusWeight
        {
            get { return this.ConceptusWt(); }
        }
        
        /// <summary>
        /// Gets the male weight
        /// </summary>
        public double MaleWeight
        {
            get { return this.GetMaleWeight(); }
        }
        
        /// <summary>
        /// Gets the female weight
        /// </summary>
        public double FemaleWeight
        {
            get { return this.GetFemaleWeight(); }
        }
        
        /// <summary>
        /// Gets the DSE
        /// </summary>
        public double DrySheepEquivs
        {
            get { return this.GetDSEs(); }
        }
        
        /// <summary>
        /// Gets or sets the potential intake
        /// </summary>
        public double PotIntake
        {
            get { return this.IntakeLimit; }
            set { this.IntakeLimit = value; }
        }
        
        /// <summary>
        /// Gets the fresh weight supplement intake
        /// </summary>
        public double SupptFW_Intake
        {
            get { return this.Supp_FWI; }
        }
        
        /// <summary>
        /// Gets the intake of supplement
        /// </summary>
        public FoodSupplement IntakeSuppt
        {
            get { return this.FIntakeSupp; }
        }
        
        /// <summary>
        /// Gets the methane energy
        /// </summary>
        public double MethaneEnergy
        {
            get { return this.GetMethaneEnergy(); }
        }
        
        /// <summary>
        /// Gets the methane weight
        /// </summary>
        public double MethaneWeight
        {
            get { return this.GetMethaneWeight(); }
        }
        
        /// <summary>
        /// Gets the methane volume
        /// </summary>
        public double MethaneVolume
        {
            get { return this.GetMethaneVolume(); }
        }
        
        /// <summary>
        /// Gets the exceeded urea warning
        /// </summary>
        public bool UreaWarning
        {
            get { return this.ExceededUreaLimit(); }
        }

        /// <summary>
        /// Returns the weight change required for these animals to have a given      
        /// change in body condition                                                  
        /// </summary>
        /// <param name="deltaBC">desired rate of change in body condition (/d)</param>
        /// <returns>Weight change</returns>
        public double WeightChangeAtCondition(double deltaBC)
        {
            double maxPrevW;
            double[] bodyCond = new double[2];
            double maxN1;
            double fA, fB;

            maxPrevW = Math.Max(this.BaseWeight, this.MaxPrevWt);
            bodyCond[0] = this.BaseWeight / this.NormalWeightFunc(this.MeanAge, maxPrevW, this.AParams.GrowthC[3]);   // Today's value of body condition          
            bodyCond[1] = bodyCond[0] + deltaBC;                                                            // Desired body condition tomorrow          
            maxN1 = MaxNormWtFunc(this.StdRefWt, this.BirthWt, this.MeanAge + 1, this.AParams);        // Maximum normal weight tomorrow           

            fA = bodyCond[1] * this.AParams.GrowthC[3] * maxN1;
            fB = bodyCond[1] * (1.0 - this.AParams.GrowthC[3]);

            return Math.Min(bodyCond[1] * maxN1, Math.Max(fA / (1.0 - fB), fA + fB * maxPrevW)) - this.BaseWeight;
        }

        /// <summary>
        /// Returns the number of male and female animals  
        /// which are aged greater than a number of days
        /// </summary>
        /// <param name="ageDays">Days of age</param>
        /// <param name="numMale">Number of male</param>
        /// <param name="numFemale">Number of female</param>
        public void GetOlder(int ageDays, ref int numMale, ref int numFemale)
        {
            this.Ages.GetOlder(ageDays, ref numMale, ref numFemale);
        }

        /// <summary>
        /// Integration of the age-dependent mortality function                       
        /// </summary>
        /// <param name="overDays">Number of days</param>
        /// <returns>Integrated value</returns>
        public double ExpectedSurvival(int overDays)
        {
            double dayDeath;
            int dayCount;
            int age;

            age = this.MeanAge;
            double result = 1.0;

            while (overDays > 0)
            {
                if ((this.LactStatus == GrazType.LactType.Suckling) || (age >= Math.Round(this.AParams.MortAge[2])))
                {
                    dayDeath = this.AParams.MortRate[1];
                    dayCount = overDays;
                }
                else if (age < Math.Round(this.AParams.MortAge[1]))
                {
                    dayDeath = this.AParams.MortRate[2];
                    dayCount = Convert.ToInt32(Math.Min(overDays, Math.Round(this.AParams.MortAge[1]) - age), CultureInfo.InvariantCulture);
                }
                else
                {
                    dayDeath = this.AParams.MortRate[1] + (this.AParams.MortRate[2] - this.AParams.MortRate[1])
                                                       * StdMath.RAMP(age, this.AParams.MortAge[2], this.AParams.MortAge[1]);
                    dayCount = 1;
                }

                result = result * Math.Pow(1.0 - dayDeath, dayCount);
                overDays -= dayCount;
                age += dayCount;
            }
            return result;
        }

        /// <summary>
        /// Combine two pools
        /// </summary>
        /// <param name="pool1">Pool one</param>
        /// <param name="pool2">Pool two</param>
        /// <returns>Combined pool</returns>
        public GrazType.DM_Pool AddDMPool(GrazType.DM_Pool pool1, GrazType.DM_Pool pool2)
        {
            int N = (int)GrazType.TOMElement.n;
            int P = (int)GrazType.TOMElement.p;
            int S = (int)GrazType.TOMElement.s;

            GrazType.DM_Pool result = new GrazType.DM_Pool();
            result.DM = pool1.DM + pool2.DM;
            result.Nu[N] = pool1.Nu[N] + pool2.Nu[N];
            result.Nu[S] = pool1.Nu[S] + pool2.Nu[S];
            result.Nu[P] = pool1.Nu[P] + pool2.Nu[P];
            result.AshAlk = pool1.AshAlk + pool2.AshAlk;

            return result;
        }

        /// <summary>
        /// Multiply pools
        /// </summary>
        /// <param name="srcPool"></param>
        /// <param name="factor"></param>
        /// <returns>The product</returns>
        public GrazType.DM_Pool MultiplyDMPool(GrazType.DM_Pool srcPool, double factor)
        {
            int N = (int)GrazType.TOMElement.n;
            int P = (int)GrazType.TOMElement.p;
            int S = (int)GrazType.TOMElement.s;

            GrazType.DM_Pool result = new GrazType.DM_Pool();
            result.DM = srcPool.DM * factor;
            result.Nu[N] = srcPool.Nu[N] * factor;
            result.Nu[S] = srcPool.Nu[S] * factor;
            result.Nu[P] = srcPool.Nu[P] * factor;
            result.AshAlk = srcPool.AshAlk * factor;

            return result;
        }

        /// <summary>
        /// Supplement relative intake.
        /// </summary>
        /// <param name="theAnimals">The animal group</param>
        /// <param name="timeStepLength"></param>
        /// <param name="suppDWPerHead"></param>
        /// <param name="supp"></param>
        /// <param name="suppRQ"></param>
        /// <param name="eatenFirst"></param>
        /// <param name="suppRI"></param>
        /// <param name="fracUnsat"></param>
        private void EatSupplement(AnimalGroup theAnimals,
                                    double timeStepLength,
                                    double suppDWPerHead,
                                    FoodSupplement supp,
                                    double suppRQ,
                                    bool eatenFirst,
                                    ref double suppRI,
                                    ref double fracUnsat)
        {
            double suppRelFill;

            if (theAnimals.IntakeLimit < GrazType.VerySmall)
                suppRelFill = 0.0;
            else
            {
                if (eatenFirst)                                                     // Relative fill of supplement           
                    suppRelFill = Math.Min(fracUnsat,
                                          suppDWPerHead / (theAnimals.IntakeLimit * suppRQ));
                else
                    suppRelFill = Math.Min(fracUnsat,
                                          suppDWPerHead / (theAnimals.IntakeLimit * timeStepLength * suppRQ));

                if ((supp.ME2DM > 0.0) && (!supp.IsRoughage))
                {
                    if (theAnimals.LactStatus == GrazType.LactType.Lactating)
                        suppRelFill = Math.Min(suppRelFill, theAnimals.AParams.GrazeC[20] / supp.ME2DM);
                    else
                        suppRelFill = Math.Min(suppRelFill, theAnimals.AParams.GrazeC[11] / supp.ME2DM);
                }
            }

            suppRI = suppRQ * suppRelFill;
            fracUnsat = StdMath.DIM(fracUnsat, suppRelFill);
        }

        /// <summary>
        /// "Relative fill" of pasture [F(d)]                                     
        /// </summary>
        /// <param name="TheAnimals">The animal group</param>
        /// <param name="FU"></param>
        /// <param name="ClassFeed"></param>
        /// <param name="TotalFeed"></param>
        /// <param name="HR"></param>
        /// <returns></returns>
        private double RelativeFill(AnimalGroup TheAnimals, double FU, double ClassFeed, double TotalFeed, double HR)
        {

            double heightFactor,
            sizeFactor,
            scaledFeed,
            propnFactor,
            rateTerm,
            timeTerm;

            double result;

            // Equation numbers refer to June 2008 revision of Freer, Moore, and Donnelly 
            heightFactor = Math.Max(0.0, (1.0 - TheAnimals.AParams.GrazeC[12]) + TheAnimals.AParams.GrazeC[12] * HR);       // Eq. 18 : HF 
            sizeFactor = 1.0 + StdMath.DIM(TheAnimals.AParams.GrazeC[7], TheAnimals.Size);                                  // Eq. 19 : ZF 
            scaledFeed = heightFactor * sizeFactor * ClassFeed;                                                             // Part of Eqs. 16, 16 : HF * ZF * B 
            propnFactor = 1.0 + TheAnimals.AParams.GrazeC[13] * StdMath.XDiv(ClassFeed, TotalFeed);                         // Part of Eqs. 16, 17 : 1 + Cr13 * Phi 
            rateTerm = 1.0 - Math.Exp(-propnFactor * TheAnimals.AParams.GrazeC[4] * scaledFeed);                            // Eq. 16 
            timeTerm = 1.0 + TheAnimals.AParams.GrazeC[5] * Math.Exp(-propnFactor * Math.Pow(TheAnimals.AParams.GrazeC[6] * scaledFeed, 2)); // Eq. 17 
            result = FU * rateTerm * timeTerm;                                                                              // Eq. 14 

            return result;
        }

        /// <summary>
        /// Eat some pasture
        /// </summary>
        /// <param name="theAnimals">The animal group</param>
        /// <param name="classFeed"></param>
        /// <param name="totalFeed"></param>
        /// <param name="HR"></param>
        /// <param name="RelQ"></param>
        /// <param name="RI"></param>
        /// <param name="FU"></param>
        private void EatPasture(AnimalGroup theAnimals, double classFeed,
                                    double totalFeed,
                                    double HR,
                                    double RelQ,
                                    ref double RI,
                                    ref double FU)
        {
            double relFill;

            relFill = this.RelativeFill(theAnimals, FU, classFeed, totalFeed, HR);
            RI = RI + relFill * RelQ;
            FU = StdMath.DIM(FU, relFill);
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
        /// <param name="theAnimals">The Animal group</param>
        /// <param name="timeStepLength"></param>
        /// <param name="feedSuppFirst"></param>
        /// <param name="waterLogScalar"></param>
        /// <param name="herbageRI"></param>
        /// <param name="seedRI"></param>
        /// <param name="suppRelIntake"></param>
        public void CalculateRelIntake(AnimalGroup theAnimals,
                              double timeStepLength,
                              bool feedSuppFirst,
                              double waterLogScalar,
                              ref double[] herbageRI,
                              ref double[,] seedRI,
                              ref double suppRelIntake)
        {
            const double CLASSWIDTH = 0.1;

            double[] availFeed = new double[GrazType.DigClassNo + 2]; // 1..DigClassNo+1   // Grazeable DM in each quality class    
            double[] heightRatio = new double[GrazType.DigClassNo + 2];                    // "Height ratio"                        
            double legume;                                                                 // Legume fraction                       
            double legumeTrop;                                                             // Legume tropicality }
            double selectFactor;                                                           // SF, adjusted for legume content       
            double[] relQ = new double[GrazType.DigClassNo + 2];                           // Function of herbage class digestib'ty 
            double suppRelQ;                                                               // Function of supplement digestibility  
            double suppFWPerHead;
            double suppDWPerHead;
            double totalFeed;

            double OMD_Supp;
            double proteinFactor;                                                          // DOM/protein and lactation factors for 
            double milkFactor;                                                             // modifying substitution rate         
            double substSuppRelQ;

            double[] relIntake = new double[GrazType.DigClassNo + 2];
            double suppEntry;
            double fillRemaining;                                                          // Proportion of maximum relative fill  that is yet to be satisfied         
            bool suppRemains;                                                              // TRUE if the animals have yet to select a supplement that is present        
            double legumeAdjust;
            int speciesIdx,
            classIdx,
            ripeIdx;

            // Start by aggregating herbage and seed into selection classes              
            for (classIdx = 1; classIdx <= GrazType.DigClassNo; classIdx++)                   
            {
                availFeed[classIdx] = theAnimals.Inputs.Herbage[classIdx].Biomass;
                heightRatio[classIdx] = theAnimals.Inputs.Herbage[classIdx].HeightRatio;
            }
            availFeed[GrazType.DigClassNo + 1] = 0.0;
            heightRatio[GrazType.DigClassNo + 1] = 1.0;

            for (speciesIdx = 1; speciesIdx <= GrazType.MaxPlantSpp; speciesIdx++)
            {
                for (ripeIdx = GrazType.UNRIPE; ripeIdx <= GrazType.RIPE; ripeIdx++)
                {
                    classIdx = theAnimals.Inputs.SeedClass[speciesIdx, ripeIdx];
                    if ((classIdx > 0) && (theAnimals.Inputs.Seeds[speciesIdx, ripeIdx].Biomass > GrazType.VerySmall))
                    {
                        fWeightAverage(ref heightRatio[classIdx],
                                        availFeed[classIdx],
                                        theAnimals.Inputs.Seeds[speciesIdx, ripeIdx].HeightRatio,
                                        theAnimals.Inputs.Seeds[speciesIdx, ripeIdx].Biomass);
                        availFeed[classIdx] = availFeed[classIdx] + theAnimals.Inputs.Seeds[speciesIdx, ripeIdx].Biomass;
                    }
                }
            }

            totalFeed = 0.0;
            for (classIdx = 1; classIdx <= GrazType.DigClassNo + 1; classIdx++)
                totalFeed = totalFeed + availFeed[classIdx];

            legume = theAnimals.Inputs.LegumePropn;
            legumeTrop = theAnimals.Inputs.LegumeTrop;

            theAnimals.TheRation.AverageSuppt(out theAnimals.FIntakeSupp);
            suppFWPerHead = theAnimals.TheRation.TotalAmount;
            suppDWPerHead = suppFWPerHead * theAnimals.FIntakeSupp.DMPropn;

            herbageRI = new double[GrazType.DigClassNo + 1];                                // Sundry initializations                
            seedRI = new double[GrazType.MaxPlantSpp + 1, GrazType.RIPE + 1];
            suppRelIntake = 0.0;
            relIntake = new double[GrazType.DigClassNo + 2];

            selectFactor = (1.0 - legume * (1.0 - legumeTrop)) * theAnimals.Inputs.SelectFactor;         // Herbage relative quality calculation  
            for (classIdx = 1; classIdx <= GrazType.DigClassNo; classIdx++)
                relQ[classIdx] = 1.0 - theAnimals.AParams.GrazeC[3] * StdMath.DIM(theAnimals.AParams.GrazeC[1] - selectFactor, theAnimals.Inputs.Herbage[classIdx].Digestibility); // Eq. 21 
            relQ[GrazType.DigClassNo + 1] = 1;                                             // fixes range check error. Set this to the value that was calc'd when range check error was in place

            suppRemains = (suppFWPerHead > GrazType.VerySmall);                           // Compute relative quality of supplement (if present)
            if (suppRemains)                                                                           
            {
                suppRelQ = Math.Min(theAnimals.AParams.GrazeC[14],
                                   1.0 - theAnimals.AParams.GrazeC[3] * (theAnimals.AParams.GrazeC[1] - theAnimals.FIntakeSupp.DMDigestibility));

                if (theAnimals.LactStatus == GrazType.LactType.Lactating)
                    milkFactor = theAnimals.AParams.GrazeC[15] * Math.Exp(-StdMath.Sqr(theAnimals.DaysLactating / theAnimals.AParams.GrazeC[8]));
                else
                    milkFactor = 0.0;

                OMD_Supp = Math.Min(1.0, 1.05 * theAnimals.FIntakeSupp.DMDigestibility - 0.01);
                if (OMD_Supp > 0.0)
                    proteinFactor = theAnimals.AParams.GrazeC[16] * StdMath.RAMP(theAnimals.FIntakeSupp.CrudeProt / OMD_Supp, theAnimals.AParams.GrazeC[9], theAnimals.AParams.GrazeC[10]);
                else
                    proteinFactor = 0.0;

                substSuppRelQ = suppRelQ - milkFactor - proteinFactor;
            }
            else
            {
                suppRelQ = 0.0;
                substSuppRelQ = 0.0;
            }

            fillRemaining = theAnimals.Start_FU;

            if (suppRemains && (feedSuppFirst || (totalFeed <= GrazType.VerySmall)))         
            {
                // Case where supplement is fed first
                EatSupplement(theAnimals, timeStepLength, suppDWPerHead, theAnimals.FIntakeSupp, suppRelQ, true, ref suppRelIntake, ref fillRemaining);
                theAnimals.Start_FU = fillRemaining;
                suppRemains = false;
            }

            if (totalFeed > GrazType.VerySmall)                                             
            {
                // Case where there is pasture available to the animals
                classIdx = 1;
                while ((classIdx <= GrazType.DigClassNo + 1) && (fillRemaining >= GrazType.VerySmall))
                {
                    suppEntry = Math.Min(1.0, 0.5 + (substSuppRelQ - relQ[classIdx])
                                                   / (CLASSWIDTH * theAnimals.AParams.GrazeC[3]));
                    if (suppRemains && (suppEntry > 0.0))
                    {
                        // This gives a continuous response to changes in supplement DMD
                        this.EatPasture(theAnimals, (1.0 - suppEntry) * availFeed[classIdx], totalFeed, heightRatio[classIdx], relQ[classIdx], ref relIntake[classIdx], ref fillRemaining);
                        this.EatSupplement(theAnimals, timeStepLength, suppDWPerHead, theAnimals.FIntakeSupp, suppRelQ, false, ref suppRelIntake, ref fillRemaining);
                        this.EatPasture(theAnimals, suppEntry * availFeed[classIdx], totalFeed, heightRatio[classIdx], relQ[classIdx], ref relIntake[classIdx], ref fillRemaining);

                        suppRemains = false;
                    }
                    else
                        this.EatPasture(theAnimals, 
                                    availFeed[classIdx], 
                                    totalFeed,
                                    heightRatio[classIdx], 
                                    relQ[classIdx],
                                    ref relIntake[classIdx], 
                                    ref fillRemaining);
                    classIdx++;
                }

                // Still supplement left? 
                if (suppRemains)                                                                          
                    this.EatSupplement(theAnimals, timeStepLength, suppDWPerHead, theAnimals.FIntakeSupp, suppRelQ, false, ref suppRelIntake, ref fillRemaining);

                legumeAdjust = theAnimals.AParams.GrazeC[2] * StdMath.Sqr(1.0 - fillRemaining) * legume;         // Adjustment to intake rate for waterlogging and legume content        
                for (classIdx = 1; classIdx <= GrazType.DigClassNo; classIdx++)                                                
                    relIntake[classIdx] = relIntake[classIdx] * waterLogScalar * (1.0 + legumeAdjust);
            }

            for (classIdx = 1; classIdx <= GrazType.DigClassNo; classIdx++)                                                                    
            {
                // Distribute relative intakes between herbage and seed
                herbageRI[classIdx] = relIntake[classIdx] * StdMath.XDiv(theAnimals.Inputs.Herbage[classIdx].Biomass, availFeed[classIdx]);
            }

            for (speciesIdx = 1; speciesIdx <= GrazType.MaxPlantSpp; speciesIdx++)
            {
                for (ripeIdx = GrazType.UNRIPE; ripeIdx <= GrazType.RIPE; ripeIdx++)
                {
                    classIdx = theAnimals.Inputs.SeedClass[speciesIdx, ripeIdx];
                    if ((classIdx > 0) && (theAnimals.Inputs.Seeds[speciesIdx, ripeIdx].Biomass > GrazType.VerySmall))
                        seedRI[speciesIdx, ripeIdx] = relIntake[classIdx] * theAnimals.Inputs.Seeds[speciesIdx, ripeIdx].Biomass / availFeed[classIdx];
                }
            }
        }

        /// <summary>
        /// Feasible range of weights for a given age and (relative) body condition   
        /// This weight range is a consequence of the normal weight function          
        /// (AnimalGroup.NormalWeightFunc)                                           
        /// </summary>
        /// <param name="reprod"></param>
        /// <param name="ageDays">Age in days</param>
        /// <param name="bodyCond">Body condition</param>
        /// <param name="paramSet">Animal params</param>
        /// <param name="lowBaseWt"></param>
        /// <param name="highBaseWt"></param>
        public void WeightRangeForCond(GrazType.ReproType reprod,
                                      int ageDays,
                                      double bodyCond,
                                      AnimalParamSet paramSet,
                                      ref double lowBaseWt,
                                      ref double highBaseWt)
        {
            double maxNormWt;

            maxNormWt = GrowthCurve(ageDays, reprod, paramSet);
            highBaseWt = bodyCond * maxNormWt;
            if (bodyCond >= 1.0)
                lowBaseWt = highBaseWt;
            else
                lowBaseWt = highBaseWt * paramSet.GrowthC[3] / (1.0 - bodyCond * (1.0 - paramSet.GrowthC[3]));
        }

        /// <summary>
        /// Chill index
        /// </summary>
        /// <param name="temp">Temperature value</param>
        /// <param name="wind">Wind speed</param>
        /// <param name="precip">The rainfall</param>
        /// <returns>Chill value</returns>
        private double ChillFunc(double temp, double wind, double precip)
        {
            return 481.0 + (11.7 + 3.1 * Math.Sqrt(wind)) * (40.0 - temp)
                               + 418 * (1.0 - Math.Exp(-0.04 * Math.Min(80, precip)));
        }
    }
    #endregion AnimalGroup

    #region AnimalList
    /// <summary>
    /// The animal list of animal groups
    /// </summary>
    public class AnimalList : List<AnimalGroup>
    {
        /// <summary>
        /// Days of weight gain
        /// </summary>
        public const int GAINDAYCOUNT = 28;       // for the AnimalList
        
        /// <summary>
        /// Keep count of how many valid entries have been made
        /// </summary>
        private int FValidGainsCount;

        /// <summary>
        /// Weight gain information
        /// </summary>
        private double[] FGains = new double[GAINDAYCOUNT - 1];

        /// <summary>
        /// Get the animal group at an index
        /// </summary>
        /// <param name="posn">Position index</param>
        /// <returns>The Animal group</returns>
        private AnimalGroup GetAt(int posn)
        {
            return base[posn];
        }
        
        /// <summary>
        /// Random number container
        /// </summary>
        public MyRandom RandFactory;
        
        /// <summary>
        /// Copy an AnimalList
        /// </summary>
        /// <returns></returns>
        public AnimalList Copy()
        {
            int I;

            AnimalList Result = new AnimalList();
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
        /// Remove empty AnimalGroups and unite similar ones
        /// </summary>
        public void Merge()
        {
            AnimalGroup AG;
            int i, j;

            for (i = 0; i <= Count - 1; i++)                                                  // Remove empty groups                      
            {
                if ((At(i) != null) && (At(i).NoAnimals == 0))
                {
                    SetAt(i, null);
                }
                else
                {
                    for (j = i + 1; j <= Count - 1; j++)
                        if ((At(i) != null) && (At(j) != null) && At(i).Similar(At(j)))     // Merge similar groups                     
                        {
                            AG = At(j);
                            this[j] = null;
                            At(i).Merge(ref AG);
                        }
                }
            }
            throw new Exception("Pack() not implemented int AnimalList.Merge() yet!");
        }

        /// <summary>
        /// Get the animal group at this position
        /// </summary>
        /// <param name="posn">Position index</param>
        /// <returns>The Animal group</returns>
        public AnimalGroup At(int posn)
        {
            return this.GetAt(posn);
        }

        /// <summary>
        /// Set the animal group at this position
        /// </summary>
        /// <param name="posn">Position index</param>
        /// <param name="animalGrp">The Animal group</param>
        public void SetAt(int posn, AnimalGroup animalGrp)
        {
            base[posn] = animalGrp;
        }

        /// <summary>
        /// Gets the days of weight gain
        /// </summary>
        public int ValidGainDays
        {
            get { return this.FValidGainsCount; }
        }

        /// <summary>
        /// Add a daily weight gain value in kg. Uses an array as a cheap fifo queue.
        /// Use gain = MISSING when a value is unavailable.
        /// </summary>
        /// <param name="gain"></param>
        public void AddWtGain(double gain)
        {
            // shuffle values down and drop the last one off
            for (int i = GAINDAYCOUNT - 1; i >= 1; i--)
                this.FGains[i] = this.FGains[i - 1];
            this.FGains[0] = gain;                   // most recent is at [0]
            if (gain == StdMath.DMISSING)
                this.FValidGainsCount = 0;           // reset
            else
                this.FValidGainsCount++;
        }

        /// <summary>
        /// Calc the average weight gain over the last number of days.
        /// </summary>
        /// <param name="days">Number days</param>
        /// <returns>Average weight gain</returns>
        public double AvGainOver(int days)
        {
            int i;
            double sum;
            int count;

            sum = 0;
            if (days > GAINDAYCOUNT)
                days = GAINDAYCOUNT;
            count = 0;                         // keep iCount in case there are less days available
            for (i = 0; i <= days - 1; i++)
            {
                if (FGains[i] != StdMath.DMISSING)
                {
                    sum = sum + FGains[i];
                    count++;
                }
            }
            return sum / count;
        }
    }
    #endregion AnimalList
}