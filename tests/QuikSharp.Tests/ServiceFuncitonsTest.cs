﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using QUIKSharp.DataStructures;

namespace QUIKSharp.Tests
{
    [TestFixture]
    public class ServiceFunctionsTest_Online {
        public ServiceFunctionsTest_Online() { isQuik = _q.Debug.IsQuik().Result; }
        private readonly Quik _q = new Quik();
        private readonly bool isQuik;

        [Test]
        public void IsConencted() {
            Console.WriteLine("IsConnected? : " + _q.Service.IsConnected().Result);
        }

        [Test]
        public void GetWorkingFolder() {
            Console.WriteLine("GetWorkingFolder: " + _q.Service.GetWorkingFolder().Result);
        }

        [Test]
        public void GetScriptPath() {
            Console.WriteLine("GetScriptPath: " + _q.Service.GetScriptPath().Result);
        }

        [Test]
        public void GetInfoParam() {
            var values = Enum.GetValues(typeof(InfoParams)).Cast<InfoParams>().ToArray();
            foreach (var value in values) {
                Console.WriteLine(value 
                    + " : " + _q.Service.GetInfoParam(value).Result);
            }
        }

        [Test]
        public void Message() {
            Console.WriteLine("This is a message: " + 
                _q.Service.Message("This is a message", NotificationType.Info).Result);
            Console.WriteLine("This is a warning: " +
                _q.Service.Message("This is a warning", NotificationType.Warning).Result);
            Console.WriteLine("This is an error: " +
                _q.Service.Message("This is an error", NotificationType.Error).Result);
        }

        [Test]
        public void PrintDbgStr() {
            Console.WriteLine("Debug: " +
                _q.Service.PrintDbgStr("This is debug info").Result);
        }

        [Test]
        public void addLabel()
        {
            var label = new Label()
            {
                XValue = DateTime.Now,
                YValue = 61000,
                ImagePath = "C:\\ClassesC\\Labels\\buy.bmp",
                Text = "si",
                Alignment = LabelAlignment.BOTTOM,
            };

            var res = _q.Service.AddLabel("si", label);
            Console.WriteLine("AddLabel: "+ String.Join(",", Convert.ToString(res.Result)));
        }

        [Test]
        public void delLabel()
        {
            long tagId = 13;
            var res = _q.Service.DelLabel("si", tagId);

            Console.WriteLine("delLabel: "
                    + String.Join(",", Convert.ToString(res.Result)));
        }

        [Test]
        public void delAllLabels()
        {
            var res = _q.Service.DelAllLabels("si");

            Console.WriteLine("delAllLabels: "
                  + String.Join(",", Convert.ToString(res)));
        }
    }
}