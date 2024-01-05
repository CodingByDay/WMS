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
    class ProductionEnteredPositionList
    {
        public string Ident { get; set; }
        public string Quantity { get; set; }
        public string Location { get; set; }
        public string SerialNumber { get; set; }
        public string SSCC { get; set; }
    }
}