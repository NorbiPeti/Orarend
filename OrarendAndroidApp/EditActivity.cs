using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Orarend;
using Android.Graphics;
using Android.Preferences;
using System.Security;

namespace OrarendAndroidApp
{
    [Activity(Label = "AddActivity", Theme = "@android:style/Theme.Holo.Light")]
    public class EditActivity : ActivityBase
    {
        private bool add;
        private int index;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.EditLayout);
            Title = (add = Intent.Extras.GetBoolean("add")) ? "Hozzáadás" : "Szerkesztés";
            index = Intent.Extras.GetInt("index");
            var osztálySpinner = FindViewById<Spinner>(Resource.Id.osztálySpinner);
            if (API.Osztályok == null)
            {
                MainActivity.Hiba(this, "Az osztálylista üres! Válaszd a Teljes frissítést a menübõl.");
                Finish();
            }
            osztálySpinner.Adapter = new ArrayAdapter(this, Resource.Layout.simple_list_item_1, API.Osztályok);
            if (!add)
            {
                var órarend = API.Órarendek[index];
                FindViewById<EditText>(Resource.Id.névEditText).Text = órarend.Név;
                int ix = Array.IndexOf(API.Osztályok, órarend.Osztály);
                /*for (int i = 0; i < API.Osztályok.Length; i++)
                {
                    var o = API.Osztályok[i];
                }*/
                osztálySpinner.SetSelection(ix);
                FindViewById<EditText>(Resource.Id.csoportokEditText).Text = órarend.Csoportok.Aggregate((a, b) => a + " " + b);
            }
            else
                FindViewById<EditText>(Resource.Id.névEditText).Text = "Órarend";
            osztálySpinner.LayoutParameters = new TableRow.LayoutParams((osztálySpinner.Parent as View)?.Width - (osztálySpinner.Parent as ViewGroup)?.GetChildAt(0)?.Width ?? TableRow.LayoutParams.MatchParent, TableRow.LayoutParams.WrapContent); //TODO
            FindViewById<Button>(Resource.Id.saveButton).Click += SaveButtonClick;
            var deleteButton = FindViewById<Button>(Resource.Id.deleteButton);
            if (add)
                deleteButton.Visibility = ViewStates.Gone;
            else
            {
                deleteButton.SetBackgroundColor(Color.DarkRed);
                deleteButton.Click += DeleteButtonClick;
                Intent.Extras.PutBoolean("deleted", false);
            }
        }

        private void DeleteButtonClick(object sender, EventArgs e)
        {
            new AlertDialog.Builder(this).SetTitle("Törlés").SetMessage("Biztosan törlöd ezt az órarendet?")
                .SetPositiveButton("Igen", (s, ea) =>
        { //Törlés
            API.Órarendek.RemoveAt(index);
            var intent = new Intent(Intent);
            intent.PutExtra("deleted", true);
            ((AlertDialog)s).Dismiss();
            ((AlertDialog)s).Dispose();
            API.Mentés(OpenFileOutput(MainActivity.DATA_FILENAME, FileCreationMode.Private));
            SetResult(Result.Ok, intent);
            Finish();
        }).SetNegativeButton("Nem", (s, ea) =>
        {
            ((AlertDialog)s).Dismiss();
            ((AlertDialog)s).Dispose();
        }).Show();
        }

        private void SaveButtonClick(object sender, EventArgs e)
        {
            var név = FindViewById<EditText>(Resource.Id.névEditText).Text;
            var osztály = API.Osztályok[FindViewById<Spinner>(Resource.Id.osztálySpinner).SelectedItemPosition];
            var csoportok = FindViewById<EditText>(Resource.Id.csoportokEditText).Text;
            if (!add)
            {
                var órarend = API.Órarendek[index];
                órarend.Név = név;
                órarend.Osztály = osztály;
                órarend.Csoportok = csoportok.Split(' ');
            }
            else
                API.Órarendek.Add(new Órarend(név, osztály, csoportok));
            API.Mentés(OpenFileOutput(MainActivity.DATA_FILENAME, FileCreationMode.Private));
            SetResult(Result.Ok, Intent);
            Finish();
        }
    }
}