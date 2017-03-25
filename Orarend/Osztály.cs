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

        public override string ToString() => Név;

        public bool Equals(Osztály other) => Azonosító == other?.Azonosító;
        public static bool operator==(Osztály a, Osztály b) => a?.Equals(b) ?? (object)b == null;
        public static bool operator!=(Osztály a, Osztály b) => !(a == b);
        public override bool Equals(object obj) => obj is Osztály ? Equals(obj as Osztály) : base.Equals(obj);
        public override int GetHashCode() => Azonosító.GetHashCode();
    }
}
