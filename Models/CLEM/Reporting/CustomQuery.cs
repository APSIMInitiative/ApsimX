using Models.Core;
using Models.Core.Attributes;
using System;

namespace Models.CLEM.Reporting
{    
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    [ViewName("ApsimNG.Views.CLEM.CustomQueryView")]
    [PresenterName("ApsimNG.Presenters.CLEM.CustomQueryPresenter")]
    [ValidParent(ParentType = typeof(Folder))]
    [ValidParent(ParentType = typeof(ZoneCLEM))]
    [Description("Allows custom SQL queries to be applied to the DataStore.")]
    [Version(1, 0, 1, "")]
    public class CustomQuery : Model
    {
        /// <summary>
        /// Raw text of an SQL query
        /// </summary>
        public string Sql { get; set; }

        /// <summary>
        /// Name of the file containing an SQL query
        /// </summary>
        public string Filename { get; set; }
                
    }
}
