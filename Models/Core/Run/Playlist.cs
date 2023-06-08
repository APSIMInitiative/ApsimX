using System;
using Models.Core;

namespace Models
{

    /// <summary>
    /// A report class for writing output to the data store.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GenericView")]
    [PresenterName("UserInterface.Presenters.GenericPresenter")]
    [ValidParent(ParentType = typeof(Simulations))]
    public class Playlist : Model
    {
        //// <summary>Link to simulations</summary>
        //[Link]
        //private Simulations simulations = null;
    }
}