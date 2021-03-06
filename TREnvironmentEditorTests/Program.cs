using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TREnvironmentEditor;
using TREnvironmentEditor.Helpers;
using TREnvironmentEditor.Model;
using TREnvironmentEditor.Model.Types;
using TRFDControl;
using TRFDControl.FDEntryTypes;
using TRFDControl.Utilities;
using TRLevelReader;
using TRLevelReader.Helpers;
using TRLevelReader.Model;

namespace TREnvironmentEditorTests
{
    class Program
    {
        static readonly TR2LevelReader _reader = new TR2LevelReader();
        static readonly TR2LevelWriter _writer = new TR2LevelWriter();

        static string _readPath, _writePath, _laraPos, _laraItems;
        static readonly EMType[] _exclusions = new EMType[] { };
        static bool _ukFloater;
        static int _allAny;

        static void Main()
        {
            string rdir = @"D:\Games\steamapps\common\Tomb Raider (II) - Untouched\data\";
            string wdir = @"D:\Games\steamapps\common\Tomb Raider (II) - UKBox Dev\data\";

            _laraPos = "52727, -9216, 61967, 2";
            _laraItems = "6,31";
            _ukFloater = false;

            List<string> lvls = LevelNames.AsListWithAssault;

            while (true)
            {
                Console.WriteLine("**********************************");
                Console.WriteLine("Levels");
                Console.WriteLine();
                for (int i = 0; i < lvls.Count; i++)
                {
                    Console.WriteLine("{0} - {1}", i, lvls[i]);
                }
                Console.WriteLine("**********************************");

                string lvl = lvls[GetInt("Choose a level")];
                _readPath = Path.Combine(rdir, lvl == LevelNames.FLOATER && _ukFloater ? "floating_uk.tr2" : lvl);
                _writePath = Path.Combine(wdir, lvl);

                Console.WriteLine();
                PickPositioning();

                EMEditorMapping mapping = EMEditorMapping.Get(lvl);
                if (_ukFloater)
                {
                    mapping.AlternateTextures();
                }

                Console.WriteLine("**********************************");
                Console.WriteLine("Apply ALL, NONPURIST and ANY?");
                Console.WriteLine("0 - None");
                Console.WriteLine("1 - ALL only");
                Console.WriteLine("2 - ANY only");
                Console.WriteLine("3 - NONPURIST only");
                Console.WriteLine("4 - ALL, NONPURIST and ANY");
                Console.WriteLine("**********************************");
                _allAny = GetInt("Pick an all/any option from above to run");
                Console.WriteLine();

                Console.WriteLine("**********************************");
                Console.WriteLine("Variable Options");
                Console.WriteLine();
                Console.WriteLine("0 - AllWithin");
                Console.WriteLine("1 - ConditionalAllWithin");
                Console.WriteLine("2 - OneOf");
                Console.WriteLine("3 - Mirrored");
                Console.WriteLine("4 - Validate locations");
                Console.WriteLine("5 - Validate OneOf combined locations");
                Console.WriteLine("**********************************");

                int opt = GetInt("Pick a variable option from above to run");
                Console.WriteLine();

                if (opt == 0)
                {
                    RunAllWithin(mapping);
                }
                else if (opt == 1)
                {
                    RunConditionalAllWithin(mapping);
                }
                else if (opt == 2)
                {
                    RunOneOf(mapping);
                }
                else if (opt == 3)
                {
                    RunMirrored(mapping);
                }
                else if (opt == 4)
                {
                    if (GetOption("Validate all levels?"))
                    {
                        foreach (string l in LevelNames.AsListWithAssault)
                        {
                            mapping = EMEditorMapping.Get(l);
                            Console.WriteLine("Checking " + l);
                            TR2Level level = _reader.ReadLevel(rdir + l);
                            ValidateLocations(level, mapping);
                            Console.WriteLine();
                        }
                    }
                    else
                    {
                        TR2Level level = _reader.ReadLevel(rdir + lvl);
                        ValidateLocations(level, mapping);
                        Console.WriteLine();
                    }
                }
                else if (opt == 5)
                {
                    ValidateOneOfCombined(mapping);
                }

                Breaker();
            }
        }

