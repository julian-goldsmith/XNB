using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

using Schemas;
using xnbgenerator.Generators.Types;

namespace xnbgenerator.Generators
{
    public class TypesGenerator
	{
		bool isExtension;
        bool basic = false;

		private TypeMap typeMap;

		public TypesGenerator(TypeMap _typeMap)
		{
			typeMap = _typeMap;
		}

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
				if (o is @xidtype)
				{
					classMembers.Add(GenXidType(o as @xidtype));
				}
				else if (o is @errorcopy)
				{
					classMembers.Add(GenErrorCopy(o as @errorcopy));
				}
				else if (o is @eventcopy)
				{
					classMembers.Add(GenEventCopy(o as @eventcopy));
				}
				else if (o is @struct)
				{
					classMembers.Add(GenStruct(o as @struct));
				}
				else if (o is @union)
				{
					classMembers.Add(GenUnion(o as @union));
				}
				else if (o is @enum)
				{
					classMembers.Add(GenEnum(o as @enum));
				}
				else if (o is @event)
				{
					classMembers.Add(GenEvent(o as @event));
				}
				else if (o is @request)
				{
					classMembers.Add(GenRequest(o as @request));
				}
				else if (o is @error)
				{
					classMembers.Add(GenError(o as @error));
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

		MemberDeclarationSyntax GenXidType(@xidtype x)
        {
			if (x.name == null)
			{
				return null;                    // FIXME
			}

			string xName = typeMap.NewTypeToCs(x.name, "Id");

			/*
             * 
                AttributeList(
                    SingletonSeparatedList<AttributeSyntax>(
                        Attribute(
                            IdentifierName("StructLayout"), 
                            AttributeArgumentList(
                                SeparatedList(new[] {
                                    AttributeArgument(
                                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                               IdentifierName("LayoutKind"),
                                                               IdentifierName("Explicit")))
                            }))))))*/
            
			FieldDeclarationSyntax valueDec = 
				FieldDeclaration(SingletonList(
    				AttributeList(SeparatedList(new[] { 
    				    Attribute(
    					    IdentifierName("FieldOffset"), 
    					    AttributeArgumentList(
    						    SingletonSeparatedList(
    							    AttributeArgument(
    								    LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(0))))))
    			    }))),
    				TokenList(Token(SyntaxKind.PrivateKeyword)),
    				VariableDeclaration(PredefinedType(Token(SyntaxKind.UIntKeyword)),
    									SeparatedList(new[] { VariableDeclarator("Value") })));



            cwt.WriteLine("[StructLayout (LayoutKind.Explicit, Pack=1, CharSet=CharSet.Ansi)]");
            cwt.WriteLine("public struct " + xName);
            cwt.WriteLine("{");
            cwt.WriteLine("[FieldOffset (0)]");
            cwt.WriteLine("private uint Value;");
            cwt.WriteLine();
            cwt.WriteLine("public " + xName + " (uint value)");
            cwt.WriteLine("{");
            cwt.WriteLine("this.Value = value;");
            cwt.WriteLine("}");
            cwt.WriteLine();
            cwt.WriteLine("public static implicit operator uint (" + xName + " x)");
            cwt.WriteLine("{");
            cwt.WriteLine("return x.Value;");
            cwt.WriteLine("}");
            cwt.WriteLine();
            cwt.WriteLine("public static implicit operator Id (" + xName + " x)");
            cwt.WriteLine("{");
            cwt.WriteLine("return new Id (x);");
            cwt.WriteLine("}");
            cwt.WriteLine();

            //TODO: generalize
            if (xName == "AtomId")
            {
                cwt.WriteLine("public static implicit operator " + xName + " (AtomType xt)");
                cwt.WriteLine("{");
                cwt.WriteLine("return new " + xName + " ((uint) xt);");
                cwt.WriteLine("}");
                cwt.WriteLine();
            }

            cwt.WriteLine("public static explicit operator " + xName + " (Id x)");
            cwt.WriteLine("{");
            cwt.WriteLine("return new " + xName + " (x);");
            cwt.WriteLine("}");

            cwt.WriteLine("}");
            cwt.WriteLine();
        }

