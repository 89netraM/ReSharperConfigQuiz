using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ReSharperConfigQuiz.Pages;

public class QuizModel(DbContext dbContext) : PageModel
{
    [MemberNotNullWhen(returnValue: false, nameof(Quiz))]
    public new bool NotFound => Response.StatusCode is StatusCodes.Status404NotFound;

    public Quiz? Quiz { get; private set; }

    public bool IsAnswerGroupQuiz { get; private set; }

    public IReadOnlyDictionary<string, string>? Answers { get; private set; }

    public IReadOnlyList<string>? Errors { get; private set; }

    public async Task OnGetAsync([FromRoute] Guid quizId, [FromQuery] Guid? answerGroupId)
    {
        Quiz = await dbContext.Quizzes
            .AsNoTracking()
            .Include(q => q.Questions)
            .ThenInclude(q => q.Answers)
            .ThenInclude(a => a.Example)
            .FirstOrDefaultAsync(q => q.Id == quizId);
        if (Quiz is null)
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
        }

        IsAnswerGroupQuiz = answerGroupId is not null;
    }

    public async Task OnPostAsync(
        [FromRoute] Guid quizId,
        [FromQuery] Guid? answerGroupId,
        [FromForm] QuizAnswerDto answer)
    {
        Quiz = await dbContext.Quizzes
            .Include(q => q.Questions)
            .ThenInclude(q => q.Answers)
            .FirstOrDefaultAsync(q => q.Id == quizId);
        if (Quiz is null)
        {
            Response.StatusCode = StatusCodes.Status404NotFound;

            return;
        }

        var checkedAnswers = new Dictionary<string, Answer>();
        var errors = new List<string>();
        foreach (var question in Quiz.Questions)
        {
            if (answer[question.PropertyName] is { } answerValue
                && question.Answers.FirstOrDefault(a => a.PropertyValue == answerValue) is { } a)
            {
                checkedAnswers.Add(question.PropertyName, a);
            }
            else
            {
                errors.Add(question.PropertyName);
            }
        }

        if (errors.Count > 0)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            Errors = errors;

            return;
        }

        Answers = checkedAnswers.ToDictionary(a => a.Key, a => a.Value.PropertyValue);

        if (answerGroupId is null)
        {
            return;
        }

        if (answer.Name is null)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            Errors = ["name"];

            return;
        }

        var answerGroup = await dbContext.AnswerGroups
            .Include(ag => ag.Submissions)
            .ThenInclude(s => s.Answers)
            .FirstOrDefaultAsync(ag => ag.Id == answerGroupId || ag.PublicId == answerGroupId);

        if (answerGroup is null)
        {
            Response.StatusCode = StatusCodes.Status404NotFound;

            return;
        }

        if (answerGroup.Submissions.FirstOrDefault(s => s.Name == answer.Name) is { } submission)
        {
            submission.Answers.Clear();
            submission.Answers.AddRange(checkedAnswers.Values);
        }
        else
        {
            submission = new() { Name = answer.Name, Answers = [.. checkedAnswers.Values] };
            dbContext.Add(submission);
            answerGroup.Submissions.Add(submission);
        }

        await dbContext.SaveChangesAsync();
    }
}

public class QuizAnswerDto
{
    [Required]
    [FromForm(Name = "answers")]
    public required Dictionary<string, string> Answers { get; init; }

    [FromForm(Name = "name")]
    public string? Name { get; init; }

    public string? this[string key] => Answers.TryGetValue(key, out var value) ? value : null;
}
