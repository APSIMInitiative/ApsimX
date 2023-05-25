using System;
using System.Linq;
using System.Collections.Generic;
using Models.Core;

namespace Models.AgPasture
{

    /// <summary>Helper class for providing outputs from multiple tissues.</summary>
    [Serializable]
    public class TissuesHelper
    {
        private IEnumerable<GenericTissue> tissues;

        /// <summary>Constructor.</summary>
        /// <param name="tissueList"></param>
        public TissuesHelper(IEnumerable<GenericTissue> tissueList)
        {
            tissues = tissueList;
        }

        /// <summary>Dry matter (kg/ha).</summary>
        [Units("kg/ha")]
        public double Wt { get { return tissues.Sum(tissue => tissue.DM.Wt); } }

        /// <summary>Nitrogen content (kg/ha).</summary>
        [Units("kg/ha")]
        public double N { get { return tissues.Sum(tissue => tissue.DM.N); } }

    }
}
