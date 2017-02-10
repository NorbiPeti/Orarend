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
            /*foreach (var óra in API.Órák(""))
            {
                TableRow tr1 = new TableRow(this);
                addCell(óra.Név + "\n" + óra.Tanár.Név + "\n" + óra.Terem, Color.White, tr1);
                table.AddView(tr1);
            }*/
            TableRow tr = new TableRow(this);
            /*API.Osztályok().ContinueWith(t =>
            {
                handler.Post(() =>
                {
                    if (t.Exception?.InnerExceptions.Count > 0)
                        foreach (var ex in t.Exception.InnerExceptions)
                            addCell(ex.ToString(), Color.Red, tr);
                    else
                        foreach (var osztály in t.Result)
                            addCell(osztály[0], Color.Aqua, tr);
                    table.AddView(tr);
                });
            });*/
            API.Frissítés();
        }
    }
}
