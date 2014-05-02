using System;
using System.Collections.Generic;
using System.Xml;
using System.Reflection;
using System.Linq;
using Models.Core;
using Models.Soils;
using System.Xml.Serialization;

namespace Models.SurfaceOM
{

    [Serializable]
    public class ResidueTypes
    {
        [XmlElement("ResidueType")]
        [XmlIgnore]
        public List<ResidueType> residues { get; set; }

        public ResidueTypes()
        {
            if (residues == null)
                residues = new List<ResidueType>();

            residues.Clear();
            residues.Add(new ResidueType("wheat") { specific_area = 0.0005 });
            residues.Add(new ResidueType("lucerne") { specific_area = 0.0002 });
            residues.Add(new ResidueType("barley") { specific_area = 0.0005 });
            residues.Add(new ResidueType("tithonia") { specific_area = 0.0005 });
            residues.Add(new ResidueType("bambatsi") { specific_area = 0.0005 });
            residues.Add(new ResidueType("barley") { specific_area = 0.0005 });
            residues.Add(new ResidueType("broccoli") { specific_area = 0.0004 });
            residues.Add(new ResidueType("butterflypea") { specific_area = 0.0004 });
            residues.Add(new ResidueType("camaldulensis") { specific_area = 0.0002 });
            residues.Add(new ResidueType("canola") { specific_area = 0.0002 });
            residues.Add(new ResidueType("centro") { specific_area = 0.0004 });
            residues.Add(new ResidueType("chickpea") { specific_area = 0.0002 });
            residues.Add(new ResidueType("cowpea") { specific_area = 0.0002 });
            residues.Add(new ResidueType("danthonia") { specific_area = 0.0005 });
            residues.Add(new ResidueType("nativepasture") { specific_area = 0.0005 });
            residues.Add(new ResidueType("pasture") { specific_area = 0.0005 });
            residues.Add(new ResidueType("globulus") { specific_area = 0.0002 });
            residues.Add(new ResidueType("grandis") { specific_area = 0.0002 });
            residues.Add(new ResidueType("oilmallee") { specific_area = 0.0002 });
            residues.Add(new ResidueType("fababean") { specific_area = 0.0002 });
            residues.Add(new ResidueType("fieldpea") { specific_area = 0.0002 });
            residues.Add(new ResidueType("grass") { specific_area = 0.0004 });
            residues.Add(new ResidueType("lablab") { specific_area = 0.0002 });
            residues.Add(new ResidueType("lentil") { specific_area = 0.0002 });
            residues.Add(new ResidueType("lolium_rigidum") { specific_area = 0.0002 });
            residues.Add(new ResidueType("lucerne") { specific_area = 0.0002 });
            residues.Add(new ResidueType("lupin") { specific_area = 0.0002 });
            residues.Add(new ResidueType("maize") { specific_area = 0.0004 });
            residues.Add(new ResidueType("medic") { specific_area = 0.0002 });
            residues.Add(new ResidueType("millet") { specific_area = 0.0004 });
            residues.Add(new ResidueType("mucuna") { specific_area = 0.0002 });
            residues.Add(new ResidueType("mungbean") { specific_area = 0.0002 });
            residues.Add(new ResidueType("horsegram") { specific_area = 0.0002 });
            residues.Add(new ResidueType("navybean") { specific_area = 0.0002 });
            residues.Add(new ResidueType("frenchbean") { specific_area = 0.0002 });
            residues.Add(new ResidueType("cotton") { specific_area = 0.0002 });
            residues.Add(new ResidueType("oats") { specific_area = 0.0005 });
            residues.Add(new ResidueType("oilpalmunderstory") { specific_area = 0.0002 });
            residues.Add(new ResidueType("orobanche") { specific_area = 0.0002 });
            residues.Add(new ResidueType("peanut") { specific_area = 0.0002 });
            residues.Add(new ResidueType("pigeonpea") { specific_area = 0.0002 });
            residues.Add(new ResidueType("poppies") { specific_area = 0.0005 });
            residues.Add(new ResidueType("potato") { specific_area = 0.0005 });
            residues.Add(new ResidueType("raphanus_raphanistrum") { specific_area = 0.0002 });
            residues.Add(new ResidueType("rice") { specific_area = 0.0005 });
            residues.Add(new ResidueType("soybean") { specific_area = 0.0002 });
            residues.Add(new ResidueType("sorghum") { specific_area = 0.0004 });
            residues.Add(new ResidueType("stylo") { specific_area = 0.0002 });
            residues.Add(new ResidueType("sugar") { specific_area = 0.0007 });
            residues.Add(new ResidueType("sunflower") { specific_area = 0.0002 });
            residues.Add(new ResidueType("sweetcorn") { specific_area = 0.0004 });
            residues.Add(new ResidueType("sweetsorghum") { specific_area = 0.0004 });
            residues.Add(new ResidueType("vetch") { specific_area = 0.0002 });
            residues.Add(new ResidueType("weed") { specific_area = 0.0004 });
            residues.Add(new ResidueType("WF_Millet") { specific_area = 0.0004 });
            residues.Add(new ResidueType("wheat") { specific_area = 0.0005 });
            residues.Add(new ResidueType("inert") { pot_decomp_rate = 0.0 });
            residues.Add(new ResidueType("slurp") { specific_area = 0.0005 });
            residues.Add(new ResidueType("manure")
            {
                fraction_C = 0.08,
                pot_decomp_rate = 0.1,
                fr_c = new double[] { 0.3, 0.3, 0.4 },
                fr_n = new double[] { 0.3, 0.3, 0.4 },
                fr_p = new double[] { 0.3, 0.3, 0.4 },
                po4ppm = 10,
                nh4ppm = 10,
                no3ppm = 10,
                specific_area = 0.0001
            });
            residues.Add(new ResidueType("RuminantDung_PastureFed")
            {
                fraction_C = 0.4,
                pot_decomp_rate = 0.1,
                fr_c = new double[] { 0.3, 0.5, 0.2 },
                fr_n = new double[] { 0.3, 0.5, 0.2 },
                fr_p = new double[] { 0.3, 0.5, 0.2 },
                po4ppm = 5,
                nh4ppm = 1250,
                no3ppm = 0,
                specific_area = 0.0001
            });
            residues.Add(new ResidueType("algae")
            {
                fraction_C = 0.4,
                pot_decomp_rate = 0.1,
                specific_area = 0.0005,
                cf_contrib = 1
            });
            residues.Add(new ResidueType("fym")
            {
                fraction_C = 0.8,
                pot_decomp_rate = 0.1,
                fr_c = new double[] { 0.3, 0.3, 0.4 },
                fr_n = new double[] { 0.3, 0.3, 0.4 },
                fr_p = new double[] { 0.3, 0.3, 0.4 },
                po4ppm = 40,
                nh4ppm = 10,
                no3ppm = 10,
                specific_area = 0.0001,
                cf_contrib = 1
            });
            residues.Add(new ResidueType("goatmanure")
            {
                fraction_C = 0.8,
                pot_decomp_rate = 0.1,
                fr_c = new double[] { 0.3, 0.6, 0.3 },
                fr_n = new double[] { 0.3, 0.6, 0.3 },
                fr_p = new double[] { 0.3, 0.6, 0.34 },
                po4ppm = 5,
                nh4ppm = 1307,
                no3ppm = 481,
                specific_area = 0.0001,
                cf_contrib = 1
            });
            residues.Add(new ResidueType("cm")
            {
                fraction_C = 0.277,
                pot_decomp_rate = 0.1,
                fr_c = new double[] { 0.0, 0.5, 0.5 },
                fr_n = new double[] { 0.0, 0.5, 0.5 },
                fr_p = new double[] { 0.0, 0.5, 0.5 },
                po4ppm = 5,
                nh4ppm = 2558,
                no3ppm = 873,
                specific_area = 0.0001,
                cf_contrib = 1
            });
            residues.Add(new ResidueType("cmA")
            {
                fraction_C = 0.374,
                pot_decomp_rate = 0.1,
                fr_c = new double[] { 0.0, 0.5, 0.5 },
                fr_n = new double[] { 0.0, 0.5, 0.5 },
                fr_p = new double[] { 0.0, 0.5, 0.5 },
                po4ppm = 5,
                nh4ppm = 1307,
                no3ppm = 481,
                specific_area = 0.0001,
                cf_contrib = 1
            });
            residues.Add(new ResidueType("cmB")
            {
                fraction_C = 0.24,
                pot_decomp_rate = 0.1,
                fr_c = new double[] { 0.0, 0.5, 0.5 },
                fr_n = new double[] { 0.0, 0.5, 0.5 },
                fr_p = new double[] { 0.0, 0.5, 0.5 },
                po4ppm = 5,
                nh4ppm = 3009,
                no3ppm = 36,
                specific_area = 0.0001,
                cf_contrib = 1
            });
            residues.Add(new ResidueType("manB")
            {
                fraction_C = 0.08,
                pot_decomp_rate = 0.1,
                fr_c = new double[] { 0.1, 0.01, 0.89 },
                fr_n = new double[] { 0.1, 0.01, 0.89 },
                fr_p = new double[] { 0.1, 0.01, 0.89 },
                po4ppm = 5,
                nh4ppm = 1307,
                no3ppm = 481,
                specific_area = 0.0001,
                cf_contrib = 1
            });
            residues.Add(new ResidueType("oilpalm")
            {
                fraction_C = 0.44,
                pot_decomp_rate = 0.05,
                specific_area = 0.0002,
            });
            residues.Add(new ResidueType("oilpalmstem")
            {
                fraction_C = 0.44,
                fr_c = new double[] { 0.2, 0.7, 0.1 },
                fr_n = new double[] { 0.2, 0.7, 0.1 },
                fr_p = new double[] { 0.2, 0.7, 0.1 },
                specific_area = 0.000005,
            });
        }

