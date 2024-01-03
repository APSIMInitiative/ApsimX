
using Models.Core;
using System;
using static Models.GrazPlan.GrazType;

namespace Models.GrazPlan
{
#pragma warning disable CS1591
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

    /// <summary>
    /// Specifies initial state of above-ground herbage pools
    /// </summary>
    [Serializable]
    public class Herbage
    {
        /// <summary>Class boundaries for the dry matter digestibility (DMD) classes to which the other fields refer. kg/kg</summary>
        public double[] dmd;
        /// <summary>kg/ha</summary>
        public double[] weight;
        /// <summary>Nitrogen concentration of each DMD class. Only meaningful if nutrients includes N. kg/kg</summary>
        public double[] n_conc;
        /// <summary>Phosphorus concentration of each DMD class. Only meaningful if nutrients includes P. kg/kg</summary>
        public double[] p_conc;
        /// <summary>Sulphur concentration of each DMD class. Only meaningful if nutrients includes S. kg/kg</summary>
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

    /// <summary>
    /// Specifies the state of a cohort of green (living) herbage
    /// </summary>
    [Serializable]
    public class GreenInit
    {
        /// <summary>
        /// Feasible values are “seedling”, “established” or “senescing”.
        /// </summary>
        public string status;

        /// <summary>
        /// Specifies initial state of above-ground herbage pools
        /// </summary>
        public Herbage[] herbage;

        /// <summary>
        /// Mass of roots. The first index denotes a root age class (0=effective roots, 1=old roots);
        /// if only one sub-array is given, it is taken to be total root mass.
        /// The second index denotes a soil layer (defined by the layers property).
        /// If only a single value is given in a sub-array, mass will be distributed over all soil layers to the current rooting depth, using a near-exponential distribution.
        /// </summary>
        public double[][] root_wt;  // kg/ha, indexed [0][0]

        /// <summary>
        /// Current rooting depth of the cohort
        /// </summary>
        public double rt_dep;       // mm

        /// <summary>
        /// Establishment index. 0,1.0-KZ1. Only meaningful if status = “seedling”.
        /// </summary>
        public double estab_index = 1.0;

        /// <summary>
        /// Stress index. 0-1. Only meaningful if status = “seedling”.
        /// </summary>
        public double stress_index = 0.0;

        /// <summary>
        /// Maximum amount of stem tissue to be relocated to seed.
        /// Only meaningful if
        /// (a) the species is modelled as having seeds,
        /// (b) status = “established” or “senescing” and
        /// (c) the phenological stage is reproductive or senescent.
        /// Default depends on the above conditions.
        /// </summary>
        public double stem_reloc = -999.0;   // kg/ha

        /// <summary>
        /// Number of frosts experienced by this herbage cohort during its lifetime
        /// </summary>
        public int frosts;
    }

    /// <summary>
    /// Specifies the state of a cohort of dry herbage (standing dead or litter):
    /// </summary>
    [Serializable]
    public class DryInit
    {
        /// <summary>
        /// Feasible values are “dead” or “litter”.
        /// </summary>
        public string status;

        /// <summary>
        /// Definition is the same as GreenInit.herbage
        /// </summary>
        public Herbage[] herbage;
    }

    /// <summary>
    /// Mass of seeds in each soil layer
    /// </summary>
    [Serializable]
    public class SeedInit
    {
        /// <summary>
        /// Mass of soft, unripe seeds. If only a single element is given, all seeds are placed in the first soil layer
        /// </summary>
        public double[] soft_unripe;    // kg/ha

        /// <summary>
        /// Mass of soft, ripe seeds. If only a single element is given, all seeds are placed in the first soil layer
        /// </summary>
        public double[] soft_ripe;      // kg/ha

        /// <summary>
        /// Mass of hard, unripe seeds. If only a single element is given, all seeds are placed in the first soil layer
        /// </summary>
        public double[] hard_unripe;    // kg/ha

        /// <summary>
        /// Mass of hard, ripe seeds. If only a single element is given, all seeds are placed in the first soil layer
        /// </summary>
        public double[] hard_ripe;      // kg/ha

        /// <summary>
        /// Default constructor
        /// </summary>
        public SeedInit()
        {
        }

        /// <summary>
        /// Construct a seed layer object
        /// </summary>
        /// <param name="layers"></param>
        public SeedInit(int layers)
        {
            soft_unripe = new double[layers];
            soft_ripe = new double[layers];
            hard_unripe = new double[layers];
            hard_ripe = new double[layers];
        }
    }

    /// <summary>
    /// Light interception profiles of plant populations
    /// </summary>
    [Serializable]
    public class LightProfile
    {
        /// <summary>
        /// Each plant population
        /// </summary>
        public Population interception = null;
        public double transmission; // MJ/m^2
    }

    public class Population
    {
        public string population;           // apsim component name
        public PopulationItem[] element;    // seedling, established, senescing, dead, litter
    }

    public class PopulationItem
    {
        public string name;                 // "seedling", "established", "senescing", "dead", "litter"
        public Layer[] layer;
    }

    public class Layer
    {
        /// <summary>m</summary>
        public double thickness;
        /// <summary>MJ/m^2</summary>
        public double amount;
        /// <summary>W/m^2</summary>
        public double intensity;
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

    /// <summary>
    /// Removal of herbage and seed
    /// </summary>
    public class PastureRemoval
    {
        /// <summary>
        /// The herbage in each digestibility class. kg/ha
        /// </summary>
        public double[] herbage = new double[GrazType.DigClassNo];

        /// <summary>
        /// The seed in each maturity pool. kg/ha
        /// </summary>
        public double[] seed = new double[GrazType.RIPE];
    }

    /// <summary>
    /// Store the litter that is being removed to SOM
    /// </summary>
    [Serializable]
    public class BiomassRemoved
    {
        /// <summary>
        /// Crop type
        /// </summary>
        public string CropType;

        /// <summary>
        /// Dry matter type
        /// </summary>
        public string[] DMType;

        /// <summary>
        /// kg/ha
        /// </summary>
        public double[] dltCropDM;

        /// <summary>
        /// kg/ha
        /// </summary>
        public double[] dltDM_N;

        /// <summary>
        /// kg/ha
        /// </summary>
        public double[] dltDM_P;

        /// <summary>
        /// 0-1
        /// </summary>
        public double[] FractionToResidue;

        public BiomassRemoved(int elements)
        {
            DMType = new string[elements];
            dltCropDM = new double[elements];
            dltDM_N = new double[elements];
            dltDM_P = new double[elements];
            FractionToResidue = new double[elements];
        }
    }

    [Serializable]
    public class PastureFOMType
    {
        public string[] FOMTypes;
        public double[] Layers; //mm
        public OrganicMatter[][] FOM; // [layer][root,leaf,stem]
    }

#pragma warning restore CS1591
}

