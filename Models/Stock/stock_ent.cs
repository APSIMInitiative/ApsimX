using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CMPServices;
using StdUnits;

namespace Models.GrazPlan
{
    /// <summary>
    /// Shear by tag details
    /// </summary>
    [Serializable]
    public struct TShearByTag
    {
        /// <summary>
        /// Tag number
        /// </summary>
        public int tagNo;
        /// <summary>
        /// Day of year
        /// </summary>
        public int doy;
    }

    /// <summary>
    /// Replacement specifics
    /// </summary>
    [Serializable]
    public class TStockReplacement
    {
        /// <summary>
        /// Purchase animals
        /// </summary>
        public bool purchase;
        /// <summary>
        /// Sex of animals
        /// </summary>
        public string sex;
        /// <summary>
        /// Age in days
        /// </summary>
        public int age;
        /// <summary>
        /// Weight in kg
        /// </summary>
        public double weight;
        /// <summary>
        /// Condition score
        /// </summary>
        public double condition;
        /// <summary>
        /// Is mated
        /// </summary>
        public bool mated;
        /// <summary>
        /// Tag number
        /// </summary>
        public int tag_no;

    }

    /// <summary>
    /// Sale and replace details for an animal group cohort
    /// </summary>
    [Serializable]
    public class TSaleReplace
    {
        /// <summary>
        /// First day for sale
        /// </summary>
        public string sell_first_day;
        /// <summary>
        /// Last day for sale
        /// </summary>
        public string sell_last_day;
        /// <summary>
        /// 
        /// </summary>
        public int sell_first_years;
        /// <summary>
        /// 
        /// </summary>
        public int sell_last_years;
        /// <summary>
        /// Selling weight
        /// </summary>
        public double sell_weight;
        /// <summary>
        /// Replacement day 
        /// </summary>
        public string replace_day;

        /// <summary>
        /// Replacement requirements
        /// </summary>
        public TStockReplacement replace_stock;
        /// <summary>
        /// Weight gain limit for sale
        /// </summary>
        public double weight_gain;
        /// <summary>
        /// Days to average the weight gain over
        /// </summary>
        public int gain_days;
    }

    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class TYoungSales
    {
        /// <summary>
        /// 
        /// </summary>
        public string first_day;
        /// <summary>
        /// 
        /// </summary>
        public string last_day;
        /// <summary>
        /// 
        /// </summary>
        public int first_years;
        /// <summary>
        /// 
        /// </summary>
        public int last_years;
        /// <summary>
        /// 
        /// </summary>
        public double weight;
        /// <summary>
        /// 
        /// </summary>
        public double weight_gain;
        /// <summary>
        /// 
        /// </summary>
        public int gain_days;
        /// <summary>
        /// 
        /// </summary>
        public int[] young_tags;
    }

    /// <summary>
    /// Enterprise type init
    /// </summary>
    [Serializable]
    public class TAgeInfo
    {
        /// <summary>
        /// 
        /// </summary>
        public string age_descr;
        /// <summary>
        /// 
        /// </summary>
        public int tag_no;
    }

    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class TTagFlock
    {
        /// <summary>
        /// 
        /// </summary>
        public string mob_descr;
        /// <summary>
        /// 
        /// </summary>
        public bool male;
        /// <summary>
        /// age lamb,weaner, x-n
        /// </summary>
        public TAgeInfo[] ages;
    }

    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class TShearingInfo
    {
        /// <summary>
        /// doy
        /// </summary>
        public string shearing_day;
        /// <summary>
        /// Each tag can be shorn on a date
        /// </summary>
        public TShearByTag[] shear_by_tag;
    }

    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class TReproduction
    {
        /// <summary>
        /// 
        /// </summary>
        public string mate_day;
        /// <summary>
        /// 
        /// </summary>
        public int mate_age;
        /// <summary>
        /// 
        /// </summary>
        public double[] conception;
        /// <summary>
        /// 
        /// </summary>
        public bool castrate;
        /// <summary>
        /// 
        /// </summary>
        public string wean_day;
        /// <summary>
        /// 
        /// </summary>
        public int wean_age;
        /// <summary>
        /// 
        /// </summary>
        public int[] mate_tags;
        /// <summary>
        /// 
        /// </summary>
        public int joined_tag;
        /// <summary>
        /// 
        /// </summary>
        public int dry_tag;
        /// <summary>
        /// 
        /// </summary>
        public int weanerM_tag;
        /// <summary>
        /// 
        /// </summary>
        public int weanerF_tag;
        /// <summary>
        /// 
        /// </summary>
        public TYoungSales[] young_sales;
        /// <summary>
        /// 
        /// </summary>
        public double male_ratio;
        /// <summary>
        /// 
        /// </summary>
        public double keep_males;
        /// <summary>
        /// ausfarm unique
        /// </summary>
        public string mate_with;
    }