        public ResidueType getResidue(string name)
        {
            if (residues != null)
                foreach (ResidueType residueType in residues)
                {
                    if (residueType.fom_type.Equals(name, StringComparison.CurrentCultureIgnoreCase))
                        return residueType;
                }

            return null;
        }
    }
        [Serializable]

    public class ResidueType : Model
    {
        public string fom_type { get; set; }
        public double fraction_C { get; set; }
        public double po4ppm { get; set; }
        public double nh4ppm { get; set; }
        public double no3ppm { get; set; }
        public double specific_area { get; set; }
        public int cf_contrib { get; set; }
        public double pot_decomp_rate { get; set; }
        public double[] fr_c { get; set; }
        public double[] fr_n { get; set; }
        public double[] fr_p { get; set; }

        public ResidueType()
        {
            fom_type = "inert";
            InitialiseWithDefaults();
        }

        public ResidueType(string fomType)
        {
            fom_type = fomType;
            InitialiseWithDefaults();
        }

        private void InitialiseWithDefaults()
        {
            fraction_C = 0.4;
            specific_area = 0.0005;
            cf_contrib = 1;
            pot_decomp_rate = 0.1;
            fr_c = new double[3] { 0.2, 0.7, 0.1 };
            fr_n = new double[3] { 0.2, 0.7, 0.1 };
            fr_p = new double[3] { 0.2, 0.7, 0.1 };
        }


    }
        [Serializable]

