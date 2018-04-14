using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

using Schemas;

public class Generator
{   
	static bool isExtension;
	static string extName;

	public static void Generate (string fname, string name)
	{
		StreamReader sr = new StreamReader (fname);
		XmlSerializer sz = new XmlSerializer (typeof (xcb));
		xcb xcb = (xcb) sz.Deserialize (sr);

		extName = xcb.extensionxname == null ? "" : xcb.extensionxname;
		isExtension = extName != "";

		CodeWriter cw = new CodeWriter (name + ".cs");
		CodeWriter cwt = new CodeWriter (name + "Types.cs");
		CodeWriter cwi = new CodeWriter (name + "Iface.cs");

		cw.WriteLine ("using System;", cwt, cwi);
		cw.WriteLine ("using System.Collections;", cwt);
		cw.WriteLine ("using System.Collections.Generic;", cwt);
		cw.WriteLine ("using System.Runtime.InteropServices;", cwt);
		cw.WriteLine ("using Mono.Unix;", cwt);
		cw.WriteLine ("using Xnb.Protocol." + "Xnb" + ";", cwt);
		cw.WriteLine ("using Xnb.Protocol." + "XProto" + ";", cwt);
		cw.WriteLine ("", cwt, cwi);

		cw.WriteLine ("namespace Xnb");
		cwt.WriteLine ("namespace Xnb.Protocol." + name);
		
		cw.WriteLine ("{", cwt);
		cw.WriteLine ("using Protocol." + name + ";");
		cw.WriteLine ("public class " + name + " : Extension");
		cwi.WriteLine ("public interface I" + name);
		cw.WriteLine ("{", cwi);
		cw.WriteLine ("public override string XName");
		cw.WriteLine ("{");
		cw.WriteLine ("get {");
		cw.WriteLine ("return \"" + extName + "\";");
		cw.WriteLine ("}");
		cw.WriteLine ("}");
		cw.WriteLine ();

		cwt.WriteLine ("#pragma warning disable 0169, 0414");

		foreach (object o in xcb.Items) {
			if (o == null)
				continue;
			else if (o is @xidtype)
				GenXidType (cwt, o as @xidtype);
			else if (o is @errorcopy)
				GenErrorCopy (cwt, o as @errorcopy);
			else if (o is @eventcopy)
				GenEventCopy (cwt, o as @eventcopy);
			else if (o is @struct)
				GenStruct (cwt, o as @struct);
			else if (o is @union)
				GenUnion (cwt, o as @union);
			else if (o is @enum)
				GenEnum (cwt, o as @enum);
			else if (o is @event)
				GenEvent (cwt, cw, o as @event, name);
			else if (o is @request) {
				GenRequest (cwt, o as @request, name);
				GenFunction (cw, cwi, o as @request, name);
			} else if (o is @error)
				GenError (cwt, o as @error, name);
		}

		cwt.WriteLine ("#pragma warning restore 0169, 0414");

		cwi.WriteLine ("}");
		cw.WriteLine ("}");
		cw.WriteLine ("}", cwt);

		cw.Close ();
		cwt.Close ();
		cwi.Close ();
	}

	static void GenXidType (CodeWriter cwt, @xidtype x)
	{
		if (x.name == null)
			return;
        
		string xName = NewTypeToCs (x.name, "Id");

		cwt.WriteLine ("[StructLayout (LayoutKind.Explicit, Pack=1, CharSet=CharSet.Ansi)]");
		cwt.WriteLine ("public struct " + xName);
		idMap.Add (xName, "uint");
		cwt.WriteLine ("{");
		cwt.WriteLine ("[FieldOffset (0)]");
		cwt.WriteLine ("private uint Value;");
		cwt.WriteLine ();
		cwt.WriteLine ("public " + xName + " (uint value)");
		cwt.WriteLine ("{");
		cwt.WriteLine ("this.Value = value;");
		cwt.WriteLine ("}");
		cwt.WriteLine ();
		cwt.WriteLine ("public static implicit operator uint (" + xName + " x)");
		cwt.WriteLine ("{");
		cwt.WriteLine ("return x.Value;");
		cwt.WriteLine ("}");
		cwt.WriteLine ();
		cwt.WriteLine ("public static implicit operator Id (" + xName + " x)");
		cwt.WriteLine ("{");
		cwt.WriteLine ("return new Id (x);");
		cwt.WriteLine ("}");
		cwt.WriteLine ();
		//TODO: generalize
		if (xName == "AtomId") {
			cwt.WriteLine ("public static implicit operator " + xName + " (" + "Atom" + "Type" + " xt)");
			cwt.WriteLine ("{");
			cwt.WriteLine ("return new " + xName + " ((uint)xt);");
			cwt.WriteLine ("}");
			cwt.WriteLine ();
		}
		
		cwt.WriteLine ("public static explicit operator " + xName + " (Id x)");
		cwt.WriteLine ("{");
		cwt.WriteLine ("return new " + xName + " (x);");
		cwt.WriteLine ("}");

		cwt.WriteLine ("}");
		cwt.WriteLine ();
	}

