using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JKH.WebApi.Test.Entity
{
    public class ThingContext : DbContext
    {
        public DbSet<Thing> Things { get; set; }
    }
}
