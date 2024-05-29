using Android.App;
using Android.OS;
using Android.Runtime;
using Android.Widget;
using Android.Views;
using Android.Net;
using System.Net;
using Stream = Android.Media.Stream;
using Android.Util;
using Android.Content;
using Plugin.Settings.Abstractions;
using System.Linq;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using static Android.App.ActionBar;
using Microsoft.AppCenter.Distribute;
using Uri = System.Uri;
using System.Threading.Tasks;
using AlertDialog = Android.App.AlertDialog;
using Square.Picasso;
using Aspose.Words.Tables;
using System.Diagnostics;
using AndroidX.AppCompat.App;
using Android;
using TrendNET.WMS.Device.Services;
using WMS.App;
using WMS.Background;
using AndroidX.AppCompat.App;
using FFImageLoading;
using Android.Graphics.Drawables;
using Android.Graphics;
using Android.Preferences;
using Newtonsoft.Json;
using Xamarin.Essentials;
using AndroidX.Core.Content;
using System.Net.Http;
using AndroidX.Core.App;
using Google.Android.Material.Snackbar;
using Android.Content.PM;
namespace WMS
{

    [Activity(Label = "WMS", Theme = "@style/AppTheme", MainLauncher = true, Icon = "@drawable/barcode", NoHistory = true)]
    public class MainActivity : CustomBaseActivity
    {
        private Dialog popupDialog;
        public static bool isValid;
        private EditText Password;
        public static ProgressBar progressBar1;
        private Button ok;
        private EditText rootURL;
        private EditText ID;
        private ImageView img;
        private TextView? txtVersion;
        private LinearLayout? chSlovenian;
        private LinearLayout? chEnglish;
        private ImageView? imgSlovenian;
        private ImageView? imgEnglish;
        private TextView deviceURL;
        private bool tablet = settings.tablet;
        private Button btnOkRestart;
        private ListView? cbLanguage;
        private List<LanguageItem> mLanguageItems;
        private LanguageAdapter mAdapter;
        private ColorMatrixColorFilter highlightFilter;
        private static readonly HttpClient httpClient = new HttpClient();
        const int RequestPermissionsId = 0;
        bool permissionsGranted = false;

        public object MenuInflaterFinal { get; private set; }

        public bool IsOnline()
        {
            var cm = (ConnectivityManager)GetSystemService(ConnectivityService);
            return cm.ActiveNetworkInfo == null ? false : cm.ActiveNetworkInfo.IsConnected;
        }


        private void ProcessRegistration()
        {
     

            var id = settings.ID.ToString();
            string result;


            if (WebApp.Get("mode=deviceActive", out result))
            {
                if (result != "Active!")
                {
                    Toast.MakeText(this, $"{Resources.GetString(Resource.String.s294)}", ToastLength.Long).Show();
                    return;
                }

                var inactivity = new Intent(this, typeof(Inactivity));
                StartService(inactivity);
                if (IsOnline())
                {
                    if (string.IsNullOrEmpty(Password.Text.Trim())) { return; }

                    Services.ClearUserInfo();
                    string error;
                    bool valid = false;

                    try
                    {
                        valid = Services.IsValidUser(Password.Text.Trim(), out error);
                    }
                    catch (Exception err)
                    {

                        SentrySdk.CaptureException(err);
                        return;

                    }
                    if (valid)
                    {
                        if (Services.HasPermission("TNET_WMS", "R"))
                        {

                            StartActivity(typeof(MainMenu));                            
                            Password.Text = "";
                            isValid = true;
                            Finish();
                        }
                        else
                        {
                            Password.Text = "";
                            isValid = false;
                            string toast = new string($"{Resources.GetString(Resource.String.s295)}");
                            Toast.MakeText(this, toast, ToastLength.Long).Show();
                            progressBar1.Visibility = ViewStates.Invisible;
                        }
                    }
                    else
                    {
                        Password.Text = "";
                        isValid = false;
                        string toast = new string($"{Resources.GetString(Resource.String.s296)}");
                        Toast.MakeText(this, toast, ToastLength.Long).Show();
                        progressBar1.Visibility = ViewStates.Invisible;
                    }
                }
                else
                {
                    // Is connected 
                    string toast = new string($"{Resources.GetString(Resource.String.s297)}");
                    Toast.MakeText(this, toast, ToastLength.Long).Show();
                    progressBar1.Visibility = ViewStates.Invisible;

                }
            }
        }




