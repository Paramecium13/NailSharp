using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Generator
{

	class Lexer
	{
		private readonly static char[] LoneSymbols = {
			'+',';','-','*','%','>','<','~','!','?','\\',/*'@','$',*/'=','&'
		};
		private readonly static char[] MultiSymbols = {
			'|','/'
		};

		private static readonly string[] Keywords = { };

		enum LexerStatus
		{
			Base,
			Zero,
			Number,
			Hex,
			Bin,
			Decimal,
			Identifier,
			SymbolLine,
			SymbolSlash,
			SymbolMinus,
			SymbolAt,
			CommentSingleline,
			CommentMultiline,
			/// <summary>
			/// When a * is seen in a multiline comment.
			/// </summary>
			CommentMultiLineStar,
			StringBase,
			StringEsc,
			StringU0,
			StringU1,
			StringU2,
			StringU3,
			LitString,
		}

		enum LexerTokenType
		{
			Unknown,
			/// <summary>
			/// Identifier or keyword.
			/// </summary>
			Word,
			Float,
			// base 10 integer
			Base10,
			Hex,
			Bin,
			String,
			Assignment,
			Operator,
			SyntaxSymbol
		}

		bool IsNegative = false;
		bool ContinueLine;
		private readonly StringBuilder builder = new StringBuilder();
		protected readonly StringBuilder UEscBuild = new StringBuilder();

		private readonly List<Token> Tokens = new List<Token>();
		private LexerTokenType TokenType;
		private uint LineNumber;
		private LexerStatus State;

		public IReadOnlyList<Token> Parse(string text)
		{
			Tokens.Clear();
			foreach (char c in text) ReadChar(c);
			return Tokens.ToList();
		}

		protected void ReadChar(char c)
		{
			if (c == '\n')
			{
				LineNumber++;
				if(ContinueLine) { ContinueLine = false; return; }
				if (State < LexerStatus.CommentSingleline)
				{
					Pop();
					Tokens.Add(new Token(Generator.TokenType.NewLine, LineNumber, "", 0));
					return;
				}
			}
			if (State < LexerStatus.CommentSingleline)
				BaseReadChar(c);
			else if (State < LexerStatus.StringBase)
				CommentReadChar(c);
			else StrReadChar(c);
		}

		private void BaseReadChar(char c)
		{
			if (char.IsWhiteSpace(c)) { Pop(); return; }
			if (LoneSymbols.Contains(c)) // It's a one char operator.
			{
				Pop();
				TokenType = LexerTokenType.Operator;
				switch (c)
				{
					case '\\':
						ContinueLine = true;
						return;
					case '-':
						State = LexerStatus.SymbolMinus;
						builder.Append(c);
						return;
					case ';':
						Pop();
						Tokens.Add(new Token(Generator.TokenType.NewLine, LineNumber, "", 0));
						return;
					default:
						builder.Append(c);
						Pop();
						return;
				}
			}
			if (c == '"')
			{
				Pop();
				State = LexerStatus.StringBase;
				TokenType = LexerTokenType.String;
				return;
			}
			if (MultiSymbols.Contains(c))
			{
				switch (c)
				{
					case '/':
						State = LexerStatus.SymbolSlash;
						builder.Append(c);
						return;
					case '|':
						State = LexerStatus.SymbolLine;
						builder.Append(c);
						return;
					default:	break;
				}
			}
			switch (State)
			{
				case LexerStatus.Base:
					if (char.IsDigit(c))
					{
						builder.Append(c);
						TokenType = LexerTokenType.Base10;
						if (c == '0') State = LexerStatus.Zero;
						else State = LexerStatus.Number;
					}
					else if (c == '@')
					{
						builder.Append(c);
						TokenType = LexerTokenType.Word;
						State = LexerStatus.SymbolAt;
					}
					else
					{
						builder.Append(c);
						TokenType = LexerTokenType.Word;
						State = LexerStatus.Identifier;
					}
					break;
				case LexerStatus.Zero:
					switch (c)
					{
						case 'x':
						case 'X':
							State = LexerStatus.Hex;
							break;
						case 'b':
						case 'B':
							State = LexerStatus.Bin;
							break;
						case '.':
							State = LexerStatus.Decimal;
							TokenType = LexerTokenType.Float;
							break;
						default:
							throw new NotImplementedException("Octal not supported...");
					}
					builder.Append(c);
					break;
				case LexerStatus.Number:
					if (char.IsDigit(c)) builder.Append(c);
					else if (c == '_') break;
					else if (c == '.')
					{
						State = LexerStatus.Decimal;
						TokenType = LexerTokenType.Float;
						builder.Append(c);
					}
					else throw new ApplicationException("Tokenization Error: Invalid number...");
					break;
				case LexerStatus.Hex:
					if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f'))
						builder.Append(c);
					else if (c == '_') break;
					else throw new ApplicationException("Tokenization Error: Invalid number...");
					break;
				case LexerStatus.Bin:
					if (c == '0' || c == '1') builder.Append(c);
					else if (c == '_') break;
					else throw new ApplicationException("Tokenization Error: Invalid number...");
					break;
				case LexerStatus.Decimal:
					if (char.IsDigit(c)) builder.Append(c);
					else if (c == '_') break;
					else throw new ApplicationException("Tokenization Error: Invalid number...");
					break;
				case LexerStatus.Identifier:
					builder.Append(c);
					break;
				case LexerStatus.SymbolLine:
					throw new NotImplementedException();
					//break;
				case LexerStatus.SymbolSlash:
					if (c == '\\')
					{
						State = LexerStatus.CommentSingleline;
						builder.Clear();
					}
					else Pop();
					break;
				case LexerStatus.SymbolAt:
					if (c == '\"')
					{
						builder.Clear();
						State = LexerStatus.LitString;
						TokenType = LexerTokenType.String;
						break;
					}
					if (char.IsDigit(c)) throw new ApplicationException("...");
					builder.Append(c);
					TokenType = LexerTokenType.Word;
					State = LexerStatus.Identifier;
					break;
				case LexerStatus.SymbolMinus:
					if (char.IsDigit(c))
					{
						builder.Append(c);
						State = LexerStatus.Number;
						TokenType = LexerTokenType.Base10;
						IsNegative = true;
					}
					else
					{
						TokenType = LexerTokenType.SyntaxSymbol;
						Pop();
						State = LexerStatus.Base;
					}
					break;
				default:
					break;
			}
		}

		private void CommentReadChar(char c)
		{
			switch (State)
			{
				case LexerStatus.CommentSingleline:
					break;
				case LexerStatus.CommentMultiline:
					break;
				case LexerStatus.CommentMultiLineStar:
					break;
				default:
					break;
			}
		}

		private void StrReadChar(char c)
		{
			switch (State)
			{
				case LexerStatus.StringBase:
					if (c == '"') Pop();
					else if (c == '\\') State = LexerStatus.StringEsc;
					else builder.Append(c);
					break;
				case LexerStatus.StringEsc:
					if (c == 'u' || c == 'x') throw new NotImplementedException();
					builder.Append(char.Parse($"\\{c}"));
					break;
				case LexerStatus.StringU0:
				case LexerStatus.StringU1:
				case LexerStatus.StringU2:
				case LexerStatus.StringU3:
					throw new NotImplementedException();
				case LexerStatus.LitString:
					if (c == '"') Pop();
					break;
				default:
					break;
			}
		}

		protected unsafe void Pop()
		{
			ulong data = 0;
			var type = Generator.TokenType.Identifier;
			var str = builder.ToString();
			switch (TokenType)
			{
				case LexerTokenType.Unknown:
				case LexerTokenType.Word:
					if (str.StartsWith("@", StringComparison.Ordinal)) type = Generator.TokenType.DepField;
					break;
				case LexerTokenType.Float:
					{
						type = Generator.TokenType.Float;
						var x = double.Parse(str);
						data = *((ulong*)&x);
						break;
					}
				case LexerTokenType.Base10:
					if (IsNegative)
					{
						type = Generator.TokenType.NegInt;
						var x = long.Parse(str);
						data = *((ulong*)&x);
					}
					else
					{
						type = Generator.TokenType.NonNegInt;
						data = ulong.Parse(str);
					}
					break;
				case LexerTokenType.Hex:
					if (IsNegative)
					{
						type = Generator.TokenType.NegInt;
						var x = long.Parse(str, System.Globalization.NumberStyles.HexNumber);
						data = *((ulong*)&x);
					}
					else
					{
						type = Generator.TokenType.NonNegInt;
						data = ulong.Parse(str, System.Globalization.NumberStyles.HexNumber);
					}
					break;
				case LexerTokenType.Bin:
					throw new NotImplementedException();
				case LexerTokenType.String:
					type = Generator.TokenType.String;
					break;
				case LexerTokenType.Assignment:
				case LexerTokenType.Operator:
				case LexerTokenType.SyntaxSymbol:
					type = Generator.TokenType.Symbol;
					break;
				default:
					break;
			}

			if(str.Length != 0)
				Tokens.Add(new Token(type, LineNumber, str, data));
			builder.Clear();
			IsNegative = false;
			State = LexerStatus.Base;
			TokenType = LexerTokenType.Unknown;
		}
	}
}
