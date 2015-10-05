using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Builder.Internal;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Hosting.Builder;
using Microsoft.AspNet.Hosting.Startup;
using Microsoft.AspNet.Http;
using Microsoft.Dnx.Runtime;
using Microsoft.Dnx.Runtime.Infrastructure;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Internal;
using Microsoft.Framework.Logging;

namespace AspNet.Hosting.Extensions {
    /// <summary>
    /// Provides extension methods for the <see cref="IApplicationBuilder"/>.
    /// </summary>
    public static class ApplicationBuilderExtensions {
        /// <summary>
        ///   If the request path starts with the given <see cref="pathMatch"/>, execute the app configured via
        ///   the configuration method of the <see cref="TStartup"/> class instead of continuing to the next component
        ///   in the pipeline.
        ///   The new app will get an own newly created <see cref="ServiceCollection"/> and will not share the
        ///   <see cref="ServiceCollection"/> of the originating app.
        /// </summary>
        /// <typeparam name="TStartup">
        ///   The startup class used to configure the new app and the service collection.
        /// </typeparam>
        /// <param name="app">The application builder to register the isolated map with.</param>
        /// <param name="pathMatch">The path to match. Must not end with a '/'.</param>
        /// <returns>The new pipeline with the isolated middleware configured.</returns>
        public static IApplicationBuilder IsolatedMap<TStartup>(
            [NotNull] this IApplicationBuilder app,
            [NotNull] PathString pathMatch)
            where TStartup : class {
            var startupLoader = app.ApplicationServices.GetRequiredService<IStartupLoader>();
            var startupMethods = startupLoader.LoadMethods(typeof(TStartup), new List<string>());

            return app.IsolatedMap(
                pathMatch,
                startupMethods.ConfigureDelegate,
                startupMethods.ConfigureServicesDelegate);
        }

        /// <summary>
        ///   If the request path starts with the given <see cref="pathMatch"/>, execute the app configured via
        ///   <see cref="configuration"/> parameter instead of continuing to the next component in the pipeline.
        ///   The new app will get an own newly created <see cref="ServiceCollection"/> and will not share the
        ///   <see cref="ServiceCollection"/> from the originating app.
        /// </summary>
        /// <param name="app">The application builder to register the isolated map with.</param>
        /// <param name="pathMatch">The path to match. Must not end with a '/'.</param>
        /// <param name="configuration">The branch to take for positive path matches.</param>
        /// <param name="serviceConfiguration">A method to configure the newly created service collection.</param>
        /// <returns>The new pipeline with the isolated middleware configured.</returns>
        public static IApplicationBuilder IsolatedMap(
            [NotNull] this IApplicationBuilder app,
            [NotNull] PathString pathMatch,
            [NotNull] Action<IApplicationBuilder> configuration,
            [NotNull] Action<IServiceCollection> serviceConfiguration)
        {
            return app.IsolatedMap(
                pathMatch,
                configuration,
                services => {
                    serviceConfiguration(services);
                    return services.BuildServiceProvider();
                });
        }

        /// <summary>
        ///   If the request path starts with the given <see cref="pathMatch"/>, execute the app configured via
        ///   <see cref="configuration"/> parameter instead of continuing to the next component in the pipeline.
        ///   The new app will get an own newly created <see cref="ServiceCollection"/> and will not share the
        ///   <see cref="ServiceCollection"/> from the originating app.
        /// </summary>
        /// <param name="app">The application builder to register the isolated map with.</param>
        /// <param name="pathMatch">The path to match. Must not end with a '/'.</param>
        /// <param name="configuration">The branch to take for positive path matches.</param>
        /// <param name="serviceConfiguration">A method to configure the newly created service collection.</param>
        /// <returns>The new pipeline with the isolated middleware configured.</returns>
        public static IApplicationBuilder IsolatedMap(
            [NotNull] this IApplicationBuilder app,
            [NotNull] PathString pathMatch,
            [NotNull] Action<IApplicationBuilder> configuration,
            [NotNull] Func<IServiceCollection, IServiceProvider> serviceConfiguration) {
            if (pathMatch.HasValue && pathMatch.Value.EndsWith("/", StringComparison.Ordinal)) {
                throw new ArgumentException("The path must not end with a '/'", nameof(pathMatch));
            }

            return app.Isolate(
                subApp => subApp.Map(pathMatch, configuration),
                serviceConfiguration);
        }
        /// <summary>
        ///   Creates a new isolated application builder which gets its own <see cref="ServiceCollection"/>, which only
        ///   has the default services registered. It will not share the <see cref="ServiceCollection"/> from the
        ///   originating app. The isolated map will be configured using the configuration methods of the
        ///   <see cref="TStartup"/> class.
        /// </summary>
        /// <typeparam name="TStartup">
        ///   The startup class used to configure the new app and the service collection.
        /// </typeparam>
        /// <param name="app">The application builder to create the isolated app from.</param>
        /// <returns>The new pipeline with the isolated application integrated.</returns>
        public static IApplicationBuilder Isolate<TStartup>(
            [NotNull] this IApplicationBuilder app)
            where TStartup : class {
            var startupLoader = app.ApplicationServices.GetRequiredService<IStartupLoader>();
            var startupMethods = startupLoader.LoadMethods(typeof(TStartup), new List<string>());

            return app.Isolate(
                startupMethods.ConfigureDelegate,
                startupMethods.ConfigureServicesDelegate);
            }