    /// <summary>
    /// The initial state of the Enterprise
    /// </summary>
    [Serializable]
    public class TEnterpriseInfo
    {
#pragma warning disable 1591 //missing xml comment
        public const int FIXEDPERIOD = 0;
        public const int FLEXIBLEPERIOD = 1;

        public const int PERIOD_COUNT = 2;
        public static string[] PERIOD_TEXT = new string[2] { "Fixed", "Flexible" };


        public const int MOB_MAIN = 1;
        public const int MOB_YOUNG = 2;
        public const int MOB_FEM = 1;
        public const int MOB_MALE = 2;
        public const int PURE = 1;
        public const int XBRED = 2;

        public const int MINWEANAGE = 60;                                               // Minimum age at weaning                 
        public const int EWEGESTATION = 150 - 1;                                        // One less than the actual gestation     
        public const int COWGESTATION = 285 - 1;                                        // One less than the actual gestation     

        //used for value of 'weight_gain' when this type is unused
        public const double INVALID_WTGAIN = -999.0;  //see greplace.pas unit
        public const int WETHER = 0;
        public const int EWEWETHER = 1;
        public const int STEER = 2;
        public const int BEEF = 3;
        public const int LAMBS = 4;

        public const int ENT_MAXIDX = 4;
#pragma warning restore 1591 //missing xml comment
        /// <summary>
        /// The stock enterprise type names
        /// This should parallel the TStockEnterprise enumeration 
        /// </summary>
        public static string[] ENT = new string[ENT_MAXIDX + 1] { "Wether", "Ewe & Wether", "Steer", "Beef Cow", "Lambs" };
        /// <summary>
        /// Enterprise type
        /// </summary>
        public enum TStockEnterprise
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
        public TStockEnterprise EntTypeFromName(string className)
        {
            if (String.Compare(className, ENT[WETHER], true) == 0)
                return TStockEnterprise.entWether;
            else if (String.Compare(className, ENT[EWEWETHER], true) == 0)
                return TStockEnterprise.entEweWether;
            else if (String.Compare(className, ENT[STEER], true) == 0)
                return TStockEnterprise.entSteer;
            else if (String.Compare(className, ENT[BEEF], true) == 0)
                return TStockEnterprise.entBeefCow;
            else if (String.Compare(className, ENT[LAMBS], true) == 0)
                return TStockEnterprise.entLamb;
            else
                return TStockEnterprise.entWether; // Use as the default if no match was found
        }
        /// <summary>
        /// Set the tag of an animal group
        /// </summary>
        /// <param name="mob"></param>
        /// <param name="ageidx"></param>
        /// <param name="value"></param>
        public void setTag(int mob, int ageidx, int value)
        {
            if (mob > tag_flock.Length)
                Array.Resize(ref tag_flock, mob);

            if (ageidx > tag_flock[mob - 1].ages.Length)
                Array.Resize(ref tag_flock[mob - 1].ages, ageidx);

            tag_flock[mob - 1].ages[ageidx - 1].tag_no = value;
        }

        /// <summary>
        /// elements are indexed 1 -> n
        /// </summary>
        /// <param name="mob">1-n</param>
        /// <param name="ageidx">1-n</param>
        /// <returns></returns>
        public int getTag(int mob, int ageidx)
        {
            int result = 0;
            if (tag_flock.Length >= mob)
            {
                if (tag_flock[mob - 1].ages.Length >= ageidx)
                    result = tag_flock[mob - 1].ages[ageidx - 1].tag_no;
            }
            return result;
        }

        /// <summary>
        /// Get the first day of sale
        /// </summary>
        /// <param name="iReplaceNo">1-n</param>
        /// <returns></returns>
        public int getFirstSaleDay(int iReplaceNo)
        {
            return asStdDate(replacement[iReplaceNo - 1].sell_first_day);

        }

