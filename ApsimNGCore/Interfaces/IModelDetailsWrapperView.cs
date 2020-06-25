namespace UserInterface.Interfaces
{
    public interface IModelDetailsWrapperView
    {
        /// <summary>
        /// Property to provide access to the model type label.
        /// </summary>
        string ModelTypeText { get; set; }

        /// <summary>
        /// Property to provide access to the model description text label.
        /// </summary>
        string ModelDescriptionText { get; set; }

        /// <summary>
        /// Property to provide access to the model version text label.
        /// </summary>
        string ModelVersionText { get; set; }

        /// <summary>
        /// Property to provide the text color for model type label.
        /// </summary>
        string ModelTypeTextColour { get; set; }

        /// <summary>
        /// Property to provide access to the model help URL.
        /// </summary>
        string ModelHelpURL { get; set; }

        ///// <summary>
        ///// Property to provide access to the lower presenter.
        ///// </summary>
        //IPresenter LowerPresenter { get; }

        /// <summary>
        /// Add a view to the right hand panel.
        /// </summary>
        void AddLowerView(object control);
    }
}
