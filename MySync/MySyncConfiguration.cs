using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using MySync.CodeContract;
using MySync.Utils;

namespace MySync
{
	public class MySyncConfiguration
	{
		private static readonly Lazy<MySyncConfiguration> _inst = new(() => new MySyncConfiguration(), true);
		public static readonly MySyncConfiguration Inst = _inst.Value;

		private MySyncConfiguration() => _appSetting = ConfigurationManager.AppSettings.AllKeys.ToDictionary(k => k, k => ConfigurationManager.AppSettings[k]);

		private readonly Dictionary<string, string> _appSetting;
		private readonly Dictionary<string, string> _cacheString = new();
		private readonly Dictionary<string, int> _cacheInt = new();
		private readonly Dictionary<string, TimeSpan> _cacheTimeSpan = new();
		private readonly Dictionary<string, IEnumerable<string>> _cacheIenumerableOfString = new();

		public string GetPrimaryDir
		{
			get
			{
				const string key = "Primary";
				const string def = "C:\\";

				if (_cacheString.ContainsKey(key)) return _cacheString[key];

				var primary = GetValue(key);
				if (string.IsNullOrWhiteSpace(primary)) primary = def;

				primary = primary.Trim();
				if (Directory.Exists(primary))
				{
					_cacheString[key] = primary;
					return _cacheString[key];
				}

				throw new Exception($"Primary directory: \"{primary}\" does not exist.  Check configuration file, key=\"Primary\"");
			}
		}

		public string GetSecondaryDir
		{
			get
			{
				const string key = "Secondary";
				const string def = "M:\\";

				if (_cacheString.ContainsKey(key)) return _cacheString[key];

				var secondary = GetValue(key);
				if (string.IsNullOrWhiteSpace(secondary)) secondary = def;

				_cacheString[key] = secondary.Trim();
				return _cacheString[key];
			}
		}

		public string SkippingTillDir
		{
			get
			{
				const string key = "SkippingTill";
				var def = string.Empty;

				if (_cacheString.ContainsKey(key)) return _cacheString[key];

				var skippingTill = GetValue(key);
				if (string.IsNullOrWhiteSpace(skippingTill)) skippingTill = def;

				_cacheString[key] = skippingTill.Trim();
				if (string.IsNullOrEmpty(_cacheString[key]))
				{
					_cacheString[key] = string.Empty;
					return _cacheString[key];
				}

				if (Directory.Exists(_cacheString[key])) return _cacheString[key];

				throw new Exception($"SkippingTill directory: \"{skippingTill}\" does not exist");
			}
		}

		private bool? _reportOnlySave = null;

		public bool IsReportOnly
		{
			get
			{
				if (_reportOnlySave.HasValue) return _reportOnlySave.Value;

				const string key = "ReportOnly";
				const bool def = false;
				var reportOnly = GetValue(key);
				if (string.IsNullOrWhiteSpace(reportOnly))
				{
					_reportOnlySave = def;
					return _reportOnlySave.Value;
				}

				reportOnly = reportOnly.Trim();
				if (reportOnly.IsTrue())
				{
					_reportOnlySave = true;
					return _reportOnlySave.Value;
				}

				if (reportOnly.IsFalse())
				{
					_reportOnlySave = false;
					return _reportOnlySave.Value;
				}

				throw new Exception($"Config value for \"{key}\" is neither YES nor NO");
			}
		}

		public TimeSpan FileDateTolerance
		{
			get
			{
				const string key = "Millisecond Tolerance For FileDate Comparison";
				var def = TimeSpan.FromMilliseconds(1000.0);
				var sTol = GetValue(key);
				if (sTol == null) return def;

				var rc = double.TryParse(sTol, NumberStyles.Any, CultureInfo.CurrentCulture, out var tolerance);
				if (!rc) return def;

				return TimeSpan.FromMilliseconds(tolerance);
			}
		}

		public int ConsoleBufferWidth
		{
			get
			{
				const string key = "Console.BufferWidth";
				const int def = 650;

				if (_cacheInt.ContainsKey(key)) return _cacheInt[key];

				var bufferWidth = GetValue(key);
				if (string.IsNullOrWhiteSpace(bufferWidth))
				{
					_cacheInt[key] = def;
					return _cacheInt[key];
				}

				const NumberStyles ns = NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite;
				var rc = int.TryParse(bufferWidth, ns, CultureInfo.CurrentCulture, out int width);
				if (!rc) throw new Exception($"Configuration value for {key} must be an integer");
				if (width <= 0) throw new Exception($"Configuration value for {key} must be a positive integer");

				_cacheInt[key] = width;
				return _cacheInt[key];
			}
		}

