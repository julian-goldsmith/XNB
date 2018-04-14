using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

using Schemas;

namespace xnbgenerator.Generators
{
    public class InterfaceGenerator
    {
		public void Generate(xcb xcb, string name)
		{
			InterfaceDeclarationSyntax ids = InterfaceDeclaration("I" + name);
            
			foreach (@request req in xcb.Items.Where(r => r is @request).Cast<@request>())
            {
				ids = ids.AddMembers(GenFunction(req, name));
            }
            
            CompilationUnitSyntax cu = CompilationUnit().
                AddUsings(UsingDirective(IdentifierName("System"))).
			    WithMembers(SingletonList<MemberDeclarationSyntax>(ids)).
                NormalizeWhitespace();

			using (FileStream fs = new FileStream(name + "Iface.cs", FileMode.Create))
			using (TextWriter tw = new StreamWriter(fs))
			{
				cu.WriteTo(tw);
			}
		}
        
        private MethodDeclarationSyntax GenFunction(@request r, string name)
        {
            if (r.name == null)
			{
                return null;
			}

            //TODO: share code with struct
			List<ParameterSyntax> parameters = new List<ParameterSyntax>();

            if (r.Items != null)
            {
                foreach (object ob in r.Items)
                {
                    if (ob is field)
                    {
                        field f = ob as field;
                        if (f.name == null)
                            continue;
                        
						parameters.Add(Parameter(Identifier(f.name)).WithType(IdentifierName(f.type)));
                    }
                    else if (ob is list)
                    {
                        list l = ob as list;

						if (l.name == null)
						{
							continue;
						}

						string listName = "@" + GeneratorUtil.ToParm(GeneratorUtil.ToCs(l.name));

                        if (l.type == "char")
                        {
							parameters.Add(Parameter(Identifier(listName)).
							               WithType(PredefinedType(Token(SyntaxKind.StringKeyword))));
                        }
                        else if (l.type == "CARD32")
                        {
							parameters.Add(Parameter(Identifier(listName)).
							               WithType(ArrayType(PredefinedType(Token(SyntaxKind.UIntKeyword))).
							                        WithRankSpecifiers(SingletonList(
								                        ArrayRankSpecifier(
									                        SingletonSeparatedList<ExpressionSyntax>(
										                        OmittedArraySizeExpression()))))));
                        }
                    }
                    else if (ob is valueparam)
                    {
                        valueparam v = ob as valueparam;

						string vName = (v.valuelistname == null) ? "Values" : GeneratorUtil.ToParm(GeneratorUtil.ToCs(v.valuelistname));
						string vType = Generator.TypeToCs(v.valuemasktype);

                        if (vType == "uint")
                        {                     
							parameters.Add(Parameter(Identifier(vName)).
                                           WithType(ArrayType(PredefinedType(Token(SyntaxKind.UIntKeyword))).
                                                    WithRankSpecifiers(SingletonList(
                                                        ArrayRankSpecifier(
                                                            SingletonSeparatedList<ExpressionSyntax>(
                                                                OmittedArraySizeExpression()))))));
                        }
                    }
                }
            }
   
			if (r.reply != null)
			{
				TypeSyntax returnType = GenericName(Identifier("Cookie"), 
				                                    TypeArgumentList(SingletonSeparatedList<TypeSyntax>(
					                                    IdentifierName(GeneratorUtil.ToCs(r.name)))));
    
				return MethodDeclaration(returnType, Identifier(GeneratorUtil.ToCs(r.name))).
                    WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword))).
					WithParameterList(ParameterList(SeparatedList(parameters))).
					WithSemicolonToken(Token(SyntaxKind.SemicolonToken));
			}
            else
			{
				return MethodDeclaration(PredefinedType(Token(SyntaxKind.VoidKeyword)), 
				                         Identifier(GeneratorUtil.ToCs(r.name))).
                    WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword))).
                    WithParameterList(ParameterList(SeparatedList(parameters))).
                    WithSemicolonToken(Token(SyntaxKind.SemicolonToken));
            }
        }
    }
}
