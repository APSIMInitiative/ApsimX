using System;

namespace Models
{
    using Models.Core;
    using Models.Interfaces;

    public partial class G_Range : Model, IPlant, ICanopy, IUptake
    {
        /// <summary>
        /// Calculate potential production and the attributes that go into potential production.
        /// This routine also includes a calculation of surface soil temperatures.
        /// POTPROD uses a crop system and forest system approach.
        /// (In CENTURY, cancvr is passed at each call, but I am using the structured approach)
        /// </summary>
        private void PotentialProduction()
        {
            double tmin = globe.minTemp;
            double tmax = globe.maxTemp;
            // Calculate temperature... in CENTURY, this uses three approaches(CURSYS), one for FOREST, on for SAVANNA, and one for GRASSLAND.
            //                           Forest isn't represented here.  I am seeking to avoid the SAVANNA/GRASSLAND split.  
            // Live biomass
            double surface_litter_biomass = (litterStructuralCarbon[surfaceIndex] +
                                             litterMetabolicCarbon[surfaceIndex]) * 2.25;    // Averaged over types

            double avg_live_biomass = 0.0;
            double avg_wood_biomass = 0.0;
            double standing_dead_biomass = 0.0;
            double temp_min_melt;
            double temp_max_melt;
            for (int iFacet = 0; iFacet < nFacets; iFacet++)
            {
                switch (iFacet)
                {
                    case Facet.herb:
                        avg_live_biomass = (leafCarbon[iFacet] + seedCarbon[iFacet]) * 2.5;
                        avg_wood_biomass = 0.0;
                        standing_dead_biomass = deadStandingCarbon[iFacet] * 2.5;
                        break;
                    case Facet.shrub:
                        avg_live_biomass = (leafCarbon[iFacet] + seedCarbon[iFacet]) * 2.5;
                        avg_wood_biomass = (shrubCarbon[WoodyPart.coarseBranch] + shrubCarbon[WoodyPart.fineBranch]) * 2.0;
                        standing_dead_biomass = deadStandingCarbon[iFacet] * 2.5;
                        break;
                    case Facet.tree:
                        avg_live_biomass = (leafCarbon[iFacet] + seedCarbon[iFacet]) * 2.5;
                        avg_wood_biomass = (treeCarbon[WoodyPart.coarseBranch] + treeCarbon[WoodyPart.fineBranch]) * 2.0;
                        standing_dead_biomass = deadStandingCarbon[iFacet] * 2.5;
                        break;
                }
                // ** Calculating soil surface temperatures
                // Century makes a call to a routine to calculate soil surface temperature.  It is only used here, and this is brief, so I will merge that into here.
                // Total biomass
                double biomass = avg_live_biomass + standing_dead_biomass +
                    (surface_litter_biomass * parms.litterEffectOnSoilTemp);
                biomass = Math.Min(biomass, parms.maximumBiomassSoilTemp);
                double woody_biomass = Math.Min(avg_wood_biomass, 5000.0);  // Number hardwired in CENTURY

                // Maximum temperature with leaf shading
                double temp_max_leaf = tmax + (25.4 / (1.0 + 18.0 * Math.Exp(-0.20 * tmax))) *  // 1 + avoids division by 0.
                                        (Math.Exp(parms.biomassEffectOnMaxSoilTemp * biomass) - 0.13);
                // Minimum temperature with leaf shading
                double temp_min_leaf = tmin + (parms.biomassEffectOnMinSoilTemp * biomass) - 1.78;

                // Maximum temperature with wood shading
                double temp_max_wood = tmax + (25.4 / (1.0 + 18.0 * Math.Exp(-0.20 * tmax))) *  // 1 + avoids division by 0.
                                        (Math.Exp(parms.biomassEffectOnMaxSoilTemp * 0.1 * woody_biomass) - 0.13);
                // Minimum temperature with wood shading
                double temp_min_wood = tmin + (parms.biomassEffectOnMinSoilTemp * 0.1 * woody_biomass) - 1.78;

                double maximum_soil_surface_temperature = Math.Min(temp_max_leaf, temp_max_wood);
                double minimum_soil_surface_temperature = Math.Max(temp_min_leaf, temp_min_wood);

                // Let soil surface temperature be affected by day length
                if (dayLength < 12.0)
                    temp_min_melt = ((12.0 - dayLength) * 3.0 + 12.0) / 24.0;
                else
                    temp_min_melt = ((12.0 - dayLength) * 1.2 + 12.0) / 24.0;
                temp_min_melt = Math.Min(0.95, temp_min_melt);
                temp_min_melt = Math.Max(0.05, temp_min_melt);
                temp_max_melt = 1.0 - temp_min_melt;
                soilSurfaceTemperature = temp_max_melt * maximum_soil_surface_temperature +
                                                  temp_min_melt * minimum_soil_surface_temperature;
            }
            // End calculating soil surface temperatures

            // Calculate potential production, using POTCRP as a guide, but not including cropping types.An edit included POTTREE as well, but mostly already present in the model.
            Potential();
        }

        private void Potential()
        {
            double h2ogef;
            double[] prop_live_per_layer = new double[nLayers]; // Filling the arrays in case something gets skipped below. (In C#, initialised to 0)

            // From Century, the value for potential plant production is now calculated from the equation of a line whose intercept changes depending on water
            // content based on soil type.
            if (potEvap >= 0.01)
                h2ogef = (waterAvailable[0] + globe.precip) / potEvap; // Irrigation was not included, unlike CENTURY
            else
                h2ogef = 0.01;

            double water_content = fieldCapacity[0] - wiltingPoint[0];

            // Doing PPRDWC in this subroutine, rather than another call to a function
            // Regression points were confirmed as 0.0, 1.0, and 0.8(using the old method in Century to reduce parameters)
            double intercept = parms.pptRegressionPoints[0] + (parms.pptRegressionPoints[1] * water_content);
            double slope;
            if (parms.pptRegressionPoints[2] != intercept)
                slope = 1.0 / (parms.pptRegressionPoints[2] - intercept);
            else
                slope = 1.0;

            // Do the correction, altering h2ogef based on these corrections.h2ogef is both x (in) and pprdwc(out) in the PPRDWC function in CENTURY
            h2ogef = 1.0 + slope * (h2ogef - parms.pptRegressionPoints[2]);
            if (h2ogef > 1.0)
                h2ogef = 1.0;
            else if (h2ogef < 0.01)
                h2ogef = 0.01;

            // Calculate how much live aboveground biomass is in each vegetation layer, and use that as a guide to distribute production
            // Get the total cover, to allow ignoring bare ground.

            double total_cover = facetCover[Facet.herb] + facetCover[Facet.shrub] + facetCover[Facet.tree];
            if (total_cover > 0.000001)
            {
                // Using an approach that provides production estimates for each facet independently.
                // Also accounting for not looking at bare ground.
                prop_live_per_layer[Layer.herb] = 1.0;
                prop_live_per_layer[Layer.herbUnderShrub] = facetCover[Facet.shrub] / total_cover;
                prop_live_per_layer[Layer.herbUnderTree] = facetCover[Facet.tree] / total_cover;
                double w_cover = facetCover[Facet.shrub] + facetCover[Facet.tree];
                if (w_cover > 0.000001)
                {
                    prop_live_per_layer[Layer.shrub] = 1.0;
                    prop_live_per_layer[Layer.shrubUnderTree] = facetCover[Facet.tree] / w_cover;
                    prop_live_per_layer[Layer.tree] = 1.0;
                }

                int ifacet = Facet.herb;
                double aisc = 0.0;
                double woody_cover = 0.0;
                for (int iLyr = 0; iLyr < nLayers; iLyr++)
                {
                    // Compute shading modifier.First, set woody cover
                    switch (iLyr)
                    {
                        case Layer.herb:
                            ifacet = Facet.herb;
                            woody_cover = 0.0;
                            aisc = 0.0;
                            break;
                        case Layer.herbUnderShrub:  // NOTE that in the following, spatial cover is substituting for what would normally be a density measure of leaves at any one place.Use LAI instead ? No, canopy cover is used in Century.Across the entire landscape cell, this measure is appropriate.
                            ifacet = Facet.herb;
                            woody_cover = facetCover[Facet.shrub];  // Facet cover should never go to 0, as it is counter to the definition.Perhaps include a catch-all in MISC_MATERIAL that makes sure a few shrubs are present, a few trees are present, in any cell.
                            if (shrubCarbon[WoodyPart.leaf] < 0.00001)
                                aisc = 0.0;
                            else
                                aisc = 5.0 * Math.Exp(-0.0035 * (shrubCarbon[WoodyPart.leaf] * 2.5) / woody_cover + 0.00000001);  // Shading by seeds ignored.
                            break;
                        case Layer.herbUnderTree:
                            ifacet = Facet.herb;
                            woody_cover = facetCover[Facet.tree];
                            if (treeCarbon[WoodyPart.leaf] < 0.00001)
                                aisc = 0.0;
                            else
                                aisc = 5.0 * Math.Exp(-0.0035 * (treeCarbon[WoodyPart.leaf] * 2.5) / woody_cover + 0.00000001);
                            break;
                        case Layer.shrub:
                            ifacet = Facet.shrub;
                            woody_cover = 0.0;   // Assumes that shrubs aren't shaded, although they do shade themselves.  But really, all these types shade themselves.  Perhaps adjust.
                            aisc = 0.0;
                            break;
                        case Layer.shrubUnderTree:
                            ifacet = Facet.shrub;
                            woody_cover = facetCover[Facet.tree];
                            if (treeCarbon[WoodyPart.leaf] < 0.00001)
                                aisc = 0.0;
                            else
                                aisc = 5.0 * Math.Exp(-0.0035 * (treeCarbon[WoodyPart.leaf] * 2.5) / woody_cover + 0.00000001);
                            break;
                        case Layer.tree:
                            ifacet = Facet.tree;
                            woody_cover = 0.0;    // Assumes trees don't shade themselves.
                            aisc = 0.0;
                            break;
                    }
                    double shading_modifier = (1.0 - woody_cover) + (woody_cover * (aisc / (aisc + 1.0)));
                    // I suspect the modifier should be less than 1.Check for this ?

                    // Estimate plant production
                    if (soilSurfaceTemperature > 0.0)
                    {
                        // Calculate temperature effect on growth
                        // Account for removal of litter effects on soil temperature as it drives plant production
                        // Century recalculates min, max, and average soil surface temperatures.I have those already, so using the existing.

                        // Century uses a function call, but I will collapse it to here.  X is ctemp or average soil surface temperature.  a,b,c,d are PPDF values
                        double a = parms.temperatureProduction[0];
                        double b = parms.temperatureProduction[1];
                        double c = parms.temperatureProduction[2];
                        double d = parms.temperatureProduction[3];

                        double frac = (b - soilSurfaceTemperature) / (b - a);   // Based mostly on parameters, so division by 0 unlikely.
                        potentialProduction = 0.0;
                        // The following appears appropriate for both herbs and woodies, based on Century documentation.
                        if (frac > 0.0)
                            potentialProduction = Math.Exp(c / d * (1.0 - Math.Pow(frac, d))) * Math.Pow(frac, c);

                        // Calculate the potential effect of standing dead on plant growth, the effect of physical obstruction of litter and standing dead
                        double bioc = deadStandingCarbon[ifacet] + 0.1 * litterStructuralCarbon[0];
                        if (bioc <= 0.0)
                            bioc = 0.01;
                        if (bioc > parms.maximumBiomassSoilTemp)
                            bioc = parms.maximumBiomassSoilTemp;
                        double bioprd = 1.0 - (bioc / (parms.standingDeadProductionHalved + bioc));   // Parameters, so division by 0 unlikely.

                        // Calculate the effect of the ratio of live biomass to dead biomass on the reduction of potential growth rate.The intercept of this equation(highest negative effect of dead plant biomass) is equal to bioprd when the ratio is zero.
                        double temp1 = (1.0 - bioprd);
                        double temp2 = temp1 * 0.75;
                        double temp3 = temp1 * 0.25;
                        double ratlc = leafCarbon[Facet.herb] / bioc;  // Logic above prevents 0.
                        double biof = 0.0;
                        if (ratlc <= 1.0)
                            biof = bioprd + (temp2 * ratlc);
                        if (ratlc > 1.0 && ratlc <= 2.0)
                            biof = (bioprd + temp2) + temp3 * (ratlc - 1.0);
                        if (ratlc > 2.0)
                            biof = 1.0;

                        totalPotProduction[iLyr] = Shortwave() * parms.radiationProductionCoefficient *
                                potentialProduction * h2ogef * biof * shading_modifier * co2EffectOnProduction[ifacet] *
                                prop_live_per_layer[iLyr];
                        // if (Rng(icell) % total_pot_production(2).gt. 10000.0) then
                        //  write(*, *) 'ZZ TOT_POT_PROD: ', icell, ilyr, month, Rng(icell) % total_pot_production(2), total_biomass, total_cover, &
                        // prop_live_per_layer(ilyr)
                        //  write(*, *) 'ZZ          S_W: ', icell, ilyr, month, shortwave(icell)
                        //  write(*, *) 'ZZ       h2ogef: ', icell, ilyr, month, h2ogef
                        //  write(*, *) 'ZZ         biof: ', icell, ilyr, month, biof
                        //  write(*, *) 'ZZ          S_M: ', icell, ilyr, month, shading_modifier
                        //  write(*, *) 'ZZ         PLPL: ', icell, ilyr, month, prop_live_per_layer(ilyr)
                        //  write(*, *) 'ZZ      RAD_P_C: ', icell, ilyr, month, Parms(iunit) % radiation_production_coefficient
                        //  write(*, *) 'ZZ          P_P: ', icell, ilyr, month, Rng(icell) % potential_production
                        //  write(*, *) 'ZZ          CO2: ', icell, ilyr, month, Rng(icell) % co2_effect_on_production(ifacet)
                        //end if

                        // Dynamic carbon allocation to compute root / shoot ratio
                        if (totalPotProduction[iLyr] > 0.0)
                        {
                            // call Crop_Dynamic_Carbon(Rng(icell)% root_shoot_ratio, fracrc)      I WON'T BE INCLUDING DYNAMIC CARBON ALLOCATION BETWEEN SHOOTS AND ROOTS FOR NOW.  TOO COMPLEX, MUST SIMPLIFY.
                            double fracrc = parms.fractionCarbonToRoots[ifacet];   // Gross simplifaction, but required for completion.
                            // Change root shoot ratio based on effects of co2
                            // The following can't be the same as effect on production.  Distruptive.  I will turn this off for now.  Specific to crops and trees, incidentally.
                            // rootShootRatio[ifacet] = rootShootRatio[ifacet] * co2EffectOnProduction[ifacet];

                            // Allocate production  
                            belowgroundPotProduction[iLyr] = totalPotProduction[iLyr] * fracrc;
                            abovegroundPotProduction[iLyr] = totalPotProduction[iLyr] - belowgroundPotProduction[iLyr];

                            // Restrict production due to that taken by grazers
                            GrazingRestrictions(ifacet, iLyr);

                            // Update accumulators and compute potential C production
                            // Skipping this for now.They seem to be detailed accumulators of C over the entire run, which may be of interest, but not now.
                        }
                        else
                        {
                            // No production this month... total potential production is 0.0
                            totalPotProduction[iLyr] = 0.0;
                            abovegroundPotProduction[iLyr] = 0.0;
                            belowgroundPotProduction[iLyr] = 0.0;
                        }
                    }
                    else
                    {
                        // No production this month... too cold
                        totalPotProduction[iLyr] = 0.0;
                        abovegroundPotProduction[iLyr] = 0.0;
                        belowgroundPotProduction[iLyr] = 0.0;
                    }
                }
            }
            else
            {
                // No production this month... there is either zero cover or zero biomass
                for (int iLyr = 0; iLyr < nLayers; iLyr++)
                {
                    prop_live_per_layer[iLyr] = 0.0;      // Nothing alive, so no production.  Or no facet cover, so no production
                    totalPotProduction[iLyr] = 0.0;       // Note this is biomass
                    abovegroundPotProduction[iLyr] = 0.0; // Note this is biomass
                    belowgroundPotProduction[iLyr] = 0.0; // Note this is biomass.Divide by 2.5 or 2 for carbon.
                }
            }
        }

