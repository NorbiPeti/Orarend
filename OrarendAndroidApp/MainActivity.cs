﻿using System;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Orarend;
using System.Linq;
using Android.Graphics;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Net;
using Android.Preferences;

namespace OrarendAndroidApp
{
    [Activity(Label = "Órarend", MainLauncher = true, Theme = "@android:style/Theme.Holo.Light")]
    public class MainActivity : ActivityBase
    {
        private Handler handler;
        private Órarend órarend;
        private Timer timer;

        private const int EDIT_ADD_ACT_REQUEST = 1;
        private const int SETTINGS_ACT_REQUEST = 2;
        public const string DATA_FILENAME = "data.json";

        protected override void OnCreate(Bundle bundle)
        {
            AndroidEnvironment.UnhandledExceptionRaiser += AndroidEnvironment_UnhandledExceptionRaiser;
            base.OnCreate(bundle);
            //RequestWindowFeature(WindowFeatures.ActionBar);
            SetContentView(Resource.Layout.MainLayout);
            //SetActionBar(new Toolbar(this));
            ActionBar.SetDisplayShowTitleEnabled(false);
            //ActionBar.CustomView = new Spinner(this);
            //ActionBar.CustomView = FindViewById<Spinner>(Resource.Id.spinner);
            //ActionBar.SetCustomView(FindViewById<Spinner>(Resource.Id.spinner), new ActionBar.LayoutParams(GravityFlags.Left));
            handler = new Handler();
            string[] list = FileList();
            if (list.Contains(DATA_FILENAME))
                API.Betöltés(OpenFileInput(DATA_FILENAME), e => Hiba("Hiba az adatok betöltése során!\n" + e)); //TODO: Release módban előfordul <--
            timer = new Timer(CsengőTimer, null, new TimeSpan(0, 0, 0), new TimeSpan(0, 0, 5));
        }

        private void AndroidEnvironment_UnhandledExceptionRaiser(object sender, RaiseThrowableEventArgs e)
        {
            if (!e.Handled)
                Hiba("Kezeletlen hiba!\n" + e.Exception);
            e.Handled = true;
        }

        private Spinner list;
        private void órarendlistafrissítés()
        {
            handler.Post(() =>
            {
                int selected = list.SelectedItemPosition;
                int count = list.Count;
                ArrayAdapter adapter;
                if (list.Adapter != null)
                {
                    adapter = (ArrayAdapter)list.Adapter;
                    adapter.Clear();
                    adapter.AddAll(API.Órarendek);
                }
                else
                {
                    adapter = new ArrayAdapter(this, Resource.Layout.simple_list_item_1, API.Órarendek);
                    list.ItemSelected += ÓrarendClick;
                }
                list.Adapter = adapter;
                adapter.NotifyDataSetChanged();
                if (selected >= list.Count || list.Count > count) //TÖrlés vagy hozzáadás után
                    selected = list.Count - 1;
                list.SetSelection(selected);
            });
        }

        private void ÓrarendClick(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            órarend = API.Órarendek[e.Position];
            órarendfrissítés();
        }

        private void addCell(string text, Color color, TableRow tr1, int[] tag = null)
        {
            TextView textview = new TextView(this);
            textview.SetText(text, TextView.BufferType.Normal);
            textview.SetTextColor(color);
            textview.SetPadding(10, 10, 10, 10);
            textview.SetBackgroundResource(DarkTheme ? Resource.Drawable.cell_shape_dark : Resource.Drawable.cell_shape_light);
            textview.Tag = tag;
            textview.Clickable = true;
            textview.Click += ÓraClick;
            tr1.AddView(textview);
        }

        private void HelyettesítésFrissítés(bool internethiba = true)
        {
            var bar = FindViewById<ProgressBar>(Resource.Id.progressBar1);
            //var menu = FindViewById<ActionMenuView>(Resource.Id.actionMenuView1);
            Action loadstart = () =>
             {
                 bar.Visibility = ViewStates.Visible;
                 //menu.Enabled = false;
             };
            handler.Post(loadstart);
            API.HelyettesítésFrissítés(() => OpenFileOutput(DATA_FILENAME, FileCreationMode.Private)).ContinueWith(t =>
            {
                handler.RemoveCallbacks(loadstart);
                handler.Post(() =>
                {
                    bar.Visibility = ViewStates.Gone;
                    //menu.Enabled = true;
                    if (TaskHiba(t, internethiba))
                    {
                        órarendfrissítés();
                        if (t.Result)
                            Toast.MakeText(this, "Helyettesítések frissítve", ToastLength.Short).Show();
                    }
                    utolsófrissítésplusz1óra = DateTime.Now + new TimeSpan(1, 0, 0); //Mindenképpen állítsa be, hogy ne írja folyamatosan a hibát
                });
            });
        }

