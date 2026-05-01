using System;
using System.Collections.Generic;
using System.Linq;
using Models.Core;
using Newtonsoft.Json;

namespace Models.GrazPlan
{

    /// <summary>
    /// SupplementRation encapsulates zero or more supplements mixed together.
    /// In essence, it is a list of SupplementItem.
    /// This is the class used for specifying the supplement fed to a group of
    /// animals in AnimGrp.pas
    /// Apart from the usual read/write properties and list-handling methods, the
    /// class has the following special methods:
    /// * AverageSuppt      computes the composition of a supplement mixture in
    /// proportions given by the fAmount values.
    /// * AverageCost       computes the cost of a supplement mixture in
    /// proportions given by the fAmount values.
    /// </summary>
    [Serializable]
    public class SupplementRation
    {
        /// <summary>
        /// The supplements array
        /// </summary>
        internal SupplementItem[] SuppArray = new SupplementItem[0];

        /// <summary>
        /// The ration choice
        /// </summary>
        private RationChoice rationChoice = RationChoice.rcStandard;

        /// <summary>
        /// The ration choice type
        /// </summary>
        [Serializable]
        public enum RationChoice
        {
            /// <summary>
            /// The rc standard mix as specified
            /// </summary>
            rcStandard,

            /// <summary>
            /// The rc only stored
            /// use only stored fodder while it lasts
            /// </summary>
            rcOnlyStored,

            /// <summary>
            /// The rc inc stored
            /// use stored fodder as first ingredient
            /// </summary>
            rcIncStored
        }

        /// <summary>
        /// Gets the count.
        /// </summary>
        /// <value>The count</value>
        [Units("-")]
        public int Count
        {
            get
            {
                return SuppArray.Length;
            }
        }

