using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

namespace Utility
{
    /// <summary>
    /// Various math utilities.
    /// </summary>
    public class Math
    {
        private const double tolerance = 0.00001;
        //------------------------------------------------
        // Returns true if specified value is 'missing'
        // -----------------------------------------------
        public static double MissingValue
        {
            get { return 999999; }
        }

        //-------------------------------------------------------------------------
        //
        //-------------------------------------------------------------------------
        public static bool FloatsAreEqual(double value1, double value2)
        {
            return FloatsAreEqual(value1, value2, tolerance);
        }
        //-------------------------------------------------------------------------
        //
        //-------------------------------------------------------------------------
        public static bool FloatsAreEqual(double value1, double value2, double tolerance)
        {
            return (System.Math.Abs(value1 - value2) < tolerance);
        }
        public static bool IsGreaterThan(double value1, double value2)
        {
            return value1 - value2 > tolerance;
        }
        public static bool IsLessThan(double value1, double value2)
            {
            return value2 - value1 > tolerance;
            }
        public static double RoundToZero(double value, double tolerance)
        {
            return (System.Math.Abs(value) <= tolerance) ? 0.0 : value;
        }
        public static double RoundToZero(double value)
        {
            return RoundToZero(value, 1.0e-15);
        }
        //-------------------------------------------------------------------------
        //
        //-------------------------------------------------------------------------
        public static double[] Multiply(double[] value1, double[] value2)
        {
            double[] results = new double[value1.Length];
            if (value1.Length == value2.Length)
            {
                results = new double[value1.Length];
                for (int iIndex = 0; iIndex < value1.Length; iIndex++)
                {
                    if (value1[iIndex] == MissingValue || value2[iIndex] == MissingValue)
                        results[iIndex] = MissingValue;
                    else
                        results[iIndex] = (value1[iIndex] * value2[iIndex]);
                }
            }
            return results;
        }
        //-------------------------------------------------------------------------
        //
        //-------------------------------------------------------------------------
        public static double[] Multiply_Value(double[] value1, double value2)
        {
            double[] results = null;
            results = new double[value1.Length];
            for (int iIndex = 0; iIndex < value1.Length; iIndex++)
            {
                if (value1[iIndex] == MissingValue)
                    results[iIndex] = MissingValue;
                else
                    results[iIndex] = (value1[iIndex] * value2);
            }
            return results;
        }
        //-------------------------------------------------------------------------
        //
        //-------------------------------------------------------------------------
        public static double[] Divide(double[] value1, double[] value2)
        {
            double[] results = null;
            if (value1.Length == value2.Length)
            {
                results = new double[value1.Length];
                for (int iIndex = 0; iIndex < value1.Length; iIndex++)
                {
                    if (value1[iIndex] == MissingValue || value2[iIndex] == MissingValue)
                        results[iIndex] = MissingValue;
                    else if (value2[iIndex] != 0)
                    {
                        results[iIndex] = (value1[iIndex] / value2[iIndex]);
                    }
                    else
                    {
                        results[iIndex] = value1[iIndex];
                    }
                }
            }
            return results;
        }
        //-------------------------------------------------------------------------
        //
        //-------------------------------------------------------------------------
        public static double[] Divide_Value(double[] value1, double value2)
        {
            double[] results = new double[value1.Length];
            //Avoid divide by zero problems
            if (value2 != 0)
            {
                for (int iIndex = 0; iIndex < value1.Length; iIndex++)
                {
                    if (value1[iIndex] == MissingValue)
                        results[iIndex] = MissingValue;
                    else
                        results[iIndex] = (value1[iIndex] / value2);
                }
            }
            else
            {
                results = value1;
            }
            return results;
        }


        //-------------------------------------------------------------------------
        //
        //-------------------------------------------------------------------------
        public static double Divide(double value1, double value2, double errVal)
        {
            return (value2 == 0.0) ? errVal : value1 / value2;
        }

        //-------------------------------------------------------------------------
        //
        //-------------------------------------------------------------------------
        public static float DivideFloat(float value1, float value2, float errVal)
        {
            return (value2 == 0.0) ? errVal : value1 / value2;
        }

