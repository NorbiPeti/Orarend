using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace Orarend
{
    public static class API
    {
        /// <summary>
        /// Visszatér az adott nap óráival.
        /// </summary>
        /// <param name="osztályid">Az osztály azonosítója, ld. <see cref="Osztályok"/></param>
        /// <param name="csoportok">A csoportok, amiknek az óráit kérjük, szóközökkel elválasztva</param>
        /// <param name="nap">A nap egy betűs formában</param>
        /// <returns></returns>
        public static IEnumerable<Óra> Órák(string osztályid, string csoportok, string nap)
        {
            return new Óra[] { new Óra { Azonosító = "test", Név = "Test", Tanár = new Tanár { Név = "A B" }, Terem = "222" }, new Óra { Azonosító = "asd", Név = "Asd", Tanár = new Tanár { Név = "B A" }, Terem = "216" } };
        }

        public static async Task<IEnumerable<string>> Osztályok()
        { //TODO: Tárolja el az adatokat, és csak külön hívásra frissítse; csak a frissítés legyen async, ezek nem
            var req = WebRequest.CreateHttp("http://deri.enaplo.net/ajax/orarend/orarendoszt.php");
            var resp = await req.GetResponseAsync();
            var doc = XDocument.Parse(new StreamReader(resp.GetResponseStream()).ReadToEnd());
            return new string[] { doc.Element(XName.Get("select")).Value };
        }
    }
}
