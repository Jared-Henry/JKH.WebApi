using System;
using System.Data.Entity;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;

namespace JKH.WebApi
{
    /// <summary>
    /// Provides default RESTful methods for <typeparamref name="TWebModel"/>.
    /// </summary>
    /// <typeparam name="TWebModel">The web DTO returned to the client.</typeparam>
    /// <typeparam name="TDataModel">The data DTO returned from the database.</typeparam>
    /// <typeparam name="TDbContext">The <see cref="DbContext"/> to use for data access.</typeparam>
    public abstract class CrudController<TWebModel, TDataModel, TDbContext> : DbContextController<TDbContext> where TDbContext : DbContext where TDataModel : class, new()
    {
        [HttpGet, Route]
        public async virtual Task<TWebModel[]> GetAll(CancellationToken cancellationToken)
        {
            return await FromDbAsync(async db =>
            {
                return await db.Set<TDataModel>().Select(CreateSelector<TDataModel, TWebModel>()).ToArrayAsync(cancellationToken);
            });
        }

        [HttpGet]
        [Route("{id}")]
        public virtual Task<TWebModel> Get(int id, CancellationToken cancellationToken)
        {
            return SingleOrDefaultAsync<TDataModel, TWebModel>(CreateKeyPredicate<TDataModel, int>(id), cancellationToken);
        }

        [HttpPut]
        [Route("{id}")]
        public async virtual Task Update(int id, [FromBody]TWebModel webModel, CancellationToken cancellationToken)
        {
            await UsingDbAsync(async db =>
            {
                //Get data model from db
                var dataModel = await db.Set<TDataModel>().SingleOrDefaultAsync(CreateKeyPredicate<TDataModel, int>(id), cancellationToken);

                //Map web model properties onto data model
                Map(webModel, dataModel);

                //Save changes
                await db.SaveChangesAsync(cancellationToken);
            });
        }

        [HttpPost]
        [Route]
        public async virtual Task<TWebModel> Insert([FromBody]TWebModel webModel, CancellationToken cancellationToken)
        {
            return await FromDbAsync(async db =>
            {
                //Create new data model
                var dataModel = new TDataModel();

                //Map web model properties onto data model
                Map(webModel, dataModel);

                //Add data model to collection
                db.Set<TDataModel>().Add(dataModel);

                //Save changes
                await db.SaveChangesAsync(cancellationToken);

                //Return web model updated with ID
                return CreateSelector<TDataModel, TWebModel>().Compile().Invoke(dataModel);
            });
        }

        [HttpDelete]
        [Route("{id}")]
        public async virtual Task Delete(int id, CancellationToken cancellationToken)
        {
            await UsingDbAsync(async db =>
            {
                //Get data model from db
                var set = db.Set<TDataModel>();
                var dataModel = await set.SingleOrDefaultAsync(CreateKeyPredicate<TDataModel, int>(id), cancellationToken);
                if (dataModel == null)
                    throw new ArgumentException("Id does not exist in database.");

                //Delete data model from collection
                set.Remove(dataModel);

                //Save changes
                await db.SaveChangesAsync(cancellationToken);
            });
        }
    }
}
