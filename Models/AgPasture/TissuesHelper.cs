
namespace Models.AgPasture
{
    using Models.Core;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

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
