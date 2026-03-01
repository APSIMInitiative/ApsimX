using Models.CLEM.Activities;
using Models.CLEM.Groupings;
using Models.CLEM.Interfaces;
using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// A component to specify an attribute value from herd summary
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(RuminantActivityControlledMating))]
    [Description("Specify an attribute for the individual with associated value")]
    [HelpUri(@"Content/Features/Resources/SetAttributeWithValue.htm")]
    [Version(1, 0, 1, "")]
    public class SetAttributeFromHerd : CLEMModel, ISetAttribute
    {
        private IndividualAttribute lastInstance { get; set; } = null;
        private IEnumerable<RuminantGroup> filterGroups;
        private CLEMRuminantActivityBase ruminantActivity;

        /// <inheritdoc/>
        [Description("Name of attribute")]
        [Required(AllowEmptyStrings = false)]
        public string AttributeName { get; set; }

        /// <inheritdoc/>
        [System.ComponentModel.DefaultValueAttribute(0)]
        [Description("Style of determining value")]
        [Required]
        public SetAttributeFromHerdType CalculationStyle { get; set; }

        /// <inheritdoc/>
        [System.ComponentModel.DefaultValueAttribute(0)]
        [Description("Style of inheritance")]
        [Required]
        public AttributeInheritanceStyle InheritanceStyle { get; set; }

        /// <inheritdoc/>
        [System.ComponentModel.DefaultValueAttribute(false)]
        [Description("Mandatory attribute")]
        [Required]
        public bool Mandatory { get; set; }

        /// <summary>
        /// Multiplier to adjust calculated value
        /// </summary>
        [System.ComponentModel.DefaultValueAttribute(1)]
        [Description("Value multiplier")]
        [Required]
        public float Multiplier { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public SetAttributeFromHerd()
        {
            base.SetDefaults();
            base.ModelSummaryStyle = HTMLSummaryStyle.SubResource;
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            filterGroups = Structure.FindChildren<RuminantGroup>();
            ruminantActivity = Structure.FindParents<CLEMRuminantActivityBase>().FirstOrDefault();
        }

        /// <inheritdoc/>
        public IndividualAttribute GetAttribute(bool createNewInstance = true)
        {
            if (createNewInstance || lastInstance is null)
            {
                // get all individual Attributes from unique individuals across all filter groups provided
                var attributeInds = CLEMRuminantActivityBase.GetUniqueIndividuals<Ruminant>(filterGroups,
                                                                                            ruminantActivity.CurrentHerd().Where(a => a.Attributes.Exists(AttributeName)),
                                                                                            Structure);
                var inds = attributeInds.Select(a => (float)a.Attributes.GetValue(AttributeName).StoredValue);

                Single valuef = 0;
                switch (CalculationStyle)
                {
                    case SetAttributeFromHerdType.Mean:
                        valuef = Convert.ToSingle(inds.Sum() / inds.Count());
                        break;
                    case SetAttributeFromHerdType.Median:
                        int cnt = inds.Count();
                        if (cnt % 2 == 0)
                            valuef = Convert.ToSingle(inds.Skip(((cnt + 1) / 2) - 1).Take(1));
                        else
                            valuef = Convert.ToSingle((inds.Skip((cnt / 2) - 1).First() + inds.Skip(cnt / 2).First()) / 2);
                        break;
                    case SetAttributeFromHerdType.Minimum:
                        valuef = Convert.ToSingle(inds.Min());
                        break;
                    case SetAttributeFromHerdType.Maximum:
                        valuef = Convert.ToSingle(inds.Max());
                        break;
                    case SetAttributeFromHerdType.Random:
                        valuef = Convert.ToSingle(inds.Take(RandomNumberGenerator.Generator.Next(0, inds.Count() - 1)));
                        break;
                    default:
                        break;
                }

                lastInstance = new IndividualAttribute()
                {
                    InheritanceStyle = InheritanceStyle,
                    StoredValue = valuef * Multiplier
                };
            }
            return lastInstance;
        }
    }

    /// <summary>
    /// Options for determining attribute value from herd
    /// </summary>
    public enum SetAttributeFromHerdType
    {
        /// <summary>
        /// Use herd mean
        /// </summary>
        Mean,
        /// <summary>
        /// Use herd median
        /// </summary>
        Median,
        /// <summary>
        /// Use herd maximum
        /// </summary>
        Minimum,
        /// <summary>
        /// Use herd minimum
        /// </summary>
        Maximum,
        /// <summary>
        /// Use random individual
        /// </summary>
        Random
    }
}
