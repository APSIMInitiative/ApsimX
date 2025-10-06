using Models.CLEM.Interfaces;
using System;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// Ruminant tracking item
    /// </summary>
    public class RuminantTrackingItem : IRuminantTrackingItem
    {
        /// <summary>
        /// Current amount
        /// </summary>
        public double Amount { get; private set; }

        /// <summary>
        /// Recent change
        /// </summary>
        public double Change { get; private set; }

        /// <summary>
        /// Previous amount
        /// </summary>
        public double Previous { get { return Amount - Change; } }

        /// <summary>
        /// Net amount showing any overall excess or shortfall
        /// </summary>
        public double Net { get; set; }

        /// <summary>
        /// Amount provided (used to track excess) energy or protein
        /// </summary>
        public double ProteinLimited { get; set; }

        /// <summary>
        /// Ruminant tracking item Constructor
        /// </summary>
        public RuminantTrackingItem(double initalAmount = 0)
        {
            Adjust(initalAmount);   
            //Amount = initalAmount; // need to include change for newborns to use the protein and fat change
        }

        /// <summary>
        /// Adjust this tracking item with change.
        /// </summary>
        /// <param name="ind">Individual to determine age and dry protein content</param>
        /// <param name="change">Amount to change by.</param>
        public void Adjust(double change, Ruminant ind = null)
        {
            Change = change;
            if (Amount + change < 0)
                Change = -Amount;
            Amount += Change;

            //if (Amount + change < 0)
            //{
            //    change = -Amount;
            //}
            
            //if (Amount > 0)
            //{
            //    Change = change;
            //}

            //Amount += change;
        }

        /// <summary>
        /// Define the last change and define previous based on current amount and change
        /// </summary>
        /// <param name="change">Amount of change</param>
        public void SetPreviousChange(double change)
        {
            if (change >= 0)
                Change = Math.Min(Amount, change);
        }

        /// <summary>
        /// Set this tracking item.
        /// </summary>
        /// <param name="amount">Amount to set.</param>
        public void Set(double amount)
        {
            Change = amount-Amount;
            Amount = amount;
        }

        /// <summary>
        /// Reset this tracking item to 0.
        /// </summary>
        public void Reset()
        {
            Change =  - Amount;
            Amount = 0;
            Net = 0;
        }
    }
}
