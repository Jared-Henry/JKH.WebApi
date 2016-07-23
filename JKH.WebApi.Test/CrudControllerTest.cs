using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data.Entity;
using JKH.WebApi.Test.Entity;
using JKH.WebApi.Test.WebApi;
using System.Threading.Tasks;
using System.Threading;
using System.Data.Entity.Core;
using System.Data.Entity.Infrastructure;

namespace JKH.WebApi.Test
{
    [TestClass]
    public class CrudControllerTest
    {
        ThingController controller;
        int textIndex = 1;

        public CrudControllerTest()
        {            
            controller = new ThingController(new ThingContextFactory());
        }

        private string GetText()
        {
            return "TEXT" + textIndex++;
        }

        [TestMethod]
        public async Task Insert()
        {
            var name = GetText();
            var thing = new ThingWebModel()
            {
                Name = name
            };
            var insertedThing = await controller.Insert(thing, default(CancellationToken));
            Assert.AreNotEqual(default(int), insertedThing.Id);
            Assert.IsNotNull(insertedThing.RowVersion);
            Assert.AreNotEqual(0, insertedThing.RowVersion.Length);
            Assert.AreEqual(insertedThing.Name, name);
            using (var db = new ThingContext())
            {
                var dataThing = await db.Things.SingleOrDefaultAsync(t => t.Id == insertedThing.Id);
                Assert.IsNotNull(dataThing);
                Assert.AreEqual(dataThing.Name, name);
            }
        }

        [TestMethod]
        public async Task Update()
        {
            var name = GetText();
            var thing = await controller.Insert(new ThingWebModel()
            {
                Name = name
            });
            var updatedName = GetText();
            thing.Name = updatedName;
            var updatedThing = await controller.Update(thing.Id, thing);
            Assert.AreEqual(updatedThing.Name, updatedName);
            using (var db = new ThingContext())
            {
                var dataThing = await db.Things.SingleAsync(t => t.Id == updatedThing.Id);
                Assert.AreEqual(dataThing.Name, updatedName);       
            }
        }

        [TestMethod]
        public async Task Delete()
        {
            var thing = await controller.Insert(new ThingWebModel() { Name = GetText() });
            await controller.Delete(thing.Id, thing);
            using (var db = new ThingContext())
            {
                var dataThing = await db.Things.SingleOrDefaultAsync(t => t.Id == thing.Id);
                Assert.IsNull(dataThing);
            }
        }

        [TestMethod]
        public async Task Get()
        {
            var thing = await controller.Insert(new ThingWebModel() { Name = GetText() });
            var getThing = await controller.Get(thing.Id);
            Assert.IsNotNull(getThing);
            Assert.AreEqual(thing.Id, getThing.Id);
            Assert.AreEqual(thing.Name, getThing.Name);
        }

        [TestMethod, ExpectedException(typeof(DbUpdateConcurrencyException))]
        public async Task Concurrency()
        {
            var thing = await controller.Insert(new ThingWebModel() { Name = GetText() });
            thing.Name = GetText();
            await controller.Update(thing.Id, thing);
            thing.Name = GetText();
            await controller.Update(thing.Id, thing);
        }
    }
}
