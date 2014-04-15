using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.IO.Compression;

namespace Univar.Helpers
{
	public static class Compressor
	{
		public static string CompressToBase64(string text)
		{
			if (string.IsNullOrEmpty(text))
				return text;

			byte[] buffer = Encoding.UTF8.GetBytes(text); MemoryStream ms = new MemoryStream();
			using (GZipStream zip = new GZipStream(ms, CompressionMode.Compress, true))
			{
				zip.Write(buffer, 0, buffer.Length);
			}
			ms.Position = 0;
			MemoryStream outStream = new MemoryStream();
			byte[] compressed = new byte[ms.Length];
			ms.Read(compressed, 0, compressed.Length);
			byte[] gzBuffer = new byte[compressed.Length + 4];
			System.Buffer.BlockCopy(compressed, 0, gzBuffer, 4, compressed.Length);
			System.Buffer.BlockCopy(BitConverter.GetBytes(buffer.Length), 0, gzBuffer, 0, 4);
			return Convert.ToBase64String(gzBuffer);
		}

		public static string UncompressFromBase64(string compressedText, bool returnNullOnError)
		{
			if (string.IsNullOrEmpty(compressedText))
				return compressedText;

			try
			{
				byte[] gzBuffer = Convert.FromBase64String(compressedText);
				using (MemoryStream ms = new MemoryStream())
				{
					int msgLength = BitConverter.ToInt32(gzBuffer, 0);
					ms.Write(gzBuffer, 4, gzBuffer.Length - 4);
					byte[] buffer = new byte[msgLength];
					ms.Position = 0;
					using (GZipStream zip = new GZipStream(ms, CompressionMode.Decompress))
					{
						zip.Read(buffer, 0, buffer.Length);
					}
					return Encoding.UTF8.GetString(buffer);
				}
			}
			catch (Exception ex)
			{
				if (returnNullOnError)
					return null;
				else
					throw ex;
			}
		}

		public static string CompressToBinary(object obj)
		{
			MemoryStream ms = new MemoryStream();
			new BinaryFormatter().Serialize(ms, obj);
			byte[] inbyt = ms.ToArray();
			MemoryStream objStream = new MemoryStream();
			DeflateStream objZS = new DeflateStream(objStream, CompressionMode.Compress);
			objZS.Write(inbyt, 0, inbyt.Length);
			objZS.Flush();
			objZS.Close();
			return Convert.ToBase64String(objStream.ToArray());
		}

		public static object DecompressFromBinary(string text)
		{
			byte[] bytCook = Convert.FromBase64String(text);
			MemoryStream inMs = new MemoryStream(bytCook);
			inMs.Seek(0, 0);
			DeflateStream zipStream = new DeflateStream(inMs, CompressionMode.Decompress, true);
			byte[] outByt = ReadFullStream(zipStream);
			zipStream.Flush();
			zipStream.Close();
			MemoryStream outMs = new MemoryStream(outByt);
			outMs.Seek(0, 0);
			BinaryFormatter bf = new BinaryFormatter();
			return (object)bf.Deserialize(outMs, null);
		}

		private static byte[] ReadFullStream(Stream stream)
		{
			byte[] buffer = new byte[32768];
			using (MemoryStream ms = new MemoryStream())
			{
				while (true)
				{
					int read = stream.Read(buffer, 0, buffer.Length);
					if (read <= 0)
						return ms.ToArray();
					ms.Write(buffer, 0, read);
				}
			}
		}
	}
}