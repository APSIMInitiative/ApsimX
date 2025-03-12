using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using Models.CLEM.Resources;
using System.ComponentModel.DataAnnotations;
using System.IO;
using DocumentFormat.OpenXml.Drawing;
using Models.Core.Attributes;

namespace Models.CLEM.Activities
{
    /// <summary>Ruminant enteric methane calulator (Charmley equation)</summary>
    /// <version>1.0</version>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(RuminantActivityGrowPF))]
    [Description("Produces enteric methane emissions based on Charmley et al equations")]
    [HelpUri(@"Content/Features/Activities/Ruminant/RuminantEntericCH4Charmley.htm")]
    [MinimumTimeStepPermitted(TimeStepTypes.Daily)]
    [ModelAssociations(associatedModels: new Type[] { typeof(RuminantParametersMethaneCharmley) }, associationStyles: new ModelAssociationStyle[] { ModelAssociationStyle.DescendentOfRuminantType })]
    public class RuminantActivityEntericCH4Charmley: CLEMRuminantActivityBase
    {
        [Link(IsOptional = true)]
        private CLEMEvents events = null;

        private GreenhouseGasesType methaneEmissions;

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
            if(methaneEmissions is not null)
                InitialiseHerd(false, true);
        }

        /// <summary>Function to calculate enteric methane from values saved to each individual by Grow activities</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMAnimalWeightGain")]
        private void OnCLEMAnimalWeightGain(object sender, EventArgs e)
        {
            // As this model is a child of RuminantActivityGrow and will be performed after CLEMAnimalWeightGain in growth.
            if (methaneEmissions is null)
                return;
            Status = ActivityStatus.NotNeeded;

            // Blaxter and Claperton 1965
            //ind.Output.Methane = ind.Paramaters.GrowPF_CH.CH1 * (ind.Intake.Feed.Actual) * ((ind.GrowPF_CH.CH2 + ind.GrowPF_CH.CH3 * ind.Intake.MDSolid) + (feedingLevel + 1) * (ind.GrowPF_CH.CH4 + ind.GrowPF_CH.CH5 * ind.Intake.MDSolid));

            // Function to calculate approximate methane produced by animal, based on feed intake based on Freer spreadsheet
            // methaneproduced is  0.02 * intakeDaily * ((13 + 7.52 * energyMetabolic) + energyMetablicFromIntake / energyMaintenance * (23.7 - 3.36 * energyMetabolic)); // MJ per day
            // methane is methaneProduced / 55.28 * 1000; // grams per day

            // Charmley et al 2016 can be substituted by intercept = 0 and coefficient = 20.7
            // per day at this point.
            //ind.Output.Methane = ind.Parameters.General.MethaneProductionCoefficient * intakeDaily * events.Interval;

            var herd = CurrentHerd(true);
            foreach (var ruminant in herd)
            {
                // Charmley et al 2016 can be substituted by MethaneProductionIntercept = 0 and MethaneProductionCoefficient = 20.7
                ruminant.Output.Methane = ruminant.Parameters.EntericMethaneCharmley.MethaneProductionCoefficient * ruminant.Intake.SolidsDaily.ActualForTimeStep(events.Interval);
            }

            // determine grouping style to report emissions
            IEnumerable <Tuple<string, double>> aa = null;
            switch (GroupingStyle)
            {
                case RuminantEmissionsGroupingStyle.ByIndividual:
                    aa = herd.GroupBy(a => a.ID).Select(t => new Tuple<string, double> ($"Rum_{t.Key}", t.Sum(u => u.Output.Methane) ));
                    break;
                case RuminantEmissionsGroupingStyle.ByClass:
                    aa = herd.GroupBy(a => a.Class).Select(t => new Tuple<string, double>(t.Key, t.Sum(u => u.Output.Methane)));
                    break;
                case RuminantEmissionsGroupingStyle.BySexAndClass:
                    aa = herd.GroupBy(a => a.SexAndClass).Select(t => new Tuple<string, double>(t.Key, t.Sum(u => u.Output.Methane)));
                    break;
                case RuminantEmissionsGroupingStyle.ByHerd:
                    aa = herd.GroupBy(a => a.HerdName).Select(t => new Tuple<string, double>(t.Key, t.Sum(u => u.Output.Methane)));
                    break;
                case RuminantEmissionsGroupingStyle.ByBreed:
                    aa = herd.GroupBy(a => a.Breed).Select(t => new Tuple<string, double>(t.Key, t.Sum(u => u.Output.Methane)));
                    break;
                case RuminantEmissionsGroupingStyle.Combined:
                    aa = herd.GroupBy(a => "All").Select(t => new Tuple<string, double>(t.Key, t.Sum(u => u.Output.Methane)));
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
