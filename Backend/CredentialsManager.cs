using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using System;
using System.IO;
using Tanakh.Model;

namespace Tanakh
{
    public class CredentialsManager
    {
        private readonly string credentialsDataPath;

        public CredentialsManager(IHostEnvironment environment)
        {
            credentialsDataPath = Path.Combine(environment.ContentRootPath, "CredentialsManager.json");
        }

        public Credentials LoadCredentials()
        {
            Credentials credentials = new Credentials();

            try
            {
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