        private void GrazingRestrictions(int ifacet, int iLyr)
        {
            // Only to make things more compressed
            double agrd = abovegroundPotProduction[iLyr];
            double bgrd = belowgroundPotProduction[iLyr];
            double fracrmv = fractionLiveRemovedGrazing;
            double rtsht;
            if (agrd > 0.0)
                rtsht = bgrd / agrd;
            else
                rtsht = 0.0;
            double graze_mult = parms.grazingEffectMultiplier;
            double bop;

            switch (parms.grazingEffect)      // Grazing effects 0 through 6 come right from CENTURY
            {
                //    case (0)! Grazing has no direct effect on production.Captured in 'case default'
                case 1:     // Linear impact of grazing on aboveground potential production
                    agrd = (1 - (2.21 * fracrmv)) * agrd;
                    if (agrd < 0.02)
                        agrd = 0.02;
                    bgrd = rtsht * agrd;
                    break;
                case 2:    // Quadratic impact of grazing on aboveground potential production and root:shoot ratio
                    agrd = (1 + (2.6 * fracrmv - (5.83 * Math.Pow(fracrmv, 2.0)))) * agrd;
                    if (agrd < 0.02)
                        agrd = 0.02;
                    bop = rtsht + 3.05 * fracrmv - 11.78 * Math.Pow(fracrmv, 2.0);
                    if (bop <= 0.01)
                        bop = 0.01;
                    bgrd = agrd * bop;
                    break;
                case 3:    // Quadratic impact of grazing of grazing on root:shoot ratio
                    bop = rtsht + 3.05 * fracrmv - 11.78 * Math.Pow(fracrmv, 2.0);
                    if (bop <= 0.01)
                        bop = 0.01;
                    bgrd = agrd * bop;
                    break;
                case 4:    // Linear impact of grazing on root:shoot ratio
                    bop = 1 - (fracrmv * graze_mult);
                    bgrd = agrd * bop;
                    break;
                case 5:    // Quadratic impact of grazing on aboveground potential production and linear impact on root: shoot ratio
                    agrd = (1 + 2.6 * fracrmv - (5.83 * Math.Pow(fracrmv, 2.0))) * agrd;
                    if (agrd < 0.02)
                        agrd = 0.02;
                    bop = 1 - (fracrmv * graze_mult);
                    bgrd = agrd * bop;
                    break;
                case 6:    // Linear impact of grazing on aboveground potential production and root:shoot ratio
                    agrd = (1 + 2.21 * fracrmv) * agrd;
                    if (agrd < 0.02)
                        agrd = 0.02;
                    bop = 1 - (fracrmv * graze_mult);
                    bgrd = agrd * bop;
                    break;
                default:
                    // Do nothing.  grazing_effect = 0, and so values will be read and re-assigned without modification
                    // This routine modifies the effects of grazing on above-and belowground potential production.It removes no forage.
                    break;
            }
            abovegroundPotProduction[iLyr] = agrd;
            belowgroundPotProduction[iLyr] = bgrd;
            totalPotProduction[iLyr] = agrd + bgrd;
            if (agrd > 0.0)
                rootShootRatio[ifacet] = bgrd / agrd;
            else
                rootShootRatio[ifacet] = 1.0;
        }

        private void HerbGrowth()
        {
            double tolerance = 1.0E-30;
            double[] uptake;
            double[] cfrac = new double[nWoodyParts];
            double amt;
            double[] mcprd = new double[2];
            mcprd[surfaceIndex] = 0.0;
            mcprd[soilIndex] = 0.0;

            maintainRespiration[Facet.herb] = 0.0;
            respirationFlows[Facet.herb] = 0.0;

            // Century includes flags set in the schedular to turn growth on or off.  I don't want to do that.  
            // I wish to use degree - days, or topsoil available water to pet ratio, or temperature limits
            // * **USE PHENOLOGY HERE?  USE DORMANCY INSTEAD? DAY LENGTH(which is in RNG)... no, not for herbs.
            if (ratioWaterPet > 0.0 && globe.minTemp > 3.0 &&
                 phenology[Facet.herb] < 3.999)
            {
                double rimpct;
                // Calculate effect of root biomass on available nutrients
                if ((parms.rootInterceptOnNutrients * fineRootCarbon[Facet.herb] * 2.5) > 33.0)
                    rimpct = 1.0;
                else
                    rimpct = (1.0 - parms.rootInterceptOnNutrients *
                            Math.Exp(-parms.rootEffectOnNutrients * fineRootCarbon[Facet.herb] * 2.5));

                // Calculate carbon fraction above and belowground
                if (totalPotProduction[Layer.herb] + totalPotProduction[Layer.herbUnderShrub] +
                        totalPotProduction[Layer.herbUnderTree] > 0.0)
                    cfrac[ABOVE] = abovegroundPotProduction[Facet.herb] / (totalPotProduction[Layer.herb] +
                                   totalPotProduction[Layer.herbUnderShrub] + totalPotProduction[Layer.herbUnderTree]);  // Using ABOVE and BELOW in CFRAC when it is dimensioned as woody parts, but that is ok.
                else
                    cfrac[ABOVE] = 0.0;
                cfrac[BELOW] = 1.0 - cfrac[ABOVE];

                double availableNitrogen = 0.0;
                for (int iLayer = 0; iLayer < nSoilLayers; iLayer++)  // Nutrients will be used to calculate mineral availablity for all layers, since they are only 15 cm each, 4, across the globe.Recent CENTURY uses a parameter(CLAYPG) here.
                {
                    availableNitrogen = availableNitrogen + mineralNitrogen[iLayer];
                }

                // Determine actual production, restricted based on carbon to nitrogen ratios.Note CFRAC &UPTAKE are arrays.
                RestrictProduction(Facet.herb, 2, availableNitrogen, rimpct, cfrac, out uptake);

                // If growth occurs ...                       (Still stored in potential production ... move to actual production?)
                // Get average potential production
                double avg_total_pot_prod_carbon = ((totalPotProduction[Layer.herb] + totalPotProduction[Layer.herbUnderShrub] +
                                  totalPotProduction[Layer.herbUnderTree]) / 3.0) * 0.4;
                double avg_aground_pot_prod_carbon = ((abovegroundPotProduction[Layer.herb] +
                                  abovegroundPotProduction[Layer.herbUnderShrub] + abovegroundPotProduction[Layer.herbUnderTree]) / 3.0) * 0.4;
                if (avg_total_pot_prod_carbon > 0.0)  //  Wouldn't this be production limited by nitrogen?
                {
                    // Compute nitrogen fixation which actually occurs and add to accumulator
                    nitrogenFixed[Facet.herb] = nitrogenFixed[Facet.herb] + plantNitrogenFixed[Facet.herb];
                    // Accumulators skipped for now.Will be added as needed (Century includes so many it is bound to confuse) EUPACC SNFXAC  NFIXAC TCNPRO

                    // Maintenance respiration calculations
                    // Growth of shoots
                    double agfrac;
                    if (avg_total_pot_prod_carbon > 0.0)
                        agfrac = avg_aground_pot_prod_carbon / avg_total_pot_prod_carbon;
                    else
                        agfrac = 0.0;

                    mcprd[ABOVE] = (totalPotProdLimitedByN[Layer.herb] + totalPotProdLimitedByN[Layer.herbUnderShrub] +
                                    totalPotProdLimitedByN[Layer.herbUnderTree]) * agfrac;
                    double resp_flow_shoots = mcprd[ABOVE] * parms.fractionNppToRespiration[Facet.herb];
                    respirationFlows[Facet.herb] = respirationFlows[Facet.herb] + resp_flow_shoots;
                    leafCarbon[Facet.herb] = leafCarbon[Facet.herb] +
                           ((1.0 - parms.fractionAgroundNppToSeeds[Facet.herb]) * mcprd[ABOVE]);    // Translated from CSHED call and complexity of CSRSNK and BGLCIS.  Not sure if interpretted correctly.
                    seedCarbon[Facet.herb] = seedCarbon[Facet.herb] +
                            (parms.fractionAgroundNppToSeeds[Facet.herb] * mcprd[ABOVE]);           // Translated from CSHED call and complexity of CSRSNK and BGLCIS.  Not sure if interpretted correctly.
                    carbonSourceSink = carbonSourceSink - mcprd[ABOVE];

                    // Growth of roots
                    double bgfrac = 1.0 - agfrac;
                    mcprd[BELOW] = ((totalPotProdLimitedByN[Layer.herb] + totalPotProdLimitedByN[Layer.herbUnderShrub] +
                                     totalPotProdLimitedByN[Layer.herbUnderTree]) / 3.0) * bgfrac;
                    double resp_flow_roots = mcprd[BELOW] * parms.fractionNppToRespiration[Facet.herb];
                    respirationFlows[Facet.herb] = respirationFlows[Facet.herb] + resp_flow_roots;
                    fineRootCarbon[Facet.herb] = fineRootCarbon[Facet.herb] + mcprd[BELOW];     // Translated from CSHED call and complexity of CSRSNK and BGLCIS.  Not sure if interpretted correctly.
                    carbonSourceSink = carbonSourceSink - mcprd[BELOW];
                    // Store maintenance respiration to storage pool
                    maintainRespiration[Facet.herb] = maintainRespiration[Facet.herb] + respirationFlows[Facet.herb];
                    carbonSourceSink = carbonSourceSink - respirationFlows[Facet.herb];

                    // Maintenance respiration fluxes reduce maintenance respiration storage pool
                    double resp_temp_effect = 0.1 * Math.Exp(0.07 * globe.temperatureAverage);
                    resp_temp_effect = Math.Min(1.0, resp_temp_effect);
                    resp_temp_effect = Math.Max(0.0, resp_temp_effect);
                    double[] cmrspflux = new double[2];
                    cmrspflux[ABOVE] = parms.herbMaxFractionNppToRespiration[ABOVE] * resp_temp_effect * leafCarbon[Facet.herb];
                    cmrspflux[BELOW] = parms.herbMaxFractionNppToRespiration[BELOW] * resp_temp_effect * fineRootCarbon[Facet.herb];

                    respirationAnnual[Facet.herb] = respirationAnnual[Facet.herb] + cmrspflux[ABOVE] + cmrspflux[BELOW];
                    carbonSourceSink = carbonSourceSink + cmrspflux[ABOVE];
                    carbonSourceSink = carbonSourceSink + cmrspflux[BELOW];

                    // Get average potential production
                    double avg_total_prod_limited_n = (totalPotProdLimitedByN[Layer.herb] +
                            totalPotProdLimitedByN[Layer.herbUnderShrub] + totalPotProduction[Layer.herbUnderTree]) * 2.5;
                    // Actual uptake
                    double[] euf = new double[2];
                    if (avg_total_prod_limited_n > 0.0)
                    {
                        euf[ABOVE] = eUp[Facet.herb, ABOVE] / avg_total_prod_limited_n;
                        euf[BELOW] = eUp[Facet.herb, BELOW] / avg_total_prod_limited_n;
                    }
                    else
                    {
                        euf[ABOVE] = 0.0;
                        euf[BELOW] = 0.0;
                    }

                    // Takeup nutrients from internal storage pool, and don't allow that if storage stored nitrogen (CRPSTG) is negative
                    if (storedNitrogen[Facet.herb] > 0.0)
                    {
                        amt = uptake[N_STORE] * euf[ABOVE];
                        storedNitrogen[Facet.herb] = storedNitrogen[Facet.herb] - amt;
                        leafNitrogen[Facet.herb] = leafNitrogen[Facet.herb] +
                           ((1.0 - parms.fractionAgroundNppToSeeds[Facet.herb]) * amt);
                        seedNitrogen[Facet.herb] = seedNitrogen[Facet.herb] +
                           (parms.fractionAgroundNppToSeeds[Facet.herb] * amt);                 // Translated from CSHED call and complexity of CSRSNK and BGLCIS.Not sure if interpretted correctly.

                        amt = uptake[N_STORE] * euf[BELOW];
                        storedNitrogen[Facet.herb] = storedNitrogen[Facet.herb] - amt;
                        fineRootNitrogen[Facet.herb] = fineRootNitrogen[Facet.herb] + amt;
                    }

                    // Takeup nutrients from the soil.
                    for (int iLayer = 0; iLayer < 2; iLayer++)  // Herbs are taking nutrients from the top two layers
                    {
                        if (mineralNitrogen[iLayer] > tolerance)
                        {
                            double fsol = 1.0;
                            double calcup;
                            if (availableNitrogen > 0.00001)
                                calcup = uptake[N_SOIL] * mineralNitrogen[iLayer] * fsol / availableNitrogen;
                            else
                                calcup = 0.0;
                            amt = uptake[N_SOIL] * euf[ABOVE];
                            mineralNitrogen[iLayer] = mineralNitrogen[iLayer] - amt;
                            leafNitrogen[Facet.herb] = leafNitrogen[Facet.herb] +
                                    ((1.0 - parms.fractionAgroundNppToSeeds[Facet.herb]) * amt);
                            seedNitrogen[Facet.herb] = seedNitrogen[Facet.herb] +
                                    (parms.fractionAgroundNppToSeeds[Facet.herb] * amt);                 // Translated from CSHED call and complexity of CSRSNK and BGLCIS.Not sure if interpretted correctly.

                            amt = uptake[N_SOIL] * euf[BELOW];
                            mineralNitrogen[iLayer] = mineralNitrogen[iLayer] - amt;
                            fineRootNitrogen[Facet.herb] = fineRootNitrogen[Facet.herb] + amt;
                        }
                    }
                    // Takeup nutrients from nitrogen fixation
                    if (plantNitrogenFixed[Facet.herb] > 0.0)
                    {
                        amt = uptake[N_FIX] * euf[ABOVE];
                        nitrogenSourceSink = nitrogenSourceSink - amt;
                        leafNitrogen[Facet.herb] = leafNitrogen[Facet.herb] +
                           ((1.0 - parms.fractionAgroundNppToSeeds[Facet.herb]) * amt);
                        seedNitrogen[Facet.herb] = seedNitrogen[Facet.herb] +
                           (parms.fractionAgroundNppToSeeds[Facet.herb] * amt);                 // Translated from CSHED call and complexity of CSRSNK and BGLCIS.Not sure if interpretted correctly.

                        amt = uptake[N_FIX] * euf[BELOW];
                        nitrogenSourceSink = nitrogenSourceSink - amt;
                        fineRootNitrogen[Facet.herb] = fineRootNitrogen[Facet.herb] + amt;
                    }
                }

                // Update lignin in plant parts incorporating new carbon contributions
                ligninLeaf[Facet.herb] = leafCarbon[Facet.herb] * plantLigninFraction[Facet.herb, surfaceIndex];
                ligninFineRoot[Facet.herb] = fineRootCarbon[Facet.herb] * plantLigninFraction[Facet.herb, soilIndex];
            }
            else // else no production this month
            {
                totalPotProduction[Layer.herb] = 0.0;
                totalPotProdLimitedByN[Layer.herb] = 0.0;
                totalPotProduction[Layer.herbUnderShrub] = 0.0;
                totalPotProdLimitedByN[Layer.herbUnderShrub] = 0.0;
                totalPotProduction[Layer.herbUnderTree] = 0.0;
                totalPotProdLimitedByN[Layer.herbUnderTree] = 0.0;
            }                               // If it is too cold or plants are dormant

        }

