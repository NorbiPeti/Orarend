using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Orarend
{
    [DataContract]
    public class Osztály
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
    }
}
