using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Builder;
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
        group.MapPost(pattern: "{quizId}/questions", AddQuestion);
        group.MapDelete(pattern: "{quizId}/questions/{id}", RemoveQuestion);
        group.MapDelete(pattern: "{id}", RemoveQuiz);
    }

    private static IAsyncEnumerable<Quiz> GetAllQuizzes([FromServices] DbContext dbContext) =>
        dbContext.Quizzes.AsNoTracking().AsAsyncEnumerable();

    private static Task<Quiz?> GetQuiz([FromServices] DbContext dbContext, [FromRoute] Guid id) =>
        dbContext.Quizzes
            .AsNoTracking()
            .Include(q => q.Questions)
            .ThenInclude(q => q.Answers)
            .ThenInclude(a => a.Example)
            .FirstOrDefaultAsync(q => q.Id == id);

    public static async Task<Quiz> AddQuiz([FromServices] DbContext dbContext, [FromBody] Quiz quiz)
    {
        var entity = dbContext.Quizzes.Add(quiz);
        await dbContext.SaveChangesAsync();

        return entity.Entity;
    }

    public static async Task<Question?> AddQuestion(
        [FromServices] DbContext dbContext,
        [FromRoute] Guid quizId,
        [FromBody] Question question)
    {
        var quiz = await dbContext.Quizzes.Include(q => q.Questions).FirstOrDefaultAsync(q => q.Id == quizId);

        if (quiz is null)
        {
            return null;
        }

        dbContext.Add(question);
        quiz.Questions.Add(question);
        await dbContext.SaveChangesAsync();

        return question;
    }

    public static async Task RemoveQuestion(
        [FromServices] DbContext dbContext,
        [FromRoute] Guid quizId,
        [FromRoute] Guid id)
    {
        var quiz = await dbContext.Quizzes.Include(q => q.Questions).FirstOrDefaultAsync(q => q.Id == quizId);

        if (quiz is null)
        {
            return;
        }

        quiz.Questions.RemoveAll(q => q.Id == id);
        await dbContext.SaveChangesAsync();
    }

    public static Task RemoveQuiz([FromServices] DbContext dbContext, [FromRoute] Guid id) =>
        dbContext.Quizzes.Where(q => q.Id == id).ExecuteDeleteAsync();
}
