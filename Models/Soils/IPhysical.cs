namespace Models.Soils
{
    /// <summary>An interface for soil physical properties.</summary>
    public interface IPhysical
    {
        /// <summary>Air dry (mm/mm).</summary>
        double[] AirDry { get; set; }
        
        /// <summary>Bulk density (g/cc).</summary>
        double[] BD { get; set;  }

        /// <summary>Drained upper limit (mm/mm).</summary>
        double[] DUL { get; set;  }

        /// <summary>KS (mm/day).</summary>
        double[] KS { get; set; }
        
        /// <summary>Lower limit 15 bar (mm/mm).</summary>
        double[] LL15 { get; set; }
        
        /// <summary>Particle size clay.</summary>
        double[] ParticleSizeClay { get; set; }

        /// <summary>Particle size sand.</summary>
        double[] ParticleSizeSand { get; set; }

        /// <summary>Particle size silt.</summary>
        double[] ParticleSizeSilt { get; set; }

        /// <summary>Rocks.</summary>
        double[] Rocks { get; }

        /// <summary>Saturation (mm/mm).</summary>
        double[] SAT { get; set; }
        
        /// <summary>Texture.</summary>
        string[] Texture { get; }
        
        /// <summary>The soil thickness (mm).</summary>
        double[] Thickness { get; set; }
    }
}