	static void GenEventCopy (CodeWriter cwt, @eventcopy e)
	{
		if (e.name == null)
			return;

		cwt.WriteLine ("[Event (" + e.number + ")]");
		GenClass (cwt, NewTypeToCs (ToCs (e.name) + "Event"), null, " : " + ToCs (e.@ref) + "Event");
	}

	static void GenErrorCopy (CodeWriter cwt, @errorcopy e)
	{
		if (e.name == null)
			return;

		cwt.WriteLine ("[Error (" + e.number + ")]");
		GenClass (cwt, NewTypeToCs (ToCs (e.name) + "Error"), null, " : " + ToCs (e.@ref) + "Error");
	}

	static void GenError (CodeWriter cwt, @error e, string name)
	{
		if (e.name == null)
			return;

		cwt.WriteLine ("[Error (" + e.number + ")]");
		GenClass (cwt, NewTypeToCs (TypeToCs (e.name) + "Error"), e.field);
	}

	static void GenEvent (CodeWriter cwt, CodeWriter cw, @event e, string name)
	{
		if (e.name == null)
			return;

		cwt.WriteLine ("[Event (" + e.number + ")]");
		GenClass (cwt, NewTypeToCs (ToCs (e.name) + "Event"), e.Items, " : " + "EventArgs");
		
		cw.WriteLine ("public event " + "EventHandler<" + (ToCs (e.name) + "Event") + "> " + (ToCs (e.name) + "Event") + ";");
		cw.WriteLine ();
	}

	static void GenRequest (CodeWriter cwt, @request r, string name)
	{
		if (r.name == null)
			return;

		string inherits = isExtension ? "ExtensionRequest" : "Request";

		cwt.WriteLine ("[Request (" + r.opcode + ")]");
		GenClass (cwt, NewTypeToCs (ToCs (r.name) + "Request"), r.Items);

		if (r.reply != null) {
			cwt.WriteLine ("[Reply (" + r.opcode + ")]");
			GenClass (cwt, NewTypeToCs (ToCs (r.name) + "Reply"), r.reply.Items);
		}
	}

	static void GenFunction (CodeWriter cw, CodeWriter cwi, @request r, string name)
	{
		if (r.name == null)
			return;

		//TODO: share code with struct
		string parms = "";
		List<string> parmList1 = new List<string> ();
		List<string> parmList2 = new List<string> ();
		if (r.Items != null) {

			foreach (object ob in r.Items) {
				if (ob is field) {
					field f = ob as field;
					if (f.name == null)
						continue;

					//if (f.name.EndsWith ("_len"))
					//		continue;

					parms += ", " + TypeToCs (f.type) + " @" + ToParm (ToCs (f.name));
					parmList1.Add (ToCs (f.name));
				} else if (ob is list) {
					list l = ob as list;
					if (l.name == null)
						continue;
					if (l.type == "char") {
						parms += ", " + "string" + " @" + ToParm (ToCs (l.name));
						parmList2.Add (ToCs (l.name));
					} else if (l.type == "CARD32") {
						parms += ", " + "uint[]" + " @" + ToParm (ToCs (l.name));
						parmList2.Add (ToCs (l.name));
					}
				} else if (ob is valueparam) {
						valueparam v = ob as valueparam;
						string vName = "Values";

						if (v.valuelistname != null)
							vName = ToCs (v.valuelistname);

						string vType = TypeToCs (v.valuemasktype);

						if (vType == "uint") {
							parms += ", " + "uint[]" + " @" + ToParm (vName);
							parmList2.Add (vName);
						}
				}
			}

			parms = parms.Trim (',', ' ');
		}
  
		if (r.reply != null)
			cw.WriteLine ("public " + "Cookie<" + ToCs (r.name) + "Reply" + ">" + " " + ToCs (r.name) + " (" + parms + ")", cwi, ";");
		else
			cw.WriteLine ("public " + "void" + " " + ToCs (r.name) + " (" + parms + ")", cwi, ";");

		cw.WriteLine ("{");
        
		cw.WriteLine ("" + ToCs (r.name) + "Request req = new " + ToCs (r.name) + "Request ();");

		if (isExtension) {
			cw.WriteLine ("req.MessageData.ExtHeader.MajorOpcode = GlobalId;");
			cw.WriteLine ("req.MessageData.ExtHeader.MinorOpcode = " + r.opcode + ";");
		} else {
			cw.WriteLine ("req.MessageData.Header.Opcode = " + r.opcode + ";");
		}
		cw.WriteLine ();
        
		foreach (string par in parmList1)
			cw.WriteLine ("req.MessageData.@" + par + " = @" + ToParm (par) + ";");

		foreach (string par in parmList2)
			cw.WriteLine ("req.@" + par + " = @" + ToParm (par) + ";");

		if (r.Items != null)
			foreach (object ob in r.Items) {
				if (ob is list) {
					list l = ob as list;
					if (l.name == null)
						continue;
					if (l.type != "char")
						continue;
     
					cw.WriteLine ("req.@" + ToCs (l.name) + " = @" + ToParm (ToCs (l.name)) + ";");
				}
			}

		cw.WriteLine ();
		cw.WriteLine ("c.xw.Send (req);");
		cw.WriteLine ();
		
		if (r.reply != null) {
			cw.WriteLine ();
			cw.WriteLine ("return c.xrr.GenerateCookie" + "<" + ToCs (r.name) + "Reply" + ">" + " ();");
		}
		
		cw.WriteLine ("}");
		cw.WriteLine ();
	}
    
