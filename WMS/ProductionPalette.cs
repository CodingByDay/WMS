﻿using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Media;
using Android.Net;
using Android.OS;
using Android.Views;
using Android.Widget;
using BarCode2D_Receiver;
using Microsoft.AppCenter.Crashes;
using WMS.App;
using Stream = Android.Media.Stream;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TrendNET.WMS.Core.Data;
using TrendNET.WMS.Device.App;
using TrendNET.WMS.Device.Services;
using static Android.App.ActionBar;
using AlertDialog = Android.App.AlertDialog;

using AndroidX.AppCompat.App;



namespace WMS
{
    [Activity(Label = "ProductionPalette", ScreenOrientation = ScreenOrientation.Portrait)]
    public class ProductionPalette : AppCompatActivity, IBarcodeResult

    {
        private EditText tbCard;
        private EditText tbWorkOrder;
        private EditText tbIdent;
        private EditText tbSSCC;
        private EditText tbSerialNum;
        private bool initial = false;
        private ListView lvCardList;
        SoundPool soundPool;
        int soundPoolId;
        private Button btConfirm;
        private Button button2;
        private NameValueObject cardInfo = (NameValueObject)InUseObjects.Get("CardInfo");
        private double totalQty = 0.0;
        private List<ListViewItem> listItems = new List<ListViewItem>();
        private Dialog popupDialog;
        private Button btnYes;
        private Button btnNo;
        private TextView lbTotalQty;
        private EditText tbLegCode;
        private LinearLayout legLayout;
        private bool result;
        private bool target;
        private string stKartona;
        private ProgressDialogClass progress;
        private double collectiveAmount;

        public void GetBarcode(string barcode)
        {
            if (tbSerialNum.HasFocus) {
                if (barcode != "Scan fail")
                {
                    Sound();
                    tbSerialNum.Text = barcode;
                    ProcessSerialNum();
                    tbCard.RequestFocus();

                } else
                {
                    tbSerialNum.Text = "";
                }

            } else if (tbCard.HasFocus)
            {
                if (barcode != "")
                {
                    if (barcode != "Scan fail")
                    {
                        Sound();
                        ProcessCard(barcode);
                    } 
                } 
            } else if (tbLegCode.HasFocus)
            {
                Sound();
                tbLegCode.Text = barcode;
            }
        }

        private void Move(ListViewItem ivis, double qty, double totalQty)
        {
            popupDialog = new Dialog(this);
            popupDialog.SetContentView(Resource.Layout.TransportPopup);
            popupDialog.Window.SetSoftInputMode(SoftInput.AdjustResize);
            popupDialog.Show();
            popupDialog.Window.SetLayout(LayoutParams.MatchParent, LayoutParams.WrapContent);
            popupDialog.Window.SetBackgroundDrawableResource(Android.Resource.Color.HoloOrangeLight);
            btnYes = popupDialog.FindViewById<Button>(Resource.Id.btnYes);
            btnNo = popupDialog.FindViewById<Button>(Resource.Id.btnNo);
            btnNo.Click += BtnNo_Click1;
            btnYes.Click += (e, ev) => { BtnYes_Click(ivis, qty); };
        }

        private void BtnYes_Click(ListViewItem ivis, double qty)
        {
            var ivi = new ListViewItem { stKartona = stKartona, quantity = qty.ToString("###,###,##0.00") };
            listItems.Add(ivi);
            lvCardList.Adapter = null;
            adapterListViewItem adapter = new adapterListViewItem(this, listItems);
            lvCardList.Adapter = adapter;
            totalQty += qty;
            btConfirm.Enabled = true;
            lbTotalQty.Text = $"Količina skupaj: {totalQty.ToString("###,###,##0.00")} / {collectiveAmount.ToString("###,###,##0.00")}";
            popupDialog.Dismiss();
            popupDialog.Cancel();          
        }

      

        private void BtnNo_Click1(object sender, EventArgs e)
        {
            popupDialog.Dismiss();
            popupDialog.Cancel();
        }

        private void color()
        {
            tbSerialNum.SetBackgroundColor(Android.Graphics.Color.Aqua);
            tbCard.SetBackgroundColor(Android.Graphics.Color.Aqua);
            tbLegCode.SetBackgroundColor(Android.Graphics.Color.Aqua);
        }

