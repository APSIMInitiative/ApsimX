using System.Reflection;
using System;
using Models;
using System.Diagnostics;
using System.Xml;
using System.Collections.Generic;
using System.IO;

namespace Models.Core
{
    [Serializable]
    public class Simulation : Zone
    {
        // Private links
        [Link] private Clock Clock = null;

        /// <summary>
        /// To commence the simulation, this event will be invoked.
        /// </summary>
        public event EventHandler Commenced;

        /// <summary>
        /// Return the filename that this simulation sits in.
        /// </summary>
        public string FileName
        {
            get
            {
                Model RootModel = this;
                while (RootModel.Parent != null)
                    RootModel = RootModel.Parent;
                if (RootModel != null)
                    return (RootModel as Simulations).FileName;
                else
                    return null;
            }
        }


        /// <summary>
        /// Run the simulation. Returns true if no fatal errors or exceptions.
        /// </summary>
        public bool Run()
        {
            bool ok = false;
            try
            {
                Utility.ModelFunctions.ConnectEventsInAllModels(this);
                Utility.ModelFunctions.ResolveLinks(this);
                Utility.ModelFunctions.CallOnCommencing(this);

                if (Commenced != null)
                {
                    Commenced.Invoke(this, new EventArgs());
                    ok = true;
                }
                else
                    throw new ApsimXException(FullPath, "Cannot invoke Commenced");
            }
            catch (ApsimXException err)
            {
                DataStore store = new DataStore();
                store.Connect(Path.ChangeExtension(FileName, ".db"));

                string Msg = err.Message;
                if (err.InnerException != null)
                    Msg += "\r\n" + err.InnerException.Message + "\r\n" + err.InnerException.StackTrace;
                else
                    Msg += "\r\n" + err.StackTrace;

                store.WriteMessage(Name, Clock.Today, err.ModelFullPath, err.Message, DataStore.ErrorLevel.Error);

                ok = false;
            }

            Utility.ModelFunctions.CallOnCompleted(this);
            Utility.ModelFunctions.UnresolveLinks(this);
            Utility.ModelFunctions.DisconnectEventsInAllModels(this);
            ok &= true;

            return ok;
        }
    }


}