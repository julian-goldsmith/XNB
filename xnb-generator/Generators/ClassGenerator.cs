using System;
using System.Collections.Generic;
using Schemas;

namespace xnbgenerator.Generators
{
    public class ClassGenerator
    {
        public void Generate(xcb xcb, string name, string extName)
		{
            CodeWriter cw = new CodeWriter(name + ".cs");

            cw.WriteLine("using System;");
            cw.WriteLine("using System.Collections;");
            cw.WriteLine("using System.Collections.Generic;");
            cw.WriteLine("using System.Runtime.InteropServices;");
            cw.WriteLine("using Mono.Unix;");
            cw.WriteLine("using Xnb.Protocol.Xnb;");
            cw.WriteLine("using Xnb.Protocol.XProto;");
            cw.WriteLine();

            cw.WriteLine("namespace Xnb");

            cw.WriteLine("{");
            cw.WriteLine("using Protocol." + name + ";");
            cw.WriteLine("public class " + name + " : Extension");
            cw.WriteLine("{");
            cw.WriteLine("public override string XName");
            cw.WriteLine("{");
            cw.WriteLine("get {");
            cw.WriteLine("return \"" + extName + "\";");
            cw.WriteLine("}");
            cw.WriteLine("}");
            cw.WriteLine();
            
			foreach (object o in xcb.Items)
			{
				if (o == null)
				{
					continue;
			    }
				else if (o is @event)
				{
					GenEvent(cw, o as @event, name);
			    }
                else if (o is @request)
                {
                    GenFunction(cw, o as @request, name);
                }
            }
			             
            cw.WriteLine("}");
            cw.WriteLine("}");

            cw.Close();
        }

		static void GenEvent(CodeWriter cw, @event e, string name)
		{
			if (e.name == null)
			{
				return;
		    }

            cw.WriteLine("public event EventHandler<" + (GeneratorUtil.ToCs(e.name) + "Event") + "> " + 
				         (GeneratorUtil.ToCs(e.name) + "Event") + ";");
            cw.WriteLine();
        }

		static void GenFunction(CodeWriter cw, @request r, string name)
        {
            if (r.name == null)
			{
                return;
			}

            //TODO: share code with struct
            string parms = "";
            List<string> parmList1 = new List<string>();
            List<string> parmList2 = new List<string>();
            
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
                        
						parms += ", " + TypeToCs(f.type) + " @" + GeneratorUtil.ToParm(GeneratorUtil.ToCs(f.name));
						parmList1.Add(GeneratorUtil.ToCs(f.name));
                    }
                    else if (ob is list)
                    {
                        list l = ob as list;
                        if (l.name == null)
						{
							continue;                        
                        }
                        else if (l.type == "char")
                        {
							parms += ", string @" + GeneratorUtil.ToParm(GeneratorUtil.ToCs(l.name));
							parmList2.Add(GeneratorUtil.ToCs(l.name));
                        }
                        else if (l.type == "CARD32")
                        {
							parms += ", uint[] @" + GeneratorUtil.ToParm(GeneratorUtil.ToCs(l.name));
							parmList2.Add(GeneratorUtil.ToCs(l.name));
                        }
                    }
                    else if (ob is valueparam)
                    {
                        valueparam v = ob as valueparam;
						string vName = (v.valuelistname == null) ? "Values" : GeneratorUtil.ToCs(v.valuelistname);                     
                        string vType = TypeToCs(v.valuemasktype);

                        if (vType == "uint")
                        {
							parms += ", uint[] @" + GeneratorUtil.ToParm(vName);
                            parmList2.Add(vName);
                        }
                    }
                }
                
                parms = parms.Trim(',', ' ');
            }
			// end foreach



			if (r.reply != null)
			{
				cw.WriteLine("public Cookie<" + GeneratorUtil.(r.name) + "Reply> " + GeneratorUtil.ToCs(r.name) + " (" + parms + ");");
			}
			else
			{
				cw.WriteLine("public void " + GeneratorUtil.ToCs(r.name) + " (" + parms + ");");
			}

            cw.WriteLine("{");

			cw.WriteLine(GeneratorUtil.ToCs(r.name) + "Request req = new " + GeneratorUtil.ToCs(r.name) + "Request ();");

            if (isExtension)
            {
                cw.WriteLine("req.MessageData.ExtHeader.MajorOpcode = GlobalId;");
                cw.WriteLine("req.MessageData.ExtHeader.MinorOpcode = " + r.opcode + ";");
            }
            else
            {
                cw.WriteLine("req.MessageData.Header.Opcode = " + r.opcode + ";");
            }

            cw.WriteLine();

            foreach (string par in parmList1)
			{
				cw.WriteLine("req.MessageData.@" + par + " = @" + GeneratorUtil.ToParm(par) + ";");               
            }

            foreach (string par in parmList2)
			{
				cw.WriteLine("req.@" + par + " = @" + GeneratorUtil.ToParm(par) + ";");               
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

						cw.WriteLine("req.@" + GeneratorUtil.ToCs(l.name) + " = @" + GeneratorUtil.ToParm(GeneratorUtil.ToCs(l.name)) + ";");
                    }
                }
            }

            cw.WriteLine();
            cw.WriteLine("c.xw.Send (req);");
            cw.WriteLine();

            if (r.reply != null)
            {
                cw.WriteLine();
				cw.WriteLine("return c.xrr.GenerateCookie<" + GeneratorUtil.ToCs(r.name) + "Reply> ();");
            }

            cw.WriteLine("}");
            cw.WriteLine();
        }         
    }
}
