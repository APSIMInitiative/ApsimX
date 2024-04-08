using System;
using static Models.GrazPlan.GrazType;

namespace Models.GrazPlan
{
    /// <summary>
    /// Common definitions used in pastures
    /// </summary>
    public static class PastureUtil
    {
        /// <summary>g/m^2</summary>
        public const double UNGRAZEABLE = 40.0;

        /// <summary>deg C</summary>
        public const double FrostThreshold = 2.2;

        /// <summary>Reference global radiation(MJ / m ^ 2 / d)</summary>
        public const double REF_RADN = 20.0;

        /// <summary>Reference day length(hr)</summary>
        public const double REF_DAYLEN = 12.0;

        /// <summary></summary>
        public const double REF_RADNFLUX = REF_RADN / REF_DAYLEN;

        /// <summary></summary>
        public const double REF_CO2_TEMP = 20.0;

        /// <summary>mm</summary>
        public const double COHORT_ROOT_DIFF = 100.0;

        /// <summary>Weight(in kg) of one mole of N, P and S</summary>
        public static double[] MOLE_WEIGHT = { 0, 0.0140, 0.0310, 0.0321 };

        /// <summary>Convert from g/m^2 to kg/ha</summary>
        public const double GM2_KGHA = 10.0;

        /// <summary>Convert kg/ha to g/m^2</summary>
        public const double KGHA_GM2 = 0.1;

        /// <summary>Convert from m^2 to cm^2</summary>
        public const double M2_CM2 = 10000.0;

        /// <summary>Default P:N and S:N ratios for use when P and S models are inactive</summary>
        public const double DEF_P2N = 0.10;

        /// <summary>Default P:N and S:N ratios for use when P and S models are inactive</summary>
        public const double DEF_S2N = 0.08;

        /// <summary></summary>
        public const double CLASSWIDTH = 0.05;

        /// <summary></summary>
        public static double[] DMDLimits = { 0.85, 0.80, 0.75, 0.70, 0.65, 0.60, 0.55, 0.50, 0.45, 0.40, 0.35, 0.30, 0.25 };            // [0..HerbClassNo]

        /// <summary></summary>
        public static double[] HerbageDMD = { 0, 0.825, 0.775, 0.725, 0.675, 0.625, 0.575, 0.525, 0.475, 0.425, 0.375, 0.325, 0.275 };   // HerbageArray [1..HerbClassNo] (filled [0] <- 0)

        /// <summary></summary>
        public static string[] StatusName = { "", "seedling", "established", "senescing", "dead", "litter", "litter2" };

        /// <summary></summary>
        public static char[] ElemAbbr = { ' ', 'N', 'P', 'S' };                           // [N..S]   [1..3]

        /// <summary></summary>
        public static TPlantElement[] Nutr2Elem = { TPlantElement.N, TPlantElement.N, TPlantElement.P, TPlantElement.S };

        /// <summary>Diffusivities in water, in m²/d   Nitrate  Ammonium Phosphate Sulphate</summary>
        public static double[] DiffuseAq = { 1.64E-4, 1.71E-4, 0.77E-4, 0.93E-4 };

        /// <summary></summary>
        public enum TDevelopType
        {
            /// <summary></summary>
            Vernalizing,

            /// <summary></summary>
            Vegetative,

            /// <summary></summary>
            Reproductive,

            /// <summary></summary>
            Dormant,

            /// <summary></summary>
            Senescent,

            /// <summary></summary>
            SprayTopped,

            /// <summary></summary>
            DormantW
        }

        /// <summary>Pasture development event</summary>
        public enum TDevelopEvent
        {
            /// <summary></summary>
            startCycle,

            /// <summary></summary>
            endVernalizing,

            /// <summary></summary>
            endVegetative,

            /// <summary></summary>
            startFlowering,

            /// <summary></summary>
            endReproductive,

            /// <summary></summary>
            startSenescing,

            /// <summary></summary>
            endDormant,

            /// <summary></summary>
            endSenescent,

            /// <summary></summary>
            endDormantW
        }

