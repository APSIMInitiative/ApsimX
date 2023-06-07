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
    public class ResidueTypes : Model
    {
        /// <summary>Gets or sets the residues.</summary>
        public List<ResidueType> ResidueType { get; set; }

        /// <summary>Return a list of fom type names</summary>
        public List<string> Names { get { return ResidueType.Select(t => t.fom_type).ToList(); } }

        /// <summary>Gets a specific residue type. Throws if not found.</summary>
        /// <param name="name">The name of the residue type to find</param>
        public ResidueType GetResidueType(string name)
        {
            if (ResidueType != null)
            {
                ResidueType residueType = ResidueType.Find(type => StringUtilities.StringsAreEqual(type.fom_type, name));
                if (residueType != null)
                    return FillDerived(residueType);
            }
            throw new Exception($"Could not find residue type '{name}'");
        }

        /// <summary>Looks at a residue type and copies properties from the base type if one was specified.</summary>
        /// <param name="residueType">The residue to examine and change</param>
        private ResidueType FillDerived(ResidueType residueType)
        {
            ResidueType baseType = null;
            if (residueType.derived_from != null)
            {
                baseType = ResidueType.Find(type => StringUtilities.StringsAreEqual(type.fom_type, residueType.derived_from));
                if (baseType != null)
                    baseType = FillDerived(baseType); // Make sure the base residue type has itself been filled
            }
            if (baseType != null)
            {
                residueType = (ResidueType)ReflectionUtilities.Clone(residueType);
                if (residueType.fraction_C == 0)
                    residueType.fraction_C = baseType.fraction_C;
                if (residueType.po4ppm == 0)
                    residueType.po4ppm = baseType.po4ppm;
                if (residueType.nh4ppm == 0)
                    residueType.nh4ppm = baseType.nh4ppm;
                if (residueType.no3ppm == 0)
                    residueType.no3ppm = baseType.no3ppm;
                if (residueType.specific_area == 0)
                    residueType.specific_area = baseType.specific_area;
                if (residueType.pot_decomp_rate == 0)
                    residueType.pot_decomp_rate = baseType.pot_decomp_rate;
                if (residueType.cf_contrib == 0)
                    residueType.cf_contrib = baseType.cf_contrib;
                if (residueType.fr_c == null)
                    residueType.fr_c = (double[])baseType.fr_c.Clone();
                if (residueType.fr_n == null)
                    residueType.fr_n = (double[])baseType.fr_n.Clone();
                if (residueType.fr_p == null)
                    residueType.fr_p = (double[])baseType.fr_p.Clone();
            }
            return residueType;
        }
    }
}
