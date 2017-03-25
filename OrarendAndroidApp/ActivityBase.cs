
using Android.App;
using Android.OS;
using Android.Preferences;

namespace OrarendAndroidApp
{
    public class ActivityBase : Activity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetTheme(this);
        }

        public static void SetTheme(Activity activity)
        {
            var settings = PreferenceManager.GetDefaultSharedPreferences(activity);
            bool darktheme = settings.GetBoolean("pref_theme", false);
            if (activity is ActivityBase ab)
                ab.DarkTheme = darktheme;
            activity.SetTheme(darktheme ? Android.Resource.Style.ThemeDeviceDefault : Android.Resource.Style.ThemeDeviceDefaultLight);
        }

        public bool DarkTheme;
    }
}