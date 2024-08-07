﻿using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Net;
using Android.Views;
using TrendNET.WMS.Core.Data;
using TrendNET.WMS.Device.App;
using TrendNET.WMS.Device.Services;
using WMS.App;
using WMS.ExceptionStore;
using static Android.App.ActionBar;
using WebApp = TrendNET.WMS.Device.Services.WebApp;
namespace WMS
{
    [Activity(Label = "WMS")]
    public class UnfinishedInterWarehouseView : CustomBaseActivity, ISwipeListener
    {

        private TextView lbInfo;
        private EditText tbBusEvent;
        private EditText tbIssueWarehouse;
        private EditText tbReceiveWarehouse;
        private EditText tbItemCount;
        private EditText tbCreatedBy;
        private EditText tbCreatedAt;
        private Button btNext;
        private Button btFinish;
        private Button btDelete;
        private Button btnNew;
        private Button btnLogout;
        private int displayedPosition = 0;
        private NameValueObjectList positions = (NameValueObjectList)InUseObjects.Get("InterWarehouseHeads");
        private Dialog popupDialog;
        private Button btnYes;
        private Button btnNo;
        private GestureDetector gestureDetector;
        private List<UnfinishedInterWarehouseList> data = new List<UnfinishedInterWarehouseList>();
        private int selected;
        private int selectedItem;
        private ListView? listData;
        private UniversalAdapter<UnfinishedInterWarehouseList> dataAdapter;

        protected async override void OnCreate(Bundle savedInstanceState)
        {
            try
            {
                base.OnCreate(savedInstanceState);
                SetTheme(Resource.Style.AppTheme_NoActionBar);
                // Create your application here
                if (App.Settings.tablet)
                {
                    base.RequestedOrientation = ScreenOrientation.Landscape;
                    base.SetContentView(Resource.Layout.UnfinishedInterWarehouseViewTablet);
                    listData = FindViewById<ListView>(Resource.Id.listData);
                    dataAdapter = UniversalAdapterHelper.GetUnfinishedInterwarehouse(this, data);
                    listData.Adapter = dataAdapter;
                    listData.ItemClick += DataList_ItemClick;
                    listData.ItemLongClick += DataList_ItemLongClick;
                }
                else
                {
                    base.RequestedOrientation = ScreenOrientation.Portrait;
                    base.SetContentView(Resource.Layout.UnfinishedInterWarehouseView);
                }

                LoaderManifest.LoaderManifestLoopResources(this);

                AndroidX.AppCompat.Widget.Toolbar toolbar = FindViewById<AndroidX.AppCompat.Widget.Toolbar>(Resource.Id.toolbar);
                var _customToolbar = new CustomToolbar(this, toolbar, Resource.Id.navIcon);
                _customToolbar.SetNavigationIcon(App.Settings.RootURL + "/Services/Logo");
                SetSupportActionBar(_customToolbar._toolbar);
                SupportActionBar.SetDisplayShowTitleEnabled(false);
                lbInfo = FindViewById<TextView>(Resource.Id.lbInfo);
                tbBusEvent = FindViewById<EditText>(Resource.Id.tbBusEvent);
                tbIssueWarehouse = FindViewById<EditText>(Resource.Id.tbIssueWarehouse);
                tbReceiveWarehouse = FindViewById<EditText>(Resource.Id.tbReceiveWarehouse);
                tbItemCount = FindViewById<EditText>(Resource.Id.tbItemCount);
                tbCreatedBy = FindViewById<EditText>(Resource.Id.tbCreatedBy);
                tbCreatedAt = FindViewById<EditText>(Resource.Id.tbCreatedAt);
                btNext = FindViewById<Button>(Resource.Id.btNext);
                btFinish = FindViewById<Button>(Resource.Id.btFinish);
                btnNew = FindViewById<Button>(Resource.Id.btnNew);
                btnLogout = FindViewById<Button>(Resource.Id.btnLogout);
                btDelete = FindViewById<Button>(Resource.Id.btDelete);
                btNext.Click += BtNext_Click;
                btFinish.Click += BtFinish_Click;
                btDelete.Click += BtDelete_Click;
                btnNew.Click += BtnNew_Click;
                btnLogout.Click += BtnLogout_Click;
                InUseObjects.Clear();

                await LoadPositions();

                if (App.Settings.tablet)
                {
                    FillItemsList();
                }

                var _broadcastReceiver = new NetworkStatusBroadcastReceiver();
                _broadcastReceiver.ConnectionStatusChanged += OnNetworkStatusChanged;
                Application.Context.RegisterReceiver(_broadcastReceiver,
                new IntentFilter(ConnectivityManager.ConnectivityAction), ReceiverFlags.NotExported);

                GestureListener gestureListener = new GestureListener(this);
                gestureDetector = new GestureDetector(this, new GestureListener(this));

                LinearLayout yourLinearLayout = FindViewById<LinearLayout>(Resource.Id.fling);
                // Initialize the GestureDetector
                yourLinearLayout.SetOnTouchListener(gestureListener);

                LoaderManifest.LoaderManifestLoopStop(this);
            }
            catch (Exception ex)
            {
                GlobalExceptions.ReportGlobalException(ex);
            }
        }

