﻿/*
 * Licensed under the Apache License, Version 2.0 (http://www.apache.org/licenses/LICENSE-2.0)
 * See https://github.com/aspnet-contrib/AspNet.Hosting.Extensions for more information
 * concerning the license and the contributors participating to this project.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNet.Builder.Internal;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Hosting.Startup;
using Microsoft.AspNet.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.PlatformAbstractions;

namespace Microsoft.AspNet.Builder {
    /// <summary>
    /// Provides extension methods for <see cref="IApplicationBuilder"/>.
    /// </summary>
    public static class HostingExtensions {
        /// <summary>
        /// If the request path starts with the given <see cref="path"/>, execute the app configured via
        /// the configuration method of the <see cref="TStartup"/> class instead of continuing to the next component
        /// in the pipeline. The new app will get an own newly created <see cref="ServiceCollection"/> and will not share
        /// the <see cref="ServiceCollection"/> of the originating app.
        /// </summary>
        /// <typeparam name="TStartup">The startup class used to configure the new app and the service collection.</typeparam>
        /// <param name="app">The application builder to register the isolated map with.</param>
        /// <param name="path">The path to match. Must not end with a '/'.</param>
        /// <returns>The new pipeline with the isolated middleware configured.</returns>
        public static IApplicationBuilder IsolatedMap<TStartup>([NotNull] this IApplicationBuilder app, PathString path)
            where TStartup : class {
            var loader = app.ApplicationServices.GetRequiredService<IStartupLoader>();
            var methods = loader.LoadMethods(typeof(TStartup), new List<string>());

            return app.IsolatedMap(path, methods.ConfigureDelegate, methods.ConfigureServicesDelegate);
        }

        /// <summary>
        /// If the request path starts with the given <see cref="path"/>, execute the app configured via
        /// <see cref="configuration"/> parameter instead of continuing to the next component in the pipeline.
        /// The new app will get an own newly created <see cref="ServiceCollection"/> and will not share the
        /// <see cref="ServiceCollection"/> from the originating app.
        /// </summary>
        /// <param name="app">The application builder to register the isolated map with.</param>
        /// <param name="path">The path to match. Must not end with a '/'.</param>
        /// <param name="configuration">The branch to take for positive path matches.</param>
        /// <param name="serviceConfiguration">A method to configure the newly created service collection.</param>
        /// <returns>The new pipeline with the isolated middleware configured.</returns>
        public static IApplicationBuilder IsolatedMap(
            [NotNull] this IApplicationBuilder app, PathString path,
            [NotNull] Action<IApplicationBuilder> configuration,
            [NotNull] Action<IServiceCollection> serviceConfiguration) {
            return app.IsolatedMap(path, configuration, services => {
                serviceConfiguration(services);

                return services.BuildServiceProvider();
            });
        }

        /// <summary>
        /// If the request path starts with the given <see cref="path"/>, execute the app configured via
        /// <see cref="configuration"/> parameter instead of continuing to the next component in the pipeline.
        /// The new app will get an own newly created <see cref="ServiceCollection"/> and will not share the
        /// <see cref="ServiceCollection"/> from the originating app.
        /// </summary>
        /// <param name="app">The application builder to register the isolated map with.</param>
        /// <param name="path">The path to match. Must not end with a '/'.</param>
        /// <param name="configuration">The branch to take for positive path matches.</param>
        /// <param name="serviceConfiguration">A method to configure the newly created service collection.</param>
        /// <returns>The new pipeline with the isolated middleware configured.</returns>
        public static IApplicationBuilder IsolatedMap(
            [NotNull] this IApplicationBuilder app, PathString path,
            [NotNull] Action<IApplicationBuilder> configuration,
            [NotNull] Func<IServiceCollection, IServiceProvider> serviceConfiguration) {
            if (path.HasValue && path.Value.EndsWith("/", StringComparison.Ordinal)) {
                throw new ArgumentException("The path must not end with a '/'", nameof(path));
            }

            return app.Isolate(builder => builder.Map(path, configuration), serviceConfiguration);
        }
        /// <summary>
        /// Creates a new isolated application builder which gets its own <see cref="ServiceCollection"/>, which only
        /// has the default services registered. It will not share the <see cref="ServiceCollection"/> from the
        /// originating app. The isolated map will be configured using the configuration methods of the
        /// <see cref="TStartup"/> class.
        /// </summary>
        /// <typeparam name="TStartup">The startup class used to configure the new app and the service collection.</typeparam>
        /// <param name="app">The application builder to create the isolated app from.</param>
        /// <returns>The new pipeline with the isolated application integrated.</returns>
        public static IApplicationBuilder Isolate<TStartup>([NotNull] this IApplicationBuilder app) where TStartup : class {
            var loader = app.ApplicationServices.GetRequiredService<IStartupLoader>();
            var methods = loader.LoadMethods(typeof(TStartup), new List<string>());

            return app.Isolate(methods.ConfigureDelegate, methods.ConfigureServicesDelegate);
        }

        /// <summary>
        /// Creates a new isolated application builder which gets its own <see cref="ServiceCollection"/>, which only
        /// has the default services registered. It will not share the <see cref="ServiceCollection"/> from the
        /// originating app.
        /// </summary>
        /// <param name="app">The application builder to create the isolated app from.</param>
        /// <param name="configuration">The branch of the isolated app.</param>
        /// <returns>The new pipeline with the isolated application integrated.</returns>
        public static IApplicationBuilder Isolate(
            [NotNull] this IApplicationBuilder app,
            [NotNull] Action<IApplicationBuilder> configuration) {
            return app.Isolate(configuration, services => services.BuildServiceProvider());
        }


        /// <summary>
        /// Creates a new isolated application builder which gets its own <see cref="ServiceCollection"/>, which only
        /// has the default services registered. It will not share the <see cref="ServiceCollection"/> from the
        /// originating app.
        /// </summary>
        /// <param name="app">The application builder to create the isolated app from.</param>
        /// <param name="configuration">The branch of the isolated app.</param>
        /// <param name="serviceConfiguration">A method to configure the newly created service collection.</param>
        /// <returns>The new pipeline with the isolated application integrated.</returns>
        public static IApplicationBuilder Isolate(
            [NotNull] this IApplicationBuilder app,
            [NotNull] Action<IApplicationBuilder> configuration,
            [NotNull] Action<IServiceCollection> serviceConfiguration) {
            return app.Isolate(configuration, services => {
                serviceConfiguration(services);

                return services.BuildServiceProvider();
            });
        }

        /// <summary>
        /// Creates a new isolated application builder which gets its own <see cref="ServiceCollection"/>, which only
        /// has the default services registered. It will not share the <see cref="ServiceCollection"/> from the
        /// originating app.
        /// </summary>
        /// <param name="app">The application builder to create the isolated app from.</param>
        /// <param name="configuration">The branch of the isolated app.</param>
        /// <param name="serviceConfiguration">A method to configure the newly created service collection.</param>
        /// <returns>The new pipeline with the isolated application integrated.</returns>
        public static IApplicationBuilder Isolate(
            [NotNull] this IApplicationBuilder app,
            [NotNull] Action<IApplicationBuilder> configuration,
            [NotNull] Func<IServiceCollection, IServiceProvider> serviceConfiguration) {
            var services = CreateDefaultServiceCollection(app.ApplicationServices);
            var provider = serviceConfiguration(services);

            var builder = new ApplicationBuilder(null);
            builder.ApplicationServices = provider;

            builder.Use(async (context, next) => {
                var priorApplicationServices = context.ApplicationServices;
                var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

                // Store the original request services in the current ASP.NET context.
                context.Items[typeof(IServiceProvider)] = context.RequestServices;

                try {
                    using (var scope = scopeFactory.CreateScope()) {
                        context.ApplicationServices = provider;
                        context.RequestServices = scope.ServiceProvider;

                        await next();
                    }
                }

                finally {
                    context.RequestServices = null;
                    context.ApplicationServices = priorApplicationServices;
                }
            });

            configuration(builder);

            return app.Use(next => {
                // Run the rest of the pipeline in the original context,
                // with the services defined by the parent application builder.
                builder.Run(async context => {
                    var priorApplicationServices = context.ApplicationServices;
                    var scopeFactory = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>();

                    try {
                        using (var scope = scopeFactory.CreateScope()) {
                            context.ApplicationServices = app.ApplicationServices;
                            context.RequestServices = scope.ServiceProvider;

                            await next(context);
                        }
                    }

                    finally {
                        context.RequestServices = null;
                        context.ApplicationServices = priorApplicationServices;
                    }
                });

                var branch = builder.Build();

                return context => branch(context);
            });
        }

        /// <summary>
        /// This creates a new <see cref="ServiceCollection"/> with the same services registered as the
        /// <see cref="WebHostBuilder"/> does when creating a new <see cref="ServiceCollection"/>.
        /// </summary>
        /// <param name="provider">The service provider used to retrieve the default services.</param>
        /// <returns>A new <see cref="ServiceCollection"/> with the default services registered.</returns>
        private static ServiceCollection CreateDefaultServiceCollection([NotNull] IServiceProvider provider) {
            var services = new ServiceCollection();

            if (PlatformServices.Default?.Application != null) {
                services.TryAdd(ServiceDescriptor.Instance(PlatformServices.Default.Application));
            }

            if (PlatformServices.Default?.Runtime != null) {
                services.TryAdd(ServiceDescriptor.Instance(PlatformServices.Default.Runtime));
            }

            if (PlatformServices.Default?.AssemblyLoadContextAccessor != null) {
                services.TryAdd(ServiceDescriptor.Instance(PlatformServices.Default.AssemblyLoadContextAccessor));
            }

            if (PlatformServices.Default?.AssemblyLoaderContainer != null) {
                services.TryAdd(ServiceDescriptor.Instance(PlatformServices.Default.AssemblyLoaderContainer));
            }

            if (PlatformServices.Default?.LibraryManager != null) {
                services.TryAdd(ServiceDescriptor.Instance(PlatformServices.Default.LibraryManager));
            }

            services.AddLogging();

            // Copy the services added by the hosting layer.
            // See https://github.com/aspnet/Hosting/blob/dev/src/Microsoft.AspNet.Hosting/WebHostBuilder.cs.
            services.AddInstance(provider.GetRequiredService<IHostingEnvironment>());
            services.AddInstance(provider.GetRequiredService<ILoggerFactory>());
            services.AddInstance(provider.GetRequiredService<IApplicationEnvironment>());
            services.AddInstance(provider.GetRequiredService<IApplicationLifetime>());
            services.AddInstance(provider.GetRequiredService<IHttpContextFactory>());
            services.AddInstance(provider.GetRequiredService<IHttpContextAccessor>());

            services.AddInstance(provider.GetRequiredService<DiagnosticSource>());
            services.AddInstance(provider.GetRequiredService<DiagnosticListener>());

            return services;
        }
    }
}
