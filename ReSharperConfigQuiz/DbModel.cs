using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

using Microsoft.EntityFrameworkCore;

using EfDbContext = Microsoft.EntityFrameworkCore.DbContext;

namespace ReSharperConfigQuiz;

public class DbContext(DbContextOptions<DbContext> options) : EfDbContext(options)
{
    public required DbSet<Quiz> Quizzes { get; init; }

    public required DbSet<AnswerGroup> AnswerGroups { get; init; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Submission>().HasMany(s => s.Answers).WithMany();

        modelBuilder.Entity<AnswerGroup>().HasAlternateKey(ag => ag.PublicId);
    }
}

[PrimaryKey(nameof(Id))]
public class Quiz
{
    public Guid Id { get; } = Guid.NewGuid();

    public required string Name { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public required List<Question> Questions { get; init; }
}

[PrimaryKey(nameof(Id))]
public class Question
{
    public Guid Id { get; } = Guid.NewGuid();

    public required string Name { get; init; }

    public required string PropertyName { get; init; }

    public required List<Answer> Answers { get; init; }
}

[PrimaryKey(nameof(Id))]
public class Answer
{
    public Guid Id { get; } = Guid.NewGuid();

    public required string Name { get; init; }

    public required string PropertyValue { get; init; }

    public required Example Example { get; init; }
}

[PrimaryKey(nameof(Id))]
public class Example
{
    public Guid Id { get; } = Guid.NewGuid();

    public required string Code { get; init; }
}

[PrimaryKey(nameof(Id))]
public class AnswerGroup
{
    public Guid Id { get; } = Guid.NewGuid();

    public Guid PublicId { get; } = Guid.NewGuid();

    public required Quiz Quiz { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public required List<Submission> Submissions { get; init; }
}

[PrimaryKey(nameof(Id))]
public class Submission
{
    public Guid Id { get; } = Guid.NewGuid();

    public required string Name { get; init; }

    public required List<Answer> Answers { get; init; }
}
