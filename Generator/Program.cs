using Microsoft.CSharp;
using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Generator
{
	class Program
	{
		static int Main(string[] args)
		{
			if(args.Length < 1)
			{
				Console.Error.WriteLine("No File Specified.");
				return 5;
			}
			var text = File.ReadAllText(args[0]);
			var ns = Path.GetFileNameWithoutExtension(args[0]);
			var parser = new SimpleParser(text, ns);
			var types = parser.Parse();
			var options = new CodeGeneratorOptions
			{
				BlankLinesBetweenMembers = true,
				VerbatimOrder = true,
				IndentString = "\t"
			};
			using (var provider = new CSharpCodeProvider())
			{
				foreach (var type in types)
				{
					var fName = type.Name + ".cs";
					var unit = new CodeCompileUnit();
					var codeNamespace = new CodeNamespace(ns);
					codeNamespace.PopulateTypes += (object sender, EventArgs _) =>
						codeNamespace.Types.Add(type);
					codeNamespace.PopulateImports += (object sender, EventArgs _) =>
					{
						codeNamespace.Imports.Add(new CodeNamespaceImport("System"));
						codeNamespace.Imports.Add(new CodeNamespaceImport("System.IO"));
					};
					unit.Namespaces.Add(codeNamespace);
					using (var file = File.CreateText(fName))
					{
						provider.GenerateCodeFromCompileUnit(unit, file, options);
					}
				}
			}
			return 0;
		}
	}
}
