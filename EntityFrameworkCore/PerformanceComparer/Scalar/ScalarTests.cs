namespace PerformanceComparer.Scalar;

using AutoMapper;
using Oasis.EntityFrameworkCore.Mapper;

internal static class ScalarTests
{
    internal const int Rounds = 10000000;

    public static void EfMapper_Scalar(IDictionary<string, TimeSpan> dict)
    {
        var mapper = new MapperBuilderFactory().MakeMapperBuilder(nameof(ScalarTests)).Register<ScalarSource, ScalarTarget>().Build();
        var source = ScalarUtilities.BuildDefaultScalarSource();
        using var timer = new StopWatchTimer(dict, nameof(EfMapper_Scalar));
        for (var i = 0; i < Rounds; i++)
        {
            _ = mapper.Map<ScalarSource, ScalarTarget>(source);
        }
    }

    public static void EfMapper_Scalar_Session(IDictionary<string, TimeSpan> dict)
    {
        var session = new MapperBuilderFactory().MakeMapperBuilder(nameof(ScalarTests)).Register<ScalarSource, ScalarTarget>().Build().CreateMappingSession();
        var source = ScalarUtilities.BuildDefaultScalarSource();
        using var timer = new StopWatchTimer(dict, nameof(EfMapper_Scalar_Session));
        for (var i = 0; i < Rounds; i++)
        {
            _ = session.Map<ScalarSource, ScalarTarget>(source);
        }
    }

    public static void AutoMapper_Scalar(IDictionary<string, TimeSpan> dict)
    {
        var config = new MapperConfiguration(cfg => cfg.CreateMap<ScalarSource, ScalarTarget>());
        var mapper = new Mapper(config);
        var source = ScalarUtilities.BuildDefaultScalarSource();
        using var timer = new StopWatchTimer(dict, nameof(AutoMapper_Scalar));
        for (var i = 1; i < Rounds; i++)
        {
            _ = mapper.Map<ScalarSource, ScalarTarget>(source);
        }
    }
}
