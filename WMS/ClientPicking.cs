﻿
using Android.Content;
using Android.Content.PM;
using Android.Media;
using Android.Text;
using Android.Views;
using BarCode2D_Receiver;
using TrendNET.WMS.Core.Data;
using TrendNET.WMS.Device.App;
using TrendNET.WMS.Device.Services;
using WMS.App;
using AlertDialog = Android.App.AlertDialog;

namespace WMS
{
    [Activity(Label = "ClientPicking", ScreenOrientation = Android.Content.PM.ScreenOrientation.Portrait)]
    public class ClientPicking : CustomBaseActivity, IBarcodeResult
    {
        private NameValueObject moveHead = (NameValueObject)InUseObjects.Get("MoveHead");
        private NameValueObject openOrder = (NameValueObject)InUseObjects.Get("OpenOrder");
        private ClientPickingAdapter adapter;
        private List<ClientPickingPosition> positions = new List<ClientPickingPosition>();
        private List<string> distinctClients = new List<string>();
        private NameValueObjectList data = new NameValueObjectList();
        private ListView ivTrail;
        private EditText tbClient;
        private EditText tbIdentFilter;
        private EditText tbLocationFilter;
        private SoundPool soundPool;
        private int soundPoolId;
        private Barcode2D barcode2D;
        private MyOnItemLongClickListener listener;
        private ClientPickingPosition chosen;
        /*
        This object contains the information about the current flow of the issueing process
        it must have a value always
        String CurrentFlow possible values: 0, 1, 2, string.Empty.
        */
        private Button btConfirm;
        private Button btDisplayPositions;
        private Button btBack;
        private Button btLogout;
        private ProgressDialog progressDialog;
        private ApiResultSet result;
        private ClientPickingPosition orderCurrent;
        private object mItem;
        private ListView? listData;
        private UniversalAdapter<ClientPickingPosition> dataAdapter;
        private ClientPickingAdapter clientAdapter;

        protected override async void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetTheme(Resource.Style.AppTheme_NoActionBar);
            if (App.Settings.tablet)
            {
                base.RequestedOrientation = ScreenOrientation.Landscape;
                base.SetContentView(Resource.Layout.ClientPickingTablet);
                listData = FindViewById<ListView>(Resource.Id.listData);
                dataAdapter = UniversalAdapterHelper.GetClientPicking(this, positions);
                listData.Adapter = dataAdapter;
            }
            else
            {
                base.RequestedOrientation = ScreenOrientation.Portrait;
                base.SetContentView(Resource.Layout.ClientPicking);
            }

            AndroidX.AppCompat.Widget.Toolbar toolbar = FindViewById<AndroidX.AppCompat.Widget.Toolbar>(Resource.Id.toolbar);
            var _customToolbar = new CustomToolbar(this, toolbar, Resource.Id.navIcon);
            _customToolbar.SetNavigationIcon(App.Settings.RootURL + "/Services/Logo");
            SetSupportActionBar(_customToolbar._toolbar);
            SupportActionBar.SetDisplayShowTitleEnabled(false);
            ivTrail = FindViewById<ListView>(Resource.Id.ivTrail);
            tbClient = FindViewById<EditText>(Resource.Id.tbClient);
            tbIdentFilter = FindViewById<EditText>(Resource.Id.tbIdentFilter);
            tbLocationFilter = FindViewById<EditText>(Resource.Id.tbLocationFilter);
            btConfirm = FindViewById<Button>(Resource.Id.btConfirm);
            btDisplayPositions = FindViewById<Button>(Resource.Id.btDisplayPositions);
            btBack = FindViewById<Button>(Resource.Id.btBack);
            btLogout = FindViewById<Button>(Resource.Id.btLogout);

            barcode2D = new Barcode2D(this, this);
            // Flow methods
            SetUpScanningFields();
            SetUpView();
            tbIdentFilter.AfterTextChanged += TbIdentFilter_AfterTextChanged;
            tbLocationFilter.AfterTextChanged += TbLocationFilter_AfterTextChanged;
            ivTrail.ItemClick += IvTrail_ItemClick;
            btConfirm.Click += BtConfirm_Click;
            btDisplayPositions.Click += BtDisplayPositions_Click;
            btBack.Click += BtBack_Click;
            btLogout.Click += BtLogout_Click;


            LoaderManifest.LoaderManifestLoopResources(this);

