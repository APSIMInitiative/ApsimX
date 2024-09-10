using System;
using Models.Core;
using Models.ForageDigestibility;
using Models.Soils;
using Models.Surface;

namespace Models.GrazPlan
{

    /// <summary>
    /// Paddock details
    /// </summary>
    [Serializable]
    public class PaddockInfo
    {
        /// <summary>
        /// Create the PaddockInfo
        /// </summary>
        /// <param name="zone">Optional zone.</param>
        public PaddockInfo(Zone zone = null)
        {
            this.Forages = new ForageList(false);
            this.SuppInPadd = new SupplementRation();

            // locate surfaceOM and soil nutrient model
            if (zone != null)
            {
                AddFaecesObj = (SurfaceOrganicMatter)zone.FindInScope<SurfaceOrganicMatter>();
                ForagesModel = (Forages)zone.FindInScope<Forages>();
                var soilPhysical = zone.FindInScope<IPhysical>();
                SoilLayerThickness = soilPhysical.Thickness;
                AddUrineObj = (ISolute)zone.FindInScope("Urea");
            }
        }

        /// <summary>
        /// Gets or sets the summed green mass
        /// </summary>
        [Units("kg/ha")]
        public double SummedGreenMass { get; set; }

        /// <summary>
        /// Gets or sets the paddock name
        /// </summary>
        [Units("-")]
        public string Name { get { if (zone == null) return string.Empty; else return zone.Name; } }

        /// <summary>
        /// Gets or sets the paddock object
        /// </summary>
        [NonSerialized]
        public Zone zone;

        /// <summary>
        /// Gets or sets the faeces destination
        /// </summary>
        [NonSerialized]
        public SurfaceOrganicMatter AddFaecesObj;

        
        /// <summary>
        /// Gets or sets the faeces destination
        /// </summary>
        [NonSerialized]
        public Forages ForagesModel;

        /// <summary>
        /// Gets or sets the urine destination
        /// </summary>
        [NonSerialized]
        public ISolute AddUrineObj;

        /// <summary>The soil layer thickness</summary>
        [Units("mm")]
        public double[] SoilLayerThickness { get; set; }

        /// <summary>
        /// Gets or sets the paddock area (ha)
        /// </summary>
        [Units("ha")]
        public double Area
        {
            get
            {
                if (zone == null)
                    return 1;
                else
                    return zone.Area;
            }
        }

        /// <summary>
        /// Gets or sets the waterlogging index (0-1)
        /// </summary>
        [Units("0-1")]
        public double Waterlog { get; set; }

        /// <summary>
        /// Gets or sets the total pot. intake
        /// </summary>
        [Units("kg")]
        public double SummedPotIntake { get; set; }

        /// <summary>
        /// Gets or sets the supplement removal amount
        /// </summary>
        [Units("kg")]
        public double SuppRemovalKG { get; set; }

        /// <summary>
        /// Gets or sets the paddock slope
        /// </summary>
        [Units("deg")]
        public double Slope
        {
            get
            {
                if (zone == null)
                    return 0;
                else
                    return zone.Slope;
            }
        }

        /// <summary>
        /// Gets the steepness code (1-2)
        /// </summary>
        [Units("1-2")]
        public double Steepness
        {
            get { return 1.0 + Math.Min(1.0, Math.Sqrt(Math.Sin(Slope * Math.PI / 180) / Math.Cos(Slope * Math.PI / 180))); }
        }

        /// <summary>
        /// Gets the supplement that is in the paddock
        /// </summary>
        public SupplementRation SuppInPadd { get; }

        /// <summary>
        /// Gets the forage list
        /// </summary>
        public ForageList Forages { get; }

        /// <summary>
        /// Gets a value indicating whether feeding the supplement first. Bail feeding.
        /// </summary>
        public bool FeedSuppFirst { get; private set; } = false;

        /// <summary>
        /// Assign a forage to this paddock
        /// </summary>
        /// <param name="forage">The forage object to assign to this paddock</param>
        public void AssignForage(ForageInfo forage)
        {
            this.Forages.Add(forage);
            forage.InPaddock = this;
        }

        /// <summary>
        /// Aggregates the initial forage availability of each species in the list       
        /// * If FForages.Count=0, then the aggregate forage availability is taken to    
        ///   have been passed at the paddock level using setGrazingInputs()             
        /// </summary>
        public void ComputeTotals()
        {
            this.SummedGreenMass = 0.0;
            for (int jdx = 0; jdx <= this.Forages.Count() - 1; jdx++)
            {
                this.Forages.ByIndex(jdx).SummariseInitHerbage();
                this.SummedGreenMass = this.SummedGreenMass + this.Forages.ByIndex(jdx).GreenMass;
            }
        }

        /// <summary>
        /// Zero the removal amounts
        /// </summary>
        public void ZeroRemoval()
        {
            this.SuppRemovalKG = 0.0;
            for (int jdx = 0; jdx <= this.Forages.Count() - 1; jdx++)
                this.Forages.ByIndex(jdx).RemovalKG = new GrazType.GrazingOutputs();
        }

        /// <summary>
        /// Feed the supplement
        /// </summary>
        /// <param name="newAmount">The amount to feed in kg</param>
        /// <param name="newSupp">The supplement to feed</param>
        /// <param name="feedSuppFirst">True if bail feeding</param>
        public void FeedSupplement(double newAmount, FoodSupplement newSupp, bool feedSuppFirst)
        {
            bool found;
            int idx;

            this.FeedSuppFirst = false;
            if (newAmount > 0.0)
            {
                this.FeedSuppFirst = feedSuppFirst;
                idx = 0;
                found = false;
                while (!found && (idx < this.SuppInPadd.Count))
                {
                    found = newSupp.IsSameAs(this.SuppInPadd[idx]);
                    if (!found)
                        idx++;
                }
                if (found)
                    this.SuppInPadd[idx].Amount = this.SuppInPadd[idx].Amount + newAmount;
                else
                {
                    FoodSupplement oneSupp = new FoodSupplement();
                    oneSupp.Assign(newSupp);
                    this.SuppInPadd.Add(oneSupp, newAmount);
                }
            }
        }

        /// <summary>
        /// Clear the supplement ration that is in the paddock
        /// </summary>
        public void ClearSupplement()
        {
            this.SuppInPadd.Clear();
        }
    }
}
