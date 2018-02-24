using System;
using System.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TechnicalSupportServiceEmulationRequests
{
    class Program
    {
        static int work = 1;

        static void CheckStopAppAsync()
        {
            while (true)
            {
                var key = Console.ReadKey();
                if (key.Key == ConsoleKey.Escape)
                {
                    Console.WriteLine("\nStop application");
                    break;
                }
            }

            Interlocked.Decrement(ref work);
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Start application for emulation client request.");
            Console.WriteLine("Initialize parameters.");

            var timeSecBetweenQueries = int.Parse(ConfigurationManager.AppSettings["TimeSecBetweenQueries"]);
            var countRequest = int.Parse(ConfigurationManager.AppSettings["CountRequest"]);
            var urlApiOperationService = ConfigurationManager.AppSettings["UrlApiOperationService"];

            Console.WriteLine($"TimeSecBetweenQueries:{timeSecBetweenQueries}");
            Console.WriteLine($"CountRequest:{countRequest}");
            Console.WriteLine($"UrlApiOperationService:{urlApiOperationService}");

            Console.WriteLine("Press Esc key for stop application.");

            Func<int, int, bool> checkStopApp = null;
            if (countRequest == 0)
            {
                checkStopApp = (i, j) => (work == 0);
            }
            else
            {
                checkStopApp = (i, j) => (i >= j) || (work == 0);
            }
            
            Task.Run(() =>
            {
                CheckStopAppAsync();
            });
            
            int currentRequest = 1;
            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(urlApiOperationService);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                while (true)
                {
                    var httpContent = new StringContent("{}", Encoding.UTF8, "application/json");
                    string requestID = Guid.NewGuid().ToString();
                    Console.WriteLine($"Send request with ID:[{requestID}] of {currentRequest}");
                    var response = client.PutAsync(requestID, httpContent).Result;
                    if (response.IsSuccessStatusCode)
                    {
                        var result = response.Content.ReadAsStringAsync().Result;
                        Console.WriteLine($"Success send request with ID:[{requestID}] and result is {result}");
                    }
                    else
                    {
                        Console.WriteLine($"Error send request with ID:[{requestID}]");
                    }

                    Thread.Sleep(timeSecBetweenQueries * 1000);

                    currentRequest++;
                    if (checkStopApp(currentRequest, countRequest)) break;

                    httpContent = null;
                    response = null;
                    GC.Collect();
                }
            }

            Console.WriteLine("Press any key for close application.");
            Console.ReadLine();
        }
    }
}