		public int ConsoleBufferHeight
		{
			get
			{
				const string key = "Console.BufferHeight";
				const int def = 900;

				if (_cacheInt.ContainsKey(key)) return _cacheInt[key];

				var bufferHight = GetValue(key);
				if (string.IsNullOrWhiteSpace(bufferHight))
				{
					_cacheInt[key] = def;
					return _cacheInt[key];
				}

				const NumberStyles ns = NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite;
				var rc = int.TryParse(bufferHight, ns, CultureInfo.CurrentCulture, out int height);
				if (!rc) throw new Exception($"Configuration value for {key} must be an integer");
				if (height <= 0) throw new Exception($"Configuration value for {key} must be a positive integer");

				_cacheInt[key] = height;
				return _cacheInt[key];
			}
		}

		private const string PatMaxTimeWaitTimeSpan = @"^\s*(?<TimeSpan>(?<hh>\d+)\s*,\s*(?<mm>\d+)\s*,\s*(?<ss>\d+))\s*$";
		private const string PatMaxTimeWaitSeconds = @"^\s*(?<Seconds>(?<ss>\d+)(?<ms>\.\d+)?)\s*$";
		private static readonly Regex ReMaxTimeWait = new Regex($"({PatMaxTimeWaitTimeSpan})|({PatMaxTimeWaitSeconds})", RegexOptions.Singleline);

		public TimeSpan MaxTimeToWaitForFileCopy
		{
			get
			{
				const string key = "Time to wait for file copy to succeed";
				TimeSpan def = new TimeSpan(0, 10, 0);

				if (_cacheTimeSpan.ContainsKey(key)) return _cacheTimeSpan[key];

				var maxWait = GetValue(key);
				if (string.IsNullOrWhiteSpace(maxWait))
				{
					_cacheTimeSpan[key] = def;
					return _cacheTimeSpan[key];
				}

				var m = ReMaxTimeWait.Match(maxWait);
				if (m.Success)
				{
					if (!string.IsNullOrEmpty(m.Groups["TimeSpan"].Value))
					{
						var hh = int.Parse(m.Groups["hh"].Value);
						var mm = int.Parse(m.Groups["mm"].Value);
						var ss = int.Parse(m.Groups["ss"].Value);
						_cacheTimeSpan[key] = new TimeSpan(hh, mm, ss);
						return _cacheTimeSpan[key];
					}

					{
						var ss = int.Parse(m.Groups["ss"].Value);
						var ms = int.Parse(m.Groups["ms"].Value);
						_cacheTimeSpan[key] = new TimeSpan(0, 0, 0, ss, ms);
						return _cacheTimeSpan[key];
					}
				}

				throw new Exception($"Configuration value for \"{key}\" can either be of form: \"\\d+,\\d+,\\d+\" or of form: \"\\d+\\.\\d+\"");
			}
		}

		private static readonly Regex ReMaxTimeWaitTimeSpan = new Regex(PatMaxTimeWaitTimeSpan, RegexOptions.Singleline);

		//public TimeSpan MaxTimeToWaitForDirectoryCopy
		//{
		//	get
		//	{
		//		const string key = "Time to wait for directory copy to succeed";

		//		if (_cacheTimeSpan.ContainsKey(key)) return _cacheTimeSpan[key];

		//		TimeSpan def = new TimeSpan(1, 0, 0);
		//		var maxWait = GetValue(key);
		//		if (string.IsNullOrWhiteSpace(maxWait))
		//		{
		//			_cacheTimeSpan[key] = def;
		//			return _cacheTimeSpan[key];
		//		}

		//		var m = ReMaxTimeWaitTimeSpan.Match(maxWait);
		//		if (m.Success)
		//		{
		//			var hh = int.Parse(m.Groups["hh"].Value);
		//			var mm = int.Parse(m.Groups["mm"].Value);
		//			var ss = int.Parse(m.Groups["ss"].Value);
		//			_cacheTimeSpan[key] = new TimeSpan(hh, mm, ss);
		//			return _cacheTimeSpan[key];
		//		}

		//		throw new Exception($"Configuration value for \"{key}\" needs to be of form: \"\\d+,\\d+,\\d+\"");
		//	}
		//}

