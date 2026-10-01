namespace UserInterface.Presenters
{
    /// <summary>
    /// Interface for a presenter that can be used within another presenter
    /// </summary>
    public interface ISubPresenter
    {
        /// <summary>Connect all widget events.</summary>
        public void ConnectEvents();

        /// <summary>Disconnect all widget events.</summary>
        public void DisconnectEvents();

        /// <summary>Updates the view and sub</summary>
        public void Refresh();
    }
}
