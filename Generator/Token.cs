using System;

namespace Generator
{
	enum TokenType
	{
		NewLine,
		Symbol,
		//NonNegInt,
		//NegInt,
		Int,
		Float,
		Identifier,
		DepField
	}

	readonly struct Token
	{
		public readonly TokenType Type;
		public readonly uint LinNum;
		public readonly string Text;
		private readonly ulong _Data;

		public unsafe double DoubleValue
		{
			get
			{
				var dat = _Data;
				var dbp = (double*)&dat;
				return *dbp;
			}
		}

		public unsafe long SignedValue
		{
			get
			{
				var dat = _Data;
				var lp = (long*)&dat;
				return *lp;
			}
		}

		public ulong UnsignedValue => _Data;

		public Token(TokenType type, uint lineNum, string text, ulong data)
		{
			Type = type; LinNum = lineNum; Text = text; _Data = data;
		}

	}
}