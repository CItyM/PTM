﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace PublicTransportManager
{
    public class DistanceDuration
    {
        public string  Distance { get; set; }
        public string  Duration { get; set; }

        public override string ToString()
        {
            return Distance + " метра и на " + Duration + " минути"; 
        }

    }
}