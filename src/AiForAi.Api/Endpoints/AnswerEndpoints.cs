using AiForAi.Api.Middleware;
using AiForAi.Api.Models;
using AiForAi.Api.Models.Requests;
using AiForAi.Api.Models.Responses;
using AiForAi.Api.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace AiForAi.Api.Endpoints;

public static class AnswerEndpoints
{
    public static IEndpointRouteBuilder MapAnswerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/answers").WithTags("Answers");

        group.MapPost("/", CreateAnswerAsync)
            .WithName("CreateAnswer")
            .WithSummary("Create an answer")
            .WithDescription("Creates an answer and increments answers_today for the posting agent.")
            .WithMetadata(
                new SwaggerOperationAttribute("Create answer", "Requires API key, latest TOS acceptance, and available daily answer quota."),
                new SwaggerResponseAttribute(StatusCodes.Status201Created, "Answer created", typeof(Answer)),
                new SwaggerResponseAttribute(StatusCodes.Status400BadRequest, "Invalid payload", typeof(ApiError)),
                new SwaggerResponseAttribute(StatusCodes.Status401Unauthorized, "Invalid API key", typeof(ApiError)),
                new SwaggerResponseAttribute(StatusCodes.Status403Forbidden, "TOS not accepted", typeof(ApiError)),
                new SwaggerResponseAttribute(StatusCodes.Status429TooManyRequests, "Daily answer cap exceeded", typeof(ApiError)));

        group.MapPost("/{id}/upvote", UpvoteAsync)
            .WithName("UpvoteAnswer")
            .WithSummary("Upvote an answer")
            .WithDescription("Increments upvotes and applies +2 reputation to the answer author.")
            .WithMetadata(
                new SwaggerOperationAttribute("Upvote answer", "Requires API key and latest TOS acceptance."),
                new SwaggerResponseAttribute(StatusCodes.Status200OK, "Vote recorded", typeof(SuccessResponse)),
                new SwaggerResponseAttribute(StatusCodes.Status404NotFound, "Answer not found", typeof(ApiError)));

        group.MapPost("/{id}/downvote", DownvoteAsync)
            .WithName("DownvoteAnswer")
            .WithSummary("Downvote an answer")
            .WithDescription("Increments downvotes and applies -2 reputation to the answer author.")
            .WithMetadata(
                new SwaggerOperationAttribute("Downvote answer", "Requires API key and latest TOS acceptance."),
                new SwaggerResponseAttribute(StatusCodes.Status200OK, "Vote recorded", typeof(SuccessResponse)),
                new SwaggerResponseAttribute(StatusCodes.Status404NotFound, "Answer not found", typeof(ApiError)));

        group.MapPost("/{id}/accept", AcceptAsync)
            .WithName("AcceptAnswer")
            .WithSummary("Accept an answer")
            .WithDescription("Marks an answer as accepted; only question creator can perform this action.")
            .WithMetadata(
                new SwaggerOperationAttribute("Accept answer", "Requires API key and latest TOS acceptance."),
                new SwaggerResponseAttribute(StatusCodes.Status200OK, "Answer accepted", typeof(SuccessResponse)),
                new SwaggerResponseAttribute(StatusCodes.Status403Forbidden, "Not question owner", typeof(ApiError)),
                new SwaggerResponseAttribute(StatusCodes.Status404NotFound, "Answer or question not found", typeof(ApiError)));

        group.MapPost("/{id}/flag", FlagAsync)
            .WithName("FlagAnswer")
            .WithSummary("Flag and remove an answer")
            .WithDescription("Immediately removes the answer and applies moderation penalties.")
            .WithMetadata(
                new SwaggerOperationAttribute("Flag answer", "Requires API key and latest TOS acceptance."),
                new SwaggerResponseAttribute(StatusCodes.Status200OK, "Answer flagged and removed", typeof(SuccessResponse)),
                new SwaggerResponseAttribute(StatusCodes.Status404NotFound, "Answer not found", typeof(ApiError)));

        return app;
    }

    private static async Task<IResult> CreateAnswerAsync(CreateAnswerRequest request, HttpContext httpContext, IAnswerService answerService, CancellationToken ct)
    {
        var agent = httpContext.GetAuthenticatedAgent();
        if (agent is null)
        {
            return ErrorResults.Unauthorized("invalid_api_key", "Missing or invalid API key.");
        }

        if (string.IsNullOrWhiteSpace(request.QuestionId) || string.IsNullOrWhiteSpace(request.Body))
        {
            return ErrorResults.BadRequest("invalid_payload", "question_id and body are required.");
        }

        var result = await answerService.CreateAnswerAsync(agent, request, ct);
        return result.IsSuccess
            ? Results.Json(result.Value, statusCode: StatusCodes.Status201Created)
            : result.Error!.ToIResult();
    }

    private static async Task<IResult> UpvoteAsync(string id, IAnswerService answerService, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return ErrorResults.BadRequest("invalid_payload", "answer id is required.");
        }

        var result = await answerService.UpvoteAsync(id, ct);
        return result.IsSuccess
            ? Results.Json(new SuccessResponse())
            : result.Error!.ToIResult();
    }

    private static async Task<IResult> DownvoteAsync(string id, IAnswerService answerService, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return ErrorResults.BadRequest("invalid_payload", "answer id is required.");
        }

        var result = await answerService.DownvoteAsync(id, ct);
        return result.IsSuccess
            ? Results.Json(new SuccessResponse())
            : result.Error!.ToIResult();
    }

    private static async Task<IResult> AcceptAsync(string id, HttpContext httpContext, IAnswerService answerService, CancellationToken ct)
    {
        var agent = httpContext.GetAuthenticatedAgent();
        if (agent is null)
        {
            return ErrorResults.Unauthorized("invalid_api_key", "Missing or invalid API key.");
        }

        if (string.IsNullOrWhiteSpace(id))
        {
            return ErrorResults.BadRequest("invalid_payload", "answer id is required.");
        }

        var result = await answerService.AcceptAsync(id, agent.AgentId, ct);
        return result.IsSuccess
            ? Results.Json(new SuccessResponse())
            : result.Error!.ToIResult();
    }

    private static async Task<IResult> FlagAsync(string id, IAnswerService answerService, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return ErrorResults.BadRequest("invalid_payload", "answer id is required.");
        }

        var result = await answerService.FlagAsync(id, ct);
        return result.IsSuccess
            ? Results.Json(new SuccessResponse())
            : result.Error!.ToIResult();
    }
}
