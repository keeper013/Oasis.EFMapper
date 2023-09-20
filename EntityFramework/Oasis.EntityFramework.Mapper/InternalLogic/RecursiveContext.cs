namespace Oasis.EntityFramework.Mapper.InternalLogic;

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
    private readonly IScalarTypeConverter _scalarTypeConverter;

    public TargetByIdTrackerFactory(Dictionary<Type, Dictionary<Type, TargetByIdTrackerMetaDataSet>> index, IScalarTypeConverter scalarTypeConverter, Type type)
    {
        _scalarTypeConverter = scalarTypeConverter;
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
                ? ((Utilities.EntityTrackerFindById<TSource, TTarget, TKeyType>)_factory._index.Find(typeof(TSource), targetType)!.find)(dict, source, _factory._scalarTypeConverter)
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

            ((Utilities.EntityTrackerTrackById<TSource, TTarget, TKeyType>)_factory._index.Find(typeof(TSource), targetType)!.track)(dict, source, target, _factory._scalarTypeConverter);
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

internal sealed class RecursiveMappingContextFactory
{
    private readonly IReadOnlyDictionary<Type, Type> _targetIdentityTypeMapping;
    private readonly IReadOnlyDictionary<Type, ITargetByIdTrackerFactory> _targetByIdTrackerFactories;
    private readonly IReadOnlyDictionary<Type, ISet<Type>>? _loopDependencyMapping;
    private readonly EntityHandler _entityHandler;

    public RecursiveMappingContextFactory(
        EntityHandler entityHandler,
        IReadOnlyDictionary<Type, Type> targetIdentityTypeMapping,
        IReadOnlyDictionary<Type, ITargetByIdTrackerFactory> targetByIdTrackerFactories,
        IReadOnlyDictionary<Type, ISet<Type>>? loopDependencyMapping)
    {
        _entityHandler = entityHandler;
        _targetIdentityTypeMapping = targetIdentityTypeMapping;
        _targetByIdTrackerFactories = targetByIdTrackerFactories;
        _loopDependencyMapping = loopDependencyMapping;
    }

    public IRecursiveMappingContext Make(bool forceTrack)
    {
        var dict = new Dictionary<Type, ITargetByIdTracker>();
        foreach (var kvp in _targetByIdTrackerFactories)
        {
            dict.Add(kvp.Key, kvp.Value.Make());
        }

        if (forceTrack)
        {
            return new RecursiveMappingContext(dict, this, true);
        }
        else if (_loopDependencyMapping != null && _loopDependencyMapping.Any())
        {
            return new RecursiveMappingContext(dict, this, _loopDependencyMapping);
        }
        else
        {
            return new RecursiveMappingContext(dict, this, false);
        }
    }

    private sealed class RecursiveMappingContext : IRecursiveMappingContext
    {
        private readonly Dictionary<Type, Dictionary<int, object>> _targetByHashCode = new ();
        private readonly IReadOnlyDictionary<Type, ITargetByIdTracker> _targetByIdTrackers;
        private readonly RecursiveMappingContextFactory _factory;
        private readonly IReadOnlyDictionary<Type, ISet<Type>>? _loopDependencyMapping;
        private readonly bool? _track;

        // for mapping a single entity, it's ok to keep all tracker data here
        // for mapping list of entities, though this value will be overritten by every list item, but its value is gonna be correct for the whole list
        private Dictionary<int, object>? _hashCodeDictionary;

        public RecursiveMappingContext(IReadOnlyDictionary<Type, ITargetByIdTracker> targetByIdTrackers, RecursiveMappingContextFactory factory, bool track)
        {
            _targetByIdTrackers = targetByIdTrackers;
            _factory = factory;
            _hashCodeDictionary = null;
            _loopDependencyMapping = null;
            _track = track;
        }

        public RecursiveMappingContext(
            IReadOnlyDictionary<Type, ITargetByIdTracker> targetByIdTrackers,
            RecursiveMappingContextFactory factory,
            IReadOnlyDictionary<Type, ISet<Type>> loopDependencyMapping)
        {
            _targetByIdTrackers = targetByIdTrackers;
            _factory = factory;
            _hashCodeDictionary = null;
            _loopDependencyMapping = loopDependencyMapping;
            _track = null;
        }

        public TTarget TrackIfNecessaryAndMap<TSource, TTarget>(
            TSource source,
            TTarget? target,
            Func<TTarget> makeTarget,
            Action<TSource, TTarget, IRecursiveMappingContext> doMapping,
            bool tryGetTracked)
            where TSource : class
            where TTarget : class
        {
            if ((_track.HasValue && !_track.Value) || _loopDependencyMapping == null || !_loopDependencyMapping.Contains(typeof(TSource), typeof(TTarget)))
            {
                if (target == default)
                {
                    target = makeTarget();
                }

                doMapping(source, target, this);
                return target;
            }
            else
            {
                IEntityTracker<TTarget>? tracker;
                if (tryGetTracked)
                {
                    var trackedTarget = GetTracked(source, out tracker);
                    if (trackedTarget != null)
                    {
                        return trackedTarget;
                    }
                }
                else
                {
                    tracker = GetTracker<TSource, TTarget>(source);
                }

                if (target == default)
                {
                    target = makeTarget();
                }

                tracker!.Track(target);
                doMapping(source, target, this);

                if (!tryGetTracked)
                {
                    Clear();
                }

                return target;
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
                var targetIsIdentifyableById = _factory._entityHandler.HasId<TSource>() && _factory._entityHandler.HasId<TTarget>() && !_factory._entityHandler.IdIsEmpty(source);
                ITargetByIdTracker? targetByIdTracker = null;
                if (targetIsIdentifyableById)
                {
                    targetByIdTracker = _targetByIdTrackers[_factory._targetIdentityTypeMapping[targetType]];
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
            if (_factory._entityHandler.HasId<TSource>() && _factory._entityHandler.HasId<TTarget>() && !_factory._entityHandler.IdIsEmpty(source))
            {
                targetByIdTracker = _targetByIdTrackers[_factory._targetIdentityTypeMapping[targetType]];
            }

            return new EntityTracker<TSource, TTarget>(source, this, sourceHashCode, targetType, targetByIdTracker);
        }

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
}