using System;
using System.CodeDom;
using System.Collections.Generic;

namespace Generator.Formats
{
	delegate void RegisterForConstructor(string typeName, string fieldName);

	class SimpleField
	{
		internal readonly string Name;
		internal string VariableName => "_" + Name;
		readonly Func<IFormat> TypeGetter;

		IFormat Type;

		internal SimpleField(string name, Func<IFormat> type)
		{
			Name = name; TypeGetter = type;
		}

		internal void OnPopulateMembers(object sender, EventArgs e)
		{
			Type = TypeGetter();
			var type = sender as CodeTypeDeclaration;

			var field = new CodeMemberField(Type.OutputTypeName, Name) { Attributes = MemberAttributes.Private };
			type.Members.Add(field);
		}

		internal void Register(CodeConstructor constructor, CodeMemberMethod reader, CodeMemberMethod writer)
		{
			constructor.PopulateParameters += (object sender, EventArgs _) =>
				((CodeConstructor)sender).Parameters.Add(new CodeParameterDeclarationExpression(Type.OutputTypeName, VariableName));
			constructor.PopulateStatements += (object sender, EventArgs _) =>
				(sender as CodeConstructor).Statements.Add(new CodeAssignStatement(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), Name),
					new CodeArgumentReferenceExpression(VariableName)));
			reader.PopulateStatements += (object sender, EventArgs _) =>
			{
				var r = sender as CodeMemberMethod;
				var readExpr = Type.GetReadValueExpression(new CodeArgumentReferenceExpression("reader"));
				r.Statements.Add(new CodeVariableDeclarationStatement(Type.OutputTypeName, VariableName, readExpr));
			};
			writer.PopulateStatements += (object sender, EventArgs _) =>
			{
				var w = sender as CodeMemberMethod;
				var fieldExpr = new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), Name);
				var writerExpr = new CodeArgumentReferenceExpression("writer");
				w.Statements.Add(Type.GetWriteStatement(writerExpr, fieldExpr));
			};
		}
	}
}