        /// <summary>
        /// Woody growth builds from potential production estimates to calculate actual woody growth, as limited by nutrients
        /// </summary>
        private void WoodyGrowth()
        {
            // Determine nitrogen available for growth... Woody plants can draw from all four layers
            double available_nitrogen = 0.0;
            for (int iLayer = 0; iLayer < nSoilLayers; iLayer++)
            {
                available_nitrogen = available_nitrogen + mineralNitrogen[iLayer];
            }

            double site_potential;
            double avg_pot_production = 0.0;
            double rimpct;
            // Century's site potential
            if (globe.precip < 20.0 / 6.0)
                site_potential = 1500.0;
            else if (globe.precip > 90.0 / 6.0)
                site_potential = 3250.0;
            else
                site_potential = Line(globe.precip, 20.0 / 6.0, 1500.0, 90.0 / 6.0, 3250.0);
            site_potential = site_potential * parms.treeSitePotential;

            // Century includes a downward correction of available minerals for savanna trees.  A module that was in Growth, but shifted here because in Growth in Century it was specific to trees.
            double tm = Math.Min(available_nitrogen, 1.5);
            double gnfrac = Math.Exp(-1.664 * Math.Exp(-0.00102 * tm * site_potential) *
                    parms.treeBasalAreaToGrassNitrogen * treeBasalArea);
            if (gnfrac < 0.0 || gnfrac > 1.0)
                gnfrac = 0.0;
            available_nitrogen = available_nitrogen * gnfrac;

            // Century includes flags set in the schedular to turn growth on or off.  I don't want to do that.  
            // I wish to use degree - days, or topsoil available water to pet ratio, or temperature limits
            // Phenology and proportion deciduous are now used to account for no growth by scenscent trees.Could use daylength.Right now, heat accumulation, presumably more sensitive to climate change
            if (ratioWaterPet > 0.0 && globe.minTemp > 0.0)
            {
                double[] cfrac = new double[nWoodyParts];
                double[] uptake;
                for (int iFacet = Facet.shrub; iFacet <= Facet.tree; iFacet++)
                {
                    // Calculate actual production values, and impact of root biomass on available nitrogen
                    if ((parms.rootInterceptOnNutrients * fineRootCarbon[iFacet] * 2.5) > 33)
                        rimpct = 1.0;
                    else
                        rimpct = (1.0 - parms.rootInterceptOnNutrients *
                                Math.Exp(-parms.rootEffectOnNutrients * leafCarbon[iFacet] * 2.5));
                    // Determine actual production, restricted based on carbon to nitrogen ratios
                    // The following uses TREE_CFRAC, which is modified through dynamic carbon allocation(TREEDYNC).I would like to skip that, so using a method like in Growth.
                    // I am going to fill TREE_CFRAC with the values that come from initial distributions.  They will be static.  Those will be done in Each_Year, and may be updated in a given year if appropriate.
                    // This TREE_CFAC that is static may be inappropriate for deciduous trees.  May need two sets or make it dynamic.
                    for (int i = 0; i < nWoodyParts; i++)
                        cfrac[i] = carbonAllocation[iFacet, i];

                    RestrictProduction(iFacet, 2, available_nitrogen, rimpct, cfrac, out uptake);           // 2 is correct here, just looking at leaves and fine roots.

                    // If the deciduous plants are in scenescence then decrease production by the proportion that are deciduous.  Those trees will not be growing.
                    if (phenology[iFacet] >= 3.95)  // Comparison to 4.0 exactly may be causing an error.
                    {
                        if (iFacet == Facet.shrub)
                        {
                            totalPotProduction[Layer.shrub] = totalPotProduction[Layer.shrub] *
                                                    (1.0 - propAnnualDecid[iFacet]);
                            totalPotProduction[Layer.shrubUnderTree] = totalPotProduction[Layer.shrubUnderTree] *
                                                    (1.0 - propAnnualDecid[iFacet]);
                        }
                        else
                        {
                            totalPotProduction[Layer.tree] = totalPotProduction[Layer.tree] *
                                                                     (1.0 - propAnnualDecid[iFacet]);
                        }
                    }
                }
            }
            else
            {
                totalPotProduction[Layer.shrub] = 0.0;
                totalPotProduction[Layer.shrubUnderTree] = 0.0;
                totalPotProduction[Layer.tree] = 0.0;
            }

            for (int iFacet = Facet.shrub; iFacet <= Facet.tree; iFacet++)
            {
                switch (iFacet)
                {

                    case Facet.shrub:
                        avg_pot_production = (totalPotProduction[Layer.shrub] + totalPotProduction[Layer.shrubUnderTree]) / 2.0;
                        break;
                    case Facet.tree:
                        avg_pot_production = totalPotProduction[Layer.tree];
                        break;
                }

                // If growth occurs ...
                if (avg_pot_production > 0.0)
                {
                    // Compute carbon allocation fraction for each woody part.
                    // * *RECALL that production is in biomass units * *but the bulk is proportions allocated, so units are ok.
                    // Portion left after some is taken by leaves and fine roots.These get priority.This doesn't shift material, just does the preliminary calculations
                    carbonAllocation[iFacet, WoodyPart.fineRoot] = parms.fractionCarbonToRoots[iFacet];
                    double cprod_left = avg_pot_production - (avg_pot_production * carbonAllocation[iFacet, WoodyPart.fineRoot]);
                    carbonAllocation[iFacet, WoodyPart.leaf] = LeafAllocation(iFacet, cprod_left, avg_pot_production);

                    double rem_c_frac = 1.0 - carbonAllocation[iFacet, WoodyPart.fineRoot] - carbonAllocation[iFacet, WoodyPart.leaf];
                    if (rem_c_frac < 1.0E-05)
                    {
                        for (int iPart = WoodyPart.fineBranch; iPart <= WoodyPart.coarseRoot; iPart++) // No carbon left, so the remaining parts get 0 new growth.
                            carbonAllocation[iFacet, iPart] = 0.0;
                    }
                    else
                    {
                        // A change from Century ... I don't want to include 10 more parameters controlling (juvenile and mature) carbon allocation to tree parts.
                        // I am going to use the initial carbon allocation.
                        double shrub_c_sum = 0.0;
                        double tree_c_sum = 0.0;
                        for (int iPart = 0; iPart < nWoodyParts; iPart++)
                        {
                            shrub_c_sum = shrub_c_sum + shrubCarbon[iPart];
                            tree_c_sum = tree_c_sum + treeCarbon[iPart];
                        }
                        switch (iFacet)
                        {

                            case Facet.shrub:
                                //  Shrubs
                                if (shrub_c_sum > 0.0) // If a division by zero error would occur, just don't change carbon_allocation
                                {
                                    carbonAllocation[Facet.shrub, WoodyPart.fineBranch] = shrubCarbon[WoodyPart.fineBranch] / shrub_c_sum;
                                    carbonAllocation[Facet.shrub, WoodyPart.coarseBranch] = shrubCarbon[WoodyPart.coarseBranch] / shrub_c_sum;
                                    carbonAllocation[Facet.shrub, WoodyPart.coarseRoot] = shrubCarbon[WoodyPart.coarseRoot] / shrub_c_sum;
                                }
                                break;
                            case Facet.tree:
                                // Trees
                                if (tree_c_sum > 0.0) // If a division by zero error would occur, just don't change carbon_allocation
                                {
                                    carbonAllocation[Facet.tree, WoodyPart.fineBranch] = treeCarbon[WoodyPart.fineBranch] / tree_c_sum;
                                    carbonAllocation[Facet.tree, WoodyPart.coarseBranch] = treeCarbon[WoodyPart.coarseBranch] / tree_c_sum;
                                    carbonAllocation[Facet.tree, WoodyPart.coarseRoot] = treeCarbon[WoodyPart.coarseRoot] / tree_c_sum;
                                }
                                break;
                        }

                        double tot_cup = carbonAllocation[iFacet, WoodyPart.fineBranch] +
                                         carbonAllocation[iFacet, WoodyPart.coarseBranch] +
                                         carbonAllocation[iFacet, WoodyPart.coarseRoot];
                        if (tot_cup > 0.0)
                        {
                            for (int iPart = WoodyPart.fineBranch; iPart <= WoodyPart.coarseRoot; iPart++)
                                carbonAllocation[iFacet, iPart] = carbonAllocation[iFacet, iPart] / tot_cup * rem_c_frac;
                        }
                    }

                    // Calculate actual production values, and impact of root biomass on available nitrogen
                    if ((parms.rootInterceptOnNutrients * fineRootCarbon[iFacet] * 2.5) > 33.0)
                        rimpct = 1.0;
                    else
                        rimpct = (1.0 - parms.rootInterceptOnNutrients *
                                  Math.Exp(-parms.rootEffectOnNutrients * fineRootCarbon[iFacet] * 2.5));
                    // Determine actual production, restricted based on carbon to nitrogen ratios
                    // The following uses TREE_CFRAC, which is modified through dynamic carbon allocation(TREEDYNC).I would like to skip that, so using a method like in Growth.
                    // I am going to fill TREE_CFRAC with the values that come from initial distributions.  They will be static.  Those will be done in Each_Year, and may be updated in a given year if appropriate.
                    // This TREE_CFAC that is static may be inappropriate for deciduous trees.  May need two sets or make it dynamic.
                    double[] cfrac = new double[nWoodyParts];
                    double[] uptake;
                    for (int i = 0; i < nWoodyParts; i++)
                        cfrac[i] = carbonAllocation[iFacet, i];
                    RestrictProduction(iFacet, nWoodyParts, available_nitrogen, rimpct, cfrac, out uptake);

                    // Calculate symbiotic N fixation accumulation
                    nitrogenFixed[iFacet] = nitrogenFixed[iFacet] + plantNitrogenFixed[iFacet];
                    // Accumulator was skipped... too many and potentially confusing, so added later.NFIXAC, EUPACC, EUPRT, TCNPRO

                    // Calculate production for each tree part
                    // This section deals with maintenance respiration calculations
                    double[] mfprd = new double[nWoodyParts];
                    for (int iPart = 0; iPart < nWoodyParts; iPart++)
                    {
                        mfprd[iPart] = carbonAllocation[iFacet, iPart] * avg_pot_production;
                        respirationFlows[iFacet] = respirationFlows[iFacet] + mfprd[iPart] + parms.fractionNppToRespiration[iFacet];
                    }
                    // Growth of forest parts, with carbon added to the part and removed from the source-sink
                    for (int iPart = 0; iPart < nWoodyParts; iPart++)
                    {
                        switch (iFacet)
                        {
                            case Facet.shrub:
                                shrubCarbon[iPart] = shrubCarbon[iPart] + mfprd[iPart];   // Translated from CSCHED... double-check as needed
                                break;
                            case Facet.tree:
                                treeCarbon[iPart] = treeCarbon[iPart] + mfprd[iPart];    // Translated from CSCHED... double-check as needed
                                break;
                        }
                        carbonSourceSink = carbonSourceSink - mfprd[iPart];
                    }
                    fineRootCarbon[iFacet] = fineRootCarbon[iFacet] + mfprd[WoodyPart.fineRoot];
                    leafCarbon[iFacet] = leafCarbon[iFacet] + ((1.0 - parms.fractionAgroundNppToSeeds[iFacet]) * mfprd[WoodyPart.leaf]);  // Translated from CSHED call and complexity of CSRSNK and BGLCIS.  Not sure if interpretted correctly.
                    seedCarbon[iFacet] = seedCarbon[iFacet] + (parms.fractionAgroundNppToSeeds[iFacet] * mfprd[WoodyPart.leaf]);   // Translated from CSHED call and complexity of CSRSNK and BGLCIS.  Not sure if interpretted correctly.
                    fineBranchCarbon[iFacet] = fineBranchCarbon[iFacet] + mfprd[WoodyPart.fineBranch];
                    coarseBranchCarbon[iFacet] = coarseBranchCarbon[iFacet] + mfprd[WoodyPart.coarseBranch];
                    coarseRootCarbon[iFacet] = coarseRootCarbon[iFacet] + mfprd[WoodyPart.coarseRoot];

                    // Add maintenance respiration flow
                    maintainRespiration[iFacet] = maintainRespiration[iFacet] + respirationFlows[iFacet];
                    carbonSourceSink = carbonSourceSink - respirationFlows[iFacet];

                    // Maintenance respiration fluxes reduce maintenance respiration storage pool
                    double resp_temp_effect = 0.1 * Math.Exp(0.07 * globe.temperatureAverage);
                    resp_temp_effect = Math.Min(1.0, resp_temp_effect);
                    resp_temp_effect = Math.Max(0.0, resp_temp_effect);
                    double[] fmrspflux = new double[nWoodyParts];
                    for (int iPart = 0; iPart < nWoodyParts; iPart++) // Merged two looping structures, the first didn't exist in Century, but works with this logic.
                    {
                        switch (iFacet)
                        {
                            case Facet.shrub:
                                fmrspflux[iPart] = parms.woodyMaxFractionNppToRespiration[iPart] * resp_temp_effect * shrubCarbon[iPart];
                                break;
                            case Facet.tree:
                                fmrspflux[iPart] = parms.woodyMaxFractionNppToRespiration[iPart] * resp_temp_effect * treeCarbon[iPart];
                                break;
                        }
                        respirationAnnual[iFacet] = respirationAnnual[iFacet] + fmrspflux[iPart];
                        carbonSourceSink = carbonSourceSink - fmrspflux[iPart];
                    }

                    double[] euf = new double[nWoodyParts];
                    // Actual uptake... using the average of the vegetation layer parts
                    switch (iFacet)
                    {
                        case Facet.shrub:
                            if ((totalPotProdLimitedByN[Layer.shrub] + totalPotProdLimitedByN[Layer.shrubUnderTree] / 2.0) > 0.0)
                            {
                                for (int iPart = 0; iPart < nWoodyParts; iPart++)
                                    euf[iPart] = eUp[iFacet, iPart] / ((totalPotProdLimitedByN[Layer.shrub] +
                                                                        totalPotProdLimitedByN[Layer.shrubUnderTree]) / 2.0);
                            }
                            else
                            {
                                for (int iPart = 0; iPart < nWoodyParts; iPart++)
                                    euf[iPart] = 1.0;      // Will cause no change later in the logic.
                            }
                            break;
                        case Facet.tree:
                            if (totalPotProdLimitedByN[Layer.tree] > 0.0)
                            {
                                for (int iPart = 0; iPart < nWoodyParts; iPart++)
                                    euf[iPart] = eUp[iFacet, iPart] / totalPotProdLimitedByN[Layer.tree];
                            }
                            else
                            {
                                for (int iPart = 0; iPart < nWoodyParts; iPart++)
                                    euf[iPart] = 1.0;      // Will cause no change later in the logic.
                            }
                            break;
                    }

                    // Takeup nutrients from internal storage pool.
                    double amt;
                    if (storedNitrogen[iFacet] > 0.0)
                    {
                        for (int iPart = 0; iPart < nWoodyParts; iPart++)
                        {
                            amt = uptake[N_STORE] * euf[iPart];
                            switch (iFacet)
                            {
                                case Facet.shrub:
                                    shrubNitrogen[iPart] = shrubNitrogen[iPart] + amt;
                                    break;
                                case Facet.tree:
                                    treeNitrogen[iPart] = treeNitrogen[iPart] + amt;
                                    break;
                            }
                            storedNitrogen[iFacet] = storedNitrogen[iFacet] - amt;
                        }
                    }
                    fineRootNitrogen[iFacet] = fineRootNitrogen[iFacet] + (uptake[N_STORE] * euf[WoodyPart.fineRoot]);
                    leafNitrogen[iFacet] = leafNitrogen[iFacet] + (1.0 - parms.fractionAgroundNppToSeeds[iFacet]) * (uptake[N_STORE] * euf[WoodyPart.leaf]);
                    seedNitrogen[iFacet] = seedNitrogen[iFacet] + (parms.fractionAgroundNppToSeeds[iFacet]) * (uptake[N_STORE] * euf[WoodyPart.leaf]);
                    fineBranchNitrogen[iFacet] = fineBranchNitrogen[iFacet] + (uptake[N_STORE] * euf[WoodyPart.fineBranch]);
                    coarseBranchNitrogen[iFacet] = coarseBranchNitrogen[iFacet] + (uptake[N_STORE] * euf[WoodyPart.coarseBranch]);
                    coarseRootNitrogen[iFacet] = coarseRootNitrogen[iFacet] + (uptake[N_STORE] * euf[WoodyPart.coarseRoot]);

                    // Takeup nutrients from the soil. ... Woody plants take nutrients from all four layers
                    double fsol;
                    double calcup;
                    for (int iLayer = 0; iLayer < nSoilLayers; iLayer++)
                    {
                        if (mineralNitrogen[iLayer] > 0.00001)
                        {
                            fsol = 1.0;
                            if (available_nitrogen > 0.00001)
                                calcup = uptake[N_SOIL] * mineralNitrogen[iLayer] * fsol / available_nitrogen;
                            else
                                calcup = 0.0;
                            for (int iPart = 0; iPart < nWoodyParts; iPart++)
                            {
                                amt = calcup * euf[iPart];
                                switch (iFacet)
                                {
                                    case Facet.shrub:
                                        shrubNitrogen[iPart] = shrubNitrogen[iPart] + amt;
                                        break;
                                    case Facet.tree:
                                        treeNitrogen[iPart] = treeNitrogen[iPart] + amt;
                                        break;
                                }
                                mineralNitrogen[iLayer] = mineralNitrogen[iLayer] - amt;
                            }
                            fineRootNitrogen[iFacet] = fineRootNitrogen[iFacet] + (calcup * euf[WoodyPart.fineRoot]);
                            leafNitrogen[iFacet] = leafNitrogen[iFacet] + (calcup * euf[WoodyPart.leaf]);
                            fineBranchNitrogen[iFacet] = fineBranchNitrogen[iFacet] + (calcup * euf[WoodyPart.fineBranch]);
                            coarseBranchNitrogen[iFacet] = coarseBranchNitrogen[iFacet] + (calcup * euf[WoodyPart.coarseBranch]);
                            coarseRootNitrogen[iFacet] = coarseRootNitrogen[iFacet] + (calcup * euf[WoodyPart.coarseRoot]);
                        }
                    }

                    // Takeup nutrients from nitrogen fixation.
                    if (plantNitrogenFixed[iFacet] > 0.000001)
                    {
                        for (int iPart = 0; iPart < nWoodyParts; iPart++)
                        {
                            amt = uptake[N_FIX] * euf[iPart];
                            switch (iFacet)
                            {
                                case Facet.shrub:
                                    shrubNitrogen[iPart] = shrubNitrogen[iPart] + amt;
                                    break;
                                case Facet.tree:
                                    treeNitrogen[iPart] = treeNitrogen[iPart] + amt;
                                    break;
                            }
                            nitrogenSourceSink = nitrogenSourceSink - amt;
                        }
                        fineRootNitrogen[iFacet] = fineRootNitrogen[iFacet] + (uptake[N_STORE] * euf[WoodyPart.fineRoot]);
                        leafNitrogen[iFacet] = leafNitrogen[iFacet] + (uptake[N_STORE] * euf[WoodyPart.leaf]);
                        fineBranchNitrogen[iFacet] = fineBranchNitrogen[iFacet] + (uptake[N_STORE] * euf[WoodyPart.fineBranch]);
                        coarseBranchNitrogen[iFacet] = coarseBranchNitrogen[iFacet] + (uptake[N_STORE] * euf[WoodyPart.coarseBranch]);
                        coarseRootNitrogen[iFacet] = coarseRootNitrogen[iFacet] + (uptake[N_STORE] * euf[WoodyPart.coarseRoot]);
                    }

                    // Update lignin in plant parts incorporating new carbon contributions
                    ligninLeaf[iFacet] = leafCarbon[iFacet] * plantLigninFraction[iFacet, surfaceIndex];
                    ligninFineRoot[iFacet] = fineRootCarbon[iFacet] * plantLigninFraction[iFacet, soilIndex];
                    ligninFineBranch[iFacet] = fineBranchCarbon[iFacet] * plantLigninFraction[iFacet, surfaceIndex];
                    ligninCoarseBranch[iFacet] = coarseBranchCarbon[iFacet] * plantLigninFraction[iFacet, surfaceIndex];
                    ligninCoarseRoot[iFacet] = coarseRootCarbon[iFacet] * plantLigninFraction[iFacet, soilIndex];
                }
                else
                {
                    // There is no production this month
                    switch (iFacet)
                    {
                        case Facet.shrub:
                            totalPotProdLimitedByN[Layer.shrub] = 0.0;
                            totalPotProduction[Layer.shrub] = 0.0;
                            totalPotProdLimitedByN[Layer.shrubUnderTree] = 0.0;
                            totalPotProduction[Layer.shrubUnderTree] = 0.0;
                            break;
                        case Facet.tree:
                            totalPotProdLimitedByN[Layer.tree] = 0.0;
                            totalPotProduction[Layer.tree] = 0.0;
                            break;
                    }
                    for (int iPart = 0; iPart < nWoodyParts; iPart++)
                        eUp[iFacet, iPart] = 0.0;
                }
            }
        }