        protected async override void OnCreate(Bundle savedInstanceState)
        {
            settings.restart = false;
            Distribute.SetEnabledAsync(true);
            AppCenter.Start("ec2ca4ce-9e86-4620-9e90-6ecc5cda0e0e",
            typeof(Distribute));
    
            ChangeTheOrientation();
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            Distribute.ReleaseAvailable = OnReleaseAvailable;
            Password = FindViewById<EditText>(Resource.Id.tbPassword);
            Password.InputType = Android.Text.InputTypes.NumberVariationPassword |
            Android.Text.InputTypes.ClassNumber;
            progressBar1 = FindViewById<ProgressBar>(Resource.Id.progressBar1);
            img = FindViewById<ImageView>(Resource.Id.imglogo);
            txtVersion = FindViewById<TextView>(Resource.Id.txtVersion);
            chSlovenian = FindViewById<LinearLayout>(Resource.Id.chSlovenian);
            chEnglish = FindViewById<LinearLayout>(Resource.Id.chSlovenian);
            imgSlovenian = FindViewById<ImageView>(Resource.Id.imgSlovenian);
            imgEnglish = FindViewById<ImageView>(Resource.Id.imgEnglish);
            SetUpLanguages();
            GetLogo();
            var _broadcastReceiver = new NetworkStatusBroadcastReceiver();
            _broadcastReceiver.ConnectionStatusChanged += OnNetworkStatusChanged;
            Application.Context.RegisterReceiver(_broadcastReceiver,
            new IntentFilter(ConnectivityManager.ConnectivityAction));
            Button btnRegistrationEvent = FindViewById<Button>(Resource.Id.btnRegistrationClick);
            btnRegistrationEvent.Clickable = true;
            btnRegistrationEvent.Enabled = true;
            btnRegistrationEvent.Click += BtnRegistrationEvent_Click;
            settings.login = false;

            InitializeSentryAsync();

            // Check and request necessary permissions at startup because of Google Play policies. 29.05.2024 Janko Jovi�i�
            RequestNecessaryPermissions();
        }

        void RequestNecessaryPermissions()
        {
            string[] requiredPermissions = new string[]
            {
                Manifest.Permission.AccessCoarseLocation,
                Manifest.Permission.WriteExternalStorage,
                Manifest.Permission.ReadExternalStorage
            };

            CheckAndRequestPermissions(requiredPermissions);
        }

