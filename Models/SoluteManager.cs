namespace Models
{
    using APSIM.Shared.Utilities;
    using Core;
    using Interfaces;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// Manages access to solutes.
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(Zone))]
    public class SoluteManager : Model
    {
        /// <summary>List of all solutes</summary>
        private List<Solute> solutes = new List<Solute>();

        /// <summary>List of all models that have solutes.</summary>
        [Link]
        private List<ISolute> soluteModels = null;

        /// <summary>Return a list of solute names.</summary>
        public string[] SoluteNames {  get { return solutes.Select(solute => solute.Name).ToArray(); } }
        
        /// <summary>
        /// Return the value of a solute. Will throw if solute not found.
        /// </summary>
        /// <param name="name">Name of solute</param>
        public double[] GetSolute(string name)
        {
            Solute foundSolute = solutes.Find(solute => solute.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
            if (foundSolute == null)
                throw new Exception("Cannot find solute: " + name);
            return foundSolute.Value;
        }

        /// <summary>
        /// Set the value of a solute. Will throw if solute not found.
        /// </summary>
        /// <param name="name">Name of solute</param>
        /// <param name="value">Value of solute</param>
        public void SetSolute(string name, double[] value)
        {
            Solute foundSolute = solutes.Find(solute => solute.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
            if (foundSolute == null)
                throw new Exception("Cannot find solute: " + name);
            foundSolute.Value = value;
        }

        /// <summary>
        /// Set the value of a solute by specifying a delta. Will throw if solute not found.
        /// </summary>
        /// <param name="name">Name of solute</param>
        /// <param name="delta">Delta values to be added to solute</param>
        public void Add(string name, double[] delta)
        {
            Solute foundSolute = solutes.Find(solute => solute.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
            if (foundSolute == null)
                throw new Exception("Cannot find solute: " + name);
            foundSolute.Value = MathUtilities.Add(foundSolute.Value, delta);
        }

        /// <summary>
        /// Add a delta value to the top layer of a solute. Will throw if solute not found.
        /// </summary>
        /// <param name="name">Name of solute</param>
        /// <param name="delta">Value to be added to top layer of solute</param>
        public void AddToTopLayer(string name, double delta)
        {
            Solute foundSolute = solutes.Find(solute => solute.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
            if (foundSolute == null)
                throw new Exception("Cannot find solute: " + name);

            // Check to see if there is a 'AddToTopLayer' method.
            if (foundSolute.addToTopLayerMethod == null)
            {
                double[] values = foundSolute.Value;
                values[0] += delta;
                foundSolute.Value = values;
            }
            else
                foundSolute.addToTopLayerMethod.Invoke(foundSolute.model, new object[] { delta });
        }

        /// <summary>
        /// Set the value of a solute by specifying a delta. Will throw if solute not found.
        /// </summary>
        /// <param name="name">Name of solute</param>
        /// <param name="delta">Delta values to be subtracted from solute</param>
        public void Subtract(string name, double[] delta)
        {
            Solute foundSolute = solutes.Find(solute => solute.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
            if (foundSolute == null)
                throw new Exception("Cannot find solute: " + name);
            foundSolute.Value = MathUtilities.Subtract(foundSolute.Value, delta);
        }

        /// <summary>Performs the initial checks and setup</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            // Find all solutes.
            foreach (IModel soluteModel in soluteModels)
            {
                foreach (PropertyInfo property in soluteModel.GetType().GetProperties())
                {
                    if (ReflectionUtilities.GetAttribute(property, typeof(SoluteAttribute), false) != null)
                    {
                        string addToTopLayerName = "Add" + property.Name + "ToTopLayer";
                        MethodInfo addToTopLayerMethod = soluteModel.GetType().GetMethod(addToTopLayerName);
                        solutes.Add(new Solute() { model = soluteModel, property = property, addToTopLayerMethod = addToTopLayerMethod });
                    }
                }
            }
        }

        /// <summary>
        /// Internal solute class.
        /// </summary>
        [Serializable]
        private class Solute
        {
            public PropertyInfo property;
            public IModel model;
            public MethodInfo addToTopLayerMethod;
            public string Name {  get { return property.Name; } }
            public double[] Value
            {
                get
                {
                    return (double[])property.GetValue(model);
                }
                set
                {
                    property.SetValue(model, value);
                }
            }
        }
    }
}
