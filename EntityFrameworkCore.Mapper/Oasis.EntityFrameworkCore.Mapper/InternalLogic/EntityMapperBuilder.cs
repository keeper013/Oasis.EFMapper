namespace Oasis.EntityFrameworkCore.Mapper.InternalLogic;

using Oasis.EntityFrameworkCore.Mapper.Exceptions;
using System.Reflection;
using System.Reflection.Emit;

internal sealed class EntityMapperBuilder : IEntityMapperBuilder
{
    private const char MapScalarPropertiesMethod = 's';
    private const char MapListPropertiesMethod = 'l';
    private static readonly MethodInfo MapListProperty = typeof(IListPropertyMapper).GetMethod("MapListProperty", Utilities.PublicInstance)!;
    private static readonly MethodInfo RecursivelyRegisterMethod = typeof(EntityMapperBuilder).GetMethod("RecursivelyRegister", Utilities.NonPublicInstance)!;

    private readonly TypeBuilder _typeBuilder;
    private readonly IDictionary<Type, IDictionary<Type, MapperMetaDataSet>> _mapper = new Dictionary<Type, IDictionary<Type, MapperMetaDataSet>>();
    private readonly IDictionary<Type, IDictionary<Type, MethodInfo>> _scalarMapper = new Dictionary<Type, IDictionary<Type, MethodInfo>>();
    private readonly ISet<Type> _convertableToScalarTypes = new HashSet<Type>();
    private readonly IDictionary<Type, IDictionary<Type, MethodInfo>> _listPropertyMapper = new Dictionary<Type, IDictionary<Type, MethodInfo>>();

    public EntityMapperBuilder(string assemblyName)
    {
        var name = new AssemblyName($"{assemblyName}.Oasis.EntityFrameworkCore.Mapper.Generated");
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(name, AssemblyBuilderAccess.Run);
        var module = assemblyBuilder.DefineDynamicModule($"{name.Name}.dll");
        _typeBuilder = module.DefineType("Mapper", TypeAttributes.Public);
    }

    public IEntityMapper Build()
    {
        var type = _typeBuilder.CreateType();
        var mapper = new Dictionary<Type, IReadOnlyDictionary<Type, MapperSet>>();
        foreach (var pair in _mapper)
        {
            var innerMapper = new Dictionary<Type, MapperSet>();
            foreach (var innerPair in pair.Value)
            {
                var mapperMetaDataSet = innerPair.Value;
                var mapperSet = new MapperSet(
                    Delegate.CreateDelegate(mapperMetaDataSet.scalarPropertiesMapper.type, type!.GetMethod(mapperMetaDataSet.scalarPropertiesMapper.name)!),
                    Delegate.CreateDelegate(mapperMetaDataSet.listPropertiesMapper.type, type!.GetMethod(mapperMetaDataSet.listPropertiesMapper.name)!));
                innerMapper.Add(innerPair.Key, mapperSet);
            }

            mapper.Add(pair.Key, innerMapper);
        }

        return new EntityMapper(mapper);
    }

    IEntityMapperBuilder IEntityMapperBuilder.Register<TSource, TTarget>()
    {
        var stack = new Stack<(Type, Type)>();
        return RecursivelyRegister<TSource, TTarget>(stack);
    }

    public IEntityMapperBuilder WithScalarMapper<TSource, TTarget>(Func<TSource, TTarget> func)
    {
        var sourceType = typeof(TSource);
        var targetType = typeof(TTarget);

        if (!func.Method.IsStatic)
        {
            throw new NonStaticScalarMapperException(sourceType, targetType);
        }

        var sourceIsNotScalarType = !Utilities.IsScalarType(sourceType);
        var targetIsNotScalarType = !Utilities.IsScalarType(targetType);
        if (sourceIsNotScalarType && targetIsNotScalarType)
        {
            throw new ScalarTypeMissingException(sourceType, targetType);
        }

        lock (_scalarMapper)
        {
            if (!_scalarMapper.TryGetValue(sourceType, out var innerDictionary))
            {
                innerDictionary = new Dictionary<Type, MethodInfo>();
                _scalarMapper[sourceType] = innerDictionary;
            }

            if (!innerDictionary.ContainsKey(targetType))
            {
                innerDictionary.Add(targetType, func.Method);
                if (sourceIsNotScalarType)
                {
                    _convertableToScalarTypes.Add(sourceType);
                }
                else if (targetIsNotScalarType)
                {
                    _convertableToScalarTypes.Add(targetType);
                }
            }
            else
            {
                throw new ScalarMapperExistsException(sourceType, targetType);
            }
        }

        return this;
    }

