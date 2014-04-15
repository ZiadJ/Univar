using System;
using System.Reflection;

namespace Univar.Helpers
{
	public static class Reflector
	{
		public static T GetPropertyValue<T>(object obj, string propertykey)
		{

			PropertyInfo property = obj.GetType().GetProperty(propertykey);
			return (T)property.GetValue(obj, null);
		}

		public static void SetPropertyValue<T>(object obj, string propertykey, T value)
		{
			SetPropertyValue<T>(obj, propertykey, value, false);
		}

		public static void SetPropertyValue<T>(object obj, string propertykey, T value, bool setOnlyWhenValuesDiffer)
		{
			PropertyInfo property = obj.GetType().GetProperty(propertykey);
			if (setOnlyWhenValuesDiffer)
			{
				T val = (T)property.GetValue(obj, null);
				if (val.Equals(value))
					return;
			}
			property.SetValue(obj, value, null);
		}
	}
}