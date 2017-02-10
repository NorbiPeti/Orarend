using HtmlAgilityPack;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        public static IEnumerable<Óra> Órák(string osztályid, string csoportok, char nap)
        {
            return new Óra[] { new Óra { Azonosító = "test", Név = "Test", Tanár = new Tanár { Név = "A B" }, Terem = "222" }, new Óra { Azonosító = "asd", Név = "Asd", Tanár = new Tanár { Név = "B A" }, Terem = "216" } };
        }

        /// <summary>
        /// <para>Visszatér az osztályok listájával, egy-egy kételemű tömbbel, az első elem az azonosító, a második a megjelenített név.</para>
        /// </summary>
        /// <returns></returns>
        public static async Task<IEnumerable<string[]>> Osztályok()
        { //TODO: Tárolja el az adatokat, és csak külön hívásra frissítse; csak a frissítés legyen async, ezek nem
            HtmlDocument doc = new HtmlDocument();
            var req = WebRequest.CreateHttp("http://deri.enaplo.net/ajax/orarend/orarendoszt.php");
            var resp = await req.GetResponseAsync();
            doc.Load(resp.GetResponseStream());
            return doc.GetElementbyId("uok").ChildNodes.Where(node => node.HasAttributes).Select(node => new string[] { node.GetAttributeValue("value", ""), node.InnerText });
        }
    }
}