        private void DataList_ItemLongClick(object sender, AdapterView.ItemLongClickEventArgs e)
        {
            try
            {
                var index = e.Position;
                DeleteFromTouch(Convert.ToInt32(index));
            }
            catch (Exception ex)
            {
                GlobalExceptions.ReportGlobalException(ex);
            }
        }

        private void No(int index)
        {
            try
            {
                // UI changes.
                RunOnUiThread(() =>
                {
                    popupDialog.Dismiss();
                    popupDialog.Hide();
                });
            }
            catch (Exception ex)
            {
                GlobalExceptions.ReportGlobalException(ex);
            }
        }
        private void FillItemsList()
        {
            try
            {
                for (int i = 0; i < positions.Items.Count; i++)
                {
                    if (i < positions.Items.Count && positions.Items.Count > 0)
                    {
                        var item = positions.Items.ElementAt(i);
                        var created = item.GetDateTime("DateInserted");

                        // UI changes.
                        RunOnUiThread(() =>
                        {
                            tbCreatedAt.Text = created == null ? "" : ((DateTime)created).ToString("dd.MM.yyyy");
                        });



                        var date = created == null ? "" : ((DateTime)created).ToString("dd.MM.yyyy");
                        data.Add(new UnfinishedInterWarehouseList
                        {
                            Document = item.GetString("DocumentTypeName"),
                            CreatedBy = item.GetString("ClerkName"),
                            Date = date,
                            NumberOfPositions = item.GetInt("ItemCount").ToString(),
                        });

                    }
                    else
                    {
                        // UI changes.
                        RunOnUiThread(() =>
                        {
                            string errorWebApp = string.Format($"{Resources.GetString(Resource.String.s247)}");
                            Toast.MakeText(this, errorWebApp, ToastLength.Long).Show();
                        });

                    }

                }

                // UI changes.
                RunOnUiThread(() =>
                {
                    dataAdapter.NotifyDataSetChanged();
                    UniversalAdapterHelper.SelectPositionProgramaticaly(listData, 0);
                });
            }
            catch (Exception ex)
            {
                GlobalExceptions.ReportGlobalException(ex);
            }
        }
        private async Task Yes(int index)
        {
            try
            {
                var item = positions.Items[index];
                var id = item.GetInt("HeadID");
                try
                {

                    var (success, result) = await WebApp.GetAsync("mode=delMoveHead&head=" + id.ToString() + "&deleter=" + Services.UserID().ToString(), this);

                    if (success)
                    {
                        if (result == "OK!")
                        {
                            positions = null;
                            await LoadPositions();
                            data.Clear();
                            FillItemsList();

                            // UI changes.
                            RunOnUiThread(() =>
                            {
                                popupDialog.Dismiss();
                                popupDialog.Hide();
                            });

                        }
                        else
                        {

                            positions = null;
                            await LoadPositions();

                            // UI changes.
                            RunOnUiThread(() =>
                            {
                                string errorWebAppIssued = string.Format($"{Resources.GetString(Resource.String.s212)}" + result);
                                Toast.MakeText(this, errorWebAppIssued, ToastLength.Long).Show();
                                popupDialog.Dismiss();
                                popupDialog.Hide();
                            });

                            return;
                        }
                    }
                    else
                    {
                        // UI changes.
                        RunOnUiThread(() =>
                        {
                            string errorWebAppIssued = string.Format($"{Resources.GetString(Resource.String.s213)}" + result);
                            Toast.MakeText(this, errorWebAppIssued, ToastLength.Long).Show();
                            popupDialog.Dismiss();
                            popupDialog.Hide();
                        });

                        return;
                    }
                }
                catch (Exception err)
                {

                    SentrySdk.CaptureException(err);
                    return;

                }

            }
            catch (Exception ex)
            {
                GlobalExceptions.ReportGlobalException(ex);
            }
        }


