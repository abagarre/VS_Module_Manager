using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace module_manager
{
    public partial class DelForm : Form
    {
        string modName; // Nom du module à supprimer
        string path;    // Chemin du projet
        Config config;

        public DelForm(string[] args)
        {
            InitializeComponent();
            path = args[0];
            modName = args[1].Replace(".git", "");
            modName = modName.Substring(modName.LastIndexOf("/r/") + 3, modName.Length - (modName.LastIndexOf("/r/") + 3));
            string mod = modName.Replace("/", @"\");
            path = path.Substring(0,path.IndexOf(mod));
            config = new Config();
        }

        private void Form4_Load(object sender, EventArgs e)
        {
            toolStripStatusLabel1.Text = path;
            metroLabel1.Text = "Voulez vous supprimer le module " + modName + " du projet " + path.Substring(path.Remove(path.Length - 2).LastIndexOf(@"\") + 1, path.Length - path.Remove(path.Length - 2).LastIndexOf(@"\") - 2) + " ?";
            
            var files = Directory.GetFiles(path + modName, ".git");
            if (files.Length == 0) 
            {
                // Si le dossier n'est pas un submodule git
                MessageBox.Show("Le répertoire sélectionné n'est pas un module", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Close();
            }
            
        }

        private void Button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        /**
         * Confirme la suppression
         */
        private void Button1_Click(object sender, EventArgs e)
        {
            backgroundWorker1.RunWorkerAsync(argument: modName);
        }

        private void BackgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            Process process = new Process();
            //====================================== DEL SUB PATH ========================================//
            process.StartInfo.FileName = config.GetAppData() + @"del_sub.bat";
            //============================================================================================//
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.StartInfo.WorkingDirectory = path;
            process.StartInfo.Arguments = (string) e.Argument;
            process.Start();
            e.Result = process.StandardError.ReadToEnd();
            while (!process.StandardOutput.EndOfStream)
            {
                string line = process.StandardOutput.ReadLine();
                Console.WriteLine(line);
                if (line == "\"status 25\"")
                {
                    worker.ReportProgress(25);
                }
                else if (line == "\"status 50\"")
                {
                    worker.ReportProgress(50);
                }
                else if (line == "\"status 75\"")
                {
                    worker.ReportProgress(75);
                }
                else if (line == "\"status 100\"")
                {
                    worker.ReportProgress(100);
                }
            }
        }

        private void BackgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            toolStripProgressBar1.Value = e.ProgressPercentage;
        }

        /**
         * Affiche les erreurs et warning du process
         */
        private void BackgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Result.ToString().Length != 0)
                MessageBox.Show(e.Result.ToString(), "Message", MessageBoxButtons.OK, MessageBoxIcon.Warning);

            this.Close();
        }
    }
}
