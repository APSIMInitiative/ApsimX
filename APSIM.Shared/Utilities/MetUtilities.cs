namespace APSIM.Shared.Utilities
{
    using System;
    using System.Data;
    using System.Globalization;

    /// <summary>
    /// A collection of weather utility functions
    /// </summary>
    public class MetUtilities
    {
        //atmospheric pressure (Pa)
        /// <summary>The p</summary>
        public const double P = 101000.0;
        //molecular weight of dry air (g/mole)
        /// <summary>The ma</summary>
        public const double Ma = 28.966;
        //specific heat of air (J/g/K)
        /// <summary>The cp</summary>
        public const double Cp = 1.01;
        //Gas constand (J/mole/K)
        /// <summary>The r</summary>
        public const double R = 8.3143;
        //Latent heat of vaporisation (J/g)
        /// <summary>The l</summary>
        public const double L = 2440;

        // iterations
        /// <summary>The itns</summary>
        public const double itns = 10.0;
        //Atmospheric Transmissivity at Azimuth - From Thornton and Running.
        /// <summary>The taz</summary>
        public const double Taz = 0.87;
        // Decrease in Atmospheric Transmissivity per unit water vapour - From Thornton and Running
        /// <summary>The alpha</summary>
        public const double Alpha = 0.0061;


        /// <summary>Relatives the hum.</summary>
        /// <param name="Ta">The ta.</param>
        /// <param name="Tw">The tw.</param>
        /// <returns></returns>
        public static double RelHum(double Ta, double Tw)
        {
            // Relative Humidity - unitless
            // Ta is air temperature (oC)
            // Tw is wet bulb temperature (oC)

            double RhoV_t = 0;
            double RhoVs_t = 0;
            RhoV_t = RhoVs(Tw) - gamma(Ta) * (Ta - Tw);
            RhoVs_t = RhoVs(Ta);
            return RhoV_t / RhoVs_t;
        }

        /// <summary>Rhoes the v.</summary>
        /// <param name="Ta">The ta.</param>
        /// <param name="Tw">The tw.</param>
        /// <returns></returns>
        public static double RhoV(double Ta, double Tw)
        {
            // Vapour Density
            return RhoVs(Tw) - gamma(Ta) * (Ta - Tw);
        }

        /// <summary>Rhoes the vs.</summary>
        /// <param name="T">The t.</param>
        /// <returns></returns>
        public static double RhoVs(double T)
        {
            // Saturated Vapour Density
            double Tk = 0;
            Tk = T + 273.3;
            return 217 * (System.Math.Exp(54.87819 - (6790.4985 / Tk + 5.02808 * System.Math.Log(Tk)))) / Tk;
        }

        /// <summary>Gammas the specified t.</summary>
        /// <param name="T">The t.</param>
        /// <returns></returns>
        public static double gamma(double T)
        {
            double Tk = 0;
            // Absolute Temperature in Kelvin
            Tk = T + 273.3;
            return P * Ma * Cp / (R * Tk * L);
        }

        /// <summary>SVPs the specified temp_c.</summary>
        /// <param name="temp_c">The temp_c.</param>
        /// <returns></returns>
        public static double svp(double temp_c)
        {
            //Saturation Vapour Pressure
            return 6.1078 * System.Math.Exp(17.269 * temp_c / (237.3 + temp_c));
        }

        // ------------------------------------------------------------------------
        /// <summary>Day_lengthes the specified day.</summary>
        /// <param name="day">The day.</param>
        /// <param name="lat">The lat.</param>
        /// <param name="sun_angle">The sun_angle.</param>
        /// <returns></returns>
        public static double day_length(int day, double lat, double sun_angle)
        {
            // ------------------------------------------------------------------------
            double DEC = 0;
            // Declination - degrees
            double DECr = 0;
            // Declination - radians
            double LATr = 0;
            // Latitude - radians
            double HS = 0;
            // Hour angle
            double sun_alt = 0;
            // solar altitude
            double coshra = 0;
            // Cosine of hour angle
            double slsd = 0;
            double clcd = 0;
            double altmn = 0;
            double altmx = 0;
            double alt = 0;

            sun_alt = sun_angle * 2.0 * System.Math.PI / 365.25;

            DEC = 23.45 * System.Math.Sin(2.0 * System.Math.PI / 365.25 * (day - 79.25));
            DECr = DEC * 2.0 * System.Math.PI / 360.0;
            LATr = lat * 2.0 * System.Math.PI / 360.0;

            if (lat == 90)
            {
                coshra = System.Math.Sign(-DEC) * System.Math.Sign(lat);
            }
            else
            {
                slsd = System.Math.Sin(LATr) * System.Math.Sin(DECr);
                clcd = System.Math.Cos(LATr) * System.Math.Cos(DECr);

                altmn = System.Math.Asin(MathUtilities.Bound(slsd - clcd, -1.0, 1.0));
                altmx = System.Math.Asin(MathUtilities.Bound(slsd + clcd, -1.0, 1.0));
                alt = MathUtilities.Bound(sun_alt, altmn, altmx);

                // get cos of the hour angle

                coshra = (System.Math.Sin(alt) - slsd) / clcd;
                coshra = MathUtilities.Bound(coshra, -1.0, 1.0);
            }

            HS = System.Math.Acos(coshra);

            return HS * 2.0 * 24.0 / (2.0 * System.Math.PI);

        }

        /// <summary>Tands the r_radn.</summary>
        /// <param name="day">The day.</param>
        /// <param name="lat">The lat.</param>
        /// <param name="dT">The d t.</param>
        /// <param name="vp">The vp.</param>
        /// <param name="rain">The rain.</param>
        /// <param name="dT30">The d T30.</param>
        /// <returns></returns>
        public static double TandR_radn(int day, double lat, double dT, double vp, double rain, double dT30)
        {
            double TransMax = 0;
            double b = 0;
            double c = 0;
            double Tfmax = 0;

            // NOTE - this needs to be converted back to stardard published approach

            TransMax = TMax(day, lat, Taz, Alpha, vp);

            //b = 0.031 + 0.201 * Exp(-0.185 * dT30)
            b = 0.08;

            c = 1.5;

            //If rain > 0 Then
            //   dT = dT * 0.75
            //End If

            Tfmax = 1 - 0.9 * System.Math.Exp(-b * System.Math.Pow(dT, c));
            if (rain > 25)
            {
                Tfmax = Tfmax * 0.5;
                //Tfmax = max(Tfmax, 0.5)
            }

            return Q0(day, lat) * Tfmax * TransMax;

        }


        /// <summary>Q0s the specified day.</summary>
        /// <param name="day">The day.</param>
        /// <param name="lat">The lat.</param>
        /// <returns></returns>
        public static double Q0(int day, double lat)
        {
            double functionReturnValue = 0;

            // Total Daily Extraterrestrial SW radiation - MJ

            double DEC = 0;
            double DECr = 0;
            double LATr = 0;
            double HS = 0;


            DEC = 23.45 * System.Math.Sin(2.0 * System.Math.PI / 365.25 * (day - 79.25));
            DECr = DEC * 2.0 * System.Math.PI / 360.0;
            LATr = lat * 2.0 * System.Math.PI / 360.0;

            HS = System.Math.Acos(-System.Math.Tan(LATr) * System.Math.Tan(DECr));
            functionReturnValue = 86400.0 * 1360.0 * (HS * System.Math.Sin(LATr) * System.Math.Sin(DECr) + System.Math.Cos(LATr) * System.Math.Cos(DECr) * System.Math.Sin(HS)) / System.Math.PI;
            functionReturnValue = functionReturnValue / 1000000.0;
            return functionReturnValue;

        }

        // ------------------------------------------------------------------------
        /// <summary>Transmissivities the specified day.</summary>
        /// <param name="day">The day.</param>
        /// <param name="lat">The lat.</param>
        /// <param name="radn">The radn.</param>
        /// <returns></returns>
        public static double Transmissivity(int day, double lat, double radn)
        {
            // ------------------------------------------------------------------------
            return radn / Q0(day, lat);
        }

        // ------------------------------------------------------------------------
        /// <summary>Q0is the specified day.</summary>
        /// <param name="day">The day.</param>
        /// <param name="HS">The hs.</param>
        /// <param name="lat">The lat.</param>
        /// <returns></returns>
        public static double Q0i(int day, double HS, double lat)
        {
            double functionReturnValue = 0;
            // ------------------------------------------------------------------------

            // Instantaneous Extraterrestrial SW radiation - MJ

            double DEC = 0;
            double DECr = 0;
            double LATr = 0;
            double HSmax = 0;

            HSmax = System.Math.Acos(-System.Math.Tan(LATr) * System.Math.Tan(DECr));
            // half daylength

            if ((HS <= HSmax))
            {
                DEC = 23.45 * System.Math.Sin(2.0 * System.Math.PI / 365.25 * (day - 79.25));

                DECr = DEC * 2.0 * System.Math.PI / 360.0;
                LATr = lat * 2.0 * System.Math.PI / 360.0;

                functionReturnValue = 1360.0 * (System.Math.Sin(LATr) * System.Math.Sin(DECr) + System.Math.Cos(LATr) * System.Math.Cos(DECr) * System.Math.Cos(HS));
                functionReturnValue = functionReturnValue / 1000000.0;
                // Convert to MJ
            }
            else
            {
                functionReturnValue = 0;
            }
            return functionReturnValue;

        }

        // ------------------------------------------------------------------------
        /// <summary>Q0ints the specified day.</summary>
        /// <param name="day">The day.</param>
        /// <param name="lat">The lat.</param>
        /// <returns></returns>
        public static double Q0int(int day, double lat)
        {
            double functionReturnValue = 0;
            // ------------------------------------------------------------------------
            // Total Daily Extraterrestrial SW radiation - MJ
            // Integrated from instantaneous values of S0

            double DEC = 0;
            double DECr = 0;
            double LATr = 0;
            double T = 0;
            //time of day as hour angle in radians
            double S1 = 0;
            double S2 = 0;
            double HS = 0;

            DEC = 23.45 * System.Math.Sin(2.0 * System.Math.PI / 365.25 * (day - 79.25));
            DECr = DEC * 2.0 * System.Math.PI / 360.0;
            LATr = lat * 2.0 * System.Math.PI / 360.0;

            HS = System.Math.Acos(-System.Math.Tan(LATr) * System.Math.Tan(DECr));
            // half daylength

            // Integrate using trapezoidal rule type thing
            functionReturnValue = 0.0;
            for (T = HS / itns; T <= HS; T += HS / itns)
            {
                S1 = Q0i(day, T - HS / itns, lat);
                S2 = Q0i(day, T, lat);
                functionReturnValue = functionReturnValue + (S1 + S2) / 2 / itns;
            }
            functionReturnValue = functionReturnValue * HS / (2.0 * System.Math.PI / (24.0 * 60.0 * 60.0));
            functionReturnValue = functionReturnValue * 2.0;
            return functionReturnValue;


        }
        // ------------------------------------------------------------------------
        /// <summary>qs the maximum.</summary>
        /// <param name="day">The day.</param>
        /// <param name="lat">The lat.</param>
        /// <param name="Taz">The taz.</param>
        /// <param name="alpha">The alpha.</param>
        /// <param name="vp">The vp.</param>
        /// <returns></returns>
        public static double QMax(int day, double lat, double Taz, double alpha, double vp)
        {
            double functionReturnValue = 0;
            // ------------------------------------------------------------------------
            double DEC = 0;
            double DECr = 0;
            double LATr = 0;
            double T = 0;
            //time of day as hour angle in radians
            double S1 = 0;
            double S2 = 0;
            double HS = 0;
            // Sunrise hour angle
            double M = 0;
            // Mixing Length

            DEC = 23.45 * System.Math.Sin(2.0 * System.Math.PI / 365.25 * (day - 79.25));
            DECr = DEC * 2.0 * System.Math.PI / 360.0;
            LATr = lat * 2.0 * System.Math.PI / 360.0;

            HS = System.Math.Acos(-System.Math.Tan(LATr) * System.Math.Tan(DECr));
            // half daylength

            // Integrate using trapezoidal rule type thing
            functionReturnValue = 0.0;
            for (T = HS / itns; T <= HS; T += HS / itns)
            {
                //avoid very small angles causing numerical errors
                T = System.Math.Min(T, HS * 0.999);
                S1 = Q0i(day, T - HS / itns, lat);
                S2 = Q0i(day, T, lat);
                // note that sin(a) = cos(z) therefore
                M = 1 / (System.Math.Sin(LATr) * System.Math.Sin(DECr) + System.Math.Cos(LATr) * System.Math.Cos(DECr) * System.Math.Cos(T));

                if (M > 2)
                {
                    // 1/cos(z) rule breaks down at low angles due to earth's curvature
                    // add correction fitted to Smithsonian met table data
                    M = 2.0 + System.Math.Pow((M - 2.0), 0.955);
                }
                functionReturnValue = functionReturnValue + (S1 + S2) / 2 / itns * ((System.Math.Pow(Taz, M)) - alpha * vp);
            }
            functionReturnValue = functionReturnValue * HS / (2.0 * System.Math.PI / (24.0 * 60.0 * 60.0));
            functionReturnValue = functionReturnValue * 2.0;
            return functionReturnValue;

        }

        /// <summary>Calculates the MaxRadiation on all rows in datatable</summary>
        /// <param name="table">The datatable containing weather data</param>
        /// <param name="latitude">The latitude for the weather data location</param>
        public static void CalcQmax(DataTable table, double latitude)
        {
            if (!double.IsNaN(latitude))
            {
                // ----------------------------------------------------------------------------------
                // Add a calculated QMax column to the daily data.
                // ----------------------------------------------------------------------------------
                if (((table.Columns["Qmax"] == null)))
                {
                    table.Columns.Add("Qmax", Type.GetType("System.Single"));
                }
                // Do we have a VP column?
                bool haveVPColumn = (table.Columns["VP"] != null);

                //do we have a "doy" or "day" column, and which column is it in
                int dayCol = table.Columns.IndexOf("day");
                if (dayCol < 0)
                    dayCol = table.Columns.IndexOf("doy");
                bool haveDOYColumn = dayCol >= 0;

                // Loop through all rows and calculate a QMax
                DateTime cDate;
                DataRow row;
                int doy = 0;
                for (int r = 0; r <= table.Rows.Count - 1; r++)
                {
                    if (haveDOYColumn == true)
                    {
                        doy = Convert.ToInt16(table.Rows[r][dayCol], CultureInfo.InvariantCulture);
                    }
                    else {
                        row = table.Rows[r];
                        cDate = DataTableUtilities.GetDateFromRow(row);
                        doy = cDate.DayOfYear;
                    }

                    if (haveVPColumn && !Convert.IsDBNull(table.Rows[r]["vp"]))
                    {
                        table.Rows[r]["Qmax"] = MetUtilities.QMax(doy + 1, latitude, MetUtilities.Taz, MetUtilities.Alpha,
                            Convert.ToSingle(table.Rows[r]["vp"], CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        table.Rows[r]["Qmax"] = MetUtilities.QMax(doy + 1, latitude, MetUtilities.Taz, MetUtilities.Alpha,
                            MetUtilities.svp(Convert.ToSingle(table.Rows[r]["mint"], CultureInfo.InvariantCulture)));
                    }
                }
            }
        }



        // ------------------------------------------------------------------------
        /// <summary>ts the maximum.</summary>
        /// <param name="day">The day.</param>
        /// <param name="lat">The lat.</param>
        /// <param name="Taz">The taz.</param>
        /// <param name="alpha">The alpha.</param>
        /// <param name="vp">The vp.</param>
        /// <returns></returns>
        public static double TMax(int day, double lat, double Taz, double alpha, double vp)
        {
            // ------------------------------------------------------------------------
            double S = 0;
            double Q = 0;
            S = QMax(day, lat, Taz, alpha, vp);
            Q = Q0(day, lat);
            return S / Q;

        }
        // ------------------------------------------------------------------------
        /// <summary>ms the specified day.</summary>
        /// <param name="day">The day.</param>
        /// <param name="lat">The lat.</param>
        /// <param name="HS">The hs.</param>
        /// <returns></returns>
        public static double M(double day, double lat, double HS)
        {
            double functionReturnValue = 0;
            // ------------------------------------------------------------------------
            double DEC = 0;
            double DECr = 0;
            double LATr = 0;
            double HSmax = 0;

            DEC = 23.45 * System.Math.Sin(2.0 * 3.14159265 / 365.25 * (day - 79.25));
            DECr = DEC * 2.0 * 3.14159265 / 360.0;
            LATr = lat * 2.0 * 3.14159265 / 360.0;

            HSmax = System.Math.Acos(-System.Math.Tan(LATr) * System.Math.Tan(DECr));
            // half daylength
            HS = System.Math.Min(HS, HSmax * 0.99);

            // note that sin(a) = cos(z) therefore
            functionReturnValue = 1 / (System.Math.Sin(LATr) * System.Math.Sin(DECr) + System.Math.Cos(LATr) * System.Math.Cos(DECr) * System.Math.Cos(HS));

            if (functionReturnValue > 2)
            {
                // 1/cos(z) rule breaks down at low angles due to earth's curvature
                // add correction fitted to Smithsonian met table data
                functionReturnValue = 2.0 + System.Math.Pow((functionReturnValue - 2.0), 0.955);
                functionReturnValue = System.Math.Min(functionReturnValue, 20.0);
                functionReturnValue = System.Math.Max(functionReturnValue, 0.0);

            }
            return functionReturnValue;

        }
        // ------------------------------------------------------------------------
        /// <summary>Mints the specified day.</summary>
        /// <param name="day">The day.</param>
        /// <param name="lat">The lat.</param>
        /// <param name="HS1">The h s1.</param>
        /// <param name="HS2">The h s2.</param>
        /// <returns></returns>
        public static double Mint(double day, double lat, double HS1, double HS2)
        {
            double functionReturnValue = 0;
            // ------------------------------------------------------------------------
            double dHS = 0;
            double start = 0;
            double T = 0;
            double M1 = 0;
            double M2 = 0;
            int i = 0;

            dHS = System.Math.Abs(HS2 - HS1);
            start = System.Math.Min(HS1, HS2);

            functionReturnValue = 0.0;

            for (i = 1; i <= Convert.ToInt32(itns, CultureInfo.InvariantCulture); i++)
            {
                T = start + i * dHS / itns;
                M1 = M(day, lat, T - dHS / itns);
                M2 = M(day, lat, T);
                functionReturnValue = functionReturnValue + (M1 + M2) / 2 / itns;
            }
            return functionReturnValue;
            //Beep
        }

        // ------------------------------------------------------------------------
        /// <summary>Q0iints the specified day.</summary>
        /// <param name="day">The day.</param>
        /// <param name="lat">The lat.</param>
        /// <param name="HS1">The h s1.</param>
        /// <param name="HS2">The h s2.</param>
        /// <returns></returns>
        public static double Q0iint(int day, double lat, double HS1, double HS2)
        {
            double functionReturnValue = 0;
            double T = 0;
            double S1 = 0;
            double S2 = 0;
            double dHS = 0;
            double start = 0;
            double finish = 0;

            functionReturnValue = 0.0;
            dHS = System.Math.Abs(HS2 - HS1);
            start = System.Math.Min(HS1, HS2);
            finish = System.Math.Max(HS1, HS2);

            for (T = start + dHS / itns; T <= finish; T += dHS / itns)
            {
                S1 = Q0i(day, T - dHS / itns, lat);
                S2 = Q0i(day, T, lat);
                functionReturnValue = functionReturnValue + (S1 + S2) / 2 / itns;
            }
            return functionReturnValue;

        }
    }

}
