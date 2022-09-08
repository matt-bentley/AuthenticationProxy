using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Weather.Api.Authentication;
using Weather.Api.Authorization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpContextAccessor();

builder.Services.AddAuthentication(GatewayAuthenticationDefaults.AuthenticationScheme)
                .AddGateway();

builder.Services.AddTransient<IAuthorizationHandler, AdminRequirementHandler>();
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
                                .RequireAuthenticatedUser()
                                .Build();
    options.AddPolicy(AuthorizationPolicies.Admin, new AuthorizationPolicyBuilder()
                                .RequireAuthenticatedUser()
                                .AddRequirements(new AdminRequirement("admin@dev-28752567-admin.okta.com"))
                                .Build());
});

builder.Services.AddHealthChecks()
                .AddLiveness("api");

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/api/liveness", new HealthCheckOptions
{
    Predicate = r => r.Name.Contains("self")
}).AllowAnonymous();
app.MapHealthChecks("/api/healthz", new HealthCheckOptions()
{
    Predicate = _ => true,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
}).AllowAnonymous();

app.Run();
