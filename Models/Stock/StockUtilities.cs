using System;

namespace Models.GrazPlan
{

    /// <summary>
    /// Collection of stock utility methods.
    /// </summary>
    public class StockUtilities
    {
        // Convert between condition scores and relative condition values   
        /// <summary>Condition score for condition = 1.0.</summary>
        private static double[] BASESCORE = { 3.0, 4.0, 4.5 };

        /// <summary>Change in condition for unit CS change .</summary>
        private static double[] SCOREUNIT = { 0.15, 0.09, 0.08 };


        /// <summary>Condition score system to use</summary>
        public enum Cond_System
        {
            /// <summary></summary>
            csSYSTEM1_5,
            /// <summary></summary>
            csSYSTEM1_8,
            /// <summary></summary>
            csSYSTEM1_9
        };

        /// <summary>
        /// Convert condition score to condition.
        /// </summary>
        /// <param name="CondScore"></param>
        /// <param name="System"></param>
        /// <returns></returns>
        static public double CondScore2Condition(double CondScore, Cond_System System = Cond_System.csSYSTEM1_5)
        {
            return 1.0 + (CondScore - BASESCORE[(int)System]) * SCOREUNIT[(int)System];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Condition"></param>
        /// <param name="System"></param>
        /// <returns></returns>
        static public double Condition2CondScore(double Condition, Cond_System System = Cond_System.csSYSTEM1_5)
        {
            return BASESCORE[(int)System] + (Condition - 1.0) / SCOREUNIT[(int)System];
        }

        /// <summary>
        /// Default fleece weight as a function of age, sex and time since shearing     
        /// </summary>
        /// <param name="Params"></param>
        /// <param name="iAgeDays"></param>
        /// <param name="Repr"></param>
        /// <param name="iFleeceDays"></param>
        /// <returns></returns>
        static public double DefaultFleece(Genotype Params,
                                     int iAgeDays,
                                     GrazType.ReproType Repr,
                                     int iFleeceDays)
        {
            double Result;
            double fMeanAgeFactor;

            iFleeceDays = Math.Min(iFleeceDays, iAgeDays);

            if ((Params.Animal == GrazType.AnimalType.Sheep) && (iFleeceDays > 0))
            {
                fMeanAgeFactor = 1.0 - (1.0 - Params.WoolC[5])
                                        * (Math.Exp(-Params.WoolC[12] * (iAgeDays - iFleeceDays)) - Math.Exp(-Params.WoolC[12] * iAgeDays))
                                        / (Params.WoolC[12] * iFleeceDays);
                Result = Params.FleeceRatio * Params.SexStdRefWt(Repr) * fMeanAgeFactor * iFleeceDays / 365.0;
            }
            else
                Result = 0.0;
            return Result;
        }

        /// <summary>
        /// Default fibre diameter as a function of age, sex, time since shearing and fleece weight                                                             
        /// </summary>
        /// <param name="Params"></param>
        /// <param name="iAgeDays"></param>
        /// <param name="Repr"></param>
        /// <param name="iFleeceDays"></param>
        /// <param name="fGFW"></param>
        /// <returns></returns>
        static public double DefaultMicron(Genotype Params, int iAgeDays, GrazType.ReproType Repr, int iFleeceDays, double fGFW)
        {
            double fPotFleece;

            if ((iFleeceDays > 0) && (fGFW > 0.0))
            {
                fPotFleece = DefaultFleece(Params, iAgeDays, Repr, iFleeceDays);
                return Params.MaxFleeceDiam * Math.Pow(fGFW / fPotFleece, Params.WoolC[13]);
            }
            else
                return Params.MaxFleeceDiam;
        }

    }
}
