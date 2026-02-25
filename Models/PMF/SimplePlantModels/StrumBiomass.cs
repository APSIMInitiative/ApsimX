using APSIM.Numerics;
using DocumentFormat.OpenXml.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Models.PMF.SimplePlantModels.StrumTreeInstance;

namespace Models.PMF.SimplePlantModels
{
    /// <summary>
    /// Utility functions for estimating mature trunk mass (total woody AGB) and pruning fraction
    /// from canopy geometry, and wood density.
    ///
    /// Definitions and Rationale:
    /// - "Trunk" in STRUM = total above-ground WOODY biomass (dry): stem + bark + branches,
    ///   i.e., AGB without leaves and fruits (woody AGB). This is standard in biomass literature. 
    /// - Mass is computed via V_ref * density with architectural form-factors:
    ///     V_ref = (π/4) * DBH^2 * H_pre,   M_stem_pre = f_stem * V_ref * ρ,   M_branch_pre = f_branch_max * V_ref * ρ
    ///   with POST-prune masses scaled by crown-volume (branches) and height ratio (topping).
    ///
    /// References:
    /// - FAO/CIRAD allometry manual (volume × density with form factor; DBH±H predictors): Picard et al. 2012.  // FAO
    ///   https://www.fao.org/4/i3058e/i3058e.pdf
    /// - i-Tree appendices: biomass equations and wood densities; crown-dimension equations (DBH↔height/crown width).
    ///   Nowak 2020/2021, Appendix 10/11/13.  // i-Tree
    ///   Biomass eqns: https://www.fs.usda.gov/nrs/pubs/gtr/gtrnrs200_appendixes/gtr_nrs200_appendix10.pdf
    ///   Wood density: https://www.fs.usda.gov/nrs/pubs/gtr/gtr-nrs200-2021_appendixes/gtr_nrs200-2021_appendix11.pdf
    ///   Crown width:  https://www.fs.usda.gov/nrs/pubs/gtr/gtrnrs200_appendixes/gtr_nrs200_appendix13.pdf
    /// - UAV studies show canopy volume changes track pruned biomass in olive hedgerows:
    ///   Jiménez‑Brenes et al. 2017 (Plant Methods) / Digennaro et al. (CNR-IBE).  // UAV pruning and volume
    ///   https://link.springer.com/content/pdf/10.1186/s13007-017-0205-3.pdf
    /// - Partitioning practice (stem vs branches+bark) in national equations supports branch fractions ~25–40%:
    ///   RNCan Biomass Calculator / Lambert et al. 2005; Ung et al. 2008.  // stem and branch components
    ///   https://apps-scf-cfs.rncan.gc.ca/calc/en/biomass-calculator
    /// </summary>
    public class StrumBiomass
    {

        /// <summary>
        /// Geometry inputs describing the tree canopy and spacing at maturity.
        /// All lengths are metres (m).
        /// </summary>
        public sealed record StrumCanopyGeometry(
            double HeightBottomPrePrune_m,  // ground → crown bottom, before prune
            double HeightTopPrePrune_m,     // crown top (mature height) before prune
            double HeightTopPostPrune_m,    // crown top after prune (topping allowed)
            double WidthPrePrune_m,         // canopy width before prune
            double WidthPostPrune_m,        // canopy width after prune
            double InRowSpacing_m,          // per-tree length along row (spacing within row)
            double RowSpacing_m,               // optional: distance between rows
            double WoodDensity_kg_m3,        // oven-dry density [kg m^-3]
            double MatureDbh_cm       // optional: DBH at maturity [cm]; inferred if null/<=0
        );