		public TimeSpan MaxWaitForExitProcToComplete
		{
			get
			{
				const string key = "Max Wait For ExitProc to Complete";

				if (_cacheTimeSpan.ContainsKey(key)) return _cacheTimeSpan[key];

				TimeSpan def = new TimeSpan(0, 0, 15);
				var maxWait = GetValue(key);
				if (string.IsNullOrWhiteSpace(maxWait))
				{
					_cacheTimeSpan[key] = def;
					return _cacheTimeSpan[key];
				}

				var m = ReMaxTimeWaitTimeSpan.Match(maxWait);
				if (m.Success)
				{
					var hh = int.Parse(m.Groups["hh"].Value);
					var mm = int.Parse(m.Groups["mm"].Value);
					var ss = int.Parse(m.Groups["ss"].Value);
					_cacheTimeSpan[key] = new TimeSpan(hh, mm, ss);
					return _cacheTimeSpan[key];
				}

				throw new Exception($"Configuration value for \"{key}\" can either be of form: \"\\d+,\\d+,\\d+\" or of form: \"\\d+\\.\\d+\"");
			}
		}

		//public int RetryCountOnCopyFailure
		//{
		//	get
		//	{
		//		const string key = "Retry count on copy failure";
		//		const int def = 10;

		//		if (_cacheInt.ContainsKey(key)) return _cacheInt[key];

		//		var sRetryCount = GetValue(key);
		//		if (string.IsNullOrWhiteSpace(sRetryCount))
		//		{
		//			_cacheInt[key] = def;
		//			return _cacheInt[key];
		//		}

		//		const NumberStyles ns = NumberStyles.AllowLeadingSign | NumberStyles.AllowTrailingWhite;
		//		var rc = int.TryParse(sRetryCount, ns, CultureInfo.CurrentCulture, out int retryCount);
		//		if (!rc) throw new Exception($"Configuration value for \"{key}\" must be a positive integer");
		//		if (retryCount == 0)
		//		{
		//			_cacheInt[key] = def;
		//			return _cacheInt[key];
		//		}

		//		_cacheInt[key] = retryCount;
		//		return _cacheInt[key];
		//	}
		//}

		public string LoggerLocator
		{
			get
			{
				const string key = "logger";
				const string def = "MySync.LogService.LogAdapter";

				if (_cacheString.ContainsKey(key)) return _cacheString[key];

				var locator = GetValue(key);
				if (string.IsNullOrWhiteSpace(locator))
				{
					_cacheString[key] = def;
					return _cacheString[key];
				}

				_cacheString[key] = locator.Trim();
				return _cacheString[key];
			}
		}

		private const string ExcludePat = @"(""(?<file1>([^""]|"""")+)""(;|$))|((?<file2>[^;]+)(;|$))";
		private static readonly Regex ExcludeRe = new Regex(ExcludePat, RegexOptions.Singleline);

		public IEnumerable<string> ExcludeFiles
		{
			get
			{
				const string key = "ExcludeFiles";

				if (_cacheIenumerableOfString.ContainsKey(key)) return _cacheIenumerableOfString[key];

				var excludeFiles = GetValue(key);

				_cacheIenumerableOfString[key] = new List<string>();
				if (string.IsNullOrEmpty(excludeFiles)) return _cacheIenumerableOfString[key];

				var ms = ExcludeRe.Matches(excludeFiles);
				foreach (Match m in ms)
				{
					if (!string.IsNullOrEmpty(m.Groups["file1"].Value))
						((List<string>)_cacheIenumerableOfString[key]).Add(m.Groups["file1"].Value.Replace("\"\"", "\""));
					else
						((List<string>)_cacheIenumerableOfString[key]).Add(m.Groups["file2"].Value);
				}

				return _cacheIenumerableOfString[key];
			}
		}

		public IEnumerable<string> ExcludeDirectoriesStartingWith
		{
			get
			{
				const string key = "ExcludeDirectoriesStartingWith";

				if (_cacheIenumerableOfString.ContainsKey(key)) return _cacheIenumerableOfString[key];

				var exDirStartingWith = GetValue(key);

				_cacheIenumerableOfString[key] = new List<string>();
				if (string.IsNullOrEmpty(exDirStartingWith)) return _cacheIenumerableOfString[key];

				var ms = ExcludeRe.Matches(exDirStartingWith);
				foreach (Match m in ms)
				{
					if (!string.IsNullOrEmpty(m.Groups["file1"].Value))
						((List<string>)_cacheIenumerableOfString[key]).Add(m.Groups["file1"].Value.Replace("\"\"", "\""));
					else
						((List<string>)_cacheIenumerableOfString[key]).Add(m.Groups["file2"].Value);
				}

				return _cacheIenumerableOfString[key];
			}
		}

		private string GetValue(string key)
		{
			Contract.Requires(() => !string.IsNullOrWhiteSpace(key), "key may not be null, empty or whitespace");
			if (!_appSetting.ContainsKey(key)) return null;

			return _appSetting[key];
		}
	}
}
