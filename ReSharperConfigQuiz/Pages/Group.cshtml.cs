using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ReSharperConfigQuiz.Pages;

public class GroupModel(DbContext dbContext) : PageModel
{
    public AnswerGroup? AnswerGroup { get; private set; }

    public string? CommonConfig { get; private set; }

    public async Task OnGetAsync([FromRoute] Guid groupId)
    {
        var group = await dbContext.AnswerGroups
            .AsNoTracking()
            .Include(ag => ag.Submissions)
            .ThenInclude(s => s.Answers)
            .Include(ag => ag.Quiz)
            .ThenInclude(q => q.Questions)
            .ThenInclude(q => q.Answers)
            .FirstOrDefaultAsync(ag => ag.Id == groupId || ag.PublicId == groupId);
        if (group is null)
        {
            Response.StatusCode = StatusCodes.Status404NotFound;

            return;
        }

        AnswerGroup = group;

        var answers = group.Submissions.SelectMany(s => s.Answers).ToLookup(a => a.Id);

        var commonConfig = new StringBuilder();
        foreach (var question in group.Quiz.Questions)
        {
            var propertyValue = question.Answers.MaxBy(a => answers[a.Id].Count())?.PropertyValue;
            if (propertyValue is not null)
            {
                commonConfig.AppendLine($"{question.PropertyName} = {propertyValue}");
            }
        }

        CommonConfig = commonConfig.ToString();
    }
}
