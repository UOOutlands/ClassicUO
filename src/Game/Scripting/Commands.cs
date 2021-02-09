#region license

// Copyright (C) 2020 ClassicUO Development Community on Github
// 
// This project is an alternative client for the game Ultima Online.
// The goal of this is to develop a lightweight client considering
// new technologies.
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;

namespace ClassicUO.Game.Scripting
{
    public static class Commands
    {
        // Milliseconds per tile
        private static readonly TimeSpan walkMs = TimeSpan.FromMilliseconds(400);
        private static readonly TimeSpan runMs = TimeSpan.FromMilliseconds(200);
        private static readonly TimeSpan mountedWalkMs = TimeSpan.FromMilliseconds(200);
        private static readonly TimeSpan mountedRunMs = TimeSpan.FromMilliseconds(100);
        private static readonly TimeSpan turnMs = TimeSpan.FromMilliseconds(Constants.TURN_DELAY);

        private static DateTime movementCooldown = DateTime.UtcNow;

        private static bool _hasPrompt = false;

        public static void Register()
        {
            Interpreter.RegisterCommandHandler("pushlist", PushList);
            Interpreter.RegisterCommandHandler("findobject", FindObject);
            Interpreter.RegisterCommandHandler("findtype", FindType);
            Interpreter.RegisterCommandHandler("fly", UnimplementedCommand);
            Interpreter.RegisterCommandHandler("land", UnimplementedCommand);
            Interpreter.RegisterCommandHandler("setability", SetAbility);
            Interpreter.RegisterCommandHandler("attack", Attack);
            Interpreter.RegisterCommandHandler("clearhands", ClearHands);
            Interpreter.RegisterCommandHandler("clickobject", ClickObject);
            Interpreter.RegisterCommandHandler("bandageself", BandageSelf);
            Interpreter.RegisterCommandHandler("usetype", UseType);
            Interpreter.RegisterCommandHandler("useobject", UseObject);
            Interpreter.RegisterCommandHandler("moveitem", MoveItem);
            Interpreter.RegisterCommandHandler("walk", Walk);
            Interpreter.RegisterCommandHandler("run", Run);
            Interpreter.RegisterCommandHandler("turn", Turn);
            Interpreter.RegisterCommandHandler("useskill", UseSkill);
            Interpreter.RegisterCommandHandler("feed", Feed);

            Interpreter.RegisterCommandHandler("unsetalias", UnsetAlias);
            Interpreter.RegisterCommandHandler("setalias", SetAlias);
            
            

            
            Interpreter.RegisterExpressionHandler("findobject", ExpFindObject);

            #region Deprecated (but supported)
            Interpreter.RegisterCommandHandler("useonce", UseOnce);
            Interpreter.RegisterCommandHandler("clearuseonce", Deprecated);
            #endregion

            #region Deprecated (not supported)
            Interpreter.RegisterCommandHandler("autoloot", Deprecated);
            Interpreter.RegisterCommandHandler("toggleautoloot", Deprecated);
            #endregion


            
            Interpreter.RegisterCommandHandler("msg", Msg);

            Interpreter.RegisterCommandHandler("pause", Pause);





            
            Interpreter.RegisterCommandHandler("rename", Rename);
            Interpreter.RegisterCommandHandler("shownames", ShowNames);
            Interpreter.RegisterCommandHandler("togglehands", ToggleHands);
            Interpreter.RegisterCommandHandler("equipitem", EquipItem);
            //Interpreter.RegisterCommandHandler("dress", DressCommand);
            //Interpreter.RegisterCommandHandler("undress", UnDressCommand);
            //Interpreter.RegisterCommandHandler("dressconfig", DressConfig);
            //Interpreter.RegisterCommandHandler("togglescavenger", ToggleScavenger);
            
            //Interpreter.RegisterCommandHandler("promptalias", PromptAlias);
            //Interpreter.RegisterCommandHandler("waitforgump", WaitForGump);
            //Interpreter.RegisterCommandHandler("clearjournal", ClearJournal);
            //Interpreter.RegisterCommandHandler("waitforjournal", WaitForJournal);
            //Interpreter.RegisterCommandHandler("poplist", PopList);
            Interpreter.RegisterCommandHandler("pushlist", PushList);
            //Interpreter.RegisterCommandHandler("removelist", RemoveList);
            //Interpreter.RegisterCommandHandler("createlist", CreateList);
            //Interpreter.RegisterCommandHandler("clearlist", ClearList);
            //Interpreter.RegisterCommandHandler("ping", Ping);
            //Interpreter.RegisterCommandHandler("resync", Resync);
            //Interpreter.RegisterCommandHandler("messagebox", MessageBox);
            //Interpreter.RegisterCommandHandler("paperdoll", Paperdoll);
            //Interpreter.RegisterCommandHandler("headmsg", HeadMsg);
            //Interpreter.RegisterCommandHandler("sysmsg", SysMsg);
            //Interpreter.RegisterCommandHandler("cast", Cast);
            //Interpreter.RegisterCommandHandler("waitfortarget", WaitForTarget);
            //Interpreter.RegisterCommandHandler("canceltarget", CancelTarget);
            //Interpreter.RegisterCommandHandler("target", Target);
            //Interpreter.RegisterCommandHandler("targettype", TargetType);
            //Interpreter.RegisterCommandHandler("targetground", TargetGround);
            //Interpreter.RegisterCommandHandler("targettile", TargetTile);
            //Interpreter.RegisterCommandHandler("targettileoffset", TargetTileOffset);
            //Interpreter.RegisterCommandHandler("targettilerelative", TargetTileRelative);
            //Interpreter.RegisterCommandHandler("settimer", SetTimer);
            //Interpreter.RegisterCommandHandler("removetimer", RemoveTimer);
            //Interpreter.RegisterCommandHandler("createtimer", CreateTimer);




            Interpreter.RegisterCommandHandler("info", UnimplementedCommand);
            Interpreter.RegisterCommandHandler("playmacro", UnimplementedCommand);
            Interpreter.RegisterCommandHandler("playsound", UnimplementedCommand);
            Interpreter.RegisterCommandHandler("snapshot", UnimplementedCommand);
            Interpreter.RegisterCommandHandler("hotkeys", UnimplementedCommand);
            Interpreter.RegisterCommandHandler("where", UnimplementedCommand);
            Interpreter.RegisterCommandHandler("mapuo", UnimplementedCommand);
            Interpreter.RegisterCommandHandler("clickscreen", UnimplementedCommand);
            Interpreter.RegisterCommandHandler("helpbutton", UnimplementedCommand);
            Interpreter.RegisterCommandHandler("guildbutton", UnimplementedCommand);
            Interpreter.RegisterCommandHandler("questsbutton", UnimplementedCommand);
            Interpreter.RegisterCommandHandler("logoutbutton", UnimplementedCommand);
            Interpreter.RegisterCommandHandler("virtue", UnimplementedCommand);
            Interpreter.RegisterCommandHandler("partymsg", UnimplementedCommand);
            Interpreter.RegisterCommandHandler("guildmsg", UnimplementedCommand);
            Interpreter.RegisterCommandHandler("allymsg", UnimplementedCommand);
            Interpreter.RegisterCommandHandler("whispermsg", UnimplementedCommand);
            Interpreter.RegisterCommandHandler("yellmsg", UnimplementedCommand);
            Interpreter.RegisterCommandHandler("chatmsg", UnimplementedCommand);
            Interpreter.RegisterCommandHandler("emotemsg", UnimplementedCommand);
            Interpreter.RegisterCommandHandler("promptmsg", UnimplementedCommand);
            Interpreter.RegisterCommandHandler("timermsg", UnimplementedCommand);
            Interpreter.RegisterCommandHandler("waitforprompt", UnimplementedCommand);
            Interpreter.RegisterCommandHandler("cancelprompt", UnimplementedCommand);
            Interpreter.RegisterCommandHandler("addfriend", UnimplementedCommand);
            Interpreter.RegisterCommandHandler("removefriend", UnimplementedCommand);
            Interpreter.RegisterCommandHandler("contextmenu", UnimplementedCommand);
            Interpreter.RegisterCommandHandler("waitforcontext", UnimplementedCommand);
            Interpreter.RegisterCommandHandler("ignoreobject", UnimplementedCommand);
            Interpreter.RegisterCommandHandler("clearignorelist", UnimplementedCommand);
            Interpreter.RegisterCommandHandler("setskill", UnimplementedCommand);
            Interpreter.RegisterCommandHandler("waitforproperties", UnimplementedCommand);
            Interpreter.RegisterCommandHandler("autocolorpick", UnimplementedCommand);
            Interpreter.RegisterCommandHandler("waitforcontents", UnimplementedCommand);
            Interpreter.RegisterCommandHandler("miniheal", UnimplementedCommand);
            Interpreter.RegisterCommandHandler("bigheal", UnimplementedCommand);
            Interpreter.RegisterCommandHandler("chivalryheal", UnimplementedCommand);
            Interpreter.RegisterCommandHandler("moveitemoffset", UnimplementedCommand);
            Interpreter.RegisterCommandHandler("movetype", UnimplementedCommand);
            Interpreter.RegisterCommandHandler("movetypeoffset", UnimplementedCommand);
            Interpreter.RegisterCommandHandler("togglemounted", UnimplementedCommand);
            Interpreter.RegisterCommandHandler("equipwand", UnimplementedCommand);
            Interpreter.RegisterCommandHandler("buy", UnimplementedCommand);
            Interpreter.RegisterCommandHandler("sell", UnimplementedCommand);
            Interpreter.RegisterCommandHandler("clearbuy", UnimplementedCommand);
            Interpreter.RegisterCommandHandler("clearsell", UnimplementedCommand);
            Interpreter.RegisterCommandHandler("organizer", UnimplementedCommand);
            Interpreter.RegisterCommandHandler("counter", UnimplementedCommand);
            Interpreter.RegisterCommandHandler("replygump", UnimplementedCommand);
            Interpreter.RegisterCommandHandler("closegump", UnimplementedCommand);
            Interpreter.RegisterCommandHandler("cleartargetqueue", UnimplementedCommand);
            Interpreter.RegisterCommandHandler("autotargetlast", UnimplementedCommand);
            Interpreter.RegisterCommandHandler("autotargetself", UnimplementedCommand);
            Interpreter.RegisterCommandHandler("autotargetobject", UnimplementedCommand);
            Interpreter.RegisterCommandHandler("autotargettype", UnimplementedCommand);
            Interpreter.RegisterCommandHandler("autotargettile", UnimplementedCommand);
            Interpreter.RegisterCommandHandler("autotargettileoffset", UnimplementedCommand);
            Interpreter.RegisterCommandHandler("autotargettilerelative", UnimplementedCommand);
            Interpreter.RegisterCommandHandler("autotargetghost", UnimplementedCommand);
            Interpreter.RegisterCommandHandler("autotargetground", UnimplementedCommand);
            Interpreter.RegisterCommandHandler("cancelautotarget", UnimplementedCommand);
            Interpreter.RegisterCommandHandler("getenemy", UnimplementedCommand);
            Interpreter.RegisterCommandHandler("getfriend", UnimplementedCommand);
        }