    /// <summary>
    /// Restrict actual production for plants based on carbon to nitrogen ratios
    /// Note, currently(and it could be changed for clarity), cfrac stores carbon allocation in ABOVE and BELOW for herbs,
    /// in plant parts for woody plants.
    /// </summary>
    /// <param name="iFacet">Facet under consideration</param>
    /// <param name="nparts"></param>
    /// <param name="availableNitrogen"></param>
    /// <param name="rimpct"></param>
    /// <param name="cfrac"></param>
    /// <param name="uptake"></param>
    private void RestrictProduction(int iFacet, int nparts, double availableNitrogen, double rimpct, double[] cfrac, out double[] uptake)
        {
            uptake = new double[3];
            if ((availableNitrogen <= 1E-4) && (parms.maxSymbioticNFixationRatio == 0.0))
                return;     // Won't things go unset?  Set something to zero?

            // Calculate available nitrogen based on maximum fraction and impact of root biomass
            double n_available = (availableNitrogen * parms.fractionNitrogenAvailable * rimpct) + storedNitrogen[iFacet];

            // Compute weighted average carbon to biomass conversion factor.  
            // The structure is a little odd here, because this section can be called for grasses or trees.

            // ctob is a weighted average carbon to biomass conversion factor
            // I don't trust the structure, replacing with a simplier structure (but the other now appears correct as well)
            double ctob;
            if (iFacet == Facet.herb)
                ctob = 2.5;              // The same conversion is used above and below ground
            else
                ctob = (cfrac[WoodyPart.leaf] * 2.5) + (cfrac[WoodyPart.fineRoot] * 2.5) + (cfrac[WoodyPart.fineBranch] * 2.0) +
                       (cfrac[WoodyPart.coarseBranch] * 2.0) + (cfrac[WoodyPart.coarseRoot] * 2.0);       // Note conversion from carbon to biomass for wood parts is 2.0, rather than 2.5

            // Calculate average N/ C of whole plant(grass, shrub, or tree)
            double max_n = 0.0;
            double[] min_n_ci = new double[nWoodyParts];
            double[] max_n_ci = new double[nWoodyParts];
            for (int iPart = 0; iPart < nWoodyParts; iPart++)
            {
                min_n_ci[iPart] = 1.0 / parms.maximumCNRatio[iFacet, iPart];  // CHECK THIS.  IS IT IN ERROR?  Unusual formatting in CENTURY
                max_n_ci[iPart] = 1.0 / parms.minimumCNRatio[iFacet, iPart];  // Note that indicators maximum and minimum are flipped
            }
            // The following bases results on shoots and roots only.It works for herbs and woody as well given that the two values of interest are 1 and 2 regardless.
            max_n = max_n + (cfrac[WoodyPart.fineRoot] * max_n_ci[WoodyPart.fineRoot]);      // CENTURY includes a biomass to carbon convertion on CFRAC(FROOT) * MAXECI.I'm not sure why.   I've removed them for now.
            max_n = max_n + ((1.0 - cfrac[WoodyPart.fineRoot]) * max_n_ci[WoodyPart.leaf]);  // CENTURY includes a biomass to carbon convertion on CFRAC(FROOT) * MAXECI.I'm not sure why.   I've removed them for now.

            // Calculate average nutrient content
            //   max_n = max_n * ctob! Skipping this for now(counter to CENTURY RSTRP.F).So MAX_N is being passed as a weighted nitrogen concentration.Converting what I have to biomass doesn't make any sense.

            // Compute the limitation on nutrients.Min_N need not be passed.It used in Century only for automatic fertilization.
            NutrientLimitation(iFacet, nparts, max_n, min_n_ci, max_n_ci, cfrac, ref ctob, n_available);

            // Calculate relative yield skipped for now.Not used in module.

            double temp_prod = 0.0;
            // Calculate uptake from all sources (storage, soil, plant n fixed)
            switch (iFacet)   // Need to use the average of production.  
            {
                case Facet.herb:
                    temp_prod = (totalPotProdLimitedByN[Layer.herb] + totalPotProdLimitedByN[Layer.herbUnderShrub] +
                                 totalPotProdLimitedByN[Layer.herbUnderTree]) / 3.0;
                    break;
                case Facet.shrub:
                    temp_prod = (totalPotProdLimitedByN[Layer.shrub] + totalPotProdLimitedByN[Layer.shrubUnderTree]) / 2.0;
                    break;
                case Facet.tree:
                    temp_prod = totalPotProdLimitedByN[Layer.tree];
                    break;
            }
            double ustorg = Math.Min(storedNitrogen[iFacet], temp_prod);
            // If the storage pool contains all that is needed for uptake, then...
            if (temp_prod <= ustorg)
            {
                uptake[N_STORE] = temp_prod;
                uptake[N_SOIL] = 0.0;
            }
            // Otherwise take what is needed from the storage pool(unneeded elseif in Century, skipped)
            else
            {
                uptake[N_STORE] = storedNitrogen[iFacet];
                uptake[N_SOIL] = temp_prod - storedNitrogen[iFacet] - plantNitrogenFixed[iFacet];
            }
            uptake[N_FIX] = plantNitrogenFixed[iFacet];
        }

        /// <summary>
        /// Compute nutrient limitation on growth  (NUTRLM.F in Century)
        /// </summary>
        /// <param name="iFacet"></param>
        /// <param name="nParts"></param>
        /// <param name="maxN"></param>
        /// <param name="minNCi"></param>
        /// <param name="maxNCi"></param>
        /// <param name="cFrac"></param>
        /// <param name="ctob"></param>
        /// <param name="availableNitrogen"></param>
        private void NutrientLimitation(int iFacet, int nParts, double maxN, double[] minNCi, double[] maxNCi, double[] cFrac, ref double ctob, double availableNitrogen)
        {
            // A lengthy section within NUTRLM deals with P and S and was skipped

            // Get the total production for the facet.  Include the different layers(i.e., do not take the average, as that will reduce total production by default)
            double max_n_fix = 0.0;
            double total_pot_production_biomass = 0.0;
            switch (iFacet)
            {
                case Facet.herb:
                    total_pot_production_biomass = totalPotProduction[Layer.herb] + totalPotProduction[Layer.herbUnderShrub] +
                                                  totalPotProduction[Layer.herbUnderTree];
                    break;
                case Facet.shrub:
                    total_pot_production_biomass = totalPotProduction[Layer.shrub] + totalPotProduction[Layer.shrubUnderTree];
                    break;
                case Facet.tree:
                    total_pot_production_biomass = totalPotProduction[Layer.tree];
                    break;
            }

            // Convert to carbon
            double total_pot_production_carbon = total_pot_production_biomass / ctob;
            // Demand based on the maximum nitrogen / carbon ratio
            // Max_n is the maximum nitrogen to carbon ratio ... convert to carbon then multiply by N: C ratio.  so X:C yields demand
            double demand = total_pot_production_carbon * maxN;  //  Weighted concentration of nitrogen in plant parts
            max_n_fix = parms.maxSymbioticNFixationRatio * total_pot_production_carbon;
            double totaln = availableNitrogen + max_n_fix;

            // Calculation of a2drat(n) skipped.It appears associated with dynamic carbon allocation, not implemented here, and not used in this module.

            double[] ecfor = new double[nWoodyParts];
            double totaln_used = 0.0;
            // New N/ C ratios based on nitrogen available
            if (totaln > demand)
            {
                for (int iPart = 0; iPart < nParts; iPart++)
                {
                    ecfor[iPart] = maxNCi[iPart];    // Nitrogen is readily available, and the maximum concentration per part is appropriate
                }
                totaln_used = demand;   //  Setting a division equal to 1, essentially, used later
            }
            else
            {
                if (demand == 0.0)
                {
                    //       write(*, *) 'Error in Nutrient_Limitation, demand = 0.0'! Disabling warning ... in a global model, a cell with no demand is reasonable, in the high arctic, say.
                    //       stop DEBUG
                    // The following is part of the DEBUG, to avoid errors    !DEBUG
                    demand = 0.001;   // DEBUG
                }
                for (int iPart = 0; iPart < nParts; iPart++)
                {
                    ecfor[iPart] = minNCi[iPart] + ((maxNCi[iPart] - minNCi[iPart]) * (totaln / demand));  // Nitrogen is limited, and so a fractional portion is assigned
                    totaln_used = totaln;
                }
            }

            double cpbe = 0.0;
            ctob = 0.0;
            // Total potential production with nutrient limitation.  Here CPBE remains a N to C ratio, but adjusted for demand.
            for (int iPart = 0; iPart < nParts; iPart++)
            {
                if (iPart < WoodyPart.fineBranch)
                {
                    cpbe = cpbe + ((cFrac[iPart] * ecfor[iPart]) / 2.5);     // Leaves and fine roots
                    ctob = ctob + (cFrac[iPart] * 2.5);          // From RESTRP.F
                }
                else
                {
                    cpbe = cpbe + ((cFrac[iPart] * ecfor[iPart]) / 2.0);  // Fine and coarse branches and coarse roots
                    ctob = ctob + (cFrac[iPart] * 2.0);          // From RESTRP.F
                }
            }

            // Increase the nitrogen estimate in line with an increase in carbon to biomass conversion(i.e., between 2 and 2.5 based on plant parts involved)
            // Send the cpbe value from carbon to biomass
            cpbe = cpbe * ctob;
            if (cpbe == 0.0)
            {
                //     write(*, *) 'Error in Nutrient Limitation, CPBE = 0.0'! Disabling warning ... in a global model, a cell with no demand is reasonable, in the high arctic, say.
                //       stop DEBUG
                // The following is part of the DEBUG, to avoid errors    !DEBUG
                cpbe = 0.001;
            }

            // Calculate potential production for the nutrient limitation

            cpbe = totaln_used / cpbe;

            // Automatic fertilization methods skipped.
            // See if production is limited by nutrients(works to compute limiting nutrient in Century, but just one here)
            if (total_pot_production_biomass > cpbe)
            {
                total_pot_production_biomass = total_pot_production_biomass * (totaln_used / demand);
            }

            // Adjustments considering P skipped.P not considered here.
            // Total potential production with nitrogen limitation
            // First store nitrogen uptake per plant part.Note use of carbon here, rather than biomass as in Century.Carbon is being related to nitrogen here.
            for (int iPart = 0; iPart < nParts; iPart++)
            {
                eUp[iFacet, iPart] = total_pot_production_carbon * cFrac[iPart] * ecfor[iPart];
                if (eUp[iFacet, iPart] < 0.0)
                    eUp[iFacet, iPart] = 0.0;
            }
            // Then put in limited production estimates

            double[] lfrac = new double[nLayers];
            switch (iFacet)
            {
                case Facet.herb:
                    // Assuming production is in-line with existing biomass, so using a weighted average.
                    // Total_pot_production is calculated from first principles, so I will use that as a guide.Using a little brute force, for speed
                    // Total_pot_production isn't limited by n, but assuming all the layers are equally affected by n limitation is appropriate.
                    if ((totalPotProduction[Layer.herb] + totalPotProduction[Layer.herbUnderShrub] +
                         totalPotProduction[Layer.herbUnderTree]) >= 0.00001)
                    {
                        lfrac[Layer.herb] = totalPotProduction[Layer.herb] / (totalPotProduction[Layer.herb] +
                                                 totalPotProduction[Layer.herbUnderShrub] + totalPotProduction[Layer.herbUnderTree]);
                        lfrac[Layer.herbUnderShrub] = totalPotProduction[Layer.herbUnderShrub] / (totalPotProduction[Layer.herb] +
                                                 totalPotProduction[Layer.herbUnderShrub] + totalPotProduction[Layer.herbUnderTree]);
                        lfrac[Layer.herbUnderTree] = totalPotProduction[Layer.herbUnderTree] / (totalPotProduction[Layer.herb] +
                                                 totalPotProduction[Layer.herbUnderShrub] + totalPotProduction[Layer.herbUnderTree]);
                    }
                    else
                    {
                        lfrac[Layer.herb] = 0.0;
                        lfrac[Layer.herbUnderShrub] = 0.0;
                        lfrac[Layer.herbUnderTree] = 0.0;
                    }
                    totalPotProdLimitedByN[Layer.herb] = total_pot_production_biomass * lfrac[Layer.herb];
                    totalPotProdLimitedByN[Layer.herbUnderShrub] = total_pot_production_biomass * lfrac[Layer.herbUnderShrub];
                    totalPotProdLimitedByN[Layer.herbUnderTree] = total_pot_production_biomass * lfrac[Layer.herbUnderTree];
                    break;
                case Facet.shrub:
                    if ((totalPotProduction[Layer.shrub] + totalPotProduction[Layer.shrubUnderTree]) >= 0.00001)
                    {
                        lfrac[Layer.shrub] = totalPotProduction[Layer.shrub] / (totalPotProduction[Layer.shrub] +
                            totalPotProduction[Layer.shrubUnderTree]);
                        lfrac[Layer.shrubUnderTree] = totalPotProduction[Layer.shrubUnderTree] / (totalPotProduction[Layer.shrub] +
                            totalPotProduction[Layer.shrubUnderTree]);
                    }
                    else
                    {
                        lfrac[Layer.shrub] = 0.0;
                        lfrac[Layer.shrubUnderTree] = 0.0;
                    }
                    totalPotProdLimitedByN[Layer.shrub] = total_pot_production_biomass * lfrac[Layer.shrub];
                    totalPotProdLimitedByN[Layer.shrubUnderTree] = total_pot_production_biomass * lfrac[Layer.shrubUnderTree];
                    break;
                case Facet.tree:
                    totalPotProdLimitedByN[Layer.tree] = total_pot_production_biomass;
                    break;
            }

            // Compute nitrogen fixation that actually occurs(Using average for plant nitrogen fixed in shrubs
            switch (iFacet)
            {
                case Facet.herb:
                    plantNitrogenFixed[iFacet] = Math.Max(totaln_used - availableNitrogen, 0.0);
                    break;
                case Facet.shrub:
                    plantNitrogenFixed[iFacet] = Math.Max(totaln_used - availableNitrogen, 0.0);
                    break;
                case Facet.tree:
                    plantNitrogenFixed[iFacet] = Math.Max(totaln_used - availableNitrogen, 0.0);
                    break;
            }
        }

