namespace Work_Experience_Search.Utils;

public static class UnitTestExtensions
{
    public static bool IsInUnitTest()
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .Any(a => a.FullName?.StartsWith("WorkExperienceSearchTests") ?? false);
    }
}
