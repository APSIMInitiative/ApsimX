using APSIM.Numerics;
using DocumentFormat.OpenXml.InkML;
using DocumentFormat.OpenXml.Math;
using DocumentFormat.OpenXml.Presentation;
using MathNet.Numerics.Distributions;
using PdfSharp.Snippets;
using System;
using System.Collections.Generic;
using static Models.PMF.SimplePlantModels.StrumTreeInstance;
using static System.Net.WebRequestMethods;

namespace Models.PMF.SimplePlantModels
{
    /// <summary>
    /// Utility functions for estimating mature wood mass (total woody AGB) and pruning fraction
    /// from canopy geometry, and wood density.
    ///
    /// Definitions and Rationale:
    /// - "Wood" in STRUM = total above-ground WOODY biomass (dry): stem + bark + branches,
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
        /// Defaults for architectural inference and biomass conversion.
        /// Notes:
        /// • “Orchard default” values are meant to be sensible for hedgerow broadleaf crops.
        /// • Tune DbhAtTypicalArea_cm and S_base per species/cultivar and training block.
        /// </summary>
        public static Dictionary<string, double> Params = new Dictionary<string, double>()
{
    // ───────────── Stem/branch geometric form (used by tapered stem volume) ─────────────
    {"StemTaperBase", 0.45},   // Baseline stem form factor (cylinder reduction); orchard default ~0.45
    {"StemTaperMin",  0.30},   // Lower clamp for taper factor (prevents unrealistic under‑taper)
    {"StemTaperMax",  0.55},   // Upper clamp for taper factor (prevents over‑taper)

    // ───────────── DBH inference: space & slenderness controls ─────────────
    {"DbhAtTypicalArea_cm",   22.0}, // DBH (cm) expected at the computed “typical” area for this block (tune per species/site)
    {"DbhAreaExponent",        0.625}, // Growing‑space exponent γ (SDI logic; 0.60–0.65 typical for broadleafs)
    {"SlendernessAreaExponent",0.15},  // How target slenderness relaxes with area (δ; small positive makes trees stouter with more space)

    // ───────────── Dynamic slenderness (S0) from canopy geometry & hedgyness ─────────────
    {"S_base",       35.0},  // Base slenderness target H/DBH (m/m) in dynamic S0 (tune per species/cultivar)
    {"alpha_aspect", 0.10},  // Weight: canopy aspect influence on S0 (tall‑narrow → slightly lower target slenderness)
    {"k_aspect",     0.75},  // Slope inside tanh() for aspect response
    {"alpha_LCR",    0.20},  // Weight: live‑crown ratio influence on S0 (longer live crown → lower S0)
    {"LCR_ref",      0.55},  // Reference live‑crown ratio for neutral effect
    {"alpha_hedge",  0.10},  // Weight: hedgyness influence on S0 (heavier hedging → slightly lower S0)

    // ───────────── Crown shape coefficient (vertical fill of the canopy wall) ─────────────
    {"CrownShapeCoeffMin", 0.70}, // Clamp: minimum crown k‑shape (vertical fill) for very squat crowns
    {"CrownShapeCoeffMax", 0.90}, // Clamp: maximum crown k‑shape for tall, narrow walls
    {"kBase",              0.80}, // Baseline crown k‑shape before aspect adjustment
    {"kSlope",             0.10}, // Slope: how strongly aspect nudges k‑shape
    {"aCenter",            0.50}, // Aspect center (width/height) where baseline applies

    // ───────────── Hedgyness → DBH blend weight w (0..1 mapped into [wMin,wMax]) ─────────────

    {"alpha", 0.35},  // Cr (across‑row confinement) weight inside hedgyness score H
    {"beta",  0.25},  // Wa (wall aspect) weight inside H
    {"gamma", 0.25},  // P (pruning intensity by volume) weight inside H
    {"delta", 0.15},  // φ (crown fill vs spacing‑limited ref) weight inside H
    {"kAspect", 0.35},// Scale for tanh in Wa = tanh(kAspect * Hcan/width)
    {"wMin",   0.35}, // Lower bound of DBH blend weight (biases some toward slenderness branch)
    {"wMax",   0.85}, // Upper bound of DBH blend weight (allows strong slenderness control in hedges)

    // ───────────── Packing (caps for usable across‑row and along‑row space) ─────────────
    {"PackingRow_frac",   0.90}, // Across‑row packing/cap fraction (0.9 for hedgerows; 1.0 for free‑standing)
    {"PackingInRow_frac", 1.00}, // Along‑row packing/cap fraction (often 1.0)

    // ───────────── Pruning response exponents ─────────────
    {"P_StemHeight",  1.1}, // Stem mass scales with (H_post/H_pre)^{p_stem} when topping
    {"P_BranchVolume",1.0}, // Branch mass scales with (V_post/V_pre)^{p_branch} when hedging

    // ───────────── Branch fraction band & predictor (ratio‑based) ─────────────
    {"BranchFractionMin", 0.20}, // Lower bound for branch mass fraction of stem (φ/R=0 → sparse canopy)
    {"BranchFractionMax", 0.35}, // Upper bound for branch mass fraction of stem (φ/R=1 or very high R → packed canopy)
    {"BF_RatioHalfSat",  400.0}, // Half‑saturation R*: V_crown/V_stem at which branch fraction is mid‑band (set to block median R)
    {"BF_RatioShape",      1.2}, // Curvature p (>1 = steeper central rise; use 1.0–1.5)

    // ───────────── Legacy slenderness reference (used in taper guard) ─────────────
    {"SlendernessRef", 70.0},   // Reference slenderness used in taper bounds (not the dynamic S0)
    };


