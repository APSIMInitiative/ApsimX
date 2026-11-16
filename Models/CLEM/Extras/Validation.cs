using APSIM.Numerics;
using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;

namespace Models.CLEM.Extras
{
    /// <summary>
    /// Tests if date greater than specified property name
    /// </summary>
    /// <remarks>
    ///
    /// </remarks>
    /// <param name="dateToCompareToFieldName"></param>
    [AttributeUsage(AttributeTargets.Property)]
    public class DateGreaterThanAttribute(string dateToCompareToFieldName) : ValidationAttribute
    {
        private string DefaultErrorMessage;

        private readonly string dateToCompareToFieldName = dateToCompareToFieldName;

        /// <summary>
        ///
        /// </summary>
        /// <param name="value"></param>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            // check valid property name
            if (validationContext.ObjectType.GetProperty(dateToCompareToFieldName) == null)
            {
                throw new Exception(String.Format("Invalid property name [{0}] provided for validation attribute [DateGreaterThan] on property [{1}] in [{2}]", dateToCompareToFieldName, validationContext.MemberName, validationContext.ObjectInstance.ToString()));
            }

            DateTime laterDate = (DateTime)value;
            DateTime earlierDate = (DateTime)validationContext.ObjectType.GetProperty(dateToCompareToFieldName).GetValue(validationContext.ObjectInstance, null);
            string[] memberNames = [validationContext.MemberName];

