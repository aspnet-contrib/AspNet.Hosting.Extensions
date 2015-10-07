/*
 * Licensed under the Apache License, Version 2.0 (http://www.apache.org/licenses/LICENSE-2.0)
 * See https://github.com/aspnet-contrib/AspNet.Hosting.Extensions for more information
 * concerning the license and the contributors participating to this project.
 */

using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.TestHost;
using Owin;
using Xunit;

namespace AspNet.Hosting.Katana.Extensions.Tests {
    /// <summary>
    /// This class contains tests for the Katana extensions.
    /// </summary>
    public class KatanaExtensionsTests {
        /// <summary>
        /// Tests that the Katana pipeline registered using
        /// <see cref="KatanaExtensions.UseKatana"/>
        /// is correctly registered in the ASP.NET 5 pipeline.
        /// </summary>
        [Fact]
        public async Task KatanaPipelineCanBeAccessedFromAspNet() {
            // Arrange
            var server = TestServer.Create(app => app.UseKatana(builder => {
                builder.Run(async context => {
                    await context.Response.WriteAsync("Bob");
                });
            }));

            var client = server.CreateClient();

            // Act
            var response = await client.GetStringAsync("/");

            // Assert
            Assert.Equal("Bob", response);
        }

        /// <summary>
        /// Tests that the Katana pipeline registered using 
        /// <see cref="KatanaExtensions.UseKatana"/> doesn't
        /// prevent the rest of the ASP.NET 5 pipeline from being executed.
        /// </summary>
        [Fact]
        public async Task KatanaPipelineIsNotTerminating() {
            // Arrange
            var server = TestServer.Create(app => {
                app.UseKatana(builder => { });

                app.Run(async context => {
                    await context.Response.WriteAsync("Bob");
                });
            });

            var client = server.CreateClient();

            // Act
            var response = await client.GetStringAsync("/");

            // Assert
            Assert.Equal("Bob", response);
        }

        /// <summary>
        /// Tests that the Katana pipeline registered using 
        /// <see cref="KatanaExtensions.UseKatana"/> stops
        /// processing the request if next() is not invoked.
        /// </summary>
        [Fact]
        public async Task KatanaPipelineCanStopRequestProcessing() {
            // Arrange
            var server = TestServer.Create(app => {
                app.UseKatana(builder => builder.Run(async context => {
                    await context.Response.WriteAsync("Alice");
                }));

                app.Run(async context => {
                    await context.Response.WriteAsync("Bob");
                });
            });

            var client = server.CreateClient();

            // Act
            var response = await client.GetStringAsync("/");

            // Assert
            Assert.Equal("Alice", response);
        }
    }
}