        // ----------------------------
        // PUBLIC ENTRY POINTS
        // ----------------------------

        /// <summary>
        /// Estimate the MATURE wood mass (kg per tree, oven-dry) BEFORE pruning, from wood density and
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
        public static double EstimateMatureWoodMassKg(
                                                    TreeShape crownShape,
                                                    double heightBottomPrePrune_m,  // ground → crown bottom, before prune
                                                    double heightTopPrePrune_m,     // crown top (mature height) before prune
                                                    double heightTopPostPrune_m,    // crown top after prune (topping allowed)
                                                    double widthPrePrune_m,         // canopy width before prune
                                                    double widthPostPrune_m,        // canopy width after prune
                                                    double inRowSpacing_m,          // per-tree length along row (spacing within row)
                                                    double rowSpacing_m,            // distance between rows
                                                    double woodDensity_kg_m3,       // oven-dry density [kg m^-3]
                                                    double reffDBHatMaturity_cm,    // diameter of stem at brest height for tree of reference canopy size when orchard tree is mature (cm)
                                                    double reffArea_m2              // canopy area for the reffDBH tree
                                                    )
        {
            if (woodDensity_kg_m3 <= 0 || heightTopPrePrune_m <= 0)
            {
                return 0;
            }

            // Architectural inference
            double kShape = InferCrownShapeCoeff(
                                                width_m: widthPrePrune_m,
                                                hBottom_m: heightBottomPrePrune_m,
                                                hTop_m: heightTopPrePrune_m
                                                );

            // Spacing‑limited reference crown volume (cap width by spacings)
            //double widthCap_m = Math.Min(widthPrePrune_m, Math.Min(rowSpacing_m, inRowSpacing_m));

            bool freeStanding = IsFreeStanding(widthPrePrune_m, inRowSpacing_m, rowSpacing_m);

            double canopyArea = GetFootprintArea_m2(crownShape, freeStanding, widthPrePrune_m, inRowSpacing_m);

            // Pre‑prune crown volume (using inferred k for consistency)
            double vCrownPre_m3 = CrownVolume_m3(crownShape, freeStanding,
                                                    width_m: widthPrePrune_m, inRow_m: inRowSpacing_m,
                                                    hBottom_m: heightBottomPrePrune_m, hTop_m: heightTopPrePrune_m,
                                                    kShape: kShape);
            // Infer DBH from area + height using w_auto
            double dbh_cm = InferDbhFromCanopyArea(reffDBHatMaturity_cm, canopyArea, reffArea_m2);

            // Reference volume and masses
            double stemVolume_m3 = StemVolume_Tapered(dbh_cm, heightTopPrePrune_m);
            double branchFraction = ComputeBranchFraction_FromCrownStemRatio(vCrownPre_m3, stemVolume_m3);
            double stemMassPrePrune_kg = stemVolume_m3 * woodDensity_kg_m3;
            double branchMassPrePrune_kg = branchFraction * stemMassPrePrune_kg;

            return stemMassPrePrune_kg + branchMassPrePrune_kg;
        }

