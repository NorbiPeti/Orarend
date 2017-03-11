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

namespace OrarendAndroidApp
{
    [Activity(Label = "Beállítások", Theme = "@android:style/Theme.Holo.Light")]
    public class SettingsActivity : PreferenceActivity, ISharedPreferencesOnSharedPreferenceChangeListener
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
#pragma warning restore CS0618 // Type or member is obsolete
            PreferenceManager.SetDefaultValues(this, Resource.Xml.preferences, false);
        }

        public void OnSharedPreferenceChanged(ISharedPreferences sharedPreferences, string key)
        {
            if (key == "pref_theme")
                Recreate();
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
    }
}