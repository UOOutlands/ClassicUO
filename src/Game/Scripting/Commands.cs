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
using System.Text.RegularExpressions;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;

namespace ClassicUO.Game.Scripting
{
    // Execution descriptor
    public class CommandExecution
    {
        // Command to be executed
        public Command Cmd { get; }

        // Parameters given of this specific execution
        public ParameterList Params { get; }

        // Indicated if the force (!) modifier was used
        public bool Force { get; }

        // Time execution was created/requested
        public DateTime CreationTime { get; }

        // Provide access to the time a given command was last executed
        private static Dictionary<string, DateTime> _lastExecutedRegistry = new Dictionary<string, DateTime>();
        public static DateTime LastExecuted(params string[] commandKeywords)
        {
            DateTime lastExecuted = DateTime.UtcNow.AddHours(-2.0); // 2 hours ago
            foreach(string key in commandKeywords)
            {
                if (_lastExecutedRegistry.ContainsKey(key) && _lastExecutedRegistry[key] > lastExecuted)
                    lastExecuted = _lastExecutedRegistry[key];
            }
            return lastExecuted;
        }

        // Build execution
        public CommandExecution(Command command, ParameterList parameters, bool force)
        {
            Cmd = command;
            Params = parameters;
            Force = force;
            CreationTime = DateTime.UtcNow;
        }

        // Inspired by Razor souce code, this method check if it is time to execute the command
        // and if the time is right, perform the execution
        // Logic inside may be complex, checking for a given target or waiting for a given gump
        public bool PerformWait()
        {
            if (Cmd.WaitLogic(this)) // check if waiting is over (no blocking, we keep checking as Razor does)
            {
                _lastExecutedRegistry[Cmd.Keyword] = DateTime.UtcNow; // store time of this execution (as the last execution)
                return Cmd.ExecutionLogic(this); // execute the command and do the magic
            }
            else return false;
        }
    }

    // Loosely types abstraction of a command using strings to define expected usage and types
    public class Command
    {
        // All commands share access to the execution queues
        public static Dictionary<Attributes, Queue<CommandExecution>> Queues = new Dictionary<Attributes, Queue<CommandExecution>>();

        // Delegate to allow customization of how execution (both action logic and wait logic) are performed
        public delegate bool Handler(CommandExecution execution);

        // Basic usage syntax
        public string Usage { get; }

        // Keyword in the script syntax
        public string Keyword { get; }

        // Name of parameters used by this command
        public string[] ParamNames { get; }

        // Handler to perform command logic (action of the command)
        public Handler ExecutionLogic { get; }

        // Handler to perform wait logic (deciding if command can execute)
        public Handler WaitLogic { get; }

        // Defines what to command related to when it comes to game logic
        // Also used to organize execution queues
        [Flags]
        public enum Attributes
        {
            ScriptAction = 0,       // This command is related to a script logic unrelated to the game logic, such as aliases
            ForceBypassQueue = 1,   // Force also means bypassing the queue
            ComplexInterAction = 2, // This command is related to a complex interaction, like drag and drop something in game
            SimpleInterAction = 4,  // This command is related to a simple interaction, like a click or dclick in an object
            StateAction = 8,        // This command is related a game state, such as finding an object in the ground
            PlayerAction = 16,      // This command is related to a player driven action, like moving or attacking
        }
        public Attributes Attribute { get; }

        // Several flavors of constructors for quality of life
        #region Constructors
        public Command(string usage, Handler executionLogic, Handler waitLogic)
            : this(usage, executionLogic, waitLogic, Attributes.ScriptAction)
        {
        }
        public Command(string usage, Handler executionLogic, Handler waitLogic, Attributes attribute)
        {
            Usage = usage;
            ExecutionLogic = executionLogic;
            WaitLogic = waitLogic;
            Attribute = attribute;

            // Make sure a queue exists for this command category
            if (!Queues.ContainsKey(Attribute))
                Queues.Add(Attribute, new Queue<CommandExecution>());

            // Processing keywords and arguments in constructor to avoid logic on every command execution
            Keyword = usage.Substring(0, usage.IndexOf(' '));
            ParamNames = String.Join("", usage.Substring(usage.IndexOf(' ') + 1).Split('[', ']', '(', ')')).Split(' '); // keeping just name - same regex [\[\]\(\)]
        }
        #endregion

        // List parameters from args collected by the Abstract Syntax Tree (AST)
        public ParameterList ListParams(Argument[] args)
        {
            return new ParameterList(args, ParamNames);
        }

        // Execute the command according to queing rules and provided logic
        public bool Process(Argument[] args, bool force)
        {
            // Parse arguments when called, independent if this execution will be qued or not.
            ParameterList parameters = new ParameterList(args, ParamNames);

            // Build execution and perform logic if queue should be bypassed
            var execution = new CommandExecution(this, parameters, force);
            if (force && (Attribute & Attributes.ForceBypassQueue) == Attributes.ForceBypassQueue)
                return execution.PerformWait();
            else Queues[Attribute].Enqueue(execution); // otherwise queue it
            return true;
        }
    }

    // Class grouping all command related functionality, including implemented handles
    public static class Commands
    {
        // Registry of available commands retrivable by name (keyword)
        public static Dictionary<string, Command> Definitions = new Dictionary<string, Command>();
        private static void AddDefinition(Command cmd)
        {
            Definitions.Add(cmd.Keyword, cmd);
        }

        // Registry of queues retrivable by name (keyword). A single queue can be shared by multiple commands
        // Key for the queue is the Attribute of the command, making sure common command share the same queue
        //public static Dictionary<Command.Attributes, Queue<CommandExecution>> Queues = new Dictionary<Command.Attributes, Queue<CommandExecution>>();