        private void DeleteFromTouch(int index)
        {
            try
            {
                // UI changes.
                RunOnUiThread(() =>
                {
                    popupDialog = new Dialog(this);
                    popupDialog.SetContentView(Resource.Layout.YesNoPopUp);
                    popupDialog.Window.SetSoftInputMode(SoftInput.AdjustResize);
                    popupDialog.Show();

                    popupDialog.Window.SetLayout(LayoutParams.MatchParent, LayoutParams.WrapContent);
                    popupDialog.Window.SetBackgroundDrawable(new ColorDrawable(Color.ParseColor("#081a45")));


                    // Access Popup layout fields like below
                    btnYes = popupDialog.FindViewById<Button>(Resource.Id.btnYes);
                    btnNo = popupDialog.FindViewById<Button>(Resource.Id.btnNo);
                    btnYes.Click += (e, ev) => { Yes(index); };
                    btnNo.Click += (e, ev) => { No(index); };
                });
            }
            catch (Exception ex)
            {
                GlobalExceptions.ReportGlobalException(ex);
            }
        }

        private void DataList_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            try
            {
                selected = e.Position;
                Select(selected);
                selectedItem = selected;
            }
            catch (Exception ex)
            {
                GlobalExceptions.ReportGlobalException(ex);
            }
        }

        private void Select(int postionOfTheItemInTheList)
        {
            try
            {
                displayedPosition = postionOfTheItemInTheList;
                if (displayedPosition >= positions.Items.Count) { displayedPosition = 0; }
                FillDisplayedItem();
            }
            catch (Exception ex)
            {
                GlobalExceptions.ReportGlobalException(ex);
            }
        }

        public void OnSwipeLeft()
        {
            try
            {
                displayedPosition--;
                if (displayedPosition < 0) { displayedPosition = positions.Items.Count - 1; }
                FillDisplayedItem();
            }
            catch (Exception ex)
            {
                GlobalExceptions.ReportGlobalException(ex);
            }
        }

