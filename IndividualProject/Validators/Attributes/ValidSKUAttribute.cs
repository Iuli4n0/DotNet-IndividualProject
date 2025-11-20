using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.Text.RegularExpressions;

namespace Week4.Validators.Attributes
{
    /// <summary>
    /// Validates that a SKU is alphanumeric, may contain hyphens,
    /// and has a length between 5 and 20 characters.
    /// Supports both server and client-side validation.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
    public class ValidSKUAttribute : ValidationAttribute, IClientModelValidator
    {
        private const string Pattern = @"^[A-Za-z0-9-]{5,20}$";

        public ValidSKUAttribute()
        {
            // Default message shown if none is specified
            ErrorMessage = "SKU must be 5–20 characters long, alphanumeric, and may include hyphens.";
        }

        public override bool IsValid(object? value)
        {
            if (value == null)
                return false;

            var sku = value.ToString()!.Trim();

            // Remove spaces (as requested in requirements)
            sku = sku.Replace(" ", "");

            return Regex.IsMatch(sku, Pattern);
        }

        //Client-side validation integration
        public void AddValidation(ClientModelValidationContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            context.Attributes["data-val"] = "true";
            context.Attributes["data-val-validsku"] = ErrorMessage;
            context.Attributes["data-val-validsku-pattern"] = Pattern;
        }
    }
}