	static void GenEnum (CodeWriter cwt, @enum e)
	{
		if (e.name == null)
			return;

		cwt.WriteLine ("public enum " + ToCs (e.name) + " : uint");

		cwt.WriteLine ("{");

		foreach (item it in e.item) {
			cwt.WriteLine (ToCs (it.name) + ",");
		}

		cwt.WriteLine ("}");
		cwt.WriteLine ();
	}

	static void GenUnion (CodeWriter cwt, @union u)
	{
		return;
	}


	static void GenStruct (CodeWriter cwt, @struct s)
	{
		if (s.name == null)
			return;

		//FIXME: just check to see if it contains complex (list etc.) values instead of this
		basic = true;
		if (s.name.EndsWith ("Rep"))
			basic  = false;
		if (s.name.EndsWith ("Req"))
			basic  = false;
		if (s.name == "DEPTH")
			basic  = false;
		if (s.name == "SCREEN")
			basic  = false;
		if (s.name == "STR")
			basic  = false;
		if (s.name == "HOST")
			basic  = false;
		GenClass (cwt, NewTypeToCs (s.name), s.Items);
		basic = false;
	}

	static void GenClass (CodeWriter cwt, string sName, object[] sItems)
	{
		GenClass (cwt, sName, sItems, "");
	}

	//FIXME: needs to know about sizes of known structs
	//FIXME: needs to know about Size=0 structs/unions
	static int StructSize (object[] sItems)
	{
		if (sItems == null)
			return 0;

		int offset = 0;

		foreach (object ob in sItems) {
			if (ob is field) {
				field f = ob as field;
				string fType = TypeToCs (f.type);
				offset += SizeOfType (fType);
			} else if (ob is pad) {
				pad p = ob as pad;

				int padding = Int32.Parse (p.bytes);
				offset += padding;
			}
		}

		return offset;
	}

