﻿using HtmlAgilityPack;
using Java.Lang;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Json;
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
        public static Settings Beállítások { get; private set; } = new Settings();
        public static List<Helyettesítés> Helyettesítések { get; } = new List<Helyettesítés>();
        /// <summary>
        /// Frissíti az osztálylistát és az eredeti órarendet, első megnyitásnál, és egy órarend hozzáadásánál/szerkesztésénél, majd hetente elegendő meghívni
        /// <param name="s">A file stream, ahova mentse az adatokat, hogy ne kelljen külön meghívni</param>
        /// </summary>
        public static async Task Frissítés(Stream s)
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
                        Osztályok = doc.GetElementbyId("uok").ChildNodes.Where(node => node.HasAttributes).Select(node => new Osztály { Azonosító = node.GetAttributeValue("value", ""), Név = node.NextSibling.InnerText }).ToArray();
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
                        ÓrarendMentés(s);
                        OsztályMentés(s);
                        Thread.Sleep(10);
                    });
            }
        }

        /// <summary>
        /// Frissíti a helyettesítéseket, naponta, indításkor vagy gombnyommásra frissítse (minden nap az első előtérbe kerüléskor)
        /// <param name="s">A file stream, ahova mentse az adatokat, hogy ne kelljen külön meghívni</param>
        /// </summary>
        public static async Task HelyettesítésFrissítés(Stream s)
        {
            HtmlDocument doc = new HtmlDocument();
            var req = WebRequest.CreateHttp("http://deri.enaplo.net/ajax/print/htlista.php");
            var resp = await req.GetResponseAsync();
            await Task.Run(() =>
            {
                using (var sr = new StreamReader(resp.GetResponseStream()))
                    doc.LoadHtml(sr.ReadToEnd());
            }); //TODO
        }

        public static void ÓrarendBetöltés(Stream s)
        {
            using (s)
            {
                var serializer = new DataContractJsonSerializer(typeof(Órarend[]));
                Órarendek.AddRange((Órarend[])serializer.ReadObject(s));
            }
        }

        public static void OsztályBetöltés(Stream s)
        {
            using (s)
            {
                var serializer = new DataContractJsonSerializer(typeof(Osztály[]));
                Osztályok = ((Osztály[])serializer.ReadObject(s));
            }
        }

        public static void BeállításBetöltés(Stream s)
        {
            using (s)
            {
                var serializer = new DataContractJsonSerializer(typeof(Settings));
                Beállítások = ((Settings)serializer.ReadObject(s));
            }
        }

        public static void HelyettesítésBetöltés(Stream s)
        {
            using (s)
            {
                var serializer = new DataContractJsonSerializer(typeof(Helyettesítés[])); //TODO: Tényleges órarendből állapítsa meg azt is, hogyha egyáltalán nincs ott egy óra, és máshol sincs, és ezt írja ki
                Helyettesítések.AddRange((Helyettesítés[])serializer.ReadObject(s));
            }
        }

        private static void ÓrarendMentés(Stream s)
        {
            using (s)
            {
                var serializer = new DataContractJsonSerializer(typeof(Órarend[]));
                serializer.WriteObject(s, Órarendek);
            }
        }

        private static void OsztályMentés(Stream s)
        {
            using (s)
            {
                var serializer = new DataContractJsonSerializer(typeof(Osztály[]));
                serializer.WriteObject(s, Osztályok);
            }
        }

        public static void BeállításMentés(Stream s)
        {
            using (s)
            {
                var serializer = new DataContractJsonSerializer(typeof(Settings));
                serializer.WriteObject(s, Beállítások);
            }
        }

        private static void HelyettesítésMentés(Stream s)
        {
            using (s)
            {
                var serializer = new DataContractJsonSerializer(typeof(Helyettesítés[]));
                serializer.WriteObject(s, Helyettesítések);
            }
        }
    }
}
