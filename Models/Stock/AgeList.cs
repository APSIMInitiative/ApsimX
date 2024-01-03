using System;
using System.Globalization;
using StdUnits;

namespace Models.GrazPlan
{

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
}
