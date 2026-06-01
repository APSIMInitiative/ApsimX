using DocumentFormat.OpenXml.Drawing.Charts;
using Models.CLEM.Interfaces;
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
        private double amount = 0;

        /// <summary>
        /// Create the adapter
        /// </summary>
        /// <param name="forageProviderModel">
        /// An enumerable of forageProviders included in this intake pool
        /// </param>
        public GrazeAPSIMForagePool(GrazPlan.ForageProvider forageProviderModel)
        {
            forageProvider = forageProviderModel;
            amount = forageProvider.ForageObj.Material.Sum(a => a.Consumable.Wt);
        }

        /// <inheritdoc/>
        public double Amount { get => amount - AmountPending; set => throw new NotImplementedException(); }
        /// <inheritdoc/>
        public double DMD { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        /// <inheritdoc/>
        public FeedType TypeOfFeed { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        /// <inheritdoc/>
        public double GrossEnergyContent { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        /// <inheritdoc/>
        public double MetabolisableEnergyContent { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        /// <inheritdoc/>
        public double DryMatterDigestibility { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        /// <inheritdoc/>
        public double FatPercent { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        /// <inheritdoc/>
        public double NitrogenPercent { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        /// <inheritdoc/>
        public double CrudeProteinPercent { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        /// <inheritdoc/>
        public double RumenDegradableProteinPercent { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        /// <inheritdoc/>
        public double AcidDetergentInsolubleProtein { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        /// <inheritdoc/>
        public double GutFill { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        /// <inheritdoc/>
        public int Age { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        /// <inheritdoc/>
        public double Detached { get; set; }
        /// <inheritdoc/>
        public double Consumed { get; set; }
        /// <inheritdoc/>
        public double Growth => double.NaN;
        /// <inheritdoc/>
        public string Name { get => forageProvider.ForageObj.Name; set => throw new NotImplementedException(); }
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