        private static bool PushList(string command, ArgumentList args, bool quiet, bool force)
        {
            try
            {
                var name = args.NextAs<string>(ArgumentList.ArgumentType.Mandatory);
                var values = args.NextAsArray<string>(ArgumentList.ArgumentType.Mandatory);
                var pos = args.NextAs<string>(ArgumentList.ArgumentType.Mandatory);
                bool front = (pos == "force");
                foreach (var val in values)
                {
                    //Interpreter.PushList(name, new Argument(), front, force);
                }
                return true;
            }
            catch (ScriptRunTimeError ex)
            {
                throw new ScriptSyntaxError("Usage: pushlist ('list name') ('element value') ['front'/'back']", ex);
            }
        }

        private static bool SetAbility(string command, ArgumentList args, bool quiet, bool force)
        {
            try
            {
                var ability = args.NextAs<string>(ArgumentList.ArgumentType.Mandatory);
                var toggle = args.NextAs<string>(ArgumentList.ArgumentType.Mandatory);
                if (toggle == "on")
                {
                    switch (ability)
                    {
                        case "primary":
                            GameActions.UsePrimaryAbility();
                            break;
                        case "secondary":
                            GameActions.UseSecondaryAbility();
                            break;
                        case "stun":
                            GameActions.RequestStun();
                            break;
                        case "disarm":
                            GameActions.RequestDisarm();
                            break;
                    }
                }
                else
                {
                    GameActions.ClearAbility();
                }
                return true;
            }
            catch (ScriptRunTimeError ex)
            {
                throw new ScriptSyntaxError("Usage: setability ('primary'/'secondary'/'stun'/'disarm') ['on'/'off']", ex);
            }
        }

