using System.Diagnostics;

namespace InstrunetTestProject;

[TestClass]
public sealed class Test1
{
    [TestMethod]
    public void TestMethod1()
    {
        dynamic text = new HttpClient();
        Console.WriteLine(text.GetType());
    }
}