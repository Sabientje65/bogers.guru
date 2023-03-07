// Decompiled with JetBrains decompiler
// Type: Boger.Guru.Website.Controllers.RiderController
// Assembly: Boger.Guru.Website, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: F957407C-8AE9-483E-9C1F-70C58A145318
// Assembly location: D:\Users\Danny\Documents\Projects\Bogers.Guru2\Boger.Guru.Website\bin\Boger.Guru.Website.dll

using System.Threading.Tasks;
using System.Web.Mvc;

namespace Boger.Guru.Website.Controllers
{
    public class RiderController : Controller
    {
        public async Task<ActionResult> Index()
        {
            await Task.Yield();
            return (ActionResult) this.View("~/Views/Home/Index.cshtml", (object) "Borista");
        }
    }
}