            if (laterDate > earlierDate)
            {
                return ValidationResult.Success;
            }
            else
            {
                DefaultErrorMessage = $"Date [{laterDate}] must be greater than {dateToCompareToFieldName} [{earlierDate}]";
                return new ValidationResult(ErrorMessage ?? DefaultErrorMessage, memberNames);
            }
        }
    }

    /// <summary>
    /// Tests if double/int is percentage
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class PercentageAttribute : ValidationAttribute
    {
        private readonly string DefaultErrorMessage = "Value must be a percentage (0-100)";

        /// <summary>
        ///
        /// </summary>
        public PercentageAttribute()
        {
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="value"></param>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            double maxValue = Convert.ToDouble(value, CultureInfo.InvariantCulture);
            string[] memberNames = [validationContext.MemberName];

            if ((MathUtilities.IsGreaterThanOrEqual(maxValue, 0)) && MathUtilities.IsLessThanOrEqual(maxValue, 100))
            {
                return ValidationResult.Success;
            }
            else
            {
                return new ValidationResult(ErrorMessage ?? DefaultErrorMessage, memberNames);
            }
        }
    }

    /// <summary>
    /// Tests if double/int is percentage
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class ProportionAttribute : ValidationAttribute
    {
        private readonly string DefaultErrorMessage = "Value must be a proportion (0-1)";

        /// <summary>
        ///
        /// </summary>
        public ProportionAttribute()
        {
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="value"></param>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            string[] memberNames = [validationContext.MemberName];

            double[] listValues;
            if (value != null && value.GetType().IsArray)
            {
                listValues = value as double[];
            }
            else
            {
                listValues = [Convert.ToDouble(value, CultureInfo.InvariantCulture)];
            }

            foreach (double item in listValues)
            {
                if (MathUtilities.IsNegative(item) | MathUtilities.IsGreaterThan(item, 1))
                {
                    return new ValidationResult(ErrorMessage ?? DefaultErrorMessage, memberNames);
                }
            }

            return ValidationResult.Success;
        }
    }

    /// <summary>
    /// Tests if int is month range
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class MonthAttribute : ValidationAttribute
    {
        private readonly string DefaultErrorMessage = "Value must represent a month from [1-January] to [12-December]";

        /// <summary>
        ///
        /// </summary>
        public MonthAttribute()
        {
        }

        /// <summary>
        /// Method to determine if the MonthAttribute is valid for the value provided
        /// </summary>
        /// <param name="value"></param>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            int monthValue;

            if(value.GetType().IsEnum)
            {
                monthValue = (int)value;
            }
            else
            {
                monthValue = Convert.ToInt32(value, CultureInfo.InvariantCulture);
            }

            string[] memberNames = [validationContext.MemberName];

            if ((monthValue >= 1) && (monthValue <= 12))
            {
                return ValidationResult.Success;
            }
            else
            {
                return new ValidationResult(ErrorMessage ?? DefaultErrorMessage, memberNames);
            }
        }
    }

    /// <summary>
    /// Tests if double/int greater than specified value
    /// </summary>
    /// <remarks>
    ///
    /// </remarks>
    /// <param name="value"></param>
    [AttributeUsage(AttributeTargets.Property)]
    public class GreaterThanValueAttribute(object value) : ValidationAttribute
    {
        private string DefaultErrorMessage;

        private readonly double compareValue = Convert.ToDouble(value, CultureInfo.InvariantCulture);

        /// <summary>
        ///
        /// </summary>
        /// <param name="value"></param>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            double maxValue;
            if (value.GetType().IsArray)
            {
                maxValue = (value as double[]).Min();
            }
            else
            {
                maxValue = Convert.ToDouble(value);
            }

            string[] memberNames = [validationContext.MemberName];

            if (MathUtilities.IsGreaterThan(maxValue, compareValue))
            {
                return ValidationResult.Success;
            }
            else
            {
                DefaultErrorMessage = $"Value [{maxValue}] must be greater than [{compareValue}]";
                return new ValidationResult(ErrorMessage ?? DefaultErrorMessage, memberNames);
            }
        }
    }

    /// <summary>
    /// Tests if double/int greater than specified value
    /// </summary>
    /// <remarks>
    ///
    /// </remarks>
    /// <param name="value"></param>
    [AttributeUsage(AttributeTargets.Property)]
    public class GreaterThanEqualValueAttribute(object value) : ValidationAttribute
    {
        private string DefaultErrorMessage;

        private readonly double compareValue = Convert.ToDouble(value, CultureInfo.InvariantCulture);

        /// <summary>
        ///
        /// </summary>
        /// <param name="value"></param>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value is null)
            {
                return ValidationResult.Success;
            }

            double maxValue;
            if (value.GetType().IsArray)
            {
                maxValue = (value as double[]).Min();
            }
            else
            {
                maxValue = Convert.ToDouble(value);
            }

            string[] memberNames = [validationContext.MemberName];

            if (MathUtilities.IsGreaterThanOrEqual(maxValue, compareValue))
            {
                return ValidationResult.Success;
            }
            else
            {
                DefaultErrorMessage = $"Value [{maxValue}] must be greater than or equal to [{compareValue}]";
                return new ValidationResult(ErrorMessage ?? DefaultErrorMessage, memberNames);
            }
        }
    }

    /// <summary>
    /// Tests if herd change reason is of a specified style (purchase or sale)
    /// </summary>
    /// <remarks>
    /// Constructor
    /// </remarks>
    /// <param name="value"></param>
    [AttributeUsage(AttributeTargets.Property)]
    public class HerdSaleReasonAttribute(object value) : ValidationAttribute
    {
        private string DefaultErrorMessage;

        private readonly string compareStyle = Convert.ToString(value, CultureInfo.InvariantCulture);

        /// <summary>
        /// Perform validation method
        /// </summary>
        /// <param name="value"></param>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (!Enum.TryParse<HerdChangeReason>(value.ToString(), out HerdChangeReason changeReason))
            {
                throw new Exception($"The property type {value.GetType().Name} is not permitted for HerdSaleReasonValidation attribute");
            }

            string result = "";
            switch (changeReason)
            {
                case HerdChangeReason.MarkedSale:
                case HerdChangeReason.TradeSale:
                case HerdChangeReason.DryBreederSale:
                case HerdChangeReason.ExcessBreederSale:
                case HerdChangeReason.ExcessPreBreederSale:
                case HerdChangeReason.ExcessSireSale:
                case HerdChangeReason.MaxAgeSale:
                case HerdChangeReason.AgeWeightSale:
                case HerdChangeReason.DestockSale:
                case HerdChangeReason.WeanerSale:
                    result = "sale";
                    break;
                case HerdChangeReason.TradePurchase:
                case HerdChangeReason.BreederPurchase:
                case HerdChangeReason.SirePurchase:
                case HerdChangeReason.RestockPurchase:
                    result = "purchase";
                    break;
                default:
                    break;
            }

            string[] memberNames = [validationContext.MemberName];

            if (result == compareStyle)
            {
                return ValidationResult.Success;
            }
            else
            {
                DefaultErrorMessage = $"Value [{ changeReason }] must be a {compareStyle} change reason";
                return new ValidationResult(ErrorMessage ?? DefaultErrorMessage, memberNames);
            }
        }
    }

    /// <summary>
    /// Tests if date greater than specified property name
    /// </summary>
    /// <remarks>
    ///
    /// </remarks>
    /// <param name="compareToFieldName"></param>
    [AttributeUsage(AttributeTargets.Property)]
    public class GreaterThanAttribute(string compareToFieldName) : ValidationAttribute
    {
        private string DefaultErrorMessage;

        private readonly string compareToFieldName = compareToFieldName;

        /// <summary>
        ///
        /// </summary>
        /// <param name="value"></param>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            double maxValue = Convert.ToDouble(value, CultureInfo.InvariantCulture);
            string[] memberNames = [validationContext.MemberName];

            // check valid property name
            if (validationContext.ObjectType.GetProperty(compareToFieldName) == null)
            {
                throw new Exception(String.Format("Invalid property name [{0}] provided for validation attribute [GreaterThan] on property [{1}] in [{2}]", compareToFieldName, validationContext.MemberName, validationContext.ObjectInstance.ToString()));
            }

            double minValue = Convert.ToDouble(validationContext.ObjectType.GetProperty(compareToFieldName).GetValue(validationContext.ObjectInstance, null), CultureInfo.InvariantCulture);

            if (MathUtilities.IsGreaterThan(maxValue, minValue))
            {
                return ValidationResult.Success;
            }
            else
            {
                DefaultErrorMessage = $"Value [{maxValue}] must be greater than {compareToFieldName} [{minValue}]";
                return new ValidationResult(ErrorMessage ?? DefaultErrorMessage, memberNames);
            }
        }
    }

    /// <summary>
    /// Tests if date greater than specified property name
    /// </summary>
    /// <remarks>
    ///
    /// </remarks>
    /// <param name="compareToFieldName"></param>
    [AttributeUsage(AttributeTargets.Property)]
    public class GreaterThanEqualAttribute(string compareToFieldName) : ValidationAttribute
    {
        private string DefaultErrorMessage;

        private readonly string compareToFieldName = compareToFieldName;

        /// <summary>
        ///
        /// </summary>
        /// <param name="value"></param>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            double maxValue = Convert.ToDouble(value);
            string[] memberNames = [validationContext.MemberName];

            // check valid property name
            if (validationContext.ObjectType.GetProperty(compareToFieldName) == null)
            {
                throw new Exception(String.Format("Invalid property name [{0}] provided for validation attribute [DateGreaterThan] on property [{1}] in [{2}]", compareToFieldName, validationContext.MemberName, validationContext.ObjectInstance.ToString()));
            }

            double minValue = Convert.ToDouble(validationContext.ObjectType.GetProperty(compareToFieldName).GetValue(validationContext.ObjectInstance, null), CultureInfo.InvariantCulture);

            if (MathUtilities.IsGreaterThanOrEqual(maxValue, minValue))
            {
                return ValidationResult.Success;
            }
            else
            {
                DefaultErrorMessage = $"Value [{maxValue}] must be greater than or equal to {compareToFieldName} [{minValue}]";
                return new ValidationResult(ErrorMessage ?? DefaultErrorMessage, memberNames);
            }
        }
    }

    /// <summary>
    /// Tests if the number of items in an array match specified value
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class ArrayItemCountAttribute : ValidationAttribute
    {
        private string DefaultErrorMessage =
            "Invalid number of values supplied";

        /// <summary>
        /// Test for set number of items in array
        /// </summary>
        /// <param name="arrayItems"></param>
        public ArrayItemCountAttribute(int arrayItems)
        {
            minNumberOfArrayItems = arrayItems;
            maxNumberOfArrayItems = arrayItems;
        }

        /// <summary>
        /// Test for set number of items in array
        /// </summary>
        /// <param name="minArrayItems">The minimum number of array items allowed</param>
        /// <param name="maxArrayItems">The maximum number of array items allowed</param>
        public ArrayItemCountAttribute(int minArrayItems, int maxArrayItems)
        {
            minNumberOfArrayItems = minArrayItems;
            maxNumberOfArrayItems = maxArrayItems;
        }

        private readonly int minNumberOfArrayItems;
        private readonly int maxNumberOfArrayItems;

        /// <summary>
        ///
        /// </summary>
        /// <param name="value"></param>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value is null)
            {
                return ValidationResult.Success;
            }

            if (minNumberOfArrayItems == maxNumberOfArrayItems)
            {
                DefaultErrorMessage += $" (expecting {minNumberOfArrayItems} values)";
            }
            else
            {
                DefaultErrorMessage += $" (expecting between {minNumberOfArrayItems} and {maxNumberOfArrayItems} values)";
            }
            string[] memberNames = [validationContext.MemberName];

            if (value.GetType().IsArray)
            {
                if ((value as Array).Length >= minNumberOfArrayItems && (value as Array).Length <= maxNumberOfArrayItems)
                {
                    return ValidationResult.Success;
                }
                else
                {
                    return new ValidationResult(ErrorMessage ?? DefaultErrorMessage, memberNames);
                }
            }
            else
            {
                return new ValidationResult(ErrorMessage ?? DefaultErrorMessage, memberNames);
            }
        }
    }



}
