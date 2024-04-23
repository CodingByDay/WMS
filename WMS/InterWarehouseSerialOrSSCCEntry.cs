﻿using Stream = Android.Media.Stream;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics.Drawables;
using Android.Media;
using Android.Net;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using BarCode2D_Receiver;
using Com.Barcode;
using Microsoft.AppCenter.Crashes;
using Newtonsoft.Json;

using WMS.App;
using TrendNET.WMS.Core.Data;
using TrendNET.WMS.Device.App;
using TrendNET.WMS.Device.Services;
using static Android.App.ActionBar;
using static Android.App.DownloadManager;
using WebApp = TrendNET.WMS.Device.Services.WebApp;

using AndroidX.AppCompat.App;
using AlertDialog = Android.App.AlertDialog;
using Android.Graphics;
using static AndroidX.ConstraintLayout.Widget.ConstraintSet;
using Org.Xml.Sax;

namespace WMS
{
    [Activity(Label = "InterWarehouseSerialOrSSCCEntry", ScreenOrientation = ScreenOrientation.Portrait)]
    public class InterWarehouseSerialOrSSCCEntry : CustomBaseActivity, IBarcodeResult
    {
        private EditText? tbIdent;
        private EditText? tbSSCC;
        private EditText? tbSerialNum;
        private EditText? tbIssueLocation;
        private EditText? tbLocation;
        private EditText? tbPacking;
        private TextView? lbQty;
        private ImageView? imagePNG;
        private EditText? lbIdentName;
        private Button? btSaveOrUpdate;
        private Button? btCreate;
        private Button? btFinish;
        private Button? btOverview;
        private Button? btExit;
        private SoundPool soundPool;
        private int soundPoolId;
        private LinearLayout ssccRow;
        private LinearLayout serialRow;
        private NameValueObject openIdent = (NameValueObject)InUseObjects.Get("OpenIdent");
        private NameValueObject openOrder = (NameValueObject)InUseObjects.Get("OpenOrder");
        private NameValueObject moveHead = (NameValueObject)InUseObjects.Get("MoveHead");
        private NameValueObject moveItem = (NameValueObject)InUseObjects.Get("MoveItem");
        private double? stock;
        private NameValueObject activityIdent;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetTheme(Resource.Style.AppTheme_NoActionBar);
            SetContentView(Resource.Layout.InterWarehouseSerialOrSSCCEntry);
            AndroidX.AppCompat.Widget.Toolbar toolbar = FindViewById<AndroidX.AppCompat.Widget.Toolbar>(Resource.Id.toolbar);
            var _customToolbar = new CustomToolbar(this, toolbar, Resource.Id.navIcon);
            _customToolbar.SetNavigationIcon(settings.RootURL + "/Services/Logo");
            SetSupportActionBar(_customToolbar._toolbar);
            SupportActionBar.SetDisplayShowTitleEnabled(false);
            Window.SetSoftInputMode(Android.Views.SoftInput.AdjustResize);

            btSaveOrUpdate = FindViewById<Button>(Resource.Id.btSaveOrUpdate);
            btCreate = FindViewById<Button>(Resource.Id.btCreate);
            btFinish = FindViewById<Button>(Resource.Id.btFinish);
            btOverview = FindViewById<Button>(Resource.Id.btOverview);
            btExit = FindViewById<Button>(Resource.Id.btExit);

            btSaveOrUpdate.Click += BtSaveOrUpdate_Click;
            btCreate.Click += BtCreate_Click;
            btFinish.Click += BtFinish_Click;
            btOverview.Click += BtOverview_Click;
            btExit.Click += BtExit_Click;

           

            tbIdent = FindViewById<EditText>(Resource.Id.tbIdent);
            tbSSCC = FindViewById<EditText>(Resource.Id.tbSSCC);
            tbSerialNum = FindViewById<EditText>(Resource.Id.tbSerialNum);
            tbIssueLocation = FindViewById<EditText>(Resource.Id.tbIssueLocation);
            tbLocation = FindViewById<EditText>(Resource.Id.tbLocation);
            tbPacking = FindViewById<EditText>(Resource.Id.tbPacking);
            tbSSCC.InputType = Android.Text.InputTypes.ClassNumber;
            lbQty = FindViewById<TextView>(Resource.Id.lbQty);
            imagePNG = FindViewById<ImageView>(Resource.Id.imagePNG);
            lbIdentName = FindViewById<EditText>(Resource.Id.lbIdentName);
            lbIdentName = FindViewById<EditText>(Resource.Id.lbIdentName);
            ssccRow = FindViewById<LinearLayout>(Resource.Id.sscc_row);
            serialRow = FindViewById<LinearLayout>(Resource.Id.serial_row);
            soundPool = new SoundPool(10, Android.Media.Stream.Music, 0);
            soundPoolId = soundPool.Load(this, Resource.Raw.beep, 1);
            Barcode2D barcode2D = new Barcode2D();
            barcode2D.open(this, this);
            tbLocation.FocusChange += TbLocation_FocusChange;
            var _broadcastReceiver = new NetworkStatusBroadcastReceiver();
            _broadcastReceiver.ConnectionStatusChanged += OnNetworkStatusChanged;
            Application.Context.RegisterReceiver(_broadcastReceiver,
            new IntentFilter(ConnectivityManager.ConnectivityAction));

