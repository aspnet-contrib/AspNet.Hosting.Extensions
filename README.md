AspNet.Hosting.Extensions
================================

**AspNet.Hosting.Extensions** is a collection of **hosting extensions** for ASP.NET Core 1.0
**that introduce isolated pipeline support and allow using OWIN/Katana middleware** in any ASP.NET Core application.

**The latest nightly builds can be found on [MyGet](https://www.myget.org/gallery/aspnet-contrib)**.

[![Build status](https://ci.appveyor.com/api/projects/status/xa09pdugnqqgn0vf/branch/dev?svg=true)](https://ci.appveyor.com/project/aspnet-contrib/aspnet-hosting-extensions/branch/dev)
[![Build status](https://travis-ci.org/aspnet-contrib/AspNet.Hosting.Extensions.svg?branch=dev)](https://travis-ci.org/aspnet-contrib/AspNet.Hosting.Extensions)

## Get started

```csharp
app.Isolate(map =>
{
    // Configure the isolated pipeline.
    map.UseMvc();
},

services =>
{
    // Register the services needed by the isolated pipeline.
    services.AddMvc();
});
```

```csharp
app.UseKatana(map =>
{
    // Configure the OWIN/Katana pipeline.
    var configuration = new HttpConfiguration();

    map.UseWebApi(configuration);
});
```

## Support

**Need help or wanna share your thoughts?** Don't hesitate to join us on Gitter or ask your question on StackOverflow:

- **Gitter: [https://gitter.im/aspnet-contrib/AspNet.Hosting.Extensions](https://gitter.im/aspnet-contrib/AspNet.Hosting.Extensions)**
- **StackOverflow: [https://stackoverflow.com/questions/tagged/aspnet-contrib](https://stackoverflow.com/questions/tagged/aspnet-contrib)**

## Contributors

**AspNet.Hosting.Extensions** is actively maintained by **[Kévin Chalet](https://github.com/PinpointTownes)**. Contributions are welcome and can be submitted using pull requests.

## License

This project is licensed under the **Apache License**. This means that you can use, modify and distribute it freely. See [http://www.apache.org/licenses/LICENSE-2.0.html](http://www.apache.org/licenses/LICENSE-2.0.html) for more details.