﻿using Android.Content;
using Android.Content.PM;
using Android.Media;
using Android.Net;
using Android.Views;
using BarCode2D_Receiver;
using TrendNET.WMS.Core.Data;
using TrendNET.WMS.Device.App;
using TrendNET.WMS.Device.Services;
using WMS.App;
using AlertDialog = Android.App.AlertDialog;
using WebApp = TrendNET.WMS.Device.Services.WebApp;

namespace WMS
{
    [Activity(Label = "PackagingUnit", ScreenOrientation = ScreenOrientation.Portrait)]
    public class PackagingUnit : CustomBaseActivity, IBarcodeResult
    {

        private NameValueObject stock = null;
        private NameValueObject head = (NameValueObject)InUseObjects.Get("PackagingHead");
        private NameValueObject item = (NameValueObject)InUseObjects.Get("PackagingItem");
        private EditText tbIdent;
        private EditText tbIdentName;
        private EditText tbLocation;
        private EditText tbSSCC;
        private EditText tbSerialNo;
        private EditText tbQty;
        private Button btNegate;
        private Button btNew;
        private Button btList;
        private Button btFinish;
        private Button btExit;
        private Button check;
        SoundPool soundPool;
        int soundPoolId;
        private Barcode2D barcode2D;
        private TextView label;
        private ProgressDialogClass progress;



        public void GetBarcode(string barcode)
        {

            if (!string.IsNullOrEmpty(barcode))
            {

                if (tbSSCC.HasFocus)
                {

                    tbSSCC.Text = barcode;
                    tbSerialNo.RequestFocus();
                }

                else if (tbLocation.HasFocus)
                {

                    tbLocation.Text = barcode;
                    if (tbLocation.Text != "Scan fail")
                    {
                        tbSSCC.RequestFocus();
                        if (!tbSSCC.Enabled && !tbSerialNo.Enabled) { ProcessQty(); }
                    }
                    else
                    {
                        tbLocation.Text = "";
                        tbLocation.RequestFocus();
                    }


                }
                else if (tbIdent.HasFocus)
                {

                    tbIdent.Text = barcode;
                    if (tbIdent.Text != "Scan fail")
                    {
                        ProcessIdent();
                        tbLocation.RequestFocus();
                    }
                    else
                    {
                        tbIdent.Text = "";
                        tbIdent.RequestFocus();
                    }

                }
                else if (tbSerialNo.HasFocus)
                {


                    tbSerialNo.Text = barcode;
                    ProcessQty();
                }
            }




        }
        private void ProcessIdent()
        {
            if (!string.IsNullOrEmpty(tbIdent.Text))
            {
                var ident = CommonData.LoadIdent(tbIdent.Text.Trim());
                tbIdentName.Text = ident == null ? "" : ident.GetString("Name");
                tbSSCC.Enabled = ident == null ? false : ident.GetBool("isSSCC");
                tbSerialNo.Enabled = ident == null ? false : ident.GetBool("HasSerialNumber");
                color();
                tbLocation.RequestFocus();
            }
            else
            {
                tbIdent.Text = "";
            }
        }
        private bool HasData()
        {
            var ident = tbIdent.Text.Trim();
            var location = tbLocation.Text.Trim();
            var sscc = tbSSCC.Text.Trim();
            var serialNo = tbSerialNo.Text.Trim();

            if (string.IsNullOrEmpty(ident) && string.IsNullOrEmpty(location) && string.IsNullOrEmpty(sscc) && string.IsNullOrEmpty(serialNo)) { return false; }
            return true;
        }




        private void color()
        {
            if (tbSSCC.Enabled == true || tbSerialNo.Enabled == true)
            {
                tbSSCC.SetBackgroundColor(Android.Graphics.Color.Aqua);
                tbSerialNo.SetBackgroundColor(Android.Graphics.Color.Aqua);
                tbLocation.SetBackgroundColor(Android.Graphics.Color.Aqua);
            }
            else if (tbSSCC.Enabled == true)
            {
                tbSSCC.SetBackgroundColor(Android.Graphics.Color.Aqua);
                tbLocation.SetBackgroundColor(Android.Graphics.Color.Aqua);

            }
            else if (tbSerialNo.Enabled == true)
            {
                tbSerialNo.SetBackgroundColor(Android.Graphics.Color.Aqua);
                tbLocation.SetBackgroundColor(Android.Graphics.Color.Aqua);

            }



        }

