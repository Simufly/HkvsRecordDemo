using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace HkvsRecordDemo
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            string logFileName = DateTime.Now.ToString("yyyy-MM-dd-HH-mm") + ".txt";
            LogHandler.CreateLogFile(logFileName);
        }
    }
}