        private static bool Attack(string command, ArgumentList args, bool quiet, bool force)
        {
            try
            {
                var serial = args.NextAs<uint>(ArgumentList.ArgumentType.Mandatory);
                GameActions.Attack(serial);
                return true;
            }
            catch (ScriptRunTimeError ex)
            {
                throw new ScriptSyntaxError("Usage: attack (serial)", ex);
            }
        }

        private static bool ClearHands(string command, ArgumentList args, bool quiet, bool force)
        {
            try
            {
                var hands = args.NextAsHand(ArgumentList.ArgumentType.Mandatory);
                if (hands == ArgumentList.Hands.Both)
                {
                    GameActions.ClearEquipped((IO.ItemExt_PaperdollAppearance)ArgumentList.Hands.Left);
                    GameActions.ClearEquipped((IO.ItemExt_PaperdollAppearance)ArgumentList.Hands.Right);
                }
                else GameActions.ClearEquipped((IO.ItemExt_PaperdollAppearance)hands);
                // TODO: check if hands are cleared to return false on fail (can you fail to clear hands? Maybe trying to unequipe during use of wand, or cast of spell?)
                return true;
            }
            catch (ScriptRunTimeError ex)
            {
                throw new ScriptSyntaxError("Usage: clearhands ('left'/'right'/'both')", ex);
            }
        }

        private static bool ClickObject(string command, ArgumentList args, bool quiet, bool force)
        {
            try
            {
                var serial = args.NextAsSerial(ArgumentList.ArgumentType.Mandatory);
                GameActions.SingleClick(serial);
                return true;
            }
            catch (ScriptRunTimeError ex)
            {
                throw new ScriptSyntaxError("Usage: clickobject (serial)", ex);
            }
        }

        private static bool BandageSelf(string command, ArgumentList args, bool quiet, bool force)
        {
            GameActions.BandageSelf();
            // TODO: maybe return false if no badages or other constraints?
            return true;
        }

        private static bool UseType(string command, ArgumentList args, bool quiet, bool force)
        {
            try
            {
                var graphic = args.NextAs<ushort>(ArgumentList.ArgumentType.Mandatory);
                Item item = CmdFindEntityByGraphic(graphic,
                    args.NextAs<ushort>(),
                    args.NextAsSource(),
                    args.NextAs<int>());

                if (item != null)
                    GameActions.DoubleClick(item.Serial);
                else
                    throw new ScriptRunTimeError(null, $"Script Error: Couldn't find '{graphic.ToString("X3")}'");

                return true;
            }
            catch (ScriptRunTimeError ex)
            {
                throw new ScriptSyntaxError("Usage: usetype (graphic) [color] [source] [range or search level]", ex);
            }
        }

        private static bool UseObject(string command, ArgumentList args, bool quiet, bool force)
        {
            try
            {
                var serial = args.NextAsSerial(ArgumentList.ArgumentType.Mandatory);
                GameActions.DoubleClick(serial);
                return true;
            }
            catch (ScriptRunTimeError ex)
            {
                throw new ScriptSyntaxError("Usage: useobject (serial)", ex);
            }
        }

        private static bool UseOnce(string command, ArgumentList args, bool quiet, bool force)
        {
            try
            {
                var graphic = args.NextAs<ushort>(ArgumentList.ArgumentType.Mandatory);
                Item item = CmdFindEntityByGraphic(graphic, args.NextAs<ushort>());

                if (item != null)
                    GameActions.DoubleClick(item.Serial);
                else
                    throw new ScriptRunTimeError(null, $"Script Error: Couldn't find '{graphic.ToString("X3")}'");

                return true;
            }
            catch (ScriptRunTimeError ex)
            {
                throw new ScriptSyntaxError("Usage: useonce (graphic) [color]", ex);
            }
        }

        private static bool MoveItem(string command, ArgumentList args, bool quiet, bool force)
        {
            try
            {
                var serial = args.NextAsSerial(ArgumentList.ArgumentType.Mandatory);

                // Destination can be both a Serial or Source (backpack, ground, etc)
                uint destination;
                if (!args.TryAsSerial(out destination))
                {
                    var source = args.NextAsSource(ArgumentList.ArgumentType.Mandatory);
                    destination = World.Player.FindItemByLayer((Layer)source).Serial;
                }
                //offest = new ArgumentList.Position((int)World.Player.X, (int)World.Player.Y, (int)World.Player.Z);
                var x = args.NextAs<int>();
                var y = args.NextAs<int>();
                var z = args.NextAs<int>();
                var amount = args.NextAs<int>();

                GameActions.PickUp(serial, 0, 0, amount);
                GameActions.DropItem(
                    serial,
                    x+ World.Player.X,
                    y + World.Player.Y,
                    z + World.Player.Z,
                    destination);

                return true;
            }
            catch (ScriptRunTimeError ex)
            {
                throw new ScriptSyntaxError("Usage: moveitem (serial) (destination) [(x, y, z)] [amount]", ex);
            }
        }

