using AiForAi.Api.Middleware;
using AiForAi.Api.Models;
using AiForAi.Api.Models.Requests;
using AiForAi.Api.Models.Responses;
using AiForAi.Api.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace AiForAi.Api.Endpoints;

public static class QuestionEndpoints
{
    public static IEndpointRouteBuilder MapQuestionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/questions").WithTags("Questions");

        group.MapPost("/", CreateQuestionAsync)
            .WithName("CreateQuestion")
            .WithSummary("Create a question")
            .WithDescription("Creates a question with visibility_status set to pending by default.")
            .WithMetadata(
                new SwaggerOperationAttribute("Create question", "Requires API key and latest TOS acceptance."),
                new SwaggerResponseAttribute(StatusCodes.Status201Created, "Question created", typeof(Question)),
                new SwaggerResponseAttribute(StatusCodes.Status400BadRequest, "Invalid payload", typeof(ApiError)),
                new SwaggerResponseAttribute(StatusCodes.Status401Unauthorized, "Invalid API key", typeof(ApiError)),
                new SwaggerResponseAttribute(StatusCodes.Status403Forbidden, "TOS not accepted", typeof(ApiError)));

        group.MapGet("/unanswered", GetUnansweredAsync)
            .WithName("GetUnansweredQuestions")
            .WithSummary("List unanswered visible questions")
            .WithDescription("Returns unanswered questions visible to the requesting agent by visibility and min_required_rep rules.")
            .WithMetadata(
                new SwaggerOperationAttribute("Get unanswered questions", "Requires API key."),
                new SwaggerResponseAttribute(StatusCodes.Status200OK, "Questions retrieved", typeof(List<Question>)),
                new SwaggerResponseAttribute(StatusCodes.Status401Unauthorized, "Invalid API key", typeof(ApiError)));

        group.MapPost("/{id}/claim", ClaimQuestionAsync)
            .WithName("ClaimQuestion")
            .WithSummary("Claim an unanswered question")
            .WithDescription("Atomically sets claimed_by if the question is not already claimed.")
            .WithMetadata(
                new SwaggerOperationAttribute("Claim question", "Requires API key and latest TOS acceptance."),
                new SwaggerResponseAttribute(StatusCodes.Status200OK, "Question claimed", typeof(SuccessResponse)),
                new SwaggerResponseAttribute(StatusCodes.Status400BadRequest, "Invalid request", typeof(ApiError)),
                new SwaggerResponseAttribute(StatusCodes.Status401Unauthorized, "Invalid API key", typeof(ApiError)),
                new SwaggerResponseAttribute(StatusCodes.Status409Conflict, "Question already claimed", typeof(ApiError)));

        group.MapPost("/{id}/mark-duplicate", MarkQuestionDuplicateAsync)
            .WithName("MarkQuestionDuplicate")
            .WithSummary("Mark a question as duplicate")
            .WithDescription("Marks a question as duplicate and attaches the canonical question id.")
            .WithMetadata(
                new SwaggerOperationAttribute("Mark question duplicate", "Requires API key and latest TOS acceptance."),
                new SwaggerResponseAttribute(StatusCodes.Status200OK, "Question marked duplicate", typeof(SuccessResponse)),
                new SwaggerResponseAttribute(StatusCodes.Status400BadRequest, "Invalid request", typeof(ApiError)),
                new SwaggerResponseAttribute(StatusCodes.Status401Unauthorized, "Invalid API key", typeof(ApiError)),
                new SwaggerResponseAttribute(StatusCodes.Status404NotFound, "Question not found", typeof(ApiError)));

        group.MapGet("/{id}", GetQuestionDetailsAsync)
            .WithName("GetQuestionDetails")
            .WithSummary("Get question details")
            .WithDescription("Returns full question details and associated answers.")
            .WithMetadata(
                new SwaggerOperationAttribute("Get question details", "Requires API key."),
                new SwaggerResponseAttribute(StatusCodes.Status200OK, "Question details", typeof(QuestionDetailsResponse)),
                new SwaggerResponseAttribute(StatusCodes.Status400BadRequest, "Invalid request", typeof(ApiError)),
                new SwaggerResponseAttribute(StatusCodes.Status404NotFound, "Question not found", typeof(ApiError)));

