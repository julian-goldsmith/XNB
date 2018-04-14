using System;
using System.Collections.Generic;
using Schemas;

namespace xnbgenerator.Generators
{
    public class TypesGenerator
	{
		bool isExtension;
        bool basic = false;

		public void Generate(xcb xcb, string name, string extName)
        {
			isExtension = !string.IsNullOrEmpty(extName);

            CodeWriter cwt = new CodeWriter(name + "Types.cs");

            cwt.WriteLine("using System;");
            cwt.WriteLine("using System.Collections;");
            cwt.WriteLine("using System.Collections.Generic;");
            cwt.WriteLine("using System.Runtime.InteropServices;");
            cwt.WriteLine("using Mono.Unix;");
            cwt.WriteLine("using Xnb.Protocol.Xnb;");
            cwt.WriteLine("using Xnb.Protocol.XProto;");
            cwt.WriteLine();

            cwt.WriteLine("namespace Xnb.Protocol." + name);

            cwt.WriteLine("{");

            cwt.WriteLine("#pragma warning disable 0169, 0414");

            foreach (object o in xcb.Items)
            {
                if (o == null)
                    continue;
                else if (o is @xidtype)
                    GenXidType(cwt, o as @xidtype);
                else if (o is @errorcopy)
                    GenErrorCopy(cwt, o as @errorcopy);
                else if (o is @eventcopy)
                    GenEventCopy(cwt, o as @eventcopy);
                else if (o is @struct)
                    GenStruct(cwt, o as @struct);
                else if (o is @union)
                    GenUnion(cwt, o as @union);
                else if (o is @enum)
                    GenEnum(cwt, o as @enum);
                else if (o is @event)
                    GenEvent(cwt, o as @event, name);
                else if (o is @request)
                    GenRequest(cwt, o as @request, name);
                else if (o is @error)
                    GenError(cwt, o as @error, name);
            }

            cwt.WriteLine("#pragma warning restore 0169, 0414");

            cwt.WriteLine("}");

            cwt.Close();
        }

        void GenXidType(CodeWriter cwt, @xidtype x)
        {
            if (x.name == null)
                return;

			string xName = Generator.NewTypeToCs(x.name, "Id");

            Generator.idMap.Add(xName, "uint");

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

        void GenEventCopy(CodeWriter cwt, @eventcopy e)
        {
            if (e.name == null)
                return;

            cwt.WriteLine("[Event (" + e.number + ")]");
			GenClass(cwt, Generator.NewTypeToCs(GeneratorUtil.ToCs(e.name) + "Event"), null, " : " + 
	                 GeneratorUtil.ToCs(e.@ref) + "Event");
        }

        void GenErrorCopy(CodeWriter cwt, @errorcopy e)
        {
            if (e.name == null)
                return;

            cwt.WriteLine("[Error (" + e.number + ")]");
			GenClass(cwt, Generator.NewTypeToCs(GeneratorUtil.ToCs(e.name) + "Error"), null, 
			         " : " + GeneratorUtil.ToCs(e.@ref) + "Error");
        }

        void GenError(CodeWriter cwt, @error e, string name)
        {
            if (e.name == null)
                return;

            cwt.WriteLine("[Error (" + e.number + ")]");
			GenClass(cwt, Generator.NewTypeToCs(Generator.TypeToCs(e.name) + "Error"), e.field);
        }

        void GenEvent(CodeWriter cwt, @event e, string name)
        {
            if (e.name == null)
                return;

            cwt.WriteLine("[Event (" + e.number + ")]");
			GenClass(cwt, Generator.NewTypeToCs(GeneratorUtil.ToCs(e.name) + "Event"), e.Items, " : " + "EventArgs");
        }

        void GenRequest(CodeWriter cwt, @request r, string name)
        {
            if (r.name == null)
                return;

            string inherits = isExtension ? "ExtensionRequest" : "Request";

            cwt.WriteLine("[Request (" + r.opcode + ")]");
			GenClass(cwt, Generator.NewTypeToCs(GeneratorUtil.ToCs(r.name) + "Request"), r.Items);

            if (r.reply != null)
            {
                cwt.WriteLine("[Reply (" + r.opcode + ")]");
				GenClass(cwt, Generator.NewTypeToCs(GeneratorUtil.ToCs(r.name) + "Reply"), r.reply.Items);
            }
        }

        void GenEnum(CodeWriter cwt, @enum e)
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

        void GenUnion(CodeWriter cwt, @union u)
        {
            return;
        }

        void GenStruct(CodeWriter cwt, @struct s)
        {
			if (s.name == null)
			{
				return;
			}

            //FIXME: just check to see if it contains complex (list etc.) values instead of this
			basic = !(s.name.EndsWith("Rep") || s.name.EndsWith("Req") || s.name == "DEPTH" 
			          || s.name == "SCREEN" || s.name == "STR" || s.name == "HOST");
			
			GenClass(cwt, Generator.NewTypeToCs(s.name), s.Items);

            basic = false;
        }

        void GenClass(CodeWriter cwt, string sName, object[] sItems)
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
					string fType = Generator.TypeToCs(f.type);
					offset += Generator.SizeOfType(fType);
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
  
        void GenClass(CodeWriter cwt, string sName, object[] sItems, string suffix)
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
							string lType = Generator.TypeToCs(l.type);

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
							string vType = Generator.TypeToCs(v.valuemasktype);

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
							string lType = Generator.TypeToCs(l.type);

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
							string vType = Generator.TypeToCs(v.valuemasktype);

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

						string lType = Generator.TypeToCs(l.type);

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
							cwt.WriteLine("public " + "string" + " @" + lName + ";");
						}
						else
						{
							cwt.WriteLine("public " + lType + "[]" + " @" + lName + ";");
						}

						offset += 4;
					}
					else if (ob is valueparam)
					{
						valueparam v = ob as valueparam;

						string vName = (v.valuelistname == null) ? "Values" : GeneratorUtil.ToCs(v.valuelistname);
						string vType = Generator.TypeToCs(v.valuemasktype);

						cwt.WriteLine("//public ValueList<" + vType + "> @" + vName + ";");
						cwt.WriteLine("public " + vType + "[] @" + vName + ";");

						offset += 4;
					}
				}
			}

            cwt.WriteLine("}");
            cwt.WriteLine();
        }

        int GenClassData(CodeWriter cwt, string sName, object[] sItems, string suffix, bool withOffsets)
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

						string fType = Generator.TypeToCs(f.type);

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
								offset += Generator.SizeOfType(fType);
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
