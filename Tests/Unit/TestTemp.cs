namespace Examples;

using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
public class TestTemp
{
    [TestCase]
    public void TestTempCounter()
    {
        var main = new Main();
        int result = main.TempCounter(5);
        AssertThat(result).IsEqual(6);
    }
}
