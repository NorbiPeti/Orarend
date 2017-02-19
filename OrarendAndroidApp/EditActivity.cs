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

namespace OrarendAndroidApp
{
    [Activity(Label = "AddActivity", Theme = "@android:style/Theme.Holo.Light")]
    public class EditActivity : Activity
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
            FindViewById<Button>(Resource.Id.saveButton).Click += SaveButtonClick;
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
                API.Órarendek.Add(new Órarend(név, osztály, csoportok)); //TODO: Órarend törlése
            Finish();
        }
    }
}