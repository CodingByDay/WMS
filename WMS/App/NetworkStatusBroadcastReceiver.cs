﻿using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Stream = Android.Media.Stream;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WMS.App
{
    public class NetworkStatusBroadcastReceiver : BroadcastReceiver
    {
        public event EventHandler ConnectionStatusChanged;
        public override void OnReceive(Context context, Intent intent)
        {
            if (ConnectionStatusChanged != null)
                ConnectionStatusChanged(this, EventArgs.Empty);
        }
    }
}