using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Models.Core;
using System.Xml.Serialization;

namespace Models
{
    /// <summary>
    /// A simple agroforestry model
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.ForestryView")]
    [PresenterName("UserInterface.Presenters.ForestryPresenter")]
    public class Forestry : Model
    {
        /// <summary>Gets or sets the table data.</summary>
        /// <value>The table.</value>
        [Summary]
        public List<List<string>> Table { get; set; }

        /// <summary>
        /// A list containing forestry information for each zone.
        /// </summary>
        [XmlIgnore]
        public List<ZoneInfo> ZoneInfoList;

        /// <summary>Called when [simulation commencing].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            ZoneInfoList = new List<ZoneInfo>();
            for (int i = 2; i < Table.Count; i++)
            {
                ZoneInfo newZone = new ZoneInfo();
                newZone.Name = Table[0][i - 1];
                newZone.Wind = Convert.ToDouble(Table[i][0]);
                newZone.Shade = Convert.ToDouble(Table[i][1]);
                newZone.RLD = new double[Table[1].Count - 3];
                for (int j = 3; j < Table[1].Count; j++)
                    newZone.RLD[j - 3] = Convert.ToDouble(Table[i][j]);
                ZoneInfoList.Add(newZone);
            }
        }
    }

    /// <summary>
    /// A structure holding forestry information for a single zone.
    /// </summary>
    public struct ZoneInfo
    {
        /// <summary>
        /// The name of the zone.
        /// </summary>
        public string Name;

        /// <summary>
        /// Wind value.
        /// </summary>
        public double Wind;

        /// <summary>
        /// Shade value.
        /// </summary>
        public double Shade;

        /// <summary>
        /// Root Length Density information for each soil layer in a zone.
        /// </summary>
        public double[] RLD;
    }
}