        private bool ProcessData()
        {
            var ident = tbIdent.Text.Trim();
            var warehouse = head.GetString("Warehouse");
            var location = tbLocation.Text.Trim();
            var sscc = tbSSCC.Text.Trim();
            var serialNo = tbSerialNo.Text.Trim();

            if (string.IsNullOrEmpty(ident))
            {
                Toast.MakeText(this, $"{Resources.GetString(Resource.String.s270)}", ToastLength.Long).Show();

                return false;
            }

            if (!CommonData.IsValidLocation(warehouse, location))
            {
                Toast.MakeText(this, $"{Resources.GetString(Resource.String.s270)}", ToastLength.Long).Show();
                return false;
            }

            if (tbSSCC.Enabled && string.IsNullOrEmpty(sscc))
            {
                Toast.MakeText(this, $"{Resources.GetString(Resource.String.s270)}", ToastLength.Long).Show();
                return false;
            }

            if (tbSerialNo.Enabled && string.IsNullOrEmpty(serialNo))
            {
                Toast.MakeText(this, $"{Resources.GetString(Resource.String.s270)}", ToastLength.Long).Show();
                return false;
            }

            var qty = Convert.ToDouble(tbQty.Text.Trim());
            if (qty > stock.GetDouble("RealStock"))
            {
                Toast.MakeText(this, $"{Resources.GetString(Resource.String.s40)} (" + qty.ToString(CommonData.GetQtyPicture()) + ") presega zalogo (" + stock.GetDouble("RealStock").ToString(CommonData.GetQtyPicture()) + ")!", ToastLength.Long).Show();
                return false;
            }

            return true;
        }

