﻿using Stream = Android.Media.Stream;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Content.PM;
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
using static Android.App.ActionBar;
using WebApp = TrendNET.WMS.Device.Services.WebApp;
using AlertDialog = Android.App.AlertDialog;

using AndroidX.AppCompat.App;

namespace WMS
{
    [Activity(Label = "PackagingEnteredPositionsView", ScreenOrientation = ScreenOrientation.Portrait)]
    public class PackagingEnteredPositionsView : AppCompatActivity

    {    
        private Dialog popupDialog;
        private TextView lbInfo;
        private EditText tbPackNum;
        private EditText tbSSCC;
        private EditText tbItemCount;
        private EditText tbCreatedBy;
        private Button btNext;
        private Button btUpdate;
        private Button btCreate;
        private Button btDelete;
        private Button btClose;
        private int displayedPosition = 0;
        private NameValueObjectList positions = null;
        private Button btnYes;
        private Button btnNo;
        /// <summary>
        /// /////////////////////////
        /// </summary>
        /// <param name="savedInstanceState"></param>

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetTheme(Resource.Style.AppTheme_NoActionBar);
            SetContentView(Resource.Layout.PackagingEnteredPositionsView);
            AndroidX.AppCompat.Widget.Toolbar toolbar = FindViewById<AndroidX.AppCompat.Widget.Toolbar>(Resource.Id.toolbar);
            var _customToolbar = new CustomToolbar(this, toolbar, Resource.Id.navIcon);
            _customToolbar.SetNavigationIcon(settings.RootURL + "/Services/Logo");
            SetSupportActionBar(_customToolbar._toolbar);
            SupportActionBar.SetDisplayShowTitleEnabled(false);
            lbInfo = FindViewById<TextView>(Resource.Id.lbInfo);
            tbPackNum = FindViewById<EditText>(Resource.Id.tbPackNum);
            tbSSCC = FindViewById<EditText>(Resource.Id.tbSSCC);
            tbItemCount = FindViewById<EditText>(Resource.Id.tbItemCount);
            tbCreatedBy = FindViewById<EditText>(Resource.Id.tbCreatedBy);
            btNext = FindViewById<Button>(Resource.Id.btNext);
            btUpdate = FindViewById<Button>(Resource.Id.btUpdate);
            btCreate = FindViewById<Button>(Resource.Id.btCreate);
            btDelete = FindViewById<Button>(Resource.Id.btDelete);
            btClose = FindViewById<Button>(Resource.Id.btClose);
            btNext.Click += BtNext_Click;
            btUpdate.Click += BtUpdate_Click;
            btDelete.Click += BtDelete_Click;
            btCreate.Click += BtCreate_Click;
            btClose.Click += BtClose_Click;
            // LoadPosition()
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
        public override void OnBackPressed()
        {

            HelpfulMethods.releaseLock();

            base.OnBackPressed();
        }
        private void BtClose_Click(object sender, EventArgs e)
        {
            StartActivity(typeof(MainMenu));
            HelpfulMethods.clearTheStack(this);
        }

        private void BtCreate_Click(object sender, EventArgs e)
        {
            InUseObjects.Set("PackagingHead", null);
            StartActivity(typeof(PackagingSetContext));
            HelpfulMethods.clearTheStack(this);
        }

        private void BtDelete_Click(object sender, EventArgs e)
        {
            popupDialog = new Dialog(this);
            popupDialog.SetContentView(Resource.Layout.YesNoPopUp);
            popupDialog.Window.SetSoftInputMode(SoftInput.AdjustResize);
            popupDialog.Show();

            popupDialog.Window.SetLayout(LayoutParams.MatchParent, LayoutParams.WrapContent);
            popupDialog.Window.SetBackgroundDrawableResource(Android.Resource.Color.HoloRedLight);

            // Access Popup layout fields like below
            btnYes = popupDialog.FindViewById<Button>(Resource.Id.btnYes);
            btnNo = popupDialog.FindViewById<Button>(Resource.Id.btnNo);
            btnYes.Click += BtnYes_Click;
            btnNo.Click += BtnNo_Click;


        }

        private void BtnNo_Click(object sender, EventArgs e)
        {
            popupDialog.Dismiss();
            popupDialog.Hide();
        }

