using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Core;

namespace Models.Soils
{
    public class TillageType : Model
    {
        public double f_incorp { get; set; }
        public double tillage_depth { get; set; }
        public int cn_red { get; set; }
        public int cn_rain { get; set; }
    }
}
