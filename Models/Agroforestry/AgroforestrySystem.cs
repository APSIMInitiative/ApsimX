using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Models.Core;
using System.Xml.Serialization;
using Models.Interfaces;
using APSIM.Shared.Utilities;
using Models.Soils.Arbitrator;
using Models.Zones;

namespace Models.Agroforestry
{
    /// <summary>
    /// A simple agroforestry model
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentModels = new Type[] { typeof(Simulation), typeof(Zone) })]
    public class AgroforestrySystem : Zone
    {

        /// <summary>
        /// The reduction in wind as a fraction.
        /// </summary>
        [Units("0-1")]
        [XmlIgnore]
        public double Urel { get; set; }

        /// <summary>
        /// A list containing forestry information for each zone.
        /// </summary>
        [XmlIgnore]
        public List<IModel> ZoneList;

        /// <summary>
        /// Return the area of the zone.
        /// </summary>
        [XmlIgnore]
        public override double Area
        {
            get
            {
                double A = 0;
                foreach (Zone Z in Apsim.Children(this, typeof(Zone)))
                    A += Z.Area;
                return A;
            }
            set
            {
            }
        }

        /// <summary>
        /// A pointer to the tree model.
        /// </summary>
        [XmlIgnore]
        public TreeProxy tree = null;

        /// <summary>Called when [simulation commencing].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            tree = Apsim.Child(this, typeof(TreeProxy)) as TreeProxy;
            ZoneList = Apsim.Children(this, typeof(Zone));
        }

        /// <summary>
        /// Passthrough for child nodes that need information from the tree.
        /// Saves having to query the simulation for the node location all the time.
        /// </summary>
        /// <param name="z">The zone.</param>
        /// <returns></returns>
        public double GetDistanceFromTrees(Zone z)
        {
            return tree.GetDistanceFromTrees(z);
        }

        /// <summary>
        /// Return the %Wind Reduction for a given zone
        /// </summary>
        /// <param name="z">Zone</param>
        /// <returns>%Wind Reduction</returns>
        public double GetWindReduction(Zone z)
        {
            foreach (Zone zone in ZoneList)
                if (zone == z)
                {
                    double UrelMin = Math.Max(0.0, 1.14 * 0.5 - 0.16); // 0.5 is porosity, will be dynamic in the future

                    if (tree.heightToday < 1)
                        Urel = 1;
                    else
                    {
                        tree.H = GetDistanceFromTrees(z) / tree.heightToday;
                        if (tree.H < 6)
                            Urel = UrelMin + (1 - UrelMin) / 2 - tree.H / 6 * (1 - UrelMin) / 2;
                        else if (tree.H < 6.1)
                            Urel = UrelMin;
                        else
                            Urel = UrelMin + (1 - UrelMin) / (1 + 0.000928 * Math.Exp(12.9372 * Math.Pow((tree.H - 6), -0.26953)));
                    }
                    return Urel;
                }
            throw new ApsimXException(this, "Could not find zone called " + z.Name);
        }
    }
}
