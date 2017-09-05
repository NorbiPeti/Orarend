using HtmlAgilityPack;
using Orarend.Events;
using SimpleTimerPortable;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Orarend
{
    [DataContract]
    public class API
    { //TODO: Előre megadott egyedi nevek használata
        internal static API példány = new API(); //TODO: FrissítésHa1ÓraEltelt() mentés
        private API()
        {
        }
        /// <summary>
        /// A kulcs az óra azonosítója
        /// </summary>
        [DataMember(Order = 1)]
        public Dictionary<string, ÓraTípus> típusok { get; private set; } = new Dictionary<string, ÓraTípus>();
        [DataMember]
        public Osztály[] osztályok { get; private set; } = new Osztály[0]; //Ez az initializáció csak akkor fut le, ha nem tölti be fájlból
        [DataMember(Order = 2)]
        public List<Órarend> órarendek { get; private set; } = new List<Órarend>();
        //[DataMember]
        public Settings beállítások { get; private set; } = new Settings();
        /// <summary>
        /// <para>Visszatér az osztályok listájával.</para>
        /// <para>Lehet null, ha még nem volt sikeres <see cref="Frissítés"/>.</para>
        /// </summary>
        /// <returns></returns>
        public static Osztály[] Osztályok { get => példány.osztályok; private set => példány.osztályok = value; }
        public static List<Órarend> Órarendek { get { return példány.órarendek; } }
        public static Settings Beállítások { get => példány.beállítások; private set => példány.beállítások = value; }
        /// <summary>
        /// Frissíti az osztálylistát és az eredeti órarendet, első megnyitásnál, és egy órarend hozzáadásánál/szerkesztésénél, majd hetente elegendő meghívni
        /// <param name="stream">A file stream, ahova mentse az adatokat, hogy ne kelljen külön meghívni - Azért funkció, hogy elkerüljök az adatvesztést, mivel így csak a mentéskor nyitja meg</param>
        /// </summary>
        public static async Task Frissítés(Func<Stream> stream, Órarend ór = null)
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
                        string html = Regex.Replace(Regex.Replace(Regex.Replace(sr.ReadToEnd(), "<th([^>]*)>((?:\\w|[áéóüöőúű./])+)(?=<)(?!\\/)", "<th$1>$2</th>"), "<(tr)" + trtd, "<$1>$2</$1>"), "<(td)" + trtd, "<$1>$2</$1>");
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
                  var doc = await load("https://deri.enaplo.net/ajax/orarend/orarendoszt.php?p=" + órarend.Osztály.Azonosító);
                  await Task.Run(() =>
                      {
                          lock (Órarendek)
                          {
                              Osztályok = doc.GetElementbyId("uok").ChildNodes.Where(node => node.HasAttributes).Select(node => new Osztály { Azonosító = node.GetAttributeValue("value", ""), Név = node.NextSibling.InnerText }).ToArray();
                              bool ahét = true;
                              int maxx = 0;
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
                                              maxx = x > maxx ? x : maxx;
                                              órarend.Órakezdetek[x] = TimeSpan.Parse(node.FirstChild.Attributes["title"].Value.Split('-')[0].Trim());
                                              var órák = (ahét ? órarend.ÓrákAHét : órarend.ÓrákBHét);
                                              for (int i = 0; i < 5; i++) //Napok
                                              {
                                                  var óranode = node.ChildNodes[i + 1].FirstChild;
                                                  var óra = órák[i][x];
                                                  if (óranode.ChildNodes.Count == 0)
                                                  {
                                                      órák[i][x] = null;
                                                      continue;
                                                  }
                                                  for (int j = 0; j < óranode.ChildNodes.Count; j += 6)
                                                  {
                                                      var csoport = óranode.ChildNodes[j].InnerText.TrimEnd(':');
                                                      if (csoport != "Egész osztály" && !órarend.Csoportok.Contains(csoport))
                                                      {
                                                          órák[i][x] = null;
                                                          continue;
                                                      }
                                                      if (óra == null)
                                                          órák[i][x] = óra = new Óra();
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
                              for (int i = maxx + 1; i < 16; i++) //Órák
                                  for (int j = 0; j < 5; j++) //Napok
                                      órarend.Órák[j][i] = null; //Kitörli a küldött órarendben nem szereplő órákat
                          }
                      });
                  await Task.Delay(10);
              };
            if (ór == null)
                foreach (var órarend in Órarendek)
                    await órarenda(órarend);
            else
                await órarenda(ór);
            Mentés(stream());
        }

        /// <summary>
        /// Frissíti a helyettesítéseket, naponta, indításkor vagy gombnyommásra frissítse (minden nap az első előtérbe kerüléskor)
        /// </summary>
        /// <param name="stream">A file stream, ahova mentse az adatokat, hogy ne kelljen külön meghívni - Azért funkció, hogy elkerüljök az adatvesztést, mivel így csak a mentéskor nyitja meg</param>
        public static async Task<bool> HelyettesítésFrissítés(Func<Stream> stream)
        {
            if (Órarendek.Count == 0 || Osztályok.Length == 0)
                return false;
            HtmlDocument doc = new HtmlDocument();
            var req = WebRequest.CreateHttp("http://deri.enaplo.net/ajax/print/htlista.php");
            var resp = await req.GetResponseAsync();
            await Task.Run(() =>
            {
                lock (Órarendek)
                {
                    using (var sr = new StreamReader(resp.GetResponseStream()))
                        doc.LoadHtml(sr.ReadToEnd());
                    foreach (var órarend in Órarendek)
                        órarend.Helyettesítések.Clear();
                    foreach (var node in doc.DocumentNode.ChildNodes[2].ChildNodes[1].ChildNodes)
                    {
                        DateTime dátum = DateTime.Parse(node.ChildNodes[0].InnerText.Substring(0, node.ChildNodes[0].InnerText.Length - 4));
                        int hét = CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(dátum, CalendarWeekRule.FirstFullWeek, DayOfWeek.Monday);
                        if (hét != Hét)
                            continue;
                        byte óraszám = byte.Parse(node.ChildNodes[1].InnerText);
                        var osztályok = node.ChildNodes[2].InnerText.Split(new string[] { ", " }, StringSplitOptions.None);
                        foreach (string osztálynév in osztályok)
                        {
                            Osztály osztály = Osztályok.SingleOrDefault(o => o.Azonosító.Contains(osztálynév));
                            if (osztály == null)
                            {
                                var x = new InvalidOperationException($"A helyettesítésekben szereplő osztály \"{osztálynév}\" nem található.");
                                x.Data.Add("OERROR", "CLS_NOT_FOUND");
                                throw x;
                            }
                            var csoportok = node.ChildNodes[3].InnerText;
                            int névindex = csoportok.IndexOf(osztálynév);
                            int végeindex = csoportok.IndexOf(")", névindex >= 0 ? névindex : 0);
                            string csoport = osztályok.Length == 1 ? csoportok : csoportok.Substring(névindex + osztálynév.Length + 1, végeindex - névindex - osztálynév.Length - 1);
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
                            {
                                if (tanár == "")
                                    tanár = órarend.Órák[(int)dátum.DayOfWeek - 1][óraszám - 1]?.Tanár.Név ?? "";
                                var helyettesítés = new Helyettesítés { EredetiNap = dátum.DayOfWeek, EredetiSorszám = óraszám, ÚjÓra = tanár == "elmarad" ? null : new Óra { Azonosító = óraaz, Csoportok = new string[] { csoport }, Terem = terem, Tanár = new Tanár { Név = tanár }, TeljesNév = óranév }, ÚjNap = újnap, ÚjSorszám = újsorszám };
                                órarend.Helyettesítések.Add(helyettesítés);
                            }
                        }
                    }
                }
                Mentés(stream());
                utolsófrissítésplusz1óra = DateTime.Now + new TimeSpan(1, 0, 0); //Mindenképpen állítsa be, hogy ne írja folyamatosan a hibát
            });
            return true;
        }

        [OnDeserializing]
        private void betöltés(StreamingContext context) => példány = this; //Az órák azonosítójának beállításakor szükséges már

        /// <summary>
        /// Betölti az adatokat, ha még nincsenek betöltve
        /// </summary>
        /// <param name="s">A stream, ahonnan betöltse az adatokat</param>
        /// <param name="hibánál">Megadja, mi történjen egy hiba esetén</param>
        /// <returns>Elvégezte-e a betöltést</returns>
        public static bool Betöltés(Stream s, Action<Exception> hibánál)
        {
            using (s)
            {
                if (!!!betöltés())
                    return false;
                using (var ms = new MemoryStream())
                {
                    s.CopyTo(ms);
                    if (ms.Length > 2)
                    {
                        try
                        {
                            ms.Seek(0, SeekOrigin.Begin);
                            var serializer = new DataContractJsonSerializer(typeof(API));
                            serializer.ReadObject(ms); //A példányt beállítja, mikor elkezdi, nem várja meg, hogy végezzen (betöltés())
                            return true;
                        }
                        catch (Exception e)
                        {
                            hibánál(e);
                            return Betöltés();
                        }
                    }
                    else return false;
                }
            }
        } //TODO: Tényleges órarendből állapítsa meg azt is, hogyha egyáltalán nincs ott egy óra, és máshol sincs, és ezt írja ki

        /// <summary>
        /// Betölti az alapértelemzett értékeket
        /// <returns>Elvégezte-e a betöltést</returns>
        /// </summary>
        public static bool Betöltés()
        {
            if (!betöltés())
                return false;
            példány = new API();
            return true;
        }

        private static Timer timer;
        private static bool betöltés()
        {
            if (!!(Órarendek.Count > 0 || Osztályok?.Length > 0 || timer != null))
                return false;
            timer = new Timer(CsengőTimer, null, new TimeSpan(0, 0, 0, 0, 100), new TimeSpan(0, 0, 5));
            return true;
        }

        public static void Mentés(Stream s)
        {
            using (s)
                if (példány != null)
                    new DataContractJsonSerializer(példány.GetType()).WriteObject(s, példány);
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

        public static bool AHét { get => Hét % 2 == 0; }

        public static bool Fókusz
        {
            set
            {
                if (value)
                {
                    timer = timer.Change(new TimeSpan(0, 0, 0), new TimeSpan(0, 0, 5));
                    frissítésHa1ÓraEltelt();
                }
                else
                    timer.Cancel();
            }
        }

        private static DateTime utolsófrissítésplusz1óra = DateTime.MinValue;
        public static event EventHandler<FrissítésEventArgs> Frissítéskor;
        public class FrissítésEventArgs { public bool Siker { get; set; } = false; }
        private static void frissítésHa1ÓraEltelt()
        {
            if (utolsófrissítésplusz1óra > DateTime.Now)
                return;
            var args = new FrissítésEventArgs();
            Frissítéskor?.Invoke(példány, args);
            if (args.Siker)
                utolsófrissítésplusz1óra = DateTime.Now + new TimeSpan(1, 0, 0);
        }

        public static DayOfWeek MaiNap
        {
            get
            {
                var x = DateTime.Today.DayOfWeek;
                if (nincstöbbóra) x++;
                return x > DayOfWeek.Saturday || x == DayOfWeek.Sunday ? DayOfWeek.Monday : x;
            }
        }

        public static (Helyettesítés innen, Helyettesítés ide) HelyettesítésInnenIde(Órarend órarend, int i, int j) =>
            (órarend.Helyettesítések.FirstOrDefault(h => (int)h.EredetiNap == i + 1 && h.EredetiSorszám == j + 1),
            órarend.Helyettesítések.FirstOrDefault(h => (int)h.ÚjNap == i + 1 && h.ÚjSorszám == j + 1 && h.ÚjÓra != null));
        //Ha az eredeti óra elmarad, és ide lesz helyezve egy másik, az áthelyezést mutassa

        public static Órarend Órarend { get; private set; }
        public static void ÓrarendKiválasztás(int position) => Órarend = Órarendek[position];
        public static void ÓrarendKiválasztásTörlése() => Órarend = null;

        private static bool nincstöbbóra = false;
        public static event EventHandler<TimerEventArgs> CsengőTimerEvent;
        private static void CsengőTimer(object state) => CsengőTimerEvent?.Invoke(példány, CsengőTimer());
        private static TimerEventArgs CsengőTimer()
        {
            if (Órarend == null)
                return new TimerEventArgs(null, "Nincs órarend kiválasztva");
            var most = DateTime.Now - DateTime.Today;
            //var most = new TimeSpan(9, 46, 0);
            bool talált = false;
            if (Órarend.Órakezdetek[0] == TimeSpan.Zero) //Még nincsenek beállítva a kezdetek
                return new TimerEventArgs(null, "Betöltés");
            string kezdveg = null, kovora = null;
            for (int i = 0; i < Órarend.Órakezdetek.Length - 1; i++)
            {
                var vége = Órarend.Órakezdetek[i].Add(new TimeSpan(0, 45, 0));
                bool becsengetés;
                int x = (int)DateTime.Today.DayOfWeek - 1;
                Óra óra;
                var (innen, ide) = HelyettesítésInnenIde(Órarend, x, i);
                Func<TimeSpan, string> óraperc = ts => ts.Hours > 0 ? ts.ToString("h\\ómm\\p") : ts.ToString("mm") + " perc";
                if (x != -1 && x < 6 && (óra = ide != null ? ide.ÚjÓra : innen != null ? innen.EredetiNap != innen.ÚjNap || innen.EredetiSorszám != innen.ÚjSorszám ? null : innen.ÚjÓra : Órarend.Órák[x][i]) != null)
                { //-1: Vasárnap
                    if (most > Órarend.Órakezdetek[i])
                    {
                        if (most < vége)
                        {
                            kezdveg = "Kicsengetés: " + óraperc(vége - most);
                            talált = true;
                            becsengetés = false;
                        }
                        else
                            continue;
                    }
                    else
                    {
                        kezdveg = "Becsengetés: " + óraperc(Órarend.Órakezdetek[i] - most);
                        talált = true;
                        becsengetés = true;
                    }
                    kovora = (becsengetés ? "Következő" : "Jelenlegi") + " óra: " + óra.EgyediNév + "\n" + óra.Terem + "\n" + óra.Tanár.Név + "\n" + óra.Csoportok.Aggregate((a, b) => a + ", " + b);
                    break;
                }
            }
            nincstöbbóra = !talált;
            return new TimerEventArgs(kovora, kezdveg);
        }
    }
}
