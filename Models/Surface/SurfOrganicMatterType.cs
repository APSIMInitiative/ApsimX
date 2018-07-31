namespace Models.Surface
{
    using System;

    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class SurfOrganicMatterType
    {
        /// <summary>The name</summary>
        public string name;
        /// <summary>The organic matter type</summary>
        public string OrganicMatterType;
        /// <summary>The pot decomp rate</summary>
        public double PotDecompRate;
        /// <summary>The no3</summary>
        public double no3;
        /// <summary>The NH4</summary>
        public double nh4;
        /// <summary>The po4</summary>
        public double po4;
        /// <summary>The standing</summary>
        public OMFractionType[] Standing;
        /// <summary>The lying</summary>
        public OMFractionType[] Lying;

        /// <summary>Initializes a new instance of the <see cref="SurfOrganicMatterType"/> class.</summary>
        public SurfOrganicMatterType()
        {
            name = null;
            OrganicMatterType = null;
            PotDecompRate = 0;
            no3 = 0;
            nh4 = 0;
            po4 = 0;
            Standing = new OMFractionType[SurfaceOrganicMatter.maxFr];
            Lying = new OMFractionType[SurfaceOrganicMatter.maxFr];

            for (int i = 0; i < SurfaceOrganicMatter.maxFr; i++)
            {
                Lying[i] = new OMFractionType();
                Standing[i] = new OMFractionType();
            }
        }

        /// <summary>Initializes a new instance of the <see cref="SurfOrganicMatterType"/> class.</summary>
        /// <param name="name">The name.</param>
        /// <param name="type">The type.</param>
        public SurfOrganicMatterType(string name, string type)
            : this()
        {
            this.name = name;
            OrganicMatterType = type;
        }
    }

}
