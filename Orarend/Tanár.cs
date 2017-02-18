using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Orarend
{
    [DataContract]
    public struct Tanár
    {
        [DataMember]
        public string Azonosító { get; set; }
        [DataMember]
        public string Név { get; set; }
    }
}
