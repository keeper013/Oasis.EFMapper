using PerformanceComparer;
using PerformanceComparer.Scalar;

var dict = new Dictionary<string, TimeSpan>();
ScalarTests.EfMapper_IdenticalItem_Scalar(dict);
//ScalarTests.EfMapper_IdenticalItem_Scalar_Session(dict);
ScalarTests.AutoMapper_IdenticalItem_Scalar(dict);
//ScalarTests.EfMapper_SeparateItem_Scalar(dict);
//ScalarTests.EfMapper_SeparateItem_Scalar_Session(dict);
//ScalarTests.AutoMapper_SeparateItem_Scalar(dict);

Utilities.Print(dict, ScalarTests.Rounds);
