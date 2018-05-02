using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.PMF;

namespace Models.Core
{
    /// <summary>
    /// An alias model
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(Cultivar))]
    public class Alias : Model
    {

    }
}