            tbIdent.KeyPress += TbIdent_KeyPress;
            tbSSCC.KeyPress += TbSSCC_KeyPress;
            tbSerialNum.KeyPress += TbSerialNum_KeyPress;
            tbIssueLocation.KeyPress += TbIssueLocation_KeyPress;
            tbLocation.KeyPress += TbLocation_KeyPress;
            // Method calls

            CheckIfApplicationStopingException();

            // Color the fields that can be scanned
            ColorFields();

            SetUpProcessDependentButtons();

            // Main logic for the entry
            SetUpForm();
        }

        private void TbLocation_KeyPress(object? sender, View.KeyEventArgs e)
        {
            e.Handled = false;
        }

        private void TbIssueLocation_KeyPress(object? sender, View.KeyEventArgs e)
        {
            e.Handled = false;
        }

        private void TbSerialNum_KeyPress(object? sender, View.KeyEventArgs e)
        {
            e.Handled = false;
        }

        private async void TbSSCC_KeyPress(object? sender, View.KeyEventArgs e)
        {
            if (e.KeyCode == Keycode.Enter && e.Event.Action == KeyEventActions.Down)
            {
                await FillDataBySSCC(tbSSCC.Text);
            }
            e.Handled = false;
        }

        private void TbIdent_KeyPress(object? sender, View.KeyEventArgs e)
        {
            if (e.KeyCode == Keycode.Enter && e.Event.Action == KeyEventActions.Down)
            {
                ProcessIdent();
            }
            e.Handled = false;
        }

        private void BtExit_Click(object? sender, EventArgs e)
        {
            
        }

        private void BtOverview_Click(object? sender, EventArgs e)
        {
            
        }

        private void BtFinish_Click(object? sender, EventArgs e)
        {
            
        }

        private async void BtCreate_Click(object? sender, EventArgs e)
        {
            if (!Base.Store.isUpdate)
            {
                double parsed;
                if (double.TryParse(tbPacking.Text, out parsed) && stock >= parsed)
                {

                    var isCorrectLocation = IsLocationCorrect();
                    if (!isCorrectLocation)
                    {
                        // Nepravilna lokacija za izbrano skladišče
                        Toast.MakeText(this, $"{Resources.GetString(Resource.String.s333)}", ToastLength.Long).Show();
                        return;
                    }


                    await CreateMethodFromStart();
                }
            }
            else
            {

            }
        }


        private bool IsLocationCorrect()
        {
            string location = tbLocation.Text;

            if (CommonData.IsValidLocation(moveHead.GetString("Issuer"), location) && CommonData.IsValidLocation(moveHead.GetString("Receiver"), location))
            {
                return true;
            }
            else
            {
                return false;
            }
        }


        private async void BtSaveOrUpdate_Click(object? sender, EventArgs e)
        {
            if(activityIdent!=null)
            {
                if(activityIdent.GetBool("isSSCC")&&tbSSCC.Text!=string.Empty)
                {
                    await CreateMethodFromStart();
                }
                else
                {
                    tbSerialNum.Text = string.Empty;
                }
            }
        }

        private void CheckIfApplicationStopingException()
        {
            if(moveItem == null && moveHead == null)
            {
                // Destroy the activity
                Finish();
                StartActivity(typeof(MainMenu));
            }
        }


        private async Task CreateMethodFromStart()
        {
            await Task.Run(() =>
            {

                    moveItem = new NameValueObject("MoveItem");
                    moveItem.SetInt("HeadID", moveHead.GetInt("HeadID"));
                    moveItem.SetString("LinkKey", string.Empty);
                    moveItem.SetInt("LinkNo", 0);
                    moveItem.SetString("Ident", tbIdent.Text);
                    moveItem.SetString("SSCC", tbSSCC.Text.Trim());
                    moveItem.SetString("SerialNo", tbSerialNum.Text.Trim());
                    moveItem.SetDouble("Packing", Convert.ToDouble(tbPacking.Text.Trim()));
                    moveItem.SetDouble("Factor", 1);
                    moveItem.SetDouble("Qty", Convert.ToDouble(tbPacking.Text.Trim()));
                    moveItem.SetInt("Clerk", Services.UserID());
                    moveItem.SetString("Location", tbLocation.Text.Trim());
                    moveItem.SetString("IssueLocation", tbIssueLocation.Text.Trim());
                    moveItem.SetString("Palette", "1");

                    string error;

                    moveItem = Services.SetObject("mi", moveItem, out error);

                    if (moveItem != null && error == string.Empty)
                    {
                        RunOnUiThread(() =>
                        {
                            StartActivity(typeof(InterWarehouseSerialOrSSCCEntry));
                        });
                        
                    }                     
            });
        }




