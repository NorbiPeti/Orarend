using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Orarend
{
    [DataContract]
    public class Osztály : IEquatable<Osztály>
    {
        [DataMember]
        public string Azonosító { get; internal set; }
        [DataMember]
        public string Név { get; internal set; }
        internal Osztály()
        {
        }

        public override string ToString()
        {
            return Név;
        }

        public bool Equals(Osztály other)
        {
            return Azonosító == other.Azonosító;
        }

        public static bool operator==(Osztály a, Osztály b)
        {
            return a.Equals(b);
        }

        public static bool operator!=(Osztály a, Osztály b)
        {
            return !a.Equals(b);
        }

        public override bool Equals(object obj)
        {
            return obj is Osztály ? Equals(obj as Osztály) : base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return Azonosító.GetHashCode();
        }
    }
}
