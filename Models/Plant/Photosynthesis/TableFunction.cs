using System;

namespace Utilities
{
    public class TableFunction
    {
        private double[] _xVals;
        private double[] _yVals;
        private bool _flatEnds;

        public double[] yVals
        {
            get { return _yVals; }
            set { _yVals = value; }
        }

        public double[] xVals
        {
            get { return _xVals; }
            set { _xVals = value; }
        }

        public TableFunction(double[] x, double[] y, bool flatEnds = true)
        {
            _xVals = x;
            _yVals = y;

            _flatEnds = flatEnds;
        }
        //--------------------------------------------------------------------
        // Get the y value of the function
        //--------------------------------------------------------------------

        public double value(double v)
        {
            // Find which sector of the function that v falls in
            int sector;

            if (!_flatEnds)
            {
                if (_xVals.Length == 0)
                {
                    throw (new Exception("Array has no data"));
                }

                if (v < _xVals[0] || v > _xVals[_xVals.Length - 1])
                {
                    throw (new Exception("X value is outside the bounds of the Array"));
                }
            }

            for (sector = 0; sector < _xVals.Length; sector++)
            {
                if (v <= _xVals[sector])
                {
                    break;
                }
            }

            if (sector == 0)
            {
                return _yVals[0];
            }

            if (sector == _xVals.Length)
            {
                return _yVals[_yVals.Length - 1];
            }

            if (v == _xVals[sector])
            {
                return _yVals[sector];
            }

            double slope;
            try
            {

                slope = (_xVals[sector] == _xVals[sector - 1]) ? 0 :
                                 (_yVals[sector] - _yVals[sector - 1]) / (_xVals[sector] - _xVals[sector - 1]);
            }
            catch(DivideByZeroException dbz)
            {
                slope = 0;
            }

            return _yVals[sector - 1] + slope * (v - _xVals[sector - 1]);
        }
    }
}
