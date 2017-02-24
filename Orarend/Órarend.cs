using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Orarend
{
    [DataContract]
    public class Órarend
    {
        [DataMember]
        internal Óra[][] ÓrákAHét { get; private set; } = new Óra[6][] { new Óra[16], new Óra[16], new Óra[16], new Óra[16], new Óra[16], new Óra[16] };
        [DataMember]
        internal Óra[][] ÓrákBHét { get; private set; } = new Óra[6][] { new Óra[16], new Óra[16], new Óra[16], new Óra[16], new Óra[16], new Óra[16] }; //Multidimensional arrays are not supported (serialization)
        /// <summary>
        /// <para>Egy 6x16 2D tömb, az első koordináta a nap indexe, a második az óráé. Az értékek lehetnek null-ok, ha nincs óra az adott időpontban</para>
        /// <para>Egy <see cref="API.Frissítés"/> hívás állítja be az órákat</para>
        /// </summary>
        public Óra[][] Órák
        {
            get
            {
                return API.AHét ? ÓrákAHét : ÓrákBHét;
            }
        }
        /// <summary>
        /// <para>Egy <see cref="API.Frissítés"/> hívás állítja be</para>
        /// </summary>
        [DataMember]
        public string Név { get; set; }
        [DataMember]
        public Osztály Osztály { get; set; }
        /// <summary>
        /// Egy 16 elemű tömb az órák kezdő időpontjaival
        /// </summary>
        [DataMember]
        public TimeSpan[] Órakezdetek { get; private set; } = new TimeSpan[16]; //A private set kell a serialization miatt
        [DataMember]
        public string[] Csoportok { get; set; }
        [DataMember]
        public List<Helyettesítés> Helyettesítések { get; private set; } = new List<Helyettesítés>();

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
            Csoportok = csoportok.Replace("Egész osztály", "").Trim().Split(' ');
        }

        public override string ToString()
        {
            return Név;
        }
    }
}