        private static bool Walk(string command, ArgumentList args, bool quiet, bool force)
        {
            try
            {
                var direction = args.NextAsDirection(ArgumentList.ArgumentType.Mandatory);

                //if (World.Player.Direction != (Direction)direction.First())
                //{
                //    // if the player is not currently facing into the direction of the run
                //    // then the player will turn first, and run next - this helps so that 
                //    // scripts do not need to include "turn" commands or call "run" multiple
                //    // times just to achieve the same result
                //    movementCooldown = DateTime.UtcNow + turnMs;
                //    World.Player.Walk((Direction)direction.First(), true);
                //    return false;
                //}

                var movementDelay = ProfileManager.CurrentProfile.AlwaysRun ? runMs : walkMs;

                if (World.Player.FindItemByLayer(Layer.Mount) != null)
                {
                    if (ProfileManager.CurrentProfile.AlwaysRun)
                    {
                        movementDelay = mountedRunMs;
                    }
                    else
                    {
                        movementDelay = mountedWalkMs;
                    }
                }

                movementCooldown = DateTime.UtcNow + movementDelay;

                foreach (var dir in direction)
                {
                    World.Player.Walk((Direction)dir, ProfileManager.CurrentProfile.AlwaysRun);
                    //if (!World.Player.Walk((Direction)dir, ProfileManager.CurrentProfile.AlwaysRun))
                    //return false;
                }
                return true;
            }
            catch (ScriptRunTimeError ex)
            {
                throw new ScriptSyntaxError("Usage: walk ('direction name')", ex);
            }
        }

        private static bool Turn(string command, ArgumentList args, bool quiet, bool force)
        {
            try
            {
                var direction = args.NextAsDirection(ArgumentList.ArgumentType.Mandatory);

                if (DateTime.UtcNow < movementCooldown)
                {
                    return false;
                }

                foreach (var dir in direction)
                {
                    if (World.Player.Direction != (Direction)dir)
                    {
                        movementCooldown = DateTime.UtcNow + turnMs;
                        if(!World.Player.Walk((Direction)dir, true))
                            return false;
                    }
                }
                return true;
            }
            catch (ScriptRunTimeError ex)
            {
                throw new ScriptSyntaxError("Usage: walk ('direction name')", ex);
            }
        }

        private static bool Run(string command, ArgumentList args, bool quiet, bool force)
        {
            try
            {
                var direction = args.NextAsDirection(ArgumentList.ArgumentType.Mandatory);

                if(DateTime.UtcNow < movementCooldown)
                {
                    return false;
                }

                //if (World.Player.Direction != (Direction)direction.First())
                //{
                //    // if the player is not currently facing into the direction of the run
                //    // then the player will turn first, and run next - this helps so that 
                //    // scripts do not need to include "turn" commands or call "run" multiple
                //    // times just to achieve the same result
                //    movementCooldown = DateTime.UtcNow + turnMs;
                //    World.Player.Walk((Direction)direction.First(), true);
                //    return false;
                //}

                var movementDelay = runMs;

                if (World.Player.FindItemByLayer(Layer.Mount) != null)
                {
                    movementDelay = mountedRunMs;
                }

                movementCooldown = DateTime.UtcNow + movementDelay;

                foreach (var dir in direction)
                {
                    if (!World.Player.Walk((Direction)dir, true))
                        return false;
                }
                return true;
            }
            catch (ScriptRunTimeError ex)
            {
                throw new ScriptSyntaxError("Usage: walk ('direction name')", ex);
            }
        }

        private static bool UseSkill(string command, ArgumentList args, bool quiet, bool force)
        {
            try
            {
                if (args[0].As<string>() == "last")
                {
                    GameActions.UseLastSkill();
                    return true;
                }
                string skillName = args[0].As<string>();

                if (!GameActions.UseSkill(skillName))
                {
                    throw new ScriptRunTimeError(null, "That skill  is not usable");
                }
                return true;
            }
            catch (ScriptRunTimeError ex)
            {
                throw new ScriptSyntaxError("Usage: useskill ('skill name'/'last')", ex);
            }
        }

        private static bool Feed(string command, ArgumentList args, bool quiet, bool force)
        {
            try
            {
                var serial = args.NextAsSerial(ArgumentList.ArgumentType.Mandatory);
                List<ushort> foodList = new List<ushort>();
                //ushort graphic;
                //if (!args.TryAsGraphic(out graphic))
                //{
                //    var source = args.NextAsSource(ArgumentList.ArgumentType.Mandatory);
                //    destination = World.Player.FindItemByLayer((Layer)source).Serial;
                //}
                //else foodList.Add(graphic)
                var color = args.NextAs<ushort>();
                var amount = args.NextAs<int>();

                //Item item = CmdFindObjectBySerial(serial, color);//, source, range);
                //return item != null && item.Amount > amount;
                return true;
            }
            catch (ScriptRunTimeError ex)
            {
                throw new ScriptSyntaxError("Usage: feed (serial) ('food name'/'food group'/'any'/graphic) [color] [amount]", ex);
            }
        }

        private static bool FindObject(string command, ArgumentList args, bool quiet, bool force)
        {
            try
            {
                var serial = args.NextAsSerial(ArgumentList.ArgumentType.Mandatory);
                var color = args.NextAs<ushort>();
                var source = args.NextAsSource();
                var amount = args.NextAs<int>();
                var range = args.NextAs<int>();

                Entity entity = CmdFindEntityBySerial(serial, color, source, range);
                if (entity != null)
                {
                    if ((entity is Item item && item.Amount > amount) || entity is Mobile)
                        Interpreter.SetAlias<uint>("found", entity.Serial); // should this be a scope variable?
                    return true;
                }
                else return false;
            }
            catch (ScriptRunTimeError ex)
            {
                throw new ScriptSyntaxError("Usage: findobject (serial) [color] [source] [amount] [range]", ex);
            }
        }

