using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using AutoMapper.QueryableExtensions;
using AutoMapper;
using AutoMapper.Execution;
using AutoMapper.Configuration;
using AutoMapper.Mappers;
using System.Data.Entity.Infrastructure;

namespace JKH.WebApi
{
    /// <summary>
    /// Base <see cref="ApiController"/> with useful utility methods for interacting 
    /// with a <see cref="DbContext"/> of type <typeparamref name="TDbContext"/>.
    /// </summary>
    /// <typeparam name="TDbContext"><see cref="DbContext"/> used to interact with database</typeparam>
    public abstract class DbContextController<TDbContext> : ApiController where TDbContext : DbContext
    {
        private static string[] ROW_VERSION_PROPERTY_NAMES = { "RowVersion", "Timestamp" };
        private IDbContextFactory<TDbContext> _dbContextFactory;

        public DbContextController() { }
        public DbContextController(IDbContextFactory<TDbContext> dbContextFactory)
        {
            this._dbContextFactory = dbContextFactory;
        }

        /// <summary>
        /// Gets a service from the dependency injection container.
        /// </summary>
        /// <typeparam name="TService">The type of service to request</typeparam>
        /// <param name="throwIfNull">Throw an exception if no service is found</param>
        /// <returns>A service of type <typeparamref name="TService"/>.</returns>
        protected virtual TService GetService<TService>(bool throwIfNull = true)
        {
            var scope = Request.GetDependencyScope();
            if (scope == null)
                throw new InvalidOperationException("No dependency scope is available for the current request.");
            var service = (TService)scope.GetService(typeof(TService));
            if (service == null && throwIfNull)
                throw new InvalidOperationException("Service not found: " + typeof(TService).FullName);
            return service;
        }

        /// <summary>
        /// Create a <see cref="DbContext"/> to use for data operations.
        /// </summary>
        /// <returns>
        /// Default implementation requests an <see cref="IDbContextFactory{TContext}"/> from <see cref="GetService{TService}(bool)"/>
        /// </returns>
        protected virtual TDbContext CreateDbContext()
        {
            if (_dbContextFactory == null)
                _dbContextFactory = GetService<IDbContextFactory<TDbContext>>();
            return _dbContextFactory.Create();
        }

        /// <summary>
        /// Asynchronously retrieve data from the database.
        /// </summary>
        /// <typeparam name="TResult">The type of data to be returned</typeparam>
        /// <param name="work">The work to execute asynchronously</param>
        /// <returns></returns>
        protected virtual async Task<TResult> FromDbAsync<TResult>(Func<TDbContext, Task<TResult>> work)
        {
            using (var db = CreateDbContext())
            {
                return await work(db);
            }
        }

        /// <summary>
        /// Asynchronously perform work on the database.
        /// </summary>
        /// <param name="work">The work to execute asynchronously</param>
        /// <returns></returns>
        protected virtual async Task UsingDbAsync(Func<TDbContext, Task> work)
        {
            using (var db = CreateDbContext())
            {
                await work(db);
            }
        }

        /// <summary>
        /// Map properties from one object to another.
        /// </summary>
        /// <param name="source">The object to map properties from</param>
        /// <param name="destination">The object to map properties to</param>
        /// <returns>A collection of destination properties that were mapped.</returns>
        /// <remarks>Default implementation uses <see cref="AutoMapper.Mapper"/></remarks>
        protected virtual ICollection<MemberInfo> Map(object source, object destination)
        {
            var map = Mapper.Configuration.ResolveTypeMap(source.GetType(), destination.GetType());
            if (map == null)
                throw new InvalidOperationException("Could not resolve type map.");
            Mapper.Map(source, destination);
            return (from p in map.GetPropertyMaps()
                    select p.DestinationProperty).ToArray();
        }     

        /// <summary>
        /// Create a selector expression.
        /// </summary>
        /// <typeparam name="TDataModel">Data model type</typeparam>
        /// <typeparam name="TWebModel">Web model type</typeparam>
        /// <returns>Default implementation uses <see cref="AutoMapper.Mapper"/></returns>        
        protected virtual Expression<Func<TDataModel, TWebModel>> CreateSelector<TDataModel, TWebModel>()
        {
            return Mapper.Configuration.ExpressionBuilder.CreateMapExpression<TDataModel, TWebModel>();
        }

