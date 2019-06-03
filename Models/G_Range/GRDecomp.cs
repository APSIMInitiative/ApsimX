using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models
{
    using Models.Core;
    using Models.Interfaces;

    public partial class G_Range : Model, IPlant, ICanopy, IUptake
    {
        /// <summary>
        /// Partition the litter into structural and metabolic components, based on the lignin C to N ratio (PARTLITR in Savanna, kept in DECOMP.F)
        /// </summary>
        void PartitionLitter(int iLayer, double dthc, double dthn, double ligninConc)
        {
            if (dthc < 0.0001)
                return;    // Nothing to partition

            // A section of code is turned off in this version using dirabs= 0, and commented out in Sav5b4, and so not included here.No direct absorbtion included here.

            // N content, using a biomass basis and 2.5 conversion
            double frn_conc = dthn / (dthc * 2.5);   //  Greater than 0 checked above.
            double rlnres = ligninConc / (frn_conc + 1.0e-6);   // Addition of 1.e - 6 prevents 0 division.
            rlnres = Math.Max(0.0, rlnres);
            double frmet = 0.85 - 0.013 * rlnres;     // The values are parameters taken from Century, spl()
            frmet = Math.Max(0.0, frmet);
            double frstruc = 1.0 - frmet;
            // Ensure the structural fraction is not greater than the lignin fraction
            if (ligninConc > frstruc)
            {
                frstruc = ligninConc;
                frmet = 1.0 - frstruc;
            }

            // Put a minimum fraction of materials to metabolic
            if (frmet < 0.2)
            {
                frmet = 0.2;
                frstruc = 0.8;
            }

            double flow_struc = frstruc * dthc;
            double flow_metab = frmet * dthc;

            // Flows to metaboloic and structural components
            for (int iFacet = 0; iFacet < nFacets; iFacet++)
            {
                // Lignin content of the structural litter.  Lignin_structural_residue is a fraction.
                double old_lignin = plantLigninFraction[iFacet, iLayer] * litterStructuralCarbon[iLayer];
                double new_lignin = ligninConc * dthc;

                if ((litterStructuralCarbon[iLayer] + flow_struc) > 0.0001)
                    plantLigninFraction[iFacet, iLayer] = (old_lignin + new_lignin) / (litterStructuralCarbon[iLayer] + flow_struc);
                else
                    plantLigninFraction[iFacet, iLayer] = 0.05;    // Assigned a typical lignin concentration.Should not occur, but to prevent errors ...  Adjust at will.

                plantLigninFraction[iFacet, iLayer] = Math.Max(0.02, plantLigninFraction[iFacet, iLayer]);
                plantLigninFraction[iFacet, iLayer] = Math.Min(0.50, plantLigninFraction[iFacet, iLayer]);
            }
            litterStructuralCarbon[iLayer] = litterStructuralCarbon[iLayer] + flow_struc;
            litterMetabolicCarbon[iLayer] = litterMetabolicCarbon[iLayer] + flow_metab;

            double flow_strucn = flow_struc / 200.0;         // 200 is C:N ratio in the "elements" structural litter C: E ratio array RCESTR() in Century and Savanna.CENTURY uses 150 for nitrogen, and a note in Savanna says "200 is used everywhere" so for simplicity...
            if (flow_strucn > dthn)
                flow_strucn = dthn;

            litterStructuralNitrogen[iLayer] = litterStructuralNitrogen[iLayer] + flow_strucn;

            double flow_metabn = dthn - flow_strucn;
            flow_metabn = Math.Max(0.0, flow_metabn);
            litterMetabolicNitrogen[iLayer] = litterMetabolicNitrogen[iLayer] + flow_metabn;
        }
    }
}
