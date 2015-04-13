using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using System.Xml.Serialization;
using APSIM.Shared.Utilities;

namespace Models.PMF
{
    /*!
    <summary>
    The biomass of plant organs.
    </summary>
    \retval Wt The total biomass weight (g m<sup>-2</sup>)
    \retval NonStructuralWt The biomass weight of non-structural component (g m<sup>-2</sup>)
    \retval StructuralWt The biomass weight of structural component (g m<sup>-2</sup>)
    \retval MetabolicWt The biomass weight of metabolic component (g m<sup>-2</sup>)
    \retval N The total nitrogen weight (g m<sup>-2</sup>)
    \retval NonStructuralN The nitrogen weight of non-structural component (g m<sup>-2</sup>)
    \retval StructuralN The nitrogen weight of structural component (g m<sup>-2</sup>)
    \retval MetabolicN The nitrogen weight of metabolic component (g m<sup>-2</sup>)
    \retval NConc The total nitrogen concentration (g g<sup>-1</sup>)
    \retval NonStructuralNConc The nitrogen concentration of non-structural component (g g<sup>-1</sup>)
    \retval StructuralNConc The nitrogen concentration of structural component (g g<sup>-1</sup>)
    \retval MetabolicNConc The nitrogen concentration of metabolic component (g g<sup>-1</sup>)
    <remarks>
    The biomass of organ is split into three components, 
    structural, non-structural and metabolic. 
    </remarks>
     */
    /// <summary>
    /// Biomass of plant organs
    /// </summary>
    [Serializable]
    [XmlInclude(typeof(CompositeBiomass))]
    public class Biomass: Model
    {
        /// <summary>The _ structural wt</summary>
        protected double _StructuralWt = 0;
        /// <summary>The _ non structural wt</summary>
        protected double _NonStructuralWt = 0;
        /// <summary>The _ structural n</summary>
        protected double _StructuralN = 0;
        /// <summary>The _ non structural n</summary>
        protected double _NonStructuralN = 0;
        /// <summary>The _ potential dm allocation</summary>
        protected double _PotentialDMAllocation = 0;
        /// <summary>The _ metabolic wt</summary>
        protected double _MetabolicWt = 0;
        /// <summary>The _ metabolic n</summary>
        protected double _MetabolicN = 0;

        /// <summary>Gets or sets the non structural n.</summary>
        /// <value>The non structural n.</value>
        [XmlIgnore]
        [Units("g/m^2")]
        virtual public double NonStructuralN
        {
            get { return _NonStructuralN; }
            set
            {
                _NonStructuralN = MathUtilities.RoundToZero(value);

            }
        }

        /// <summary>Gets or sets the structural n.</summary>
        /// <value>The structural n.</value>
        [XmlIgnore]
        [Units("g/m^2")]
        virtual public double StructuralN
        {
            get { return _StructuralN; }
            set
            {
                _StructuralN = MathUtilities.RoundToZero(value);
            }
        }

        /// <summary>Gets or sets the non structural wt.</summary>
        /// <value>The non structural wt.</value>
        [XmlIgnore]
        [Units("g/m^2")]
        virtual public double NonStructuralWt
        {
            get { return _NonStructuralWt; }
            set
            {
                _NonStructuralWt = MathUtilities.RoundToZero(value);
            }
        }

        /// <summary>Gets or sets the structural wt.</summary>
        /// <value>The structural wt.</value>
        [XmlIgnore]
        [Units("g/m^2")]
        virtual public double StructuralWt
        {
            get { return _StructuralWt; }
            set
            {
                _StructuralWt = MathUtilities.RoundToZero(value);
            }
        }

        /// <summary>Gets or sets the potential dm allocation.</summary>
        /// <value>The potential dm allocation.</value>
        [XmlIgnore]
        [Units("g/m^2")]
        public double PotentialDMAllocation
        {
            get { return _PotentialDMAllocation; }
            set
            {
                _PotentialDMAllocation = MathUtilities.RoundToZero(value);
            }
        } //FIXME  This was only added because it was the only way I could get potential DM allocation values into a root layer array.  need to pull back to the root module

        /// <summary>Gets or sets the metabolic wt.</summary>
        /// <value>The metabolic wt.</value>
        [XmlIgnore]
        [Units("g/m^2")]
        virtual public double MetabolicWt
        {
            get { return _MetabolicWt; }
            set
            {
                _MetabolicWt = MathUtilities.RoundToZero(value);
            }
        }

        /// <summary>Gets or sets the metabolic n.</summary>
        /// <value>The metabolic n.</value>
        [XmlIgnore]
        [Units("g/m^2")]
        virtual public double MetabolicN
        {
            get { return _MetabolicN; }
            set
            {
                _MetabolicN = MathUtilities.RoundToZero(value);
            }
        }

