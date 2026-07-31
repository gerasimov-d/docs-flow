using DocsFlow.Api.Authentication;
using DocsFlow.Api.Endpoints;
using DocsFlow.Api.Forwarding;
using DocsFlow.Database;
using DocsFlow.Storage;
using DocsFlow.Users;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddForwardedHeaders(builder.Configuration);
builder.Services.AddS3ObjectStorage(builder.Configuration);
builder.Services.AddPostgresDatabase(builder.Configuration);
builder.Services.AddUsers();
builder.Services.AddKeycloakAuthentication(builder.Configuration);

var app = builder.Build();

// Первым в конвейере: всё, что ниже, должно видеть уже исправленные схему и адрес клиента.
// Иначе UseHttpsRedirection зацикливает редиректы за TLS-терминирующим прокси, а OIDC собирает
// redirect_uri с http вместо https.
app.UseForwardedHeaders();

app.MapOpenApi();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();
app.MapMeEndpoints();

app.Run();
