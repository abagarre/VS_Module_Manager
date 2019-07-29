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
        public static string path;  // Chemin du répertoire du projet sélectionné
        public static List<Repo> moduleList = new List<Repo>(); // Liste de tous les modules du serveur
        Repo proj;
        List<Repo> repoList = new List<Repo>();
        Functions functions;
        Config config;

        public AddSubForm(Repo repo)
        {
            InitializeComponent();
            proj = repo;
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            toolStripStatusLabel2.Text = proj.Path;
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
                repoList = MainForm.repoList.ToList();
            }
            catch (Exception)
            {
                Console.WriteLine("Can't retreive repoList from MainForm");
                repoList = functions.GetRepoList();
            }

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
                        installList.Add(item.ToString().Substring(0,item.ToString().IndexOf("(")-1));
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

            int i = 0, j = 0;

            List<Repo> modList = repoList.OrderBy(mod => mod.Name).ToList();
            i = 0;
            foreach (Repo mod in modList)
            {
                if(mod.Type == "module")
                {
                    int toAdd = 1;

                    if (mod.IsInList(proj.Modules))
                    {
                        // Si le module est déjà ajouté au projet, ajoute un indicateur et grise la case
                        checkedListBox1.Invoke(new Action(() => checkedListBox1.Items.Add(mod.Name + " (" + mod.Server + ")" + " (✓)", true)));
                        checkedListBox1.Invoke(new Action(() => checkedListBox1.SetItemCheckState(i, CheckState.Indeterminate)));
                        toAdd = 0;
                    }

                    if (toAdd == 1)
                    {
                        checkedListBox1.Invoke(new Action(() => checkedListBox1.Items.Add(mod.Name + " (" + mod.Server + ")")));
                    }

                    i++;
                    
                }
                j++;
                worker.ReportProgress(j * 100 / modList.Count());

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