        /// <summary>
        /// Compute optimum leaf area index, based on a maximum and available production
        /// </summary>
        /// <param name="iFacet"></param>
        /// <param name="cProdLeft"></param>
        /// <param name="cprodf"></param>
        /// <returns></returns>
        private double LeafAllocation(int iFacet, double cProdLeft, double cprodf)
        {
            if (coarseBranchCarbon[iFacet] > 0.0)
            {
                optimumLeafAreaIndex[iFacet] = parms.maximumLeafAreaIndex *
                        (coarseBranchCarbon[iFacet] * 2.0) / (parms.kLeafAreaIndex +
                        (coarseBranchCarbon[iFacet] * 2.0));
                if (optimumLeafAreaIndex[iFacet] < 0.1)
                    optimumLeafAreaIndex[iFacet] = 0.1;
            }
            else
            {
                optimumLeafAreaIndex[iFacet] = 0.1;
            }

            double leafprod;
            double rleavc_opt = optimumLeafAreaIndex[iFacet] / (2.5 * parms.biomassToLeafAreaIndexFactor);
            if (rleavc_opt > leafCarbon[iFacet])
                leafprod = Math.Min((rleavc_opt - leafCarbon[iFacet]), cProdLeft);
            else
                leafprod = 0.0;

            double result;
            if (cprodf > 0.0)
                result = leafprod / cprodf;
            else
                // !!NOTE!! CENTURY has this as 0, which is likely appropriate once trees get larger and optimum lia is more reasonable.
                result = 0.01;
            if (result < 0.01)
                result = 0.01;    // This trims where Century throws errors.
            if (result > 1.0)
                result = 1.0;
            return result;
        }

        /// <summary>
        /// Remove forage that is grazed (GREM in Century).
        /// </summary>
        private void Grazing()
        {
            double total_carbon = 0.0;
            double total_nitrogen = 0.0;
            double carbon_removed;
            double nitrogen_removed;
            for (int iFacet = 0; iFacet < nFacets; iFacet++)
            {
                // Shoots removed.  Moving away from Century somewhat.
                if (leafCarbon[iFacet] > 0.0)
                    carbon_removed = leafCarbon[iFacet] * (fractionLiveRemovedGrazing * parms.fractionGrazedByFacet[iFacet]);
                else
                    carbon_removed = 0.0;
                if (leafNitrogen[iFacet] > 0.0)
                    nitrogen_removed = leafNitrogen[iFacet] * (fractionLiveRemovedGrazing * parms.fractionGrazedByFacet[iFacet]);
                else
                    nitrogen_removed = 0.0;
                leafCarbon[iFacet] = leafCarbon[iFacet] - carbon_removed;
                leafNitrogen[iFacet] = leafNitrogen[iFacet] - nitrogen_removed;
                carbonSourceSink = carbonSourceSink + carbon_removed;
                nitrogenSourceSink = nitrogenSourceSink + nitrogen_removed;
                total_carbon = total_carbon + carbon_removed;
                total_nitrogen = total_nitrogen + nitrogen_removed;

                // Standing dead removed.
                if (deadStandingCarbon[iFacet] > 0.0)
                    carbon_removed = deadStandingCarbon[iFacet] * (fractionDeadRemovedGrazing * parms.fractionGrazedByFacet[iFacet]);
                else
                    carbon_removed = 0.0;
                if (deadStandingNitrogen[iFacet] > 0.0)
                    nitrogen_removed = deadStandingNitrogen[iFacet] * (fractionDeadRemovedGrazing * parms.fractionGrazedByFacet[iFacet]);
                else
                    nitrogen_removed = 0.0;
                deadStandingCarbon[iFacet] = deadStandingCarbon[iFacet] - carbon_removed;
                deadStandingNitrogen[iFacet] = deadStandingNitrogen[iFacet] - nitrogen_removed;
                carbonSourceSink = carbonSourceSink + carbon_removed;
                nitrogenSourceSink = nitrogenSourceSink + nitrogen_removed;
                total_carbon = total_carbon + carbon_removed;
                total_nitrogen = total_nitrogen + nitrogen_removed;
            }

            // Return portions of the carbon and nitrogen grazed back to the environment.  Carbon in the form of feces, urine includes nitrogen.
            double nitrogen_returned;
            double fraction_nitrogen_grazed_returned;
            double carbon_returned = parms.fractionCarbonGrazedReturned * total_carbon;
            if (carbon_returned <= 0.0)
            {
                carbon_returned = 0.0;
                nitrogen_returned = 0.0;
                fraction_nitrogen_grazed_returned = 0.0;
            }
            else
            {
                // The portion of nitrogen returned is a function of clay content(CENTURY GREM.F)
                if (clay[surfaceIndex] < 0.0)
                    fraction_nitrogen_grazed_returned = 0.7;
                else if (clay[surfaceIndex] > 0.3)
                    fraction_nitrogen_grazed_returned = 0.85;
                else
                    fraction_nitrogen_grazed_returned = (0.85 - 0.7) / (0.3 - 0.0) * (clay[surfaceIndex] - 0.3) + 0.85;
            }
            nitrogen_returned = fraction_nitrogen_grazed_returned * total_nitrogen;
            double urine = (1.0 - parms.fractionExcretedNitrogenInFeces) * nitrogen_returned;
            double feces = parms.fractionExcretedNitrogenInFeces * nitrogen_returned;

            // Do flows
            mineralNitrogen[surfaceIndex] = mineralNitrogen[surfaceIndex] + urine;
            nitrogenSourceSink = nitrogenSourceSink - urine;

            volatizedN = volatizedN * (parms.fractionUrineVolatized * urine);
            urine = urine - volatizedN;

            // Move materials into litter
            double avg_lignin = (ligninLeaf[Facet.herb] + ligninLeaf[Facet.shrub] + ligninLeaf[Facet.tree]) / 3.0;
            avg_lignin = Math.Max(0.02, avg_lignin);   // From Century CmpLig.f
            avg_lignin = Math.Min(0.50, avg_lignin);
            PartitionLitter(surfaceIndex, feces, urine, avg_lignin);  
        }

