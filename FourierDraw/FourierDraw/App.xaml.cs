﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace FourierDraw
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public string[] StartArgs;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            StartArgs = e.Args;
        }
    }
}
