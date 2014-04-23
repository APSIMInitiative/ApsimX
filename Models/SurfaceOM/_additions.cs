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
        public List<ResidueType> residues { get; set; }

        public ResidueTypes()
        {
            if (residues == null)
                residues = new List<ResidueType>();
            residues.Add(new ResidueType("wheat") { specific_area = 0.0005 });
            residues.Add(new ResidueType("lucerne") { specific_area = 0.0002 });
            residues.Add(new ResidueType("barley") { specific_area = 0.0005 });
            residues.Add(new ResidueType("potato") { specific_area = 0.0005 });
            residues.Add(new ResidueType("grass") { specific_area = 0.0004 });
            residues.Add(new ResidueType("oilpalm") { specific_area = 0.0002, fraction_C = 0.44, pot_decomp_rate= 0.05 });
            residues.Add(new ResidueType("slurp") { specific_area = 0.0005 });
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