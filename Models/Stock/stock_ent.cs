// -----------------------------------------------------------------------
// <copyright file="stock_ent.cs" company="CSIRO">
// CSIRO Agriculture & Food
// </copyright>
// -----------------------------------------------------------------------
using System;
using System.Collections.Generic;
using StdUnits;

namespace Models.GrazPlan
{

    /// <summary>
    /// Enterprise type init
    /// </summary>
    [Serializable]
    public class EnterpriseAgeInfo
    {
        /// <summary>
        /// Age description
        /// </summary>
        public string AgeDescr;
        /// <summary>
        /// The tag number
        /// </summary>
        public int TagNumber;
    }

    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class TagFlock
    {
        /// <summary>
        /// Mob description
        /// </summary>
        public string MobDescr;
        /// <summary>
        /// Is male
        /// </summary>
        public bool Male;
        /// <summary>
        /// age lamb,weaner, x-n
        /// </summary>
        public EnterpriseAgeInfo[] Ages;
    }

    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class Reproduction
    {
        /// <summary>
        /// Mating day
        /// </summary>
        public string MateDay;
        /// <summary>
        /// Mating age
        /// </summary>
        public int MateAge;
        /// <summary>
        /// Conception rates
        /// </summary>
        public double[] Conception;
        /// <summary>
        /// Do castrate
        /// </summary>
        public bool Castrate;
        /// <summary>
        /// Weaning day
        /// </summary>
        public string WeanDay;
        /// <summary>
        /// Weaning age
        /// </summary>
        public int WeanAge;
        /// <summary>
        /// 
        /// </summary>
        public int[] MateTags;
        /// <summary>
        /// 
        /// </summary>
        public int JoinedTag;
        /// <summary>
        /// 
        /// </summary>
        public int DryTag;
        /// <summary>
        /// 
        /// </summary>
        public int WeanerMaleTag;
        /// <summary>
        /// 
        /// </summary>
        public int WeanerFemaleTag;
        /// <summary>
        /// 
        /// </summary>
        public double MaleRatio;
        /// <summary>
        /// 
        /// </summary>
        public double KeepMales;
        /// <summary>
        /// ausfarm unique
        /// </summary>
        public string MateWith;
    }

    /// <summary>
    /// The initial state of the Enterprise
    /// </summary>
    [Serializable]
    public class EnterpriseInfo
    {
#pragma warning disable 1591 //missing xml comment
        public const int FIXEDPERIOD = 0;
        public const int FLEXIBLEPERIOD = 1;

        public const int PERIOD_COUNT = 2;
        public static string[] PERIOD_TEXT = new string[2] { "Fixed", "Flexible" };

        public const int MINWEANAGE = 60;                                               // Minimum age at weaning                 
        public const int EWEGESTATION = 150 - 1;                                        // One less than the actual gestation     
        public const int COWGESTATION = 285 - 1;                                        // One less than the actual gestation     

        //used for value of 'weight_gain' when this type is unused
        public const double INVALID_WTGAIN = -999.0;  //see greplace.pas unit
        public const int WETHER = 0;
        public const int EWEWETHER = 1;
        public const int STEERS_COWS = 2;
        public const int BEEF = 3;
        public const int LAMBS = 4;

        public const int ENT_MAXIDX = 4;
#pragma warning restore 1591 //missing xml comment
        /// <summary>
        /// The stock enterprise type names
        /// This should parallel the TStockEnterprise enumeration 
        /// </summary>
        public static string[] ENT = new string[ENT_MAXIDX + 1] { "Wether", "Ewe & Wether", "Cattle", "Beef Cow", "Lambs" };
        /// <summary>
        /// Enterprise type
        /// </summary>
        public enum StockEnterprise
        {
            /// <summary>
            /// Wether
            /// </summary>
            entWether,
            /// <summary>
            /// Ewes and wethers
            /// </summary>
            entEweWether,
            /// <summary>
            /// Steers
            /// </summary>
            entSteer,
            /// <summary>
            /// Beef cow breeding
            /// </summary>
            entBeefCow,
            /// <summary>
            /// Trading lambs
            /// </summary>
            entLamb
        };
        /// <summary>
        /// Get the enterprise type from the name
        /// </summary>
        /// <param name="className"></param>
        /// <returns></returns>
        public StockEnterprise EntTypeFromName(string className)
        {
            if (String.Compare(className, ENT[WETHER], true) == 0)
                return StockEnterprise.entWether;
            else if (String.Compare(className, ENT[EWEWETHER], true) == 0)
                return StockEnterprise.entEweWether;
            else if (String.Compare(className, ENT[STEERS_COWS], true) == 0)
                return StockEnterprise.entSteer;
            else if (String.Compare(className, ENT[BEEF], true) == 0)
                return StockEnterprise.entBeefCow;
            else if (String.Compare(className, ENT[LAMBS], true) == 0)
                return StockEnterprise.entLamb;
            else
                return StockEnterprise.entWether; // Use as the default if no match was found
        }

