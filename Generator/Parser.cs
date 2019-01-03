using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Generator.Formats;

namespace Generator
{
	/*class PreStructure
	{
		private readonly IReadOnlyList<Token[]> Lines;
	}*/

	class SimpleParser
	{
		private readonly IReadOnlyList<Token[]> Lines;

		private readonly IDictionary<string, IFormat> Formats = new Dictionary<string, IFormat>();

		//private readonly IDictionary<string, PreStructure> PreStructures;

		private readonly IList<CodeTypeDeclaration> TypeDeclarations = new List<CodeTypeDeclaration>();

		private Action<Token[]> LineReader;

		private readonly string Namespace;

		internal SimpleParser(string text, string _namespace)
		{
			Namespace = _namespace;
			var lexer = new Lexer();
			Lines = ToLines(lexer.Parse(text));
			LineReader = BaseReadLine;
		}

		internal IFormat GetFormat(string name)
		{
			if (name.StartsWith("int", StringComparison.Ordinal) || name.StartsWith("uint", StringComparison.Ordinal))
				return SimpleNumberFormat.GetFormat(name);
			return Formats[name];
		}

		internal CodeTypeDeclaration[] Parse()
		{
			if (LineReader == null)
				throw new ApplicationException();
			foreach (var line in Lines)
				LineReader(line);
			return TypeDeclarations.ToArray();
		}

		void BaseReadLine(Token[] line)
		{
			// Not how I usually do things... Its sort of a functional pattern; I thought about using states (as classes)
			// but didn't feel like it...
			// It's quite concise and is easily understandable immediately after having written it.
			if(line.Length >= 2 && line[1].Text == "=")
			{
				var name = line[0].Text;
				var fields = new List<SimpleField>();
				void readStruct(Token[] x)
				{
					var capture = name;
					var f = fields;

					if (x.Length == 2)
					{
						fields.Add(new SimpleField(x[0].Text, () => GetFormat(x[1].Text)));
					}
					else if (x.Length == 1 && x[0].Text == "}")
					{
						var format = new SimpleStructure(name, fields);
						TypeDeclarations.Add(format.GetDeclaration());
						Formats.Add(name, format);
						LineReader = BaseReadLine;
					}
					else throw new ApplicationException();
				}
				if (line.Length > 2)
				{
					if(line[2].Text != "{")
						throw new NotImplementedException();
					LineReader = readStruct;
				}
				else LineReader = (Token[] x) =>
				{
					if (x.Length != 1 || x[0].Text != "{")
						throw new ApplicationException();
					LineReader = readStruct;
				};
			}
		}

		static IReadOnlyList<Token[]> ToLines(IReadOnlyList<Token> tokens)
		{
			var lines = new List<Token[]>();
			var currentLine = new List<Token>();
			foreach (var token in tokens)
			{
				if (token.Type == TokenType.NewLine)
				{
					if (currentLine.Count != 0)
					{
						lines.Add(currentLine.ToArray());
						currentLine.Clear();
					}
				}
				else currentLine.Add(token);
			}

			return lines;
		}
	}
}
