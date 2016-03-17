using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SWIMFrame
{
    interface ISink
    {
        void SetSinks(int n, int ns, int idrip, int idrn, double driprate, double[] dripsol, double dcond);
        void Wsinks(double t, int[] isat, double[] var, out double[,] qwex, out double[,] qwexd);
        void Ssinks(double t, double ti, double tf, int isol, double[,] dwex, double[] c, out double[,] qsex, out double[,] qsexd);
    }
}
