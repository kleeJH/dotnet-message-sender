using MessageSender.Properties;
using System;
using System.Windows.Forms;

namespace MessageSender
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.ApplicationExit += new EventHandler(Application_ApplicationExit);
            Application.Run(new FormMain());
        }

        static void Application_ApplicationExit(object sender, EventArgs e)
        {
            Settings.Default.Save();
        }
    }
}