        /// <summary>
        /// Estimate the pruning fraction for STRUM’s winter prune using geometry:
        ///   • Branch mass scales with crown-volume ratio:  (V_post / V_pre)^{p_branch}
        ///   • Stem mass   scales with height ratio (topping): (H_post / H_pre)^{p_stem}
        ///
        /// Inputs:
        ///   - MatureWoodMass: pre-prune total woody mass (stem+bark+branches), kg dry
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
                                            TreeShape crownShape,
                                            double heightBottomPrePrune_m,
                                            double heightBottomPostPrune_m,
                                            double heightTopPrePrune_m,
                                            double heightTopPostPrune_m,
                                            double widthPrePrune_m,
                                            double widthPostPrune_m,
                                            double interRowSpacing_m,
                                            double rowSpacing_m,
                                            double matureDbh_cm,
                                            double matureWoodMass
                                            )
        {
            if (matureWoodMass <= 0)
                return 0.0;

              // --- kShape pre/post (same inference used elsewhere)
            double kShapePre = InferCrownShapeCoeff(widthPrePrune_m, heightBottomPrePrune_m, heightTopPrePrune_m);
            double kShapePost = InferCrownShapeCoeff(widthPostPrune_m, heightBottomPostPrune_m, heightTopPostPrune_m);

            // --- Crown volumes and spacing-limited ref (for φ and branch fraction)
            bool freeStanding = IsFreeStanding(widthPrePrune_m, interRowSpacing_m, rowSpacing_m);
            double vCrownPre = CrownVolume_m3(crownShape, freeStanding, widthPrePrune_m, interRowSpacing_m, heightBottomPrePrune_m, heightTopPrePrune_m, kShapePre);
            double vCrownPost = CrownVolume_m3(crownShape, freeStanding, widthPostPrune_m, interRowSpacing_m, heightBottomPostPrune_m, heightTopPostPrune_m, kShapePost);
            
            double stemVolume_m3 = StemVolume_Tapered(matureDbh_cm, heightTopPrePrune_m);

            double branchFraction = ComputeBranchFraction_FromCrownStemRatio(vCrownPre, stemVolume_m3);

            // --- Pre-prune stem/branch masses aligned to estimator split
            double mStemPre = matureWoodMass / (1.0 + branchFraction);
            double mBranchPre = matureWoodMass - mStemPre;

            // --- Scaling factors with zero-division guards
            double fCrown;
            if (vCrownPre > 0.0)
                fCrown = MathUtilities.Bound(Math.Pow(vCrownPost / vCrownPre, Params["P_BranchVolume"]), 0.0, 1.0);
            else
                // No pre-prune crown volume ⇒ no retained branch mass after prune by construction
                fCrown = 0.0;

            double fStemH;
            if (heightTopPrePrune_m > 0.0)
                fStemH = MathUtilities.Bound(Math.Pow(heightTopPostPrune_m / heightTopPrePrune_m, Params["P_StemHeight"]), 0.0, 1.0);
            else
                fStemH = 0.0;

            // --- Post masses & fraction
            double mBranchPost = mBranchPre * fCrown;
            double mStemPost = mStemPre * fStemH;

            double mPost = Math.Max(0.0, mStemPost + mBranchPost);
            return MathUtilities.Bound(1.0 - (mPost / matureWoodMass), 0.0, 1.0);
        }


        /// <summary>
        /// Infers DBH (cm) from canopy *area* only, using a fixed reference canopy area A_typ (m²)
        /// and an anchored size–density scaling:
        ///
        ///     DBH = DBH_typ * (A_user / A_typ)^γ
        ///
        /// • DBH_typ  (cm): DBH for a tree grown at the *reference* canopy area A_typ, set via
        ///                  Params["DbhAtTypicalArea_cm"].  
        /// • A_typ     (m²): fixed reference canopy area per tree (Params["DbhTypicalArea_m2"]).  
        /// • A_user    (m²): effective rectangular hedgerow footprint under current width/spacing
        ///                   (capped across-row).  
        /// • γ          (-): DbhAreaExponent (~0.6), from size–density allometry (Reineke) and
        ///                   consistent with inverted crown-width allometries.
        ///
        /// ------------------------------------------------------------------
        /// WHY HEIGHT IS *NOT* INCLUDED IN THE DBH INFERENCE
        /// ------------------------------------------------------------------
        /// STRUM intentionally excludes canopy height from DBH inference because:
        ///
        /// (1) Height–DBH allometry is highly variable and species-/management-dependent:  
        ///     Re-evaluations of Reineke’s rule show strong divergence in height–DBH slopes among
        ///     species (beech, spruce, pine, oak), indicating poor universality and weak predictive
        ///     power for DBH. [1](https://academic.oup.com/forestscience/article-abstract/51/4/304/4617289)  
        ///
        /// (2) Crown height (vertical dimension) correlates *weakly* with DBH in practice:  
        ///     Urban tree studies and multi-species analyses show crown diameter (width) has strong,
        ///     reliable scaling with DBH, while crown height relationships are weaker because crown
        ///     lifting, pruning, and management decouple height from stem diameter.  
        ///     (Urban crown models: strong DBH → crown width dependence; height only weakly linked.)  
        ///     [2](https://www.fs.usda.gov/nrs/pubs/jrnl/2020/nrs_2020_westfall_001.pdf)[3](https://auf.isa-arbor.com/content/27/4/169)  
        ///
        /// (3) Orchard/hedgerow systems decouple height from DBH even more strongly:  
        ///     In orchard settings, height is not a free allometric variable — it is actively shaped by
        ///     topping, training, and pruning. This makes height a *management parameter*, not a
        ///     biological driver of trunk cross-section.
        ///
        /// (4) The dominant natural determinant of DBH is *horizontal growing space* (area per tree):  
        ///     Stand density theory (Reineke): N ∝ D^{-1.6} ⇒ area per tree A ∝ D^{1.6} ⇒
        /// — DBH ∝ A^{1/1.6} ≈ A^{0.62}, giving a stable and literature-backed γ ≈ 0.6.  
        ///     Crown-width allometry also implies crown area ∝ DBH^{1.4–2.0}, whose inversion gives
        ///     DBH ∝ A^{0.5–0.7}, matching the same exponent range.  
        ///     [1](https://academic.oup.com/forestscience/article-abstract/51/4/304/4617289)[4](https://www.fs.usda.gov/nrs/pubs/gtr/gtr-nrs200-2021_appendixes/gtr_nrs200-2021_appendix13.pdf)  
        ///
        /// (5) Including height would introduce feedback loops and require extra parameters:  
        ///     Because STRUM computes height from pruning/management, using height to infer DBH
        ///     would create a circular dependency and destabilise modelling without adding
        ///     predictive accuracy.
        ///
        /// ------------------------------------------------------------------
        /// RATIONALE FOR THE AREA-ONLY APPROACH
        /// ------------------------------------------------------------------
        /// • Uses well‑supported size–density theory (Reineke) and DBH–crown width scaling.  
        /// • Matches how crown geometry behaves in hedgerows and orchards (width limited; height
        ///   management-driven).  
        /// • Avoids biologically invalid coupling between managed height and DBH.  
        /// • Keeps model transparent: “DBH at A_typ” is a clear, tunable anchor.
        ///
        /// References:  
        /// • Pretzsch and Biber (2005) — species-specific variation in size–density allometry. [1](https://academic.oup.com/forestscience/article-abstract/51/4/304/4617289)  
        /// • USFS i‑Tree Appendix 13 — DBH-based crown width equations (width strongly coupled to DBH).  
        ///   [4](https://www.fs.usda.gov/nrs/pubs/gtr/gtr-nrs200-2021_appendixes/gtr_nrs200-2021_appendix13.pdf)  
        /// • Westfall et al. (2020) — national-scale crown width models: DBH→width dominant; height weaker.  
        ///   [2](https://www.fs.usda.gov/nrs/pubs/jrnl/2020/nrs_2020_westfall_001.pdf)  
        /// • Peper et al. / McPherson et al. — height/crown-height correlations with DBH often weak due to
        ///   pruning in urban trees. [3](https://auf.isa-arbor.com/content/27/4/169)  
        /// </summary>
        private static double InferDbhFromCanopyArea(
                                                    double dbhAtTypicalArea_cm,
                                                    double A_user_m2,
                                                    double A_typ
                                                    )
        {
            double gamma = Params["DbhAreaExponent"];          
            double ratio = (A_typ > 1e-12) ? (A_user_m2 / A_typ) : 1.0;
            return dbhAtTypicalArea_cm * Math.Pow(MathUtilities.Bound(ratio, 1e-6, 1e6), gamma);
        }



        // ----------------------------
        // INTERNAL HELPERS (documented)
        // ----------------------------
        /// <summary>
        /// Computes a **taper‑corrected stem volume** (m³) using basal area × height
        /// scaled by a slenderness‑dependent taper factor. This function returns the
        /// volume of the **main stem only** (excluding branches and bark), providing a
        /// more realistic estimate than a simple cylindrical reference volume.
        /// 
        /// The taper factor reduces the cylinder volume in proportion to tree
        /// slenderness (H/DBH), so tall, narrow trees receive a smaller correction
        /// than short, stout trees. This separates true geometric stem taper from
        /// biological branch allocation, which should be handled independently.
        /// </summary>
        private static double StemVolume_Tapered(double dbh_cm, double height_m)
        {
            double dbh_m = dbh_cm / 100.0;
            double basalArea_m2 = Math.PI * 0.25 * dbh_m * dbh_m;

            double vRef_m3 = basalArea_m2 * height_m;
            double slenderness = height_m / dbh_m;

            double F0 = Params["StemTaperBase"];
            double Amp = 0.10; // (Optional param: "StemTaperAmp")
            double Ftap = F0 + Amp * MathUtilities.Bound(Params["SlendernessRef"] / Math.Max(1e-6, slenderness), 0.0, 1.0);
            Ftap = MathUtilities.Bound(Ftap, Params["StemTaperMin"], Params["StemTaperMax"]);
            return Ftap * basalArea_m2 * height_m;
        }

        /// <summary>
        /// Saturating mapping from crown-to-stem volume ratio R to branch fraction:
        /// BF = bMin + (bMax - bMin) * R^p / (K^p + R^p), bounded in [bMin, bMax].
        /// Robust to very large R; guards zero/near-zero stem volume.
        /// </summary>
        private static double ComputeBranchFraction_FromCrownStemRatio(double vCrown_m3, double vStem_m3)
        {
            double bMin = Params["BranchFractionMin"];
            double bMax = Params["BranchFractionMax"];
            if (!(bMax > bMin)) { bMax = bMin + 1e-3; }  // guard misconfig

            double K = Params.ContainsKey("BF_RatioHalfSat") ? Params["BF_RatioHalfSat"] : 400.0;
            double p = Params.ContainsKey("BF_RatioShape") ? Params["BF_RatioShape"] : 1.2;

            // Guard: if stem volume is tiny, treat as very large ratio (saturate at bMax)
            if (vStem_m3 <= 1e-9) return bMax;

            double R = Math.Max(0.0, vCrown_m3 / vStem_m3);
            double num = Math.Pow(R, p);
            double den = Math.Pow(K, p) + num;
            double frac = bMin + (bMax - bMin) * ((den > 0) ? (num / den) : 0.0);
            return MathUtilities.Bound(frac, bMin, bMax);
        }

        /// <summary>
        /// Crown volume (m³) from explicit TreeShape and spacing regime.
        /// If RowSpacing > WidthPrePrune => free-standing solid; otherwise column.
        /// Uses shape-specific footprint and vertical fill (k) so V = k * A_fp * H_crown.
        /// </summary>
        private static double CrownVolume_m3(
            TreeShape crownShape, bool freeStanding,
            double width_m, double inRow_m,
            double hBottom_m, double hTop_m,
            double kShape)
        {
            double Hc = Math.Max(0.0, hTop_m - hBottom_m);
            if (Hc <= 0.0 || width_m <= 0.0 || inRow_m <= 0.0) return 0.0;
            double Afp = GetFootprintArea_m2(crownShape, freeStanding, width_m, inRow_m);
            return kShape * Afp * Hc;
        }

        /// <summary>
        /// Computes the planform (footprint) area of a tree canopy in m²,
        /// using consistent rectangular effective dimensions and then applying
        /// TreeShape if the tree is free-standing. Row-constrained trees always
        /// return a rectangle (Weff × Leff).
        ///
        /// This version delegates to ComputeEffectiveRectangularFootprint(...)
        /// so packing and width-caps are handled consistently with the DBH logic.
        /// </summary>
        private static double GetFootprintArea_m2(TreeShape shape, bool freeStanding, double width_m, double inRow_m)
        {
            // In crown geometry we *do not* apply across-row caps here.
            // Spacing-limited caps (row, inRow) are applied at call-sites
            // such as when computing vCrownRef.
            var (Leff, Weff) = ComputeEffectiveRectangularFootprint(
                width_m,
                inRow_m,
                row_m: double.PositiveInfinity, // no across-row cap here
                capAcrossRow: false,
                packingRowFrac: 1.0,           // packing handled upstream
                packingInRowFrac: 1.0);

            if (freeStanding)
            {
                // Apply shape on Weff (interpreted as canopy diameter/width).
                switch (shape)
                {
                    case TreeShape.Round:
                        return Math.PI * 0.25 * Weff * Weff;

                    case TreeShape.Square:
                        return Weff * Weff;

                    case TreeShape.Triangular:
                        // Using square base area for triangular planform as before.
                        return Weff * Weff;

                    default:
                        return Weff * Weff;
                }
            }
            else
            {
                // Row-constrained: purely rectangular footprint.
                return Weff * Leff;
            }
        }

        /// <summary>
        /// Infer crown-shape coefficient (k_shape) from canopy aspect (width vs height), 
        /// clamped to [kMin, kMax]. Narrow, tall hedgerow walls tend toward higher k (0.8–0.9).
        /// </summary>
        private static double InferCrownShapeCoeff(double width_m, double hBottom_m, double hTop_m)
        {
             double canopyH = Math.Max(0.0, hTop_m - hBottom_m);
            if (canopyH <= 0 || width_m <= 0) return MathUtilities.Bound(0.80, Params["CrownShapeCoeffMin"], Params["CrownShapeCoeffMax"]);
            double aspect = width_m / canopyH; // <1 = tall & narrow; >1 = squat
                                               // Map aspect to 0.70–0.90 with gentle slope; hedgerow walls yield higher coeffs.
            double coeff = Params["kBase"] + Params["kSlope"] * MathUtilities.Bound(Params["aCenter"] - aspect, -1.0, 1.0);

            return MathUtilities.Bound(coeff, Params["CrownShapeCoeffMin"], Params["CrownShapeCoeffMax"]);
        }

        private static bool IsFreeStanding(double width_m, double inRow_m, double row_m)
        {
            // free-standing only if isolated both across-row and along-row
            return (row_m > width_m) && (inRow_m > width_m);
        }

        /// Core rectangular footprint with consistent packing/caps.
        static (double Leff, double Weff) ComputeEffectiveRectangularFootprint(
            double width_m, double inRow_m, double row_m,
            bool capAcrossRow,        // true for DBH anchor; false if you pre-cap elsewhere
            double packingRowFrac,    // Params["PackingRow_frac"]
            double packingInRowFrac   // Params["PackingInRow_frac"]
        )
        {
            double Leff = Math.Max(1e-6, inRow_m * packingInRowFrac);
            double Wcap = row_m * packingRowFrac;
            double Weff = Math.Max(1e-6, capAcrossRow ? Math.Min(width_m, Wcap) : width_m);
            return (Leff, Weff);
        }

    }
}