        private bool SavePackagingItem()
        {
            if (!HasData()) { return true; }
            if (ProcessData())
            {
                if (item == null) { item = new NameValueObject("PackagingItem"); }

                item.SetInt("HeadID", head.GetInt("HeadID"));
                item.SetString("Ident", tbIdent.Text.Trim());
                item.SetString("SerialNo", tbSerialNo.Text.Trim());
                item.SetString("SSCC", tbSSCC.Text.Trim());
                item.SetDouble("Qty", Convert.ToDouble(tbQty.Text.Trim()));
                item.SetString("Location", tbLocation.Text.Trim());
                item.SetInt("Clerk", Services.UserID());

                string error;
                item = Services.SetObject("pi", item, out error);
                if (item != null)
                {
                    return true;
                }
                else
                {
                    Toast.MakeText(this, $"{Resources.GetString(Resource.String.s213)}" + error, ToastLength.Long).Show();
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        private void ProcessQty()
        {
            var ident = tbIdent.Text.Trim();
            var warehouse = head.GetString("Warehouse");
            var location = tbLocation.Text.Trim();
            var sscc = tbSSCC.Text.Trim();
            var serialNo = tbSerialNo.Text.Trim();

            if (string.IsNullOrEmpty(ident))
            {
                Toast.MakeText(this, $"{Resources.GetString(Resource.String.s270)}", ToastLength.Long).Show();
                return;
            }

            if (!CommonData.IsValidLocation(warehouse, location))
            {
                Toast.MakeText(this, $"{Resources.GetString(Resource.String.s270)}", ToastLength.Long).Show();
                return;
            }

            if (tbSSCC.Enabled && string.IsNullOrEmpty(sscc))
            {
                Toast.MakeText(this, $"{Resources.GetString(Resource.String.s270)}", ToastLength.Long).Show();
                return;
            }

            if (tbSerialNo.Enabled && string.IsNullOrEmpty(serialNo))
            {
                Toast.MakeText(this, $"{Resources.GetString(Resource.String.s270)}", ToastLength.Long).Show();
                return;
            }

            if (LoadStock(warehouse, location, sscc, serialNo, ident))
            {
                label.Text = $"{Resources.GetString(Resource.String.s40)} (" + stock.GetDouble("RealStock").ToString(CommonData.GetQtyPicture()) + "):";
                tbQty.Text = stock.GetDouble("RealStock").ToString(CommonData.GetQtyPicture());
            }
            else
            {
                label.Text = $"{Resources.GetString(Resource.String.s240)}";
                tbQty.Text = "";
            }
        }
        private bool LoadStock(string warehouse, string location, string sscc, string serialNum, string ident)
        {
            try
            {


                string error;
                stock = Services.GetObject("str", warehouse + "|" + location + "|" + sscc + "|" + serialNum + "|" + ident, out error);
                if (stock == null)
                {
                    Toast.MakeText(this, $"{Resources.GetString(Resource.String.s216)}", ToastLength.Long).Show();
                    return false;
                }

                return true;
            }
            catch (Exception err)
            {

                SentrySdk.CaptureException(err);
                return false;

            }
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetTheme(Resource.Style.AppTheme_NoActionBar);
            if (App.Settings.tablet)
            {
                base.RequestedOrientation = ScreenOrientation.Landscape;
                base.SetContentView(Resource.Layout.PackagingUnitTablet);
            }
            else
            {
                base.RequestedOrientation = ScreenOrientation.Portrait;
                base.SetContentView(Resource.Layout.PackagingUnit);
            }
            AndroidX.AppCompat.Widget.Toolbar toolbar = FindViewById<AndroidX.AppCompat.Widget.Toolbar>(Resource.Id.toolbar);
            var _customToolbar = new CustomToolbar(this, toolbar, Resource.Id.navIcon);
            _customToolbar.SetNavigationIcon(App.Settings.RootURL + "/Services/Logo");
            SetSupportActionBar(_customToolbar._toolbar);
            SupportActionBar.SetDisplayShowTitleEnabled(false);
            tbIdent = FindViewById<EditText>(Resource.Id.tbIdent);
            tbIdentName = FindViewById<EditText>(Resource.Id.tbIdentName);
            tbLocation = FindViewById<EditText>(Resource.Id.tbLocation);
            tbSSCC = FindViewById<EditText>(Resource.Id.tbSSCC);
            tbSerialNo = FindViewById<EditText>(Resource.Id.tbSerialNo);
            tbQty = FindViewById<EditText>(Resource.Id.tbQty);
            label = FindViewById<TextView>(Resource.Id.label);
            btNegate = FindViewById<Button>(Resource.Id.btNegate);
            btNew = FindViewById<Button>(Resource.Id.btNew);
            btList = FindViewById<Button>(Resource.Id.btList);
            btFinish = FindViewById<Button>(Resource.Id.btFinish);
            btExit = FindViewById<Button>(Resource.Id.btExit);

            tbQty.FocusChange += TbQty_FocusChange;

            btNew.Click += BtNew_Click;
            btList.Click += BtList_Click;
            btFinish.Click += BtFinish_Click;
            btExit.Click += BtExit_Click;
            btNegate.Click += BtNegate_Click;

            barcode2D = new Barcode2D(this, this);
            tbIdentName.FocusChange += TbIdentName_FocusChange;
            if (item != null)
            {
                tbIdent.Text = item.GetString("Ident");
                ProcessIdent();
                tbLocation.Text = item.GetString("Location");
                tbSSCC.Text = item.GetString("SSCC");
                tbSerialNo.Text = item.GetString("SerialNo");
                ProcessQty();
                tbQty.Text = item.GetDouble("Qty").ToString(CommonData.GetQtyPicture());
            }



            tbIdent.SetBackgroundColor(Android.Graphics.Color.Aqua);

            tbIdent.RequestFocus();

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

        private void TbQty_FocusChange(object sender, View.FocusChangeEventArgs e)
        {
            ProcessQty();
        }

        private void TbIdentName_FocusChange(object sender, View.FocusChangeEventArgs e)
        {
            ProcessIdent();
        }

        private void Check_Click(object sender, EventArgs e)
        {
            ProcessIdent();
            tbLocation.RequestFocus();

        }

        private void BtNegate_Click(object sender, EventArgs e)
        {
            var qty = tbQty.Text;
            if (qty.Trim().StartsWith("-"))
            {
                qty = qty.Trim().Substring(1);
            }
            else
            {
                qty = "-" + qty;
            }
            tbQty.Text = qty;
        }

        private void BtExit_Click(object sender, EventArgs e)
        {
            StartActivity(typeof(MainMenu));
            Finish();
        }
        private async Task FinishMethod()
        {

            await Task.Run(async () =>
            {
                if (SavePackagingItem())
                {

                    try
                    {

                        var headID = head.GetInt("HeadID");

                        var (success, result) = await WebApp.GetAsync("mode=finishPack&print=" + Services.DeviceUser() + "&id=" + headID.ToString(), this);
                        if (success)
                        {
                            if (result.StartsWith("OK!"))
                            {

                                var id = result.Split('+')[1];

                                Toast.MakeText(this, $"{Resources.GetString(Resource.String.s264)}" + id, ToastLength.Long).Show();
                                AlertDialog.Builder alert = new AlertDialog.Builder(this);
                                alert.SetTitle($"{Resources.GetString(Resource.String.s263)}");
                                alert.SetMessage($"{Resources.GetString(Resource.String.s264)}" + id);

                                alert.SetPositiveButton("Ok", (senderAlert, args) =>
                                {
                                    alert.Dispose();
                                    StartActivity(typeof(MainMenu));
                                    Finish();
                                });


                                Dialog dialog = alert.Create();
                                dialog.Show();

                            }
                            else
                            {
                                RunOnUiThread(() =>
                                {
                                    AlertDialog.Builder alert = new AlertDialog.Builder(this);
                                    alert.SetTitle($"{Resources.GetString(Resource.String.s265)}");
                                    alert.SetMessage($"{Resources.GetString(Resource.String.s266)}" + result);

                                    alert.SetPositiveButton("Ok", (senderAlert, args) =>
                                    {
                                        alert.Dispose();
                                    });

                                    Dialog dialog = alert.Create();
                                    dialog.Show();
                                });

                            }
                        }
                        else
                        {
                            Toast.MakeText(this, $"{Resources.GetString(Resource.String.s216)}" + result, ToastLength.Long).Show();
                        }
                    }
                    catch
                    {

                    }
                }
            });

        }
        private async void BtFinish_Click(object sender, EventArgs e)
        {
            await FinishMethod();


        }
        public override bool OnKeyDown(Keycode keyCode, KeyEvent e)
        {
            switch (keyCode)
            {
                // in smartphone
                case Keycode.F2:
                    if (btNew.Enabled == true)
                    {
                        BtNew_Click(this, null);
                    }
                    break;

                case Keycode.F3:
                    if (btList.Enabled == true)
                    {
                        BtList_Click(this, null);
                    }
                    break;
                case Keycode.F4:
                    if (btFinish.Enabled == true)
                    {
                        BtFinish_Click(this, null);
                    }
                    break;

                case Keycode.F8:
                    BtExit_Click(this, null);
                    break;

                case Keycode.F5:
                    if (check.Enabled == true)
                    {
                        Check_Click(this, null);
                    }
                    break;


            }
            return base.OnKeyDown(keyCode, e);
        }

        private void BtList_Click(object sender, EventArgs e)
        {
            if (SavePackagingItem())
            {
                InUseObjects.Set("PackagingItem", null);
                StartActivity(typeof(PackagingUnitList));
                Finish();

            }

        }

        private void BtNew_Click(object sender, EventArgs e)
        {
            if (SavePackagingItem())
            {
                InUseObjects.Set("PackagingItem", null);
                StartActivity(typeof(PackagingUnit));
                Finish();

            }
        }
    }
}