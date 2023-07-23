namespace Oasis.EntityFrameworkCore.Mapper.InternalLogic;

using Oasis.EntityFrameworkCore.Mapper.Exceptions;

internal class KeyPropertyNameManager
{
    private readonly KeyPropertyNameConfiguration _defaultConfiguration;
    private readonly Dictionary<Type, KeyPropertyConfiguration> _typesUsingCustomConfiguration = new ();

    public KeyPropertyNameManager(KeyPropertyNameConfiguration defaultConfiguration)
    {
        _defaultConfiguration = defaultConfiguration;
    }

    public bool ContainsConfiguration(Type type)
    {
        return _typesUsingCustomConfiguration.ContainsKey(type);
    }

    public void Add(Type type, KeyPropertyConfiguration config)
    {
        if (_typesUsingCustomConfiguration.ContainsKey(type))
        {
            throw new TypeConfiguratedException(type);
        }

        _typesUsingCustomConfiguration.Add(type, config);
    }

    public void Clear()
    {
        _typesUsingCustomConfiguration.Clear();
    }

    public string? GetIdentityPropertyName(Type type)
    {
        return _typesUsingCustomConfiguration.TryGetValue(type, out var configuration) ? configuration.identityPropertyName : _defaultConfiguration.identityPropertyName;
    }

    public string? GetConcurrencyTokenPropertyName(Type type)
    {
        return _typesUsingCustomConfiguration.TryGetValue(type, out var configuration) ? configuration.concurrencyTokenPropertyName : _defaultConfiguration.concurrencyTokenPropertyName;
    }

    public bool IsKeyPropertyName(string name, Type type)
    {
        if (_typesUsingCustomConfiguration.TryGetValue(type, out var configuration))
        {
            return (!string.IsNullOrEmpty(configuration.identityPropertyName) && string.Equals(configuration.identityPropertyName, name))
                || (!string.IsNullOrEmpty(configuration.concurrencyTokenPropertyName) && string.Equals(configuration.concurrencyTokenPropertyName, name));
        }

        return (!string.IsNullOrEmpty(_defaultConfiguration.identityPropertyName) && string.Equals(_defaultConfiguration.identityPropertyName, name))
                || (!string.IsNullOrEmpty(_defaultConfiguration.concurrencyTokenPropertyName) && string.Equals(_defaultConfiguration.concurrencyTokenPropertyName, name));
    }
}

internal record struct KeyPropertyNameConfiguration(string? identityPropertyName = null, string? concurrencyTokenPropertyName = null);
