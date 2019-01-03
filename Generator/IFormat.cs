using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Text;

namespace Generator
{
	interface IFormat
	{
		string OutputTypeName { get; }
		string NsLTypeName { get; }

		CodeExpression GetReadValueExpression(CodeExpression binReaderExpression);

		CodeStatement GetWriteStatement(CodeExpression binWriterExpression, CodeExpression selfExpression);
	}
}
