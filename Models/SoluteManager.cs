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
        /// <summary>List of all solutes.</summary>
        private Dictionary<string, ISolute> solutes = new Dictionary<string, ISolute>(StringComparer.InvariantCultureIgnoreCase);

        /// <summary>Link to top level simulation.</summary>
        [Link]
        private Simulation simulation = null;

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
                if (solutes.Count == 0)
                    FindSolutes();
                return solutes.Keys.ToArray();
            }
        }
        
        /// <summary>
        /// Return the value of a solute. Will throw if solute not found.
        /// </summary>
        /// <param name="name">Name of solute</param>
        public double[] GetSolute(string name)
        {
            return FindSolute(name).kgha;
        }

        /// <summary>
        /// Set the value of a solute. Will throw if solute not found.
        /// </summary>
        /// <param name="name">Name of solute</param>
        /// <param name="callingModelType">Type of calling model</param>
        /// <param name="value">Value of solute</param>
        public void SetSolute(string name, SoluteSetterType callingModelType, double[] value)
        {
            var solute = FindSolute(name);
            solute.SetKgHa(callingModelType, value);
        }

        /// <summary>
        /// Set the value of a solute by specifying a delta. Will throw if solute not found.
        /// </summary>
        /// <param name="name">Name of solute</param>
        /// <param name="callingModelType">Type of calling model</param>
        /// <param name="delta">Delta values to be added to solute</param>
        public void Add(string name, SoluteSetterType callingModelType, double[] delta)
        {
            var solute = FindSolute(name);
            solute.SetKgHa(callingModelType, MathUtilities.Add(solute.kgha, delta));
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
            var solute = FindSolute(name);

            double[] values = solute.kgha;
            values[layerIndex] += delta;
            solute.SetKgHa(callingModelType, values);
        }

        /// <summary>
        /// Set the value of a solute by specifying a delta. Will throw if solute not found.
        /// </summary>
        /// <param name="name">Name of solute</param>
        /// <param name="callingModelType">Type of calling model</param>
        /// <param name="delta">Delta values to be subtracted from solute</param>
        public void Subtract(string name, SoluteSetterType callingModelType, double[] delta)
        {
            var solute = FindSolute(name);
            solute.SetKgHa(callingModelType, MathUtilities.Subtract(solute.kgha, delta));
        }

        /// <summary>Performs the initial checks and setup</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            FindSolutes();
        }

        /// <summary>
        /// Find a solute. Throw exception if not found.
        /// </summary>
        /// <param name="name">Name of solute.</param>
        private ISolute FindSolute(string name)
        {
            FindSolutes();
            ISolute foundSolute;
            solutes.TryGetValue(name, out foundSolute);
            if (foundSolute == null)
                throw new Exception("Cannot find solute: " + name);
            return foundSolute;
        }

        /// <summary>Find all solutes.</summary>
        private void FindSolutes()
        {
            if (solutes.Count == 0)
            {
                // Find all models that have solutes.
                foreach (var soluteModel in Apsim.ChildrenRecursively(simulation, typeof(IHasSolutes)).Cast<IHasSolutes>())
                    soluteModel.GetSolutes().ForEach(solute => solutes.Add(solute.Name, solute));

                // Find all solutes.
                foreach (var solute in Apsim.ChildrenRecursively(simulation, typeof(ISolute)).Cast<ISolute>())
                    solutes.Add(solute.Name, solute);
            }
        }
    }
}
