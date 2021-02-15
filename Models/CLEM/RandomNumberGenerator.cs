using Models.CLEM.Activities;
using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Models.CLEM
{
    /// <summary>Random numbers generator</summary>
    /// <summary>This component provides the random number sequence to be used for all stochastic processes in CLEM</summary>
    /// <summary>This functionality has been moved from the CLEM component to an individual component placed under the simulation</summary>
    /// <summary>This allows sharing of a single sequence between multiple farms in a simulation</summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Simulation))]
    [Description("This component provides the random number sequence to be used for all stochastic processes in CLEM")]
    [Version(1, 0, 1, "Moved this functionality from the CLEM component to an individual component placed under the simulation to allow sharing between multiple farms in a simulation")]
    [HelpUri(@"Content/Features/Random numbers generator.htm")]
    public class RandomNumberGenerator: Model
    {
        [ThreadStatic]
        private static Random generator = null;

        /// <summary>
        /// Seed for random number generator (0 uses GuId rather than clock)
        /// </summary>
        [System.ComponentModel.DefaultValueAttribute(1)]
        [Required, GreaterThanEqualValue(0)]
        [Description("Generator seed (0 to use clock)")]
        public int Seed { get; set; }

        /// <summary>
        /// Iteration number for multiple simulations of stochasitc processes
        /// </summary>
        [System.ComponentModel.DefaultValueAttribute(1)]
        [Required, GreaterThanEqualValue(0)]
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
                if(generator is null)
                {
                    throw new ApsimXException(new Model() { Name = "RandomNumberGenerator" }, "Missing random number generator!\r\nThis simulation uses stochastic processes requiring random numbers\r\nYou must add a [o=CLEM.RandomNumberGenerator] component below the [o=Simulation]");
                }
                return generator; 
            } 
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            if (Seed == 0)
            {
                generator = new Random(Guid.NewGuid().GetHashCode());
            }
            else
            {
                generator = new Random(Seed);
            }
        }
    }
}
