using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Assmnts.Infrastructure
{
    public static class Configuration
    {
        /// <summary>
        /// Read configuration values from text/config file, return dictionary data structure of those values 
        /// Config file should be in the format of:
        /// item1 = value1
        /// item2 = value2
        /// </summary>
        /// <param name="configFile"></param>
        /// <returns>dictionary</returns>
        public static Dictionary<String, String> LoadConfig(string configFile)
        {
            var kvPairs = new Dictionary<String, String>();

            if (System.IO.File.Exists(configFile))
            {
                var configData = System.IO.File.ReadAllLines(configFile);

                for (int i = 0; i < configData.Length; i++)
                {
                    var configItem = configData[i].Trim();
                    var index = configItem.IndexOf("=");

                    if (index >= 0)
                    {
                        var key = configItem.Substring(0, index).Trim();
                        var value = configItem.Substring(index + 1).Trim();

                        if (!kvPairs.ContainsKey(key))
                            kvPairs.Add(key, value);
                    }
                }
            }

            return kvPairs;
        }
    }
}