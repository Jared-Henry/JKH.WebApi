using System;
using System.Data.Entity;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using AutoMapper;
using System.Data.Entity.Infrastructure;

namespace JKH.WebApi
{
    /// <summary>
    /// Provides default RESTful methods for <typeparamref name="TWebModel"/>.
    /// </summary>
    /// <typeparam name="TWebModel">The web DTO returned to the client.</typeparam>
    /// <typeparam name="TDataModel">The data DTO returned from the database.</typeparam>
    /// <typeparam name="TDbContext">The <see cref="DbContext"/> to use for data access.</typeparam>
    public abstract class RestController<TDbContext, TDataModel, TWebModel, TKey> : DbContextController<TDbContext> where TDbContext : DbContext where TDataModel : class, new()
    {
        public RestController() { }
        public RestController(IDbContextFactory<TDbContext> dbContextFactory) : base(dbContextFactory) { }

        [HttpGet, Route]
        public virtual Task<TWebModel[]> GetAll(CancellationToken cancellationToken = default(CancellationToken))
        {
            return GetAllAsync<TDataModel, TWebModel>(cancellationToken);
        }

        [HttpGet]
        [Route("{key}")]
        public virtual Task<TWebModel> Get(TKey key, CancellationToken cancellationToken = default(CancellationToken))
        {
            return GetAsync<TDataModel, TWebModel, TKey>(key, cancellationToken);
        }

        [HttpPut]
        [Route("{key}")]
        public virtual Task<TWebModel> Update(TKey key, [FromBody]TWebModel webModel, CancellationToken cancellationToken = default(CancellationToken))
        {
            return UpdateAsync<TDataModel, TWebModel, TKey>(key, webModel, cancellationToken);
        }

        [HttpPost]
        [Route]
        public virtual Task<TWebModel> Insert([FromBody]TWebModel webModel, CancellationToken cancellationToken = default(CancellationToken))
        {
            return InsertAsync<TDataModel, TWebModel>(webModel, cancellationToken);
        }

        [HttpDelete]
        [Route("{key}")]
        public virtual Task Delete(TKey key, [FromBody]TWebModel webModel, CancellationToken cancellationToken = default(CancellationToken))
        {
            return DeleteAsync<TDataModel, TWebModel, TKey>(key, webModel, cancellationToken);
        }
    }
}
