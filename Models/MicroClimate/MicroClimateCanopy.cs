using System;
using Models.Interfaces;

namespace Models
{

    /// <summary>A micro climate wrapper around a ICanopy instance.</summary>
    [Serializable]
    public class MicroClimateCanopy
    {
        /// <summary>The canopy.</summary>
        public ICanopy Canopy;

        /// <summary>The ktot</summary>
        public double Ktot;

        /// <summary>The k</summary>
        public double K;

        /// <summary>The area of the canopy in m2 </summary>
        /// Only != 1 if TreeRow radiation mode is used as it needs to adjust ET components for area to go from mm to L)
        public double CanopyArea = 1.0;

        /// <summary>The layer lai</summary>
        public double[] LAI;

        /// <summary>The layer la itot</summary>
        public double[] LAItot;

        /// <summary>The ftot</summary>
        public double[] Ftot;

        /// <summary>The fgreen</summary>
        public double[] Fgreen;

        /// <summary>The rs</summary>
        public double[] Rs;

        /// <summary>The rl</summary>
        public double[] Rl;

        /// <summary>The rsoil</summary>
        public double[] Rsoil;

        /// <summary>The gc</summary>
        public double[] Gc;

        /// <summary>The ga</summary>
        public double[] Ga;

        /// <summary>The pet</summary>
        public double[] PET;

        /// <summary>The pe tr</summary>
        public double[] PETr;

        /// <summary>The pe ta</summary>
        public double[] PETa;

        /// <summary>The omega</summary>
        public double[] Omega;

        /// <summary>The interception</summary>
        public double[] interception;

        /// <summary>Constructor</summary>
        /// <param name="canopy">The canopy to wrap.</param>
        public MicroClimateCanopy(ICanopy canopy)
        {
            Canopy = canopy;
        }
    }
}