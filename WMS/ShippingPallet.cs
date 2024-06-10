﻿using Android.Content;
using Android.Content.PM;
using Android.Media;
using Android.Net;
using Android.Views;
using TrendNET.WMS.Device.Services;
using WMS.App;
using AlertDialog = Android.App.AlertDialog;
namespace WMS
{
    [Activity(Label = "ShippingPallet", ScreenOrientation = Android.Content.PM.ScreenOrientation.Portrait)]
    public class ShippingPallet : CustomBaseActivity, IBarcodeResult
    {
        private EditText pallet;
        private EditText machine;
        private Button btConfirm;
        SoundPool soundPool;
        int soundPoolId;
        private BarCode2D_Receiver.Barcode2D barcode2D;
        public string ETpallet;
        public string ETmachine;

        private ProgressDialogClass progress;

        public void GetBarcode(string barcode)
        {
            if (pallet.HasFocus)
            {
                if (barcode != "Scan fail")
                {

                    pallet.Text = barcode;
                }
                else
                {
                    pallet.Text = "";
                }

            }
            else if (machine.HasFocus)
            {
                if (barcode != "Scan fail")
                {

                    machine.Text = barcode;
                }
                else
                {
                    machine.Text = "";
                }
            }

        }


        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetTheme(Resource.Style.AppTheme_NoActionBar);
            if (App.Settings.tablet)
            {
                base.RequestedOrientation = ScreenOrientation.Landscape;
                base.SetContentView(Resource.Layout.ShippingPalletTablet);
            }
            else
            {
                base.RequestedOrientation = ScreenOrientation.Portrait;
                base.SetContentView(Resource.Layout.ShippingPallet);
            }
            AndroidX.AppCompat.Widget.Toolbar toolbar = FindViewById<AndroidX.AppCompat.Widget.Toolbar>(Resource.Id.toolbar);
            var _customToolbar = new CustomToolbar(this, toolbar, Resource.Id.navIcon);
            _customToolbar.SetNavigationIcon(App.Settings.RootURL + "/Services/Logo");
            SetSupportActionBar(_customToolbar._toolbar);
            SupportActionBar.SetDisplayShowTitleEnabled(false);
            // Create your application here
            pallet = FindViewById<EditText>(Resource.Id.pallet);
            machine = FindViewById<EditText>(Resource.Id.machine);
            btConfirm = FindViewById<Button>(Resource.Id.btConfirm);

            btConfirm.Click += BtConfirm_Click;

            color();

            barcode2D = new BarCode2D_Receiver.Barcode2D(this, this);

            machine.RequestFocus();
            machine.FocusChange += Machine_FocusChange;

            var _broadcastReceiver = new NetworkStatusBroadcastReceiver();
            _broadcastReceiver.ConnectionStatusChanged += OnNetworkStatusChanged;
            Application.Context.RegisterReceiver(_broadcastReceiver,
            new IntentFilter(ConnectivityManager.ConnectivityAction));
        }
        public bool IsOnline()
        {
            var cm = (ConnectivityManager)GetSystemService(ConnectivityService);
            return cm.ActiveNetworkInfo == null ? false : cm.ActiveNetworkInfo.IsConnected;

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

        private void Machine_FocusChange(object sender, View.FocusChangeEventArgs e)
        {
            pallet.RequestFocus();
        }

        private void color()
        {
            pallet.SetBackgroundColor(Android.Graphics.Color.Aqua);
            machine.SetBackgroundColor(Android.Graphics.Color.Aqua);
        }


        private async Task FinishMethod()
        {

            ETpallet = pallet.Text;
            ETmachine = machine.Text;

            await Task.Run(async () =>
            {



                try
                {


                    RunOnUiThread(() =>
                    {
                        progress = new ProgressDialogClass();

                        progress.ShowDialogSync(this, $"{Resources.GetString(Resource.String.s308)}");
                    });

                    var (success, result) = await WebApp.GetAsync("mode=palMac&pal=" + ETpallet + "&mac=" + ETmachine, this);
                    if (success)
                    {
                        if (result == "OK")
                        {
                            RunOnUiThread(() =>
                            {


                                progress.StopDialogSync();
                                AlertDialog.Builder alert = new AlertDialog.Builder(this);
                                alert.SetTitle($"{Resources.GetString(Resource.String.s323)}");
                                alert.SetMessage($"{Resources.GetString(Resource.String.s324)}");

                                alert.SetPositiveButton("Ok", (senderAlert, args) =>
                                {
                                    alert.Dispose();
                                    System.Threading.Thread.Sleep(500);
                                    StartActivity(typeof(MainMenu));

                                });



                                Dialog dialog = alert.Create();
                                dialog.Show();
                            });

                        }
                        else
                        {
                            RunOnUiThread(() =>
                            {


                                progress.StopDialogSync();
                                AlertDialog.Builder alert = new AlertDialog.Builder(this);
                                alert.SetTitle($"{Resources.GetString(Resource.String.s265)}");
                                alert.SetMessage($"{Resources.GetString(Resource.String.s216)}" + result);

                                alert.SetPositiveButton("Ok", (senderAlert, args) =>
                                {
                                    alert.Dispose();
                                    System.Threading.Thread.Sleep(500);
                                    StartActivity(typeof(MainMenu));

                                });



                                Dialog dialog = alert.Create();
                                dialog.Show();
                            });
                        }
                    }
                    else
                    {
                        RunOnUiThread(() =>
                        {


                            progress.StopDialogSync();
                            AlertDialog.Builder alert = new AlertDialog.Builder(this);
                            alert.SetTitle($"{Resources.GetString(Resource.String.s265)}");
                            alert.SetMessage($"{Resources.GetString(Resource.String.s213)}");

                            alert.SetPositiveButton("Ok", (senderAlert, args) =>
                            {
                                alert.Dispose();
                                System.Threading.Thread.Sleep(500);
                                StartActivity(typeof(MainMenu));

                            });



                            Dialog dialog = alert.Create();
                            dialog.Show();
                        });



                    }
                }
                catch (Exception ex)
                {


                    RunOnUiThread(() =>
                    {


                        progress.StopDialogSync();
                        AlertDialog.Builder alert = new AlertDialog.Builder(this);
                        alert.SetTitle($"{Resources.GetString(Resource.String.s265)}");
                        alert.SetMessage($"{Resources.GetString(Resource.String.s216)}" + ex.Message);

                        alert.SetPositiveButton("Ok", (senderAlert, args) =>
                        {
                            alert.Dispose();
                            System.Threading.Thread.Sleep(500);
                            StartActivity(typeof(MainMenu));

                        });



                        Dialog dialog = alert.Create();
                        dialog.Show();
                    });
                }
                finally
                {
                    progress.StopDialogSync();
                }


            });
        }


        private async void BtConfirm_Click(object sender, EventArgs e)
        {
            await FinishMethod();


        }
    }
}
