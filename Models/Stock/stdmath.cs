namespace StdUnits
{
    using System;
    using System.Globalization;

    /// <summary>
    /// Math utilities
    /// </summary>
    public static class StdMath
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
        /// <param name="X">X value</param>
        /// <param name="Y">Y value</param>
        /// <returns>The difference if X >= Y</returns>
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
        /// <param name="X">X value</param>
        /// <param name="Y">Y value</param>
        /// <returns>The difference if X >= Y</returns>
        public static int IDIM(int X, int Y)
        {
            if (X >= Y)
                return X - Y;
            else
                return 0;
        }

        /// <summary>
        /// RAMP function
        /// </summary>
        /// <param name="X">X value</param>
        /// <param name="Z1">Z1 value</param>
        /// <param name="Z2">Z2 value</param>
        /// <returns>The result</returns>
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
        /// <param name="value1">Value 1</param>
        /// <param name="value2">Value 2</param>
        /// <param name="errVal">Error value</param>
        /// <returns>The result of the division if the denominator is > 0</returns>
        public static double Divide(double value1, double value2, double errVal)
        {
            return (value2 == 0.0) ? errVal : value1 / value2;
        }

        /// <summary>
        /// Division operation. If numerator is close to zero then return 0.0
        /// </summary>
        /// <param name="X">X value</param>
        /// <param name="Y">Y value</param>
        /// <returns>Result of the division</returns>
        public static double XDiv(double X, double Y)
        {
            if (Math.Abs(X) < EPS)
                return 0.0;
            else
                return X / Y;
        }

        /// <summary>
        /// Raise a number to a power. Throws exception when X is -ve.
        /// </summary>
        /// <param name="X">X Value</param>
        /// <param name="Y">Indice Y</param>
        /// <returns>Zero if X is close to zero. Otherwise X^Y</returns>
        public static double Pow(double X, double Y)
        {
            if (Math.Abs(X) < EPS)
                return 0.0;                    // Catch underflows 
            else if (X < 0.0)
                throw new Exception("Power of negative number attempted");
            else
                return Math.Exp(Y * Math.Log(X));
        }

        /// <summary>
        /// CumNormal function
        /// </summary>
        /// <param name="x">x value</param>
        /// <returns>The result</returns>
        public static double CumNormal(double x)
        {
            double T, A;

            if (x < 0.0)
                return 1.0 - CumNormal(-x);
            else
            {
                T = 1.0 / (1.0 + 0.2316419 * x);
                A = T * (0.31938153 + T * (-0.356563782 + T * (1.781477794 + T * (-1.821255978 + T * 1.330274429))));
                return 1.0 - A * Math.Exp(-0.5 * Sqr(x)) / Root2Pi;
            }
        }

        /// <summary>
        /// SIG function
        /// </summary>
        /// <param name="X">X value</param>
        /// <param name="C">C array</param>
        /// <returns>The result</returns>
        public static double SIG(double X, double[] C)
        {
            double scaledX;

            scaledX = C[1] * (X - C[0]);
            if (scaledX < -30.0)
                return 0;
            else if (scaledX > 30.0)
                return 1.0;
            else
                return 1.0 / (1.0 + Math.Exp(-scaledX));
        }
        
        /// <summary>
        /// Constants class
        /// </summary>
        public class TSigConsts
        {
            /// <summary>
            /// Array of values
            /// </summary>
            public double[] values = new double[2];
        }
    }

    /// <summary>
    /// The random number class
    /// </summary>
    [Serializable]
    public class MyRandom
    {
        /// <summary>
        /// The system random object
        /// </summary>
        [NonSerialized]
        private Random SysRandom;

        /// <summary>
        /// Next random number
        /// </summary>
        private int FNextRandom;

        /// <summary>
        /// Array of random numbers
        /// </summary>
        private double[] FRandomBuffer;

        /// <summary>
        /// 
        /// </summary>
        private double FRandNo;

        /// <summary>
        /// 
        /// </summary>
        private uint FSeed; //store for later read access

        /// <summary>
        /// Fill the array buffer with random numbers
        /// </summary>
        private void MyRandomize()
        {
            int i;

            for (i = 0; i <= 96; i++)
                this.FRandomBuffer[0] = this.Random();
            for (i = 0; i <= 96; i++)
                this.FRandomBuffer[i] = this.Random();
            this.FRandNo = this.Random();
            this.FNextRandom = 0;
        }
        
        /// <summary>
        /// Generate a random number 0 - 1
        /// </summary>
        /// <returns>The random number</returns>
        private double Random()
        {
            const double two2neg32 = ((1.0 / 0x10000) / 0x10000);  // 2^-32

            ulong temp;
            ulong f;

            temp = (ulong)this.FSeed * (ulong)(0x08088405) + 1;       
            this.FSeed = (uint)(temp & 0xFFFFFFFF);  // mask all but first 32bits
            f = this.FSeed;
            return f * two2neg32;
        }
        
        /// <summary>
        /// Container class for a random number generator. This means that it becomes
        /// thread safe and won't be trampled my another thread generating random
        /// numbers. Code moved from global implementation in StdMATH.pas and System.pas.
        /// </summary>
        /// <param name="seedVal">The seed value</param>
        public MyRandom(int seedVal)
        {
            this.FRandomBuffer = new double[97];
            this.FSeed = 0;
            if (seedVal != 0)
                this.SysRandom = new System.Random(seedVal);
            else
                this.SysRandom = new System.Random();
            this.Initialise(seedVal);
        }
        
        /// <summary>
        /// Uses the SeedVal if it is > 0 otherwise it uses the system seed generated
        /// </summary>
        /// <param name="seedVal">The seed value</param>
        public void Initialise(int seedVal)
        {
            this.FNextRandom = -1;

            // if want repeatable start point
            if (seedVal != 0)   
                this.FSeed = Convert.ToUInt32(seedVal, CultureInfo.InvariantCulture);
            else
            {
                this.FSeed = Convert.ToUInt32(this.SysRandom.Next(), CultureInfo.InvariantCulture);
            }
            this.MyRandomize();
        }
        
        /// <summary>
        /// Calculate a random number to insert into the buffer
        /// </summary>
        /// <returns>The random number</returns>
        public double RandomValue()
        {
            // Initialises automatically on first use   }
            if (this.FNextRandom < 0)                                                   
            {
                this.MyRandomize();
            }
            this.FNextRandom = (int)Math.Truncate(96.0 * this.FRandNo);
            this.FRandNo = this.FRandomBuffer[this.FNextRandom];
            double result = this.FRandNo;
            this.FRandomBuffer[this.FNextRandom] = this.Random();

            return result;
        }
        
        /// <summary>
        /// Get an integer random number
        /// </summary>
        /// <param name="x">x value</param>
        /// <returns>Random value</returns>
        public int RndRound(double x)
        {
            int result;
            if (this.RandomValue() > (Math.Abs(x) - Math.Floor(Math.Abs(x))))
                result = (int)Math.Truncate(x);
            else if (x >= 0.0)
            {
                result = (int)(Math.Truncate(x));
                result++;
            }
            else
            {
                result = (int)(Math.Truncate(x));
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
            return Math.Max(0, Math.Min(N, this.RndRound(N * P)));
        }
        
        /// <summary>
        /// Gets the random number
        /// </summary>
        public double RandNo
        {
            get { return this.FRandNo; }
        }
        
        /// <summary>
        /// Gets the seed value
        /// </summary>
        public uint Seed
        {
            get { return this.FSeed; }
        }
    }
}