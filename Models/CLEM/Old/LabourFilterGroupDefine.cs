using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Models.CLEM.Groupings
{
    ///<summary>
    /// Contains a group of filters to identify individul ruminants
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [Description("This labour filter group provides days available or needed for specific individuals from the labour pool using any number of Labour Filters.. Multiple filters will select groups of individuals required.")]
    public class LabourFilterGroupDefine: CLEMModel
    {
        /// <summary>
        /// Days per month selected individuals available
        /// </summary>
        [Description("Days per month selected individuals available")]
        [ArrayItemCount(12)]
        public double[] DaysPerMonth { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public LabourFilterGroupDefine()
        {
            DaysPerMonth = new double[12];
        }

    }
}
