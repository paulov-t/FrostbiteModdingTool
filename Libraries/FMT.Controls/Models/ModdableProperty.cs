using FMT.Controls.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows;

namespace FMT.Models
{

    public class ModdableProperty : ModdableEntity, INotifyPropertyChanged
    {
        private object propValue;

        public override object PropertyValue
        {
            get { return propValue; }
            set
            {
                if (propValue != value)
                {
                    propValue = value;

                    _ = RootObject;
                    _ = Property;
                    if (Property.PropertyType.FullName.Contains("List`1") && (IsInList || (ArrayType != null && ArrayIndex.HasValue)))
                    {
                        if (PropertyChanged != null)
                        {
                            PropertyChanged.Invoke(this, new PropertyChangedEventArgs(value.ToString()));
                        }
                    }
                    else
                    {
                        try
                        {
                            if (Property.PropertyType.Name.Equals("AssetClassGuid"))
                                return;

                            if (Property.CanWrite && Property.SetMethod != null)
                            {
                                if (Property.GetValue(RootObject) == value)
                                    return;

                                Property.SetValue(RootObject, Convert.ChangeType(value, Property.PropertyType));
                                if (PropertyChanged != null)
                                {
                                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs(PropertyParentName != null ? PropertyParentName : PropertyName));
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex.ToString());
                        }

                    }
                }
            }
        }

        public PropertyInfo Property { get; set; }
        public override bool IsInList => ArrayType != null || Property.PropertyType.IsGenericType;

        public override bool IsReadOnly { get => Property != null && !Property.CanWrite; }

        public override string GetPropertyDescription()
        {
            try
            {

                if (EBXDescriptions.CachedDescriptions != null)
                {
                    foreach (var tDescription in EBXDescriptions.CachedDescriptions.Descriptions)
                    {
                        var indexOfPropertyDescription = tDescription
                            .Properties
                            .FindIndex(x => x.PropertyName.Equals(Property.Name, StringComparison.OrdinalIgnoreCase));
                        if (indexOfPropertyDescription != -1)
                            return tDescription.Properties[indexOfPropertyDescription].Description;
                    }
                }
            }
            catch
            {

            }
            return null;
        }

        public ModdableProperty(string n, string t, object v) : base(n, t, v)
        {
            PropertyName = n;
            PropertyType = t;
            propValue = v;
        }

        public ModdableProperty(object rootObject, PropertyInfo property, int? arrayIndex, PropertyChangedEventHandler modpropchanged = null, object vanillaRootObject = null) 
            : base(property.Name, property.PropertyType.Name, property.GetValue(rootObject))
        {
            RootObject = rootObject;
            VanillaRootObject = vanillaRootObject;
            Property = property;
            PropertyName = property.Name;
            PropertyType = property.PropertyType.FullName;
            PropertyValue = property.GetValue(rootObject, BindingFlags.GetProperty, null, null, null);

            PropertyOriginalValue = "";

            if (vanillaRootObject != null)
                PropertyOriginalValue = property.GetValue(vanillaRootObject, BindingFlags.GetProperty, null, null, null);
            else
                PropertyOriginalValue = property.GetValue(rootObject, BindingFlags.GetProperty, null, null, null);

            if (property.PropertyType.FullName.Contains("List`1"))
            {
                ArrayType = property.PropertyType.GetGenericArguments()[0];
                ArrayIndex = arrayIndex;
                if (ArrayIndex.HasValue)
                {
                    PropertyType = ArrayType.FullName;
                    PropertyName = ArrayIndex.Value.ToString();
                    PropertyValue = ((IList)property.GetValue(rootObject))[ArrayIndex.Value];

                    if (vanillaRootObject == null)
                        return;

                    var vanillaList = ((IList)property.GetValue(vanillaRootObject));
                    if (vanillaList == null)
                        return;

                    if (ArrayIndex.Value > vanillaList.Count - 1)
                        return;

                    PropertyOriginalValue = vanillaList[ArrayIndex.Value];
                }

            }
            if (modpropchanged != null)
                PropertyChanged += modpropchanged;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public static IEnumerable<ModdableProperty> GetModdableProperties(object obj, PropertyChangedEventHandler modpropchanged = null, object vanillaObj = null)
        {
            if (obj == null)
            {
                yield break;
            }

            var objType = obj.GetType();
            var objProperties = objType.GetProperties();
            foreach (var p in objProperties)
            {
                ModdableProperty moddableProperty = null;
                //var subObj = p.GetValue(obj, BindingFlags.Public | BindingFlags.Instance, null, null, null);
                try
                {
                    var subObj = p.GetValue(obj);
                    if (subObj != null)
                        moddableProperty = new ModdableProperty(obj, p, null, modpropchanged, vanillaObj);
                }
                catch { }

                if(moddableProperty != null)
                    yield return moddableProperty;
                //if(p.CanWrite && p.SetMethod != null)
                //    yield return new ModdableProperty(obj, p, null, modpropchanged, vanillaObj);
                //else
                //{
                //    if(p.PropertyType.GetProperties().Any(x=>x.CanWrite && x.SetMethod != null))
                //    {
                //        yield return new ModdableProperty(obj, p, null, modpropchanged, vanillaObj);
                //    }

                //    var subObj = p.GetValue(obj);
                //    if(GetModdableProperties(subObj, modpropchanged, subObj).Any())
                //        yield return new ModdableProperty(obj, p, null, modpropchanged, vanillaObj);
                //}

            }

        }
    }


}
