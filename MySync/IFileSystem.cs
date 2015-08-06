using System.IO;

namespace MySync
{
	public interface IFileSystem
	{
		void CopyFile(string srcP, string dstP);
		void CopyDirectory(DirectoryInfo src, DirectoryInfo dst);
		void CopyDirectory(string src, string dst);
    }
}