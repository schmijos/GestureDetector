﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;
using GestureEvents;
using Gesture_Detector;

namespace DataSources
{
    public class Device
    {
        private static KinectSensor Dev;
        private Vector4 lastAcc;
        private List<Person> persons;

        public Device()
        {
            Dev = KinectSensor.KinectSensors.FirstOrDefault(x => x.Status == KinectStatus.Connected);
            lastAcc = new Vector4();
            persons = new List<Person>();
            Dev.SkeletonStream.Enable();
            Dev.SkeletonFrameReady += NewSkeletons;
        }

        public Device(string uniqueId)
        {
            foreach (KinectSensor ks in KinectSensor.KinectSensors)
            {
                if (ks.UniqueKinectId == uniqueId)
                {
                    Dev = ks;
                }
            }
        }

        public KinectStatus Status { get { return Dev.Status; } }

        public bool Start()
        {
            if (!Dev.IsRunning)
            {
             Dev.Start();
            }
            return Dev.IsRunning;
        }

        public bool Stop()
        {
            if (Dev.IsRunning)
            {
                Dev.Stop();
            }
            return Dev.IsRunning;
        }

        public void Dispose()
        {
            Dev.Dispose();
        }

        public Person GetActive()
        {
            return null;
        }

        public List<Person> GetAll()
        {
            return persons;
        }

        // TODO wie wärs mit einem griffigeren Namen?
        void NewSkeletons(object source, SkeletonFrameReadyEventArgs e)
        {
            double diff=0;
			diff += (Dev.AccelerometerGetCurrentReading().W - lastAcc.W);
            diff += (Dev.AccelerometerGetCurrentReading().X - lastAcc.X);
            diff += (Dev.AccelerometerGetCurrentReading().Y - lastAcc.Y);
            diff += (Dev.AccelerometerGetCurrentReading().Z - lastAcc.Z);
            if ((diff > 0.1 || diff < -0.1) && Accelerated != null)
            {
                Accelerated(this, new AccelerationEventArgs(diff));
            }
            else
            {
                SkeletonFrame skeFrm = e.OpenSkeletonFrame();
                Skeleton[] skeletons = new Skeleton[skeFrm.SkeletonArrayLength];
                skeFrm.CopySkeletonDataTo(skeletons);

                // Berücksichtige nur getrackte Skelette
                List<SmothendSkeleton> skeletonList = new List<SmothendSkeleton>();
                foreach (Skeleton ske in skeletons)
                {
                    if (ske.TrackingState == SkeletonTrackingState.Tracked)
                    {
                        skeletonList.Add(new SmothendSkeleton(ske));
                    }
                }

                /**
                 * Matchmatrix - 7 Skelette werden mit je 7 Personen gematcht
                 * 
                 *      | P1 | P2 | P3 | ..
                 *   S1 |  1 | 12 |  2 | ..
                 *   S2 |  5 | 15 |  7 | ..
                 *    : |  : |  : |  : | ..
                 *
                 * Ein tiefes Matching bedeutet dass ein neues Skelett und ein 
                 * bestehendes einer Person näher zusammenliegen als bei einem
                 * hohen
                 */
                int[,] matches = new int[7,7]; // da 4-fach verkettete Listen blöd sind, nehmen wir einen Array
                for (int i = 0; i < persons.Count; i++) // für alle Personen
                {
                    for (int j = 0; j < skeletonList.Count; j++) // für alle Skelette
                    {
                        matches[i,j] = persons[i].CompareTo(skeletonList[j]);
                    }
                }

                /**
                 * Es gibt 3 verschiedene Möglichkeiten den aktuellen Status zu beschreiben:
                 * - Alle Personen hatten schon ein Skelett. Die Zuweisung muss neu erfolgen
                 * - Es gibt weniger Skelette als Personen. übrige Person muss gelöscht werden
                 * - Es gibt mehr Skelette als Personen. Eine neue Person muss erstellt werden
                 */
                Dictionary<Person, SmothendSkeleton> bestMatches = new Dictionary<Person, SmothendSkeleton>();
                if (skeletonList.Count == persons.Count) // jede Person bekommt ein neues Skelett
                {
                    foreach (Person p in persons)
                    {
                        Tuple<int, int> indexes = iterateMatches(ref matches);
                        bestMatches.Add(persons[indexes.Item1], skeletonList[indexes.Item2]);
                    }
                }
                else if(skeletonList.Count < persons.Count) // eine Person ging aus dem Bild
                {
                    foreach (SmothendSkeleton s in skeletonList)
                    {
                        Tuple<int, int> indexes = iterateMatches(ref matches);
                        bestMatches.Add(persons[indexes.Item1], skeletonList[indexes.Item2]);
                    }
                    // TODO Person löschen, die nicht in der bestMatches Liste ist
                } 
                else // eine Person kam ins Bild
                {
                    foreach (Person p in persons)
                    {
                        Tuple<int, int> indexes = iterateMatches(ref matches);
                        bestMatches.Add(persons[indexes.Item1], skeletonList[indexes.Item2]);
                    }
                    // TODO Person aus übriggebliebenem Skelett erstellen
                }
            }
            lastAcc = Dev.AccelerometerGetCurrentReading();
        }

        /**
         * Es wird das Minimum in einem Array gesucht. Die ganze Zeile und die 
         * ganze Spalte des Fundortes wird gelöscht (bzw. auf unendlich gesetzt).
         * 
         *      | P1 | P2 | P3 | ..
         *   S1 |  8 |  ∞ | 12 | ..
         *   S2 | 15 |  ∞ |  7 | ..
         *   S3 |  ∞ |  ∞ |  ∞ | ..
         *   S4 | 75 |  ∞ | 47 | ..
         *    : |  : |  : |  : | ..
         * 
         * Bsp: Es wurde [P2, S3] als Minimum gefunden
         */
        private Tuple<int, int> iterateMatches(ref int[,] matches)
        {
            // finde Minimum
            int matchI = -1;
            int matchJ = -1;
            int minMatch = int.MaxValue;
            for (int i = 0; i < matches.GetLength(0); i++) // für alle Personen
            {
                for (int j = 0; j < matches.GetLength(1); j++) // für alle Skelette
                {
                    if (matches[i, j] < minMatch)
                    {
                        matchI = i;
                        matchJ = j;
                    }
                }
            }

            // speichere Minimum
            Tuple<int, int> found = new Tuple<int, int>(matchI, matchJ);

            // überschreibe Zeile und Spalte des Fundortes
            for (int i = 0; i < matches.GetLength(0); i++) // überschreibe Zeile des gefundenen Minimums
            {
                matches[i, matchJ] = int.MaxValue;
            }
            for (int j = 0; j < matches.GetLength(1); j++) // überschreibe Spalte des gefundenen Minimums
            {
                matches[matchI, j] = int.MaxValue;
            }

            return found;
        }

        #region Events

        public event EventHandler<ActivePersonEventArgs> PersonActive;
        //public delegate void ActivePersonHandler<TEventArgs> (object source, TEventArgs e) where TEventArgs:EventArgs;

        public event EventHandler<NewPersonEventArgs> NewPerson;
        //public delegate void PersonHandler<TEventArgs>(object source, TEventArgs e) where TEventArgs : EventArgs;

        public event EventHandler<AccelerationEventArgs> Accelerated;

        #endregion
    }
}