		AttributeListSyntax BuildSingleAttributeList(string name, string value)
		{
			return AttributeList(
                SingletonSeparatedList(
                    Attribute(
                        IdentifierName(name),
                        AttributeArgumentList(
                            SingletonSeparatedList(
                                AttributeArgument(
									LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(value))))))));
		}

		BaseListSyntax BuildSingleBaseList(string baseName)
		{
            return
                BaseList(
                    SingletonSeparatedList<BaseTypeSyntax>(
                        SimpleBaseType(
                            IdentifierName(baseName))));
		}

		MemberDeclarationSyntax GenEventCopy(@eventcopy e)
        {
			if (e.name == null)
			{
				throw new InvalidOperationException("Name cannot be null");
			}

			return
				ClassDeclaration(typeMap.NewTypeToCs(GeneratorUtil.ToCs(e.name) + "Event")).
				    AddAttributeLists(BuildSingleAttributeList("Event", e.number)).
					AddBaseListTypes(SimpleBaseType(IdentifierName(GeneratorUtil.ToCs(e.@ref) + "Event")));
        }

		MemberDeclarationSyntax GenErrorCopy(@errorcopy e)
        {
			if (e.name == null)
			{
				throw new InvalidOperationException("Name cannot be null");
			}
                
            return
                ClassDeclaration(typeMap.NewTypeToCs(GeneratorUtil.ToCs(e.name) + "Error")).
                    AddAttributeLists(BuildSingleAttributeList("Error", e.number)).
			        AddBaseListTypes(SimpleBaseType(IdentifierName(GeneratorUtil.ToCs(e.@ref) + "Error")));
        }

		MemberDeclarationSyntax GenError(@error e)
        {
			if (e.name == null)
			{
                throw new InvalidOperationException("Name cannot be null");
			}

			return GenClass(typeMap.NewTypeToCs(typeMap.TypeToCs(e.name) + "Error"), e.field).
			    AddAttributeLists(BuildSingleAttributeList("Error", e.number));
        }

		MemberDeclarationSyntax GenEvent(@event e)
        {
            if (e.name == null)
			{            
                throw new InvalidOperationException("Name cannot be null");
			}

			return GenClass(typeMap.NewTypeToCs(typeMap.TypeToCs(e.name) + "Event"), e.Items, " : " + "EventArgs").
                AddAttributeLists(BuildSingleAttributeList("Event", e.number));
        }

		MemberDeclarationSyntax GenRequest(@request r)
        {
            if (r.name == null)
                return;

            string inherits = isExtension ? "ExtensionRequest" : "Request";

            cwt.WriteLine("[Request (" + r.opcode + ")]");
			GenClass(cwt, typeMap.NewTypeToCs(GeneratorUtil.ToCs(r.name) + "Request"), r.Items);

            if (r.reply != null)
            {
                cwt.WriteLine("[Reply (" + r.opcode + ")]");
				GenClass(cwt, typeMap.NewTypeToCs(GeneratorUtil.ToCs(r.name) + "Reply"), r.reply.Items);
            }
        }

		MemberDeclarationSyntax GenEnum(@enum e)
        {
            if (e.name == null)
                return;

			cwt.WriteLine("public enum " + GeneratorUtil.ToCs(e.name) + " : uint");

            cwt.WriteLine("{");

            foreach (item it in e.item)
            {
				cwt.WriteLine(GeneratorUtil.ToCs(it.name) + ",");
            }

            cwt.WriteLine("}");
            cwt.WriteLine();
        }

		MemberDeclarationSyntax GenUnion(@union u)
        {
            return;
        }

