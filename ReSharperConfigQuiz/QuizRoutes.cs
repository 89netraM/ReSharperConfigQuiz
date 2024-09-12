using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace ReSharperConfigQuiz;

public static class QuizRoutes
{
    public static void MapQuizRoutes(this IEndpointRouteBuilder routeBuilder, string prefix)
    {
        var group = routeBuilder.MapGroup(prefix);
        group.MapGet(pattern: "", GetAllQuizzes);
        group.MapGet(pattern: "{id}", GetQuiz);

        group.MapPost(pattern: "", AddQuiz);
        group.MapPost(pattern: "{quizId}/questions", AddQuestion).RequireAuthorization();
        group.MapDelete(pattern: "{id}", RemoveQuiz).RequireAuthorization();
        group.MapDelete(pattern: "{quizId}/questions/{id}", RemoveQuestion).RequireAuthorization();
    }

    private static IAsyncEnumerable<Quiz> GetAllQuizzes([FromServices] DbContext dbContext) =>
        dbContext.Quizzes.AsNoTracking().AsAsyncEnumerable();

    private static async Task<IResult> GetQuiz([FromServices] DbContext dbContext, [FromRoute] Guid id)
    {
        var quiz = await dbContext.Quizzes
            .AsNoTracking()
            .Include(q => q.Questions)
            .ThenInclude(q => q.Answers)
            .ThenInclude(a => a.Example)
            .FirstOrDefaultAsync(q => q.Id == id);

        return quiz is not null ? Results.Json(quiz) : Results.NotFound();
    }

    public static async Task<Quiz> AddQuiz([FromServices] DbContext dbContext, [FromBody] Quiz quiz)
    {
        var entity = dbContext.Quizzes.Add(quiz);
        await dbContext.SaveChangesAsync();

        return entity.Entity;
    }

    public static async Task<IResult> AddQuestion(
        [FromServices] DbContext dbContext,
        [FromRoute] Guid quizId,
        [FromBody] Question question)
    {
        var quiz = await dbContext.Quizzes.Include(q => q.Questions).FirstOrDefaultAsync(q => q.Id == quizId);

        if (quiz is null)
        {
            return Results.NotFound(value: "Quiz not found");
        }

        dbContext.Add(question);
        quiz.Questions.Add(question);
        await dbContext.SaveChangesAsync();

        return Results.Json(question);
    }

    public static async Task<IResult> RemoveQuestion(
        [FromServices] DbContext dbContext,
        [FromRoute] Guid quizId,
        [FromRoute] Guid id)
    {
        var quiz = await dbContext.Quizzes
            .Include(q => q.Questions)
            .ThenInclude(q => q.Answers)
            .ThenInclude(a => a.Example)
            .FirstOrDefaultAsync(q => q.Id == quizId);

        if (quiz is null)
        {
            return Results.NotFound(value: "Quiz not found");
        }

        quiz.Questions.RemoveAll(q => q.Id == id);
        await dbContext.SaveChangesAsync();

        return Results.Ok();
    }

    public static async Task<IResult> RemoveQuiz([FromServices] DbContext dbContext, [FromRoute] Guid id)
    {
        var quiz = await dbContext.Quizzes
            .Include(q => q.Questions)
            .ThenInclude(q => q.Answers)
            .ThenInclude(a => a.Example)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (quiz is null)
        {
            return Results.NotFound();
        }

        dbContext.Quizzes.Remove(quiz);
        await dbContext.SaveChangesAsync();

        return Results.Ok();
    }
}
