﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MF.Engineering.MF8910.GestureDetector.Events;
using MF.Engineering.MF8910.GestureDetector.Tools;

namespace Emulator
{
    class ThrustGestureEventArgs : GestureEventArgs
    {
        public double DistanceToKnee { get; set; }
    }
}
