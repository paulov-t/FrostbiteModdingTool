using FMT.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FMT.Controls.Models
{
    public class ModdableField : ModdableEntity
    {

        public FieldInfo Field { get; }
        public PropertyChangedEventHandler FieldChanged { get; }

        public ModdableField(string n, string t, object v) : base(n, t, v)
        {
        }



        public ModdableField(object rootObject, FieldInfo field, int? arrayIndex, PropertyChangedEventHandler modpropchanged = null, object vanillaRootObject = null)
           : base(field.Name, field.FieldType.Name, field.GetValue(rootObject))
        {
            RootObject = rootObject;
            VanillaRootObject = vanillaRootObject;
            Field = field;
            PropertyName = field.Name;
            PropertyType = field.FieldType.FullName;
            PropertyValue = field.GetValue(rootObject);

            PropertyOriginalValue = "";

            if (vanillaRootObject != null)
                PropertyOriginalValue = field.GetValue(vanillaRootObject);
            else
                PropertyOriginalValue = field.GetValue(rootObject);

            if (field.FieldType.FullName.Contains("List`1"))
            {
                ArrayType = field.FieldType.GetGenericArguments()[0];
                ArrayIndex = arrayIndex;
                if (ArrayIndex.HasValue)
                {
                    PropertyType = ArrayType.FullName;
                    PropertyName = ArrayIndex.Value.ToString();
                    PropertyValue = ((IList)field.GetValue(rootObject))[ArrayIndex.Value];

                    if (vanillaRootObject == null)
                        return;

                    var vanillaList = ((IList)field.GetValue(vanillaRootObject));
                    if (vanillaList == null)
                        return;

                    if (ArrayIndex.Value > vanillaList.Count - 1)
                        return;

                    PropertyOriginalValue = vanillaList[ArrayIndex.Value];
                }

            }
            if (modpropchanged != null)
                FieldChanged += modpropchanged;
        }


        public static IEnumerable<ModdableField> GetModdableFields(object obj, PropertyChangedEventHandler modpropchanged = null, object vanillaObj = null)
        {
            if (obj == null)
            {
                yield break;
            }

            var objType = obj.GetType();
            var objFields = objType.GetFields();
            foreach (var p in objFields)
            {
                var subObj = p.GetValue(obj);
                if (subObj != null)
                    yield return new ModdableField(obj, p, null, modpropchanged, vanillaObj);

            }
        }
    }
}
