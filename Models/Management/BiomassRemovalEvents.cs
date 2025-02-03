using Models.Core;
using Models.Interfaces;
using Models.PMF.Interfaces;
using Models.PMF.Phen;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Newtonsoft.Json;
using APSIM.Shared.Utilities;
using Models.Utilities;
using Models.PMF;

namespace Models.Management
{
    /// <summary>
    /// This is used to create Biomass Removal actions on a crop without requiring a manager script.
    /// It has a linked crop plant, a type of removal, a list of dates to do the action on and a setting for changing the phenology.
    /// It has an array of removal values for each organ of the linked plant.
    /// The removal can be manually called from a manager script with the Remove() function.
    /// </summary>
    [ValidParent(ParentType = typeof(Zone))]
    [ValidParent(ParentType = typeof(Folder))]
    [Serializable]
    [ViewName("UserInterface.Views.PropertyAndGridView")]
    [PresenterName("UserInterface.Presenters.PropertyAndGridPresenter")]
    public class BiomassRemovalEvents : Model
    {
        /// <summary>
        /// Crop to remove biomass from
        /// </summary>
        [Description("Crop to remove biomass from")]
        [Display(Type = DisplayType.PlantName)]
        public string PlantToRemoveBiomassFrom { get; set; }

        [JsonIgnore]
        private Plant PlantToRemoveFrom { get; set; }

        /// <summary>
        /// The type of biomass removal event
        /// </summary>
        [Description("Type of biomass removal.  This triggers events OnCutting, OnGrazing etc")]
        public BiomassRemovalType RemovalType
        {
            get { return _removalType; }
            set { _removalType = value; LinkCrop(); }
        }
        [JsonIgnore]
        private BiomassRemovalType _removalType { get; set; }

        /// <summary>
        /// The stage to set phenology to on removal event
        /// </summary>
        [Description("Stage to set phenology to on removal.  Leave blank if phenology not changed")]
        [Display(Type = DisplayType.CropStageName)]
        public string StageToSet { get; set; }

        /// <summary>
        /// Dates to trigger biomass removal events
        /// </summary>
        [Description("Specific Removal Event Dates (comma seperated dd/mm/yyyy")]
        public string RemovalDatesInput { get; set; }

        /// <summary>
        /// Date to trigger annual biomass removal events
        /// </summary>
        [Description("Annual Removal Event Date (d-mmm).  Removal occurs on the same date each year")]
        public string RemovalDate { get; set; }

        /// <summary>Removal Options in Table</summary>
        [Display]
        public List<BiomassRemovalOfPlantOrganType> BiomassRemovals { get; set; }

        /// <summary>Cutting Event</summary>
        public event EventHandler<EventArgs> Cutting;

        /// <summary>Grazing Event</summary>
        public event EventHandler<EventArgs> Grazing;

        /// <summary>Pruning Event</summary>
        public event EventHandler<EventArgs> Pruning;

        /// <summary>Harvesting Event</summary>
        public event EventHandler<EventArgs> Harvesting;

        /// <summary>Harvesting Event</summary>
        public event EventHandler<EventArgs> EndCrop;

        [Link]
        private Clock Clock = null;

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
                    dates.Add(DateUtilities.GetDate(input));

