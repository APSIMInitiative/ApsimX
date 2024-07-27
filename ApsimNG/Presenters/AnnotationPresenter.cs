namespace UserInterface.Presenters
{
    using Models;
    using System;
    using Views;
    using APSIM.Shared.Graphing;

    /// <summary>This presenter lets the set properties of a graph annotation.</summary>
    public class AnnotationPresenter : IPresenter
    {
        /// <summary>The model to add a child model to.</summary>
        private Graph graphModel;

        /// <summary>The drop down control.</summary>
        private DropDownView dropDown;

        /// <summary>The parent explorer presenter</summary>
        private ExplorerPresenter explorerPresenter;

        /// <summary>Attach the specified Model and View.</summary>
        /// <param name="model">The axis model</param>
        /// <param name="view">The axis view</param>
        /// <param name="explorerPresenter">The parent explorer presenter</param>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            graphModel = model as Graph;
            this.explorerPresenter = explorerPresenter;

            dropDown = (view as ViewBase).GetControl<DropDownView>("combobox1");
            dropDown.Values = new string[] { "TopLeft", "TopRight", "BottomLeft", "BottomRight" };
            dropDown.SelectedValue = graphModel.AnnotationLocation.ToString();

            // Trap events from the view.
            dropDown.Changed += OnDropDownChanged;
        }

        /// <summary>Detach the model from the view.</summary>
        public void Detach()
        {
            dropDown.Changed -= OnDropDownChanged;
        }

        private void OnDropDownChanged(object sender, EventArgs e)
        {
            try
            {
                explorerPresenter.CommandHistory.Add(new Commands.ChangeProperty(graphModel, "AnnotationLocation", Enum.Parse(typeof(AnnotationPosition), dropDown.SelectedValue)));
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
            }
        }
    }
}