        /// <summary>
        /// Gets or sets the total amount.
        /// </summary>
        /// <value>
        /// The total amount.
        /// </value>
        [Units("kg")]
        public double TotalAmount
        {
            get
            {
                double totAmt = 0.0;
                for (int i = 0; i < SuppArray.Length; i++)
                    totAmt += SuppArray[i].Amount;
                return totAmt;
            }

            set
            {
                double scale = 0.0;
                double totAmt = TotalAmount;
                if (totAmt > 0.0)
                    scale = value / totAmt;
                else if (SuppArray.Length > 0)
                    scale = value / SuppArray.Length;

                for (int i = 0; i < SuppArray.Length; i++)
                    SuppArray[i].Amount *= scale;
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="SupplementItem"/> with the specified index.
        /// </summary>
        /// <value>
        /// The <see cref="SupplementItem"/>.
        /// </value>
        /// <param name="idx">The index.</param>
        /// <returns>The supplement object</returns>
        [JsonIgnore]
        public SupplementItem this[int idx]
        {
            get
            {
                return SuppArray[idx];
            }

            set
            {
                // Note: Assigning to the Suppt property copies the attributes of the source
                // FoodSupplement, not the FoodSupplement instance itself.
                if (idx >= SuppArray.Length)
                {
                    Array.Resize(ref SuppArray, (int)idx + 1);
                    SuppArray[idx] = new SupplementItem(value);
                }
            }
        }

        /// <summary>
        /// Assigns the specified source ration.
        /// </summary>
        /// <param name="srcRation">The source ration.</param>
        public void Assign(SupplementRation srcRation)
        {
            Array.Resize(ref SuppArray, srcRation.Count);
            for (int idx = 0; idx < srcRation.Count; idx++)
            {
                if (SuppArray[idx] == null)
                    SuppArray[idx] = new SupplementItem();
                SuppArray[idx].Assign(srcRation[idx]);
            }
            rationChoice = srcRation.rationChoice;
        }

        /// <summary>
        /// Gets the fresh weight fraction.
        /// </summary>
        /// <param name="idx">The index of the supplement.</param>
        /// <returns>The fresh weight fraction for the supplement at idx</returns>
        public double GetFWFract(int idx)
        {
            return SuppArray[idx].Amount >= 1e-7 ? SuppArray[idx].Amount / TotalAmount : 0.0;
        }

        /// <summary>
        /// Adds the specified supp.
        /// </summary>
        /// <param name="supp">The supp.</param>
        /// <param name="amt">The amt.</param>
        /// <param name="cost">The cost.</param>
        /// <returns>The array index of the new supplement</returns>
        public int Add(FoodSupplement supp, double amt = 0.0, double cost = 0.0)
        {
            int idx = SuppArray.Length;
            Insert(idx, supp, amt, cost);
            return idx;
        }

        /// <summary>
        /// Adds the specified supp item.
        /// </summary>
        /// <param name="suppItem">The supp item.</param>
        /// <returns>The array index of the new supplement</returns>
        public int Add(SupplementItem suppItem)
        {
            return Add(suppItem, suppItem.Amount, suppItem.Cost);
        }

        /// <summary>
        /// Inserts the specified index.
        /// </summary>
        /// <param name="idx">The index.</param>
        /// <param name="supp">The supp.</param>
        /// <param name="amt">The amt.</param>
        /// <param name="cost">The cost.</param>
        public void Insert(int idx, FoodSupplement supp, double amt = 0.0, double cost = 0.0)
        {
            Array.Resize(ref SuppArray, SuppArray.Length + 1);
            for (int jdx = SuppArray.Length - 1; jdx > idx; jdx--)
                SuppArray[jdx] = SuppArray[jdx - 1];
            SuppArray[idx] = new SupplementItem(supp, amt, cost);
        }

        /// <summary>
        /// Deletes the specified index.
        /// </summary>
        /// <param name="idx">The index.</param>
        public void Delete(int idx)
        {
            for (int jdx = idx + 1; jdx < SuppArray.Length; jdx++)
                SuppArray[jdx - 1] = SuppArray[jdx];
            Array.Resize(ref SuppArray, SuppArray.Length - 1);
        }

        /// <summary>
        /// Clears this instance.
        /// </summary>
        public void Clear()
        {
            Array.Resize(ref SuppArray, 0);
        }

        /// <summary>
        /// Get the index of the supplement in the supplements array
        /// </summary>
        /// <param name="name">Name of the supplement.</param>
        /// <param name="checkTrans">if set to <c>true</c> [check trans].</param>
        /// <returns>The array index of the supplement or -1 if not found</returns>
        public int IndexOf(string name, bool checkTrans = false)
        {
            int result = -1;
            for (int idx = 0; idx < SuppArray.Length; idx++)
            {
                if (SuppArray[idx].Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    result = idx;
                    break;
                }
            }

            if (result < 0 && checkTrans)
            {
                for (int idx = 0; idx < SuppArray.Length; idx++)
                    for (int itr = 0; itr < SuppArray[idx].Translations.Length; itr++)
                        if (SuppArray[idx].Translations[itr].Text.Equals(name, StringComparison.OrdinalIgnoreCase))
                        {
                            result = idx;
                            break;
                        }
            }
            return result;
        }

        /// <summary>
        /// Computes a weighted average supplement composition
        /// </summary>
        public FoodSupplement AverageSuppt()
        {
            var aveSupp = new FoodSupplement();
            if (TotalAmount > 0.0)
                aveSupp.MixMany(SuppArray);
            return aveSupp;
        }

        /// <summary>
        /// Weighted average cost of a supplement
        /// </summary>
        /// <returns>
        /// The weighted average cost in the same units as SupplementItem.cost
        /// </returns>
        public double AverageCost()
        {
            if (this.TotalAmount < 1e-7)
                return 0.0;
            double totCost = 0.0;
            for (int idx = 0; idx < this.SuppArray.Length; idx++)
                totCost += this.SuppArray[idx].Amount * this.SuppArray[idx].Cost;
            return totCost / this.TotalAmount;
        }

        /// <summary>
        /// The property n_ attrs
        /// </summary>
        private static FoodSupplement.SuppAttribute[] PROPNATTRS =
                                                            {
                                                            FoodSupplement.SuppAttribute.spaDMP,
                                                            FoodSupplement.SuppAttribute.spaDMD,
                                                            FoodSupplement.SuppAttribute.spaEE,
                                                            FoodSupplement.SuppAttribute.spaCP,
                                                            FoodSupplement.SuppAttribute.spaDG,
                                                            FoodSupplement.SuppAttribute.spaADIP,
                                                            FoodSupplement.SuppAttribute.spaPH,
                                                            FoodSupplement.SuppAttribute.spaSU,
                                                            FoodSupplement.SuppAttribute.spaMaxP
                                                            };

        /// <summary>
        /// Scales the attributes of the members of the supplement so that the weighted
        /// average attributes match those of aveSupp. Ensures that fractional values
        /// remain within the range 0-1
        /// * Assumes that all values are non-negative
        /// </summary>
        /// <param name="scaleToSupp">The scale to supp.</param>
        /// <param name="attrs">The attrs.</param>
        public void RescaleRation(FoodSupplement scaleToSupp, IList<FoodSupplement.SuppAttribute> attrs)
        {
            Array attribs = Enum.GetValues(typeof(FoodSupplement.SuppAttribute));

            // NB this only works because of the way the supplement attributes are ordered
            // i.e. DM proportion first and CP before dg and ADIP:CP
            foreach (FoodSupplement.SuppAttribute attr in attribs)
            {
                if (attrs.Contains(attr))
                {
                    double newWtMean = scaleToSupp[attr];

                    if (this.SuppArray.Length == 1)
                        this.SuppArray[0][attr] = newWtMean;
                    else
                    {
                        double oldWtMean = 0.0;
                        double totalWeight = 0.0;
                        double weight = 0.0;
                        for (int idx = 0; idx < SuppArray.Length; idx++)
                        {
                            switch (attr)
                            {
                                case FoodSupplement.SuppAttribute.spaDMP:
                                    weight = GetFWFract(idx);
                                    break;
                                case FoodSupplement.SuppAttribute.spaDG:
                                case FoodSupplement.SuppAttribute.spaADIP:
                                    weight = GetFWFract(idx) * SuppArray[idx].DMPropn * SuppArray[idx].CrudeProt;
                                    break;
                                default:
                                    weight = GetFWFract(idx) * SuppArray[idx].DMPropn;
                                    break;
                            }
                            oldWtMean += weight * this.SuppArray[idx][attr];
                            totalWeight += weight;
                        }
                        if (totalWeight > 0.0)
                            oldWtMean /= totalWeight;

                        for (int idx = 0; idx < this.SuppArray.Length; idx++)
                        {
                            if (totalWeight == 0.0)
                                this.SuppArray[idx][attr] = newWtMean;
                            else if ((newWtMean < oldWtMean) || (!PROPNATTRS.Contains(attr)))
                                this.SuppArray[idx][attr] *= newWtMean / oldWtMean;
                            else
                                this.SuppArray[idx][attr] += (1.0 - this.SuppArray[idx][attr]) * (newWtMean - oldWtMean)
                                                        / (1.0 - oldWtMean);
                        }
                    }
                }
            }
        }
    }
}
