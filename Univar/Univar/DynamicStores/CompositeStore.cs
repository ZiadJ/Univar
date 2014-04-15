using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Web.UI.WebControls;
using System.Web.UI;
using Univar.Helpers;

namespace Univar
{
	/// <summary>
	/// A class inheriting from DynamicStore that adds the ability to include an plain objects
	/// in the source list. The target property name must be specified for object(s) specified
	/// and must obviously not be readonly if any changes are to be applied to them. Only one
	/// property name can be used in this implementation.
	/// </summary>
	/// <typeparam name="T">The generic type T for the variable.</typeparam>
	public class CompositeStore<T> : DynamicStore<T>
	{
		public object[] SourceObjects { get; set; }
		public string PropertyName { get; set; }

		public CompositeStore(string baseKey, string propertyName, params object[] sourceObjects)
			: this(default(T), baseKey, propertyName, sourceObjects) { }

		/// <summary>
		/// Creates a value store that, on top of using available cache objects like the session, cache, querystring, 
		/// can use the property of any object as store.
		/// </summary>
		/// <param name="defaultValue">The default value if no value was found.</param>
		/// <param name="key">The value key.</param>
		/// <param name="propertyName">The property name of the property name for the object(s) to be cached.</param>
		/// <param name="sourceObjects">A list of alternate objects from which the value for the specified property will be used.</param>
		public CompositeStore(T defaultValue, string baseKey, string propertyName, params object[] sourceObjects)
			: base(defaultValue, baseKey, null, null, null, null)
		{
			SourceObjects = sourceObjects;
		}

		public new T Value
		{
			get
			{
				if (SourceObjects != null)
				{
					object value;
					// Scan the SourceTypes & SourceControls list for the key or property name.
					foreach (object obj in SourceObjects)
					{
						if (obj.GetType() == typeof(Source))
						{
							value = GetValue<T>(Key, IsCompressed, IsEncrypted, (Source)obj);
							if (LastAccessedSource != Source.None && value != null)
								return (T)value;
						}
						else
						{
							value = Reflector.GetPropertyValue<object>(obj, PropertyName);
							if (value != null)
							{
								// Serialization is not applied on primitive or string types.
								// This allows json data to be stored in a hidden field as well.
								return Serializer.Deserialize<T>(value.ToString(), SuppressReadErrors);
							}
						}

					}
				}
				return default(T);
			}
			set
			{
				// Set the value for all target types
				base.Value = value;
				// Set the value for all target controls
				foreach (object obj in SourceObjects)
				{
					if (obj.GetType() == typeof(Source))
					{
						SetValue<T>(Key, value, IsCompressed, IsEncrypted, false);
					}
					else
					{    // Serialization is not applied on primitive or string types.
						// This allows json data to be stored in a hidden field as well.
						Reflector.SetPropertyValue<object>(obj, PropertyName, Serializer.Serialize<T>(value, JsonEncoding.None, true));
					}
				}
			}
		}

	}
}