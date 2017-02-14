using HtmlAgilityPack;
using Java.Lang;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Orarend
{
    public static class API
    { //TODO: Beállítások: Téma (sötét, világos) (Android; platformfüggő beállítások), Előre megadott egyedi nevek használata
        /// <summary>
        /// <para>Visszatér az osztályok listájával.</para>
        /// <para>Lehet null, ha még nem volt sikeres <see cref="Frissítés"/>.</para>
        /// </summary>
        /// <returns></returns>
        public static Osztály[] Osztályok { get; private set; }
        public static List<Órarend> Órarendek { get; } = new List<Órarend>();
        /// <summary>
        /// Frissíti az osztálylistát és az eredeti órarendet, első megnyitásnál, és egy órarend hozzáadásánál/szerkesztésénél, majd hetente elegendő meghívni
        /// </summary>
        public static async Task Frissítés()
        {
            Func<string, Task<HtmlDocument>> load = async (url) =>
            {
                HtmlDocument doc = new HtmlDocument();
                var req = WebRequest.CreateHttp(url);
                var resp = await req.GetResponseAsync();
                await Task.Run(() =>
                {
                    using (var sr = new StreamReader(resp.GetResponseStream()))
                    {
                        const string trtd = @"(?:\s\w+=(?:\""|\')?(?:\w|[áéóüöőúű.:;])+(?:\""|\')?)*>(?!.+?\<table(?:\s\w+?=\""?\w+\""?)*\>.+?)(.+?)(?=<\1(?:\s\w+=(?:\""|\')?(?:\w|[áéóüöőúű.:;])+(?:\""|\')?)*>)";
                        string html = Regex.Replace(Regex.Replace(Regex.Replace(sr.ReadToEnd(), "<th([^>]*)>((?:\\w|[áéóüöőúű.])+)(?=<)(?!\\/)", "<th$1>$2</th>"), "<(tr)" + trtd, "<$1>$2</$1>"), "<(td)" + trtd, "<$1>$2</$1>");
                        doc.LoadHtml(html);
                    }
                });
                return doc;
            };
            if (Órarendek.Count == 0)
            {
                var doc = await load("http://deri.enaplo.net/ajax/orarend/orarendoszt.php");
                await Task.Run(() => Osztályok = doc.GetElementbyId("uok").ChildNodes.Where(node => node.HasAttributes).Select(node => new Osztály { Azonosító = node.GetAttributeValue("value", ""), Név = node.NextSibling.InnerText }).ToArray());
            }
            foreach (var órarend in Órarendek)
            {
                var doc = await load("http://deri.enaplo.net/ajax/orarend/orarendoszt.php?p=" + Uri.EscapeDataString(órarend.Osztály.Azonosító));
                await Task.Run(() =>
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
                                        órarend.Órakezdetek[x] = TimeSpan.Parse(node.FirstChild.Attributes["title"].Value.Split('-')[0].Trim());
                                        for (int i = 0; i < 5; i++) //Napok
                                            { //TODO: for ciklus az egy időben tartott órákhoz
                                                var óranode = node.ChildNodes[i + 1].FirstChild;
                                            var óra = (ahét ? órarend.ÓrákAHét : órarend.ÓrákBHét)[i, x];
                                            if (óranode.ChildNodes.Count == 0)
                                                continue;
                                            var csoport = óranode.FirstChild.InnerText.TrimEnd(':');
                                            if (csoport != "Egész osztály" && !órarend.Csoportok.Contains(csoport))
                                                continue;
                                            if (óra == null)
                                                (ahét ? órarend.ÓrákAHét : órarend.ÓrákBHét)[i, x] = óra = new Óra();
                                            óra.Sorszám = x + 1;
                                            óra.Csoportok = new string[] { csoport }; //Az állandó órarendben osztályonként csak egy csoport van egy órán
                                            óra.Azonosító = óranode.ChildNodes[2].InnerText;
                                            óra.TeljesNév = óranode.ChildNodes[2].Attributes["title"].Value;
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
                    });
                Thread.Sleep(10);
            }
        }

        /// <summary>
        /// Frissíti a helyettesítéseket, naponta, indításkor vagy gombnyommásra frissítse (minden nap az első előtérbe kerüléskor)
        /// </summary>
        public static async Task HelyettesítésFrissítés()
        {
            //TODO
        }
    }
}
