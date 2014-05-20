using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Core;
using System.Data;
using System.IO;
namespace Models.PostSimulationTools
{
    /// <summary>
    /// A simple post processing model that produces some regression stats.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PredictedObservedView")]
    [PresenterName("UserInterface.Presenters.PredictedObservedPresenter")]
    public class Regression : Model
    {
        public string TableName { get; set; }
        public string XFieldName { get; set; }
        public string YFieldName { get; set; }


        /// <summary>
        /// Go find the top level simulations object.
        /// </summary>
        public Simulations Simulations
        {
            get
            {
                Model obj = this;
                while (obj.Parent != null && obj.GetType() != typeof(Simulations))
                    obj = obj.Parent;
                if (obj == null)
                    throw new ApsimXException(FullPath, "Cannot find a root simulations object");
                return obj as Simulations;
            }
        }

        /// <summary>
        /// Simulation has completed. Create a regression table in the data store.
        /// </summary>
        public override void OnAllCompleted()
        {
            DataStore dataStore = new DataStore(this);

            dataStore.DeleteTable(this.Name);

            DataTable data = dataStore.GetData("*", TableName);

            dataStore.Disconnect();
        }
    }
}
