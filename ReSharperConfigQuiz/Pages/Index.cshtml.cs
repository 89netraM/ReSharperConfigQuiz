using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ReSharperConfigQuiz.Pages;

public class IndexModel(DbContext dbContext) : PageModel
{
    public IReadOnlyCollection<Quiz> Quizzes { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Quizzes = await dbContext.Quizzes.AsNoTracking().ToListAsync();
    }
}