	static bool basic = false;
	static void GenClass (CodeWriter cwt, string sName, object[] sItems, string suffix)
	{
		Dictionary<string,int> sizeParams = new Dictionary<string,int> ();
  
		bool basicStruct = basic;

		if (sName == "GContext")
			basicStruct = true;

		if (sName == "Drawable")
			basicStruct = true;

		if (sName == "Fontable")
			basicStruct = true;

		//if (sName == "ClientMessageData")
		//	basicStruct = true;
		
		bool isRequest = sName.EndsWith ("Request");
		bool isEvent = sName.EndsWith ("Event");

		if (!basicStruct) {
    		int structSize = StructSize (sItems);
    		cwt.WriteLine ("[StructLayout (LayoutKind.Explicit, Pack=1, CharSet=CharSet.Ansi, Size=" + structSize + ")]");
    		cwt.WriteLine ("public struct @" + sName + "Data");
    		cwt.WriteLine ("{");
    		if (isRequest) { //TODO: generate one or the other
    			cwt.WriteLine ("[FieldOffset (0)]");
    			cwt.WriteLine ("public Request Header;");
    			cwt.WriteLine ("[FieldOffset (0)]");
    			cwt.WriteLine ("public ExtensionRequest ExtHeader;");
    		}

    		GenClassData (cwt, sName + "Data", sItems, "", true);
    		cwt.WriteLine ("}");
    		cwt.WriteLine ();
		}

		if (basicStruct) {
			int structSize = StructSize (sItems);
			cwt.WriteLine ("[StructLayout (LayoutKind.Explicit, Pack=1, CharSet=CharSet.Ansi, Size=" + structSize + ")]");
			cwt.WriteLine ("public struct @" + sName + suffix);
		} else {
			//TODO: clean up hack
			if (suffix == "")
				suffix += " : ";
			else
				suffix += ", ";

			suffix += "IMessagePart";
			cwt.WriteLine ("public class @" + sName + suffix);
		}
		cwt.WriteLine ("{");
		if (!basicStruct) {
			cwt.WriteLine ("public " + sName + "Data" + " MessageData;");
		}
		
		int offset = GenClassData (cwt, sName, sItems, "", basicStruct);

		if (!basicStruct) {
			cwt.WriteLine ("public int Read (IntPtr ptr)");
			cwt.WriteLine ("{");
			cwt.WriteLine ("int offset = 0;");
			cwt.WriteLine ("unsafe {");
			cwt.WriteLine ("MessageData = *(" + sName + "Data" + "*)ptr;");
			cwt.WriteLine ("offset += sizeof (" + sName + "Data" + ");");
			cwt.WriteLine ("}");

			if (sItems != null)
				foreach (object ob in sItems) {
					if (ob is list) {
						list l = ob as list;

						string lName = ToCs (l.name);
						string lType = TypeToCs (l.type);
						if (lName == sName) {
							Console.Error.WriteLine ("Warning: list field renamed: " + lName);
							lName = "Values";
						}
						if (l.type == "CHAR2B" || lType == "sbyte") {
							cwt.WriteLine ("//if (@" + lName + " != null)");
							cwt.WriteLine ("//yield return XMarshal.Do (@" + lName + ");");
							//cwt.WriteLine (lName + " = Marshal.PtrToStringAnsi (new IntPtr ((int)ptr + offset), MessageData.@" + ToCs (l.fieldref) + ");");
							cwt.WriteLine ("//" + lName + " = Marshal.PtrToStringAnsi (new IntPtr ((int)ptr + offset), MessageData.@" + (lName + "Len") + ");");
							cwt.WriteLine ("//offset += " + (lName + "Len") + ";");
						}

					} else if (ob is valueparam) {
						valueparam v = ob as valueparam;
						string vName = "Values";

						if (v.valuelistname != null)
							vName = ToCs (v.valuelistname);

						string vType = TypeToCs (v.valuemasktype);

						if (vType == "uint") {
							cwt.WriteLine ("//if (@" + vName + " != null)");
							cwt.WriteLine ("//yield return XMarshal.Do (ref @" + vName + ");");
						}
					}
				}

			cwt.WriteLine ("return offset;");
			cwt.WriteLine ("}");
			cwt.WriteLine ();


			cwt.WriteLine ("IEnumerator IEnumerable.GetEnumerator () { return GetEnumerator (); }");
			cwt.WriteLine ();
			cwt.WriteLine ("public IEnumerator<IOVector> GetEnumerator ()");
			cwt.WriteLine ("{");
			cwt.WriteLine ("yield return XMarshal.Do (ref MessageData);");

			if (sItems != null)
				foreach (object ob in sItems) {
					if (ob is list) {
						list l = ob as list;

						string lName = ToCs (l.name);
						string lType = TypeToCs (l.type);
						if (lName == sName) {
							Console.Error.WriteLine ("Warning: list field renamed: " + lName);
							lName = "Values";
						}
						if (l.type == "CHAR2B" || lType == "sbyte" || lType == "byte") {
							cwt.WriteLine ("if (@" + lName + " != null)");
							cwt.WriteLine ("yield return XMarshal.Do (ref @" + lName + ");");
						}
					} else if (ob is valueparam) {
						valueparam v = ob as valueparam;
						string vName = "Values";

						if (v.valuelistname != null)
							vName = ToCs (v.valuelistname);

						string vType = TypeToCs (v.valuemasktype);

						if (vType == "uint") {
							cwt.WriteLine ("if (@" + vName + " != null)");
							cwt.WriteLine ("yield return XMarshal.Do (ref @" + vName + ");");
						}
					}
				}

			cwt.WriteLine ("}");
			cwt.WriteLine ();
		}

		if (sItems != null)
		foreach (object ob in sItems) {
			if (ob is list) {
				list l = ob as list;

				string lName = ToCs (l.name);
				if (lName == sName) {
					Console.Error.WriteLine ("Warning: list field renamed: " + lName);
					lName = "Values";
				}

				string lType = TypeToCs (l.type);
				if (!sizeParams.ContainsKey (l.name)) {
					Console.Error.WriteLine ("Warning: No length given for " + lName);
					cwt.WriteLine ("//FIXME: No length given");
				} else {
					if (l.type == "CHAR2B" || lType == "sbyte")
						cwt.WriteLine ("//[MarshalAs (UnmanagedType.LPStr, SizeParamIndex=" + sizeParams[l.name] + ")]");
					else
						cwt.WriteLine ("[MarshalAs (UnmanagedType.LPArray, SizeParamIndex=" + sizeParams[l.name] + ")]");
				}
				
				if (l.type == "CHAR2B" || lType == "sbyte")
					cwt.WriteLine ("public " + "string" + " @" + lName + ";");
				else
					cwt.WriteLine ("public " + lType + "[]" + " @" + lName + ";");

				offset += 4;
			} else if (ob is valueparam) {
				valueparam v = ob as valueparam;
				string vName = (v.valuelistname == null) ? "Values" : ToCs(v.valuelistname);               
				string vType = TypeToCs (v.valuemasktype);
				
				cwt.WriteLine ("//public ValueList<" + vType + "> @" + vName + ";");
				cwt.WriteLine ("public " + vType + "[] @" + vName + ";");
				offset += 4;
			}
		}
		cwt.WriteLine ("}");
		cwt.WriteLine ();
	}

