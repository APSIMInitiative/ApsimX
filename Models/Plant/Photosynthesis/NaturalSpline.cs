using System;

namespace Models.PMF.Phenology
{
    class NaturalSpline
    {
        private double[] sx;    //The array of sample x values
        private double[] sy;    //The array of corresponding sample y values
        private double[] sc;    //An array of interpolation constants
       // private int count;      //The number of entries in the above arrays

        public NaturalSpline(double[] x, double[] y)
        {
            if (x.Length != y.Length)
            {
                throw (new Exception("Arrays must be the same size"));
            }

            sx = new double[x.Length];
            sy = new double[x.Length];
            sc = new double[x.Length];
            for (int i = 0; i < x.Length; i++)
            {
                sx[i] = x[i];
                sy[i] = y[i];
                sc[i] = 0.0;
            }

            setUp();
        }

        //---------------------------------------------------------------------------
        //This routine is based on "Numerical Recipes in C"
        //by Press, et al. (1988)
        //---------------------------------------------------------------------------
        private void setUp()
        {

            double r1, r2;                 //Intermediate results
            double[] tsv = new double[sx.Length];    //Temporary storage vector

            sc[1] = 0.0;
            tsv[1] = 0.0;

            for (int i = 1; i < (sx.Length - 2); i++)
            {
                r2 = 0;
                if (sx[(i + 1)] != sx[(i - 1)])  //This shouldn't happen - multiple values for x
                {
                    r2 = (sx[i] - sx[(i - 1)]) / (sx[(i + 1)] - sx[(i - 1)]);
                }
                r1 = r2 * sc[(i - 1)] + 2;
                sc[i] = (r2 - 1) / r1;
                tsv[i] = 0;
                if ((sx[i] - sx[(i - 1)]) != 0)  //This shouldn't happen - multiple values for x
                {
                    tsv[i] = (sy[(i + 1)] - sy[i]) / (sx[(i + 1)] - sx[i]) - (sy[i] - sy[(i - 1)]) / (sx[i] - sx[(i - 1)]);
                }

                tsv[i] = (6 * tsv[i] / (sx[(i + 1)] - sx[(i - 1)]) - r2 * tsv[(i - 1)]) / r1;
            }

            sc[(sx.Length - 1)] = 0;

            for (int i = (sx.Length - 2); i >= 0; i--)
            {
                sc[i] = sc[i] * sc[i + 1] + tsv[i];
            }
            return;
        }

        //--------------------------------------------------------------------------
        // This routine is based on "Numerical Recipes in C" 
        // by Press, et al. (1988)                           
        // double xVal,   The x value for which f(x) is desired            
        //---------------------------------------------------------------------------
        public double getValue(double xVal)
        {
            //Local Vars
            int ll, lu, m;
            double dx, dxl, dxu, Y;

            ll = 0;
            lu = sx.Length - 1;

            m = (lu - ll) / 2;

            while (lu - ll > 1)
            {
                m = (lu + ll) / 2;
                if (sx[m] > xVal)
                {
                    lu = m;
                }
                else
                {
                    ll = m;
                }
            }
            dx = sx[lu] - sx[ll];
            dxu = (sx[lu] - xVal) / dx;
            dxl = (xVal - sx[ll]) / dx;

            Y = dxu * sy[ll] + dxl * sy[lu] + ((dxu * dxu * dxu - dxu) *
                     sc[ll] + (dxl * dxl * dxl - dxl) * sc[lu]) * (dx * dx) / 6.0;

            return Y;
        }
    }
}
