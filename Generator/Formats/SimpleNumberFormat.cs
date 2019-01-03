using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Generator.Formats
{
	class SimpleNumberFormat : IFormat
	{
		private readonly byte NumBits;

		private readonly bool IsSigned;

		public string OutputTypeName
		{
			get
			{
				switch (NumBits)
				{
					case 8:
						return IsSigned ? "SByte" : "Byte";
					case 16:
						return IsSigned ? "Int16" : "UInt16";
					case 32:
						return IsSigned ? "Int32" : "UInt32";
					case 64:
						return IsSigned ? "Int64" : "UInt64";
					default:
						throw new NotImplementedException();
				}
			}
		}

		public string NsLTypeName { get; private set; }

		private SimpleNumberFormat(string text)
		{
			NsLTypeName = text;
			string num;
			if (text.StartsWith("u", StringComparison.Ordinal))
			{
				// uint32
				num = text.Substring(4);
				IsSigned = false;
			}
			else
			{
				IsSigned = true;
				num = text.Substring(3);
			}
			NumBits = byte.Parse(num);
		}

		private static readonly IDictionary<string, SimpleNumberFormat> Formats = new Dictionary<string, SimpleNumberFormat>();
		public static SimpleNumberFormat GetFormat(string name)
		{
			if (Formats.ContainsKey(name)) return Formats[name];
			var format = new SimpleNumberFormat(name);
			Formats.Add(name, format);
			return format;
		}

		public CodeExpression GetReadValueExpression(CodeExpression binReaderExpression)
			=> new CodeMethodInvokeExpression(binReaderExpression, "Read" + OutputTypeName);

		public CodeStatement GetWriteStatement(CodeExpression binWriterExpression, CodeExpression selfExpression)
			=> new CodeExpressionStatement(new CodeMethodInvokeExpression(binWriterExpression, "Write", selfExpression));
	}
}