	static int GenClassData (CodeWriter cwt, string sName, object[] sItems, string suffix, bool withOffsets)
	{
		string sizeString = "";

		//FIXME: EndsWith hack
		bool isRequest = sName.EndsWith ("RequestData");
		bool isEvent = sName.EndsWith ("EventData");
		bool isError = sName.EndsWith ("ErrorData");
		//FIXME: Rep shouldn't have offsets/inherits
		bool isReply = sName.EndsWith ("ReplyData");

		bool isStruct = (!isRequest && !isEvent && !isError && !isReply && !sName.EndsWith ("RepData"));


		if (sName.EndsWith ("EventData") || sName.EndsWith ("ErrorData"))
			sizeString = ", Size=" + 28;

		Dictionary<string,int> sizeParams = new Dictionary<string,int> ();
		
		int index = 0, offset = 0;

		if (sItems != null) {

			if (isRequest)
				offset += 4;

			if (isError || isEvent)
				offset += 4;

			if (isReply && !sName.EndsWith ("RepData"))
				offset += 8;

			bool first = true;

			foreach (object ob in sItems) {
				//bool isData = (offset == startOffset) && (isReply || (isRequest && !isExtension));
				bool isData = first && (isReply || (isRequest && !isExtension));
				first = false;

				if (ob is field) {
					field f = ob as field;
					if (f.name != null) {
						string fName = ToCs (f.name);
						if (fName == sName || fName + "Data" == sName) {
							Console.Error.WriteLine ("Warning: field renamed: " + fName);
							fName = "Value";
						}

						string fType = TypeToCs (f.type);

						//in non-extension requests, the data field carries the first element
						if (withOffsets) {
							if (isData)
								cwt.WriteLine ("[FieldOffset (" + 1 + ")]");
							else {
								cwt.WriteLine ("[FieldOffset (" + offset + ")]");
								offset += SizeOfType (fType);
							}
						}
      
						if (withOffsets) {
							cwt.WriteLine ("public " + fType + " @" + fName + ";");
						} else {
							cwt.WriteLine ("public " + fType + " @" + fName);
							cwt.WriteLine ("{");
							cwt.WriteLine ("get {");
							cwt.WriteLine ("return MessageData.@" + fName + ";");
							cwt.WriteLine ("} set {");
							cwt.WriteLine ("MessageData.@" + fName + " = value;");
							cwt.WriteLine ("}");
							cwt.WriteLine ("}");
						}
						index++;
					}
				} else if (ob is pad) {
					if (!withOffsets)
						continue;

					pad p = ob as pad;

					int padding = Int32.Parse (p.bytes);

					if (isData)
						padding--;

					if (padding > 0) {
						cwt.WriteLine ("//byte[" + padding + "]");
						offset += padding;
					}
				}
			}
		}

		return offset;
	}

