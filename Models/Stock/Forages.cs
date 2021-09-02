using Models.Core;
using Models.PMF.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.GrazPlan
{
    /// <summary>Encapsulates a collection of forages.</summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(GrazPlan.Stock))]
    public class Forages : Model
    {
        [Link]
        private readonly Zone zone = null;

        [Link]
        private readonly ForageParameters[] forageParameters = null;

        private List<Forage> allForages;

        /// <summary>Start of simulation.</summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        [EventSubscribe("StartOfSimulation")]
        private void OnStartOfSimulation(object sender, EventArgs e)
        {
            allForages = new List<Forage>();
            foreach (var plant in zone.FindAllInScope<IPlantDamage>())
            {
                var parameters = forageParameters.FirstOrDefault(f => f.Name.Equals(plant.Name, StringComparison.InvariantCultureIgnoreCase));
                if (parameters == null)
                    throw new Exception($"Cannot find grazing parameters for {plant.Name}");

                if (parameters.HasGrazableMaterial)
                    allForages.Add(new Forage(plant, parameters));
            }
        }

        /// <summary>Return a collection of forages for a zone.</summary>
        public IEnumerable<Forage> GetForages(Zone zone)
        {
            return allForages.Where(f => f.Zone == zone);
        }
    }
}