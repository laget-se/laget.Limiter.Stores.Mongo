# laget.Limiter.Store.Mongo
MongoDB store implementation for laget.Limiter...


## Usage
> You can specifiy that a limiter should be used by the following
```c#
builder.Register<ISomeLimit>(c =>
    new AuthorizationLimit(new MongoStore(new MongoUrl(c.Resolve<IConfiguration>().GetConnectionString("MongoConnectionString")), "authorization.calls"),
        new StandardLimit(300, TimeSpan.FromHours(3)))
).SingleInstance();
```