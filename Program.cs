using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static System.IO.Path;

namespace MindustyDraftToFunctionTranslator {
    public static class Program {
        public const string OldFilenameExtension = ".minfndft";
        public const string NewFilenameExtension = ".min";
        public const char Separator = '#';
        public const string Pointer = "->";
        public const int PointerIndex = 0;
        public const int LabelIndex = 1;


        public static void Main(string[] args) {
            if (args.Length == 0) {
                FinishAllDraftsInFolder();
                return;
            }

            FinishSingleFile(args[0]);
        }



        private static void FinishSingleFile(string path) {
            CheckFilenameExtension(path);
            FinishFile(path);
        }
        private static void CheckFilenameExtension(string path) {
            string filepathExtension = GetExtension(path);
            if (filepathExtension != OldFilenameExtension) {
                throw new InvalidDataException($"Wrong filepath extension: \"{filepathExtension}\". Only \"{OldFilenameExtension}\" awailable.");
            }
        }
        private static string[] GetLines(string path) {
            string mindastryFunctionDraft;
            using (StreamReader sr = new StreamReader(path)) {
                mindastryFunctionDraft = sr.ReadToEnd();
            }

            return mindastryFunctionDraft.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        }


        private static void FinishAllDraftsInFolder() {
            foreach (var filePath in Directory.EnumerateFiles(".", "*" + OldFilenameExtension, SearchOption.TopDirectoryOnly)) {
                try {
                    FinishFile(filePath);
                }
                catch (Exception e) { Console.WriteLine(GetFileName(filePath + ":")); Console.WriteLine(e.Message); Console.WriteLine(e.StackTrace); }
            }
        }
        private static void FinishFile(string path) {
            string[] mindastryFunctionDraftLines = GetLines(path);
            // mindastryFunctionDraftLines result isnt empty
            Finish(mindastryFunctionDraftLines);
            WriteToFile(mindastryFunctionDraftLines, path);
        }

        private static void Finish(string[] draft) {
            (Dictionary<int, int> labeledLines, Dictionary<int, int> pointersLines) = GetDraftLabelsAndPointers(draft);
            // labeledLines isnt empty
            // pointersLines isnt empty
            Dictionary<int, string> linesPointerReplacementValue = GetPointersReplacements(labeledLines, pointersLines);
            foreach (var pointerLineIndex in pointersLines.Keys) {
                draft[pointerLineIndex] = draft[pointerLineIndex].Substring(0, draft[pointerLineIndex].IndexOf(Separator)).Trim().Replace(" n", " " + linesPointerReplacementValue[pointerLineIndex]);
            }
            foreach (var labeledLinesIndex in labeledLines.Keys) {

                // It might me destroin by upper function.
                int separatorLineIndex = draft[labeledLinesIndex].IndexOf(Separator);
                if (separatorLineIndex == -1) {
                    continue;
                }

                draft[labeledLinesIndex] = draft[labeledLinesIndex].Substring(0, draft[labeledLinesIndex].IndexOf(Separator)).Trim();
            }
        }
        private static (Dictionary<int, int>, Dictionary<int, int>) GetDraftLabelsAndPointers(string[] draft) {
            Dictionary<int, int> labeledLines = new Dictionary<int, int>();
            Dictionary<int, int> pointersLines = new Dictionary<int, int>();

            for (int i = 0; i < draft.Length; i++) {
                string line = draft[i];
                GetLineLabelsAndPointers(line, i, labeledLines, pointersLines);
            }

            return (labeledLines, pointersLines);
        }
        private static void GetLineLabelsAndPointers(string line, int lineIndex, Dictionary<int, int> labeledLines, Dictionary<int, int> pointersLines) {
            if (line.Contains(Separator)) {
                string[] @params = line.Substring(line.IndexOf(Separator) + 1).Split(Separator);

                // @params.Length > 0
                if (@params.Length > 2) {
                    throw new FormatException($"Too much params by separator \'{Separator}\' in line {lineIndex}: \"{line}\".");
                }
                else
                if (@params.Length == 2) {
                    labeledLines.Add(lineIndex, ParseLabelHere(@params[LabelIndex]));
                    pointersLines.Add(lineIndex, ParsePointerHere(@params[PointerIndex]));
                }
                else {
                    string param = @params[0];
                    if (IsPointer(param)) {
                        pointersLines.Add(lineIndex, ParsePointerHere(param));
                    }
                    else {
                        labeledLines.Add(lineIndex, ParseLabelHere(param));
                    }
                }


                int ParseLabelHere(string param) {
                    return ParseLabel(param, lineIndex);
                }
                int ParsePointerHere(string param) {
                    return ParsePointer(param, lineIndex);
                }
            }
        }
        private static int ParseLabel(string param, int lineIndex) {
            try {
                return int.Parse(param.Trim());
            }
            catch (FormatException) {
                throw new FormatException($"Label expected in parameter \"{param}\" on line {lineIndex}.");
            }
        }
        private static int ParsePointer(string param, int lineIndex) {
            string cleanedParam = param.Trim();
            int startIndex = param.IndexOf(Pointer) + Pointer.Length;
            try {
                return int.Parse(param.Substring(startIndex));
            }
            catch (FormatException) {
                throw new FormatException($"Pointer expected in param \"{param}\" on line {lineIndex}.");
            }
        }
        private static bool IsPointer(string param) {
            return param.Contains(Pointer);
        }

        private static Dictionary<int, string> GetPointersReplacements(Dictionary<int, int> labeledLines, Dictionary<int, int> pointersLines) {
            var outDic = new Dictionary<int, string>();
            foreach (var pointerLineIndex in pointersLines.Keys) {
                int labelLineIndex;
                try {
					labelLineIndex = labeledLines.First(pair => pair.Value.Equals(pointersLines[pointerLineIndex])).Key;
                }
                catch (InvalidOperationException) {
                    throw new InvalidDataException($"No label found for pointer \"{pointersLines[pointerLineIndex]}\".");
                }
				
				int result;
				if (pointerLineIndex < labelLineIndex) {
					result = labelLineIndex - pointerLineIndex - 1;
				}
				else {
					result = pointerLineIndex - labelLineIndex + 1;
				}
                outDic.Add(pointerLineIndex, result.ToString());
            }

            return outDic;
        }

        private static void WriteToFile(string[] sourse, string draftPath) {
            using FileStream fs = new FileStream(ChangeExtension(draftPath, NewFilenameExtension), FileMode.Create, FileAccess.Write);
            StreamWriter sw = new StreamWriter(fs);
            for (int i = 0; i < sourse.Length - 1; i++) {
                string line = sourse[i];
                sw.WriteLine(line.Trim());
            }
            sw.Write(sourse.Last().Trim());

            sw.Flush();
        }

    }
}
