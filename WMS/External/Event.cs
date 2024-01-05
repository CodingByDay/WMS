﻿using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Java.Util.Functions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrendNET.WMS.Device.Services;
using Xamarin.Essentials;

namespace Scanner.External
{
    public class Event
    {

        /// <summary>
        ///  Tale objekt bo serializiran in poslan na AR očala v obliki json.
        /// </summary>
        public enum EventType
        {
            TakeoverList, // Prevzem blaga seznam
            TakeOverPosition, // Prevzem pozicij
            IssuedList, // Izdaja seznam
            IssuedPosition // Izdaja pozicij
        }

        public string orderNumber { get; set; }
        public string clientName { get; set; }

        public Position chosenPosition { get; set; } // Pozicije naročila, če je event = IssuedPosition || TakeOverPosition

        public class Position
        {
            public string ident { get; set; }
            public string name { get; set; }
            public double qty { get; set; }
            public string unitOfMeasurement { get; set; }
            public int location { get; set; } // App pelje na to lokacijo
        }

        public List<Position> positions { get; set; } // Pozicije naročila, če je event = IssuedList || TakeOverList
        public EventType eventType { get; set; } // switch (eventType)
        public bool isRefreshCallback { get; set; } // Če je true ponovno naložiti positions array
    }
}



