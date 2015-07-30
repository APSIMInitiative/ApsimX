using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using Models.PMF.Phen;

namespace Models.PMF.Functions
{
    /// <summary>
    /// Returns the a value depending on whether an event has occurred.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class OnEventFunction : Model, IFunction
    {
        /// <summary>The _ value</summary>
        private double _Value = 0;

        /// <summary>The set event</summary>
        public string SetEvent { get; set; }

        /// <summary>The re set event</summary>
        public string ReSetEvent {get; set;}


        /// <summary>The pre event value</summary>
        [Link]
        IFunction PreEventValue = null;
        /// <summary>The post event value</summary>
        [Link]
        IFunction PostEventValue = null;

        /// <summary>Called when [simulation commencing].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            _Value = PreEventValue.Value;
        }

        /// <summary>Called when [phase changed].</summary>
        /// <param name="PhaseChange">The phase change.</param>
        [EventSubscribe("PhaseChanged")]
        private void OnPhaseChanged(PhaseChangedType PhaseChange)
        {
            if (PhaseChange.EventStageName == SetEvent)
                OnSetEvent();

            if (PhaseChange.EventStageName == ReSetEvent)
                OnReSetEvent();
        }

        /// <summary>Called when [re set event].</summary>
        public void OnReSetEvent()
        {
            _Value = PreEventValue.Value;
        }

        /// <summary>Called when [set event].</summary>
        public void OnSetEvent()
        {
            _Value = PostEventValue.Value;
        }

        /// <summary>Gets the value.</summary>
        public double Value
        {
            get
            {
                return _Value;
            }
        }

        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        /// <param name="tags">The list of tags to add to.</param>
        /// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
        /// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
        public override void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
        {
            // add a heading.
            tags.Add(new AutoDocumentation.Heading(Name, headingLevel));

            // write memos.
            foreach (IModel memo in Apsim.Children(this, typeof(Memo)))
                memo.Document(tags, -1, indent);

            tags.Add(new AutoDocumentation.Paragraph("Before " + SetEvent, indent));
            (PreEventValue as IModel).Document(tags, -1, indent + 1);

            tags.Add(new AutoDocumentation.Paragraph("After " + SetEvent, indent));
            (PostEventValue as IModel).Document(tags, -1, indent + 1);
        }

    }

}