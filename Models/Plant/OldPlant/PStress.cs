using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Core;

namespace Models.PMF.OldPlant
{
    /// <summary>
    /// Phosphorus stress model
    /// </summary>
    [Serializable]
    public class PStress : Model
    {
        /// <summary>Gets the photo.</summary>
        /// <value>The photo.</value>
        public double Photo
        {
            get
            {
                return 1.0;
            }
        }

        /// <summary>Gets the pheno.</summary>
        /// <value>The pheno.</value>
        public double Pheno
        {
            get
            {
                return 1.0;
            }
        }

        /// <summary>Gets the expansion.</summary>
        /// <value>The expansion.</value>
        public double Expansion
        {
            get
            {
                return 1.0;
            }
        }

        /// <summary>Does the plant sw stress.</summary>
        public void DoPlantSWStress()
        {
        }



    }
}
