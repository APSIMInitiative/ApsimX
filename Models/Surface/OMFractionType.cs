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
    }
}
