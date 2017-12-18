using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Models.CLEM.Groupings
{
    ///<summary>
    /// Contains a group of filters to identify individul ruminants
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [Description("This labour filter group selects specific individuals from the labour pool using any number of Labour Filters. Multiple filters will select groups of individuals required.")]
    public class LabourFilterGroup: CLEMModel
    {

    }
}
