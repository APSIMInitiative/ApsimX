using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;

namespace Models.Plant
{
    public class Biomass: Model
    {
        protected double _StructuralWt = 0;
        protected double _NonStructuralWt = 0;
        protected double _StructuralN = 0;
        protected double _NonStructuralN = 0;
        protected double _PotentialDMAllocation = 0;
        protected double _MetabolicWt = 0;
        protected double _MetabolicN = 0;

        
        [Units("g/m^2")]
        virtual public double NonStructuralN
        {
            get { return _NonStructuralN; }
            set
            {
                _NonStructuralN = Utility.Math.RoundToZero(value);

            }
        }
        
        [Units("g/m^2")]
        virtual public double StructuralN
        {
            get { return _StructuralN; }
            set
            {
                _StructuralN = Utility.Math.RoundToZero(value);
            }
        }
        
        [Units("g/m^2")]
        virtual public double NonStructuralWt
        {
            get { return _NonStructuralWt; }
            set
            {
                _NonStructuralWt = Utility.Math.RoundToZero(value);
            }
        }
        
        [Units("g/m^2")]
        virtual public double StructuralWt
        {
            get { return _StructuralWt; }
            set
            {
                _StructuralWt = Utility.Math.RoundToZero(value);
            }
        }
        
        [Units("g/m^2")]
        public double PotentialDMAllocation
        {
            get { return _PotentialDMAllocation; }
            set
            {
                _PotentialDMAllocation = Utility.Math.RoundToZero(value);
            }
        } //FIXME  This was only added because it was the only way I could get potential DM allocation values into a root layer array.  need to pull back to the root module
        
        [Units("g/m^2")]
        virtual public double MetabolicWt
        {
            get { return _MetabolicWt; }
            set
            {
                _MetabolicWt = Utility.Math.RoundToZero(value);
            }
        }
        
        [Units("g/m^2")]
        virtual public double MetabolicN
        {
            get { return _MetabolicN; }
            set
            {
                _MetabolicN = Utility.Math.RoundToZero(value);
            }
        }
        
        [Units("g/m^2")]
        virtual public double Wt
        {
            get
            {
                return StructuralWt + NonStructuralWt + MetabolicWt;
            }
        }
        
        [Units("g/m^2")]
        virtual public double N
        {
            get
            {
                return StructuralN + NonStructuralN + MetabolicN;
            }
        }
        
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
        public Biomass() { }

        public Biomass(Biomass from)
        {
            _StructuralWt = from.StructuralWt;
            _NonStructuralWt = from.NonStructuralWt;
            _MetabolicWt = from.MetabolicWt;
            _StructuralN = from.StructuralN;
            _NonStructuralN = from.NonStructuralN;
            _MetabolicN = from.MetabolicN;
        }

        virtual public void Clear()
        {
            _StructuralWt = 0;
            _NonStructuralWt = 0;
            _MetabolicWt = 0;
            _StructuralN = 0;
            _NonStructuralN = 0;
            _MetabolicN = 0;
        }
        public void Add(Biomass a)
        {
            _StructuralWt += a._StructuralWt;
            _NonStructuralWt += a._NonStructuralWt;
            _MetabolicWt += a._MetabolicWt;
            _StructuralN += a._StructuralN;
            _NonStructuralN += a._NonStructuralN;
            _MetabolicN += a._MetabolicN;
        }
        public void SetTo(Biomass a)
        {
            _StructuralWt = a.StructuralWt;
            _NonStructuralWt = a.NonStructuralWt;
            _MetabolicWt = a.MetabolicWt;
            _StructuralN = a.StructuralN;
            _NonStructuralN = a.NonStructuralN;
            _MetabolicN = a.MetabolicN;
        }
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

        private static Biomass _Empty = new Biomass();
        public static Biomass None { get { return _Empty; } }
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