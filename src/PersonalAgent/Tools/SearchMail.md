# SearchMail Tool

Searches emails using Gmail search syntax.

## Usage
Use this tool when the user wants to find specific emails based on criteria like sender, subject, read status, or other filters.

## Parameters
- **query**: A Gmail search query string

## Gmail Search Syntax Examples
- `from:user@example.com` - Emails from a specific sender
- `to:user@example.com` - Emails sent to a specific recipient
- `subject:meeting` - Emails with "meeting" in the subject
- `is:unread` - Unread emails only
- `is:starred` - Starred emails
- `has:attachment` - Emails with attachments
- `after:2024/01/01` - Emails after a date
- `before:2024/12/31` - Emails before a date
- `label:important` - Emails with a specific label
- Combine queries: `from:boss@company.com is:unread`

## Returns
A list of matching email summaries (up to 20) containing:
- **Id**: The Gmail message ID (use with GetMailContents)
- **From**: Sender's email address
- **Subject**: Email subject line
- **Date**: When received
- **Preview**: Short content snippet

## Example
User: "Find unread emails from john@example.com"
→ Call SearchMail with query="from:john@example.com is:unread"
