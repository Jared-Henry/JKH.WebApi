using AutoMapper;
using JKH.WebApi.Test.Entity;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JKH.WebApi.Test
{
    [TestClass]
    public class Global
    {
        [AssemblyInitialize]
        public static void Initialize(TestContext context)
        {
            Mapper.Initialize(config => config.CreateMissingTypeMaps = true);
            Database.SetInitializer<ThingContext>(new DropCreateDatabaseAlways<ThingContext>());
        }
    }
}