        /// <summary>
        /// Defaults for architectural inference and biomass conversion.
        /// </summary>
        public static Dictionary<string, double> StrumBiomassParams = new Dictionary<string, double>()
        {
            // Slenderness & architectural controls
            {"StemFormFactorMin", 0.22 },
            {"StemFormFactorMax", 0.30},
            {"BranchFormFactorMin", 0.15 },
            {"BranchFormFactorMaxCap", 0.25 },
            {"CrownShapeCoeffMin", 0.70 },
            {"CrownShapeCoeffMax", 0.90 },
            {"SlendernessRef", 70.0 },          // reference H/DBH (m / m) ~ 70 typical for orchard trees
            {"TaperExponent", 0.15 },           // exponent for slenderness→stem form-factor
            // Pruning exponents (post-prune scaling)
            {"P_StemHeight", 1.1 },             // stem mass ~ (H_post/H_pre)^{p_stem}
            {"P_BranchVolume", 1.0 },            // branch mass ~ (V_post/V_pre)^{p_branch}
            {"DbhAreaExponent", 0.625},  // ~ 1/1.6 from Reineke SDI  (D ∝ A^0.625)
            {"DbhAreaRef_m2", 1.0},      // A_ref for scale
            {"DbhAtAreaRef_cm", 12.0},   // K: DBH (cm) at A_ref; set per system/species
            {"PackingRow_frac", 0.90},
            {"PackingInRow_frac", 1.00}, // often 1.0 along-row; adjust if you want packing there too
        };


        // ----------------------------
        // PUBLIC ENTRY POINTS
        // ----------------------------

        /// <summary>
        /// Estimate the MATURE trunk mass (kg per tree, oven-dry) BEFORE pruning, from wood density and
        /// geometry. 
        ///
        /// Units:
        ///   - Wood density: kg m^-3 (oven-dry; see i-Tree Appendix 11)
        ///   - DBH: centimetres (cm). Internally converted to metres.
        ///   - All lengths (heights, widths, spacings): metres (m)
        ///   - Output: kg (dry)
        ///
        /// Typical ranges:
        ///   - Wood density: 550–750 kg m^-3 for many orchard trees (apple/pear/stonefruit/olive/citrus)  // i-Tree Appendix 11
        ///   - DBH: Infered from geometry, 6–20 cm (high-density walls), 15–35 cm (standard), 20–60+ cm (large/long-lived)
        ///
        /// Rationale:
        ///   Uses basal-area × height (V_ref) with architectural form-factors (stem + branches).
        ///   Form-factors are inferred from slenderness (H/DBH) and crown fill (pre-prune volume vs spacing-limited reference).
        ///   This is consistent with FAO/CIRAD guidance and practice in i-Tree/national biomass systems.  // FAO and i-Tree
        /// </summary>
        public static (double, double) EstimateMatureTrunkMassKg(
                                                    TreeShape CrownShape,
                                                    double HeightBottomPrePrune_m,  // ground → crown bottom, before prune
                                                    double HeightTopPrePrune_m,     // crown top (mature height) before prune
                                                    double HeightTopPostPrune_m,    // crown top after prune (topping allowed)
                                                    double WidthPrePrune_m,         // canopy width before prune
                                                    double WidthPostPrune_m,        // canopy width after prune
                                                    double InRowSpacing_m,          // per-tree length along row (spacing within row)
                                                    double RowSpacing_m,            // distance between rows
                                                    double WoodDensity_kg_m3        // oven-dry density [kg m^-3]
                                                    )
        {
            if (WoodDensity_kg_m3 <= 0 || HeightTopPrePrune_m <= 0)
            {
                return (0,0);
            }

            // Architectural inference
            double kShape = InferCrownShapeCoeff(
                                                width_m: WidthPrePrune_m,
                                                hBottom_m: HeightBottomPrePrune_m,
                                                hTop_m: HeightTopPrePrune_m,
                                                kMin: StrumBiomassParams["CrownShapeCoeffMin"],
                                                kMax: StrumBiomassParams["CrownShapeCoeffMax"]
                                                );

            // Compute auto w from hedgyness
            double w_auto = ComputeHedgynessAndW(
                hBottomPre_m: HeightBottomPrePrune_m,
                hTopPre_m: HeightTopPrePrune_m,
                widthPre_m: WidthPrePrune_m,
                widthPost_m: WidthPostPrune_m,
                hTopPost_m: HeightTopPostPrune_m,
                inRow_m: InRowSpacing_m,
                row_m: RowSpacing_m,
                kShape: kShape,
                packingRow: 0.90, // 0.90 for hedgerow; 1.0 for free-standing tests
                alpha: 0.35, beta: 0.25, gamma: 0.25, delta: 0.15,
                kAspect: 0.35, wMin: 0.35, wMax: 0.85
            );

            // Infer DBH from area + height using w_auto
            double dbh_cm = InferDbh_FromAreaAndHeight(
                widthForInference_m: WidthPostPrune_m, // or WidthPrePrune_m if you prefer maximum seasonal spread
                inRow_m: InRowSpacing_m,
                row_m: RowSpacing_m,
                height_m: HeightTopPrePrune_m,
                w_auto: w_auto,
                K_cm: StrumBiomassParams.ContainsKey("DbhAtAreaRef_cm") ? StrumBiomassParams["DbhAtAreaRef_cm"] : 13.5,
                gamma: StrumBiomassParams.ContainsKey("DbhAreaExponent") ? StrumBiomassParams["DbhAreaExponent"] : 0.625,
                Aref_m2: StrumBiomassParams.ContainsKey("DbhAreaRef_m2") ? StrumBiomassParams["DbhAreaRef_m2"] : 10.0,
                S0: StrumBiomassParams.ContainsKey("S0") ? StrumBiomassParams["S0"] : 50.0,
                delta: StrumBiomassParams.ContainsKey("SlendernessAreaExponent") ? StrumBiomassParams["SlendernessAreaExponent"] : 0.15,
                packingRow: StrumBiomassParams["PackingRow_frac"],
                packingInRow: StrumBiomassParams["PackingInRow_frac"]
            );


            (double fStem, double fBranchMax) = InferFormFactors(
                                                                CrownShape,
                                                                dbh_cm, HeightBottomPrePrune_m, HeightTopPrePrune_m, WidthPrePrune_m,
                                                                InRowSpacing_m,
                                                                rowSpacing_m: RowSpacing_m,
                                                                kShape,
                                                                StrumBiomassParams["SlendernessRef"], StrumBiomassParams["TaperExponent"],
                                                                StrumBiomassParams["StemFormFactorMin"], StrumBiomassParams["StemFormFactorMax"],
                                                                StrumBiomassParams["BranchFormFactorMin"], StrumBiomassParams["BranchFormFactorMaxCap"]
                                                                );

            // Reference volume and masses
            double vRef_m3 = ReferenceStemVolume_m3(dbh_cm, HeightTopPrePrune_m);
            double stemMassPrePrune_kg = fStem * vRef_m3 * WoodDensity_kg_m3;
            double branchMassPrePrune_kg = fBranchMax * vRef_m3 * WoodDensity_kg_m3;

            return (stemMassPrePrune_kg + branchMassPrePrune_kg, dbh_cm);
        }

