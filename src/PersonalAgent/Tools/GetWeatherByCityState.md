# GetWeatherByCityState Tool

Gets the current weather conditions for a location specified by city and state.

## Usage
Use this tool when the user provides a city and state (not a zip code) to get weather information.

## Parameters
- **city**: The city name (e.g., "Los Angeles", "New York")
- **state**: The state name or abbreviation (e.g., "California", "CA", "New York", "NY")

## Returns
Weather information including:
- Current temperature and "feels like" temperature
- Weather description (sunny, cloudy, rainy, etc.)
- Humidity percentage
- Wind speed and direction
- UV index

## Example
User: "What's the weather in Los Angeles, California?"
→ Call GetWeatherByCityState with city="Los Angeles", state="California"
