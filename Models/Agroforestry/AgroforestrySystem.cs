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
    /// The APSIM AgroforestrySystem model calculates interactions between trees and neighbouring crop or pasture zones.  The model is therefore derived from the Zone class within APSIM and includes child zones to simulate soil can plant processes within the system.  It obtains information from a tree model within its scope (ie a child) and uses information about the tree structure (such as height and canopy dimensions) to calculate microclimate impacts on its child zones.  Below-ground interactions between trees and crops or pastures are calculated by the APSIM SoilArbitrator model.
    /// 
    /// Windbreaks are simulated using the approach used by [Huthetal2002] which calculates windspeeds in the lee of windbreaks as a function distance (described in terms of multiples of tree heights) and windbreak optical porosity.
    /// 
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentModels = new Type[] { typeof(Simulation), typeof(Zone) })]
    public class AgroforestrySystem : Zone
    {
        /// <summary>
        /// Fraction of rainfall intercepted by canopy
        /// </summary>
        [Description("Fraction of rainfall incepted within the tree canopy (0-1)")]
        public double RainfallInterceptionFraction { get; set; }

        /// <summary>
        /// Width of the tree rain shaddow in terms of tree heights
        /// </summary>
        [Description("Width of tree rainfall shaddow (H)")]
        public double RainShaddowWidth { get; set; }

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
        /// 
        /// </summary>
        [XmlIgnore]
        public TreeProxy tree = null;

        /// <summary>Called when [simulation commencing].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            //find the tree
            tree = Apsim.Child(this, typeof(TreeProxy)) as TreeProxy;
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

        /// <summary>Writes documentation for this cultivar by adding to the list of documentation tags.</summary>
        /// <param name="tags">The list of tags to add to.</param>
        /// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
        /// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
        public override void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
        {
// need to put something here
            // add a heading.
            tags.Add(new AutoDocumentation.Heading(Name, headingLevel));

            // write description of this class.
            AutoDocumentation.GetClassDescription(this, tags, indent);

            tree = Apsim.Child(this, typeof(TreeProxy)) as TreeProxy;
            tree.Document(tags, headingLevel, indent);
        }
    }
}
