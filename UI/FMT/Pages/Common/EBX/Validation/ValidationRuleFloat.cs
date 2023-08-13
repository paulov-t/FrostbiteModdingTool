using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Controls;

namespace FMT.Pages.Common.EBX.Validation
{
    internal class ValidationRuleFloat : ValidationRule
    {
        public ValidationRuleFloat()
        {
        }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            var invalidFloat = new ValidationResult(false, "Input is not a valid Float");
            if (value == null)
                return invalidFloat;

            // ------------------------------------------
            // Issue 34 Fixes ---------------------------

            if (value.ToString().Contains(","))
                return invalidFloat;

            //if (!value.ToString().Contains("."))
            //    return invalidFloat;

            if (value.ToString().EndsWith("."))
                return invalidFloat;

            if (!Regex.IsMatch(value.ToString(), "\\d+(?:\\.?\\d+)?"))
                return invalidFloat;

            //
            // ------------------------------------------

            return float.TryParse(value.ToString(), cultureInfo.NumberFormat, out _) ? ValidationResult.ValidResult : invalidFloat;
        }
    }
}
