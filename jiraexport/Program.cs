using Newtonsoft.Json;
using System.Net;
using System.Text;

namespace jiraexport
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            if (!File.Exists("config.json"))
            {
                Console.WriteLine("Could not find config file (config.json). Exiting...");
                return;
            }
            FileStream fs = new FileStream("config.json", FileMode.Open, FileAccess.Read);
            StreamReader sr = new StreamReader(fs, Encoding.UTF8);

            string configContent = sr.ReadToEnd();

            JiraRequestConfig? config = JsonConvert.DeserializeObject<JiraRequestConfig>(configContent);

            if (config == null)
            {
                Console.WriteLine("Could not read config file. Exiting...");
                return;
            }

            if (config.username == null)
            {
                Console.WriteLine("Could not find username in config file. Exiting...");
                return;
            }

            if (config.password == null)
            {
                Console.WriteLine("Could not find password in config file. Exiting...");
                return;
            }

            if (config.url == null)
            {
                Console.WriteLine("Could not find url in config file. Exiting...");
                return;
            }

            int start = 0;
            bool lastPage = false;

            string emailString = String.Empty;

            while (!lastPage)
            {
                JiraResponse? response = sendRequest(config.url, start, config.username, config.password);

                if (response == null)
                {
                    break;
                }

                foreach (JiraValue val in response.values)
                {
                    if (val.emailAddress == null || val.emailAddress == String.Empty)
                    {
                        continue;
                    }
                    bool filterDomain = false;
                    foreach (string domain in config.excludedDomains)
                    {
                        if (val.emailAddress.Length > domain.Length && val.emailAddress.Substring(val.emailAddress.Length - domain.Length) == domain)
                        {
                            filterDomain = true;
                        }
                    }
                    if (!filterDomain)
                    {
                        emailString += val.emailAddress + "; ";
                    }
                }
                start += response.size;
                lastPage = response.isLastPage;
            }

            Console.WriteLine(emailString);
        }

        private static JiraResponse? sendRequest(string url, int start, string username, string password)
        {
            CookieContainer myContainer = new CookieContainer();
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url + "?start=" + start.ToString());
            request.CookieContainer = myContainer;
            request.PreAuthenticate = true;

            string encodedCreds = System.Convert.ToBase64String(Encoding.GetEncoding("ISO-8859-1")
                                           .GetBytes(username + ":" + password));
            request.Headers.Add("Authorization", "Basic " + encodedCreds);
            request.Headers.Add("X-ExperimentalApi", "opt-in");
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            string responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
            JiraResponse? jiraResponse = JsonConvert.DeserializeObject<JiraResponse>(responseString);

            return jiraResponse;
        }
    }

    public class JiraResponse
    {
        public int size;
        public int start;
        public int limit;
        public bool isLastPage;

        public List<JiraValue> values;

        public JiraResponse()
        {
            values = new List<JiraValue>();
        }
    }

    public class JiraValue
    {
        public string? accountId;
        public string? emailAddress;
        public string? displayName;
        public bool active;
        public string? timeZone;
    }

    public class JiraRequestConfig
    {
        public string? username;
        public string? password;
        public string? url;
        public List<string> excludedDomains;

        public JiraRequestConfig()
        {
            excludedDomains = new List<string>();
        }
    }
}