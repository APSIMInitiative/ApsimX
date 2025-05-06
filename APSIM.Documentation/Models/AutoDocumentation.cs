using System;
using System.Collections.Generic;
using APSIM.Shared.Documentation;
using Models;
using Models.Core;
using Models.Functions;
using APSIM.Documentation.Models.Types;
using Models.PMF;
using Models.PMF.Phen;
using Models.PMF.Organs;
using Models.PMF.Struct;
using Models.Functions.DemandFunctions;
using Models.Factorial;
using Models.Functions.SupplyFunctions;
using Models.Functions.RootShape;
using Models.PMF.OilPalm;
using M = Models;
using Models.AgPasture;
using Models.Soils.Nutrients;
using APSIM.Documentation.Bibliography;
using ModelsMap = Models.Map;

namespace APSIM.Documentation.Models
{
    /// <summary>
    /// A class of auto-documentation methods and HTML building widgets.
    /// </summary>
    public class AutoDocumentation
    {
        /// <summary>BibteX library loaded with APSIM.bib</summary>
        public static BibTeX Bibilography = new BibTeX();

        /// <summary>Returns a dictionary to match model classes to document class.</summary>
        private static Dictionary<Type, Type> DefineFunctions()
        {
            Dictionary<Type, Type> documentMap = new()
            {
                {typeof(Plant), typeof(DocPlant)},
                {typeof(PastureSpecies), typeof(DocPlant)},
                {typeof(Sugarcane), typeof(DocPlant)},
                {typeof(Clock), typeof(DocClock)},
                {typeof(Simulation), typeof(DocGenericWithChildren)},
                {typeof(CalculateCarbonFractionFromNConc), typeof(DocBiomassArbitrationFunction)},
                {typeof(DeficitDemandFunction), typeof(DocBiomassArbitrationFunction)},
                {typeof(MobilisationSupplyFunction), typeof(DocBiomassArbitrationFunction)},
                {typeof(PlantPartitionFractions), typeof(DocBiomassArbitrationFunction)},
                {typeof(NutrientDemandFunctions), typeof(DocGenericWithChildren)},
                {typeof(NutrientPoolFunctions), typeof(DocGenericWithChildren)},
                {typeof(NutrientProportionFunctions), typeof(DocGenericWithChildren)},
                {typeof(NutrientSupplyFunctions), typeof(DocGenericWithChildren)},
                {typeof(Phenology), typeof(DocPhenology)},
                {typeof(Root), typeof(DocRoot)},
                {typeof(Cultivar), typeof(DocCultivar)},
                {typeof(ModelsMap), typeof(DocMap)},
                {typeof(BoundFunction), typeof(DocBoundFunction)},
                {typeof(Memo), typeof(DocMemo)},
                {typeof(Structure), typeof(DocStructure)},
                {typeof(Folder),typeof(DocFolder)},
                {typeof(LinearInterpolationFunction), typeof(DocLinearInterpolationFunction)},
                {typeof(HeightFunction), typeof(DocFunction)},
                {typeof(BudNumberFunction), typeof(DocFunction)},
                {typeof(ZadokPMFWheat), typeof(DocZadokPMFWheat)},
                {typeof(XYPairs), typeof(DocXYPairs)},
                {typeof(VernalisationPhase), typeof(DocPhase)},
                {typeof(SubDailyInterpolation), typeof(DocSubDailyInterpolation)},
                {typeof(StorageNDemandFunction), typeof(DocStorageNDemandFunction)},
                {typeof(StartPhase), typeof(DocPhase)},
                {typeof(PhotoperiodPhase), typeof(DocPhase)},
                {typeof(NodeNumberPhase), typeof(DocPhase)},
                {typeof(LeafDeathPhase), typeof(DocPhase)},
                {typeof(LeafAppearancePhase), typeof(DocPhase)},
                {typeof(GrazeAndRewind), typeof(DocPhase)},
                {typeof(GotoPhase), typeof(DocPhase)},
                {typeof(GerminatingPhase), typeof(DocPhase)},
                {typeof(GenericPhase), typeof(DocPhase)},
                {typeof(EndPhase), typeof(DocPhase)},
                {typeof(EmergingPhase), typeof(DocPhase)},
                {typeof(SorghumLeaf), typeof(DocSorghumLeaf)},
                {typeof(ReproductiveOrgan), typeof(DocReproductiveOrgan) },
                {typeof(PerennialLeaf), typeof(DocPerennialLeaf)},
                {typeof(Nodule), typeof(DocNodule)},
                {typeof(Leaf), typeof(DocLeaf)},
                {typeof(Manager), typeof(DocManager)},
                {typeof(Experiment), typeof(DocExperiment)},
                {typeof(FrostSenescenceFunction), typeof(DocFrostSenescenceFunction)},
                {typeof(RUEModel), typeof(DocGenericWithChildren)},
                {typeof(LeafCohortParameters), typeof(DocLeafCohortParameters)},
                {typeof(RUECO2Function), typeof(DocGenericWithChildren)},
                {typeof(RootShapeSemiCircle), typeof(DocGenericWithChildren)},
                {typeof(RootShapeCylinder), typeof(DocGenericWithChildren)},
                {typeof(RootShapeSemiEllipse), typeof(DocGenericWithChildren)},
                {typeof(RootShapeSemiCircleSorghum), typeof(DocGenericWithChildren)},
                {typeof(HIReproductiveOrgan), typeof(DocGenericWithChildren)},
                {typeof(BasialBuds), typeof(DocGenericWithChildren)},
                {typeof(OilPalm), typeof(DocPlant)},
                {typeof(GenericOrgan), typeof(DocGenericOrgan)},
                {typeof(WaterSenescenceFunction), typeof(DocWaterSenescenceFunction)},
                {typeof(CanopyGrossPhotosynthesisHourly), typeof(DocGenericWithChildren)},
                {typeof(CanopyPhotosynthesis), typeof(DocGenericWithChildren)},
                {typeof(LeafLightUseEfficiency), typeof(DocGenericWithChildren)},
                {typeof(LeafMaxGrossPhotosynthesis), typeof(DocGenericWithChildren)},
                {typeof(LimitedTranspirationRate), typeof(DocGenericWithChildren)},
                {typeof(StomatalConductanceCO2Modifier), typeof(DocGenericWithChildren)},
                {typeof(BiomassDemand), typeof(DocGenericWithChildren)},
                {typeof(BiomassDemandAndPriority), typeof(DocGenericWithChildren)},
                {typeof(EnergyBalance), typeof(DocGenericWithChildren)},
                {typeof(Alias), typeof(DocAlias)},
                {typeof(Simulations), typeof(DocSimulations)},
                {typeof(M.Graph), typeof(DocGraph)},
                {typeof(Nutrient), typeof(DocNutrient)},
            };
            return documentMap;
        }

        /// <summary>Writes the description of a class to the tags.</summary>
        /// <param name="model">The model to get documentation for.</param>
        public static List<ITag> Document(IModel model)
        {
            List<ITag> newTags;
            newTags = AutoDocumentation.DocumentModel(model);
            newTags = DocumentationUtilities.CleanEmptySections(newTags);
            newTags = DocumentationUtilities.AddHeader(model.Name, newTags);
            return newTags;
        }

        /// <summary>Writes the description of a class to the tags.</summary>
        /// <param name="model">The model to get documentation for.</param>
        public static List<ITag> DocumentModel(IModel model)
        {
            List<ITag> newTags;

            DefineFunctions().TryGetValue(model.GetType(), out Type docType);

            if (docType != null) 
            {
                object documentClass = Activator.CreateInstance(docType, new object[]{model});
                newTags = (documentClass as DocGeneric).Document(0);
            }
            else if (docType == null && model as IFunction != null)
            {
                newTags = new DocFunction(model).Document(0);
            }
            else
            {
                newTags = new DocGeneric(model).Document(0);
            }
            return newTags;
        }
    }
    
}
