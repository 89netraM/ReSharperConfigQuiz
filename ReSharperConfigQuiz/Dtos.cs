using System;
using System.Collections.Generic;

namespace ReSharperConfigQuiz;

public class SubmissionDto
{
    public required string Name { get; init; }

    public required IReadOnlyCollection<Guid> Answers { get; init; }
}
