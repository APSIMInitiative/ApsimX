using APSIM.Numerics;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Grazplan;
using Models.Interfaces;
using Models.Soils;
using Models.Soils.Arbitrator;
using Models.Soils.Nutrients;
using Models.Surface;
using Newtonsoft.Json;
using StdUnits;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using static Models.GrazPlan.GrazType;
using static Models.GrazPlan.PastureUtil;
using APSIM.Core;
using Models.PMF;
using Models.PMF.Interfaces;


namespace Models.Grazplan
{
    /// <summary>
    /// TESTING aboveground organs
    /// </summary>
    public class Organs: Model, IOrganDamage
    {
        
        /// <summary>Live biomass.</summary>
        public Biomass Live { get; private set; } = new Biomass();

        /// <summary>Dead biomass</summary>
        public Biomass Dead { get; private set; } = new Biomass();

         /// <summary>Flag indicating whether the biomass is above ground or not.</summary>
        public bool IsAboveGround { get { return true; } }

    }    



}