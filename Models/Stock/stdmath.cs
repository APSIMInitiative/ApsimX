using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace StdUnits
{
    /// <summary>
    /// Math utilities
    /// </summary>
    static public class StdMath
    {
        /// <summary>
        /// Missing float value
        /// </summary>
        public const float MISSING  = -3.33333E33f;
        /// <summary>
        /// missing double value
        /// </summary>
        public const double DMISSING = -3.33333E33;
        /// <summary>
        /// Square root of 2 * pi
        /// </summary>
        public const double Root2Pi = 2.50662827465;

        /// <summary>
        /// Constants
        /// </summary>
        public class TSigConsts
        {
            /// <summary>
            /// Array of values
            /// </summary>
            public double[] values = new double[2];
        }

        /// <summary>
        /// Small value 1E-7
        /// </summary>
        private const double EPS = 1.0E-7;

        /// <summary>
        /// Square the value
        /// </summary>
        /// <param name="value"></param>
        /// <returns>Value squared</returns>
        public static double Sqr(double value)
        {
            return value * value;
        }
        /// <summary>
        /// Mimics FORTRAN DIM function              
        /// </summary>
        /// <param name="X"></param>
        /// <param name="Y"></param>
        /// <returns></returns>
        public static double DIM(double X, double Y)
        {
            if (X >= Y)
                return X - Y;
            else 
                return 0.0; 
        }

        /// <summary>
        /// Integer DIM function
        /// </summary>
        /// <returns></returns>
        public static int IDIM(int X, int Y)
        {
            if (X >= Y)
                return X - Y;
            else
                return 0;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="X"></param>
        /// <param name="Z1"></param>
        /// <param name="Z2"></param>
        /// <returns></returns>
        public static double RAMP(double X, double Z1, double Z2)
        {
            if (Z1 > Z2)
                return 1 - RAMP(X, Z2, Z1);
            else if (X <= Z1)
                return 0.0;
            else if (X >= Z2)
                return 1.0;
            else
                return (X - Z1) / (Z2 - Z1);
        }
        
        /// <summary>
        /// Divide value1 by value2. On error, the value errVal will be returned.
        /// </summary>
        public static double Divide(double value1, double value2, double errVal)
        {
            return (value2 == 0.0) ? errVal : value1 / value2;
        }

        /// <summary>
        /// Division operation. If numerator is close to zero then return 0.0
        /// </summary>
        /// <param name="X"></param>
        /// <param name="Y"></param>
        /// <returns></returns>
        static public double XDiv(double X, double Y)
        {
            if (Math.Abs(X) < EPS)
                return 0.0;
            else
                return X / Y;
        }

        /// <summary>
        /// Raise a number to a power. Throws exception when X is -ve.
        /// </summary>
        /// <param name="X">Value</param>
        /// <param name="Y">Indice</param>
        /// <returns>Zero if X is close to zero. Otherwise X^Y</returns>
        static public double Pow(double X, double Y)
        {
            if (Math.Abs(X) < EPS)
                return 0.0;                    // Catch underflows 
            else if (X < 0.0)
                throw new Exception("Power of negative number attempted");
            else
                return Math.Exp(Y * Math.Log(X));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="X"></param>
        /// <returns></returns>
        static public double CumNormal(double X)
        {
            double T, A;

            if (X < 0.0)
                return 1.0 - CumNormal(-X);
            else
            {
                T = 1.0 / (1.0 + 0.2316419 * X);
                A = T * (0.31938153 + T * (-0.356563782 + T * (1.781477794 + T * (-1.821255978 + T * 1.330274429))));
                return 1.0 - A * Math.Exp(-0.5 * Sqr(X)) / Root2Pi;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="X"></param>
        /// <param name="C"></param>
        /// <returns></returns>
        static public double SIG(double X, double[] C)
        {
            double fScaledX;

            fScaledX = C[1] * (X - C[0]);
            if (fScaledX < -30.0)
                return 0;
            else if (fScaledX > 30.0)
                return 1.0;
            else
                return 1.0 / (1.0 + Math.Exp(-fScaledX));
        }
    }

    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class TMyRandom
    {
        private Random SysRandom;
        private int FNextRandom;
        private double[] FRandomBuffer;
        private double FRandNo;
        uint FSeed; //store for later read access

        private void MyRandomize()
        {
            int i;

            for (i = 0; i <= 96; i++)
                FRandomBuffer[0] = this.Random();
            for (i = 0; i <= 96; i++)
                FRandomBuffer[i] = this.Random();
            FRandNo = this.Random();
            FNextRandom = 0;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private double Random()
        {
            const double two2neg32 = ((1.0 / 0x10000) / 0x10000);  // 2^-32

            UInt64 Temp;
            UInt64 F;

            Temp = (UInt64)FSeed * (UInt64)(0x08088405) + 1;       
            FSeed = (uint)(Temp & 0xFFFFFFFF);  //mask all but first 32bits
            F = FSeed;
            return F * two2neg32;
        }
        /// <summary>
        /// Container class for a random number generator. This means that it becomes
        /// thread safe and won't be trampled my another thread generating random
        /// numbers. Code moved from global implementation in StdMATH.pas and System.pas.
        /// </summary>
        /// <param name="SeedVal"></param>
        public TMyRandom(int SeedVal)
        {
            FRandomBuffer = new double[97];
            FSeed = 0;
            if (SeedVal != 0)
                SysRandom = new System.Random(SeedVal);
            else
                SysRandom = new System.Random();
            Initialise(SeedVal);
        }
        /// <summary>
        /// Uses the SeedVal if it is > 0 otherwise it uses the system seed generated
        /// </summary>
        /// <param name="SeedVal"></param>
        public void Initialise(int SeedVal)
        {
            FNextRandom = -1;
            if (SeedVal != 0)   //if want repeatable start point
                FSeed = Convert.ToUInt32(SeedVal);
            else
            {
                FSeed = Convert.ToUInt32(SysRandom.Next());
            }
            MyRandomize();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public double MyRandom()
        {
            if (FNextRandom < 0)                                                   // Initialises automatically on first use   }
            {
                this.MyRandomize();
            }
            FNextRandom = (int)Math.Truncate(97.0 * FRandNo);
            FRandNo = FRandomBuffer[FNextRandom];
            double result = FRandNo;
            FRandomBuffer[FNextRandom] = this.Random();

            return result;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="X"></param>
        /// <returns></returns>
        public int RndRound(double X)
        {
            int result;
            if (MyRandom() > (Math.Abs(X) - Math.Floor(Math.Abs(X))) )
                result = (int)Math.Truncate(X);
            else if (X >= 0.0)
            {
                result = (int)(Math.Truncate(X));
                result++;
            }
            else
            {
                result = (int)(Math.Truncate(X));
                result--;
            }
            return result;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="N"></param>
        /// <param name="P"></param>
        /// <returns></returns>
        public int RndPropn(int N, double P)
        {
            return Math.Max( 0, Math.Min( N, RndRound(N*P) ) );
        }
        /// <summary>
        /// Gets the random number
        /// </summary>
        public double RandNo
        {
            get { return FRandNo; }
        }
        /// <summary>
        /// Gets the seed value
        /// </summary>
        public uint Seed
        {
            get { return FSeed; }
        }
    }
    
}