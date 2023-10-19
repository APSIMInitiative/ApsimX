using System;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.PMF.Interfaces;
using Newtonsoft.Json;

namespace Models.PMF
{
    /// <summary>
    /// Biomass of plant organs
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(IOrgan))]
    [ValidParent(ParentType = typeof(Plant))]
    [ValidParent(ParentType = typeof(IOrgan))]
    public class Biomass : Model, IBiomass
    {
        /// <summary>The structural wt</summary>
        protected double _StructuralWt = 0;
        /// <summary>The non structural wt</summary>
        protected double _StorageWt = 0;
        /// <summary>The structural n</summary>
        protected double _StructuralN = 0;
        /// <summary>The non structural n</summary>
        protected double _StorageN = 0;
        /// <summary>The metabolic wt</summary>
        protected double _MetabolicWt = 0;
        /// <summary>The metabolic n</summary>
        protected double _MetabolicN = 0;


        /// <summary>Gets or sets the non structural n.</summary>
        /// <value>The non structural n.</value>
        [JsonIgnore]
        [Units("g/m^2")]
        virtual public double StorageN
        {
            get { return _StorageN; }
            set
            {
                if (value < 0)
                    throw new Exception("Trying to StorageN to a negative value");
                _StorageN = MathUtilities.RoundToZero(value);
            }
        }

        /// <summary>Gets or sets the structural n.</summary>
        /// <value>The structural n.</value>
        [JsonIgnore]
        [Units("g/m^2")]
        virtual public double StructuralN
        {
            get { return _StructuralN; }
            set
            {
                if (value < 0)
                    throw new Exception("Trying to StructuralN to a negative value");
                _StructuralN = MathUtilities.RoundToZero(value);
            }
        }

        /// <summary>Gets or sets the non structural wt.</summary>
        /// <value>The non structural wt.</value>
        [JsonIgnore]
        [Units("g/m^2")]
        virtual public double StorageWt
        {
            get { return _StorageWt; }
            set
            {
                if (value < 0)
                    throw new Exception("Trying to StorageWt to a negative value");
                _StorageWt = MathUtilities.RoundToZero(value);
            }
        }

        /// <summary>Gets or sets the structural wt.</summary>
        /// <value>The structural wt.</value>
        [JsonIgnore]
        [Units("g/m^2")]
        virtual public double StructuralWt
        {
            get { return _StructuralWt; }
            set
            {
                if (value < 0)
                    throw new Exception("Trying to StructuralWt to a negative value");
                _StructuralWt = MathUtilities.RoundToZero(value);
            }
        }

        /// <summary>Gets or sets the metabolic wt.</summary>
        /// <value>The metabolic wt.</value>
        [JsonIgnore]
        [Units("g/m^2")]
        virtual public double MetabolicWt
        {
            get { return _MetabolicWt; }
            set
            {
                if (value < 0)
                    throw new Exception("Trying to Metabolic to a negative value");
                _MetabolicWt = MathUtilities.RoundToZero(value);
            }
        }

        /// <summary>Gets or sets the metabolic n.</summary>
        /// <value>The metabolic n.</value>
        [JsonIgnore]
        [Units("g/m^2")]
        virtual public double MetabolicN
        {
            get { return _MetabolicN; }
            set
            {
                if (value < 0)
                    throw new Exception("Trying to MetabolicN to a negative value");
                _MetabolicN = MathUtilities.RoundToZero(value);
            }
        }

        /// <summary>Gets the wt.</summary>
        /// <value>The wt.</value>
        [JsonIgnore]
        [Units("g/m^2")]
        virtual public double Wt
        {
            get
            {
                return StructuralWt + StorageWt + MetabolicWt;
            }
        }

        /// <summary>Gets the N amount.</summary>
        /// <value>The n.</value>
        [JsonIgnore]
        [Units("g/m^2")]
        virtual public double N
        {
            get
            {
                return StructuralN + StorageN + MetabolicN;
            }
        }

        /// <summary>Gets the N concentration.</summary>
        /// <value>The n conc.</value>
        [JsonIgnore]
        [Units("g/g")]
        public double NConc
        {
            get
            {
                if (Wt > 0)
                    return N / Wt;
                else
                    return 0.0;
            }
        }

        /// <summary>Gets the structural N concentration.</summary>
        /// <value>The structural n conc.</value>
        [Units("g/g")]
        public double StructuralNConc
        {
            get
            {
                if (StructuralWt > 0)
                    return StructuralN / StructuralWt;
                else
                    return 0.0;
            }
        }

        /// <summary>Gets the non structural N concentration.</summary>
        /// <value>The non structural n conc.</value>
        [Units("g/g")]
        public double StorageNConc
        {
            get
            {
                if (StorageWt > 0)
                    return StorageN / StorageWt;
                else
                    return 0.0;
            }
        }

        /// <summary>Gets the metabolic N concentration.</summary>
        /// <value>The metabolic n conc.</value>
        [Units("g/g")]
        public double MetabolicNConc
        {
            get
            {
                if (MetabolicWt > 0)
                    return MetabolicN / MetabolicWt;
                else
                    return 0.0;
            }
        }

        /// <summary>Initializes a new instance of the <see cref="Biomass"/> class.</summary>
        public Biomass() { }

        /// <summary>Initializes a new instance of the <see cref="Biomass"/> class.</summary>
        /// <param name="from">From.</param>
        public Biomass(Biomass from)
        {
            StructuralWt = from.StructuralWt;
            StorageWt = from.StorageWt;
            MetabolicWt = from.MetabolicWt;
            StructuralN = from.StructuralN;
            StorageN = from.StorageN;
            MetabolicN = from.MetabolicN;
        }

        /// <summary>Clears this instance.</summary>
        virtual public void Clear()
        {
            StructuralWt = 0.0;
            StorageWt = 0.0;
            MetabolicWt = 0.0;
            StructuralN = 0.0;
            StorageN = 0.0;
            MetabolicN = 0.0;
        }
        /// <summary>Adds the specified a.</summary>
        /// <param name="a">a.</param>
        public void Add(Biomass a)
        {
            StructuralWt += a._StructuralWt;
            StorageWt += a._StorageWt;
            MetabolicWt += a._MetabolicWt;
            StructuralN += a._StructuralN;
            StorageN += a._StorageN;
            MetabolicN += a._MetabolicN;
        }
        /// <summary>Subtracts the specified a.</summary>
        /// <param name="a">a.</param>
        public void Subtract(Biomass a)
        {
            StructuralWt -= a._StructuralWt;
            StorageWt -= a._StorageWt;
            MetabolicWt -= a._MetabolicWt;
            StructuralN -= a._StructuralN;
            StorageN -= a._StorageN;
            MetabolicN -= a._MetabolicN;
        }
        /// <summary>Multiplies a biomass object by a given scalar</summary>
        /// <param name="scalar">a.</param>
        public void Multiply(double scalar)
        {
            StructuralWt *= scalar;
            StorageWt *= scalar;
            MetabolicWt *= scalar;
            StructuralN *= scalar;
            StorageN *= scalar;
            MetabolicN *= scalar;
        }
        /// <summary>Sets to.</summary>
        /// <param name="a">a.</param>
        public void SetTo(Biomass a)
        {
            StructuralWt = a.StructuralWt;
            StorageWt = a.StorageWt;
            MetabolicWt = a.MetabolicWt;
            StructuralN = a.StructuralN;
            StorageN = a.StorageN;
            MetabolicN = a.MetabolicN;
        }
        /// <summary>Implements the operator +.</summary>
        /// <param name="a">a.</param>
        /// <param name="b">The b.</param>
        /// <returns>The result of the operator.</returns>
        public static Biomass operator +(Biomass a, Biomass b)
        {
            return new Biomass
            {
                StructuralWt = a.StructuralWt + b.StructuralWt,
                StorageWt = a.StorageWt + b.StorageWt,
                MetabolicWt = a.MetabolicWt + b.MetabolicWt,
                StructuralN = a.StructuralN + b.StructuralN,
                StorageN = a.StorageN + b.StorageN,
                MetabolicN = a.MetabolicN + b.MetabolicN,
            };

        }
        /// <summary>Implements the operator -.</summary>
        /// <param name="a">a.</param>
        /// <param name="b">The b.</param>
        /// <returns>The result of the operator.</returns>
        public static Biomass operator -(Biomass a, Biomass b)
        {
            return new Biomass
            {
                StructuralWt = a.StructuralWt - b.StructuralWt,
                StorageWt = a.StorageWt - b.StorageWt,
                MetabolicWt = a.MetabolicWt - b.MetabolicWt,
                StructuralN = a.StructuralN - b.StructuralN,
                StorageN = a.StorageN - b.StorageN,
                MetabolicN = a.MetabolicN - b.MetabolicN,
            };
        }
        /// <summary>Implements the operator *.</summary>
        /// <param name="a">a.</param>
        /// <param name="Fraction">The fraction.</param>
        /// <returns>The result of the operator.</returns>
        public static Biomass operator *(Biomass a, double Fraction)
        {
            return new Biomass
            {
                StructuralWt = a.StructuralWt * Fraction,
                StorageWt = a.StorageWt * Fraction,
                MetabolicWt = a.MetabolicWt * Fraction,
                StructuralN = a.StructuralN * Fraction,
                StorageN = a.StorageN * Fraction,
                MetabolicN = a.MetabolicN * Fraction,
            };
        }
    }
}