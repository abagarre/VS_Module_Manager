using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace module_manager
{
    static class Program
    {
        /// <summary>
        /// Point d'entrée principal de l'application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        { 
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            if(!Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\ModuleManager\"))
            {
                string path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\ModuleManager\";
                Directory.CreateDirectory(path);
                Application.Run(new FirstLaunch());
            }
            else
            {
                Application.Run(new MainForm());
            }
        }
    }
}
