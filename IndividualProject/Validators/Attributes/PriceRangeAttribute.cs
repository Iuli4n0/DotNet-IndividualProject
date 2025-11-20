using System;
using System.ComponentModel.DataAnnotations;

namespace Week4.Validators.Attributes
{
    /// <summary>
    /// Validates that a decimal value is between a minimum and maximum price.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
    public class PriceRangeAttribute : ValidationAttribute
    {
        private readonly decimal _min;
        private readonly decimal _max;

        public PriceRangeAttribute(double min, double max)
        {
            _min = (decimal)min;
            _max = (decimal)max;
        }

        public override bool IsValid(object? value)
        {
            if (value == null)
                return false;

            if (value is not decimal price)
                return false;

            return price >= _min && price <= _max;
        }

        public override string FormatErrorMessage(string name)
        {
            return $"{name} must be between {_min:C2} and {_max:C2}.";
        }
    }
}