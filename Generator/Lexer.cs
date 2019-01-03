using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Generator
{

	class Lexer
	{
		private readonly static char[] LoneSymbols = {
			'+','-','*','/','%','>','<','~','!','?',/*'@','$',*/'=','&'
		};
		private readonly static char[] MultiSymbols = {
			'|','\\'
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
			Dec,
			Hex,
			Bin,
			String,
			Assignment,
			Operator,
			SyntaxSymbol
		}

		bool IsNegative = false;

		private readonly StringBuilder builder = new StringBuilder();
		protected readonly StringBuilder UEscBuild = new StringBuilder();

		private readonly List<Token> Tokens = new List<Token>();
		private LexerTokenType TokenType;
		private uint LineNumber;
		private LexerStatus State;

		protected void ReadChar(char c)
		{
			if (c == '\n')
			{
				LineNumber++;
				if (State < LexerStatus.CommentSingleline)
				{
					Pop();
					Tokens.Add(new Token(Generator.TokenType.NewLine, LineNumber, "", 0));
				}
			}
			else if (State < LexerStatus.CommentSingleline)
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
				builder.Append(c);
				Pop();
				return;
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
					case '\\':
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
						TokenType = LexerTokenType.Dec;
						if (c == '0') State = LexerStatus.Zero;
						else State = LexerStatus.Number;
					}
					else if (c == '@')
					{
						builder.Append(c);
						TokenType = LexerTokenType.Word;
						State = LexerStatus.SymbolAt;
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
					break;
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
			throw new NotImplementedException();
		}

		protected void Pop()
		{
			ulong data = 0;
			var type = Generator.TokenType.Identifier;
			switch (TokenType)
			{
				case LexerTokenType.Unknown:
				case LexerTokenType.Word:
					break;
				case LexerTokenType.Float:
					break;
				case LexerTokenType.Dec:
					break;
				case LexerTokenType.Hex:
					break;
				case LexerTokenType.Bin:
					break;
				case LexerTokenType.String:
					break;
				case LexerTokenType.Assignment:
				case LexerTokenType.Operator:
				case LexerTokenType.SyntaxSymbol:
					type = Generator.TokenType.Symbol;
					break;
				default:
					break;
			}

			Tokens.Add(new Token(type, LineNumber, builder.ToString(), data));
			builder.Clear();
		}
	}
}
