using System;
using System.Collections.Generic;
using System.Text;

public class SoilWat
   {
   [Output] public double[] ESW = new double[] { 10, 20 };
   [Output] public double[] dlayer = new double[] { 150, 150, 300, 300, 300, 300, 300 };
   [Output] public double[] dul = new double[] { 0.450, 0.459, 0.450, 0.440, 0.420, 0.410, 0.410 };
   [Output] public double[] ll15 = new double[] { 0.230, 0.240, 0.240, 0.250, 0.260, 0.270, 0.280 };
   [Output] public double[] sw = new double[] { 0.280, 0.364, 0.430, 0.430, 0.400, 0.410, 0.410 };
   [Output] public double[] bd = new double[] { 1.300, 1.300, 1.290, 1.310, 1.350, 1.360, 1.360 };
   [Output] public double[] no3 = new double[] { 9.2, 7.6, 4.9, 2.7, 1.5, 1.8, 1.4 };
   [Output] public double[] nh4 = new double[] { 0.30, 0.30, 0.30, 0.30, 0.30, 0.50, 0.50 };
   [Output] public double[] sw_dep
      {
      get
         {
         double[] values = new double[dlayer.Length];
         for (int i = 0; i < dlayer.Length; i++)
            values[i] = sw[i] * dlayer[i];
         return values;
         }
      }
   [Output]
   public double[] dul_dep
      {
      get
         {
         double[] values = new double[dlayer.Length];
         for (int i = 0; i < dlayer.Length; i++)
            values[i] = dul[i] * dlayer[i];
         return values;
         }
      }
   [Output]
   public double[] ll15_dep
      {
      get
         {
         double[] values = new double[dlayer.Length];
         for (int i = 0; i < dlayer.Length; i++)
            values[i] = ll15[i] * dlayer[i];
         return values;
         }
      }
   }
