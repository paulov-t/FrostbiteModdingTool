using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace FrostySdk
{
    public class DbObject : IEquatable<DbObject>
    {

        public bool IsList => List != null;

        public bool IsObject => Dictionary != null;

        public IDictionary<string, object> hash { get; set; }

        public IDictionary<string, object> Hash => hash;

        public IDictionary<string, object> Dictionary => hash;

        public List<object> list { get; set; }

        public List<object> List => list;

        public bool SanitizeDataOnAdd = false;

        public int Count
        {
            get
            {
                if (!IsList)
                {
                    return Dictionary.Count;
                }
                return list.Count;
            }
        }

        public object this[string key]
        {
            get
            {
                if (Dictionary == null)
                    throw new ArgumentNullException("No dictionary to read");

                return Dictionary[key];
            }
        }

        public object this[int id]
        {
            get
            {
                if (id >= list.Count)
                {
                    return null;
                }
                return list[id];
            }
        }

        public DbObject(bool bObject = true)
        {
            if (bObject)
            {
                hash = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            }
            else
            {
                list = new List<object>();
            }
        }

        public DbObject(object inVal)
        {
            var vType = inVal.GetType();
            if (inVal is IList)
            {
                list = new List<object>();
                list.AddRange((IEnumerable<object>)inVal);
            }
            else if (inVal is IDictionary)
            {
                hash = (IDictionary<string, object>)inVal;
            }
        }

        public static DbObject CreateObject()
        {
            return new DbObject();
        }

        public static DbObject CreateList()
        {
            return new DbObject(bObject: false);
        }

        public Type GetValueType(string name)
        {
            if (hash == null || !hash.ContainsKey(name))
                return null;

            return hash[name].GetType();
        }

        public T GetValue<T>(string name, T defaultValue = default(T))
        {
            if (hash == null || !hash.ContainsKey(name))
            {
                return defaultValue;
            }
            if (hash[name] is T)
            {
                return (T)hash[name];
            }
            return (T)Convert.ChangeType(hash[name], typeof(T));
        }

        public void SetValue(string name, object newValue)
        {
            if (newValue != null)
            {
                if (!hash.ContainsKey(name))
                {
                    AddValue(name, newValue);
                }
                else
                {
                    hash[name] = newValue;
                }
            }
        }

        //public void AddValue(string name, object value)
        //{
        //	var containsKey = hash.ContainsKey(name);
        //	var valueIsNotNull = value != null;
        //	if (!containsKey && valueIsNotNull)
        //	{
        //		hash.Add(name, SanitizeData(value));
        //	}
        //	else if(!valueIsNotNull)
        //          {
        //		Debug.WriteLine("[ERROR] DbObject::AddValue - Value was null");
        //          }
        //}

        public void AddValue(string name, object value, bool Sanitize = true)
        {
            var containsKey = hash.ContainsKey(name);
            var valueIsNotNull = value != null;
            if (!containsKey && valueIsNotNull)
            {
                hash.Add(name, Sanitize ? SanitizeData(value) : value);
            }
            else if (!valueIsNotNull)
            {
                throw new InvalidOperationException("DbObject::AddValue - Value was null");
                //Debug.WriteLine("[ERROR] DbObject::AddValue - Value was null");
            }
        }

        public void RemoveValue(string name)
        {
            if (hash.ContainsKey(name))
            {
                hash.Remove(name);
            }
        }

        public bool HasValue(string name)
        {
            return hash.ContainsKey(name);
        }

        public bool HasValue(ReadOnlySpan<byte> name)
        {
            return hash.ContainsKey(Encoding.UTF8.GetString(name));
        }

        public IEnumerator GetEnumerator()
        {
            int count = (list != null) ? list.Count : 0;
            for (int i = 0; i < count; i++)
            {
                yield return list[i];
            }
        }

        public DbObject Add(object value)
        {
            if (list == null)
            {
                list = new List<object>();
            }
            if (list != null)
            {
                bool handled = false;
                if (value is DbObject)
                {
                    DbObject other = value as DbObject;
                    if (other.Dictionary == null && other.IsList && other.List.Count > 0)
                    {
                        for (var i = 0; i < other.List.Count; i++)
                        {
                            list.Add(other.List[i]);
                        }
                        handled = true;
                    }
                }
                if (!handled)
                    list.Add(SanitizeData(value));
            }
            return this;
        }

        public void SetAt(int id, object value)
        {
            if (list != null && id < list.Count)
            {
                list[id] = SanitizeData(value);
            }
        }

        public void Insert(int id, object value)
        {
            list.Insert(id, value);
        }

        public void RemoveAt(int id)
        {
            if (list != null && id < list.Count)
            {
                list.RemoveAt(id);
            }
        }

        public int FindIndex(Predicate<object> match)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (match(list[i]))
                {
                    return i;
                }
            }
            return -1;
        }

        public T Find<T>(Predicate<object> match)
        {
            if (list == null)
            {
                return default(T);
            }
            return (T)list.Find(match);
        }

        private object SanitizeData(object value)
        {
            if (SanitizeDataOnAdd)
            {
                if (value.GetType() == typeof(uint))
                {
                    value = (int)(uint)value;
                }
                else if (value.GetType() == typeof(ulong))
                {
                    value = (long)(ulong)value;
                }
            }
            return value;
        }

        public static DbObject FromObject(object o)
        {
            DbObject dbObject = new DbObject();

            Type myType = o.GetType();
            IList<PropertyInfo> props = new List<PropertyInfo>(myType.GetProperties());

            foreach (PropertyInfo prop in props)
            {
                object propValue = prop.GetValue(o, null);
                dbObject.SetValue(prop.Name, propValue);
            }

            return dbObject;
        }

        public T ToObject<T>()
        {
            var newOb = default(T);
            Type myType = newOb.GetType();
            IList<PropertyInfo> props = new List<PropertyInfo>(myType.GetProperties());

            foreach (var kvp in Dictionary)
            {
                var p = props.FirstOrDefault(x => x.Name == kvp.Key);
                if (p != null)
                {
                    p.SetValue(p, kvp.Value);
                }
            }

            return newOb;
        }

        public override string ToString()
        {
            if (IsObject)
            {
                if (Dictionary.ContainsKey("name"))
                    return Dictionary["name"].ToString();

                if (Dictionary.ContainsKey("Name"))
                    return Dictionary["Name"].ToString();

                if (Dictionary.ContainsKey("Id"))
                    return Dictionary["Id"].ToString();

                if (Dictionary.ContainsKey("id"))
                    return Dictionary["id"].ToString();
            }
            return base.ToString();
        }

        public bool Equals(DbObject other)
        {
            if (other.HasValue("ebx"))
            {
                if (this.HasValue("ebx"))
                {
                    var ebxValues = this.GetValue<DbObject>("ebx");
                }
            }

            return this == other || this.ToString() == other.ToString();
        }
    }
}
