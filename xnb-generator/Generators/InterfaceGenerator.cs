using System;
using System.Collections.Generic;

using Schemas;

namespace xnbgenerator.Generators
{
    public class InterfaceGenerator
    {      
		public void Generate(xcb xcb, string name)
		{
            CodeWriter cwi = new CodeWriter(name + "Iface.cs");

			cwi.WriteLine("using System;");
            cwi.WriteLine("");

            cwi.WriteLine("public interface I" + name);
            cwi.WriteLine("{");
            
            foreach (object o in xcb.Items)
            {
                if (o == null)
				{
					continue;               
                }
                else if (o is @request)
                {
                    GenFunction(cwi, o as @request, name);
                }
            }

            cwi.WriteLine("}");
   
            cwi.Close();
		}


        static void GenFunction(CodeWriter cwi, @request r, string name)
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
                            continue;

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

                        if (l.type == "char")
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

			if (r.reply != null)
			{
				cwi.WriteLine("public Cookie<" + GeneratorUtil.ToCs(r.name) + "Reply> " + 
				              GeneratorUtil.ToCs(r.name) + " (" + parms + ");");
			}
            else
			{
				cwi.WriteLine("public void " + GeneratorUtil.ToCs(r.name) + " (" + parms + ");");            
            }
        }
    }
}
