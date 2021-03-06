﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MF.Engineering.MF8910.GestureDetector.Events;
using MF.Engineering.MF8910.GestureDetector.Tools;

namespace Emulator
{
    public class SteeringGestureEventArgs: GestureEventArgs
    {
        /// <summary>
        /// left is -1, -2, right is 1, 2
        /// </summary>
        public int Direction { get; set; }
    }
}
