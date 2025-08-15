using System;
using System.Collections.Generic;
using System.Linq;
using APSIM.Core;
using Models.Core;
using Models.Core.ApsimFile;
using Models.Functions;

namespace Models.PMF
{
    /// <summary>
    /// Calculates the Deficit of a given labile nutrient pool and returns it to use for a demand.
    /// </summary>
    [Serializable]
    [Description("This function calculates demands for metabolic and storage pools based on the size of the potential deficits of these pools.  For nutrients is uses maximum, critical and minimum concentration thresholds and for carbon it uses structural, metabolic and storage partitioning proportions to ")]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(IPlant))]
    [ValidParent(ParentType = typeof(BiomassArbitrator))]
    public class PlantPartitionFractions : Model, IStructureDependency
    {
        /// <summary>Structure instance supplied by APSIM.core.</summary>
        [field: NonSerialized]
        public IStructure Structure { private get; set; }

        [Link(Type = LinkType.Ancestor)]
        private Plant plant = null;

        /// <summary>The list of organs</summary>
        private List<string> organNames = new List<string>();

        /// <summary> List of Child Functions to represent each organ</summary>
        ///
        public IEnumerable<IFunction> ChildFunctions { get; set; }

        /// <summary>Dictionary containing each organs partitioning fraction</summary>
        public Dictionary<string, double> PartitionFractions { get; set; } = new Dictionary<string, double>();

        /// <summary>Things the plant model does when the simulation starts</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        virtual protected void OnSimulationCommencing(object sender, EventArgs e)
        {
            foreach (Organ organ in Structure.FindChildren<Organ>(relativeTo: plant))
            {
                organNames.Add(organ.Name + "Fraction");
            }

            ChildFunctions = Structure.FindChildren<IFunction>().ToList();

            int orgNum = 0;
            foreach (IFunction c in ChildFunctions)
            {
                orgNum += 1;
                if (!organNames.Contains(((IModel)c).Name))
                    throw new Exception(this.Name + "Must have children functions with names of OrganFraction matching all organs");
            }

            if (orgNum != organNames.Count)
                throw new Exception(this.Name + "Must have children functions with names matching all organs");
        }

        /// <summary>Called when [do daily initialisation].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoDailyInitialisation")]
        protected void OnDoDailyInitialisation(object sender, EventArgs e)
        {
            foreach (IFunction c in ChildFunctions)
            {
                string name = ((IModel)c).Name;
                PartitionFractions[name] = c.Value();
            }

            if ((PartitionFractions.Sum(x => x.Value) < 0.99) || (PartitionFractions.Sum(x => x.Value) > 1.01))
                throw new Exception("Sum of partitioning fractions in " + this.FullPath + "does not add to 1");
        }
    }
}
