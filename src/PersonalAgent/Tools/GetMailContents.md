# GetMailContents Tool

Gets the full contents of a specific email by its ID.

## Usage
Use this tool after calling GetMail or SearchMail to read the complete content of a specific email. You must have the message ID from a previous email listing.

## Parameters
- **messageId**: The Gmail message ID (obtained from GetMail or SearchMail results)

## Returns
The complete email details:
- **Id**: The Gmail message ID
- **From**: The sender's email address
- **To**: The recipient's email address
- **Subject**: The email subject line
- **Date**: When the email was received
- **Body**: The full email body content

## Workflow
1. First, call GetMail or SearchMail to get a list of emails with their IDs
2. Identify the email the user wants to read
3. Call GetMailContents with that email's Id

## Example
User: "Read the first email"
1. (Previously called GetMail, got email with Id="abc123")
2. → Call GetMailContents with messageId="abc123"

## Notes
- Always get the message ID from GetMail or SearchMail first
- Do not guess or make up message IDs
