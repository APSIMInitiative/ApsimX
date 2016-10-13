using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SWIMFrame
{
    public class SolProps
    {
        public string[,] isotype;   // adsorption isotherm code for soil types.
        public double[] bd;         // bulk densities for soil types.
        public double[] dis;        // dispersivities for soil types.
        public double[,][] isopar;    // adsorption isotherm params for soil types.
        int np;

        /// <summary>
        /// Allocate soil solute parameters
        /// </summary>
        /// <param name="nt">Number of soil hydraulic property types.</param>
        /// <param name="ns">Number of solutes.</param>
        public SolProps(int nt, int ns)
        {
            isotype = new string[ns + 1, nt + 1];
            bd = new double[nt + 1];
            dis = new double[nt + 1];
            isopar = new double[ns + 1, nt + 1][];
            for (int j = 1; j < isotype.GetLength(0); j++)
                for (int x = 1; x < isotype.GetLength(1); x++)
                isotype[j, x] = "no"; //will be changed if required in sub setiso
        }

        /// <summary>
        /// Set soil solute property parameters.
        /// </summary>
        /// <param name="">Soil type number.</param>
        /// <param name="bdj">Soil bulk density.</param>
        /// <param name="disj">Dispersivity.</param>
        public void Solpar(int j, double bdj, double disj)
        {
            bd[j] = bdj;
            dis[j] = disj;
        }

        /// <summary>
        /// Set soil solute adsorption isotherm and parameters.
        /// </summary>
        /// <param name="j">Soil type number</param>
        /// <param name="isol">Solute number</param>
        /// <param name="isotypeji">Isotherm code.</param>
        /// <param name="isoparji">Isotherm parameters.</param>
        public void Setiso(int j, int isol, string isotypeji, double[] isoparji)
        {
            isotype[isol, j] = isotypeji;
            np = isoparji.Length - 1; // -1 to ignore 0th element
            if (isotypeji == "Fr")
                isopar[isol, j] = new double[np + 2 + 1]; //check these
            else
                isopar[isol, j] = new double[np + 1];
            Array.Copy(isoparji, isopar[isol, j], isoparji.Length);
        }

        /// <summary>
        /// Subroutine to get adsorbed solute (units/g soil) from concn in soil water
        /// according to chosen isotherm code("Fr" for Freundlich, "La" for Langmuir
        /// and "Ll" for Langmuir-linear).
        /// </summary>
        /// <param name="iso">2 character code.</param>
        /// <param name="c">Concentration in soil water.</param>
        /// <param name="dsmmax">Max solute change per time step.</param>
        /// <param name="p">Isotherm parameters.</param>
        /// <param name="f">Adsorbed mass per gram of soil.</param>
        /// <param name="fd">Derivitive of f wrt c.</param>
        public void Isosub(string iso, double c, double dsmmax, ref double[] p, out double f, out double fd)
        {
            double x;
            f = 0;
            fd = 0;
            switch(iso)
            {
                case "Fr":
                    if(p[3]==0)
                    {
                        p[3] = Math.Pow(0.01 * dsmmax / p[1], 1.0 / p[2]); //concn at 0.01*dsmmax
                        p[4] = Math.Pow(p[1] * p[3], p[2] - 1.0); // slope
                    }
                    if(c<p[3])
                    {
                        fd = p[4];
                        f = fd * c;
                    }
                    else
                    {
                        x = p[1] * Math.Exp((p[2] - 1.0) * Math.Log(c));
                        f = x * c;
                        fd = p[2] * x;
                    }
                    break;
                case "La":
                    x = 1.0 / (1.0 + p[2] * c);
                    f = p[1] * c * x;
                    fd = p[1] * (x - p[2] * c * Math.Pow(x, 2));
                    break;
                case "Ll":
                    x = 1.0 / (1.0 + p[2] * c);
                    f = p[1] * c * x + p[3] * c;
                    fd = p[1] * (x - p[2] * c * Math.Pow(x, 2)) + p[3];
                    break;
                default:
                    Console.WriteLine("isosub: illegal isotherm type.");
                    Environment.Exit(1);
                    break;
            }
        }
    }
}
