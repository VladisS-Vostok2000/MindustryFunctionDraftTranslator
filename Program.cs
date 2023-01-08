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
                Console.WriteLine($"Please give me path to {OldFilenameExtension} file.");
                Console.ReadKey();
                return;
            }

            string filePath = args[0];

            string filepathExtension = GetExtension(filePath);
            if (filepathExtension != OldFilenameExtension) {
                Console.WriteLine($"Wrong filepath extension: \"{filepathExtension}\". Only \"{OldFilenameExtension}\" awailable.");
                Console.ReadKey();
                return;
            }

            string mindastryFunctionDraft;
            using (StreamReader sr = new StreamReader(filePath)) {
                mindastryFunctionDraft = sr.ReadToEnd();
            }

            string[] mindastryFunctionDraftLines = mindastryFunctionDraft.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
            // mindastryFunctionDraftLines isnt empty
            Finish(mindastryFunctionDraftLines);


            WriteToFile(mindastryFunctionDraftLines, filePath);
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
            for (int i = 0; i < draft.Length; i++) {
                draft[i] = draft[i].Trim();
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
            string path = CastFilePath(draftPath);

            using FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write);
            StreamWriter sw = new StreamWriter(fs);
            foreach (var line in sourse) {
                sw.WriteLine(line);
            }

            sw.Flush();
        }
        private static string CastFilePath(string draftPath) {
            string fileName = GetFileName(draftPath);
            int fileNameIndexInPath = draftPath.IndexOf(fileName);

            // REFACTORING: повторяющийся код.
            if (fileNameIndexInPath == 0) {
                return fileName.Substring(0, fileName.IndexOf('.')) + NewFilenameExtension;
            }
            else {
                return draftPath.Substring(0, fileNameIndexInPath) + // Path without file
                    fileName.Substring(0, fileName.IndexOf('.')) + NewFilenameExtension;
            }
        }

    }
}