        /// <summary>
        /// Estimate the pruning fraction for STRUM’s winter prune using geometry:
        ///   • Branch mass scales with crown-volume ratio:  (V_post / V_pre)^{p_branch}
        ///   • Stem mass   scales with height ratio (topping): (H_post / H_pre)^{p_stem}
        ///
        /// Inputs:
        ///   - MatureTrunkMass: pre-prune total woody mass (stem+bark+branches), kg dry
        ///   - MatureDbh_cm:    DBH used to recover architectural form-factors (pre-prune)
        ///   - Heights/widths/spacing: metres
        ///
        /// Returns:
        ///   - fraction in [0,1]: 1 - (PostMass / PreMass)
        ///
        /// Notes:
        ///   - V_crown = kShape * Width * InRowSpacing * (H_top - H_bottom)
        ///   - If V_pre == 0, branches after pruning are set to 0 by construction (f_crown = 0).
        ///
        /// References:
        ///   - UAV orchard studies: crown volume change ↔ pruned biomass change (olive).         // Jiménez‑Brenes et al. 2017
        ///   - Crown volume approximations and geometry measures.                                   // Zhu et al. 2021 (Forestry)
        ///   - Volume×density and allometry context (FAO/CIRAD).                                  // Picard et al. (FAO/CIRAD)
        /// </summary>
        public static double StrumPruningFraction(
                                            TreeShape CrownShape,
                                            double HeightBottomPrePrune_m,
                                            double HeightBottomPostPrune_m,
                                            double HeightTopPrePrune_m,
                                            double HeightTopPostPrune_m,
                                            double WidthPrePrune_m,
                                            double WidthPostPrune_m,
                                            double InRowSpacing_m,
                                            double RowSpacing_m,
                                            double MatureDbh_cm,
                                            double MatureTrunkMass,
                                            double overrideCrownShapeCoeffPre = double.NaN,
                                            double overrideCrownShapeCoeffPost = double.NaN
                                            )
        {
            if (MatureTrunkMass <= 0)
                return 0.0;



            // --- kShape pre/post
            double kShapePre = !double.IsNaN(overrideCrownShapeCoeffPre)
                ? overrideCrownShapeCoeffPre
                : InferCrownShapeCoeff(
                    WidthPrePrune_m, HeightBottomPrePrune_m, HeightTopPrePrune_m,
                    StrumBiomassParams["CrownShapeCoeffMin"], StrumBiomassParams["CrownShapeCoeffMax"]);

            double kShapePost = !double.IsNaN(overrideCrownShapeCoeffPost)
                ? overrideCrownShapeCoeffPost
                : InferCrownShapeCoeff(
                    WidthPostPrune_m, HeightBottomPostPrune_m, HeightTopPostPrune_m,
                    StrumBiomassParams["CrownShapeCoeffMin"], StrumBiomassParams["CrownShapeCoeffMax"]);

            // --- Pre-prune architectural shares
            (double fStem, double fBranchMax) = InferFormFactors(CrownShape,
                MatureDbh_cm, HeightBottomPrePrune_m, HeightTopPrePrune_m, WidthPrePrune_m,
                InRowSpacing_m, rowSpacing_m: RowSpacing_m,
                kShapePre,
                StrumBiomassParams["SlendernessRef"], StrumBiomassParams["TaperExponent"],
                StrumBiomassParams["StemFormFactorMin"], StrumBiomassParams["StemFormFactorMax"],
                StrumBiomassParams["BranchFormFactorMin"], StrumBiomassParams["BranchFormFactorMaxCap"]);

            double share = fStem + fBranchMax;
            if (share <= 0) return 0.0;

            double mStemPre = MatureTrunkMass * (fStem / share);
            double mBranchPre = MatureTrunkMass * (fBranchMax / share);

            // --- Crown volumes
            bool freeStanding = (RowSpacing_m > WidthPrePrune_m);
            double vCrownPre = CrownVolume_m3(CrownShape, freeStanding, WidthPrePrune_m, InRowSpacing_m, HeightBottomPrePrune_m, HeightTopPrePrune_m);
            double vCrownPost = CrownVolume_m3(CrownShape, freeStanding, WidthPostPrune_m, InRowSpacing_m, HeightBottomPostPrune_m, HeightTopPostPrune_m);

            // --- Scaling factors with zero-division guards
            double fCrown;
            if (vCrownPre > 0.0)
                fCrown = MathUtilities.Bound(Math.Pow(vCrownPost / vCrownPre, StrumBiomassParams["P_BranchVolume"]), 0.0, 1.0);
            else
                // No pre-prune crown volume ⇒ no retained branch mass after prune by construction
                fCrown = 0.0;

            double fStemH;
            if (HeightTopPrePrune_m > 0.0)
                fStemH = MathUtilities.Bound(Math.Pow(HeightTopPostPrune_m / HeightTopPrePrune_m, StrumBiomassParams["P_StemHeight"]), 0.0, 1.0);
            else
                fStemH = 0.0;

            // --- Post masses & fraction
            double mBranchPost = mBranchPre * fCrown;
            double mStemPost = mStemPre * fStemH;

            double mPost = Math.Max(0.0, mStemPost + mBranchPost);
            return MathUtilities.Bound(1.0 - (mPost / MatureTrunkMass), 0.0, 1.0);
        }

