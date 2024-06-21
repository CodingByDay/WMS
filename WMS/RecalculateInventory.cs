﻿using Android.Content;
using Android.Content.PM;
using Android.Media;
using Android.Net;
using Android.Preferences;
using Android.Views;
using BarCode2D_Receiver;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using TrendNET.WMS.Device.Services;
using WMS.App;
using AlertDialog = Android.App.AlertDialog;
namespace WMS
{
    [Activity(Label = "RecalculateInventory")]
    public class RecalculateInventory : CustomBaseActivity, IBarcodeResult
    {

        private EditText ident;
        private Button btCalculate;
        SoundPool soundPool;
        int soundPoolId;
        private Barcode2D barcode2D;


        private List<string> identData = new List<string>();
        private List<string> returnList;
        private List<string> savedIdents;
        private CustomAutoCompleteTextView tbIdent;
        private CustomAutoCompleteAdapter<string> tbIdentAdapter;

        public void GetBarcode(string barcode)
        {
            if (ident.HasFocus)
            {

                ident.Text = barcode;

            }
        }
 


        protected  override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetTheme(Resource.Style.AppTheme_NoActionBar);
            // Create your application here
            if (App.Settings.tablet)
            {
                base.RequestedOrientation = ScreenOrientation.Landscape;
                base.SetContentView(Resource.Layout.RecalculateInventoryTablet);
            }
            else
            {
                base.RequestedOrientation = ScreenOrientation.Portrait;
                base.SetContentView(Resource.Layout.RecalculateInventory);
            }
            AndroidX.AppCompat.Widget.Toolbar toolbar = FindViewById<AndroidX.AppCompat.Widget.Toolbar>(Resource.Id.toolbar);
            var _customToolbar = new CustomToolbar(this, toolbar, Resource.Id.navIcon);
            _customToolbar.SetNavigationIcon(App.Settings.RootURL + "/Services/Logo");
            SetSupportActionBar(_customToolbar._toolbar);
            SupportActionBar.SetDisplayShowTitleEnabled(false);
            ident = FindViewById<EditText>(Resource.Id.ident);

            btCalculate = FindViewById<Button>(Resource.Id.btCalculate);

            barcode2D = new Barcode2D(this, this);

            color();



            btCalculate.Click += BtCalculate_Click;
            tbIdent = FindViewById<CustomAutoCompleteTextView>(Resource.Id.ident);

            ISharedPreferences sharedPreferences = PreferenceManager.GetDefaultSharedPreferences(Application.Context);
            ISharedPreferencesEditor editor = sharedPreferences.Edit();
            string savedIdentsJson = sharedPreferences.GetString("idents", "");
            if (!string.IsNullOrEmpty(savedIdentsJson))
            {
                savedIdents = JsonConvert.DeserializeObject<List<string>>(savedIdentsJson);
            }
            tbIdent.LongClick += ClearTheFields;
            tbIdentAdapter = new CustomAutoCompleteAdapter<string>(this, Android.Resource.Layout.SimpleDropDownItem1Line, new List<string>());
            tbIdent.Adapter = tbIdentAdapter;
            tbIdent.TextChanged += (sender, e) =>
            {
                string userInput = e.Text.ToString();
                UpdateSuggestions(userInput);
            };

            tbIdent.LongClick += ClearTheFields;
            var DataAdapter = new CustomAutoCompleteAdapter<string>(this,
            Android.Resource.Layout.SimpleSpinnerItem, identData);
            DataAdapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
            ident.LongClick += ClearTheFields;
            var _broadcastReceiver = new NetworkStatusBroadcastReceiver();
            _broadcastReceiver.ConnectionStatusChanged += OnNetworkStatusChanged;
            Application.Context.RegisterReceiver(_broadcastReceiver,
            new IntentFilter(ConnectivityManager.ConnectivityAction), ReceiverFlags.NotExported);
        }

        private void UpdateSuggestions(string userInput)
        {
            List<string> suggestions = GetCustomSuggestions(userInput);
            tbIdentAdapter.Clear();
            tbIdentAdapter.AddAll(suggestions);
            tbIdentAdapter.NotifyDataSetChanged();
        }

