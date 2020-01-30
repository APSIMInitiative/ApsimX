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

        /// <summary>
        /// The primary water submodel, based on H2OLOSS in CENTURY.
        /// </summary>
        private void WaterLoss()
        {
            double fwloss_1 = 1.0;     // A variable in CENTURY, but defaults to 1.0
            double fwloss_2 = 1.0;     // A variable in CENTURY, but defaults to 1.0.Include as needed.
            waterAvailable[0] = 0.0;   // Zero - ed out at the beginning of each pass, as in Century
            waterAvailable[1] = 0.0;
            waterAvailable[2] = 0.0;

            // The following is in Cycle, prior to the H2OLoss call, so moved to here.  The co2 production value(CO2CTR) is calculated in co2eff.f in CENTURY.
            // Rng(icell) % co2_value(ifacet) = Parms(iunit) % effect_of_co2_on_production(ifacet)... wrong... need CO2 coming in, not this.
            // CO2 is not used directly.Instead, the CO2 EFFECT ON PRODUCTION is used, and only that.So if production increases, that increases.   I.E.It is at the landscape unit level.
            if (melt > 0.0)
                ratioWaterPet = melt / potEvap;
            else
                ratioWaterPet = (waterAvailable[2] + globe.precip) / potEvap;    // Excludes irrigation
            // Concludes the part from Cycle prior to the call to H2OLoss.

            double trap = 0.01;
            double add_water = 0.0;
            double @base = 0;
            double strm = 0.0;
            double asimx = 0.0;
            transpiration = 0.0;
            evaporation = 0.0;
            double temp_avg = (globe.maxTemp + globe.minTemp) / 2.0;     // The method used in Century

            // Skipping irrigation, so "inputs" in h2oloss is just precipitation

            // PET remaining stores the energy available to evaporate after each step that saps energy
            petRemaining = potEvap;

            //  Set the snowpack, snow melt, and sublimation
            // call Snow_Dynamics(icell) ... NO, already handled in Weather Update

            // Calculate runoff using Probert et al. (1995).See CENTURY H2OLoss for full citation.
            runoff = Math.Max(0.0, parms.prcpThresholdFraction * (pptSoil - parms.prcpThreshold));
            pptSoil = pptSoil - runoff;    // PPT_SOIL from SNOWCENT is WINPUTS in H2OLOSS.It is the precipitation that reaches the soil, versus that which is converted to snow.

            // Compute bare soil water loss and interception, when there is no snow.
            // (The following line was changed from CENTURY to avoid an equality test on 0.0)
            double avg_live_biomass = 0.0;
            double avg_bare_soil_evap;
            double evl;
            if (snow <= 0.00001)
            {
                avg_live_biomass = totalAgroundLiveBiomass;
                double avg_dead_biomass = totalBgroundLiveBiomass;

                double avg_litter_biomass = (litterStructuralCarbon[surfaceIndex] + litterMetabolicCarbon[surfaceIndex]) * 2.5;

                double sd = avg_live_biomass + avg_dead_biomass;
                // The following were modified to use modern functions, and the 0 test added to avoid infinite results in avg_bare_soil_evap.In short, biomass should not be negative.  Going negative suggests a problem elsewhere, but one debug at a time.
                //    if (sd.gt. 800.0) sd = 800.0
                sd = Math.Min(800.0, sd);
                sd = Math.Max(sd, 0.0);
                double litterd;
                if (avg_litter_biomass > 400.0)
                    litterd = 400.0;
                else
                    litterd = avg_litter_biomass;   // Avg_litter_biomass is not used after this point

                // Calculate canopy interception
                double avg_int = (0.0003 * litterd + 0.0006 * sd) * fwloss_1;   // Not stored long-term until shown as needed
                                                                                // Calculate bare soil evaporation
                avg_bare_soil_evap = 0.5 * Math.Exp((-0.002 * litterd) - (0.004 * sd)) * fwloss_2;  // Not stored long-term so far.
                                                                                                           // Calculate total surface evaporation losses.  The maximum allowable is 0.4 * PET
                evl = Math.Min(((avg_bare_soil_evap + avg_int) * pptSoil), (0.4 * petRemaining));
                evaporation = evaporation + evl;
                // Calculate remaining water to add to soil and potential transpiration as remaining pet
                add_water = pptSoil - evl;
                add_water = Math.Max(0.0, add_water);
                trap = petRemaining - evl;
            }
            else
            {
                avg_bare_soil_evap = 0.0;   // Initialized in H2OLOS, here in if-else, but functionally the same.
                trap = 0.0;
                add_water = 0.0;
                evl = 0.0;
            }

            // Determine potential transpiration water loss(cm/ mon) as a function of precipitation and live biomass.
            // If the temperature is less than 2 degrees C, transpiration will be turned off.
            double pttr;
            if (temp_avg < 2.0)
                pttr = 0.0;
            else
                pttr = petRemaining * 0.65 * (1.0 - Math.Exp(-0.020 * avg_live_biomass)) *
                  ((co2EffectOnProduction[Facet.herb] + co2EffectOnProduction[Facet.shrub] +
                    co2EffectOnProduction[Facet.tree]) / 3.0);  // NOT facet-based.Water loss will be based on the average of the
                                                                     // three facets.
            if (pttr <= trap)
                trap = pttr;
            if (trap <= 0.0)
                trap = 0.01;

            // CENTURY maintains pttr for work on harvest.I don't think we need that here now.  REVISIT
            // Calculate potential evapotranspiration rate from the top soil layer(cm / day).This is not subtracted until
            // after transpiration losses are calculated.
            petTopSoil = petRemaining - trap - evl;
            if (petTopSoil < 0.0)
                petTopSoil = 0.0;

            // Transpire water from added water, then pass it on to soil.  
            transpiration = Math.Min((trap - 0.01), add_water);
            transpiration = Math.Max(0.0, transpiration);
            trap = trap - transpiration;
            add_water = add_water - transpiration;

            // Add water to the soil, including base flow and storm flow
            // stream_water = 0.0
            // base_flow = 0.0;
            // Rng(icell) % stream(1) = 0.0

            for (int soilLayer = 0; soilLayer < nSoilLayers; soilLayer++)
            {
                asmos[soilLayer] = asmos[soilLayer] + add_water;   // Add water to layer
                // Calculate field capacity of soil, drain, and pass excess on to amov
                double afl = soilDepth[soilLayer] * fieldCapacity[soilLayer];
                if (asmos[soilLayer] > afl)
                {
                    amov[soilLayer] = asmos[soilLayer] - afl;
                    asmos[soilLayer] = afl;
                    // If the bottom layer, the remainder is storm flow
                    if (soilLayer == nSoilLayers - 1)
                        strm = amov[soilLayer] * stormFlow;
                }
                else
                {
                    amov[soilLayer] = 0.0;
                }
                add_water = amov[soilLayer];
            }

            // Compute base flow and stream flow
            holdingTank = holdingTank + add_water - strm;
            // Drain base flow fraction from holding tank
            @base = holdingTank * parms.baseFlowFraction;
            holdingTank = holdingTank - @base;
            // Add runoff to stream flow
            // Rng(icell) % stream(1) = strm + base + Rng(icell) % runoff          Commenting out stream flow modeling for now.Not needed for now.
            //Save asmos(1) before transpiration
            asimx = asmos[0];

            // Calculate transpiration water loss  
            double tot = 0.0;
            double tot2 = 0.0;
            double avinj = 0.0;
            double[] awwt = new double[nSoilLayers];
            for (int soilLayer = 0; soilLayer < nSoilLayers; soilLayer++)
            {
                double avail_water = asmos[soilLayer] - wiltingPoint[soilLayer] * soilDepth[soilLayer];
                if (avail_water < 0.0)
                    avail_water = 0.0;
                // Calculate available water weighted by transpiration depth distribution factors
                awwt[soilLayer] = avail_water * parms.soilTranspirationFraction[soilLayer];
                tot = tot + avail_water;
                tot2 = tot2 + awwt[soilLayer];
                // Moving up a copy of this update, which will do no harm, and fix a problem if the following clause is skipped(i.e.,
                //  if tot2.le. 0
                relativeWaterContent[soilLayer] = (asmos[soilLayer] / soilDepth[soilLayer] - wiltingPoint[soilLayer]) /
                      (fieldCapacity[soilLayer] - wiltingPoint[soilLayer]);
#if !G_RANGE_BUG
                if (relativeWaterContent[soilLayer] > vLarge)
                    relativeWaterContent[soilLayer] = vLarge;
                if (relativeWaterContent[soilLayer] < 0.0)
                    relativeWaterContent[soilLayer] = 0.0;
#endif
            }

            // Calculate transpiration water loss(cm/ month)
            trap = Math.Min(tot, trap);

            // CENTURY calculates the layers providing water, based on crops, trees, and total leaf area.Another
            // simplification here.  All layers will provide water, given that they go down to 60 cm.In short,
            // I'm not simulating crops, and trees are a secondary interest.  REVISIT as necessary.
            // But... this was the way it was done in Century until 2003.

            if (tot2 > 0.0)
            {
                for (int soilLayer = 0; soilLayer < nSoilLayers; soilLayer++)
                {
                    avinj = asmos[soilLayer] - wiltingPoint[soilLayer] * soilDepth[soilLayer];
                    if (avinj < 0.0)
                        avinj = 0.0;
                    // Calculate transpiration loss from soil layer, using weighted availabilities
                    double trl = (trap * awwt[soilLayer]) / tot2;
                    if (trl > avinj)
                        trl = avinj;
                    asmos[soilLayer] = asmos[soilLayer] - trl;
                    avinj = avinj - trl;
                    transpiration = transpiration + trl;
                    relativeWaterContent[soilLayer] = (asmos[soilLayer] / soilDepth[soilLayer] - wiltingPoint[soilLayer]) /
                        (fieldCapacity[soilLayer] - wiltingPoint[soilLayer]);
                    if (relativeWaterContent[soilLayer] > vLarge)
                        relativeWaterContent[soilLayer] = vLarge;
                    if (relativeWaterContent[soilLayer] < 0.0)
                        relativeWaterContent[soilLayer] = 0.0;

                    // Calculate water available to plants for growth

                    waterAvailable[0] = waterAvailable[0] + avinj;
#if !G_RANGE_BUG
                    waterAvailable[1] = waterAvailable[1] + avinj;
#endif
                    if (soilLayer <= 1)
                        waterAvailable[2] = waterAvailable[2] + avinj;
                }
            }


            // Sum water available for plants to survive.
#if G_RANGE_BUG
            for (int soilLayer = 0; soilLayer < nSoilLayers; soilLayer++)
                waterAvailable[1] = waterAvailable[1] + avinj;   //// THIS DOES NOT LOOK RIGHT. avinj will have the value for the bottom soil layer (only) - EJZ
#endif
            // Ignoring entry regarding harvesting of crops ... REVISIT ?
            // Minimum relative water content for top layer to evaporate(MANY OF THESE COMMENTS COME DIRECTLY FROM CENTURY)
            double fwlos = 0.25;

            // Fraction of water content between FWLOS and field capacity            
            double evmt = (relativeWaterContent[0] - fwlos) / (1.0 - fwlos);
            if (evmt < 0.01)
                evmt = 0.01;

            // Evaporation loss from layer 1
            double evlos = evmt * petTopSoil * avg_bare_soil_evap * 0.10;
            avinj = asmos[0] - wiltingPoint[0] * soilDepth[0];
            if (avinj < 0.0)
                avinj = 0.0;
            if (evlos > avinj)
                evlos = avinj;
            asmos[0] = asmos[0] - evlos;
            evaporation = evaporation + evlos;

            // Recalculate RWCF(1) to estimate mid - month water content
            double avhsm = (asmos[0] + relativeWaterContent[0] * asimx) / (1.0 + relativeWaterContent[0]);
            relativeWaterContent[0] = (avhsm / soilDepth[0] - wiltingPoint[0]) / (fieldCapacity[0] - wiltingPoint[0]);
            if (relativeWaterContent[0] > vLarge)
            {
                relativeWaterContent[0] = vLarge;
                summary.WriteWarning(this, "Relative water content reset to very large in Water_Loss, layer 1");
            }
            if (relativeWaterContent[0] < 0.0)
            {
                relativeWaterContent[0] = 0.0;
                summary.WriteWarning(this, "Relative water content reset to 0.0 in Water_Loss, layer 1");
            }

            // Update water available pools minus evaporation from top layer
            waterAvailable[0] = waterAvailable[0] - evlos;
            waterAvailable[0] = Math.Max(0.0, waterAvailable[0]);
            waterAvailable[1] = waterAvailable[1] - evlos;
            waterAvailable[1] = Math.Max(0.0, waterAvailable[1]);
            waterAvailable[2] = waterAvailable[2] - evlos;
            waterAvailable[2] = Math.Max(0.0, waterAvailable[2]);

            // Compute annual actual evapotranspiration
            annualEvapotranspiration = annualEvapotranspiration + evaporation + transpiration;

            // From CYCLE, following the call to H2OLoss, a single if-then, which will be incorporated here.
            if (snow > 0.0)
                soilSurfaceTemperature = 0.0;
            // Concludes the material from CYCLE that relates(sort of) to water loss.
        }

        /// <summary>
        /// Nitrogen volatization and leaching routine, building on Century's version.  
        /// Century's version was simplified to only deal with nitrogen.
        /// </summary>
        private void NitrogenLosses()
        {
            // -----------------------------
            // LEACHING FOLLOWS
            double minlch = 18.0;     // Default value for Century, a parameter there.Hardwired here.Revisit if needed.

            // Century includes stream flow.  These cells will be large, and so the importance of stream flow should be limited.
            // Beyond that, it appears to be an accumulator just for output and inspection, so skipped here.
            double @base = 0.0;

            for (int iLayer = 0; iLayer < nSoilLayers; iLayer++)
            {
                nLeached[iLayer] = 0.0;
                double strm = 0.0;
                int nxt = iLayer + 1;   // Call for ilayer = 4 will cause the system to fail, given that nxt would equal 5, which is not defined.

                if ((amov[iLayer] > 0.0) && (mineralNitrogen[iLayer] > 0.0))
                {
                    double linten = Math.Min(1.0 - (minlch - amov[iLayer]) / minlch, 1.0);
                    linten = Math.Max(linten, 0.0);
                    double texture_effect = 0.2 + 0.7 * sand[iLayer];   // Using Savanna's version, rather than Century
                    nLeached[iLayer] = texture_effect * mineralNitrogen[iLayer] * linten;

                    // If at the bottom of the stack of layers, compute storm flow
                    if (iLayer == nSoilLayers - 1)
                        strm = nLeached[iLayer] * stormFlow;
                    mineralNitrogen[iLayer] = mineralNitrogen[iLayer] - nLeached[iLayer];
                    if (nxt < nSoilLayers - 1)
                    {
                        mineralNitrogen[nxt] = mineralNitrogen[nxt] + (nLeached[iLayer] - strm);
                    }
                    else
                    {
                        @base = mineralNitrogen[iLayer] * parms.baseFlowFraction;
                        mineralNitrogen[iLayer] = mineralNitrogen[iLayer] - @base;
                    }
                }
            }

            // Century uses extra soil layers(up to 10), with layer +1 storing base flow.I only have the four layers.
            // Leaching is moving down.  Must store that using a unique approach, relfected in the else section immediately above.
            // Streamflow not simulated.

            // END OF LEACHING
            //-------------------------------- -

            // ---------------------------------
            // Given the relatedness and brevity, adding VOLATILIZATION here
            if (mineralNitrogen[surfaceIndex] > 0.0)
            {
                // Annual fraction of mineral n volatized
                double volex = (parms.annualFractionVolatilizedN * (1.0 / 12.0)) * mineralNitrogen[surfaceIndex];
                mineralNitrogen[surfaceIndex] = mineralNitrogen[surfaceIndex] - volex;
                nitrogenSourceSink = nitrogenSourceSink + volex;
            }
        }

        /// <summary>
        /// The main decomposition model, which is a simplified (or really, very similar version of) version of CENTURY that M. Coughenour adapted for use in Savanna.
        /// </summary>
        private void Decomposition()
        {
            double decodt = 1.0 / 12.0; // 48.0;            // 12 months, 4 calls to decomposition each month, in CENTURY and in SAVANNA.I am going to experiment with a single call, until it proves ineffective.  Multiple calls in a monthly model is not clear.  In Savanna's weekly model it is clearer.
            double fxmca = -0.125;
            double fxmcb = 0.005;
            double fxmxs = 0.35;
            double[] ps1s3 = new double[2];
            ps1s3[0] = 0.003;
            ps1s3[1] = 0.032;
            double[] ps2s3 = new double[2];
            ps2s3[0] = 0.003;
            ps2s3[1] = 0.009;
            double peftxa = 0.25;
            double peftxb = 0.75;
            double rces1 = 13.0;    // Default values for rces in Savanna
            double rces2 = 18.0;
            double rces3 = 8.0;
            double[] prf = new double[nFacets];
            double frac_lignin;
            double demnsom3;
            double demn;
            double somtomin;
            double smintosom;

            // nsoiltp is soil type in Savanna.I have access to that through the structures
            // * ***NOTE that in Savanna, the tree facet is 2, the shrub facet is 3.The opposite here.  * ****

            // Partition some of litter fall among facets.Calculated elsewhere... so commented out.
            // Rng(icell) % herb_cover = 1. - Rng(icell) % woody_cover - Rng(icell) % shrub_cover

            for (int iFacet = 0; iFacet < nFacets; iFacet++)
            {
                // decomp_litter_mix_facets determines mixing, litter flows to facets in proportion to their cover. 1 =in proportion to their cover 0 = only to the current facet.
                switch (iFacet)
                {
                    case Facet.herb:
                        prf[Facet.tree] = facetCover[Facet.tree] * parms.decompLitterMixFacets;
                        prf[Facet.shrub] = facetCover[Facet.shrub] * parms.decompLitterMixFacets;
                        prf[Facet.herb] = 1.0 - prf[Facet.shrub] - prf[Facet.tree];
                        break;
                    case Facet.shrub:
                        prf[Facet.herb] = facetCover[Facet.herb] * parms.decompLitterMixFacets;
                        prf[Facet.tree] = facetCover[Facet.tree] * parms.decompLitterMixFacets;
                        prf[Facet.shrub] = 1.0 - prf[Facet.herb] - prf[Facet.tree];
                        break;
                    case Facet.tree:
                        prf[Facet.herb] = facetCover[Facet.herb] * parms.decompLitterMixFacets;
                        prf[Facet.shrub] = facetCover[Facet.shrub] * parms.decompLitterMixFacets;
                        prf[Facet.tree] = 1.0 - prf[Facet.herb] - prf[Facet.shrub];
                        break;
                }

                // Cut - off litter input into a facet with 0 cover
                if (facetCover[Facet.tree] < 0.01 && iFacet != Facet.tree)
                {
                    prf[Facet.tree] = 0.0;
                    prf[Facet.herb] = prf[Facet.herb] + facetCover[Facet.tree];
                }
                if (facetCover[Facet.shrub] < 0.01 && iFacet != Facet.shrub)
                {
                    prf[Facet.shrub] = 0.0;
                    prf[Facet.herb] = prf[Facet.herb] + facetCover[Facet.shrub];
                }

                // Loop skipped that deals with species.

                // Note that units in this section may be gC/ m ^ 2 or gB/ m ^ 2, so use caution.
                // The array index here is:  1 - Phenological death, 2 - Incremental death, 3 - Herbivory, 4 - Fire(NO IT DOESN'T, RIGHT NOW)
                // [For C#, indices are 0, 1, 2, 3, respectively....]
                //! Fire is not transfered here
                double dthc;
                double dthn;
                if (prf[iFacet] > 0.0)
                {
                    // do itype = 1,3(not used presently)

                    // Fine roots
                    if (deadFineRootCarbon[iFacet] > 0.0)
                        dthc = deadFineRootCarbon[iFacet];         //  * prf(ifacet)
                    else
                        dthc = 0.0;
                    if (deadFineRootNitrogen[iFacet] > 0.0)
                        dthn = deadFineRootNitrogen[iFacet];       // * prf(ifacet)
                    else
                        dthn = 0.0;
                    frac_lignin = ligninFineRoot[iFacet] / fineRootCarbon[iFacet];
                    frac_lignin = Math.Max(0.02, frac_lignin);     // From Century CmpLig.f
                    frac_lignin = Math.Min(0.50, frac_lignin);
                    PartitionLitter(soilIndex, dthc, dthn, frac_lignin);

                    // Standing dead, leaves and stems
                    //    Standing dead is already partitioned in PLANT_DEATH.That includes LEAVES(plus shoots)

                    // Seed
                    if (deadSeedCarbon[iFacet] > 0.0)
                        dthc = deadSeedCarbon[iFacet];            // * prf(ifacet)
                    else
                        dthc = 0.0;
                    if (deadSeedNitrogen[iFacet] > 0.0)
                        dthn = deadSeedNitrogen[iFacet];          // * prf(ifacet)
                    else
                        dthn = 0.0;
                    // Using leaf lignin for simplicity
                    frac_lignin = ligninLeaf[iFacet] / leafCarbon[iFacet];
                    frac_lignin = Math.Max(0.02, frac_lignin);    // From Century CmpLig.f
                    frac_lignin = Math.Min(0.50, frac_lignin);
                    PartitionLitter(surfaceIndex, dthc, dthn, frac_lignin);

                    // Leaf                                          AN ACCUMULATOR ONLY - DEAD LEAF GOES TO STANDING DEAD THEN TO LITTER THEN DECOMPOSITION

                    // Fine branch and stem wood
                    dthc = deadFineBranchCarbon[iFacet];         // * prf(ifacet)
                    dthn = deadFineBranchNitrogen[iFacet];       // * prf(ifacet)
                    _deadTotalFineBranchCarbon = _deadTotalFineBranchCarbon + dthc;     // Accumulating values for shrubs and trees.
                    _deadTotalFineBranchNitrogen = _deadTotalFineBranchNitrogen + dthn;
                    frac_lignin = ligninFineBranch[iFacet] / fineBranchCarbon[iFacet];
                    frac_lignin = Math.Max(0.02, frac_lignin);   // From Century CmpLig.f
                    frac_lignin = Math.Min(0.50, frac_lignin);
                    PartitionLitter(surfaceIndex, dthc, dthn, frac_lignin);

                    // Coarse roots
                    dthc = deadCoarseRootCarbon[iFacet];          // * prf(ifacet)
                    dthn = deadCoarseRootNitrogen[iFacet];        // * prf(ifacet)
                    _deadTotalCoarseRootCarbon = _deadTotalCoarseRootCarbon + dthc;    // Accumulating values for shrubs and trees.Be sure they are partitioned when done.
                    _deadTotalCoarseRootNitrogen = _deadTotalCoarseRootNitrogen + dthn;
                    // Do flows to litter, keeping track of structural and metabolic components
                    frac_lignin = ligninCoarseRoot[iFacet] / coarseRootCarbon[iFacet];
                    frac_lignin = Math.Max(0.02, frac_lignin);     // From Century CmpLig.f
                    frac_lignin = Math.Min(0.50, frac_lignin);
                    PartitionLitter(soilIndex, dthc, dthn, frac_lignin);

                    // Coarse branches
                    dthc = deadCoarseBranchCarbon[iFacet];       // * prf(ifacet)
                    dthn = deadCoarseBranchNitrogen[iFacet];     // * prf(ifacet)
                    _deadTotalCoarseBranchCarbon = _deadTotalCoarseBranchCarbon + dthc;
                    _deadTotalCoarseBranchNitrogen = _deadTotalCoarseBranchNitrogen + dthn;
                    // Do flows to litter, keeping track of structural and metabolic components
                    // Coarse branches cannot be part of dead standing carbon.It could be, but functionally coarse branches are not useful for herbivores, for example
                    frac_lignin = ligninCoarseBranch[iFacet] / coarseBranchCarbon[iFacet];
                    frac_lignin = Math.Max(0.02, frac_lignin);    // From Century CmpLig.f
                    frac_lignin = Math.Min(0.50, frac_lignin);
                    PartitionLitter(surfaceIndex, dthc, dthn, frac_lignin);
                    //  end do  //End type of mortality(e.g., phenology, fire, herbivory)
                } // End if any dead carbon flow
            } // End facet

            // Calculate effect of attributes of cell on decomposition.  These values are in DECOMP in Savanna directly, but I had programmed them drawing from
            // CENTURY, and so will use those values.  They should be essentially directly replacable.
            EffectsOnDecomposition();

            for (int iLayer = surfaceIndex; iLayer <= soilIndex; iLayer++)   // Confirmed two layers
            {
                tnetmin[iLayer] = 0.0;
                tminup[iLayer] = 0.0;
                grossmin[iLayer] = 0.0;
                // SOM1(fast microbial carbon) decomposition goes to som2 and som3, but some is ignored for now.
                // Note:  Som1 n / c ratio is always higher than som2 and some3, always mineralize n
                double eftext = peftxa + peftxb * sand[iLayer];
                double decart = parms.decompRateFastSom[iLayer] * allEffectsOnDecomp * eftext * decodt;
                double tcdec = fastSoilCarbon[iLayer] * decart;

                // Respiration loss
                double p1co2 = 0.0;
                switch (iLayer)
                {
                    case surfaceIndex:
                        p1co2 = 0.6;
                        break;
                    case soilIndex:
                        p1co2 = 0.17 + 0.68 * sand[1];  // Second layer of sand used.Sand is calculated in Savanna.We have that directly, if I am interpreting it correctly.
                        break;
                }
                double co2los = tcdec * p1co2;   // Fixed - June 2014

                // Net flow to SOM3(passive_soil_carbon)
                double fps1s3 = ps1s3[0] + ps1s3[1] * clay[1];    // Second layer of clay used.
                double tosom3net = tcdec * fps1s3 * (1.0 + 5.0 * (1.0 - anerobicEffectOnDecomp));

                // Net flow to SOM2(intermediate_soil_carbon)
                double tosom2net = tcdec - co2los - tosom3net;
                tosom2net = Math.Max(0.0, tosom2net);
                double tndec;
                if (tcdec > 0.0)
                {
                    if (fastSoilCarbon[iLayer] > 0.0001)
                        tndec = tcdec * fastSoilNitrogen[iLayer] / fastSoilCarbon[iLayer];
                    else
                        tndec = 0.0;
                    double demnsom2 = tosom2net / rces2;
                    demnsom3 = tosom3net / rces3;
                    demn = demnsom2 + demnsom3;
                    if (tndec > demn)
                    {
                        somtomin = tndec - demn;
                        smintosom = 0.0;
                    }
                    else
                    {
                        smintosom = demn - tndec;
                        somtomin = 0.0;
                        if (smintosom > 0.00001)
                        {
                            if (smintosom > mineralNitrogen[iLayer])
                            {
                                double fr = mineralNitrogen[iLayer] / smintosom;      // Division by zero prevented two lines above
                                fr = Math.Max(0.0, fr);
                                smintosom = mineralNitrogen[iLayer];
                                smintosom = Math.Max(0.0, smintosom);
                                tcdec = tcdec * fr;
                                tndec = tndec * fr;
                                tosom2net = tosom2net * fr;
                                tosom3net = tosom3net * fr;
                                demnsom2 = demnsom2 * fr;
                                demnsom3 = demnsom3 * fr;
                            }
                        }
                    }

                    // Doing summaries that change the values stored in structures
                    // Note that only fast SOM and SOME have two layers
                    fastSoilCarbon[iLayer] = fastSoilCarbon[iLayer] - tcdec;
                    intermediateSoilCarbon = intermediateSoilCarbon + tosom2net;
                    passiveSoilCarbon = passiveSoilCarbon + tosom3net;
                    fastSoilNitrogen[iLayer] = fastSoilNitrogen[iLayer] - tndec;
                    intermediateSoilNitrogen = intermediateSoilNitrogen + demnsom2;
                    passiveSoilNitrogen = passiveSoilNitrogen + demnsom3;
                    mineralNitrogen[iLayer] = mineralNitrogen[iLayer] + somtomin - smintosom;
                    grossmin[iLayer] = grossmin[iLayer] + tndec;
                    tnetmin[iLayer] = tnetmin[iLayer] + somtomin - smintosom;
                    if (tnetmin[iLayer] < 0.0)
                        tnetmin[iLayer] = 0.0;
                }

                // Metabolic decomposition
                double decmrt = parms.decompRateMetabolicLitter[iLayer] * allEffectsOnDecomp * decodt;
                tcdec = litterMetabolicCarbon[iLayer] * decmrt;
                tcdec = Math.Min(tcdec, litterMetabolicCarbon[iLayer]);
                tcdec = Math.Max(tcdec, 0.0);
                if (litterMetabolicCarbon[iLayer] > 0.0001)
                {
                    tndec = tcdec * litterMetabolicNitrogen[iLayer] / litterMetabolicCarbon[iLayer];
                    tndec = Math.Min(tndec, litterMetabolicNitrogen[iLayer]);
                    tndec = Math.Max(tndec, 0.0);
                }
                else
                {
                    tndec = 0.0;
                }

                // 0.55 is the fraction respired, PMCO2 from Century
                co2los = tcdec * 0.55;
                double tosom1net = tcdec - co2los;
                double demnsom1 = tosom1net / rces1;
                if (tndec > demnsom1)
                {
                    somtomin = tndec - demnsom1;
                    smintosom = 0.0;
                }
                else         // Changed in Sav5... a multi-line addition
                {
                    somtomin = 0.0;
                    smintosom = demnsom1 - tndec;
                    // Reduce decomp if not enough mineral N
                    if (smintosom > mineralNitrogen[iLayer])
                    {
                        double fr = mineralNitrogen[iLayer] / smintosom;
                        fr = Math.Max(0.0, fr);
                        smintosom = mineralNitrogen[iLayer];
                        smintosom = Math.Max(0.0, smintosom);
                        tcdec = tcdec * fr;
                        tndec = tndec * fr;
                        tosom1net = tosom1net * fr;
                        demnsom1 = demnsom1 * fr;
                    }
                }

                // Doing summaries that change the values stored in structures
                // Note that only fast SOM and SOME have two layers
                litterMetabolicCarbon[iLayer] = litterMetabolicCarbon[iLayer] - tcdec;
                fastSoilCarbon[iLayer] = fastSoilCarbon[iLayer] + tosom1net;
                litterMetabolicNitrogen[iLayer] = litterMetabolicNitrogen[iLayer] - tndec;
                fastSoilNitrogen[iLayer] = fastSoilNitrogen[iLayer] + demnsom1;
                mineralNitrogen[iLayer] = mineralNitrogen[iLayer] + somtomin - smintosom;
                grossmin[iLayer] = grossmin[iLayer] + tndec;
                tnetmin[iLayer] = tnetmin[iLayer] + somtomin - smintosom;
                if (tnetmin[iLayer] < 0.0)
                    tnetmin[iLayer] = 0.0;
                // Structural decomposition, goes to SOM1(fast) and SOM2(intermediate)
                frac_lignin = (plantLigninFraction[Facet.herb, iLayer] + plantLigninFraction[Facet.shrub, iLayer] +
                               plantLigninFraction[Facet.tree, iLayer]) / 3.0;
                frac_lignin = Math.Max(0.02, frac_lignin);         // From Century CmpLig.f
                frac_lignin = Math.Min(0.50, frac_lignin);
                double grmin = 0.0;
                double immobil = 0.0;
                TrackLignin(iLayer, 0, ref litterStructuralCarbon[iLayer], ref litterStructuralNitrogen[iLayer], allEffectsOnDecomp, decodt, frac_lignin, ref grmin, ref immobil);
                grossmin[iLayer] = grossmin[iLayer] + grmin;
                tnetmin[iLayer] = tnetmin[iLayer] - immobil;
                tminup[iLayer] = tminup[iLayer] + immobil;

                // Intermediate pool(SOM2) decomposition, which goes to SOM1(fast) and SOM3(passive)
                if (iLayer == soilIndex)
                {
                    double dec2rt = parms.decompRateInterSom * allEffectsOnDecomp * decodt;
                    tcdec = intermediateSoilCarbon * dec2rt;
                    if (tcdec > 0.0)
                    {
                        co2los = tcdec * 0.55 * anerobicEffectOnDecomp;     // 0.55 is fraction respired (p2co2 in Century)
                        // Net flow to SOM3(passive)
                        double fps2s3 = ps2s3[0] + ps2s3[1] * clay[iLayer];  // Using top two soil layers.Note confusion of ilayer.
                        tosom3net = tcdec * fps2s3 * (1.0 + 5.0 * (1.0 - anerobicEffectOnDecomp));
                        // Net flow to SOM1(fast)
                        tosom1net = tcdec - co2los - tosom3net;
                        // N flows
                        demnsom1 = tosom1net / rces1;
                        demnsom3 = tosom3net / rces3;
                        if (intermediateSoilCarbon > 0.0)
                            tndec = tcdec * intermediateSoilNitrogen / intermediateSoilCarbon;
                        else
                            tndec = 0.0;
                        demn = demnsom1 + demnsom3;

                        if (tndec > demn)
                        {
                            somtomin = tndec - demn;
                            smintosom = 0.0;
                        }
                        else
                        {
                            smintosom = demn - tndec;
                            somtomin = 0.0;
                        }

                        // Reduce decomposition if not enough mineral nitrogen to support it via immoblization
                        if (smintosom > 0.0)
                        {
                            if (smintosom > mineralNitrogen[iLayer]) // Check the use of ILAYER.Caution not cited anymore.
                            {
                                double fr = mineralNitrogen[iLayer] / smintosom;  // Checked for 0 above.
                                fr = Math.Max(0.0, fr);
                                smintosom = mineralNitrogen[iLayer];
                                smintosom = Math.Max(0.0, smintosom);
                                tcdec = tcdec * fr;
                                tndec = tndec * fr;
                                tosom1net = tosom1net * fr;
                                tosom3net = tosom3net * fr;
                                demnsom1 = demnsom1 * fr;
                                demnsom3 = demnsom3 * fr;
                            }
                        }

                        // Do the updates to the information in the structure
                        fastSoilCarbon[iLayer] = fastSoilCarbon[iLayer] + tosom1net;
                        intermediateSoilCarbon = intermediateSoilCarbon - tcdec;
                        passiveSoilCarbon = passiveSoilCarbon + tosom3net;

                        intermediateSoilNitrogen = intermediateSoilNitrogen - tndec;
                        mineralNitrogen[iLayer] = mineralNitrogen[iLayer] - smintosom + somtomin;
                        fastSoilNitrogen[iLayer] = fastSoilNitrogen[iLayer] + demnsom1;
                        passiveSoilNitrogen = passiveSoilNitrogen + demnsom3;

                        grossmin[iLayer] = grossmin[iLayer] + tndec;
                        tnetmin[iLayer] = tnetmin[iLayer] + somtomin - smintosom;
                        if (tnetmin[iLayer] < 0.0)
                            tnetmin[iLayer] = 0.0;
                        tminup[iLayer] = tminup[iLayer] + smintosom;
                    }

                    // SOM3(passive component) decomposition
                    double dec3rt = parms.decompRateSlowSom * allEffectsOnDecomp * decodt;
                    tcdec = passiveSoilCarbon * dec3rt;

                    if (tcdec > 0.0)
                    {
                        co2los = tcdec * 0.55 * anerobicEffectOnDecomp;    // 0.55 is fraction respired (p3co2 in Century)
                                                                           // Net flow to SOM1
                        tosom1net = tcdec - co2los;
                        // N flows, mineralization, because N / C of SOM3(1 / rces3 = 7) is less than SOM1(microb nc = 10)(comment from Savanna)
                        if (passiveSoilCarbon > 0.0)
                            tndec = tcdec * passiveSoilNitrogen / passiveSoilCarbon;
                        else
                            tndec = 0.0;
                        demnsom1 = tosom1net / rces1;
                        somtomin = tndec - demnsom1;

                        // Do the updates to the main material in the strucutre.
                        passiveSoilCarbon = passiveSoilCarbon - tcdec;
                        fastSoilCarbon[iLayer] = fastSoilCarbon[iLayer] + tosom1net;
                        fastSoilNitrogen[iLayer] = fastSoilNitrogen[iLayer] + somtomin;
                        passiveSoilNitrogen = passiveSoilNitrogen - tndec;
                        fastSoilNitrogen[iLayer] = fastSoilNitrogen[iLayer] + demnsom1;    //Note two summations for fast soil nitrogen.Odd, but ok.
                        grossmin[iLayer] = grossmin[iLayer] + tndec;
                        tnetmin[iLayer] = tnetmin[iLayer] + somtomin - smintosom;
                        if (tnetmin[iLayer] < 0.0)
                            tnetmin[iLayer] = 0.0;
                        tminup[iLayer] = tminup[iLayer] + smintosom;
                    }
                }  // End if layer = 2

                // Invertebrate decomposition or herbivory of structural litter.C is respired, N is recycled
                double[] decinv = new double[2];
                decinv[iLayer] = parms.decompRateStructuralLitterInverts[iLayer] * tempEffectOnDecomp * decodt;
                double decc = litterStructuralCarbon[iLayer] * decinv[iLayer];
                decc = Math.Min(decc, litterStructuralCarbon[iLayer]);
                double decn;
                if (litterStructuralCarbon[iLayer] > 0.0)
                    decn = decc * litterStructuralNitrogen[iLayer] / litterStructuralCarbon[iLayer];
                else
                    decn = 0.0;
                litterStructuralNitrogen[iLayer] = litterStructuralNitrogen[iLayer] - decn;
                mineralNitrogen[iLayer] = mineralNitrogen[iLayer] + decn;
                tnetmin[iLayer] = tnetmin[iLayer] + decn;
                litterStructuralCarbon[iLayer] = litterStructuralCarbon[iLayer] - decc;
                // An entry in Savanna storing a temporary accumulator appears never to be used, and was skipped.

                // Fine branch(these are ordered, first do fine then do coarse)
                TrackLignin(surfaceIndex, 1, ref _deadTotalFineBranchCarbon, ref _deadTotalFineBranchNitrogen, allEffectsOnDecomp, decodt, 0.25, ref grmin, ref immobil);
                grossmin[iLayer] = grossmin[iLayer] + grmin;
                tnetmin[iLayer] = tnetmin[iLayer] - immobil;
                if (tnetmin[iLayer] < 0.0)
                    tnetmin[iLayer] = 0.0;
                tminup[iLayer] = tminup[iLayer] + immobil;

                // Coarse branch
#if G_RANGE_BUG
                TrackLignin(surfaceIndex, 2, ref deadCoarseBranchCarbon[0], ref deadCoarseBranchNitrogen[0], allEffectsOnDecomp, decodt, 0.25, ref grmin, ref immobil);
#else
                TrackLignin(surfaceIndex, 2, ref _deadTotalCoarseBranchCarbon, ref _deadTotalCoarseBranchNitrogen, allEffectsOnDecomp, decodt, 0.25, ref grmin, ref immobil);
#endif
                grossmin[iLayer] = grossmin[iLayer] + grmin;
                tnetmin[iLayer] = tnetmin[iLayer] - immobil;
                if (tnetmin[iLayer] < 0.0)
                    tnetmin[iLayer] = 0.0;
                tminup[iLayer] = tminup[iLayer] + immobil;

                // Coarse root
#if G_RANGE_BUG
                TrackLignin(soilIndex, 3, ref deadCoarseRootCarbon[0], ref deadCoarseRootNitrogen[0], allEffectsOnDecomp, decodt, 0.25, ref grmin, ref immobil);
#else
                TrackLignin(soilIndex, 3, ref _deadTotalCoarseRootCarbon, ref _deadTotalCoarseRootNitrogen, allEffectsOnDecomp, decodt, 0.25, ref grmin, ref immobil);
#endif
                grossmin[iLayer] = grossmin[iLayer] + grmin;
                tnetmin[iLayer] = tnetmin[iLayer] - immobil;
                if (tnetmin[iLayer] < 0.0)
                    tnetmin[iLayer] = 0.0;
                tminup[iLayer] = tminup[iLayer] + immobil;

                // N gains and losses from system
                if (iLayer == surfaceIndex)
                {
                    // Fraction could include a clay component, being a function of soil texture
                    volitn[iLayer] = parms.fractionGrossNMineralVolatized * grossmin[iLayer];
                    double volex = parms.rateVolatizationMineralN * mineralNitrogen[iLayer] * decodt;
                    volitn[iLayer] = volitn[iLayer] + volex;
                    volitn[iLayer] = Math.Min(volitn[iLayer], mineralNitrogen[iLayer]);
                    volitn[iLayer] = Math.Max(0.0, volitn[iLayer]);
                    mineralNitrogen[iLayer] = mineralNitrogen[iLayer] - volitn[iLayer];
                }
            }

            // Fixation and deposition
            // Using the calculation for base_n_deposition done with Century, already completed and more clear than in Savanna.That is the one that used EPNFA in Century, and calculated here in Misc_Materials, once a year.
            // REMOVEING the symbiotic parameter in the LAND UNITS set.Symbiotic fixation is handled elsewhere.

            double fixa = parms.precipNDeposition[0] * (12.0 / 365.0) + (parms.precipNDeposition[1] * globe.precip);
            fixa = Math.Max(0.0, fixa);

            // tbio is used to sum total green biomass.  "total_aground_live_biomass" will store that variable, so using that.
            double tbio = totalAgroundLiveBiomass * 0.4;
            double biof = fxmca + (fxmcb * tbio);   // * N_DECOMP_LOOPS
            double fxbiom = 1.0 - biof;
            fxbiom = Math.Min(1.0, fxbiom);
            double temper = globe.temperatureAverage;
            double fwdfx;
            if (fxbiom < 0.0 || temper < 7.5)
                fwdfx = 0.0;
            else
                fwdfx = fxbiom;
            double fixs = fxmxs * fwdfx;

            // Total fixation
            fixNit = fixa + fixs;

            // Convert to g / m2 for the facet cover, and add to the soil
            // Rng(icell) % fixnit = Rng(icell) % fixnit
#if G_RANGE_BUG
            mineralNitrogen[soilIndex] = mineralNitrogen[soilIndex] + fixNit;
#else
            mineralNitrogen[surfaceIndex] = mineralNitrogen[surfaceIndex] + fixNit;
#endif
            // Incorporate runoff from the cell.By rights, this should have a minor effect on our large cells.
            // Note different units on runoff here than in SAVANNA.CM in Century, most likely, MM in Savanna.
            runoffN = parms.precipNDeposition[1] * runoff;

            mineralNitrogen[surfaceIndex] = mineralNitrogen[surfaceIndex] - runoffN;
        }        

        /// <summary>
        /// calculate effects on decomposition, including temperature, water, and anerobic effects from precipitation.
        /// The differences in decomposition are the source of very different soil total carbon seen,
        /// and it appears to be because of these coefficients.Error in calculating minimum of temperature effect.
        /// Replacing runoff estimate, which is not well tracked, with drain estimate, which is a variable in Century and now in G-Range.That variable is a general coefficient, DRAIN=1 for sandy soils, DRAIN = 0 for clay soils.
        /// </summary>
        private void EffectsOnDecomposition()
        {
            // CYCLE Contains calculations used in decomposition, after the H2OLoss call, and after plant production has been calculated.
            // I will include those here.  STEMP deals with temperature, and is in productivity.So here we start with TFUNC, line 151.

            // Not using TCALC function call here, just incorporating the function here.
            // !An exponential function is used in CENTURY, dependent upon 4 parameters, stored in an array.
            double x = 30.0;
            double a = parms.temperatureEffectDecomposition[0];
            double b = parms.temperatureEffectDecomposition[1];
            double c = parms.temperatureEffectDecomposition[2];
            double d = parms.temperatureEffectDecomposition[3];
            double normalizer = b + (c / Math.PI) * Math.Atan(Math.PI * d * (x - a));   // Note standardized to 30.
            x = soilSurfaceTemperature;
            double catanf = b + (c / Math.PI) * Math.Atan(Math.PI * d * (x - a));
            tempEffectOnDecomp = Math.Max(0.01, (catanf / normalizer));

            // Two methods of calculating water effect on decomposition in CENTURY.I am incorporating the second, as it appears to perform better in this setting.
            if (ratioWaterPet > 9.0)
                waterEffectOnDecomp = 1.0;
            else
                waterEffectOnDecomp = 1.0 / (1.0 + 30.0 * Math.Exp(-8.5 * ratioWaterPet));   // 1 + demonator prevents division by 0.

            // Anerobic effects on decomposition(ANEROB.F)
            anerobicEffectOnDecomp = 1.0;
            if (ratioWaterPet > parms.anerobicEffectDecomposition[0])
            {
                double xh2o = (ratioWaterPet - parms.anerobicEffectDecomposition[0]) * potEvap * (1.0 - parms.drainageAffectingAnaerobicDecomp);
                if (xh2o > 0.0)
                {
                    double newrat;
                    if (potEvap > 0.0) //  Avoiding division error
                        newrat = parms.anerobicEffectDecomposition[0] + (xh2o / potEvap);
                    else
                        newrat = parms.anerobicEffectDecomposition[0];
                    double slope = (1.0 - parms.anerobicEffectDecomposition[2]) /
                                         (parms.anerobicEffectDecomposition[0] - parms.anerobicEffectDecomposition[1]);  // Parameters, so unlikely to yield division by 0.
                    anerobicEffectOnDecomp = 1.0 + slope * (newrat - parms.anerobicEffectDecomposition[0]);
                }
                if (anerobicEffectOnDecomp < parms.anerobicEffectDecomposition[2])
                    anerobicEffectOnDecomp = parms.anerobicEffectDecomposition[2];
            }

            if (anerobicEffectOnDecomp < 0.0)
                anerobicEffectOnDecomp = 0.0;
            if (anerobicEffectOnDecomp > 1.0)
                anerobicEffectOnDecomp = 1.0;
            if (tempEffectOnDecomp < 0.0)
                tempEffectOnDecomp = 0.0;
            if (tempEffectOnDecomp >= 1.0)
                tempEffectOnDecomp = 1.0;
            if (waterEffectOnDecomp < 0.0)
                waterEffectOnDecomp = 0.0;
            if (waterEffectOnDecomp >= 1.0)
                waterEffectOnDecomp = 1.0;

            // Combining effects of temperature and moisture(CYCLE stores a monthly value ... I don't think I want or need this.  Only monthly cycles are represented here, and the past is the past, and the future unknown.
            allEffectsOnDecomp = tempEffectOnDecomp * waterEffectOnDecomp * anerobicEffectOnDecomp;
            if (allEffectsOnDecomp < 0.0)
                allEffectsOnDecomp = 0.0;    // DEFAC in Savanna DECOMP.F
            if (allEffectsOnDecomp > 1.0)
                allEffectsOnDecomp = 1.0;    // DEFAC in Savanna DECOMP.F
        }

        /// <summary>
        /// A routine that tracks lignin, called DECLIG in Savanna.
        /// </summary>
        /// <param name="iLayer"></param>
        /// <param name="ic"></param>
        /// <param name="woodc"></param>
        /// <param name="woodn"></param>
        /// <param name="defac"></param>
        /// <param name="decodt"></param>
        /// <param name="frlig"></param>
        /// <param name="tndec"></param>
        /// <param name="smintosom"></param>
        private void TrackLignin(int iLayer, int ic, ref double woodc, ref double woodn, double defac, double decodt, double frlig, ref double tndec, ref double smintosom)
        {
            double[] ps1co2 = new double[2];
            ps1co2[0] = 0.45;
            ps1co2[1] = 0.55;
            double rsplig = 0.3;
            double rces1 = 13.0;   // Default values for rces in Savanna
            double rces2 = 18.0;

            double eflig = Math.Exp(-3.0 * frlig);
            double decrt = 0.0;

#if !G_RANGE_BUG
            // EJZ - The original looks wrong to me. These are effectively output variables, and should probably
            // be assigned a value of 0, rather than being left unaltered.
            tndec = 0.0;
            smintosom = 0.0;
#endif
            switch (ic)
            {
                case 0:
                    decrt = parms.decompRateStructuralLitter[iLayer] * defac * eflig * decodt;
                    break;
                case 1:
                    decrt = parms.decompRateFineBranch * defac * eflig * decodt;
                    break;
                case 2:
                    decrt = parms.decompRateCoarseBranch * defac * eflig * decodt;
                    break;
                case 3:
                    decrt = parms.decompRateCoarseRoot * defac * eflig * decodt;
                    break;
            }

            double tcdec = woodc * decrt;

            if (tcdec > 0.0)
            {
                double tosom2gross = tcdec * frlig;
                double co2los2 = tosom2gross * rsplig;
                double tosom2net = tosom2gross - co2los2;

                double tosom1gross = tcdec - tosom2gross;
                double co2los1 = tosom1gross * ps1co2[iLayer];
                double tosom1net = tosom1gross - co2los1;

                if (woodc > 0.0)
                    tndec = tcdec * woodn / woodc;
                else
                    tndec = 0.0;

                double demnsom1 = tosom1net / rces1;
                double demnsom2 = tosom2net / rces2;
                double demnt = demnsom1 + demnsom2;

                if (demnt > tndec)
                    smintosom = demnt - tndec;
                else
                    smintosom = 0.0;

                if (smintosom > 0.0)
                {
                    if (smintosom > mineralNitrogen[iLayer]) // NOTE: Possible error, mineral nitrogen is soil layers, ilayer is surface versus soil.Won't break program, but logic may not work.
                    {
                        mineralNitrogen[iLayer] = Math.Max(mineralNitrogen[iLayer], 0.0);
                        double fr = mineralNitrogen[iLayer] / smintosom;
                        smintosom = mineralNitrogen[iLayer];
                        tcdec = tcdec * fr;
                        tndec = tndec * fr;
                        tosom1net = tosom1net * fr;
                        tosom2net = tosom2net * fr;
                    }
                }

                fastSoilCarbon[iLayer] = fastSoilCarbon[iLayer] + tosom1net;
                intermediateSoilCarbon = intermediateSoilCarbon + tosom2net;
                woodc = woodc - tcdec;

                double demn1 = tosom1net / rces1;
                double demn2 = tosom2net / rces2;
                fastSoilNitrogen[iLayer] = fastSoilNitrogen[iLayer] + demn1;
                intermediateSoilNitrogen = intermediateSoilNitrogen + demn2;
                woodn = woodn - tndec;
                mineralNitrogen[iLayer] = mineralNitrogen[iLayer] - smintosom;
            }
        }
    }
}
