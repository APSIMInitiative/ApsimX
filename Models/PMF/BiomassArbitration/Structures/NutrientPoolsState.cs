namespace Models.PMF
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using Models.Functions;
    using Models.PMF.Interfaces;
    using Models.PMF.Organs;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// An interface that any class that has a NutrientPoolState child must implement
    /// </summary>
    public interface IParentOfNutrientsPoolState
    {
        /// <summary>Update own properties and tell parent class to update its properties that are derived from this</summary>
        void UpdateProperties();
    }

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

        private double tolerence = 1e-12;

        /// <summary>Update own properties and tell parent class to update its properties that are derived from this</summary>
        private void updateProperties(IParentOfNutrientsPoolState ponps)
        {
            Total = Structural + Metabolic + Storage;
            if (ponps != null)
                ponps.UpdateProperties();
        }

        /// <summary>parameterless constructor.</summary>
        public NutrientPoolsState()
        { }


        /// <summary>the constructor.</summary>
        public NutrientPoolsState(double structural, double metabolic, double storage, IParentOfNutrientsPoolState ponps)
        {
            Structural = structural;
            Metabolic = metabolic;
            Storage = storage;
            updateProperties(ponps);
            testPools(this);
        }

        /// <summary>Clear</summary>
        public void Clear()
        {
            Structural = 0;
            Storage = 0;
            Metabolic = 0;
            Total = 0;
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

        /// <summary>Add Delta</summary>
        public void AddDelta(NutrientPoolsState delta, IParentOfNutrientsPoolState ponps)
        {
            if (delta.Structural < -tolerence)
                throw new Exception(this.FullPath + ".Structural trying to add a negative");
            if (delta.Metabolic < -tolerence)
                throw new Exception(this.FullPath + ".Metabolic trying to add a negative");
            if (delta.Storage < -tolerence)
                throw new Exception(this.FullPath + ".Storage trying to add a negative");

            Structural += delta.Structural;
            Metabolic += delta.Metabolic;
            Storage += delta.Storage;
            updateProperties(ponps);
            testPools(this);
        }

        /// <summary>subtract Delta</summary>
        public void SubtractDelta(NutrientPoolsState delta, IParentOfNutrientsPoolState ponps)
        {
            if (delta.Structural < -tolerence)
                throw new Exception(this.FullPath + ".Structural trying to subtract a negative");
            if (delta.Metabolic < -tolerence)
                throw new Exception(this.FullPath + ".Metabolic trying to subtract a negative");
            if (delta.Storage < -tolerence)
                throw new Exception(this.FullPath + ".Storage trying to subtract a negative");

            Structural -= delta.Structural;
            Metabolic -= delta.Metabolic;
            Storage -= delta.Storage;
            updateProperties(ponps);
            testPools(this);
        }

        /// <summary>Set to new value</summary>
        public void SetTo(NutrientPoolsState newValue, IParentOfNutrientsPoolState ponps)
        {
            Structural = newValue.Structural;
            Metabolic = newValue.Metabolic;
            Storage = newValue.Storage;
            updateProperties(ponps);
            testPools(this);
        }

        /// <summary>multiply by value</summary>
        public void MultiplyBy(double multiplier, IParentOfNutrientsPoolState ponps)
        {
            Structural *= multiplier;
            Metabolic *= multiplier;
            Storage *= multiplier;
            updateProperties(ponps);
            testPools(this);
        }

        /// <summary>divide by value</summary>
        public void DivideBy(double divisor, IParentOfNutrientsPoolState ponps)
        {
            Structural /= divisor;
            Metabolic /= divisor;
            Storage /= divisor;
            updateProperties(ponps);
            testPools(this);
        }

        /// <summary>return pools divied by value</summary>
        public static NutrientPoolsState operator /(NutrientPoolsState a, double b)
        {
            return new NutrientPoolsState(
            MathUtilities.Divide(a.Structural, b, 0),
            MathUtilities.Divide(a.Metabolic, b, 0),
            MathUtilities.Divide(a.Storage, b, 0),
            null);
        }

        /// <summary>return pools divied by value</summary>
        public static NutrientPoolsState operator /(NutrientPoolsState a, NutrientPoolsState b)
        {
            return new NutrientPoolsState(
            MathUtilities.Divide(a.Structural, b.Structural, 0),
            MathUtilities.Divide(a.Metabolic, b.Metabolic, 0),
            MathUtilities.Divide(a.Storage, b.Storage, 0),
            null);
        }

        /// <summary>return pools multiplied by value</summary>
        public static NutrientPoolsState operator *(NutrientPoolsState a, double b)
        {
            return new NutrientPoolsState(
                a.Structural * b,
                a.Metabolic * b,
                a.Storage * b,
                null);
        }

        /// <summary>return pools divied by value</summary>
        public static NutrientPoolsState operator *(NutrientPoolsState a, NutrientPoolsState b)
        {
            return new NutrientPoolsState(
                a.Structural * b.Structural,
                a.Metabolic * b.Metabolic,
                a.Storage * b.Storage,
                null);
        }

        /// <summary>return sum or two pools</summary>
        public static NutrientPoolsState operator +(NutrientPoolsState a, NutrientPoolsState b)
        {
            return new NutrientPoolsState(
                a.Structural + b.Structural,
                a.Storage + b.Storage,
                a.Metabolic + b.Metabolic,
                null);
        }

        /// <summary>return pool a - pool b</summary>
        public static NutrientPoolsState operator -(NutrientPoolsState a, NutrientPoolsState b)
        {
            return new NutrientPoolsState(
                a.Structural - b.Structural,
                a.Storage - b.Storage,
                a.Metabolic - b.Metabolic,
                null);
        }

    }

}

