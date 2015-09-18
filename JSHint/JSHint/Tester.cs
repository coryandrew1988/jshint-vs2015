namespace JSHint
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Text.RegularExpressions;

    public class Tester
    {
        private bool IsJSFile(string filename)
        {
            return filename.EndsWith(".js");
        }

        private static Regex hintExtractionRegex = new Regex("(.*):\\s+line\\s+(\\d+),\\s+col (\\d+),\\s+(.*)");

        private Hint[] ExtractHints(string resultString)
        {
            var ls = resultString.Split("\r\n".ToCharArray());
            var hs = new List<Hint>();

            foreach (var l in ls)
            {
                var m = hintExtractionRegex.Match(l);
                if (!m.Success) { continue; }

                hs.Add(new Hint
                {
                    Filename = m.Groups[1].ToString(),
                    LineNumber = int.Parse(m.Groups[2].ToString()),
                    ColumnNumber = int.Parse(m.Groups[3].ToString()),
                    Message = m.Groups[4].ToString()
                });
            }

            return hs.ToArray();
        }

        public Result Test(string filename)
        {
            if (!IsJSFile(filename)) { return new Result(); }

            Process p = new Process();
            p.StartInfo.FileName = "cmd";
            p.StartInfo.WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory;
            p.StartInfo.Arguments = "/C jshint \"" + filename + "\"";
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.CreateNoWindow = true;

            p.Start();

            var o = p.StandardOutput.ReadToEnd();
            var e = p.StandardError.ReadToEnd();

            return new Result
            {
                ErrorMessage = e,
                Hints = ExtractHints(o)
            };
        }

        public void Refresh()
        {
            // TODO keep a list of currently-tracked files
        }

        public void Clear()
        {

        }
    }
}
