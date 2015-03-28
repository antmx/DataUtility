using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;

namespace Netricity.DataUtility.Core
{
	public static class DbParameterCollectionExtensions
	{
		public static DbParameter AddWithValue(this DbParameterCollection collection, string name, object value)
		{
			var idx = collection.Add(value);
			var param = collection[idx];
			param.ParameterName = name;
			//param.Direction = ParameterDirection.InputOutput;

			return param;
		}
	}
}
