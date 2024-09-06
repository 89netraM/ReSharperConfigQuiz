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

    public static Task RemoveQuiz([FromServices] DbContext dbContext, [FromRoute] Guid id) =>
        dbContext.Quizzes.Where(q => q.Id == id).ExecuteDeleteAsync();
}
