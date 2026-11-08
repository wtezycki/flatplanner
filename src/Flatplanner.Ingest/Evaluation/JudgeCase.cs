namespace Flatplanner.Ingest.Evaluation;

public sealed record JudgeCase(string Id, string Message, JudgeExpectation[] Expectations);