        protected virtual Task<TWebModel[]> GetArrayAsync<TDataModel, TWebModel>(Func<IQueryable<TDataModel>, IQueryable<TDataModel>> filter = null, CancellationToken cancellationToken = default(CancellationToken)) where TDataModel : class
        {
            return FromDbAsync(db =>
            {
                var set = db.Set<TDataModel>();
                var query = filter == null ? set : filter(db.Set<TDataModel>());
                var projection = query.Select(CreateSelector<TDataModel, TWebModel>());
                return projection.ToArrayAsync(cancellationToken);
            });
        }

        /// <summary>
        /// Asynchronously retrieve a single object from the database.
        /// </summary>
        /// <typeparam name="TDataModel">The data model type</typeparam>
        /// <typeparam name="TWebModel">The web model type</typeparam>
        /// <param name="predicate">The predicate used to filter results</param>
        /// <param name="cancellationToken"></param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a single object of type <typeparamref name="TWebModel"/>, or null.</returns>
        protected virtual Task<TWebModel> GetSingleOrDefaultAsync<TDataModel, TWebModel>(Expression<Func<TDataModel, bool>> predicate, CancellationToken cancellationToken = default(CancellationToken)) where TDataModel : class
        {
            return FromDbAsync(db =>
            {
                var set = db.Set<TDataModel>();
                var query = set.Where(predicate);
                var projection = query.Select(CreateSelector<TDataModel, TWebModel>());
                return projection.SingleOrDefaultAsync(cancellationToken);
            });
        }

        /// <summary>
        /// Creates a predicate expression on the key value of <typeparamref name="TDataModel"/>
        /// </summary>
        /// <typeparam name="TDataModel">The type of the data model</typeparam>
        /// <typeparam name="TKey">The type of the data model key</typeparam>
        /// <param name="keyValue">The value of the data model key</param>
        /// <returns></returns>
        protected virtual Expression<Func<TDataModel, bool>> CreateKeyPredicate<TDataModel, TKey>(TKey keyValue)
        {
            var keyProperty = GetKeyProperty<TDataModel>();
            var parameter = Expression.Parameter(typeof(TDataModel));
            var propertyAccessor = Expression.Property(parameter, keyProperty);
            var equals = Expression.Equal(propertyAccessor, Expression.Constant(keyValue));
            return Expression.Lambda<Func<TDataModel, bool>>(equals, parameter);
        }

        protected PropertyInfo GetRowVersionProperty<TDataModel>(bool throwIfNull = true)
        {
            var found = (from prop in typeof(TDataModel).GetProperties(BindingFlags.Instance | BindingFlags.Public)
                         let timestampAttr = prop.GetCustomAttribute<TimestampAttribute>(true)
                         where
                            timestampAttr != null ||
                            (ROW_VERSION_PROPERTY_NAMES.Contains(prop.Name) && prop.PropertyType == typeof(byte[]))
                         select prop).SingleOrDefault();
            if (found == null && throwIfNull)
                throw new InvalidOperationException("Row version property not found: " + typeof(TDataModel).FullName);
            return found;
        }

        /// <summary>
        /// Get the key property of <typeparamref name="TDataModel"/>.
        /// </summary>
        /// <typeparam name="TDataModel">The data model type</typeparam>
        /// <returns>A <see cref="PropertyInfo"/> object for the key property of <typeparamref name="TDataModel"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown when no key property is found, or multiple key properties are found.</exception>
        protected PropertyInfo GetKeyProperty<TDataModel>()
        {
            var type = typeof(TDataModel);
            var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            var keyProperties = (from prop in properties
                                 let key = prop.GetCustomAttribute<KeyAttribute>()
                                 where key != null
                                 select prop).ToArray();
            if (keyProperties.Length == 0)
            {
                var typeNameLower = type.Name.ToLower();
                keyProperties = (from prop in properties
                                 let nameLower = prop.Name.ToLower()
                                 where nameLower == "id" || nameLower == typeNameLower + "id"
                                 select prop).ToArray();
            }
            if (keyProperties.Length > 1)
                throw new InvalidOperationException("Multiple key properties found.");
            if (keyProperties.Length == 0)
                throw new InvalidOperationException("Key property not found.");
            return keyProperties[0];
        }

