using System;
using System.Collections.Generic;
using Mono.Options;

namespace xnbgenerator
{
    public class Program
	{
        public static int Main(string[] args)
        {
            string reference = null;
            string outName = null;

            var options = new OptionSet
            {
                { "r|ref=", "Reference", r => reference = r },
                { "o|out=", "Output name", o => outName = o },
            };

            List<string> srcFiles = options.Parse(args);

            if (string.IsNullOrEmpty(outName))
            {
                Console.WriteLine("Must have output name");
                return 1;
            }

            Generator.LoadTypeMap("TypeMap");
			Generator.LoadTypeMap(reference + "TypeMap");

            foreach (string src in srcFiles)
            {
				Generator.Generate(src, outName);
				Generator.SaveTypeMap(outName + "TypeMap");
            }

            return 0;
        }
    }
}
