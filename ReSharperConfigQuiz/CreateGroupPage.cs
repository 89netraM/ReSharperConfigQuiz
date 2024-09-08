using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace ReSharperConfigQuiz;

public static class CreateGroupPage
{
    public static void MapCreateGroupPage(this IEndpointRouteBuilder builder)
    {
        builder.MapPost(pattern: "group", CreateGroup).DisableAntiforgery();
    }

    private static async Task<IResult> CreateGroup(
        [FromServices] DbContext dbContext,
        [FromForm] CreateGroupDto createGroupDto)
    {
        var quiz = await dbContext.Quizzes.FirstOrDefaultAsync(q => q.Id == createGroupDto.QuizId);
        if (quiz is null)
        {
            return Results.NotFound();
        }

        var group = new AnswerGroup { Quiz = quiz, Submissions = [] };
        dbContext.AnswerGroups.Add(group);
        await dbContext.SaveChangesAsync();

        return Results.LocalRedirect($"~/group/{group.Id}", preserveMethod: false);
    }

    private class CreateGroupDto
    {
        [Required]
        [FromForm(Name = "quizId")]
        public required Guid QuizId { get; init; }
    }
}
