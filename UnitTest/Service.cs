using System;
using System.Linq;
using Xunit;

namespace UnitTest
{
    public class Service
    {
        [Fact]
        public void TestMethod1()
        {
            bool value = true;

            Assert.Equal(true, value);
        }
    }
}