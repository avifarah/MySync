namespace MySync
{
	public struct DirectoryNumbers
	{
		public DirectoryNumbers(int nestingLevel, int directoryCount, int totalDirectoryCount)
		{
			NestingLevel = nestingLevel;
			DirectoryCount = directoryCount;
			TotalDirectoryCount = totalDirectoryCount;
		}

		public int NestingLevel { get; }

		public int DirectoryCount { get; }

		public int TotalDirectoryCount { get; }

		public DirectoryNumbers NextNumbers(int directoryCount, int totalDirectoryCount) => new(NestingLevel + 1, directoryCount, totalDirectoryCount);
	}
}