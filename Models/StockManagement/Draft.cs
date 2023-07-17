using System;
using System.Collections.Generic;
using System.Linq;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.GrazPlan;
using Models.PMF.Interfaces;

namespace Models.StockManagement
{

    /// <summary>
    /// An instance of this class creates a genotype cross and adds it to the list of
    /// available crosses.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Simulation))]
    [ValidParent(ParentType = typeof(Folder))]
    public class Draft : Model
    {
        [Link]
        private Stock stock = null;

        [Link]
        private IClock clock = null;

        [Link]
        private List<Zone> zones = null;

        /// <summary>The different types of grazing.</summary>
        public enum DraftType
        {
            /// <summary>Fixed grazing.</summary>
            Fixed,

            /// <summary>Flexible grazing.</summary>
            Flexible
        }

        /// <summary>Start date of grazing.</summary>
        [Separator("Animal draft parameters")]
        [Description("Start date of draft (dd-mmm)")]
        public string StartDate;

        /// <summary>End date of grazing.</summary>
        [Description("End date of draft (dd-mmm)")]
        public string EndDate;

        /// <summary>The paddock names where the tagged animals move to.</summary>
        [Description("Paddock names to move the tagged animals into (comma separated)")]
        public string[] PaddockNames { get; set; }

        /// <summary>Type of grazing.</summary>
        [Description("Type of draft")]
        public DraftType TypeOfDraft { get; set; }


        /// <summary>The tag numbers of the animals to move.</summary>
        [Separator("Fixed animal draft")]
        [Description("Tag numbers of animal groups to move (comma separated)")]
        [Display(EnabledCallback = "TypeIsFixed")]
        public int[] TagNumbers { get; set; }

        /// <summary>The tag numbers of the highest priority animals.</summary>
        [Separator("Flexible animal draft")]

        [Description("The tag numbers of the highest priority animals (comma separated)")]
        [Display(EnabledCallback = "TypeIsFlexible")]
        public int[] TagNumberPriority1 { get; set; }

        /// <summary>The tag numbers of the second highest priority animals.</summary>
        [Description("The tag numbers of the second priority animals (comma separated)")]
        [Display(EnabledCallback = "TypeIsFlexible")]
        public int[] TagNumberPriority2 { get; set; }

        /// <summary>The tag numbers of the third priority animals.</summary>
        [Description("The tag numbers of the third priority animals (comma separated)")]
        [Display(EnabledCallback = "TypeIsFlexible")]
        public int[] TagNumberPriority3 { get; set; }

        /// <summary>How often should the flexible draft be conducted (days)</summary>
        [Description("How often should the flexible draft be conducted (days)")]
        [Display(EnabledCallback = "TypeIsFlexible")]
        public int CheckEvery { get; set; }

        /// <summary>Returns true if the grazing type is fixed.</summary>
        public bool TypeIsFixed { get { return TypeOfDraft == DraftType.Fixed; } }

        /// <summary>Returns true if the grazing type is flexible.</summary>
        public bool TypeIsFlexible { get { return TypeOfDraft == DraftType.Flexible; } }


        /// <summary>
        /// Invoked every day to do management.
        /// </summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">The event arguments</param>
        [EventSubscribe("DoManagement")]
        private void OnDoManagement(object sender, EventArgs e)
        {
            if (DateUtilities.WithinDates(StartDate, clock.Today, EndDate))
            {
                if (TypeOfDraft == DraftType.Fixed)
                {
                    // Loop through all tag numbers.
                    for (int t = 0; t < TagNumbers.Length; t++)
                    {
                        // Find the animal groups that have the tag number and move them to the
                        // specified padock.
                        foreach (var group in stock.StockModel.Animals.Skip(1).Where(g => g.Tag == TagNumbers[t]))
                            group.MoveToPaddock(PaddockNames[t]);
                    }
                }
                else
                {
                    var fullStartDate = DateUtilities.GetDate(StartDate, clock.Today);
                    if (fullStartDate > clock.Today)
                        fullStartDate = fullStartDate.AddYears(-1);
                    var numDaysSinceStartDate = (clock.Today - fullStartDate).Days;

                    // Flexible grazing
                    // X day intervals from the start of the period - is this the check day or day one?
                    if (CheckEvery > 0 && numDaysSinceStartDate % CheckEvery == 0)
                        MoveAnimals();
                }
            }
        }

        /// <summary>
        /// Perform a draft (move animals) into the specified paddocks. Only move those animals
        /// that have the specified tag numbers. Tags are assumed to be in priority order - the
        /// first tag is the highest priority, the second tag is the 2nd highest etc. The highest
        /// priority tag gets the best paddock (the one with the most forage), the second highest
        /// priority tag gets the second best paddock etc.
        /// </summary>
        private void MoveAnimals()
        {
            // Get a list of fields that we want to consider for a draft.
            var fields = new List<FieldWithForage>();
            foreach (var zone in zones.Where(zone => PaddockNames.Contains(zone.Name)))
                fields.Add(new FieldWithForage(zone, stock));

            // Sort paddocks according to how much forage they have (highest first in list).
            var sortedFields = fields.OrderByDescending(f => f.AmountForage).ToList();

            if (sortedFields.Count >= 1 && TagNumberPriority1 != null)
                sortedFields[0].MoveAnimals(TagNumberPriority1); // move highest priority animals into best paddock.
            if (sortedFields.Count >= 2 && TagNumberPriority2 != null)
                sortedFields[1].MoveAnimals(TagNumberPriority2); // move 2nd highest priority animals into 2nd best paddock.
            if (sortedFields.Count >= 3 && TagNumberPriority3 != null)
                sortedFields[2].MoveAnimals(TagNumberPriority3); // move 3rd highest priority animals into 3rd best paddock.
        }

        /// <summary>A private class to encapsulate the forages in a field.</summary>
        private class FieldWithForage
        {
            private Zone field;
            private IEnumerable<AnimalGroup> animalGroups;
            private List<IOrganDamage> forageOrgans = new List<IOrganDamage>();

            /// <summary>Constructor</summary>
            /// <param name="zone">The zone object representing the field.</param>
            /// <param name="stockModel">The stock model.</param>
            public FieldWithForage(Zone zone, Stock stockModel)
            {
                field = zone;
                animalGroups = stockModel.StockModel.Animals;
                foreach (IPlantDamage forage in zone.FindAllDescendants<IPlantDamage>())
                    foreach (var organ in forage.Organs)
                        if (organ.IsAboveGround)
                            forageOrgans.Add(organ);
            }

            /// <summary>The amount for forage of all above ground organs (g/m2)</summary>
            public double AmountForage { get { return forageOrgans.Sum(f => f.Live.Wt); } }

            /// <summary>Move animals withe the specified tag numbers</summary>
            /// <param name="tagNumbers">Tag numbers of animal groups to move.</param>
            public void MoveAnimals(int[] tagNumbers)
            {
                var animalsToMove = animalGroups.Skip(1).Where(g => tagNumbers.Contains(g.Tag));
                foreach (var animalGroup in animalsToMove)
                    animalGroup.MoveToPaddock(field.Name);
            }
        }
    }
}