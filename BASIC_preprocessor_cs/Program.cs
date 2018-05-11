using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/*
 * Here are some of the limitations of QuiteBasic:
     - screen resolution from maybe the 1960's
     - QuiteBasic peaks at one frame per 7-12 seconds on a modern Core i7 machine
     - no support for real variable names
     - no param stack for subroutines
     - no multidimensional arrays
     - severe input buffer lag
     - lack of support for multiple keypresses
     - no support for key press and hold
     - limit at 9999 lines
     - no support for nested conditionals
     - use of GOTOs
     - no support for timers
 
 * I developed this preprocessor to add support for named subroutines, variable names,
 * full RGB spectrum image rendering, and basic VS tooling support.
 */

namespace BASIC_preprocessor_cs
{
    class Program
    {
        static void Main(string[] args)
        {
            var lines = System.IO.File.ReadAllLines("code.nr").Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
            var dict = new Dictionary<string, int>();
            var code = new List<string>();
            var variables = new Dictionary<string, string>();
            var curIdx = 0;
            
            for (var i = 0; i < lines.Count; i++)
            {
                lines[i] = lines[i].Trim();

                if (lines[i].StartsWith("REM"))
                    continue;

                if (lines[i].Contains("|"))
                {
                    var s = lines[i].Split('|');
                    dict[s[0].Trim()] = i + 1;
                    lines[i] = "REM " + s[1].Trim();
                }
                
                var partsProc = lines[i].Split(' ');
                for (var j = 0; j < partsProc.Count(); j++)
                {
                    if (partsProc[j].Contains("VAR_"))
                    {
                        var cleanVar = string.Concat(partsProc[j].Substring(partsProc[j].IndexOf("VAR_")).Where(x => char.IsLetter(x) || x == '_'));

                        if (!variables.ContainsKey(cleanVar))
                            variables[cleanVar] = "X" + ++curIdx;

                        partsProc[j] = partsProc[j].Replace(cleanVar, variables[cleanVar]);
                    }

                    if (partsProc[j] == "LOAD_IMG_RAW")
                    {
                        var imgName = partsProc[j+1];
                        partsProc[j + 1] = "";
                        var b = new System.Drawing.Bitmap("images/" + imgName + ".png");
                        partsProc[j] = "\"" + GetColorsFromBitmap(b) + "\"";
                    }
                }
                
                code.Add(string.Join(" ", partsProc));
            }

            // one more loop once we have the locations of all the functions
            for (var i = 0; i < code.Count; i++)
            {
                // dont care about redundancy rn

                if (code[i].Contains("GOSUB "))
                {
                    var sub = code[i].Substring(code[i].IndexOf("GOSUB ") + "GOSUB ".Length);
                    code[i] = code[i].Replace("GOSUB " + sub, "GOSUB " + dict[sub]);
                }

                if (code[i].Contains("GOTO "))
                {
                    var sub = code[i].Substring(code[i].IndexOf("GOTO ") + "GOTO ".Length);
                    code[i] = code[i].Replace("GOTO " + sub, "GOTO " + dict[sub]);
                }

                code[i] = (i + 1) + " " + code[i];
            }

            var html = System.IO.File.ReadAllText("Mario.html").Replace("{{{ RENDER_BODY }}}", string.Join("\r\n", code));
            System.IO.File.WriteAllText("out.html", html);
            System.Diagnostics.Process.Start("out.html");
        }

        // Encode pixel data of the image
        private static string GetColorsFromBitmap(System.Drawing.Bitmap bmp)
        {
            var pixels = new string[bmp.Width * bmp.Height];

            for (int x = 0; x < bmp.Width; x++)
                for (int y = 0; y < bmp.Height; y++)
                    pixels[y * bmp.Width + (bmp.Width - x - 1)] = System.Drawing.ColorTranslator.ToHtml(bmp.GetPixel(x, y)).Replace("#", "");

            return string.Join("", pixels.Reverse());
        }
    }
}
