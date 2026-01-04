namespace GoogleTools.Models;

public record EmailListResult(
    int Count,
    string? Query,
    List<EmailSummary> Emails);

public record EmailSummary(
    string Id,
    string From,
    string Subject,
    DateTime Date,
    string Preview);