        // ----------------------------
        // INTERNAL HELPERS (documented)
        // ----------------------------

        /// <summary>
        /// Reference stem volume (m³) before pruning:
        ///   V_ref = (π/4) * DBH^2 * H_pre
        /// </summary>
        private static double ReferenceStemVolume_m3(double dbh_cm, double heightTopPre_m)
        {
            double dbh_m = Math.Max(0.001, dbh_cm / 100.0);
            double basalArea_m2 = Math.PI * 0.25 * dbh_m * dbh_m;
            return basalArea_m2 * heightTopPre_m;
        }

        /// <summary>
        /// Crown volume (m³) from explicit TreeShape and spacing regime.
        /// If RowSpacing > WidthPrePrune => free-standing solid; otherwise column.
        /// Uses shape-specific footprint and vertical fill (k) so V = k * A_fp * H_crown.
        /// </summary>
        private static double CrownVolume_m3(
            TreeShape shape, bool freeStanding,
            double width_m, double inRow_m,
            double hBottom_m, double hTop_m)
        {
            double Hc = Math.Max(0.0, hTop_m - hBottom_m);
            if (Hc <= 0.0 || width_m <= 0.0 || inRow_m <= 0.0) return 0.0;
            (double k, double Afp) = GetShapeCoefficients(shape, freeStanding, width_m, inRow_m);
            return k * Afp * Hc;
        }

