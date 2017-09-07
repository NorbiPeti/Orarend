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
using Android.Preferences;
using Orarend;

namespace OrarendAndroidApp
{
    [Activity(Label = "Beállítások", Theme = "@android:style/Theme.Holo.Light")]
    public class SettingsActivity : PreferenceActivity, ISharedPreferencesOnSharedPreferenceChangeListener, Preference.IOnPreferenceClickListener
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            ActivityBase.SetTheme(this);
            /*var settings = PreferenceManager.GetDefaultSharedPreferences(this);
            bool darktheme = settings.GetBoolean("pref_theme", false);
            //SetTheme(darktheme ? Android.Resource.Style.ThemeDeviceDefault : Android.Resource.Style.ThemeDeviceDefaultLight);
            SetTheme(darktheme ? Android.Resource.Style.ThemeHolo : Android.Resource.Style.ThemeHoloLight);*/
            base.OnCreate(savedInstanceState);
#pragma warning disable CS0618 // Type or member is obsolete
            AddPreferencesFromResource(Resource.Xml.preferences);
            FindPreference("pref_commonnames").OnPreferenceClickListener = this;
#pragma warning restore CS0618 // Type or member is obsolete
            PreferenceManager.SetDefaultValues(this, Resource.Xml.preferences, false);
        }

        private Intent intent;
        public void OnSharedPreferenceChanged(ISharedPreferences sharedPreferences, string key)
        {
            switch (key)
            {
                case "pref_theme":
                    Recreate();
                    break;
                case "pref_commonnames":
                    API.Beállítások.UseCommonNames();
                    Toast.MakeText(this, "Óranevek frissítve", ToastLength.Short).Show();
                    break;
                case "pref_offset":
                    API.Beállítások.ÓraOffset = sbyte.Parse(sharedPreferences.GetString(key, "0"));
                    intent = new Intent(Intent);
                    intent.PutExtra("offsetchanged", true);
                    SetResult(Result.Ok, intent);
                    break;
            }
        }

        protected override void OnResume()
        {
            base.OnResume();
            PreferenceManager.GetDefaultSharedPreferences(this).RegisterOnSharedPreferenceChangeListener(this);
        }

        protected override void OnPause()
        {
            base.OnPause();
            PreferenceManager.GetDefaultSharedPreferences(this).UnregisterOnSharedPreferenceChangeListener(this);
        }

        public override void OnBackPressed()
        {
            SetResult(Result.Ok, intent);
            //base.OnBackPressed();
            Finish();
        }

        public bool OnPreferenceClick(Preference preference)
        {
            if (preference.Key == "pref_commonnames")
            {
                API.Beállítások.UseCommonNames();
                Toast.MakeText(this, "Óranevek frissítve", ToastLength.Short).Show();
            }
            return true;
        }
    }
}