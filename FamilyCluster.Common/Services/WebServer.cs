namespace FamilyCluster.Brother
{
    using Microsoft.Owin.Hosting;
    using Owin;
    using System;
    using System.Collections.Generic;
    using System.Web.Http;

    public class WebServer
    {
        public static void StartWebServer()
        {
            //http://localhost:8080/api/demo
            using (WebApp.Start<Startup>("http://localhost:8080"))
            {
                Console.WriteLine("Web Server is running.");
                Console.WriteLine("Press any key to quit.");
                Console.ReadLine();
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
    }
}