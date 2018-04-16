using System;
using System.Linq;
using System.IO;
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
            
			AccessorDeclarationSyntax xnameGetter =
				AccessorDeclaration(SyntaxKind.GetAccessorDeclaration,
				                    Block(ReturnStatement(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(extName)))));

			PropertyDeclarationSyntax xnameProperty =
				PropertyDeclaration(List<AttributeListSyntax>(),
									TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.OverrideKeyword)),
				                    PredefinedType(Token(SyntaxKind.StringKeyword)),
				                    null, Identifier("XName"), AccessorList(SingletonList(xnameGetter)));
			classMembers.Add(xnameProperty);
            
			foreach (object o in xcb.Items)
			{
				if (o is @event)
				{
					classMembers.Add(GenEvent(o as @event));
			    }
                else if (o is @request)
                {
					classMembers.Add(GenFunction(o as @request));
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
				                     List<UsingDirectiveSyntax>(), 
				                     SingletonList<MemberDeclarationSyntax>(cd));

            CompilationUnitSyntax cu = CompilationUnit().
			    WithUsings(List(usings)).
                WithMembers(SingletonList<MemberDeclarationSyntax>(ns)).
                NormalizeWhitespace();

            using (FileStream fs = new FileStream(name + ".cs", FileMode.Create))
            using (TextWriter tw = new StreamWriter(fs))
            {
                cu.WriteTo(tw);
            }
        }

		MemberDeclarationSyntax GenEvent(@event e)
		{
			if (e.name == null)
			{
				throw new Exception("Can't have null name");            // FIXME: handle this
		    }

			string name = GeneratorUtil.ToCs(e.name);

			return EventFieldDeclaration(
				VariableDeclaration(
                    GenericName(Identifier("EventHandler"),
						 TypeArgumentList(
							 SingletonSeparatedList<TypeSyntax>(
								 IdentifierName(name + "Event")))),
					SingletonSeparatedList(VariableDeclarator(Identifier(name + "Event"))))).
                WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)));
        }

		MemberDeclarationSyntax GenFunction(@request r)
        {
			// TODO: we should be able to share a lot of this with InterfaceGenerator
            if (r.name == null)
			{
                throw new Exception("Can't have null name");            // FIXME: handle this
			}

            //TODO: share code with struct         
			List<ParameterSyntax> methodParameters = new List<ParameterSyntax>();
			List<StatementSyntax> methodBody = new List<StatementSyntax>();

			//cw.WriteLine(GeneratorUtil.ToCs(r.name) + "Request req = new " + GeneratorUtil.ToCs(r.name) + "Request ();");

			var requestType = IdentifierName(GeneratorUtil.ToCs(r.name) + "Request");

            methodBody.Add(
				LocalDeclarationStatement(
				    VariableDeclaration(
						requestType,
                        SingletonSeparatedList(
                            VariableDeclarator(Identifier("req"),
		                                       null,
                                               EqualsValueClause(
                                                   ObjectCreationExpression(
									                   requestType).
								                   WithArgumentList(ArgumentList())))))));
            

			var messageDataAccess =
				MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
									   IdentifierName("req"),
									   IdentifierName("MessageData"));

            if (isExtension)
            {            
				var extHeaderAccess = MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
				                                             messageDataAccess,
															 IdentifierName("ExtHeader"));

				methodBody.Add(ExpressionStatement(
					AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
					                     MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
						                                        extHeaderAccess,
					                                            IdentifierName("MajorOpcode")),
					                     IdentifierName("GlobalId"))));

                methodBody.Add(ExpressionStatement(
                    AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                         MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
					                                            extHeaderAccess,
                                                                IdentifierName("MajorOpcode")),
					                     LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(int.Parse(r.opcode))))));
            }
            else
			{            
                var headerAccess = MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                          messageDataAccess,
                                                          IdentifierName("Header"));

                methodBody.Add(ExpressionStatement(
                    AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                         MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                                headerAccess,
                                                                IdentifierName("Opcode")),
					                     LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(int.Parse(r.opcode))))));
			}

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

                        string paramName = "@" + GeneratorUtil.ToParm(GeneratorUtil.ToCs(f.name));

						methodParameters.Add(Parameter(Identifier(paramName)).
						                     WithType(IdentifierName(Generator.TypeToCs(f.type))));

                        methodBody.Add(ExpressionStatement(
                            AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                                 MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                                        messageDataAccess,
							                                            IdentifierName("@" + 
					                                                        GeneratorUtil.ToCs(f.name))),
							                     IdentifierName(paramName))));
                    }
                    else if (ob is list)
                    {
                        list l = ob as list;

                        if (l.name == null)
                        {
                            continue;
                        }

						string paramName = "@" + GeneratorUtil.ToParm(GeneratorUtil.ToCs(l.name));

						TypeSyntax paramType;

                        if (l.type == "char")
                        {
							paramType = PredefinedType(Token(SyntaxKind.StringKeyword));
                        }
                        else if (l.type == "CARD32")
                        {
							paramType = ArrayType(PredefinedType(Token(SyntaxKind.UIntKeyword))).
                                        WithRankSpecifiers(SingletonList(
                                            ArrayRankSpecifier(
                                                SingletonSeparatedList<ExpressionSyntax>(
                                                    OmittedArraySizeExpression()))));
                        }
						else
						{
							// FIXME: handle these
							continue;
						}

						methodParameters.Add(Parameter(Identifier(paramName)).
						                     WithType(paramType));

                        methodBody.Add(ExpressionStatement(
                            AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                                 MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                                        IdentifierName("req"),
                                                                        IdentifierName("@" + 
							                                                GeneratorUtil.ToCs(l.name))),
							                     IdentifierName(paramName))));
                    }
                    else if (ob is valueparam)
                    {
                        valueparam v = ob as valueparam;
                        string vName = (v.valuelistname == null) ? "Values" : GeneratorUtil.ToCs(v.valuelistname);
                        string vType = Generator.TypeToCs(v.valuemasktype);

                        string paramName = "@" + GeneratorUtil.ToParm(vName);

                        if (vType == "uint")
						{
							methodParameters.Add(Parameter(Identifier(paramName)).
                                           WithType(ArrayType(PredefinedType(Token(SyntaxKind.UIntKeyword))).
                                                    WithRankSpecifiers(SingletonList(
                                                        ArrayRankSpecifier(
                                                            SingletonSeparatedList<ExpressionSyntax>(
                                                                OmittedArraySizeExpression()))))));
							
                            methodBody.Add(ExpressionStatement(
                                AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                                     MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                                            IdentifierName("req"),
                                                                            IdentifierName("@" + vName)),
								                     IdentifierName(paramName))));
                        }
                    }
                }
            }
   
            //cw.WriteLine("c.xw.Send (req);");

			methodBody.Add(ExpressionStatement(
				InvocationExpression(
					MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
										   MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
																  IdentifierName("c"),
																  IdentifierName("xw")),
										   IdentifierName("Send"))).
				WithArgumentList(ArgumentList(
					SingletonSeparatedList(Argument(IdentifierName("req")))))));

            if (r.reply != null)
            {
				//cw.WriteLine("return c.xrr.GenerateCookie<" + GeneratorUtil.ToCs(r.name) + "Reply> ();");

				var xrrAccess =
					MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
										   IdentifierName("c"),
										   IdentifierName("xrr"));

				methodBody.Add(
					ReturnStatement(
						InvocationExpression(
							MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
												   xrrAccess,
												   GenericName("GenerateCookie").
							                       WithTypeArgumentList(
								                       TypeArgumentList(
									                       SingletonSeparatedList<TypeSyntax>(
										                       IdentifierName(
											                       GeneratorUtil.ToCs(r.name) + "Reply"))))))));
            }
            

            if (r.reply != null)
            {
                //cw.WriteLine("public Cookie<" + GeneratorUtil.ToCs(r.name) + "Reply> " + GeneratorUtil.ToCs(r.name) +
                //             " (" + parms + ");");

				return MethodDeclaration(
					GenericName("Cookie").
					WithTypeArgumentList(
						TypeArgumentList(
							SingletonSeparatedList<TypeSyntax>(
								IdentifierName(GeneratorUtil.ToCs(r.name) + "Reply")))),
					GeneratorUtil.ToCs(r.name)).
					WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword))).
					WithParameterList(ParameterList(SeparatedList(methodParameters))).
				    WithBody(Block(methodBody));
            }
            else
            {
				//cw.WriteLine("public void " + GeneratorUtil.ToCs(r.name) + " (" + parms + ");");

                return MethodDeclaration(
					PredefinedType(Token(SyntaxKind.VoidKeyword)),
                    GeneratorUtil.ToCs(r.name)).
                    WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword))).
                    WithParameterList(ParameterList(SeparatedList(methodParameters))).
                    WithBody(Block(methodBody));
            }
        }         
    }
}
