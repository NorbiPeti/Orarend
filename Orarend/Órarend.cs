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
        /// <para>Egy 6x16 2D tömb, az első koordináta a nap indexe, a második az óráé. Az értékek lehetnek null-ok, ha nincs óra az adott időpontban</para>
        /// <para>Egy <see cref="API.Frissítés"/> hívás állítja be az órákat</para>
        /// </summary>
        public Óra[,] ÓrákAHét { get; } = new Óra[6, 16];
        /// <summary>
        /// <para>Egy 6x16 2D tömb, az első koordináta a nap indexe, a második az óráé. Az értékek lehetnek null-ok, ha nincs óra az adott időpontban</para>
        /// <para>Egy <see cref="API.Frissítés"/> hívás állítja be az órákat</para>
        /// </summary>
        public Óra[,] ÓrákBHét { get; } = new Óra[6, 16];
        /// <summary>
        /// <para>Egy <see cref="API.Frissítés"/> hívás állítja be</para>
        /// </summary>
        public string Név { get; set; }
        public Osztály Osztály { get; set; }
        /// <summary>
        /// Egy 16 elemű tömb az órák kezdő időpontjaival
        /// </summary>
        public TimeSpan[] Órakezdetek { get; } = new TimeSpan[16];
        public List<string> Csoportok { get; }

        /// <summary>
        /// Létrehoz egy új órarendet
        /// </summary>
        /// <param name="név">Az órarend neve. Ez jelenik meg a fejlécen</param>
        /// <param name="osztály">Az osztály, amihez tartozik ez az órarend. Lásd <see cref="API.Osztályok"/> </param>
        /// <param name="csoportok">A megjelenítendő csoportok szóközzel elválasztva</param>
        public Órarend(string név, Osztály osztály, string csoportok)
        {
            Név = név;
            Osztály = osztály;
            Csoportok = new List<string>(csoportok.Replace("Egész osztály", "").Trim().Split(' '));
        }
    }
}
