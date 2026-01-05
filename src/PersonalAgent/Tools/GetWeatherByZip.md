# GetWeatherByZip Tool

Gets the current weather conditions for a location specified by US zip code.

## Usage
Use this tool when the user provides a US zip code to get weather information.

## Parameters
- **zipCode**: The US zip code to get weather for (e.g., "90210", "10001")

## Returns
Weather information including:
- Current temperature and "feels like" temperature
- Weather description (sunny, cloudy, rainy, etc.)
- Humidity percentage
- Wind speed and direction
- UV index

## Example
User: "What's the weather in 90210?"
→ Call GetWeatherByZip with zipCode="90210"