    private static string BuildMethodName(char type, Type sourceType, Type targetType)
    {
        return $"_{type}_{sourceType.FullName!.Replace(".", "_")}__To__{targetType.FullName!.Replace(".", "_")}";
    }

    private IEntityMapperBuilder RecursivelyRegister<TSource, TTarget>(Stack<(Type, Type)> stack)
        where TSource : class, IEntityBase
        where TTarget : class, IEntityBase
    {
        var sourceType = typeof(TSource);
        var targetType = typeof(TTarget);
        if (!stack.Any(i => i.Item1 == sourceType && i.Item2 == targetType))
        {
            stack.Push(new (sourceType, targetType));
            lock (_mapper)
            {
                if (!_mapper.TryGetValue(sourceType, out var innerDictionary))
                {
                    innerDictionary = new Dictionary<Type, MapperMetaDataSet>();
                    _mapper[sourceType] = innerDictionary;
                }

                if (!innerDictionary.ContainsKey(targetType))
                {
                    innerDictionary[targetType] = new MapperMetaDataSet(
                        BuildUpScalarPropertiesMapperMethod<TSource, TTarget>(),
                        BuildUpListPropertiesMapperMethod<TSource, TTarget>(stack));
                }
            }

            stack.Pop();
        }

        return this;
    }

    private MapperMetaData BuildUpScalarPropertiesMapperMethod<TSource, TTarget>()
        where TSource : class, IEntityBase
        where TTarget : class, IEntityBase
    {
        var sourceType = typeof(TSource);
        var targetType = typeof(TTarget);

        var method = InitializeDynamicMethod(sourceType, targetType, MapScalarPropertiesMethod, new[] { sourceType, targetType });
        FillScalarPropertiesMapper(method.GetILGenerator(), sourceType, targetType);

        return new MapperMetaData(typeof(Utilities.MapScalarProperties<TSource, TTarget>), method.Name);
    }

    private MapperMetaData BuildUpListPropertiesMapperMethod<TSource, TTarget>(Stack<(Type, Type)> stack)
        where TSource : class, IEntityBase
        where TTarget : class, IEntityBase
    {
        var sourceType = typeof(TSource);
        var targetType = typeof(TTarget);

        var method = InitializeDynamicMethod(sourceType, targetType, MapListPropertiesMethod, new[] { sourceType, targetType, typeof(IListPropertyMapper) });
        FillListPropertiesMapper(method.GetILGenerator(), sourceType, targetType, stack);

        return new MapperMetaData(typeof(Utilities.MapListProperties<TSource, TTarget>), method.Name);
    }

    private MethodBuilder InitializeDynamicMethod(Type sourceType, Type targetType, char type, Type[] parameterTypes)
    {
        var methodName = BuildMethodName(type, sourceType, targetType);
        var methodBuilder = _typeBuilder.DefineMethod(methodName, MethodAttributes.Public | MethodAttributes.Static);
        methodBuilder.SetParameters(parameterTypes);
        methodBuilder.SetReturnType(typeof(void));

        return methodBuilder;
    }

    private void FillScalarPropertiesMapper(ILGenerator generator, Type sourceType, Type targetType)
    {
        var sourceProperties = sourceType.GetProperties(Utilities.PublicInstance).Where(p => p.IsScalarProperty(_convertableToScalarTypes, true, false));
        var targetProperties = targetType.GetProperties(Utilities.PublicInstance).Where(p => p.IsScalarProperty(_convertableToScalarTypes, true, true)).ToDictionary(p => p.Name, p => p);

        foreach (var sourceProperty in sourceProperties)
        {
            var sourcePropertyType = sourceProperty.PropertyType;
            if (targetProperties.TryGetValue(sourceProperty.Name, out var targetProperty) && ScalarTypeMatch(sourcePropertyType, targetProperty.PropertyType))
            {
                var targetPropertyType = targetProperty.PropertyType;
                generator.Emit(OpCodes.Ldarg_1);
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Callvirt, sourceProperty.GetMethod!);
                if (sourcePropertyType != targetPropertyType)
                {
                    var converter = _scalarMapper[sourcePropertyType][targetPropertyType];
                    generator.Emit(OpCodes.Call, converter);
                }

                generator.Emit(OpCodes.Callvirt, targetProperty.SetMethod!);
            }
        }