        /// <summary>
        /// Returns (kShape, fpArea_m2) for the chosen TreeShape and row constraint.
        /// If freeStanding=true, footprint ignores the row length and uses shape’s own base;
        /// otherwise footprint is W * L (column).
        /// </summary>
        private static (double kShape, double fpArea_m2) GetShapeCoefficients(
            TreeShape shape, bool freeStanding, double width_m, double inRow_m)
        {
            width_m = Math.Max(width_m, 1e-6);
            double W = width_m;
            double L = Math.Max(inRow_m, 1e-6);

            if (freeStanding)
            {
                switch (shape)
                {
                    case TreeShape.Round:
                        // circular footprint; ellipsoid-ish vertical fill
                        double AfpCirc = Math.PI * 0.25 * W * W;
                        return (0.85, AfpCirc);

                    case TreeShape.Square:
                        double AfpSq = W * W;
                        return (0.80, AfpSq);

                    case TreeShape.Triangular:
                        // square base but pyramidal vertical fill
                        double AfpTri = W * W;
                        return (0.55, AfpTri);
                }
            }
            else
            {
                // Row-constrained “columns”: footprint is W * L
                double AfpCol = W * L;
                switch (shape)
                {
                    case TreeShape.Round:
                        return (0.85, AfpCol);      // cylindrical wall with rounded edges
                    case TreeShape.Square:
                        return (0.85, AfpCol);      // boxy wall; similar k as round-column in practice
                    case TreeShape.Triangular:
                        return (0.55, AfpCol);      // triangular column
                }
            }

            // Fallback (should not hit)
            return (0.80, W * L);
        }

        /// <summary>
        /// Infer crown-shape coefficient (k_shape) from canopy aspect (width vs height), 
        /// clamped to [kMin, kMax]. Narrow, tall hedgerow walls tend toward higher k (0.8–0.9).
        /// </summary>
        private static double InferCrownShapeCoeff(double width_m, double hBottom_m, double hTop_m, double kMin, double kMax)
        {
            double canopyH = Math.Max(0.0, hTop_m - hBottom_m);
            if (canopyH <= 0 || width_m <= 0) return MathUtilities.Bound(0.80, kMin, kMax);
            double aspect = width_m / canopyH; // <1 = tall & narrow; >1 = squat
                                               // Map aspect to 0.70–0.90 with gentle slope; hedgerow walls yield higher coeffs.
            double coeff = 0.80 + 0.10 * MathUtilities.Bound(0.5 - aspect, -1.0, 1.0);
            return MathUtilities.Bound(coeff, kMin, kMax);
        }

