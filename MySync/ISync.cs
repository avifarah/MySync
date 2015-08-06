using System.IO;

namespace MySync
{
	public interface ISync
	{
		void SyncDirectory(DirectoryInfo pDir, DirectoryInfo sDir);
	}
}