        private static bool FindType(string command, ArgumentList args, bool quiet, bool force)
        {
            try
            {
                var graphic = args.NextAs<ushort>(ArgumentList.ArgumentType.Mandatory);
                var color = args.NextAs<ushort>();
                var source = args.NextAsSource();
                var amount = args.NextAs<int>();
                var range = args.NextAs<int>();

                Item item = CmdFindEntityByGraphic(graphic, color, source, range);
                return item != null && item.Amount > amount;
            }
            catch (ScriptRunTimeError ex)
            {
                throw new ScriptSyntaxError("Usage: findtype (graphic) [color] [source] [amount] [range or search level]", ex);
            }
        }


        private static bool UnimplementedCommand(string command, ArgumentList args, bool quiet, bool force)
        {
            GameActions.Print($"Unimplemented command: '{command}'", type: MessageType.System);
            return true;
        }

        private static bool Deprecated(string command, ArgumentList args, bool quiet, bool force)
        {
            GameActions.Print($"Deprecated command: '{command}'", type: MessageType.System);
            return true;
        }

        

        private static bool ExpFindObject(string expression, ArgumentList args, bool quiet)
        {
            return FindObject(expression, args, quiet, false);
        }



        //private static bool UseItem(Item cont, ushort find)
        //{
        //    for (int i = 0; i < cont.Contains.Count; i++)
        //    {
        //        Item item = cont.Contains[i];

        //        if (item.ItemID == find)
        //        {
        //            PlayerData.DoubleClick(item);
        //            return true;
        //        }
        //        else if (item.Contains != null && item.Contains.Count > 0)
        //        {
        //            if (UseItem(item, find))
        //                return true;
        //        }
        //    }

        //    return false;
        //}

        

        

        

       

        

        private static bool Rename(string command, ArgumentList args, bool quiet, bool force)
        {
            if (args.Length != 2)
            {
                throw new ScriptRunTimeError(null, "Usage: rename (serial) ('name')");
            }

            var target = args[0].As<uint>();
            var name = args[1].As<string>();

            GameActions.Rename(target, name);

            return true;
        }

        private static bool SetAlias(string command, ArgumentList args, bool quiet, bool force)
        {
            if (args.Length != 2)
                throw new ScriptRunTimeError(null, "Usage: setalias ('name') [serial]");

            Interpreter.SetAlias(args[0].As<string>(), args[1].As<uint>());

            return true;
        }

        //private static bool PromptAlias(string command, ArgumentList args, bool quiet, bool force)
        //{
        //    Interpreter.Pause(60000);

        //    if (!_hasPrompt)
        //    {
        //        _hasPrompt = true;
        //        Targeting.OneTimeTarget((location, serial, p, gfxid) =>
        //        {
        //            Interpreter.SetAlias(args[0].As<string>(), serial);
        //            Interpreter.Unpause();
        //        });
        //        return false;
        //    }

        //    _hasPrompt = false;
        //    return true;
        //}

        //private static bool WaitForGump(string command, ArgumentList args, bool quiet, bool force)
        //{
        //    if (args.Length < 2)
        //        throw new RunTimeError(null, "Usage: waitforgump (gump id/'any') (timeout)");

        //    bool any = args[0].As<string>() == "any";

        //    if (any)
        //    {
        //        if (World.Player.HasGump || World.Player.HasCompressedGump)
        //            return true;
        //    }
        //    else
        //    {
        //        uint gumpId = args[0].AsSerial();

        //        if (World.Player.CurrentGumpI == gumpId)
        //            return true;
        //    }

        //    Interpreter.Timeout(args[1].AsUInt(), () => { return true; });
        //    return false;
        //}

        //private static bool ClearJournal(string command, ArgumentList args, bool quiet, bool force)
        //{
        //    Journal.Clear();

        //    return true;
        //}

        //private static bool WaitForJournal(string command, ArgumentList args, bool quiet, bool force)
        //{
        //    if (args.Length < 2)
        //        throw new RunTimeError(null, "Usage: waitforjournal ('text') (timeout) ['author'/'system']");

        //    if (!Journal.ContainsSafe(args[0].As<string>()))
        //    {
        //        Interpreter.Timeout(args[1].AsUInt(), () => { return true; });
        //        return false;
        //    }

        //    return true;
        //}

        //private static bool PopList(string command, ArgumentList args, bool quiet, bool force)
        //{
        //    if (args.Length != 2)
        //        throw new RunTimeError(null, "Usage: poplist ('list name') ('element value'/'front'/'back')");

        //    if (args[1].As<string>() == "front")
        //    {
        //        if (force)
        //            while (Interpreter.PopList(args[0].As<string>(), true)) { }
        //        else
        //            Interpreter.PopList(args[0].As<string>(), true);
        //    }
        //    else if (args[1].As<string>() == "back")
        //    {
        //        if (force)
        //            while (Interpreter.PopList(args[0].As<string>(), false)) { }
        //        else
        //            Interpreter.PopList(args[0].As<string>(), false);
        //    }
        //    else
        //    {
        //        if (force)
        //            while (Interpreter.PopList(args[0].As<string>(), args[1])) { }
        //        else
        //            Interpreter.PopList(args[0].As<string>(), args[1]);
        //    }

        //    return true;
        //}

        

        //private static bool RemoveList(string command, ArgumentList args, bool quiet, bool force)
        //{
        //    if (args.Length != 1)
        //        throw new RunTimeError(null, "Usage: removelist ('list name')");

        //    Interpreter.DestroyList(args[0].As<string>());

        //    return true;
        //}

        //private static bool CreateList(string command, ArgumentList args, bool quiet, bool force)
        //{
        //    if (args.Length != 1)
        //        throw new RunTimeError(null, "Usage: createlist ('list name')");

        //    Interpreter.CreateList(args[0].As<string>());

        //    return true;
        //}

        //private static bool ClearList(string command, ArgumentList args, bool quiet, bool force)
        //{
        //    if (args.Length != 1)
        //        throw new RunTimeError(null, "Usage: clearlist ('list name')");

