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
    public class GrazeAPSIMForagePool: IGrazeIntakePool
    {
        private GrazPlan.ForageProvider forageProvider;
        private IFeed feedDetails;
        private double amount = 0;
        private double nitrogen = 0;
        private double dmd = 0;

        /// <summary>
        /// Create the adapter
        /// </summary>
        /// <param name="details"></param>
        /// <param name="forageProviderModel">
        /// An enumerable of forageProviders included in this intake pool
        /// </param>
        /// <param name="paddock"></param>
        public GrazeAPSIMForagePool(GrazeFoodStoreAPSIMLink details, GrazPlan.ForageProvider forageProviderModel, PaddockInfo paddock)
        {
            forageProvider = forageProviderModel;
            feedDetails = details as IFeed;
            amount = forageProvider.ForageObj.Material.Sum(a => a.Consumable.Wt) * paddock.Area;
            nitrogen = forageProvider.ForageObj.Material.Sum(a => a.Consumable.N) * paddock.Area;

            double totalDMD = 0;
            double totalWt = 0;
            foreach (var live in forageProvider.ForageObj.Material.Where(m => m.IsLive))
            {
                Name = live.Name;

                // Find corresponding dead material
                var dead = forageProvider.ForageObj.Material.FirstOrDefault(m => !m.IsLive && m.Name == live.Name);
                if (dead == null)
                    throw new Exception($"Cannot find dead material for {live.Name}.");

                if (live.Consumable.Wt > 0 || dead.Consumable.Wt > 0)
                {
                    // we can find the dmd of structural, assume storage and metabolic are 100% digestible
                    dmd = (paddock.ForagesModel.GetDigestibility(live) * live.Consumable.StructuralWt) + (1 * live.Consumable.StorageWt) + (1 * live.Consumable.MetabolicWt);    // storage and metab are 100% dmd
                    dmd += (paddock.ForagesModel.GetDigestibility(dead) * dead.Consumable.StructuralWt) + (1 * dead.Consumable.StorageWt) + (1 * dead.Consumable.MetabolicWt);
                    totalDMD += dmd;
                    totalWt += live.Total.Wt + dead.Total.Wt;
                }
            }
            dmd = totalDMD / totalWt * 100;
            GutFill = details.CalculateGutFill(totalDMD);
        }

        /// <inheritdoc/>
        public double Amount { get => amount - AmountPending; set => throw new NotImplementedException(); }
        /// <inheritdoc/>
        public FeedType TypeOfFeed { get => feedDetails.TypeOfFeed; set => throw new NotImplementedException(); }
        /// <inheritdoc/>
        public double GrossEnergyContent { get => feedDetails.GrossEnergyContent; set => throw new NotImplementedException(); }
        /// <inheritdoc/>
        public double MetabolisableEnergyContent { get => feedDetails.MetabolisableEnergyContent; set => throw new NotImplementedException(); }
        /// <inheritdoc/>
        public double DryMatterDigestibility { get => dmd; set => throw new NotImplementedException(); }
        /// <inheritdoc/>
        public double FatPercent { get => feedDetails.FatPercent; set => throw new NotImplementedException(); }
        /// <inheritdoc/>
        public double NitrogenPercent { get => nitrogen / amount; set => throw new NotImplementedException(); }
        /// <inheritdoc/>
        public double CrudeProteinPercent { get => nitrogen * 6.25 / amount; set => throw new NotImplementedException(); }
        /// <inheritdoc/>
        public double RumenDegradableProteinPercent { get => feedDetails.RumenDegradableProteinPercent; set => throw new NotImplementedException(); }
        /// <inheritdoc/>
        public double AcidDetergentInsolubleProtein { get => feedDetails.AcidDetergentInsolubleProtein; set => throw new NotImplementedException(); }
        /// <inheritdoc/>
        public double GutFill { get; set; }
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
        public double AmountAvailable => amount - AmountPending;
        /// <inheritdoc/>
        public double AmountPending { get; set; }
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
            throw new NotImplementedException();
        }
        /// <inheritdoc/>
        public double Detach(double proportion)
        {
            throw new NotImplementedException();
        }
        /// <inheritdoc/>
        public void ReducePending(double amountReturned)
        {
            throw new NotImplementedException();
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
    }
}
