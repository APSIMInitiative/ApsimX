using System;
using System.Collections.Generic;
using System.Text;

namespace StdUnits
{
    /// <summary>
    /// Split the long integer into three       
    /// parts: day and month get a byte each,  
    /// year gets two. Note that this arrangement allows relational                   
    /// operators to be used on date values
    /// </summary>
    public struct DMY
    {
        //TODO: may need to change this if overloaded operators are required
        /// <summary>
        /// Day, Month
        /// </summary>
        public int D, M;
        /// <summary>
        /// Year
        /// </summary>
        public int Y;
    }

    /// <summary>
    /// GrazPlan date utilities
    /// </summary>
    public static class StdDate
    {
        /// <summary>
        /// No. of days up to the last day of previous month. 29 Feb not included   [months 1..12]
        /// </summary>
        public static int[] CumulDays = { 0, 0, 31, 59, 90, 120, 151, 181, 212, 243, 273, 304, 334 };

        /// <summary>
        /// Last day of each month   [1..12]
        /// </summary>
        public static int[] LastDay = { 0, 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
        /// <summary>
        /// Short text name of the month
        /// </summary>
        public static string[] MonthText = { "", "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };    //[1..12]

        /// <summary>
        /// Get the date value for specified dmy
        /// </summary>
        /// <param name="Day">Day number</param>
        /// <param name="Month">Month number</param>
        /// <param name="Year">Year number</param>
        /// <returns>The date value integer</returns>
        public static int DateVal(int Day, int Month, int Year)
        {
            return Day + Month * 0x100 + Year * 0x10000;
        }

        /// <summary>
        /// Get the day of month from the date value
        /// </summary>
        /// <param name="Dt">Date value</param>
        /// <returns>Day of month</returns>
        public static int DayOf(int Dt)
        {
            return (Dt & 0xFF);
        }
        /// <summary>
        /// Get the month of the year from the date value
        /// </summary>
        /// <param name="Dt">Date value</param>
        /// <returns>Month of year</returns>
        public static int MonthOf(int Dt)
        {
            return ((Dt >> 8) & 0xFF);
        }
        /// <summary>
        /// Get the year number from the date value
        /// </summary>
        /// <param name="Dt">The date value</param>
        /// <returns>Year number</returns>
        public static int YearOf(int Dt)
        {
            return ((Dt >> 16) & 0xFFFF);
        }
        /// <summary>
        /// Test that this is a real date
        /// </summary>
        /// <param name="Dt">Date value</param>
        /// <returns>True if this is a valid date</returns>
        public static bool DateValid(int Dt)
        {
            DMY aDate;

            aDate.D = DayOf(Dt);
            aDate.M = MonthOf(Dt);
            aDate.Y = YearOf(Dt);
            if (aDate.M == 0)                                                         // 0-0-Y date                               
                return ((aDate.D == 0) && (aDate.Y != 0));
            else if (aDate.D == 0)                                                     // 0-M-Y or 0-M-0 date                      
                return ((aDate.M >= 1) && (aDate.M <= 12));
            else                                                                    // D-M-Y or D-M-0 date                      
                return (((aDate.M >= 1) && (aDate.M <= 12))
                          && ((aDate.D <= LastDay[aDate.M])
                                || ((aDate.D == 29) && (aDate.M == 2) && LeapYear(aDate.Y))));       // N.B. LeapYear(0)=TRUE

        }

        /// <summary>
        /// Is a year a leap year?  N.B. returns TRUE for Y=0                         }
        /// </summary>
        /// <param name="Y"></param>
        /// <returns></returns>
        public static bool LeapYear(int Y)
        {
            return (((Y % 4 == 0) && (Y % 100 != 0)) || (Y % 400 == 0));
        }

        /// <summary>
        /// Format a number with or without zeros
        /// </summary>
        /// <param name="N"></param>
        /// <param name="Len"></param>
        /// <param name="ZeroFill"></param>
        /// <returns>Formatted number</returns>
        public static string Num2Str(int N, int Len, bool ZeroFill)
        {
            if (ZeroFill)
                return String.Format("{0, 0" + Len.ToString() + ":d" + Len.ToString() + "}", N);
            else
                return String.Format("{0, " + Len.ToString() + "}", N);
        }

        /// <summary>
        /// Format a date value
        /// </summary>
        /// <param name="Dt">Date value</param>
        /// <param name="Fmt">Format string YYYY, yy, YY, mmm, MM, mm, M, DD, dd, D</param>
        /// <returns>Formatted date string</returns>
        public static string DateStrFmt(int Dt, string Fmt)
        {
            // Longer pictures must precede shorter     
            string[] Pictures = { "YYYY", "yy", "YY", "mmm", "MM", "mm", "M", "DD", "dd", "D" };
            const int NOPICTURE = 10;

            string St;
            int Pic;
            DMY aDate;

            aDate.D = DayOf(Dt);
            aDate.M = MonthOf(Dt);
            aDate.Y = YearOf(Dt);

            St = "";
            while (Fmt != "")                                                       // Work through the formatting string       
            {
                Pic = 0;                                                               // Search for a string picture              
                while ((Pic < NOPICTURE) && (Fmt.IndexOf(Pictures[Pic]) != 0))
                    Pic++;

                switch (Pic)
                {
                    case 0: St = St + Num2Str(aDate.Y, 4, false);                      // YYYY                                     
                        break;
                    case 1: if ((aDate.Y >= 1900) && (aDate.Y <= 1999))                // yy                                      
                            St = St + Num2Str(aDate.Y % 100, 2, true);
                        else
                            St = St + Num2Str(aDate.Y, 4, true);
                        break;
                    case 2: if (aDate.Y % 100 == 0)                                  // YY                                       
                            St = St + "00";
                        else
                            St = St + Num2Str(aDate.Y % 100, 2, true);
                        break;
                    case 3: if ((aDate.M >= 1) && (aDate.M <= 12))                    // mmm                                      
                            St = St + MonthText[aDate.M];
                        break;
                    case 4: St = St + Num2Str(aDate.M, 2, true);                      // MM                                       
                        break;
                    case 5: St = St + Num2Str(aDate.M, 2, false);                      // mm                                       
                        break;
                    case 6: St = St + Num2Str(aDate.M, 0, false);                      // M                                        
                        break;
                    case 7: St = St + Num2Str(aDate.D, 2, true);                      // DD                                       
                        break;
                    case 8: St = St + Num2Str(aDate.D, 2, false);                      // dd                                       
                        break;
                    case 9: St = St + Num2Str(aDate.D, 0, false);                      // D                                        
                        break;
                    case NOPICTURE: St = St + Fmt[0];                                      // Literal text                             
                        break;
                }

                if (Pic == NOPICTURE)                                                // Delete the part of Fmt we have just used 
                    Fmt = Fmt.Remove(0, 1); //Delete( Fmt, 1, 1 )
                else
                    Fmt = Fmt.Remove(0, Pictures[Pic].Length); //Delete( Fmt, 1, Length(Pictures[Pic]) ); //TODO: check this
            }
            return St;
        }

        /// <summary>
        /// Get the length of the month in days
        /// </summary>
        /// <param name="M">Month number</param>
        /// <param name="Y">Year number</param>
        /// <returns>Days in the month</returns>
        static private int MonthLength(int M, int Y)
        {
            if (M == 0)
                return 31;
            else if ((M == 2) && LeapYear(Y))
                return 29;
            else
                return LastDay[M];
        }

        /// <summary>
        ///  Shift a date by a given number of months                              
        /// </summary>
        /// <param name="aDate">Original date value</param>
        /// <param name="Months">Months to increment by</param>
        static private void MonthShift(ref DMY aDate, int Months)
        {
            int TotalMonths;

            if ((aDate.Y != 0) && (aDate.M != 0))
            {
                TotalMonths = 12 * aDate.Y + aDate.M + Months - 13;                // Months from 1 Jan 0001 
                aDate.Y = 1 + TotalMonths / 12;
                aDate.M = 1 + TotalMonths % 12;
            }
            else if (aDate.Y != 0)
                aDate.Y += Months / 12;
            else if (aDate.M != 0)
                aDate.M = 1 + (aDate.M + Months - 12 * (Months / 12) + 11) % 12;
        }

        /// <summary>
        /// Change a date by a given number of days, months and/or years, forward or back.                                                                     
        /// </summary>
        /// <param name="Dt">Starting date value</param>
        /// <param name="Sh_D">Number of days</param>
        /// <param name="Sh_M"></param>
        /// <param name="Sh_Y"></param>
        /// <returns>Date moved</returns>
        static public int DateShift(int Dt, int Sh_D, int Sh_M, int Sh_Y)
        {
            int TempDay;
            DMY aDate;

            aDate.D = DayOf(Dt);
            aDate.M = MonthOf(Dt);
            aDate.Y = YearOf(Dt);

            if (aDate.Y != 0)                                                     // Increment the number of years if Dt is a 
                aDate.Y += Sh_Y;                                                  //   historical date (Y <> 0)               

            if (aDate.M != 0)                                                     // Shift the months                         
            {
                MonthShift(ref aDate, Sh_M);
                aDate.D = Math.Min(aDate.D, MonthLength(aDate.M, aDate.Y));        // If we move from, say 31 Aug to 31 Sep,   
            }                                                                      //   go back to the end of the month       

            if (aDate.D != 0)
            {
                TempDay = aDate.D + Sh_D;                                              // Use temporary storage for the day so     
                //   as to avoid overflows                 
                while (TempDay > MonthLength(aDate.M, aDate.Y))
                {
                    TempDay -= MonthLength(aDate.M, aDate.Y);
                    MonthShift(ref aDate, 1);
                }
                while (TempDay < 1)
                {
                    TempDay += MonthLength(aDate.M - 1, aDate.Y);
                    MonthShift(ref aDate, -1);
                }

                aDate.D = TempDay;                                                     // Return the day value to DMY(Dt)          
            } // _ IF (D <> 0) _

            return DateVal(aDate.D, aDate.M, aDate.Y);
        }

        //TODO: test this function
        /// <summary>
        ///  Interval between two dates.  Note that Interval(D,D) = 0.                 
        /// </summary>
        /// <param name="D1"></param>
        /// <param name="D2"></param>
        /// <returns></returns>
        static public int Interval(int D1, int D2)
        {
            int Sum;
            int I;
            DMY aDate, bDate;

            aDate.D = DayOf(D1);
            aDate.M = MonthOf(D1);
            aDate.Y = YearOf(D1);
            bDate.D = DayOf(D2);
            bDate.M = MonthOf(D2);
            bDate.Y = YearOf(D2);
            int result;

            if (D1 > D2)                                                        // If the first date is after the second, 
                result = -Interval(D2, D1);                                     // return a negative value                
            else if ((D1 == 0) || (D2 == 0))                                    //if any of the dates are zero
                result = 0;                                                     //interval is always zero
            else
            {
                if (aDate.M == 0)                                               //if the month is zero 
                {
                    aDate.D = 1;                                                //make the date 1 Jan
                    aDate.M = 1;
                }
                if (bDate.M == 0)                                               //if the month is zero 
                {
                    bDate.D = 1;                                                //make the date 1 Jan
                    bDate.M = 1;
                }

                Sum = 365 - CumulDays[aDate.M] - aDate.D;                       // Days to the end of the year of D1 

                if ((LeapYear(aDate.Y)) &&                                      // Add one if Feb 29 falls in the period 
                   ((aDate.M == 1) ||                                           // from D1 to the end of the year        
                    ((aDate.M == 2) && (aDate.D <= 29))))
                    Sum++;

                if (aDate.Y == bDate.Y)                                         // Now move to the end of the year       
                {                                                               // before that containing D2.            
                    Sum = Sum - 365;                                            // Same year; subtract the number of     
                    if (LeapYear(bDate.Y)) Sum--;                               // days in the year                      
                }
                else
                    for (I = aDate.Y + 1; I <= bDate.Y - 1; I++)                // Otherwise work through the            
                        if (LeapYear(I))                                        // intervening years, adding the number  
                            Sum += 366;                                         // of days in each                       
                        else
                            Sum += 365;

                Sum = Sum + CumulDays[bDate.M] + bDate.D;                       // add number of days from the start     
                // of the year of D2                     }
                if (LeapYear(bDate.Y) && (bDate.M >= 3))
                    Sum++;
                result = Sum;
            }
            return result;
        }

        /// <summary>
        /// Day of year function.                                                     
        ///   Dt     Date to convert to day-of-year                                   
        ///   UseYr  If TRUE, Feb 29 is counted if YearOf(D) is zero or a leap year.  
        ///          If FALSE, Feb 29 is counted regardless of the year.              
        /// </summary>
        /// <param name="Dt"></param>
        /// <param name="UseYr"></param>
        /// <returns></returns>
        static public int DOY(int Dt, bool UseYr)
        {
            DMY aDate;

            aDate.D = DayOf(Dt);
            aDate.M = MonthOf(Dt);
            aDate.Y = YearOf(Dt);
            if ((aDate.M <= 2) || (UseYr && !LeapYear(aDate.Y)))
                return CumulDays[aDate.M] + aDate.D;
            else
                return CumulDays[aDate.M] + 1 + aDate.D;
        }
    }
}