        //-------------------------------------------------------------------------
        //
        //-------------------------------------------------------------------------
        public static double[] Add_Value(double[] value1, double value2)
        {
            double[] results = new double[value1.Length];
            for (int iIndex = 0; iIndex < value1.Length; iIndex++)
            {
                if (value1[iIndex] == MissingValue)
                    results[iIndex] = MissingValue;
                else
                    results[iIndex] = (value1[iIndex] + value2);
            }
            return results;
        }

        public static double[] Subtract(double[] value1, double[] value2)
        {
            double[] results = null;
            if (value1.Length == value2.Length)
            {
                results = new double[value1.Length];
                for (int iIndex = 0; iIndex < value1.Length; iIndex++)
                {
                    if (value1[iIndex] == MissingValue || value2[iIndex] == MissingValue)
                        results[iIndex] = MissingValue;
                    else
                        results[iIndex] = (value1[iIndex] - value2[iIndex]);
                }
            }
            return results;
        }

        //-------------------------------------------------------------------------
        //
        //-------------------------------------------------------------------------
        public static double[] Subtract_Value(double[] value1, double value2)
        {
            double[] results = new double[value1.Length];
            for (int iIndex = 0; iIndex < value1.Length; iIndex++)
            {
                if (value1[iIndex] == MissingValue)
                    results[iIndex] = MissingValue;
                else
                    results[iIndex] = (value1[iIndex] - value2);
            }
            return results;
        }
        //-------------------------------------------------------------------------
        // Sum an array of doubles 
        //-------------------------------------------------------------------------
        public static double Sum(IEnumerable Values)
        {
            double result = 0.0;
            foreach (double Value in Values)
                result += Value;
            return result;
        }

        //-------------------------------------------------------------------------
        // Average an array of doubles 
        //-------------------------------------------------------------------------
        public static double Average(IEnumerable Values)
        {
            double Sum = 0.0;
            int Count = 0;
            foreach (double Value in Values)
            {
                Sum += Value;
                Count++;
            }
            if (Count > 0)
                return Sum / Count;
            else
                return 0.0;
        }

        //-------------------------------------------------------------------------
        // Sum an array of numbers starting at startIndex up to (but not including) endIndex
        // beginning with an initial value
        //-------------------------------------------------------------------------
        public static double Sum(IEnumerable Values, int iStartIndex, int iEndIndex,
                                double dInitialValue)
        {
            double result = dInitialValue;
            if (iStartIndex < 0)
                throw new Exception("Utility.Math.Sum: End index or start index is out of range");
            int iIndex = 0;
            foreach (double Value in Values)
            {
                if (iIndex >= iStartIndex && iIndex < iEndIndex && Value != MissingValue)
                    result += Value;
                iIndex++;
            }

            return result;
        }
        //-------------------------------------------------------------------------
        //Linearly interpolates a value y for a given value x and a given
        //set of xy co-ordinates.
        //When x lies outside the x range_of, y is set to the boundary condition.
        //Returns true for Did_interpolate if interpolation was necessary.
        //-------------------------------------------------------------------------
        public static double LinearInterpReal(double dX, double[] dXCoordinate, double[] dYCoordinate, out bool bDidInterpolate)
        {
            bDidInterpolate = false;
            if (dXCoordinate == null || dYCoordinate == null)
                return 0;
            //find where x lies in the x coordinate
            if (dXCoordinate.Length == 0 || dYCoordinate.Length == 0 || dXCoordinate.Length != dYCoordinate.Length)
            {
                throw new Exception("Utility.Math.LinearInterpReal: Lengths of passed in arrays are incorrect");
            }

            for (int iIndex = 0; iIndex < dXCoordinate.Length; iIndex++)
            {
                if (dX <= dXCoordinate[iIndex])
                {
                    //Chcek to see if dX is exactly equal to dXCoordinate[iIndex]
                    //If so then don't calcuate dY.  This was added to remove roundoff error.
                    if (dX == dXCoordinate[iIndex])
                    {
                        bDidInterpolate = false;
                        return dYCoordinate[iIndex];
                    }
                    //Found position
                    else if (iIndex == 0)
                    {
                        bDidInterpolate = true;
                        return dYCoordinate[iIndex];
                    }
                    else
                    {
                        //interpolate - y = mx+c
                        if ((dXCoordinate[iIndex] - dXCoordinate[iIndex - 1]) == 0)
                        {
                            bDidInterpolate = true;
                            return dYCoordinate[iIndex - 1];
                        }
                        else
                        {
                            bDidInterpolate = true;
                            return ((dYCoordinate[iIndex] - dYCoordinate[iIndex - 1]) / (dXCoordinate[iIndex] - dXCoordinate[iIndex - 1]) * (dX - dXCoordinate[iIndex - 1]) + dYCoordinate[iIndex - 1]);
                        }
                    }
                }
                else if (iIndex == (dXCoordinate.Length - 1))
                {
                    bDidInterpolate = true;
                    return dYCoordinate[iIndex];
                }
            }// END OF FOR LOOP
            return 0.0;
        }

