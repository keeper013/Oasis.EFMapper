namespace Oasis.EntityFramework.Mapper.InternalLogic;

using System.Data.Entity;

internal interface IRecursiveRegister
{
    void RecursivelyRegister(Type sourceType, Type targetType, IRecursiveRegisterContext context);

    void RegisterEntityListDefaultConstructorMethod(Type type);

    void RegisterEntityDefaultConstructorMethod(Type type);

    void RegisterForListItemProperty(Type sourceListItemPropertyType, Type targetListItemPropertyType);
}

internal interface IRecursiveRegisterContext
{
    void Push(Type sourceType, Type targetType);

    void Pop();

    void RegisterIf(IRecursiveRegister recursiveRegister, Type sourceType, Type targetType, bool hasRegistered);
}

internal sealed class RecursiveRegisterContext : IRecursiveRegisterContext
{
    private readonly Stack<(Type, Type)> _stack = new ();
    private readonly Dictionary<Type, ISet<Type>> _loopDependencyMapping;

    public RecursiveRegisterContext(Dictionary<Type, ISet<Type>> loopDependencyMapping)
    {
        _loopDependencyMapping = loopDependencyMapping;
    }

    public void Push(Type sourceType, Type targetType) => _stack.Push((sourceType, targetType));

    public void Pop() => _stack.Pop();

    public void DumpLoopDependency()
    {
        foreach (var mappingTuple in _stack)
        {
            if (_loopDependencyMapping.TryGetValue(mappingTuple.Item1, out var set))
            {
                set.Add(mappingTuple.Item2);
            }
            else
            {
                _loopDependencyMapping.Add(mappingTuple.Item1, new HashSet<Type> { mappingTuple.Item2 });
            }
        }
    }

    public void RegisterIf(IRecursiveRegister recursiveRegister, Type sourceType, Type targetType, bool hasRegistered)
    {
        if (hasRegistered)
        {
            if (_loopDependencyMapping.TryGetValue(sourceType, out var set) && set.Contains(targetType))
            {
                // this is a short cut to identify loop dependency if the mapping to the same source type to target type has been recorded
                DumpLoopDependency();
            }
        }
        else if (_stack.Any(i => i.Item1 == sourceType && i.Item2 == targetType))
        {
            DumpLoopDependency();
        }
        else if (!hasRegistered && !_stack.Any(i => i.Item1 == sourceType && i.Item2 == targetType))
        {
            recursiveRegister.RecursivelyRegister(sourceType, targetType, this);
        }
    }
}

internal interface ITargetByIdTracker
{
    TTarget? Find<TSource, TTarget>(TSource source)
        where TSource : class
        where TTarget : class;

    void Track<TSource, TTarget>(TSource source, TTarget target)
        where TSource : class
        where TTarget : class;

    void Clear();
}

internal interface ITargetByIdTrackerFactory
{
    ITargetByIdTracker Make();
}

internal sealed class TargetByIdTrackerFactory<TKeyType> : ITargetByIdTrackerFactory
    where TKeyType : notnull
{
    private readonly IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, TargetByIdTrackerMethods>> _index;
    private readonly IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, Delegate>> _scalarTypeConverters;

    public TargetByIdTrackerFactory(Dictionary<Type, Dictionary<Type, TargetByIdTrackerMetaDataSet>> index, IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, Delegate>> scalarTypeConverters, Type type)
    {
        _scalarTypeConverters = scalarTypeConverters;
        var dict = new Dictionary<Type, IReadOnlyDictionary<Type, TargetByIdTrackerMethods>>();
        foreach (var kvp in index)
        {
            var inner = new Dictionary<Type, TargetByIdTrackerMethods>();
            foreach (var mapping in kvp.Value)
            {
                inner.Add(mapping.Key, new TargetByIdTrackerMethods(
                    Delegate.CreateDelegate(mapping.Value.find.type, type.GetMethod(mapping.Value.find.name)!),
                    Delegate.CreateDelegate(mapping.Value.track.type, type.GetMethod(mapping.Value.track.name)!)));
            }

            dict.Add(kvp.Key, inner);
        }

        _index = dict;
    }

    public ITargetByIdTracker Make()
    {
        return new TargetByIdTracker(this);
    }

    private sealed class TargetByIdTracker : ITargetByIdTracker
    {
        private readonly Dictionary<Type, Dictionary<TKeyType, object>> _objects = new ();
        private readonly TargetByIdTrackerFactory<TKeyType> _factory;

        public TargetByIdTracker(TargetByIdTrackerFactory<TKeyType> factory)
        {
            _factory = factory;
        }

        public TTarget? Find<TSource, TTarget>(TSource source)
            where TSource : class
            where TTarget : class
        {
            var targetType = typeof(TTarget);
            return _objects.TryGetValue(targetType, out var dict)
                ? ((Utilities.EntityTrackerFindById<TSource, TTarget, TKeyType>)_factory._index.Find(typeof(TSource), targetType)!.find)(dict, source, _factory._scalarTypeConverters)
                : null;
        }

        public void Track<TSource, TTarget>(TSource source, TTarget target)
            where TSource : class
            where TTarget : class
        {
            var targetType = typeof(TTarget);
            if (!_objects.TryGetValue(targetType, out var dict))
            {
                dict = new Dictionary<TKeyType, object>();
                _objects.Add(targetType, dict);
            }

            ((Utilities.EntityTrackerTrackById<TSource, TTarget, TKeyType>)_factory._index.Find(typeof(TSource), targetType)!.track)(dict, source, target, _factory._scalarTypeConverters);
        }

        public void Clear()
        {
            foreach (var dict in _objects.Values)
            {
                dict.Clear();
            }
        }
    }
}

