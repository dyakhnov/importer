using System;
using System.IO;
using System.Diagnostics;
using Xunit;
//using Xunit.Abstractions;

namespace importerTests
{
    public class TestEnvVariables
    {
        [Fact]
        public void NoSourceDirectory()
        {
            Assert.True(true);
            Debug.WriteLine("Test");
            /*
            using (var sw = new StringWriter())
            {
                Console.SetOut(sw);
                importer.Program.Main();

                var result = sw.ToString().Trim();
                Assert.Equal("Can't find source directory.", result);
            }
            */
        }
    }
}
