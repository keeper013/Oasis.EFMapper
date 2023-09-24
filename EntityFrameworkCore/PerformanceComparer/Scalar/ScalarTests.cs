namespace PerformanceComparer.Scalar;

using AutoMapper;
using Oasis.EntityFrameworkCore.Mapper;

internal static class ScalarTests
{
    internal const int Rounds = 10000000;

    public static void EfMapper_IdenticalItem_Scalar(IDictionary<string, TimeSpan> dict)
    {
        var mapper = new MapperBuilderFactory().MakeMapperBuilder().Register<ScalarSource, ScalarTarget>().Build().MakeToMemoryMapper();
        var source = ScalarUtilities.BuildDefaultScalarSource();
        using var timer = new StopWatchTimer(dict, nameof(EfMapper_IdenticalItem_Scalar));
        for (var i = 0; i < Rounds; i++)
        {
            _ = mapper.Map<ScalarSource, ScalarTarget>(source);
        }
    }

    public static void EfMapper_SeparateItem_Scalar(IDictionary<string, TimeSpan> dict)
    {
        var mapper = new MapperBuilderFactory().MakeMapperBuilder().Register<ScalarSource, ScalarTarget>().Build().MakeToMemoryMapper();
        var list = new List<ScalarSource>();
        for (var i = 0; i < Rounds; i++)
        {
            list.Add(ScalarUtilities.BuildDefaultScalarSource());
        }

        using var timer = new StopWatchTimer(dict, nameof(EfMapper_SeparateItem_Scalar));
        foreach (var item in list)
        {
            _ = mapper.Map<ScalarSource, ScalarTarget>(item);
        }
    }

    public static void EfMapper_IdenticalItem_Scalar_Session(IDictionary<string, TimeSpan> dict)
    {
        var session = new MapperBuilderFactory().MakeMapperBuilder().Register<ScalarSource, ScalarTarget>().Build().MakeToMemorySession();
        
        var source = ScalarUtilities.BuildDefaultScalarSource();
        using var timer = new StopWatchTimer(dict, nameof(EfMapper_IdenticalItem_Scalar_Session));
        for (var i = 0; i < Rounds; i++)
        {
            _ = session.Map<ScalarSource, ScalarTarget>(source);
        }
    }

    public static void EfMapper_SeparateItem_Scalar_Session(IDictionary<string, TimeSpan> dict)
    {
        var session = new MapperBuilderFactory().MakeMapperBuilder().Register<ScalarSource, ScalarTarget>().Build().MakeToMemorySession();
        var list = new List<ScalarSource>();
        for (var i = 0; i < Rounds; i++)
        {
            list.Add(ScalarUtilities.BuildDefaultScalarSource());
        }

        using var timer = new StopWatchTimer(dict, nameof(EfMapper_SeparateItem_Scalar_Session));
        foreach (var item in list)
        {
            _ = session.Map<ScalarSource, ScalarTarget>(item);
        }
    }

    public static void AutoMapper_IdenticalItem_Scalar(IDictionary<string, TimeSpan> dict)
    {
        var config = new MapperConfiguration(cfg => cfg.CreateMap<ScalarSource, ScalarTarget>());
        var mapper = new Mapper(config);
        var source = ScalarUtilities.BuildDefaultScalarSource();
        using var timer = new StopWatchTimer(dict, nameof(AutoMapper_IdenticalItem_Scalar));
        for (var i = 1; i < Rounds; i++)
        {
            _ = mapper.Map<ScalarSource, ScalarTarget>(source);
        }
    }

    public static void AutoMapper_SeparateItem_Scalar(IDictionary<string, TimeSpan> dict)
    {
        var config = new MapperConfiguration(cfg => cfg.CreateMap<ScalarSource, ScalarTarget>());
        var mapper = new Mapper(config);
        var list = new List<ScalarSource>();
        for (var i = 0; i < Rounds; i++)
        {
            list.Add(ScalarUtilities.BuildDefaultScalarSource());
        }

        using var timer = new StopWatchTimer(dict, nameof(AutoMapper_SeparateItem_Scalar));
        foreach (var item in list)
        {
            _ = mapper.Map<ScalarSource, ScalarTarget>(item);
        }
    }
}
