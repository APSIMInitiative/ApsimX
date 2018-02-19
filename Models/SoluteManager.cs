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
        private List<Solute> solutes = null;

        /// <summary>List of all models that have solutes.</summary>
        [Link]
        private List<ISolute> soluteModels = null;

        /// <summary>The known types of solute setters.</summary>
        public enum SoluteSetterType
        {
            /// <summary>The setting model is a plant model</summary>
            Plant,
            /// <summary>The setting model is a soil model</summary>
            Soil,
            /// <summary>The setting model is a fertiliser model</summary>
            Fertiliser
        }

        /// <summary>Return a list of solute names.</summary>
        public string[] SoluteNames
        {
            get
            {
                if (solutes == null)
                    FindSolutes();
                return solutes.Select(solute => solute.Name).ToArray();
            }
        }
        
        /// <summary>
        /// Return the value of a solute. Will throw if solute not found.
        /// </summary>
        /// <param name="name">Name of solute</param>
        public double[] GetSolute(string name)
        {
            if (solutes == null)
                FindSolutes();
            Solute foundSolute = solutes.Find(solute => solute.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
            if (foundSolute == null)
                throw new Exception("Cannot find solute: " + name);
            return foundSolute.GetValue();
        }

        /// <summary>
        /// Set the value of a solute. Will throw if solute not found.
        /// </summary>
        /// <param name="name">Name of solute</param>
        /// <param name="callingModelType">Type of calling model</param>
        /// <param name="value">Value of solute</param>
        public void SetSolute(string name, SoluteSetterType callingModelType, double[] value)
        {
            Solute foundSolute = solutes.Find(solute => solute.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
            if (foundSolute == null)
                throw new Exception("Cannot find solute: " + name);
            foundSolute.SetValue(callingModelType, value);
        }

        /// <summary>
        /// Set the value of a solute by specifying a delta. Will throw if solute not found.
        /// </summary>
        /// <param name="name">Name of solute</param>
        /// <param name="callingModelType">Type of calling model</param>
        /// <param name="delta">Delta values to be added to solute</param>
        public void Add(string name, SoluteSetterType callingModelType, double[] delta)
        {
            Solute foundSolute = solutes.Find(solute => solute.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
            if (foundSolute == null)
                throw new Exception("Cannot find solute: " + name);
            foundSolute.SetValue(callingModelType, MathUtilities.Add(foundSolute.GetValue(), delta));
        }

        /// <summary>
        /// Add a delta value to the top layer of a solute. Will throw if solute not found.
        /// </summary>
        /// <param name="name">Name of solute</param>
        /// <param name="callingModelType">Type of calling model</param>
        /// <param name="layerIndex">Layer index to add delta to</param>
        /// <param name="delta">Value to be added to top layer of solute</param>
        public void AddToLayer(int layerIndex, string name, SoluteSetterType callingModelType, double delta)
        {
            Solute foundSolute = solutes.Find(solute => solute.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
            if (foundSolute == null)
                throw new Exception("Cannot find solute: " + name);

            double[] values = foundSolute.GetValue();
            values[layerIndex] += delta;
            foundSolute.SetValue(callingModelType, values);
        }

        /// <summary>
        /// Set the value of a solute by specifying a delta. Will throw if solute not found.
        /// </summary>
        /// <param name="name">Name of solute</param>
        /// <param name="callingModelType">Type of calling model</param>
        /// <param name="delta">Delta values to be subtracted from solute</param>
        public void Subtract(string name, SoluteSetterType callingModelType, double[] delta)
        {
            Solute foundSolute = solutes.Find(solute => solute.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
            if (foundSolute == null)
                throw new Exception("Cannot find solute: " + name);
            foundSolute.SetValue(callingModelType, MathUtilities.Subtract(foundSolute.GetValue(), delta));
        }

        /// <summary>Performs the initial checks and setup</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            if (solutes == null)
                FindSolutes();
        }

        /// <summary>Find all solutes.</summary>
        private void FindSolutes()
        {
            solutes = new List<Solute>();
            // Find all solutes.
            foreach (IModel soluteModel in soluteModels)
            {
                foreach (PropertyInfo property in soluteModel.GetType().GetProperties())
                {
                    if (ReflectionUtilities.GetAttribute(property, typeof(SoluteAttribute), false) != null)
                    {
                        string setterName = "Set" + property.Name;
                        MethodInfo setterMethod = soluteModel.GetType().GetMethod(setterName);
                        solutes.Add(new Solute() { model = soluteModel, property = property, setMethod = setterMethod});
                    }
                }

                PropertyInfo kgha = soluteModel.GetType().GetProperty("kgha");
                if (kgha != null)
                    solutes.Add(new Solute() { model = soluteModel, property = kgha });
            }
        }

        /// <summary>
        /// Internal solute class.
        /// </summary>
        [Serializable]
        private class Solute
        {
            public PropertyInfo property;
            public MethodInfo setMethod;
            public IModel model;
            public string Name
            {
                get
                {
                    if (property.Name == "kgha")
                        return model.Name;
                    else
                        return property.Name;
                }
            }
            public double[] GetValue()
            {
                return (double[])property.GetValue(model);
            }
            public void SetValue(SoluteSetterType callingModelType, double[] value)
            {
                if (setMethod == null)
                    property.SetValue(model, value);
                else
                    setMethod.Invoke(model, new object[] { callingModelType, value });
            }
        }
    }
}
