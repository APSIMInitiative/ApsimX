using Models.CLEM.Activities;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Models.CLEM
{
    /// <summary>
    /// Interface for Activities able to report performed event
    /// </summary>
    public interface IActivityPerformedNotifier
    {

        ///// <summary>
        ///// Current status of this activity
        ///// </summary>
        //[XmlIgnore]
        //ActivityStatus Status { get; set; }

        /// <summary>
        /// Activity performed event handler
        /// </summary>
        event EventHandler ActivityPerformed;

        ///// <summary>
        ///// Activity has occurred 
        ///// </summary>
        ///// <param name="e"></param>
        //void OnActivityPerformed(EventArgs e);

        ///// <summary>
        ///// Method to trigger an Activity Performed event 
        ///// </summary>
        //void TriggerOnActivityPerformed();

        ///// <summary>
        ///// Method to trigger an Activity Performed event 
        ///// </summary>
        //void TriggerOnActivityPerformed(ActivityStatus status);
    }
}