        static public double Constrain(double dValue, double dLowerLimit, double dUpperLimit)
        {
            double dConstrainedValue = 0.0;
            dConstrainedValue = System.Math.Min(dUpperLimit, System.Math.Max(dLowerLimit, dValue));
            return dConstrainedValue;
        }

        static public double[] Constrain(double[] dValues, double[] dLowerLimits, double[] dUpperLimits)
        {
            double[] Values = new double[dValues.Length];
            for (int i = 0; i < dValues.Length; i++)
                Values[i] = System.Math.Min(dUpperLimits[i], System.Math.Max(dLowerLimits[i], dValues[i]));
            return Values;
        }

        static public double Round(double Value, int NumDecPlaces)
        // rounds properly rather than the System.Math.round function.
        // e.g. 3.4 becomes 3.0
        //      3.5 becomes 4.0
        {
            double Multiplier = System.Math.Pow(10.0, NumDecPlaces);  // gives 1 or 10 or 100 for decplaces=0, 1, or 2 etc
            Value = System.Math.Truncate(Value * Multiplier + 0.5);
            return Value / Multiplier;
        }

        static public double[] Round(double[] Values, int NumDecPlaces)
        // rounds properly rather than the System.Math.round function.
        // e.g. 3.4 becomes 3.0
        //      3.5 becomes 4.0
        {
            for (int i = 0; i != Values.Length; i++)
                Values[i] = Round(Values[i], NumDecPlaces);
            return Values;
        }

        static public bool AreEqual(double[] Values1, double[] Values2)
        {
            // Return true if the 2 arrays of numbers are equal.
            if (Values1.Length == Values2.Length)
            {
                for (int i = 0; i < Values1.Length; i++)
                {
                    if (!Math.FloatsAreEqual(Values1[i], Values2[i]))
                        return false;
                }
            }
            else
                return false;
            return true;
        }

        // ---------------------------------------------
        // Reverse the contents of the specified array.
        // ---------------------------------------------
        static public double[] Reverse(double[] Values)
        {
            double[] ReturnValues = new double[Values.Length];

            int Index = 0;
            for (int Layer = Values.Length - 1; Layer >= 0; Layer--)
            {
                ReturnValues[Index] = Values[Layer];
                Index++;
            }
            return ReturnValues;
        }

        static public bool ValuesInArray(double[] Values)
        {
            if (Values != null)
            {
                foreach (double Value in Values)
                {
                    if (Value != Math.MissingValue && !double.IsNaN(Value))
                        return true;
                }
            }
            return false;
        }
        static public bool ValuesInArray(string[] Values)
        {
            if (Values != null)
            {
                foreach (string Value in Values)
                {
                    if (Value != "")
                        return true;
                }
            }
            return false;
        }
        // --------------------------------------------------
        // Convert an array of strings to an array of doubles
        // --------------------------------------------------
        static public double[] StringsToDoubles(IList Values)
        {
            double[] ReturnValues = new double[Values.Count];

            for (int Index = 0; Index != Values.Count; Index++)
            {
                if (Values[Index].ToString() == "" || Values[Index].ToString() == "NaN")
                    ReturnValues[Index] = Math.MissingValue;
                else
                    ReturnValues[Index] = Convert.ToDouble(Values[Index]);
            }
            return ReturnValues;
        }

