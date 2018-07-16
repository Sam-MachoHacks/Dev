using System;
using System.Configuration;
using Akka.Actor;
using Akka.Routing;
using FamilyCluster.Common;

namespace FamilyCluster.Brother
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Web.Http;
    using Akka.Cluster;
    using Akka.Configuration;
    using Microsoft.Owin.Hosting;
    using Newtonsoft.Json;
    using Owin;
    using Twilio;
    using Twilio.Rest.Api.V2010.Account;

    internal class Program
    {
       static string id = "BrotherEchoActor";
      
        private static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("Starting BrotherSystem ...");
                using (var system = ActorSystem.Create("FamilyCluster", ConfigurationFactory.ParseString(AppConfiguration.GetAkkaConfiguration(id))))
                {
                  var brotherEchoActor = system.ActorOf(Props.Create(() => new BlockProcessorActor(new BlockChainHandler())).WithRouter(FromConfig.Instance), id);
                    
                  while (true)
                  {
                   
                        var message = Console.ReadLine();
                      
                        var Blocks = BlockProcessorActor.Blocks;

                        Hello mainMessage;
                        string key;
                        List<DataBlock> blockChain;
                        if (Blocks.Count == 0)
                        {
                            var newChain = new KeyValuePair<string, List<DataBlock>>(Guid.NewGuid().ToString(), new List<DataBlock>());
                            Blocks.GetOrAdd(newChain.Key, newChain.Value);

                             key =Guid.NewGuid().ToString();
                             blockChain = new List<DataBlock>();
                        }
                        else
                        {
                             var blockChainData = Blocks.First();
                             key= blockChainData.Key;
                             blockChain= blockChainData.Value;
                        }

                        var newBlock = BlockProcessorActor.CreateNewBlock(blockChain, new BlockTransaction(100, BlockProcessorActor.DefaultCreditScore));

                        blockChain.Add(newBlock);

                        mainMessage = new Hello(blockChain, key);
                        var result = brotherEchoActor.Ask<string>(mainMessage).Result;
                        Console.WriteLine($"RESULT FIRST : {result}");
                        Console.WriteLine($"CURRENT BLOCK : {Blocks.Count}");
                        
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Console.ReadKey();
                
                throw;
            }
        }

    
    }

  

    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // Configure Web API for self-host. 
            var config = new HttpConfiguration();
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            app.UseWebApi(config);
        }
    }
    public class DemoController : ApiController
    {
        // GET api/demo 
        public IEnumerable<string> Get()
        {
            return new string[] { "Hello", "World" };
        }

        // GET api/demo/5 
        public string Get(int id)
        {
            return "Hello, World!";
        }

        // POST api/demo 
        public void Post([FromBody]string value)
        {
        }

        // PUT api/demo/5 
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/demo/5 
        public void Delete(int id)
        {
        }
    }
}//http://localhost:8080/api/demo
                    //using (WebApp.Start<Startup>("http://localhost:8080"))
                    //{
                    //    Console.WriteLine("Web Server is running.");
                    //    Console.WriteLine("Press any key to quit.");
                    //    Console.ReadLine();
                    //}