	static int SizeOfType (string t)
	{
		switch (t) {
			case "sbyte":
				case "byte":
				case "bool":
				return 1;
			case "short":
				case "ushort":
				case "char":
				case "uchar":
				return 2;
			case "int":
				case "uint":
				return 4;
			case "long":
				case "ulong":
				return 8;
		}

		//FIXME: these are hacks, add SizeMap stanza to TypeMap
		if (t.EndsWith ("Id"))
			return 4;

		if (idMap.ContainsKey (t))
			return SizeOfType (idMap[t]);

		Console.Error.WriteLine ("Error: Size not known for type: " + t);
		return 0;
	}

	static string Studlify (string name)
	{
		string r = "";

		foreach (string s in name.Split ('_'))
			r += Char.ToUpper (s[0]) + s.Substring (1);

		return r;
	}

	//TODO: numbers etc? Write tests
	//GetXidRange GetXIDRange GetX, numbers etc.
	static string Destudlify (string s)
	{
		string o = "";

		bool xC = true;
		bool Cx = false;

		for (int i = 0 ; i != s.Length ; i++) {

			if (i != 0)
				xC = Char.IsLower (s[i-1]);

			if (i != s.Length - 1)
				Cx = Char.IsLower (s[i+1]);

			if (i == 0) {
				o += Char.ToLower (s[i]);
				continue;
			}

			if (Char.IsUpper (s[i]))
				if (Cx || xC && !Cx)
					o += '_';

			o += Char.ToLower (s[i]);
		}

		return o;
	}

	static string ToParm (string name)
	{
		return name.Substring (0, 1).ToLower () + name.Substring (1, name.Length - 1);
	}

	static string ToCs (string name)
	{
		return Studlify (Destudlify (name));
	}

	static string TypeToCs (string name)
	{
		if (typeMap.ContainsKey (name))
			return typeMap[name];

		Console.Error.WriteLine ("Warning: typeMap doesn't contain " + name);
		return ToCs (name);
	}

	static string NewTypeToCs (string name)
	{
		return NewTypeToCs (name, "");
	}

	static string NewTypeToCs (string name, string suffix)
	{
		//TODO: error reporting?

		string cs;

		if (typeMap.ContainsKey (name)) {
			cs = typeMap[name];
			if (cs.ToLower () == name.ToLower ()) {
				caseMap[name.ToLower ()] = cs;
			} else {
				//this type is already defined as a primitive
				return NewTypeToCs (name + "_fake");
			}
		} else
		{
			cs = ToCs (name) + suffix;
			typeMap[name] = cs;
		}

		return cs;
	}

	static Dictionary<string,string> idMap = new Dictionary<string,string> ();
	static Dictionary<string,string> typeMap = new Dictionary<string,string> ();
	//TODO: use caseMap for field case corrections?
	static Dictionary<string,string> caseMap = new Dictionary<string,string> ();
	public static void LoadTypeMap (string fname)
	{
		char[] delim = {'\t'};

		StreamReader sr = new StreamReader (new FileStream (fname, FileMode.Open, FileAccess.Read));

		string ln;
		while ((ln = sr.ReadLine ()) != null) {
			ln = ln.Trim ();

			if (ln == "")
				continue;

			if (ln.StartsWith ("#"))
				continue;

			string[] parts = ln.Split (delim);
			if (parts.Length != 2) {
				Console.Error.WriteLine ("Error: Bad type map file: " + fname);
				continue;
			}

			string key = parts[0].Trim ();
			string value = parts[1].Trim ();

			typeMap[key] = value;
		}

		sr.Close ();
	}

	public static void SaveTypeMap (string fname)
	{
		StreamWriter sw = new StreamWriter (new FileStream (fname, FileMode.Create, FileAccess.Write));
		sw.WriteLine ("#TypeMap for " + "[]");
		sw.WriteLine ("#Generated by xnb-generator");
		sw.WriteLine ();

		foreach (KeyValuePair<string,string> entry in typeMap)
			sw.WriteLine (entry.Key + "\t" + entry.Value);

		sw.Close ();
	}
}
