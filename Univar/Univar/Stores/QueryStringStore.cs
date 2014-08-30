using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Univar.Helpers;
using System.Text.RegularExpressions;

namespace Univar
{
	public class QueryStringStore : QueryStringStore<string>
	{
		public QueryStringStore(string baseKey) :
			base(baseKey) { }
		public QueryStringStore(string baseKey, bool isCompressed, bool isEncrypted)
			: base(baseKey, isCompressed, isEncrypted) { }
	}

	/// <summary>
	/// A store that uses the querystring for storage.
	/// Note that since querystrings set using this class will automatically cause a page redirect so 
	/// as to be able to update the querystring value.
	/// </summary>
	/// <typeparam name="T">The type of the variable to be stored.</typeparam>
	public class QueryStringStore<T> : DataStore<T, QueryStringStore<T>>
	{
		public bool ClearCurrentQueryString { get; set; }
		/// <summary>
		/// Note that compression is only recommended for data larger than 200 bytes in size since
		///	attempting to compress data below this size always returns a larger string instead.
		/// </summary>
		public bool IsCompressed { get; set; }
		public bool IsEncrypted { get; set; }

		public QueryStringStore(string baseKey) 
			: base(baseKey, Scope.None, Source.QueryString) { }

		public QueryStringStore(string baseKey, bool isCompressed, bool isEncrypted)
			: base(baseKey, Scope.None, Source.QueryString)
		{
			IsCompressed = isCompressed;
			IsEncrypted = isEncrypted;
		}

		protected override T GetValue(string key)
		{
			string value = Storage.QueryString.Get(key, IsCompressed, IsEncrypted, SuppressReadErrors);
			if (value == null)
				return DefaultValue;
			else
				return Serializer.Deserialize<T>(value, DefaultValue, SuppressReadErrors);
		}

		protected override void SetValue(string key, T value, TimeSpan? lifeTime)
		{
			Storage.QueryString.Set<T>(ClearCurrentQueryString, key, value, IsCompressed, IsEncrypted);
		}

		protected override object GetData(string key)
		{
			return Storage.QueryString.Get<object>(key);
		}

		protected override void SetData(string key, object value)
		{
			Storage.QueryString.Set(key, value);
		}

		/// <summary>
		/// Returns the length of the serialized value.
		/// </summary>
		/// <returns></returns>
		public new long GetSize() { return base.GetSize(); }


		protected override IEnumerable<string> GetKeys(Regex regexMatcher)
		{
			return Storage.QueryString.GetKeys(regexMatcher);
		}
	}

}
