﻿using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Windows.Forms;
using System.Text;
using System.Security.Cryptography;
using module_manager;
using HtmlAgilityPack;
using System.Xml.Linq;
using System.Linq;

public class Functions
{
    
    public static bool affiche = false;
    Config config = new Config();
    string entropy = "";

    public string Query(string type, string url)
    {
        if (entropy == "")
        {
            using (Password formOptions = new Password())
            {
                formOptions.ShowDialog();
                try
                {
                    if (formOptions.pass.Length != 0)
                        entropy = formOptions.pass;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }
        try
        {
            byte[] ciphertext = File.ReadAllBytes(config.GetAppData() + @".cred" + config.GetCurrentSource());
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            WebRequest myReq = WebRequest.Create(url);
            myReq.Method = "GET";
            CredentialCache mycache = new CredentialCache();
            if(type == "bitbucket")
            {
                myReq.Headers["Authorization"] = "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes(config.GetUserName() + ":" + Encoding.UTF8.GetString(ProtectedData.Unprotect(ciphertext, Encoding.Default.GetBytes(entropy), DataProtectionScope.CurrentUser))));

            } else if(type == "devops")
            {
                myReq.Headers["Authorization"] = "Basic " + Convert.ToBase64String(
                ASCIIEncoding.ASCII.GetBytes(
                    string.Format("{0}:{1}", "", Encoding.UTF8.GetString(ProtectedData.Unprotect(ciphertext, Encoding.Default.GetBytes(entropy), DataProtectionScope.CurrentUser)))));
            }
            WebResponse wr = myReq.GetResponse();
            Stream receiveStream = wr.GetResponseStream();
            StreamReader reader = new StreamReader(receiveStream, Encoding.UTF8);
            return reader.ReadToEnd();
        }
        catch (CryptographicException ex)
        {
            entropy = "";
            if (MessageBox.Show("Mot de passe incorrect", "Erreur", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error) == DialogResult.Retry)
            {
                Query(type,url);
            }
            Console.WriteLine(type + " query error : " + ex.Message);
        }
        return "";

    }

    /**
     * Retourne la liste des modules présents dans le fichier .gitmodules d'un projet local
     */
    public List<string> GetGitmodulesLoc(string path)
    {
        List<string> submodules = new List<string>();
        try
        {
            foreach (string f in Directory.GetFiles(path))
            {
                if (f.Contains(".gitmodules"))
                {
                    int counter = 0;
                    string line;
                    StreamReader file = new StreamReader(f);
                    while ((line = file.ReadLine()) != null)
                    {
                        if (line.Contains("url"))
                        {
                            string name = line.Replace(".git", "");
                            submodules.Add(name.Substring(name.LastIndexOf("/") + 1, name.Length - name.LastIndexOf("/") - 1));
                        }
                        counter++;
                    }
                    file.Close();
                }
            }
        }
        catch (Exception excpt)
        {
            Console.WriteLine(excpt.Message);
        }

        return submodules;
    }

    internal List<string> GetLocalList(string path)
    {
        List<string> localList = new List<string>();
        string[] subdir = Directory.GetDirectories(path);
        foreach(string dir in subdir)
        {
            if(Directory.Exists(Path.Combine(dir,".git")))
            {
                localList.Add(dir);
            }
            else
            {
                localList.AddRange(GetLocalList(dir));
            }
        }
        return localList; 
    }

    public List<string> GetGitmodulesLocPath(string path)
    {
        List<string> submodules = new List<string>();
        try
        {
            foreach (string f in Directory.GetFiles(path))
            {
                if (f.Contains(".gitmodules"))
                {
                    int counter = 0;
                    string line;
                    StreamReader file = new StreamReader(f);
                    while ((line = file.ReadLine()) != null)
                    {
                        if (line.Contains("path = "))
                        {
                            string name = line.Replace("path = ", "").Trim();
                            submodules.Add(name);
                        }
                        counter++;
                    }
                    file.Close();
                }
            }
        }
        catch (Exception excpt)
        {
            Console.WriteLine(excpt.Message);
        }

        return submodules;
    }

    public List<string> BitBucketRepoList()
    {
        List<string> repoList = new List<string>();
        string url = "https://api.bitbucket.org/2.0/repositories/" + config.GetUserName();
        string json = Query("bitbucket",url);
        Console.WriteLine(json);
        if(json != "")
        {
            JObject obj = JObject.Parse(json);
            JArray list = (JArray)obj["values"];
            foreach (JObject ob in list)
            {
                string name = ((string)ob["name"]).Replace(".git", "");
                repoList.Add(name.Substring(name.LastIndexOf("/") + 1, name.Length - name.LastIndexOf("/") - 1));
            }
        }
        return repoList;
    }

    public List<string> GitBlitRepoList()
    {
        List<string> repoList = new List<string>();

        string json;
        using (var client = new WebClient())
        {
            json = client.DownloadString(config.GetServerUrl() + "rpc/?req=LIST_REPOSITORIES");
        }

        byte[] bytes = Encoding.Default.GetBytes(json);
        json = Encoding.UTF8.GetString(bytes);

        JObject list = JObject.Parse(json);
        var properties = list.Properties();
        foreach (var prop in properties)
        {
            JObject infos = JObject.FromObject(list[prop.Name]);
            if (!((string)infos["name"]).Contains("~"))
            {
                string name = infos["name"].ToString().Replace(".git", "");
                repoList.Add(name);
            }
        }
        return repoList;
    }

    public List<string> DevOpsRepoList()
    {
        List<string> repoList = new List<string>();
        string json;
        try
        {
            //=====================================================================================//
            json = Query("devops", config.GetServerUrl() + "_apis/git/repositories?api-version=5.0");
            //=====================================================================================//
            if (json != "")
            {
                JObject obj = JObject.Parse(json);
                JArray list = (JArray)obj["value"];
                foreach (JObject jObject in list)
                {
                    Console.WriteLine(jObject["name"].ToString());
                    repoList.Add(jObject["name"].ToString());
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
        /*
        //=========================================== MODULES DEVOPS ====================================================================//
        try
        {
            json = Query("devops", "https://dev.azure.com/" + config.GetUserName() + "/KIMOdules/_apis/git/repositories?api-version=5.0");
            if (json != "")
            {
                JObject obj = JObject.Parse(json);
                JArray list = (JArray)obj["value"];
                foreach (JObject jObject in list)
                {
                    repoList.Add(jObject["name"].ToString());
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
        //=========================================== MODULES DEVOPS ====================================================================//
        */
        return repoList;
    }

    public List<string> GitHubRepoList()
    {
        List<string> repoList = new List<string>();
        string url = "https://api.github.com/users/" + config.GetUserName() + "/repos";
        Console.WriteLine(url);
        string json = "";
        try
        {
            using (var client = new WebClient())
            {
                json = client.DownloadString(url);
            }
        }
        catch(Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        
        Console.WriteLine(json);
        if (json != "")
        {
            JArray list = JArray.Parse(json);
            foreach (JObject ob in list)
            {
                string name = ((string)ob["name"]).Replace(".git", "");
                repoList.Add(name.Substring(name.LastIndexOf("/") + 1, name.Length - name.LastIndexOf("/") - 1));
            }
        }
        return repoList;
    }

    /**
     * Retourne la liste des dépôts du serveur
     */
    public List<string> GetRepoList()
    {
        List<string> repoList = new List<string>();
        string currentType = config.GetCurrentType();
        if (currentType == "gitblit")
        {
            return GitBlitRepoList();
        }
        else if (currentType == "bitbucket")
        {
            return BitBucketRepoList();
        }
        else if (currentType == "devops")
        {
            return DevOpsRepoList();
        }
        else if (currentType == "github")
        {
            return GitHubRepoList();
        }
        return repoList;
    }

    /**
     * Retourne la liste des modules du serveur (<=> contient MODULES dans le nom du dépôt)
     */
    public List<string> GetModuleList(List<string> repoList)
    {
        List<string> moduleList = new List<string>();
        foreach(string rep in repoList)
        {
            //=============== MODULES FOLDER NAME ==================//
            if (rep.Contains("MODULES"))
            //======================================================//
                moduleList.Add(rep);
        }
        return moduleList;
    }

    /**
     * Liste les fichiers distants du dépôt d'un module et récupère la liste des #include
     */
    public List<string> GetModuleDep(string moduleText, string branch)
    {
        List<string> dep = new List<string>();
        string currentType = config.GetCurrentType();
        if(currentType == "gitblit")
        {
            string url = config.GetServerUrl() + "raw/" + moduleText + "/" + branch;
            try {
                string html;
                using (var client = new WebClient())
                {
                    html = client.DownloadString(url);
                }
                HtmlAgilityPack.HtmlDocument document = new HtmlAgilityPack.HtmlDocument();
                document.LoadHtml(html);
                if (html.Contains("branch!") && branch != "master")
                {
                    return GetModuleDep(moduleText, "master");
                }
                HtmlNodeCollection collection = document.DocumentNode.SelectNodes("//a");
                foreach (HtmlNode link in collection)
                {
                    string target = link.Attributes["href"].Value;
                    if (target.Contains(".c") || target.Contains(".h"))
                    {
                        string serverUrl = config.GetServerUrl().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                        dep.AddRange(GetIncludes(serverUrl + target));
                    }
                }
            } catch (Exception) { }
            
        }
        else if(currentType == "bitbucket")
        {
            string url = "https://api.bitbucket.org/2.0/repositories/" + config.GetUserName() + "/" + moduleText + "/src/" + branch + "/";
            string json = "";
            try
            {
                json = Query("bitbucket",url);
            }
            catch (Exception)
            {
                if(branch != "master")
                    return GetModuleDep(moduleText, "master");
            }
            if(json != "")
            {
                JObject obj = JObject.Parse(json);
                JArray list = (JArray)obj["values"];
                foreach (JObject ob in list)
                {
                    if (((string)ob["path"]).Contains(".c") || ((string)ob["path"]).Contains(".h"))
                    {
                        dep.AddRange(GetIncludes("https://bitbucket.org/" + config.GetUserName() + "/" + moduleText + "/raw/" + branch + "/" + (string)ob["path"]));
                    }
                }
            }
            else if(branch != "master")
            {
                return GetModuleDep(moduleText, "master");
            }
            
        }
        else if(currentType == "devops")
        {
            string res = Query("devops", config.GetServerUrl() + "_apis/git/repositories/" + moduleText + "/items?recursionLevel=OneLevel&api-version=5.0");
            JObject obj = JObject.Parse(res);
            JArray list = (JArray)obj["value"];
            foreach(JObject ob in list)
            {
                if(ob["path"].ToString().Contains(".c") || ob["path"].ToString().Contains(".h"))
                {
                    string url = config.GetServerUrl() + "_apis/git/repositories/" + moduleText + "/items?path=" + ob["path"].ToString() + "&versionType=Branch&version=" + branch + "&includeContent=true&api-version=5.0";
                    try
                    {
                        Query("devops",url);
                    }
                    catch (Exception)
                    {
                        if (branch != "master")
                            return GetModuleDep(moduleText, "master");
                    }
                    dep.AddRange(GetIncludes(url));
                }
            }
        }
        else if (currentType == "github")
        {
            string res;
            using (var client = new WebClient())
            {
                res = client.DownloadString("https://api.github.com/repos/" + config.GetUserName() + "/" + moduleText + "/contents/");
            }
            JArray list = JArray.Parse(res);
            foreach (JObject ob in list)
            {
                if (ob["path"].ToString().Contains(".c") || ob["path"].ToString().Contains(".h"))
                {
                    string url = ob["download_url"].ToString();
                    dep.AddRange(GetIncludes(url));
                }
            }
        }
        return dep;
    }

    /**
     * Retourne la liste des #include d'un fichier distant
     */
    public List<string> GetIncludes(string url)
    {
        List<string> dep = new List<string>();
        string result = "";
        string currentType = config.GetCurrentType();
        if (currentType == "gitblit")
        {
            using (var wc = new WebClient())
                result = wc.DownloadString(url);
        }
        else if (currentType == "bitbucket")
        {
            result = Query("bitbucket",url);
        }
        else if (currentType == "devops")
        {
            result = Query("devops",url);
        }
        else if(currentType == "github")
        {
            using (var client = new WebClient())
            {
                result = client.DownloadString(url);
            }
        }


        using (StringReader reader = new StringReader(result))
        {
            string ligne;
            while ((ligne = reader.ReadLine()) != null)
            {
                if (!ligne.Contains("Error") && ligne.Contains("#include \""))
                {
                    string module = ligne.Replace("\"", "").Replace("#include ", "").Replace(" ", "");
                    if (!url.Contains(module.Replace(".h", "")))
                        dep.Add(module);
                }
            }
        }

        return dep;
    }

    /**
     * Ouvre une fenêtre lorsqu'un dépôt n'a pas de branche _DEV_
     */
    public static bool ShowDialog(string text, string caption)
    {
        Form prompt = new Form
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowOnly,
            Padding = new Padding(20, 20, 20, 0),
            Size = new System.Drawing.Size(300, 200)
        };
        CheckBox chk = new CheckBox { Text = text };
        Label label = new Label
        {
            Text = caption,
            AutoSize = true
        };
        Button ok = new Button() { Text = "OK" };
        ok.Click += (sender, e) => { prompt.Close(); };
        FlowLayoutPanel panel = new FlowLayoutPanel { Dock = DockStyle.Fill };
        panel.Controls.Add(label);
        panel.Controls.Add(chk);
        panel.SetFlowBreak(chk, true);
        panel.Controls.Add(ok);
        prompt.Controls.Add(panel);
        prompt.ShowDialog();
        return chk.Checked;
    }

    public List<string> GetSubGitblit(string branch, string rep)
    {
        List<string> modList = new List<string>();
        string url = config.GetServerUrl() + "raw/" + rep + "/" + branch + "/.gitmodules";
        try
        {
            var client = new WebClient();
            using (var stream = client.OpenRead(url))
            using (var reader = new StreamReader(stream))
            {
                string ligne;
                string ligne1 = "", ligne2 = "", ligne3 = "";
                int compteur = 1;
                while ((ligne = reader.ReadLine()) != null)
                {
                    if (ligne.Contains("branch!") && branch != "master")
                    {
                        if (affiche)
                        {
                            affiche = !ShowDialog("Ne plus afficher", "Le projet " + rep + " ne contient pas de branche " + config.GetBranchDev() + " ! \n\nRecherche sur la branche master\n ");
                        }
                        return GetSubmodList("master", rep);
                    }
                    switch (compteur)
                    {
                        case 1:
                            ligne1 = ligne;
                            break;
                        case 2:
                            ligne2 = ligne;
                            break;
                        case 3:
                            ligne3 = ligne;
                            compteur = 1;
                            break;
                    }
                    if (ligne.Contains("url = "))
                    {
                        string name = ligne.Replace("url = ", "").Replace(".git", "");
                        modList.Add(name.Substring(name.LastIndexOf("/") + 1, name.Length - name.LastIndexOf("/") - 1));
                    }
                }
            }
        }
        catch (Exception) { }
        return modList;
    }

    public List<string> GetSubBitBucket(string branch, string rep)
    {
        List<string> modList = new List<string>();
        string result;
        try
        {
            result = Query("bitbucket","https://bitbucket.org/" + config.GetUserName() + "/" + rep + "/raw/" + branch + "/.gitmodules");
        }
        catch (Exception ex)
        {
            Console.WriteLine("GetSubmodError : " + ex.Message);
            if (branch != "master")
                return GetSubmodList("master", rep);
            result = "null";
        }
        if (result.Length == 0 && branch != "master")
            return GetSubmodList("master", rep);
        using (StringReader reader = new StringReader(result))
        {
            string ligne;
            while ((ligne = reader.ReadLine()) != null)
            {
                if (ligne.Contains("dashboard") && branch != "master")
                {
                    if (affiche)
                    {
                        affiche = !ShowDialog("Ne plus afficher", "Le projet " + rep + " ne contient pas de branche " + config.GetBranchDev() + " ! \n\nRecherche sur la branche master\n ");
                    }
                    return GetSubmodList("master", rep);
                }
                if (ligne.Contains("url = "))
                {
                    string name = ligne.Replace("url = ", "").Replace(".git", "").Trim();
                    modList.Add(name.Substring(name.LastIndexOf("/") + 1, name.Length - name.LastIndexOf("/") - 1));
                }
            }
        }
        return modList;
    }

    public List<string> GetSubDevOps(string branch, string rep)
    {
        List<string> modList = new List<string>();
        string result;
        try
        {
            result = Query("devops", config.GetServerUrl() + "_apis/git/repositories/" + rep + "/items?path=.gitmodules&includeContent=true&api-version=5.0");
        }
        catch (Exception ex)
        {
            Console.WriteLine("GetSubmodError : " + ex.Message);
            if (branch != "master")
                return GetSubmodList("master", rep);
            result = "null";
        }
        if (result.Length == 0 && branch != "master")
            return GetSubmodList("master", rep);

        using (StringReader reader = new StringReader(result))
        {
            string ligne;
            while ((ligne = reader.ReadLine()) != null)
            {
                if (ligne.Contains("url = "))
                {
                    string name = ligne.Replace("url = ", "").Replace(".git", "").Trim();
                    modList.Add(name.Substring(name.LastIndexOf("/") + 1, name.Length - name.LastIndexOf("/") - 1));
                }
            }
        }
        return modList;
    }

    public List<string> GetSubGitHub(string branch, string rep)
    {
        List<string> modList = new List<string>();
        string result;
        try
        {
            using (var client = new WebClient())
            {
                result = client.DownloadString("https://raw.githubusercontent.com/" + config.GetUserName() + "/" + rep + "/" + branch + "/.gitmodules");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("GetSubmodError : " + ex.Message);
            if (branch != "master")
                return GetSubmodList("master", rep);
            result = "null";
        }
        if (result.Length == 0 && branch != "master")
            return GetSubmodList("master", rep);

        using (StringReader reader = new StringReader(result))
        {
            string ligne;
            while ((ligne = reader.ReadLine()) != null)
            {
                if (ligne.Contains("url = "))
                {
                    string name = ligne.Replace("url = ", "").Replace(".git", "").Trim();
                    modList.Add(name.Substring(name.LastIndexOf("/") + 1, name.Length - name.LastIndexOf("/") - 1));
                }
            }
        }
        return modList;
    }

    /**
     * Retourne la liste des modules présents dans le fichier .gitmodules d'un projet distant
     */
    public List<string> GetSubmodList(string branch, string rep)
    {
        List<string> modList = new List<string>();
        string currentType = config.GetCurrentType();
        if(currentType == "gitblit")
        {
            return GetSubGitblit(branch, rep);
        }
        else if (currentType == "bitbucket")
        {
            return GetSubBitBucket(branch, rep);
        }
        else if (currentType == "devops")
        {
            return GetSubDevOps(branch, rep);
        }
        else if(currentType == "github")
        {
            return GetSubGitHub(branch, rep);
        }
        return modList;
    }

    /**
     * Retourne le nom du projet avec le dossier qui le contient
     */
    public string GetProjFullName(string path)
    {
        string line;
        string prev = "";
        string fullName = "";
        StreamReader file = new StreamReader(path + @"\.git\config");
        while ((line = file.ReadLine()) != null)
        {
            if (line.Contains("remote"))
            {
                prev = line;
            }
            if(prev.Contains("remote") && line.Contains("url"))
            {
                fullName = line.Replace("url = ","");
                break;
            }
        }
        file.Close();
        string currentType = config.GetCurrentType();
        if(currentType == "gitblit")
        {
            //========================================= GITBLIT PATH =================================================//
            fullName = fullName.Substring(fullName.IndexOf(@"/_") + 1, fullName.Length - fullName.IndexOf(@"/_") - 1);
            //========================================================================================================//
        }
        else
        {
            fullName = fullName.Substring(fullName.LastIndexOf(@"/") + 1, fullName.Length - fullName.LastIndexOf(@"/") - 1);
        }
        return fullName;

    }

    public string GetProjURL(string path, string name, string type)
    {
        string line;
        string prev = "";
        string url = "";
        StreamReader file;
        try
        {
            file = new StreamReader(path + @"\.git\config");
        }
        catch (Exception) { return ""; }
        
        while ((line = file.ReadLine()) != null)
        {
            if(type == "module")
            {
                if (line.Contains("submodule") && line.Contains(name))
                    prev = line;
                if (prev.Contains("submodule") && line.Contains("url"))
                    return line.Replace("url = ", "");
            }
            else
            {
                if (line.Contains("remote"))
                    prev = line;
                if (prev.Contains("remote") && line.Contains("url"))
                    return line.Replace("url = ", "");
            }
            
        }
        return url;
    }

    /**
     * Retourne la liste des neouds cochés dans un TreeView
     */
    public List<string> GetCheckedNodes(TreeNodeCollection treeNode)
    {
        List<string> checkedList = new List<string>();

        foreach (TreeNode node in treeNode)
        {
            if (node.Checked)
            {
                checkedList.Add(node.Text);
            }

            if(node.Nodes.Count != 0)
            {
                checkedList.AddRange(GetCheckedNodes(node.Nodes));
            }
        }

        return checkedList;
    }

    public List<string> GetNodes(TreeNodeCollection treeNode)
    {
        List<string> checkedList = new List<string>();

        foreach (TreeNode node in treeNode)
        {
            
            checkedList.Add(node.Text);

            if (node.Nodes.Count != 0)
            {
                checkedList.AddRange(GetNodes(node.Nodes));
            }
        }

        return checkedList;
    }

    /**
     * Retourne le contenu d'un fichier README dans une chaîne de caractères
     */
    public string GetMarkdown(string projName, string branch)
    {
        string md = "";
        string currentType = config.GetCurrentType();
        if (currentType == "gitblit")
        {
            try
            {
                Console.WriteLine(config.GetServerUrl() + @"raw/" + projName + @".git/" + branch + @"/README.md");
                using (var wc = new WebClient())
                    md = wc.DownloadString(config.GetServerUrl() + @"raw/" + projName + @".git/" + branch + @"/README.md");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed downloading README.md : " + ex.Message);
                if (branch != "master")
                    return GetMarkdown(projName, "master");
            }
            if (md.Contains("branch") && branch != "master")
            {
                return GetMarkdown(projName, "master");
            }
            byte[] bytes = Encoding.Default.GetBytes(md);
            md = Encoding.UTF8.GetString(bytes);
        }
        else if(currentType == "bitbucket")
        {
            string url = "https://bitbucket.org/" + config.GetUserName() + "/" + projName + "/raw/" + branch + "/README.md";
            if(Query("bitbucket",url).Length == 0 && branch != "master")
            {
                return GetMarkdown(projName, "master");
            } else
            {
                return Query("bitbucket",url);
            }
        }
        else if(currentType == "devops")
        {
            Console.WriteLine(config.GetServerUrl() + "_apis/git/repositories/" + projName + "/items?path=README.md&includeContent=true&api-version=5.0");
            string result = Query("devops", config.GetServerUrl() + "_apis/git/repositories/" + projName + "/items?path=README.md&includeContent=true&api-version=5.0");
            /*
            JObject obj = JObject.Parse(result);
            result = (string)obj["content"];
            */
            if (result.Length == 0 && branch != "master")
            {
                return GetMarkdown(projName, "master");
            }
            else
            {
                return result;
            }
        }
        else if(currentType == "github")
        {
            try
            {
                using (var wc = new WebClient())
                    md = wc.DownloadString("https://raw.githubusercontent.com/" + config.GetUserName() + "/" + projName + "/" + branch + "/.gitmodules");
            }
            catch (Exception ex)
            {
                if(branch != "master")
                {
                    return GetMarkdown(projName, "master");
                }
                Console.WriteLine(ex.Message);
            }
        }
        return md;
    }

    public string GetMarkdownLoc(string projPath)
    {
        string md = "";
        try
        {
            md = File.ReadAllText(projPath + @"\README.md");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Local Markdown Exception (path : " + projPath + ") : " + ex.Message);
        }
        return md;
    }

    /**
     * 
     */
    public bool SavePassword(string token, string name)
    {
        byte[] plaintext = Encoding.Default.GetBytes(token);
        byte[] entropy;
        byte[] ciphertext;
        if(config.GetPass() == "true")
        {
            using (Password formOptions = new Password())
            {
                formOptions.ShowDialog();
                try
                {
                    string result = formOptions.pass;
                    if (result.Length != 0)
                    {
                        entropy = Encoding.Default.GetBytes(result);
                        ciphertext = ProtectedData.Protect(plaintext, entropy, DataProtectionScope.CurrentUser);
                        File.WriteAllBytes(config.GetAppData() + ".cred" + name, ciphertext);
                        return true;
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        } else
        {
            using (NewPassword formOptions = new NewPassword())
            {
                formOptions.ShowDialog();
                try
                {
                    string result = formOptions.pass;
                    if (result.Length != 0)
                    {
                        entropy = Encoding.Default.GetBytes(result);
                        ciphertext = ProtectedData.Protect(plaintext, entropy, DataProtectionScope.CurrentUser);
                        File.WriteAllBytes(config.GetAppData() + ".cred" + name, ciphertext);
                        config.SetPass("true");
                        return true;
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }
        
        return false;
    }

    public int GetIndex(TreeNode node)
    {
        /// source : David Fletcher

        int returnValue = 0;

        // Always make a way to exit the recursion.
        if (node.Index == 0 && node.Parent == null)
            return returnValue;

        // Now, count every node.
        returnValue = 1;

        // If I have siblings higher in the index, then count them and their decendants.
        if (node.Index > 0)
        {
            TreeNode previousSibling = node.PrevNode;
            while (previousSibling != null)
            {
                returnValue += GetDecendantCount(previousSibling);
                previousSibling = previousSibling.PrevNode;
            }
        }

        if (node.Parent == null)
            return returnValue;
        else
            return returnValue + GetIndex(node.Parent);
    }

    public int GetDecendantCount(TreeNode node)
    {
        int returnValue = 0;

        // If the node is not the root node, then we want to count it.
        if (node.Index != 0 || node.Parent != null)
            returnValue = 1;

        // Always make a way to exit a recursive function.
        if (node.Nodes.Count == 0)
            return returnValue;

        foreach (TreeNode childNode in node.Nodes)
        {
            returnValue += GetDecendantCount(childNode);
        }
        return returnValue;
    }

    public List<string> GetSmartGitList()
    {
        List<string> smartList = new List<string>();

        XDocument xml = XDocument.Load(config.GetSmartGitRepo());

        IEnumerable<XElement> ob = xml.Root.Elements();
        IEnumerable<XElement> coll = ob.ElementAt(0).Elements();

        for (int i = 0; i < coll.Count(); i++)
        {
            XElement obj = coll.ElementAt(i);
            var query = from c in obj.Descendants("prop")
                        where c.Attribute("key").Value == "path"
                        select new
                        {
                            path = c.Attribute("value").Value
                        };
            foreach (var path in query)
            {
                string chemin = path.path.Replace(@"\\", @"\");
                smartList.Add(chemin);
            }
        }

        return smartList;
    }

    public List<string> GetSourceTreetList()
    {
        List<string> sourceList = new List<string>();

        XDocument xml = XDocument.Load(config.GetSourceTreeRepo());

        foreach(XElement elem in xml.Root.Elements())
        {
            sourceList.Add(elem.Value.ToString());
        }

        return sourceList;
    }

}