            if (CommonData.GetSetting("IssueSummaryView") == "1")
            {
                await initializeViewMultipleLocations();
            }
            else
            {
                await initializeView();
            }
            LoaderManifest.LoaderManifestLoopStop(this);

        }

        private void BtLogout_Click(object sender, EventArgs e)
        {
            StartActivity(typeof(MainMenu));
            Finish();
        }

        private void BtBack_Click(object sender, EventArgs e)
        {
            OnBackPressed();
        }

        private void BtDisplayPositions_Click(object sender, EventArgs e)
        {
            StartActivity(typeof(IssuedGoodsEnteredPositionsView));
            Finish();
        }



        private void BtConfirm_Click(object sender, EventArgs e)
        {
            if (adapter.returnSelected() != null)
            {
                if (SaveMoveHead())
                {
                    var obj = adapter.returnSelected();
                    var ident = obj.Ident;
                    var qty = obj.Quantity;
                    Intent i = new Intent(Application.Context, typeof(IssuedGoodsSerialOrSSCCEntryClientPicking));
                    i.PutExtra("ident", ident);
                    i.PutExtra("qty", qty);
                    i.PutExtra("selected", ClientPickingPosition.Serialize(obj));
                    StartActivity(i);
                    this.Finish();
                }
            }
            else
            {
                RunOnUiThread(() =>
                {
                    Toast.MakeText(this, $"{Resources.GetString(Resource.String.s228)}", ToastLength.Long);
                });

                return;
            }
        }



        private bool SaveMoveHead()
        {
            try
            {
                var obj = adapter.returnSelected();
                var ident = obj.Ident;
                var location = obj.Location;
                var qty = Convert.ToDouble(obj.Quantity);
                var extraData = new NameValueObject("ExtraData");
                extraData.SetString("Location", location);
                extraData.SetDouble("Qty", qty);
                InUseObjects.Set("ExtraData", extraData);
                string error;
                try
                {
                    var openIdent = Services.GetObject("id", ident, out error);

                    if (openIdent == null)
                    {
                        string WebError = string.Format($"{Resources.GetString(Resource.String.s229)}" + error);
                        Toast.MakeText(this, WebError, ToastLength.Long).Show();
                        return false;
                    }

                    InUseObjects.Set("OpenIdent", openIdent);
                }
                catch (Exception ex)
                {
                    SentrySdk.CaptureMessage("Error" + ex.Message);
                    return false;
                }
                if (!moveHead.GetBool("Saved"))
                {
                    try
                    {
                        // warehouse
                        moveHead.SetInt("Clerk", Services.UserID());
                        moveHead.SetString("CurrentFlow", Base.Store.modeIssuing.ToString());
                        moveHead.SetString("Type", "P");
                        moveHead.SetString("Receiver", moveHead.GetString("Receiver"));
                        moveHead.SetString("LinkKey", orderCurrent.Order);

                        var savedMoveHead = Services.SetObject("mh", moveHead, out error);
                        if (savedMoveHead == null)
                        {
                            string WebError = string.Format($"{Resources.GetString(Resource.String.s213)}" + error);
                            Toast.MakeText(this, WebError, ToastLength.Long).Show();
                            return false;
                        }
                        else
                        {
                            moveHead.SetInt("HeadID", savedMoveHead.GetInt("HeadID"));
                            moveHead.SetBool("Saved", true);
                            return true;
                        }
                    }
                    catch (Exception ex)
                    {
                        SentrySdk.CaptureMessage("Error" + ex.Message);
                        return false;
                    }
                }
                else
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureMessage("Error" + ex.Message);
                return false;
            }
        }

