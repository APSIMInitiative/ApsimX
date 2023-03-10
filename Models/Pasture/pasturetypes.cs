
namespace Models.GrazPlan
{
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

    public class DryInit
    {
        public string status;
        public Herbage[] herbage;
    }

    public class SeedInit
    {
        public double[] soft_unripe;    // kg/ha
        public double[] soft_ripe;      // kg/ha
        public double[] hard_unripe;    // kg/ha
        public double[] hard_ripe;      // kg/ha
    }
    
}