        /// <summary>
        /// Infer architectural form-factors:
        ///   - Stem form-factor f_stem via slenderness (H/DBH) with a gentle exponent (taper proxy).
        ///   - Maximum branch form-factor f_branch_max via "crown fill": (actual pre-prune crown volume) / (spacing-limited ref).
        ///
        /// Constraints:
        ///   f_stem ∈ [StemFormFactorMin, StemFormFactorMax]  (~0.22–0.30 typical)
        ///   f_branch_max ∈ [BranchFormFactorMin, BranchFormFactorMaxCap] (~0.15–0.25 for hedgerows)
        ///
        /// Rationale and Evidence:
        /// - Stem taper/slenderness links to biomechanics and hydraulics; we use a conservative exponent to avoid over-sensitivity.
        ///   (Pipe-model and biomechanics reviews discuss sapwood/area scaling and limitations.)  // PMT review
        /// - Branch fraction rises with crown filling; national and orchard datasets often show branches+bark ~25–40% of woody AGB,
        ///   with intensively pruned hedges at the lower end.  // RNCan and orchard practice
        /// </summary>
        private static (double fStem, double fBranchMax) InferFormFactors(
                                                                        TreeShape CrownShape,
                                                                        double dbh_cm,
                                                                        double heightBottomPrePrune_m,
                                                                        double heightTopPrePrune_m,
                                                                        double widthPrePrune_m,
                                                                        double inRowSpacing_m,
                                                                        double rowSpacing_m,
                                                                        double kShape,
                                                                        double slendernessRef,
                                                                        double taperExponent,
                                                                        double fStemMin,
                                                                        double fStemMax,
                                                                        double fBranchMin,
                                                                        double fBranchMaxCap
                                                                        )
        {
            double dbh_m = Math.Max(0.001, dbh_cm / 100.0);
            double s = (heightTopPrePrune_m > 0) ? heightTopPrePrune_m / dbh_m : slendernessRef;

            // Slenderness-aware stem form-factor (gentle): f_stem = f0*(s/s0)^α
            double fStem = 0.26 * Math.Pow(s / slendernessRef, taperExponent);
            fStem = MathUtilities.Bound(fStem, fStemMin, fStemMax);

            // Crown fill

            bool freeStanding = (inRowSpacing_m > widthPrePrune_m);

            double vCrownPre = CrownVolume_m3(CrownShape, freeStanding, widthPrePrune_m, inRowSpacing_m, heightBottomPrePrune_m, heightTopPrePrune_m);


            double vCrownPost = CrownVolume_m3(CrownShape, freeStanding, widthPrePrune_m, inRowSpacing_m, heightBottomPrePrune_m, heightTopPrePrune_m);

            double phi = (vCrownPost > 0) ? MathUtilities.Bound(vCrownPre / vCrownPost, 0.0, 1.0) : 0.0;

            double fBranchMax = 0.18 + 0.10 * phi;  // ~0.18 at sparse canopies → up to ~0.28 if fully packed
            fBranchMax = MathUtilities.Bound(fBranchMax, fBranchMin, fBranchMaxCap);

            return (fStem, fBranchMax);
        }


