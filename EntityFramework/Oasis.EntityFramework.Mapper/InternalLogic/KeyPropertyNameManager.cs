namespace Oasis.EntityFramework.Mapper.InternalLogic;

internal interface IKeyPropertyNameManager
{
    string? GetIdentityPropertyName(Type type);

    string? GetTimeStampPropertyName(Type type);

    bool IsKeyPropertyName(string name, Type type);
}

internal class KeyPropertyNameManager : IKeyPropertyNameManager
{
    private readonly KeyPropertyNameConfiguration _defaultConfiguration;
    private readonly IReadOnlyDictionary<Type, TypeConfiguration> _typesUsingCustomConfiguration;

    public KeyPropertyNameManager(KeyPropertyNameConfiguration defaultConfiguration, Dictionary<Type, TypeConfiguration> dictionary)
    {
        _defaultConfiguration = defaultConfiguration;
        _typesUsingCustomConfiguration = dictionary;
    }

    public string? GetIdentityPropertyName(Type type)
    {
        return _typesUsingCustomConfiguration.TryGetValue(type, out var configuration) ? configuration.identityPropertyName : _defaultConfiguration.identityPropertyName;
    }

    public string? GetTimeStampPropertyName(Type type)
    {
        return _typesUsingCustomConfiguration.TryGetValue(type, out var configuration) ? configuration.timestampPropertyName : _defaultConfiguration.timestampPropertyName;
    }

    public bool IsKeyPropertyName(string name, Type type)
    {
        if (_typesUsingCustomConfiguration.TryGetValue(type, out var configuration))
        {
            return (!string.IsNullOrEmpty(configuration.identityPropertyName) && string.Equals(configuration.identityPropertyName, name))
                || (!string.IsNullOrEmpty(configuration.timestampPropertyName) && string.Equals(configuration.timestampPropertyName, name));
        }

        return (!string.IsNullOrEmpty(_defaultConfiguration.identityPropertyName) && string.Equals(_defaultConfiguration.identityPropertyName, name))
                || (!string.IsNullOrEmpty(_defaultConfiguration.timestampPropertyName) && string.Equals(_defaultConfiguration.timestampPropertyName, name));
    }
}

internal record struct KeyPropertyNameConfiguration(string? identityPropertyName = null, string? timestampPropertyName = null);
