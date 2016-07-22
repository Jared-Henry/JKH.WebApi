using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Data.Entity.Infrastructure;
using System.Net.Http;
using System.Reflection;
using System.Linq.Expressions;
using System.ComponentModel.DataAnnotations;
using System.Threading;

namespace JKH.WebApi
{
    /// <summary>
    /// Base <see cref="ApiController"/> with useful utility methods for interacting 
    /// with a <see cref="DbContext"/> of type <typeparamref name="TDbContext"/>.
    /// </summary>
    /// <typeparam name="TDbContext"><see cref="DbContext"/> used to interact with database</typeparam>
    public abstract class DbContextController<TDbContext> : ApiController where TDbContext : DbContext
    {
        protected virtual TService GetService<TService>(bool throwIfNull = true)
        {
            var service = (TService)Request.GetDependencyScope().GetService(typeof(TService));
            if (service == null && throwIfNull)
                throw new InvalidOperationException("Service not found: " + typeof(TService).FullName);
            return service;
        }

        /// <summary>
        /// Create a <see cref="DbContext"/> to use for data operations.
        /// </summary>
        /// <returns></returns>
        protected abstract TDbContext CreateDbContext();

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
        /// <param name="from">The object to map properties from</param>
        /// <param name="to">The object to map properties to</param>
        /// <remarks>Default implementation maps all properties with the same name.</remarks>
        protected virtual void Map(object from, object to)
        {
            if (from == null) throw new ArgumentNullException("from");
            if (to == null) throw new ArgumentNullException("to");
            var webModelProperties = from.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var property in webModelProperties)
            {
                MapProperty(from, to, property);
            }
        }

        /// <summary>
        /// Map a property from one object to another.
        /// </summary>
        /// <param name="from">The object to map the property from</param>
        /// <param name="to">The object to map the property to</param>
        /// <param name="fromProperty">The property to map from</param>
        protected virtual void MapProperty(object from, object to, PropertyInfo fromProperty)
        {
            var dataModelProperty = to.GetType().GetProperty(fromProperty.Name, BindingFlags.Public | BindingFlags.Instance);
            if (dataModelProperty != null)
            {
                var value = fromProperty.GetValue(from);
                dataModelProperty.SetValue(to, value);
            }
        }

        /// <summary>
        /// Create a selector expression.
        /// </summary>
        /// <typeparam name="TDataModel">Data model type</typeparam>
        /// <typeparam name="TWebModel">Web model type</typeparam>
        /// <returns>Default implementation selects all properties with the same name.</returns>        
        protected virtual Expression<Func<TDataModel, TWebModel>> CreateSelector<TDataModel, TWebModel>()
        {
            var dataModelArgument = Expression.Parameter(typeof(TDataModel));
            var memberBindings = CreateMemberBindings<TDataModel, TWebModel>(dataModelArgument);
            var memberInit = Expression.MemberInit(Expression.New(typeof(TWebModel)), memberBindings);
            return Expression.Lambda<Func<TDataModel, TWebModel>>(memberInit, dataModelArgument);
        }

        /// <summary>
        /// Create member bindings for use in <see cref="CreateSelector{TDataModel, TWebModel}"/>
        /// </summary>
        /// <typeparam name="TDataModel">The data model type</typeparam>
        /// <typeparam name="TWebModel">The web model type</typeparam>
        /// <param name="dataModel">The data model parameter expression</param>
        /// <returns>A collection of <see cref="MemberBinding"/> objects</returns>
        protected virtual ICollection<MemberBinding> CreateMemberBindings<TDataModel, TWebModel>(ParameterExpression dataModel)
        {
            var webModelProperties = typeof(TWebModel).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var memberBindings = new List<MemberBinding>();
            foreach (var webModelProperty in webModelProperties)
            {
                var dataModelProperty = typeof(TDataModel).GetProperty(webModelProperty.Name, BindingFlags.Public | BindingFlags.Instance);
                if (dataModelProperty != null)
                {
                    memberBindings.Add(Expression.Bind(webModelProperty, Expression.MakeMemberAccess(dataModel, dataModelProperty)));
                }
            }
            return memberBindings;
        }

        /// <summary>
        /// Asynchronously retrieve an array of objects from the database.
        /// </summary>
        /// <typeparam name="TDataModel">The data model type</typeparam>
        /// <typeparam name="TWebModel">The web model type</typeparam>
        /// <param name="predicate">The predicate used to filter results</param>
        /// <param name="cancellationToken"></param>
        /// <returns>A task that represents the asynchronous operation. The task result contains an array of type <typeparamref name="TWebModel"/>.</returns>
        protected virtual Task<TWebModel[]> ToArrayAsync<TDataModel, TWebModel>(Expression<Func<TDataModel, bool>> predicate, CancellationToken cancellationToken) where TDataModel : class
        {
            return FromDbAsync(db => db.Set<TDataModel>().Where(predicate).Select(CreateSelector<TDataModel, TWebModel>()).ToArrayAsync(cancellationToken));
        }

        /// <summary>
        /// Asynchronously retrieve a single object from the database.
        /// </summary>
        /// <typeparam name="TDataModel">The data model type</typeparam>
        /// <typeparam name="TWebModel">The web model type</typeparam>
        /// <param name="predicate">The predicate used to filter results</param>
        /// <param name="cancellationToken"></param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a single object of type <typeparamref name="TWebModel"/>, or null.</returns>
        protected virtual Task<TWebModel> SingleOrDefaultAsync<TDataModel, TWebModel>(Expression<Func<TDataModel, bool>> predicate, CancellationToken cancellationToken) where TDataModel : class
        {
            return FromDbAsync(db => db.Set<TDataModel>().Where(predicate).Select(CreateSelector<TDataModel, TWebModel>()).SingleOrDefaultAsync(cancellationToken));
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
    }
}
