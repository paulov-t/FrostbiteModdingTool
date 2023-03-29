using System.Globalization;
using System.Windows.Controls;

namespace FMT.Pages.Common.EBX.Validation
{
    internal class ValidationRuleUInt : ValidationRule
    {
        public ValidationRuleUInt()
        {
        }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            return uint.TryParse(value.ToString(), out _) ? ValidationResult.ValidResult : new ValidationResult(false, "Input is not a valid Integer");
        }
    }
}
