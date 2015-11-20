using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using Models.Core;
using Models.PostSimulationTools;

namespace Models
{
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(PostSimulationTools.PredictedObserved))]
    public class Tests : Model, ITestable
    {
        /// <summary>
        /// data table
        /// </summary>
        public DataTable table { get; set; }
        /// <summary>
        /// Run tests
        /// </summary>
        public void Test()
        {
            PredictedObserved PO = Parent as PredictedObserved;
            DataStore DS = PO.Parent as DataStore;
            table = DS.GetData("*", PO.Name);
        }
    }
}
