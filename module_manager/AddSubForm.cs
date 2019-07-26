using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace module_manager
{
    public partial class AddSubForm : Form
    {

        private string repo;        // Chemin du répertoire du projet sélectionné
        public static string path;  // Chemin du répertoire du projet sélectionné
        public static List<Repo> moduleList = new List<Repo>(); // Liste de tous les modules du serveur
        List<Repo> repoList = new List<Repo>();
        Functions functions;
        Config config;

        public AddSubForm(string[] args)
        {
            repo = args[0];
            InitializeComponent();
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            toolStripStatusLabel2.Text = repo;
            path = repo;
            toolStripStatusLabel1.Text = "Chargement...";
            config = new Config();
            try
            {
                functions = MainForm.functions;
            }
            catch (Exception)
            {
                functions = new Functions();
            }

            try
            {
                repoList = MainForm.repoList;
            }
            catch (Exception)
            {
                repoList = functions.GetRepoList();
            }
            moduleList = repoList;
            backgroundWorker1.RunWorkerAsync();
        }

        /**
         * Empêche le clic sur un module déjà installé
         */
        private void CheckedListBox1_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            List<string> allItems = checkedListBox1.Items.OfType<string>().ToList();
            string curItem = e.CurrentValue.ToString();
            int index = e.Index;
            if (checkedListBox1.GetItemCheckState(index) == CheckState.Indeterminate)
            {
                e.NewValue = e.CurrentValue;
            }
        }

        /**
         * Charge le Form de confirmation de téléchargement des modules avec la liste des modules sélectionnés
         */
        private void MetroButton1_Click(object sender, EventArgs e)
        {
            if (checkedListBox1.CheckedItems.Count != 0)
            {
                // Si des modules ont été sélectionnés
                List<string> installList = new List<string>();
                foreach (object item in checkedListBox1.CheckedItems)
                {
                    if (!item.ToString().Contains("(✓)"))
                    {
                        // Si le module n'est pas déjà installé dans le projet
                        installList.Add(item.ToString());
                    }
                }
                if(installList.Count != 0)
                {
                    var frm = new AddConfirmForm(installList);
                    frm.Location = this.Location;
                    frm.StartPosition = FormStartPosition.Manual;
                    frm.FormClosed += CloseForm;
                    frm.Show();
                }
                else
                {
                    MessageBox.Show("Veuillez sélectionner un module", "Aucun module séléctionné", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                
            } else
            {
                MessageBox.Show("Veuillez sélectionner un module", "Aucun module séléctionné", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void CloseForm(object sender, FormClosedEventArgs e)
        {
            this.Close();
        }

        private void BackgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            List<Repo> projModules = new List<Repo>();  // Liste des modules du projet

            int i = 0;
            try
            {
                // Récupère la liste des modules du projet (lecture sur projet distant)
                projModules = functions.GetSubmodList(config.GetBranchDev(), functions.GetProjFullName(repo).Replace(".git", ""), new Repo());
            }
            catch (Exception ex)
            {
                Console.WriteLine("Add Mod error retreiving submod : " + ex.Message);
            }
            
            repo = repo.Substring(repo.LastIndexOf("\\") + 1, repo.Length - repo.LastIndexOf("\\") - 1);

            List<Repo> modList = moduleList.ToList();
            modList.Sort();
            i = 0;
            foreach (Repo mod in modList)
            {
                int toAdd = 1;
                
                foreach (Repo module in projModules)
                {
                    if (mod.Name.ToLower() == module.Name.ToLower())
                    {
                        // Si le module est déjà ajouté au projet, ajoute un indicateur et grise la case
                        checkedListBox1.Invoke(new Action(() => checkedListBox1.Items.Add(mod + " (✓)", true)));
                        checkedListBox1.Invoke(new Action(() => checkedListBox1.SetItemCheckState(i, CheckState.Indeterminate)));
                        toAdd = 0;
                        break;
                    }
                }
                
                if (toAdd == 1)
                {
                    checkedListBox1.Invoke(new Action(() => checkedListBox1.Items.Add(mod)));
                }

                i++;
                worker.ReportProgress(i * 100 / modList.Count());
            }
            worker.ReportProgress(0);
        }

        private void BackgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            toolStripProgressBar1.Value = e.ProgressPercentage; 
        }

        private void BackgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            toolStripStatusLabel1.Text = "Prêt";
            toolStripProgressBar1.Visible = false;
        }
    }
}
