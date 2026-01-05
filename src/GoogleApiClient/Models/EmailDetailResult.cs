namespace GoogleApiClient.Models;

public record EmailDetailResult(
    string Id,
    string From,
    string To,
    string Subject,
    DateTime Date,
    string Body);