        //    Interpreter.ClearList(args[0].As<string>());

        //    return true;
        //}

        private static bool UnsetAlias(string command, ArgumentList args, bool quiet, bool force)
        {
            if (args.Length == 0)
                throw new ScriptRunTimeError(null, "Usage: unsetalias (string)");

            Interpreter.SetAlias(args[0].As<string>(), 0);

            return true;
        }

        private static bool ShowNames(string command, ArgumentList args, bool quiet, bool force)
        {
            if (args.Length == 0)
                throw new ScriptRunTimeError(null, "Usage: shownames ['mobiles'/'corpses']");

            if (args[0].As<string>() == "mobiles")
            {
                GameActions.AllNames(GameActions.AllNamesTargets.Mobiles);
            }
            else if (args[0].As<string>() == "corpses")
            {
                GameActions.AllNames(GameActions.AllNamesTargets.Corpses);
            }
            return true;
        }

        public static bool ToggleHands(string command, ArgumentList args, bool quiet, bool force)
        {
            if (args.Length == 0)
            {
                throw new ScriptRunTimeError(null, "Usage: togglehands ('left'/'right')");
            }

            switch (args[0].As<string>())
            {
                case "left":
                    GameActions.ToggleEquip(IO.ItemExt_PaperdollAppearance.Left);
                    break;
                case "right":
                    GameActions.ToggleEquip(IO.ItemExt_PaperdollAppearance.Right);
                    break;
                default:
                    throw new ScriptRunTimeError(null, "Usage: togglehands ('left'/'right')");
            }

            return true;
        }

        public static bool EquipItem(string command, ArgumentList args, bool quiet, bool force)
        {
            if (args.Length < 1)
            {
                throw new ScriptRunTimeError(null, "Usage: equipitem (serial)");
            }

            var item = (Item)World.Get(args[0].As<uint>());

            if (item != null)
            {
                GameActions.Equip(item);
            }

            return true;
        }

        //public static bool ToggleScavenger(string command, ArgumentList args, bool quiet, bool force)
        //{
        //    ScavengerAgent.Instance.ToggleEnabled();

        //    return true;
        //}

        private static bool Pause(string command, ArgumentList args, bool quiet, bool force)
        {
            if (args.Length == 0)
                throw new ScriptRunTimeError(null, "Usage: pause (timeout)");

            Interpreter.Pause(args[0].As<uint>());
            return true;
        }

        //private static bool Ping(string command, ArgumentList args, bool quiet, bool force)
        //{
        //    Assistant.Ping.StartPing(5);

        //    return true;
        //}

        //private static bool Resync(string command, ArgumentList args, bool quiet, bool force)
        //{
        //    Client.Instance.SendToServer(new ResyncReq());

        //    return true;
        //}

        //private static bool MessageBox(string command, ArgumentList args, bool quiet, bool force)
        //{
        //    if (args.Length != 2)
        //        throw new RunTimeError(null, "Usage: messagebox ('title') ('body')");

        //    System.Windows.Forms.MessageBox.Show(args[0].As<string>(), args[1].As<string>());

        //    return true;
        //}

        public static bool Msg(string command, ArgumentList args, bool quiet, bool force)
        {
            switch (args.Length)
            {
                case 1:
                    GameActions.Say(args[0].As<string>());
                    break;
                case 2:
                    GameActions.Say(args[0].As<string>(), hue: args[1].As<ushort>());
                    break;
                default:
                    throw new ScriptRunTimeError(null, "Usage: msg ('text') [color]");
            }

            return true;
        }

        //private static bool Paperdoll(string command, ArgumentList args, bool quiet, bool force)
        //{
        //    if (args.Length > 1)
        //        throw new RunTimeError(null, "Usage: paperdoll [serial]");

        //    uint serial = args.Length == 0 ? World.Player.Serial.Value : args[0].AsSerial();
        //    Client.Instance.SendToServer(new DoubleClick(serial));

        //    return true;
        //}

        //public static bool Cast(string command, ArgumentList args, bool quiet, bool force)
        //{
        //    if (args.Length == 0)
        //        throw new RunTimeError(null, "Usage: cast 'spell' [serial]");

        //    Spell spell;

        //    if (int.TryParse(args[0].As<string>(), out int spellnum))
        //        spell = Spell.Get(spellnum);
        //    else
        //        spell = Spell.GetByName(args[0].As<string>());
        //    if (spell != null)
        //    {
        //        if (args.Length > 1)
        //        {
        //            Serial s = args[1].AsSerial();
        //            if (force)
        //                Targeting.ClearQueue();
        //            if (s > Serial.Zero && s != Serial.MinusOne)
        //            {
        //                Targeting.Target(s);
        //            }
        //            else if (!quiet)
        //                throw new RunTimeError(null, "cast - invalid serial or alias");
        //        }
        //    }
        //    else if (!quiet)
        //        throw new RunTimeError(null, "cast - spell name or number not valid");

        //    return true;
        //}

        //private static bool WaitForTarget(string command, ArgumentList args, bool quiet, bool force)
        //{
        //    if (args.Length != 1)
        //        throw new RunTimeError(null, "Usage: waitfortarget (timeout)");

        //    if (Targeting.HasTarget)
        //        return true;

        //    Interpreter.Timeout(args[0].AsUInt(), () => { return true; });
        //    return false;
        //}

        //private static bool CancelTarget(string command, ArgumentList args, bool quiet, bool force)
        //{
        //    if (args.Length != 0)
        //        throw new RunTimeError(null, "Usage: canceltarget");

        //    if (Targeting.HasTarget)
        //        Targeting.CancelOneTimeTarget();

        //    return true;
        //}

        //private static bool Target(string command, ArgumentList args, bool quiet, bool force)
        //{
        //    if (args.Length != 1)
        //        throw new RunTimeError(null, "Usage: target (serial)");

        //    if (!Targeting.HasTarget)
        //        ScriptManager.Error(quiet, command, "No target cursor available. Consider using waitfortarget.");
        //    else
        //        Targeting.Target(args[0].AsSerial());

