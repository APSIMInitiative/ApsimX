

namespace Models.PMF.Library
{
    using Models.Core;
    using Models.Interfaces;
    using Interfaces;
    using Soils;
    using System;
    using System.Collections.Generic;
    using System.Data;

    /// <summary>
    /// # [Name]
    /// This class impliments biomass removal from live + dead pools.
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(IOrgan))]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.BiomassRemovalPresenter")]
    public class BiomassRemoval : Model, ICustomDocumentation
    {
        [Link]
        Plant plant = null;

        [Link]
        ISurfaceOrganicMatter surfaceOrganicMatter = null;

        [Link]
        Summary summary = null;

        /// <summary>Biomass removal defaults for different event types e.g. prune, cut etc.</summary>
        [ChildLink]
        public List<OrganBiomassRemovalType> defaults = null;

        /// <summary>Invoked when fresh organic matter needs to be incorporated into soil</summary>
        public event FOMLayerDelegate IncorpFOM;

        /// <summary>Removes biomass from live and dead biomass pools, may send to surface organic matter</summary>
        /// <param name="biomassRemoveType">Name of event that triggered this biomass removal call.</param>
        /// <param name="amount">The fractions of biomass to remove</param>
        /// <param name="Live">Live biomass pool</param>
        /// <param name="Dead">Dead biomass pool</param>
        /// <param name="Removed">The removed pool to add to.</param>
        /// <param name="Detached">The detached pool to add to.</param>
        /// <param name="writeToSummary">Write the biomass removal to summary file?</param>
        /// <returns>The remaining live fraction.</returns>
        public double RemoveBiomass(string biomassRemoveType, OrganBiomassRemovalType amount, 
                                    Biomass Live, Biomass Dead, 
                                    Biomass Removed, Biomass Detached,
                                    bool writeToSummary = true)
        {
            if (amount == null)
                amount = FindDefault(biomassRemoveType);
            else
                CheckRemoveFractions(biomassRemoveType, amount);

            double liveFractionToRemove = amount.FractionLiveToRemove + amount.FractionLiveToResidue;
            double deadFractionToRemove = amount.FractionDeadToRemove + amount.FractionDeadToResidue;

            if (liveFractionToRemove+ deadFractionToRemove > 0.0)
            {
                Biomass removing;
                Biomass detaching;
                double totalBiomass = Live.Wt + Dead.Wt;
                if (totalBiomass > 0)
                {
                    double remainingLiveFraction = RemoveBiomassFromLiveAndDead(amount, Live, Dead, out removing, out detaching);

                    // Add the detaching biomass to total removed and detached
                    Removed.Add(removing);
                    Detached.Add(detaching);

                    // Pass the detaching biomass to surface organic matter model.
                    //TODO: in reality, the dead material is different from the live, so it would be better to add them as separate pools to SurfaceOM
                    surfaceOrganicMatter.Add(detaching.Wt * 10.0, detaching.N * 10.0, 0.0, plant.CropType, Name);

                    if (writeToSummary)
                    {
                        double totalFractionToRemove = (Removed.Wt + detaching.Wt) * 100.0 / totalBiomass;
                        double toResidue = detaching.Wt * 100.0 / (Removed.Wt + detaching.Wt);
                        double removedOff = Removed.Wt * 100.0 / (Removed.Wt + detaching.Wt);
                        summary.WriteMessage(Parent, "Removing " + totalFractionToRemove.ToString("0.0")
                                                 + "% of " + Parent.Name.ToLower() + " biomass from " + plant.Name
                                                 + ". Of this " + removedOff.ToString("0.0") + "% is removed from the system and "
                                                 + toResidue.ToString("0.0") + "% is returned to the surface organic matter.");
                        summary.WriteMessage(Parent, "Removed " + Removed.Wt.ToString("0.0") + " g/m2 of dry matter weight and "
                                                 + Removed.N.ToString("0.0") + " g/m2 of N.");
                    }
                    return remainingLiveFraction;
                }
            }

            return 1.0;
        }


        /// <summary>Removes biomass from live and dead biomass pools and send to soil</summary>
        /// <param name="biomassRemoveType">Name of event that triggered this biomass remove call.</param>
        /// <param name="removal">The fractions of biomass to remove</param>
        /// <param name="Live">Live biomass pool</param>
        /// <param name="Dead">Dead biomass pool</param>
        /// <param name="Removed">The removed pool to add to.</param>
        /// <param name="Detached">The detached pool to add to.</param>
        public void RemoveBiomassToSoil(string biomassRemoveType, OrganBiomassRemovalType removal,
                                        Biomass[] Live, Biomass[] Dead,
                                        Biomass Removed, Biomass Detached)
        {
            if (removal == null)
                removal = FindDefault(biomassRemoveType);

            //NOTE: roots don't have dead biomass
            double totalFractionToRemove = removal.FractionLiveToRemove + removal.FractionLiveToResidue;

            if (totalFractionToRemove > 0)
            {
                //NOTE: at the moment Root has no Dead pool
                FOMLayerLayerType[] FOMLayers = new FOMLayerLayerType[Live.Length];
                double remainingFraction = 1.0 - (removal.FractionLiveToResidue + removal.FractionLiveToRemove);
                for (int layer = 0; layer < Live.Length; layer++)
                {
                    Biomass removing;
                    Biomass detaching;
                    double remainingLiveFraction = RemoveBiomassFromLiveAndDead(removal, Live[layer], Dead[layer], out removing, out detaching);

                    // Add the detaching biomass to total removed and detached
                    Removed.Add(removing);
                    Detached.Add(detaching);

                    // Pass the detaching biomass to surface organic matter model.
                    FOMType fom = new FOMType();
                    fom.amount = (float)(detaching.Wt * 10);
                    fom.N = (float)(detaching.N * 10);
                    fom.C = (float)(0.40 * detaching.Wt * 10);
                    fom.P = 0.0;
                    fom.AshAlk = 0.0;

                    FOMLayerLayerType Layer = new FOMLayerLayerType();
                    Layer.FOM = fom;
                    Layer.CNR = 0.0;
                    Layer.LabileP = 0.0;
                    FOMLayers[layer] = Layer;
                }
                FOMLayerType FomLayer = new FOMLayerType();
                FomLayer.Type = plant.CropType;
                FomLayer.Layer = FOMLayers;
                IncorpFOM.Invoke(FomLayer);
            }
        }


        /// <summary>Finds a specific biomass removal default for the specified name</summary>
        /// <param name="name">Name of the removal type e.g. cut, prune etc.</param>
        /// <returns>Returns the default or null if not found.</returns>
        public OrganBiomassRemovalType FindDefault(string name)
        {
            OrganBiomassRemovalType amount = defaults.Find(d => d.Name == name);

            CheckRemoveFractions(name, amount);

            return amount;
        }

        /// <summary>Checks whether specified biomass removal fractions are within limits</summary>
        /// <param name="name">Name of the removal type e.g. cut, prune etc.</param>
        /// <param name="amount">The removal amount fractions</param>
        /// <returns>Returns true if fractions are ok, false otherwise.</returns>
        public bool CheckRemoveFractions(string name, OrganBiomassRemovalType amount)
        {
            bool testFractions = true;
            if (amount == null)
            {
                testFractions = false;
                throw new Exception("Cannot test null biomass removal - " + Parent.Name + ".BiomassRemovalFractions." + name);
            }

            if (amount.FractionLiveToRemove + amount.FractionLiveToResidue > 1.0)
            {
                testFractions = false;

                throw new Exception("The sum of FractionToResidue and FractionToRemove for " + Parent.Name
                                    + " is greater than one for live biomass.  Had this exception not been triggered, the biomass for "
                                    + Name + " would go negative");
            }

            if (amount.FractionDeadToRemove + amount.FractionDeadToResidue > 1.0)
            {
                testFractions = false;
                throw new Exception("The sum of FractionToResidue and FractionToRemove for " + Parent.Name
                                    + " is greater than one for dead biomass.  Had this exception not been triggered, the biomass for "
                                    + Name + " would go negative");
            }

            return testFractions;
        }

        /// <summary>Removes biomass from live and dead biomass pools</summary>
        /// <param name="amount">The fractions of biomass to remove</param>
        /// <param name="Live">Live biomass pool</param>
        /// <param name="Dead">Dead biomass pool</param>
        /// <param name="removing">The removed pool to add to.</param>
        /// <param name="detaching">The amount of detaching material</param>
        private static double RemoveBiomassFromLiveAndDead(OrganBiomassRemovalType amount, Biomass Live, Biomass Dead, out Biomass removing, out Biomass detaching)
        {
            double remainingLiveFraction = 1.0 - (amount.FractionLiveToResidue + amount.FractionLiveToRemove);
            double remainingDeadFraction = 1.0 - (amount.FractionDeadToResidue + amount.FractionDeadToRemove);

            detaching = Live * amount.FractionLiveToResidue + Dead * amount.FractionDeadToResidue;
            removing = Live * amount.FractionLiveToRemove + Dead * amount.FractionDeadToRemove;

            Live.Multiply(remainingLiveFraction);
            Dead.Multiply(remainingDeadFraction);
            return remainingLiveFraction;
        }

        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        /// <param name="tags">The list of tags to add to.</param>
        /// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
        /// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
        public void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
        {
            if (IncludeInDocumentation)
            {
                tags.Add(new AutoDocumentation.Heading(Name, headingLevel));

                tags.Add(new AutoDocumentation.Paragraph("This organ will respond to certain management actions by either removing some of its biomass from the system or transferring some of its biomass to the soil surface residues.  The following table describes the default proportions of live and dead biomass that are transferred out of the simulation using \"Removed\" or to soil surface residue using \"To Residue\" for a range of management actions. The total percentage removed for live or dead must not exceed 100%. The difference between the total and 100% gives the biomass remaining on the plant. These can be changed during a simulation using a manager script.", indent));

                DataTable data = new DataTable();
                data.Columns.Add("Method", typeof(string));
                data.Columns.Add("% Live Removed", typeof(int));
                data.Columns.Add("% Dead Removed", typeof(int));
                data.Columns.Add("% Live To Residue", typeof(int));
                data.Columns.Add("% Dead To Residue", typeof(int));

                foreach (OrganBiomassRemovalType removal in Apsim.Children(this, typeof(OrganBiomassRemovalType)))
                {
                    DataRow row = data.NewRow();
                    data.Rows.Add(row);
                    row["Method"] = removal.Name;
                    row["% Live Removed"] = removal.FractionLiveToRemove * 100;
                    row["% Dead Removed"] = removal.FractionDeadToRemove * 100;
                    row["% Live To Residue"] = removal.FractionLiveToResidue * 100;
                    row["% Dead To Residue"] = removal.FractionDeadToResidue * 100;
                }

                foreach (Memo childMemo in Apsim.Children(this, typeof(Memo)))
                    AutoDocumentation.DocumentModel(childMemo, tags, headingLevel + 1, indent);

                tags.Add(new AutoDocumentation.Table(data, indent));
            }
        }
    }
}