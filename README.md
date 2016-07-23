#JKH.WebApi
This is just a utility library I created to allow easy creation of RESTful Web API controllers that
use Entity Framework for backend storage.

If you stumble across this library in a Google search, please realize that I didn't create it as any kind of best practices code base. It's just a bunch of shortcuts and tricks that I use on a regular basis. So... leave the pitchforks at home :)
###Usage:
```C#
//DB Entity
public class Thing
{
  public int Key { get; set; }
  public string Name { get; set; }
  public string Secret { get; set; }
}
//DB Entity Context
public class ThingContext : DbContext
{
  public DbSet<Thing> Things { get; set; }
}
//Web API Controller DTO
public class ThingDto
{
  public int Key { get; set; }
  public string Name { get; set; }
}
//Web API Controller
[RoutePrefix("api/things/{key}")
public class ThingController : JKH.WebApi.CrudController<ThingContext, Thing, ThingDto, int>
{
  //Inherits RESTful methods from CrudController
  //Override inherited methods as needed, but out of the box
  //you get Get, GetAll, Insert, Update, Delete
}
//Web API Startup
public static class WebApiStartup
{
  public static void Configure(HttpConfiguration config){
    //Configure Web API attribute routing to allow inheritance
    config.MapHttpAttributeRoutes(new JKH.WebApi.AllowInheritanceDirectRouteProvider());
  }
}
```
