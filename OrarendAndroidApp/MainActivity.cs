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
using Java.Lang;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.IO;

namespace OrarendAndroidApp
{
    [Activity(Label = "Órarend", MainLauncher = true, Icon = "@drawable/icon", Theme = "@android:style/Theme.Holo.Light")]
    public class MainActivity : Activity
    {
        private Handler handler;
        private Órarend órarend;
        
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.MainLayout);
            ActionBar.SetDisplayShowTitleEnabled(false);
            ActionBar.CustomView = FindViewById<Spinner>(Resource.Id.spinner);
                handler = new Handler();
            new StreamReader(OpenFileInput("osztaly")).ReadToEnd(); //TODO: TMP, fix "[{},{},{},{},{},{},{},{},{},{},{},{},{},{},{},{},{},{},{},{},{},{},{},{},{},{},{},{},{},{},{},{},{}]"
            string[] list = FileList();
            if (list.Contains("beallitasok"))
                API.BeállításBetöltés(OpenFileInput("beallitasok"));
            if (list.Contains("orarend"))
                API.ÓrarendBetöltés(OpenFileInput("orarend"));
            if (list.Contains("osztaly"))
                API.OsztályBetöltés(OpenFileInput("osztaly"));
            if (list.Contains("osztaly"))
                API.OsztályBetöltés(OpenFileInput("osztaly"));
            if (API.Osztályok == null || API.Osztályok.Length == 0)
                ÓrarendFrissítés();
            var timer = new Timer(CsengőTimer, null, TimeSpan.Zero, new TimeSpan(0, 0, 1));
        }

        private void addCell(string text, Color color, TableRow tr1, bool clickable = false, int[] tag = null)
        {
            TextView textview = new TextView(this);
            textview.SetText(text, TextView.BufferType.Normal);
            textview.SetTextColor(color);
            textview.SetPadding(10, 10, 10, 10);
            textview.SetBackgroundResource(Resource.Drawable.cell_shape_light);
            textview.Tag = tag;
            if (textview.Clickable = clickable)
                textview.Click += ÓraClick;
            tr1.AddView(textview);
        }

        private void HelyettesítésFrissítés()
        {
            var bar = FindViewById<ProgressBar>(Resource.Id.progressBar1);
            handler.Post(() => bar.Visibility = ViewStates.Visible);
            API.HelyettesítésFrissítés(OpenFileOutput("helyettesites", FileCreationMode.Private)).ContinueWith(t =>
            {
                handler.Post(() =>
                {
                    bar.Visibility = ViewStates.Gone;
                    Toast.MakeText(this, "Helyettesítések frissítve", ToastLength.Short);
                });
            });
        }

        private void ÓrarendFrissítés(Órarend ór = null)
        { //TODO: Meghívni minden tervezett alkalommal; hozzáadásnál csak a hozzáadott órarendet frissítse
            var bar = FindViewById<ProgressBar>(Resource.Id.progressBar1);
            handler.Post(() => bar.Visibility = ViewStates.Visible);
            API.Frissítés(OpenFileOutput("orarend", FileCreationMode.Private), OpenFileOutput("osztaly", FileCreationMode.Private), ór).ContinueWith(t =>
            {
                handler.Post(() =>
                {
                    if (TaskHiba(t) && órarend != null && (ór == null || ór == órarend))
                    {
                        var table = FindViewById<TableLayout>(Resource.Id.tableLayout1);
                        if (table.ChildCount > 1)
                            table.RemoveViews(1, table.ChildCount);
                        TableRow tr = new TableRow(this);
                        addCell("", Color.Black, tr);
                        addCell("Hétfő", Color.Black, tr);
                        addCell("Kedd", Color.Black, tr);
                        addCell("Szerda", Color.Black, tr);
                        addCell("Csütörtök", Color.Black, tr);
                        addCell("Péntek", Color.Black, tr);
                        addCell("Szombat", Color.Black, tr);
                        table.AddView(tr, new ViewGroup.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent));
                        for (int j = 0; j < órarend.ÓrákAHét.GetLength(1); j++)
                        {
                            tr = new TableRow(this);
                            bool notnull = false;
                            for (int i = 0; i < órarend.ÓrákAHét.GetLength(0); i++)
                            { //Kihagyja az üres sorokat
                                if (órarend.ÓrákAHét[i, j] != null)
                                {
                                    notnull = true;
                                    break;
                                }
                            }
                            if (notnull)
                            {
                                addCell((j + 1).ToString(), Color.Black, tr);
                                for (int i = 0; i < órarend.ÓrákAHét.GetLength(0); i++)
                                    addCell(órarend.ÓrákAHét[i, j] != null ? órarend.ÓrákAHét[i, j].EgyediNév : "", Color.Black, tr, true, new int[2] { i, j });
                                table.AddView(tr, new ViewGroup.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent));
                            }
                        }
                    }
                    bar.Visibility = ViewStates.Gone;
                    Toast.MakeText(this, "Órarend és osztálylista frissítve", ToastLength.Long);
                });
            });
        }

        private TextView selected;
        /// <summary>
        /// Kiválasztja az adott órát
        /// </summary>
        private void ÓraClick(object sender, EventArgs e)
        {
            var tv = (TextView)sender;
            var ij = (int[])tv.Tag;
            Óra óra = órarend.ÓrákAHét[ij[0], ij[1]];
            if (óra == null)
                return;
            if (selected != null && selected != sender)
                selected.SetBackgroundResource(Resource.Drawable.cell_shape_light);
            tv.SetBackgroundResource(Resource.Drawable.cell_shape_selected_light);
            selected = tv;
            FindViewById<TextView>(Resource.Id.pozTV).Text = ((TextView)((TableRow)FindViewById<TableLayout>(Resource.Id.tableLayout1).GetChildAt(1)).GetChildAt(ij[0] + 1)).Text + " " + (ij[1] + 1) + ". óra";
            FindViewById<TextView>(Resource.Id.pozTV).Visibility = ViewStates.Visible;
            FindViewById<TextView>(Resource.Id.nevTV).Text = óra.TeljesNév;
            FindViewById<TextView>(Resource.Id.nevTV).Visibility = ViewStates.Visible;
            FindViewById<TextView>(Resource.Id.teremTV).Text = óra.Terem;
            FindViewById<TextView>(Resource.Id.teremTV).Visibility = ViewStates.Visible;
            FindViewById<TextView>(Resource.Id.tanarTV).Text = óra.Tanár.Név;
            FindViewById<TextView>(Resource.Id.tanarTV).Visibility = ViewStates.Visible;
            FindViewById<TextView>(Resource.Id.idoTV).Text = órarend.Órakezdetek[ij[1]].ToString("hh\\:mm") + "-" + órarend.Órakezdetek[ij[1]].Add(new TimeSpan(0, 45, 0)).ToString("hh\\:mm");
            FindViewById<TextView>(Resource.Id.idoTV).Visibility = ViewStates.Visible;
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.main_menu_light, menu);
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
                case Resource.Id.menu_add: //TODO
                    break;
                case Resource.Id.menu_edit: //TODO
                    break;
                case Resource.Id.menu_preferences: //TODO
                    break;
                case Resource.Id.menu_fullrefresh: //TODO: Megjelenítés frissítése
                    { //TODO: Beállítások mentése
                        ÓrarendFrissítés();
                        break;
                    }
            }
            return base.OnOptionsItemSelected(item);
        }

        private void Hiba(string msg)
        {
            new AlertDialog.Builder(this).SetMessage(msg).SetNeutralButton("OK", (s, e) => { ((AlertDialog)s).Dismiss(); ((AlertDialog)s).Dispose(); }).SetTitle("Hiba").Show();
        }

        /// <summary>
        /// Az összes hibát kiírja, ami a <see cref="Task"/> futása közben keletkezett
        /// </summary>
        /// <param name="t"></param>
        /// <returns>Igaz, ha nem volt hiba</returns>
        private bool TaskHiba(Task t)
        {
            bool ret = true;
            foreach (var ex in (IEnumerable<System.Exception>)t.Exception?.InnerExceptions ?? new System.Exception[0])
            {
                Hiba(ex.ToString());
                ret = false;
            }
            return ret;
        }

        private void CsengőTimer(object state)
        {
            handler.Post(() =>
            {
                if (órarend == null)
                    return;
                var kezdveg = FindViewById<TextView>(Resource.Id.kezdvegTV);
                //var most = DateTime.Now - DateTime.Today;
                var most = new TimeSpan(10, 0, 0) + (DateTime.Now - DateTime.Today - new TimeSpan(22, 0, 0));
                bool talált = false;
                var kovora = FindViewById<TextView>(Resource.Id.kovoraTV);
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
                        {
                            if (órarend.Órakezdetek[i] == TimeSpan.Zero)
                            { //Még nincsenek beállítva a kezdetek
                                kezdveg.Text = "Betöltés";
                                talált = true;
                                break;
                            }
                            continue;
                        }
                    }
                    else
                    {
                        kezdveg.Text = "Becsengetés: " + (órarend.Órakezdetek[i] - most).ToString("hh\\:mm\\:ss");
                        talált = true;
                        kovora.Visibility = ViewStates.Invisible;
                    }
                    int x = (int)DateTime.Today.DayOfWeek - 1;
                    var óra = órarend.ÓrákAHét[x, i];
                    if (x < 6 && óra != null)
                    {
                        kovora.Text = "Következő óra: " + óra.EgyediNév + "\n" + óra.Terem + "\n" + óra.Tanár.Név+"\n"+"ASD";
                        kovora.Visibility = ViewStates.Visible;
                    }
                    else
                        kovora.Visibility = ViewStates.Invisible;
                    break;
                }
                if (!talált)
                {
                    kezdveg.Text = "Nincs több óra ma";
                    kovora.Visibility = ViewStates.Invisible;
                }
                kezdveg.Visibility = ViewStates.Visible;
            }); //TODO: Az egészet függőlegesen görgethetővé tenni
        }
    }
}
