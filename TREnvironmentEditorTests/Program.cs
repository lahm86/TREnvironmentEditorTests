using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TREnvironmentEditor;
using TREnvironmentEditor.Helpers;
using TREnvironmentEditor.Model;
using TREnvironmentEditor.Model.Types;
using TRLevelReader;
using TRLevelReader.Helpers;
using TRLevelReader.Model;

namespace TREnvironmentEditorTests
{
    class Program
    {
        static void Main(string[] args)
        {
            string rdir = @"D:\Games\steamapps\common\Tomb Raider (II) - Untouched\data\";
            string wdir = @"D:\Games\steamapps\common\Tomb Raider (II) - UKBox Dev\data\";

            string lvl = LevelNames.LQ;
            TR2LevelReader reader = new TR2LevelReader();
            TR2LevelWriter writer = new TR2LevelWriter();

            EMEditorMapping mapping = EMEditorMapping.Get(lvl);
            EMType[] exclusions = new EMType[] { };

            Console.WriteLine("0 - All");
            Console.WriteLine("1 - Any");
            Console.WriteLine("2 - AllWithin");
            Console.WriteLine("3 - OneOf");
            Console.WriteLine("4 - Validate locations");

            int opt = GetInt("Pick an option:");

            if (mapping.All.Count > 0 && opt == 0)
            {
                TR2Level level = reader.ReadLevel(rdir + lvl);
                mapping.All.ApplyToLevel(level, exclusions);
                MoveLara(level);
                writer.WriteLevelToFile(level, wdir + lvl);
                Console.WriteLine("All AL mods applied, level saved.");
            }
            else if (mapping.Any.Count > 0 && opt == 1)
            {
                TR2Level level = reader.ReadLevel(rdir + lvl);
                foreach (EMEditorSet mod in mapping.Any)
                {
                    mod.ApplyToLevel(level, exclusions);
                }
                MoveLara(level);
                writer.WriteLevelToFile(level, wdir + lvl);
                Console.WriteLine("All AN mods applied, level saved.");
            }
            else if (mapping.AllWithin.Count > 0 && opt == 2)
            {
                int setIndex = mapping.AllWithin.Count == 1 ? 0 : GetInt(string.Format("Choose a set (0 - {0})", mapping.AllWithin.Count - 1));
                List<EMEditorSet> set = mapping.AllWithin[setIndex];

                if (set.Count > 1 && GetOption("Loop?"))
                {
                    for (int i = 0; i < set.Count; i++)
                    {
                        TR2Level level = reader.ReadLevel(rdir + lvl);
                        EMEditorSet mod = set[i];
                        mod.ApplyToLevel(level, exclusions);
                        MoveLara(level);
                        writer.WriteLevelToFile(level, wdir + lvl);
                        Console.WriteLine("Mod {0} applied, level saved...press any key to continue.", i);
                        Console.ReadLine();
                    }
                }
                else
                {
                    TR2Level level = reader.ReadLevel(rdir + lvl);
                    int modIndex = set.Count == 1 ? 0 : GetInt(string.Format("Choose a mod for AW {0} (0 - {1})", setIndex, set.Count - 1));
                    EMEditorSet mod = set[modIndex];
                    mod.ApplyToLevel(level, exclusions);
                    MoveLara(level);
                    writer.WriteLevelToFile(level, wdir + lvl);
                    Console.WriteLine("Mod applied, level saved.");
                }
                Console.WriteLine();
            }
            else if (mapping.OneOf.Count > 0 && opt == 3)
            {
                int setIndex = mapping.OneOf.Count == 1 ? 0 : GetInt(string.Format("Choose a set (0 - {0})", mapping.OneOf.Count - 1));
                EMEditorGroupedSet set = mapping.OneOf[setIndex];

                if (set.Followers.Length > 1 && GetOption("Loop?"))
                {
                    for (int i = 0; i < set.Followers.Length; i++)
                    {
                        TR2Level level = reader.ReadLevel(rdir + lvl);
                        EMEditorSet mod = set.Followers[i];
                        set.ApplyToLevel(level, mod, exclusions);
                        MoveLara(level);
                        writer.WriteLevelToFile(level, wdir + lvl);
                        Console.WriteLine("Mod {0} applied, level saved...press any key to continue.", i);
                        Console.ReadLine();
                    }
                }
                else
                {
                    int modIndex = set.Followers.Length == 1 ? 0 : GetInt(string.Format("Choose a follower mod (0 - {0})", set.Followers.Length - 1));
                    EMEditorSet mod = set.Followers[modIndex];
                    TR2Level level = reader.ReadLevel(rdir + lvl);
                    set.ApplyToLevel(level, mod, exclusions);
                    MoveLara(level);
                    writer.WriteLevelToFile(level, wdir + lvl);
                    Console.WriteLine("Mod applied, level saved.");
                }
                Console.WriteLine();
            }
            else if (opt == 4)
            {
                foreach (string l in LevelNames.AsListWithAssault)
                {
                    mapping = EMEditorMapping.Get(l);
                    Console.WriteLine("Checking " + l);
                    TR2Level level = reader.ReadLevel(rdir + l);
                    ValidateLocations(level, mapping);
                }
                Console.Write("Done");
            }

            Console.Read();
        }

