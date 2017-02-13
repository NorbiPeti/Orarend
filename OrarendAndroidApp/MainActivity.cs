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
        }

        private void HelyettesítésFrissítés()
        {
            var table = FindViewById<TableLayout>(Resource.Id.tableLayout1);
            Action<string, Color, TableRow> addCell = (text, color, tr1) =>
            {
                TextView textview = new TextView(this);
                textview.SetText(text, TextView.BufferType.Normal);
                textview.SetTextColor(color);
                tr1.AddView(textview);
            };
            API.HelyettesítésFrissítés().ContinueWith(t =>
            {
                handler.Post(() =>
                {
                    if (t.Exception?.InnerExceptions.Count > 0)
                    {
                        foreach (var ex in t.Exception.InnerExceptions)
                        {
                            TableRow tr = new TableRow(this);
                            addCell(ex.ToString(), Color.Red, tr);
                            table.AddView(tr);
                        }
                    }
                    else
                    {
                        for (int j = 0; j < órarend.ÓrákAHét.GetLength(1); j++)
                        {
                            TableRow tr = new TableRow(this);
                            for (int i = 0; i < órarend.ÓrákAHét.GetLength(0); i++)
                                    addCell(órarend.ÓrákAHét[i, j] != null ? órarend.ÓrákAHét[i, j].EgyediNév : "", Color.Aqua, tr);
                            table.AddView(tr);
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
    }
}
