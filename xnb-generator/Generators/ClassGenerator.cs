using System;
using System.Linq;
using System.Collections.Generic;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

using Schemas;

namespace xnbgenerator.Generators
{
    public class ClassGenerator
    {
		bool isExtension = false;

        public void Generate(xcb xcb, string name, string extName)
		{
			isExtension = !string.IsNullOrEmpty(extName);

            List<MemberDeclarationSyntax> classMembers = new List<MemberDeclarationSyntax>();

            CodeWriter cw = new CodeWriter(name + ".cs");
            
			AccessorDeclarationSyntax xnameGetter =
				AccessorDeclaration(SyntaxKind.GetAccessorDeclaration,
				                    Block(ReturnStatement(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(extName)))));

			PropertyDeclarationSyntax xnameProperty =
				PropertyDeclaration(List<AttributeListSyntax>(),
									TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.OverrideKeyword)),
				                    PredefinedType(Token(SyntaxKind.StringKeyword)),
				                    null, Identifier("XName"), AccessorList(SingletonList(xnameGetter)));
            
			foreach (object o in xcb.Items)
			{
				if (o is @event)
				{
					classMembers.Add(GenEvent(o as @event));
			    }
                else if (o is @request)
                {
					classMembers.Add(GenFunction(cw, o as @request));
                }
			}

			ClassDeclarationSyntax cd = 
				ClassDeclaration(name).
			    AddBaseListTypes(SimpleBaseType(IdentifierName("Extension"))).
                WithMembers(List(classMembers));

			List<UsingDirectiveSyntax> usings = 
				new[]
                {
                    "System", "System.Collections", "System.Collections.Generic", "System.Runtime.InteropServices",
                    "Mono.Unix", "XNB.Protocol.XNB", "XNB.Protocol.XProto"
    			}.
    		    Select(IdentifierName).
    		    Select(UsingDirective).
    			ToList();

			NamespaceDeclarationSyntax ns = 
				NamespaceDeclaration(IdentifierName("XNB"), 
                                     List<ExternAliasDirectiveSyntax>(), 
                                     List(usings), 
				                     SingletonList<MemberDeclarationSyntax>(cd));

            cw.Close();
        }

		MemberDeclarationSyntax GenEvent(@event e)
		{
			if (e.name == null)
			{
				throw new Exception("Can't have null name");            // FIXME: handle this
		    }

			string name = GeneratorUtil.ToCs(e.name);

			return EventDeclaration(GenericName(Identifier("EventHandler"),
										 TypeArgumentList(
											 SingletonSeparatedList<TypeSyntax>(
												 IdentifierName(name + "Event")))),
							        name + "Event").
                WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)));
        }

		MemberDeclarationSyntax GenFunction(CodeWriter cw, @request r)
        {
            if (r.name == null)
			{
                throw new Exception("Can't have null name");            // FIXME: handle this
			}

            //TODO: share code with struct
			List<Tuple<string, string>> messageParams = new List<Tuple<string, string>>();      // type, name
            List<Tuple<string, string>> listParams = new List<Tuple<string, string>>();         // type, name
            
            if (r.Items != null)
            {               
                foreach (object ob in r.Items)
                {
                    if (ob is field)
                    {
                        field f = ob as field;
                        if (f.name == null)
						{
							continue;
                        }
                        
						messageParams.Add(Tuple.Create(Generator.TypeToCs(f.type), GeneratorUtil.ToCs(f.name)));
                    }
                    else if (ob is list)
                    {
                        list l = ob as list;

                        if (l.name == null)
						{
							continue;
                        }
                        
                        if (l.type == "char")
                        {
							listParams.Add(Tuple.Create("string", GeneratorUtil.ToCs(l.name)));
                        }
                        else if (l.type == "CARD32")
						{
                            listParams.Add(Tuple.Create("uint[]", GeneratorUtil.ToCs(l.name)));
                        }
                    }
                    else if (ob is valueparam)
                    {
                        valueparam v = ob as valueparam;
						string vName = (v.valuelistname == null) ? "Values" : GeneratorUtil.ToCs(v.valuelistname);                     
						string vType = Generator.TypeToCs(v.valuemasktype);

                        if (vType == "uint")
                        {
							listParams.Add(Tuple.Create("uint[]", vName));
                        }
                    }
                }
            }
			// end foreach
            

			cw.WriteLine(GeneratorUtil.ToCs(r.name) + "Request req = new " + GeneratorUtil.ToCs(r.name) + "Request ();");

			var vardec = VariableDeclaration(IdentifierName("Request"), 
			                                 SingletonSeparatedList(
				                                 VariableDeclarator(Identifier("req"), BracketedArgumentList(), 
				                                                    EqualsValueClause(
					                                                    ObjectCreationExpression(
						                                                    IdentifierName("Request"))))));

			var messageDataAccess =
				MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
									   IdentifierName("req"),
									   IdentifierName("MessageData"));

            if (isExtension)
            {
                //cw.WriteLine("req.MessageData.ExtHeader.MajorOpcode = GlobalId;");
                //cw.WriteLine("req.MessageData.ExtHeader.MinorOpcode = " + r.opcode + ";");

				var extHeaderAccess = MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
				                                             messageDataAccess,
															 IdentifierName("ExtHeader"));

				var majorOpcodeSet = ExpressionStatement(
					AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
					                     MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
						                                        extHeaderAccess,
					                                            IdentifierName("MajorOpcode")),
					                     IdentifierName("GlobalId")));

                var minorOpcodeSet = ExpressionStatement(
                    AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                         MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
					                                            extHeaderAccess,
                                                                IdentifierName("MajorOpcode")),
					                     LiteralExpression(SyntaxKind.NumericLiteralToken, Literal(int.Parse(r.opcode)))));
            }
            else
			{            
				//cw.WriteLine("req.MessageData.Header.Opcode = " + r.opcode + ";");

                var headerAccess = MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                          messageDataAccess,
                                                          IdentifierName("Header"));

                var opcodeSet = ExpressionStatement(
                    AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                         MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                                headerAccess,
                                                                IdentifierName("Opcode")),
                                         LiteralExpression(SyntaxKind.NumericLiteralToken, Literal(int.Parse(r.opcode)))));
            }

			foreach (var par in messageParams)
			{
				//cw.WriteLine("req.MessageData.@" + par + " = @" + GeneratorUtil.ToParm(par) + ";");

				var messageParamSet = ExpressionStatement(
                    AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                         MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
					                                            messageDataAccess,
					                                            IdentifierName("@" + par.Item2)),
					                     IdentifierName("@" + GeneratorUtil.ToParm(par.Item2))));
            }

			foreach (var par in listParams)
			{
				//cw.WriteLine("req.@" + par + " = @" + GeneratorUtil.ToParm(par) + ";");

                var listParamSet = ExpressionStatement(
                    AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                         MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
					                                            IdentifierName("ref"),
					                                            IdentifierName("@" + par.Item2)),
                                         IdentifierName("@" + GeneratorUtil.ToParm(par.Item2))));
            }

            if (r.Items != null)
            {
                foreach (object ob in r.Items)
                {
                    if (ob is list)
                    {
                        list l = ob as list;

						if (l.name == null || l.type != "char")
						{
							continue;
                        }

						//cw.WriteLine("req.@" + GeneratorUtil.ToCs(l.name) + " = @" + 
						//             GeneratorUtil.ToParm(GeneratorUtil.ToCs(l.name)) + ";");
                        
                        var listParamSet = ExpressionStatement(
                                    AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                                         MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                                                IdentifierName("ref"),
                                                                                IdentifierName("@" + l.name)),
							                             IdentifierName("@" + GeneratorUtil.ToParm(l.name))));
                    }
                }
            }
   
            //cw.WriteLine("c.xw.Send (req);");

			var sendInvoke = ExpressionStatement(
				InvocationExpression(
					MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
										   MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
																  IdentifierName("c"),
																  IdentifierName("xw")),
										   IdentifierName("Send"))).
				WithArgumentList(ArgumentList(
					SingletonSeparatedList(Argument(IdentifierName("req"))))));

            if (r.reply != null)
            {
				//cw.WriteLine("return c.xrr.GenerateCookie<" + GeneratorUtil.ToCs(r.name) + "Reply> ();");

				var xrrAccess =
					MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
										   IdentifierName("c"),
										   IdentifierName("xrr"));

				var returnStatement =
					ReturnStatement(
						InvocationExpression(
							MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
												   xrrAccess,
												   GenericName("GenerateCookie").
							                       WithTypeArgumentList(
								                       TypeArgumentList(
									                       SingletonSeparatedList<TypeSyntax>(
										                       IdentifierName(
											                       GeneratorUtil.ToCs(r.name) + "Reply")))))));
            }
            

            if (r.reply != null)
            {
                cw.WriteLine("public Cookie<" + GeneratorUtil.ToCs(r.name) + "Reply> " + GeneratorUtil.ToCs(r.name) +
                             " (" + parms + ");");

				return MethodDeclaration(
					GenericName("Cookie").
					WithTypeArgumentList(
						TypeArgumentList(
							SingletonSeparatedList<TypeSyntax>(
								IdentifierName(GeneratorUtil.ToCs(r.name) + "Reply")))),
					GeneratorUtil.ToCs(r.name)).
					WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword))).
					WithParameterList(ParameterList(SeparatedList<ParameterSyntax>())).     // FIXME
				    WithBody(Block());                                                      // FIXME
            }
            else
            {
                cw.WriteLine("public void " + GeneratorUtil.ToCs(r.name) + " (" + parms + ");");
            }

            throw new NotImplementedException();
        }         
    }
}
