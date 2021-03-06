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
using Orarend.Events;
using System.Security;

namespace OrarendAndroidApp
{
    [Activity(Label = "Órarend", MainLauncher = true, Theme = "@android:style/Theme.DeviceDefault")]
    public class MainActivity : ActivityBase
    {
        private Handler handler;

        private const int EDIT_ADD_ACT_REQUEST = 1;
        private const int SETTINGS_ACT_REQUEST = 2;
        public const string DATA_FILENAME = "data.json";

        protected override void OnCreate(Bundle bundle)
        {
            AndroidEnvironment.UnhandledExceptionRaiser += AndroidEnvironment_UnhandledExceptionRaiser;
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.MainLayout);
            ActionBar.SetDisplayShowTitleEnabled(false);
            handler = new Handler();
            string[] list = FileList();
            if (list.Contains(DATA_FILENAME)
                ? API.Betöltés(OpenFileInput(DATA_FILENAME), e => Hiba("Hiba az adatok betöltése során!\n" + e)) : API.Betöltés())
            {
                API.CsengőTimerEvent += CsengőTimer;
                API.Frissítéskor += (_, args) => HelyettesítésFrissítés(false, args);
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        private void AndroidEnvironment_UnhandledExceptionRaiser(object sender, RaiseThrowableEventArgs e)
        {
            if (!e.Handled) Hiba("Kezeletlen hiba!\n" + e.Exception);
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
            API.ÓrarendKiválasztás(e.Position);
            órarendfrissítés();
        }

        [SecuritySafeCritical]
        private void addCell(string text, Color color, TableRow tr1, (int, int)? tag = null)
        {
            TextView textview = new TextView(this);
            textview.SetText(text, TextView.BufferType.Normal);
            textview.SetTextColor(color);
            textview.SetPadding(10, 10, 10, 10);
            textview.SetBackgroundResource(DarkTheme ? Resource.Drawable.cell_shape_dark : Resource.Drawable.cell_shape_light);
            textview.Tag = tag.HasValue ? new JavaTuple<int, int>(tag.Value) : null;
            textview.Clickable = true;
            textview.Click += ÓraClick;
            //textview.LongClick += ÓraLongClick;
            RegisterForContextMenu(textview);
            textview.ContextMenuCreated += ÓraContextMenuCreated;
            tr1.AddView(textview);
        }

        [SecuritySafeCritical]
        private class JavaTuple<T1, T2> : Java.Lang.Object
        {
            public (T1, T2) obj;
            public JavaTuple((T1, T2) obj) => this.obj = obj;
            public void Deconstruct(out T1 first, out T2 second) => (first, second) = obj;
        }

        private void HelyettesítésFrissítés(bool internethiba = true, API.FrissítésEventArgs args = null)
        {
            var bar = FindViewById<ProgressBar>(Resource.Id.progressBar1);
            //var menu = FindViewById<ActionMenuView>(Resource.Id.actionMenuView1);
            Action loadstart = () => bar.Visibility = ViewStates.Visible;
            if (internethiba)
                handler.Post(loadstart);
            else
                handler.PostDelayed(loadstart, 500);
            API.HelyettesítésFrissítés(() => OpenFileOutput(DATA_FILENAME, FileCreationMode.Private)).ContinueWith(t =>
            {
                handler.RemoveCallbacks(loadstart);
                handler.Post(() =>
                {
                    bar.Visibility = ViewStates.Gone;
                    if (TaskHibaNemVolt(t, internethiba) && t.Result)
                    {
                        órarendfrissítés();
                        Toast.MakeText(this, "Helyettesítések frissítve", ToastLength.Short).Show();
                        if (args != null) args.Siker = true;
                    }
                    else if (!internethiba && args != null) args.Siker = true;
                });
            });
        }

        private void ÓrarendFrissítés(bool auto, Órarend ór = null)
        {
            var bar = FindViewById<ProgressBar>(Resource.Id.progressBar1);
            Action loadstart = () => bar.Visibility = ViewStates.Visible;
            if (auto)
                handler.PostDelayed(loadstart, 500);
            else
                handler.Post(loadstart);
            API.Frissítés(() => OpenFileOutput(DATA_FILENAME, FileCreationMode.Private), ór).ContinueWith(t =>
              {
                  handler.RemoveCallbacks(loadstart);
                  handler.Post(() =>
                  {
                      bar.Visibility = ViewStates.Gone;
                      órarendlistafrissítés();
                      HelyettesítésFrissítés();
                      if (TaskHibaNemVolt(t))
                      {
                          if (ór == null || ór == API.Órarend)
                              órarendfrissítés();
                          Toast.MakeText(this, (API.Órarendek.Count > 0 ? "Órarend" + (ór == null ? "ek" : "") + " és o" : "O") + "sztálylista frissítve", ToastLength.Short).Show();
                      }
                  });
              });
        }

        private string[] Napok = new string[6] { "Hétfő", "Kedd", "Szerda", "Csütörtök", "Péntek", "Szombat" };

        [SecuritySafeCritical]
        private void órarendfrissítés()
        {
            var table = FindViewById<TableLayout>(Resource.Id.tableLayout1);
            deselect();
            if (table.ChildCount > 0)
                table.RemoveViews(0, table.ChildCount);
            if (API.Órarend == null)
                return;
            TableRow tr = new TableRow(this);
            addCell(API.AHét ? "A" : "B", DarkTheme ? Color.White : Color.Black, tr);
            for (int i = 0; i < Napok.Length; i++)
                addCell(Napok[i], DarkTheme ? Color.White : Color.Black, tr);
            table.AddView(tr, new ViewGroup.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent));
            byte rowadds = 0;
            for (int j = 0; j < 16; j++)
            {
                tr = new TableRow(this);
                bool notnull = false;
                for (int i = 0; i < 6; i++)
                { //Kihagyja az üres sorokat
                    if (API.Órarend.Órák[i][j] != null && API.HelyettesítésInnenIde(API.Órarend, i, j).Item2 != null)
                    {
                        notnull = true;
                        break;
                    }
                }
                if (notnull)
                {
                    for (int x = 0; x < rowadds; x++)
                    {
                        var tr1 = new TableRow(this);
                        addCell((j + x).ToString(), DarkTheme ? Color.White : Color.Black, tr1);
                        for (int i = 0; i < 6; i++)
                            addCell("", Color.Black, tr1);
                        table.AddView(tr1, new ViewGroup.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent));
                    }
                    rowadds = 0;
                    addCell((j + 1).ToString(), DarkTheme ? Color.White : Color.Black, tr);
                    for (int i = 0; i < 6; i++)
                    {
                        var (innen, ide) = API.HelyettesítésInnenIde(API.Órarend, i, j);
                        addCell(ide != null ? ide.ÚjÓra.EgyediNév : innen != null ? innen.EredetiNap != innen.ÚjNap || innen.EredetiSorszám != innen.ÚjSorszám ? "áthelyezve" : innen.ÚjÓra?.EgyediNév ?? "elmarad" : API.Órarend.Órák[i][j]?.EgyediNév ?? "", innen == null ? (DarkTheme ? Color.WhiteSmoke : Color.Black) : Color.Red, tr, (i, j));
                    }
                    table.AddView(tr, new ViewGroup.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent));
                }
                else rowadds++;
            }
            handler.Post(() => MaiNaphozGörgetés());
        }

        private (int i, int j, Óra óra, Helyettesítés innen, Helyettesítés ide)? TV2Óra(TextView tv)
        {
            var ij = (JavaTuple<int, int>)tv.Tag;
            int i, j;
            Helyettesítés innen, ide;
            Óra óra;
            if (ij != null)
            {
                (i, j) = ij;
                (innen, ide) = API.HelyettesítésInnenIde(API.Órarend, i, j);
                if ((óra = API.Órarend.Órák[i][j]) == null && ide?.ÚjÓra == null)
                    return null;
            }
            else
                return null;
            return (i, j, óra, innen, ide);
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
        [SecuritySafeCritical]
        private void ÓraClick(object sender, EventArgs e)
        {
            var tv = (TextView)sender;
            if (selected != null && selected != sender)
                selected.SetBackgroundResource(DarkTheme ? Resource.Drawable.cell_shape_dark : Resource.Drawable.cell_shape_light);
            var x = TV2Óra(tv);
            if (x == null)
            {
                deselect();
                return;
            }
            var (i, j, óra, helyettesítésInnen, helyettesítésIde) = x?.ToTuple();
            tv.SetBackgroundResource(DarkTheme ? Resource.Drawable.cell_shape_selected_dark : Resource.Drawable.cell_shape_selected_light);
            selected = tv;
            var kivora = FindViewById<TextView>(Resource.Id.kivoraTV);
            if (óra == null)
                kivora.Visibility = ViewStates.Gone;
            else
            {
                kivora.Text = Napok[i] + " " + (j + 1) + ". óra"
                + "\nNév: " + óra.TeljesNév
                + "\nTerem: " + óra.Terem
                + "\nTanár: " + óra.Tanár.Név
                + "\nIdőtartam: " + API.Órarend.Órakezdetek[j].ToString("hh\\:mm") + "-" + API.Órarend.Órakezdetek[j].Add(new TimeSpan(0, 45, 0)).ToString("hh\\:mm")
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
                        : helyettesítésIde != null && (helyettesítésIde.EredetiNap != helyettesítésIde.ÚjNap || helyettesítésIde.EredetiSorszám != helyettesítésIde.ÚjSorszám)
                        ? "" : "Az óra elmarad") //Ha át lett helyezve ide másik óra, akkor nem kell kiírni, hogy elmarad ez az óra
            + (helyettesítésIde == null ? ""
                : helyettesítésIde.EredetiNap != helyettesítésIde.ÚjNap || helyettesítésIde.EredetiSorszám != helyettesítésIde.ÚjSorszám
                ? "Áthelyezve: " + Napok[(int)helyettesítésIde.EredetiNap - 1] + " " + helyettesítésIde.EredetiSorszám + ". óra --> ide"
                    + (helyettesítésIde.ÚjÓra.EgyediNév != óra?.EgyediNév ? "\nÓra: " + helyettesítésIde.ÚjÓra.EgyediNév : "")
                    + (helyettesítésIde.ÚjÓra.Terem != óra?.Terem ? "\nTerem: " + helyettesítésIde.ÚjÓra.Terem : "")
                    + ((óra?.Tanár.Név != (helyettesítésIde.ÚjÓra.Tanár.Név == "" ? API.Órarend.Órák[(int)helyettesítésIde.EredetiNap - 1][helyettesítésIde.EredetiSorszám - 1].Tanár.Név : helyettesítésIde.ÚjÓra.Tanár.Név)) ? "\nTanár: " + (óra?.Tanár.Név == "" ? API.Órarend.Órák[(int)helyettesítésIde.EredetiNap - 1][helyettesítésIde.EredetiSorszám - 1].Tanár.Név : helyettesítésIde.ÚjÓra.Tanár.Név) : "")
                    + (helyettesítésIde.ÚjÓra.Csoportok[0] != óra?.Csoportok[0] ? "\nCsoport: " + helyettesítésIde.ÚjÓra.Csoportok.Aggregate((a, b) => a + ", " + b) : "") //ˇˇ De ha változott, akkor nem
                    : "") //Ha a pozicíó nem változott, a fentebbi rész már kiírta az adatait
            ;
            hely.Visibility = hely.Text.Length > 0 ? ViewStates.Visible : ViewStates.Gone;
        }

        private void ÓraContextMenuCreated(object sender, View.CreateContextMenuEventArgs e)
        {
            switch (sender)
            {
                case TextView tv:
                    var x = TV2Óra(tv);
                    Óra óra;
                    if (x != null)
                        (_, _, óra, _, _) = x?.ToTuple();
                    else
                        óra = null;
                    if (óra == null)
                    { //TODO
                        ÓraContextItemData.Add(e.Menu.Add("Hozzáadás"), () => StartActivity(new Intent(this, typeof(SettingsActivity))));
                    }
                    break;
                default:
                    Hiba("Ismeretlen küldő a menühöz!");
                    break;
            }
        }

        private Dictionary<IMenuItem, Action> ÓraContextItemData = new Dictionary<IMenuItem, Action>();
        private T ctor<T>() where T : new() => new T();

        public override bool OnContextItemSelected(IMenuItem item)
        {
            bool ret = ÓraContextItemData.ContainsKey(item);
            if (ret)
                ÓraContextItemData[item]();
            return ret;
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
                ÓrarendFrissítés(true);
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
                        if (API.Órarend == null)
                        {
                            Toast.MakeText(this, "Nincs órarend kiválasztva", ToastLength.Short).Show();
                            break;
                        }
                        var intent = new Intent(this, typeof(EditActivity));
                        intent.PutExtra("add", false);
                        intent.PutExtra("index", API.Órarendek.IndexOf(API.Órarend));
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
                        ÓrarendFrissítés(false);
                        break;
                    }
            }
            return base.OnOptionsItemSelected(item);
        }

        private void Hiba(string msg) => Hiba(this, msg);

        public static void Hiba(Context c, string msg) =>
            new AlertDialog.Builder(c).SetMessage(msg).SetNeutralButton("OK", (s, e) => { ((AlertDialog)s).Dismiss(); ((AlertDialog)s).Dispose(); }).SetTitle("Hiba").Show();

        /// <summary>
        /// Az összes hibát kiírja, ami a <see cref="Task"/> futása közben keletkezett
        /// </summary>
        /// <param name="t"></param>
        /// <param name="internethiba">Ha igaz, kiírja a WebException-öket is</param>
        /// <returns>Igaz, ha nem volt hiba</returns>
        private bool TaskHibaNemVolt(Task t, bool internethiba = true)
        {
            bool ret = true;
            foreach (var ex in (IEnumerable<Exception>)t.Exception?.InnerExceptions ?? new Exception[0])
            {
                if (ex is WebException wex)
                {
                    if (internethiba && wex.Status == WebExceptionStatus.ConnectFailure)
                        Hiba("Nem sikerült csatlakozni az E-naplóhoz.\nHa van internet, próbáld újraindítani az alkalmazást.");
                    else if (internethiba)
                        Hiba("Nem sikerült csatlakozni az E-naplóhoz.\n" + wex.Message);
                }
                else if (ex is InvalidOperationException oex && oex.Data.Contains("OERROR") && (string)oex.Data["OERROR"] == "CLS_NOT_FOUND")
                {
                    ÓrarendFrissítés(true);
                    Toast.MakeText(this, oex.Message, ToastLength.Short).Show();
                }
                else
                    Hiba(ex.ToString());
                ret = false;
            }
            return ret;
        }

        private void CsengőTimer(object sender, TimerEventArgs e)
        {
            handler.Post(() =>
            {
                var kezdveg = FindViewById<TextView>(Resource.Id.kezdvegTV);
                var kovora = FindViewById<TextView>(Resource.Id.kovoraTV);
                kovora.Visibility = e.KövetkezőÓra == null ? ViewStates.Invisible : ViewStates.Visible;
                kovora.Text = e.KövetkezőÓra ?? "";
                kezdveg.Visibility = e.HátralévőIdő == null ? ViewStates.Invisible : ViewStates.Visible;
                kezdveg.Text = e.HátralévőIdő ?? "";
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
                    ÓrarendFrissítés(false, API.Órarendek[index]);
                else
                {
                    API.ÓrarendKiválasztásTörlése();
                    órarendfrissítés();
                }
                órarendlistafrissítés();
            }
            else if (requestCode == SETTINGS_ACT_REQUEST)
            {
                if (data?.Extras?.GetBoolean("offsetchanged") ?? false)
                    ÓrarendFrissítés(false);
                Recreate();
            }
        }

        public override void OnWindowFocusChanged(bool hasFocus)
        {
            base.OnWindowFocusChanged(hasFocus);
            API.Fókusz = hasFocus;
            if (!hasFocus)
                return;
            MaiNaphozGörgetés();
        }

        private void MaiNaphozGörgetés()
        {
            var table = FindViewById<TableLayout>(Resource.Id.tableLayout1);
            if (table.ChildCount == 0)
                return;
            var cell = (table.GetChildAt(0) as ViewGroup).GetChildAt((int)API.MaiNap);
            FindViewById<HorizontalScrollView>(Resource.Id.horizontalView).SmoothScrollTo(Math.Max(cell.Left - (FindViewById(Resource.Id.container).Width - cell.Width) / 2, 0), 0);
        }
    }
}
