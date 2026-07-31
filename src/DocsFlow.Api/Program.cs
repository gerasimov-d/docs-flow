using DocsFlow.Api.Authentication;
using DocsFlow.Api.Endpoints;
using DocsFlow.Database;
using DocsFlow.Storage;
using DocsFlow.Users;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddS3ObjectStorage(builder.Configuration);
builder.Services.AddPostgresDatabase(builder.Configuration);
builder.Services.AddUsers();
builder.Services.AddKeycloakAuthentication(builder.Configuration);

var app = builder.Build();

app.MapOpenApi();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();
app.MapMeEndpoints();

app.Run();
