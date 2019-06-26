using System;
using System.Collections.Generic;
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
            if (args.Length > 1)
            {
                switch (args[0])
                {
                    case "add":
                        Application.Run(new Form2(args.Skip(1).ToArray()));
                        break;
                    case "list":
                        //Application.Run(new Form4(args.Skip(1).ToArray()));
                        break;
                    case "del":
                        Application.Run(new Form4(args.Skip(1).ToArray()));
                        break;
                    default:
                        Application.Run(new Form1());
                        break;
                }
            }
            else
            {
                Application.Run(new Form1());
            }
        }
    }
}
