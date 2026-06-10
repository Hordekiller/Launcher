using System;
using System.Windows.Forms;

namespace LauncherShyax
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new Launcher());
        }
    }
}
