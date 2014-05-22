using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.PMF;
using Models.PMF.Organs;
namespace Models.PMF
{
    /// <summary>
    /// A class for adding biomass objects of a given type.
    /// </summary>
    public class BiomassSummariser : Biomass
    {
        private List<Organ> Organs = new List<Organ>();
        public BiomassSummariser(List<Organ> organs, Type TypeToMatch)
        {
            foreach (Organ o in organs)
                if (TypeToMatch == null || Utility.Reflection.IsOfType(o.GetType(), TypeToMatch.Name))
                    Organs.Add(o);
        }
        public Biomass Live
        {
            get
            {
                Biomass b = new Biomass();
                foreach (Organ o in Organs)
                    b += o.Live;
                return b;
            }
        }
        public Biomass Dead
        {
            get
            {
                Biomass b = new Biomass();
                foreach (Organ o in Organs)
                    b += o.Dead;
                return b;
            }
        }

        public override double NonStructuralN
        {
            get
            {
                return Live.NonStructuralN + Dead.NonStructuralN;
            }
            set
            {
                throw new NotImplementedException();
            }
        }
        public override double StructuralN
        {
            get
            {
                return Live.StructuralN + Dead.StructuralN;
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public override double NonStructuralWt
        {
            get
            {
                return Live.NonStructuralWt + Dead.NonStructuralWt;
            }
            set
            {
                throw new NotImplementedException();
            }
        }
        public override double StructuralWt
        {
            get
            {
                return Live.StructuralWt + Dead.StructuralWt;
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public override double MetabolicWt
        {
            get
            {
                return Live.MetabolicWt + Dead.MetabolicWt;
            }
            set
            {
                throw new NotImplementedException();
            }
        }
        public override double MetabolicN
        {
            get
            {
                return Live.MetabolicN + Dead.MetabolicN;
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public override double N { get { return Live.N + Dead.N; } }


        public override double Wt
        {
            get
            {
                return Live.Wt + Dead.Wt;
            }
        }

    }
}
