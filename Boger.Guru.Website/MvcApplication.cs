﻿// Decompiled with JetBrains decompiler
// Type: Boger.Guru.Website.MvcApplication
// Assembly: Boger.Guru.Website, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: F957407C-8AE9-483E-9C1F-70C58A145318
// Assembly location: D:\Users\Danny\Documents\Projects\Bogers.Guru2\Boger.Guru.Website\bin\Boger.Guru.Website.dll

using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace Boger.Guru.Website
{
    public class MvcApplication : HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            RouteConfig.RegisterRoutes(RouteTable.Routes);
        }
    }
}