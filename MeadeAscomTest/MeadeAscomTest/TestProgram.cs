using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace MeadeAscomTest
{
    [TestFixture]
    class TestProgram
    {
        [Test]
        public void Test()
        {
            double RA = Program.getRA(12, 34, 56);
            Assert.AreEqual(RA,1);
        }
    }
}
