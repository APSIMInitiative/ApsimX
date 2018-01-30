using System;
using System.Collections.Generic;

namespace Utilities
   {
    /// <summary></summary>
    public class LookupTable
      {
      private List<double> _XVals;
      private List<double> _YVals;
      //---------------------------------------------------------------------------
      /// <summary>
      /// 
      /// </summary>
      public LookupTable()
         {
         _XVals = new List<double>();
         _YVals = new List<double>();
         }
      //---------------------------------------------------------------------------
      /// <summary>
      /// 
      /// </summary>
      /// <param name="_xVals"></param>
      /// <param name="_yVals"></param>
      public LookupTable(double[] _xVals, double[] _yVals)
         : this()
         {
         _XVals.InsertRange(0, _xVals);
         _YVals.InsertRange(0, _yVals);
         }
      //---------------------------------------------------------------------------
      /// <summary>
      /// 
      /// </summary>
      /// <param name="fileName"></param>
      /// <param name="headerRow"></param>
      public LookupTable(String fileName, bool headerRow) : this()
         {
         //StreamReader sr = new StreamReader(fileName);

         //String line;
         //String[] words;

         //char[] charSeparators = { ' ' };

         //if (headerRow)
         //   {
         //   //Throw away the first line
         //   sr.ReadLine();
         //   }

         ////Read the data
         //while (!sr.EndOfStream)
         //   {
         //   line = sr.ReadLine();

         //   words = line.Split(charSeparators, StringSplitOptions.RemoveEmptyEntries);
         //   if (words.Length > 1)
         //      {
         //      _XVals.Add(double.Parse(words[0]));
         //      _YVals.Add(double.Parse(words[1]));
         //      }
         //   }
         //sr.Close();
         /*
         String[]lines = File.ReadAllLines(fileName);
         String[] words;

         char[] charSeparators = { ' ' };

         int counter = 0;
         if (headerRow)
            {
            //Throw away the first line
            counter = 1;
            }

         //Read the data
         while(counter < lines.Length)
            {
            words = lines[counter].Split(charSeparators, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length > 1)
               {
               _XVals.Add(double.Parse(words[0]));
               _YVals.Add(double.Parse(words[1]));
               }
            }*/
         }
      //---------------------------------------------------------------------------
      /// <summary>
      /// 
      /// </summary>
      /// <param name="xVal"></param>
      /// <returns></returns>
      public double getYVal(double xVal)
         {
         if(xVal == _XVals[_XVals.Count - 1])
            {
            return _YVals[_YVals.Count - 1];
            }

         double? yVal = null;

         //Find the psition of xVal
         for (int i = 0; i < _XVals.Count - 1; i++)
            {
            if (xVal >= _XVals[i] && xVal < _XVals[i + 1])
               {
               double dX = _XVals[i + 1] - _XVals[i];
               double dY = _YVals[i + 1] - _YVals[i];

               yVal = (((xVal - _XVals[i]) / dX) * dY) + _YVals[i];
               }
            }
         if (yVal.HasValue)
            {
            return yVal.Value;
            }
         else
            {
            throw (new IndexOutOfRangeException());
            }
         }
        //---------------------------------------------------------------------------
        /// <summary></summary>
        public double maxX
         {
         get
            {
            return _XVals[_XVals.Count - 1];
            }
         set
            {
            }
         }
      }
   }
