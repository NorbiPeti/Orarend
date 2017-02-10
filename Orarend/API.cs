using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
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
        /// <para>Lehet null, ha még nem volt sikeres <see cref="Frissítés"/>.</para>
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<string[]> Osztályok { get; set; }
        private static Órarend aktuálisÓrarend;
        public static async Task Frissítés()
        {
            aktuálisÓrarend = new Órarend { OsztályID = "12.b|2" }; //TODO: TMP
            HtmlDocument doc = new HtmlDocument();
            var req = WebRequest.CreateHttp("http://deri.enaplo.net/ajax/orarend/orarendoszt.php" + (aktuálisÓrarend == null ? "" : "?p=" + Uri.EscapeDataString(aktuálisÓrarend.OsztályID)));
            var resp = await req.GetResponseAsync();
            await Task.Run(() =>
            {
                using (var sr = new StreamReader(resp.GetResponseStream()))
                    doc.LoadHtml(Regex.Replace(Regex.Replace(sr.ReadToEnd(), "<th([^>]*)>((?:\\w|[áéóüöőúű.])+)(?=<)(?!\\/)", "<th$1>$2</th><"), "(?<!\\/tr\\>)\\<tr\\>", "</tr><tr>")); //TODO
                Osztályok = doc.GetElementbyId("uok").ChildNodes.Where(node => node.HasAttributes).Select(node => new string[] { node.GetAttributeValue("value", ""), node.NextSibling.InnerText });
                if (aktuálisÓrarend != null)
                {
                    bool ahét = true;
                    foreach (var node in doc.GetElementbyId("oda").FirstChild.FirstChild.ChildNodes[1].ChildNodes)
                    {
                        switch (node.FirstChild.InnerText)
                        {
                            case "A":
                                ahét = true;
                                break;
                            case "B":
                                ahét = false;
                                break;
                            default:
                                {
                                    int x = int.Parse(node.FirstChild.InnerText) - 1;
                                    aktuálisÓrarend.Órakezdetek[x] = TimeSpan.Parse(node.FirstChild.Attributes["title"].Value.Split('-')[0].Trim());
                                    for (int i = 0; i < 5; i++) //Napok
                                    { //TODO: for ciklus az egy időben tartott órákhoz
                                        var óranode = node.ChildNodes[i + 1].FirstChild;
                                        var csoportok = óranode.FirstChild.InnerText.TrimEnd(':');
                                        var óra = (ahét ? aktuálisÓrarend.ÓrákAHét : aktuálisÓrarend.ÓrákBHét)[i, x];
                                        if (óra == null)
                                            óra = new Óra();
                                        óra.Sorszám = x + 1;
                                        óra.Csoportok = csoportok;
                                        óra.Név = óranode.ChildNodes[2].Attributes["title"].Value;
                                        óra.Azonosító = óranode.ChildNodes[2].InnerText;
                                        óra.Terem = óranode.ChildNodes[3].InnerText.Trim(' ', '(', ')');
                                        óra.Tanár = new Tanár
                                        {
                                            Azonosító = óranode.ChildNodes[4].InnerText,
                                            Név = óranode.ChildNodes[4].Attributes["title"].Value
                                        };
                                    }
                                    break;
                                }
                        }
                    }
                }
            });
        }
    }
}