        /// <summary>
        /// Set the first day of sale
        /// </summary>
        /// <param name="iReplaceNo">1-n</param>
        /// <param name="Value"></param>
        public void setFirstSaleDay(int iReplaceNo, int Value)
        {
            replacement[iReplaceNo - 1].sell_first_day = setFromStdDate(Value);
        }

        /// <summary>
        /// Get the first year of sale
        /// </summary>
        /// <param name="iReplaceNo">1-n</param>
        /// <returns></returns>
        public int getFirstSaleYears(int iReplaceNo)
        {
            return replacement[iReplaceNo - 1].sell_first_years;
        }

        /// <summary>
        /// Set the first year of sale
        /// </summary>
        /// <param name="iReplaceNo">1-n</param>
        /// <param name="Value"></param>
        public void setFirstSaleYears(int iReplaceNo, int Value)
        {
            replacement[iReplaceNo - 1].sell_first_years = Value;
        }
        /// <summary>
        /// Is this purchased
        /// </summary>
        /// <param name="iReplaceNo"></param>
        /// <returns></returns>
        public bool getPurchase(int iReplaceNo)
        {
            return replacement[iReplaceNo - 1].replace_stock.purchase;
        }

        /// <summary>
        /// Set this is purchased
        /// </summary>
        /// <param name="iReplaceNo"></param>
        /// <param name="Value"></param>
        public void setPurchase(int iReplaceNo, bool Value)
        {
            replacement[iReplaceNo - 1].replace_stock.purchase = Value;
        }

        /// <summary>
        /// Get first day of sale for young
        /// </summary>
        /// <param name="idx"></param>
        /// <returns></returns>
        public int getYoungSaleFirstDay(int idx)
        {
            return asStdDate(reproduction.young_sales[idx - 1].first_day);
        }

        /// <summary>
        /// Set first day of sale for young
        /// </summary>
        /// <param name="idx"></param>
        /// <param name="Value"></param>
        public void setYoungSaleFirstDay(int idx, int Value)
        {
            reproduction.young_sales[idx - 1].first_day = setFromStdDate(Value);
        }

        /// <summary>
        /// Get first year of young sale
        /// </summary>
        /// <param name="idx"></param>
        /// <returns></returns>
        public int getYoungSaleFirstYears(int idx)
        {
            return reproduction.young_sales[idx - 1].first_years;
        }

        /// <summary>
        /// Set first year of young sale
        /// </summary>
        /// <param name="idx"></param>
        /// <param name="Value"></param>
        public void setYoungSaleFirstYears(int idx, int Value)
        {
            reproduction.young_sales[idx - 1].first_years = Value;
        }
        /*function getYoungSaleGainDays(const idx: integer): integer; 
        procedure setYoungSaleLastYears(const idx, Value: integer);*/
        /// <summary>
        /// Get last day of young sale
        /// </summary>
        /// <param name="idx"></param>
        /// <returns></returns>
        public int getYoungSaleLastDay(int idx)
        {
            return asStdDate(reproduction.young_sales[idx - 1].last_day);
        }

        /// <summary>
        /// Set last day of young sale
        /// </summary>
        /// <param name="idx"></param>
        /// <param name="Value"></param>
        public void setYoungSaleLastDay(int idx, int Value)
        {
            reproduction.young_sales[idx - 1].last_day = setFromStdDate(Value);
        }
        /// <summary>
        /// Get young sale weight
        /// </summary>
        /// <param name="idx"></param>
        /// <returns></returns>
        public double getYoungSaleWeight(int idx)
        {
            return reproduction.young_sales[idx - 1].weight;
        }
        /// <summary>
        /// Set the young sale weight
        /// </summary>
        /// <param name="idx"></param>
        /// <param name="Value"></param>
        public void setYoungSaleWeight(int idx, double Value)
        {
            reproduction.young_sales[idx - 1].weight = Value;
        }
        /// <summary>
        /// Get young sale weight gain
        /// if YoungSaleWeightGain = -999 => not used
        /// </summary>
        /// <param name="idx"></param>
        /// <returns></returns>
        public double getYoungSaleWtGain(int idx)
        {
            return reproduction.young_sales[idx - 1].weight_gain;
        }

        /// <summary>
        /// Set young sale weight gain
        /// </summary>
        /// <param name="idx"></param>
        /// <param name="Value"></param>
        public void setYoungSaleWtGain(int idx, double Value)
        {
            reproduction.young_sales[idx - 1].weight_gain = Value;
        }
        /// <summary>
        /// Is the paddock stocked
        /// </summary>
        /// <param name="idx">idx 1..n</param>
        /// <returns></returns>
        public bool getStockedPad(int idx)
        {
            bool result = false;
            if (stocked.Length >= idx)
                result = stocked[idx - 1];
            return result;
        }