        /// <summary>
        /// Computes a dimensionless “hedgyness” score H ∈ [0,1] from existing canopy geometry,
        /// and maps it to a DBH-blend weight w ∈ [wMin, wMax] used in the two-factor DBH model:
        ///     D = D_area^(1-w) * D_slender^(w)
        ///
        /// Intuition:
        ///   Hedgerows are tall, narrow, strongly pruned, and width-limited by row spacing.
        ///   Free-standing trees are wider, less confined, and less pruned. We quantify this as:
        ///     (1) Across-row confinement  Cr = min( Width_post / (packingRow * RowSpacing), 1 )
        ///     (2) Wall aspect            Wa = tanh( kAspect * crownHeight / Width_post )
        ///     (3) Pruning intensity       P = 1 - V_crown_post / V_crown_pre
        ///     (4) Crown fill             φ  = V_crown_pre / V_ref  (V_ref uses min(Width_pre, RowSpacing))
        ///   H = α·Cr + β·Wa + γ·P + δ·φ ;  w = wMin + (wMax - wMin)·H
        ///
        /// Notes:
        /// - Uses only inputs already present in STRUM (no new parameters).
        /// - Set packingRow=0.90 to enforce hedgerow behavior or 1.0 for unconstrained rows.
        /// - kShape is your crown-volume shape coefficient (elliptical/boxy wall).
        ///
        /// References:
        /// - Spacing / growing-space logic (Reineke SDI): larger growing space increases stem size sub-linearly.  (D ∝ A^γ, γ≈0.62) [1](https://link.springer.com/content/pdf/10.1093/forestscience/51.4.304.pdf)[2](https://oakmissouri.org/nrbiometrics/topics/maxsdi.html)
        /// - Height–diameter allometry (Näslund, Chapman–Richards families) motivates slenderness as a control.   [3](https://www.srs.fs.usda.gov/pubs/gtr/gtr_srs175/gtr_srs175_577.pdf)[4](https://www.academia.edu/54145080/The_Possibility_of_Using_the_Chapman_Richards_and_N%C3%A4slund_Functions_to_Model_Height_Diameter_Relationships_in_Hemiboreal_Old_Growth_Forest_in_Estonia)
        /// - Crown geometry–DBH relations (width/area ↔ DBH) justify using canopy base area signals.             [5](https://plantandfood-my.sharepoint.com/personal/hamish_brown_plantandfood_co_nz/Documents/Microsoft%20Copilot%20Chat%20Files/StrumBiomass.cs)[6](https://www.fs.usda.gov/nrs/pubs/gtr/gtrnrs200_appendixes/gtr_nrs200_appendix13.pdf)
        /// </summary>
        private static double ComputeHedgynessAndW(
                                                    double hBottomPre_m, double hTopPre_m,
                                                    double widthPre_m, double widthPost_m,
                                                    double hTopPost_m,
                                                    double inRow_m, double row_m,
                                                    double kShape,
                                                    // knobs / weights with sensible defaults for orchard systems
                                                    double packingRow = 0.90,
                                                    double alpha = 0.35, double beta = 0.25, double gamma = 0.25, double delta = 0.15,
                                                    double kAspect = 0.35, double wMin = 0.35, double wMax = 0.85
                                                    )
            {
                double Hcan_pre = Math.Max(0.0, hTopPre_m - hBottomPre_m);
                double Hcan_post = Math.Max(0.0, hTopPost_m - hBottomPre_m);
                double W_wall = Math.Max(1e-6, widthPost_m); // use post-prune width as “wall” width

                // 1) Across-row confinement (0..1)
                double Cr = MathUtilities.Bound(W_wall / (Math.Max(1e-6, packingRow * row_m)), 0.0, 1.0);

                // 2) Wall aspect via smooth tanh, taller/narrower → closer to 1
                double Wa = MathUtilities.Bound(Math.Tanh(kAspect * (Hcan_pre / W_wall)), 0.0, 1.0);

                // 3) Pruning intensity (volume ratio)
                double Vpre = kShape * widthPre_m * inRow_m * Hcan_pre;
                double Vpost = kShape * widthPost_m * inRow_m * Hcan_post;
                double P = 0.0;
                if (Vpre > 0.0)
                    P = MathUtilities.Bound(1.0 - (Vpost / Vpre), 0.0, 1.0);

                // 4) Crown fill vs spacing-limited reference (use pre-prune geometry against row cap)
                double Vref = kShape * Math.Min(widthPre_m, row_m) * inRow_m * Hcan_pre;
                double phi = (Vref > 0.0) ? MathUtilities.Bound(Vpre / Vref, 0.0, 1.0) : 0.0;

                // Weighted hedgyness and mapping to w
                // Ensure weights sum to ~1.0; use defaults if not.
                double sumW = alpha + beta + gamma + delta;
                if (sumW <= 0) { alpha = 0.35; beta = 0.25; gamma = 0.25; delta = 0.15; sumW = 1.0; }

                double H = (alpha * Cr + beta * Wa + gamma * P + delta * phi) / sumW;
                double w = wMin + (wMax - wMin) * MathUtilities.Bound(H, 0.0, 1.0);

                return MathUtilities.Bound(w, wMin, wMax);
            }

