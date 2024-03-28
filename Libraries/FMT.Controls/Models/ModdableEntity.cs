using FMT.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace FMT.Controls.Models
{
    public abstract class ModdableEntity
    {
        public string PropertyName { get; set; }
        private string displayName;

        public string DisplayName
        {
            get 
            {
                return !string.IsNullOrEmpty(displayName) ? displayName : PropertyName;
            }
            set { displayName = value; }
        }
                
        public string PropertyType { get; set; }
        public string PropertyParentName { get; set; }

        private object propValue;

        public object PropertyOriginalValue { get; protected set; }

        public virtual object PropertyValue
        {
            get { return propValue; }
            set
            {
                if (propValue != value)
                {
                    propValue = value;
                }
            }
        }
        public object RootObject { get; set; }
        public object VanillaRootObject { get; set; }
        public Type ArrayType { get; set; }
        public int? ArrayIndex { get; set; }
        public virtual bool IsInList => ArrayType != null;

        //public bool IsReadOnly => Property != null && !Property.CanWrite;

        public string PropertyDescription
        {
            get
            {
                return GetPropertyDescription();
            }
        }

        public virtual string GetPropertyDescription()
        {
            try
            {

                if (EBXDescriptions.CachedDescriptions != null)
                {
                    foreach (var tDescription in EBXDescriptions.CachedDescriptions.Descriptions)
                    {
                        var indexOfPropertyDescription = tDescription
                        .Properties
                            .FindIndex(x => x.PropertyName.Equals(PropertyName, StringComparison.OrdinalIgnoreCase));
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

        public virtual bool IsReadOnly { get; } = false;

        public bool IsEditable => !IsReadOnly;

        public bool HasPropertyDescription
        {
            get
            {
                return !string.IsNullOrEmpty(PropertyDescription);
            }
        }

        public Visibility HasPropertyDescriptionVisibility
        {
            get
            {
                return HasPropertyDescription ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public ModdableEntity(string n, string t, object v)
        {
            PropertyName = n;
            PropertyType = t;
            propValue = v;
        }

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(PropertyName))
            {
                return string.Format("{0} - {1}", PropertyName, PropertyType);
            }

            return base.ToString();
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        
    }
}
