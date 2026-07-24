using PruebaTecnicaGabriel.Configutarion;
using PruebaTecnicaGabriel.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<ConfiguracionNodo>(
    builder.Configuration.GetSection("Node"));

builder.Services.AddSingleton<ContenedorPagos>();
builder.Services.AddSingleton<EncolamientoPagosPendiente>();

builder.Services.AddHttpClient<ClienteMallaNodos>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(3);
});

builder.Services.AddHostedService<ProcesadorPagos>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();