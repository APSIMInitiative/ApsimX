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
            DateTime earlierDate = (DateTime)value;

            DateTime laterDate = (DateTime)validationContext.ObjectType.GetProperty(DateToCompareToFieldName).GetValue(validationContext.ObjectInstance, null);

            if (laterDate > earlierDate)
            {
                return ValidationResult.Success;
            }
            else
            {
                return new ValidationResult("Date is not later");
            }
        }
    }

    /// <summary>
    /// Tests if date greater than specified property name
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class GreaterThanAttribute : ValidationAttribute
    {
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

            double maxvalue = Convert.ToDouble(validationContext.ObjectType.GetProperty(CompareToFieldName).GetValue(validationContext.ObjectInstance, null));

            if (maxvalue > minvalue)
            {
                return ValidationResult.Success;
            }
            else
            {
                return new ValidationResult("Value is smaller than compared value");
            }
        }
    }



}
