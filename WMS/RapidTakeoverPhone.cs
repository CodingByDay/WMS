﻿using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Media;
using Android.Net;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using BarCode2D_Receiver;
using Microsoft.AppCenter.Crashes;
using WMS.App;
using Stream = Android.Media.Stream;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TrendNET.WMS.Core.Data;
using TrendNET.WMS.Device.App;
using TrendNET.WMS.Device.Services;
using WebApp = TrendNET.WMS.Device.Services.WebApp;

using AndroidX.AppCompat.App;using AlertDialog = Android.App.AlertDialog;namespace WMS
{
    [Activity(Label = "RapidTakeoverPhone", ScreenOrientation = ScreenOrientation.Portrait)]
    public class RapidTakeoverPhone : AppCompatActivity, IBarcodeResult
    {
        private EditText tbSSCC;
        private Spinner cbWarehouses;
        private EditText tbLocation;
        private Button btConfirm;
        private Button btLogout;
        private EditText tbIdent;
        private List<ComboBoxItem> data = new List<ComboBoxItem>();
        private EditText tbReceiveLocation;
        private EditText tbRealStock;
        public static NameValueObject dataItem;
        private NameValueObject moveHead = (NameValueObject)InUseObjects.Get("MoveHead");
        private NameValueObject moveItem = (NameValueObject)InUseObjects.Get("MoveItem");
        private NameValueObject moveItemFinal;
        private ListView listData;
        private int selected;
        private int tempLocation;
        SoundPool soundPool;
        int soundPoolId;
        public void GetBarcode(string barcode)
        {
            if(tbSSCC.HasFocus)
            {
                Sound();
                tbSSCC.Text = barcode;
                ProcessSSCC();
            } else if (tbLocation.HasFocus)
            {
                tbLocation.Text = barcode;
            } else if(tbIdent.HasFocus)
            {             
                Sound();
                tbIdent.Text = barcode;
                // ProcessIdent();                
            }
        }




        public override bool OnKeyDown(Keycode keyCode, KeyEvent e)
        {
            switch (keyCode)
            {
                // In smartphone.  
                case Keycode.F2:
                    BtConfirm_Click(this, null);
                    break;
                // Return true;

                case Keycode.F8:
                    BtLogout_Click(this, null);
                    break;
            }
            return base.OnKeyDown(keyCode, e);
        }



        private void Sound()
        {
            soundPool.Play(soundPoolId, 1, 1, 0, 0, 1);
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {

            base.OnCreate(savedInstanceState);
            SetTheme(Resource.Style.AppTheme_NoActionBar);
            SetContentView(Resource.Layout.RapidTakeoverPhone);
            AndroidX.AppCompat.Widget.Toolbar toolbar = FindViewById<AndroidX.AppCompat.Widget.Toolbar>(Resource.Id.toolbar);
            var _customToolbar = new CustomToolbar(this, toolbar, Resource.Id.navIcon);
            _customToolbar.SetNavigationIcon(settings.RootURL + "/Services/Logo");
            SetSupportActionBar(_customToolbar._toolbar);
            SupportActionBar.SetDisplayShowTitleEnabled(false);
            soundPool = new SoundPool(10, Stream.Music, 0);
            soundPoolId = soundPool.Load(this, Resource.Raw.beep, 1);
            Barcode2D barcode2D = new Barcode2D();
            barcode2D.open(this, this);
            tbIdent = FindViewById<EditText>(Resource.Id.tbIdent);
            // Create your application here
            tbSSCC = FindViewById<EditText>(Resource.Id.tbSSCC);
            cbWarehouses = FindViewById<Spinner>(Resource.Id.cbWarehouses);
            tbLocation = FindViewById<EditText>(Resource.Id.tbLocation);
            btConfirm = FindViewById<Button>(Resource.Id.btConfirm);
            btLogout = FindViewById<Button>(Resource.Id.btLogout);
            tbReceiveLocation = FindViewById<EditText>(Resource.Id.tbReceiveLocation);
            tbRealStock = FindViewById<EditText>(Resource.Id.tbRealStock);
            btConfirm.Click += BtConfirm_Click;
            tbLocation.FocusChange += TbLocation_FocusChange;
            btLogout.Click += BtLogout_Click;
            listData = FindViewById<ListView>(Resource.Id.listData);
            color();


            var whs = CommonData.ListWarehouses();

            whs.Items.ForEach(wh =>
            {
                data.Add(new ComboBoxItem { ID = wh.GetString("Subject"), Text = wh.GetString("Name") });
            });

            var adapterWarehouse = new ArrayAdapter<ComboBoxItem>(this,
            Android.Resource.Layout.SimpleSpinnerItem, data);
            adapterWarehouse.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
            cbWarehouses.Adapter = adapterWarehouse;
            cbWarehouses.ItemSelected += CbWarehouses_ItemSelected;  
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
                    Crashes.TrackError(err);
                }
            }
            else
            {
                LoaderManifest.LoaderManifestLoop(this);
            }
        }

        private void BtLogout_Click(object sender, EventArgs e)
        {
            StartActivity(typeof(MainMenu));
            HelpfulMethods.clearTheStack(this);
        }

        private void BtConfirm_Click(object sender, EventArgs e)
        {

            if (saveHead())
            {
                try
                {
                    var headID = moveItem.GetInt("HeadID");
                    string result;
                    if (WebApp.Get("mode=finish&stock=add&print=" + Services.DeviceUser() + "&id=" + headID.ToString(), out result))
                    {
                        if (result.StartsWith("OK!"))
                        {
                            var id = result.Split('+')[1];
                            Toast.MakeText(this, "Zaključevanje uspešno! Št. prevzema:\r\n" + id, ToastLength.Long).Show();
                            AlertDialog.Builder alert = new AlertDialog.Builder(this);
                            alert.SetTitle("Zaključevanje uspešno");
                            alert.SetMessage("Zaključevanje uspešno! Št.prevzema:\r\n" + id);

                            alert.SetPositiveButton("Ok", (senderAlert, args) =>
                            {
                                alert.Dispose();
                            });



                            Dialog dialog = alert.Create();
                            dialog.Show();

                        }
                        else
                        {
                            Toast.MakeText(this, "Napaka pri zaključevanju: " + result, ToastLength.Long).Show();

                        }
                    }
                    else
                    {
                        Toast.MakeText(this, "Napaka pri klicu web aplikacije: " + result, ToastLength.Long).Show();

                    }
                }
                catch (Exception err)
                {

                    Crashes.TrackError(err);
                    return;

                }

            }
        }


        private bool saveHead()
        {
            var warehouse = data.ElementAt(tempLocation);
            if (!CommonData.IsValidLocation(warehouse.ID, tbLocation.Text.Trim()))
            {
                string WebError = string.Format("Prejemna lokacija" + tbLocation.Text.Trim() + "ni veljavna za sladišče:" + " " + data.ElementAt(tempLocation).Text + "!");
                Toast.MakeText(this, WebError, ToastLength.Long).Show();
                tbLocation.RequestFocus();
                return false;
            }
            else
            {

                {
                    string ssscError;
                    var data = Services.GetObject("sscc", tbSSCC.Text, out ssscError);
                    string error;


                    if (moveItem == null) { moveItem = new NameValueObject("MoveItem"); }

                
                    moveItem.SetInt("HeadID", data.GetInt("HeadID"));
                    moveItem.SetString("LinkKey", data.GetString("LinkKey"));
                    moveItem.SetInt("LinkNo", data.GetInt("LinkNo"));
                    moveItem.SetString("Ident", data.GetString("Ident"));
                    moveItem.SetString("SSCC", tbSSCC.Text.Trim());
                    moveItem.SetString("SerialNo", data.GetString("SerialNo"));
                    moveItem.SetDouble("Packing", data.GetDouble("Packing"));
                    moveItem.SetDouble("Factor", data.GetDouble("Factor"));
                    moveItem.SetDouble("Qty", data.GetDouble("Qty"));
                    moveItem.SetString("Location", tbLocation.Text.Trim());
                    moveItem.SetInt("Clerk", data.GetInt("Clerk"));

                    string error2;
                    moveItemFinal = Services.SetObject("mi", moveItem, out error2); /* Save move item method */

                    Toast.MakeText(this, moveItemFinal.ToString(), ToastLength.Long).Show();

                    InUseObjects.Invalidate("MoveItem");
                    return true;
                }
            }
        }

        private void TbLocation_FocusChange(object sender, View.FocusChangeEventArgs e)
        {
            ProcessSSCC();
        }

        private void CbWarehouses_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            tempLocation = e.Position;
        }

        private void ProcessSSCC()
        {
            var sscc = tbSSCC.Text.Trim();
            if (string.IsNullOrEmpty(sscc)) { return; }

            try
            {


                string error;
                dataItem = Services.GetObject("sscc", tbSSCC.Text, out error);
                if (dataItem == null)
                {
                    Toast.MakeText(this, "Napaka pri preverjanju sscc kode." + error, ToastLength.Long).Show();

                    tbSSCC.Text = "";

                }
                else
                {
                    tbIdent.Text = dataItem.GetString("Ident");
                    tbReceiveLocation.Text = dataItem.GetString("Location");
                    tbRealStock.Text = dataItem.GetDouble("RealStock").ToString();
                    colorLocation();
                }

            }
            catch (Exception err)
            {

                Crashes.TrackError(err);
                return;

            }

        }

 
        private void colorLocation()
        {
            tbLocation.SetBackgroundColor(Android.Graphics.Color.Aqua);
        }

        private void color()
        {

            tbSSCC.SetBackgroundColor(Android.Graphics.Color.Aqua);
        }
    }
}
