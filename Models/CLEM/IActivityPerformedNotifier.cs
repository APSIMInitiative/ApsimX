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
        /// <summary>
        /// Activity performed event handler
        /// </summary>
        event EventHandler ActivityPerformed;
    }
}