        public static void Register()
        {
            AddDefinition(new Command("findobject (serial) [color] [source] [amount] [range]", FindObject, WaitForMs(100), Command.Attributes.StateAction));    
            AddDefinition(new Command("attack (serial)", Attack, WaitForMs(500), Command.Attributes.SimpleInterAction));
            AddDefinition(new Command("walk (direction)", Walk, WaitForMovement));
            AddDefinition(new Command("poplist ('list name') ('element value'/'front'/'back')", PopList, WaitForMs(25)));
            AddDefinition(new Command("pushlist ('list name') ('element value') ['front'/'back']", PushList, WaitForMs(25)));
            AddDefinition(new Command("createlist ('list name')", CreateList, WaitForMs(25)));
            AddDefinition(new Command("removelist ('list name')", RemoveList, WaitForMs(25)));
     

            ////Interpreter.RegisterCommandHandler("poplist", );
            //Interpreter.RegisterCommandHandler("pushlist", );
            ////Interpreter.RegisterCommandHandler("removelist", );
            ////Interpreter.RegisterCommandHandler("createlist", CreateList);

            //Interpreter.RegisterCommandHandler("findtype", FindType);
            //Interpreter.RegisterCommandHandler("fly", UnimplementedCommand);
            //Interpreter.RegisterCommandHandler("land", UnimplementedCommand);
            //Interpreter.RegisterCommandHandler("setability", SetAbility);
            //Interpreter.RegisterCommandHandler("attack", Attack);
            //Interpreter.RegisterCommandHandler("clearhands", ClearHands);
            //Interpreter.RegisterCommandHandler("clickobject", ClickObject);
            //Interpreter.RegisterCommandHandler("bandageself", BandageSelf);
            //Interpreter.RegisterCommandHandler("usetype", UseType);
            //Interpreter.RegisterCommandHandler("useobject", UseObject);
            //Interpreter.RegisterCommandHandler("moveitem", MoveItem);
            //Interpreter.RegisterCommandHandler("walk", Walk);
            //Interpreter.RegisterCommandHandler("run", Run);
            //Interpreter.RegisterCommandHandler("turn", Turn);
            //Interpreter.RegisterCommandHandler("useskill", UseSkill);
            //Interpreter.RegisterCommandHandler("feed", Feed);

            //Interpreter.RegisterCommandHandler("unsetalias", UnsetAlias);
            //Interpreter.RegisterCommandHandler("setalias", SetAlias);




            //Interpreter.RegisterExpressionHandler("findobject", ExpFindObject);

            //#region Deprecated (but supported)
            //Interpreter.RegisterCommandHandler("useonce", UseOnce);
            //Interpreter.RegisterCommandHandler("clearuseonce", Deprecated);
            //#endregion

            //#region Deprecated (not supported)
            //Interpreter.RegisterCommandHandler("autoloot", Deprecated);
            //Interpreter.RegisterCommandHandler("toggleautoloot", Deprecated);
            //#endregion



            //Interpreter.RegisterCommandHandler("msg", Msg);

            //Interpreter.RegisterCommandHandler("pause", Pause);






            //Interpreter.RegisterCommandHandler("rename", Rename);
            //Interpreter.RegisterCommandHandler("shownames", ShowNames);
            //Interpreter.RegisterCommandHandler("togglehands", ToggleHands);
            //Interpreter.RegisterCommandHandler("equipitem", EquipItem);
            ////Interpreter.RegisterCommandHandler("dress", DressCommand);
            ////Interpreter.RegisterCommandHandler("undress", UnDressCommand);
            ////Interpreter.RegisterCommandHandler("dressconfig", DressConfig);
            ////Interpreter.RegisterCommandHandler("togglescavenger", ToggleScavenger);

            ////Interpreter.RegisterCommandHandler("promptalias", PromptAlias);
            ////Interpreter.RegisterCommandHandler("waitforgump", WaitForGump);
            ////Interpreter.RegisterCommandHandler("clearjournal", ClearJournal);
            ////Interpreter.RegisterCommandHandler("waitforjournal", WaitForJournal);

            ////Interpreter.RegisterCommandHandler("clearlist", ClearList);
            ////Interpreter.RegisterCommandHandler("ping", Ping);
            ////Interpreter.RegisterCommandHandler("resync", Resync);
            ////Interpreter.RegisterCommandHandler("messagebox", MessageBox);
            ////Interpreter.RegisterCommandHandler("paperdoll", Paperdoll);
            ////Interpreter.RegisterCommandHandler("headmsg", HeadMsg);
            ////Interpreter.RegisterCommandHandler("sysmsg", SysMsg);
            ////Interpreter.RegisterCommandHandler("cast", Cast);
            ////Interpreter.RegisterCommandHandler("waitfortarget", WaitForTarget);
            ////Interpreter.RegisterCommandHandler("canceltarget", CancelTarget);
            ////Interpreter.RegisterCommandHandler("target", Target);
            ////Interpreter.RegisterCommandHandler("targettype", TargetType);
            ////Interpreter.RegisterCommandHandler("targetground", TargetGround);
            ////Interpreter.RegisterCommandHandler("targettile", TargetTile);
            ////Interpreter.RegisterCommandHandler("targettileoffset", TargetTileOffset);
            ////Interpreter.RegisterCommandHandler("targettilerelative", TargetTileRelative);
            ////Interpreter.RegisterCommandHandler("settimer", SetTimer);
            ////Interpreter.RegisterCommandHandler("removetimer", RemoveTimer);
            ////Interpreter.RegisterCommandHandler("createtimer", CreateTimer);




            //Interpreter.RegisterCommandHandler("info", UnimplementedCommand);
            //Interpreter.RegisterCommandHandler("playmacro", UnimplementedCommand);
            //Interpreter.RegisterCommandHandler("playsound", UnimplementedCommand);
            //Interpreter.RegisterCommandHandler("snapshot", UnimplementedCommand);
            //Interpreter.RegisterCommandHandler("hotkeys", UnimplementedCommand);
            //Interpreter.RegisterCommandHandler("where", UnimplementedCommand);
            //Interpreter.RegisterCommandHandler("mapuo", UnimplementedCommand);
            //Interpreter.RegisterCommandHandler("clickscreen", UnimplementedCommand);
            //Interpreter.RegisterCommandHandler("helpbutton", UnimplementedCommand);
            //Interpreter.RegisterCommandHandler("guildbutton", UnimplementedCommand);
            //Interpreter.RegisterCommandHandler("questsbutton", UnimplementedCommand);
            //Interpreter.RegisterCommandHandler("logoutbutton", UnimplementedCommand);
            //Interpreter.RegisterCommandHandler("virtue", UnimplementedCommand);
            //Interpreter.RegisterCommandHandler("partymsg", UnimplementedCommand);
            //Interpreter.RegisterCommandHandler("guildmsg", UnimplementedCommand);
            //Interpreter.RegisterCommandHandler("allymsg", UnimplementedCommand);
            //Interpreter.RegisterCommandHandler("whispermsg", UnimplementedCommand);
            //Interpreter.RegisterCommandHandler("yellmsg", UnimplementedCommand);
            //Interpreter.RegisterCommandHandler("chatmsg", UnimplementedCommand);
            //Interpreter.RegisterCommandHandler("emotemsg", UnimplementedCommand);
            //Interpreter.RegisterCommandHandler("promptmsg", UnimplementedCommand);
            //Interpreter.RegisterCommandHandler("timermsg", UnimplementedCommand);
            //Interpreter.RegisterCommandHandler("waitforprompt", UnimplementedCommand);
            //Interpreter.RegisterCommandHandler("cancelprompt", UnimplementedCommand);
            //Interpreter.RegisterCommandHandler("addfriend", UnimplementedCommand);
            //Interpreter.RegisterCommandHandler("removefriend", UnimplementedCommand);
            //Interpreter.RegisterCommandHandler("contextmenu", UnimplementedCommand);
            //Interpreter.RegisterCommandHandler("waitforcontext", UnimplementedCommand);
            //Interpreter.RegisterCommandHandler("ignoreobject", UnimplementedCommand);
            //Interpreter.RegisterCommandHandler("clearignorelist", UnimplementedCommand);
            //Interpreter.RegisterCommandHandler("setskill", UnimplementedCommand);
            //Interpreter.RegisterCommandHandler("waitforproperties", UnimplementedCommand);
            //Interpreter.RegisterCommandHandler("autocolorpick", UnimplementedCommand);
            //Interpreter.RegisterCommandHandler("waitforcontents", UnimplementedCommand);
            //Interpreter.RegisterCommandHandler("miniheal", UnimplementedCommand);
            //Interpreter.RegisterCommandHandler("bigheal", UnimplementedCommand);
            //Interpreter.RegisterCommandHandler("chivalryheal", UnimplementedCommand);
            //Interpreter.RegisterCommandHandler("moveitemoffset", UnimplementedCommand);
            //Interpreter.RegisterCommandHandler("movetype", UnimplementedCommand);
            //Interpreter.RegisterCommandHandler("movetypeoffset", UnimplementedCommand);
            //Interpreter.RegisterCommandHandler("togglemounted", UnimplementedCommand);
            //Interpreter.RegisterCommandHandler("equipwand", UnimplementedCommand);
            //Interpreter.RegisterCommandHandler("buy", UnimplementedCommand);
            //Interpreter.RegisterCommandHandler("sell", UnimplementedCommand);
            //Interpreter.RegisterCommandHandler("clearbuy", UnimplementedCommand);
            //Interpreter.RegisterCommandHandler("clearsell", UnimplementedCommand);
            //Interpreter.RegisterCommandHandler("organizer", UnimplementedCommand);
            //Interpreter.RegisterCommandHandler("counter", UnimplementedCommand);
            //Interpreter.RegisterCommandHandler("replygump", UnimplementedCommand);
            //Interpreter.RegisterCommandHandler("closegump", UnimplementedCommand);
            //Interpreter.RegisterCommandHandler("cleartargetqueue", UnimplementedCommand);
            //Interpreter.RegisterCommandHandler("autotargetlast", UnimplementedCommand);
            //Interpreter.RegisterCommandHandler("autotargetself", UnimplementedCommand);
            //Interpreter.RegisterCommandHandler("autotargetobject", UnimplementedCommand);
            //Interpreter.RegisterCommandHandler("autotargettype", UnimplementedCommand);
            //Interpreter.RegisterCommandHandler("autotargettile", UnimplementedCommand);
            //Interpreter.RegisterCommandHandler("autotargettileoffset", UnimplementedCommand);
            //Interpreter.RegisterCommandHandler("autotargettilerelative", UnimplementedCommand);
            //Interpreter.RegisterCommandHandler("autotargetghost", UnimplementedCommand);
            //Interpreter.RegisterCommandHandler("autotargetground", UnimplementedCommand);
            //Interpreter.RegisterCommandHandler("cancelautotarget", UnimplementedCommand);
            //Interpreter.RegisterCommandHandler("getenemy", UnimplementedCommand);
            //Interpreter.RegisterCommandHandler("getfriend", UnimplementedCommand);
        }

