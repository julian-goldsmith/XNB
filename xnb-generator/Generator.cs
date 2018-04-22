using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

using Schemas;
using xnbgenerator.Generators;
using xnbgenerator.Generators.Types;

public class Generator
{
	public static void Generate(TypeMap typeMap, string fname, string name)
	{
		StreamReader sr = new StreamReader(fname);
		XmlSerializer sz = new XmlSerializer(typeof(xcb));
		xcb xcb = (xcb) sz.Deserialize(sr);

		string extName = xcb.extensionxname ?? "";

		TypesGenerator tg = new TypesGenerator(typeMap);
		tg.Generate(xcb, name, extName);

		InterfaceGenerator ig = new InterfaceGenerator(typeMap);
		ig.Generate(xcb, name);

		ClassGenerator cg = new ClassGenerator(typeMap);
		cg.Generate(xcb, name, extName);
	}
 }
