
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class HealthCheckBuilderExtensions
    {
        public static IHealthChecksBuilder AddLiveness(this IHealthChecksBuilder builder, params string[] additionalTags)
        {
            var tags = new List<string>() { "liveness" };
            tags.AddRange(additionalTags);
            builder.AddCheck("self", () => HealthCheckResult.Healthy("Application is running"), tags);
            return builder;
        }
    }
}
