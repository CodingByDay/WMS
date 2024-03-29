﻿using Stream = Android.Media.Stream;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Net;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Microsoft.AppCenter.Crashes;
using WMS.App;
using TrendNET.WMS.Core.Data;
using TrendNET.WMS.Device.App;
using TrendNET.WMS.Device.Services;
using WebApp = TrendNET.WMS.Device.Services.WebApp;

using AndroidX.AppCompat.App;using AlertDialog = Android.App.AlertDialog;namespace WMS
{
    [Activity(Label = "InventoryConfirmTablet", ScreenOrientation = Android.Content.PM.ScreenOrientation.Landscape)]
    public class InventoryConfirmTablet : AppCompatActivity
    {
        private TextView lbInfo;
        private EditText tbWarehouse;
        private EditText tbTitle;
        private EditText tbDate;
        private EditText tbItems;
        private EditText tbCreatedBy;
        private EditText tbCreatedAt;

        private Button btNext;
        private Button target;
        private Button button3;
        private int displayedPosition = 0;

        private NameValueObjectList positions = null;
        private int output;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetTheme(Resource.Style.AppTheme_NoActionBar);
            SetContentView(Resource.Layout.InventoryConfirmTablet);
            AndroidX.AppCompat.Widget.Toolbar toolbar = FindViewById<AndroidX.AppCompat.Widget.Toolbar>(Resource.Id.toolbar);
            var _customToolbar = new CustomToolbar(this, toolbar, Resource.Id.navIcon);
            _customToolbar.SetNavigationIcon(settings.RootURL + "/Services/Logo");
            SetSupportActionBar(_customToolbar._toolbar);
            SupportActionBar.SetDisplayShowTitleEnabled(false);
            lbInfo = FindViewById<TextView>(Resource.Id.lbInfo);
            tbWarehouse = FindViewById<EditText>(Resource.Id.tbWarehouse);
            tbTitle = FindViewById<EditText>(Resource.Id.tbTitle);
            tbDate = FindViewById<EditText>(Resource.Id.tbDate);
            tbItems = FindViewById<EditText>(Resource.Id.tbItems);
            tbCreatedBy = FindViewById<EditText>(Resource.Id.tbCreatedBy);
            tbCreatedAt = FindViewById<EditText>(Resource.Id.tbCreatedAt);
            target = FindViewById<Button>(Resource.Id.target);
            target.Click += Target_Click;
            btNext = FindViewById<Button>(Resource.Id.btNext);
            button3 = FindViewById<Button>(Resource.Id.button3);
            btNext.Click += BtNext_Click;
            button3.Click += Button3_Click;
            InUseObjects.Clear();
            LoadPositions();
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




        public async Task DoWorkAsync()
        {
            await Task.Run(() => {

                var moveHead = positions.Items[displayedPosition];

                try
                {
                    var headID = moveHead.GetInt("HeadID");
                    string result;
                    if (WebApp.Get("mode=finish&id=" + headID.ToString(), out result))

                    {
                        if (result.StartsWith("OK!"))
                        {
                            var id = result.Split('+')[1];
                            DialogHelper.ShowDialogSuccess(this, this, "Potrjevanje uspešno! Št. potrditve: " + id);
                            output = 1;
                            StartActivity(typeof(MainMenu));
                        }
                        else
                        {
                            output = 2;
                            DialogHelper.ShowDialogError(this, this, "Napaka pri potrjevanju: " + result);
                        }
                    }
                    else
                    {
                        DialogHelper.ShowDialogError(this, this, "Napaka pri klicu web aplikacije: " + result);

                    }
                }
                catch(Exception err)
                {
                    Crashes.TrackError(err);
                    return;
                }
            });
        }



        private async void Target_Click(object sender, EventArgs e)
        {

            await DoWorkAsync();

            if (output == 1)
            {
                Toast.MakeText(this, "Potrjevanje uspešno!: ", ToastLength.Long).Show();
            }
            else
            {
                Toast.MakeText(this, "Potrjevanje neuspešno.: ", ToastLength.Long).Show();
            }

        }

    

        private void Button3_Click(object sender, EventArgs e)
        {
            StartActivity(typeof(MainMenuTablet));
            HelpfulMethods.clearTheStack(this);
        }


        public override bool OnKeyDown(Keycode keyCode, KeyEvent e)
        {
            switch (keyCode)
            {

                case Keycode.F2:
                    if (btNext.Enabled == true)
                    {
                        BtNext_Click(this, null);
                    }
                    break;
                // return true;

                case Keycode.F3:
                    if (target.Enabled == true)
                    {
                        Target_Click(this, null);
                    }
                    break;

                case Keycode.F8:
                    if (button3.Enabled == true)
                    {
                        Button3_Click(this, null);
                    }
                    break;
            }
            return base.OnKeyDown(keyCode, e);
        }



        private void BtNext_Click(object sender, EventArgs e)
        {
            displayedPosition++;
            if (displayedPosition >= positions.Items.Count) { displayedPosition = 0; }
            FillDisplayedItem();
        }

        private void LoadPositions()
        {

            try
            {
                if (positions == null)
                {
                    var error = "";
                    if (positions == null)
                    {
                        positions = Services.GetObjectList("mh", out error, "N");
                    }
                    if (positions == null)
                    {
                        DialogHelper.ShowDialogError(this, this, "Napaka pri dostopu do web aplikacije: " + error);

                        return;
                    }
                }

                displayedPosition = 0;
                FillDisplayedItem();
            }
            catch (Exception err)
            {

                Crashes.TrackError(err);
                return;

            }
        }




        private void FillDisplayedItem()
        {
            if ((positions != null) && (positions.Items.Count > 0))
            {
                lbInfo.Text = "Odprte inventure na čitalcu (" + (displayedPosition + 1).ToString() + "/" + positions.Items.Count + ")";
                var item = positions.Items[displayedPosition];

                tbWarehouse.Text = item.GetString("Wharehouse");
                tbTitle.Text = item.GetString("WharehouseName");
                var date = item.GetDateTime("Date");
                tbDate.Text = date == null ? "" : ((DateTime)date).ToString("dd.MM.yyyy");
                tbItems.Text = item.GetInt("ItemCount").ToString();
                tbCreatedBy.Text = item.GetString("ClerkName");
                var created = item.GetDateTime("DateInserted");
                tbCreatedAt.Text = created == null ? "" : ((DateTime)created).ToString("dd.MM.yyyy");
                btNext.Enabled = true;
                target.Enabled = true;
                tbWarehouse.Enabled = false;
                tbTitle.Enabled = false;
                tbDate.Enabled = false;
                tbItems.Enabled = false;
                tbCreatedBy.Enabled = false;
                tbCreatedAt.Enabled = false;
                tbWarehouse.SetTextColor(Android.Graphics.Color.Black);
                tbTitle.SetTextColor(Android.Graphics.Color.Black);
                tbDate.SetTextColor(Android.Graphics.Color.Black);
                tbItems.SetTextColor(Android.Graphics.Color.Black);
                tbCreatedBy.SetTextColor(Android.Graphics.Color.Black);
                tbCreatedAt.SetTextColor(Android.Graphics.Color.Black);

            }
            else
            {
                lbInfo.Text = "Odprte inventure na čitalcu (ni)";

                tbWarehouse.Text = "";
                tbTitle.Text = "";
                tbDate.Text = "";
                tbItems.Text = "";
                tbCreatedBy.Text = "";
                tbCreatedAt.Text = "";
                btNext.Enabled = false;
                target.Enabled = false;
                tbWarehouse.Enabled = false;
                tbTitle.Enabled = false;
                tbDate.Enabled = false;
                tbItems.Enabled = false;
                tbCreatedBy.Enabled = false;
                tbCreatedAt.Enabled = false;
                tbWarehouse.SetTextColor(Android.Graphics.Color.Black);
                tbTitle.SetTextColor(Android.Graphics.Color.Black);
                tbDate.SetTextColor(Android.Graphics.Color.Black);
                tbItems.SetTextColor(Android.Graphics.Color.Black);
                tbCreatedBy.SetTextColor(Android.Graphics.Color.Black);
                tbCreatedAt.SetTextColor(Android.Graphics.Color.Black);
            }
        }

    }
}