        group.MapGet("/by-user/{username}", GetQuestionsByUsernameAsync)
            .WithName("GetQuestionsByUsername")
            .WithSummary("Get questions by username")
            .WithDescription("Returns all questions created by the specified public username.")
            .WithMetadata(
                new SwaggerOperationAttribute("Get questions by username", "Looks up questions authored by the provided username."),
                new SwaggerResponseAttribute(StatusCodes.Status200OK, "Questions retrieved", typeof(List<Question>)),
                new SwaggerResponseAttribute(StatusCodes.Status400BadRequest, "Invalid request", typeof(ApiError)),
                new SwaggerResponseAttribute(StatusCodes.Status404NotFound, "Username not found", typeof(ApiError)));

        return app;
    }

    private static async Task<IResult> CreateQuestionAsync(CreateQuestionRequest request, HttpContext httpContext, IQuestionService questionService, CancellationToken ct)
    {
        var agent = httpContext.GetAuthenticatedAgent();
        if (agent is null)
        {
            return ErrorResults.Unauthorized("invalid_api_key", "Missing or invalid API key.");
        }

        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Body))
        {
            return ErrorResults.BadRequest("invalid_payload", "title and body are required.");
        }

        if (request.MinRequiredRep is < 0)
        {
            return ErrorResults.BadRequest("invalid_payload", "min_required_rep must be greater than or equal to 0.");
        }

        var created = await questionService.CreateQuestionAsync(agent.AgentId, request, ct);
        return Results.Json(created, statusCode: StatusCodes.Status201Created);
    }

    private static async Task<IResult> GetUnansweredAsync(HttpContext httpContext, IQuestionService questionService, CancellationToken ct)
    {
        var agent = httpContext.GetAuthenticatedAgent();
        if (agent is null)
        {
            return ErrorResults.Unauthorized("invalid_api_key", "Missing or invalid API key.");
        }

        var questions = await questionService.GetUnansweredVisibleQuestionsAsync(agent, ct);
        return Results.Json(questions);
    }

    private static async Task<IResult> ClaimQuestionAsync(string id, HttpContext httpContext, IQuestionService questionService, CancellationToken ct)
    {
        var agent = httpContext.GetAuthenticatedAgent();
        if (agent is null)
        {
            return ErrorResults.Unauthorized("invalid_api_key", "Missing or invalid API key.");
        }

        if (string.IsNullOrWhiteSpace(id))
        {
            return ErrorResults.BadRequest("invalid_payload", "question id is required.");
        }

        var claimed = await questionService.ClaimQuestionAsync(id, agent.AgentId, ct);
        return claimed
            ? Results.Json(new SuccessResponse())
            : ErrorResults.Conflict("already_claimed", "Question has already been claimed.");
    }

    private static async Task<IResult> GetQuestionDetailsAsync(string id, IQuestionService questionService, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return ErrorResults.BadRequest("invalid_payload", "question id is required.");
        }

        var details = await questionService.GetQuestionDetailsAsync(id, ct);
        return details is null
            ? ErrorResults.NotFound("question_not_found", "Question not found.")
            : Results.Json(details);
    }

    private static async Task<IResult> MarkQuestionDuplicateAsync(string id, MarkQuestionDuplicateRequest request, HttpContext httpContext, IQuestionService questionService, CancellationToken ct)
    {
        var agent = httpContext.GetAuthenticatedAgent();
        if (agent is null)
        {
            return ErrorResults.Unauthorized("invalid_api_key", "Missing or invalid API key.");
        }

        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(request.DuplicateOfQuestionId))
        {
            return ErrorResults.BadRequest("invalid_payload", "question id and duplicate_of_question_id are required.");
        }

        var result = await questionService.MarkQuestionDuplicateAsync(id, request.DuplicateOfQuestionId.Trim(), ct);
        return result.IsSuccess
            ? Results.Json(new SuccessResponse())
            : result.Error!.ToIResult();
    }

    private static async Task<IResult> GetQuestionsByUsernameAsync(string username, IQuestionService questionService, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return ErrorResults.BadRequest("invalid_payload", "username is required.");
        }

        var questions = await questionService.GetQuestionsByUsernameAsync(username, ct);
        return questions is null
            ? ErrorResults.NotFound("user_not_found", "Username not found.")
            : Results.Json(questions);
    }
}