        /// <summary>
        /// Two-factor DBH inference from canopy base area and height, blending:
        ///   • Space-driven DBH:   D_A = K * (A_eff / A_ref)^γ     (SDI-derived growing-space scaling)
        ///   • Slenderness-driven: D_S = H / S_targ(A)             (H–D allometry via target slenderness)
        /// Final: D = D_A^(1-w) * D_S^w, where w comes from ComputeHedgynessAndW().
        /// 
        /// Parameters (species/system):
        ///   - K_cm: DBH (cm) at A_ref; choose per species/training from one reference tree.
        ///   - γ:    area exponent; SDI suggests ≈ 1/1.605 ≈ 0.62 (species vary).                      [1](https://link.springer.com/content/pdf/10.1093/forestscience/51.4.304.pdf)[2](https://oakmissouri.org/nrbiometrics/topics/maxsdi.html)
        ///   - S0:   slenderness (H/DBH, m/m) at A_ref; orchard free-standing often 45–60.            [3](https://www.srs.fs.usda.gov/pubs/gtr/gtr_srs175/gtr_srs175_577.pdf)
        ///   - δ:    how slenderness relaxes with area; small (0.10–0.25) makes trees stouter as A↑.
        ///   - packingRow/packingInRow: set to 0.9 for hedgerow (respect row cap) or 1.0 for free.
        /// 
        /// Usage:
        ///   1) w_auto = ComputeHedgynessAndW(...);    // from your existing geometry
        ///   2) dbh_cm = InferDbh_FromAreaAndHeight(..., w_auto, ...); // pass w_auto here
        ///
        /// References:
        /// - Reineke SDI and density management diagrams motivate the area exponent γ.                 [1](https://link.springer.com/content/pdf/10.1093/forestscience/51.4.304.pdf)[7](https://auf.isa-arbor.com/content/27/6/306)
        /// - Height–diameter families (Näslund/Chapman–Richards) justify a slenderness-based control.  [3](https://www.srs.fs.usda.gov/pubs/gtr/gtr_srs175/gtr_srs175_577.pdf)[4](https://www.academia.edu/54145080/The_Possibility_of_Using_the_Chapman_Richards_and_N%C3%A4slund_Functions_to_Model_Height_Diameter_Relationships_in_Hemiboreal_Old_Growth_Forest_in_Estonia)
        /// - Crown geometry and DBH covary; using canopy footprint is consistent with i-Tree practice.  [5](https://plantandfood-my.sharepoint.com/personal/hamish_brown_plantandfood_co_nz/Documents/Microsoft%20Copilot%20Chat%20Files/StrumBiomass.cs)
        /// </summary>
        private static double InferDbh_FromAreaAndHeight(
                                                        double widthForInference_m, // choose pre- or post-prune width per your system
                                                        double inRow_m,
                                                        double row_m,
                                                        double height_m,
                                                        double w_auto,              // from ComputeHedgynessAndW(...)
                                                                                    // species/system parameters with robust defaults
                                                        double K_cm = 13.5,
                                                        double gamma = 0.625,
                                                        double Aref_m2 = 10.0,
                                                        double S0 = 50.0,
                                                        double delta = 0.15,
                                                        double packingRow = 1.0,
                                                        double packingInRow = 1.0
                                                        )
        {
            // 1) Effective footprint
            double Leff = Math.Max(1e-6, inRow_m * packingInRow);
            double Weff = Math.Max(1e-6, Math.Min(widthForInference_m, row_m * packingRow));
            double Aeff = Leff * Weff;

            // 2) Space-driven diameter (cm)
            double D_A_cm = K_cm * Math.Pow(Aeff / Aref_m2, gamma);
            D_A_cm = Math.Max(1.0, D_A_cm);

            // 3) Slenderness-driven diameter (cm), with area-conditioned target slenderness
            double S_targ = S0 * Math.Pow(Aref_m2 / Aeff, delta);    // H/DBH (m/m)
            double D_S_cm = (height_m / Math.Max(1e-6, S_targ)) * 100.0;
            D_S_cm = Math.Max(1.0, D_S_cm);

            // 4) Blend with auto w (no end clamp needed)
            double w = MathUtilities.Bound(w_auto, 0.0, 1.0);
            double lnD = (1.0 - w) * Math.Log(D_A_cm) + w * Math.Log(D_S_cm);
            return Math.Exp(lnD);
        }
    }
}