        public static bool FindObject(CommandExecution execution)
        {
            var serial = execution.Params.NextAs<uint>(ParameterList.Expectation.Mandatory);
            var color = execution.Params.NextAs<ushort>();
            var source = execution.Params.NextAs<string>();
            var amount = execution.Params.NextAs<int>();
            var range = execution.Params.NextAs<int>();

            Entity entity = CmdFindEntityBySerial(serial, color, source, range);
            if (entity != null)
            {
                if ((entity is Item item && item.Amount > amount) || entity is Mobile)
                    Aliases.Write<uint>("found", entity.Serial); // should this be a scope variable?
                return true;
            }
            else return false;
        }

        private static bool Attack(CommandExecution execution)
        {
            var serial = execution.Params.NextAs<uint>(ParameterList.Expectation.Mandatory);
            GameActions.Attack(serial);
            return true;
        }

        private static bool PopList(CommandExecution execution)
        {
            var listName = execution.Params.NextAs<string>(ParameterList.Expectation.Mandatory);
            var value = execution.Params.NextAs<string>(ParameterList.Expectation.Mandatory);

            if (value == "front")
            {
                if (execution.Force)
                    while (Interpreter.PopList(listName, true)) { }
                else
                    Interpreter.PopList(listName, true);
            }
            else if (value == "back")
            {
                if (execution.Force)
                    while (Interpreter.PopList(listName, false)) { }
                else
                    Interpreter.PopList(listName, false);
            }
            else
            {
                if (execution.Force)
                    while (Interpreter.PopList(listName, execution.Params[1])) { }
                else
                    Interpreter.PopList(listName, execution.Params[1]);
            }

            return true;
        }

