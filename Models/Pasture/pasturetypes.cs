
using Models.Core;
using System;
using System.IO.Pipes;
using static Models.GrazPlan.GrazType;

namespace Models.GrazPlan
{
    [Serializable]
    public class WaterInfo
    {
        public string Name;
        public string PlantType;
        [Units("kg/m^2")]
        public double Demand;
        public WaterLayer[] Layer;
    }

    [Serializable] 
    public class WaterLayer 
    {
        [Units("mm")]
        public double Thickness;
        
        [Units("kg/m^2")]
        public double MaxSupply;

        [Units("mm/mm^3")]
        public double RLD;
        
        [Units("mm")]
        public double Radius;
    }

    [Serializable]
    public class Canopy
    {
        public string Name;
        public string PlantType;
        public CanopyLayer[] Layer;
    }

    [Serializable]
    public class CanopyLayer
    {
        /// <summary>m</summary>
        [Units("m")]
        public double Thickness;

        /// <summary>m^2/m^2</summary>
        [Units("m^2/m^2")]
        public double AreaIndex;
        
        /// <summary>-</summary>
        public double CoverGreen;
        
        /// <summary>-</summary>
        public double CoverTotal;
    }

    [Serializable]
    public class Leachate
    {
        public double N;    // kg/ha
        public double P;    // kg/ha
        public double S;    // kg/ha
    }
    

    [Serializable]
    public class OrganicMatter
    {
        /// <summary>kg/ha</summary>
        public double Weight;
        /// <summary>kg/ha</summary>
        public double N;
        /// <summary>kg/ha</summary>
        public double P;
        /// <summary>kg/ha</summary>
        public double S;
        /// <summary>mol/ha</summary>
        public double AshAlk;
    }

    [Serializable]
    public class Residue : OrganicMatter
    {
        public void CopyFrom(DM_Pool Pool)
        {
                Weight = Pool.DM;                    // kg/ha            
                N = Pool.Nu[(int)TPlantElement.N];   // kg/ha            
                P = Pool.Nu[(int)TPlantElement.P];   // kg/ha            
                S = Pool.Nu[(int)TPlantElement.S];   // kg/ha            
                AshAlk = Pool.AshAlk;                // mol/ha           
        }
    }

    [Serializable]
    public class Allocation
    {
        public double Leaf;
        public double Stem;
        public double Root;
        public double Seed;
    }
    

    [Serializable]
    public class HerbageProfile
    {
        /// <summary>mm</summary>
        public double[] bottom;
        /// <summary>mm</summary>
        public double[] top;
        /// <summary>kg/ha</summary>
        public double[] leaf;
        /// <summary>kg/ha</summary>
        public double[] stem;
        /// <summary>kg/ha</summary>
        public double[] head;

        public HerbageProfile(int items)
        {
            bottom = new double[items];
            top = new double[items];
            leaf = new double[items];
            stem = new double[items];
            head = new double[items];
        }
    }

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

        public Herbage(int count, TPlantElement[] elements) 
        {
            dmd = new double[count];
            weight = new double[count];
            spec_area = new double[count];
            for (int i = 0; i < count; i++)
            {
                if (elements[i] == TPlantElement.N)
                    n_conc = new double[count];
                if (elements[i] == TPlantElement.P)
                    p_conc = new double[count];
                if (elements[i] == TPlantElement.S)
                    s_conc = new double[count];
            }
        }
    }

    [Serializable]
    public class GreenInit
    {
        public string status;
        public Herbage[] herbage;
        public double[][] root_wt;  // kg/ha, indexed [0][0]
        public double rt_dep;       // mm
        public double estab_index = 1.0;
        public double stress_index = 0.0;
        public double stem_reloc = -999.0;   // kg/ha
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
        public double amount;       // kg/m^2 (water), fract (roots propn), kg/ha (nutr uptake)
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

