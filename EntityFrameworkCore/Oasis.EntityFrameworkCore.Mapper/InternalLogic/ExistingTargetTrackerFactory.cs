namespace Oasis.EntityFrameworkCore.Mapper.InternalLogic;

public sealed class ExistingTargetTracker<TKeyType> : IExistingTargetTracker
{
    private readonly ISet<TKeyType> _existingTargetHashCodeSet = new HashSet<TKeyType>();
    private readonly Delegate _startTrackingMetaData;

    public ExistingTargetTracker(Delegate startTrackingMetaData)
    {
        _startTrackingMetaData = startTrackingMetaData;
    }

    public bool StartTracking<TTarget>(TTarget target)
        where TTarget : class
    {
        return ((Utilities.StartTrackingNewTarget<TTarget, TKeyType>)_startTrackingMetaData)(_existingTargetHashCodeSet, target);
    }
}

internal sealed class ExistingTargetTrackerFactory
{
    private readonly Dictionary<Type, ExistingTargetTrackerSet> _dict = new ();
    private readonly IReadOnlySet<Type> _targetsToBeTracked;

    public ExistingTargetTrackerFactory(IReadOnlyDictionary<Type, ExistingTargetTrackerMetaDataSet> dict, IReadOnlySet<Type> targetsToBeTracked, Type type)
    {
        _targetsToBeTracked = targetsToBeTracked;
        foreach (var kvp in dict)
        {
            _dict.Add(
                kvp.Key,
                new ExistingTargetTrackerSet(
                    Delegate.CreateDelegate(kvp.Value.buildExistingTargetTracker.type, type.GetMethod(kvp.Value.buildExistingTargetTracker.name)!),
                    Delegate.CreateDelegate(kvp.Value.startTrackingTarget.type, type.GetMethod(kvp.Value.startTrackingTarget.name)!)));
        }
    }

    public IExistingTargetTracker Make()
    {
        return new ExistingTargetTracker(_dict);
    }

    public IExistingTargetTracker? Make<TTarget>()
        where TTarget : class
    {
        return _targetsToBeTracked.Contains(typeof(TTarget)) ? new ExistingTargetTracker(_dict) : null;
    }

    internal sealed class ExistingTargetTracker : IExistingTargetTracker
    {
        private readonly IReadOnlyDictionary<Type, ExistingTargetTrackerSet> _dict;
        private readonly Dictionary<Type, IExistingTargetTracker> _cache = new ();

        public ExistingTargetTracker(IReadOnlyDictionary<Type, ExistingTargetTrackerSet> dict)
        {
            _dict = dict;
        }

        public bool StartTracking<TTarget>(TTarget target)
            where TTarget : class
        {
            var type = typeof(TTarget);
            if (!_cache.TryGetValue(type, out var result))
            {
                var set = _dict[type];
                result = ((Utilities.BuildExistingTargetTracker)set.buildExistingTargetTracker)(set.startTrackingTarget);
                _cache.Add(type, result);
            }

            return result.StartTracking(target);
        }
    }
}
