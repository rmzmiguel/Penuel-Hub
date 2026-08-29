using Penuel.Infrastructure.Persistence;
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

// La barra final se recorta. `WithOrigins` compara EXACTAMENTE contra la cabecera
// Origin, que nunca la lleva: pegar la URL del panel de Vercel —que sí la muestra—
// hacía que el navegador se topara con una preflight sin `Allow-Origin` y con un
// mensaje que no menciona la barra por ningún lado. Es el error de configuración más
// fácil de cometer y el más difícil de ver.
var allowedOrigins = (frontendUrl ?? string.Empty)
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    .Select(origin => origin.TrimEnd('/'))
    .Where(origin => origin.Length > 0)
    .ToArray();

if (allowedOrigins.Length > 0)
{
    builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
        .WithOrigins(allowedOrigins)
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
// Se registra al arrancar qué orígenes quedaron admitidos. Un fallo de CORS se ve
// desde el navegador como "falta la cabecera" y desde el servidor como un 204 normal:
// sin esta línea, averiguar si la variable llegó bien exige adivinar.
app.Logger.LogInformation(
    "CORS: {Count} origen(es) admitido(s){Origins}",
    allowedOrigins.Length,
    allowedOrigins.Length == 0 ? " — FRONTEND_URL vacía" : " → " + string.Join(", ", allowedOrigins));

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

/*
 * Latido para el monitor externo (UptimeRobot).
 *
 * Toca la BASE a propósito, no solo el proceso. El plan gratuito de Render duerme el
 * servicio a los 15 minutos y el de Supabase pausa el proyecto por inactividad de la
 * BASE DE DATOS: un endpoint que solo respondiera "vivo" despertaría a uno y dejaría
 * que el otro se apagara igual. Con un viaje de ida y vuelta, un solo ping sostiene
 * los dos.
 *
 * `CanConnectAsync` abre y cierra una conexión, que es lo más barato que cuenta como
 * actividad. No lee ninguna tabla ni revela nada: la respuesta es la misma para
 * cualquiera y por eso el endpoint es anónimo.
 *
 * Si la base no responde, devuelve 503 y el monitor avisa. Eso es deseable: un backend
 * "vivo" que no alcanza su base no está sano, y quien mire el panel merece enterarse.
 *
 * Atiende GET y HEAD. No es un detalle: los monitores de disponibilidad —UptimeRobot
 * entre ellos— usan HEAD por omisión, porque solo necesitan el código de estado y no el
 * cuerpo. Con `MapGet` a secas, un HEAD recibía 405 y el monitor marcaba el servicio
 * como caído estando perfectamente vivo.
 */
app.MapMethods("/health", ["GET", "HEAD"], async (ApplicationDbContext db, CancellationToken ct) =>
    await db.Database.CanConnectAsync(ct)
        ? Results.Ok(new { status = "ok" })
        : Results.Json(new { status = "sin base de datos" }, statusCode: StatusCodes.Status503ServiceUnavailable))
   .AllowAnonymous()
   .ExcludeFromDescription();

app.MapControllers();

app.Run();
