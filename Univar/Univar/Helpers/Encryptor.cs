using System;
using System.Web;
using System.Text;
using System.Web.Security;
using System.Reflection;

namespace Univar.Helpers
{
    /// <summary>
    /// A class to encrypt and decrypt data based on the ASP.NET encryption mechanism
    /// typically used to create tamper proof cookies. It uses MachineKey.Encode and 
    /// MachineKeyProtection.Decode internally which are what ASP.NET uses for MachineKey
    /// based cookie authorization.
    /// </summary>
    public static class Encryptor
    {
        private const string PURPOSE = "Authentication Token";


        public static string Encrypt(string text)
        {
            return Encrypt(text, PURPOSE);
        }

        public static string Encrypt(string text, string purpose)
        {
            var buf = Encoding.UTF8.GetBytes(text);
            var protectedBytes = MachineKey.Protect(buf, purpose);
            return Convert.ToBase64String(protectedBytes);

            //byte[] buf = Encoding.UTF8.GetBytes(text);
            //return (string)MachineKey.Encode(buf, machineProtection);
        }

        /// <summary>
        /// Decrypts a data that has all levels of machinekey protection.
        /// </summary>
        public static string Decrypt(string text, bool suppressErrors)
        {
            return Decrypt(text, PURPOSE, suppressErrors);
        }

        /// <summary>
        /// Decodes a string.
        /// </summary>
        /// <param name="text">String to decode.</param>
        /// <param name="machineProtection">The method in which the string is protected.</param>
        /// <param name="throwExceptionOnError">Throw an exception message when an error occurs
        /// instead of returning a null value.</param>
        /// <returns>The decrypted string or throws InvalidCastException if tampered with.</returns>
        public static string Decrypt(string text, string purpose, bool suppressErrors)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            byte[] buf;
            try
            {
                buf = Convert.FromBase64String(text);
                buf = MachineKey.Unprotect(buf, purpose);
                
                //buf = (byte[])MachineKey.Decode(text, machineProtection);
                if (buf == null || buf.Length == 0)
                    throw new Exception();
            }
            catch (Exception ex)
            {
                if (suppressErrors)
                    return null;
                else
                    throw new InvalidCastException("Unable to decrypt the text", ex.InnerException);
            }

            return Encoding.UTF8.GetString(buf);
        }
    }
}