        static public double[] ProbabilityDistribution(int NumPoints, bool Exceed)
        {
            double[] Probability = new double[NumPoints];

            for (int x = 1; x <= NumPoints; x++)
                Probability[x - 1] = (x - 0.5) / NumPoints * 100;

            if (Exceed)
                Array.Reverse(Probability);
            return Probability;
        }


        public class RegrStats
        {
            public int n;
            public double m;
            public double c;
            public double SEslope;
            public double SEcoeff;
            public double R2;
            public double ADJR2;
            public double R2YX;
            public double VarRatio;
            public double RMSD;
        };

        static public RegrStats CalcRegressionStats(List<double> X, List<double> Y)
        {
            // ------------------------------------------------------------------
            //    Calculate regression stats.   
            // ------------------------------------------------------------------
            RegrStats stats = new RegrStats();
            double SumX = 0;
            double SumY = 0;
            double SumXY = 0;
            double SumX2 = 0;
            double SumY2 = 0;
            double SumXYdiff2 = 0;
            double CSSX, CSSXY;
            double Xbar, Ybar;
            double TSS, TSSM;
            double REGSS, REGSSM;
            double RESIDSS, RESIDSSM;
            double S2;

            stats.n = 0;
            stats.m = 0.0;
            stats.c = 0.0;
            stats.SEslope = 0.0;
            stats.SEcoeff = 0.0;
            stats.R2 = 0.0;
            stats.ADJR2 = 0.0;
            stats.R2YX = 0.0;
            stats.VarRatio = 0.0;
            stats.RMSD = 0.0;
            int Num_points = X.Count;

            if (Num_points > 1)
            {

                for (int Point = 0;
                     Point < Num_points;
                     Point++)
                {
                    SumX = SumX + X[Point];
                    SumX2 = SumX2 + X[Point] * X[Point];       // SS for X
                    SumY = SumY + Y[Point];
                    SumY2 = SumY2 + Y[Point] * Y[Point];       // SS for y
                    SumXY = SumXY + X[Point] * Y[Point];       // SS for products
                }
                Xbar = SumX / Num_points;
                Ybar = SumY / Num_points;

                CSSXY = SumXY - SumX * SumY / Num_points;     // Corrected SS for products
                CSSX = SumX2 - SumX * SumX / Num_points;      // Corrected SS for X
                stats.n = Num_points;
                stats.m = CSSXY / CSSX;                             // Calculate slope
                stats.c = Ybar - stats.m * Xbar;                          // Calculate intercept

                TSS = SumY2 - SumY * SumY / Num_points;       // Corrected SS for Y = Sum((y-ybar)^2)
                TSSM = TSS / (Num_points - 1);                // Total mean SS
                REGSS = stats.m * CSSXY;                            // SS due to regression = Sum((yest-ybar)^2)
                REGSSM = REGSS;                               // Regression mean SS
                RESIDSS = TSS - REGSS;                        // SS about the regression = Sum((y-yest)^2)

                if (Num_points > 2)                           // MUST HAVE MORE THAN TWO POINTS FOR REG
                    RESIDSSM = RESIDSS / (Num_points - 2);     // Residual mean SS, variance of residual
                else
                    RESIDSSM = 0.0;

                stats.RMSD = System.Math.Sqrt(RESIDSSM);                        // Root mean square deviation
                stats.VarRatio = REGSSM / RESIDSSM;                  // Variance ratio - for F test (1,n-2)
                stats.R2 = 1.0 - (RESIDSS / TSS);                   // Unadjusted R2 calculated from SS
                stats.ADJR2 = 1.0 - (RESIDSSM / TSSM);              // Adjusted R2 calculated from mean SS
                if (stats.ADJR2 < 0.0)
                    stats.ADJR2 = 0.0;
                S2 = RESIDSSM;                                // Resid. MSS is estimate of variance
                // about the regression
                stats.SEslope = System.Math.Sqrt(S2) / System.Math.Sqrt(CSSX);              // Standard errors estimated from S2 & CSSX
                stats.SEcoeff = System.Math.Sqrt(S2) * System.Math.Sqrt(SumX2 / (Num_points * CSSX));

                // Statistical parameters of Butler, Mayer and Silburn

                stats.R2YX = 1.0 - (SumXYdiff2 / TSS);              // If you are on the 1:1 line then R2YX=1

                // If R2YX is -ve then the 1:1 line is a worse fit than the line y=ybar

                //      MeanAbsError = SumXYdiff / Num_points;
                //      MeanAbsPerError = SumXYDiffPer / Num_points;  // very dangerous when y is low
                // could use MeanAbsError over mean
            }
            return stats;
        }

