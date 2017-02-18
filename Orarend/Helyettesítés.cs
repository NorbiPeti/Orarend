using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Orarend
{
    [DataContract]
    public class Helyettesítés
    {
        [DataMember]
        public byte EredetiNap { get; set; }
        [DataMember]
        public byte EredetiSorszám { get; set; }
        [DataMember]
        public Óra EredetiÓra { get; set; }
        [DataMember]
        public byte ÚjNap { get; set; }
        [DataMember]
        public byte ÚjSorszám { get; set; }
        [DataMember]
        public Óra ÚjÓra { get; set; }
    }
}
