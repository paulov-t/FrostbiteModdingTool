using System.Globalization;
using System.Windows.Controls;

namespace FMT.Controls.Validation
{ 
    internal class ValidationRuleUInt64 : ValidationRule
    {
        public ValidationRuleUInt64()
        {
        }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            return ulong.TryParse(value.ToString(), out _) ? ValidationResult.ValidResult : new ValidationResult(false, "Input is not a valid UInt64/ULong");
        }
    }
}