        /// <summary>
        /// Return the time elasped in hours between the specified sun angle
        ///  from 90 deg in am and pm. +ve above the horizon, -ve below the horizon.
        /// </summary>
        /// <param name="SunAngle">Angle to measure time between such as twilight (deg).
        ///  angular distance between 90 deg and end of twilight - altitude of sun. +ve up, -ve down.
        ///  Civil twilight ends after sunset or begins before sunrise when the solar depression angle is 6°. e.g SunAngle = -6
        ///  Nautical twilight : 12°
        ///  Astronomical twilight : 18°
        ///  </param>
        static public double DayLength(double DayOfYear, double SunAngle, double Latitude)
        {
            //+ Constant Values
            const double aeqnox = 79.25;   //  average day number of autumnal equinox
            const double pi = 3.14159265359;
            const double dg2rdn = (2.0 * pi) / 360.0; // convert degrees to radians
            const double decsol = 23.45116 * dg2rdn; // amplitude of declination of sun
            //   - declination of sun at solstices.
            // cm says here that the maximum
            // declination is 23.45116 or 23 degrees
            // 27 minutes.
            // I have seen else_where that it should
            // be 23 degrees 26 minutes 30 seconds -
            // 23.44167
            const double dy2rdn = (2.0 * pi) / 365.25; // convert days to radians
            const double rdn2hr = 24.0 / (2.0 * pi); // convert radians to hours

            //+ Local Variables
            double alt;// twilight altitude limited to max/min
            //   sun altitudes end of twilight
            //   - altitude of sun. (radians)
            double altmn;// altitude of sun at midnight
            double altmx;// altitude of sun at midday
            double clcd;// cos of latitude * cos of declination
            double coshra;// cos of hour angle - angle between the
            //   sun and the meridian.
            double dec;// declination of sun in radians - this
            //   is the angular distance at solar
            //   noon between the sun and the equator.
            double hrangl;// hour angle - angle between the sun
            //   and the meridian (radians).
            double hrlt;// day_length in hours
            double latrn;// latitude in radians
            double slsd;// sin of latitude * sin of declination
            double sun_alt;// angular distance between
            // sunset and end of twilight - altitude
            // of sun. (radians)
            // Twilight is defined as the interval
            // between sunrise or sunset and the
            // time when the true centre of the sun
            // is 6 degrees below the horizon.
            // Sunrise or sunset is defined as when
            // the true centre of the sun is 50'
            // below the horizon.

            sun_alt = SunAngle * dg2rdn;

            // calculate daylangth in hours by getting the
            // solar declination (radians) from the day of year, then using
            // the sin and cos of the latitude.

            // declination ranges from -.41 to .41 (summer and winter solstices)

            dec = decsol * System.Math.Sin(dy2rdn * (DayOfYear - aeqnox));

            // get the max and min altitude of sun for today and limit
            // the twilight altitude between these.

            if (Math.FloatsAreEqual(System.Math.Abs(Latitude), 90.0))
            {
                coshra = Sign(1.0, -dec) * Sign(1.0, Latitude);
            }
            else
            {
                latrn = Latitude * dg2rdn;
                slsd = System.Math.Sin(latrn) * System.Math.Sin(dec);
                clcd = System.Math.Cos(latrn) * System.Math.Cos(dec);

                altmn = System.Math.Asin(System.Math.Min(System.Math.Max(slsd - clcd, -1.0), 1.0));
                altmx = System.Math.Asin(System.Math.Min(System.Math.Max(slsd + clcd, -1.0), 1.0));
                alt = System.Math.Min(System.Math.Max(sun_alt, altmn), altmx);

                // get cos of the hour angle
                coshra = (System.Math.Sin(alt) - slsd) / clcd;
                coshra = System.Math.Min(System.Math.Max(coshra, -1.0), 1.0);
            }

            // now get the hour angle and the hours of light
            hrangl = System.Math.Acos(coshra);
            hrlt = hrangl * rdn2hr * 2.0;
            return hrlt;
        }

