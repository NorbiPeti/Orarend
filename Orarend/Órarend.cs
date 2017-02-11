using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orarend
{
    public class Órarend
    {
        /// <summary>
        /// Egy 6x16 2D tömb, az első koordináta a nap indexe, a második az óráé. Az értékek lehetnek null-ok, ha nincs óra az adott időpontban
        /// </summary>
        public Óra[,] ÓrákAHét { get; } = new Óra[6, 16];
        /// <summary>
        /// Egy 6x16 2D tömb, az első koordináta a nap indexe, a második az óráé. Az értékek lehetnek null-ok, ha nincs óra az adott időpontban
        /// </summary>
        public Óra[,] ÓrákBHét { get; } = new Óra[6, 16];
        public string Név { get; set; }
        public string OsztályID { get; set; }
        public string OsztályNév { get; set; }
        /// <summary>
        /// Egy 16 elemű tömb az órák kezdő időpontjaival
        /// </summary>
        public TimeSpan[] Órakezdetek { get; } = new TimeSpan[16];
        public List<string> Csoportok { get; }
    }
}