        void CheckAndRequestPermissions(string[] permissions)
        {
            var permissionsToRequest = new List<string>();

            foreach (var permission in permissions)
            {
                if (ContextCompat.CheckSelfPermission(this, permission) != (int)Permission.Granted)
                {
                    permissionsToRequest.Add(permission);
                }
            }

            if (permissionsToRequest.Count > 0)
            {
                ActivityCompat.RequestPermissions(this, permissionsToRequest.ToArray(), RequestPermissionsId);
            }

            else
            {
                // All permissions are already granted
                OnAllPermissionsGranted();
            }
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            if (requestCode == RequestPermissionsId)
            {
                bool allGranted = grantResults.All(result => result == Permission.Granted);

                if (allGranted)
                {
                    OnAllPermissionsGranted();
                }
                else
                {
                    // Handle the case where permissions are not granted
                    OnPermissionsDenied();
                }
            }

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        void OnAllPermissionsGranted()
        {
            // All necessary permissions are granted, proceed with the app's functionality
            permissionsGranted = true;
        }

        void OnPermissionsDenied()
        {
            // Inform the user that not all permissions were granted and the app might not work properly
            Snackbar.Make(FindViewById(Android.Resource.Id.Content), "Permissions denied. The app may not function correctly.", Snackbar.LengthIndefinite)
                .SetAction("Settings", v =>
                {
                    // Open app settings
                    var intent = new Android.Content.Intent(Android.Provider.Settings.ActionApplicationDetailsSettings);
                    var uri = Android.Net.Uri.FromParts("package", PackageName, null);
                    intent.SetData(uri);
                    StartActivity(intent);
                })
                .Show();
        }

        public void InitializeSentryAsync()
        {     
            SentrySdk.Init(o =>
            {    
                // Tells which project in Sentry to send events to:
                o.Dsn = "https://4da007db4594a10f53ab292097e612f8@o4507304617836544.ingest.de.sentry.io/4507304993751120";               
            });      
        }

   

        public string GetAppVersion()
        {
            return AppInfo.VersionString;
        }

        private void SetUpLanguages()
        {
            ISharedPreferences sharedPreferences = PreferenceManager.GetDefaultSharedPreferences(Application.Context);
            ISharedPreferencesEditor editor = sharedPreferences.Edit();

            // Create a color matrix for the highlight effect
            float[] colorMatrixValues = {
                2, 0, 0, 0, 0, // Red
                0, 2, 0, 0, 0, // Green
                0, 0, 2, 0, 0, // Blue
                0, 0, 0, 1, 0  // Alpha
            };

            ColorMatrix colorMatrix = new ColorMatrix(colorMatrixValues);
            highlightFilter = new ColorMatrixColorFilter(colorMatrix);
            txtVersion.Text = "v." + GetAppVersion();
            var language = Resources.Configuration.Locale.Country;      
            
            if(language == "SI")
            {
                imgSlovenian.SetColorFilter(highlightFilter);
                Base.Store.language = "sl";
            }
            else if(language == "US")
            {
                imgEnglish.SetColorFilter(highlightFilter);
                Base.Store.language = "en";
            }
        }

        private void GetLogo()
        {
            try
            {
                var url = settings.RootURL + "/Services/Logo";
                // Load and set the image with Picasso
                Picasso.Get()
                    .Load(url)
                    .Into(img);
            }
            catch 
            {
              return;
            }
        }


        private void OnNetworkStatusChanged(object sender, EventArgs e)
        {
            if (IsOnline())
            {
                try
                {
                    LoaderManifest.LoaderManifestLoopStop(this);
                }
                catch (Exception err)
                {
                    SentrySdk.CaptureException(err);
                }
            }
            else
            {
                LoaderManifest.LoaderManifestLoop(this);
            }
        }
        public override bool DispatchKeyEvent(Android.Views.KeyEvent e)
        {
            if (e.KeyCode == Keycode.Enter) { BtnRegistrationEvent_Click(this, null); }
            return base.DispatchKeyEvent(e);
        }
        private void ChangeTheOrientation()
        {
            if (settings.tablet == true)
            {
                RequestedOrientation = Android.Content.PM.ScreenOrientation.Landscape;
            }
            else
            {
                RequestedOrientation = Android.Content.PM.ScreenOrientation.Portrait;
            }
        }

        private bool OnReleaseAvailable(ReleaseDetails releaseDetails)
        {
            try
            {
                string versionName = releaseDetails.ShortVersion;
                string versionCodeOrBuildNumber = releaseDetails.Version;
                string releaseNotes = releaseDetails.ReleaseNotes;
                Uri releaseNotesUrl = releaseDetails.ReleaseNotesUrl;
                var title = "Version " + versionName + " available!";
                popupDialog = new Dialog(this);
                popupDialog.SetContentView(Resource.Layout.update);
                popupDialog.Window.SetSoftInputMode(SoftInput.AdjustResize);
                popupDialog.Show();
                popupDialog.Window.SetLayout(LayoutParams.MatchParent, LayoutParams.WrapContent);
                popupDialog.Window.SetBackgroundDrawable(new ColorDrawable(Color.ParseColor("#081a45")));

                // Access Pop-up layout fields like below
                btnOkRestart = popupDialog.FindViewById<Button>(Resource.Id.btnOk);
                btnOkRestart.Click += BtnOk_Click;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            Distribute.NotifyUpdateAction(UpdateAction.Update);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.action_setting1:
                    {
                        Finish();
                        StartActivity(typeof(Settings));
                        HelpfulMethods.clearTheStack(this);
                        return true;
                    }

            }

            return base.OnOptionsItemSelected(item);
        }
        private void Listener_Click(object sender, EventArgs e)
        {
            StartActivity(typeof(Settings));
            HelpfulMethods.clearTheStack(this);
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Layout.popup_action, menu);
            return base.OnCreateOptionsMenu(menu);
        }

        private void BtnRegistrationEvent_Click(object sender, System.EventArgs e)
        {
            if (permissionsGranted)
            {
                progressBar1.Visibility = ViewStates.Visible;
                ProcessRegistration();
            }
        }

        public override bool OnKeyDown(Keycode keyCode, KeyEvent e)
        {
            switch (keyCode)
            {
                // in smart-phone
                case Keycode.Enter:
                    BtnRegistrationEvent_Click(this, null);
                    break;
                    // return true;
            }
            return base.OnKeyDown(keyCode, e);
        }


    }
}