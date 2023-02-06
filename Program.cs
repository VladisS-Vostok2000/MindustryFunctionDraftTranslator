using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using static System.IO.Path;
using BasicLibrary;

namespace MindustyDraftToFunctionTranslator {
    public static class Program {
        public const string FunctionDraftExtension = ".minfndft";
        public const string RawCodeExtension = ".minraw";
        public const string NewFilenameExtension = ".min";
        public const char Separator = '#';
        public const string Pointer = "->";
        public const char ParameterToSubstitute = 'n';
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
            string filenameExtension = Path.GetExtension(path);
            if (!SuitableFilenameExtension(filenameExtension)) {
                throw new InvalidDataException($"Unsuitable filename extension. Only {RawCodeExtension} and {FunctionDraftExtension} suitable. Extension was {filenameExtension}.");
            }

            FinishFile(path);
        }
        private static void FinishAllDraftsInFolder() {
            foreach (var filePath in Directory.EnumerateFiles(".", "*" + FunctionDraftExtension, SearchOption.TopDirectoryOnly)) {
                try {
                    FinishFile(filePath);
                }
                catch (InvalidDataException e) { Console.WriteLine(GetFileName(filePath + ":")); Console.WriteLine(e.Message); }
            }
            foreach (var filePath in Directory.EnumerateFiles(".", "*" + RawCodeExtension, SearchOption.TopDirectoryOnly)) {
                try {
                    FinishFile(filePath);
                }
                catch (InvalidDataException e) { Console.WriteLine(GetFileName(filePath + ":")); Console.WriteLine(e.Message); }
            }
        }
        private static bool SuitableFilenameExtension(string extension) {
            if (extension == FunctionDraftExtension || extension == RawCodeExtension) {
                return true;
            }
            else {
                return false;
            }
        }
        private static void FinishFile(string path) {
            string[] code = GetLines(path);
            // code isnt empty

            (Dictionary<int, string> labeledLines, Dictionary<int, string> pointersLines) = GetDraftLabelsAndPointers(code);
            CheckValidness(labeledLines, pointersLines);

            FinishDraft(code, labeledLines, pointersLines);

            ClearLines(code);
            
            WriteToFile(code, path);
        }
        private static string[] GetLines(string path) {
            string mindastryFunctionDraft;
            using (StreamReader sr = new StreamReader(path)) {
                mindastryFunctionDraft = sr.ReadToEnd();
            }

            return mindastryFunctionDraft.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        }
        private static (Dictionary<int, string>, Dictionary<int, string>) GetDraftLabelsAndPointers(string[] draft) {
            Dictionary<int, string> labeledLines = new Dictionary<int, string>();
            Dictionary<int, string> pointersLines = new Dictionary<int, string>();

            for (int i = 0; i < draft.Length; i++) {
                string line = draft[i];
                if (line.Contains(Separator)) {
                    InsertLineLabelsAndPointers(line, i, labeledLines, pointersLines);
                }
            }

            return (labeledLines, pointersLines);
        }
        private static void InsertLineLabelsAndPointers(string line, int lineIndex, Dictionary<int, string> labeledLines, Dictionary<int, string> pointersLines) {
            string[] @params = line.Substring(line.IndexOf(Separator) + 1).Split(Separator);

            // REFACTORING: дублирующийся код.
            if (@params.Length > 2) {
                throw new InvalidDataException($"Too much params by separator \'{Separator}\' on line {lineIndex}.");
            }
            else
            if (@params.Length == 2) {
                bool parsed = TryParseLabel(@params[LabelIndex], out string label);
                if (!parsed) {
                    throw new InvalidDataException($"Label expected as parameter on line {lineIndex}. Value was {@params[LabelIndex]}.");
                }
                if (!labeledLines.ContainsValue(label)) {
                    labeledLines.Add(lineIndex, label);
                }
                else {
                    throw new InvalidDataException($"Several times same label. Label was {label} on lines {lineIndex} and {labeledLines.FirstKeyByValue(label)}");
                }

                parsed = TryParsePointer(@params[PointerIndex], out string pointer);
                if (!parsed) {
                    throw new InvalidDataException($"Pointer expected as parameter on line {lineIndex}. Value was {@params[PointerIndex]}.");
                }
                pointersLines.Add(lineIndex, pointer);
            }
            else {
                string param = @params[0];
                if (IsPointer(param)) {
                    bool parsed = TryParsePointer(param, out string pointer);
                    if (!parsed) {
                        throw new InvalidDataException($"Pointer expected as parameter on line {lineIndex}. Value was {param}.");
                    }
                    pointersLines.Add(lineIndex, pointer);
                }
                else {
                    bool parsed = TryParseLabel(param, out string label);
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
        private static bool TryParseLabel(string param, out string result) {
            result = param.Trim();
            return true;
        }
        private static bool TryParsePointer(string param, out string pointer) {
            pointer = param.Substring(param.IndexOf(Pointer) + Pointer.Length).Trim();
            return true;
        }
        private static bool IsPointer(string param) {
            return param.Contains(Pointer);
        }

        private static void CheckValidness(Dictionary<int, string> labeledLines, Dictionary<int, string> pointersLines) {
            HashSet<string> labels = new HashSet<string>(labeledLines.Count);
            foreach (var label in labeledLines.Values) {
                if (!labels.Add(label)) {
                    throw new InvalidDataException($"The same label declared several times. Label was {label}.");
                }
            }

            foreach (var pointer in pointersLines.Values) {
                if (!labels.Contains(pointer)) {
                    throw new InvalidDataException($"The pointer points out on non-exist label. Pointer was {pointer}.");
                }
            }

            foreach (var pointerLine in pointersLines) {
                if (labeledLines.FirstKeyByValue(pointerLine.Value) == pointerLine.Key) {
                    throw new InvalidDataException($"The pointer and label are on the same line. Line number was {pointerLine.Key}.");
                }
            }
        }

        private static void FinishDraft(string[] draft, IDictionary<int, string> labeledLines, IDictionary<int, string> pointersLines) {
            foreach (var pointerLine in pointersLines) {
                int pointerLineIndex = pointerLine.Key;
                string instruction = draft[pointerLineIndex].Substring(0, draft[pointerLineIndex].IndexOf(Separator)).Trim();
                string[] instructionParts = instruction.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (instructionParts[0] == "op" && instructionParts.Length >= 5 && instructionParts[1] == ParameterToSubstitute.ToString() && instructionParts[4] == ParameterToSubstitute.ToString()) {
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
                    instructionParts[4] = index.ToString();
                }
                else
                if (instructionParts[0] == "jump" && instructionParts.Length >= 2 && instructionParts[1] == ParameterToSubstitute.ToString()) {
                    instructionParts[1] = labeledLines.FirstKeyByValue(pointerLine.Value).ToString();
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
