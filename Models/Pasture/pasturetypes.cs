
using System;
using System.IO.Pipes;

namespace Models.GrazPlan
{
    [Serializable]
    public class Herbage
    {
        /// <summary>kg/kg</summary>
        public double[] dmd;
        /// <summary>kg/ha</summary>
        public double[] weight;
        /// <summary>kg/kg</summary>
        public double[] n_conc;
        /// <summary>kg/kg</summary>
        public double[] p_conc;
        /// <summary>kg/kg</summary>
        public double[] s_conc;
        /// <summary>cm^2/g</summary>
        public double[] spec_area;
    }

    [Serializable]
    public class GreenInit
    {
        public string status;
        public Herbage[] herbage;
        public double[,] root_wt;   // kg/ha
        public double rt_dep;       // mm
        public double estab_index;
        public double stress_index;
        public double stem_reloc;   // kg/ha
        public int frosts;
    }

    [Serializable]
    public class DryInit
    {
        public string status;
        public Herbage[] herbage;
    }

    [Serializable]
    public class SeedInit
    {
        public double[] soft_unripe;    // kg/ha
        public double[] soft_ripe;      // kg/ha
        public double[] hard_unripe;    // kg/ha
        public double[] hard_ripe;      // kg/ha
    }

    /// <summary>
    /// Light interception profiles of plant populations - obtained from the resource allocator (paddock)
    /// </summary>
    [Serializable]
    public class LightProfile
    {
        public Population[] interception;
        public double transmission; // MJ/m^2
    }

    public class Population
    {
        public string population;
        public PopulationItem[] element; 
    }

    public class PopulationItem
    {
        public string name;
        public Layer[] layer;
    }

    public class Layer
    {
        public double thickness;    // m
        public double amount;       // MJ/m^2
        public double intensity;    // W/m^2
    }

    /// <summary>
    /// Water uptake by plant populations from the allocator (paddock)
    /// </summary>
    [Serializable]
    public class WaterUptake
    {
        public string population;
        public WaterPopItem[] element;
    }

    public class WaterPopItem
    {
        public string name;
        public SoilLayer[] layer;
    }

    public class SoilLayer
    {
        public double thickness;    // mm
        public double amount;       // kg/m^2 (water), fract (roots propn)
    }

    /// <summary>
    /// Proportion of the soil volume occupied by roots of plant populations
    /// </summary>
    [Serializable]
    public class SoilFract
    {
        public string population;
        public SoilPopItem[] element;
    }

    public class SoilPopItem
    {
        public string name;
        public SoilLayer[] layer;
    }

    /// <summary>
    /// Soil nutrient availability 
    /// </summary>
    [Serializable]
    public class NutrAvail
    {
        public double[] layers; // mm
        public Nutrient[] nutrient;
    }

    public class Nutrient
    {
        public double area_fract;
        public double[] soiln_conc; // mg/l
        public double[] avail_nutr; // kg/ha
    }

}

