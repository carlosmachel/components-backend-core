﻿using System;
using Microsoft.Extensions.Logging;
using Optsol.Components.Infra.Data;
using Optsol.Components.Infra.Data.Provider;
using Optsol.Components.Test.Utils.Data.Entities;

namespace Optsol.Components.Test.Utils.Data.Repositories.Tenants
{
    public class TestTenantWriteRepository : Repository<TestTenantEntity, Guid>, ITestTenantWriteRepository
    {
        public TestTenantWriteRepository(CoreContext context, ILoggerFactory logger, ITenantProvider tenantProvider) 
            : base(context, logger, tenantProvider)
        {
        }
    }
}
