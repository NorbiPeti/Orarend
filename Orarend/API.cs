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
        /// <para>Visszatér az osztályok listájával, egy-egy kételemű tömbbel, az első elem az azonosító, a második a megjelenített név.</para>
        /// <para>Lehet null, ha még nem volt sikeres <see cref="Frissítés"/>.</para>
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<string[]> Osztályok { get; private set; }
        public static Órarend AktuálisÓrarend { get; private set; }
        public static async Task Frissítés()
        {
            AktuálisÓrarend = new Órarend { OsztályID = "12.b|2" }; //TODO: TMP
            HtmlDocument doc = new HtmlDocument();
            var req = WebRequest.CreateHttp("http://deri.enaplo.net/ajax/orarend/orarendoszt.php" + (AktuálisÓrarend == null ? "" : "?p=" + Uri.EscapeDataString(AktuálisÓrarend.OsztályID)));
            var resp = await req.GetResponseAsync();
            await Task.Run(() =>
            {
                using (var sr = new StreamReader(resp.GetResponseStream()))
                {
                    const string trtd = @"(?:\s\w+=(?:\""|\')?(?:\w|[áéóüöőúű.:;])+(?:\""|\')?)*>(?!.+?\<table(?:\s\w+?=\""?\w+\""?)*\>.+?)(.+?)(?=<\1(?:\s\w+=(?:\""|\')?(?:\w|[áéóüöőúű.:;])+(?:\""|\')?)*>)";
                    string html = Regex.Replace(Regex.Replace(Regex.Replace(sr.ReadToEnd(), "<th([^>]*)>((?:\\w|[áéóüöőúű.])+)(?=<)(?!\\/)", "<th$1>$2</th>"), "<(tr)" + trtd, "<$1>$2</$1>"), "<(td)" + trtd, "<$1>$2</$1>");
                    doc.LoadHtml(html);
                }
                Osztályok = doc.GetElementbyId("uok").ChildNodes.Where(node => node.HasAttributes).Select(node => new string[] { node.GetAttributeValue("value", ""), node.NextSibling.InnerText });
                if (AktuálisÓrarend != null)
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
                                    AktuálisÓrarend.Órakezdetek[x] = TimeSpan.Parse(node.FirstChild.Attributes["title"].Value.Split('-')[0].Trim());
                                    for (int i = 0; i < 5; i++) //Napok
                                    { //TODO: for ciklus az egy időben tartott órákhoz
                                        var óranode = node.ChildNodes[i + 1].FirstChild;
                                        var óra = (ahét ? AktuálisÓrarend.ÓrákAHét : AktuálisÓrarend.ÓrákBHét)[i, x];
                                        if (óranode.ChildNodes.Count == 0)
                                            continue;
                                        if (óra == null)
                                            (ahét ? AktuálisÓrarend.ÓrákAHét : AktuálisÓrarend.ÓrákBHét)[i, x] = óra = new Óra();
                                        var csoportok = óranode.FirstChild.InnerText.TrimEnd(':');
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
