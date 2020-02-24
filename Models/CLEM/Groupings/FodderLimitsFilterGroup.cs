using Models.CLEM.Resources;
using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Models.CLEM.Groupings
{
    ///<summary>
    /// Contains a group of filters to identify individual ruminants
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [Description("This grouping is not currently used.")]
    [Version(1, 0, 1, "")]
    public class FodderLimitsFilterGroup: CLEMModel, IFilterGroup
    {
        /// <summary>
        /// Monthly values to supply selected individuals
        /// </summary>
        [Description("Monthly proportion of intake that can come from each pool")]
        [ArrayItemCount(12)]
        public double[] PoolValues { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public FodderLimitsFilterGroup()
        {
            PoolValues = new double[12];
        }

        /// <summary>
        /// Are set limits strict, or can individual continue eating if food available? 
        /// </summary>
        public bool StrictLimits { get; set; }

        /// <summary>
        /// Combined ML ruleset for LINQ expression tree
        /// </summary>
        [XmlIgnore]
        public object CombinedRules { get; set; } = null;

    }
}
