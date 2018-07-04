using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using log4net;


namespace MySync.CodeContract
{
	public static class Contract
	{
		private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		public static void Requires(Func<bool> condition, string errMsg, [CallerMemberName] string callerName = null, [CallerLineNumber] int lineNum = 0)
		{
			if (condition()) return;

			var msg = $"caller name: {callerName}, in line: {lineNum}.  Error: {errMsg}";
			Log.Error(msg);
			throw new Exception(msg);
		}
	}
}
