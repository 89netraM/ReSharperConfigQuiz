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

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    await using var db = scope.ServiceProvider.GetRequiredService<DbContext>();
    await db.Database.MigrateAsync();
}

app.MapQuizRoutes(prefix: "api/quizzes");

app.Run();