        /// <summary>
        /// Plant material dies for various reasons.  This routine includes a call to Woody_Plant_Part_Death.
        /// </summary>
        private void PlantPartDeath()
        {
            double deck5 = 5.0;         // Value appears standard in 100 files.Not documented anywhere I can find.
            double death_rate;
            double death_carbon;
            double death_nitrogen;
            // Death of herb fine roots(DROOT.F)
            if ((fineRootCarbon[Facet.herb] > 0.0) && soilSurfaceTemperature > 0.0)
            {
                // In the following, water_available(1) is for growth, 2 is for survival, 3 for the top two layers.
                // For C#, make that: water_available[0] is for growth, 1 is for survival, 2 for the top two layers.
                double rtdh = 1.0 - waterAvailable[0] / (deck5 + waterAvailable[0]);    // Deck5 is set, so no division by 0.Century uses the first layer... avh20(1) like here.
                death_rate = parms.maxHerbRootDeathRate * rtdh;
                if (death_rate > 0.95)
                    death_rate = 0.95;
                if (death_rate > 0.0)
                    death_carbon = death_rate * fineRootCarbon[Facet.herb];
                else
                    death_carbon = 0.0;
                // Moving away from CENTURY here, which goes into labeled materials, etc.
                if (death_rate > 0.0 && fineRootNitrogen[Facet.herb] > 0.0)
                    death_nitrogen = death_rate * fineRootNitrogen[Facet.herb];
                else
                    death_nitrogen = 0.0;
                // Do flows for plant parts
                // Dead fine root carbon and nitrogen are short - term data holders, reset at the end of each month.The
                // material is passed directly to litter.
                deadFineRootCarbon[Facet.herb] = deadFineRootCarbon[Facet.herb] + death_carbon;
                fineRootCarbon[Facet.herb] = fineRootCarbon[Facet.herb] - death_carbon;
                deadFineRootNitrogen[Facet.herb] = deadFineRootNitrogen[Facet.herb] + death_nitrogen;
                fineRootNitrogen[Facet.herb] = fineRootNitrogen[Facet.herb] - death_nitrogen;
                // Do flows to litter, keeping track of structural and metabolic components
                // Dead fine root carbon is already partitioned to litter in the main decomposition program.The same is true for seeds.
                // A portion of stored carbon for maintence respiration is loss associated with death of plant part.                 ! FLOWS may be misnamed, or the wrong indicator.
                respirationFlows[Facet.herb] = respirationFlows[Facet.herb] - (respirationFlows[Facet.herb] * death_rate);
            }

            // Death of leaves and shoots(DSHOOT.F)
            // *****INCLUDE STANDING DEAD HERE, RATHER THAN TRANSFERS TO BELOW - GROUND * ****
            // (Century uses aboveground live carbon)  
            if (leafCarbon[Facet.herb] > 0.00001)
            {
                // Century increases to the maximum the death rate during month of senescencee.  I am going to experiment using phenology
                // Century uses a series of four additional parameters for shoot death.  They describe losses due to 1) water stress, 2) phenology, and 3) shading as indicated in 4.
                if (phenology[Facet.herb] >= 3.95)   // Comparison to 4.0 exactly may be causing an issue.
                    // Weighted so that annuals move entirely to standing dead, but are likely just a portion of the total herbs
                    // Should include death rate
                    // Edited to include water function in death rate for non - annual plants - 02 / 13 / 2013
                    death_rate = parms.shootDeathRate[1] * (1.0 - propAnnualDecid[Facet.herb]) * waterFunction;
                else
                    death_rate = parms.shootDeathRate[0] * waterFunction;
                if (month == Math.Round(parms.monthToRemoveAnnuals))
                    death_rate = death_rate + propAnnualDecid[Facet.herb];    // A one-time loss, so not corrected by month
                if (leafCarbon[Facet.herb] > parms.shootDeathRate[3])          // Shoot death rate 4 [3 in C#] stores g / m ^ 2, a threshold for shading affecting shoot death, stored in 3
                    death_rate = death_rate + parms.shootDeathRate[2];

                death_rate = Math.Min(1.0, death_rate);
                death_rate = Math.Max(0.0, death_rate);
                partBasedDeathRate[Facet.herb] = death_rate;
                death_carbon = death_rate * leafCarbon[Facet.herb];
                // Moving away from CENTURY here, which goes into labeled materials, etc.
                death_nitrogen = death_rate * leafNitrogen[Facet.herb];
                //  Do flows for plant parts
                // NOTE:  dead_leaf_carbon, dead_leaf_nitrogen are accumulators only, and cleared at the end of the month.They
                //        are not used in modeling.Leaves go from living to standing dead, and standing dead is the main operator.
                deadLeafCarbon[Facet.herb] = deadLeafCarbon[Facet.herb] + death_carbon;
                deadStandingCarbon[Facet.herb] = deadStandingCarbon[Facet.herb] + death_carbon;
                leafCarbon[Facet.herb] = leafCarbon[Facet.herb] - death_carbon;
                deadStandingNitrogen[Facet.herb] = deadStandingNitrogen[Facet.herb] + death_nitrogen;
                deadLeafNitrogen[Facet.herb] = deadLeafNitrogen[Facet.herb] + death_nitrogen;
                leafNitrogen[Facet.herb] = leafNitrogen[Facet.herb] - death_nitrogen;
                // Do flows to litter, keeping track of structural and metabolic components
                // Not using Partition_Litter here.  The leaves and shoots from herbs go to standing dead biomass.That is handled elsewhere
                // A portion of stored carbon for maintence respiration is loss associated with death of plant part.    ! FLOWS may be misnamed, or the wrong indicator.
                respirationFlows[Facet.herb] = respirationFlows[Facet.herb] - (respirationFlows[Facet.herb] * death_rate);
                // Death of seeds
                if (seedCarbon[Facet.herb] > 0.00001)
                    death_carbon = seedCarbon[Facet.herb] * (parms.fractionSeedsNotGerminated[Facet.herb] / 12.0);
                else
                    death_carbon = 0.0;

                if (seedNitrogen[Facet.herb] > 0.00001)
                    death_nitrogen = seedNitrogen[Facet.herb] * (parms.fractionSeedsNotGerminated[Facet.herb] / 12.0);
                else
                    death_nitrogen = 0.0;
                deadSeedCarbon[Facet.herb] = deadSeedCarbon[Facet.herb] + death_carbon;
                seedCarbon[Facet.herb] = seedCarbon[Facet.herb] - death_carbon;
                deadSeedNitrogen[Facet.herb] = deadSeedNitrogen[Facet.herb] + death_nitrogen;
                seedNitrogen[Facet.herb] = seedNitrogen[Facet.herb] - death_nitrogen;
                // Seeds won't play a role in respiration.  These seeds are destined for decomposition only.
                // Do flows to litter for seeds, keeping track of structural and metabolic components
                // Seeds are partitioned to litter in the main decomposition module, so removed here.
            }

            // Leaf death, Fine branch death, Coarse stem death, Fine root death, Coarse root death
            WoodyPlantPartDeath();

            for (int iFacet = 0; iFacet < nFacets; iFacet++)
            {
                // Simulate fall of standing dead to litter, for all facets
                if (deadStandingCarbon[iFacet] > 0.00001)
                    death_carbon = deadStandingCarbon[iFacet] * parms.fallRateOfStandingDead[iFacet];
                else
                    death_carbon = 0.0;
                if (deadStandingNitrogen[iFacet] > 0.00001)
                    death_nitrogen = deadStandingNitrogen[iFacet] * parms.fallRateOfStandingDead[iFacet];
                else
                    death_nitrogen = 0.0;

                // Do flows for plant parts
                deadStandingCarbon[iFacet] = deadStandingCarbon[iFacet] - death_carbon;
                deadStandingNitrogen[iFacet] = deadStandingNitrogen[iFacet] - death_nitrogen;
                // Do flows to litter, keeping track of structural and metabolic components
                PartitionLitter(surfaceIndex, death_carbon, death_nitrogen, plantLigninFraction[iFacet, surfaceIndex]);
            }

            // Simulate fire for all the facets
            // Decide whether fire is being modeled.   If maps are being used, then move ahead(too many possibilities to judge if fire is not occurring in the maps).
            // If maps are not being used, then check to see that the frequency of fire and the fraction burned are both greater than 0.
            // Otherwise, there is no fire.
            double proportion_cell_burned = 0.0;
            if (globe.fireMapsUsed != 0 || (globe.fireMapsUsed == 0 && parms.frequencyOfFire > 0.0 && parms.fractionBurned > 0.0))
            {
#if G_RANGE_BUG
                if (globe.fireMapsUsed != 0)
                {
                    // Model fire, with their occurrence determined in maps.The maps will store the proportion of each cell burned.
                    // No month is included here.  If someone wants to give detailed month - level fire maps, they may
                    proportion_cell_burned = globe.propBurned;
                }
                else
                {
                    // Fire based on probabilies and percentages
                    // Fire is confined to one month, otherwise the method would be overly complex, requiring checks to judge which months are appropriate.
                    if (parms.burnMonth == month)
                    {
                        // The cell may burn
                        double harvest = new Random().NextDouble();
                        if (parms.frequencyOfFire > harvest)
                            // Some portion of the cell will burn...
                            proportion_cell_burned = parms.fractionBurned;
                    }
                }
#else
                proportion_cell_burned = proportionCellBurned;
#endif

                // If some of the cell is to burn, do that
                if (proportion_cell_burned > 0.0009)
                {
                    // Calculate the intensity of the fire, using the method in SAVANNA
                    double fuel = 0.0;
                    double green = 0.0;
                    double prop_litter_burned = 0.0;
                    double burned_ash_c = 0.0;
                    double burned_ash_n = 0.0;
                    double prop_burned_carbon_ash = 0.0;
                    double prop_burned_nitrogen_ash = 0.0;
                    double[] data_val = new double[4];

                    for (int iFacet = 0; iFacet < nFacets; iFacet++)
                    {
                        fuel = fuel + leafCarbon[iFacet] + deadStandingCarbon[iFacet] +
                                      fineBranchCarbon[iFacet] + coarseBranchCarbon[iFacet];
                        green = green + leafCarbon[iFacet];

                        double perc_green = green / (fuel + 0.000001);
                        data_val[0] = parms.greenVsIntensity[0, 0];
                        data_val[1] = parms.greenVsIntensity[0, 1];
                        data_val[2] = parms.greenVsIntensity[1, 0];
                        data_val[3] = parms.greenVsIntensity[1, 1];
                        double effect_of_green = Linear(perc_green, data_val, 2);
                        data_val[0] = parms.fuelVsIntensity[0];
                        data_val[1] = 0.0;
                        data_val[2] = parms.fuelVsIntensity[1];
                        data_val[3] = 1.0;
                        fireSeverity = Linear(fuel, data_val, 2);

                        // Now calculate the proportion burned of different plant parts, and proportion ash
                        // data_val(2) = 0.0 and data_val(4) = 1.0 still, and in all that follows
                        data_val[0] = parms.fractionShootsBurned[iFacet, 0];
                        data_val[2] = parms.fractionShootsBurned[iFacet, 1];
                        // Proportion of cell burned is captured here, in these proportion burned entries that are used below.
                        double prop_shoots_burned = Linear(fireSeverity, data_val, 2) * proportion_cell_burned;
                        data_val[0] = parms.fractionStandingDeadBurned[iFacet, 0];
                        data_val[2] = parms.fractionStandingDeadBurned[iFacet, 1];
                        double prop_standing_dead_burned = Linear(fireSeverity, data_val, 2) * proportion_cell_burned;
                        data_val[0] = parms.fractionLitterBurned[iFacet, 0];
                        data_val[2] = parms.fractionLitterBurned[iFacet, 1];
                        prop_litter_burned = Linear(fireSeverity, data_val, 2) * proportion_cell_burned;
                        prop_burned_carbon_ash = parms.fractionBurnedCarbonAsAsh;
                        prop_burned_nitrogen_ash = parms.fractionBurnedNitrogenAsAsh;

                        // Burn the materials
                        // LEAVES
                        leafCarbon[iFacet] = leafCarbon[iFacet] - (leafCarbon[iFacet] * prop_shoots_burned);
                        burnedCarbon = burnedCarbon + (leafCarbon[iFacet] * prop_shoots_burned);
                        burned_ash_c = (leafCarbon[iFacet] * prop_shoots_burned) * prop_burned_carbon_ash;
                        leafNitrogen[iFacet] = leafNitrogen[iFacet] - (leafNitrogen[iFacet] * prop_shoots_burned);
                        burnedNitrogen = burnedNitrogen + (leafNitrogen[iFacet] * prop_shoots_burned);
                        burned_ash_n = (leafNitrogen[iFacet] * prop_shoots_burned) * prop_burned_nitrogen_ash;
                        // Do flows to litter, keeping track of structural and metabolic components
                        // Assume the fraction lignin does not change with combustion ... EDIT AS NEEDED
                        double frac_lignin = ligninLeaf[iFacet] / leafCarbon[iFacet];
                        frac_lignin = Math.Max(0.02, frac_lignin);    // From Century CmpLig.f
                        frac_lignin = Math.Min(0.50, frac_lignin);
                        PartitionLitter(surfaceIndex, burned_ash_c, burned_ash_n, frac_lignin);
                        // A portion of stored carbon for maintence respiration is loss associated with death of plant part.
                        // This is done only once, otherwise there would be multiple occurrences of the transfer        
                        respirationFlows[iFacet] = respirationFlows[iFacet] - (respirationFlows[iFacet] * prop_shoots_burned);

                        // SEEDS
                        seedCarbon[iFacet] = seedCarbon[iFacet] - (seedCarbon[iFacet] * prop_shoots_burned);
                        burnedCarbon = burnedCarbon + (seedCarbon[iFacet] * prop_shoots_burned);
                        burned_ash_c = (seedCarbon[iFacet] * prop_shoots_burned) * prop_burned_carbon_ash;
                        seedNitrogen[iFacet] = seedNitrogen[iFacet] - (seedNitrogen[iFacet] * prop_shoots_burned);
                        burnedNitrogen = burnedNitrogen + (seedNitrogen[iFacet] * prop_shoots_burned);
                        burned_ash_n = (seedNitrogen[iFacet] * prop_shoots_burned) * prop_burned_nitrogen_ash;
                        // Do flows to litter, keeping track of structural and metabolic components
                        // Assume the fraction lignin does not change with combustion... EDIT AS NEEDED
                        // Using leaf lignin for seed lignin, as an approximate
                        frac_lignin = ligninLeaf[iFacet] / leafCarbon[iFacet];
                        frac_lignin = Math.Max(0.02, frac_lignin);  // From Century CmpLig.f
                        frac_lignin = Math.Min(0.50, frac_lignin);
                        PartitionLitter(surfaceIndex, burned_ash_c, burned_ash_n, frac_lignin);

                        // FINE BRANCHES
                        fineBranchCarbon[iFacet] = fineBranchCarbon[iFacet] - (fineBranchCarbon[iFacet] * prop_shoots_burned);
                        burnedCarbon = burnedCarbon + (fineBranchCarbon[iFacet] * prop_shoots_burned);
                        burned_ash_c = (fineBranchCarbon[iFacet] * prop_shoots_burned) * prop_burned_carbon_ash;
                        fineBranchNitrogen[iFacet] = fineBranchNitrogen[iFacet] - (fineBranchNitrogen[iFacet] * prop_shoots_burned);
                        burnedNitrogen = burnedNitrogen + (fineBranchNitrogen[iFacet] * prop_shoots_burned);
                        burned_ash_n = (fineBranchNitrogen[iFacet] * prop_shoots_burned) * prop_burned_nitrogen_ash;
                        // Do flows to litter, keeping track of structural and metabolic components.BURNED MATERIALS are not going to standing dead
                        // Assume the fraction lignin does not change with combustion... EDIT AS NEEDED
                        frac_lignin = ligninFineBranch[iFacet] / fineBranchCarbon[iFacet];
                        frac_lignin = Math.Max(0.02, frac_lignin);   // From Century CmpLig.f
                        frac_lignin = Math.Min(0.50, frac_lignin);
                        PartitionLitter(surfaceIndex, burned_ash_c, burned_ash_n, frac_lignin);

                        // COARSE BRANCHES
                        coarseBranchCarbon[iFacet] = coarseBranchCarbon[iFacet] - (coarseBranchCarbon[iFacet] * prop_shoots_burned);
                        burnedCarbon = burnedCarbon + (coarseBranchCarbon[iFacet] * prop_shoots_burned);
                        burned_ash_c = (coarseBranchCarbon[iFacet] * prop_shoots_burned) * prop_burned_carbon_ash;
                        coarseBranchNitrogen[iFacet] = coarseBranchNitrogen[iFacet] - (coarseBranchNitrogen[iFacet] * prop_shoots_burned);
                        burnedNitrogen = burnedNitrogen + (coarseBranchNitrogen[iFacet] * prop_shoots_burned);
                        burned_ash_n = (coarseBranchNitrogen[iFacet] * prop_shoots_burned) * prop_burned_nitrogen_ash;
                        // Do flows to litter, keeping track of structural and metabolic components.BURNED MATERIALS are not going to standing dead
                        // Assume the fraction lignin does not change with combustion... EDIT AS NEEDED
                        frac_lignin = ligninCoarseBranch[iFacet] / coarseBranchCarbon[iFacet];
                        frac_lignin = Math.Max(0.02, frac_lignin);  // From Century CmpLig.f
                        frac_lignin = Math.Min(0.50, frac_lignin);
                        PartitionLitter(surfaceIndex, burned_ash_c, burned_ash_n, frac_lignin);

                        // STANDING DEAD
                        deadStandingCarbon[iFacet] = deadStandingCarbon[iFacet] - (deadStandingCarbon[iFacet] * prop_standing_dead_burned);
                        burnedCarbon = burnedCarbon + (deadStandingCarbon[iFacet] * prop_standing_dead_burned);
                        burned_ash_c = (deadStandingCarbon[iFacet] * prop_standing_dead_burned) * prop_burned_carbon_ash;
                        deadStandingNitrogen[iFacet] = deadStandingNitrogen[iFacet] - (deadStandingNitrogen[iFacet] * prop_standing_dead_burned);
                        burnedNitrogen = burnedNitrogen + (deadStandingNitrogen[iFacet] * prop_standing_dead_burned);
                        burned_ash_n = (deadStandingNitrogen[iFacet] * prop_standing_dead_burned) * prop_burned_nitrogen_ash;
                        // Do flows to litter, keeping track of structural and metabolic components.BURNED MATERIALS are not going to standing dead
                        // Assume the fraction lignin does not change with combustion... EDIT AS NEEDED
                        // Using leaf lignin for standing dead lignin, as an approximate
                        frac_lignin = ligninLeaf[iFacet] / leafCarbon[iFacet];
                        frac_lignin = Math.Max(0.02, frac_lignin);     // From Century CmpLig.f
                        frac_lignin = Math.Min(0.50, frac_lignin);
                        PartitionLitter(surfaceIndex, burned_ash_c, burned_ash_n, frac_lignin);
                    } // End of facet loop

                    // LITTER - STRUCTURAL CARBON
                    litterStructuralCarbon[surfaceIndex] = litterStructuralCarbon[surfaceIndex] -
                                                    (litterStructuralCarbon[surfaceIndex] * prop_litter_burned);
                    burnedCarbon = burnedCarbon + (litterStructuralCarbon[surfaceIndex] * prop_litter_burned);
                    burned_ash_c = (litterStructuralCarbon[surfaceIndex] * prop_litter_burned) * prop_burned_carbon_ash;
                    litterStructuralNitrogen[surfaceIndex] = litterStructuralNitrogen[surfaceIndex] -
                                     (litterStructuralNitrogen[surfaceIndex] * prop_litter_burned);
                    burnedNitrogen = burnedNitrogen + (litterStructuralNitrogen[surfaceIndex] * prop_litter_burned);
                    burned_ash_n = (litterStructuralNitrogen[surfaceIndex] * prop_litter_burned) * prop_burned_nitrogen_ash;

                    // Flows to litter not required, given litter is burning
                    // LITTER - METABOLIC CARBON
                    litterMetabolicCarbon[surfaceIndex] = litterMetabolicCarbon[surfaceIndex] -
                                   (litterMetabolicCarbon[surfaceIndex] * prop_litter_burned);
                    burnedCarbon = burnedCarbon + (litterMetabolicCarbon[surfaceIndex] * prop_litter_burned);
                    burned_ash_c = (litterMetabolicCarbon[surfaceIndex] + prop_litter_burned) * prop_burned_carbon_ash;
                    litterMetabolicNitrogen[surfaceIndex] = litterMetabolicNitrogen[surfaceIndex] -
                                   (litterMetabolicNitrogen[surfaceIndex] * prop_litter_burned);
                    burnedNitrogen = burnedNitrogen + (litterMetabolicNitrogen[surfaceIndex] * prop_litter_burned);
                    burned_ash_n = (litterStructuralNitrogen[surfaceIndex] * prop_litter_burned) * prop_burned_nitrogen_ash;
                    // Flows to litter not required, given litter is burning
                }
            }
            // else ... no fire
        }

