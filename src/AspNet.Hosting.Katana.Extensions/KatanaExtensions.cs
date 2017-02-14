﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Owin.Builder;
using Microsoft.Owin.BuilderProperties;
using Owin;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Provides extension methods for <see cref="IApplicationBuilder"/>.
    /// </summary>
    public static class KatanaExtensions
    {
        /// <summary>
        /// Provides a Katana/ASP.NET Core bridge allowing to register middleware designed for OWIN/Katana.
        /// </summary>
        /// <param name="app">The ASP.NET Core application builder.</param>
        /// <param name="configuration">
        /// The delegate allowing to configure the OWIN/Katana
        /// pipeline before adding it in the ASP.NET Core application.
        /// </param>
        /// <returns>The ASP.NET Core application builder.</returns>
        public static IApplicationBuilder UseKatana(
            [NotNull] this IApplicationBuilder app,
            [NotNull] Action<IAppBuilder> configuration)
        {
            return app.UseOwin(setup => setup(next =>
            {
                var builder = new AppBuilder();
                var lifetime = app.ApplicationServices.GetService<IApplicationLifetime>();

                var properties = new AppProperties(builder.Properties);
                properties.AppName = app.ApplicationServices.GetService<IHostingEnvironment>()?.ContentRootPath;
                properties.OnAppDisposing = lifetime?.ApplicationStopping ?? CancellationToken.None;
                properties.DefaultApp = next;

                configuration(builder);

                return builder.Build<Func<IDictionary<string, object>, Task>>();
            }));
        }
    }
}