        private List<string> GetCustomSuggestions(string userInput)
        {
            if (savedIdents != null)
            {
                // In order to improve performance try to implement paralel processing. 23.05.2024 Janko Jovičić

                var lowerUserInput = userInput.ToLower();
                var result = new ConcurrentBag<string>();

                Parallel.ForEach(savedIdents, suggestion =>
                {
                    if (suggestion.ToLower().Contains(lowerUserInput))
                    {
                        result.Add(suggestion);
                    }
                });

                return result.Take(100).ToList();
            }

            // Service not yet loaded. 6.6.2024 J.J
            return new List<string>();
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



        private void ClearTheFields(object sender, View.LongClickEventArgs e)
        {
            ident.Text = "";
        }


        private async void BtCalculate_Click(object sender, EventArgs e)
        {
            var value = ident.Text;
            await FinishMethod(value);
        }

        private void color()
        {
            ident.SetBackgroundColor(Android.Graphics.Color.Aqua);
        }


        public override bool OnKeyDown(Keycode keyCode, KeyEvent e)
        {
            switch (keyCode)
            {
                // In smartphone.  
                case Keycode.F2:
                    if (btCalculate.Enabled == true)
                    {
                        BtCalculate_Click(this, null);
                    }
                    break;



            }
            return base.OnKeyDown(keyCode, e);
        }



        private async Task FinishMethod(string ident)
        {



            await Task.Run(async () =>
            {
                LoaderManifest.LoaderManifestLoopResources(this);


                try
                {


        


                    var (success, result) = await WebApp.GetAsync("mode=recalc&id=" + ident, this);
                    if (success)
                    {


                        if (result == "OK")
                        {
                            RunOnUiThread(() =>
                            {


                                AlertDialog.Builder alert = new AlertDialog.Builder(this);

                                alert.SetTitle($"{Resources.GetString(Resource.String.s263)}");


                                alert.SetMessage($"{Resources.GetString(Resource.String.s318)}");

                                alert.SetPositiveButton("Ok", (senderAlert, args) =>
                                {
                                    alert.Dispose();
                                    StartActivity(typeof(MainMenu));
                                    Finish();
                                });



                                Dialog dialog = alert.Create();
                                dialog.Show();
                            });

                        }
                        else
                        {
                            RunOnUiThread(() =>
                            {


                                AlertDialog.Builder alert = new AlertDialog.Builder(this);
                                alert.SetTitle($"{Resources.GetString(Resource.String.s265)}");
                                alert.SetMessage($"Napaka: {result}");

                                alert.SetPositiveButton("Ok", (senderAlert, args) =>
                                {
                                    alert.Dispose();
                                    StartActivity(typeof(MainMenu));
                                    Finish();

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


                            AlertDialog.Builder alert = new AlertDialog.Builder(this);
                            alert.SetTitle($"{Resources.GetString(Resource.String.s265)}");
                            alert.SetMessage($"{Resources.GetString(Resource.String.s213)}");

                            alert.SetPositiveButton("Ok", (senderAlert, args) =>
                            {
                                alert.Dispose();
                                StartActivity(typeof(MainMenu));
                                Finish();

                            });



                            Dialog dialog = alert.Create();
                            dialog.Show();
                        });



                    }
                }
                catch (Exception ex)
                {

                    SentrySdk.CaptureException(ex);

                    RunOnUiThread(() =>
                    {
                        AlertDialog.Builder alert = new AlertDialog.Builder(this);
                        alert.SetTitle($"{Resources.GetString(Resource.String.s265)}");
                        alert.SetMessage($"{Resources.GetString(Resource.String.s247)}" + ex.Message);
                        alert.SetPositiveButton("Ok", (senderAlert, args) =>
                        {
                            alert.Dispose();
                            StartActivity(typeof(MainMenu));
                            Finish();
                        });
                        Dialog dialog = alert.Create();
                        dialog.Show();
                    });
                } finally
                {
                    LoaderManifest.LoaderManifestLoopStop(this);
                }

            });
        }
    }
}