        private void ProcessSerialNum()
        {


            try
            {
                string error;
                var cardObj = Services.GetObject("cq", tbSerialNum.Text + "|1|" + tbIdent.Text, out error);
                if (cardObj == null)
                {
                    string WebError = string.Format("Napaka pri preverjanju serijske št.: " + error);
                    Toast.MakeText(this, WebError, ToastLength.Long).Show();

                    return;
                }

                var qty = cardObj.GetDouble("Qty");
                if (qty > 0)
                {
                    tbSerialNum.Enabled = false;
                    lvCardList.Enabled = true;
                }
                else
                {
                    string WebError = string.Format("Serijska št. nima pripravljenih kartonov.");
                    Toast.MakeText(this, WebError, ToastLength.Long).Show();

                    return;
                }
            }
            catch (Exception err)
            {

                Crashes.TrackError(err);
                return;

            }
        }


        private IEnumerable<int> ScannedCardNumbers()
        {
            foreach (ListViewItem lvi in listItems)
            {
                yield return Convert.ToInt32(lvi.stKartona);
            }
        }


        private void Sound()
        {
            soundPool.Play(soundPoolId, 1, 1, 0, 0, 1);
        }

        private void ProcessCard(string data)
        {
            try
            {
                if (data.Length > tbSerialNum.Text.Length)
                {
                    try
                    {
                        stKartona = Convert.ToInt32(data.Substring(tbSerialNum.Text.Length)).ToString();

                    }
                    catch (Exception error)
                    {
                        Toast.MakeText(this, "Napaka...", ToastLength.Long).Show();
                    }
                }
                else { stKartona = Convert.ToInt32(tbCard.Text).ToString(); }

                if (stKartona != null)
                {
                    var next = true;
                    if (!data.StartsWith(tbSerialNum.Text) && tbSerialNum.Text.Length < tbCard.Text.Length)
                    {

                        string WebError = string.Format("Karton ne ustreza serijski številki.");
                        Toast.MakeText(this, WebError, ToastLength.Long).Show();
                    }
                    else
                    {

                        foreach (ListViewItem existing in listItems)
                        {
                            if (existing.stKartona == stKartona)
                            {
                                string WebError = string.Format("Karton je že dodan na paleto!");
                                Toast.MakeText(this, WebError, ToastLength.Long).Show();

                                return;
                            }
                        }
                        try
                        {
                            string error;

                            var cardObj = Services.GetObject("cq", tbSerialNum.Text + "|" + stKartona + "|" + tbIdent.Text, out error);

                            if (cardObj == null)
                            {
                                string WebError = string.Format("Napaka pri preverjanju kartona: " + error);
                                Toast.MakeText(this, WebError, ToastLength.Long).Show();
                                return;
                            }

                            var qty = cardObj.GetDouble("Qty");

                            if (qty > 0.0)
                            {
                                if (cardObj.GetInt("IDHead") > 0)
                                {

                                    var ivis = new ListViewItem { stKartona = stKartona, quantity = qty.ToString("###,###,##0.00") };

                                    Move(ivis, qty, totalQty);



                                }
                                else
                                {

                                    var ivi = new ListViewItem { stKartona = stKartona, quantity = qty.ToString("###,###,##0.00") };
                                    listItems.Add(ivi);
                                    lvCardList.Adapter = null;
                                    adapterListViewItem adapter = new adapterListViewItem(this, listItems);
                                    lvCardList.Adapter = adapter;
                                    totalQty += qty;


                                    lbTotalQty.Text = $"Količina skupaj: {totalQty.ToString("###,###,##0.00")} / {collectiveAmount.ToString("###,###,##0.00")}";

                                    btConfirm.Enabled = true;
                                }
                            }

                            else
                            {
                                string WebError = string.Format("Neveljaven karton: " + data);
                                Toast.MakeText(this, WebError, ToastLength.Long).Show();
                                return;
                            }
                        }
                        finally
                        {
                            tbCard.Text = "";
                        }
                    }
                }
                else
                {
                    Toast.MakeText(this, "Nepravilen vnos.", ToastLength.Long).Show();
                }
            } catch { return; }
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetTheme(Resource.Style.AppTheme_NoActionBar);
            SetContentView(Resource.Layout.ProductionPalette);
            AndroidX.AppCompat.Widget.Toolbar toolbar = FindViewById<AndroidX.AppCompat.Widget.Toolbar>(Resource.Id.toolbar);
            var _customToolbar = new CustomToolbar(this, toolbar, Resource.Id.navIcon);
            _customToolbar.SetNavigationIcon(settings.RootURL + "/Services/Logo");
            SetSupportActionBar(_customToolbar._toolbar);
            SupportActionBar.SetDisplayShowTitleEnabled(false);
            tbWorkOrder = FindViewById<EditText>(Resource.Id.tbWorkOrder);
            tbIdent = FindViewById<EditText>(Resource.Id.tbIdent);
            tbSSCC = FindViewById<EditText>(Resource.Id.tbSSCC);
            tbSerialNum = FindViewById<EditText>(Resource.Id.tbSerialNum);
            lvCardList = FindViewById<ListView>(Resource.Id.lvCardList);
            tbCard = FindViewById<EditText>(Resource.Id.tbCard);
            btConfirm = FindViewById<Button>(Resource.Id.btConfirm);
            button2 = FindViewById<Button>(Resource.Id.button2);
            lbTotalQty = FindViewById<TextView>(Resource.Id.lbTotalQty);
            tbLegCode = FindViewById<EditText>(Resource.Id.tbLegCode);
            legLayout = FindViewById<LinearLayout>(Resource.Id.legLayout);




            var isPalletCode = CommonData.GetSetting("Pi.HideLegCode");

            if (isPalletCode != null)
            {
                if (isPalletCode != "1")
                {
                   
                } else
                {
                    legLayout.Visibility = ViewStates.Invisible;

                }
            } else
            {
                legLayout.Visibility = ViewStates.Invisible;
            }




            tbWorkOrder.Text = cardInfo.GetString("WorkOrder").Trim();
            tbIdent.Text = cardInfo.GetString("Ident").Trim();
            tbSSCC.Text = CommonData.GetNextSSCC();
            soundPool = new SoundPool(10, Stream.Music, 0);
            soundPoolId = soundPool.Load(this, Resource.Raw.beep, 1);
            Barcode2D barcode2D = new Barcode2D();
            barcode2D.open(this, this);
            btConfirm.Click += BtConfirm_Click;
            button2.Click += Button2_Click;
            adapterListViewItem adapter = new adapterListViewItem(this, listItems);
            lvCardList.Adapter = adapter;
            lvCardList.ItemLongClick += LvCardList_ItemLongClick;
            tbSerialNum.RequestFocus();
            tbSerialNum.KeyPress += TbSerialNum_KeyPress;
            tbCard.KeyPress += TbCard_KeyPress;

            color();


            var ident = cardInfo.GetString("Ident").Trim();


            setUpMaximumQuantity(ident);


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

        private void setUpMaximumQuantity(string ident)
        {
            string error;
            var identObject = Services.GetObject("id", ident, out error);
            
             collectiveAmount = identObject.GetDouble("UM1toUM2") * identObject.GetDouble("UM1toUM3");
        }

        private void TbCard_KeyPress(object sender, View.KeyEventArgs e)
        {
            if (e.KeyCode == Keycode.Enter)
            {
                if (tbCard.Text != "")
                {
                    ProcessCard(tbCard.Text);
                    tbCard.RequestFocus();
                } 
            }
            else
            {
                e.Handled = false;
            }
        }

        private void TbSerialNum_KeyPress(object sender, View.KeyEventArgs e)
        {
            if (e.KeyCode == Keycode.Enter)
            {
                ProcessSerialNum();
                tbCard.RequestFocus();
            }
            else
            {
                e.Handled = false;
            }
        }

   

        private void LvCardList_ItemLongClick(object sender, AdapterView.ItemLongClickEventArgs e)
        {

            popupDialog = new Dialog(this);
            popupDialog.SetContentView(Resource.Layout.YesNoGeneric);
            popupDialog.Window.SetSoftInputMode(SoftInput.AdjustResize);
            popupDialog.Show();
            popupDialog.Window.SetLayout(LayoutParams.MatchParent, LayoutParams.WrapContent);
            popupDialog.Window.SetBackgroundDrawableResource(Android.Resource.Color.HoloOrangeLight);
            btnYes = popupDialog.FindViewById<Button>(Resource.Id.btnYes);
            btnNo = popupDialog.FindViewById<Button>(Resource.Id.btnNo);
            btnNo.Click += BtnNo_Click;
            btnYes.Click += (e, ev) => { ButtonYes(lvCardList.SelectedItemId); };
          
        }

        private void ButtonYes(long selectedItemId)
        {
            ListViewItem itemPriorToDelete = listItems.ElementAt((int)selectedItemId);
            totalQty = totalQty - Convert.ToDouble(itemPriorToDelete.quantity);
            listItems.RemoveAt((int)selectedItemId);
            lbTotalQty.Text = $"Količina skupaj: {totalQty.ToString("###,###,##0.00")} / {collectiveAmount.ToString("###,###,##0.00")}";

            lvCardList.Adapter = null;
            adapterListViewItem adapter = new adapterListViewItem(this, listItems);
            lvCardList.Adapter = adapter;
            popupDialog.Dismiss();
            popupDialog.Cancel();

        }

        private void BtnNo_Click(object sender, EventArgs e)
        {
            popupDialog.Dismiss();
            popupDialog.Cancel();
        }

        private void Button2_Click(object sender, EventArgs e)
        {
            StartActivity(typeof(MainMenu));
            HelpfulMethods.clearTheStack(this);
        }




        private async Task runOnBothThreads()
        {
            await Task.Run(() =>
            {

            try
            {
                    RunOnUiThread(() =>
                    {
                        progress = new ProgressDialogClass();
                        progress.ShowDialogSync(this, "Pošiljam podatke, prosim počakajte.");

                    });

                    var palInfo = new NameValueObject("PaletteInfo");
                    System.Threading.Thread.Sleep(1000);
                    palInfo.SetString("WorkOrder", tbWorkOrder.Text);
                    palInfo.SetString("Ident", tbIdent.Text);
                    palInfo.SetInt("Clerk", Services.UserID());
                    palInfo.SetString("SerialNum", tbSerialNum.Text);
                    palInfo.SetString("SSCC", tbSSCC.Text);
                    palInfo.SetString("CardNums", string.Join(",", ScannedCardNumbers().Select(x => x.ToString()).ToArray()));
                    palInfo.SetDouble("TotalQty", totalQty);
                    palInfo.SetString("DeviceID", Services.DeviceUser());
                    string error;
                    palInfo = Services.SetObject($"cf&&legCode={tbLegCode.Text}", palInfo, out error);
                    if (palInfo == null)
                    {
                      
                        RunOnUiThread(() =>
                        {                        
                            progress.StopDialogSync();
                            AlertDialog.Builder alert = new AlertDialog.Builder(this);
                            alert.SetTitle("Napaka");
                            alert.SetMessage("Napaka pri potrjevanju palete: " + error);

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
                        var result = palInfo.GetString("Result");
                        if (result.StartsWith("OK!"))
                        {
                            RunOnUiThread(() =>
                            {
                                progress.StopDialogSync();
                                var id = result.Split('+')[1];

                                AlertDialog.Builder alert = new AlertDialog.Builder(this);
                                alert.SetTitle("Zaključevanje uspešno");
                                alert.SetMessage("Paletiranje uspešno! Št. prevzema:\r\n" + id);

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
                                alert.SetTitle("Napaka");
                                alert.SetMessage("Napaka pri paletiranju: " + result);

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
                }
                finally
                {
                    RunOnUiThread(() =>
                    {
                        progress.StopDialogSync();
                    });
                }
            });
        }

        private async void BtConfirm_Click(object sender, EventArgs e)
        {
            RunOnUiThread(() =>
            {
                    AlertDialog.Builder alert = new AlertDialog.Builder(this);

                    alert.SetTitle("Noga palete manjka");
                    alert.SetMessage("Ali sigurno želite nadaljevati?"); 
                    alert.SetPositiveButton("Ok", async (senderAlert, args) =>
                    {
                        alert.Dispose();
                        await runOnBothThreads();
                    });

                    alert.SetNegativeButton("Nazaj", (senderAlert, args) =>
                    {
                        alert.Dispose();
                    });

                    Dialog dialog = alert.Create();
                    dialog.Show();
            });
        }
    }
}