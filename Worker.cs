using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Polly;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace WorkerService_CheckURLS
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly List<string> urls = new List<string>{"https://www.youtube.com/feed/subscriptions", "https://mangasaki.com/", "https://www.mangasail.co/",
                                            "https://mangapark.net","https://translate.google.com/",
                                             "https://www.twitch.tv/agbin3r/videos?filter=archives&sort=time",
                                             "https://www.twitch.tv/ag_copdavid/videos?filter=archives&sort=time",
                                             "https://www.youtube.com/playlist?list=WL", "https://www.tiktok.com/"};

        private readonly List<string> urls2 = new List<string>{ "https://localhost:7089/api/v1/autores/GUID" };
        public Worker(ILogger<Worker> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            this.httpClientFactory = httpClientFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            //var timer = new PeriodicTimer(TimeSpan.FromMinutes(60));
            //while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await CheckUrls();

                    try
                    {
                        var client = httpClientFactory.CreateClient();
                        var response = await client.GetAsync("https://localhost:7089/api/v1/autores/GUID");
                        if (response.IsSuccessStatusCode)
                        {
                            await EndpointCall();
                        }
                    }
                    catch
                    {
                        Console.WriteLine("Service Down");
                    }
                    
                    



                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error WorkerService \n+++++++++++++++++++++++++++++{ex}\n+++++++++++++++++++++++++++++");
                }
                finally
                {
                    await Task.Delay(1000, stoppingToken);
                }
                
                
            }
        }

        private async Task CheckUrls()
        {
            var tasksList = new List<Task>();
            foreach (var url in urls)
            {
                tasksList.Add(CheckOneUrl(url));
            }

            await Task.WhenAll(tasksList);
        }

        private async Task CheckOneUrl(string url)
        {
            try
            {
                var client = httpClientFactory.CreateClient();
                var response = await client.GetAsync(url);
                var a = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"{url} : is online");

                }
            }
            catch (Exception ex /* just in case */)
            {
                _logger.LogInformation($"{url} : is offline");
            }
        }



        private async Task<string> GetToken()
        {
            var queryString = new Dictionary<string, string>()
                                      {
                                        { "Pagina", "1" },
                                        { "RecordsPorPagina", "6" }
                                      };

            var client3 = httpClientFactory.CreateClient();
            //client3.BaseAddress = new Uri("https://localhost:7089/api/V1/cuentas/login");

            //var requestUri = QueryHelpers.AddQueryString("autores", queryString);
            var request = new HttpRequestMessage(HttpMethod.Post, "https://localhost:7089/api/V1/cuentas/login");
            request.Headers.Add("Accept", "application/json");

            request.Content = JsonContent.Create(new
            {
                email= "user0@gmail.com",
                password= "Aa.123456"
            });

            var resp = await client3.SendAsync(request);
            var a = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

            var json = JObject.Parse(a);

            var token= json.GetValue("token");

            string result= token.ToString();

            return result.Replace("\"","");
        }

        

        private async Task EndpointCall()
        {

            var httpClient = new HttpClient();
            var maxRetryAttempts = 3;
            var pauseBetweenFailures = TimeSpan.FromSeconds(2);

            var retryPolicy = Policy
                .Handle<HttpRequestException>()
                .WaitAndRetryAsync(maxRetryAttempts, i => pauseBetweenFailures);

            await retryPolicy.ExecuteAsync(async () =>
            {
                Console.WriteLine("\n----------------------------------------------------------------------" +
                                           "\n----------------------------------------------------------------------" +
                                           "\n----------------------------------------------------------------------");
                

                try
                {

                    //Query string parameters
                    var queryString = new Dictionary<string, string>()
                                      {
                                        { "Pagina", "1" },
                                        { "RecordsPorPagina", "6" }
                                      };

                    var client2 = httpClientFactory.CreateClient();
                    client2.BaseAddress = new Uri("https://localhost:7089/apis/V1/");

                    var requestUri = QueryHelpers.AddQueryString("autores", queryString);
                    try
                    {
                        var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
                        request.Headers.Add("Accept", "application/json");
                        request.Headers.Add("incluirHATEOAS", "false");
                        request.Headers.Add("Authorization", "bearer " + await GetToken());
                        var resp = await client2.SendAsync(request);
                        if (!resp.StatusCode.Equals(200))
                        {
                            throw new Exception("Service Down");
                        }
                            
                        var a = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                        Console.WriteLine(a);

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Service Down : "+ex);
                    }
                    
                }
                catch(Exception ex)
                {
                    Console.WriteLine("Unhandled from EndPointCall()");
                }
                
                
                Console.WriteLine("\n----------------------------------------------------------------------" +
                    "\n----------------------------------------------------------------------" +
                    "\n----------------------------------------------------------------------");
                Thread.Sleep(2000);

            });
            
        }
        
    }
}