        private static bool RemoveList(CommandExecution execution)
        {
            var listName = execution.Params.NextAs<string>(ParameterList.Expectation.Mandatory);
            Interpreter.DestroyList(listName);
            return true;
        }

        private static bool CreateList(CommandExecution execution)
        {
            var listName = execution.Params.NextAs<string>(ParameterList.Expectation.Mandatory);
            Interpreter.CreateList(listName);
            return true;
        }

        private static bool ClearList(CommandExecution execution)
        {
            var listName = execution.Params.NextAs<string>(ParameterList.Expectation.Mandatory);
            Interpreter.ClearList(listName);
            return true;
        }

        private static bool PushList(CommandExecution execution)
        {
            var name = execution.Params.NextAs<string>(ParameterList.Expectation.Mandatory);
            var values = execution.Params.NextAsArray<string>(ParameterList.Expectation.Mandatory);
            var pos = execution.Params.NextAs<string>(ParameterList.Expectation.Mandatory);
            bool front = (pos == "force");
            foreach (var val in values)
            {
                //Interpreter.PushList(name, new Argument(), front, force);
            }
            return true;
        }

        private static bool Walk(CommandExecution execution)
        {
            // Be prepared for multiple directions -> walk "North, East, East, West, South, Southeast"
            var dirArray = execution.Params.NextAsArray<string>(ParameterList.Expectation.Mandatory);

            // At least one is mandatory, so perform walk command on it
            var direction = (Direction)Enum.Parse(typeof(Direction), dirArray[0], true);
            World.Player.Walk(direction, ProfileManager.CurrentProfile.AlwaysRun);

            // For all remaining, explode it as one command per single direction
            for (int i = 1; i < dirArray.Length; i++)
            {
                // So queue it again with one less parameter
                VirtualArgument arg = new VirtualArgument(dirArray[i]);
                ParameterList newParams = new ParameterList(new Argument[1] { arg }, execution.Cmd.ParamNames);
                Command.Queues[execution.Cmd.Attribute].Enqueue(new CommandExecution(execution.Cmd, newParams, execution.Force));
            }

            return true;
        }

        //private static bool SetAbility(string command, ParameterList args)
        //{
        //    try
        //    {
        //        var ability = args.NextAs<string>(ParameterList.ArgumentType.Mandatory);
        //        var toggle = args.NextAs<string>(ParameterList.ArgumentType.Mandatory);
        //        if (toggle == "on")
        //        {
        //            switch (ability)
        //            {
        //                case "primary":
        //                    GameActions.UsePrimaryAbility();
        //                    break;
        //                case "secondary":
        //                    GameActions.UseSecondaryAbility();
        //                    break;
        //                case "stun":
        //                    GameActions.RequestStun();
        //                    break;
        //                case "disarm":
        //                    GameActions.RequestDisarm();
        //                    break;
        //            }
        //        }
        //        else
        //        {
        //            GameActions.ClearAbility();
        //        }
        //        return true;
        //    }
        //    catch (ScriptRunTimeError ex)
        //    {
        //        throw new ScriptSyntaxError("Usage: setability ('primary'/'secondary'/'stun'/'disarm') ['on'/'off']", ex);
        //    }
        //}



        //private static bool ClearHands(string command, ParameterList args)
        //{
        //    try
        //    {
        //        var hands = args.NextAsHand(ParameterList.ArgumentType.Mandatory);
        //        if (hands == ParameterList.Hands.Both)
        //        {
        //            GameActions.ClearEquipped((IO.ItemExt_PaperdollAppearance)ParameterList.Hands.Left);
        //            GameActions.ClearEquipped((IO.ItemExt_PaperdollAppearance)ParameterList.Hands.Right);
        //        }
        //        else GameActions.ClearEquipped((IO.ItemExt_PaperdollAppearance)hands);
        //        // TODO: check if hands are cleared to return false on fail (can you fail to clear hands? Maybe trying to unequipe during use of wand, or cast of spell?)
        //        return true;
        //    }
        //    catch (ScriptRunTimeError ex)
        //    {
        //        throw new ScriptSyntaxError("Usage: clearhands ('left'/'right'/'both')", ex);
        //    }
        //}

        //private static bool ClickObject(string command, ParameterList args)
        //{
        //    try
        //    {
        //        var serial = args.NextAsSerial(ParameterList.ArgumentType.Mandatory);
        //        GameActions.SingleClick(serial);
        //        return true;
        //    }
        //    catch (ScriptRunTimeError ex)
        //    {
        //        throw new ScriptSyntaxError("Usage: clickobject (serial)", ex);
        //    }
        //}

