using APSIM.Numerics;
using DocumentFormat.OpenXml.Drawing.Charts;
using Models.CLEM.Interfaces;
using Models.ForageDigestibility;
using Models.GrazPlan;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// An adapter to convert an APSIM forage object to a CLEM grazing intake pool
    ///
    /// </summary>
    public class GrazeAPSIMForagePool : IGrazeIntakePool
    {
        private ModelWithDigestibleBiomass biomassModel;
        private IFeed feedDetails;
        private double amount = 0;
        private double nitrogen = 0;
        private const double gm2Tokgha = 10.0;

        /// <summary>
        /// Provides the model with digestibile biomass associated with this pool
        /// </summary>
        public ModelWithDigestibleBiomass BiomassModel => biomassModel;

        /// <summary>
        /// Create the adapter
        /// </summary>
        /// <param name="details">The IFeed quality details of the pool</param>
        /// <param name="biomassModel">
        /// The model with digestible biomass associated with the pool
        /// </param>
        /// <param name="forages"></param>
        /// <param name="area"></param>
        public GrazeAPSIMForagePool(GrazeFoodStoreAPSIMLink details, ModelWithDigestibleBiomass biomassModel, Forages forages, double area)
        {
            Name = biomassModel.Material.FirstOrDefault().Name;
            this.biomassModel = biomassModel;
            feedDetails = new FoodResourcePacket(details);
            amount = biomassModel.Material.Sum(a => a.Consumable.Wt * gm2Tokgha) * area;
            nitrogen = biomassModel.Material.Sum(a => a.Consumable.N * gm2Tokgha) * area;

            double totalDMD = 0;
            double totalWt = 0;

            foreach (var material in biomassModel.Material)
            {
                totalDMD += (forages.GetDigestibility(material) * material.Consumable.StructuralWt) + (1 * material.Consumable.StorageWt) + (1 * material.Consumable.MetabolicWt);    // storage and metab are 100% dmd
                totalWt += material.Consumable.Wt;
            }

            // Note: Previous code from Stock uses dmd of structural, assume storage and metabolic are 100% digestible
            // ToDo: still need to find the FromModel DMD provided by AgPasture models
            // the previous approach was taken from Stock, ensured there was a dead material available for every live material but SimpleGrazing in AgPasture example throws a "Cannot find dead material error" so simplified to just work with any material present

            feedDetails.DryMatterDigestibility = totalDMD / totalWt * 100;
            feedDetails.MetabolisableEnergyContent = 0;  //16.0 * (feedDetails.DryMatterDigestibility / 100.0); from APSIM or use calcs from CLEM foodReesourcePacket
            feedDetails.GutFill = details.CalculateGutFill(feedDetails.DryMatterDigestibility);
        }

        /// <inheritdoc/>
        public double Amount { get => amount; }
        /// <inheritdoc/>
        public FeedType TypeOfFeed { get => feedDetails.TypeOfFeed; set => throw new NotImplementedException(); }
        /// <inheritdoc/>
        public double GrossEnergyContent { get => feedDetails.GrossEnergyContent; set => throw new NotImplementedException(); }
        /// <inheritdoc/>
        public double MetabolisableEnergyContent { get => feedDetails.MetabolisableEnergyContent; set => throw new NotImplementedException(); }
        /// <inheritdoc/>
        public double DryMatterDigestibility { get => feedDetails.DryMatterDigestibility; set => throw new NotImplementedException(); }
        /// <inheritdoc/>
        public double FatPercent { get => feedDetails.FatPercent; set => throw new NotImplementedException(); }
        /// <inheritdoc/>
        public double NitrogenPercent { get => nitrogen / amount * 100.0; set => throw new NotImplementedException(); }
        /// <inheritdoc/>
        public double CrudeProteinPercent { get => NitrogenPercent * 6.25; set => throw new NotImplementedException(); }
        /// <inheritdoc/>
        public double RumenDegradableProteinPercent { get => feedDetails.RumenDegradableProteinPercent; set => throw new NotImplementedException(); }
        /// <inheritdoc/>
        public double AcidDetergentInsolubleProtein { get => feedDetails.AcidDetergentInsolubleProtein; set => throw new NotImplementedException(); }
        /// <inheritdoc/>
        public double GutFill { get => feedDetails.GutFill; set => throw new NotImplementedException(); }
        /// <inheritdoc/>
        public int Age { get => 10; set => throw new NotImplementedException(); }
        /// <inheritdoc/>
        public double Detached { get; set; }
        /// <inheritdoc/>
        public double Consumed { get; set; }
        /// <inheritdoc/>
        public double Growth => double.NaN;
        /// <inheritdoc/>
        public string Name { get; set; }
        /// <inheritdoc/>
        public double AmountAvailable => MathUtilities.RoundToZero(amount - AmountPending, 1e-7);
        /// <inheritdoc/>
        public double AmountPending { get; private set; }
        /// <inheritdoc/>
        public double AmountInitialPending { get; private set; }
        /// <inheritdoc/>
        public void Add(GrazeFoodStorePool pool)
        {
            throw new NotImplementedException();
        }
        /// <inheritdoc/>
        public void Consume(double amount, bool reducePending = true)
        {
            throw new NotImplementedException();
        }
        /// <inheritdoc/>
        public void ConsumePending()
        {
            Consumed += AmountPending;
            this.amount -= AmountPending;
            AmountPending = 0;
            AmountInitialPending = 0;
        }
        /// <inheritdoc/>
        public double Detach(double proportion)
        {
            throw new NotImplementedException();
        }
        /// <inheritdoc/>
        public void ReducePending(double amountReturned)
        {
            AmountPending -= Math.Min(AmountPending, amountReturned);
        }
        /// <inheritdoc/>
        public void Remove(double removeAmount)
        {
            throw new NotImplementedException();
        }
        /// <inheritdoc/>
        public void Reset()
        {
            throw new NotImplementedException();
        }
        /// <inheritdoc/>
        public void SetPending(double amountPending)
        {
            AmountPending = amountPending;
            AmountInitialPending = amountPending;
        }
    }
}