        protected virtual Task<TWebModel> GetAsync<TDataModel, TWebModel, TKey>(TKey key, CancellationToken cancellationToken = default(CancellationToken)) where TDataModel : class
        {
            return GetSingleOrDefaultAsync<TDataModel, TWebModel>(CreateKeyPredicate<TDataModel, TKey>(key), cancellationToken);
        }

        protected virtual Task<TWebModel[]> GetAllAsync<TDataModel, TWebModel>(CancellationToken cancellationToken = default(CancellationToken)) where TDataModel : class
        {
            return GetArrayAsync<TDataModel, TWebModel>(null, cancellationToken);
        }

        protected virtual Task<TWebModel> InsertAsync<TDataModel, TWebModel>(TWebModel webModel, CancellationToken cancellationToken = default(CancellationToken)) where TDataModel : class, new()
        {
            return FromDbAsync(async db =>
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

        protected virtual Task<TWebModel> UpdateAsync<TDataModel, TWebModel, TKey>(TKey key, TWebModel webModel, CancellationToken cancellationToken = default(CancellationToken)) where TDataModel : class, new()
        {
            return FromDbAsync(async db =>
            {
                //Get key and row version properties      
                var keyProperty = GetKeyProperty<TDataModel>();
                var rowVersionProperty = GetRowVersionProperty<TDataModel>(throwIfNull: false);

                //Map to temp model to get key and row version values. Validate key
                var tempModel = new TDataModel();
                Map(webModel, tempModel);
                var modelId = (TKey)keyProperty.GetValue(tempModel);
                if (!modelId.Equals(key))
                    throw new ArgumentException("Model ID does not match ID from REST URL.", "id");

                //Create data model for attachment to context and set id
                var dataModel = new TDataModel();
                keyProperty.SetValue(dataModel, key);

                //If row version property, the set that also
                if (rowVersionProperty != null)
                {
                    var rowVersion = rowVersionProperty.GetValue(tempModel);
                    rowVersionProperty.SetValue(dataModel, rowVersion);
                }

                //Attach data model BEFORE mapping properties from web model. This
                //ensures that only changed properties will be updated.
                var entry = db.Entry<TDataModel>(dataModel);
                entry.State = EntityState.Unchanged;

                //Map web model properties to data model
                var destinationProperties = Map(webModel, dataModel);

                //Ensure null or default values are marked as modified
                foreach (var destinationProperty in destinationProperties)
                {
                    if (destinationProperty != keyProperty && destinationProperty != rowVersionProperty)
                        entry.Property(destinationProperty.Name).IsModified = true;
                }

                //Save changes
                await db.SaveChangesAsync(cancellationToken);

                return CreateSelector<TDataModel, TWebModel>().Compile().Invoke(dataModel);
            });
        }    
        
        protected virtual Task DeleteAsync<TDataModel, TWebModel, TKey>(TKey key, TWebModel webModel, CancellationToken cancellationToken = default(CancellationToken)) where TDataModel : class, new()
        {
            return UsingDbAsync(async db =>
            {
                //Get key property and create data model to be deleted
                var keyProperty = GetKeyProperty<TDataModel>();
                var dataModel = new TDataModel();
                if (webModel == null)
                {
                    //Set key from URL
                    keyProperty.SetValue(dataModel, key);
                }
                else
                {
                    //Map web model properties onto data model
                    Map(webModel, dataModel);
                    //Check that key values in URL and body are the same
                    var keyValue = (TKey)keyProperty.GetValue(dataModel);
                    if (!keyValue.Equals(key))
                        throw CreateUrlKeyDiffersFromBodyKeyException();
                }
                //Attach data model as deleted
                db.Entry(dataModel).State = EntityState.Deleted;
                //Save changes
                await db.SaveChangesAsync(cancellationToken);
            });
        }

        protected virtual Exception CreateUrlKeyDiffersFromBodyKeyException()
        {
            return new InvalidOperationException("Key in URL differs from key in body.");
        }
    }
}