namespace MySync
{
	using System.IO;

	public interface IProgramArgs
	{
		string Primary { get; }

		string Secondary { get; }

		string SkipTil { get; }

		bool IsPrimaryLegit();

		bool IsSecondaryLegit();

		/// <summary>
		/// SkipTill may be:
		///		>	Empty
		///		>	Must exits as a directory
		///		>	It must be a superset of the primary directory
		/// </summary>
		/// <returns></returns>
		bool IsSkipTilLegit();

		bool IsSkipFileCopy(DirectoryInfo dir);

		bool IsSkipFileCopy(string dir);

		bool IsSkipDirCopy(DirectoryInfo dir);

		bool IsSkipDirCopy(string dir);
	}
}