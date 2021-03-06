﻿namespace Sitecore.FakeDb.Pipelines.InitFakeDb
{
  using Sitecore.FakeDb.Security.AccessControl;
  using Sitecore.Security.AccessControl;
  using System.Collections.Generic;

  public class InitAuthorizationProvider : InitDbProcessor
  {
    public override void Process(InitDbArgs args)
    {
      this.SetDataStorage(AuthorizationManager.Provider, args.DataStorage);
    }
  }
}