using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Core;

namespace Models.PMF.OldPlant
{
    [Serializable]
    public class PStress : Model
    {
        public double Photo
        {
            get
            {
                return 1.0;
            }
        }

        public double Pheno
        {
            get
            {
                return 1.0;
            }
        }

        public double Expansion
        {
            get
            {
                return 1.0;
            }
        }

        public void DoPlantSWStress()
        {
        }



    }
}
