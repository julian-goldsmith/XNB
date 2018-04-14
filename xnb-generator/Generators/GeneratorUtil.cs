using System;
namespace xnbgenerator.Generators
{
    public static class GeneratorUtil
	{      
        //TODO: numbers etc? Write tests
        //GetXidRange GetXIDRange GetX, numbers etc.
        public static string Destudlify(string s)
        {
            string o = "";

            bool xC = true;
            bool Cx = false;

            for (int i = 0; i != s.Length; i++)
            {

                if (i != 0)
                    xC = Char.IsLower(s[i - 1]);

                if (i != s.Length - 1)
                    Cx = Char.IsLower(s[i + 1]);

                if (i == 0)
                {
                    o += Char.ToLower(s[i]);
                    continue;
                }

                if (Char.IsUpper(s[i]))
                    if (Cx || xC && !Cx)
                        o += '_';

                o += Char.ToLower(s[i]);
            }

            return o;
        }

        public static string ToParm(string name)
        {
            return name.Substring(0, 1).ToLower() + name.Substring(1, name.Length - 1);
        }

        public static string ToCs(string name)
        {
            return Studlify(Destudlify(name));
		}

        public static string Studlify(string name)
        {
            string r = "";

            foreach (string s in name.Split('_'))
                r += Char.ToUpper(s[0]) + s.Substring(1);

            return r;
        }
    }
}