        /// <summary>
        /// Woody plant material dies for various reasons.
        /// </summary>
        private void WoodyPlantPartDeath()
        {
            double temp_average = (globe.maxTemp + globe.minTemp) / 2.0;
            double death_carbon = 0.0;
            double death_nitrogen = 0.0;
            for (int iFacet = Facet.shrub; iFacet <= Facet.tree; iFacet++)
            {
                // Death of leaves
                if (leafCarbon[iFacet] > 0.0001)
                {
                    // Must account for deciduous leaves.  Century uses a death rate for non - deciduous months and another for deciduous months.  I am going to do the same, but the deciduous will be additive.
                    // Extremely complex if-then being replaced with a series of tests...
                    bool l1 = temp_average < parms.temperatureLeafOutAndFall[1];
                    bool l2 = !dayLengthIncreasing;
                    // The following cannot use .eq.months as CENTURY does.In CENTURY, a SAVE stores the state of "drop leaves" between months.  
                    // I will edit it to more closely match CENTURY, so that IN_WINTER *is *saved
                    bool l3 = !inWinter && Latitude > 0.0 && month == 12;
                    bool l4 = !inWinter && Latitude <= 0.0 && month == 6;
                    if ((l1 && l2) || l3 || l4)
                        inWinter = true;

#if !G_RANGE_BUG
                    l1 = temp_average > parms.temperatureLeafOutAndFall[0];
                    l2 = dayLengthIncreasing;
                    // The following cannot use .eq.months as CENTURY does.In CENTURY, a SAVE stores the state of "drop leaves" between months.  
                    // I will edit it to more closely match CENTURY, so that IN_WINTER *is *saved
                    l3 = inWinter && Latitude > 0.0 && month == 6;
                    l4 = inWinter && Latitude <= 0.0 && month == 12;
                    if ((l1 && l2) || l3 || l4)
                        inWinter = false;
#endif

                    // Do the base line leaf death rate
                    death_carbon = leafCarbon[iFacet] * parms.leafDeathRate[iFacet];
                    // Century has an option to adjust death rate associated with element concentration, but I will skip that.
                    // And incorporate deciduous death.That's the proportion that are deciduous times the carbon times the death rate)
                    if (inWinter && propAnnualDecid[iFacet] > 0.001)
                        death_carbon = death_carbon + (propAnnualDecid[iFacet] * (leafCarbon[iFacet] * parms.deathRateOfDeciduousLeaves));
                    if (parms.droughtDeciduous[iFacet] > 0.0001)
                        death_carbon = death_carbon + ((leafCarbon[iFacet] * (1.0 - waterFunction) * parms.deathRateOfDeciduousLeaves) * parms.droughtDeciduous[iFacet]);
                    deadStandingCarbon[iFacet] = deadStandingCarbon[iFacet] + death_carbon;
                    // Make sure the following is only a placeholder.Death_Carbon should not accumulate twice.If it is happening, only use standing dead carbon.
                    deadLeafCarbon[iFacet] = deadLeafCarbon[iFacet] + death_carbon;
                    // Calculate the proportion of carbon that has been killed
                    // A different pathway than in Century.I may misinterpret, but perhaps it is just simplier given the structure I have used.
                    double c_prop;
                    if (leafCarbon[iFacet] > 0.0001)
                        c_prop = death_carbon / leafCarbon[iFacet];
                    else
                        c_prop = 0.0;
                    leafCarbon[iFacet] = leafCarbon[iFacet] - death_carbon;
                    double to_storage = leafNitrogen[iFacet] * c_prop * parms.fractionWoodyLeafNTranslocated;
                    storedNitrogen[iFacet] = storedNitrogen[iFacet] + to_storage;
                    death_nitrogen = (leafNitrogen[iFacet] * c_prop) - to_storage;
                    leafNitrogen[iFacet] = leafNitrogen[iFacet] - death_nitrogen;
                    deadStandingNitrogen[iFacet] = deadStandingNitrogen[iFacet] + death_nitrogen;
                    deadLeafNitrogen[iFacet] = deadLeafNitrogen[iFacet] + death_nitrogen;

                    // A portion of stored carbon for maintence respiration is loss associated with death of plant part.    ! FLOWS may be misnamed, or the wrong indicator.
                    respirationFlows[Facet.herb] = respirationFlows[Facet.herb] - (respirationFlows[Facet.herb] * c_prop);

                    // Do flows to litter, keeping track of structural and metabolic components
                    // DON'T PASS TO LITTER UNTIL STANDING DEAD LEAVES FALL.  CENTURY doesn't use standing dead for woody plants, but leaves can take some time to fall, so I will use them here.
                }

                // Death of seeds
                if (seedCarbon[iFacet] > 0.0)
                    death_carbon = seedCarbon[iFacet] * (parms.fractionSeedsNotGerminated[iFacet] / 12.0);
                else
                    death_carbon = 0.0;
                if (seedNitrogen[iFacet] > 0.0)
                    death_nitrogen = seedNitrogen[iFacet] * (parms.fractionSeedsNotGerminated[iFacet] / 12.0);
                else
                    death_nitrogen = 0.0;
                deadSeedCarbon[iFacet] = deadSeedCarbon[iFacet] + death_carbon;
                seedCarbon[iFacet] = seedCarbon[iFacet] - death_carbon;
                deadSeedNitrogen[iFacet] = deadSeedNitrogen[iFacet] + death_nitrogen;
                seedNitrogen[iFacet] = seedNitrogen[iFacet] - death_nitrogen;
                // Seeds won't play a role in respiration.  These seeds are destined for decomposition only.
                // Seeds don't move to standing dead.  They are viable until they drop from the plant and a portion becomes litter.
                // Do flows to litter for seeds, keeping track of structural and metabolic components
                // Seed litter is already partitioned in the main decomposition routine.Commented out here.

                // Death of fine branches
                if (fineBranchCarbon[iFacet] > 0.0)
                    death_carbon = fineBranchCarbon[iFacet] * parms.fineBranchDeathRate[iFacet];
                else
                    death_carbon = 0.0;
                fineBranchCarbon[iFacet] = fineBranchCarbon[iFacet] - death_carbon;
                deadFineBranchCarbon[iFacet] = deadFineBranchCarbon[iFacet] + death_carbon;
                // Fine branches should remain in standing dead until fall
                deadStandingCarbon[iFacet] = deadStandingCarbon[iFacet] + death_carbon;
                if (fineBranchNitrogen[iFacet] > 0.0001)
                    death_nitrogen = fineBranchNitrogen[iFacet] * parms.fineBranchDeathRate[iFacet];
                else
                    death_nitrogen = 0.0;
                fineBranchNitrogen[iFacet] = fineBranchNitrogen[iFacet] - death_nitrogen;
                deadFineBranchNitrogen[iFacet] = deadFineBranchNitrogen[iFacet] + death_nitrogen;
                deadStandingNitrogen[iFacet] = deadStandingNitrogen[iFacet] + death_nitrogen;
                // WAIT until standing dead falls to litter before partitioning... FINE BRANCHES JOIN STANDING DEAD

                // Death of fine roots
                if (fineRootCarbon[iFacet] > 0.0)
                    death_carbon = fineRootCarbon[iFacet] * parms.fineRootDeathRate[iFacet];
                else
                    death_carbon = 0.0;
                fineRootCarbon[iFacet] = fineRootCarbon[iFacet] - death_carbon;
                deadFineRootCarbon[iFacet] = deadFineRootCarbon[iFacet] + death_carbon;
                if (fineRootNitrogen[iFacet] > 0.00001)
                    death_nitrogen = fineRootNitrogen[iFacet] * parms.fineRootDeathRate[iFacet];
                else
                    death_nitrogen = 0.0;
                fineRootNitrogen[iFacet] = fineRootNitrogen[iFacet] - death_nitrogen;
                deadFineRootNitrogen[iFacet] = deadFineRootNitrogen[iFacet] + death_nitrogen;
                // Do flows to litter, keeping track of structural and metabolic components
                // Fine roots are already partitioned in the main decomposition module.  

                // Death of coarse branches
                if (coarseBranchCarbon[iFacet] > 0.00001)
                    death_carbon = coarseBranchCarbon[iFacet] * parms.coarseBranchDeathRate[iFacet];
                else
                    death_carbon = 0.0;
                partBasedDeathRate[iFacet] = parms.coarseBranchDeathRate[iFacet];
                coarseBranchCarbon[iFacet] = coarseBranchCarbon[iFacet] - death_carbon;
                deadCoarseBranchCarbon[iFacet] = deadCoarseBranchCarbon[iFacet] + death_carbon;
                if (coarseBranchNitrogen[iFacet] > 0.00001)
                    death_nitrogen = coarseBranchNitrogen[iFacet] * parms.coarseBranchDeathRate[iFacet];
                else
                    death_nitrogen = 0.0;
                coarseBranchNitrogen[iFacet] = coarseBranchNitrogen[iFacet] - death_nitrogen;
                deadCoarseBranchNitrogen[iFacet] = deadCoarseBranchNitrogen[iFacet] + death_nitrogen;

                // Death of coarse root
                if (coarseRootCarbon[iFacet] > 0.00001)
                    death_carbon = coarseRootCarbon[iFacet] * parms.coarseRootDeathRate[iFacet];
                else
                    death_carbon = 0.0;
                coarseRootCarbon[iFacet] = coarseRootCarbon[iFacet] - death_carbon;
                deadCoarseRootCarbon[iFacet] = deadCoarseRootCarbon[iFacet] + death_carbon;
                if (coarseRootNitrogen[iFacet] > 0.00001)
                    death_nitrogen = coarseRootNitrogen[iFacet] * parms.coarseRootDeathRate[iFacet];
                else
                    death_nitrogen = 0.0;
                coarseRootNitrogen[iFacet] = coarseRootNitrogen[iFacet] - death_nitrogen;
                deadCoarseRootNitrogen[iFacet] = deadCoarseRootNitrogen[iFacet] + death_nitrogen;
            }
        }

        /// <summary>
        /// Simulate whole plant deaths
        /// To represent populations, I will use an area 1 km x 1 km, and explicit tallying of space-filling plants.  There is the
        /// option to do this for entire cells modeled, but their areas will vary across the globe.  Moreover, none of the results
        /// will be reported on a per-cell basis, so the results from the 1 km^2 area will be suitable for reporting covers and
        /// concentrations, etc.The 1 km^2 area was selected because it is convenient and sufficiently large enough to minimize
        /// rounding effects.  For example, if the effective root area of a tree is 8 x 8 m, 15,625 trees could fit within the
        ///1 km^2 area.Many thousands of herbs could fit, etc.I speak of root area rather than volume because the soil depths
        /// and layers define volume (i.e., herbs have access to the first two layers, shrubs up to layer three, trees all four
        /// layers).  Incidentally, an herb that occupies 0.2 x 0.2 m would fill the entire 1 km^2 area with 25,000,000 individuals,
        /// so there is no need for specially defined storage spaces(double and the like)
        /// </summary>
        private void WholePlantDeath()
        {
            double temperature = (globe.minTemp + globe.maxTemp) / 2.0;
            double[] data_val = new double[4];
            int iLayer;

            // Note that this is whole plant death and removal.This is not related to standing dead on scenecent perennials, etc.  But to count flows here
            // or not?  There is a risk of double-counting flows of nutrients and stocks.NO... nutrient flows are on a cell-basis, not captured here.

            // Death occurs mostly when plants are not dormant.  Here I will use temperature as a surrogate for that.I could use phenology, or a dormancy flag.
            if (temperature > 0.0)
            {
                int[] died = new int[6]; // temporary for debugging
                for (int iFacet = 0; iFacet < nFacets; iFacet++)
                {
                    double death_rate = parms.nominalPlantDeathRate[iFacet];
                    // Probably pass correctly, but to decrease risk of error ...
                    // Those at the facet level are handled outside of the layers level
                    for (int i = 0; i < 4; i++)
                        data_val[i] = parms.waterEffectOnDeathRate[iFacet, i];
                    death_rate = death_rate + Linear(ratioWaterPet, data_val, 2);
                    for (int i = 0; i < 4; i++)
                        data_val[i] = parms.grazingEffectOnDeathRate[iFacet, i];
                    death_rate = death_rate + Linear(fractionLiveRemovedGrazing, data_val, 2);
                    double temp_rate = death_rate;   // Save the death rate up to this point, so that recalculating based on LAI below won't keep adding to death rate for the different layers
                    // Incorproate annuals.  Do so after the season has ended and standing dead has fallen to litter, etc. The best time to account for
                    // annual death may be the beginning of the following year, when phenology is reset to 0.
                    if (month == Math.Round(parms.monthToRemoveAnnuals) && iFacet == Facet.herb)
                        death_rate = death_rate + propAnnualDecid[Facet.herb];  
                    // EJZ - The C# compiler points out that the assignment above is futile; the value is never used.
#if CONSTRAIN_MODEL
                    temp_rate = Math.Min(death_rate, partBasedDeathRate[iFacet]);
#endif
                    // Plants die regardless of their placement, whether in the understory of another plant, or defining a facet.   The rate is the same, except for LAI effects.  
                    // Kill the plants...
                    // Shading effect on death rate is at the facet level, but the leaf area index is at the layer level, so store
                    // death rate in temp_rate and use that in the following functions.
                    for (int i = 0; i < 4; i++)
                        data_val[i] = parms.shadingEffectOnDeathRate[iFacet, i];
                    switch (iFacet)
                    {
                        case Facet.herb:
                            // Do the three herbaceous layers, open, under shrubs, and under trees
                            for (iLayer = Layer.herb; iLayer <= Layer.herbUnderTree; iLayer++)
                            {
                                death_rate = temp_rate + Linear(leafAreaIndex[Facet.herb], data_val, 2);
                                death_rate = Math.Min(1.0, death_rate);
                                death_rate = Math.Max(0.0, death_rate);
                                died[iLayer] = (int)(totalPopulation[iLayer] * death_rate);
                                totalPopulation[iLayer] = totalPopulation[iLayer] - (totalPopulation[iLayer] * death_rate);
                            }
                            break;
                        case Facet.shrub:
                            // Do the two shrub layers, open, and under trees
                            for (iLayer = Layer.shrub; iLayer <= Layer.shrubUnderTree; iLayer++)
                            {
                                death_rate = temp_rate + Linear(leafAreaIndex[Facet.shrub], data_val, 2);
                                death_rate = Math.Min(1.0, death_rate);
                                death_rate = Math.Max(0.0, death_rate);
                                died[iLayer] = (int)(totalPopulation[iLayer] * death_rate);
                                totalPopulation[iLayer] = totalPopulation[iLayer] - (totalPopulation[iLayer] * death_rate);
                            }
                            break;
                        case Facet.tree:
                            // Do the tree layer, open
                            iLayer = Layer.tree;
                            death_rate = temp_rate + Linear(leafAreaIndex[Facet.tree], data_val, 2);
                            death_rate = Math.Min(1.0, death_rate);
                            death_rate = Math.Max(0.0, death_rate);
                            died[iLayer] = (int)(totalPopulation[iLayer] * death_rate);
                            totalPopulation[iLayer] = totalPopulation[iLayer] - (totalPopulation[iLayer] * death_rate);
                            break;
                    }
                }

                // Now calculate facet covers.  This is much streamlined with the 6 - layer approach than the 3 - facet only approach.
                // ilayers 1, 4, and 6 [0, 3 and 5 in C#] are the overstory layers defining facets directly.

                // TREES
                facetCover[Facet.tree] = (totalPopulation[Layer.tree] * parms.indivPlantArea[Facet.tree]) / refArea;
                // SHRUBS
                facetCover[Facet.shrub] = (totalPopulation[Layer.shrub] * parms.indivPlantArea[Facet.shrub]) / refArea;
                // HERBS
                facetCover[Facet.herb] = (totalPopulation[Layer.herb] * parms.indivPlantArea[Facet.herb]) / refArea;

                // And update bare ground proportion.
                bareCover = (1.0 - (facetCover[Facet.tree] + facetCover[Facet.shrub] + facetCover[Facet.herb]));
            }

            // Fire will be handled separately, because fire may occur when temperatures are below freezing. A little duplicative, but no matter.
            // Decide whether fire is being modeled.   If maps are being used, then move ahead(too many possibilities to judge if fire is not occurring in the maps).
            // If maps are not being used, then check to see that the frequency of fire and the fraction burned are both greater than 0.
            // Otherwise, there is no fire.
            if (globe.fireMapsUsed != 0 || (globe.fireMapsUsed == 0 && parms.frequencyOfFire > 0.0 && parms.fractionBurned > 0.0))
            {
#if G_RANGE_BUG
                double proportion_cell_burned = 0.0;
                if (globe.fireMapsUsed != 0)
                {
                    // Model fire, with their occurrence determined in maps.The maps will store the proportion of each cell burned.
                    // No month is included here.  If someone wants to give detailed month - level fire maps, they may
                    proportion_cell_burned = globe.propBurned;
                }
                else
                {
                    // Fire based on probabilies and percentages
                    // Fire is confined to one month, otherwise the method would be overly complex, requiring checks to judge which months are appropriate.
                    if (parms.burnMonth == month)
                    {
                        // The cell may burn
                        // EJZ - Is this right? We also have a similar random value in PlantPartDeath, so the two are not going to
                        // coincide. Shouldn't there just be one fire "event"?
                        double harvest = new Random().NextDouble();
                        if (parms.frequencyOfFire > harvest)
                        {
                            // Some portion of the cell will burn...
                            proportion_cell_burned = parms.fractionBurned;
                        }
                    }
                }
#else
                double proportion_cell_burned = proportionCellBurned;
#endif

                // If some of the cell is to burn, do that, removing whole dead plants
                if (proportion_cell_burned > 0.0)
                {
                    for (int iFacet = 0; iFacet < nFacets; iFacet++)
                    {
                        // Use fire severity, already calculated in modeling plant part death.
                        // Calculate the proportion of plants to be killed.
                        data_val[1] = 0.0;
                        data_val[3] = 1.0;
                        data_val[0] = parms.fractionPlantsBurnedDead[iFacet, 0];
                        data_val[2] = parms.fractionPlantsBurnedDead[iFacet, 1];
                        double proportion_plants_killed = Linear(fireSeverity, data_val, 2) * proportion_cell_burned;

                        switch (iFacet)
                        {
                            case Facet.herb:
                                iLayer = Layer.herb;
                                // Porportion of plants to be killed by fire ... 
                                totalPopulation[iLayer] = totalPopulation[iLayer] - (totalPopulation[iLayer] * proportion_plants_killed);
                                iLayer = Layer.herbUnderShrub;
                                // Porportion of plants to be killed by fire ... 
                                totalPopulation[iLayer] = totalPopulation[iLayer] - (totalPopulation[iLayer] * proportion_plants_killed);
                                iLayer = Layer.herbUnderTree;
                                // Porportion of plants to be killed by fire ... 
                                totalPopulation[iLayer] = totalPopulation[iLayer] - (totalPopulation[iLayer] * proportion_plants_killed);
                                break;
                            case Facet.shrub:
                                iLayer = Layer.shrub;
                                // Porportion of plants to be killed by fire ... 
                                totalPopulation[iLayer] = totalPopulation[iLayer] - (totalPopulation[iLayer] * proportion_plants_killed);
                                iLayer = Layer.shrubUnderTree;
                                // Porportion of plants to be killed by fire ... 
                                totalPopulation[iLayer] = totalPopulation[iLayer] - (totalPopulation[iLayer] * proportion_plants_killed);
                                break;
                            case Facet.tree:
                                iLayer = Layer.tree;
                                // Porportion of plants to be killed by fire ... 
                                totalPopulation[iLayer] = totalPopulation[iLayer] - (totalPopulation[iLayer] * proportion_plants_killed);
                                break;
                        }
                    }
                    // Now recalculate facet covers.  This is much streamlined with the 6 - layer approach than the 3 - facet only approach.
                    // ilayers 1, 4, and 6 [0, 3 and 5 in C#] are the overstory layers defining facets directly.

                    // TREES
                    facetCover[Facet.tree] = (totalPopulation[Layer.tree] * parms.indivPlantArea[Facet.tree]) / refArea;
                    // SHRUBS
                    facetCover[Facet.shrub] = (totalPopulation[Layer.shrub] * parms.indivPlantArea[Facet.shrub]) / refArea;
                    // HERBS
                    facetCover[Facet.herb] = (totalPopulation[Layer.herb] * parms.indivPlantArea[Facet.herb]) / refArea;

                    // And update bare ground proportion.
                    bareCover = (1.0 - (facetCover[Facet.tree] + facetCover[Facet.shrub] + facetCover[Facet.herb]));
                }
            }
        }

