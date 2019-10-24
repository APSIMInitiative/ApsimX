namespace Models
{
    using System;
    using Models.Interfaces;

    /// <summary>Wraps a canopy object.</summary>
    [Serializable]
    public class CanopyType
    {
        /// <summary>The canopy.</summary>
        public ICanopy Canopy;

        /// <summary>The ktot</summary>
        public double Ktot;

        /// <summary>The k</summary>
        public double K;

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
        public CanopyType(ICanopy canopy)
        {
            Canopy = canopy;
        }

        /// <summary>Zero all variables.</summary>
        public void Clear()
        {
            Ktot = 0;
            K = 0;
            Array.Clear(LAI, 0, LAI.Length);
            Array.Clear(LAItot, 0, LAItot.Length);
            Array.Clear(Ftot, 0, Ftot.Length);
            Array.Clear(Fgreen, 0, Fgreen.Length);
            Array.Clear(Rs, 0, Rs.Length);
            Array.Clear(Rl, 0, Rl.Length);
            Array.Clear(Rsoil, 0, Rsoil.Length);
            Array.Clear(Gc, 0, Gc.Length);
            Array.Clear(Ga, 0, Ga.Length);
            Array.Clear(PET, 0, PET.Length);
            Array.Clear(PETr, 0, PETr.Length);
            Array.Clear(PETa, 0, PETa.Length);
            Array.Clear(Omega, 0, Omega.Length);
            Array.Clear(interception, 0, interception.Length);
        }
    }
}