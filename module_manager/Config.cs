using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace module_manager
{
    public class Config
    {
        public string GetServersPath()
        {
            string path = GetAppData() + @"servers.json";
            return path;
        }

        public string GetConfigPath()
        {
            string path = GetAppData() + @"config.json";
            return path;
        }

        public string GetSettingsPath()
        {
            string path = GetAppData() + @"settings.json";
            return path;
        }

        public string GetAppData()
        {
            string path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\ModuleManager\";
            Directory.CreateDirectory(path);
            return path;
        }

        public string GetSmartGitRepo()
        {
            string json = File.ReadAllText(GetSettingsPath());
            byte[] bytes = Encoding.Default.GetBytes(json);
            json = Encoding.UTF8.GetString(bytes);
            JObject sett = JObject.Parse(json);
            try
            {
                return (string)sett["smartgit"];
            }
            catch (Exception)
            {
                return "";
            }
            
        }

        public string GetSourceTreeRepo()
        {
            string json = File.ReadAllText(GetSettingsPath());
            byte[] bytes = Encoding.Default.GetBytes(json);
            json = Encoding.UTF8.GetString(bytes);
            JObject sett = JObject.Parse(json);
            try
            {
                return (string)sett["sourcetree"];
            }
            catch (Exception)
            {
                return "";
            }
        }

        public string GetLocalRepo()
        {
            string json = File.ReadAllText(GetSettingsPath());
            byte[] bytes = Encoding.Default.GetBytes(json);
            json = Encoding.UTF8.GetString(bytes);
            JObject sett = JObject.Parse(json);
            try
            {
                return (string)sett["local"];
            }
            catch (Exception)
            {
                return "";
            }
        }

        /// <summary>
        /// Retourne le contenu du fichier des serveurs enregistrés
        /// </summary>
        public string DispServerList()
        {
            string json;
            json = File.ReadAllText(GetServersPath());
            byte[] bytes = Encoding.Default.GetBytes(json);
            json = Encoding.UTF8.GetString(bytes);
            return json;
        }

        /// <summary>
        /// Retourne le type de serveur (source) en cours (gitblit, bitbucket...)
        /// </summary>
        public string GetCurrentType()
        {
            string json = File.ReadAllText(GetConfigPath());
            byte[] bytes = Encoding.Default.GetBytes(json);
            json = Encoding.UTF8.GetString(bytes);
            JObject serv = JObject.Parse(json);
            return (string)serv["type"];
        }

        /// <summary>
        ///  Retourne le nom de la source en cours
        /// </summary>
        public string GetCurrentSource()
        {
            string json = File.ReadAllText(GetConfigPath());
            byte[] bytes = Encoding.Default.GetBytes(json);
            json = Encoding.UTF8.GetString(bytes);
            JObject serv = JObject.Parse(json);
            return (string)serv["source"];
        }

        /// <summary>
        ///  Retourne l'URL du serveur en cours
        /// </summary>
        public string GetServerUrl()
        {
            string json = File.ReadAllText(GetConfigPath());
            byte[] bytes = Encoding.Default.GetBytes(json);
            json = Encoding.UTF8.GetString(bytes);
            JObject serv = JObject.Parse(json);
            return (string)serv["url"];
        }

        /// <summary>
        /// Retourne l'URL du serveur correspondant au nom donné
        /// </summary>
        public string GetServerUrl(string name)
        {
            string json = File.ReadAllText(GetServersPath());
            JObject conf = JObject.Parse(json);
            JArray list = (JArray)conf["servers"];
            foreach (JObject serv in list)
            {
                if (serv["name"].ToString() == name)
                {
                    return serv["url"].ToString();
                }
            }
            return "";
        }

        /// <summary>
        /// Retourne le nom d'utilisateur du serveur en cours
        /// </summary>
        public string GetUserName()
        {
            string json = File.ReadAllText(GetConfigPath());
            byte[] bytes = Encoding.Default.GetBytes(json);
            json = Encoding.UTF8.GetString(bytes);
            JObject serv = JObject.Parse(json);
            return (string)serv["username"];
        }

        /// <summary>
        /// Retourne le nom d'utilisateur du serveur correspondant au nom donné
        /// </summary>
        public string GetUserName(string name)
        {
            string json = File.ReadAllText(GetServersPath());
            JObject conf = JObject.Parse(json);
            JArray list = (JArray)conf["servers"];
            foreach (JObject serv in list)
            {
                if (serv["name"].ToString() == name)
                {
                    return serv["username"].ToString();
                }
            }
            return "";
        }

        /// <summary>
        /// Retourne le nom de tous les serveurs enregistrés
        /// </summary>
        public List<string> GetAllNames()
        {
            List<string> allNames = new List<string>();
            string json = File.ReadAllText(GetServersPath());
            JObject conf = JObject.Parse(json);
            JArray list = (JArray)conf["servers"];
            foreach (JObject serv in list)
            {
                allNames.Add(serv["name"].ToString());
            }
            return allNames;
        }

        /// <summary>
        /// Retourne l'identifiant unique de tous les serveurs enregistrés
        /// </summary>
        public List<string> GetAllUniques()
        {
            List<string> allUniques = new List<string>();
            string json = File.ReadAllText(GetServersPath());
            JObject conf = JObject.Parse(json);
            JArray list = (JArray)conf["servers"];
            foreach (JObject serv in list)
            {
                allUniques.Add(serv["unique"].ToString());
            }
            return allUniques;
        }

        /// <summary>
        /// Retourne le type de tous les serveurs enregistrés
        /// </summary>
        public List<string> GetAllTypes()
        {
            List<string> allTypes = new List<string>();
            string json = File.ReadAllText(GetServersPath());
            JObject conf = JObject.Parse(json);
            JArray list = (JArray)conf["servers"];
            foreach (JObject serv in list)
            {
                allTypes.Add(serv["type"].ToString());
            }
            return allTypes;
        }

        /// <summary>
        /// Retourne le client (smartgit, sourcetree ou local)
        /// </summary>
        public string GetClient()
        {
            string json = File.ReadAllText(GetConfigPath());
            byte[] bytes = Encoding.Default.GetBytes(json);
            json = Encoding.UTF8.GetString(bytes);
            JObject serv = JObject.Parse(json);
            return (string)serv["client"];
        }

        /// <summary>
        /// Retourne "true" si un mot de passe a déjà été enregistré, "false" sinon
        /// </summary>
        public string GetPass()
        {
            string json = File.ReadAllText(GetConfigPath());
            byte[] bytes = Encoding.Default.GetBytes(json);
            json = Encoding.UTF8.GetString(bytes);
            JObject serv = JObject.Parse(json);
            return (string)serv["password"];
        }

        /// <summary>
        /// Change la configuration "password" à "true" si un mot de passe vient d'être enregistré
        /// </summary>
        public void SetPass(string pass)
        {
            string json;
            json = File.ReadAllText(GetConfigPath());
            JObject conf = JObject.Parse(json);
            conf["password"] = pass;
            File.WriteAllText(GetConfigPath(), conf.ToString());
        }

        /// <summary>
        /// Modifie le fichier config.json avec les valeurs du serveur choisi
        /// </summary>
        public void ChangeServer(string name)
        {
            Console.WriteLine("change to " + name);
            string json;
            json = File.ReadAllText(GetConfigPath());
            JObject conf = JObject.Parse(json);
            conf["source"] = name;

            string servs = File.ReadAllText(GetServersPath());
            JObject servlist = JObject.Parse(servs);
            foreach (JObject obj in servlist["servers"])
            {
                if ((string)obj["name"] == name)
                {
                    conf["type"] = obj["type"];
                    conf["url"] = obj["url"];
                    conf["username"] = obj["username"];
                }
            }
            File.WriteAllText(GetConfigPath(), conf.ToString());
        }

        /// <summary>
        /// Ajoute un nouveau serveur
        /// </summary>
        public bool AddServer(string type, string name, string url, string username)
        {
            List<string> allNames = GetAllNames();
            if (allNames.Contains(name))
                return false;
            string json;
            json = File.ReadAllText(GetServersPath());
            JObject conf = JObject.Parse(json);
            JArray list = (JArray)conf["servers"];
            var itemToAdd = new JObject();
            itemToAdd["type"] = type;
            itemToAdd["name"] = name;
            url = url.EndsWith("/") ? url : url + "/";
            itemToAdd["url"] = url.EndsWith("/") ? url : url+"/";
            itemToAdd["username"] = username;
            if(type == "gitblit")
            {
                Uri myUri = new Uri(url);
                itemToAdd["unique"] = myUri.Host;
            } else
            {
                url = url.Remove(url.Length - 1);
                itemToAdd["unique"] = url.Substring(url.LastIndexOf("/") + 1, url.Length - url.LastIndexOf("/") - 1);
            }
            list.Add(itemToAdd);
            File.WriteAllText(GetServersPath(), conf.ToString());
            return true;
        }

        /// <summary>
        /// Modifie un serveur existant
        /// </summary>
        public void EditServer(string oldName, string newName, string URL, string username)
        {
            string json;
            json = File.ReadAllText(GetServersPath());
            JObject conf = JObject.Parse(json);
            JArray list = (JArray)conf["servers"];
            foreach(JObject serv in list)
            {
                if(serv["name"].ToString() == oldName)
                {
                    serv["name"] = newName;
                    serv["url"] = URL;
                    serv["username"] = username;
                    if (serv["type"].ToString() == "gitblit")
                    {
                        Uri myUri = new Uri(URL);
                        serv["unique"] = myUri.Host;
                    }
                    else
                    {
                        URL = URL.Remove(URL.Length - 1);
                        serv["unique"] = URL.Substring(URL.LastIndexOf("/") + 1, URL.Length - URL.LastIndexOf("/") - 1);
                    }
                    if (File.Exists(Path.Combine(GetAppData(), ".cred" + oldName)))
                        File.Move(Path.Combine(GetAppData(), ".cred" + oldName), Path.Combine(GetAppData(), ".cred" + newName));
                    break;
                }
            }
            File.WriteAllText(GetServersPath(), conf.ToString());
        }

        /// <summary>
        /// Supprime un serveur
        /// </summary>
        public void DeleteServer(string name)
        {
            string json;
            json = File.ReadAllText(GetServersPath());
            JObject conf = JObject.Parse(json);
            JArray list = (JArray)conf["servers"];
            int i = 0;
            foreach (JObject serv in list)
            {
                if (serv["name"].ToString() == name)
                {
                    list.RemoveAt(i);
                    break;
                }
                i++;
            }
            File.WriteAllText(GetServersPath(), conf.ToString());
            File.Delete(GetAppData() + ".cred" + name);
        }

        public string GetBranchDev()
        {
            string json = File.ReadAllText(GetSettingsPath());
            byte[] bytes = Encoding.Default.GetBytes(json);
            json = Encoding.UTF8.GetString(bytes);
            JObject sett = JObject.Parse(json);
            try
            {
                return (string)sett["dev"];
            }
            catch (Exception)
            {
                return "_DEV_";
            }
        }

    }
}