        generator.Emit(OpCodes.Ret);
    }

    private void FillListPropertiesMapper(ILGenerator generator, Type sourceType, Type targetType, Stack<(Type, Type)> stack)
    {
        var sourceProperties = sourceType.GetProperties(Utilities.PublicInstance).Where(p => p.IsListOfNavigationProperty(true, false));
        var targetProperties = targetType.GetProperties(Utilities.PublicInstance).Where(p => p.IsListOfNavigationProperty(true, true)).ToDictionary(p => p.Name, p => p);

        foreach (var sourceProperty in sourceProperties)
        {
            if (targetProperties.TryGetValue(sourceProperty.Name, out var targetProperty))
            {
                var sourceItemType = sourceProperty.PropertyType.GenericTypeArguments[0];
                var targetItemType = targetProperty.PropertyType.GenericTypeArguments[0];

                // cascading mapper creation: if list item mapper doesn't exist, create it
                if (!stack.Any(i => i.Item1 == sourceItemType && i.Item2 == targetItemType) && !TypeMapperExists(sourceItemType, targetItemType))
                {
                    var recursivelyRegisterMethod = RecursivelyRegisterMethod.MakeGenericMethod(sourceItemType, targetItemType);
                    recursivelyRegisterMethod.Invoke(this, new object[] { stack });
                }

                // now it's made sure that mapper between list items exists, emit the list property mapping code
                generator.Emit(OpCodes.Ldarg_1);
                generator.Emit(OpCodes.Callvirt, targetProperty.GetMethod!);
                var jumpLabel = generator.DefineLabel();
                generator.Emit(OpCodes.Brtrue_S, jumpLabel);
                generator.Emit(OpCodes.Ldarg_1);
                generator.Emit(OpCodes.Newobj, typeof(List<>).MakeGenericType(targetProperty.PropertyType.GetGenericArguments()).GetConstructor(Array.Empty<Type>())!);
                generator.Emit(OpCodes.Callvirt, targetProperty.SetMethod!);
                generator.MarkLabel(jumpLabel);
                generator.Emit(OpCodes.Ldarg_2);
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Callvirt, sourceProperty.GetMethod!);
                generator.Emit(OpCodes.Ldarg_1);
                generator.Emit(OpCodes.Callvirt, targetProperty.GetMethod!);
                generator.Emit(OpCodes.Callvirt, GetMapListPropertyMethod(sourceItemType, targetItemType));
            }
        }

        generator.Emit(OpCodes.Ret);
    }

    private MethodInfo GetMapListPropertyMethod(Type sourceItemType, Type targetItemType)
    {
        if (!_listPropertyMapper.TryGetValue(sourceItemType, out var innerDictionary))
        {
            innerDictionary = new Dictionary<Type, MethodInfo>();
            _listPropertyMapper[sourceItemType] = innerDictionary;
        }

        if (!innerDictionary.TryGetValue(targetItemType, out var method))
        {
            method = MapListProperty.MakeGenericMethod(sourceItemType, targetItemType);
            innerDictionary[targetItemType] = method;
        }

        return method;
    }

    private bool TypeMapperExists(Type sourceType, Type targetType)
    {
        return _mapper.TryGetValue(sourceType, out var innerDictionary) && innerDictionary.ContainsKey(targetType);
    }

    private bool ScalarTypeMatch(Type sourceType, Type targetType)
    {
        return sourceType == targetType || (_scalarMapper.TryGetValue(sourceType, out var innerDictionary) && innerDictionary.ContainsKey(targetType));
    }

    private record struct MapperMetaData(Type type, string name);

    private record struct MapperMetaDataSet(MapperMetaData scalarPropertiesMapper, MapperMetaData listPropertiesMapper);
}
