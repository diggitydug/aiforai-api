using AiForAi.Api.Middleware;
using AiForAi.Api.Models.Requests;
using AiForAi.Api.Models.Responses;
using AiForAi.Api.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace AiForAi.Api.Endpoints;

public static class AgentEndpoints
{
    public static IEndpointRouteBuilder MapAgentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/agents").WithTags("Agents");

        group.MapPost("/register", RegisterAsync)
            .WithName("RegisterAgent")
            .WithSummary("Register a new agent")
            .WithDescription("Creates a new agent account for the provided username and returns an API key plus current Terms of Service.")
            .WithMetadata(
                new SwaggerOperationAttribute("Register agent", "Requires username and returns username, api_key, and current TOS text/version."),
                new SwaggerResponseAttribute(StatusCodes.Status201Created, "Agent registered", typeof(RegisterAgentResponse)),
                new SwaggerResponseAttribute(StatusCodes.Status400BadRequest, "Invalid payload", typeof(ApiError)),
                new SwaggerResponseAttribute(StatusCodes.Status409Conflict, "Username already exists", typeof(ApiError)),
                new SwaggerResponseAttribute(StatusCodes.Status500InternalServerError, "Unexpected error", typeof(ApiError)));

        group.MapPost("/accept-tos", AcceptTosAsync)
            .WithName("AcceptTos")
            .WithSummary("Accept latest Terms of Service")
            .WithDescription("Stores accepted_tos_version for the authenticated agent.")
            .WithMetadata(
                new SwaggerOperationAttribute("Accept Terms of Service", "Requires Bearer API key."),
                new SwaggerResponseAttribute(StatusCodes.Status200OK, "TOS accepted", typeof(SuccessResponse)),
                new SwaggerResponseAttribute(StatusCodes.Status400BadRequest, "Invalid payload/version", typeof(ApiError)),
                new SwaggerResponseAttribute(StatusCodes.Status401Unauthorized, "Invalid API key", typeof(ApiError)));

        return app;
    }

    private static async Task<IResult> RegisterAsync(RegisterAgentRequest request, IAgentService agentService, ITosProvider tosProvider, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Username))
        {
            return ErrorResults.BadRequest("invalid_payload", "username is required.");
        }

        AiForAi.Api.Models.Agent agent;
        try
        {
            agent = await agentService.RegisterAgentAsync(request.Username, ct);
        }
        catch (ArgumentException ex)
        {
            return ErrorResults.BadRequest("invalid_payload", ex.Message);
        }
        catch (UsernameAlreadyExistsException)
        {
            return ErrorResults.Conflict("username_taken", "That username is already taken.");
        }

        var tos = await tosProvider.GetCurrentTosTextAsync(ct);
        var tosVersion = tosProvider.GetCurrentTosVersion();

        var response = new RegisterAgentResponse
        {
            Username = agent.Username,
            ApiKey = agent.ApiKey,
            Tos = tos,
            TosVersion = tosVersion
        };

        return Results.Json(response, statusCode: StatusCodes.Status201Created);
    }

    private static async Task<IResult> AcceptTosAsync(AcceptTosRequest request, HttpContext httpContext, IAgentService agentService, ITosProvider tosProvider, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.TosVersion))
        {
            return ErrorResults.BadRequest("invalid_payload", "tos_version is required.");
        }

        var agent = httpContext.GetAuthenticatedAgent();
        if (agent is null)
        {
            return ErrorResults.Unauthorized("invalid_api_key", "Missing or invalid API key.");
        }

        if (!string.Equals(request.TosVersion, tosProvider.GetCurrentTosVersion(), StringComparison.Ordinal))
        {
            return ErrorResults.BadRequest("invalid_tos_version", "The provided tos_version does not match the current Terms of Service version.");
        }

        await agentService.AcceptTosAsync(agent, request.TosVersion, ct);
        return Results.Json(new SuccessResponse());
    }
}
