using System;
using System.Collections;  //enumerator
using System.Collections.Generic;
using Models.Core;
using Newtonsoft.Json;
using APSIM.Shared.Utilities;
using APSIM.Numerics;

namespace Models.Aqua
    {

    ///<summary>
    /// Aquaculture Food in the Pond.
    /// Stores the different feeds that are in the pond.
    ///</summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class FoodInPond : Model
        {


        #region Links


        ///// <summary>The clock</summary>
        //[Link]
        //private IClock Clock = null;


        ///// <summary>The summary</summary>
        //[Link]
        //private ISummary Summary = null;


        #endregion




        private Food food;



        /// <summary>
        ///  Data Structure that stores the different feeds that are in the pond.
        /// </summary>
        [JsonIgnore]
        public Food Food { get { return food; } }






        #region Clock Event Handlers


        [EventSubscribe("StartOfSimulation")]
        private void OnStartOfSimulation(object sender, EventArgs e)
            {
            this.food = new Food();
            }




        #endregion





        }






    #region Food in the Pond



    /// <summary>
    /// Data Structure that stores the different feeds that are in the pond.
    /// </summary>
    [Serializable]
    public class Food : IEnumerable
        {

        //TODO: Perhaps sort these feeds alphabetically on their name at some point.

        private List<Feed> feeds; //feeds already in the pond


        /// <summary>
        /// Constructor
        /// </summary>
        public Food()
            {
            feeds = new List<Feed>();
            }



        /// <summary>
        /// Default Iterator
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.
        /// </returns>
        public IEnumerator GetEnumerator()
            {
            for (int i = 0; i < feeds.Count; i++)
                {
                yield return feeds[i];
                }

            }





        #region Food Manipulation Methods


        /// <summary>
        /// Add some food to this food.
        /// If a feed (in the food to add) already exist then add it to the exsting feed.
        /// otherwise just add it as a new feed.
        /// </summary>
        /// <param name="FoodToAdd">Food to add to this food</param>
        public void AddToExisting(Food FoodToAdd)
            {
            foreach (Feed feed in FoodToAdd)
                {
                AddFeed(feed);
                }
            }


        /// <summary>
        /// Remove some food from this food.
        /// If a feed (in the food to remove) already exists then remove it from the existing feed.
        /// otherwise just ignore that feed.
        /// </summary>
        /// <param name="FoodToRemove">Food to remove from this food</param>
        public void RemoveFromExisting(Food FoodToRemove)
            {
            foreach (Feed feed in FoodToRemove)
                {
                if (IsThisFeedInFood(feed.FeedName))
                    {
                    Feed existing = GetFeed(feed.FeedName);
                    existing.RemoveFromExisting(feed);
                    }

                }
            }


        #endregion




        #region Feed Manipulation Methods



        private string CleanUpFeedName(string Name)
            {
            string name = Name;
            name = name.Trim();
            name = name.ToLower();
            return name;
            }


        private bool AreFeedNamesEqual(string Name1, string Name2)
            {
            string name1 = CleanUpFeedName(Name1);
            string name2 = CleanUpFeedName(Name2);

            if (name1 == name2)
                return true;
            else
                return false;

            }


        /// <summary>
        /// Check to see if this feed is already in the food
        /// </summary>
        /// <param name="Name"></param>
        /// <returns></returns>
        public bool IsThisFeedInFood(string Name)
            {
            foreach (Feed f in this)
                {
                if (AreFeedNamesEqual(f.FeedName, Name))
                    return true;
                }
            return false;
            }




        /// <summary>
        /// Get the Feed with the specified Name
        /// </summary>
        /// <param name="Name">Name of the Feed (case insensitive)</param>
        /// <returns>
        /// The Feed with the specified Name.
        /// If not found returns null.
        /// </returns>
        public Feed GetFeed(string Name)
            {
            foreach (Feed f in this)
                {
                if (AreFeedNamesEqual(f.FeedName, Name))
                    return f;
                }
            return null;
            }




        /// <summary>
        /// Add a Feed to the Food
        /// If it is not already present in this food then add it
        /// If it is already present then add it to the existing feed.
        /// </summary>
        /// <param name="NewFeed">The new feed to add to the food</param>
        public void AddFeed(Feed NewFeed)
            {
            NewFeed.FeedName = CleanUpFeedName(NewFeed.FeedName);
            bool alreadyInFood = IsThisFeedInFood(NewFeed.FeedName);

            if (alreadyInFood == false)
                {
                feeds.Add(NewFeed);
                }
            else
                {
                Feed existing = GetFeed(NewFeed.FeedName);
                existing.AddToExisting(NewFeed);
                }
            }



        /// <summary>
        /// Remove ALL the feeds from the Food.
        /// </summary>
        public void RemoveAllFeedFromFood()
            {
            feeds.Clear();
            }


        #endregion






        #region Outputs



        /// <summary>
        /// Total Dry Matter in the food (kg)
        /// </summary>
        /// <value>
        /// Summation of every type of feed in the food.
        /// </value>
        [JsonIgnore]
        [Units("kg")]
        public double TotalDM
            {
            get
                {
                int i = 0;
                double result = 0.0;
                foreach (Feed f in this)
                    {
                    result = result + f.DryMatter;
                    i++;
                    }
                return result;
                }
            }


        /// <summary>
        /// Total Nitrogen in the food(kg)
        /// </summary>
        /// <value>
        ///  Summation of every type of feed in the food.
        /// </value>
        [JsonIgnore]
        [Units("kg")]
        public double TotalN
            {
            get
                {
                int i = 0;
                double result = 0.0;
                foreach (Feed f in this)
                    {
                    result = result + f.Nitrogen;
                    i++;
                    }
                return result;
                }
            }




        /// <summary>
        /// Total Digestible Energy in the food (MJ)
        /// </summary>
        /// <value>
        ///  Summation of every type of feed in the food.
        /// </value>
        [JsonIgnore]
        [Units("MJ")]
        public double TotalDE
            {
            get
                {
                int i = 0;
                double result = 0.0;
                foreach (Feed f in this)
                    {
                    result = result + f.DigestibleEnergy;
                    i++;
                    }
                return result;
                }
            }




        #endregion






        #region Array Outputs



        /// <summary>
        /// Gets the Number of feeds in the pond
        /// </summary>
        [JsonIgnore]
        [Units("")]
        public int NumFeeds
        { get { return feeds.Count; } }




        /// <summary>
        /// Names for each feed type in the pond
        /// (This is for output only. You can not change these values)
        /// </summary>
        /// <value>
        /// returns an Array where each element is the name for a different feed in the pond.
        /// </value>
        [JsonIgnore]
        [Units("")]
        public string[] FeedNames
            {
            get
                {
                int i = 0;
                //string[] result = new string[NumFeeds];
                string[] result = new string[3];   //TODO: Fix the problem with ApsimX Report module not being able to report arrays which change size during a simulation
                foreach (Feed f in this)
                    {
                    result[i] = f.FeedName;
                    i++;
                    }
                return result;
                }
            }



        /// <summary>
        /// Dry Matter for each feed type in the pond (kg)
        /// (This is for output only. You can not change these values)
        /// </summary>
        /// <value>
        /// returns an Array where each element is a dry matter for a different feed in the pond.
        /// </value>
        [JsonIgnore]
        [Units("kg")]
        public double[] FeedDMs
            {
            get
                {
                int i = 0;
                //double[] result = new double[NumFeeds];
                double[] result = new double[3];   //TODO: Fix the problem with ApsimX Report module not being able to report arrays which change size during a simulation
                foreach (Feed f in this)
                    {
                    result[i] = f.DryMatter;
                    i++;
                    }
                return result;
                }
            }



        /// <summary>
        /// Nitrogen for each feed type in the pond (kg)
        /// (This is for output only. You can not change these values)
        /// </summary>
        /// <value>
        /// returns an Array where each element is the nitrogen content for a different feed in the pond.
        /// </value>
        [JsonIgnore]
        [Units("kg")]
        public double[] FeedNs
            {
            get
                {
                int i = 0;
                //double[] result = new double[NumFeeds];
                double[] result = new double[3];   //TODO: Fix the problem with ApsimX Report module not being able to report arrays which change size during a simulation
                foreach (Feed f in this)
                    {
                    result[i] = f.Nitrogen;
                    i++;
                    }
                return result;
                }
            }



        /// <summary>
        /// Digestible Energy for each feed type in the pond (MJ)
        /// (This is for output only. You can not change these values)
        /// </summary>
        /// <value>
        /// returns an Array where each element is the digestible energy content for a different feed in the pond.
        /// </value>
        [JsonIgnore]
        [Units("MJ")]
        public double[] FeedDEs
            {
            get
                {
                int i = 0;
                //double[] result = new double[NumFeeds];
                double[] result = new double[3];   //TODO: Fix the problem with ApsimX Report module not being able to report arrays which change size during a simulation
                foreach (Feed f in this)
                    {
                    result[i] = f.DigestibleEnergy;
                    i++;
                    }
                return result;
                }
            }




        #endregion



        }





    #endregion







    #region An Individual Feed Type


    /// <summary>
    /// This is an individual Feed Type that is currently in the Pond
    /// </summary>
    [Serializable]
    public class Feed
        {

        /// <summary>
        /// Name of this Feed
        /// </summary>
        [JsonIgnore]
        public string FeedName;


        /// <summary>
        /// Mass of this Feed (on a Dry Matter basis) in the pond
        /// (kg)
        /// </summary>
        [JsonIgnore]
        public double DryMatter;


        /// <summary>
        /// Nitrogen in all of this feeds Dry Matter.
        /// (kg)
        /// </summary>
        [JsonIgnore]
        public double Nitrogen;


        /// <summary>
        /// Digestible Energy in all of this feeds Dry Matter.
        /// (MJ)
        /// </summary>
        [JsonIgnore]
        public double DigestibleEnergy;


        /// <summary>
        /// Nitrogen Per Kilogram of Dry Matter.
        /// (kg)
        /// </summary>
        [JsonIgnore]
        public double NperKgOfDM { get { return MathUtilities.Divide(Nitrogen, DryMatter, 0.0); } }


        /// <summary>
        /// Digestible Energy Per Kilogram of Dry Matter.
        /// (MJ)
        /// </summary>
        [JsonIgnore]
        public double DEperKgOfDM { get { return MathUtilities.Divide(DigestibleEnergy, DryMatter, 0.0); } }



        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="FeedName">Name of this Feed</param>
        /// <param name="DryMatter">Mass of this Feed (on a Dry Matter basis) in the pond (kg)</param>
        /// <param name="Nitrogen">Nitrogen in all of this feeds Dry Matter (kg)</param>
        /// <param name="DigestibleEnergy">Digestible Energy in all of this feeds Dry Matter (MJ)</param>
        public Feed(string FeedName, double DryMatter, double Nitrogen, double DigestibleEnergy)
            {
            this.FeedName = FeedName;
            this.DryMatter = DryMatter;
            this.Nitrogen = Nitrogen;
            this.DigestibleEnergy = DigestibleEnergy;
            }




        /// <summary>
        /// Add some feed to the existing feed.
        /// </summary>
        /// <param name="AddThis"></param>
        public void AddToExisting(Feed AddThis)
            {
            this.DryMatter = this.DryMatter + AddThis.DryMatter;
            this.Nitrogen = this.Nitrogen + AddThis.Nitrogen;
            this.DigestibleEnergy = this.DigestibleEnergy + AddThis.DigestibleEnergy;
            }

        /// <summary>
        /// Remove some feed from the existing feed.
        /// If you are trying to remove more feed than exists
        /// then it will only remove whatever feed is there.
        /// </summary>
        /// <param name="RemoveThis">Amounts in this feed (to remove) should be positive values</param>
        public void RemoveFromExisting(Feed RemoveThis)
            {
            double result;

            //Dry Matter
            result = this.DryMatter - RemoveThis.DryMatter;
            this.DryMatter = Math.Max(result, 0.0);

            //Nitrogen
            result = this.Nitrogen - RemoveThis.Nitrogen;
            this.Nitrogen = Math.Max(result, 0.0);

            //Digestible Energy
            result = this.DigestibleEnergy - RemoveThis.DigestibleEnergy;
            this.DigestibleEnergy = Math.Max(result, 0.0);

            }

        }

    #endregion





    }