        private void IvTrail_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            adapter.setSelected(e.Position);
            orderCurrent = adapter.returnSelected();
        }

        private void SetUpScanningFields()
        {
            tbIdentFilter.SetBackgroundColor(Android.Graphics.Color.Aqua);
            tbLocationFilter.SetBackgroundColor(Android.Graphics.Color.Aqua);
        }

        private void SetUpView()
        {
            if (moveHead != null)
            {
                tbClient.Text = moveHead.GetString("Receiver");
            }
        }

        private async Task initializeView()
        {
            await Task.Run(async () =>
            {
                NameValueObjectList oodtw = new NameValueObjectList();
                if (moveHead != null)
                {
                    adapter = new ClientPickingAdapter(this, positions);
                    ivTrail.Adapter = adapter;
                    var parameters = new List<Services.Parameter>();

                    parameters.Add(new Services.Parameter { Name = "acDocType", Type = "String", Value = moveHead.GetString("DocumentType") });
                    parameters.Add(new Services.Parameter { Name = "acSubject", Type = "String", Value = moveHead.GetString("Receiver") });
                    parameters.Add(new Services.Parameter { Name = "acWarehouse", Type = "String", Value = moveHead.GetString("Wharehouse") });

                    string sql = $"SELECT * FROM uWMSOrderItemBySubjectTypeWarehouseOut WHERE acDocType = @acDocType AND acSubject = @acSubject AND acWarehouse = @acWarehouse;";
                    result = await AsyncServices.AsyncServices.GetObjectListBySqlAsync(sql, parameters);
                }
                if (moveHead != null && result != null && result.Success && result.Rows.Count > 0)
                {
                    int counter = 0;

                    foreach (var row in result.Rows)
                    {
                        var ident = row.StringValue("acIdent");
                        var location = row.StringValue("aclocation");
                        var name = row.StringValue("acName");
                        var key = row.StringValue("acKey");
                        var lvi = new ClientPickingPosition();
                        var no = row.IntValue("anNo");

                        if (no != null)
                        {
                            lvi.No = (int)no;
                            lvi.Order = key;
                            lvi.Ident = ident;
                            lvi.Location = location;
                            lvi.Quantity = string.Format("{0:###,##0.00}", row.DoubleValue("anQty"));
                            lvi.originalIndex = counter;
                            counter += 1;
                            positions.Add(lvi);
                        }
                    }
                    RunOnUiThread(() =>
                    {
                        adapter.NotifyDataSetChanged();
                        adapter.Filter(positions, true, string.Empty, false);
                        listener = new MyOnItemLongClickListener(this, adapter.returnData(), adapter);
                        ivTrail.OnItemLongClickListener = listener;
                        if (App.Settings.tablet)
                        {

                        }
                    });
                }
                else if (!result.Success)
                {
                    RunOnUiThread(() =>
                    {
                        AlertDialog.Builder alert = new AlertDialog.Builder(this);
                        alert.SetTitle($"{Resources.GetString(Resource.String.s265)}");
                        alert.SetMessage($"{result.Error}");
                        alert.SetPositiveButton("Ok", (senderAlert, args) =>
                        {
                            alert.Dispose();
                        });
                        Dialog dialog = alert.Create();
                        dialog.Show();
                    });
                }
            });
        }


        private async Task initializeViewMultipleLocations()
        {
            await Task.Run(async () =>
            {
                NameValueObjectList oodtw = new NameValueObjectList();
                if (moveHead != null)
                {
                    adapter = new ClientPickingAdapter(this, positions);
                    ivTrail.Adapter = adapter;
                    var parameters = new List<Services.Parameter>();

                    parameters.Add(new Services.Parameter { Name = "acDocType", Type = "String", Value = moveHead.GetString("DocumentType") });
                    parameters.Add(new Services.Parameter { Name = "acSubject", Type = "String", Value = moveHead.GetString("Receiver") });
                    parameters.Add(new Services.Parameter { Name = "acWarehouse", Type = "String", Value = moveHead.GetString("Wharehouse") });

                    string sql = $"SELECT * FROM uWMSOrderItemBySubjectTypeWarehouseOutSUM WHERE acDocType = @acDocType AND acSubject = @acSubject AND acWarehouse = @acWarehouse;";
                    result = await AsyncServices.AsyncServices.GetObjectListBySqlAsync(sql, parameters);
                }
                if (moveHead != null && result.Success && result.Rows.Count > 0)
                {
                    int counter = 0;

                    foreach (var row in result.Rows)
                    {
                        var ident = row.StringValue("acIdent");
                        var location = row.IntValue("anLocation");
                        var name = row.StringValue("acName");
                        var key = row.StringValue("acKey");
                        var lvi = new ClientPickingPosition();
                        var no = row.IntValue("anNo");
                        var actualLocation = row.StringValue("acActualLocation");

                        if (no != null)
                        {
                            lvi.Name = name;
                            lvi.No = (int)no;
                            lvi.Order = key;
                            lvi.Ident = ident;
                            if (location > 1)
                            {
                                lvi.Location = Resources.GetString(Resource.String.s346);
                            }
                            else if (location == 1)
                            {
                                /* Extra field for the use case where users want to see instantly on which location
                                 is the item present in the case of only one. 6.7.2024 JJ */
                                lvi.Location = actualLocation;
                            }
                            else if (location <= 0)
                            {
                                lvi.Location = Resources.GetString(Resource.String.s345);
                            }
                            lvi.Quantity = string.Format("{0:###,##0.00}", row.DoubleValue("anQty"));
                            lvi.originalIndex = counter;
                            counter += 1;
                            positions.Add(lvi);
                        }
                    }
                    RunOnUiThread(() =>
                    {
                        adapter.NotifyDataSetChanged();
                        adapter.Filter(positions, true, string.Empty, false);
                        listener = new MyOnItemLongClickListener(this, adapter.returnData(), adapter);
                        ivTrail.OnItemLongClickListener = listener;
                        if (App.Settings.tablet)
                        {

                        }
                    });
                }
                else if (!result.Success)
                {
                    RunOnUiThread(() =>
                    {
                        AlertDialog.Builder alert = new AlertDialog.Builder(this);
                        alert.SetTitle($"{Resources.GetString(Resource.String.s265)}");
                        alert.SetMessage($"{result.Error}");
                        alert.SetPositiveButton("Ok", (senderAlert, args) =>
                        {
                            alert.Dispose();
                        });
                        Dialog dialog = alert.Create();
                        dialog.Show();
                    });
                }
            });
        }



        private void TbLocationFilter_AfterTextChanged(object sender, AfterTextChangedEventArgs e)
        {
            try
            {
                adapter.Filter(positions, false, tbLocationFilter.Text, false);
                listener.updateData(adapter.returnData());
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
            }
        }

        private void TbIdentFilter_AfterTextChanged(object sender, AfterTextChangedEventArgs e)
        {
            try
            {
                adapter.Filter(positions, true, tbIdentFilter.Text, true);
                listener.updateData(adapter.returnData());
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
            }
        }

        public void GetBarcode(string barcode)
        {
            try
            {
                if (barcode != "Scan fail" && barcode != "")
                {
                    if (tbIdentFilter.HasFocus)
                    {


                        tbIdentFilter.Text = barcode;
                        adapter.Filter(positions, true, tbIdentFilter.Text, true);
                        if (adapter.returnNumberOfItems() == 0)
                        {
                            tbIdentFilter.Text = string.Empty;
                        }
                    }
                    else if (tbLocationFilter.HasFocus)
                    {
                        tbLocationFilter.Text = barcode;
                        adapter.Filter(positions, false, tbLocationFilter.Text, false);
                        if (adapter.returnNumberOfItems() == 0)
                        {
                            tbIdentFilter.Text = string.Empty;
                        }
                    }
                }
                listener.updateData(adapter.returnData());
            }
            catch (Exception error)
            {
                SentrySdk.CaptureMessage("Error" + error.Message);
            }
        }

        // Class for handling long click
        public class MyOnItemLongClickListener : Java.Lang.Object, AdapterView.IOnItemLongClickListener
        {
            public Context context_;
            public List<ClientPickingPosition> data_;
            public ClientPickingAdapter adapter_;

            public void updateData(List<ClientPickingPosition> data)
            {
                data_ = data;
            }

            public MyOnItemLongClickListener(Context context, List<ClientPickingPosition> data, ClientPickingAdapter adapter)
            {
                context_ = context;
                data_ = data;
                adapter_ = adapter;
            }

            public bool OnItemLongClick(AdapterView parent, View view, int position, long id)
            {
                adapter_.setSelected(position);
                ClientPickingPosition selected = data_.ElementAt(position);
                AlertDialog.Builder builder = new AlertDialog.Builder(context_);
                builder.SetTitle($"{context_.Resources.GetString(Resource.String.s256)}");
                builder.SetMessage($"{context_.Resources.GetString(Resource.String.s257)}: {selected.Ident}\n{context_.Resources.GetString(Resource.String.s258)}: {selected.Location}\n{context_.Resources.GetString(Resource.String.s259)}: {selected.Order}\n{context_.Resources.GetString(Resource.String.s260)}: {selected.Name}");
                builder.SetPositiveButton("OK", (s, args) =>
                {
                });
                AlertDialog alertDialog = builder.Create();
                alertDialog.Show();
                return true; // Return true to consume the long click event
            }
        }
    }
}