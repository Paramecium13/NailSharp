using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;

namespace Generator.Formats
{
	class SimpleStructure : IFormat
	{
		private readonly string _Name;
		CodeTypeDeclaration Declaration;
		public string OutputTypeName => _Name;
		public string NsLTypeName => _Name;

		private readonly IReadOnlyList<SimpleField> Fields;

		internal SimpleStructure(string name, IEnumerable<SimpleField> fields)
		{
			Fields = fields.ToList(); _Name = name;
		}

		internal CodeTypeDeclaration GetDeclaration()
		{
			Declaration = new CodeTypeDeclaration(_Name)
			{
				IsClass = true,
				IsPartial = true
			};
			var constructor = new CodeConstructor
			{
				Attributes = MemberAttributes.Public,
				Name = _Name
			};
			var reader = new CodeMemberMethod
			{
				Attributes = MemberAttributes.Public | MemberAttributes.Static,
				Name = "Read",
				ReturnType = new CodeTypeReference(_Name)
			};
			reader.PopulateParameters += (object sender, EventArgs _) =>
				((CodeMemberMethod)sender).Parameters.Add(new CodeParameterDeclarationExpression(typeof(BinaryReader), "reader"));
			var writer = new CodeMemberMethod
			{
				Attributes = MemberAttributes.Public,
				Name = "Write"
			};
			writer.PopulateParameters += (object sender, EventArgs _) =>
				((CodeMemberMethod)sender).Parameters.Add(new CodeParameterDeclarationExpression(typeof(BinaryWriter), "writer"));
			foreach (var field in Fields)
			{
				Declaration.PopulateMembers += field.OnPopulateMembers;
				field.Register(constructor, reader, writer);
			}
			reader.PopulateStatements += (object sender, EventArgs _) =>
			{
				var create = new CodeObjectCreateExpression { CreateType = new CodeTypeReference(_Name) };
				foreach (var field in Fields)
					create.Parameters.Add(new CodeVariableReferenceExpression(field.VariableName));
				reader.Statements.Add(new CodeMethodReturnStatement(create));
			};
			Declaration.PopulateMembers += (object sender, EventArgs _) =>
			{
				Declaration.Members.Add(constructor);
				Declaration.Members.Add(reader);
				Declaration.Members.Add(writer);
			};
			return Declaration;
		}

		public CodeExpression GetReadValueExpression(CodeExpression binReaderExpression)
			=> new CodeMethodInvokeExpression(new CodeTypeReferenceExpression(_Name), "Read", binReaderExpression);

		public CodeStatement GetWriteStatement(CodeExpression binWriterExpression, CodeExpression selfExpression)
			=> new CodeExpressionStatement(new CodeMethodInvokeExpression(selfExpression, "Write", binWriterExpression));
	}
}