        /// <summary>Growth limiting factor</summary>
        public enum TGrowthLimit
        {
            /// <summary>GAI</summary>
            glGAI,

            /// <summary></summary>
            glVPD,

            /// <summary></summary>
            glSM,

            /// <summary></summary>
            glLowT,

            /// <summary></summary>
            glWLog,

            /// <summary></summary>
            gl_N,

            /// <summary></summary>
            gl_P,

            /// <summary></summary>
            gl_S
        }

        /// <summary>
        /// Division with 0 / 0 = 0
        /// </summary>
        /// <param name="X"></param>
        /// <param name="Y"></param>
        /// <returns></returns>
        public static double Div0(double X, double Y)
        {
            if (X == 0.0)
            {
                return 0.0;
            }
            else
            {
                return X / Y;
            }
        }

        /// <summary>
        /// Matches Pascal Frac()
        /// </summary>
        /// <param name="X"></param>
        /// <returns></returns>
        public static double Frac(double X)
        {
            return X - Math.Truncate(X);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="X"></param>
        /// <param name="X0"></param>
        /// <param name="X1"></param>
        /// <returns></returns>
        public static double RAMP(double X, double X0, double X1)
        {
            double result;

            if (X0 > X1)
            {
                result = 1 - RAMP(X, X1, X0);
            }
            else if (X <= X0)
            {
                result = 0.0;
            }
            else if (X >= X1)
            {
                result = 1.0;
            }
            else
            {
                result = (X - X0) / (X1 - X0);
            }

            return result;
        }

        /// <summary></summary>
        public const double SIGScale = 5.88878;  // 2 * (Ln(0.95)-Ln(0.05))

        /// <summary>
        ///  Sigmoid function, given 5% and 95% points
        /// </summary>
        /// <param name="X"></param>
        /// <param name="X05"></param>
        /// <param name="X95"></param>
        /// <returns></returns>
        public static double SIG(double X, double X05, double X95)
        {
            double result;
            X = SIGScale * (X - 0.5 * (X95 + X05)) / (X95 - X05);
            if (X < -30.0)
            {
                result = 0.0;
            }
            else if (X < +30.0)
            {
                result = 1.0 / (1.0 + Math.Exp(-X));
            }
            else
            {
                result = 1.0;
            }

            return result;
        }

        /// <summary>
        /// Q10(scaled power) function of temperature
        /// </summary>
        /// <param name="X"></param>
        /// <param name="Y10Deg"></param>
        /// <param name="Q10"></param>
        /// <returns></returns>
        public static double Q10Func(double X, double Y10Deg, double Q10)
        {
            return Y10Deg * Math.Pow(Q10, (X - 10.0) / 10.0);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="X"></param>
        /// <param name="delta"></param>
        public static void XInc(ref double X, double delta)
        {
            X += delta;
        }

        /// <summary>
        /// Decrement by a delta value
        /// </summary>
        /// <param name="X">Base value</param>
        /// <param name="delta">The delta</param>
        public static void XDec(ref double X, double delta)
        {
            X -= delta;
        }

        /// <summary>
        /// Zero the pool DM if it is insignificant
        /// </summary>
        /// <param name="aPool">Dry matter pool</param>
        public static void ZeroRoundOff(ref DM_Pool aPool)
        {
            const double EPSILON = +1.0E-6;
            const double ERROR = -1.0E-4;

            if ((aPool.DM != 0) && (aPool.DM < EPSILON))
            {
                if (aPool.DM > ERROR)
                {
                    GrazType.ZeroDMPool(ref aPool);
                }
                else
                {
                    throw new Exception("Mass balance error");
                }
            }
        }

        /// <summary>
        /// Fill a single dimension array
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="array"></param>
        /// <param name="value"></param>
        public static void FillArray<T>(T[] array, T value)
        {
            if (array == null)
            {
                throw new Exception("Can't fill array==null");
            }

            if (typeof(T).IsValueType && array.GetType() == typeof(T[]))
            {
                for (int i = 0; i < array.GetLength(0); i++)
                {
                    array[i] = value;
                }
            }
            else
            {
                throw new Exception("Cannot fill <T>[]");
            }
        }

        /// <summary>
        /// Fill a two dimensional array
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="array"></param>
        /// <param name="value"></param>
        /// <exception cref="Exception"></exception>
        public static void FillArray<T>(T[,] array, T value)
        {
            if (array == null)
            {
                throw new Exception("Can't fill array==null");
            }

            if (typeof(T).IsValueType && array.GetType() == typeof(T[,]))
            {
                for (int i = 0; i < array.GetLength(0); i++)
                {
                    for (int j = 0; j < array.GetLength(1); j++)
                    {
                        array[i, j] = value;
                    }
                }
            }
            else
            {
                throw new Exception("Cannot fill <T>[,]");
            }
        }

        /// <summary>
        /// Fill an array of 3 dimensions
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="array"></param>
        /// <param name="value"></param>
        /// <exception cref="Exception"></exception>
        public static void FillArray<T>(T[,,] array, T value)
        {
            if (array == null)
            {
                throw new Exception("Can't fill array==null");
            }

            if (typeof(T).IsValueType && array.GetType() == typeof(T[,,]))
            {
                for (int i = 0; i < array.GetLength(0); i++)
                {
                    for (int j = 0; j < array.GetLength(1); j++)
                    {
                        for (int k = 0; k < array.GetLength(2); k++)
                        {
                            array[i, j, k] = value;
                        }
                    }
                }
            }
            else
            {
                throw new Exception("Cannot fill <T>[,,]");
            }
        }

        /// <summary>
        /// Fill an array [][][] with values
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="array"></param>
        /// <param name="value"></param>
        /// <exception cref="Exception"></exception>
        public static void Fill3DArray<T>(T[][][] array, T value)
        {
            if (array == null)
            {
                throw new Exception("Can't fill array==null");
            }

            if (typeof(T).IsValueType && array.GetType() == typeof(T[][][]))
            {
                for (int i = 0; i < array.GetLength(0); i++)
                {
                    for (int j = 0; j < array[i].GetLength(0); j++)
                    {
                        for (int k = 0; k < array[i][j].GetLength(0); k++)
                        {
                            array[i][j][k] = value;
                        }
                    }
                }
            }
            else
            {
                throw new Exception("Cannot fill <T>[]][][]");
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="DMDValue"></param>
        /// <param name="bRoundHigh"></param>
        /// <returns></returns>
        public static int DMDToClass(double DMDValue, bool bRoundHigh)
        {
            int result;

            if (bRoundHigh)
            {
                result = (int)(1 + Math.Round((HerbageDMD[1] - DMDValue) / CLASSWIDTH - 1.0E-5));
            }
            else
            {
                result = (int)(1 + Math.Round((HerbageDMD[1] - DMDValue) / CLASSWIDTH + 1.0E-5));
            }

            result = Math.Max(1, Math.Min(result, HerbClassNo));

            return result;
        }

        // =====================================================================
        // Routines for handling DM pools.
        // AddPool0, MovePool0, ResizePool0 Used when S, N &P all absent
        // AddPool1, MovePool1 Used when N only present
        // AddPool2, MovePool2 Used otherwise

        /// <summary></summary>
        public const double TOLER = 1.0E-8;

        /// <summary>
        /// Zero the DM pool use in Pasture
        /// </summary>
        /// <param name="Pool"></param>
        public static void ZeroPool(ref DM_Pool Pool)
        {
            int pe;

            Pool.DM = 0;

            // PastureUtil.FillArray(Pool.Nu, 0.0);
            for (pe = (int)TPlantElement.N; pe <= (int)TPlantElement.S; pe++)
            {
                Pool.Nu[pe] = 0;
            }

            Pool.AshAlk = 0;
        }

        /// <summary>
        /// Add the partial DM pool to the Total pool
        /// </summary>
        /// <param name="PartPool"></param>
        /// <param name="TotPool"></param>
        /// <param name="allowLoss"></param>
        public static void AddPool0(DM_Pool PartPool, ref DM_Pool TotPool, bool allowLoss = false)
        {
            if (allowLoss || (PartPool.DM > 0.0))
            {
                TotPool.DM += PartPool.DM;
            }
        }

        /// <summary>
        /// Add the partial DM pool to the Total pool
        /// </summary>
        /// <param name="PartPool"></param>
        /// <param name="TotPool"></param>
        /// <param name="allowLoss"></param>
        public static void AddPool1(DM_Pool PartPool, ref DM_Pool TotPool, bool allowLoss = false)
        {
            if (allowLoss || (PartPool.DM > 0.0))
            {
                TotPool.DM += PartPool.DM;
                TotPool.Nu[(int)TPlantElement.N] = TotPool.Nu[(int)TPlantElement.N] + PartPool.Nu[(int)TPlantElement.N];
                TotPool.AshAlk += PartPool.AshAlk;
            }
        }

        /// <summary>
        /// Add the partial DM pool to the Total pool
        /// </summary>
        /// <param name="PartPool"></param>
        /// <param name="TotPool"></param>
        /// <param name="allowLoss"></param>
        public static void AddPool2(DM_Pool PartPool, ref DM_Pool TotPool, bool allowLoss = false)
        {
            if (allowLoss || (PartPool.DM > 0.0))
            {
                TotPool.DM += PartPool.DM;
                TotPool.Nu[(int)TPlantElement.N] = TotPool.Nu[(int)TPlantElement.N] + PartPool.Nu[(int)TPlantElement.N];
                TotPool.Nu[(int)TPlantElement.P] = TotPool.Nu[(int)TPlantElement.P] + PartPool.Nu[(int)TPlantElement.P];
                TotPool.Nu[(int)TPlantElement.S] = TotPool.Nu[(int)TPlantElement.S] + PartPool.Nu[(int)TPlantElement.S];
                TotPool.AshAlk += PartPool.AshAlk;
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="KgHaDM"></param>
        /// <param name="SrcPool"></param>
        /// <param name="DstPool"></param>
        public static void MovePool0(double KgHaDM, ref DM_Pool SrcPool, ref DM_Pool DstPool)
        {
            KgHaDM = Math.Min(KgHaDM, SrcPool.DM);
            if (KgHaDM > 0.0)
            {
                SrcPool.DM -= KgHaDM;
                DstPool.DM += KgHaDM;
                if (SrcPool.DM < TOLER)
                {
                    ZeroPool(ref SrcPool);
                }
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="KgHaDM"></param>
        /// <param name="SrcPool"></param>
        /// <param name="DstPool"></param>
        public static void MovePool1(double KgHaDM, ref DM_Pool SrcPool, ref DM_Pool DstPool)
        {
            double KgHaN, MolHaAlk;

            KgHaDM = Math.Min(KgHaDM, SrcPool.DM);
            if (KgHaDM > 0.0)
            {
                KgHaN = KgHaDM * SrcPool.Nu[(int)TPlantElement.N] / SrcPool.DM;
                MolHaAlk = KgHaDM * SrcPool.AshAlk / SrcPool.DM;
                SrcPool.DM -= KgHaDM;
                SrcPool.Nu[(int)TPlantElement.N] = SrcPool.Nu[(int)TPlantElement.N] - KgHaN;
                SrcPool.AshAlk -= MolHaAlk;
                DstPool.DM += KgHaDM;
                DstPool.Nu[(int)TPlantElement.N] = DstPool.Nu[(int)TPlantElement.N] + KgHaN;
                DstPool.AshAlk += MolHaAlk;

                if (SrcPool.DM < TOLER)
                {
                    ZeroPool(ref SrcPool);
                }
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="KgHaDM"></param>
        /// <param name="SrcPool"></param>
        /// <param name="DstPool"></param>
        public static void MovePool2(double KgHaDM, ref DM_Pool SrcPool, ref DM_Pool DstPool)
        {
            double KgHaS, KgHaN, KgHaP, MolHaAlk;

            KgHaDM = Math.Min(KgHaDM, SrcPool.DM);
            if (KgHaDM > 0.0)
            {
                KgHaN = KgHaDM * SrcPool.Nu[(int)TPlantElement.N] / SrcPool.DM;
                KgHaP = KgHaDM * SrcPool.Nu[(int)TPlantElement.P] / SrcPool.DM;
                KgHaS = KgHaDM * SrcPool.Nu[(int)TPlantElement.S] / SrcPool.DM;
                MolHaAlk = KgHaDM * SrcPool.AshAlk / SrcPool.DM;
                SrcPool.DM -= KgHaDM;
                SrcPool.Nu[(int)TPlantElement.N] = SrcPool.Nu[(int)TPlantElement.N] - KgHaN;
                SrcPool.Nu[(int)TPlantElement.P] = SrcPool.Nu[(int)TPlantElement.P] - KgHaP;
                SrcPool.Nu[(int)TPlantElement.S] = SrcPool.Nu[(int)TPlantElement.S] - KgHaS;
                SrcPool.AshAlk -= MolHaAlk;
                DstPool.DM += KgHaDM;
                DstPool.Nu[(int)TPlantElement.N] = DstPool.Nu[(int)TPlantElement.N] + KgHaN;
                DstPool.Nu[(int)TPlantElement.P] = DstPool.Nu[(int)TPlantElement.P] + KgHaP;
                DstPool.Nu[(int)TPlantElement.S] = DstPool.Nu[(int)TPlantElement.S] + KgHaS;
                DstPool.AshAlk += MolHaAlk;

                if (SrcPool.DM < TOLER)
                {
                    ZeroPool(ref SrcPool);
                }
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="aPool"></param>
        /// <param name="newDM"></param>
        public static void ResizePool0(ref DM_Pool aPool, double newDM)
        {
            aPool.DM = newDM;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="aPool"></param>
        /// <param name="newDM"></param>
        public static void ResizePool1(ref DM_Pool aPool, double newDM)
        {
            aPool.Nu[(int)TPlantElement.N] = newDM * Div0(aPool.Nu[(int)TPlantElement.N], aPool.DM);
            aPool.AshAlk = newDM * Div0(aPool.AshAlk, aPool.DM);
            aPool.DM = newDM;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="aPool"></param>
        /// <param name="newDM"></param>
        public static void ResizePool2(ref DM_Pool aPool, double newDM)
        {
            aPool.Nu[(int)TPlantElement.N] = newDM * Div0(aPool.Nu[(int)TPlantElement.N], aPool.DM);
            aPool.Nu[(int)TPlantElement.P] = newDM * Div0(aPool.Nu[(int)TPlantElement.P], aPool.DM);
            aPool.Nu[(int)TPlantElement.S] = newDM * Div0(aPool.Nu[(int)TPlantElement.S], aPool.DM);
            aPool.AshAlk = newDM * Div0(aPool.AshAlk, aPool.DM);
            aPool.DM = newDM;
        }

        /// <summary>
        /// Scale the mass value from the units specified to g/m^2
        /// </summary>
        /// <param name="aValue">Value</param>
        /// <param name="units">Units</param>
        /// <returns>Scaled value</returns>
        public static double ReadMass(double aValue, string units)
        {
            double result;
            if (units == "g/m^2")
            {
                result = aValue;
            }
            else
            {
                result = aValue / GM2_KGHA;
            }

            return result;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="srcPool"></param>
        /// <param name="dstPool"></param>
        /// <param name="Elem"></param>
        /// <param name="amount"></param>
        public static void MoveNutrient(ref DM_Pool srcPool, ref DM_Pool dstPool, TPlantElement Elem, double amount)
        {
            if (amount > 0.0)
            {
                amount = Math.Min(amount, srcPool.Nu[(int)Elem]);
                srcPool.Nu[(int)Elem] = srcPool.Nu[(int)Elem] - amount;
                dstPool.Nu[(int)Elem] = dstPool.Nu[(int)Elem] + amount;
            }
        }
    }
}