        static void PickPositioning()
        {
            Console.WriteLine("**********************************");
            Console.WriteLine("Lara");
            Console.WriteLine("**********************************");
            Console.WriteLine("Choose Lara's position [X, Y, Z, Room] or press Enter to use the defaault.");
            Console.WriteLine("Current: {0}", _laraPos);
            string newPos = Console.ReadLine();
            if (!string.IsNullOrEmpty(newPos))
            {
                _laraPos = newPos;
            }

            Console.WriteLine("Move items to Lara [3, 5, 9...etc] or press enter to use the default.");
            Console.WriteLine("Current: {0}", _laraItems);
            string newItems = Console.ReadLine();
            if (!string.IsNullOrEmpty(newItems))
            {
                _laraItems = newItems;
            }
        }

        static TR2Level DefaultLoadLevel(EMEditorMapping mapping)
        {
            // Read in the level and apply the All and Any mods by default.
            // Also move Lara and any items we need.
            TR2Level level = _reader.ReadLevel(_readPath);
            MoveLara(level);

            if (_allAny == 1 || _allAny == 4)
            {
                mapping.All.ApplyToLevel(level, _exclusions);
            }

            if (_allAny == 1 || _allAny == 3)
            {
                mapping.NonPurist.ApplyToLevel(level, _exclusions);
            }

            if (_allAny == 2 || _allAny == 4)
            {
                foreach (EMEditorSet mod in mapping.Any)
                {
                    mod.ApplyToLevel(level, _exclusions);
                }
            }
            
            return level;
        }

        static void WriteLevel(TR2Level level)
        {
            _writer.WriteLevelToFile(level, _writePath);
        }

        static void Breaker()
        {
            Console.WriteLine();
            Console.WriteLine("-----------------------------------------------------");
            Console.WriteLine();
        }

        static void MoveLara(TR2Level level)
        {
            int lara = level.Entities.ToList().FindIndex(e => e.TypeID == 0);
            string[] pos = _laraPos.Split(',');

            int x = int.Parse(pos[0].Trim());
            int y = int.Parse(pos[1].Trim());
            int z = int.Parse(pos[2].Trim());
            short room = short.Parse(pos[3].Trim());

            List<int> ents = new List<int>() { lara };

            string[] items = _laraItems.Split(',');
            foreach (string item in items)
            {
                if (int.TryParse(item, out int i))
                {
                    ents.Add(i);
                }
            }

            foreach (int ent in ents)
            {
                level.Entities[ent].X = x;
                level.Entities[ent].Y = y;
                level.Entities[ent].Z = z;
                level.Entities[ent].Room = room;
            }
        }

        static bool GetOption(string msg)
        {
            Console.Write(msg + " [Y/N]: ");
            string s = Console.ReadLine();
            return s.Trim().ToLower().StartsWith("y");
        }

        static int GetInt(string msg)
        {
            Console.Write(msg + ": ");
            string s = Console.ReadLine();
            return int.TryParse(s, out int i) ? i : GetInt(msg);
        }

        static void RunAllWithin(EMEditorMapping mapping)
        {
            if (mapping.AllWithin.Count > 0)
            {
                int setIndex = mapping.AllWithin.Count == 1 ? 0 : GetInt(string.Format("Choose a set (0 - {0})", mapping.AllWithin.Count - 1));
                List<EMEditorSet> set = mapping.AllWithin[setIndex];

                int modIndex = set.Count == 1 ? 0 : GetInt(string.Format("Choose a mod for AW {0} (0 - {1}). Enter -1 to loop", setIndex, set.Count - 1));
                if (modIndex == -1)
                {
                    for (int i = 0; i < set.Count; i++)
                    {
                        TR2Level level = DefaultLoadLevel(mapping);

                        EMEditorSet mod = set[i];
                        mod.ApplyToLevel(level, _exclusions);

                        WriteLevel(level);
                        Console.WriteLine("Mod {0} applied, level saved...press return to continue.", i);
                        Console.ReadLine();
                    }
                    Console.WriteLine("All mods executed.");
                }
                else
                {
                    TR2Level level = DefaultLoadLevel(mapping);

                    EMEditorSet mod = set[modIndex];
                    mod.ApplyToLevel(level, _exclusions);

                    WriteLevel(level);
                    Console.WriteLine("Mod applied, level saved.");
                }
            }
            else
            {
                Console.WriteLine("AllWithin group is empty.");
            }
        }