        /// <summary>Gets the wt.</summary>
        /// <value>The wt.</value>
        [XmlIgnore]
        [Units("g/m^2")]
        virtual public double Wt
        {
            get
            {
                return StructuralWt + NonStructuralWt + MetabolicWt;
            }
        }

        /// <summary>Gets the n.</summary>
        /// <value>The n.</value>
        [XmlIgnore]
        [Units("g/m^2")]
        virtual public double N
        {
            get
            {
                return StructuralN + NonStructuralN + MetabolicN;
            }
        }

        /// <summary>Gets the n conc.</summary>
        /// <value>The n conc.</value>
        [XmlIgnore]
        [Units("g/g")]
        public double NConc
        {
            get
            {
                double wt = (StructuralWt + NonStructuralWt + MetabolicWt);
                double n = (StructuralN + NonStructuralN + MetabolicN);
                if (wt > 0)
                    return n / wt;
                else
                    return 0.0;
            }
        }

        /// <summary>Gets the structural n conc.</summary>
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

        /// <summary>Gets the non structural n conc.</summary>
        /// <value>The non structural n conc.</value>
        [Units("g/g")]
        public double NonStructuralNConc
        {
            get
            {
                if (NonStructuralWt > 0)
                    return NonStructuralN / NonStructuralWt;
                else
                    return 0.0;
            }
        }

        /// <summary>Gets the metabolic n conc.</summary>
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
            _StructuralWt = from.StructuralWt;
            _NonStructuralWt = from.NonStructuralWt;
            _MetabolicWt = from.MetabolicWt;
            _StructuralN = from.StructuralN;
            _NonStructuralN = from.NonStructuralN;
            _MetabolicN = from.MetabolicN;
        }

        /// <summary>Clears this instance.</summary>
        virtual public void Clear()
        {
            _StructuralWt = 0;
            _NonStructuralWt = 0;
            _MetabolicWt = 0;
            _StructuralN = 0;
            _NonStructuralN = 0;
            _MetabolicN = 0;
        }
        /// <summary>Adds the specified a.</summary>
        /// <param name="a">a.</param>
        public void Add(Biomass a)
        {
            _StructuralWt += a._StructuralWt;
            _NonStructuralWt += a._NonStructuralWt;
            _MetabolicWt += a._MetabolicWt;
            _StructuralN += a._StructuralN;
            _NonStructuralN += a._NonStructuralN;
            _MetabolicN += a._MetabolicN;
        }
        /// <summary>Sets to.</summary>
        /// <param name="a">a.</param>
        public void SetTo(Biomass a)
        {
            _StructuralWt = a.StructuralWt;
            _NonStructuralWt = a.NonStructuralWt;
            _MetabolicWt = a.MetabolicWt;
            _StructuralN = a.StructuralN;
            _NonStructuralN = a.NonStructuralN;
            _MetabolicN = a.MetabolicN;
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
                NonStructuralWt = a.NonStructuralWt + b.NonStructuralWt,
                MetabolicWt = a.MetabolicWt + b.MetabolicWt,
                StructuralN = a.StructuralN + b.StructuralN,
                NonStructuralN = a.NonStructuralN + b.NonStructuralN,
                MetabolicN = a.MetabolicN + b.MetabolicN
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
                NonStructuralWt = a.NonStructuralWt - b.NonStructuralWt,
                MetabolicWt = a.MetabolicWt - b.MetabolicWt,
                StructuralN = a.StructuralN - b.StructuralN,
                NonStructuralN = a.NonStructuralN - b.NonStructuralN,
                MetabolicN = a.MetabolicN - b.MetabolicN
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
                NonStructuralWt = a.NonStructuralWt * Fraction,
                MetabolicWt = a.MetabolicWt * Fraction,
                StructuralN = a.StructuralN * Fraction,
                NonStructuralN = a.NonStructuralN * Fraction,
                MetabolicN = a.MetabolicN * Fraction
            };

        }

        /// <summary>The _ empty</summary>
        private static Biomass _Empty = new Biomass();
        /// <summary>Gets the none.</summary>
        /// <value>The none.</value>
        public static Biomass None { get { return _Empty; } }
        /// <summary>Gets a value indicating whether this instance is empty.</summary>
        /// <value><c>true</c> if this instance is empty; otherwise, <c>false</c>.</value>
        public bool IsEmpty
        {
            get
            {
                return StructuralWt == 0 &&
                        NonStructuralWt == 0 &&
                        MetabolicWt == 0 &&
                        StructuralN == 0 &&
                        NonStructuralN == 0 &&
                        MetabolicN == 0;
            }
        }
    }

}