        //private static bool BandageSelf(string command, ParameterList args)
        //{
        //    GameActions.BandageSelf();
        //    // TODO: maybe return false if no badages or other constraints?
        //    return true;
        //}

        //private static bool UseType(string command, ParameterList args)
        //{
        //    try
        //    {
        //        var graphic = args.NextAs<ushort>(ParameterList.ArgumentType.Mandatory);
        //        Item item = CmdFindEntityByGraphic(graphic,
        //            args.NextAs<ushort>(),
        //            args.NextAsSource(),
        //            args.NextAs<int>());

        //        if (item != null)
        //            GameActions.DoubleClick(item.Serial);
        //        else
        //            throw new ScriptRunTimeError(null, $"Script Error: Couldn't find '{graphic.ToString("X3")}'");

        //        return true;
        //    }
        //    catch (ScriptRunTimeError ex)
        //    {
        //        throw new ScriptSyntaxError("Usage: usetype (graphic) [color] [source] [range or search level]", ex);
        //    }
        //}

        //private static bool UseObject(string command, ParameterList args)
        //{
        //    try
        //    {
        //        var serial = args.NextAsSerial(ParameterList.ArgumentType.Mandatory);
        //        GameActions.DoubleClick(serial);
        //        return true;
        //    }
        //    catch (ScriptRunTimeError ex)
        //    {
        //        throw new ScriptSyntaxError("Usage: useobject (serial)", ex);
        //    }
        //}

        //private static bool UseOnce(string command, ParameterList args)
        //{
        //    try
        //    {
        //        var graphic = args.NextAs<ushort>(ParameterList.ArgumentType.Mandatory);
        //        Item item = CmdFindEntityByGraphic(graphic, args.NextAs<ushort>());

        //        if (item != null)
        //            GameActions.DoubleClick(item.Serial);
        //        else
        //            throw new ScriptRunTimeError(null, $"Script Error: Couldn't find '{graphic.ToString("X3")}'");

        //        return true;
        //    }
        //    catch (ScriptRunTimeError ex)
        //    {
        //        throw new ScriptSyntaxError("Usage: useonce (graphic) [color]", ex);
        //    }
        //}

        //private static bool MoveItem(string command, ParameterList args)
        //{
        //    try
        //    {
        //        var serial = args.NextAsSerial(ParameterList.ArgumentType.Mandatory);

        //        // Destination can be both a Serial or Source (backpack, ground, etc)
        //        uint destination;
        //        if (!args.TryAsSerial(out destination))
        //        {
        //            var source = args.NextAsSource(ParameterList.ArgumentType.Mandatory);
        //            destination = World.Player.FindItemByLayer((Layer)source).Serial;
        //        }
        //        //offest = new ParameterList.Position((int)World.Player.X, (int)World.Player.Y, (int)World.Player.Z);
        //        var x = args.NextAs<int>();
        //        var y = args.NextAs<int>();
        //        var z = args.NextAs<int>();
        //        var amount = args.NextAs<int>();

        //        GameActions.PickUp(serial, 0, 0, amount);
        //        GameActions.DropItem(
        //            serial,
        //            x+ World.Player.X,
        //            y + World.Player.Y,
        //            z + World.Player.Z,
        //            destination);

        //        return true;
        //    }
        //    catch (ScriptRunTimeError ex)
        //    {
        //        throw new ScriptSyntaxError("Usage: moveitem (serial) (destination) [(x, y, z)] [amount]", ex);
        //    }
        //}

        //private static bool Walk(string command, ParameterList args)
        //{
        //    try
        //    {
        //        var direction = args.NextAsDirection(ParameterList.ArgumentType.Mandatory);

        //        //if (World.Player.Direction != (Direction)direction.First())
        //        //{
        //        //    // if the player is not currently facing into the direction of the run
        //        //    // then the player will turn first, and run next - this helps so that 
        //        //    // scripts do not need to include "turn" commands or call "run" multiple
        //        //    // times just to achieve the same result
        //        //    movementCooldown = DateTime.UtcNow + turnMs;
        //        //    World.Player.Walk((Direction)direction.First(), true);
        //        //    return false;
        //        //}

        //        var movementDelay = ProfileManager.CurrentProfile.AlwaysRun ? runMs : walkMs;

        //        if (World.Player.FindItemByLayer(Layer.Mount) != null)
        //        {
        //            if (ProfileManager.CurrentProfile.AlwaysRun)
        //            {
        //                movementDelay = mountedRunMs;
        //            }
        //            else
        //            {
        //                movementDelay = mountedWalkMs;
        //            }
        //        }

        //        movementCooldown = DateTime.UtcNow + movementDelay;

        //        foreach (var dir in direction)
        //        {
        //            World.Player.Walk((Direction)dir, ProfileManager.CurrentProfile.AlwaysRun);
        //            //if (!World.Player.Walk((Direction)dir, ProfileManager.CurrentProfile.AlwaysRun))
        //            //return false;
        //        }
        //        return true;
        //    }
        //    catch (ScriptRunTimeError ex)
        //    {
        //        throw new ScriptSyntaxError("Usage: walk ('direction name')", ex);
        //    }
        //}

        //private static bool Turn(string command, ParameterList args)
        //{
        //    try
        //    {
        //        var direction = args.NextAsDirection(ParameterList.ArgumentType.Mandatory);

        //        if (DateTime.UtcNow < movementCooldown)
        //        {
        //            return false;
        //        }

        //        foreach (var dir in direction)
        //        {
        //            if (World.Player.Direction != (Direction)dir)
        //            {
        //                movementCooldown = DateTime.UtcNow + turnMs;
        //                if(!World.Player.Walk((Direction)dir, true))
        //                    return false;
        //            }
        //        }
        //        return true;
        //    }
        //    catch (ScriptRunTimeError ex)
        //    {
        //        throw new ScriptSyntaxError("Usage: walk ('direction name')", ex);
        //    }
        //}

