using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace module_manager
{
    public partial class Form2 : Form
    {

        private string repo;

        public Form2(string[] args)
        {
            repo = args[0];
            InitializeComponent();
            toolStripStatusLabel2.Text = repo;
            toolStripStatusLabel1.Text = "Chargement...";
            Thread getModList = new Thread(new ThreadStart(ThreadLoop));
            getModList.Start();
        }



        private void MetroLabel1_Click(object sender, EventArgs e)
        {

        }

        private void ThreadLoop()
        {
            List<string> projModules = new List<string>();
            List<string> repoList = Form1.repoList;
            List<List<string>> projList = Form1.projList;
            repo = repo.Substring(repo.LastIndexOf("\\") + 1, repo.Length - repo.LastIndexOf("\\") - 1);
            
            int i = 0;
            foreach (string rep in repoList)
            {
                if (rep.Contains(repo))
                {
                    projModules = projList.ElementAt(i);
                    break;
                }
                i++;
            }

            repoList.Sort();
            i = 0;
            foreach (string mod in repoList)
            {
                int toAdd = 1;
                
                foreach (string module in projModules)
                {
                    if (module.Contains(mod.Replace(".git", "")))
                    {
                        checkedListBox1.Invoke(new Action(() => checkedListBox1.Items.Add(mod.Replace(".git", "") + " (✓)", true)));
                        checkedListBox1.Invoke(new Action(() => checkedListBox1.SetItemCheckState(i,CheckState.Indeterminate)));
                        toAdd = 0;
                        break;
                    }
                }
                if(toAdd == 1)
                {
                    checkedListBox1.Invoke(new Action(() => checkedListBox1.Items.Add(mod.Replace(".git", ""))));
                }
                i++;
                ReportProgress(i * 100 / repoList.Count());
            }

            ReportProgress(0);

        }

        private void ReportProgress(int value)
        {
            statusStrip1.Invoke(new Action(() => toolStripProgressBar1.Value = value));
            
            if (value == 0)
                statusStrip1.Invoke(new Action(() => toolStripStatusLabel1.Text = "Prêt"));
        }

        private void CheckedListBox1_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            List<string> allItems = checkedListBox1.Items.OfType<string>().ToList();
            string curItem = checkedListBox1.SelectedItem.ToString();
            metroLabel2.Text = curItem;
        }

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

        private void MetroButton1_Click(object sender, EventArgs e)
        {
            StringBuilder sb = new StringBuilder();
            List<string> installList = new List<string>();
            sb.AppendLine("les modules suivant vont être ajoutés au projet " + repo + " :");
            sb.AppendLine("");
            foreach (object item in checkedListBox1.CheckedItems)
            {
                if(!item.ToString().Contains("(✓)"))
                {
                    installList.Add(item.ToString());
                    sb.AppendLine(item.ToString());
                }

            }
            if(MessageBox.Show(sb.ToString(),"Ajouter des modules", MessageBoxButtons.OKCancel, MessageBoxIcon.Information) == DialogResult.OK)
            {
                foreach(string module in installList)
                {
                    Process process = new Process();
                    process.StartInfo.FileName = @"C:\Users\STBE\Downloads\PortableGit\git-bash.exe";
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    process.StartInfo.WorkingDirectory = @toolStripStatusLabel2.Text;
                    process.StartInfo.Arguments = @"C:\Users\STBE\Downloads\PortableGit\home\TestMaster\clone.sh http://192.168.55.218:8082/r/" + module + " " + module;
                    process.Start();
                    process.WaitForExit();
                }
            }
        }
    }
}
