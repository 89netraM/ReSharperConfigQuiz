using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace ReSharperConfigQuiz;

public static class AnswerGroupRoutes
{
    public static void MapAnswerGroupRoutes(this IEndpointRouteBuilder routeBuilder, string prefix)
    {
        var group = routeBuilder.MapGroup(prefix);
        group.MapGet(pattern: "{id}", GetAnswerGroup);
        group.MapGet(pattern: "{id}/common-config", GetCommonConfig);

        group.MapPost(pattern: "", AddAnswerGroup);
        group.MapDelete(pattern: "{id}", RemoveAnswerGroup);
        group.MapPost(pattern: "{id}/submission", AddSubmission);
    }

    private static async Task<IResult> GetAnswerGroup([FromServices] DbContext dbContext, [FromRoute] Guid id)
    {
        var answerGroup = await dbContext.AnswerGroups
            .AsNoTracking()
            .Include(ag => ag.Submissions)
            .ThenInclude(a => a.Answers)
            .FirstOrDefaultAsync(ag => ag.Id == id || ag.PublicId == id);

        return answerGroup is not null ? Results.Json(answerGroup) : Results.NotFound();
    }

    private static async Task<IResult> AddAnswerGroup([FromServices] DbContext dbContext, [FromQuery] Guid quizId)
    {
        var quiz = await dbContext.Quizzes.FindAsync(quizId);
        if (quiz is null)
        {
            return Results.NotFound(value: "Quiz not found");
        }

        var answerGroup = new AnswerGroup { Quiz = quiz, Submissions = [] };
        var entity = dbContext.AnswerGroups.Add(answerGroup);
        await dbContext.SaveChangesAsync();

        return Results.Json(entity.Entity);
    }

    private static async Task<IResult> RemoveAnswerGroup([FromServices] DbContext dbContext, [FromRoute] Guid id)
    {
        var answerGroup =
            await dbContext.AnswerGroups.Include(ag => ag.Submissions).FirstOrDefaultAsync(ag => ag.Id == id);

        if (answerGroup is null)
        {
            return Results.NotFound();
        }

        dbContext.AnswerGroups.Remove(answerGroup);
        await dbContext.SaveChangesAsync();

        return Results.Ok();
    }

    private static async Task<IResult> AddSubmission(
        [FromServices] DbContext dbContext,
        [FromRoute] Guid id,
        [FromBody] SubmissionDto submissionDto)
    {
        var answerGroup = await dbContext.AnswerGroups
            .Include(ag => ag.Submissions)
            .Include(ag => ag.Quiz)
            .ThenInclude(q => q.Questions)
            .ThenInclude(q => q.Answers)
            .ThenInclude(a => a.Example)
            .FirstOrDefaultAsync(ag => ag.Id == id || ag.PublicId == id);

        if (answerGroup is null)
        {
            return Results.NotFound(value: "Answer group not found");
        }

        var answerMap = answerGroup.Quiz.Questions.SelectMany(q => q.Answers).ToDictionary(a => a.Id);

        var submission = new Submission { Name = submissionDto.Name, Answers = [] };
        dbContext.Add(submission);
        foreach (var answerId in submissionDto.Answers)
        {
            if (!answerMap.TryGetValue(answerId, out var answer))
            {
                return Results.BadRequest(error: "Invalid answer ID");
            }

            submission.Answers.Add(answer);
        }

        answerGroup.Submissions.Add(submission);
        await dbContext.SaveChangesAsync();

        return Results.Json(submission);
    }

    private static async Task<IResult> GetCommonConfig([FromServices] DbContext dbContext, [FromRoute] Guid id)
    {
        var answerGroup = await dbContext.AnswerGroups
            .AsNoTracking()
            .Include(ag => ag.Submissions)
            .ThenInclude(s => s.Answers)
            .Include(ag => ag.Quiz)
            .ThenInclude(q => q.Questions)
            .ThenInclude(q => q.Answers)
            .FirstOrDefaultAsync(ag => ag.Id == id || ag.PublicId == id);
        if (answerGroup is null)
        {
            return Results.NotFound(value: "Answer group not found");
        }

        var answers = answerGroup.Submissions.SelectMany(s => s.Answers).ToLookup(a => a.Id);

        var commonConfig = new Dictionary<string, string>();
        foreach (var question in answerGroup.Quiz.Questions)
        {
            var propertyValue = question.Answers.MaxBy(a => answers[a.Id].Count())?.PropertyValue;
            if (propertyValue is not null)
            {
                commonConfig[question.PropertyName] = propertyValue;
            }
        }

        return Results.Json(commonConfig);
    }
}
