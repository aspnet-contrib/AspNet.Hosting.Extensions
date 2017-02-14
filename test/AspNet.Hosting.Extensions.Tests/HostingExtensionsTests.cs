﻿/*
 * Licensed under the Apache License, Version 2.0 (http://www.apache.org/licenses/LICENSE-2.0)
 * See https://github.com/aspnet-contrib/AspNet.Hosting.Extensions for more information
 * concerning the license and the contributors participating to this project.
 */

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AspNet.Hosting.Extensions.Tests
{
    /// <summary>
    /// This class contains tests for the hosting extensions.
    /// </summary>
    public class HostingExtensionsTests
    {
        /// <summary>
        /// Tests that the service registered in the outer layer is not available in the isolated app.
        /// </summary>
        [Fact]
        public async Task OuterServiceNotAvailableInIsolation()
        {
            // Arrange
            var builder = new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddSingleton(new ValueService("Dummy"));
                })

                .Configure(app =>
                {
                    // Configure the isolated pipeline.
                    app.Isolate(map => map.Run(async context =>
                    {
                        var service = context.RequestServices.GetService<ValueService>();

                        await context.Response.WriteAsync(service?.Value ?? "<null>");
                    }));
                });

            var server = new TestServer(builder);

            var client = server.CreateClient();

            // Act
            var response = await client.GetStringAsync("/");

            // Assert
            Assert.Equal("<null>", response);
        }

        /// <summary>
        /// Tests that the service registered in the isolated app is not available outside the isolation.
        /// </summary>
        [Fact]
        public async Task InnerServiceNotAvailableOutsideIsolation()
        {
            // Arrange
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.Isolate(
                        // Configure the isolated pipeline.
                        map => { },

                        // Configure the isolated services.
                        services => services.AddSingleton(new ValueService("Dummy")));

                    app.Run(async context =>
                    {
                        var service = context.RequestServices.GetService<ValueService>();

                        await context.Response.WriteAsync(service?.Value ?? "<null>");
                    });
                });

            var server = new TestServer(builder);

            var client = server.CreateClient();

            // Act
            var response = await client.GetStringAsync("/");

            // Assert
            Assert.Equal("<null>", response);
        }

        /// <summary>
        /// Tests that the service registered in the isolated app does not
        /// conflict with services available outside the isolation.
        /// </summary>
        [Fact]
        public async Task InnerServiceNotConflictingWithServicesOutsideIsolation()
        {
            // Arrange
            var builder = new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddSingleton(new ValueService("Bob"));
                })

                .Configure(app =>
                {
                    app.Isolate(
                        // Configure the isolated pipeline.
                        map => { },

                        // Configure the isolated services.
                        services => services.AddSingleton(new ValueService("Dummy")));

                    app.Run(async context =>
                    {
                        var service = context.RequestServices.GetRequiredService<ValueService>();

                        await context.Response.WriteAsync(service.Value);
                    });
                });

            var server = new TestServer(builder);

            var client = server.CreateClient();

            // Act
            var response = await client.GetStringAsync("/");

            // Assert
            Assert.Equal("Bob", response);
        }

        /// <summary>
        /// Tests that a service registered in the isolated app can access
        /// services available outside the isolation via the ASP.NET context.
        /// </summary>
        [Fact]
        public async Task InnerServiceCanResolveServicesOutsideIsolationViaHttpContext()
        {
            // Arrange
            var builder = new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    // Allow the isolated environment to resolve
                    // the value service defined at the global level.
                    services.AddScoped(provider =>
                    {
                        var accessor = provider.GetRequiredService<IHttpContextAccessor>();
                        var container = (IServiceScope) accessor.HttpContext.Items[typeof(IServiceProvider)];

                        return container.ServiceProvider.GetRequiredService<ValueService>();
                    });
                })

                .Configure(app =>
                {
                    app.Isolate(
                        // Configure the isolated pipeline.
                        map => map.Run(async context =>
                        {
                            var service = context.RequestServices.GetRequiredService<ValueService>();

                            await context.Response.WriteAsync(service.Value);
                        }),

                        // Configure the isolated services.
                        services => services.AddSingleton(new ValueService("Dummy")));
                });

            var server = new TestServer(builder);

            var client = server.CreateClient();

            // Act
            var response = await client.GetStringAsync("/");

            // Assert
            Assert.Equal("Dummy", response);
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
