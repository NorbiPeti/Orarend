using System;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Support.V4.View;
using Orarend;
using System.Linq;
using Android.Graphics;

namespace OrarendAndroidApp
{
    [Activity(Label = "OrarendAndroidApp", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        private Handler handler;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.MainLayout);
            handler = new Handler();
            var table = FindViewById<TableLayout>(Resource.Id.tableLayout1);
            Action<string, Color, TableRow> addCell = (text, color, tr1) =>
                {
                    TextView textview = new TextView(this);
                    textview.SetText(text, TextView.BufferType.Normal);
                    textview.SetTextColor(color);
                    tr1.AddView(textview);
                };
            API.Frissítés().ContinueWith(t =>
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
                        for (int j = 0; j < API.AktuálisÓrarend.ÓrákAHét.GetLength(1); j++)
                        {
                            TableRow tr = new TableRow(this);
                            for (int i = 0; i < API.AktuálisÓrarend.ÓrákAHét.GetLength(0); i++)
                                    addCell(API.AktuálisÓrarend.ÓrákAHét[i, j] != null ? API.AktuálisÓrarend.ÓrákAHét[i, j].Név : "", Color.Aqua, tr);
                            table.AddView(tr);
                        }
                    }
                });
            });
        }
    }
}
