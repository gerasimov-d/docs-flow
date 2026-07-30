using DocsFlow.Storage;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddS3ObjectStorage(builder.Configuration);

var app = builder.Build();

app.MapOpenApi();

app.UseHttpsRedirection();

app.Run();
