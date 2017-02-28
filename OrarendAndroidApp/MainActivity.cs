using System;
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

namespace OrarendAndroidApp
{
    [Activity(Label = "Órarend", MainLauncher = true, Theme = "@android:style/Theme.Holo.Light")]
    public class MainActivity : Activity
    {
        private Handler handler;
        private Órarend órarend;
        private Timer timer;

        private const int EDIT_ADD_ACT_REQUEST = 1;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.MainLayout);
            ActionBar.SetDisplayShowTitleEnabled(false);
            ActionBar.CustomView = FindViewById<Spinner>(Resource.Id.spinner);
            handler = new Handler();
            string[] list = FileList();
            if (list.Contains("beallitasok"))
                API.BeállításBetöltés(OpenFileInput("beallitasok"), e => Hiba("Hiba a beállítások betöltése során!\n" + e));
            if (list.Contains("orarend") && API.Órarendek.Count == 0)
                API.ÓrarendBetöltés(OpenFileInput("orarend"), e => Hiba("Hiba az órarendek betöltése során!\n" + e));
            if (list.Contains("osztaly") && API.Osztályok == null)
                API.OsztályBetöltés(OpenFileInput("osztaly"), e => Hiba("Hiba az osztályok betöltése során!\n" + e));
            timer = new Timer(CsengőTimer, null, new TimeSpan(0, 0, 1), new TimeSpan(0, 0, 1));
            frissítésHa1ÓraEltelt();
        }

        private void órarendlistafrissítés()
        {
            handler.Post(() =>
            {
                var list = FindViewById<Spinner>(Resource.Id.spinner);
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
            textview.SetBackgroundResource(Resource.Drawable.cell_shape_light);
            textview.Tag = tag;
            textview.Clickable = true;
            textview.Click += ÓraClick;
            tr1.AddView(textview);
        }

        private void HelyettesítésFrissítés(bool internethiba = true)
        {
            var bar = FindViewById<ProgressBar>(Resource.Id.progressBar1);
            var menu = FindViewById<ActionMenuView>(Resource.Id.actionMenuView1);
            Action loadstart = () =>
             {
                 bar.Visibility = ViewStates.Visible;
                 menu.Enabled = false;
             };
            handler.Post(loadstart);
            API.HelyettesítésFrissítés(OpenFileOutput("orarend", FileCreationMode.Private)).ContinueWith(t =>
            {
                handler.RemoveCallbacks(loadstart);
                handler.Post(() =>
                {
                    bar.Visibility = ViewStates.Gone;
                    menu.Enabled = true;
                    if (TaskHiba(t, internethiba))
                    {
                        órarendfrissítés();
                        Toast.MakeText(this, "Helyettesítések frissítve", ToastLength.Short).Show();
                        utolsófrissítésplusz1óra = DateTime.Now + new TimeSpan(1, 0, 0);
                    }
                });
            });
        }

        private void ÓrarendFrissítés(Órarend ór = null)
        {
            var bar = FindViewById<ProgressBar>(Resource.Id.progressBar1);
            var menu = FindViewById<ActionMenuView>(Resource.Id.actionMenuView1);
            Action loadstart = () =>
            {
                bar.Visibility = ViewStates.Visible;
                menu.Enabled = false;
            };
            handler.Post(loadstart);
            API.Frissítés(OpenFileOutput("orarend", FileCreationMode.Private), OpenFileOutput("osztaly", FileCreationMode.Private), ór).ContinueWith(t =>
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
            addCell(API.AHét ? "A" : "B", Color.Black, tr);
            for (int i = 0; i < Napok.Length; i++)
                addCell(Napok[i], Color.Black, tr);
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
                    addCell((j + 1).ToString(), Color.Black, tr);
                    for (int i = 0; i < 6; i++)
                    {
                        var helyettesítés = órarend.Helyettesítések.FirstOrDefault(h => (int)h.EredetiNap - 1 == i && h.EredetiSorszám - 1 == j);
                        var helyettesítésIde = órarend.Helyettesítések.FirstOrDefault(h => (int)h.ÚjNap - 1 == i && h.ÚjSorszám - 1 == j && h.ÚjÓra != null); //Ha az eredeti óra elmarad, és ide lesz helyezve egy másik, az áthelyezést mutassa
                        //addCell(helyettesítés?.ÚjÓra?.EgyediNév ?? órarend.Órák[i][j]?.EgyediNév ?? "", helyettesítés == null ? Color.Black : Color.Red, tr, new int[2] { i, j });
                        addCell(helyettesítésIde != null ? helyettesítésIde.ÚjÓra.EgyediNév : helyettesítés != null ? helyettesítés.EredetiNap != helyettesítés.ÚjNap || helyettesítés.EredetiSorszám != helyettesítés.ÚjSorszám ? "Áthelyezve" : helyettesítés.ÚjÓra?.EgyediNév ?? "elmarad" : órarend.Órák[i][j]?.EgyediNév ?? "", helyettesítés == null ? Color.Black : Color.Red, tr, new int[2] { i, j });
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

        private TextView selected;
        /// <summary>
        /// Kiválasztja az adott órát
        /// </summary>
        private void ÓraClick(object sender, EventArgs e)
        {
            var tv = (TextView)sender;
            var ij = (int[])tv.Tag;
            if (selected != null && selected != sender)
                selected.SetBackgroundResource(Resource.Drawable.cell_shape_light);
            Óra óra;
            Helyettesítés helyettesítésInnen = null;
            Helyettesítés helyettesítésIde = null;
            if (ij != null)
            {
                helyettesítésInnen = órarend.Helyettesítések.FirstOrDefault(h => (int)h.EredetiNap == ij[0] + 1 && h.EredetiSorszám == ij[1] + 1);
                helyettesítésIde = órarend.Helyettesítések.FirstOrDefault(h => (int)h.ÚjNap == ij[0] + 1 && h.ÚjSorszám == ij[1] + 1 && h.ÚjÓra != null); //Ha az eredeti óra elmarad, és ide lesz helyezve egy másik, az áthelyezést mutassa
            }
            //if (ij == null || (óra = órarend.Órák[ij[0]][ij[1]] ?? ((helyettesítésIde = órarend.Helyettesítések.SingleOrDefault(h => (int)h.ÚjNap == ij[0] + 1 && h.ÚjSorszám == ij[1] + 1))?.ÚjÓra)) == null)
            if (ij == null || (óra = órarend.Órák[ij[0]][ij[1]] ?? (helyettesítésIde?.ÚjÓra)) == null)
            { //Ha az óra nincs beállítva, beállítja a helyettesítettre - TODO: Ne; rejtse el
                deselect();
                return;
            }
            tv.SetBackgroundResource(Resource.Drawable.cell_shape_selected_light);
            selected = tv;
            var kivora = FindViewById<TextView>(Resource.Id.kivoraTV);
            kivora.Text = Napok[ij[0]] + " " + (ij[1] + 1) + ". óra"
            + "\nNév: " + óra.TeljesNév
            + "\nTerem: " + óra.Terem
            + "\nTanár: " + óra.Tanár.Név
            + "\nIdőtartam: " + órarend.Órakezdetek[ij[1]].ToString("hh\\:mm") + "-" + órarend.Órakezdetek[ij[1]].Add(new TimeSpan(0, 45, 0)).ToString("hh\\:mm")
            + "\nCsoport: " + óra.Csoportok.Aggregate((a, b) => a + ", " + b);
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
                ? "Áthelyezve: " + Napok[(int)helyettesítésInnen.EredetiNap - 1] + " " + helyettesítésIde.EredetiSorszám + ". óra --> ide"
                    + (helyettesítésIde.ÚjÓra.EgyediNév != óra.EgyediNév ? "\nÓra: " + helyettesítésIde.ÚjÓra.EgyediNév : "")
                    + (helyettesítésIde.ÚjÓra.Terem != óra.Terem ? "\nTerem: " + helyettesítésIde.ÚjÓra.Terem : "")
                    + ((óra.Tanár.Név != (helyettesítésIde.ÚjÓra.Tanár.Név == "" ? órarend.Órák[(int)helyettesítésIde.EredetiNap - 1][helyettesítésIde.EredetiSorszám - 1].Tanár.Név : helyettesítésIde.ÚjÓra.Tanár.Név)) ? "\nTanár: " + (óra.Tanár.Név == "" ? órarend.Órák[(int)helyettesítésIde.EredetiNap - 1][helyettesítésIde.EredetiSorszám - 1].Tanár.Név : helyettesítésIde.ÚjÓra.Tanár.Név) : "") //TODO: A tanár mező üres ("")
                    + (helyettesítésIde.ÚjÓra.Csoportok[0] != óra.Csoportok[0] ? "\nCsoport: " + helyettesítésIde.ÚjÓra.Csoportok.Aggregate((a, b) => a + ", " + b) : "") //ˇˇ De ha változott, akkor nem
                    : "") //Ha a pozicíó nem változott, a fentebbi rész már kiírta az adatait
            ;
            kivora.Visibility = ViewStates.Visible;
            hely.Visibility = ViewStates.Visible;
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.main_menu_light, menu);
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
                case Resource.Id.menu_preferences: //TODO
                    break;
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
                bool talált = false;
                var kovora = FindViewById<TextView>(Resource.Id.kovoraTV);
                if (órarend.Órakezdetek[0] == TimeSpan.Zero)
                { //Még nincsenek beállítva a kezdetek
                    kezdveg.Text = "Betöltés";
                    kovora.Visibility = ViewStates.Invisible;
                    return;
                }
                for (int i = 0; i < órarend.Órakezdetek.Length - 1; i++)
                {
                    var vége = órarend.Órakezdetek[i].Add(new TimeSpan(0, 45, 0));
                    if (most > órarend.Órakezdetek[i])
                    {
                        if (most < vége)
                        {
                            kezdveg.Text = "Kicsengetés: " + (vége - most).ToString("hh\\:mm\\:ss");
                            talált = true;
                        }
                        else
                            continue;
                    }
                    else
                    {
                        kezdveg.Text = "Becsengetés: " + (órarend.Órakezdetek[i] - most).ToString("hh\\:mm\\:ss");
                        talált = true;
                        kovora.Visibility = ViewStates.Invisible;
                    }
                    int x = (int)DateTime.Today.DayOfWeek - 1;
                    Óra óra;
                    if (x != -1 && x < 6 && (óra = órarend.Órák[x][i]) != null)
                    { //-1: Vasárnap
                        kovora.Text = "Következő óra: " + óra.EgyediNév + "\n" + óra.Terem + "\n" + óra.Tanár.Név + "\n" + óra.Csoportok.Aggregate((a, b) => a + ", " + b);
                        kovora.Visibility = ViewStates.Visible;
                        kezdveg.Visibility = ViewStates.Visible;
                    }
                    else
                    {
                        kovora.Visibility = ViewStates.Invisible;
                        kezdveg.Visibility = ViewStates.Invisible;
                    }
                    break;
                }
                if (!talált)
                {
                    kezdveg.Visibility = ViewStates.Invisible;
                    kovora.Visibility = ViewStates.Invisible;
                }
            });
        }

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            if (resultCode == Result.Canceled)
                return;
            int index = data.Extras.GetBoolean("add") ? API.Órarendek.Count - 1 : data.Extras.GetInt("index");
            if (requestCode == EDIT_ADD_ACT_REQUEST)
            {
                if (!data.Extras.GetBoolean("deleted"))
                    ÓrarendFrissítés(API.Órarendek[index]);
                else
                {
                    órarend = null;
                    órarendfrissítés();
                }
                órarendlistafrissítés();
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
            timer.Change(new TimeSpan(0, 0, 0), new TimeSpan(0, 0, 1));
            frissítésHa1ÓraEltelt();
            MaiNaphozGörgetés();
        }

        private void MaiNaphozGörgetés()
        {
            var x = DateTime.Today.DayOfWeek == DayOfWeek.Sunday ? DayOfWeek.Monday : DateTime.Today.DayOfWeek;
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
