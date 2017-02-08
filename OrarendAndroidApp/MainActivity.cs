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
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.MainLayout);
            //ViewPager viewPager = FindViewById<ViewPager>(Resource.Id.viewpager);
            var table = FindViewById<TableLayout>(Resource.Id.tableLayout1);
            Action<string, Color, TableRow> addCell = (text, color, tr1) =>
                {
                    TextView textview = new TextView(this);
                    textview.SetText(text, TextView.BufferType.Normal);
                    textview.SetTextColor(color);
                    tr1.AddView(textview);
                };
            foreach (var óra in API.Órák(""))
            {
                TableRow tr1 = new TableRow(this);
                addCell(óra.Név + "\n" + óra.Tanár.Név + "\n" + óra.Terem, Color.White, tr1);
                table.AddView(tr1);
            }
        }
    }
}
