namespace Hotmail.Models;

/// <summary>
/// Represents an email message from Hotmail/Outlook.com.
/// </summary>
public record HotmailMessage(
    string Id,
    string Subject,
    string From,
    string To,
    string Snippet,
    DateTime ReceivedDateTime,
    bool IsRead);

/// <summary>
/// Response from GetHotmail tool.
/// </summary>
public record GetHotmailResponse(
    bool Success,
    List<HotmailMessage> Messages,
    string? Error = null);

/// <summary>
/// Response from SearchHotmail tool.
/// </summary>
public record SearchHotmailResponse(
    bool Success,
    List<HotmailMessage> Messages,
    string? Error = null);

/// <summary>
/// Microsoft Graph message list response.
/// </summary>
public record GraphMessageListResponse(
    GraphMessage[]? Value,
    string? OdataNextLink);

/// <summary>
/// Microsoft Graph message response.
/// </summary>
public record GraphMessage(
    string Id,
    string Subject,
    GraphEmailAddress From,
    GraphEmailAddress[] ToRecipients,
    string BodyPreview,
    DateTime ReceivedDateTime,
    bool IsRead);

/// <summary>
/// Microsoft Graph email address.
/// </summary>
public record GraphEmailAddress(
    GraphEmailAddressValue EmailAddress);

/// <summary>
/// Microsoft Graph email address value.
/// </summary>
public record GraphEmailAddressValue(
    string Name,
    string Address);