        //    return true;
        //}

        //private static bool TargetType(string command, ArgumentList args, bool quiet, bool force)
        //{
        //    if (args.Length < 1 || args.Length > 3)
        //        throw new RunTimeError(null, "Usage: targettype (graphic) [color] [range]");

        //    if (!Targeting.HasTarget)
        //    {
        //        ScriptManager.Error(quiet, command, "No target cursor available. Consider using waitfortarget.");
        //        return true;
        //    }

        //    var graphic = args[0].AsInt();

        //    uint serial = Serial.MinusOne;

        //    switch (args.Length)
        //    {
        //        case 1:
        //            // Only graphic
        //            serial = World.FindItemByType(graphic).Serial;
        //            break;
        //        case 2:
        //            {
        //                // graphic and color
        //                var color = args[1].AsUShort();
        //                foreach (var item in World.Items.Values)
        //                {
        //                    if (item.ItemID.Value == graphic && item.Hue == color)
        //                    {
        //                        serial = item.Serial;
        //                        break;
        //                    }
        //                }
        //                break;
        //            }
        //        case 3:
        //            {
        //                // graphic, color, range
        //                var color = args[1].AsUShort();
        //                var range = args[2].AsInt();
        //                foreach (var item in World.Items.Values)
        //                {
        //                    if (item.ItemID.Value == graphic && item.Hue == color && Utility.Distance(item.Position, World.Player.Position) < range)
        //                    {
        //                        serial = item.Serial;
        //                        break;
        //                    }
        //                }
        //                break;
        //            }

        //    }

        //    if (serial == Serial.MinusOne)
        //        throw new RunTimeError(null, "Unable to find suitable target");

        //    Targeting.Target(serial);
        //    return true;
        //}

        //private static bool TargetGround(string command, ArgumentList args, bool quiet, bool force)
        //{
        //    if (args.Length < 1 || args.Length > 3)
        //        throw new RunTimeError(null, "Usage: targetground (graphic) [color] [range]");

        //    throw new RunTimeError(null, $"Unimplemented command {command}");
        //}

        //private static bool TargetTile(string command, ArgumentList args, bool quiet, bool force)
        //{
        //    if (!(args.Length == 1 || args.Length == 3))
        //        throw new RunTimeError(null, "Usage: targettile ('last'/'current'/(x y z))");

        //    if (!Targeting.HasTarget)
        //    {
        //        ScriptManager.Error(quiet, command, "No target cursor available. Consider using waitfortarget.");
        //        return true;
        //    }

        //    Point3D position = Point3D.MinusOne;

        //    switch (args.Length)
        //    {
        //        case 1:
        //            {
        //                var alias = args[0].As<string>();
        //                if (alias == "last")
        //                {
        //                    if (Targeting.LastTargetInfo.Type != 1)
        //                        throw new RunTimeError(null, "Last target was not a ground target");

        //                    position = new Point3D(Targeting.LastTargetInfo.X, Targeting.LastTargetInfo.Y, Targeting.LastTargetInfo.Z);
        //                }
        //                else if (alias == "current")
        //                {
        //                    position = World.Player.Position;
        //                }
        //                break;
        //            }
        //        case 3:
        //            position = new Point3D(args[0].AsInt(), args[1].AsInt(), args[2].AsInt());
        //            break;
        //    }

        //    if (position == Point3D.MinusOne)
        //        throw new RunTimeError(null, "Usage: targettile ('last'/'current'/(x y z))");

        //    Targeting.Target(position);
        //    return true;
        //}

        //private static bool TargetTileOffset(string command, ArgumentList args, bool quiet, bool force)
        //{
        //    if (args.Length != 3)
        //        throw new RunTimeError(null, "Usage: targettileoffset (x y z)");

        //    if (!Targeting.HasTarget)
        //    {
        //        ScriptManager.Error(quiet, command, "No target cursor available. Consider using waitfortarget.");
        //        return true;
        //    }

        //    var position = World.Player.Position;

        //    position.X += args[0].AsInt();
        //    position.Y += args[1].AsInt();
        //    position.Z += args[2].AsInt();

        //    Targeting.Target(position);
        //    return true;
        //}

        //private static bool TargetTileRelative(string command, ArgumentList args, bool quiet, bool force)
        //{
        //    if (args.Length != 2)
        //        throw new RunTimeError(null, "Usage: targettilerelative (serial) (range). Range may be negative.");

        //    if (!Targeting.HasTarget)
        //    {
        //        ScriptManager.Error(quiet, command, "No target cursor available. Consider using waitfortarget.");
        //        return true;
        //    }

        //    var serial = args[0].AsSerial();
        //    var range = args[1].AsInt();

        //    var mobile = World.FindMobile(serial);

        //    if (mobile == null)
        //    {
        //        /* TODO: Search items if mobile not found. Although this isn't very useful. */
        //        ScriptManager.Error(quiet, command, "item or mobile not found.");
        //        return true;
        //    }

        //    var position = mobile.Position;

        //    switch (mobile.Direction)
        //    {
        //        case Direction.North:
        //            position.Y -= range;
        //            break;
        //        case Direction.Right:
        //            position.X += range;
        //            position.Y -= range;
        //            break;
        //        case Direction.East:
        //            position.X += range;
        //            break;
        //        case Direction.Down:
        //            position.X += range;
        //            position.Y += range;
        //            break;
        //        case Direction.South:
        //            position.Y += range;
        //            break;
        //        case Direction.Left:
        //            position.X -= range;
        //            position.Y += range;
        //            break;
        //        case Direction.West:
        //            position.X -= range;
        //            break;
        //        case Direction.Up:
        //            position.X -= range;
        //            position.Y -= range;
        //            break;
        //    }

        //    Targeting.Target(position);

        //    return true;
        //}

