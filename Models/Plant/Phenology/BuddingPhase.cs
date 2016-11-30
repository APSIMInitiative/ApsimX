using System;
using System.Collections.Generic;
using Models.Core;
using Models.PMF.Functions;
using System.IO;
using System.Xml.Serialization;

namespace Models.PMF.Phen
{
    /// <summary>
    /// has all the functionality of generic phase,
    /// but used to set the emerging date of pereniel crops
    /// 
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class BuddingPhase : GenericPhase
    {
    }
}
      
      