        /// <summary>
        /// Set this paddock as stocked
        /// </summary>
        /// <param name="idx">idx 1..n</param>
        /// <param name="Value"></param>
        public void setStockedPad(int idx, bool Value)
        {
            if (stocked.Length >= idx)
                stocked[idx - 1] = Value;
        }
        /// <summary>
        /// user entered name
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Enterprise type
        /// </summary>
        public string EntClass { get; set; }
        /// <summary>
        /// Is cattle
        /// </summary>
        public bool IsCattle
        {
            get { return ((EntTypeFromName(EntClass) == TStockEnterprise.entSteer) || (EntTypeFromName(EntClass) == TStockEnterprise.entBeefCow)); }
        }
        /// <summary>
        /// flock/herd genotype
        /// </summary>
        public string BaseGenoType { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public bool ManageInit { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public bool ManageSR { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public bool ManageShearing { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public bool ManageCFA { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public bool ManageSelling { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public bool ManageReproduction { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public bool ManageReplacement { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public bool ManageMaintFeed { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public bool ManageProdFeed { get; set; }
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
        public TTagFlock[] tag_flock;
        /// <summary>
        /// Count of stocked paddocks
        /// </summary>
        public int StockedPaddocks
        {
            get { return stocked.Length; }
            set { Array.Resize(ref stocked, value); }
        }
        /// <summary>
        /// which paddock is stocked at init time
        /// </summary>
        public bool[] stocked;
        /// <summary>
        /// Stocking rate of the females /ha
        /// </summary>
        public double StockRateFemale { get; set; }
        /// <summary>
        /// Stocking rate of the males /ha
        /// </summary>
        public double StockRateMale { get; set; }
        /// <summary>
        /// alternative to the stocked array, ha
        /// </summary>
        public double GrazedArea { get; set; }
        /// <summary>
        /// Shearing plan
        /// </summary>
        public TShearingInfo shearing;
        /// <summary>
        /// Shearing date property
        /// </summary>
        public int ShearingDate
        {
            get { return asStdDate(shearing.shearing_day); }
            set { shearing.shearing_day = setFromStdDate(value); }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="idx">idx: 1 -> n</param>
        /// <param name="shearInfo"></param>
        /// <returns></returns>
        public bool getShearingTag(int idx, ref TShearByTag shearInfo)
        {
            bool result = false;
            if (idx <= shearing.shear_by_tag.Length)
            {
                TShearByTag tagItem = shearing.shear_by_tag[idx - 1];
                shearInfo.tagNo = tagItem.tagNo;
                shearInfo.doy = tagItem.doy;
                result = true;
            }
            return result;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="idx">idx: 1 -> n</param>
        /// <param name="shearInfo"></param>
        public void setShearingTag(int idx, TShearByTag shearInfo)
        {
            if (idx <= shearing.shear_by_tag.Length)
            {
                shearing.shear_by_tag[idx - 1].tagNo = shearInfo.tagNo;
                shearing.shear_by_tag[idx - 1].doy = shearInfo.doy;
            }
        }
        /// <summary>
        /// The shearing tags count
        /// </summary>
        public int ShearingTagCount
        {
            get { return shearing.shear_by_tag.Length; }
            set { Array.Resize(ref shearing.shear_by_tag, value); }
        }
        /// <summary>
        /// Mating day
        /// </summary>
        public int MateDay
        {
            get { return asStdDate(reproduction.mate_day); }
            set { reproduction.mate_day = setFromStdDate(value); }
        }
        /// <summary>
        /// Mating age in years
        /// </summary>
        public int MateYears
        {
            get { return reproduction.mate_age; }
            set { reproduction.mate_age = value; }
        }
        /// <summary>
        /// Mate with genotype
        /// </summary>
        public string MateWith
        {
            get { return reproduction.mate_with; }
            set { reproduction.mate_with = value; }
        }
        /// <summary>
        /// Do castrate
        /// </summary>
        public bool Castrate
        {
            get { return reproduction.castrate; }
            set { reproduction.castrate = value; }
        }
        /// <summary>
        /// Weaning day
        /// </summary>
        public int WeanDay
        {
            get { return asStdDate(reproduction.wean_day); }
            set { reproduction.wean_day = setFromStdDate(value); }
        }
        /// <summary>
        /// Count of tags mated
        /// </summary>
        public int MateTagCount
        {
            get { return reproduction.mate_tags.Length; }
            set { Array.Resize(ref reproduction.mate_tags, value); }
        }
        /// <summary>
        /// Get the mating tag at idx
        /// </summary>
        /// <param name="idx">1-n</param>
        /// <returns></returns>
        public int getMateTag(int idx)
        {
            int result = 0;

            if (reproduction.mate_tags.Length >= idx)
                result = reproduction.mate_tags[idx - 1];
            return result;
        }
        /// <summary>
        /// Set the mating tag at idx
        /// </summary>
        /// <param name="idx">1-n</param>
        /// <param name="Value"></param>
        public void setMateTag(int idx, int Value)
        {
            if (reproduction.mate_tags.Length >= idx)
                reproduction.mate_tags[idx - 1] = Value;
        }
        /// <summary>
        /// Joined tag
        /// </summary>
        public int JoinedTag
        {
            get { return reproduction.joined_tag; }
            set { reproduction.joined_tag = value; }
        }
        /// <summary>
        /// Drying off tag
        /// </summary>
        public int DryTag
        {
            get { return reproduction.dry_tag; }
            set { reproduction.dry_tag = value; }
        }
        /// <summary>
        /// Weaner female tag
        /// </summary>
        public int WeanerFTag
        {
            get { return reproduction.weanerF_tag; }
            set { reproduction.weanerF_tag = value; }
        }
        /// <summary>
        /// Weaner male tag
        /// </summary>
        public int WeanerMTag
        {
            get { return reproduction.weanerM_tag; }
            set { reproduction.weanerM_tag = value; }
        }
        /// <summary>
        /// Replacement day
        /// </summary>
        public int ReplacementDay
        {
            get { return asStdDate(replacement[0].replace_day); }
            set { replacement[0].replace_day = setFromStdDate(value); }
        }
        /// <summary>
        /// Replacement age
        /// </summary>
        public int ReplaceAge
        {
            get { return replacement[0].replace_stock.age; }
            set { replacement[0].replace_stock.age = value; }
        }
        /// <summary>
        /// Replacement condition score
        /// </summary>
        public double ReplaceCond
        {
            get { return replacement[0].replace_stock.condition; }
            set { replacement[0].replace_stock.condition = value; }
        }
        /// <summary>
        /// Replacement weight
        /// </summary>
        public double ReplaceWeight
        {
            get { return replacement[0].replace_stock.weight; }
            set { replacement[0].replace_stock.weight = value; }
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
            TTagFlock mobItem;

            found = false;
            mob = 1;
            while (!found && (mob <= tag_flock.Length))
            {
                mobItem = tag_flock[mob - 1];
                agegrp = 1;
                while (!found && (agegrp <= mobItem.ages.Length))
                {
                    if (mobItem.ages[agegrp - 1].tag_no == tagNo)
                        found = true;
                    agegrp++;
                }
                mob++;
            }
            return found;
        }
        /// <summary>
        /// Day of tag updates
        /// </summary>
        public int TagUpdateDay
        {
            get { return asStdDate(tag_update_day); }
            set { tag_update_day = setFromStdDate(value); }
        }
        /// <summary>
        /// 
        /// </summary>
        public TSaleReplace[] replacement;
        /// <summary>
        /// 
        /// </summary>
        public TReproduction reproduction;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="strDay"></param>
        /// <returns></returns>
        protected int asStdDate(string strDay)
        {
            int doy = 0;

            //convert the day to the day number of the year
            if (!StdStrng.TokenDate(ref strDay, ref doy))//doy = decimal stddate
                doy = 0;
            return doy;

        }
        /// <summary>
        /// Get the string of a std date (integer). The string form is 'dd mmm'
        /// </summary>
        /// <param name="value"></param>
        protected string setFromStdDate(int value)
        {
            if (value != 0)
                return StdDate.DateStrFmt(value, "D mmm");
            else
                return String.Empty;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class TEnterpriseList
    {
        private List<TEnterpriseInfo> FEnterpriseList = new List<TEnterpriseInfo>();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ent"></param>
        public void Add(TEnterpriseInfo ent)
        {
            FEnterpriseList.Add(ent);
        }

        /// <summary>
        /// Count of enterprises
        /// </summary>
        public int Count
        {
            get { return FEnterpriseList.Count; }
            
        }
        /// <summary>
        /// Delete enterprise item 
        /// </summary>
        /// <param name="iValue">0-n</param>
        public void Delete(int iValue)
        {
            if ((FEnterpriseList.Count > iValue) && (iValue >= 0))
                FEnterpriseList.RemoveAt(iValue);
        }
        /// <summary>
        /// Get the enterprise by name
        /// </summary>
        /// <param name="sName"></param>
        /// <returns></returns>
        public TEnterpriseInfo byName(string sName)
        {
            int Idx;

            Idx = IndexOf(sName);
            if (Idx >= 0)
                return this.byIndex(Idx);
            else
                return null;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="iValue">iValue: 0->n</param>
        /// <returns></returns>
        public TEnterpriseInfo byIndex(int iValue)
        {
            return FEnterpriseList[iValue];
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sName"></param>
        /// <returns>Returns the index of the item in the list. 0-n</returns>
        public int IndexOf(string sName)
        {
            int result = Count - 1;
            while ((result >= 0) && (byIndex(result).Name.ToLower()) != sName.ToLower())
                result--;
            return result;
        }
    }

    /// <summary>
    /// A period of grazing. Could be flexible or fixed dates
    /// </summary>
    [Serializable]
    public class TGrazingPeriod
    {
        /// <summary>
        /// 
        /// </summary>
        public string start_day;
        /// <summary>
        /// 
        /// </summary>
        public string finish_day;
        /// <summary>
        /// 
        /// </summary>
        public string descr;
        /// <summary>
        /// fixed/flexible
        /// </summary>
        public string type;
        /// <summary>
        /// 
        /// </summary>
        public int check_every;
        /// <summary>
        /// cover / dm / draft
        /// </summary>
        public string test;
        /// <summary>
        /// 
        /// </summary>
        public class TTagPaddock
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
        public TTagPaddock[] tag_list;

        /// <summary>
        /// 
        /// </summary>
        public class TTagIndex
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
        public TTagIndex[] paddock_list;   
    }

    /// <summary>
    /// List of grazing periods
    /// </summary>
    [Serializable]
    public class TGrazingList
    {
        private List<TGrazingPeriod> FGrazingList = new List<TGrazingPeriod>();
        /// <summary>
        /// Count of periods
        /// </summary>
        /// <returns></returns>
        public int Count()
        {
            return FGrazingList.Count;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="iValue">0-n</param>
        public void Delete(int iValue)
        {
            if ((FGrazingList.Count() > iValue) && (iValue >= 0))
                FGrazingList.RemoveAt(iValue);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="period"></param>
        public void Add(TGrazingPeriod period)
        {
            FGrazingList.Add(period);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="idx">0-n</param>
        /// <returns></returns>
        public TGrazingPeriod byIndex(int idx)
        {
            return FGrazingList[idx];
        }

        /// <summary>
        /// Check the paddock every x days
        /// </summary>
        /// <param name="periodIdx"></param>
        /// <returns></returns>
        public int getMoveCheck(int periodIdx)
        {
            return FGrazingList[periodIdx - 1].check_every;
        }
        /// <summary>
        /// Check for drafting every x days
        /// </summary>
        /// <param name="periodIdx"></param>
        /// <param name="Value"></param>
        public void setDraftCheck(int periodIdx, int Value)
        {
            FGrazingList[periodIdx - 1].check_every = Value;
        }
        /// <summary>
        /// Get the count of paddocks in the tag list
        /// </summary>
        /// <param name="periodIdx">1-n</param>
        /// <param name="idx">1-n</param>
        /// <returns></returns>
        public int getTagPaddocks(int periodIdx, int idx)
        {
            int result = 0;
            if (FGrazingList[periodIdx - 1].tag_list.Length >= idx)
                result = FGrazingList[periodIdx - 1].tag_list[idx - 1].paddock.Length;
            return result;
        }
        /// <summary>
        /// Set the tag list count of paddocks
        /// </summary>
        /// <param name="periodIdx">1-n</param>
        /// <param name="idx">1-n</param>
        /// <param name="Value"></param>
        public void setTagPaddocks(int periodIdx, int idx, int Value)
        {
            if (FGrazingList[periodIdx - 1].tag_list.Length >= idx)
                Array.Resize(ref FGrazingList[periodIdx - 1].tag_list, Value);
        }
        /// <summary>
        /// Get the tag item for the grazing period
        /// </summary>
        /// <param name="periodIdx"></param>
        /// <param name="idx"></param>
        /// <returns></returns>
        public int getTag(int periodIdx, int idx)
        {
            int result = 0;
            if (FGrazingList[periodIdx - 1].tag_list.Length >= idx)
                result = FGrazingList[periodIdx - 1].tag_list[idx - 1].tag_no;
            return result;
        }
        /// <summary>
        /// Set the tag item for the grazing period
        /// </summary>
        /// <param name="periodIdx"></param>
        /// <param name="idx"></param>
        /// <param name="Value"></param>
        public void setTag(int periodIdx, int idx, int Value)
        {
            if (FGrazingList[periodIdx - 1].tag_list.Length >= idx)
                FGrazingList[periodIdx - 1].tag_list[idx - 1].tag_no = Value;
        }
        /// <summary>
        /// Get the count of tag items in the list
        /// </summary>
        /// <param name="periodIdx"></param>
        /// <returns></returns>
        public int getTagCount(int periodIdx)
        {
            return FGrazingList[periodIdx - 1].tag_list.Length;
        }
        /// <summary>
        /// Get grazing criteria
        /// </summary>
        /// <param name="periodIdx"></param>
        /// <returns></returns>
        public string getCriteria(int periodIdx)
        {
            return FGrazingList[periodIdx - 1].test;
        }
        /// <summary>
        /// Set the grazing criteria
        /// </summary>
        /// <param name="periodIdx"></param>
        /// <param name="Value"></param>
        public void setCriteria(int periodIdx, int Value)
        {
            FGrazingList[periodIdx - 1].test = Value.ToString();
        }
        /// <summary>
        /// Get the finish day
        /// </summary>
        /// <param name="periodIdx"></param>
        /// <returns></returns>
        public int getFinishDay(int periodIdx)
        {
            return asStdDate(FGrazingList[periodIdx - 1].finish_day);
        }
        /// <summary>
        /// Set the finish day
        /// </summary>
        /// <param name="periodIdx"></param>
        /// <param name="Value"></param>
        public void setFinish(int periodIdx, int Value)
        {
            FGrazingList[periodIdx - 1].finish_day =setFromStdDate(Value);
        }
        /// <summary>
        /// Set the number of paddocks
        /// </summary>
        /// <param name="periodIdx"></param>
        /// <param name="Value"></param>
        public void setFixedPaddCount(int periodIdx, int Value)
        {
            Array.Resize(ref FGrazingList[periodIdx - 1].paddock_list,Value);
        }
        /// <summary>
        /// Get the number of paddocks
        /// </summary>
        /// <param name="periodIdx"></param>
        /// <returns></returns>
        public int getFixedPaddCount(int periodIdx)
        {
            return FGrazingList[periodIdx - 1].paddock_list.Length;
        }
        /// <summary>
        /// Get the paddock from the list
        /// </summary>
        /// <param name="periodIdx"></param>
        /// <param name="idx"></param>
        /// <returns></returns>
        public int getFixedPadd(int periodIdx, int idx)
        {
            int result = -1;
            if ((FGrazingList[periodIdx - 1].paddock_list.Length > 0) && (FGrazingList[periodIdx - 1].paddock_list.Length >= idx))
                result = FGrazingList[periodIdx - 1].paddock_list[idx-1].index;
            return result;
        }
        /// <summary>
        /// Set the paddock in the list
        /// </summary>
        /// <param name="periodIdx"></param>
        /// <param name="idx"></param>
        /// <param name="Value"></param>
        public void setFixedPadd(int periodIdx, int idx, int Value)
        {
            if ((FGrazingList[periodIdx - 1].paddock_list.Length > 0) && (FGrazingList[periodIdx - 1].paddock_list.Length >= idx))
                FGrazingList[periodIdx - 1].paddock_list[idx - 1].index = Value;
        }

        /// <summary>
        /// Get the count of tags in the paddocks
        /// </summary>
        /// <param name="periodIdx"></param>
        /// <param name="idx"></param>
        /// <returns></returns>
        public int getFixedPaddTagCount(int periodIdx, int idx)
        {
            return FGrazingList[periodIdx - 1].paddock_list[idx-1].tag_no.Length;
        }
        /// <summary>
        /// Set the count of tags in the paddocks
        /// </summary>
        /// <param name="periodIdx"></param>
        /// <param name="idx"></param>
        /// <param name="Value"></param>
        public void setFixedPaddTagCount(int periodIdx, int idx, int Value)
        {
            Array.Resize(ref FGrazingList[periodIdx - 1].paddock_list[idx - 1].tag_no, Value);
        }
        /// <summary>
        /// Get the tag from paddock
        /// </summary>
        /// <param name="periodIdx"></param>
        /// <param name="idx"></param>
        /// <param name="tagidx"></param>
        /// <returns></returns>
        public int getFixedPaddTag(int periodIdx, int idx, int tagidx)
        {
            int result = 0;
            int len = FGrazingList[periodIdx - 1].paddock_list[idx - 1].tag_no.Length;
            if ((len > 0) && (len >= tagidx))
                result = FGrazingList[periodIdx - 1].paddock_list[idx - 1].tag_no[tagidx - 1];
            return result;
        }
        /// <summary>
        /// Set the tag in the paddock
        /// </summary>
        /// <param name="periodIdx"></param>
        /// <param name="idx"></param>
        /// <param name="tagidx"></param>
        /// <param name="Value"></param>
        public void setFixedPaddTag(int periodIdx, int idx, int tagidx, int Value)
        {
            int len = FGrazingList[periodIdx - 1].paddock_list[idx - 1].tag_no.Length;
            if ((len > 0) && (len >= tagidx))
                FGrazingList[periodIdx - 1].paddock_list[idx - 1].tag_no[tagidx - 1] = Value;
        }
        /// <summary>
        /// Get the grazing period type
        /// </summary>
        /// <param name="periodIdx"></param>
        /// <returns></returns>
        public string getPeriodType(int periodIdx)
        {
            return FGrazingList[periodIdx - 1].type;
        }
        /// <summary>
        /// Set the grazing period type
        /// </summary>
        /// <param name="periodIdx"></param>
        /// <param name="Value"></param>
        public void setPeriodType(int periodIdx, string Value)
        {
            FGrazingList[periodIdx - 1].type=Value;
        }
        /// <summary>
        /// StartDay[1..n]
        /// </summary>
        /// <param name="periodIdx"></param>
        /// <returns></returns>
        public int getStartDay(int periodIdx)
        {
            return asStdDate(FGrazingList[periodIdx - 1].start_day);
        }
        /// <summary>
        /// StartDay[1..n]
        /// </summary>
        /// <param name="periodIdx"></param>
        /// <param name="Value"></param>
        public void setStart(int periodIdx, int Value)
        {
            FGrazingList[periodIdx - 1].start_day=setFromStdDate(Value);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="periodIdx"></param>
        /// <param name="idx"></param>
        /// <param name="paddIdx"></param>
        /// <returns></returns>
        public int getPaddock(int periodIdx, int idx, int paddIdx)
        {
            int result = 0;
            if (FGrazingList[periodIdx - 1].tag_list.Length >= idx)
            {
                if (FGrazingList[periodIdx - 1].tag_list[idx-1].paddock.Length >= paddIdx)
                {
                    result = FGrazingList[periodIdx - 1].tag_list[idx - 1].paddock[paddIdx-1];
                }
            }
            return result;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="periodIdx"></param>
        /// <param name="idx"></param>
        /// <param name="paddIdx"></param>
        /// <param name="Value"></param>
        public void setPaddock(int periodIdx, int idx, int paddIdx, int Value)
        {
            if (FGrazingList[periodIdx - 1].tag_list.Length >= idx)
            {
                if (FGrazingList[periodIdx - 1].tag_list[idx - 1].paddock.Length >= paddIdx)
                {
                    FGrazingList[periodIdx - 1].tag_list[idx - 1].paddock[paddIdx - 1] = Value;
                }
            }
        }

        /// <summary>
        /// "dd mmm" -> StdDate
        /// </summary>
        /// <param name="strDay"></param>
        /// <returns></returns>
        protected int asStdDate(string strDay)
        {
            int doy = 0;

            //convert the day to the day number of the year
            if (!StdStrng.TokenDate(ref strDay, ref doy))//doy = decimal stddate
                doy = 0;
            return doy;

        }
        /// <summary>
        /// Get the string of a std date (integer). The string form is 'dd mmm'
        /// </summary>
        /// <param name="value"></param>
        protected string setFromStdDate(int value)
        {
            if (value != 0)
                return StdDate.DateStrFmt(value, "D mmm");
            else
                return String.Empty;
        }
    }
}