        //private static bool Run(string command, ParameterList args)
        //{
        //    try
        //    {
        //        var direction = args.NextAsDirection(ParameterList.ArgumentType.Mandatory);

        //        if(DateTime.UtcNow < movementCooldown)
        //        {
        //            return false;
        //        }

        //        //if (World.Player.Direction != (Direction)direction.First())
        //        //{
        //        //    // if the player is not currently facing into the direction of the run
        //        //    // then the player will turn first, and run next - this helps so that 
        //        //    // scripts do not need to include "turn" commands or call "run" multiple
        //        //    // times just to achieve the same result
        //        //    movementCooldown = DateTime.UtcNow + turnMs;
        //        //    World.Player.Walk((Direction)direction.First(), true);
        //        //    return false;
        //        //}

        //        var movementDelay = runMs;

        //        if (World.Player.FindItemByLayer(Layer.Mount) != null)
        //        {
        //            movementDelay = mountedRunMs;
        //        }

        //        movementCooldown = DateTime.UtcNow + movementDelay;

        //        foreach (var dir in direction)
        //        {
        //            if (!World.Player.Walk((Direction)dir, true))
        //                return false;
        //        }
        //        return true;
        //    }
        //    catch (ScriptRunTimeError ex)
        //    {
        //        throw new ScriptSyntaxError("Usage: walk ('direction name')", ex);
        //    }
        //}

        //private static bool UseSkill(string command, ParameterList args)
        //{
        //    try
        //    {
        //        if (args[0].As<string>() == "last")
        //        {
        //            GameActions.UseLastSkill();
        //            return true;
        //        }
        //        string skillName = args[0].As<string>();

        //        if (!GameActions.UseSkill(skillName))
        //        {
        //            throw new ScriptRunTimeError(null, "That skill  is not usable");
        //        }
        //        return true;
        //    }
        //    catch (ScriptRunTimeError ex)
        //    {
        //        throw new ScriptSyntaxError("Usage: useskill ('skill name'/'last')", ex);
        //    }
        //}

        //private static bool Feed(string command, ParameterList args)
        //{
        //    try
        //    {
        //        var serial = args.NextAsSerial(ParameterList.ArgumentType.Mandatory);
        //        List<ushort> foodList = new List<ushort>();
        //        //ushort graphic;
        //        //if (!args.TryAsGraphic(out graphic))
        //        //{
        //        //    var source = args.NextAsSource(ParameterList.ArgumentType.Mandatory);
        //        //    destination = World.Player.FindItemByLayer((Layer)source).Serial;
        //        //}
        //        //else foodList.Add(graphic)
        //        var color = args.NextAs<ushort>();
        //        var amount = args.NextAs<int>();

        //        //Item item = CmdFindObjectBySerial(serial, color);//, source, range);
        //        //return item != null && item.Amount > amount;
        //        return true;
        //    }
        //    catch (ScriptRunTimeError ex)
        //    {
        //        throw new ScriptSyntaxError("Usage: feed (serial) ('food name'/'food group'/'any'/graphic) [color] [amount]", ex);
        //    }
        //}



        //private static bool FindType(string command, ParameterList args)
        //{
        //    try
        //    {
        //        var graphic = args.NextAs<ushort>(ParameterList.ArgumentType.Mandatory);
        //        var color = args.NextAs<ushort>();
        //        var source = args.NextAsSource();
        //        var amount = args.NextAs<int>();
        //        var range = args.NextAs<int>();

        //        Item item = CmdFindEntityByGraphic(graphic, color, source, range);
        //        return item != null && item.Amount > amount;
        //    }
        //    catch (ScriptRunTimeError ex)
        //    {
        //        throw new ScriptSyntaxError("Usage: findtype (graphic) [color] [source] [amount] [range or search level]", ex);
        //    }
        //}


        //private static bool UnimplementedCommand(string command, ParameterList args)
        //{
        //    GameActions.Print($"Unimplemented command: '{command}'", type: MessageType.System);
        //    return true;
        //}

        //private static bool Deprecated(string command, ParameterList args)
        //{
        //    GameActions.Print($"Deprecated command: '{command}'", type: MessageType.System);
        //    return true;
        //}



        ////private static bool ExpFindObject(string expression, ParameterList args, bool quiet)
        ////{
        ////    return FindObject(expression, args);
        ////}



        ////private static bool UseItem(Item cont, ushort find)
        ////{
        ////    for (int i = 0; i < cont.Contains.Count; i++)
        ////    {
        ////        Item item = cont.Contains[i];

        ////        if (item.ItemID == find)
        ////        {
        ////            PlayerData.DoubleClick(item);
        ////            return true;
        ////        }
        ////        else if (item.Contains != null && item.Contains.Count > 0)
        ////        {
        ////            if (UseItem(item, find))
        ////                return true;
        ////        }
        ////    }

        ////    return false;
        ////}











        //private static bool Rename(string command, ParameterList args)
        //{
        //    if (args.Length != 2)
        //    {
        //        throw new ScriptRunTimeError(null, "Usage: rename (serial) ('name')");
        //    }

        //    var target = args[0].As<uint>();
        //    var name = args[1].As<string>();

        //    GameActions.Rename(target, name);

        //    return true;
        //}

        //private static bool SetAlias(string command, ParameterList args)
        //{
        //    if (args.Length != 2)
        //        throw new ScriptRunTimeError(null, "Usage: setalias ('name') [serial]");

        //    Interpreter.SetAlias(args[0].As<string>(), args[1].As<uint>());

        //    return true;
        //}