		MemberDeclarationSyntax GenStruct(@struct s)
        {
			if (s.name == null)
			{
				return;
			}

            //FIXME: just check to see if it contains complex (list etc.) values instead of this
			basic = !(s.name.EndsWith("Rep") || s.name.EndsWith("Req") || s.name == "DEPTH" 
			          || s.name == "SCREEN" || s.name == "STR" || s.name == "HOST");
			
			GenClass(cwt, typeMap.NewTypeToCs(s.name), s.Items);

            basic = false;
        }

		ClassDeclarationSyntax GenClass(string sName, object[] sItems)
        {
            GenClass(cwt, sName, sItems, "");
        }

        //FIXME: needs to know about sizes of known structs
        //FIXME: needs to know about Size=0 structs/unions
        int StructSize(object[] sItems)
        {
            if (sItems == null)
                return 0;

            int offset = 0;

            foreach (object ob in sItems)
            {
                if (ob is field)
                {
                    field f = ob as field;
					string fType = typeMap.TypeToCs(f.type);
					offset += typeMap.SizeOfType(fType);
                }
                else if (ob is pad)
                {
                    pad p = ob as pad;

                    int padding = Int32.Parse(p.bytes);
                    offset += padding;
                }
            }

            return offset;
        }
  
		ClassDeclarationSyntax GenClass(string sName, object[] sItems, string suffix)
        {
            Dictionary<string, int> sizeParams = new Dictionary<string, int>();

            bool basicStruct = basic;

            if (sName == "GContext")
                basicStruct = true;

            if (sName == "Drawable")
                basicStruct = true;

            if (sName == "Fontable")
                basicStruct = true;

            //if (sName == "ClientMessageData")
            //  basicStruct = true;

            bool isRequest = sName.EndsWith("Request");
            bool isEvent = sName.EndsWith("Event");

            if (!basicStruct)
            {
                int structSize = StructSize(sItems);
                cwt.WriteLine("[StructLayout (LayoutKind.Explicit, Pack=1, CharSet=CharSet.Ansi, Size=" + structSize + ")]");
                cwt.WriteLine("public struct @" + sName + "Data");
                cwt.WriteLine("{");
                if (isRequest)
                { //TODO: generate one or the other
                    cwt.WriteLine("[FieldOffset (0)]");
                    cwt.WriteLine("public Request Header;");
                    cwt.WriteLine("[FieldOffset (0)]");
                    cwt.WriteLine("public ExtensionRequest ExtHeader;");
                }

                GenClassData(cwt, sName + "Data", sItems, "", true);
                cwt.WriteLine("}");
                cwt.WriteLine();
            }

            if (basicStruct)
            {
                int structSize = StructSize(sItems);
                cwt.WriteLine("[StructLayout (LayoutKind.Explicit, Pack=1, CharSet=CharSet.Ansi, Size=" + structSize + ")]");
                cwt.WriteLine("public struct @" + sName + suffix);
            }
            else
            {
                //TODO: clean up hack
                if (suffix == "")
                    suffix += " : ";
                else
                    suffix += ", ";

                suffix += "IMessagePart";
                cwt.WriteLine("public class @" + sName + suffix);
            }
            cwt.WriteLine("{");
            if (!basicStruct)
            {
                cwt.WriteLine("public " + sName + "Data" + " MessageData;");
            }

            int offset = GenClassData(cwt, sName, sItems, "", basicStruct);

            if (!basicStruct)
            {
                cwt.WriteLine("public int Read (IntPtr ptr)");
                cwt.WriteLine("{");
                cwt.WriteLine("int offset = 0;");
                cwt.WriteLine("unsafe {");
                cwt.WriteLine("MessageData = *(" + sName + "Data" + "*)ptr;");
                cwt.WriteLine("offset += sizeof (" + sName + "Data" + ");");
                cwt.WriteLine("}");

				if (sItems != null)
				{
					foreach (object ob in sItems)
					{
						if (ob is list)
						{
							list l = ob as list;

							string lName = GeneratorUtil.ToCs(l.name);
							string lType = typeMap.TypeToCs(l.type);

							if (lName == sName)
							{
								Console.Error.WriteLine("Warning: list field renamed: " + lName);
								lName = "Values";
							}

							if (l.type == "CHAR2B" || lType == "sbyte")
							{
								cwt.WriteLine("//if (@" + lName + " != null)");
								cwt.WriteLine("//yield return XMarshal.Do (@" + lName + ");");
								//cwt.WriteLine (lName + " = Marshal.PtrToStringAnsi (new IntPtr ((int)ptr + offset), "
								// "MessageData.@" + ToCs (l.fieldref) + ");");
								cwt.WriteLine("//" + lName + " = Marshal.PtrToStringAnsi (new IntPtr ((int)ptr + offset), MessageData.@" + 
								              (lName + "Len") + ");");
								cwt.WriteLine("//offset += " + (lName + "Len") + ";");
							}                     
						}
						else if (ob is valueparam)
						{
							valueparam v = ob as valueparam;

							string vName = (v.valuelistname == null) ? "Values" : GeneratorUtil.ToCs(v.valuelistname);                     
							string vType = typeMap.TypeToCs(v.valuemasktype);

							if (vType == "uint")
							{
								cwt.WriteLine("//if (@" + vName + " != null)");
								cwt.WriteLine("//yield return XMarshal.Do (ref @" + vName + ");");
							}
						}
					}
				}

                cwt.WriteLine("return offset;");
                cwt.WriteLine("}");
                cwt.WriteLine();
                
                cwt.WriteLine("IEnumerator IEnumerable.GetEnumerator () { return GetEnumerator (); }");
                cwt.WriteLine();
                cwt.WriteLine("public IEnumerator<IOVector> GetEnumerator ()");
                cwt.WriteLine("{");
                cwt.WriteLine("yield return XMarshal.Do (ref MessageData);");

				if (sItems != null)
				{
					foreach (object ob in sItems)
					{
						if (ob is list)
						{
							list l = ob as list;

							string lName = GeneratorUtil.ToCs(l.name);
							string lType = typeMap.TypeToCs(l.type);

							if (lName == sName)
							{
								Console.Error.WriteLine("Warning: list field renamed: " + lName);
								lName = "Values";
							}
							if (l.type == "CHAR2B" || lType == "sbyte" || lType == "byte")
							{
								cwt.WriteLine("if (@" + lName + " != null)");
								cwt.WriteLine("yield return XMarshal.Do (ref @" + lName + ");");
							}
						}
						else if (ob is valueparam)
						{
							valueparam v = ob as valueparam;

							string vName = (v.valuelistname == null) ? "Values" : GeneratorUtil.ToCs(v.valuelistname);                     
							string vType = typeMap.TypeToCs(v.valuemasktype);

							if (vType == "uint")
							{
								cwt.WriteLine("if (@" + vName + " != null)");
								cwt.WriteLine("yield return XMarshal.Do (ref @" + vName + ");");
							}
						}
					}
				}

                cwt.WriteLine("}");
                cwt.WriteLine();
            }

			if (sItems != null)
			{
				foreach (object ob in sItems)
				{
					if (ob is list)
					{
						list l = ob as list;

						string lName = GeneratorUtil.ToCs(l.name);

						if (lName == sName)
						{
							Console.Error.WriteLine("Warning: list field renamed: " + lName);
							lName = "Values";
						}

						string lType = typeMap.TypeToCs(l.type);

						if (!sizeParams.ContainsKey(l.name))
						{
							Console.Error.WriteLine("Warning: No length given for " + lName);
							cwt.WriteLine("//FIXME: No length given");
						}
						else if (l.type == "CHAR2B" || lType == "sbyte")
						{
							cwt.WriteLine("//[MarshalAs (UnmanagedType.LPStr, SizeParamIndex=" + sizeParams[l.name] + ")]");
						}
						else
						{
							cwt.WriteLine("[MarshalAs (UnmanagedType.LPArray, SizeParamIndex=" + sizeParams[l.name] + ")]");
						}

						if (l.type == "CHAR2B" || lType == "sbyte")
						{
							cwt.WriteLine("public string @" + lName + ";");
						}
						else
						{
							cwt.WriteLine("public " + lType + "[] @" + lName + ";");
						}

						offset += 4;
					}
					else if (ob is valueparam)
					{
						valueparam v = ob as valueparam;

						string vName = (v.valuelistname == null) ? "Values" : GeneratorUtil.ToCs(v.valuelistname);
						string vType = typeMap.TypeToCs(v.valuemasktype);

						cwt.WriteLine("//public ValueList<" + vType + "> @" + vName + ";");
						cwt.WriteLine("public " + vType + "[] @" + vName + ";");

						offset += 4;
					}
				}
			}

            cwt.WriteLine("}");
            cwt.WriteLine();
        }

