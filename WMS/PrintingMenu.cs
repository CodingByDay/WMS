﻿using Android.Content;
using Android.Content.PM;
using Android.Net;
using Android.Views;
using TrendNET.WMS.Device.Services;
using WMS.App;
using WMS.ExceptionStore;
namespace WMS
{
    [Activity(Label = "WMS")]
    public class PrintingMenu : CustomBaseActivity
    {
        public static string target = App.Settings.device;
        public bool result = Services.isTablet(target); /* Is the device tablet. */
        private Button button1;
        private Button button2;
        private Button button6;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            try
            {
                ChangeTheOrientation();
                base.OnCreate(savedInstanceState);
                SetTheme(Resource.Style.AppTheme_NoActionBar);

                if (App.Settings.tablet)
                {
                    base.RequestedOrientation = ScreenOrientation.Landscape;
                    base.SetContentView(Resource.Layout.PrintingMenuTablet);

                }
                else
                {
                    base.RequestedOrientation = ScreenOrientation.Portrait;
                    base.SetContentView(Resource.Layout.PrintingMenu);
                }
                AndroidX.AppCompat.Widget.Toolbar toolbar = FindViewById<AndroidX.AppCompat.Widget.Toolbar>(Resource.Id.toolbar);
                var _customToolbar = new CustomToolbar(this, toolbar, Resource.Id.navIcon);
                _customToolbar.SetNavigationIcon(App.Settings.RootURL + "/Services/Logo");
                SetSupportActionBar(_customToolbar._toolbar);
                SupportActionBar.SetDisplayShowTitleEnabled(false);
                button1 = FindViewById<Button>(Resource.Id.button1);
                button1.Click += Button1_Click;
                button2 = FindViewById<Button>(Resource.Id.button2);
                button2.Click += Button_Click;

                button6 = FindViewById<Button>(Resource.Id.button6);
                button6.Click += Button6_Click;

                var _broadcastReceiver = new NetworkStatusBroadcastReceiver();
                _broadcastReceiver.ConnectionStatusChanged += OnNetworkStatusChanged;
                Application.Context.RegisterReceiver(_broadcastReceiver,
                new IntentFilter(ConnectivityManager.ConnectivityAction), ReceiverFlags.NotExported);
            }
            catch (Exception ex)
            {
                GlobalExceptions.ReportGlobalException(ex);
            }
        }
        public bool IsOnline()
        {
            try
            {
                var cm = (ConnectivityManager)GetSystemService(ConnectivityService);
                return cm.ActiveNetworkInfo == null ? false : cm.ActiveNetworkInfo.IsConnected;
            }
            catch (Exception ex)
            {
                GlobalExceptions.ReportGlobalException(ex);
                return false;
            }
        }

        private void OnNetworkStatusChanged(object sender, EventArgs e)
        {
            try
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
            catch (Exception ex)
            {
                GlobalExceptions.ReportGlobalException(ex);
            }
        }


        private void ChangeTheOrientation()
        {
            try
            {
                if (App.Settings.tablet == true)
                {
                    base.RequestedOrientation = Android.Content.PM.ScreenOrientation.Landscape;
                }
                else
                {
                    base.RequestedOrientation = Android.Content.PM.ScreenOrientation.Portrait;

                }
            }
            catch (Exception ex)
            {
                GlobalExceptions.ReportGlobalException(ex);
            }
        }


        public override bool OnKeyDown(Keycode keyCode, KeyEvent e)
        {
            try
            {
                switch (keyCode)
                {
                    // In smartphone
                    case Keycode.F2:
                        Button1_Click(this, null);
                        break;
                    // Return true;

                    case Keycode.F3:
                        Button_Click(this, null);
                        break;


                    case Keycode.F4:
                        Button1_Click(this, null);
                        break;


                    case Keycode.F8:
                        Button6_Click(this, null);
                        break;

                        // return true;
                }
                return base.OnKeyDown(keyCode, e);
            }
            catch (Exception ex)
            {
                GlobalExceptions.ReportGlobalException(ex);
                return false;
            }
        }
        private void Button6_Click(object sender, EventArgs e)
        {
            try
            {
                StartActivity(typeof(MainMenu));
                Finish();
            }
            catch (Exception ex)
            {
                GlobalExceptions.ReportGlobalException(ex);
            }
        }




        private void Button_Click(object sender, EventArgs e)
        {
            try
            {
                StartActivity(typeof(PrintingSSCCCodes));
                Finish();
            }
            catch (Exception ex)
            {
                GlobalExceptions.ReportGlobalException(ex);
            }
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            try
            {
                StartActivity(typeof(PrintingReprintLabels));
                Finish();
            }
            catch (Exception ex)
            {
                GlobalExceptions.ReportGlobalException(ex);
            }
        }
    }
}