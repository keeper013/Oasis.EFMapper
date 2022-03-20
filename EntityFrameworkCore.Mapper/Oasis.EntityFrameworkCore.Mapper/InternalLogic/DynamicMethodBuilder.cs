namespace Oasis.EntityFrameworkCore.Mapper.InternalLogic;

using System.Reflection;
using System.Reflection.Emit;

internal interface IDynamicMethodBuilder
{
    MethodBuilder Build(string methodName, Type[] parameterTypes, Type returnType);
}

internal sealed class DynamicMethodBuilder : IDynamicMethodBuilder
{
    private readonly TypeBuilder _typeBuilder;

    public DynamicMethodBuilder(TypeBuilder typeBuilder)
    {
        _typeBuilder = typeBuilder;
    }

    public MethodBuilder Build(string methodName, Type[] parameterTypes, Type returnType)
    {
        var methodBuilder = _typeBuilder.DefineMethod(methodName, MethodAttributes.Public | MethodAttributes.Static);
        methodBuilder.SetParameters(parameterTypes);
        methodBuilder.SetReturnType(returnType);

        return methodBuilder;
    }

    public Type Build()
    {
        return _typeBuilder.CreateType()!;
    }
}