internal interface IRecursiveMapperContext
{
    bool TargetIsIdentifyableById<TSource, TTarget>(TSource source)
        where TSource : class
        where TTarget : class;

    Type GetIdentityType(Type type);
}

internal sealed class RecursiveMappingContext : IRecursiveMappingContext
{
    private readonly Dictionary<Type, Dictionary<int, object>> _targetByHashCode = new ();
    private readonly IReadOnlyDictionary<Type, ITargetByIdTracker> _targetByIdTrackers;
    private readonly IRecursiveMapperContext _mapperContext;

    // for mapping a single entity, it's ok to keep all tracker data here
    // for mapping list of entities, though this value will be overritten by every list item, but its value is gonna be correct for the whole list
    private Dictionary<int, object>? _hashCodeDictionary;

    public RecursiveMappingContext(IReadOnlyDictionary<Type, ITargetByIdTrackerFactory> targetByIdTrackerFactories, IRecursiveMapperContext mapperContext, bool forceTrack, DbContext? databaseContext = null)
    {
        var dict = new Dictionary<Type, ITargetByIdTracker>();
        foreach (var kvp in targetByIdTrackerFactories)
        {
            dict.Add(kvp.Key, kvp.Value.Make());
        }

        _targetByIdTrackers = dict;
        _mapperContext = mapperContext;
        ForceTrack = forceTrack;
        DatabaseContext = databaseContext;
        _hashCodeDictionary = null;
    }

    public bool ForceTrack { get; }

    public DbContext? DatabaseContext { get; set; }

    public void Clear()
    {
        foreach (var targetByIdTracker in _targetByIdTrackers.Values)
        {
            targetByIdTracker.Clear();
        }

        foreach (var dict in _targetByHashCode.Values)
        {
            dict.Clear();
        }
    }

    public TTarget? GetTracked<TSource, TTarget>(TSource source, out IEntityTracker<TTarget>? tracker)
        where TSource : class
        where TTarget : class
    {
        var sourceHashCode = source.GetHashCode();
        var targetType = typeof(TTarget);
        if (_targetByHashCode.TryGetValue(targetType, out _hashCodeDictionary) && _hashCodeDictionary.TryGetValue(sourceHashCode, out var value))
        {
            tracker = null;
            return (TTarget)value;
        }
        else
        {
            ITargetByIdTracker? targetByIdTracker = null;
            if (_mapperContext.TargetIsIdentifyableById<TSource, TTarget>(source))
            {
                targetByIdTracker = _targetByIdTrackers[_mapperContext.GetIdentityType(targetType)];
                var trackedTarget = targetByIdTracker.Find<TSource, TTarget>(source);
                if (trackedTarget != default)
                {
                    if (!_targetByHashCode.TryGetValue(targetType, out var inner1))
                    {
                        inner1 = new Dictionary<int, object> { { sourceHashCode, trackedTarget } };
                        _targetByHashCode.Add(targetType, inner1);
                    }
                    else
                    {
                        inner1.Add(sourceHashCode, trackedTarget);
                    }

                    tracker = null;
                    return trackedTarget;
                }
            }

            tracker = new EntityTracker<TSource, TTarget>(source, this, sourceHashCode, targetType, targetByIdTracker);
            return null;
        }
    }

    public IEntityTracker<TTarget> GetTracker<TSource, TTarget>(TSource source)
        where TSource : class
        where TTarget : class
    {
        var sourceHashCode = source.GetHashCode();
        var targetType = typeof(TTarget);
        ITargetByIdTracker? targetByIdTracker = null;
        if (_mapperContext.TargetIsIdentifyableById<TSource, TTarget>(source))
        {
            targetByIdTracker = _targetByIdTrackers[_mapperContext.GetIdentityType(targetType)];
        }

        return new EntityTracker<TSource, TTarget>(source, this, sourceHashCode, targetType, targetByIdTracker);
    }

    private readonly struct EntityTracker<TSource, TTarget> : IEntityTracker<TTarget>
        where TSource : class
        where TTarget : class
    {
        private readonly TSource _source;
        private readonly RecursiveMappingContext _context;
        private readonly int _sourceHashCode;
        private readonly Type _targetType;
        private readonly ITargetByIdTracker? _targetByIdTracker;

        public EntityTracker(TSource source, RecursiveMappingContext context, int sourceHashCode, Type targetType, ITargetByIdTracker? targetByIdTracker)
        {
            _source = source;
            _context = context;
            _sourceHashCode = sourceHashCode;
            _targetType = targetType;
            _targetByIdTracker = targetByIdTracker;
        }

        public void Track(TTarget target)
        {
            // add to hash code dictionary
            if (_context._hashCodeDictionary == default)
            {
                _context._hashCodeDictionary = new Dictionary<int, object>();
                _context._targetByHashCode.Add(_targetType, _context._hashCodeDictionary);
            }

            _context._hashCodeDictionary.Add(_sourceHashCode, target);

            // add to identity dictionary
            _targetByIdTracker?.Track(_source, target);
        }
    }
}