		Tuple<MemberDeclarationSyntax, int> GenClassData(string sName, object[] sItems, string suffix, bool withOffsets)
        {
            string sizeString = "";

            //FIXME: EndsWith hack
            bool isRequest = sName.EndsWith("RequestData");
            bool isEvent = sName.EndsWith("EventData");
            bool isError = sName.EndsWith("ErrorData");

            //FIXME: Rep shouldn't have offsets/inherits
            bool isReply = sName.EndsWith("ReplyData");

            bool isStruct = (!isRequest && !isEvent && !isError && !isReply && !sName.EndsWith("RepData"));

			if (sName.EndsWith("EventData") || sName.EndsWith("ErrorData"))
			{
				sizeString = ", Size=" + 28;
			}

            Dictionary<string, int> sizeParams = new Dictionary<string, int>();

            int offset = 0;

            if (sItems != null)
            {            
                if (isRequest)
                    offset += 4;

                if (isError || isEvent)
                    offset += 4;

                if (isReply && !sName.EndsWith("RepData"))
                    offset += 8;

                bool first = true;

                foreach (object ob in sItems)
                {
                    bool isData = first && (isReply || (isRequest && !isExtension));
                    first = false;

                    if (ob is field)
                    {
                        field f = ob as field;

						if (f.name == null)
						{
							continue;
						}

                        string fName = GeneratorUtil.ToCs(f.name);
                        if (fName == sName || fName + "Data" == sName)
                        {
                            Console.Error.WriteLine("Warning: field renamed: " + fName);
                            fName = "Value";
                        }

						string fType = typeMap.TypeToCs(f.type);

						//in non-extension requests, the data field carries the first element
						if (withOffsets)
						{
							if (isData)
							{
								cwt.WriteLine("[FieldOffset (" + 1 + ")]");
						    }
                            else
                            {
                                cwt.WriteLine("[FieldOffset (" + offset + ")]");
								offset += typeMap.SizeOfType(fType);
                            }
                        }

                        if (withOffsets)
                        {
                            cwt.WriteLine("public " + fType + " @" + fName + ";");
                        }
                        else
                        {
                            cwt.WriteLine("public " + fType + " @" + fName);
                            cwt.WriteLine("{");
                            cwt.WriteLine("get {");
                            cwt.WriteLine("return MessageData.@" + fName + ";");
                            cwt.WriteLine("} set {");
                            cwt.WriteLine("MessageData.@" + fName + " = value;");
                            cwt.WriteLine("}");
                            cwt.WriteLine("}");
                        }
                    }
                    else if (ob is pad)
                    {
						if (!withOffsets)
						{
							continue;
						}

                        pad p = ob as pad;

                        int padding = Int32.Parse(p.bytes);

						if (isData)
						{
							padding--;
						}

                        if (padding > 0)
                        {
                            cwt.WriteLine("//byte[" + padding + "]");
                            offset += padding;
                        }
                    }
                }
            }

            return offset;
        }
    }
}
