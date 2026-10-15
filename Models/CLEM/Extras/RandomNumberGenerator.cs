using Models.Core;
using Models.Core.Attributes;
using System;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace Models.CLEM
{
    /// <summary>Random numbers generator</summary>
    /// <summary>This component provides the random number sequence to be used for all stochastic processes in CLEM</summary>
    /// <summary>This functionality has been moved from the CLEM component to an individual component placed under the simulation</summary>
    /// <summary>This allows sharing of a single sequence between multiple farms in a simulation</summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Simulation))]
    [Description("Provide the random number sequence to be used for all stochastic processes in CLEM")]
    [Version(1, 0, 1, "Moved this functionality from the CLEM component to an individual component placed under the simulation to allow sharing between multiple farms in a simulation")]
    [HelpUri(@"Content/Features/Random numbers generator.htm")]
    public class RandomNumberGenerator: Model
    {
        [ThreadStatic]
        private static Random _generator = null;

        [ThreadStatic]
        private static bool _disableRandom = false;

        /// <summary>
        /// Seed for random number generator (0 uses GuId rather than clock)
        /// </summary>
        [System.ComponentModel.DefaultValueAttribute(1)]
        [Required]
        [Description("Generator seed (0 for no seed, -1 to disable random)")]
        public int Seed { get; set; }

        /// <summary>
        /// Iteration number for multiple simulations of stochastic processes
        /// </summary>
        [System.ComponentModel.DefaultValueAttribute(1)]
        [Required, GreaterThanEqualValue(0)]
        [JsonIgnore]
        public int Iteration { get; set; }

        /// <summary>
        /// Access the random number generator
        /// </summary>
        [JsonIgnore]
        [Description("Random number generator")]
        public static Random Generator 
        { 
            get 
            {
                if (_generator is null)
                    throw new Exception($"Missing random number generator!{Environment.NewLine}This simulation uses stochastic processes requiring random numbers{Environment.NewLine}You must add a [o=CLEM.RandomNumberGenerator] component below the [o=Simulation].");
                else if (_disableRandom)
                    return new Random(1);
                else
                    return _generator; 
            } 
        }

        /// <summary>
        /// Determines if the random nunber generator has been created and is available.
        /// </summary>
        public static bool IsAvailable => _generator is not null;

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            if (Seed == -1)
                _disableRandom = true;
            if (Seed == 0)
                _generator = new Random(Guid.NewGuid().GetHashCode());
            else
                _generator = new Random(Seed);
        }

        /// <summary>
        /// A method to initialise the random number generator needed for UI calculations such as the Descriptive Summary
        /// </summary>
        public static void SetForPreSimulation()
        {
            _generator = new Random(1);
        }
    }
}
