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
            string path = @"C:\Users\STBE\Downloads\SmartGit\.settings\repositories.xml";
            return path;
        }

        public string GetSourceTreeRepo()
        {
            string path = @"C:\Users\STBE\AppData\Local\Atlassian\SourceTree\opentabs.xml";
            return path;
        }

        /**
         * Retoure la string du fichier des serveurs enregistrés
         */
        public string DispServerList()
        {
            string json;
            json = File.ReadAllText(GetServersPath());

            byte[] bytes = Encoding.Default.GetBytes(json);
            json = Encoding.UTF8.GetString(bytes);

            return json;
        }

        /**
         * Retourne le type de serveur en cours (gitblit, bitbucket...)
         */
        public string GetCurrentType()
        {
            string json = File.ReadAllText(GetConfigPath());
            byte[] bytes = Encoding.Default.GetBytes(json);
            json = Encoding.UTF8.GetString(bytes);
            JObject serv = JObject.Parse(json);
            return (string)serv["type"];
        }

        /**
         * Retourne le nom de la source en cours
         */
        public string GetCurrentSource()
        {
            string json = File.ReadAllText(GetConfigPath());
            byte[] bytes = Encoding.Default.GetBytes(json);
            json = Encoding.UTF8.GetString(bytes);
            JObject serv = JObject.Parse(json);
            return (string)serv["source"];
        }

        /**
         * Retourne l'URL du serveur en cours
         */
        public string GetServerUrl()
        {
            string json = File.ReadAllText(GetConfigPath());
            byte[] bytes = Encoding.Default.GetBytes(json);
            json = Encoding.UTF8.GetString(bytes);
            JObject serv = JObject.Parse(json);
            return (string)serv["url"];
        }

        public string GetUserName()
        {
            string json = File.ReadAllText(GetConfigPath());
            byte[] bytes = Encoding.Default.GetBytes(json);
            json = Encoding.UTF8.GetString(bytes);
            JObject serv = JObject.Parse(json);
            return (string)serv["username"];
        }

        public string GetClient()
        {
            string json = File.ReadAllText(GetConfigPath());
            byte[] bytes = Encoding.Default.GetBytes(json);
            json = Encoding.UTF8.GetString(bytes);
            JObject serv = JObject.Parse(json);
            return (string)serv["client"];
        }

        public string GetPass()
        {
            string json = File.ReadAllText(GetConfigPath());
            byte[] bytes = Encoding.Default.GetBytes(json);
            json = Encoding.UTF8.GetString(bytes);
            JObject serv = JObject.Parse(json);
            return (string)serv["password"];
        }

        public void SetPass(string pass)
        {
            string json;
            json = File.ReadAllText(GetConfigPath());
            JObject conf = JObject.Parse(json);
            conf["password"] = pass;
            File.WriteAllText(GetConfigPath(), conf.ToString());
        }

        /**
         * Modifie le fichier config.json avec les valeurs du serveur choisi
         */
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

        public void AddServer(string type, string name, string url, string username)
        {
            string json;
            json = File.ReadAllText(GetServersPath());
            JObject conf = JObject.Parse(json);
            JArray list = (JArray)conf["servers"];
            var itemToAdd = new JObject();
            itemToAdd["type"] = type;
            itemToAdd["name"] = name;
            itemToAdd["url"] = url;
            itemToAdd["username"] = username;
            list.Add(itemToAdd);
            File.WriteAllText(GetServersPath(), conf.ToString());
        }

        public string GetBranchDev()
        {
            return "_DEV_";
        }

        public string GetToken()
        {
            using (Password formOptions = new Password())
            {
                formOptions.ShowDialog();
                try
                {
                    string result = formOptions.pass;
                    if (result.Length != 0)
                    {
                        return result;
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            return "";
        }
    }
}
