// ----------------------------------------------------------------------
// <copyright file="OnEventFunction.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models.PMF.Functions
{
    using Models.Core;
    using Models.PMF.Phen;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Returns the a value depending on whether an event has occurred.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class OnEventFunction : BaseFunction, ICustomDocumentation
    {
        /// <summary>The _ value</summary>
        private double[] returnValue = new double[] { 0 };

        /// <summary>The pre event value</summary>
        [ChildLinkByName]
        private IFunction preEventValue = null;

        /// <summary>The post event value</summary>
        [ChildLinkByName]
        private IFunction postEventValue = null;

        /// <summary>The set event</summary>
        [Description("The event that triggers change from pre to post event value")]
        public string SetEvent { get; set; }

        /// <summary>The re set event</summary>
        [Description("(optional) The event resets to pre event value")]
        public string ReSetEvent {get; set;}

        /// <summary>Called when [simulation commencing].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            returnValue[0] = preEventValue.Value();
        }

        /// <summary>Called when [phase changed].</summary>
        /// <param name="phaseChange">The phase change.</param>
        /// <param name="sender">Sender plant.</param>
        [EventSubscribe("PhaseChanged")]
        private void OnPhaseChanged(object sender, PhaseChangedType phaseChange)
        {
            if (phaseChange.EventStageName == SetEvent)
                OnSetEvent();

            if (phaseChange.EventStageName == ReSetEvent)
                OnReSetEvent();
        }

        /// <summary>Called when [re set event].</summary>
        public void OnReSetEvent()
        {
            returnValue[0] = preEventValue.Value();
        }

        /// <summary>Called when [set event].</summary>
        public void OnSetEvent()
        {
            returnValue[0] = postEventValue.Value();
        }

        /// <summary>Gets the value.</summary>
        public override double[] Values()
        {
            return returnValue;
        }

        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        /// <param name="tags">The list of tags to add to.</param>
        /// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
        /// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
        public void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
        {
            if (IncludeInDocumentation)
            {
                // add a heading.
                tags.Add(new AutoDocumentation.Heading(Name, headingLevel));

                // write memos.
                foreach (IModel memo in Apsim.Children(this, typeof(Memo)))
                    AutoDocumentation.DocumentModel(memo, tags, -1, indent);

                if (preEventValue != null)
                {
                    tags.Add(new AutoDocumentation.Paragraph("Before " + SetEvent, indent));
                    AutoDocumentation.DocumentModel(preEventValue as IModel, tags, -1, indent + 1);
                }

                if (postEventValue != null)
                {
                    tags.Add(new AutoDocumentation.Paragraph("On " + SetEvent + " the value is set to:", indent));
                    AutoDocumentation.DocumentModel(postEventValue as IModel, tags, -1, indent + 1);
                }
            }
        }

    }

}