using Xunit;

namespace UnitTesting.AppLogic.Services
{
    public static class EnvironmentVariablesCollection
    {
        public const string Name = "EnvironmentVariables";
    }

    [CollectionDefinition(EnvironmentVariablesCollection.Name, DisableParallelization = true)]
    public sealed class EnvironmentVariablesCollectionDefinition
    {
    }

    internal sealed class EnvironmentVariableScope : IDisposable
    {
        private readonly Dictionary<string, string?> _originalValues = new();

        public EnvironmentVariableScope(params (string Name, string? Value)[] variables)
        {
            foreach (var (name, value) in variables)
            {
                _originalValues[name] = Environment.GetEnvironmentVariable(name);
                Environment.SetEnvironmentVariable(name, value);
            }
        }

        public void Dispose()
        {
            foreach (var (name, value) in _originalValues)
            {
                Environment.SetEnvironmentVariable(name, value);
            }
        }
    }
}
