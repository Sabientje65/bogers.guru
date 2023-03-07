// Decompiled with JetBrains decompiler
// Type: Boger.Guru.Website.RouteConfig
// Assembly: Boger.Guru.Website, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: F957407C-8AE9-483E-9C1F-70C58A145318
// Assembly location: D:\Users\Danny\Documents\Projects\Bogers.Guru2\Boger.Guru.Website\bin\Boger.Guru.Website.dll

using System.Web.Mvc;
using System.Web.Routing;

namespace Boger.Guru.Website
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            routes.MapRoute("Default", "{controller}/{action}/{id}", (object) new
            {
                controller = "Home",
                action = "Index",
                id = UrlParameter.Optional
            });
        }
    }
}