                return dates.ToArray();
            }
        }

        /// <summary>
        /// Renames column headers for display
        /// </summary>
        public DataTable ConvertModelToDisplay(DataTable dt)
        {
            dt.Columns["PlantName"].ColumnName = "Plant";
            dt.Columns["OrganName"].ColumnName = "Organ";
            dt.Columns["TypeString"].ColumnName = "Type";
            dt.Columns["LiveToRemove"].ColumnName = "Live To Remove";
            dt.Columns["DeadToRemove"].ColumnName = "Dead To Remove";
            dt.Columns["LiveToResidue"].ColumnName = "Live To Residue";
            dt.Columns["DeadToResidue"].ColumnName = "Dead To Residue";
            return dt;
        }

        /// <summary>
        /// Renames the columns back to model property names
        /// </summary>
        public DataTable ConvertDisplayToModel(DataTable dt)
        {
            dt.Columns["Plant"].ColumnName = "PlantName";
            dt.Columns["Organ"].ColumnName = "OrganName";
            dt.Columns["Type"].ColumnName = "TypeString";
            dt.Columns["Live To Remove"].ColumnName = "LiveToRemove";
            dt.Columns["Dead To Remove"].ColumnName = "DeadToRemove";
            dt.Columns["Live To Residue"].ColumnName = "LiveToResidue";
            dt.Columns["Dead To Residue"].ColumnName = "DeadToResidue";
            return dt;
        }

        /// <summary>
        /// Method to initiate biomass removal from plant
        /// </summary>
        public void Remove()
        {
            LinkCrop();
            if (RemovalType.ToString() == BiomassRemovalType.Cutting.ToString())
                Cutting?.Invoke(this, new EventArgs());
            if (RemovalType.ToString() == BiomassRemovalType.Grazing.ToString())
                Grazing?.Invoke(this, new EventArgs());
            if (RemovalType.ToString() == BiomassRemovalType.Pruning.ToString())
                Pruning?.Invoke(this, new EventArgs());
            if (RemovalType.ToString() == BiomassRemovalType.Harvesting.ToString())
                Harvesting?.Invoke(this, new EventArgs());
            if (RemovalType.ToString() == BiomassRemovalType.EndCrop.ToString())
                EndCrop?.Invoke(this, new EventArgs());

            foreach (BiomassRemovalOfPlantOrganType removal in BiomassRemovals)
            {
                checkRemoval(removal);
                if (removal.Type == RemovalType)
                {
                    IOrgan organ = PlantToRemoveFrom.FindDescendant<IOrgan>(removal.OrganName);
                    (organ as IHasDamageableBiomass).RemoveBiomass(liveToRemove: removal.LiveToRemove,
                                                                   deadToRemove: removal.DeadToRemove,
                                                                   liveToResidue: removal.LiveToResidue,
                                                                   deadToResidue: removal.DeadToResidue);
                }
            }


            double stage;
            Double.TryParse(StageToSet, out stage);
            if (!double.IsNaN(stage) && stage >= 1.0)
            {
                Phenology phenology = PlantToRemoveFrom.FindChild<Phenology>();
                if (phenology != null)
                    phenology?.SetToStage(stage);
                else
                    throw new Exception($"Plant {PlantToRemoveFrom.Name} does not have a Phenology that can be set");
            }
        }

        [EventSubscribe("DoManagement")]
        private void OnDoManagement(object sender, EventArgs e)
        {
            if (!String.IsNullOrEmpty(RemovalDate))
            {
                if (DateUtilities.WithinDates(RemovalDate, Clock.Today, RemovalDate))
                {
                    Remove();
                }
            }

            if (String.IsNullOrEmpty(RemovalDatesInput))
                return;

            string[] inputs = RemovalDatesInput.Split(',');
            foreach (string date in inputs)
            {
                if (DateUtilities.CompareDates(date, Clock.Today) == 0)
                    Remove();
            }


            return;
        }

        [EventSubscribe("PhenologyDefoliate")]
        private void OnPhenologyDefoliate(object sender, BiomassRemovalEventArgs e)
        {
            if (RemovalType == e.RemovalType)
                Remove();
        }

        [EventSubscribe("PhenologyPrune")]
        private void OnPhenologyPrune(object sender, EventArgs e)
        {
            if (RemovalType == BiomassRemovalType.Pruning)
                Remove();
        }

        [EventSubscribe("PhenologyHarvest")]
        private void OnPhenologyHarvest(object sender, EventArgs e)
        {
            if (RemovalType == BiomassRemovalType.Harvesting)
                Remove();
        }

        private void LinkCrop()
        {
            if (this.Parent == null)
                return;

            //check if our plant is currently linked, link if not
            if (PlantToRemoveFrom == null)
                PlantToRemoveFrom = this.Parent.FindDescendant<Plant>();

            if (PlantToRemoveFrom != null)
                if (PlantToRemoveFrom.Parent == null)
                    PlantToRemoveFrom = this.Parent.FindDescendant<Plant>(PlantToRemoveFrom.Name);

            if (PlantToRemoveFrom == null)
                throw new Exception("BiomassRemovalEvents could not find a crop in this simulation.");

            if (BiomassRemovals == null)
                BiomassRemovals = new List<BiomassRemovalOfPlantOrganType>();

            //check if it has organs, if not, check if it is in replacements
            List<IOrgan> organs = PlantToRemoveFrom.FindAllDescendants<IOrgan>().ToList();
            if (organs.Count == 0)
            {
                Simulations sims = PlantToRemoveFrom.FindAncestor<Simulations>();
                Folder replacements = sims.FindChild<Folder>("Replacements");
                if (replacements != null)
                {
                    Plant plant = replacements.FindChild<Plant>(PlantToRemoveFrom.Name);
                    if (plant != null)
                        PlantToRemoveFrom = plant;
                }
            }

            //remove all non-matching plants
            for (int i = BiomassRemovals.Count - 1; i >= 0; i--)
            {
                BiomassRemovalOfPlantOrganType rem = BiomassRemovals[i];
                if (PlantToRemoveFrom.Name != rem.PlantName || RemovalType != rem.Type)
                    BiomassRemovals.Remove(rem);
            }
            //remove duplicates
            List<BiomassRemovalOfPlantOrganType> removeList = new List<BiomassRemovalOfPlantOrganType>();
            for (int i = BiomassRemovals.Count - 1; i >= 0; i--)
            {
                BiomassRemovalOfPlantOrganType rem = BiomassRemovals[i];
                for (int j = BiomassRemovals.Count - 1; j >= 0; j--)
                    if (!removeList.Contains(rem))
                        if (i != j && rem.OrganName == BiomassRemovals[j].OrganName)
                            removeList.Add(BiomassRemovals[j]);
            }
            for (int i = 0; i < removeList.Count; i++)
                BiomassRemovals.Remove(removeList[i]);

            //add in organs that are missing
            foreach (IOrgan organ in organs)
            {
                bool isInList = false;
                for (int i = 0; i < BiomassRemovals.Count && !isInList; i++)
                    if (organ.Name == BiomassRemovals[i].OrganName)
                        isInList = true;

                if (!isInList)
                {
                    BiomassRemovalOfPlantOrganType rem = new BiomassRemovalOfPlantOrganType(PlantToRemoveFrom.Name, organ.Name, RemovalType.ToString(), 0, 0, 0, 0);
                    BiomassRemovals.Add(rem);
                }
            }
        }
        /// <summary>
        /// Method to check each biomass removal for invalid parameters
        /// </summary>
        /// <param name="removal"></param>
        /// <exception cref="Exception"></exception>
        private void checkRemoval(BiomassRemovalOfPlantOrganType removal)
        {
            List<double> removals = new List<double>{removal.LiveToRemove,
                                                     removal.DeadToRemove,
                                                     removal.LiveToResidue,
                                                     removal.DeadToResidue};

            foreach (double rem in removals)
            {
                if (Double.IsNaN(rem))
                    throw new Exception("a removal fraction in " + this.Name + " is not a number.  all values must be numbers between zero and one");
                if (rem < 0)
                    throw new Exception("a removal fraction in " + this.Name + " is negative.  all values must be numbers between zero and one");
                if (rem > 1)
                    throw new Exception("a removal fraction in " + this.Name + " greater than one.  all values must be numbers between zero and one");
            }
        }

    }
    /// <summary>
    /// Arguments passed to defoliate event when stage is reset
    /// </summary>
    public class BiomassRemovalEventArgs : EventArgs
    {
        /// <summary>
        /// Type of biomass removal
        /// </summary>
        public BiomassRemovalType RemovalType { get; set; }
    }
}