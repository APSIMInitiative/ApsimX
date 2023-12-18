using System;
using Models.Core;

namespace Models.Soils
{
    /// <summary>
    /// Tillage type structure
    /// </summary>
    [Serializable]
    public class TillageType : Model
    {
        /// <summary>Gets or sets the f_incorp.</summary>
        /// <value>The f_incorp.</value>
        public double f_incorp { get; set; }
        /// <summary>Gets or sets the tillage_depth.</summary>
        /// <value>The tillage_depth.</value>
        public double tillage_depth { get; set; }
        /// <summary>Gets or sets the cn_red.</summary>
        /// <value>The cn_red.</value>
        public int cn_red { get; set; }
        /// <summary>Gets or sets the cn_rain.</summary>
        /// <value>The cn_rain.</value>
        public int cn_rain { get; set; }
    }

    /// <summary>A deletegate for publishing a tillage event.</summary>
    /// <param name="sender"></param>
    /// <param name="tillageType"></param>
    public delegate void TillageTypeDelegate(object sender, TillageType tillageType);
}
