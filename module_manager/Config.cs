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
            List<string> servList = new List<string>();
            string json;
            json = File.ReadAllText(GetServersPath());
            byte[] bytes = Encoding.Default.GetBytes(json);
            json = Encoding.UTF8.GetString(bytes);
            JObject serv = JObject.Parse(json);
            foreach (JObject obj in serv["servers"])
            {
                if ((string)obj["name"] == GetCurrentSource())
                    return (string)obj["url"];
            }
            return "null";
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
                }
            }
            File.WriteAllText(GetConfigPath(), conf.ToString());
        }

        public string GetBranchDev()
        {
            return "_DEV_";
        }
    }
}
