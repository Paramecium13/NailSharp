using System;
using System.Collections.Generic;
using System.Text;

namespace Generator
{
	interface IFormat
	{
		string OutputTypeName { get; }
		string NsLTypeName { get; }
	}
}
