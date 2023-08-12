using System.Globalization;
using System.Reflection;
using System.Windows.Controls;

namespace FMT.Pages.Common.EBX.Validation
{
    internal class ValidationRuleTyped : ValidationRule
    {
        public PropertyInfo Property { get; set; }

        public ValidationRuleTyped()
        {
        }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            var propertyType = Property != null ? Property.PropertyType : null;
            if (propertyType != null)
            {
                return ValidationResult.ValidResult;
            }
            else
                return float.TryParse(value.ToString(), out _) ? ValidationResult.ValidResult : new ValidationResult(false, "");
        }
    }
}
