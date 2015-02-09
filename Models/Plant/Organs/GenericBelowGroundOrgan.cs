using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using Models.PMF.Interfaces;

namespace Models.PMF.Organs
{
    /// <summary>
    /// A generic below ground organ
    /// </summary>
    [Serializable]
    public class GenericBelowGroundOrgan : GenericOrgan, BelowGround
    {
        #region Event handlers
        // Nothing here yet - need event handlers for end crop to return OM to soil
        #endregion
    }
}
