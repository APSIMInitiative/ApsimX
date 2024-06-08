using APSIM.Shared.Utilities;
using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;

namespace Models.CLEM
{
    /// <summary>
    /// Tests if date greater than specified property name
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class DateGreaterThanAttribute : ValidationAttribute
    {
        private string DefaultErrorMessage;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dateToCompareToFieldName"></param>
        public DateGreaterThanAttribute(string dateToCompareToFieldName)
        {
            this.dateToCompareToFieldName = dateToCompareToFieldName;
        }

        private string dateToCompareToFieldName { get; set; }

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
                throw new Exception(String.Format("Invalid property name [{0}] provided for validation attribute [DateGreaterThan] on property [{1}] in [{2}]", dateToCompareToFieldName, validationContext.MemberName, validationContext.ObjectInstance.ToString()));

            DateTime laterDate = (DateTime)value;
            DateTime earlierDate = (DateTime)validationContext.ObjectType.GetProperty(dateToCompareToFieldName).GetValue(validationContext.ObjectInstance, null);
            string[] memberNames = new string[] { validationContext.MemberName };

            if (laterDate > earlierDate)
                return ValidationResult.Success;
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

        private double compareValue { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            double maxvalue = Convert.ToDouble(value, CultureInfo.InvariantCulture);
            string[] memberNames = new string[] { validationContext.MemberName };

            if ((MathUtilities.IsGreaterThanOrEqual(maxvalue, 0)) && MathUtilities.IsLessThanOrEqual(maxvalue, 100))
                return ValidationResult.Success;
            else
                return new ValidationResult(ErrorMessage ?? DefaultErrorMessage, memberNames);
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

        private double compareValue { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            string[] memberNames = new string[] { validationContext.MemberName };

            double[] listvalues;
            if (value != null && value.GetType().IsArray)
                listvalues = value as double[];
            else
                listvalues = new double[] { Convert.ToDouble(value, CultureInfo.InvariantCulture) };

            // allow for arrays of values to be checked
            foreach (double item in listvalues)
                if (MathUtilities.IsNegative(item) | MathUtilities.IsGreaterThan(item, 1))
                    return new ValidationResult(ErrorMessage ?? DefaultErrorMessage, memberNames);

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

        private double compareValue { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            int monthvalue;

            if(value.GetType().IsEnum)
                monthvalue = (int)value;
            else
                monthvalue = Convert.ToInt32(value, CultureInfo.InvariantCulture);
                
            string[] memberNames = new string[] { validationContext.MemberName };

            if ((monthvalue >= 1) && (monthvalue <= 12))
                return ValidationResult.Success;
            else
                return new ValidationResult(ErrorMessage ?? DefaultErrorMessage, memberNames);
        }
    }

    /// <summary>
    /// Tests if double/int greater than specified value
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class GreaterThanValueAttribute : ValidationAttribute
    {
        private string DefaultErrorMessage;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public GreaterThanValueAttribute(object value)
        {
            compareValue = Convert.ToDouble(value, CultureInfo.InvariantCulture);
        }

        private double compareValue { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            double maxvalue;
            if (value.GetType().IsArray)
                maxvalue = (value as double[]).Min();
            else
                maxvalue = Convert.ToDouble(value);

            string[] memberNames = new string[] { validationContext.MemberName };

            if (MathUtilities.IsGreaterThan(maxvalue, compareValue))
                return ValidationResult.Success;
            else
            {
                DefaultErrorMessage = $"Value [{maxvalue}] must be greater than [{compareValue}]";
                return new ValidationResult(ErrorMessage ?? DefaultErrorMessage, memberNames);
            }
        }
    }

    /// <summary>
    /// Tests if double/int greater than specified value
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class GreaterThanEqualValueAttribute : ValidationAttribute
    {
        private string DefaultErrorMessage;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public GreaterThanEqualValueAttribute(object value)
        {
            compareValue = Convert.ToDouble(value, CultureInfo.InvariantCulture);
        }

        private double compareValue { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value is null   )
                return ValidationResult.Success;

            double maxvalue;
            if (value.GetType().IsArray)
                maxvalue = (value as double[]).Min();
            else
                maxvalue = Convert.ToDouble(value);

            string[] memberNames = new string[] { validationContext.MemberName };

            if (MathUtilities.IsGreaterThanOrEqual(maxvalue, compareValue))
                return ValidationResult.Success;
            else
            {
                DefaultErrorMessage = $"Value [{maxvalue}] must be greater than or equal to [{compareValue}]";
                return new ValidationResult(ErrorMessage ?? DefaultErrorMessage, memberNames);
            }
        }
    }

    /// <summary>
    /// Tests if herd change reason is of a specified style (purchase or sale)
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class HerdSaleReasonAttribute : ValidationAttribute
    {
        private string DefaultErrorMessage;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="value"></param>
        public HerdSaleReasonAttribute(object value)
        {
            compareStyle = Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        private string compareStyle { get; set; }

        /// <summary>
        /// Perfom validation method
        /// </summary>
        /// <param name="value"></param>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (!Enum.TryParse<HerdChangeReason>(value.ToString(), out HerdChangeReason changeReason))
                throw new Exception($"The property type {value.GetType().Name} is not permitted for HerdSaleReasonValidation attribute");

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

            string[] memberNames = new string[] { validationContext.MemberName };

            if (result == compareStyle)
                return ValidationResult.Success;
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
    [AttributeUsage(AttributeTargets.Property)]
    public class GreaterThanAttribute : ValidationAttribute
    {
        private string DefaultErrorMessage;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="compareToFieldName"></param>
        public GreaterThanAttribute(string compareToFieldName)
        {
            this.compareToFieldName = compareToFieldName;
        }

        private string compareToFieldName { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            double maxvalue = Convert.ToDouble(value, CultureInfo.InvariantCulture);
            string[] memberNames = new string[] { validationContext.MemberName };

            // check valid property name
            if(validationContext.ObjectType.GetProperty(compareToFieldName) == null)
                throw new Exception(String.Format("Invalid property name [{0}] provided for validation attribute [GreaterThan] on property [{1}] in [{2}]", compareToFieldName, validationContext.MemberName, validationContext.ObjectInstance.ToString()));

            double minvalue = Convert.ToDouble(validationContext.ObjectType.GetProperty(compareToFieldName).GetValue(validationContext.ObjectInstance, null), CultureInfo.InvariantCulture);

            if (MathUtilities.IsGreaterThan(maxvalue, minvalue))
                return ValidationResult.Success;
            else
            {
                DefaultErrorMessage = $"Value [{maxvalue}] must be greater than {compareToFieldName} [{minvalue}]";
                return new ValidationResult(ErrorMessage ?? DefaultErrorMessage, memberNames);
            }
        }
    }

    /// <summary>
    /// Tests if date greater than specified property name
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class GreaterThanEqualAttribute : ValidationAttribute
    {
        private string DefaultErrorMessage;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="compareToFieldName"></param>
        public GreaterThanEqualAttribute(string compareToFieldName)
        {
            this.compareToFieldName = compareToFieldName;
        }

        private string compareToFieldName { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            double maxvalue = Convert.ToDouble(value);
            string[] memberNames = new string[] { validationContext.MemberName };

            // check valid property name
            if (validationContext.ObjectType.GetProperty(compareToFieldName) == null)
                throw new Exception(String.Format("Invalid property name [{0}] provided for validation attribute [DateGreaterThan] on property [{1}] in [{2}]", compareToFieldName, validationContext.MemberName, validationContext.ObjectInstance.ToString()));

            double minvalue = Convert.ToDouble(validationContext.ObjectType.GetProperty(compareToFieldName).GetValue(validationContext.ObjectInstance, null), CultureInfo.InvariantCulture);

            if (MathUtilities.IsGreaterThanOrEqual(maxvalue, minvalue))
                return ValidationResult.Success;
            else
            {
                DefaultErrorMessage = $"Value [{maxvalue}] must be greater than or equal to {compareToFieldName} [{minvalue}]";
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
        /// Test for set number of itesma in array
        /// </summary>
        /// <param name="arrayItems"></param>
        public ArrayItemCountAttribute(int arrayItems)
        {
            minNumberOfArrayItems = arrayItems;
            maxNumberOfArrayItems = arrayItems;
        }

        /// <summary>
        /// Test for set number of itesma in array
        /// </summary>
        /// <param name="minArrayItems">The minimum number of array items allowed</param>
        /// <param name="maxArrayItems">The maximum number of array items allowed</param>
        public ArrayItemCountAttribute(int minArrayItems, int maxArrayItems)
        {
            minNumberOfArrayItems = minArrayItems;
            maxNumberOfArrayItems = maxArrayItems;
        }

        private int minNumberOfArrayItems { get; set; }
        private int maxNumberOfArrayItems { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if(value is null)
                return ValidationResult.Success;

            if (minNumberOfArrayItems == maxNumberOfArrayItems)
            {
                DefaultErrorMessage += $" (expecting {minNumberOfArrayItems} values)";
            }
            else
            {
                DefaultErrorMessage += $" (expecting between {minNumberOfArrayItems} and {maxNumberOfArrayItems} values)";
            }
            string[] memberNames = new string[] { validationContext.MemberName };

            if (value.GetType().IsArray)
            {
                if ((value as Array).Length >= minNumberOfArrayItems && (value as Array).Length <= maxNumberOfArrayItems)
                    return ValidationResult.Success;
                else
                    return new ValidationResult(ErrorMessage ?? DefaultErrorMessage, memberNames);
            }
            else
                return new ValidationResult(ErrorMessage ?? DefaultErrorMessage, memberNames);
        }
    }



}
