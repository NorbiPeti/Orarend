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
            Title = (add = Intent.Extras.GetBoolean("add")) ? "Hozz�ad�s" : "Szerkeszt�s";
            index = Intent.Extras.GetInt("index");
            var oszt�lySpinner = FindViewById<Spinner>(Resource.Id.oszt�lySpinner);
            oszt�lySpinner.Adapter = new ArrayAdapter(this, Resource.Layout.simple_list_item_1, API.Oszt�lyok);
            if (!add)
            {
                var �rarend = API.�rarendek[index];
                FindViewById<EditText>(Resource.Id.n�vEditText).Text = �rarend.N�v;
                int ix = Array.IndexOf(API.Oszt�lyok, �rarend.Oszt�ly);
                /*for (int i = 0; i < API.Oszt�lyok.Length; i++)
                {
                    var o = API.Oszt�lyok[i];
                }*/
                oszt�lySpinner.SetSelection(ix);
                FindViewById<EditText>(Resource.Id.csoportokEditText).Text = �rarend.Csoportok.Aggregate((a, b) => a + " " + b);
            }
            FindViewById<Button>(Resource.Id.saveButton).Click += SaveButtonClick;
        }

        private void SaveButtonClick(object sender, EventArgs e)
        {
            var n�v = FindViewById<EditText>(Resource.Id.n�vEditText).Text;
            var oszt�ly = API.Oszt�lyok[FindViewById<Spinner>(Resource.Id.oszt�lySpinner).SelectedItemPosition];
            var csoportok = FindViewById<EditText>(Resource.Id.csoportokEditText).Text;
            if (!add)
            {
                var �rarend = API.�rarendek[index];
                �rarend.N�v = n�v;
                �rarend.Oszt�ly = oszt�ly;
                �rarend.Csoportok = csoportok.Split(' ');
            }
            else
                API.�rarendek.Add(new �rarend(n�v, oszt�ly, csoportok)); //TODO: �rarend t�rl�se
            Finish();
        }
    }
}