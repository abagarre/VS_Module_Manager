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
        public static List<string> moduleList = new List<string>(); // Liste de tous les modules du serveur
        Functions functions;

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
            functions = new Functions();
            moduleList = functions.DispModList();
            backgroundWorker1.RunWorkerAsync();
        }

        private void CheckedListBox1_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            //List<string> allItems = checkedListBox1.Items.OfType<string>().ToList();
            //string curItem = checkedListBox1.SelectedItem.ToString();
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
            if(checkedListBox1.CheckedItems.Count != 0)
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

                var frm = new AddConfirmForm(installList);
                frm.Location = this.Location;
                frm.StartPosition = FormStartPosition.Manual;
                frm.FormClosed += CloseForm;
                frm.Show();
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

            List<string> projModules = new List<string>();  // Liste des modules du projet
            List<string> repoList = new List<string>();     // Liste des dépôts distants

            try
            {
                // Si possible, récupère la liste des dépôts distant déjà chargée dans le MainForm
                repoList = MainForm.repoList.ToList();
            }
            catch (Exception ex)
            {
                // Sinon la recharge (cas où l'on ajoute un module depuis SmartGit)
                repoList = functions.DispRepoList();
                Console.WriteLine(ex.Message);
            }

            int i = 0;
            try
            {
                // Récupère la liste des modules du projet (lecture sur projet distant)
                projModules = functions.GetModList("_DEV_", functions.GetProjFullName(repo).Replace(".git", ""));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            
            repo = repo.Substring(repo.LastIndexOf("\\") + 1, repo.Length - repo.LastIndexOf("\\") - 1);

            repoList.Sort();
            i = 0;
            foreach (string mod in repoList)
            {
                int toAdd = 1;
                
                foreach (string module in projModules)
                {
                    if (module.Contains(mod.Replace(".git", "")))
                    {
                        // Si le module est déjà ajouté au projet, ajoute un indicateur et grise la case
                        checkedListBox1.Invoke(new Action(() => checkedListBox1.Items.Add(mod.Replace(".git", "") + " (✓)", true)));
                        checkedListBox1.Invoke(new Action(() => checkedListBox1.SetItemCheckState(i, CheckState.Indeterminate)));
                        toAdd = 0;
                        break;
                    }
                }
                
                if (toAdd == 1)
                {
                    checkedListBox1.Invoke(new Action(() => checkedListBox1.Items.Add(mod.Replace(".git", ""))));
                }

                i++;
                worker.ReportProgress(i * 100 / repoList.Count());
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