    public class TillageTypes : Model
    {
        public List<TillageType> TillageType { get; set; }

        public TillageType GetTillageData(string Name)
        {
            foreach (TillageType tillageType in TillageType)
            {
                if (tillageType.Name == Name)
                    return tillageType;
            }
            return null;
        }
    }
    public partial class SurfaceOrganicMatter
    {
        const double acceptableErr = 1e-4;

        #region Math Operations

        static double bound(double tobound, double lower, double upper)
        {
            return Math.Max(Math.Min(tobound, upper), lower);
        }

        int bound(int tobound, int lower, int upper)
        {
            return Math.Max(Math.Min(tobound, upper), lower);
        }

        static double divide(double numerator, double denominator, double on_denom_is_0)
        {
            return denominator == 0 ? on_denom_is_0 : numerator / denominator;
        }

        /// <summary>
        /// "cover1" and "cover2" are numbers between 0 and 1 which
        ///     indicate what fraction of sunlight is intercepted by the
        ///     foliage of plants.  This function returns a number between
        ///     0 and 1 indicating the fraction of sunlight intercepted
        ///     when "cover1" is combined with "cover2", i.e. both sets of
        ///     plants are present.
        /// </summary>
        /// <param name="cover1"></param>
        /// <param name="cover2"></param>
        /// <returns></returns>
        private double add_cover(double cover1, double cover2)
        {
            double bare = (1 - cover1) * (1 - cover2);
            return 1 - bare;
        }

