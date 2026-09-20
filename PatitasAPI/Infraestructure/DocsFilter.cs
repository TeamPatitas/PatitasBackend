namespace PatitasAPI.Infraestructure;

using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.AspNetCore.Authorization;

public class DocsFilter : IOperationFilter {
    public class OpenApiStringExtension(string value) : IOpenApiExtension
    {
        private readonly string _value = value;

        public void Write(IOpenApiWriter writer, OpenApiSpecVersion specVersion)
        {
            writer.WriteValue(_value); // Escribe el valor limpiamente en el JSON
        }
    }
    
    public void Apply(OpenApiOperation operation, OperationFilterContext context) {
        var metadata = context.ApiDescription.ActionDescriptor.EndpointMetadata;

        var authAttributes = metadata.OfType<IAuthorizeData>().ToList();
        var policies = authAttributes.Select(a => a.Policy).Where(p => !string.IsNullOrWhiteSpace(p)).ToList();
        operation.Extensions ??= new Dictionary<string, IOpenApiExtension>();
        
        if (policies.Count != 0) {
            operation.Extensions.Add("x-roles-policy", new OpenApiStringExtension(string.Join(", ", policies)));
        }
        else if (authAttributes.Count != 0) {
            operation.Extensions.Add("x-roles-policy", new OpenApiStringExtension("Authenticated"));
        }

        var rateLimitMetadata = metadata.FirstOrDefault(m => m.GetType().Name.Contains("RateLimiting"));
        if (rateLimitMetadata != null) {
            var policyProp = rateLimitMetadata.GetType().GetProperty("PolicyName");
            var policyName = policyProp?.GetValue(rateLimitMetadata)?.ToString();
            
            if (!string.IsNullOrEmpty(policyName)){
                operation.Extensions.Add("x-cooldown-policy", new OpenApiStringExtension(policyName));
                string cooldownSeconds = policyName switch
                {
                    "HealthCheckCooldown" => "30 segundos",              
                    "EmailVerificationCooldown" => "1 minuto",         
                    "ShelterSwitchAviabilityCooldown" => "5 minutos", 
                    _ => "Sin cooldown"
                };

                operation.Extensions.Add("x-cooldown", new OpenApiStringExtension(cooldownSeconds));
            }
        }
    }
}