        static void MoveLara(TR2Level level)
        {
            TR2Entity lara = level.Entities.ToList().Find(e => e.TypeID == 0);
            string[] pos = "48650, 1530, 71161".Split(',');
            lara.X = int.Parse(pos[0].Trim());
            lara.Y = int.Parse(pos[1].Trim());
            lara.Z = int.Parse(pos[2].Trim());
            lara.Room = 26;

            //int[] arr = new int[] { 149, 84, 96 };
            //foreach (int ent in arr)
            //{
            //    level.Entities[ent].X = lara.X;
            //    level.Entities[ent].Y = lara.Y;
            //    level.Entities[ent].Z = lara.Z;
            //    level.Entities[ent].Room = lara.Room;
            //}
        }

        static bool GetOption(string msg)
        {
            Console.WriteLine(msg + " [Y/N]");
            string s = Console.ReadLine();
            return s.Trim().ToLower().StartsWith("y");
        }

        static int GetInt(string msg)
        {
            Console.WriteLine(msg);
            string s = Console.ReadLine();
            return int.TryParse(s, out int i) ? i : GetInt(msg);
        }

        static void ValidateLocations(TR2Level level, EMEditorMapping mapping)
        {
            TestEditorSet(level, mapping.All);

            foreach (EMEditorSet set in mapping.Any)
            {
                TestEditorSet(level, set);
            }

            foreach (List<EMEditorSet> lst in mapping.AllWithin)
            {
                foreach (EMEditorSet set in lst)
                {
                    TestEditorSet(level, set);
                }
            }

            foreach (EMEditorGroupedSet set in mapping.OneOf)
            {
                TestEditorSet(level, set.Leader);
                foreach (EMEditorSet follower in set.Followers)
                {
                    TestEditorSet(level, follower);
                }
            }

            // Need to ensure mirroring has been done
            //TestEditorSet(level, mapping.Mirrored);
        }

        static void TestEditorSet(TR2Level level, EMEditorSet set)
        {
            foreach (BaseEMFunction f in set)
            {
                if (f is EMMoveSlotFunction mf)
                {
                    ValidateLocation(level, mf.Location, f);
                }
                else if (f is EMMoveEntityFunction ef)
                {
                    ValidateLocation(level, ef.TargetLocation, f);
                }
                //else if (f is EMMovePickupFunction pf)
                //{
                //    ValidateLocation(level, pf.TargetLocation, f);
                //}
                //else if (f is EMLadderFunction lf)
                //{
                //    ValidateLocation(level, lf.Location);
                //}
                else if (f is EMAddStaticMeshFunction sm)
                {
                    foreach (EMLocation location in sm.Locations)
                    {
                        ValidateLocation(level, location, f);
                    }
                }
            }
        }

        static void ValidateLocation(TR2Level level, EMLocation location, BaseEMFunction f)
        {
            if (location == null)
            {
                Console.WriteLine("Warning - null location found.");
            }
            else if (!TestLocation(level, location))
            {
                Console.WriteLine("Location is outwith room bounds - " + ToString(location));
            }
        }

        static bool TestLocation(TR2Level level, EMLocation location)
        {
            TR2Room room = level.Rooms[location.Room];
            int minX = room.Info.X;
            int maxX = room.Info.X + 1024 * room.NumXSectors;
            int minZ = room.Info.Z;
            int maxZ = room.Info.Z + 1024 * room.NumZSectors;
            int minY = room.Info.YTop;
            int maxY = room.Info.YBottom;

            return minX <= location.X && maxX >= location.X &&
                minY <= location.Y && maxY >= location.Y &&
                minZ <= location.Z && maxZ >= location.Z;
        }

        static string ToString(EMLocation location)
        {
            return string.Format("[X={0}, Y={1}, Z={2}, R={3}]", location.X, location.Y, location.Z, location.Room);
        }
    }
}