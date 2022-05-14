using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using TREnvironmentEditor;
using TREnvironmentEditor.Model;
using TRLevelReader;
using TRLevelReader.Helpers;
using TRLevelReader.Model;

namespace TREnvironmentEditorTests
{
    public class RewriteTest
    {
        public void TestAll()
        {
            ISet<string> allTestedTypes = new SortedSet<string>();
            foreach (string lvl in TR2LevelNames.AsList)
            {
                ISet<Type> testedTypes = Test(lvl);
                foreach (Type t in testedTypes)
                {
                    allTestedTypes.Add(t.ToString());
                }
            }

            Console.WriteLine();
            Console.WriteLine("Tested types:");
            Console.WriteLine(string.Join(Environment.NewLine, allTestedTypes));

            Console.Read();
        }

        public ISet<Type> Test(string lvl)
        {
            EMEditorMapping oldMapping = EMEditorMapping.Get(@"OrigEnvironment\" + lvl + "-Environment.json");
            oldMapping.SerializeTo(lvl + "-Environment.json");

            EMEditorMapping newMapping = EMEditorMapping.Get(lvl + "-Environment.json");

            List<BaseEMFunction> oldFuncs = GetFunctions(oldMapping);
            List<BaseEMFunction> newFuncs = GetFunctions(newMapping);

            Debug.Assert(oldFuncs.Count == newFuncs.Count);
            for (int i = 0; i < oldFuncs.Count; i++)
            {
                Debug.Assert(oldFuncs[i].GetType() == newFuncs[i].GetType());
            }

            TR2LevelReader r = new TR2LevelReader();
            TR2LevelWriter w = new TR2LevelWriter();
            TR2Level oldLevel, newLevel;
            ISet<Type> testedTypes = new HashSet<Type>();
            for (int i = 0; i < oldFuncs.Count; i++)
            {
                Console.WriteLine("{0} : Func {1}/{2}", lvl, i + 1, oldFuncs.Count);

                oldLevel = r.ReadLevel(lvl);
                newLevel = r.ReadLevel(lvl);

                try
                {
                    oldFuncs[i].ApplyToLevel(oldLevel);
                    newFuncs[i].ApplyToLevel(newLevel);

                    w.WriteLevelToFile(oldLevel, "old_" + lvl);
                    w.WriteLevelToFile(newLevel, "new_" + lvl);

                    string c1 = Checksum("old_" + lvl);
                    string c2 = Checksum("new_" + lvl);

                    Debug.Assert(c1 == c2);

                    testedTypes.Add(oldFuncs[i].GetType());
                }
                catch (IndexOutOfRangeException)
                {
                    // Will happen if a function relies on something else
                    // having been done, e.g. finding a sector in a room
                    // which may not exist
                }
            }

            return testedTypes;
        }

        private List<BaseEMFunction> GetFunctions(EMEditorMapping mapping)
        {
            List<BaseEMFunction> funcs = new List<BaseEMFunction>();

            funcs.AddRange(mapping.All);
            funcs.AddRange(mapping.NonPurist);
            funcs.AddRange(mapping.Mirrored);

            mapping.Any.ForEach(s => funcs.AddRange(s));
            mapping.AllWithin.ForEach(a => a.ForEach(s => funcs.AddRange(s)));

            foreach (EMConditionalSingleEditorSet cond in mapping.ConditionalAll)
            {
                if (cond.OnTrue != null)
                    funcs.AddRange(cond.OnTrue);
                if (cond.OnFalse != null)
                    funcs.AddRange(cond.OnFalse);
            }

            foreach (EMConditionalEditorSet cond in mapping.ConditionalAllWithin)
            {
                if (cond.OnTrue != null)
                    cond.OnTrue.ForEach(s => funcs.AddRange(s));
                if (cond.OnFalse != null)
                    cond.OnFalse.ForEach(s => funcs.AddRange(s));
            }

            foreach (EMEditorGroupedSet groupedSet in mapping.OneOf)
            {
                funcs.AddRange(groupedSet.Leader);
                groupedSet.Followers.ForEach(s => funcs.AddRange(s));
            }

            return funcs;
        }

        public string Checksum(string file)
        {
            using (MD5 md5 = MD5.Create())
            using (FileStream stream = File.OpenRead(file))
            {
                byte[] hash = md5.ComputeHash(stream);
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hash.Length; i++)
                {
                    sb.Append(hash[i].ToString("x2"));
                }
                return sb.ToString();
            }
        }
    }
}