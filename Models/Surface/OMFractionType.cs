using System;

namespace Models.Surface
{

    /// <summary>Type carrying information about the CNP composition of an organic matter fraction</summary>
    [Serializable]
    public class OMFractionType
    {
        /// <summary>The amount</summary>
        public double amount;
        /// <summary>The c</summary>
        public double C;
        /// <summary>The n</summary>
        public double N;
        /// <summary>The p</summary>
        public double P;

        /// <summary>Initializes a new instance of the <see cref="OMFractionType"/> class.</summary>
        public OMFractionType()
        {
            amount = 0;
            C = 0;
            N = 0;
            P = 0;
        }

        /// <summary>Remove a fraction of the material.</summary>
        public OMFractionType Remove(double fraction)
        {
            OMFractionType removed = new();

            removed.amount = amount * fraction;
            amount -= removed.amount;

            removed.C = C * fraction;
            C -= removed.C;

            removed.N = N * fraction;
            N -= removed.N;

            removed.P = P * fraction;
            P -= removed.P;

            return removed;
        }

        internal void Add(OMFractionType pool)
        {
            C += pool.C;
            N += pool.N;
            P += pool.P;
        }

        internal void Add(FOMType fomType)
        {
            C += fomType.C;
            N += fomType.N;
            P += fomType.P;
        }
    }
}