        private void SetUpForm()
        {
            // This is the default focus of the view.
            tbSSCC.RequestFocus();

            if (Base.Store.isUpdate)
            {

            }

            else
            {
               
                

            }


        }

        private void Sound()
        {
            soundPool.Play(soundPoolId, 1, 1, 0, 0, 1);
        }


        public async void GetBarcode(string barcode)
        {
            if (barcode != "Scan fail")
            {
                if (tbIdent.HasFocus)
                {
                    Sound();
                    tbIdent.Text = barcode;
                    ProcessIdent();
                    tbSSCC.RequestFocus();
                }
                else if (tbSSCC.HasFocus)
                {
                    Sound();
                    await FillDataBySSCC(barcode);         
                    tbSSCC.Text = barcode;
                }
                else if (tbSerialNum.HasFocus)
                {
                    Sound();
                    tbSerialNum.Text = barcode;
                    tbIssueLocation.RequestFocus();
                }
                else if (tbIssueLocation.HasFocus)
                {
                    Sound();
                    tbIssueLocation.Text = barcode;
                    tbLocation.RequestFocus();

                }
                else if (tbLocation.HasFocus)
                {
                    Sound();
                    tbLocation.Text = barcode;
                    tbPacking.RequestFocus();
                }
            }
     
        }

        private async Task FillDataBySSCC(string sscc)
        {
            var parameters = new List<Services.Parameter>();
            string warehouse = moveHead.GetString("Issuer");
            parameters.Add(new Services.Parameter { Name = "acSSCC", Type = "String", Value = sscc });
            parameters.Add(new Services.Parameter { Name = "acWarehouse", Type = "String", Value = warehouse });
            string sql = $"SELECT * FROM uWMSItemBySSCCWarehouse WHERE acSSCC = @acSSCC AND acWarehouse = @acWarehouse";
            var ssccResult = await AsyncServices.AsyncServices.GetObjectListBySqlAsync(sql, parameters);
            RunOnUiThread(() =>
            {
                if (ssccResult.Success && ssccResult.Rows.Count > 0)
                {
                    tbIdent.Text = ssccResult.Rows[0].StringValue("acIdent");
                    // Process ident, recommended location is processed as well. 23.04.2024 Janko Jovičić
                    ProcessIdent();
                    tbIssueLocation.Text = ssccResult.Rows[0].StringValue("aclocation");
                    tbSerialNum.Text = ssccResult.Rows[0].StringValue("acSerialNo");
                    tbSSCC.Text = ssccResult.Rows[0].StringValue("acSSCC").ToString();
                    tbPacking.RequestFocus();
                    tbPacking.SelectAll();

                    lbQty.Text = $"{Resources.GetString(Resource.String.s83)} ( " + ssccResult.Rows[0].DoubleValue("anQty").ToString() + " )";
                    tbPacking.Text = ssccResult.Rows[0].DoubleValue("anQty").ToString();
                    stock = ssccResult.Rows[0].DoubleValue("anQty");
                }
                else
                {
                    Toast.MakeText(this, Resources.GetString(Resource.String.s337), ToastLength.Long).Show();
                }
            });
        }


        private void ProcessIdent()
        {
            activityIdent = CommonData.LoadIdent(tbIdent.Text.Trim());

            if (activityIdent == null)
            {
                tbIdent.Text = "";
                lbIdentName.Text = "";
                return;
            }

            if (CommonData.GetSetting("IgnoreStockHistory") != "1")
            {
                try
                {
                    string error;
                    var recommededLocation = Services.GetObject("rl", activityIdent.GetString("Code") + "|" + moveHead.GetString("Receiver"), out error);
                    if (recommededLocation != null)
                    {
                        tbLocation.Text = recommededLocation.GetString("Location");
                    }
                }
                catch (Exception err)
                {

                    Crashes.TrackError(err);
                    return;

                }
            }
    
            lbIdentName.Text = activityIdent.GetString("Name");
            tbSSCC.Enabled = activityIdent.GetBool("isSSCC");
            tbSerialNum.Enabled = activityIdent.GetBool("HasSerialNumber");
        }


        private void ColorFields()
        {
            tbIdent.SetBackgroundColor(Android.Graphics.Color.Aqua);
            tbSSCC.SetBackgroundColor(Android.Graphics.Color.Aqua);
            tbSerialNum.SetBackgroundColor(Android.Graphics.Color.Aqua);
            tbLocation.SetBackgroundColor(Android.Graphics.Color.Aqua);
            tbIssueLocation.SetBackgroundColor(Android.Graphics.Color.Aqua);
        }
        private void SetUpProcessDependentButtons()
        {
            // This method changes the UI so it shows in a visible way that it is the update screen. - 18.03.2024
            if (Base.Store.isUpdate)
            {
                btSaveOrUpdate.Visibility = ViewStates.Gone;
                btCreate.Text = $"{Resources.GetString(Resource.String.s290)}";
            }

        }

        private void TbLocation_FocusChange(object? sender, View.FocusChangeEventArgs e)
        {
            
        }

        private void OnNetworkStatusChanged(object? sender, EventArgs e)
        {
            
        }


    }
}