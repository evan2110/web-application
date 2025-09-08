using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using server.Services;

namespace test.ServiceTests
{
    public class SupabaseServiceTests
    {
        [Fact]
        public void Constructs_Client_With_Config()
        {
            var settings = new Dictionary<string, string?>
            {
                { "Supabase:Url", "http://localhost" },
                { "Supabase:ServiceKey", "key" }
            };
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings!).Build();
            var logger = new Mock<ILogger<SupabaseService>>();
            var svc = new SupabaseService(configuration, logger.Object);
            svc.GetClient().Should().NotBeNull();
        }
    }
}


