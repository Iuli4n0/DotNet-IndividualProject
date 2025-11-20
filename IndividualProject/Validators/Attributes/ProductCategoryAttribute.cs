using System.ComponentModel.DataAnnotations;


namespace Week4.Validators.Attributes
{
    /// <summary>
    /// Validates that the product's category is one of the allowed categories.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
    public class ProductCategoryAttribute : ValidationAttribute
    {
        private readonly ProductCategory[] _allowedCategories;

        public ProductCategoryAttribute(params ProductCategory[] allowedCategories)
        {
            _allowedCategories = allowedCategories;
        }

        public override bool IsValid(object? value)
        {
            if (value is not ProductCategory category)
                return false;

            return _allowedCategories.Contains(category);
        }

        public override string FormatErrorMessage(string name)
        {
            string allowedList = string.Join(", ", _allowedCategories);
            return $"{name} must be one of the following categories: {allowedList}.";
        }
    }
}