        static void RunConditionalAllWithin(EMEditorMapping mapping)
        {
            if (mapping.ConditionalAllWithin.Count > 0)
            {
                int setIndex = mapping.ConditionalAllWithin.Count == 1 ? 0 : GetInt(string.Format("Choose a set (0 - {0})", mapping.ConditionalAllWithin.Count - 1));
                EMConditionalEditorSet editorSet = mapping.ConditionalAllWithin[setIndex];
                List<EMEditorSet> testSet;
                if (GetInt("Simulate condition 0 = False, 1 = True") == 1)
                {
                    testSet = editorSet.OnTrue;
                }
                else
                {
                    testSet = editorSet.OnFalse;
                }

                int modIndex = testSet.Count == 1 ? 0 : GetInt(string.Format("Choose a mod for CAW {0} (0 - {1}). Enter -1 to loop", setIndex, testSet.Count - 1));
                if (modIndex == -1)
                {
                    for (int i = 0; i < testSet.Count; i++)
                    {
                        TR2Level level = DefaultLoadLevel(mapping);

                        EMEditorSet mod = testSet[i];
                        mod.ApplyToLevel(level, _exclusions);

                        WriteLevel(level);
                        Console.WriteLine("Mod {0} applied, level saved...press return to continue.", i);
                        Console.ReadLine();
                    }
                    Console.WriteLine("All mods executed.");
                }
                else
                {
                    TR2Level level = DefaultLoadLevel(mapping);

                    EMEditorSet mod = testSet[modIndex];
                    mod.ApplyToLevel(level, _exclusions);

                    WriteLevel(level);
                    Console.WriteLine("Mod applied, level saved.");
                }
            }
            else
            {
                Console.WriteLine("ConditionalAllWithin group is empty.");
            }
        }

        static void RunOneOf(EMEditorMapping mapping)
        {
            if (mapping.OneOf.Count > 0)
            {
                int setIndex = mapping.OneOf.Count == 1 ? 0 : GetInt(string.Format("Choose a set (0 - {0})", mapping.OneOf.Count - 1));
                EMEditorGroupedSet set = mapping.OneOf[setIndex];

                int modIndex = set.Followers.Count == 1 ? 0 : GetInt(string.Format("Choose a follower mod (0 - {0}). Enter -1 to loop", set.Followers.Count - 1));

                if (modIndex == -1)
                {
                    Console.WriteLine();
                    for (int i = 0; i < set.Followers.Count; i++)
                    {
                        TR2Level level = DefaultLoadLevel(mapping);

                        EMEditorSet mod = set.Followers[i];
                        set.ApplyToLevel(level, mod, _exclusions);

                        WriteLevel(level);
                        Console.WriteLine("Mod {0} applied, level saved...press enter to continue.", i);
                        Console.ReadLine();
                    }
                }
                else
                {
                    TR2Level level = DefaultLoadLevel(mapping);

                    EMEditorSet mod = set.Followers[modIndex];
                    set.ApplyToLevel(level, mod, _exclusions);

                    WriteLevel(level);
                    Console.WriteLine("Mod applied, level saved.");
                }
            }
            else
            {
                Console.WriteLine("OneOf group is empty.");
            }
        }

        static void RunMirrored(EMEditorMapping mapping)
        {
            TR2Level level = DefaultLoadLevel(mapping);

            EMMirrorFunction mirrorFunc = new EMMirrorFunction();
            mirrorFunc.ApplyToLevel(level);

            mapping.Mirrored.ApplyToLevel(level, _exclusions);

            WriteLevel(level);
            Console.WriteLine("Mod applied, level saved.");
        }

