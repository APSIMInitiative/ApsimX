using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Core;

namespace Models.PMF.OldPlant
{
    class Conversions
    {
        // WEIGHT conversion
        public const double gm2kg = (float)(1.0 / 1000.0);   // constant to convert g to kg
        public const double kg2gm = 1000.0f;               // conversion of kilograms to grams
        public const double mg2gm = (float)(1.0 / 1000.0);   // conversion of mg to grams
        public const double t2g = (float)(1000.0 * 1000.0);  // tonnes to grams
        public const double g2t = (float)(1.0 / t2g);       // grams to tonnes
        public const double t2kg = 1000.0f;                // tonnes to kilograms
        public const double kg2t = (float)(1.0 / t2kg);     // kilograms to tonnes

        // AREA conversion
        public const double ha2scm = (float)(10000.0 * 10000.0); // ha to sq cm
        public const double ha2sm = 10000.0f;              // conversion of hectares to sq metres
        public const double sm2ha = (float)(1.0 / 10000.0);  // constant to convert m^2 to hectares
        public const double sm2smm = 1000000.0f;           // conversion of square metres to square mm
        public const double smm2sm = (float)(1.0 / 1000000.0); // conversion factor of mm^2 to m^2
        public const double scm2smm = 100.0f;              // conversion factor of cm^2 to mm^2

        // PRESSURE and related conversion
        public const double g2mm = (float)(1.0e3 / 1.0e6);  // convert g water/m^2 to mm water
        // 1 g water = 1,000 cubic mm and
        // 1 sq m = 1,000,000 sq mm
        public const double mb2kpa = (float)(100.0 / 1000.0);  // convert pressure mbar to kpa
        // 1000 mbar = 100 kpa

        // LENGTH conversion
        public const double cm2mm = 10.0f;                 // cm to mm
        public const double mm2cm = (float)(1.0 / 10.0);     // conversion of mm to cm
        public const double mm2m = (float)(1.0 / 1000.0);    // conversion of mm to m
        public const double km2m = 1000.0f;               // conversion of km to metres

        // VOLUME conversion
        public const double cmm2cc = (float)(1.0 / 1000.0);     // conversion of cubic mm to cubic cm
        public const double conv_gmsm2kgha = 100.0f;       // convert g/sm -> kg/ha
        public const double conv_pc2fr = 0.01f;            // convert %age to fraction
        public const double pcnt2fract = (float)(1.0 / 100.0);  // convert percent to fraction
        public const double fract2pcnt = 100.0f;           // convert fraction to percent
        public const double mm2lpsm = 1.0f;                // mm depth to litres per sq m
        public const double lpsm2mm = 1.0f;                // litres per sq m to mm depth
        public const double day2hr = 24.0f;               // days to hours
        public const double hr2s = (float)(60.0 * 60.0);  // hours to seconds
        public const double s2hr = (float)(1.0 / hr2s);   // seconds to hours

    }

}