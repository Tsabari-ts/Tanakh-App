using Newtonsoft.Json;
using System;
using System.IO;
using Tanakh.Model;

namespace Tanakh
{
    public class CredentialsManager
    {
        public Credentials LoadCredentials()
        {
            Credentials credentials = new Credentials();

            try
            {
                string credentialsDataPath = "C:\\Users\\Tomer\\Desktop\\tomer\\myProjects\\Tanach in c#\\Tanakh\\Properties\\CredentialsManager.json";

                using (StreamReader reader = new StreamReader(credentialsDataPath))
                {
                    string jsonData = reader.ReadToEnd();
                    credentials = JsonConvert.DeserializeObject<Credentials>(jsonData);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error loading credentials: " + ex.Message);
            }

            return credentials;
        }
    }
}