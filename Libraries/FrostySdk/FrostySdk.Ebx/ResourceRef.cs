using System;

namespace FrostySdk.Ebx
{
    public struct ResourceRef
    {
        public static ResourceRef Zero = new ResourceRef(0uL);

        public ulong ResourceId { get; set; }

        public ResourceRef(ulong value)
        {
            ResourceId = value;
        }

        public static implicit operator ulong(ResourceRef value)
        {
            return value.ResourceId;
        }

        public static implicit operator ResourceRef(ulong value)
        {
            return new ResourceRef(value);
        }

        public override bool Equals(object obj)
        {
            if (obj is ResourceRef)
            {
                ResourceRef resourceRef = (ResourceRef)obj;
                return ResourceId == resourceRef.ResourceId;
            }
            if (obj is ulong)
            {
                ulong num = (ulong)obj;
                return ResourceId == num;
            }
            return false;
        }

        public static bool operator ==(ResourceRef a, ResourceRef b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(ResourceRef a, ResourceRef b)
        {
            return !a.Equals(b);
        }

        public override int GetHashCode()
        {
            return Convert.ToInt32(-2128831035L * 16777619) ^ ResourceId.GetHashCode();
        }

        public override string ToString()
        {
            return ResourceId.ToString("X16");
        }
    }
}
