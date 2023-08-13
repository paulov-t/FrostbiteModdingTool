using System;
using System.Data;
using System.Linq;
using System.Reflection;

namespace CareerExpansionMod.CEM
{
    public enum NumberTrueFalse
    {
        False = 0,
        True = 1
    }
    public static class CEMUtilities
    {

        public static DateTime FIFACoreDateTime
        {
            get
            {
                return new DateTime(1582, 10, 14);
            }
        }

        public static string FormatCurrencyNumber(int num)
        {
            if (num >= 1000000)
            {

                return (num / 1000000D).ToString("0.#") + "M";

            }
            else if (num >= 100000)
                return FormatCurrencyNumber(num / 1000) + "K";
            else if (num >= 10000)
            {
                return (num / 1000D).ToString("0.#") + "K";
            }
            return num.ToString("#,0");
        }

        // function that creates an object from the given data row
        public static T CreateItemFromRow<T>(DataRow row, string tableShortName = null) where T : new()
        {
            // create a new object
            T item = new T();

            // set the item
            SetItemFromRow(item, row, tableShortName);

            // return 
            return item;
        }

        public static void SetItemFromRow<T>(T item, DataRow row, string tableShortName = null) where T : new()
        {
            // go through each column
            foreach (DataColumn c in row.Table.Columns)
            {

                PropertyInfo p = item.GetType().GetProperty(c.ColumnName);


                // if exists, set the value
                if (p != null && row[c] != DBNull.Value)
                {

                    p.SetValue(item, row[c], null);

                }
            }
        }
    }

    public static class EnumExtensionMethods
    {
        public static string GetDescription(this Enum GenericEnum)
        {
            Type genericEnumType = GenericEnum.GetType();
            MemberInfo[] memberInfo = genericEnumType.GetMember(GenericEnum.ToString());
            if ((memberInfo != null && memberInfo.Length > 0))
            {
                var _Attribs = memberInfo[0].GetCustomAttributes(typeof(System.ComponentModel.DescriptionAttribute), false);
                if ((_Attribs != null && _Attribs.Count() > 0))
                {
                    return ((System.ComponentModel.DescriptionAttribute)_Attribs.ElementAt(0)).Description;
                }
            }
            return GenericEnum.ToString();
        }

    }
}