        /// <summary>
        /// Read in the maps that are associated with fire or fertilization, or other management.
        /// </summary>
        private void Management()
        {
            // Simulate fertilization.This will almost certainly be set through maps, but perhaps deposition or the like may be simulated
            // using the probability option, so I will leave it.It is there because of the parallels with fire.
            if (globe.fertilizeMapsUsed != 0 || (globe.fertilizeMapsUsed == 0 && parms.frequencyOfFertilization > 0.0 && parms.fractionFertilized > 0.0))
            {
                double proportion_cell_fertilized = 0.0;
                if (globe.fertilizeMapsUsed != 0)
                {
                    // Model fire, with their occurrence determined in maps.The maps will store the proportion of each cell burned.
                    // No month is included here.  If someone wants to give detailed month - level fire maps, they may
                    proportion_cell_fertilized = globe.propFertilized;
                }
                else
                {
                    // Fertilized based on probabilies and percentages
                    // Fertilized is confined to one month, otherwise the method would be overly complex, requiring checks to judge which months are appropriate.
                    if (parms.fertilizeMonth == month)
                    {
                        // The cell may burn
                        double harvest = new Random().NextDouble();
                        if (parms.frequencyOfFertilization > harvest)
                        {
                            // Some portion of the cell will burn...
                            proportion_cell_fertilized = parms.fractionFertilized;
                        }
                    }
                }

                // If some of the cell is to be fertilized, do that
                if (proportion_cell_fertilized > 0.0)
                {
                    mineralNitrogen[surfaceIndex] = mineralNitrogen[surfaceIndex] + (parms.fertilizeNitrogenAdded * parms.fractionFertilized);
                    fertilizedNitrogenAdded = fertilizedNitrogenAdded + (parms.fertilizeNitrogenAdded * parms.fractionFertilized);
                    // Add organic matter if requested.It gets partitioned in litter, following CENTURY
                    // NOTE that the lignin ratio is hardwired here.
                    double c_added = parms.fertilizeCarbonAdded * parms.fractionFertilized;
                    double n_added = parms.fertilizeNitrogenAdded * parms.fractionFertilized;
                    double organic_fert_lignin = 0.20;
                    PartitionLitter(soilIndex, c_added, n_added, organic_fert_lignin);
                    fertilizedNitrogenAdded = fertilizedNitrogenAdded + n_added;
                    fertilizedCarbonAdded = fertilizedCarbonAdded + c_added;
                }
            }
            // else ... no fertilization
        }

        /// <summary>
        /// Simulate whole plant reproduction
        /// </summary>
        private void PlantReproduction()
        {
            double temperature = (globe.minTemp + globe.maxTemp) / 2.0;

            // Establishment occurs only when plants are not dormant.  Here I will use temperature as a surrogate for that.I could use phenology, or a dormancy flag.
            if (temperature > 0.0)
            {
                // Get the empty space, to determine establishment
                // Note: As long as a plant is alive, it maintains control of that area.  This is based on herbaceous size classes
                // only, since even though the plants reserve their full size, they can become established on a smaller area.  Really it is more
                // about the logic, so that any given herb-size patch has a chance to be occupeid by an herb, shrub, or tree... or nothing.
                // Note that deaths have already been accounted for, and bare cover updated.
                // The only means of shifts in facet cover is through differential allotment of bare cover to the three facets.
                // So I am going to calculate those values first, then do the three special cases after the main facets are attended to.

                double[] data_val = new double[4];
                double[] relative_establishment = new double[nLayers];
                relative_establishment[Layer.herb] = parms.relativeSeedProduction[Facet.herb];
                relative_establishment[Layer.herbUnderShrub] = parms.relativeSeedProduction[Facet.herb];
                relative_establishment[Layer.herbUnderTree] = parms.relativeSeedProduction[Facet.herb];
                relative_establishment[Layer.shrub] = parms.relativeSeedProduction[Facet.shrub];
                relative_establishment[Layer.shrubUnderTree] = parms.relativeSeedProduction[Facet.shrub];
                relative_establishment[Layer.tree] = parms.relativeSeedProduction[Facet.tree];

                //    write(ECHO_FILE, '(A40, I5, F9.3, 6(F10.3))') ' REL_EST pre water FACETS: ',icell, &
                // Rng(icell) % ratio_water_pet, relative_establishment
                // Do water limitations
                for (int i = 0; i < 4; i++)
                    data_val[i] = parms.waterEffectOnEstablish[Facet.herb, i];
                if (ratioWaterPet < 0.0 || ratioWaterPet >= vLarge)
                    ratioWaterPet = 0.0;
                relative_establishment[Layer.herb] = relative_establishment[Layer.herb] * Linear(ratioWaterPet, data_val, 2);
                relative_establishment[Layer.herbUnderShrub] = relative_establishment[Layer.herbUnderShrub] * Linear(ratioWaterPet, data_val, 2);
                relative_establishment[Layer.herbUnderTree] = relative_establishment[Layer.herbUnderTree] * Linear(ratioWaterPet, data_val, 2);
                for (int i = 0; i < 4; i++)
                    data_val[i] = parms.waterEffectOnEstablish[Facet.shrub, i];
                relative_establishment[Layer.shrub] = relative_establishment[Layer.shrub] * Linear(ratioWaterPet, data_val, 2);
                relative_establishment[Layer.shrubUnderTree] = relative_establishment[Layer.shrubUnderTree] * Linear(ratioWaterPet, data_val, 2);
                for (int i = 0; i < 4; i++)
                    data_val[i] = parms.waterEffectOnEstablish[Facet.tree, i];
                relative_establishment[Layer.tree] = relative_establishment[Layer.tree] * Linear(ratioWaterPet, data_val, 2);

                // Do litter limitation
                for (int i = 0; i < 4; i++)
                    data_val[i] = parms.litterEffectOnEstablish[Facet.herb, i];
                if (totalLitterCarbon[surfaceIndex] < 0.0 || totalLitterCarbon[surfaceIndex] >= vLarge)
                    totalLitterCarbon[surfaceIndex] = 0.0;
                relative_establishment[Layer.herb] = relative_establishment[Layer.herb] * Linear((totalLitterCarbon[surfaceIndex] * 2.5), data_val, 2);
                relative_establishment[Layer.herbUnderShrub] = relative_establishment[Layer.herbUnderShrub] * Linear((totalLitterCarbon[surfaceIndex] * 2.5), data_val, 2);
                relative_establishment[Layer.herbUnderTree] = relative_establishment[Layer.herbUnderTree] * Linear((totalLitterCarbon[surfaceIndex] * 2.5), data_val, 2);
                for (int i = 0; i < 4; i++)
                    data_val[i] = parms.litterEffectOnEstablish[Facet.shrub, i];
                relative_establishment[Layer.shrub] = relative_establishment[Layer.shrub] * Linear((totalLitterCarbon[surfaceIndex] * 2.5), data_val, 2);
                relative_establishment[Layer.shrubUnderTree] = relative_establishment[Layer.shrubUnderTree] * Linear((totalLitterCarbon[surfaceIndex] * 2.5), data_val, 2);
                for (int i = 0; i < 4; i++)
                    data_val[i] = parms.litterEffectOnEstablish[Facet.tree, i];
                relative_establishment[Layer.tree] = relative_establishment[Layer.tree] * Linear((totalLitterCarbon[surfaceIndex] * 2.5), data_val, 2);

                // Do limitation due to herbaceous root biomass limiations.Only empty patches are candidates for establishment, but that
                // is taking the logic too rigorously.Root biomass will play a role, as represented in the non - spatial portion of the model.
                for (int i = 0; i < 4; i++)
                    data_val[i] = parms.herbRootEffectOnEstablish[Facet.herb, i];
                if (fineRootCarbon[Facet.herb] < 0.0 || fineRootCarbon[Facet.herb] >= vLarge)
                    fineRootCarbon[Facet.herb] = 0.0;
                relative_establishment[Layer.herb] = relative_establishment[Layer.herb] * Linear((fineRootCarbon[Facet.herb] * 2.5), data_val, 2);
                relative_establishment[Layer.herbUnderShrub] = relative_establishment[Layer.herbUnderShrub] * Linear((fineRootCarbon[Facet.herb] * 2.5), data_val, 2);
                relative_establishment[Layer.herbUnderTree] = relative_establishment[Layer.herbUnderTree] * Linear((fineRootCarbon[Facet.herb] * 2.5), data_val, 2);
                for (int i = 0; i < 4; i++)
                    data_val[i] = parms.herbRootEffectOnEstablish[Facet.shrub, i];
                relative_establishment[Layer.shrub] = relative_establishment[Layer.shrub] * Linear((fineRootCarbon[Facet.herb] * 2.5), data_val, 2);
                relative_establishment[Layer.shrubUnderTree] = relative_establishment[Layer.shrubUnderTree] * Linear((fineRootCarbon[Facet.herb] * 2.5), data_val, 2);
                for (int i = 0; i < 4; i++)
                    data_val[i] = parms.herbRootEffectOnEstablish[Facet.tree, i];
                relative_establishment[Layer.tree] = relative_establishment[Layer.tree] * Linear((fineRootCarbon[Facet.herb] * 2.5), data_val, 2);

                //    write(ECHO_FILE, 998) ' REL_EST pre wood FACETS: ',icell,Rng(icell) % facet_cover(S_FACET), &
                // Rng(icell) % facet_cover(T_FACET), relative_establishment
                // Do woody cover limitation ... this is applicable to most of the entries, but not all.
                for (int i = 0; i < 4; i++)
                    data_val[i] = parms.woodyCoverEffectOnEstablish[Facet.herb, i];
                relative_establishment[Layer.herbUnderShrub] = relative_establishment[Layer.herbUnderShrub] * Linear(facetCover[Facet.shrub], data_val, 2);
                relative_establishment[Layer.herbUnderTree] = relative_establishment[Layer.herbUnderTree] * Linear(facetCover[Facet.tree], data_val, 2);
                for (int i = 0; i < 4; i++)
                    data_val[i] = parms.woodyCoverEffectOnEstablish[Facet.shrub, i];
                relative_establishment[Layer.shrub] = relative_establishment[Layer.shrub] * Linear(facetCover[Facet.shrub], data_val, 2);
                relative_establishment[Layer.shrubUnderTree] = relative_establishment[Layer.shrubUnderTree] * Linear(facetCover[Facet.tree], data_val, 2);
                for (int i = 0; i < 4; i++)
                    data_val[i] = parms.woodyCoverEffectOnEstablish[Facet.tree, i];
                relative_establishment[Layer.tree] = relative_establishment[Layer.tree] * Linear(facetCover[Facet.tree], data_val, 2);

                // Calculate total seed production, to allow competition between facets for the empty patches.           
                // This needs to be based on populations, rather than facet area, because understory plants may produce seed as well.
                // Baseline relative seed production, incorporating total population size for the facet.

                double[] tot_pop = new double[nFacets];
                tot_pop[Facet.herb] = totalPopulation[Layer.herb] + totalPopulation[Layer.herbUnderShrub] + totalPopulation[Layer.herbUnderTree];
                tot_pop[Facet.shrub] = totalPopulation[Layer.shrub] + totalPopulation[Layer.shrubUnderTree];
                tot_pop[Facet.tree] = totalPopulation[Layer.tree];

                // Potential plants established
                double[] pot_established = new double[nLayers];
                pot_established[Layer.herb] = tot_pop[Facet.herb] * relative_establishment[Layer.herb];
                pot_established[Layer.herbUnderShrub] = tot_pop[Facet.herb] * relative_establishment[Layer.herbUnderShrub];
                pot_established[Layer.herbUnderTree] = tot_pop[Facet.herb] * relative_establishment[Layer.herbUnderTree];
                pot_established[Layer.shrub] = tot_pop[Facet.shrub] * relative_establishment[Layer.shrub];
                pot_established[Layer.shrubUnderTree] = tot_pop[Facet.shrub] * relative_establishment[Layer.shrubUnderTree];
                pot_established[Layer.tree] = tot_pop[Facet.tree] * relative_establishment[Layer.tree];
                // 995 format(A40, I5, 6(I15))
                // Now plant the plants that will fit.The ordering will be critical here.          
                // First, trees will take precedence, shading out the other facets, and recognizing that woody plant limitations were considered above.     
                // Second, shrubs will take precedence.Third, herbs will be planted.
                // Plants are being put in place full - size.
                // Recall that death is already simulated at this point - facets will not shrink, but they may grow.The order in which they grow is relavant.

                // TREE
                totalPopulation[Layer.tree] = totalPopulation[Layer.tree] + pot_established[Layer.tree];
                if (totalPopulation[Layer.tree] > parms.potPopulation[Facet.tree])
                    totalPopulation[Layer.tree] = parms.potPopulation[Facet.tree];
                facetCover[Facet.tree] = (totalPopulation[Layer.tree] * parms.indivPlantArea[Facet.tree]) / refArea;

                // SHRUB
                // First, adjust pot_population(S_FACET) to incorporate the tree facet.
                double max_population = parms.potPopulation[Facet.shrub] * (1.0 - facetCover[Facet.tree]);  // The pot_pop is at 100 %.So if trees are 30 %, then pot_pop *0.70 would be the maximum shrubs could occupy.Again, an ordered approach, with trees, then shrubs, then herbs.
                totalPopulation[Layer.shrub] = totalPopulation[Layer.shrub] + pot_established[Layer.shrub];
                if (totalPopulation[Layer.shrub] > max_population)
                    totalPopulation[Layer.shrub] = max_population;
                facetCover[Facet.shrub] = (totalPopulation[Layer.shrub] * parms.indivPlantArea[Facet.shrub]) / refArea;

                // HERB
                // First, adjust pot_population(H_FACET) to incorporate the tree and shrub facets.
                max_population = parms.potPopulation[Facet.herb] * (1.0 - (facetCover[Facet.tree] + facetCover[Facet.shrub]));  // The pot_pop is at 100 %.So if trees are 30 %, then pot_pop *0.70 would be the maximum shrubs could occupy.Again, an ordered approach, with trees, then shrubs, then herbs.
                totalPopulation[Layer.herb] = totalPopulation[Layer.herb] + pot_established[Layer.herb];
                if (totalPopulation[Layer.herb] > max_population)
                    totalPopulation[Layer.herb] = max_population;
                facetCover[Facet.herb] = (totalPopulation[Layer.herb] * parms.indivPlantArea[Facet.herb]) / refArea;

                // HERBS UNDER SHRUBS
                max_population = (parms.potPopulation[Facet.herb] * facetCover[Facet.shrub] * parms.indivPlantArea[Facet.shrub]) /
                                parms.indivPlantArea[Facet.herb];  // Parameter, so no division by 0 likely.The same is true below.
                totalPopulation[Layer.herbUnderShrub] = totalPopulation[Layer.herbUnderShrub] + pot_established[Layer.herbUnderShrub];
                if (totalPopulation[Layer.herbUnderShrub] > max_population)
                    totalPopulation[Layer.herbUnderShrub] = max_population;

                // SHRUBS UNDER TREES
                max_population = (parms.potPopulation[Facet.shrub] * facetCover[Facet.tree] * parms.indivPlantArea[Facet.tree]) /
                                parms.indivPlantArea[Facet.shrub];
                totalPopulation[Layer.shrubUnderTree] = totalPopulation[Layer.shrubUnderTree] + pot_established[Layer.shrubUnderTree];
                if (totalPopulation[Layer.shrubUnderTree] > max_population)
                    totalPopulation[Layer.shrubUnderTree] = max_population;

                // HERBS UNDER TREES
                max_population = (parms.potPopulation[Facet.herb] * facetCover[Facet.tree] * parms.indivPlantArea[Facet.tree]) /
                                parms.indivPlantArea[Facet.herb];
                totalPopulation[Layer.herbUnderTree] = totalPopulation[Layer.herbUnderTree] + pot_established[Layer.herbUnderTree];
                if (totalPopulation[Layer.herbUnderTree] > max_population)
                    totalPopulation[Layer.herbUnderTree] = max_population;

                // And update bare ground proportion.
                bareCover = (1.0 - (facetCover[Facet.tree] + facetCover[Facet.shrub] + facetCover[Facet.herb]));

                // 999 format(A40, I5, F9.3, 6(I15))
                // 998 format(A40, I5, 2(F9.3), 6(F10.7))
                // 997 format(A40, I5, 6(I14))
            }
        }
    }
}
