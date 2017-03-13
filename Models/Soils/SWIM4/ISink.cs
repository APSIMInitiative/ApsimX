using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Models.Soils.SWIM4
{
    /// <summary>
    /// 
    /// </summary>
    public interface ISoil
    {
        int nex { get; set; }
        void SetSinks(int n, int ns, int idrip, int idrn, double driprate, double[] dripsol, double dcond);
        void Wsinks(double t, int[] isat, double[] var, double[] he, ref double[,] qwex, ref double[,] qwexd);
        void Ssinks(double t, double ti, double tf, int isol, double[,] dwex, double[] c, ref double[,] qsex, ref double[,] qsexd);
    }
}