        static void ValidateOneOfCombined(EMEditorMapping mapping)
        {
            if (mapping.OneOf.Count > 1)
            {
                int setIndex1, setIndex2;
                if (mapping.OneOf.Count > 2)
                {
                    while (true)
                    {
                        setIndex1 = GetInt(string.Format("Choose the 1st set (0 - {0})", mapping.OneOf.Count - 1));
                        setIndex2 = GetInt(string.Format("Choose the 2nd set (0 - {0})", mapping.OneOf.Count - 1));
                        if (setIndex1 == setIndex2)
                        {
                            Console.WriteLine("Choose unique sets!");
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                else
                {
                    setIndex1 = 0;
                    setIndex2 = 1;
                }

                TR2Level level = DefaultLoadLevel(mapping);
                FDControl control = new FDControl();
                control.ParseFromLevel(level);

                EMEditorGroupedSet set1 = mapping.OneOf[setIndex1];
                EMEditorGroupedSet set2 = mapping.OneOf[setIndex2];
                for (int i = 0; i < set1.Followers.Count; i++)
                {
                    EMLocation slot1Location = GetSlotMoveLocation(set1.Followers[i]);
                    if (slot1Location != null)
                    {
                        TRRoomSector sector1 = FDUtilities.GetRoomSector(slot1Location.X, slot1Location.Y, slot1Location.Z, slot1Location.Room, level, control);
                        for (int j = 0; j < set2.Followers.Count; j++)
                        {
                            EMLocation slot2Location = GetSlotMoveLocation(set2.Followers[j]);
                            if (slot2Location != null)
                            {
                                TRRoomSector sector2 = FDUtilities.GetRoomSector(slot2Location.X, slot2Location.Y, slot2Location.Z, slot2Location.Room, level, control);
                                if (sector1 == sector2)
                                {
                                    Console.WriteLine("Set {0} follower {1} has the same location as set {2} follower {3}", setIndex1, i, setIndex2, j);
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("OneOf group requires at least 2 members.");
            }
        }

        static EMLocation GetSlotMoveLocation(EMEditorSet set)
        {
            foreach (BaseEMFunction f in set)
            {
                if (f is EMMoveSlotFunction s)
                {
                    return s.Location;
                }
            }
            return null;
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

            foreach (EMConditionalEditorSet condSet in mapping.ConditionalAllWithin)
            {
                foreach (EMEditorSet set in condSet.OnTrue)
                {
                    TestEditorSet(level, set);
                }
                foreach (EMEditorSet set in condSet.OnFalse)
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
            new EMMirrorFunction().ApplyToLevel(level);
            TestEditorSet(level, mapping.Mirrored);
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
                if (!(f is EMSwapSlotFunction))
                {
                    Console.WriteLine("Warning - null location found.");
                }
            }
            else if (!TestLocation(level, location))
            {
                Console.WriteLine("Location is outwith room bounds - " + ToString(location));
            }
            else if (!TestAngle(location.Angle))
            {
                Console.WriteLine("Location's angle % 16384 != 0 : " + location.Angle);
            }
            else if (f is EMMoveSlotFunction)
            {
                if (!TestFlip(level, location))
                {
                    // We don't want to move slots, switches etc in flip map rooms
                    Console.WriteLine("Room has flip map - check there are no flip map triggers - " + ToString(location));
                }
                if (!TestSlope(level, location))
                {
                    // Slots, levers etc don't work on sloped floors unless underwater
                    Console.WriteLine("Slot is positioned on a slope - " + ToString(location));
                }
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

        static bool TestFlip(TR2Level level, EMLocation location)
        {
            TR2Room room = level.Rooms[location.Room];
            if (room.AlternateRoom != -1)
            {
                FDControl control = new FDControl();
                control.ParseFromLevel(level);

                TRRoomSector flipSector = FDUtilities.GetRoomSector(location.X, location.Y, location.Z, room.AlternateRoom, level, control);
                if (flipSector.FDIndex != 0)
                {
                    List<FDEntry> triggers = control.Entries[flipSector.FDIndex].FindAll(e => e is FDTriggerEntry);
                    return triggers.Count == 0;
                }
            }
            return true;
        }

        static bool TestSlope(TR2Level level, EMLocation location)
        {
            if (level.Rooms[location.Room].ContainsWater)
            {
                return true;
            }

            FDControl control = new FDControl();
            control.ParseFromLevel(level);

            TRRoomSector sector = FDUtilities.GetRoomSector(location.X, location.Y, location.Z, location.Room, level, control);
            if (sector.FDIndex != 0)
            {
                List<FDEntry> slants = control.Entries[sector.FDIndex].FindAll(e => e is FDSlantEntry s && s.Type == FDSlantEntryType.FloorSlant);
                return slants.Count == 0;
            }

            return true;
        }

        static bool TestAngle(short angle)
        {
            return angle % 16384 == 0;
        }

        static string ToString(EMLocation location)
        {
            return string.Format("[X={0}, Y={1}, Z={2}, R={3}]", location.X, location.Y, location.Z, location.Room);
        }
    }
}