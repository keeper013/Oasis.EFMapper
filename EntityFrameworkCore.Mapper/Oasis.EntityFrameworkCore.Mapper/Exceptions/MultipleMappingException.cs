namespace Oasis.EntityFrameworkCore.Mapper.Exceptions;

public sealed class MultipleMappingException : Exception
{
    private readonly Type _existingType;
    private readonly Type _targetType;

    public MultipleMappingException(Type existingType, Type targetType)
    {
        _existingType = existingType;
        _targetType = targetType;
    }

    public override string Message => $"An instance has been used to initialize an instance of {_existingType}, it shouldn't be used to initialize other instance types (specifically {_targetType}).";
}
