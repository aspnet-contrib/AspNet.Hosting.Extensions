using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.TestHost;
using Microsoft.Framework.DependencyInjection;
using Xunit;

namespace AspNet.Hosting.Extensions.Tests
{
    /// <summary>
    /// This class contains tests for the isolation feature.
    /// </summary>
    public class IsolationTests
    {
        /// <summary>
        ///   Test that the service registered in the outer layer is not available in the isolated app.
        /// </summary>
        [Fact]
        public async Task OuterServiceNotAvailableInIsolation()
        {
            // Arrange
            var testServer = TestServer.Create(
                app => app.Isolate(
                    subApp => subApp.Run(async context =>
                    {
                        var valueService = context.ApplicationServices.GetService<ValueService>();
                        await context.Response.WriteAsync(valueService?.Value ?? "<missing>");
                    })),
                services => services.AddInstance(new ValueService("Dummy")));

            // Act
            var testClient = testServer.CreateClient();
            var response = await testClient.GetStringAsync("/");

            // Assert
            Assert.Equal("<missing>", response);
        }

        /// <summary>
        ///   Test that the service registered in the isolated app is not available outside the isolation.
        /// </summary>
        [Fact]
        public async Task InnerServiceNotAvailableOutsideIsolation()
        {
            // Arrange
            var testServer = TestServer.Create(
                app =>
                {
                    app.Isolate(
                        subApp => { },
                        subServices => subServices.AddInstance(new ValueService("Dummy")));

                    app.Run(async context => {
                        var valueService = context.ApplicationServices.GetService<ValueService>();
                        await context.Response.WriteAsync(valueService?.Value ?? "<missing>");
                    });
                });
            
            // Act
            var testClient = testServer.CreateClient();
            var response = await testClient.GetStringAsync("/");

            // Assert
            Assert.Equal("<missing>", response);
        }

        /// <summary>
        /// Dummy service that is used in tests.
        /// </summary>
        private class ValueService
        {
            public ValueService(string value)
            {
                Value = value;
            }

            public string Value { get; }
        }
    }
}
