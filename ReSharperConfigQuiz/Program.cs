using System;

using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using ReSharperConfigQuiz;

using DbContext = ReSharperConfigQuiz.DbContext;

var builder = WebApplication.CreateBuilder(args);

var quizDbConnectionString = builder.Configuration.GetConnectionString(name: "QuizDatabase")
    ?? throw new ArgumentException(message: "Connection string required");
builder.Services.AddDbContext<DbContext>(o => o.UseNpgsql(quizDbConnectionString));

builder.Services.AddAuthorization();
builder.Services.AddAuthentication(defaultScheme: "Bearer").AddJwtBearer();

builder.Services.AddRazorPages();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    using var db = scope.ServiceProvider.GetRequiredService<DbContext>();
    db.Database.Migrate();
}

app.UseAuthorization();

var apiGroup = app.MapGroup(prefix: "api");
apiGroup.MapQuizRoutes(prefix: "quizzes");
apiGroup.MapAnswerGroupRoutes(prefix: "answer-groups");

app.MapCreateGroupPage();

app.UseStaticFiles();
app.UseRouting();
app.MapRazorPages();

app.Run();