        private int count_of_real_vals(double[] p, int max_layer)
        {
            throw new NotImplementedException();
        }

        private double l_bound(double val, double bound)
        {
            return Math.Max(val, bound);
        }


        const string apsim_bounds_warning_error =
    @"'{0}' out of bounds!
     {1} < {2} < {3} evaluates 'FALSE'";

        private void Bound_check_real_var(double value, double lower, double upper, string vname)
        {
       
            if  (Utility.Math.IsLessThan(value, lower) || Utility.Math.IsGreaterThan(value, upper))
                Summary.WriteWarning(FullPath, String.Format(apsim_bounds_warning_error, vname, lower, value, upper));
        }

        private bool reals_are_equal(double first, double second)
        {
            return Math.Abs(first - second) < 2 * double.Epsilon;
        }

        /// <summary>
        /// <para>+ Purpose</para>
        /// <para>
        /// Find the first element of an array where a given value
        /// is contained with the cumulative sum_of of the elements.
        /// If sum_of is not reached by the end of the array, then it
        /// is ok to set it to the last element. This will take
        /// account of the case of the number of levels being 0.
        /// </para>
        /// <para>Definition</para>
        /// <para>
        /// Returns ndx where ndx is the smallest value in the range
        /// 1.."size_of" such that the sum of "array"(j), j=1..ndx is
        /// greater than or equal to "cum_sum".  If there is no such
        /// value of ndx, then "size_of" will be returned.
        /// <para>
        /// <para>Mission Statement</para>
        /// <para>
        /// Find index for cumulative %2 = %1
        /// </para>
        /// </summary>
        /// <param name="cum_sum">sum_of to be found</param>
        /// <param name="array">array to be searched</param>
        /// <param name="size_of">size_of of array</param>
        /// <returns>Index for a 1-BASED ARRAY</returns>
        private int get_cumulative_index_real(double cum_sum, double[] array, int size_of)
        {

            double cum = 0;
            for (int i = 0; i < size_of; i++)
                if ((cum += array[i]) >= cum_sum)
                    return i;
            return size_of;
        }

        #endregion

        T[] ToArray<T>(string str)
        {
            string[] temp;

            if (str == null || str == "")
                temp = new string[0];
            else
                temp = str.Split(new char[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);

            MethodInfo parser = null;
            if (typeof(T) != typeof(string))
                parser = typeof(T).GetMethod("Parse", new Type[] { typeof(string) });

            T[] result = new T[temp.Length];

            for (int i = 0; i < result.Length; i++)
                result[i] = parser == null ? (T)(object)temp[i] : (T)parser.Invoke(null, new[] { temp[i] });

            return result;
        }

        double Sum2DArray(double[,] _2Darray)
        {
            double result = 0;
            foreach (double f in _2Darray)
                result += f;

            return result;
        }

        double SumSurfOMStandingLying(List<SurfOrganicMatterType> var, Func<OMFractionType, double> func)
        {
            return var.Sum<SurfOrganicMatterType>(x => SumSurfOMStandingLying(x, func));
        }

        double SumSurfOMStandingLying(SurfOrganicMatterType var, Func<OMFractionType, double> func)
        {
            return var.Lying.Sum<OMFractionType>(func) + var.Standing.Sum<OMFractionType>(func);
        }

        double SumSurfOM(List<SurfOrganicMatterType> var, Func<SurfOrganicMatterType, double> func)
        {
            return var.Sum<SurfOrganicMatterType>(func);
        }

        double SumOMFractionType(OMFractionType[] var, Func<OMFractionType, double> func)
        {
            return var.Sum<OMFractionType>(func);
        }

        double surfaceom_wt_(string type, Func<OMFractionType, double> func)
        {
            int SOMNo = surfom_number(type);
            if (SOMNo > 0)
                return g.SurfOM.Sum<SurfOrganicMatterType>(x => x.Lying.Sum<OMFractionType>(func) + x.Standing.Sum<OMFractionType>(func));
            else
                throw new Exception("No organic matter called " + type + " present");
        }
    }

}