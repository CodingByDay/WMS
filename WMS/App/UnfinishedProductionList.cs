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
    class UnfinishedProductionList
    {
        public string WorkOrder { get; set; }

        public string Orderer { get; set; }

        public string Ident { get; set; }

        public string NumberOfPositions { get; set; }

    }
}