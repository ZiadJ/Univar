using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Web.Caching;

namespace Univar
{
	/// <summary>
	/// An implementation of the DynamicStore<T> class that provides the NullableValue property for non nullable types.
	/// It returns null if the key does not exist.
	/// </summary>
	/// <typeparam key="T">The generic type T for the variable. 
	/// Note that only non nullable types are accepted</typeparam>
	public class NullableDynamicStore<T> : DynamicStore<T> where T : struct
	{
		public NullableDynamicStore(string baseKey, params Source[] sourceTypes)
			: base(default(T), baseKey, null, null, null, sourceTypes) { }

		public NullableDynamicStore(T defaultValue, string baseKey, params Source[] sourceTypes)
			: base(defaultValue, baseKey, null, null, null, sourceTypes) { }


		/// <summary>
		/// Initializes a new instance of the <see cref="NullableDynamicStore&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="defaultValue">The default value used when the specified key does not exist in the 
		/// specified sources.</param>
		/// <param name="key">The key for the stored value.</param>
		/// <param name="cookieLifeTime">
		/// The cookie life time. Its value is set to 100 days when a null value is specified.</param>
		/// <param name="cacheLifeTime">
		/// The cache life time. Its value is set to 20 minutes when a null value is specified.</param>
		/// <param name="sourcesStorages">A list of storage types from which the value will be retrieved
		/// Each type is hardcoded within the Sources enum and they must be seperated by a comma.
		/// The order in which they are listed determines the order in which they are accessed until a value is found.
		/// Note that sources starting with ReadOnly are not saved when a value is assigned.
		/// </param>
		public NullableDynamicStore(T defaultValue, string baseKey, TimeSpan? cookieLifeTime, CacheDependency cacheDependency, TimeSpan? cacheLifeTime,
			params Source[] sourcesStorages)
			: base(defaultValue, baseKey, cookieLifeTime, cacheDependency, cacheLifeTime, sourcesStorages) { }

		public T? NullableValue
		{
			get
			{
				T value = GetValue<T>(Key, IsCompressed, IsEncrypted, DataSources);
				if (LastAccessedSource != Source.None)
					return value;
				else
					return null;
			}
			set
			{
				if (value.HasValue)
					SetValue(Key, value.Value);
				else
					Clear();
			}
		}
	}
}