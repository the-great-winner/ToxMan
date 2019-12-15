using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace ToxMan
{
    /// <summary>
    /// Логика взаимодействия для App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static string[] Args { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            Args = e.Args;
            base.OnStartup(e);
        }
    }
}
