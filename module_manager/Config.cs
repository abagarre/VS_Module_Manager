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
        /**
         * Retourne le type de serveur en cours (gitblit, bitbucket...)
         */
        public string GetCurrentType()
        {
            string json = File.ReadAllText(@"C:\Users\STBE\source\repos\module_manager\module_manager\bin\Debug\config.json");
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
            string json = File.ReadAllText(@"C:\Users\STBE\source\repos\module_manager\module_manager\bin\Debug\config.json");
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
            if (GetCurrentType() == "gitblit")
            {
                List<string> servList = new List<string>();
                string json;
                json = File.ReadAllText(@"C:\Users\STBE\source\repos\module_manager\module_manager\bin\Debug\servers.json");
                byte[] bytes = Encoding.Default.GetBytes(json);
                json = Encoding.UTF8.GetString(bytes);
                JObject serv = JObject.Parse(json);
                foreach (JObject obj in serv["servers"])
                {
                    if ((string)obj["name"] == GetCurrentSource())
                        return (string)obj["url"];
                }
            }
            return "null";
        }

        /**
         * Modifie le fichier config.json avec les valeurs du serveur choisi
         */
        internal static void ChangeServer(string name)
        {
            Console.WriteLine("change to " + name);
            string json;
            json = File.ReadAllText(@"C:\Users\STBE\source\repos\module_manager\module_manager\bin\Debug\config.json");
            JObject conf = JObject.Parse(json);
            conf["source"] = name;

            string servs = File.ReadAllText(@"C:\Users\STBE\source\repos\module_manager\module_manager\bin\Debug\servers.json");
            JObject servlist = JObject.Parse(servs);
            foreach (JObject obj in servlist["servers"])
            {
                if ((string)obj["name"] == name)
                {
                    conf["type"] = obj["type"];
                    conf["url"] = obj["url"];
                }
            }
            File.WriteAllText(@"C:\Users\STBE\source\repos\module_manager\module_manager\bin\Debug\config.json", conf.ToString());
        }
    }
}
