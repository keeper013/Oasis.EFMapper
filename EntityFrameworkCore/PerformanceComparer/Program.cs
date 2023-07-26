using PerformanceComparer;
using PerformanceComparer.Scalar;

var dict = new Dictionary<string, TimeSpan>();
ScalarTests.EfMapper_Scalar(dict);
ScalarTests.EfMapper_Scalar_Session(dict);
ScalarTests.AutoMapper_Scalar(dict);

Utilities.Print(dict, ScalarTests.Rounds);
