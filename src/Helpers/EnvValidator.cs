using System;
using System.IO;

namespace importer.Helpers
{
	public class EnvValidatior
	{
		public bool IsValidTargetDirectory(string targetName, string targetDirectory)
		{
			if (targetDirectory == null)
			{
				Console.WriteLine("Can't find {0} directory.", targetName);
				return false;
			}
			if (!Directory.Exists(targetDirectory))
			{
				Console.WriteLine("Invalid {0} directory, {1}.", targetName, targetDirectory);
				return false;
			}
			return true;
		}
	}
}
