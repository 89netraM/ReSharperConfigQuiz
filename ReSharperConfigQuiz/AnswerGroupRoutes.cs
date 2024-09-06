using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Builder;
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

    private static Task<AnswerGroup?> GetAnswerGroup([FromServices] DbContext dbContext, [FromRoute] Guid id) =>
        dbContext.AnswerGroups
            .AsNoTracking()
            .Include(ag => ag.Submissions)
            .ThenInclude(a => a.Answers)
            .FirstOrDefaultAsync(ag => ag.Id == id);

    private static async Task<AnswerGroup?> AddAnswerGroup([FromServices] DbContext dbContext, [FromQuery] Guid quizId)
    {
        var quiz = await dbContext.Quizzes.FindAsync(quizId);
        if (quiz is null)
        {
            return null;
        }

        var answerGroup = new AnswerGroup { Quiz = quiz, Submissions = [] };
        var entity = dbContext.AnswerGroups.Add(answerGroup);
        await dbContext.SaveChangesAsync();

        return entity.Entity;
    }

    private static async Task RemoveAnswerGroup([FromServices] DbContext dbContext, [FromRoute] Guid id)
    {
        var quiz = await dbContext.AnswerGroups.Include(ag => ag.Submissions).FirstOrDefaultAsync(ag => ag.Id == id);

        if (quiz is null)
        {
            return;
        }

        dbContext.AnswerGroups.Remove(quiz);
        await dbContext.SaveChangesAsync();
    }

    private static async Task<Submission?> AddSubmission(
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
            .FirstOrDefaultAsync(ag => ag.Id == id);

        if (answerGroup is null)
        {
            return null;
        }

        var answerMap = answerGroup.Quiz.Questions.SelectMany(q => q.Answers).ToDictionary(a => a.Id);

        var submission = new Submission { Name = submissionDto.Name, Answers = [] };
        dbContext.Add(submission);
        foreach (var answerId in submissionDto.Answers)
        {
            if (!answerMap.TryGetValue(answerId, out var answer))
            {
                return null;
            }

            submission.Answers.Add(answer);
        }

        answerGroup.Submissions.Add(submission);
        await dbContext.SaveChangesAsync();

        return submission;
    }

    private static async Task<Dictionary<string, string>?> GetCommonConfig(
        [FromServices] DbContext dbContext,
        [FromRoute] Guid id)
    {
        var answerGroup = await dbContext.AnswerGroups
            .AsNoTracking()
            .Include(ag => ag.Submissions)
            .ThenInclude(s => s.Answers)
            .Include(ag => ag.Quiz)
            .ThenInclude(q => q.Questions)
            .ThenInclude(q => q.Answers)
            .FirstOrDefaultAsync(ag => ag.Id == id);
        if (answerGroup is null)
        {
            return null;
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

        return commonConfig;
    }
}
