﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataSources;
using GestureEvents;

namespace Gesture_Detector
{
    class Tester
    {
        static void Main(string[] args)
        {
            Device d = new Device();
            d.NewPerson += NewPerson;
            d.Start();
            System.Threading.Thread.Sleep(100000);
        }

        static void NewPerson(object src, NewPersonEventArgs e)
        {
            Console.WriteLine(e.Person.ID);
        }
    }
}