        //public static bool HeadMsg(string command, ArgumentList args, bool quiet, bool force)
        //{
        //    switch (args.Length)
        //    {
        //        case 1:
        //            World.Player.OverheadMessage(Config.GetInt("SysColor"), args[0].As<string>());
        //            break;
        //        case 2:
        //            World.Player.OverheadMessage(args[1].AsInt(), args[0].As<string>());
        //            break;
        //        case 3:
        //            Mobile m = World.FindMobile(args[2].AsSerial());

        //            if (m != null)
        //                m.OverheadMessage(args[1].AsInt(), args[0].As<string>());
        //            break;
        //        default:
        //            throw new RunTimeError(null, "Usage: headmsg (text) [color] [serial]");
        //    }

        //    return true;
        //}

        //public static bool SysMsg(string command, ArgumentList args, bool quiet, bool force)
        //{
        //    switch (args.Length)
        //    {
        //        case 1:
        //            World.Player.SendMessage(Config.GetInt("SysColor"), args[0].As<string>());
        //            break;
        //        case 2:
        //            World.Player.SendMessage(args[1].AsInt(), args[0].As<string>());
        //            break;
        //        default:
        //            throw new RunTimeError(null, "Usage: sysmsg ('text') [color]");
        //    }

        //    return true;
        //}

        //public static bool DressCommand(string command, ArgumentList args, bool quiet, bool force)
        //{
        //    //we're using a named dresslist or a temporary dresslist?
        //    if (args.Length == 0)
        //    {
        //        if (DressList._Temporary != null)
        //            DressList._Temporary.Dress();
        //        else if (!quiet)
        //            throw new RunTimeError(null, "No dresslist specified and no temporary dressconfig present - usage: dress ['dresslist']");
        //    }
        //    else
        //    {
        //        var d = DressList.Find(args[0].As<string>());
        //        if (d != null)
        //            d.Dress();
        //        else if (!quiet)
        //            throw new RunTimeError(null, $"dresslist {args[0].As<string>()} not found");
        //    }

        //    return true;
        //}

        //public static bool UnDressCommand(string command, ArgumentList args, bool quiet, bool force)
        //{
        //    //we're using a named dresslist or a temporary dresslist?
        //    if (args.Length == 0)
        //    {
        //        if (DressList._Temporary != null)
        //            DressList._Temporary.Undress();
        //        else if (!quiet)
        //            throw new RunTimeError(null, "No dresslist specified and no temporary dressconfig present - usage: undress ['dresslist']");
        //    }
        //    else
        //    {
        //        var d = DressList.Find(args[0].As<string>());
        //        if (d != null)
        //            d.Undress();
        //        else if (!quiet)
        //            throw new RunTimeError(null, $"dresslist {args[0].As<string>()} not found");
        //    }

        //    return true;
        //}

        //public static bool DressConfig(string command, ArgumentList args, bool quiet, bool force)
        //{
        //    if (DressList._Temporary == null)
        //        DressList._Temporary = new DressList("dressconfig");

        //    DressList._Temporary.Items.Clear();
        //    for (int i = 0; i < World.Player.Contains.Count; i++)
        //    {
        //        Item item = World.Player.Contains[i];
        //        if (item.Layer <= Layer.LastUserValid && item.Layer != Layer.Backpack && item.Layer != Layer.Hair &&
        //            item.Layer != Layer.FacialHair)
        //            DressList._Temporary.Items.Add(item.Serial);
        //    }

        //    return true;
        //}

        //private static bool SetTimer(string command, ArgumentList args, bool quiet, bool force)
        //{
        //    if (args.Length != 2)
        //        throw new RunTimeError(null, "Usage: settimer (timer name) (value)");


        //    Interpreter.SetTimer(args[0].As<string>(), args[1].AsInt());
        //    return true;
        //}

        //private static bool RemoveTimer(string command, ArgumentList args, bool quiet, bool force)
        //{
        //    if (args.Length != 1)
        //        throw new RunTimeError(null, "Usage: removetimer (timer name)");

        //    Interpreter.RemoveTimer(args[0].As<string>());
        //    return true;
        //}

        //private static bool CreateTimer(string command, ArgumentList args, bool quiet, bool force)
        //{
        //    if (args.Length != 1)
        //        throw new RunTimeError(null, "Usage: createtimer (timer name)");

        //    Interpreter.CreateTimer(args[0].As<string>());
        //    return true;
        //}

        private static Entity CmdFindEntityBySerial(uint serial, ushort color, ArgumentList.Sources source, int range)
        {
            // Try retrieving an Item
            Entity entity = null;
            var graphic = World.GetOrCreateItem(serial).Graphic;
            if (source == ArgumentList.Sources.Any) 
            {
                // Any also look at the ground if not found in player belongings
                entity = World.Player.FindItem(graphic, color);
                if(entity != null)
                    entity = World.Player.FindItemByTypeOnGroundWithHueInRange(graphic, color, range);
            }
            else if (source == ArgumentList.Sources.Ground)
                entity = World.Player.FindItemByTypeOnGroundWithHueInRange(graphic, color, range);
            else
                entity = World.Player.FindItemByLayer((Layer)source)?.FindItem(graphic, color);

            // Try retrieving a Mobile
            if (entity == null)
                entity = World.GetOrCreateMobile(serial);

            return entity;
        }

        private static Item CmdFindEntityByGraphic(ushort graphic, ushort color = ushort.MaxValue, ArgumentList.Sources source = ArgumentList.Sources.Any, int range = ArgumentList.DefaultRange)
        {
            Item item = null;
            if (source == ArgumentList.Sources.Any)
            {
                item = World.Player.FindItem(graphic, (ushort)color);
                if (item != null) // For Any, also look at the ground if not found in player belongings
                    item = World.Player.FindItemByTypeOnGroundWithHueInRange(graphic, (ushort)color, range);
            }
            else if (source == ArgumentList.Sources.Ground)
                item = World.Player.FindItemByTypeOnGroundWithHueInRange(graphic, (ushort)color, range);
            else
                item = World.Player.FindItemByLayer((Layer)source)?.FindItem(graphic, (ushort)color);

            return item;
        }
    }
}
