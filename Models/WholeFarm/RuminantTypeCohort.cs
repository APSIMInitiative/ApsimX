using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Xml.Serialization;
using Models.Core;

namespace Models.WholeFarm
{

    /// <summary>
    /// This stores the initialisation parameters for a Cohort of a specific Ruminant Type.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(RuminantType))]
    public class RuminantTypeCohort : Model
    {

        /// <summary>
        /// Gender
        /// </summary>
        [Description("Gender")]
        public string Gender { get; set; }

        /// <summary>
        /// Starting Age (Months)
        /// </summary>
        [Description("Starting Age")]
        public double StartingAge { get; set; }

        /// <summary>
        /// Starting Number
        /// </summary>
        [Description("Starting Number")]
        public double StartingNumber { get; set; }

        /// <summary>
        /// Starting Weight
        /// </summary>
        [Description("Starting Weight (kg)")]
        public double StartingWeight { get; set; }

        /// <summary>
        /// Starting Price
        /// </summary>
        [Description("Starting Price")]
        public double StartingPrice { get; set; }




        /// <summary>
        /// Create the individual ruminant animals using the Cohort parameterisations.
        /// </summary>
        /// <returns></returns>
        public List<Ruminant> CreateIndividuals()
        {
            List<Ruminant> Individuals = new List<Ruminant>();

            IModel parentNode = Apsim.Parent(this, typeof(IModel));
            RuminantType parent = parentNode as RuminantType;

            for (int i = 1; i <= StartingNumber; i++)
            {
                Ruminant ruminant = new Ruminant();

                ruminant.BreedParams = parent;

                ruminant.Gender = Gender;
                ruminant.Age = StartingAge;
                ruminant.Weight = StartingWeight;
                ruminant.Price = StartingPrice;

                Individuals.Add(ruminant);
            }

            return Individuals;
        }


    }




    /// <summary>
    /// Object for an individual Ruminant Animal.
    /// </summary>
    public class Ruminant
    {
        /// <summary>
        /// Reference to the Breed Parameters.
        /// </summary>
        public RuminantType BreedParams;

        /// <summary>
        /// Gender
        /// </summary>
        public string Gender;

        /// <summary>
        /// Age (Months)
        /// </summary>
        public double Age;

        /// <summary>
        /// Weight (kg)
        /// </summary>
        public double Weight;

        /// <summary>
        /// Price
        /// </summary>
        public double Price;

    }



}