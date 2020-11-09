namespace UserInterface.Presenters
{
    /// <summary>
    /// Interface for a presenter
    /// </summary>
    public interface IPresenter
    {
        /// <summary>
        /// Attach the objects to this presenter
        /// </summary>
        /// <param name="model">The model</param>
        /// <param name="view">The view</param>
        /// <param name="explorerPresenter">The explorer</param>
        void Attach(object model, object view, ExplorerPresenter explorerPresenter);

        /// <summary>
        /// Detach the objects
        /// </summary>
        void Detach();
    }
}
