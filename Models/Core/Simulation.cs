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
using System.Linq;
using Models.Core.ApsimFile;

namespace Models.Core
{
    /// <summary>
    /// # [Name]
    /// A simulation model
    /// </summary>
    [ValidParent(ParentType = typeof(Simulations))]
    [ValidParent(ParentType = typeof(Experiment))]
    [ValidParent(ParentType = typeof(Morris))]
    [ValidParent(ParentType = typeof(Sobol))]
    [Serializable]
    [ScopedModel]
    public class Simulation : Model, ISimulationGenerator
    {
        [NonSerialized]
        private ScopingRules scope = null;

        /// <summary>Has this simulation been run?</summary>
        private bool hasRun;

        /// <summary>Return total area.</summary>
        public double Area
        {
            get
            {
                return Apsim.Children(this, typeof(Zone)).Sum(z => (z as Zone).Area);
            }
        }

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

        /// <summary>
        /// An enum that is used to indicate message severity when writing messages to the status window.
        /// </summary>
        public enum MessageType
        {
            /// <summary>Information</summary>
            Information,

            /// <summary>Warning</summary>
            Warning
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
        private void OnBeginRun()
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

                // We are breaking.NET naming rules with our manager scripts.All our manager scripts are class 
                // Script in the Models namespace.This is OK until we do a clone(binary serialise/deserialise). 
                // When this happens, the serialisation engine seems to choose the first assembly it can find 
                // that has the 'right' code.It seems that if the script c# is close to an existing assembly then 
                // it chooses that assembly. In the attached .apsimx, it chooses the wrong temporary assembly for 
                // SowingRule2. It chooses SowingRule assembly even though the script for the 2 manager files is 
                // different. I'm not sure how to fix this yet. A brute force way is to recompile all manager 
                // scripts after cloning.
                // https://github.com/APSIMInitiative/ApsimX/issues/2603

                simulationEngine.MakeSubsAndLoad(simulationToRun);
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

        /// <summary>Gets a list of factors</summary>
        public List<ISimulationGeneratorFactors> GetFactors()
        {
            List<ISimulationGeneratorFactors> factors = new List<ISimulationGeneratorFactors>();
            // Add top level simulation zone. This is needed if Report is in top level.
            factors.Add(new SimulationGeneratorFactors(new string[] { "SimulationName", "Zone" },
                                                       new string[] { Name, Name },
                                                       "Simulation", Name));
            factors[0].AddFactor("Zone", Name);
            foreach (IModel zone in Apsim.ChildrenRecursively(this).Where(c => ScopingRules.IsScopedModel(c)))
            {
                var factor = new SimulationGeneratorFactors(new string[] { "SimulationName", "Zone" },
                                                            new string[] { Name, zone.Name }, 
                                                            "Simulation", Name);
                factors.Add(factor);
                factor.AddFactor("Zone", zone.Name);
            }
            return factors;
        }

        /// <summary>
        /// Generates an .apsimx file for this simulation.
        /// </summary>
        /// <param name="path">Directory to write the file to.</param>
        public void GenerateApsimXFile(string path)
        {
            IModel obj = Simulations.Create(new List<IModel> { this, new Models.Storage.DataStore() });
            string st = FileFormat.WriteToString(obj);
            File.WriteAllText(Path.Combine(path, Name + ".apsimx"), st);
        }
    }
}