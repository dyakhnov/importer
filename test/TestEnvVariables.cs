using System;
using System.IO;
using Xunit;

namespace importerTests
{
    public class TestEnvVariables
    {
        [Fact]
        public void NoSourceDirectory()
        {
            StringWriter sw = new StringWriter();
            Console.SetOut(sw);

            var app = new importer.Importer();
            app.Run();

            Assert.Contains("Can't find source directory.", sw.ToString());
        }
        [Fact]
        public void NoDestinationDirectory()
        {
            StringWriter sw = new StringWriter();
            Console.SetOut(sw);

            Environment.SetEnvironmentVariable("SRC", "../../../Resources/in");

            var app = new importer.Importer();
            app.Run();

            Assert.Contains("Can't find destination directory.", sw.ToString());
        }
    }
}
