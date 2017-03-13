using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Models.Soils.SWIM4
{
    /// <summary>
    /// 
    /// </summary>
    public interface ISink
    {
        /// <summary></summary>
        int nex { get; set; }
        /// <summary></summary>
        void SetSinks(int n, int ns, int idrip, int idrn, double driprate, double[] dripsol, double dcond);
        /// <summary></summary>
        void Wsinks(double t, int[] isat, double[] var, double[] he, ref double[,] qwex, ref double[,] qwexd);
        /// <summary></summary>
        void Ssinks(double t, double ti, double tf, int isol, double[,] dwex, double[] c, ref double[,] qsex, ref double[,] qsexd);
    }
}
