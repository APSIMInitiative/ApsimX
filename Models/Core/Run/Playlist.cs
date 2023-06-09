using System;
using Models.Core;

namespace Models
{

    /// <summary>
    /// A report class for writing output to the data store.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PlaylistView")]
    [PresenterName("UserInterface.Presenters.PlaylistPresenter")]
    [ValidParent(ParentType = typeof(Simulations))]
    public class Playlist : Model
    {
        //// <summary>Link to simulations</summary>
        //[Link]
        //private Simulations simulations = null;

        /// <summary>Gets or sets the memo text.</summary>
        [Description("Text of the playlist")]
        public string Text { get; set; }
    }
}