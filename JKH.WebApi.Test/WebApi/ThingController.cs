using JKH.WebApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JKH.WebApi.Test.Entity;
using System.Data.Entity.Infrastructure;

namespace JKH.WebApi.Test.WebApi
{
    public class ThingController : CrudController<ThingContext, Thing, ThingWebModel, int>
    {
        public ThingController(IDbContextFactory<ThingContext> dbContextFactory) : base(dbContextFactory) { }
    }

    public class ThingWebModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public byte[] RowVersion { get; set; }
    }
}
