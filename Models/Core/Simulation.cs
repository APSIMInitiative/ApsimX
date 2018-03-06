using System.Reflection;
using System;
using Models;
using System.Diagnostics;
using System.Xml;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.IO;
using APSIM.Shared.Utilities;
using Models.Factorial;
using System.ComponentModel;
using Models.Core.Runners;

namespace Models.Core
{
    /// <summary>
    /// # [Name]
    /// A simulation model
    /// </summary>
    [ValidParent(ParentType = typeof(Simulations))]
    [ValidParent(ParentType = typeof(Experiment))]
    [Serializable]
    [ScopedModel]
    public class Simulation : Model, ISimulationGenerator
    {
        [NonSerialized]
        private ScopingRules scope = null;

        /// <summary>Has this simulation been run?</summary>
        private bool hasRun;

        /// <summary>
        /// An enum that is used to indicate message severity when writing messages to the .db
        /// </summary>
        public enum ErrorLevel
        {
            /// <summary>Information</summary>
            Information,

            /// <summary>Warning</summary>
            Warning,

            /// <summary>Error</summary>
            Error
        };

        /// <summary>Returns the object responsible for scoping rules.</summary>
        public ScopingRules Scope
        {
            get
            {
                if (scope == null)
                {
                    scope = new ScopingRules();
                }
                return scope;
            }
        }

        /// <summary>A locater object for finding models and variables.</summary>
        [NonSerialized]
        private Locater locater;

        /// <summary>Cache to speed up scope lookups.</summary>
        /// <value>The locater.</value>
        public Locater Locater
        {
            get
            {
                if (locater == null)
                {
                    locater = new Locater();
                }

                return locater;
            }
        }

        /// <summary>Gets the value of a variable or model.</summary>
        /// <param name="namePath">The name of the object to return</param>
        /// <returns>The found object or null if not found</returns>
        public object Get(string namePath)
        {
            return Locater.Get(namePath, this);
        }

        /// <summary>Get the underlying variable object for the given path.</summary>
        /// <param name="namePath">The name of the variable to return</param>
        /// <returns>The found object or null if not found</returns>
        public IVariable GetVariableObject(string namePath)
        {
            return Locater.GetInternal(namePath, this);
        }

        /// <summary>Sets the value of a variable. Will throw if variable doesn't exist.</summary>
        /// <param name="namePath">The name of the object to set</param>
        /// <param name="value">The value to set the property to</param>
        public void Set(string namePath, object value)
        {
            Locater.Set(namePath, this, value);
        }

        /// <summary>Return the filename that this simulation sits in.</summary>
        /// <value>The name of the file.</value>
        [XmlIgnore]
        public string FileName { get; set; }

        /// <summary>
        /// Simulation has completed. Clear scope and locator
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Completed")]
        private void OnSimulationCompleted(object sender, EventArgs e)
        {
            ClearCaches();
        }

        /// <summary>
        /// Clears the existing Scoping Rules
        /// </summary>
        public void ClearCaches()
        {
            Scope.Clear();
            Locater.Clear();
        }


        /// <summary>Simulation runs are about to begin.</summary>
        [EventSubscribe("BeginRun")]
        private void OnBeginRun(IEnumerable<string> knownSimulationNames = null, IEnumerable<string> simulationNamesBeingRun = null)
        {
            hasRun = false;
        }

        /// <summary>Gets the next job to run</summary>
        public Simulation NextSimulationToRun(bool doFullFactorial = true)
        {
            if (Parent is ISimulationGenerator || hasRun)
                return null;
            hasRun = true;

            Simulation simulationToRun;
            if (this.Parent == null)
                simulationToRun = this;
            else
            {
                Simulations simulationEngine = Apsim.Parent(this, typeof(Simulations)) as Simulations;
                simulationToRun = Apsim.Clone(this) as Simulation;
                simulationEngine.MakeSubstitutions(simulationToRun);
            }
            return simulationToRun;
        }

        /// <summary>Gets a list of simulation names</summary>
        public IEnumerable<string> GetSimulationNames(bool fullFactorial = true)
        {
            if (Parent is ISimulationGenerator)
                return new string[0];
            return new string[] { Name };
        }
    }
}