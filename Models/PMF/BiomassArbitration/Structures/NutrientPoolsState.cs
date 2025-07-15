using System;
using APSIM.Numerics;
using APSIM.Shared.Utilities;
using Models.Core;

namespace Models.PMF
{

    /// <summary>
    /// The class that holds states of Structural, Metabolic and Storage components of a resource
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Organ))]
    [ValidParent(ParentType = typeof(OrganNutrientDelta))]
    [ValidParent(ParentType = typeof(OrganNutrientsState))]
    public class NutrientPoolsState : Model
    {
        /// <summary>Gets or sets the structural.</summary>
        [Units("g/m2")]
        public double Structural { get; private set; }
        /// <summary>Gets or sets the storage.</summary>
        [Units("g/m2")]
        public double Storage { get; private set; }
        /// <summary>Gets or sets the metabolic.</summary>
        [Units("g/m2")]
        public double Metabolic { get; private set; }
        /// <summary>Gets the total amount of biomass.</summary>
        [Units("g/m2")]
        public double Total { get; private set; }

        private double tolerence = 1e-11;

        /// <summary>parameterless constructor.</summary>
        public NutrientPoolsState()
        {
        }


        /// <summary>the constructor.</summary>
        public NutrientPoolsState(double structural, double metabolic, double storage)
        {
            Structural = structural;
            Metabolic = metabolic;
            Storage = storage;
            Total = structural + metabolic + storage;
            testPools(this);
        }

        /// <summary>the constructor.</summary>
        public NutrientPoolsState(NutrientPoolsState values)
        {
            Structural = values.Structural;
            Metabolic = values.Metabolic;
            Storage = values.Storage;
            Total = Structural + Metabolic + Storage;
            testPools(this);
        }

        /// <summary>Pools can not be negative.  Test for negatives each time an opperator is applied</summary>
        private void testPools(NutrientPoolsState p)
        {
            if (p.Structural < 0)
            {
                if (p.Structural < -tolerence) //Throw if really negative
                    throw new Exception(this.FullPath + ".Structural was set to negative value");
                else  // if negative in floating point tollerence, zero the pool
                    this.Structural = 0.0;
            }
            if (p.Metabolic < 0)
            {
                if (p.Metabolic < -tolerence) //Throw if really negative
                    throw new Exception(this.FullPath + ".Metabolic was set to negative value");
                else  // if negative in floating point tollerence, zero the pool
                    this.Metabolic = 0.0;
            }
            if (p.Storage < 0)
            {
                if (p.Storage < -tolerence) //Throw if really negative
                    throw new Exception(this.FullPath + ".Storage was set to negative value");
                else  // if negative in floating point tollerence, zero the pool
                    this.Storage = 0.0;
            }
            if (Double.IsNaN(p.Structural))
                throw new Exception(this.FullPath + ".Structural was set to nan");
            if (Double.IsNaN(p.Metabolic))
                throw new Exception(this.FullPath + ".Metabolic was set to nan");
            if (Double.IsNaN(p.Storage))
                throw new Exception(this.FullPath + ".Storage was set to nan");
        }



        /// <summary>return pools divied by value</summary>
        public static NutrientPoolsState operator /(NutrientPoolsState a, double b)
        {
            return new NutrientPoolsState(
            MathUtilities.Divide(a.Structural, b, 0),
            MathUtilities.Divide(a.Metabolic, b, 0),
            MathUtilities.Divide(a.Storage, b, 0));
        }

        /// <summary>return pools divied by value</summary>
        public static NutrientPoolsState operator /(NutrientPoolsState a, NutrientPoolsState b)
        {
            return new NutrientPoolsState(
            MathUtilities.Divide(a.Structural, b.Structural, 0),
            MathUtilities.Divide(a.Metabolic, b.Metabolic, 0),
            MathUtilities.Divide(a.Storage, b.Storage, 0));
        }

        /// <summary>return pools multiplied by value</summary>
        public static NutrientPoolsState operator *(NutrientPoolsState a, double b)
        {
            return new NutrientPoolsState(
                a.Structural * b,
                a.Metabolic * b,
                a.Storage * b);
        }

        /// <summary>return pools divied by value</summary>
        public static NutrientPoolsState operator *(NutrientPoolsState a, NutrientPoolsState b)
        {
            return new NutrientPoolsState(
                a.Structural * b.Structural,
                a.Metabolic * b.Metabolic,
                a.Storage * b.Storage);
        }

        /// <summary>return sum or two pools</summary>
        public static NutrientPoolsState operator +(NutrientPoolsState a, NutrientPoolsState b)
        {
            return new NutrientPoolsState(
                a.Structural + b.Structural,
                a.Metabolic + b.Metabolic,
                a.Storage + b.Storage);
        }

        /// <summary>return pool a - pool b</summary>
        public static NutrientPoolsState operator -(NutrientPoolsState a, NutrientPoolsState b)
        {
            return new NutrientPoolsState(
                a.Structural - b.Structural,
                a.Metabolic - b.Metabolic,
                a.Storage - b.Storage);
        }

    }

}

