namespace Agent.Core;

public class InstructionsLoader
{
    private readonly Dictionary<string, string> _cache = [];

    public string Load(string fileName)
    {
        if (!_cache.TryGetValue(fileName, out var instructions))
        {
            instructions = File.ReadAllText(fileName);
            _cache[fileName] = instructions;
        }
        return instructions;
    }
}
