using EventInfo.Configurations;
using EventInfo.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<MqttSettings>(builder.Configuration.GetSection("DefaultMqttSettings"));
builder.Services.Configure<ClickHouseSettings>(builder.Configuration.GetSection("DefaultClickHouseSettings"));

builder.Services.AddTransient<ClickHouseService>();
builder.Services.AddHostedService<MqttService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
