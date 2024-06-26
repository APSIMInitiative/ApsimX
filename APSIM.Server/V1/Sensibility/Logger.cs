using System;
using System.Collections.Generic;
using System.Linq;
using APSIM.Shared.Utilities;
using Models;
using Models.Core;

namespace APSIM.Server.Sensibility
{
    /// <summary>
    /// This model will serialize the entire simulation to JSON after the first
    /// day of the simulation.
    /// </summary>
    internal class Logger : Model
    {
        /// <summary>
        /// Link to clock, used to find first day of simulation.
        /// </summary>
        [Link]
        private IClock clock = null;

        /// <summary>
        /// Link to the simulation. This is what will be serialized.
        /// </summary>
        [Link(Type = LinkType.Ancestor)]
        private Simulation sim = null;

        /// <summary>
        /// This is a dictionary mapping models to their serialized json.
        /// </summary>
        /// <typeparam name="IModel">A model.</typeparam>
        /// <typeparam name="string">Serialized form of the model.</typeparam>
        private IDictionary<IModel, string> state;

        /// <summary>
        /// Called at end of day. If today is simulation start date, will
        /// serialize the simulation to json.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        [EventSubscribe("EndOfDay")]
        private void Log(object sender, EventArgs args)
        {
            if (clock.Today == clock.StartDate)
            {
                // Set json to null before logging, to prevent it from being serialized.
                IDictionary<IModel, string> previousState = state;
                state = null;
                state = GetSimulationState();
                if (previousState != null)
                {
                    // Second run.
                    VerifyState(previousState, state);

                    // Terminate simulation early.
                    clock.EndDate = clock.Today;
                }
            }
        }

        /// <summary>
        /// Verify that the given simulation state matches the state from a
        /// previous run, for all models.
        /// </summary>
        /// <param name="previousState">State from a previous run.</param>
        /// <param name="state">State from the recent run.</param>
        private void VerifyState(IDictionary<IModel, string> previousState, IDictionary<IModel, string> state)
        {
            List<Exception> errors = new List<Exception>();
            foreach ((IModel model, string json) in previousState)
            {
                try
                {
                    VerifyModel(model, json, state);
                }
                catch (Exception error)
                {
                    errors.Add(error);
                }
            }
            if (errors.Any())
            {
                bool plural = errors.Count > 1;
                throw new AggregateException($"{errors.Count} model{(plural ? "s" : "")} failed to reset {(plural ? "their" : "its")} state", errors);
            }
        }

        /// <summary>
        /// Ensure that a model's state matches the state recorded for the model
        /// in the provided state dictionary.
        /// </summary>
        /// <param name="model">A model.</param>
        /// <param name="json">Json representation of the model.</param>
        /// <param name="state">Cache of state from a previous simulation run.</param>
        private void VerifyModel(IModel model, string json, IDictionary<IModel, string> state)
        {
            if (!state.TryGetValue(model, out string newJson))
                throw new Exception($"Model {model.FullPath} was removed from simulation {sim.Name}");
            if (!string.Equals(json, newJson, StringComparison.Ordinal))
                throw new SimulationResetException(sim.Name, model, json, newJson);
        }

        /// <summary>
        /// Serialize all models in the simulation, individually.
        /// </summary>
        private IDictionary<IModel, string> GetSimulationState()
        {
            Dictionary<IModel, string> result = new Dictionary<IModel, string>();
            foreach (IModel model in sim.FindAllDescendants())
                result[model] = GetModelState(model);
            return result;
        }

        /// <summary>
        /// Serialize a model to json. All private members will be serialized,
        /// but child models will not.
        /// </summary>
        /// <param name="model">The model to be serialized.</param>
        private string GetModelState(IModel model)
        {
            return ReflectionUtilities.JsonSerialise(model, true, includeChildren: false);
        }
    }
}
