using System;
using System.Collections.Generic;
using System.Linq;
using APSIM.Shared.Utilities;
using Models.Core;

namespace Models.Surface
{

    /// <summary>
    /// Encapsulates a list of residue types for SurfaceOrganicMatter model
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(SurfaceOrganicMatter))]
    public class ResidueTypes : Model
    {
        /// <summary>Gets or sets the residues.</summary>
        private List<ResidueType> _residueTypes { get { return Children.OfType<ResidueType>().ToList(); } } 

        /// <summary>Return a list of fom type names</summary>
        public List<string> Names { get { return _residueTypes.Select(t => t.FomType).ToList(); } }

        /// <summary>Gets a specific residue type. Throws if not found.</summary>
        /// <param name="name">The name of the residue type to find</param>
        public ResidueType GetResidueType(string name)
        {
            ResidueType residueType = _residueTypes.Find(type => StringUtilities.StringsAreEqual(type.FomType, name));
            if (residueType != null)
                return FillDerived(residueType);
            throw new Exception($"Could not find residue type '{name}'");
        }

        /// <summary>Looks at a residue type and copies properties from the base type if one was specified.</summary>
        /// <param name="residueType">The residue to examine and change</param>
        private ResidueType FillDerived(ResidueType residueType)
        {
            if (residueType.FomType == residueType.DerivedFrom)
                throw new Exception($"Cannot derive ResidueType {residueType.FomType} from itself.");
            
            ResidueType baseType = null;
            baseType = _residueTypes.Find(type => StringUtilities.StringsAreEqual(type.FomType, residueType.DerivedFrom));
            if (baseType != null)
            {
                baseType = FillDerived(baseType); // Make sure the base residue type has itself been filled
                residueType = (ResidueType)ReflectionUtilities.Clone(residueType);
                if (residueType.FractionC == 0)
                    residueType.FractionC = baseType.FractionC;
                if (residueType.PO4ppm == 0)
                    residueType.PO4ppm = baseType.PO4ppm;
                if (residueType.NH4ppm == 0)
                    residueType.NH4ppm = baseType.NH4ppm;
                if (residueType.NO3ppm == 0)
                    residueType.NO3ppm = baseType.NO3ppm;
                if (residueType.SpecificArea == 0)
                    residueType.SpecificArea = baseType.SpecificArea;
                if (residueType.PotDecompRate == 0)
                    residueType.PotDecompRate = baseType.PotDecompRate;
                if (residueType.CfContrib == 0)
                    residueType.CfContrib = baseType.CfContrib;
                if (residueType.FR_C == null)
                    residueType.FR_C = (double[])baseType.FR_C.Clone();
                if (residueType.FR_N == null)
                    residueType.FR_N = (double[])baseType.FR_N.Clone();
                if (residueType.FR_P == null)
                    residueType.FR_P = (double[])baseType.FR_P.Clone();
            }
            return residueType;
        }
    }
}
