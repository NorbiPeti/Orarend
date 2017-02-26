﻿using HtmlAgilityPack;
using Java.Lang;
using System;
using System.Collections.Generic;
using System.Globalization;
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
        /// <summary>
        /// Frissíti az osztálylistát és az eredeti órarendet, első megnyitásnál, és egy órarend hozzáadásánál/szerkesztésénél, majd hetente elegendő meghívni
        /// <param name="órarendstream">A file stream, ahova mentse az adatokat, hogy ne kelljen külön meghívni</param>
        /// <param name="osztálystream">A file stream, ahova mentse az adatokat, hogy ne kelljen külön meghívni</param>
        /// </summary>
        public static async Task Frissítés(Stream órarendstream, Stream osztálystream, Órarend ór = null)
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
            Func<Órarend, Task> órarenda = async órarend =>
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
                                          {
                                              var óranode = node.ChildNodes[i + 1].FirstChild;
                                              var óra = (ahét ? órarend.ÓrákAHét : órarend.ÓrákBHét)[i][x];
                                              if (óranode.ChildNodes.Count == 0)
                                                  continue;
                                              for (int j = 0; j < óranode.ChildNodes.Count; j += 6)
                                              {
                                                  var csoport = óranode.ChildNodes[j].InnerText.TrimEnd(':');
                                                  if (csoport != "Egész osztály" && !órarend.Csoportok.Contains(csoport))
                                                      continue;
                                                  if (óra == null)
                                                      (ahét ? órarend.ÓrákAHét : órarend.ÓrákBHét)[i][x] = óra = new Óra();
                                                  óra.Csoportok = new string[] { csoport }; //Az állandó órarendben osztályonként csak egy csoport van egy órán
                                                  óra.Azonosító = óranode.ChildNodes[j + 2].InnerText;
                                                  óra.TeljesNév = óranode.ChildNodes[j + 2].Attributes["title"].Value;
                                                  óra.Terem = óranode.ChildNodes[j + 3].InnerText.Trim(' ', '(', ')');
                                                  óra.Tanár = new Tanár
                                                  {
                                                      Azonosító = óranode.ChildNodes[j + 4].InnerText,
                                                      Név = óranode.ChildNodes[j + 4].Attributes["title"].Value
                                                  };
                                                  break;
                                              }
                                          }
                                          break;
                                      }
                              }
                          }
                          Thread.Sleep(10);
                      });
              };
            if (ór == null)
                foreach (var órarend in Órarendek)
                    await órarenda(órarend);
            else
                await órarenda(ór);
            ÓrarendMentés(órarendstream);
            OsztályMentés(osztálystream);
        }

        /// <summary>
        /// Frissíti a helyettesítéseket, naponta, indításkor vagy gombnyommásra frissítse (minden nap az első előtérbe kerüléskor)
        /// <param name="s">A file stream, ahova mentse az ÓRARENDEKET, hogy ne kelljen külön meghívni</param>
        /// </summary>
        public static async Task HelyettesítésFrissítés(Stream s)
        {
            if (Órarendek.Count == 0 || Osztályok.Length == 0)
                return;
            foreach (var órarend in Órarendek)
                órarend.Helyettesítések.Clear();
            HtmlDocument doc = new HtmlDocument();
            var req = WebRequest.CreateHttp("http://deri.enaplo.net/ajax/print/htlista.php");
            var resp = await req.GetResponseAsync();
            await Task.Run(() =>
            {
                using (var sr = new StreamReader(resp.GetResponseStream()))
                    doc.LoadHtml(sr.ReadToEnd());
                foreach (var node in doc.DocumentNode.ChildNodes[2].ChildNodes[1].ChildNodes)
                {
                    DateTime dátum = DateTime.Parse(node.ChildNodes[0].InnerText.Substring(0, node.ChildNodes[0].InnerText.Length - 4));
                    int hét = CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(dátum, CalendarWeekRule.FirstFullWeek, DayOfWeek.Monday);
                    if (hét != Hét)
                        continue;
                    byte óraszám = byte.Parse(node.ChildNodes[1].InnerText);
                    Osztály osztály = Osztályok.Single(o => o.Azonosító.Contains(node.ChildNodes[2].InnerText));
                    string csoport = node.ChildNodes[3].InnerText;
                    string óraaz = node.ChildNodes[4].InnerText;
                    string terem = node.ChildNodes[5].InnerText.Split(new string[] { " -> " }, StringSplitOptions.None).Last(); //Mindig az új termet tárolja el, ha változott
                    string tanár = node.ChildNodes[7].InnerText;
                    string[] megj = node.ChildNodes[8].InnerText.Split(' ');
                    string óranév = node.ChildNodes[9].InnerText;
                    DayOfWeek újnap = dátum.DayOfWeek;
                    byte újsorszám = óraszám;
                    if (megj.Length > 2)
                    {
                        újnap = DateTime.Parse(megj[1]).DayOfWeek;
                        újsorszám = byte.Parse(megj[3].Trim('.'));
                    }
                    foreach (var órarend in (csoport == "Egész osztály" ? Órarendek : Órarendek.Where(ór => ór.Csoportok.Contains(csoport))).Where(ór => ór.Osztály == osztály))
                    //foreach (var órarend in Órarendek.Where(ór => ór.Osztály == osztály && (csoport == "Egész osztály" || ór.Csoportok.Contains(csoport)))) - A probléma valószínűleg a referencia változások miatt volt, a serialization miatt, és hogy alapból nem a .Equals-ot futtatja le ==-kor
                    {
                        var helyettesítés = new Helyettesítés { EredetiNap = dátum.DayOfWeek, EredetiSorszám = óraszám, ÚjÓra = tanár == "elmarad" ? null : new Óra { Azonosító = óraaz, Csoportok = new string[] { csoport }, Terem = terem, Tanár = new Tanár { Név = tanár }, TeljesNév = óranév }, ÚjNap = újnap, ÚjSorszám = újsorszám };
                        órarend.Helyettesítések.Add(helyettesítés);
                    }
                }
                ÓrarendMentés(s);
            });
        }

        private static T betöltés<T>(Stream s)
        {
            using (s)
            {
                using (var ms = new MemoryStream())
                {
                    s.CopyTo(ms);
                    if (ms.Length > 2)
                    {
                        try
                        {
                            ms.Seek(0, SeekOrigin.Begin);
                            var serializer = new DataContractJsonSerializer(typeof(T));
                            return (T)serializer.ReadObject(ms);
                        }
                        catch
                        {
                            return default(T);
                        }
                    }
                    return default(T);
                }
            }
        }

        public static void ÓrarendBetöltés(Stream s)
        {
            Órarendek.AddRange(betöltés<Órarend[]>(s) ?? new Órarend[0]);
        }

        public static void OsztályBetöltés(Stream s)
        {
            Osztályok = betöltés<Osztály[]>(s) ?? new Osztály[0];
        }

        public static void BeállításBetöltés(Stream s)
        {
            Beállítások = betöltés<Settings>(s);
        } //TODO: Tényleges órarendből állapítsa meg azt is, hogyha egyáltalán nincs ott egy óra, és máshol sincs, és ezt írja ki

        private static void mentés<T>(Stream s, T obj)
        {
            using (s)
            {
                if (obj != null)
                {
                    var serializer = new DataContractJsonSerializer(typeof(T));
                    serializer.WriteObject(s, obj);
                }
            }
        }

        public static void ÓrarendMentés(Stream s)
        {
            mentés(s, Órarendek.ToArray());
        }

        private static void OsztályMentés(Stream s)
        {
            mentés(s, Osztályok);
        }

        public static void BeállításMentés(Stream s)
        {
            mentés(s, Beállítások);
        }

        /// <summary>
        /// Visszatér a megjelenítendő héttel. Ez megegyezik a tényleges héttel, kivéve hétvégén, amikor a következő
        /// </summary>
        public static int Hét
        {
            get
            {
                int jelenlegihét = CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(DateTime.Today, CalendarWeekRule.FirstFullWeek, DayOfWeek.Monday);
                if (DateTime.Today.DayOfWeek > DayOfWeek.Friday || DateTime.Today.DayOfWeek == DayOfWeek.Sunday)
                    jelenlegihét++;
                return jelenlegihét;
            }
        }

        public static bool AHét
        {
            get
            {
                return Hét % 2 == 0;
            }
        }
    }
}
