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
    [TestMethod]
    public void LulTest()
    {
        byte[] a = [2, 34, 4, 56, 45, 254];
        var mStream = new MemoryStream(a);
        mStream.Dispose();
        Console.WriteLine(a[2]); 
        Assert.IsTrue(a is not null); 
    }
}