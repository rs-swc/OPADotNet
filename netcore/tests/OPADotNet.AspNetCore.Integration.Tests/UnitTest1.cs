using NUnit.Framework;
using System.Net.Http;
using System.Threading.Tasks;

namespace OPADotNet.AspNetCore.Integration.Tests
{
    public class NonJwtStyleAuthorizationHeader
    {
        private readonly IntegrationWebApplicationFactory _factory;
        public NonJwtStyleAuthorizationHeader()
        {
            _factory = new();
        }

        private HttpClient Client => _factory.CreateClient();

        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public async Task Run()
        {
            var response = await Client.GetAsync("secured-get");
            Assert.Pass();
        }
    }
}