        private void ÓrarendFrissítés(Órarend ór = null)
        {
            var bar = FindViewById<ProgressBar>(Resource.Id.progressBar1);
            //var menu = FindViewById<ActionMenuView>(Resource.Id.actionMenuView1);
            Action loadstart = () =>
            {
                bar.Visibility = ViewStates.Visible;
                //menu.Enabled = false;
            };
            handler.Post(loadstart);
            API.Frissítés(() => OpenFileOutput(DATA_FILENAME, FileCreationMode.Private), ór).ContinueWith(t =>
              {
                  handler.RemoveCallbacks(loadstart);
                  handler.Post(() =>
                  {
                      bar.Visibility = ViewStates.Gone;
                      órarendlistafrissítés();
                      HelyettesítésFrissítés();
                      if (TaskHiba(t))
                      {
                          if (ór == null || ór == órarend)
                              órarendfrissítés();
                          Toast.MakeText(this, (API.Órarendek.Count > 0 ? "Órarend" + (ór == null ? "ek" : "") + " és o" : "O") + "sztálylista frissítve", ToastLength.Short).Show();
                      }
                  });
              });
        }

        private string[] Napok = new string[6] { "Hétfő", "Kedd", "Szerda", "Csütörtök", "Péntek", "Szombat" };

        private void órarendfrissítés()
        {
            var table = FindViewById<TableLayout>(Resource.Id.tableLayout1);
            deselect();
            if (table.ChildCount > 0)
                table.RemoveViews(0, table.ChildCount);
            if (órarend == null)
                return;
            TableRow tr = new TableRow(this);
            addCell(API.AHét ? "A" : "B", DarkTheme ? Color.White : Color.Black, tr);
            for (int i = 0; i < Napok.Length; i++)
                addCell(Napok[i], DarkTheme ? Color.White : Color.Black, tr);
            table.AddView(tr, new ViewGroup.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent));
            for (int j = 0; j < 16; j++)
            {
                tr = new TableRow(this);
                bool notnull = false;
                for (int i = 0; i < 6; i++)
                { //Kihagyja az üres sorokat
                    if (órarend.Órák[i][j] != null)
                    {
                        notnull = true;
                        break;
                    }
                }
                if (notnull)
                {
                    addCell((j + 1).ToString(), DarkTheme ? Color.White : Color.Black, tr);
                    for (int i = 0; i < 6; i++)
                    {
                        var innenide = helyettesítésInnenIde(i, j);
                        var helyettesítés = innenide[0];
                        var helyettesítésIde = innenide[1];
                        addCell(helyettesítésIde != null ? helyettesítésIde.ÚjÓra.EgyediNév : helyettesítés != null ? helyettesítés.EredetiNap != helyettesítés.ÚjNap || helyettesítés.EredetiSorszám != helyettesítés.ÚjSorszám ? "Áthelyezve" : helyettesítés.ÚjÓra?.EgyediNév ?? "elmarad" : órarend.Órák[i][j]?.EgyediNév ?? "", helyettesítés == null ? (DarkTheme ? Color.WhiteSmoke : Color.Black) : Color.Red, tr, new int[2] { i, j });
                    }
                    table.AddView(tr, new ViewGroup.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent));
                }
            }
            handler.Post(() => MaiNaphozGörgetés());
        }

        /// <summary>
        /// A cellát nem frissíti, csak a szöveget tünteti el
        /// </summary>
        private void deselect()
        {
            FindViewById<TextView>(Resource.Id.kivoraTV).Visibility = ViewStates.Gone;
            FindViewById<TextView>(Resource.Id.helyTV).Visibility = ViewStates.Gone;
            selected = null;
        }

        private Helyettesítés[] helyettesítésInnenIde(int i, int j)
        { //TODO: API-ba
            return new Helyettesítés[]
                {
            órarend.Helyettesítések.FirstOrDefault(h => (int)h.EredetiNap == i + 1 && h.EredetiSorszám == j + 1),
            órarend.Helyettesítések.FirstOrDefault(h => (int)h.ÚjNap == i + 1 && h.ÚjSorszám == j + 1 && h.ÚjÓra != null) //Ha az eredeti óra elmarad, és ide lesz helyezve egy másik, az áthelyezést mutassa
                };
        }

        private TextView selected;
        /// <summary>
        /// Kiválasztja az adott órát
        /// </summary>
        private void ÓraClick(object sender, EventArgs e)
        {
            var tv = (TextView)sender;
            var ij = (int[])tv.Tag;
            if (selected != null && selected != sender)
                selected.SetBackgroundResource(DarkTheme ? Resource.Drawable.cell_shape_dark : Resource.Drawable.cell_shape_light);
            Óra óra;
            Helyettesítés helyettesítésInnen = null;
            Helyettesítés helyettesítésIde = null;
            if (ij != null)
            {
                var innenide = helyettesítésInnenIde(ij[0], ij[1]);
                helyettesítésInnen = innenide[0];
                helyettesítésIde = innenide[1];
            }
            if (ij == null || (óra = órarend.Órák[ij[0]][ij[1]]) == null && helyettesítésIde?.ÚjÓra == null)
            {
                deselect();
                return;
            }
            tv.SetBackgroundResource(DarkTheme ? Resource.Drawable.cell_shape_selected_dark : Resource.Drawable.cell_shape_selected_light);
            selected = tv;
            var kivora = FindViewById<TextView>(Resource.Id.kivoraTV);
            if (óra == null)
                kivora.Visibility = ViewStates.Gone;
            else
            {
                kivora.Text = Napok[ij[0]] + " " + (ij[1] + 1) + ". óra"
                + "\nNév: " + óra.TeljesNév
                + "\nTerem: " + óra.Terem
                + "\nTanár: " + óra.Tanár.Név
                + "\nIdőtartam: " + órarend.Órakezdetek[ij[1]].ToString("hh\\:mm") + "-" + órarend.Órakezdetek[ij[1]].Add(new TimeSpan(0, 45, 0)).ToString("hh\\:mm")
                + "\nCsoport: " + óra.Csoportok.Aggregate((a, b) => a + ", " + b);
                kivora.Visibility = ViewStates.Visible;
            }
            var hely = FindViewById<TextView>(Resource.Id.helyTV);
            hely.Text = (helyettesítésInnen == null ? ""
                : helyettesítésInnen.EredetiNap != helyettesítésInnen.ÚjNap || helyettesítésInnen.EredetiSorszám != helyettesítésInnen.ÚjSorszám
                ? "Áthelyezve: innen --> " + Napok[(int)helyettesítésInnen.ÚjNap - 1] + " " + helyettesítésInnen.ÚjSorszám + ". óra"
                    : helyettesítésInnen.ÚjÓra != null && helyettesítésInnen.ÚjÓra != óra
                        ? "Helyettesítés:"
                        + (helyettesítésInnen.ÚjÓra.EgyediNév != óra.EgyediNév ? "\nÓra: " + helyettesítésInnen.ÚjÓra.EgyediNév : "")
                        + (helyettesítésInnen.ÚjÓra.Terem != óra.Terem ? "\nTerem: " + helyettesítésInnen.ÚjÓra.Terem : "")
                        + (helyettesítésInnen.ÚjÓra.Tanár.Név != óra.Tanár.Név ? "\nTanár: " + helyettesítésInnen.ÚjÓra.Tanár.Név : "")
                        + (helyettesítésInnen.ÚjÓra.Csoportok[0] != óra.Csoportok[0] ? "\nCsoport: " + helyettesítésInnen.ÚjÓra.Csoportok.Aggregate((a, b) => a + ", " + b) : "")
                        : "Az óra elmarad")
            + (helyettesítésIde == null ? ""
                : helyettesítésIde.EredetiNap != helyettesítésIde.ÚjNap || helyettesítésIde.EredetiSorszám != helyettesítésIde.ÚjSorszám
                ? "Áthelyezve: " + Napok[(int)helyettesítésIde.EredetiNap - 1] + " " + helyettesítésIde.EredetiSorszám + ". óra --> ide"
                    + (helyettesítésIde.ÚjÓra.EgyediNév != óra?.EgyediNév ? "\nÓra: " + helyettesítésIde.ÚjÓra.EgyediNév : "")
                    + (helyettesítésIde.ÚjÓra.Terem != óra?.Terem ? "\nTerem: " + helyettesítésIde.ÚjÓra.Terem : "")
                    + ((óra?.Tanár.Név != (helyettesítésIde.ÚjÓra.Tanár.Név == "" ? órarend.Órák[(int)helyettesítésIde.EredetiNap - 1][helyettesítésIde.EredetiSorszám - 1].Tanár.Név : helyettesítésIde.ÚjÓra.Tanár.Név)) ? "\nTanár: " + (óra?.Tanár.Név == "" ? órarend.Órák[(int)helyettesítésIde.EredetiNap - 1][helyettesítésIde.EredetiSorszám - 1].Tanár.Név : helyettesítésIde.ÚjÓra.Tanár.Név) : "")
                    + (helyettesítésIde.ÚjÓra.Csoportok[0] != óra?.Csoportok[0] ? "\nCsoport: " + helyettesítésIde.ÚjÓra.Csoportok.Aggregate((a, b) => a + ", " + b) : "") //ˇˇ De ha változott, akkor nem
                    : "") //Ha a pozicíó nem változott, a fentebbi rész már kiírta az adatait
            ;
            hely.Visibility = ViewStates.Visible;
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.main_menu_light, menu);
            ActionBar.SetCustomView(list = new Spinner(this, SpinnerMode.Dropdown), new ActionBar.LayoutParams(ActionBar.LayoutParams.MatchParent, ActionBar.LayoutParams.MatchParent, GravityFlags.Left));
            ActionBar.SetDisplayShowCustomEnabled(true);
            if (DarkTheme)
            {
                menu.FindItem(Resource.Id.menu_add).SetIcon(Resource.Drawable.ic_add_white_24dp);
                menu.FindItem(Resource.Id.menu_edit).SetIcon(Resource.Drawable.ic_create_white_24dp);
                menu.FindItem(Resource.Id.menu_refresh).SetIcon(Resource.Drawable.ic_autorenew_white_24dp);
                menu.FindItem(Resource.Id.menu_preferences).SetIcon(Resource.Drawable.ic_settings_white_24dp);
            }
            if (API.Osztályok == null || API.Osztályok.Length == 0)
                ÓrarendFrissítés();
            else
                órarendlistafrissítés();
            return base.OnCreateOptionsMenu(menu);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.menu_refresh:
                    {
                        HelyettesítésFrissítés();
                        break;
                    }
                case Resource.Id.menu_add:
                    {
                        var intent = new Intent(this, typeof(EditActivity));
                        intent.PutExtra("add", true);
                        StartActivityForResult(intent, EDIT_ADD_ACT_REQUEST);
                        break;
                    }
                case Resource.Id.menu_edit:
                    {
                        if (órarend == null)
                        {
                            Toast.MakeText(this, "Nincs órarend kiválasztva", ToastLength.Short).Show();
                            break;
                        }
                        var intent = new Intent(this, typeof(EditActivity));
                        intent.PutExtra("add", false);
                        intent.PutExtra("index", API.Órarendek.IndexOf(órarend));
                        StartActivityForResult(intent, EDIT_ADD_ACT_REQUEST);
                        break;
                    }
                case Resource.Id.menu_preferences:
                    {
                        var intent = new Intent(this, typeof(SettingsActivity));
                        StartActivityForResult(intent, SETTINGS_ACT_REQUEST);
                        break;
                    }
                case Resource.Id.menu_fullrefresh:
                    {
                        ÓrarendFrissítés();
                        break;
                    }
            }
            return base.OnOptionsItemSelected(item);
        }

        private void Hiba(string msg)
        {
            Hiba(this, msg);
        }

        public static void Hiba(Context c, string msg)
        {
            new AlertDialog.Builder(c).SetMessage(msg).SetNeutralButton("OK", (s, e) => { ((AlertDialog)s).Dismiss(); ((AlertDialog)s).Dispose(); }).SetTitle("Hiba").Show();
        }

        /// <summary>
        /// Az összes hibát kiírja, ami a <see cref="Task"/> futása közben keletkezett
        /// </summary>
        /// <param name="t"></param>
        /// <param name="internethiba">Ha igaz, kiírja a WebException-öket is</param>
        /// <returns>Igaz, ha nem volt hiba</returns>
        private bool TaskHiba(Task t, bool internethiba = true)
        {
            bool ret = true;
            foreach (var ex in (IEnumerable<Exception>)t.Exception?.InnerExceptions ?? new Exception[0])
            {
                if (ex is WebException)
                {
                    if (internethiba)
                        Hiba("Nem sikerült csatlakozni az E-naplóhoz.\n" + ex.Message);
                }
                else
                    Hiba(ex.ToString());
                ret = false;
            }
            return ret;
        }

        private bool nincstöbbóra = false;
        private void CsengőTimer(object state)
        {
            handler.Post(() =>
            {
                var kezdveg = FindViewById<TextView>(Resource.Id.kezdvegTV);
                if (órarend == null)
                {
                    kezdveg.Text = "Nincs órarend kiválasztva";
                    return;
                }
                var most = DateTime.Now - DateTime.Today;
                //var most = new TimeSpan(9, 46, 0);
                bool talált = false;
                var kovora = FindViewById<TextView>(Resource.Id.kovoraTV);
                nincstöbbóra = false;
                if (órarend.Órakezdetek[0] == TimeSpan.Zero)
                { //Még nincsenek beállítva a kezdetek
                    kezdveg.Text = "Betöltés";
                    kovora.Visibility = ViewStates.Invisible;
                    return;
                }
                for (int i = 0; i < órarend.Órakezdetek.Length - 1; i++)
                {
                    var vége = órarend.Órakezdetek[i].Add(new TimeSpan(0, 45, 0));
                    bool becsengetés;
                    int x = (int)DateTime.Today.DayOfWeek - 1; //TODO: A mai nap és ez az egész az API-ba
                    //x = 2; //TODO: TMP
                    Óra óra;
                    var innenide = helyettesítésInnenIde(x, i);
                    Func<TimeSpan, string> óraperc = ts => ts.Hours > 0 ? ts.ToString("h\\ómm\\p") : ts.ToString("mm") + " perc";
                    if (x != -1 && x < 6 && (óra = innenide[1] != null ? innenide[1].ÚjÓra : innenide[0] != null ? innenide[0].EredetiNap != innenide[0].ÚjNap || innenide[0].EredetiSorszám != innenide[0].ÚjSorszám ? null : innenide[0].ÚjÓra : órarend.Órák[x][i]) != null)
                    { //-1: Vasárnap
                        if (most > órarend.Órakezdetek[i])
                        {
                            if (most < vége)
                            {
                                kezdveg.Text = "Kicsengetés: " + óraperc(vége - most);
                                talált = true;
                                becsengetés = false;
                            }
                            else
                                continue;
                        }
                        else
                        {
                            kezdveg.Text = "Becsengetés: " + óraperc(órarend.Órakezdetek[i] - most);
                            talált = true;
                            becsengetés = true;
                        }
                        kovora.Text = (becsengetés ? "Következő" : "Jelenlegi") + " óra: " + óra.EgyediNév + "\n" + óra.Terem + "\n" + óra.Tanár.Név + "\n" + óra.Csoportok.Aggregate((a, b) => a + ", " + b);
                        kovora.Visibility = ViewStates.Visible;
                        kezdveg.Visibility = ViewStates.Visible;
                        nincstöbbóra = false;
                        break;
                    }
                }
                if (!talált)
                {
                    kezdveg.Visibility = ViewStates.Invisible;
                    kovora.Visibility = ViewStates.Invisible;
                    nincstöbbóra = true;
                }
            });
        }

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            if (requestCode == EDIT_ADD_ACT_REQUEST)
            {
                if (resultCode == Result.Canceled)
                    return;
                int index = data.Extras.GetBoolean("add") ? API.Órarendek.Count - 1 : data.Extras.GetInt("index");
                if (!data.Extras.GetBoolean("deleted"))
                    ÓrarendFrissítés(API.Órarendek[index]);
                else
                {
                    órarend = null;
                    órarendfrissítés();
                }
                órarendlistafrissítés();
            }
            else if (requestCode == SETTINGS_ACT_REQUEST)
            {
                Recreate();
            }
        }
        
        public override void OnWindowFocusChanged(bool hasFocus)
        {
            base.OnWindowFocusChanged(hasFocus);
            if (!hasFocus)
            {
                timer.Change(Timeout.Infinite, Timeout.Infinite);
                return;
            }
            timer.Change(new TimeSpan(0, 0, 0), new TimeSpan(0, 0, 5));
            frissítésHa1ÓraEltelt();
            MaiNaphozGörgetés();
        }

        private void MaiNaphozGörgetés()
        {
            var x = DateTime.Today.DayOfWeek;
            if (nincstöbbóra) x++;
            x = x > DayOfWeek.Saturday || x == DayOfWeek.Sunday ? DayOfWeek.Monday : x;
            var table = FindViewById<TableLayout>(Resource.Id.tableLayout1);
            if (table.ChildCount == 0)
                return;
            var cell = (table.GetChildAt(0) as ViewGroup).GetChildAt((int)x);
            FindViewById<HorizontalScrollView>(Resource.Id.horizontalView).SmoothScrollTo(Math.Max(cell.Left - (FindViewById(Resource.Id.container).Width - cell.Width) / 2, 0), 0);
        }

        private DateTime utolsófrissítésplusz1óra = DateTime.MinValue;
        private void frissítésHa1ÓraEltelt()
        {
            if (utolsófrissítésplusz1óra > DateTime.Now)
                return;
            HelyettesítésFrissítés(false);
        }
    }
}
