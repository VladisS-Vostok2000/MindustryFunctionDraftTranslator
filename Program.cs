using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using static System.IO.Path;
using BasicLibrary;

namespace MindustyDraftToFunctionTranslator {
    public static class Program {
        public const string OldFilenameExtension = ".minfndft";
        public const string NewFilenameExtension = ".min";
        public const char Separator = '#';
        public const string Pointer = "->";
        public const int PointerIndex = 0;
        public const int LabelIndex = 1;
        public const string UpperLineOperation = "sub";
        public const string LowerLineOperation = "add";
        public const int operationIndex = 1;



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
                catch (InvalidDataException e) { Console.WriteLine(GetFileName(filePath + ":")); Console.WriteLine(e.Message); }
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

            CheckValid(labeledLines, pointersLines);
            Substitute(draft, labeledLines, pointersLines);
            ClearLines(draft);
        }
        private static (Dictionary<int, int>, Dictionary<int, int>) GetDraftLabelsAndPointers(string[] draft) {
            Dictionary<int, int> labeledLines = new Dictionary<int, int>();
            Dictionary<int, int> pointersLines = new Dictionary<int, int>();

            for (int i = 0; i < draft.Length; i++) {
                string line = draft[i];
                if (line.Contains(Separator)) {
                    InsertLineLabelsAndPointers(line, i, labeledLines, pointersLines);
                }
            }

            return (labeledLines, pointersLines);
        }
        private static void InsertLineLabelsAndPointers(string line, int lineIndex, Dictionary<int, int> labeledLines, Dictionary<int, int> pointersLines) {
            string[] @params = line.Substring(line.IndexOf(Separator) + 1).Split(Separator);

            // REFACTORING: дублирующийся код.
            if (@params.Length > 2) {
                throw new InvalidDataException($"Too much params by separator \'{Separator}\' on line {lineIndex}.");
            }
            else
            if (@params.Length == 2) {
                bool parsed = TryParseLabel(@params[LabelIndex], out int label);
                if (!parsed) {
                    throw new InvalidDataException($"Label expected as parameter on line {lineIndex}. Value was {@params[LabelIndex]}.");
                }
                if (!labeledLines.ContainsValue(label)) {
                    labeledLines.Add(lineIndex, label);
                }
                else {
                    throw new InvalidDataException($"Several times same label. Label was {label} on lines {lineIndex} and {labeledLines.FirstKeyByValue(label)}");
                }

                parsed = TryParsePointer(@params[PointerIndex], out int pointer);
                if (!parsed) {
                    throw new InvalidDataException($"Pointer expected as parameter on line {lineIndex}. Value was {@params[PointerIndex]}.");
                }
                pointersLines.Add(lineIndex, pointer);
            }
            else {
                string param = @params[0];
                if (IsPointer(param)) {
                    bool parsed = TryParsePointer(param, out int pointer);
                    if (!parsed) {
                        throw new InvalidDataException($"Pointer expected as parameter on line {lineIndex}. Value was {param}.");
                    }
                    pointersLines.Add(lineIndex, pointer);
                }
                else {
                    bool parsed = TryParseLabel(param, out int label);
                    if (!parsed) {
                        throw new InvalidDataException($"Label or pointer expected as parameter on line {lineIndex}. Value was {param}.");
                    }
                    if (!labeledLines.ContainsValue(label)) {
                        labeledLines.Add(lineIndex, label);
                    }
                    else {
                        throw new InvalidDataException($"Several times same label. Label was {label} on lines {lineIndex} and {labeledLines.FirstKeyByValue(label)}");
                    }
                }
            }
        }
        private static bool TryParseLabel(string param, out int result) {
            return int.TryParse(param, out result);
        }
        private static bool TryParsePointer(string param, out int pointer) {
            return int.TryParse(param.Substring(param.IndexOf(Pointer) + Pointer.Length), out pointer);
        }
        private static bool IsPointer(string param) {
            return param.Contains(Pointer);
        }

        private static void CheckValid(Dictionary<int, int> labeledLines, Dictionary<int, int> pointersLines) {
            HashSet<int> hashTable = new HashSet<int>(labeledLines.Count);
            foreach (var label in labeledLines.Values) {
                if (!hashTable.Add(label)) {
                    throw new InvalidDataException($"The same label declared several times. Label was {label}.");
                }
            }

            foreach (var pointer in pointersLines.Values) {
                if (!hashTable.Contains(pointer)) {
                    throw new InvalidDataException($"The pointer points out on non-exist label. Pointer was {pointer}.");
                }
            }
        }

        private static void Substitute(string[] draft, Dictionary<int, int> labeledLines, Dictionary<int, int> pointersLines) {
            foreach (var pointerLine in pointersLines) {
                int pointerLineIndex = pointerLine.Key;
                string instruction = draft[pointerLineIndex].Substring(0, draft[pointerLineIndex].IndexOf(Separator)).Trim();
                string[] instructionParts = instruction.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (instructionParts[0] == "op" && instructionParts.Length >= 5) {
                    int index;
                    string operation;
                    int lineIndexPointerPoints = labeledLines.FirstKeyByValue(pointerLine.Value);
                    // REFACTORING: повторяющийся код.
                    if (pointerLineIndex < lineIndexPointerPoints) {
                        index = lineIndexPointerPoints - pointerLineIndex - 1;
                        operation = LowerLineOperation;
                    }
                    else
                    if (pointerLineIndex > lineIndexPointerPoints) {
                        index = pointerLineIndex - lineIndexPointerPoints + 1;
                        operation = UpperLineOperation;
                    }
                    else {
                        throw new InvalidDataException($"Pointer points out the current line. Line was {pointerLineIndex}.");
                    }

                    instructionParts[1] = operation;
                    instructionParts[instructionParts.GetUpperBound(0)] = index.ToString();
                }
                else {
                    throw new InvalidDataException($"Unknown instruction type. Type was \"{instructionParts[0]}\" on line {pointerLineIndex}.");
                }

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < instructionParts.Length - 1; i++) {
                    string linePart = instructionParts[i];
                    sb.Append(linePart + ' ');
                }
                sb.Append(instructionParts[instructionParts.GetUpperBound(0)]);

                draft[pointerLineIndex] = sb.ToString();
            }
        }

        private static void ClearLines(string[] draft) {
            for (int i = 0; i < draft.Length; i++) {
                string line = draft[i];
                int separatorLineIndex = line.IndexOf(Separator);
                string lineWithoutCode = line;
                if (separatorLineIndex != -1) {
                    lineWithoutCode = line.Substring(0, separatorLineIndex);
                }

                draft[i] = lineWithoutCode.Trim();
            }
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