        ////private static bool PromptAlias(string command, ParameterList args)
        ////{
        ////    Interpreter.Pause(60000);

        ////    if (!_hasPrompt)
        ////    {
        ////        _hasPrompt = true;
        ////        Targeting.OneTimeTarget((location, serial, p, gfxid) =>
        ////        {
        ////            Interpreter.SetAlias(args[0].As<string>(), serial);
        ////            Interpreter.Unpause();
        ////        });
        ////        return false;
        ////    }

        ////    _hasPrompt = false;
        ////    return true;
        ////}

        ////private static bool WaitForGump(string command, ParameterList args)
        ////{
        ////    if (args.Length < 2)
        ////        throw new RunTimeError(null, "Usage: waitforgump (gump id/'any') (timeout)");

        ////    bool any = args[0].As<string>() == "any";

        ////    if (any)
        ////    {
        ////        if (World.Player.HasGump || World.Player.HasCompressedGump)
        ////            return true;
        ////    }
        ////    else
        ////    {
        ////        uint gumpId = args[0].AsSerial();

        ////        if (World.Player.CurrentGumpI == gumpId)
        ////            return true;
        ////    }

        ////    Interpreter.Timeout(args[1].AsUInt(), () => { return true; });
        ////    return false;
        ////}

        ////private static bool ClearJournal(string command, ParameterList args)
        ////{
        ////    Journal.Clear();

        ////    return true;
        ////}

        ////private static bool WaitForJournal(string command, ParameterList args)
        ////{
        ////    if (args.Length < 2)
        ////        throw new RunTimeError(null, "Usage: waitforjournal ('text') (timeout) ['author'/'system']");

        ////    if (!Journal.ContainsSafe(args[0].As<string>()))
        ////    {
        ////        Interpreter.Timeout(args[1].AsUInt(), () => { return true; });
        ////        return false;
        ////    }

        ////    return true;
        ////}



        //private static bool UnsetAlias(string command, ParameterList args)
        //{
        //    if (args.Length == 0)
        //        throw new ScriptRunTimeError(null, "Usage: unsetalias (string)");

        //    Interpreter.SetAlias(args[0].As<string>(), 0);

        //    return true;
        //}

        //private static bool ShowNames(string command, ParameterList args)
        //{
        //    if (args.Length == 0)
        //        throw new ScriptRunTimeError(null, "Usage: shownames ['mobiles'/'corpses']");

        //    if (args[0].As<string>() == "mobiles")
        //    {
        //        GameActions.AllNames(GameActions.AllNamesTargets.Mobiles);
        //    }
        //    else if (args[0].As<string>() == "corpses")
        //    {
        //        GameActions.AllNames(GameActions.AllNamesTargets.Corpses);
        //    }
        //    return true;
        //}

        //public static bool ToggleHands(string command, ParameterList args)
        //{
        //    if (args.Length == 0)
        //    {
        //        throw new ScriptRunTimeError(null, "Usage: togglehands ('left'/'right')");
        //    }

        //    switch (args[0].As<string>())
        //    {
        //        case "left":
        //            GameActions.ToggleEquip(IO.ItemExt_PaperdollAppearance.Left);
        //            break;
        //        case "right":
        //            GameActions.ToggleEquip(IO.ItemExt_PaperdollAppearance.Right);
        //            break;
        //        default:
        //            throw new ScriptRunTimeError(null, "Usage: togglehands ('left'/'right')");
        //    }

        //    return true;
        //}

        //public static bool EquipItem(string command, ParameterList args)
        //{
        //    if (args.Length < 1)
        //    {
        //        throw new ScriptRunTimeError(null, "Usage: equipitem (serial)");
        //    }

        //    var item = (Item)World.Get(args[0].As<uint>());

        //    if (item != null)
        //    {
        //        GameActions.Equip(item);
        //    }

        //    return true;
        //}

        ////public static bool ToggleScavenger(string command, ParameterList args)
        ////{
        ////    ScavengerAgent.Instance.ToggleEnabled();

        ////    return true;
        ////}

        //private static bool Pause(string command, ParameterList args)
        //{
        //    if (args.Length == 0)
        //        throw new ScriptRunTimeError(null, "Usage: pause (timeout)");

        //    Interpreter.Pause(args[0].As<uint>());
        //    return true;
        //}

        ////private static bool Ping(string command, ParameterList args)
        ////{
        ////    Assistant.Ping.StartPing(5);

        ////    return true;
        ////}

        ////private static bool Resync(string command, ParameterList args)
        ////{
        ////    Client.Instance.SendToServer(new ResyncReq());

        ////    return true;
        ////}

        ////private static bool MessageBox(string command, ParameterList args)
        ////{
        ////    if (args.Length != 2)
        ////        throw new RunTimeError(null, "Usage: messagebox ('title') ('body')");

        ////    System.Windows.Forms.MessageBox.Show(args[0].As<string>(), args[1].As<string>());

        ////    return true;
        ////}

        //public static bool Msg(string command, ParameterList args)
        //{
        //    switch (args.Length)
        //    {
        //        case 1:
        //            GameActions.Say(args[0].As<string>());
        //            break;
        //        case 2:
        //            GameActions.Say(args[0].As<string>(), hue: args[1].As<ushort>());
        //            break;
        //        default:
        //            throw new ScriptRunTimeError(null, "Usage: msg ('text') [color]");
        //    }

        //    return true;
        //}

        //private static bool Paperdoll(string command, ParameterList args)
        //{
        //    if (args.Length > 1)
        //        throw new RunTimeError(null, "Usage: paperdoll [serial]");

        //    uint serial = args.Length == 0 ? World.Player.Serial.Value : args[0].AsSerial();
        //    Client.Instance.SendToServer(new DoubleClick(serial));

        //    return true;
        //}

