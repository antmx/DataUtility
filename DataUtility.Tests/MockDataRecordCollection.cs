using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlServer.Server;

namespace Netricity.DataUtility.Tests
{
	class MockDataRecordCollection
	{
		public IEnumerable<IDataRecord> FetchItems()
		{
			// Define the SqlDataRecord's schema
			var dataRecord = new SqlDataRecord(
				new SqlMetaData("Name", SqlDbType.VarChar, 100),
				new SqlMetaData("Value", SqlDbType.NVarChar, -1)); // -1 means NVarChar(Max)

			var settings = new Dictionary<string, string> {
				{"Setting 1", "Foo"},
				{"Setting 2", "Bar"},
				{"Setting 3", "Baz"},
				{"Setting 4", "Qux"}
			};

			// Yield an SqlDataRecord for each setting in this List
			foreach (var setting in settings)
			{
				// Skip if anything is null
				if (setting.Key == null || setting.Value == null)
					continue;

				dataRecord.SetString(0, setting.Key);
				dataRecord.SetString(1, setting.Value.ToString());

				yield return dataRecord;
			}
		}
	}
}
