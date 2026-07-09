// -----------------------------------------------------------------------
// GrazPlan Supplement model
// -----------------------------------------------------------------------
using System;

namespace Models.GrazPlan
{

public partial class Supplement
    {
        /// <summary>
        /// This class encapsulates an amount of feed of a particular type that will
        /// be fed each day.
        /// </summary>
        [Serializable]
        private class SupplementFeeding
        {
            private string supplement;
            private double amount;
            private string paddock;
            private bool feedSuppFirst;

            /// <summary>Constructor.</summary>
            /// <param name="nam">Name of feed schedule.</param>
            /// <param name="sup">The supplement.</param>
            /// <param name="amt">The amount.</param>
            /// <param name="pad">The paddock.</param>
            /// <param name="feedSupFirst">Feed supplement before pasture. Bail feeding.</param>
            public SupplementFeeding(string nam, string sup, double amt, string pad, bool feedSupFirst)
            {
                Name = nam;
                supplement = sup;
                amount = amt;
                paddock = pad;
                feedSuppFirst = feedSupFirst;
            }

            /// <summary>Name of feeding.</summary>
            public string Name { get; }

            /// <summary>
            /// Tell supplement to do a feed.
            /// </summary>
            public void Feed(Supplement supp)
            {
                supp.Feed(supplement, amount, paddock, feedSuppFirst);
            }

        }
    }
}
