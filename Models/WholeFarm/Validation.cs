using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.WholeFarm
{
    /// <summary>
    /// Tests if date greater than specified property name
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class DateGreaterThanAttribute : ValidationAttribute
    {
        private const string DefaultErrorMessage =
            "Date is less than the specified date";

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dateToCompareToFieldName"></param>
        public DateGreaterThanAttribute(string dateToCompareToFieldName)
        {
            DateToCompareToFieldName = dateToCompareToFieldName;
        }

        private string DateToCompareToFieldName { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            DateTime laterDate = (DateTime)value;
            DateTime earlierDate = (DateTime)validationContext.ObjectType.GetProperty(DateToCompareToFieldName).GetValue(validationContext.ObjectInstance, null);
            string[] memberNames = new string[] { validationContext.MemberName };

            if (laterDate > earlierDate)
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
    /// Tests if date greater than specified property name
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class GreaterThanAttribute : ValidationAttribute
    {
        private const string DefaultErrorMessage =
            "Value is less than the specified date";

        /// <summary>
        /// 
        /// </summary>
        /// <param name="compareToFieldName"></param>
        public GreaterThanAttribute(string compareToFieldName)
        {
            CompareToFieldName = compareToFieldName;
        }

        private string CompareToFieldName { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            double minvalue = Convert.ToDouble(value);
            string[] memberNames = new string[] { validationContext.MemberName };

            double maxvalue = Convert.ToDouble(validationContext.ObjectType.GetProperty(CompareToFieldName).GetValue(validationContext.ObjectInstance, null));

            if (maxvalue > minvalue)
            {
                return ValidationResult.Success;
            }
            else
            {
                return new ValidationResult(ErrorMessage ?? DefaultErrorMessage, memberNames);
            }
        }
    }



}
