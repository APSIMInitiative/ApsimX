using System;
using System.Data;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.PMF;
using Models.PMF.Interfaces;
using Models.PMF.Phen;

namespace Models.Management
{
    /// <summary>
    /// This is used to create biomass removal actions on a crop without requiring a manager script.
    /// It has a linked crop plant, a type of removal, a list of dates to do the action on, and a setting for changing the phenology.
    /// It has also a list of values for the removal fractions for each organ of the linked plant.
    /// The removal can be triggered 'manually' from a schedule or manager script by calling the Remove() method.
    /// </summary>
    [ValidParent(ParentType = typeof(Zone))]
    [ValidParent(ParentType = typeof(Folder))]
    [Serializable]
    [ViewName("UserInterface.Views.PropertyAndGridView")]
    [PresenterName("UserInterface.Presenters.PropertyAndGridPresenter")]
    public class BiomassRemovalEvents : Model
    {
        /// <summary>Name of crop to remove biomass from.</summary>
        [Description("Crop to remove biomass from:")]
        [Display(Type = DisplayType.PlantName)]
        public string NameOfPlantToRemoveFrom
        {
            get { return _NameOfPlantToRemoveFrom; }
            set { _NameOfPlantToRemoveFrom = value; CheckCropIsLinked(); }
        }

        private string _NameOfPlantToRemoveFrom {get;set;}

        /// <summary>Crop to remove biomass from.</summary>
        [JsonIgnore]
        public Plant PlantInstanceToRemoveFrom { get; private set; }

        /// <summary>The type of biomass removal event.</summary>
        [Description("Type of biomass removal (triggers events OnCutting, OnGrazing, etc.):")]
        public BiomassRemovalType RemovalType
        {
            get { return _RemovalType; }
            set { _RemovalType = value; CheckCropIsLinked(); }
        }

        /// <summary>Internal type of biomass removal event.</summary>
        private BiomassRemovalType _RemovalType { get; set; }

        /// <summary>The stage to set phenology to on removal event.</summary>
        [Description("Stage to set phenology to on removal (leave blank if not changing):")]
        [Display(Type = DisplayType.CropStageName)]
        public string StageToSet
        {
            get { return _StageToSet; }
            set { _StageToSet = value; CheckCropIsLinked(); }
        }

        private string _StageToSet { get; set; }
        /// <summary>List of dates to trigger biomass removal events.</summary>
        [Description("List of dates for removal events (comma separated, dd/mm/yyyy or dd-mmm):")]
        public string[] RemovalDates { get; set; }

        /// <summary>List of all biomass removal fractions, per organ.</summary>
        [Display(Type = DisplayType.SubModel)]
        public List<BiomassRemovalOfPlantOrganType> BiomassRemovalFractions { get; set; }

        /// <summary>Cutting Event.</summary>
        public event EventHandler<EventArgs> Cutting;

        /// <summary>Grazing Event.</summary>
        public event EventHandler<EventArgs> Grazing;

        /// <summary>Pruning Event.</summary>
        public event EventHandler<EventArgs> Pruning;

        /// <summary>Harvesting Event.</summary>
        public event EventHandler<EventArgs> Harvesting;

        /// <summary>Harvesting Event.</summary>
        public event EventHandler<EventArgs> EndCrop;

        /// <summary>Link to the simulation clock.</summary>
        [Link]
        private Clock Clock = null;

        /// <summary>Renames column headers for display.</summary>
        public DataTable ConvertModelToDisplay(DataTable removalData)
        {
            removalData.Columns["PlantName"].ColumnName = "Plant";
            removalData.Columns["OrganName"].ColumnName = "Organ";
            removalData.Columns["TypeString"].ColumnName = "Type";
            removalData.Columns["LiveToRemove"].ColumnName = "Live To Remove";
            removalData.Columns["DeadToRemove"].ColumnName = "Dead To Remove";
            removalData.Columns["LiveToResidue"].ColumnName = "Live To Residue";
            removalData.Columns["DeadToResidue"].ColumnName = "Dead To Residue";
            return removalData;
        }

        /// <summary>Renames the columns back to model property names.</summary>
        public DataTable ConvertDisplayToModel(DataTable removalData)
        {
            removalData.Columns["Plant"].ColumnName = "PlantName";
            removalData.Columns["Organ"].ColumnName = "OrganName";
            removalData.Columns["Type"].ColumnName = "TypeString";
            removalData.Columns["Live To Remove"].ColumnName = "LiveToRemove";
            removalData.Columns["Dead To Remove"].ColumnName = "DeadToRemove";
            removalData.Columns["Live To Residue"].ColumnName = "LiveToResidue";
            removalData.Columns["Dead To Residue"].ColumnName = "DeadToResidue";
            return removalData;
        }

        /// <summary>Sets up a biomass removal from plant.</summary>
        public void Remove()
        {
            CheckCropIsLinked();
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

            foreach (BiomassRemovalOfPlantOrganType removalFraction in BiomassRemovalFractions)
            {
                checkRemoval(removalFraction);
                if (removalFraction.Type == RemovalType)
                {
                    IOrgan organ = PlantInstanceToRemoveFrom.FindDescendant<IOrgan>(removalFraction.OrganName);
                    (organ as IHasDamageableBiomass).RemoveBiomass(liveToRemove: removalFraction.LiveToRemove,
                                                                   deadToRemove: removalFraction.DeadToRemove,
                                                                   liveToResidue: removalFraction.LiveToResidue,
                                                                   deadToResidue: removalFraction.DeadToResidue);
                }
            }

            if ((StageToSet != "")&&(StageToSet != null))
            {
                Phenology phenology = PlantInstanceToRemoveFrom.FindChild<Phenology>();
                if (phenology != null)
                    phenology?.SetToStage(StageToSet);
                else
                    throw new Exception($"Plant {PlantInstanceToRemoveFrom.Name} does not have a Phenology that can be set");
            }
        }

