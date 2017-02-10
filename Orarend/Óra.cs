using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orarend
{
    public class Óra
    {
        public string Azonosító { get; set; }
        public string Név { get; set; }
        public Tanár Tanár { get; set; }
        public int Sorszám { get; set; }
        public string Terem { get; set; }
        /// <summary>
        /// Az órán résztvevő csoportok, pluszjelekkel elválasztva
        /// </summary>
        public string Csoportok { get; set; }
    }
}
