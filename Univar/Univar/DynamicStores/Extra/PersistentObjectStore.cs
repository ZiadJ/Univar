using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.UI;
using System.IO;
using System.Xml;
using System.Runtime.Serialization;

namespace Univar
{
	/// <summary>
	/// Provides a class to persist an object. Asp.Net Server controls however are 
	/// not supported since they are not serializable as yet.
	/// </summary>
	/// <typeparam name="T">The object type.</typeparam>
	public class PersistentObjectStore<T> : DynamicStore<T>
	{
		private string _parentKey;
		private T _object;
		public string DefaultParentKey = "PersistentObject";

		public PersistentObjectStore(string baseKey, ref T objectToPersist)
			: this(null, baseKey, Source.Session, ref objectToPersist)
		{ }
		public PersistentObjectStore(string baseKey, Source sourceType, ref T objectToPersist)
			: this(null, baseKey, sourceType, ref objectToPersist)
		{ }

		public PersistentObjectStore(string parentKey, string baseKey, Source sourceType, ref T objectToPersist)
			: base(default(T), baseKey, null, null, null, sourceType)
		{
			_parentKey = parentKey == null ? DefaultParentKey : parentKey;
			_object = objectToPersist;
		}

		public static void Preserve(string parentKey, string baseKey, Source sourceType, bool save, ref T objectToPersist)
		{
			new PersistentObjectStore<T>(parentKey, baseKey, sourceType, ref objectToPersist).Preserve(save);
		}

		public static void Preserve(string baseKey, Source sourceType, bool save, ref T objectToPersist)
		{
			new PersistentObjectStore<T>(null, baseKey, sourceType, ref objectToPersist).Preserve(save);
		}

		public void Preserve(bool save)
		{
			if (save)
				SetValue(_parentKey + Storage.KeyDelimiter + Key, _object);
			else
				_object = GetValue(_parentKey + Storage.KeyDelimiter + Key);
		}
	}
}