        /// <summary>
        /// Enterprise type
        /// </summary>
        public string EntClass { get; set; }
        /// <summary>
        /// Is cattle
        /// </summary>
        public bool IsCattle
        {
            get { return ((EntTypeFromName(EntClass) == StockEnterprise.entSteer) || (EntTypeFromName(EntClass) == StockEnterprise.entBeefCow)); }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool ManageReproduction { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public bool ManageGrazing { get; set; }

        /// <summary>
        /// doy
        /// </summary>
        public string tag_update_day;

        /// <summary>
        /// mob - sex,breeding
        /// </summary>
        public TagFlock[] tag_flock;

        /// <summary>
        /// Mating day
        /// </summary>
        public int MateDay
        {
            get { return AsStdDate(reproduction.MateDay); }
            set { reproduction.MateDay = SetFromStdDate(value); }
        }
        /// <summary>
        /// Mating age in years
        /// </summary>
        public int MateYears
        {
            get { return reproduction.MateAge; }
            set { reproduction.MateAge = value; }
        }
        /// <summary>
        /// Mate with genotype
        /// </summary>
        public string MateWith
        {
            get { return reproduction.MateWith; }
            set { reproduction.MateWith = value; }
        }
        /// <summary>
        /// Do castrate
        /// </summary>
        public bool Castrate
        {
            get { return reproduction.Castrate; }
            set { reproduction.Castrate = value; }
        }
        /// <summary>
        /// Weaning day
        /// </summary>
        public int WeanDay
        {
            get { return AsStdDate(reproduction.WeanDay); }
            set { reproduction.WeanDay = SetFromStdDate(value); }
        }
        /// <summary>
        /// Count of tags mated
        /// </summary>
        public int MateTagCount
        {
            get { return reproduction.MateTags.Length; }
            set { Array.Resize(ref reproduction.MateTags, value); }
        }
        /// <summary>
        /// Get the mating tag at idx
        /// </summary>
        /// <param name="idx">1-n</param>
        /// <returns></returns>
        public int GetMateTag(int idx)
        {
            int result = 0;

            if (reproduction.MateTags.Length >= idx)
                result = reproduction.MateTags[idx - 1];
            return result;
        }

        /// <summary>
        /// Joined tag
        /// </summary>
        public int JoinedTag
        {
            get { return reproduction.JoinedTag; }
            set { reproduction.JoinedTag = value; }
        }
        /// <summary>
        /// Drying off tag
        /// </summary>
        public int DryTag
        {
            get { return reproduction.DryTag; }
            set { reproduction.DryTag = value; }
        }
        /// <summary>
        /// Weaner female tag
        /// </summary>
        public int WeanerFTag
        {
            get { return reproduction.WeanerFemaleTag; }
            set { reproduction.WeanerFemaleTag = value; }
        }
        /// <summary>
        /// Weaner male tag
        /// </summary>
        public int WeanerMTag
        {
            get { return reproduction.WeanerMaleTag; }
            set { reproduction.WeanerMaleTag = value; }
        }

        /// <summary>
        /// Determine if this Enterprise uses this tag number to specify an animal group.
        /// </summary>
        /// <param name="tagNo"></param>
        /// <returns></returns>
        public bool ContainsTag(int tagNo)
        {
            int mob, agegrp;
            bool found;
            TagFlock mobItem;

            found = false;
            mob = 1;
            while (!found && (mob <= tag_flock.Length))
            {
                mobItem = tag_flock[mob - 1];
                agegrp = 1;
                while (!found && (agegrp <= mobItem.Ages.Length))
                {
                    if (mobItem.Ages[agegrp - 1].TagNumber == tagNo)
                        found = true;
                    agegrp++;
                }
                mob++;
            }
            return found;
        }

        /// <summary>
        /// Reproduction object
        /// </summary>
        public Reproduction reproduction;

        /// <summary>
        /// Get the string date as a std date value
        /// </summary>
        /// <param name="strDay">Date string</param>
        /// <returns>The std date value</returns>
        protected int AsStdDate(string strDay)
        {
            int doy = 0;    // doy = decimal stddate

            // convert the day to the day number of the year 
            if (!StdStrng.TokenDate(ref strDay, ref doy))
            {
                doy = 0;
            }
            return doy;

        }
        /// <summary>
        /// Get the string of a std date (integer). The string form is 'dd mmm'
        /// </summary>
        /// <param name="value">Std date value</param>
        /// <returns>The string D mmm</returns>
        protected string SetFromStdDate(int value)
        {
            if (value != 0)
                return StdDate.DateStrFmt(value, "D mmm");
            else
                return String.Empty;
        }
    }

    /// <summary>
    /// A period of grazing. Could be flexible or fixed dates
    /// </summary>
    [Serializable]
    public class GrazingPeriod
    {
        /// <summary>
        /// 
        /// </summary>
        public string StartDay;
        /// <summary>
        /// 
        /// </summary>
        public string FinishDay;
        /// <summary>
        /// 
        /// </summary>
        public string Descr;
        /// <summary>
        /// fixed/flexible
        /// </summary>
        public string type;
        /// <summary>
        /// 
        /// </summary>
        public int CheckEvery;
        /// <summary>
        /// cover / dm / draft
        /// </summary>
        public string test;
        /// <summary>
        /// 
        /// </summary>
        [Serializable]
        public class TagPaddock
        {
            /// <summary>
            /// 
            /// </summary>
            public int tag_no;
            /// <summary>
            /// 
            /// </summary>
            public int[] paddock;
        }
        /// <summary>
        /// used for flexible
        /// </summary>
        public TagPaddock[] tag_list;

        /// <summary>
        /// 
        /// </summary>
        [Serializable]
        public class TagIndex
        {
            /// <summary>
            /// 
            /// </summary>
            public int index;
            /// <summary>
            /// 
            /// </summary>
            public int[] tag_no;

        }
        /// <summary>
        /// used for fixed
        /// </summary>
        public TagIndex[] paddock_list;
    }

    /// <summary>
    /// List of grazing periods
    /// </summary>
    [Serializable]
    public class GrazingList
    {
        private List<GrazingPeriod> grazingList = new List<GrazingPeriod>();
        /// <summary>
        /// Count of periods
        /// </summary>
        /// <returns></returns>
        public int Count()
        {
            return grazingList.Count;
        }

        /// <summary>
        /// Check the paddock every x days
        /// </summary>
        /// <param name="periodIdx"></param>
        /// <returns></returns>
        public int GetMoveCheck(int periodIdx)
        {
            return grazingList[periodIdx - 1].CheckEvery;
        }

        /// <summary>
        /// Get the count of paddocks in the tag list
        /// </summary>
        /// <param name="periodIdx">1-n</param>
        /// <param name="idx">1-n</param>
        /// <returns></returns>
        public int GetTagPaddocks(int periodIdx, int idx)
        {
            int result = 0;
            if (grazingList[periodIdx - 1].tag_list.Length >= idx)
                result = grazingList[periodIdx - 1].tag_list[idx - 1].paddock.Length;
            return result;
        }

        /// <summary>
        /// Get the tag item for the grazing period
        /// </summary>
        /// <param name="periodIdx"></param>
        /// <param name="idx"></param>
        /// <returns></returns>
        public int GetTag(int periodIdx, int idx)
        {
            int result = 0;
            if (grazingList[periodIdx - 1].tag_list.Length >= idx)
                result = grazingList[periodIdx - 1].tag_list[idx - 1].tag_no;
            return result;
        }

        /// <summary>
        /// Get the count of tag items in the list
        /// </summary>
        /// <param name="periodIdx"></param>
        /// <returns></returns>
        public int GetTagCount(int periodIdx)
        {
            return grazingList[periodIdx - 1].tag_list.Length;
        }
        /// <summary>
        /// Get grazing criteria
        /// </summary>
        /// <param name="periodIdx"></param>
        /// <returns></returns>
        public string GetCriteria(int periodIdx)
        {
            return grazingList[periodIdx - 1].test;
        }

        /// <summary>
        /// Get the finish day
        /// </summary>
        /// <param name="periodIdx"></param>
        /// <returns></returns>
        public int GetFinishDay(int periodIdx)
        {
            return AsStdDate(grazingList[periodIdx - 1].FinishDay);
        }

        /// <summary>
        /// Get the number of paddocks
        /// </summary>
        /// <param name="periodIdx"></param>
        /// <returns></returns>
        public int GetFixedPaddCount(int periodIdx)
        {
            return grazingList[periodIdx - 1].paddock_list.Length;
        }
        /// <summary>
        /// Get the paddock from the list
        /// </summary>
        /// <param name="periodIdx"></param>
        /// <param name="idx"></param>
        /// <returns></returns>
        public int GetFixedPadd(int periodIdx, int idx)
        {
            int result = -1;
            if ((grazingList[periodIdx - 1].paddock_list.Length > 0) && (grazingList[periodIdx - 1].paddock_list.Length >= idx))
                result = grazingList[periodIdx - 1].paddock_list[idx - 1].index;
            return result;
        }

        /// <summary>
        /// Get the count of tags in the paddocks
        /// </summary>
        /// <param name="periodIdx"></param>
        /// <param name="idx"></param>
        /// <returns></returns>
        public int GetFixedPaddTagCount(int periodIdx, int idx)
        {
            return grazingList[periodIdx - 1].paddock_list[idx - 1].tag_no.Length;
        }

        /// <summary>
        /// Get the tag from paddock
        /// </summary>
        /// <param name="periodIdx"></param>
        /// <param name="idx"></param>
        /// <param name="tagidx"></param>
        /// <returns></returns>
        public int GetFixedPaddTag(int periodIdx, int idx, int tagidx)
        {
            int result = 0;
            int len = grazingList[periodIdx - 1].paddock_list[idx - 1].tag_no.Length;
            if ((len > 0) && (len >= tagidx))
                result = grazingList[periodIdx - 1].paddock_list[idx - 1].tag_no[tagidx - 1];
            return result;
        }

        /// <summary>
        /// Get the grazing period type
        /// </summary>
        /// <param name="periodIdx"></param>
        /// <returns></returns>
        public string GetPeriodType(int periodIdx)
        {
            return grazingList[periodIdx - 1].type;
        }

        /// <summary>
        /// StartDay[1..n]
        /// </summary>
        /// <param name="periodIdx"></param>
        /// <returns></returns>
        public int GetStartDay(int periodIdx)
        {
            return AsStdDate(grazingList[periodIdx - 1].StartDay);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="periodIdx"></param>
        /// <param name="idx"></param>
        /// <param name="paddIdx"></param>
        /// <returns></returns>
        public int GetPaddock(int periodIdx, int idx, int paddIdx)
        {
            int result = 0;
            if (grazingList[periodIdx - 1].tag_list.Length >= idx)
            {
                if (grazingList[periodIdx - 1].tag_list[idx - 1].paddock.Length >= paddIdx)
                {
                    result = grazingList[periodIdx - 1].tag_list[idx - 1].paddock[paddIdx - 1];
                }
            }
            return result;
        }

        /// <summary>
        /// "dd mmm" -> StdDate
        /// </summary>
        /// <param name="strDay"></param>
        /// <returns></returns>
        protected int AsStdDate(string strDay)
        {
            int doy = 0;

            // convert the day to the day number of the year
            if (!StdStrng.TokenDate(ref strDay, ref doy)) // doy = decimal stddate
                doy = 0;
            return doy;

        }

    }
}