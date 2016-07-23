using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JKH.WebApi.Test.Entity
{
    public class ThingContextFactory : IDbContextFactory<ThingContext>
    {
        public ThingContext Create()
        {
            return new ThingContext();
        }
    }
}