        // ------------------------------------------------------------------
        // Transfer of sign - from FORTRAN.
        // The result is of the same type and kind as a. Its value is the abs(a) of a,
        // if b is greater than or equal positive zero; and -abs(a), if b is less than
        // or equal to negative zero.
        // Example a = sign (30,-2) ! a is assigned the value -30
        // ------------------------------------------------------------------
        static public double Sign(double a, double b)
        {
            if (b >= 0)
                return System.Math.Abs(a);
            else
                return -System.Math.Abs(a);
        }

        public static double Min(IEnumerable Values)
        {
            double Minimum = 9999999;
            foreach (double Value in Values)
                Minimum = System.Math.Min(Value, Minimum);
            return Minimum;
        }

        public static double Max(IEnumerable Values)
        {
            double Maximum = -9999999;
            foreach (double Value in Values)
                Maximum = System.Math.Max(Value, Maximum);
            return Maximum;
        }

        // ------------------------------------------------------------------------
        public static double Bound(double x, double x1, double x2)
        {
            return System.Math.Min(System.Math.Max(x, x1), x2);
        }

        public static double[] CreateArrayOfValues(double Value, int NumValues)
        {
            double[] Values = new double[NumValues];
            for (int i = 0; i < NumValues; i++)
                Values[i] = Value;
            return Values;
        }

        static public bool GreaterThan(double Value1, double Value2, int NumDecPlaces)
        // rounds properly rather than the System.Math.round function.
        // e.g. 3.4 becomes 3.0
        //      3.5 becomes 4.0
        {
            double Multiplier = System.Math.Pow(10.0, NumDecPlaces);  // gives 1 or 10 or 100 for decplaces=0, 1, or 2 etc
            Value1 = System.Math.Truncate(Value1 * Multiplier + 0.5);
            Value2 = System.Math.Truncate(Value2 * Multiplier + 0.5);
            return (Value1 > Value2);
        }

        static public bool LessThan(double Value1, double Value2, int NumDecPlaces)
        // rounds properly rather than the System.Math.round function.
        // e.g. 3.4 becomes 3.0
        //      3.5 becomes 4.0
        {
            double Multiplier = System.Math.Pow(10.0, NumDecPlaces);  // gives 1 or 10 or 100 for decplaces=0, 1, or 2 etc
            Value1 = System.Math.Truncate(Value1 * Multiplier + 0.5);
            Value2 = System.Math.Truncate(Value2 * Multiplier + 0.5);
            return (Value1 < Value2);
        }

        static public bool IsNumerical(string StringValue)
        {
            double Value;
            if (StringValue != "" && !Double.TryParse(StringValue, out Value))
                return false;
            else
                return true;
        }

        static public bool IsNumerical(string[] Values)
        {
            foreach (string Value in Values)
                if (!IsNumerical(Value))
                    return false;
            return true;
        }

        static public bool ValuesAreNumerical(string[] Values)
        {
            for (int i = 0; i < Values.Length; i++)
            {
                if (!IsNumerical(Values[i]))
                    return false;
            }
            return true;
        }

