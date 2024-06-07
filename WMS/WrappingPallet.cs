﻿using Android.App;
using Android.Content;
using Android.Media;
using Android.Net;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using BarCode2D_Receiver;

using WMS.App;
using Stream = Android.Media.Stream;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrendNET.WMS.Device.Services;

using AndroidX.AppCompat.App;
using AlertDialog = Android.App.AlertDialog;
using Android.Content.PM;
using Com.Rscja.Deviceapi;
namespace WMS
{
    [Activity(Label = "WrappingPallet")]
    public class WrappingPallet : CustomBaseActivity, IBarcodeResult
    {
        private EditText pallet;
        private Button btConfirm;
        SoundPool soundPool;
        int soundPoolId;
        private ProgressDialogClass progress;
        private BarCode2D_Receiver.Barcode2D barcode2D;


        public void GetBarcode(string barcode)
        {
            if (pallet.HasFocus)
            {
                if (barcode != "Scan fail")
                {
                    
                    pallet.Text = barcode;
                } else
                {
                    pallet.Text = "";
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
                base.SetContentView(Resource.Layout.WrappingPalletTablet);
            }
            else
            {
                base.RequestedOrientation = ScreenOrientation.Portrait;
                base.SetContentView(Resource.Layout.WrappingPallet);
            }
            AndroidX.AppCompat.Widget.Toolbar toolbar = FindViewById<AndroidX.AppCompat.Widget.Toolbar>(Resource.Id.toolbar);
            var _customToolbar = new CustomToolbar(this, toolbar, Resource.Id.navIcon);
            _customToolbar.SetNavigationIcon(App.Settings.RootURL + "/Services/Logo");
            SetSupportActionBar(_customToolbar._toolbar);
            SupportActionBar.SetDisplayShowTitleEnabled(false);
            // Create your application here
            pallet = FindViewById<EditText>(Resource.Id.pallet);
            btConfirm = FindViewById<Button>(Resource.Id.btConfirm);

            btConfirm.Click += BtConfirm_Click;
            color();
            barcode2D = new BarCode2D_Receiver.Barcode2D(this, this);
            pallet.RequestFocus();
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

        private void color()
        {
            pallet.SetBackgroundColor(Android.Graphics.Color.Aqua);
        }



        private async Task FinishMethod()
        {
            await Task.Run(() =>
            {
                RunOnUiThread(() =>
                {
                    progress = new ProgressDialogClass();

                    progress.ShowDialogSync(this, $"{Resources.GetString(Resource.String.s308)}");
                });
                string TextPallet = pallet.Text;
                string result;
                if (WebApp.Get("mode=palPck&pal=" + TextPallet, out result))
                {
                    if (result == "OK")
                    {
                        RunOnUiThread(() =>
                        {


                            progress.StopDialogSync();
                            AlertDialog.Builder alert = new AlertDialog.Builder(this);
                            alert.SetTitle($"{Resources.GetString(Resource.String.s323)}");
                            alert.SetMessage($"{Resources.GetString(Resource.String.s332)}");

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
            });
        }
        private async void BtConfirm_Click(object sender, EventArgs e)
        {
            await FinishMethod();
            //string TextPallet = pallet.Text;
            //string result;
            //if (WebApp.Get("mode=palPck&pal=" + TextPallet, out result))
            //{
            //    if (result == "OK")
            //    {
            //        Toast.MakeText(this, $"{Resources.GetString(Resource.String.s332)}", ToastLength.Long).Show();
            //    } else
            //    {
            //        Toast.MakeText(this, $"Napaka pri zavijanju palete. {result}", ToastLength.Long).Show();
            //    }
            //} else
            //{
            //    Toast.MakeText(this, $"Napaka pri dostopu do web aplikacije. {result}", ToastLength.Long).Show();
            //}

        }
    }
}