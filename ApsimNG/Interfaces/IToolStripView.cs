namespace UserInterface.Interfaces
{
    using System.Collections.Generic;

    /// <summary>
    /// The interface for a toolstrip (button bar)
    /// </summary>
    public interface IToolStripView
    {
        /// <summary>Populate the main menu tool strip.</summary>
        /// <param name="menuDescriptions">Descriptions for each item.</param>
        void Populate(List<MenuDescriptionArgs> menuDescriptions);

        /// <summary>Destroy the toolstrip</summary>
        void Destroy();
    }
}
