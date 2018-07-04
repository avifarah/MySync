using Microsoft.VisualStudio.TestTools.UnitTesting;
using MySync;

namespace UnitTestMySync
{
	class ProgArgsDirectoryExistsFake : ProgramArgs
	{
		public ProgArgsDirectoryExistsFake(string primary, string secondary, string skipTil) : base(primary, secondary, skipTil) { }

		protected override bool IsDirectoryExist(string dir) => true;
	}

	[TestClass]
	public class UnitTestProgramArgs
	{
		//[TestMethod]
		//public void IsSkipTilLegitNetworkDriveSuccess()
		//{
		//	// Arrange
		//	var primary = @"\\part1\part2\part3";
		//	var skipTil = @"\\part1\part2\part3\part4\part5";
		//	var progArgs = new ProgArgsDirectoryExistsFake(primary, null, skipTil);

		//	// Act
		//	bool rc = progArgs.IsSkipTilLegit();

		//	// Assert
		//	Assert.IsTrue(rc);
		//}

		//[TestMethod]
		//public void IsSkipTilLegitLocalDriveSuccess()
		//{
		//	// Arrange
		//	var primary = @"c:\part1\part2\part3";
		//	var skipTil = @"c:\part1\part2\part3\part4\part5";
		//	var progArgs = new ProgArgsDirectoryExistsFake(primary, null, skipTil);

		//	// Act
		//	bool rc = progArgs.IsSkipTilLegit();

		//	// Assert
		//	Assert.IsTrue(rc);
		//}

		//[TestMethod]
		//public void IsSkipTilLegitLocalDriveSkipIsNullSuccess()
		//{
		//	// Arrange
		//	var primary = @"\\part1\part2\part3";
		//	var skipTil = string.Empty;
		//	var progArgs = new ProgramArgs(primary, null, skipTil);

		//	// Act
		//	bool rc = progArgs.IsSkipTilLegit();

		//	// Assert
		//	Assert.IsTrue(rc);
		//}

		[TestMethod]
		public void IsSkipTilLegitNetworkDriveSkipIsNullSuccess()
		{
			// Arrange
			var primary = @"c:\part1\part2\part3";
			var skipTil = string.Empty;
			var progArgs = new ProgramArgs(primary, null, skipTil);

			// Act
			bool rc = progArgs.IsSkipTilLegit();

			// Assert
			Assert.IsTrue(rc);
		}

		//[TestMethod]
		//public void IsSkipDirCopySkipGtDirTrue()
		//{
		//	// Arrange
		//	var primary = @"c:\part1\part2";
		//	var skipTil = @"c:\part1\part2\part3\part4";
		//	var progArgs = new ProgArgsDirectoryExistsFake(primary, null, skipTil);

		//	// Act
		//	bool rc = progArgs.IsSkipDirCopy(@"c:\part1\part2\different");

		//	// Assert
		//	Assert.IsTrue(rc);
		//}

		[TestMethod]
		public void IsSkipDirCopySkipGtDirFalse()
		{
			// Arrange
			var primary = @"c:\part1\part2\";
			var skipTil = @"c:\part1\part2\part3\part4";
			var progArgs = new ProgArgsDirectoryExistsFake(primary, null, skipTil);

			// Act
			bool rc = progArgs.IsSkipDirCopy(@"c:\part1\part2\");

			// Assert
			Assert.IsFalse(rc);
		}
	}
}
