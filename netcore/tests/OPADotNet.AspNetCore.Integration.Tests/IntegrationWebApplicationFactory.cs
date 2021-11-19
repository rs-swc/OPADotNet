using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using OPADotNet.AspNetCore.Integration.Client;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using WebMotions.Fake.Authentication.JwtBearer;

namespace OPADotNet.AspNetCore.Integration.Tests
{
    public class IntegrationWebApplicationFactory : WebApplicationFactory<EntryPoint>
    {
        public new HttpClient CreateClient()
        {
            return CreateClient("admin");
        }

        public HttpClient CreateClient(string userName)
        {
            var client = base.CreateClient();
            dynamic token = new ExpandoObject();
            token.sub = Guid.NewGuid();
            token.name = userName;

            client.SetFakeBearerToken((object)token);

            return client;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseTestServer();
            builder.ConfigureTestServices(services =>
            {
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = FakeJwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = FakeJwtBearerDefaults.AuthenticationScheme;
                }).AddFakeJwtBearer();
            });
        }
    }
}
