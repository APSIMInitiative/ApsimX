using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Models.Core;

namespace Models
{
    /// <summary>
    /// A simple agroforestry model
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.ForestryView")]
    [PresenterName("UserInterface.Presenters.ForestryPresenter")]
    public class Forestry : Model
    {
        /// <summary>Gets or sets the ft.</summary>
        /// <value>The ft.</value>
        [Summary]
        [Description("BD")]
        public List<List<string>> Table { get; set; }
    }
}