        static public bool IsNumericalenUS(string StringValue)
        {
            double Value;
            if (StringValue != "" && !Double.TryParse(StringValue, NumberStyles.Any, new CultureInfo("en-US"), out Value))
                return false;
            else
                return true;
        }
        static public bool ValuesAreNumericalenUS(string[] Values)
        {
            for (int i = 0; i < Values.Length; i++)
            {
                if (!IsNumericalenUS(Values[i]))
                    return false;
            }
            return true;
        }

        static public double[] RemoveMissingValuesFromBottom(double[] Values)
        {
            if (Values == null) return null;
            // Find the last non missing value.
            int i;
            for (i = Values.Length - 1; i >= 0; i--)
            {
                if (Values[i] != MissingValue && !double.IsNaN(Values[i]))
                    break;
            }
            if (i < 0)
                return new double[0];
            double[] ReturnValues = new double[i + 1];
            for (int j = 0; j <= i; j++)
                ReturnValues[j] = Values[j];
            return ReturnValues;
        }
        static public double[] RemoveValueAt(double[] Values, int Index)
        {
            List<double> NewValues = new List<double>();
            for (int i = 0; i < Values.Length; i++)
            {
                if (i != Index)
                    NewValues.Add(Values[i]);
            }
            return NewValues.ToArray();
        }

        public static string[] DoublesToStrings(IList DoubleValues)
        {
            string[] Values = new string[DoubleValues.Count];
            for (int i = 0; i < DoubleValues.Count; i++)
                Values[i] = DoubleValues[i].ToString();
            return Values;
        }

        public static float[] DoublesToSingles(double[] DoubleValues)
        {
            float[] Values = new float[DoubleValues.Length];
            for (int i = 0; i < DoubleValues.Length; i++)
                Values[i] = Convert.ToSingle(DoubleValues[i]);
            return Values;
        }
        public static double Sqr(double x) { return x * x; }

        public static double LnGamma(double xx)
        {
            double x = xx - 1.0;
            double tmp = x + 5.5;
            tmp = (x + 0.5) * System.Math.Log(tmp) - tmp;
            double ser = 1.0 + 76.18009173 / (x + 1.0) - 86.50532033 / (x + 2.0) + 24.01409822 / (x + 3.0)
             - 1.231739516 / (x + 4.0) + 0.120858003e-2 / (x + 5.0) - 0.536382e-5 / (x + 6.0);
            return tmp + System.Math.Log(2.50662827465 * ser);
        }

        // From http://rosettacode.org/wiki/Gamma_function#Java
        public static double Gamma(double x)
        {
            double[] p = {0.99999999999980993, 676.5203681218851, -1259.1392167224028,
			     	  771.32342877765313, -176.61502916214059, 12.507343278686905,
			     	  -0.13857109526572012, 9.9843695780195716e-6, 1.5056327351493116e-7};
            int g = 7;
            if (x < 0.5) return System.Math.PI / (System.Math.Sin(System.Math.PI * x) * Gamma(1 - x));

            x -= 1;
            double a = p[0];
            double t = x + g + 0.5;
            for (int i = 1; i < p.Length; i++)
            {
                a += p[i] / (x + i);
            }

            return System.Math.Sqrt(2 * System.Math.PI) * System.Math.Pow(t, x + 0.5) * System.Math.Exp(-t) * a;
        }


        static public bool AreEqual(IList L1, IList L2)
        {
            if (L1.Count != L2.Count)
                return false;
            for (int i = 0; i < L1.Count; i++)
                if (L1[i] != L2[i])
                    return false;

            return true;
        }


        /// <summary>
        /// Perform an insertion sort (stable sort) on the specified list.
        /// </summary>
        public static void StableSort<T>(IList<T> list, Comparison<T> comparison)
        {
            if (list == null)
                throw new ArgumentNullException("list");
            if (comparison == null)
                throw new ArgumentNullException("comparison");

            int count = list.Count;
            for (int j = 1; j < count; j++)
            {
                T key = list[j];

                int i = j - 1;
                for (; i >= 0 && comparison(list[i], key) > 0; i--)
                {
                    list[i + 1] = list[i];
                }
                list[i + 1] = key;
            }
        }

    }

}
