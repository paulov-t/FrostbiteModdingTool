using System.Globalization;
using System.Windows.Controls;

namespace FMT.Controls.Validation
{
    internal class ValidationRuleInteger : ValidationRule
    {
        public ValidationRuleInteger()
        {
        }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            return int.TryParse(value.ToString(), out _) ? ValidationResult.ValidResult : new ValidationResult(false, "Input is not a valid Integer");
        }
    }
}