        //public static bool Cast(string command, ParameterList args)
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

        //private static bool WaitForTarget(string command, ParameterList args)
        //{
        //    if (args.Length != 1)
        //        throw new RunTimeError(null, "Usage: waitfortarget (timeout)");

        //    if (Targeting.HasTarget)
        //        return true;

        //    Interpreter.Timeout(args[0].AsUInt(), () => { return true; });
        //    return false;
        //}

        //private static bool CancelTarget(string command, ParameterList args)
        //{
        //    if (args.Length != 0)
        //        throw new RunTimeError(null, "Usage: canceltarget");

        //    if (Targeting.HasTarget)
        //        Targeting.CancelOneTimeTarget();

        //    return true;
        //}

        //private static bool Target(string command, ParameterList args)
        //{
        //    if (args.Length != 1)
        //        throw new RunTimeError(null, "Usage: target (serial)");

        //    if (!Targeting.HasTarget)
        //        ScriptManager.Error(quiet, command, "No target cursor available. Consider using waitfortarget.");
        //    else
        //        Targeting.Target(args[0].AsSerial());

        //    return true;
        //}

        //private static bool TargetType(string command, ParameterList args)
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

        //private static bool TargetGround(string command, ParameterList args)
        //{
        //    if (args.Length < 1 || args.Length > 3)
        //        throw new RunTimeError(null, "Usage: targetground (graphic) [color] [range]");

        //    throw new RunTimeError(null, $"Unimplemented command {command}");
        //}

        //private static bool TargetTile(string command, ParameterList args)
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

        //private static bool TargetTileOffset(string command, ParameterList args)
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

        //private static bool TargetTileRelative(string command, ParameterList args)
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

        //public static bool HeadMsg(string command, ParameterList args)
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

        //public static bool SysMsg(string command, ParameterList args)
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

        //public static bool DressCommand(string command, ParameterList args)
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

        //public static bool UnDressCommand(string command, ParameterList args)
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

        //public static bool DressConfig(string command, ParameterList args)
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

        //private static bool SetTimer(string command, ParameterList args)
        //{
        //    if (args.Length != 2)
        //        throw new RunTimeError(null, "Usage: settimer (timer name) (value)");


        //    Interpreter.SetTimer(args[0].As<string>(), args[1].AsInt());
        //    return true;
        //}

        //private static bool RemoveTimer(string command, ParameterList args)
        //{
        //    if (args.Length != 1)
        //        throw new RunTimeError(null, "Usage: removetimer (timer name)");

        //    Interpreter.RemoveTimer(args[0].As<string>());
        //    return true;
        //}

        //private static bool CreateTimer(string command, ParameterList args)
        //{
        //    if (args.Length != 1)
        //        throw new RunTimeError(null, "Usage: createtimer (timer name)");

        //    Interpreter.CreateTimer(args[0].As<string>());
        //    return true;
        //}

        // HELPER FUNCTIONS FOR THE COMMANDS

        private static Entity CmdFindEntityBySerial(uint serial, ushort color, string source = "any", int range = 0)
        {
            // Try fetching and Item from Serial
            Entity entity = null;
            var graphic = World.GetOrCreateItem(serial).Graphic;

            if (source == "any")
            {
                // Any also look at the ground if not found in player belongings
                entity = World.Player.FindItem(graphic, color);
                if (entity != null)
                    entity = World.Player.FindItemByTypeOnGroundWithHueInRange(graphic, color, range);
            }
            else if (source == "ground")
                entity = World.Player.FindItemByTypeOnGroundWithHueInRange(graphic, color, range);
            else
            {
                var layer = (Layer)Enum.Parse(typeof(Layer), source, true);
                entity = World.Player.FindItemByLayer(layer)?.FindItem(graphic, color);
            }

            // Try fetching a Mobile from Serial
            if (entity == null)
                entity = World.GetOrCreateMobile(serial);

            return entity;
        }

        private static Item CmdFindItemByGraphic(ushort graphic, ushort color = ushort.MaxValue, string source = "any", int range = 0)
        {
            Item item = null;
            if (source == "any")
            {
                item = World.Player.FindItem(graphic, color);
                if (item != null) // For Any, also look at the ground if not found in player belongings
                    item = World.Player.FindItemByTypeOnGroundWithHueInRange(graphic, color, range);
            }
            else if (source == "ground")
                item = World.Player.FindItemByTypeOnGroundWithHueInRange(graphic, color, range);
            else
            {
                var layer = (Layer)Enum.Parse(typeof(Layer), source, true);
                item = World.Player.FindItemByLayer(layer)?.FindItem(graphic, color);
            }

            return item;
        }

        // Wait for a given amount of milliseconds (suing "curry" technique for param reduction)
        public static Command.Handler WaitForMs(int waitTime)
        {
            return (execution) => { return (execution.CreationTime - DateTime.Now > TimeSpan.FromMilliseconds(waitTime)); };
        }

        public static bool WaitForMovement(CommandExecution execution)
        { 
            // Based on command and settings we select the expected delay
            var movementDelay = Constants.PLAYER_WALKING_DELAY * 2.0;    // walk default delay
            if (World.Player.FindItemByLayer(Layer.Mount) != null)
                movementDelay = Constants.PLAYER_WALKING_DELAY * 1.2;    // mount default delay
            else if (ProfileManager.CurrentProfile.AlwaysRun || execution.Cmd.Keyword == "run")
                movementDelay = Constants.PLAYER_WALKING_DELAY * 1.2;    // run default delay
            else if (execution.Cmd.Keyword == "turn")
                movementDelay = Constants.TURN_DELAY * 1.2;              // turn default delay

            return ((DateTime.UtcNow - CommandExecution.LastExecuted(execution.Cmd.Keyword)).TotalMilliseconds > movementDelay);
        }
    }
}