        private void BtnYes_Click(object sender, EventArgs e)
        {
            var item = positions.Items[displayedPosition];
            var id = item.GetInt("HeadID");

            try
            {

                string result;
                if (WebApp.Get("mode=delPackHead&head=" + id.ToString(), out result))
                {
                    if (result == "OK!")
                    {
                        positions = null;
                        LoadPositions();
                        popupDialog.Dismiss();
                        popupDialog.Hide();
                    }
                    else
                    {

                   
                       
                        AlertDialog.Builder alert = new AlertDialog.Builder(this);
                        alert.SetTitle("Napaka");
                        alert.SetMessage("Napaka pri brisanju pozicije." + result);

                        alert.SetPositiveButton("Ok", (senderAlert, args) =>
                        {
                            alert.Dispose();
                            System.Threading.Thread.Sleep(500);
                            throw new ApplicationException("Error, openIdent");
                        });



                        Dialog dialog = alert.Create();
                        dialog.Show();
                    }
                }
                else
                {

                    string toastError = string.Format("Napaka pri dostopu do web aplikacije." + result);
                    Toast.MakeText(this, toastError, ToastLength.Long).Show();
                    System.Diagnostics.Process.GetCurrentProcess().Kill();
                    return;
                }
            }
            catch (Exception err)
            {

                Crashes.TrackError(err);
                return;

            }
        }

        private void BtUpdate_Click(object sender, EventArgs e)
        {
            var item = positions.Items[displayedPosition];
            InUseObjects.Set("PackagingHead", item);
           StartActivity(typeof(PackagingUnitList));
            HelpfulMethods.clearTheStack(this);
        }




        public override bool OnKeyDown(Keycode keyCode, KeyEvent e)
        {
            switch (keyCode)
            {
                // In smartphone
                case Keycode.F1:

                    // next
                    BtNext_Click(this, null);
                    break;


                case Keycode.F2:

                    // edit
                    BtUpdate_Click(this, null);
                    break;


                case Keycode.F3:

                    // create
                    BtCreate_Click(this, null);
                    break;

                case Keycode.F4:

                    // delete 
                    BtDelete_Click(this, null);
                    break;


                case Keycode.F8:

                    // logout
                    BtClose_Click(this, null);
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
                        positions = Services.GetObjectList("ph", out error, "");
                        InUseObjects.Set("PackagingHeadPositions", positions);
                    }
                    if (positions == null)
                    {
                       string toast = string.Format("Napaka pri dostopu do web aplikacije" + error);
                       Toast.MakeText(this, toast, ToastLength.Long).Show();
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
            if ((positions != null) && (displayedPosition < positions.Items.Count))
            {
                var item = positions.Items[displayedPosition];
                lbInfo.Text = "Odprta pakiranja (" + (displayedPosition + 1).ToString() + "/" + positions.Items.Count + ")";

                tbPackNum.Text = item.GetInt("HeadID").ToString();
                tbSSCC.Text = item.GetString("SSCC");
                tbItemCount.Text = item.GetInt("ItemCount").ToString();

                var created = item.GetDateTime("Date");
                tbCreatedBy.Text = created == null ? "" : ((DateTime)created).ToString("dd.MM.") + " " + item.GetString("ClerkName");

                btUpdate.Enabled = true;
                btDelete.Enabled = true;

                tbPackNum.Enabled = false;
                tbSSCC.Enabled = false;
                tbItemCount.Enabled = false;
                tbCreatedBy.Enabled = false;



                tbPackNum.SetTextColor(Android.Graphics.Color.Black);
                tbSSCC.SetTextColor(Android.Graphics.Color.Black);
                tbItemCount.SetTextColor(Android.Graphics.Color.Black);
                tbCreatedBy.SetTextColor(Android.Graphics.Color.Black);
            }
            else
            {
                lbInfo.Text = "Odprta pakiranja (ni)";

                tbPackNum.Text = "";
                tbSSCC.Text = "";
                tbItemCount.Text = "";
                tbCreatedBy.Text = "";

                btUpdate.Visibility = ViewStates.Gone;
                btDelete.Visibility = ViewStates.Gone;
                btNext.Visibility = ViewStates.Gone;

                tbPackNum.Visibility = ViewStates.Gone;
                tbSSCC.Visibility = ViewStates.Gone;
                tbItemCount.Visibility = ViewStates.Gone;
                tbCreatedBy.Visibility = ViewStates.Gone;



                tbPackNum.SetTextColor(Android.Graphics.Color.Black);
                tbSSCC.SetTextColor(Android.Graphics.Color.Black);
                tbItemCount.SetTextColor(Android.Graphics.Color.Black);
                tbCreatedBy.SetTextColor(Android.Graphics.Color.Black);

            }
        }


    }
}