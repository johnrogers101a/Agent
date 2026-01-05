# GetMail Tool

Gets the latest 20 emails from the user's Gmail inbox.

## Usage
Use this tool when the user wants to see their recent emails or check their inbox.

## Parameters
None - this tool fetches the 20 most recent emails automatically.

## Returns
A list of email summaries containing:
- **Id**: The Gmail message ID (use this with GetMailContents to read the full email)
- **From**: The sender's email address
- **Subject**: The email subject line
- **Date**: When the email was received
- **Preview**: A short snippet of the email content

## Example
User: "Check my email" or "What's in my inbox?"
→ Call GetMail (no parameters needed)

## Notes
- Returns only summaries, not full email content
- To read a full email, note the Id and use GetMailContents
