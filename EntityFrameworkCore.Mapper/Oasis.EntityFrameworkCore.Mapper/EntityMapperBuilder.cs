namespace Oasis.EntityFrameworkCore.Mapper;

using System.Reflection;
using System.Reflection.Emit;

internal sealed class EntityMapperBuilder : IEntityMapperBuilder
{
    private const char MapScalarPropertiesMethod = 's';
    private const char MapListPropertiesMethod = 'l';
    private static readonly MethodInfo MapListProperty = typeof(IListPropertyMapper).GetMethod("MapListProperty", Utilities.PublicInstance)!;

    private readonly TypeBuilder _typeBuilder;
    private readonly IDictionary<Type, IDictionary<Type, MapperMetaDataSet>> _mapper = new Dictionary<Type, IDictionary<Type, MapperMetaDataSet>>();
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

    void IEntityMapperBuilder.Register<TSource, TTarget>()
    {
        var sourceType = typeof(TSource);
        var targetType = typeof(TTarget);
        if (!_mapper.TryGetValue(sourceType, out var innerDictionary))
        {
            innerDictionary = new Dictionary<Type, MapperMetaDataSet>();
            _mapper[sourceType] = innerDictionary;
        }

        if (!innerDictionary.ContainsKey(targetType))
        {
            innerDictionary[targetType] = new MapperMetaDataSet(
                BuildUpScalarPropertiesMapperMethod<TSource, TTarget>(),
                BuildUpListPropertiesMapperMethod<TSource, TTarget>());
        }
    }

    private static string BuildMethodName(char type, Type sourceType, Type targetType)
    {
        return $"_{type}_{sourceType.FullName!.Replace(".", "_")}__To__{targetType.FullName!.Replace(".", "_")}";
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

    private MapperMetaData BuildUpListPropertiesMapperMethod<TSource, TTarget>()
        where TSource : class, IEntityBase
        where TTarget : class, IEntityBase
    {
        var sourceType = typeof(TSource);
        var targetType = typeof(TTarget);

        var method = InitializeDynamicMethod(sourceType, targetType, MapListPropertiesMethod, new[] { sourceType, targetType, typeof(IListPropertyMapper) });
        FillListPropertiesMapper(method.GetILGenerator(), sourceType, targetType);

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
        var sourceProperties = sourceType.GetProperties(Utilities.PublicInstance).Where(p => p.IsScalarType(true, false));
        var targetProperties = targetType.GetProperties(Utilities.PublicInstance).Where(p => p.IsScalarType(true, true)).ToDictionary(p => p.Name, p => p);

        foreach (var sourceProperty in sourceProperties)
        {
            if (targetProperties.TryGetValue(sourceProperty.Name, out var targetProperty) && targetProperty.PropertyType == sourceProperty.PropertyType)
            {
                generator.Emit(OpCodes.Ldarg_1);
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Callvirt, sourceProperty.GetMethod!);
                generator.Emit(OpCodes.Callvirt, targetProperty.SetMethod!);
            }
        }

        generator.Emit(OpCodes.Ret);
    }

    private void FillListPropertiesMapper(ILGenerator generator, Type sourceType, Type targetType)
    {
        var sourceProperties = sourceType.GetProperties(Utilities.PublicInstance).Where(p => p.IsListOfNavigationType(true, false));
        var targetProperties = targetType.GetProperties(Utilities.PublicInstance).Where(p => p.IsListOfNavigationType(true, true)).ToDictionary(p => p.Name, p => p);

        foreach (var sourceProperty in sourceProperties)
        {
            if (targetProperties.TryGetValue(sourceProperty.Name, out var targetProperty) && ListTypeMatch(sourceProperty.PropertyType, targetProperty.PropertyType))
            {
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
                generator.Emit(OpCodes.Callvirt, GetMapListPropertyMethod(sourceProperty.PropertyType, targetProperty.PropertyType));
            }
        }

        generator.Emit(OpCodes.Ret);
    }

    private MethodInfo GetMapListPropertyMethod(Type sourceType, Type targetType)
    {
        var sourceItemType = sourceType.GenericTypeArguments[0];
        var targetItemType = targetType.GenericTypeArguments[0];

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

    private bool ListTypeMatch(Type sourceListType, Type targetListType)
    {
        return _mapper.TryGetValue(sourceListType.GenericTypeArguments[0], out var innerDictionary) && innerDictionary.ContainsKey(targetListType.GenericTypeArguments[0]);
    }

    private record struct MapperMetaData(Type type, string name);

    private record struct MapperMetaDataSet(MapperMetaData scalarPropertiesMapper, MapperMetaData listPropertiesMapper);
}
