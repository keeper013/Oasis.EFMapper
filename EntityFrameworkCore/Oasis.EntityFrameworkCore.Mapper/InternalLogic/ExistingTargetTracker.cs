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

internal sealed class ExistingTargetTracker
{
    private readonly Dictionary<Type, ExistingTargetTrackerSet> _dict = new ();

    public ExistingTargetTracker(IReadOnlyDictionary<Type, ExistingTargetTrackerMetaDataSet> dict, Type type)
    {
        foreach (var kvp in dict)
        {
            _dict.Add(
                kvp.Key,
                new ExistingTargetTrackerSet(
                    Delegate.CreateDelegate(kvp.Value.buildExistingTargetTracker.type, type.GetMethod(kvp.Value.buildExistingTargetTracker.name)!),
                    Delegate.CreateDelegate(kvp.Value.startTrackingTarget.type, type.GetMethod(kvp.Value.startTrackingTarget.name)!)));
        }
    }

    public IExistingTargetTracker GetTargetTracker(Type type)
    {
        var set = _dict[type];
        return ((Utilities.BuildExistingTargetTracker)set.buildExistingTargetTracker)(set.startTrackingTarget);
    }
}
