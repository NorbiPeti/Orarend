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
            Title = (add = Intent.Extras.GetBoolean("add")) ? "Hozz�ad�s" : "Szerkeszt�s";
            index = Intent.Extras.GetInt("index");
            var oszt�lySpinner = FindViewById<Spinner>(Resource.Id.oszt�lySpinner);
            if (API.Oszt�lyok == null)
            {
                MainActivity.Hiba(this, "Az oszt�lylista �res! V�laszd a Teljes friss�t�st a men�b�l.");
                Finish();
            }
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
            else
                FindViewById<EditText>(Resource.Id.n�vEditText).Text = "�rarend";
            oszt�lySpinner.LayoutParameters = new TableRow.LayoutParams((oszt�lySpinner.Parent as View)?.Width - (oszt�lySpinner.Parent as ViewGroup)?.GetChildAt(0)?.Width ?? TableRow.LayoutParams.MatchParent, TableRow.LayoutParams.WrapContent); //TODO
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
            new AlertDialog.Builder(this).SetTitle("T�rl�s").SetMessage("Biztosan t�rl�d ezt az �rarendet?")
                .SetPositiveButton("Igen", (s, ea) =>
        { //T�rl�s
            API.�rarendek.RemoveAt(index);
            var intent = new Intent(Intent);
            intent.PutExtra("deleted", true);
            ((AlertDialog)s).Dismiss();
            ((AlertDialog)s).Dispose();
            API.Ment�s(OpenFileOutput(MainActivity.DATA_FILENAME, FileCreationMode.Private));
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
                API.�rarendek.Add(new �rarend(n�v, oszt�ly, csoportok));
            API.Ment�s(OpenFileOutput(MainActivity.DATA_FILENAME, FileCreationMode.Private));
            SetResult(Result.Ok, Intent);
            Finish();
        }
    }
}