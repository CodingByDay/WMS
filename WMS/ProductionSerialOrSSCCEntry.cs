﻿using Stream = Android.Media.Stream;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
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
using TrendNET.WMS.Core.Data;
using TrendNET.WMS.Device.App;
using TrendNET.WMS.Device.Services;
using static Android.App.ActionBar;
using WebApp = TrendNET.WMS.Device.Services.WebApp;

using AndroidX.AppCompat.App;using AlertDialog = Android.App.AlertDialog;namespace WMS
{
    [Activity(Label = "ProductionSerialOrSSCCEntry", ScreenOrientation = ScreenOrientation.Portrait)]
    public class ProductionSerialOrSSCCEntry : AppCompatActivity, IBarcodeResult
    {
        private NameValueObject moveHead = (NameValueObject)InUseObjects.Get("MoveHead");
        private NameValueObject moveItem = (NameValueObject)InUseObjects.Get("MoveItem");
        private NameValueObject openWorkOrder = null;
        private bool editMode = false;
        private EditText tbIdent;
        private EditText tbSSCC;
        private EditText tbSerialNum;
        private EditText tbLocation;
        private EditText tbPacking;
        private EditText tbUnits;
        private TextView lbQty;
        private Button btSaveOrUpdate;
        private Button button3;
        private Button button4;
        private Button button5;
        SoundPool soundPool;
        int soundPoolId;
        public override bool OnKeyDown(Keycode keyCode, KeyEvent e)
        {
            switch (keyCode)
            {
                
                case Keycode.F2:
                    if (btSaveOrUpdate.Enabled == true)
                    {
                        BtSaveOrUpdate_Click(this, null);
                    }
                    break;

                case Keycode.F3:
                    if (button3.Enabled == true)
                    {
                        Button3_Click(this, null);
                    }
                    break;

                case Keycode.F4://
                    if (button4.Enabled == true)
                    {
                        Button4_Click(this, null);
                    }
                    break;

                case Keycode.F8:
                    if (button5.Enabled == true)
                    {
                        Button5_Click(this, null);
                    }
                    break;

            }
            return base.OnKeyDown(keyCode, e);
        }
        public void GetBarcode(string barcode)
        {

            if (tbSSCC.HasFocus)
            {if (barcode != "Scan fail")
                {
                    tbSSCC.Text = "";
                    tbSerialNum.Text = "";
                    tbPacking.Text = "";
                    tbLocation.Text = "";
                    tbIdent.Text = "";
                    Sound();
                    tbSSCC.Text = barcode;
                    tbSerialNum.RequestFocus();
                    
                }
            } else if (tbSerialNum.HasFocus)
            {if (barcode != "Scan fail")
                {
                    Sound();
                    tbSerialNum.Text = barcode;
                    ProcessSerialNum();
                    tbLocation.RequestFocus();
                }
            } else if (tbLocation.HasFocus)
            {      
                Sound();
                tbLocation.Text = barcode;
            }
        }
        private static bool? checkWorkOrderOpenQty = null;

        private void Sound()
        {
            soundPool.Play(soundPoolId, 1, 1, 0, 0, 1);
        }


        private static bool? getWorkOrderDefaultQty = null;
        private ProgressDialogClass progress;
        private Dialog popupDialogConfirm;
        private Button btnYesConfirm;
        private Button btnNoConfirm;

        private void GetWorkOrderDefaultQty()
        {
            if (getWorkOrderDefaultQty == null)
            {

                try
                {
                    string error;
                    var useObj = Services.GetObject("wodqUse", "", out error);
                    getWorkOrderDefaultQty = useObj == null ? false : useObj.GetBool("Use");
                }
                catch (Exception err)
                {

                    Crashes.TrackError(err);
                    return;

                }
            }

            if ((bool)getWorkOrderDefaultQty)
            {

                try
                {
                    string error;
                    var qtyObj = Services.GetObject("wodq", openWorkOrder.GetString("Key") + "|" + openWorkOrder.GetString("Ident"), out error);
                    if (qtyObj != null)
                    {
                        var qty = qtyObj.GetDouble("DefaultQty");
                        if (qty < 0)
                        {
                            getWorkOrderDefaultQty = false;
                        }
                        else
                        {
                            tbPacking.Text = qty.ToString(CommonData.GetQtyPicture());
                        }
                    }
                }
                catch (Exception err)
                {

                    Crashes.TrackError(err);
                    return;

                }
            }
        }

        private async Task<bool> SaveMoveItem()
        {

            if (string.IsNullOrEmpty(tbPacking.Text.Trim()))
            {
                return true;
            }

            if (tbSSCC.Enabled && string.IsNullOrEmpty(tbSSCC.Text.Trim()))
            {
                RunOnUiThread(() =>
                {
                    string SuccessMessage = string.Format("SSCC koda je obvezen podatek.");
                    DialogHelper.ShowDialogError(this, this, SuccessMessage);

                    tbSSCC.RequestFocus();
                });
               
                return false;
            }

            if (tbSerialNum.Enabled && string.IsNullOrEmpty(tbSerialNum.Text.Trim()))
            {
                tbSerialNum.Text = GetNextSerialNum();
                if (string.IsNullOrEmpty(tbSerialNum.Text.Trim()))
                {
                    RunOnUiThread(() =>
                    {
                        string SuccessMessage = string.Format("Ni mogoče pridobiti serijske št.");
                        DialogHelper.ShowDialogError(this, this, SuccessMessage);
                        tbSerialNum.RequestFocus();
                    });
                   
                    return false;
                }
            }

            if (!CommonData.IsValidLocation(moveHead.GetString("Wharehouse"), tbLocation.Text.Trim()))
            {
                RunOnUiThread(() =>
                {
                    string SuccessMessage = string.Format("Lokacija '" + tbLocation.Text.Trim() + "' ni veljavna za skladišče '" + moveHead.GetString("Wharehouse") + "'!");
                    DialogHelper.ShowDialogError(this, this, SuccessMessage);
                    tbLocation.RequestFocus();
                });
             
                return false;
            }

            string error;       
            try
            {
             

                if (tbSSCC.Enabled)
                {
                    var stock = Services.GetObject("sts", tbSSCC.Text.Trim(), out error);
                    if (stock == null)
                    {
                        RunOnUiThread(() =>
                        {
                            string SuccessMessage = string.Format("Napaka pri dostopu do web aplikacije" + error);
                            DialogHelper.ShowDialogError(this, this, SuccessMessage);
                        });
                     
                        
                        return false;
                    }

                    if (stock.GetBool("ExistsSSCC"))
                    {
                        RunOnUiThread(() =>
                        {
                            string SuccessMessage = string.Format("SSCC koda že obstaja");
                            DialogHelper.ShowDialogError(this, this, SuccessMessage);
                        });
         
                      
                        return false;
                    }
                }
                if (string.IsNullOrEmpty(tbPacking.Text.Trim()))
                {
                    RunOnUiThread(() =>
                    {
                        string SuccessMessage = string.Format("Količina je obvezan podatek");
                        DialogHelper.ShowDialogError(this, this, SuccessMessage);
                    });
         
                    return false;
                }
                else
                {
                    try
                    {
                        var qty = Convert.ToDouble(tbPacking.Text.Trim());

                        if (qty == 0.0)
                        {
                            RunOnUiThread(() =>
                            {
                                string SuccessMessage = string.Format("Količina je obvezen podatek in mora bit različna od nič.");
                                DialogHelper.ShowDialogError(this, this, SuccessMessage);
                            });
                         
                     
                            return false;
                        }

                        if (CheckWorkOrderOpenQty())
                        {
                            var max = Math.Abs(openWorkOrder.GetDouble("OpenQty"));
                            if (Math.Abs(qty) > max)
                            {
                                RunOnUiThread(() =>
                                {
                                    string SuccessMessage = string.Format("Količina (" + qty.ToString(CommonData.GetQtyPicture()) + ") ne sme presegati max. količine (" + max.ToString(CommonData.GetQtyPicture()) + ")!");
                                    DialogHelper.ShowDialogError(this, this, SuccessMessage);
                                    tbPacking.RequestFocus();
                                });
                          
                                return false;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        RunOnUiThread(() =>
                        {
                            string SuccessMessage = string.Format("Količina mora biti število (" + e.Message + ")!");
                            DialogHelper.ShowDialogError(this, this, SuccessMessage);

                            tbPacking.RequestFocus();
                        });
                        
                        return false;
                    }
                }

                if (string.IsNullOrEmpty(tbUnits.Text.Trim())) {
                    RunOnUiThread(() =>
                    {
                        string SuccessMessage = string.Format("Št. enota je obvezen podatek!");
                        DialogHelper.ShowDialogError(this, this, SuccessMessage);
                        tbUnits.RequestFocus();
                    });
                   

                    return false;
                }
                else
                {
                    try
                    {
                        var qty = Convert.ToDouble(tbUnits.Text.Trim());
                        if (qty == 0.0)
                        {
                            RunOnUiThread(() =>
                            {
                                string SuccessMessage = string.Format("Št. enota je obvezen podatek, in more biti različit od nič.");
                                DialogHelper.ShowDialogError(this, this, SuccessMessage);
                                tbUnits.RequestFocus();
                            });
                          
                            return false;
                        }
                    }
                    catch (Exception e)
                    {
                        RunOnUiThread(() =>
                        {
                            string SuccessMessage = string.Format("Št. enot mora biti število (" + e.Message + ")!");
                            DialogHelper.ShowDialogError(this, this, SuccessMessage);
                            tbUnits.RequestFocus();
                        });
                     
                        return false;
                    }
                }

                if (moveItem == null) { moveItem = new NameValueObject("MoveItem"); }
                moveItem.SetInt("HeadID", moveHead.GetInt("HeadID"));
                moveItem.SetString("LinkKey", openWorkOrder.GetString("Key"));
                moveItem.SetInt("LinkNo", 0);
                moveItem.SetString("Ident", openWorkOrder.GetString("Ident"));
                moveItem.SetString("SSCC", tbSSCC.Text.Trim());
                moveItem.SetString("SerialNo", tbSerialNum.Text.Trim());
                moveItem.SetDouble("Packing", Convert.ToDouble(tbPacking.Text.Trim()));
                moveItem.SetDouble("Factor", Convert.ToDouble(tbUnits.Text.Trim()));
                moveItem.SetDouble("Qty", Convert.ToDouble(tbPacking.Text.Trim()) * Convert.ToDouble(tbUnits.Text.Trim()));
                moveItem.SetString("Location", tbLocation.Text.Trim());
                moveItem.SetInt("Clerk", Services.UserID());

                moveItem = Services.SetObject("mi", moveItem, out error);
                if (moveItem == null)
                {
                    RunOnUiThread(() =>
                    {
                        string SuccessMessage = string.Format("Napaka pri dostopu do web aplikacije: " + error);
                        DialogHelper.ShowDialogError(this, this, SuccessMessage);
                    });
               
                   
                    return false;
                }
                else
                {
                    InUseObjects.Invalidate("MoveItem");
                    return true;
                }
            }
            catch (Exception err)
            {

                Crashes.TrackError(err);
                return false;

            }
        }
      
        private void fillSugestedLocation(string warehouse)
        {
            var location = CommonData.GetSetting("DefaultProductionLocation");
            tbLocation.Text = location;


        }

        private void ProcessSerialNum()
        {
            if (string.IsNullOrEmpty(tbSerialNum.Text.Trim()))
            {
                tbSerialNum.Text = GetNextSerialNum();
                if (string.IsNullOrEmpty(tbSerialNum.Text.Trim()))
                {

                    tbSerialNum.RequestFocus();
                    return;
                }
            }
            GetWorkOrderDefaultQty();
            // ---
        } 
    private bool CheckWorkOrderOpenQty()
        {
            if (checkWorkOrderOpenQty == null)
            {       
                try
                {
                    string error;
                    var useObj = Services.GetObject("cwooqUse", "", out error);
                    checkWorkOrderOpenQty = useObj == null ? false : useObj.GetBool("Use");
                }
                catch (Exception err)
                {

                    Crashes.TrackError(err);
                    return false;

                }
            }
            return (bool)checkWorkOrderOpenQty;
        }
        private string GetNextSerialNum()
        {
            
            try
            {
            
                string error;
                var ident = openWorkOrder.GetString("Ident");
                var workOrder = openWorkOrder.GetString("Key");
                var serNumObj = Services.GetObject("sn", ident + "|" + workOrder, out error);


                if (serNumObj != null)
                {
                    return serNumObj.GetString("SerialNo");
                }
                else
                {
                    return "";
                }
            }
            catch (Exception err)
            {

                Crashes.TrackError(err);
                return "";

            }
        }
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetTheme(Resource.Style.AppTheme_NoActionBar);
            SetContentView(Resource.Layout.ProductionSerialOrSSCCEntry);
            AndroidX.AppCompat.Widget.Toolbar toolbar = FindViewById<AndroidX.AppCompat.Widget.Toolbar>(Resource.Id.toolbar);
            var _customToolbar = new CustomToolbar(this, toolbar, Resource.Id.navIcon);
            _customToolbar.SetNavigationIcon(settings.RootURL + "/Services/Logo");
            SetSupportActionBar(_customToolbar._toolbar);
            SupportActionBar.SetDisplayShowTitleEnabled(false);


            tbIdent = FindViewById<EditText>(Resource.Id.tbIdent);
            tbSSCC = FindViewById<EditText>(Resource.Id.tbSSCC);
            tbSerialNum = FindViewById<EditText>(Resource.Id.tbSerialNum);
            tbLocation = FindViewById<EditText>(Resource.Id.tbLocation);
            tbPacking = FindViewById<EditText>(Resource.Id.tbPacking);
            tbUnits = FindViewById<EditText>(Resource.Id.tbUnits);
            lbQty = FindViewById<TextView>(Resource.Id.lbQty);
            btSaveOrUpdate = FindViewById<Button>(Resource.Id.btSaveOrUpdate);
            button3 = FindViewById<Button>(Resource.Id.button3);
            button4 = FindViewById<Button>(Resource.Id.button4);
            button5 = FindViewById<Button>(Resource.Id.button5);
            tbIdent.InputType = Android.Text.InputTypes.ClassNumber;
            tbSSCC.InputType = Android.Text.InputTypes.ClassNumber;
            tbSerialNum.InputType = Android.Text.InputTypes.ClassNumber;
            tbLocation.InputType = Android.Text.InputTypes.ClassNumber;
            tbUnits.InputType = Android.Text.InputTypes.ClassNumber;

            soundPool = new SoundPool(10, Stream.Music, 0);
            soundPoolId = soundPool.Load(this, Resource.Raw.beep, 1);
            color();
            tbSSCC.RequestFocus();
            btSaveOrUpdate.Click += BtSaveOrUpdate_Click;
            button3.Click += Button3_Click;
            button4.Click += Button4_Click;
            button5.Click += Button5_Click;
            tbSSCC.FocusChange += TbSSCC_FocusChange;
            Barcode2D barcode2D = new Barcode2D();
            barcode2D.open(this, this);
            try
            {
                string SuccessMessage = string.Format("Preverjam povezovani DN");
                Toast.MakeText(this, SuccessMessage, ToastLength.Long).Show();
                var key = moveHead.GetString("LinkKey");
                string error;
                openWorkOrder = Services.GetObject("wo", key, out error);
                if (openWorkOrder == null) { throw new ApplicationException("Neveljaven povezan dokument: " + key); }
                lbQty.Text = "Količina (" + openWorkOrder.GetDouble("OpenQty").ToString(CommonData.GetQtyPicture()) + ")";
            }
            catch (Exception err)
            {

                Crashes.TrackError(err);
               

            }
            var ident = CommonData.LoadIdent(openWorkOrder.GetString("Ident"));
            tbIdent.Text = ident.GetString("Code") + " " + ident.GetString("Name");
            tbSSCC.Enabled = ident.GetBool("isSSCC");
            tbSerialNum.Enabled = ident.GetBool("HasSerialNumber");
            editMode = moveItem != null;

            if (editMode)

            {

                tbSSCC.Text = moveItem.GetString("SSCC");

                tbSerialNum.Text = moveItem.GetString("SerialNo");

                tbPacking.Text = moveItem.GetDouble("Packing").ToString(CommonData.GetQtyPicture());

                tbUnits.Text = moveItem.GetDouble("Factor").ToString("###,###,##0.00");

                tbPacking.RequestFocus();

            }

            else

            {
                if (tbSSCC.Enabled)

                {
                    tbSSCC.RequestFocus();
                }

                else if (tbSerialNum.Enabled)

                {

                    tbSerialNum.RequestFocus();

                }

                else

                {

                    tbPacking.RequestFocus();

                }

            }



            if (string.IsNullOrEmpty(tbUnits.Text.Trim())) { tbUnits.Text = "1"; }

            if (CommonData.GetSetting("ShowNumberOfUnitsField") == "1")
            {
                tbUnits.Visibility = ViewStates.Visible;
            }



            if (tbSSCC.Enabled && (CommonData.GetSetting("AutoCreateSSCCProduction") == "1"))

            {

                tbSSCC.Text = CommonData.GetNextSSCC();
                tbPacking.RequestFocus();

            }

            ProcessSerialNum();


            if(String.IsNullOrEmpty(tbUnits.Text)) { tbUnits.Text = "1"; }

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



        private void TbSSCC_FocusChange(object sender, View.FocusChangeEventArgs e)
        {
            var warehouse = moveHead.GetString("Wharehouse");

            fillSugestedLocation(warehouse); 
        }

        private void color()
        {

            tbSSCC.SetBackgroundColor(Android.Graphics.Color.Aqua);
            tbSerialNum.SetBackgroundColor(Android.Graphics.Color.Aqua);
            tbLocation.SetBackgroundColor(Android.Graphics.Color.Aqua);
        }


            private void Button5_Click(object sender, EventArgs e)
        {
            StartActivity(typeof(MainMenu));
            HelpfulMethods.clearTheStack(this);
        }



        private async Task FinishMethod()
        {
            await Task.Run(async () =>
            {
                var resultAsync = SaveMoveItem().Result;
                if (resultAsync)
                {
                    var headID = moveHead.GetInt("HeadID");
                    //
                    SelectSubjectBeforeFinish.ShowIfNeeded(headID);

                    RunOnUiThread(() =>
                    {
                        progress = new ProgressDialogClass();

                        progress.ShowDialogSync(this, "Zaključujem");
                    });
                    try
                    {

                        string result;
                        if (WebApp.Get("mode=finish&stock=add&print=" + Services.DeviceUser() + "&id=" + headID.ToString(), out result))
                        {
                            if (result.StartsWith("OK!"))
                            {
                                RunOnUiThread(() =>
                                {
                                    progress.StopDialogSync();
                                    var id = result.Split('+')[1];

                                    AlertDialog.Builder alert = new AlertDialog.Builder(this);
                                    alert.SetTitle("Zaključevanje uspešno");
                                    alert.SetMessage("Zaključevanje uspešno! Št.prevzema:\r\n" + id);

                                    alert.SetPositiveButton("Ok", (senderAlert, args) =>
                                    {
                                        alert.Dispose();
                                        System.Threading.Thread.Sleep(500);
                                        StartActivity(typeof(MainMenu));
                                        HelpfulMethods.clearTheStack(this);
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
                                    alert.SetTitle("Napaka");
                                    alert.SetMessage("Napaka pri zaključevanju: " + result);

                                    alert.SetPositiveButton("Ok", (senderAlert, args) =>
                                    {
                                        alert.Dispose();
                                        System.Threading.Thread.Sleep(500);
                                        StartActivity(typeof(MainMenu));
                                        HelpfulMethods.clearTheStack(this);

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
                                alert.SetTitle("Napaka");
                                alert.SetMessage("Napaka pri klicu web aplikacije: " + result);

                                alert.SetPositiveButton("Ok", (senderAlert, args) =>
                                {
                                    alert.Dispose();

                                });

                                Dialog dialog = alert.Create();
                                dialog.Show();
                            });



                        }
                    }
                    finally
                    {
                        RunOnUiThread(() =>
                        {
                            progress.StopDialogSync();

                        });
                    }
                }
            });
           
        }
        private async void Button4_Click(object sender, EventArgs e)
        {
            popupDialogConfirm = new Dialog(this);
            popupDialogConfirm.SetContentView(Resource.Layout.Confirmation);
            popupDialogConfirm.Window.SetSoftInputMode(SoftInput.AdjustResize);
            popupDialogConfirm.Show();

            popupDialogConfirm.Window.SetLayout(LayoutParams.MatchParent, LayoutParams.WrapContent);
            popupDialogConfirm.Window.SetBackgroundDrawableResource(Android.Resource.Color.HoloRedLight);

            // Access Popup layout fields like below
            btnYesConfirm = popupDialogConfirm.FindViewById<Button>(Resource.Id.btnYes);
            btnNoConfirm = popupDialogConfirm.FindViewById<Button>(Resource.Id.btnNo);
            btnYesConfirm.Click += BtnYesConfirm_Click;
            btnNoConfirm.Click += BtnNoConfirm_Click;
         
        }

        private void BtnNoConfirm_Click(object sender, EventArgs e)
        {
            popupDialogConfirm.Dismiss();
            popupDialogConfirm.Hide();
        }

        private async void BtnYesConfirm_Click(object sender, EventArgs e)
        {
            await FinishMethod();

        }

        private void Button3_Click(object sender, EventArgs e)
        {
            StartActivity(typeof(ProductionEnteredPositionsView));
            HelpfulMethods.clearTheStack(this);
        }

        private void BtSaveOrUpdate_Click(object sender, EventArgs e)
        {
            if (SaveMoveItem().Result)
            {
                if (editMode)
                {
                    StartActivity(typeof(ProductionEnteredPositionsView));
                    HelpfulMethods.clearTheStack(this);
                }
                else
                {
                    StartActivity(typeof(ProductionSerialOrSSCCEntry));
                    HelpfulMethods.clearTheStack(this);
                }
            }
        }
    }
}