using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM
{
    /// <summary>
    /// Interface for a pasture manager
    /// Can be CropActivityManageCrop or PastureActivityManage
    /// </summary>
    public interface IPastureManager
    {
        /// <summary>
        /// The area currently assigned to the managed pasture
        /// </summary>
        double Area { get; set; }

        /// <summary>
        /// The model name
        /// </summary>
        string Name { get; set; }
    }
}
