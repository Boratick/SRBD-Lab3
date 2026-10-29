using StoreApi.Data;
using StoreApi.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "Store API (ЛБ3, СРБД)", Version = "v1" });
    var xml = Path.Combine(AppContext.BaseDirectory, "StoreApi.xml");
    if (File.Exists(xml)) options.IncludeXmlComments(xml);
});

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<SqlExceptionHandler>();
builder.Services.AddSingleton<StoreRepository>();

var app = builder.Build();

app.UseExceptionHandler();

app.UseSwagger();
app.UseSwaggerUI();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();

app.Run();