        [EventSubscribe("DoManagement")]
        private void OnDoManagement(object sender, EventArgs e)
        {
            if ((RemovalDates != null) && (RemovalDates.Length > 0))
            { // some date were given, check whether removal can be triggered
                foreach (string date in RemovalDates)
                {
                    if (DateUtilities.CompareDates(date, Clock.Today) == 0)
                    { // date match, trigger a removal
                        Remove();
                    }
                }
            }
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

        /// <summary>Checks whether the crop and its organs are linked, and link if not.</summary>
        private void CheckCropIsLinked()
        {
            if (this.Parent == null)
                return;

            //check if our plant is currently linked, link if not
            if (PlantInstanceToRemoveFrom == null)
                PlantInstanceToRemoveFrom = this.Parent.FindDescendant<Plant>(NameOfPlantToRemoveFrom);

            if (PlantInstanceToRemoveFrom != null)
                if (PlantInstanceToRemoveFrom.Parent == null)
                    PlantInstanceToRemoveFrom = this.Parent.FindDescendant<Plant>(NameOfPlantToRemoveFrom);

            if (PlantInstanceToRemoveFrom == null)
                throw new Exception("BiomassRemovalEvents could not find a crop in this simulation.");

            if (BiomassRemovalFractions == null)
                BiomassRemovalFractions = new List<BiomassRemovalOfPlantOrganType>();

            //check if it has organs, if not, check if it is in replacements

            try
            {
                Folder replacements = Folder.FindReplacementsFolder(PlantInstanceToRemoveFrom);
                if (replacements != null)
                {
                    Plant plant = replacements.FindChild<Plant>(PlantInstanceToRemoveFrom.Name);
                    if (plant != null)
                        PlantInstanceToRemoveFrom = plant;
                }
            }
            catch
            { }

            
            List<IOrgan> organs = PlantInstanceToRemoveFrom.FindAllDescendants<IOrgan>().ToList();


            //remove all non-matching plants
            for (int i = BiomassRemovalFractions.Count - 1; i >= 0; i--)
            {
                BiomassRemovalOfPlantOrganType rem = BiomassRemovalFractions[i];
                if (PlantInstanceToRemoveFrom.Name != rem.PlantName || RemovalType != rem.Type)
                    BiomassRemovalFractions.Remove(rem);
            }

            //remove duplicates
            List<BiomassRemovalOfPlantOrganType> removeList = new List<BiomassRemovalOfPlantOrganType>();
            for (int i = BiomassRemovalFractions.Count - 1; i >= 0; i--)
            {
                BiomassRemovalOfPlantOrganType rem = BiomassRemovalFractions[i];
                for (int j = BiomassRemovalFractions.Count - 1; j >= 0; j--)
                {
                    if (!removeList.Contains(rem))
                        if (i != j && rem.OrganName == BiomassRemovalFractions[j].OrganName)
                            removeList.Add(BiomassRemovalFractions[j]);
                }
            }

            for (int i = 0; i < removeList.Count; i++)
                BiomassRemovalFractions.Remove(removeList[i]);

            //add in organs that are missing
            foreach (IOrgan organ in organs)
            {
                bool isInList = false;
                for (int i = 0; i < BiomassRemovalFractions.Count && !isInList; i++)
                {
                    if (organ.Name == BiomassRemovalFractions[i].OrganName)
                        isInList = true;
                }

                if (!isInList)
                {
                    BiomassRemovalOfPlantOrganType rem = new BiomassRemovalOfPlantOrganType(PlantInstanceToRemoveFrom.Name, organ.Name, RemovalType.ToString(), 0, 0, 0, 0);
                    BiomassRemovalFractions.Add(rem);
                }
            }
        }

        /// <summary>Checks each biomass removal for invalid parameters.</summary>
        /// <param name="removalFractions">Fractions of biomass to remove (live and dead, to remove and to residue)</param>
        private void checkRemoval(BiomassRemovalOfPlantOrganType removalFractions)
        {
            List<double> removals = new List<double>{removalFractions.LiveToRemove,
                                                     removalFractions.DeadToRemove,
                                                     removalFractions.LiveToResidue,
                                                     removalFractions.DeadToResidue};

            foreach (double rem in removals)
            {
                if (double.IsNaN(rem))
                    throw new Exception("a removal fraction in " + Name + " is not a number. All values must be numbers between zero and one");
                if (rem < 0)
                    throw new Exception("a removal fraction in " + Name + " is negative. All values must be numbers between zero and one");
                if (rem > 1)
                    throw new Exception("a removal fraction in " + Name + " greater than one. All values must be numbers between zero and one");
            }
        }

    }
    /// <summary>Set of arguments passed to defoliate event when stage is reset.</summary>
    public class BiomassRemovalEventArgs : EventArgs
    {
        /// <summary>Type of biomass removal.</summary>
        public BiomassRemovalType RemovalType { get; set; }
    }
}