using Autofac;
using Autofac.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
builder.Host.ConfigureContainer<ContainerBuilder>(builder =>
{
    builder.RegisterGeneric(typeof(Repository<>))
        .Named("RealRepo", typeof(IRepository<>))
        .InstancePerLifetimeScope();

    builder.RegisterGenericDecorator(typeof(LoggingRepository<>), typeof(IRepository<>), "RealRepo")
        .InstancePerLifetimeScope();
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/weatherforecast", (IRepository<WeatherForecast> repo) =>
{
    return repo.List();
})
.WithName("GetWeatherForecast")
.WithOpenApi();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}


interface IRepository<T>
{
    IEnumerable<T> List();
}

class Repository<T> : IRepository<T>
{
    public Repository() { }
    public IEnumerable<T> List()
    {
        var summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };
        var forecast = Enumerable.Range(1, 5).Select(index =>
            new WeatherForecast
            (
                DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                Random.Shared.Next(-20, 55),
                summaries[Random.Shared.Next(summaries.Length)]
            )
        );

        // if T is WeatherForecast, then return forecast
        // otherwise, return empty list
        return typeof(T) == typeof(WeatherForecast) ? forecast.Cast<T>() : Enumerable.Empty<T>();
    }
}

class LoggingRepository<T> : IRepository<T>
{
    private readonly IRepository<T> _repository;

    public LoggingRepository(IRepository<T> repository)
    {
        _repository = repository;
    }

    public IEnumerable<T> List()
    {
        Console.WriteLine($"Calling {_repository.GetType().Name}<{typeof(T).Name}>.List()");
        return _repository.List();
    }
}

