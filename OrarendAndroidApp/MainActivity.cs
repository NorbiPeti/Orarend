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
            handler = new Handler();
            API.Frissítés().ContinueWith(t =>
            {
                handler.Post(() =>
                {
                    TaskHiba(t);
                });
                órarend = new Órarend("Teszt", API.Osztályok.First(), "gy1");
                API.Órarendek.Add(órarend);
                API.Frissítés().ContinueWith(tt => HelyettesítésFrissítés());
            });
        }

        private int a = 0; //TODO: TMP
        private void HelyettesítésFrissítés()
        {
            var table = FindViewById<TableLayout>(Resource.Id.tableLayout1);
            Action<string, Color, TableRow> addCell = (text, color, tr1) =>
            {
                TextView textview = new TextView(this);
                textview.SetText(text, TextView.BufferType.Normal);
                textview.SetTextColor(color);
                textview.SetPadding(10, 10, 10, 10);
                switch(a)
                {
                    case 0:
                        textview.SetBackgroundResource(Resource.Drawable.cell_shape_light);
                        a++;
                        break;
                    case 1:
                        textview.SetBackgroundResource(Resource.Drawable.cell_shape_selected_light);
                        a++;
                        break;
                    case 2:
                        textview.SetBackgroundResource(Resource.Drawable.cell_shape_removed_light);
                        a++;
                        break;
                    case 3:
                        textview.SetBackgroundResource(Resource.Drawable.cell_shape_added_light);
                        a++;
                        break;
                    default:
                        a = 0;
                        break;
                }
                tr1.AddView(textview);
            };
            API.HelyettesítésFrissítés().ContinueWith(t =>
            {
                handler.Post(() =>
                {
                    TaskHiba(t);
                    {
                        TableRow tr = new TableRow(this);
                        addCell("", Color.Black, tr);
                        addCell("Hétfő", Color.Black, tr);
                        addCell("Kedd", Color.Black, tr);
                        addCell("Szerda", Color.Black, tr);
                        addCell("Csütörtök", Color.Black, tr);
                        addCell("Péntek", Color.Black, tr);
                        addCell("Szombat", Color.Black, tr);
                        table.AddView(tr, new ViewGroup.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent));

                    }
                    if ((t.Exception?.InnerExceptions?.Count ?? 0) == 0)
                    {
                        for (int j = 0; j < órarend.ÓrákAHét.GetLength(1); j++)
                        {
                            TableRow tr = new TableRow(this);
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
                                    addCell(órarend.ÓrákAHét[i, j] != null ? órarend.ÓrákAHét[i, j].EgyediNév : "", Color.Black, tr);
                                table.AddView(tr, new ViewGroup.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent));
                            }
                        }
                    }
                });
            });
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
                        var bar = FindViewById<ProgressBar>(Resource.Id.progressBar1);
                        handler.Post(() => bar.Visibility = ViewStates.Visible);
                        API.HelyettesítésFrissítés().ContinueWith(t => //TODO: Megjelenítés frissítése
                        {
                            handler.Post(() => bar.Visibility = ViewStates.Gone);
                        });
                        break;
                    }
                case Resource.Id.menu_add: //TODO
                    break;
                case Resource.Id.menu_edit: //TODO
                    break;
                case Resource.Id.menu_preferences: //TODO
                    break;
                case Resource.Id.menu_fullrefresh:
                    {
                        var bar = FindViewById<ProgressBar>(Resource.Id.progressBar1);
                        handler.Post(() => bar.Visibility = ViewStates.Visible);
                        API.Frissítés().ContinueWith(t => //TODO: Megjelenítés frissítése
                        {
                            handler.Post(() => bar.Visibility = ViewStates.Gone);
                        });
                        break;
                    }
            }
            return base.OnOptionsItemSelected(item);
        }

        private void Hiba(string msg)
        {
            new AlertDialog.Builder(this).SetMessage(msg).SetNeutralButton("OK", (s, e) => { ((AlertDialog)s).Dismiss(); ((AlertDialog)s).Dispose(); }).SetTitle("Hiba").Show();
        }

        private void TaskHiba(Task t)
        {
            foreach (var ex in (IEnumerable<System.Exception>)t.Exception?.InnerExceptions ?? new System.Exception[0])
                Hiba(ex.ToString());
        }
    }
}
