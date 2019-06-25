﻿using System;
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
        public static string path;
        Functions functions;

        public Form2(string[] args)
        {
            repo = args[0];
            InitializeComponent();
            //Thread getModList = new Thread(new ThreadStart(ThreadLoop));
            //getModList.Start();
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            toolStripStatusLabel2.Text = repo;
            path = repo;
            toolStripStatusLabel1.Text = "Chargement...";
            functions = new Functions();
            backgroundWorker1.RunWorkerAsync();
        }

        private void MetroLabel1_Click(object sender, EventArgs e)
        {

        }

        private void ThreadLoop()
        {
            List<string> projModules = new List<string>();
            List<string> repoList = Form1.repoList.ToList();
            List<List<string>> projList = Form1.projList.ToList();
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
                Thread.Sleep(500);
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
            if(checkedListBox1.CheckedItems.Count != 0)
            {
                List<string> installList = new List<string>();
                foreach (object item in checkedListBox1.CheckedItems)
                {
                    if (!item.ToString().Contains("(✓)"))
                    {
                        installList.Add(item.ToString());
                        // sb.AppendLine(item.ToString());
                    }
                }

                var frm = new Form3(installList);
                frm.Location = this.Location;
                frm.StartPosition = FormStartPosition.Manual;
                frm.Show();
            } else
            {
                MessageBox.Show("Veuillez sélectionner un module", "Aucun module séléctionné", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            
        }

        private void BackgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            List<string> projModules = new List<string>();
            List<string> repoList = Form1.repoList.ToList();
            List<List<string>> projList = Form1.projList.ToList();
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
