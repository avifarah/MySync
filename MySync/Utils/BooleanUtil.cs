using System;
using System.Collections.Generic;
using System.Linq;


namespace MySync.Utils
{
	public static class BooleanUtil
	{
		private static readonly List<string> Yeses = new() { "Yes", "Y", "True", "T", "OK", "K", "1" };
		private static readonly List<string> Nos = new() { "No", "N", "False", "F", "0" };

		public static bool IsTrue(this string text) => Yeses.FirstOrDefault(y => string.Compare(y, text, StringComparison.OrdinalIgnoreCase) == 0) != null;

		public static bool IsFalse(this string text) => Nos.FirstOrDefault(n => string.Compare(n, text, StringComparison.OrdinalIgnoreCase) == 0) != null;
	}
}
