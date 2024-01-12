using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using Models.CLEM.Resources;
using System.ComponentModel.DataAnnotations;
using System.IO;

namespace Models.CLEM.Activities
{
    /// <summary>Ruminant enteric methane calulator (Charmley equation)</summary>
    /// <version>1.0</version>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(RuminantActivityGrowSCA))]
    [Description("Produces enteric methane emissions based on Charmley et al equations")]
    [HelpUri(@"Content/Features/Activities/Ruminant/RuminantEntericCH4Charmley.htm")]
    public class RuminantEntericCH4Charmley: CLEMRuminantActivityBase
    {
        private GreenhouseGasesType methaneEmissions;
        private RuminantHerd ruminantHerd;

        /// <summary>
        /// Transaction grouping style
        /// </summary>
        [Description("Herd emissions grouping style")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Herd emissions grouping style required")]
        [System.ComponentModel.DefaultValue(RuminantEmissionsGroupingStyle.Combined)]
        public RuminantEmissionsGroupingStyle GroupingStyle { get; set; }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            // find first GreenhouseGasType flagged to autocollect methane. 
            methaneEmissions = FindAllInScope<GreenhouseGasesType>().Where(a => a.AutoCollectType == GreenhouseGasTypes.CH4).FirstOrDefault();
            ruminantHerd = Resources.FindResourceGroup<RuminantHerd>();
        }

        /// <summary>Function to calculate enteric methane from values saved to each individual by Grow activities</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMAnimalWeightGain")]
        private void OnCLEMAnimalWeightGain(object sender, EventArgs e)
        {
            // As this model is a child of RuminantActivityGrow it will be preformed after weight gain in growth
            // The CLEMAnimalWeightGain event is used for the calulation of all energy and growth.
            
            if (methaneEmissions is null)
                return;
            Status = ActivityStatus.NotNeeded;

            foreach (var ruminant in ruminantHerd.Herd)
            {
                // Charmley et al 2016 can be substituted by MethaneProductionIntercept = 0 and MethaneProductionCoefficient = 20.7
                ruminant.Output.Methane = ruminant.Parameters.General.MethaneProductionCoefficient * ruminant.Intake.Feed.Actual;
            }

            // determine grouping style to report emissions
            IEnumerable <Tuple<string, double>> aa = null;
            switch (GroupingStyle)
            {
                case RuminantEmissionsGroupingStyle.ByIndividual:
                    aa = ruminantHerd.Herd.GroupBy(a => a.ID).Select(t => new Tuple<string, double> ($"Rum_{t.Key}", t.Sum(u => u.Output.Methane) ));
                    break;
                case RuminantEmissionsGroupingStyle.ByClass:
                    aa = ruminantHerd.Herd.GroupBy(a => a.Class).Select(t => new Tuple<string, double>(t.Key, t.Sum(u => u.Output.Methane)));
                    break;
                case RuminantEmissionsGroupingStyle.BySexAndClass:
                    aa = ruminantHerd.Herd.GroupBy(a => a.SexAndClass).Select(t => new Tuple<string, double>(t.Key, t.Sum(u => u.Output.Methane)));
                    break;
                case RuminantEmissionsGroupingStyle.ByHerd:
                    aa = ruminantHerd.Herd.GroupBy(a => a.HerdName).Select(t => new Tuple<string, double>(t.Key, t.Sum(u => u.Output.Methane)));
                    break;
                case RuminantEmissionsGroupingStyle.ByBreed:
                    aa = ruminantHerd.Herd.GroupBy(a => a.Breed).Select(t => new Tuple<string, double>(t.Key, t.Sum(u => u.Output.Methane)));
                    break;
                case RuminantEmissionsGroupingStyle.Combined:
                    aa = ruminantHerd.Herd.GroupBy(a => "All").Select(t => new Tuple<string, double>(t.Key, t.Sum(u => u.Output.Methane)));
                    break;
            }

            foreach (Tuple<string, double> a in aa)
            {
                // g -> total kg. per timestep calculated for each individual in CalculateEnergy()  
                methaneEmissions?.Add(a.Item2 / 1000, this, a.Item1, TransactionCategory);
            }
        }

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            using StringWriter htmlWriter = new();
            htmlWriter.Write("\r\n<div class=\"activityentry\">Methane emissions will be placed in the first [GreenhouseGasType] automatically receiving Methane (CH<sub>4</sub>)");
            return htmlWriter.ToString();
        }
        #endregion

    }
}
