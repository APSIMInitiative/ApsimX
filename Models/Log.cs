using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Core;
using System.IO;

namespace Models
{
    /// <summary>
    /// A low level log component that writes state / parameter variables to a text file.
    /// </summary>
    [Serializable]
    public class Log : Model
    {
        [Link]
        Clock Clock = null;

        private StreamWriter Writer;

        /// <summary>
        /// Initialise the model.
        /// </summary>
        public override void OnCommencing()
        {
            if (Simulation.FileName != null)
            {
                string fileName = Path.ChangeExtension(Simulation.FileName, ".log");
                Writer = new StreamWriter(fileName);
            }
        }

        /// <summary>
        /// Simulation has completed.
        /// </summary>
        public override void OnCompleted()
        {
            Writer.Close();
        }

        [EventSubscribe("Tick")]
        private void OnTick(object sender, EventArgs e)
        {
            Writer.WriteLine("Date: " + Clock.Today.ToString());
            Model[] models = this.FindAll();

            foreach (Model model in models)
                Summary.WriteModelProperties(Writer, model, false, true);
        }

    }
}
