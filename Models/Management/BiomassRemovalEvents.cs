using Models.Core;
using Models.Functions;
using Models.Interfaces;
using Models.PMF.Interfaces;
using Models.PMF.Phen;
using System;
using System.Collections.Generic;
using System.Data;
using Newtonsoft.Json;
using System.Linq;
using APSIM.Shared.Utilities;
using Models.PMF;
using static Models.Management.BiomassRemovalFractions;

namespace Models.Management
{
    /// <summary>
    /// Steps through each OrganBiomassRemoval child when Do() method is called and removes the specified fractions of biomass from each.  
    /// Organ names must match the name of an organ in the specified crop.  
    /// Biomass will only be removed from organs that are specified with an OrganBiomassRemoval child on this class.  
    /// Add Child of name "StageSet" to specify a phenology rewind when ever the Do() method is called
    /// </summary>
    [ValidParent(ParentType = typeof(Zone))]
    [ValidParent(ParentType = typeof(Folder))]
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class BiomassRemovalEvents : Model
    {
        /// <summary>
        /// Crop to remove biomass from
        /// </summary>
        [Description("Crop to remove biomass from")]
        public IPlant PlantToRemoveFrom { get; set; }

        /// <summary>
        /// The type of biomass removal event
        /// </summary>
        [Description("Type of biomass removal.  This triggers events OnCutting, OnGrazing etc")]
        public BiomassRemovalType Removaltype { get; set; }

        /// <summary>
        /// The stage to set phenology to on removal event
        /// </summary>
        [Description("Stage to set phenology to on removal.  Leave blank if phenology not changed")]
        public double StageToSet { get; set; }

        /// <summary>
        /// Dates to trigger biomass removal events
        /// </summary>
        [Description("Removal Event Dates (comma seperated dd/mm/yyyy")]
        public string RemovalDatesInput { get; set; }

        /// <summary>
        /// Dates to trigger biomass removal events as dates
        /// Will append a default year to dates that do not have a year
        /// </summary>
        [JsonIgnore]
        public DateTime[] RemovalDates
        {
            get
            {
                if (String.IsNullOrEmpty(RemovalDatesInput))
                    return new List<DateTime>().ToArray();
                List<DateTime> dates = new List<DateTime>();
                string[] inputs = RemovalDatesInput.Split(',');
                foreach (string input in inputs)
                {
                    dates.Add(DateUtilities.GetDate(input));
                }
                return dates.ToArray();
            }
        }

        [Link] private Clock Clock = null;
        [Link(Type = LinkType.Child)] private BiomassRemovalFractions biomRemFra = null;
        
        /// <summary>
        /// Method to initiate biomass removal from plant
        /// </summary>
        public void Remove ()
        {
            biomRemFra.Do(this.Removaltype);
            if (StageToSet >= 1.0)
            {
                Phenology phenology = PlantToRemoveFrom.FindChild<Phenology>();
                phenology?.SetToStage(StageToSet);
            }
        }

        [EventSubscribe("DoManagement")]
        private void OnDoManagement(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(RemovalDatesInput))
                return;

            string[] inputs = RemovalDatesInput.Split(',');            
            foreach (DateTime d in RemovalDates)
            {
                if (d == Clock.Today)
                    Remove();                
            }
            return;
        }

    }
}