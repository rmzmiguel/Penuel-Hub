using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi.Models;
using Penuel.Application;
using Penuel.Application.Abstractions;
using Penuel.Domain.Constants;
using Penuel.Infrastructure;
using Penuel.Infrastructure.Security;
using Penuel.WebApi.Authorization;
using Penuel.WebApi.Middleware;
using Penuel.WebApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);

builder.Services.AddAuthorization(options =>
{
    // Los superusuarios se añaden a TODA política en lugar de nombrarse en cada una: una
    // política que se olvidara de incluirlos los dejaría fuera en la puerta del controlador,
    // y ni siquiera llegarían a AuthorizationBehavior, que es donde saltan la autorización.
    // Componer aquí es lo que garantiza que las dos puertas digan lo mismo.
    string[] ConRoles(params string[] roles) => [.. roles, .. RoleNames.Superusers];

    // Política nombrada por intención, no por rol suelto (Sección 8.2).
    options.AddPolicy(Policies.RequirePastor, policy => policy
        .RequireAuthenticatedUser()
        .RequireRole(ConRoles(RoleNames.Pastor)));

    options.AddPolicy(Policies.RequireSundaySchoolRecorder, policy => policy
        .RequireAuthenticatedUser()
        .RequireRole(ConRoles(RoleNames.Pastor, RoleNames.SundaySchoolRecorder)));
});

// ICurrentUser lee los claims de la petición en curso: por eso vive en la WebApi,
// que es la única capa que conoce HttpContext.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Penuel API — Core",
        Version = "v1",
        Description =
            "Identidad, membresía, organización y control de acceso de la Comunidad Cristiana Penuel. " +
            "Salvo /api/auth/* y /api/me/*, todos los endpoints requieren el rol Pastor."
    });

    var scheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = JwtBearerDefaults.AuthenticationScheme.ToLowerInvariant(),
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Pega aquí el accessToken que devuelve /api/auth/login (sin escribir 'Bearer').",
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = JwtBearerDefaults.AuthenticationScheme
        }
    };

    options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, scheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement { [scheme] = Array.Empty<string>() });
});

// Render —y cualquier PaaS— asigna el puerto por la variable PORT y espera que el
// proceso escuche ahí. ASP.NET no la lee solo, así que se traduce aquí en vez de
// pedirle a quien despliega que recuerde poner ASPNETCORE_URLS a mano.
var port = Environment.GetEnvironmentVariable("PORT");

if (!string.IsNullOrWhiteSpace(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

// CORS: el origen sale EXCLUSIVAMENTE de FRONTEND_URL. Sin esa variable no se
// registra ninguna política, así que en local —donde el proxy de Vite hace que
// todo sea mismo origen— no hay nada abierto de más.
var frontendUrl = builder.Configuration["FRONTEND_URL"];

if (!string.IsNullOrWhiteSpace(frontendUrl))
{
    builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
        .WithOrigins(frontendUrl.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        .AllowAnyHeader()
        .AllowAnyMethod()));
}

var app = builder.Build();

// Primero de la cadena: cualquier excepción de lo que venga después termina aquí (Sección 8.3).
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Penuel API — Core");
        options.DocumentTitle = "Penuel API";
    });
}

// El TLS lo termina el proxy de la plataforma, que reenvía HTTP plano. Sin esto la
// aplicación se cree en texto claro y UseHttpsRedirection intentaría redirigir una
// petición que YA venía cifrada; con la cabecera, ve el esquema original y no hace nada.
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    // El proxy es de la plataforma y su dirección no se conoce de antemano.
    KnownNetworks = { },
    KnownProxies = { }
});

app.UseHttpsRedirection();

// Antes de autenticar: una preflight OPTIONS no lleva token y debe responderse igual.
if (!string.IsNullOrWhiteSpace(frontendUrl))
{
    app.UseCors();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