        public void OnSwipeRight()
        {
            try
            {
                displayedPosition++;
                if (displayedPosition >= positions.Items.Count) { displayedPosition = 0; }
                FillDisplayedItem();
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

        public override bool OnKeyDown(Keycode keyCode, KeyEvent e)
        {
            try
            {
                switch (keyCode)
                {
                    // in smartphone
                    case Keycode.F1:
                        if (btNext.Enabled == true)
                        {
                            BtNext_Click(this, null);
                        }
                        break;
                    // return true;
                    case Keycode.F2:
                        if (btFinish.Enabled == true)
                        {
                            BtFinish_Click(this, null);
                        }
                        break;
                    case Keycode.F3:
                        if (btDelete.Enabled == true)
                        {
                            BtDelete_Click(this, null);
                        }
                        break;
                    case Keycode.F4:
                        if (btnNew.Enabled == true)
                        {
                            BtnNew_Click(this, null);
                        }
                        break;
                    case Keycode.F5:
                        if (btnLogout.Enabled == true)
                        {
                            BtnLogout_Click(this, null);
                        }
                        break;
                }
                return base.OnKeyDown(keyCode, e);
            }
            catch (Exception ex)
            {
                GlobalExceptions.ReportGlobalException(ex);
                return false;
            }
        }
        private void BtnLogout_Click(object sender, EventArgs e)
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

        private void BtnNew_Click(object sender, EventArgs e)
        {
            try
            {
                NameValueObject moveHead = new NameValueObject("MoveHead");
                moveHead.SetBool("Saved", false);
                InUseObjects.Set("MoveHead", moveHead);
                StartActivity(typeof(InterWarehouseBusinessEventSetup));
                Finish();
            }
            catch (Exception ex)
            {
                GlobalExceptions.ReportGlobalException(ex);
            }
        }

        private void BtDelete_Click(object sender, EventArgs e)
        {
            try
            {
                if (positions.Items.Count > 0)
                {
                    popupDialog = new Dialog(this);
                    popupDialog.SetContentView(Resource.Layout.YesNoPopUp);
                    popupDialog.Window.SetSoftInputMode(SoftInput.AdjustResize);
                    popupDialog.Show();
                    popupDialog.Window.SetLayout(LayoutParams.MatchParent, LayoutParams.WrapContent);
                    popupDialog.Window.SetBackgroundDrawable(new ColorDrawable(Color.ParseColor("#081a45")));
                    btnYes = popupDialog.FindViewById<Button>(Resource.Id.btnYes);
                    btnNo = popupDialog.FindViewById<Button>(Resource.Id.btnNo);
                    btnYes.Click += BtnYes_Click;
                    btnNo.Click += BtnNo_Click;
                }
            }
            catch (Exception ex)
            {
                GlobalExceptions.ReportGlobalException(ex);
            }
        }

        private void BtnNo_Click(object sender, EventArgs e)
        {
            try
            {
                popupDialog.Dismiss();
                popupDialog.Hide();
            }
            catch (Exception ex)
            {
                GlobalExceptions.ReportGlobalException(ex);
            }
        }

        private async void BtnYes_Click(object sender, EventArgs e)
        {
            try
            {
                try
                {

                    var item = positions.Items[displayedPosition];
                    var id = item.GetInt("HeadID");
                    LoaderManifest.LoaderManifestLoopResources(this);
                    var (success, result) = await WebApp.GetAsync("mode=delMoveHead&head=" + id.ToString() + "&deleter=" + Services.UserID().ToString(), this);
                    if (success)
                    {
                        if (result == "OK!")
                        {
                            positions = null;
                            await LoadPositions();

                            if (App.Settings.tablet)
                            {
                                data.Clear();
                                FillItemsList();
                            }
                            popupDialog.Dismiss();
                            popupDialog.Hide();

                        }
                        else
                        {
                            string errorWebApp = string.Format($"{Resources.GetString(Resource.String.s212)}" + result);
                            Toast.MakeText(this, errorWebApp, ToastLength.Long).Show();
                            positions = null;
                            await LoadPositions();
                            popupDialog.Dismiss();
                            popupDialog.Hide();
                            return;

                        }
                    }
                    else
                    {
                        string errorWebApp = string.Format($"{Resources.GetString(Resource.String.s216)}" + result);
                        Toast.MakeText(this, errorWebApp, ToastLength.Long).Show();

                        popupDialog.Dismiss();
                        popupDialog.Hide();
                        return;
                    }
                }
                finally
                {
                    popupDialog.Dismiss();
                    popupDialog.Hide();
                    LoaderManifest.LoaderManifestLoopStop(this);
                }
            }
            catch (Exception ex)
            {
                GlobalExceptions.ReportGlobalException(ex);
            }

        }

        private void BtFinish_Click(object sender, EventArgs e)
        {
            try
            {
                var moveHead = positions.Items[displayedPosition];

                string error;
                if (!Services.TryLock("MoveHead" + moveHead.GetInt("HeadID").ToString(), out error))
                {
                    string toast = string.Format("Napaka:" + error);
                    Toast.MakeText(this, toast, ToastLength.Long).Show();
                    return;
                }

                moveHead.SetBool("Saved", true);
                InUseObjects.Set("MoveHead", moveHead);

                StartActivity(typeof(InterWarehouseEnteredPositionsView));

                Finish();
            }
            catch (Exception ex)
            {
                GlobalExceptions.ReportGlobalException(ex);
            }
        }

        private void BtNext_Click(object sender, EventArgs e)
        {
            try
            {
                if (App.Settings.tablet)
                {
                    selectedItem++;

                    if (selectedItem <= (positions.Items.Count - 1))
                    {
                        listData.RequestFocusFromTouch();
                        listData.SetSelection(selectedItem);
                        listData.SetItemChecked(selectedItem, true);
                    }
                    else
                    {
                        selectedItem = 0;
                        listData.RequestFocusFromTouch();
                        listData.SetSelection(selectedItem);
                        listData.SetItemChecked(selectedItem, true);
                    }
                }
                displayedPosition++;
                if (displayedPosition >= positions.Items.Count) { displayedPosition = 0; }
                FillDisplayedItem();
            }
            catch (Exception ex)
            {
                GlobalExceptions.ReportGlobalException(ex);
            }
        }

        private async Task LoadPositions()
        {
            try
            {
                try
                {

                    if (positions == null)
                    {
                        var error = "";

                        try
                        {
                            positions = await AsyncServices.AsyncServices.GetObjectListAsync("mh", "E", this);
                        }
                        catch (Exception)
                        {

                            return;

                        }

                        InUseObjects.Set("InterWarehouseHeads", positions);

                        if (positions == null)
                        {
                            // UI changes.
                            RunOnUiThread(() =>
                            {
                                string toast = string.Format($"{Resources.GetString(Resource.String.s213)}" + error);
                                Toast.MakeText(this, toast, ToastLength.Long).Show();
                            });

                            return;
                        }
                    }

                    displayedPosition = 0;
                    FillDisplayedItem();
                }
                catch (Exception err)
                {

                    SentrySdk.CaptureException(err);
                    return;

                }
            }
            catch (Exception ex)
            {
                GlobalExceptions.ReportGlobalException(ex);
            }
        }



        private void FillDisplayedItem()
        {
            try
            {
                if ((positions != null) && (positions.Items.Count > 0))
                {
                    // UI changes.
                    RunOnUiThread(() =>
                    {
                        lbInfo.Text = $"{Resources.GetString(Resource.String.s61)}" + " (" + (displayedPosition + 1).ToString() + "/" + positions.Items.Count + ")";
                        var item = positions.Items[displayedPosition];

                        tbBusEvent.Text = item.GetString("DocumentTypeName");
                        tbIssueWarehouse.Text = item.GetString("Issuer");
                        tbReceiveWarehouse.Text = item.GetString("Receiver");
                        tbItemCount.Text = item.GetInt("ItemCount").ToString();
                        tbCreatedBy.Text = item.GetString("ClerkName");

                        var created = item.GetDateTime("DateInserted");
                        tbCreatedAt.Text = created == null ? "" : ((DateTime)created).ToString("dd.MM.yyyy");
                        tbBusEvent.Enabled = false;
                        tbIssueWarehouse.Enabled = false;
                        tbReceiveWarehouse.Enabled = false;
                        tbItemCount.Enabled = false;
                        tbCreatedBy.Enabled = false;
                        tbCreatedAt.Enabled = false;


                        tbBusEvent.SetTextColor(Android.Graphics.Color.Black);
                        tbIssueWarehouse.SetTextColor(Android.Graphics.Color.Black);
                        tbReceiveWarehouse.SetTextColor(Android.Graphics.Color.Black);
                        tbItemCount.SetTextColor(Android.Graphics.Color.Black);
                        tbCreatedBy.SetTextColor(Android.Graphics.Color.Black);
                        tbCreatedAt.SetTextColor(Android.Graphics.Color.Black);

                        btNext.Enabled = true;
                        btDelete.Enabled = true;
                        btFinish.Enabled = true;
                    });

                }
                else
                {
                    // UI changes.
                    RunOnUiThread(() =>
                    {
                        lbInfo.Text = $"{Resources.GetString(Resource.String.s267)}";

                        tbBusEvent.Text = "";
                        tbIssueWarehouse.Text = "";
                        tbReceiveWarehouse.Text = "";
                        tbItemCount.Text = "";
                        tbCreatedBy.Text = "";
                        tbCreatedAt.Text = "";

                        tbBusEvent.Enabled = false;
                        tbIssueWarehouse.Enabled = false;
                        tbReceiveWarehouse.Enabled = false;
                        tbItemCount.Enabled = false;
                        tbCreatedBy.Enabled = false;
                        tbCreatedAt.Enabled = false;


                        tbBusEvent.SetTextColor(Android.Graphics.Color.Black);
                        tbIssueWarehouse.SetTextColor(Android.Graphics.Color.Black);
                        tbReceiveWarehouse.SetTextColor(Android.Graphics.Color.Black);
                        tbItemCount.SetTextColor(Android.Graphics.Color.Black);
                        tbCreatedBy.SetTextColor(Android.Graphics.Color.Black);
                        tbCreatedAt.SetTextColor(Android.Graphics.Color.Black);


                        btNext.Enabled = false;
                        btFinish.Enabled = false;

                        btDelete.Enabled = false;
                    });
                }
            }
            catch (Exception ex)
            {
                GlobalExceptions.ReportGlobalException(ex);
            }
        }
    }
}