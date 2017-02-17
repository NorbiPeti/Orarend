using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orarend
{
    public class Helyettesítés
    {
        public byte EredetiNap { get; set; }
        public byte EredetiSorszám { get; set; }
        public Óra EredetiÓra { get; set; }
        public byte ÚjNap { get; set; }
        public byte ÚjSorszám { get; set; }
        public Óra ÚjÓra { get; set; }
    }
}
