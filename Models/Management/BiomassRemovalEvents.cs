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
    [PresenterName("UserInterface.Presenters.PropertyAndTablePresenter")]
    public class BiomassRemovalEvents : Model, IModelAsTable
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
        public BiomassRemovalType RemovalType { get; set; }

        /// <summary>
        /// The stage to set phenology to on removal event
        /// </summary>
        [Description("Stage to set phenology to on removal.  Leave blank if phenology not changed")]
        public string StageToSet { get; set; }

        /// <summary>
        /// Dates to trigger biomass removal events
        /// </summary>
        [Description("Removal Event Dates (comma seperated dd/mm/yyyy")]
        public string RemovalDatesInput { get; set; }

        /// <summary>Removal Options in Table</summary>
        public List<BiomassRemovalOfPlantOrganType> BiomassRemovals { get; set; }

        /// <summary>Cutting Event</summary>
        public event EventHandler<EventArgs> Cutting;

        /// <summary>Grazing Event</summary>
        public event EventHandler<EventArgs> Grazing;

        /// <summary>Pruning Event</summary>
        public event EventHandler<EventArgs> Pruning;

        /// <summary>Harvesting Event</summary>
        public event EventHandler<EventArgs> Harvesting;

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
        /// Gets or sets the table of values.
        /// </summary>
        [JsonIgnore]
        public List<DataTable> Tables
        {
            get
            {
                LinkCrop();

                List<DataTable> tables = new List<DataTable>();

                //Add table headers
                var data = new DataTable();
                data.Columns.Add("Plant");
                data.Columns.Add("Type");
                data.Columns.Add("Organ");
                data.Columns.Add("Live To Remove");
                data.Columns.Add("Dead To Remove");
                data.Columns.Add("Live To Residue");
                data.Columns.Add("Dead To Residue");
                data.Columns.Add(" "); // add an empty table so that the last column doesn't stretch across the screen

                //Create the list of removal fractions for the crop specified in the parent BiomassRemovalEvents
                List<IOrgan> organs = PlantToRemoveFrom.FindAllDescendants<IOrgan>().ToList();
                foreach (IOrgan organ in organs)
                {
                    BiomassRemovalType type = RemovalType;
                    DataRow row = data.NewRow();
                    row["Plant"] = PlantToRemoveFrom.Name;
                    row["Type"] = type;
                    row["Organ"] = organ.Name;
                    data.Rows.Add(row);
                }

                if (BiomassRemovals != null)
                {
                    //add in stored values
                    foreach (BiomassRemovalOfPlantOrganType removal in BiomassRemovals)
                    {
                        //find which line of the data table matches the organ.
                        //We don't check Plant Name or Cut Type anymore so that they aren't cleared when properties are changed
                        foreach (DataRow row in data.Rows)
                        {
                            if (row["Organ"].ToString().Equals(removal.OrganName))
                            {
                                //matching row, fill in fractions
                                row["Live To Remove"] = removal.LiveToRemove;
                                row["Dead To Remove"] = removal.DeadToRemove;
                                row["Live To Residue"] = removal.LiveToResidue;
                                row["Dead To Residue"] = removal.DeadToResidue;
                            }
                        }
                    }
                }

                tables.Add(data);
                //pass this generated table back to be stored
                this.Tables = tables;
                return tables;
            }
            set
            {
                BiomassRemovals = new List<BiomassRemovalOfPlantOrganType>();
                DataTable data = value[0];
                foreach (DataRow row in data.Rows)
                {
                    string plantName = row["Plant"].ToString();
                    string organName = row["Organ"].ToString();
                    string type = row["Type"].ToString();
                    double liveToRemove, deadToRemove, liveToResidue, deadToResidue;

                    double.TryParse(row["Live To Remove"].ToString(), out liveToRemove);
                    double.TryParse(row["Dead To Remove"].ToString(), out deadToRemove);
                    double.TryParse(row["Live To Residue"].ToString(), out liveToResidue);
                    double.TryParse(row["Dead To Residue"].ToString(), out deadToResidue);

                    if (!String.IsNullOrEmpty(plantName)) // Dont add the blank row at the end as a removal.
                        BiomassRemovals.Add(new BiomassRemovalOfPlantOrganType(plantName, organName, type, liveToRemove, deadToRemove, liveToResidue, deadToResidue));
                }
                return;
            }
        }

        /// <summary>
        /// Method to initiate biomass removal from plant
        /// </summary>
        public void Remove()
        {
            LinkCrop();

            foreach (BiomassRemovalOfPlantOrganType removal in BiomassRemovals)
            {
                if (removal.Type == RemovalType)
                {
                    IOrgan organ = PlantToRemoveFrom.FindDescendant<IOrgan>(removal.OrganName);
                    (organ as IHasDamageableBiomass).RemoveBiomass(liveToRemove: removal.LiveToRemove,
                                                                   deadToRemove: removal.DeadToRemove,
                                                                   liveToResidue: removal.LiveToResidue,
                                                                   deadToResidue: removal.DeadToResidue);
                }
            }

            if (PlantToRemoveFrom.ToString() == BiomassRemovalType.Cutting.ToString())
                Cutting?.Invoke(this, new EventArgs());
            if (PlantToRemoveFrom.ToString() == BiomassRemovalType.Grazing.ToString())
                Grazing?.Invoke(this, new EventArgs());
            if (PlantToRemoveFrom.ToString() == BiomassRemovalType.Pruning.ToString())
                Pruning?.Invoke(this, new EventArgs());
            if (PlantToRemoveFrom.ToString() == BiomassRemovalType.Harvesting.ToString())
                Harvesting?.Invoke(this, new EventArgs());

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

        private void LinkCrop()
        {
            //check if our plant is currently linked, link if not
            if (PlantToRemoveFrom == null)
                PlantToRemoveFrom = this.Parent.FindDescendant<IPlant>();

            if (PlantToRemoveFrom != null)
                if (PlantToRemoveFrom.Parent == null)
                    PlantToRemoveFrom = this.Parent.FindDescendant<IPlant>(PlantToRemoveFrom.Name);

            //check if it has organs, if not, check if it is in replacements
            List<IOrgan> organs = PlantToRemoveFrom.FindAllDescendants<IOrgan>().ToList();
            if (organs.Count == 0)
            {
                Simulations sims = PlantToRemoveFrom.FindAncestor<Simulations>();
                Folder replacements = sims.FindChild<Folder>("Replacements");
                if (replacements != null)
                {
                    IPlant plant = replacements.FindChild<IPlant>(PlantToRemoveFrom.Name);
                    if (plant != null)
                        PlantToRemoveFrom = plant;
                }
            }

            if (PlantToRemoveFrom == null)
                throw new Exception("BiomassRemovalEvents could not find a crop in this simulation.");
        }

    }
}