        /// <summary>
        ///   Creates a new isolated application builder which gets its own <see cref="ServiceCollection"/>, which only
        ///   has the default services registered. It will not share the <see cref="ServiceCollection"/> from the
        ///   originating app.
        /// </summary>
        /// <param name="app">The application builder to create the isolated app from.</param>
        /// <param name="configuration">The branch of the isolated app.</param>
        /// <returns>The new pipeline with the isolated application integrated.</returns>
        public static IApplicationBuilder Isolate(
            [NotNull] this IApplicationBuilder app,
            [NotNull] Action<IApplicationBuilder> configuration) {
            return app.Isolate(
                configuration,
                services => services.BuildServiceProvider());
        }


        /// <summary>
        ///   Creates a new isolated application builder which gets its own <see cref="ServiceCollection"/>, which only
        ///   has the default services registered. It will not share the <see cref="ServiceCollection"/> from the
        ///   originating app.
        /// </summary>
        /// <param name="app">The application builder to create the isolated app from.</param>
        /// <param name="configuration">The branch of the isolated app.</param>
        /// <param name="serviceConfiguration">A method to configure the newly created service collection.</param>
        /// <returns>The new pipeline with the isolated application integrated.</returns>
        public static IApplicationBuilder Isolate(
            [NotNull] this IApplicationBuilder app,
            [NotNull] Action<IApplicationBuilder> configuration,
            [NotNull] Action<IServiceCollection> serviceConfiguration) {
            return app.Isolate(
                configuration,
                services => {
                    serviceConfiguration(services);
                    return services.BuildServiceProvider();
                });
        }

        /// <summary>
        ///   Creates a new isolated application builder which gets its own <see cref="ServiceCollection"/>, which only
        ///   has the default services registered. It will not share the <see cref="ServiceCollection"/> from the
        ///   originating app.
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
            var serviceProvider = serviceConfiguration(services);
            var builder = new ApplicationBuilder(null);
            builder.ApplicationServices = serviceProvider;

            builder.Use(async (context, next) => {
                var priorApplicationServices = context.ApplicationServices;
                var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

                // Store the original request services in the current ASP.NET context.
                context.Items[typeof(IServiceProvider)] = context.RequestServices;

                try {
                    using (var scope = scopeFactory.CreateScope()) {
                        context.ApplicationServices = serviceProvider;
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
        ///   This creates a new <see cref="ServiceCollection"/> with the same services registered as the
        ///   <see cref="WebHostBuilder"/> does when creating a new <see cref="ServiceCollection"/>.
        /// </summary>
        /// <remarks>
        ///   List of services is taken from source code:
        ///   https://github.com/aspnet/Hosting/blob/dev/src/Microsoft.AspNet.Hosting/WebHostBuilder.cs
        /// </remarks>
        /// <param name="serviceProvider">The service provider used to retrieve the default services.</param>
        /// <returns>A new <see cref="ServiceCollection"/> with the default services registered.</returns>
        private static ServiceCollection CreateDefaultServiceCollection(
            [NotNull] IServiceProvider serviceProvider) {
            var services = new ServiceCollection();

            // Retrieve the runtime services from the host provider.
            var manifest = CallContextServiceLocator.Locator.ServiceProvider.GetService<IRuntimeServices>();
            if (manifest != null) {
                foreach (var service in manifest.Services) {
                    services.AddTransient(service, sp => serviceProvider.GetService(service));
                }
            }

            services.AddLogging();

            // Copy the services added by the hosting layer.
            services.AddInstance(serviceProvider.GetRequiredService<IHostingEnvironment>());
            services.AddInstance(serviceProvider.GetRequiredService<ILoggerFactory>());
            services.AddInstance(serviceProvider.GetRequiredService<IApplicationEnvironment>());
            services.AddInstance(serviceProvider.GetRequiredService<IApplicationLifetime>());
            services.AddInstance(serviceProvider.GetRequiredService<IHttpContextFactory>());
            services.AddInstance(serviceProvider.GetRequiredService<IHttpContextAccessor>());
            services.AddInstance(serviceProvider.GetRequiredService<TelemetrySource>());
            services.AddInstance(serviceProvider.GetRequiredService<TelemetryListener>());

            return services;
        }
    }
}
