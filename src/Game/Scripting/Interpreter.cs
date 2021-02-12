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

using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using ClassicUO.Utility;
using System.ComponentModel;

namespace ClassicUO.Game.Scripting
{
    // Generic exception for the script functionality
    public class ScriptRunTimeError : Exception
    {
        public ASTNode Node;

        public ScriptRunTimeError(ASTNode node, string error) : base(error)
        {
            Node = node;
        }

        public ScriptRunTimeError(ASTNode node, string error, Exception inner) : base(error, inner)
        {
            Node = node;
        }
    }

    // Script exception related to calling a command with the wrong syntax (most valuable to player and UI feedback)
    public class ScriptSyntaxError : ScriptRunTimeError
    {
        public ScriptSyntaxError(string error, ScriptRunTimeError inner) : base(inner.Node, error, inner)
        {
        }
    }

    // Script exception related to conversion issues between types, enums, dictionaries, etc
    public class ScriptTypeConversionError : ScriptRunTimeError
    {
        public ScriptTypeConversionError(ASTNode node, string error) : base(node, error)
        {
        }
    }

    internal static class TypeConverter
    {
        public static T To<T>(string token)
        {
            var converter = TypeDescriptor.GetConverter(typeof(T));
            if (converter != null)
            {
                if (token.StartsWith("0x"))
                {
                    long hexValue = long.Parse(token.Substring(2), NumberStyles.AllowHexSpecifier);
                    return (T)Convert.ChangeType(hexValue, typeof(T));
                }
                else return (T)converter.ConvertFromString(token);
            }
            else throw new ScriptTypeConversionError(null, "Cannot convert argument to " + typeof(T).FullName);
        }

        //public static uint ToUInt(string token)
        //{
        //    uint val;

        //    if (token.StartsWith("0x"))
        //    {
        //        if (uint.TryParse(token.Substring(2), NumberStyles.HexNumber, Interpreter.Culture, out val))
        //            return val;
        //    }
        //    else if (uint.TryParse(token, out val))
        //        return val;

        //    throw new ScriptTypeConversionError(null, "Cannot convert argument to uint");
        //}

        //public static ushort ToUShort(string token)
        //{
        //    ushort val;

        //    if (token.StartsWith("0x"))
        //    {
        //        if (ushort.TryParse(token.Substring(2), NumberStyles.HexNumber, Interpreter.Culture, out val))
        //            return val;
        //    }
        //    else if (ushort.TryParse(token, out val))
        //        return val;

        //    throw new ScriptTypeConversionError(null, "Cannot convert argument to ushort");
        //}

        //public static double ToDouble(string token)
        //{
        //    double val;

        //    if (double.TryParse(token, out val))
        //        return val;

        //    throw new ScriptTypeConversionError(null, "Cannot convert argument to double");
        //}

        //public static bool ToBool(string token)
        //{
        //    bool val;

        //    if (bool.TryParse(token, out val))
        //        return val;

        //    throw new ScriptTypeConversionError(null, "Cannot convert argument to bool");
        //}
    }

    internal class Scope
    {
        private Dictionary<string, Argument> _namespace = new Dictionary<string, Argument>();

        public readonly ASTNode StartNode;
        public readonly Scope Parent;

        public Scope(Scope parent, ASTNode start)
        {
            Parent = parent;
            StartNode = start;
        }

        public Argument GetVar(string name)
        {
            Argument arg;

            if (_namespace.TryGetValue(name, out arg))
                return arg;

            return null;
        }

        public void SetVar(string name, Argument val)
        {
            _namespace[name] = val;
        }

        public void ClearVar(string name)
        {
            _namespace.Remove(name);
        }
    }

    public class ScriptExecutionState
    {
        private ASTNode _statement;

        private Scope _scope;

        public Argument Lookup(string name)
        {
            var scope = _scope;
            Argument result = null;

            while (scope != null)
            {
                result = scope.GetVar(name);
                if (result != null)
                    return result;

                scope = scope.Parent;
            }

            return result;
        }

        private void PushScope(ASTNode node)
        {
            _scope = new Scope(_scope, node);
        }

        private void PopScope()
        {
            _scope = _scope.Parent;
        }

        private Argument[] ConstructArgumentList(ref ASTNode node)
        {
            List<Argument> args = new List<Argument>();

            node = node.Next();

            while (node != null)
            {
                switch (node.Type)
                {
                    case ASTNodeType.AND:
                    case ASTNodeType.OR:
                    case ASTNodeType.EQUAL:
                    case ASTNodeType.NOT_EQUAL:
                    case ASTNodeType.LESS_THAN:
                    case ASTNodeType.LESS_THAN_OR_EQUAL:
                    case ASTNodeType.GREATER_THAN:
                    case ASTNodeType.GREATER_THAN_OR_EQUAL:
                        return args.ToArray();
                }

                args.Add(new Argument(this, node));

                node = node.Next();
            }

            return args.ToArray();
        }

        // For now, the scripts execute directly from the
        // abstract syntax tree. This is relatively simple.
        // A more robust approach would be to "compile" the
        // scripts to a bytecode. That would allow more errors
        // to be caught with better error messages, as well as
        // make the scripts execute more quickly.
        public ScriptExecutionState(ASTNode root)
        {
            // Set current to the first statement
            _statement = root.FirstChild();

            // Create a default scope
            _scope = new Scope(null, _statement);
        }

        public bool ExecuteNext()
        {
            if (_statement == null)
                return false;

            if (_statement.Type != ASTNodeType.STATEMENT)
                throw new ScriptRunTimeError(_statement, "Invalid script");

            var node = _statement.FirstChild();

            if (node == null)
                throw new ScriptRunTimeError(_statement, "Invalid statement");

            int depth = 0;

            switch (node.Type)
            {
                case ASTNodeType.IF:
                    {
                        PushScope(node);

                        var expr = node.FirstChild();
                        var result = EvaluateExpression(ref expr);

                        // Advance to next statement
                        Advance();

                        // Evaluated true. Jump right into execution.
                        if (result)
                            break;

                        // The expression evaluated false, so keep advancing until
                        // we hit an elseif, else, or endif statement that matches
                        // and try again.
                        depth = 0;

                        while (_statement != null)
                        {
                            node = _statement.FirstChild();

                            if (node.Type == ASTNodeType.IF)
                            {
                                depth++;
                            }
                            else if (node.Type == ASTNodeType.ELSEIF)
                            {
                                if (depth == 0)
                                {
                                    expr = node.FirstChild();
                                    result = EvaluateExpression(ref expr);

                                    // Evaluated true. Jump right into execution
                                    if (result)
                                    {
                                        Advance();
                                        break;
                                    }
                                }
                            }
                            else if (node.Type == ASTNodeType.ELSE)
                            {
                                if (depth == 0)
                                {
                                    // Jump into the else clause
                                    Advance();
                                    break;
                                }
                            }
                            else if (node.Type == ASTNodeType.ENDIF)
                            {
                                if (depth == 0)
                                    break;

                                depth--;
                            }

                            Advance();
                        }

                        if (_statement == null)
                            throw new ScriptRunTimeError(node, "If with no matching endif");

                        break;
                    }
                case ASTNodeType.ELSEIF:
                    // If we hit the elseif statement during normal advancing, skip over it. The only way
                    // to execute an elseif clause is to jump directly in from an if statement.
                    depth = 0;

                    while (_statement != null)
                    {
                        node = _statement.FirstChild();

                        if (node.Type == ASTNodeType.IF)
                        {
                            depth++;
                        }
                        else if (node.Type == ASTNodeType.ENDIF)
                        {
                            if (depth == 0)
                                break;

                            depth--;
                        }

                        Advance();
                    }

                    if (_statement == null)
                        throw new ScriptRunTimeError(node, "If with no matching endif");

                    break;
                case ASTNodeType.ENDIF:
                    PopScope();
                    Advance();
                    break;
                case ASTNodeType.ELSE:
                    // If we hit the else statement during normal advancing, skip over it. The only way
                    // to execute an else clause is to jump directly in from an if statement.
                    depth = 0;

                    while (_statement != null)
                    {
                        node = _statement.FirstChild();

                        if (node.Type == ASTNodeType.IF)
                        {
                            depth++;
                        }
                        else if (node.Type == ASTNodeType.ENDIF)
                        {
                            if (depth == 0)
                                break;

                            depth--;
                        }

                        Advance();
                    }

                    if (_statement == null)
                        throw new ScriptRunTimeError(node, "If with no matching endif");

                    break;
                case ASTNodeType.WHILE:
                    {
                        // When we first enter the loop, push a new scope
                        if (_scope.StartNode != node)
                        {
                            PushScope(node);
                        }

                        var expr = node.FirstChild();
                        var result = EvaluateExpression(ref expr);

                        // Advance to next statement
                        Advance();

                        // The expression evaluated false, so keep advancing until
                        // we hit an endwhile statement.
                        if (!result)
                        {
                            depth = 0;

                            while (_statement != null)
                            {
                                node = _statement.FirstChild();

                                if (node.Type == ASTNodeType.WHILE)
                                {
                                    depth++;
                                }
                                else if (node.Type == ASTNodeType.ENDWHILE)
                                {
                                    if (depth == 0)
                                    {
                                        PopScope();
                                        // Go one past the endwhile so the loop doesn't repeat
                                        Advance();
                                        break;
                                    }

                                    depth--;
                                }

                                Advance();
                            }
                        }
                        break;
                    }
                case ASTNodeType.ENDWHILE:
                    // Walk backward to the while statement
                    _statement = _statement.Prev();

                    depth = 0;

                    while (_statement != null)
                    {
                        node = _statement.FirstChild();

                        if (node.Type == ASTNodeType.ENDWHILE)
                        {
                            depth++;
                        }
                        else if (node.Type == ASTNodeType.WHILE)
                        {
                            if (depth == 0)
                                break;

                            depth--;
                        }

                        _statement = _statement.Prev();
                    }

                    if (_statement == null)
                        throw new ScriptRunTimeError(node, "Unexpected endwhile");

                    break;
                case ASTNodeType.FOR:
                    {
                        // The iterator variable's name is the hash code of the for loop's ASTNode.
                        var iterName = node.GetHashCode().ToString();

                        // When we first enter the loop, push a new scope
                        if (_scope.StartNode != node)
                        {
                            PushScope(node);

                            // Grab the ArgumentList
                            var max = node.FirstChild();

                            if (max.Type != ASTNodeType.INTEGER)
                                throw new ScriptRunTimeError(max, "Invalid for loop syntax");

                            // Create a dummy argument that acts as our loop variable
                            var iter = new ASTNode(ASTNodeType.INTEGER, "0", node, 0);

                            _scope.SetVar(iterName, new Argument(this, iter));
                        }
                        else
                        {
                            // Increment the iterator argument
                            var arg = _scope.GetVar(iterName);

                            var iter = new ASTNode(ASTNodeType.INTEGER, (arg.As<uint>() + 1).ToString(), node, 0);

                            _scope.SetVar(iterName, new Argument(this, iter));
                        }

                        // Check loop condition
                        var i = _scope.GetVar(iterName);

                        // Grab the max value to iterate to
                        node = node.FirstChild();
                        var end = new Argument(this, node);

                        if (i.As<uint>() < end.As<uint>())
                        {
                            // enter the loop
                            Advance();
                        }
                        else
                        {
                            // Walk until the end of the loop
                            Advance();

                            depth = 0;

                            while (_statement != null)
                            {
                                node = _statement.FirstChild();

                                if (node.Type == ASTNodeType.FOR ||
                                    node.Type == ASTNodeType.FOREACH)
                                {
                                    depth++;
                                }
                                else if (node.Type == ASTNodeType.ENDFOR)
                                {
                                    if (depth == 0)
                                    {
                                        PopScope();
                                        // Go one past the end so the loop doesn't repeat
                                        Advance();
                                        break;
                                    }

                                    depth--;
                                }

                                Advance();
                            }
                        }
                    }
                    break;
                case ASTNodeType.FOREACH:
                    {
                        // foreach VAR in LIST
                        // The iterator's name is the hash code of the for loop's ASTNode.
                        var varName = node.FirstChild().Lexeme;
                        var listName = node.FirstChild().Next().Lexeme;
                        var iterName = node.GetHashCode().ToString();

                        // When we first enter the loop, push a new scope
                        if (_scope.StartNode != node)
                        {
                            PushScope(node);

                            // Create a dummy argument that acts as our iterator object
                            var iter = new ASTNode(ASTNodeType.INTEGER, "0", node, 0);
                            _scope.SetVar(iterName, new Argument(this, iter));

                            // Make the user-chosen variable have the value for the front of the list
                            var arg = Interpreter.GetListValue(listName, 0);

                            if (arg != null)
                                _scope.SetVar(varName, arg);
                            else
                                _scope.ClearVar(varName);
                        }
                        else
                        {
                            // Increment the iterator argument
                            var idx = _scope.GetVar(iterName).As<int>() + 1;
                            var iter = new ASTNode(ASTNodeType.INTEGER, idx.ToString(), node, 0);
                            _scope.SetVar(iterName, new Argument(this, iter));

                            // Update the user-chosen variable
                            var arg = Interpreter.GetListValue(listName, idx);

                            if (arg != null)
                                _scope.SetVar(varName, arg);
                            else
                                _scope.ClearVar(varName);
                        }

                        // Check loop condition
                        var i = _scope.GetVar(varName);

                        if (i != null)
                        {
                            // enter the loop
                            Advance();
                        }
                        else
                        {
                            // Walk until the end of the loop
                            Advance();

                            depth = 0;

                            while (_statement != null)
                            {
                                node = _statement.FirstChild();

                                if (node.Type == ASTNodeType.FOR ||
                                    node.Type == ASTNodeType.FOREACH)
                                {
                                    depth++;
                                }
                                else if (node.Type == ASTNodeType.ENDFOR)
                                {
                                    if (depth == 0)
                                    {
                                        PopScope();
                                        // Go one past the end so the loop doesn't repeat
                                        Advance();
                                        break;
                                    }

                                    depth--;
                                }

                                Advance();
                            }
                        }
                        break;
                    }
                case ASTNodeType.ENDFOR:
                    // Walk backward to the for statement
                    _statement = _statement.Prev();

                    while (_statement != null)
                    {
                        node = _statement.FirstChild();

                        if (node.Type == ASTNodeType.FOR ||
                            node.Type == ASTNodeType.FOREACH)
                        {
                            break;
                        }

                        _statement = _statement.Prev();
                    }

                    if (_statement == null)
                        throw new ScriptRunTimeError(node, "Unexpected endfor");

                    break;
                case ASTNodeType.BREAK:
                    // Walk until the end of the loop
                    Advance();

                    depth = 0;

                    while (_statement != null)
                    {
                        node = _statement.FirstChild();

                        if (node.Type == ASTNodeType.WHILE ||
                            node.Type == ASTNodeType.FOR ||
                            node.Type == ASTNodeType.FOREACH)
                        {
                            depth++;
                        }
                        else if (node.Type == ASTNodeType.ENDWHILE ||
                            node.Type == ASTNodeType.ENDFOR)
                        {
                            if (depth == 0)
                            {
                                PopScope();

                                // Go one past the end so the loop doesn't repeat
                                Advance();
                                break;
                            }

                            depth--;
                        }

                        Advance();
                    }

                    PopScope();
                    break;
                case ASTNodeType.CONTINUE:
                    // Walk backward to the loop statement
                    _statement = _statement.Prev();

                    depth = 0;

                    while (_statement != null)
                    {
                        node = _statement.FirstChild();

                        if (node.Type == ASTNodeType.ENDWHILE ||
                            node.Type == ASTNodeType.ENDFOR)
                        {
                            depth++;
                        }
                        else if (node.Type == ASTNodeType.WHILE ||
                                 node.Type == ASTNodeType.FOR ||
                                 node.Type == ASTNodeType.FOREACH)
                        {
                            if (depth == 0)
                                break;

                            depth--;
                        }

                        _statement = _statement.Prev();
                    }

                    if (_statement == null)
                        throw new ScriptRunTimeError(node, "Unexpected continue");
                    break;
                case ASTNodeType.STOP:
                    _statement = null;
                    break;
                case ASTNodeType.REPLAY:
                    _statement = _statement.Parent.FirstChild();
                    break;
                case ASTNodeType.QUIET:
                case ASTNodeType.FORCE:
                case ASTNodeType.COMMAND:
                    if (ExecuteCommand(node))
                        Advance();

                    break;
            }

            return (_statement != null) ? true : false;
        }

        public void Advance()
        {
            Interpreter.ClearTimeout();
            _statement = _statement.Next();
        }

        private ASTNode EvaluateModifiers(ASTNode node, out bool quiet, out bool force, out bool not)
        {
            quiet = false;
            force = false;
            not = false;

            while (true)
            {
                switch (node.Type)
                {
                    case ASTNodeType.QUIET:
                        quiet = true;
                        break;
                    case ASTNodeType.FORCE:
                        force = true;
                        break;
                    case ASTNodeType.NOT:
                        not = true;
                        break;
                    default:
                        return node;
                }

                node = node.Next();
            }
        }

        private bool ExecuteCommand(ASTNode node)
        {
            node = EvaluateModifiers(node, out bool quiet, out bool force, out _);
            try
            {
                if(!Commands.Definitions.ContainsKey(node.Lexeme))
                    throw new ScriptRunTimeError(node, "Command is not defined");

                var executionResult = Commands.Definitions[node.Lexeme].Process(ConstructArgumentList(ref node), force);

                // Attention - even if command logic does noe execute, it parses arguments and therefore should be consuming all nodes
                if (node != null)
                    throw new ScriptRunTimeError(node, "Command did not consume all available ArgumentList");

                return executionResult;
            }
            catch(ScriptRunTimeError ex)
            {
                // If quiete consume script related error to ignore it
                if(!quiet)
                    throw ex;
                return false;
            }  
        }

        private bool EvaluateExpression(ref ASTNode expr)
        {
            if (expr == null || (expr.Type != ASTNodeType.UNARY_EXPRESSION && expr.Type != ASTNodeType.BINARY_EXPRESSION && expr.Type != ASTNodeType.LOGICAL_EXPRESSION))
                throw new ScriptRunTimeError(expr, "No expression following control statement");

            var node = expr.FirstChild();

            if (node == null)
                throw new ScriptRunTimeError(expr, "Empty expression following control statement");

            switch (expr.Type)
            {
                case ASTNodeType.UNARY_EXPRESSION:
                    return EvaluateUnaryExpression(ref node);
                case ASTNodeType.BINARY_EXPRESSION:
                    return EvaluateBinaryExpression(ref node);
            }

            bool lhs = EvaluateExpression(ref node);

            node = node.Next();

            while (node != null)
            {
                // Capture the operator
                var op = node.Type;
                node = node.Next();

                if (node == null)
                    throw new ScriptRunTimeError(node, "Invalid logical expression");

                bool rhs;

                var e = node.FirstChild();

                switch (node.Type)
                {
                    case ASTNodeType.UNARY_EXPRESSION:
                        rhs = EvaluateUnaryExpression(ref e);
                        break;
                    case ASTNodeType.BINARY_EXPRESSION:
                        rhs = EvaluateBinaryExpression(ref e);
                        break;
                    default:
                        throw new ScriptRunTimeError(node, "Nested logical expressions are not possible");
                }

                switch (op)
                {
                    case ASTNodeType.AND:
                        lhs = lhs && rhs;
                        break;
                    case ASTNodeType.OR:
                        lhs = lhs || rhs;
                        break;
                    default:
                        throw new ScriptRunTimeError(node, "Invalid logical operator");
                }

                node = node.Next();
            }

            return lhs;
        }

        private bool CompareOperands(ASTNodeType op, IComparable lhs, IComparable rhs)
        {
            if (lhs.GetType() != rhs.GetType())
            {
                // Different types. Try to convert one to match the other.

                if (rhs is double)
                {
                    // Special case for rhs doubles because we don't want to lose precision.
                    lhs = (double)lhs;
                }
                else if (rhs is bool)
                {
                    // Special case for rhs bools because we want to down-convert the lhs.
                    var tmp = Convert.ChangeType(lhs, typeof(bool));
                    lhs = (IComparable)tmp;
                }
                else
                {
                    var tmp = Convert.ChangeType(rhs, lhs.GetType());
                    rhs = (IComparable)tmp;
                }
            }

            try
            {
                // Evaluate the whole expression
                switch (op)
                {
                    case ASTNodeType.EQUAL:
                        return lhs.CompareTo(rhs) == 0;
                    case ASTNodeType.NOT_EQUAL:
                        return lhs.CompareTo(rhs) != 0;
                    case ASTNodeType.LESS_THAN:
                        return lhs.CompareTo(rhs) < 0;
                    case ASTNodeType.LESS_THAN_OR_EQUAL:
                        return lhs.CompareTo(rhs) <= 0;
                    case ASTNodeType.GREATER_THAN:
                        return lhs.CompareTo(rhs) > 0;
                    case ASTNodeType.GREATER_THAN_OR_EQUAL:
                        return lhs.CompareTo(rhs) >= 0;
                }
            }
            catch (ArgumentException e)
            {
                throw new ScriptRunTimeError(null, e.Message);
            }

            throw new ScriptRunTimeError(null, "Unknown operator in expression");

        }

        private bool EvaluateUnaryExpression(ref ASTNode node)
        {
            node = EvaluateModifiers(node, out bool quiet, out _, out bool not);

            var handler = Interpreter.GetExpressionHandler(node.Lexeme);

            if (handler == null)
                throw new ScriptRunTimeError(node, "Unknown expression");

            var result = handler(node.Lexeme, ConstructArgumentList(ref node), quiet);

            if (not)
                return CompareOperands(ASTNodeType.EQUAL, result, false);
            else
                return CompareOperands(ASTNodeType.EQUAL, result, true);
        }

        private bool EvaluateBinaryExpression(ref ASTNode node)
        {
            // Evaluate the left hand side
            var lhs = EvaluateBinaryOperand(ref node);

            // Capture the operator
            var op = node.Type;
            node = node.Next();

            // Evaluate the right hand side
            var rhs = EvaluateBinaryOperand(ref node);

            return CompareOperands(op, lhs, rhs);
        }

        private IComparable EvaluateBinaryOperand(ref ASTNode node)
        {
            IComparable val;

            node = EvaluateModifiers(node, out bool quiet, out _, out _);
            switch (node.Type)
            {
                case ASTNodeType.INTEGER:
                    val = TypeConverter.To<int>(node.Lexeme);
                    break;
                case ASTNodeType.SERIAL:
                    val = TypeConverter.To<uint>(node.Lexeme);
                    break;
                case ASTNodeType.STRING:
                    val = node.Lexeme;
                    break;
                case ASTNodeType.DOUBLE:
                    val = TypeConverter.To<double>(node.Lexeme);
                    break;
                case ASTNodeType.OPERAND:
                    {
                        // This might be a registered keyword, so do a lookup
                        var handler = Interpreter.GetExpressionHandler(node.Lexeme);

                        if (handler == null)
                        {
                            // It's just a string
                            val = node.Lexeme;
                        }
                        else
                        {
                            val = handler(node.Lexeme, ConstructArgumentList(ref node), quiet);
                        }
                        break;
                    }
                default:
                    throw new ScriptRunTimeError(node, "Invalid type found in expression");
            }

            return val;
        }
    }

    public static class Interpreter
    {
        // Lists
        private static Dictionary<string, List<Argument>> _lists = new Dictionary<string, List<Argument>>();

        // Timers
        private static Dictionary<string, DateTime> _timers = new Dictionary<string, DateTime>();

        // Expressions
        public delegate IComparable ExpressionHandler(string expression, Argument[] args, bool quiet);
        public delegate T ExpressionHandler<T>(string expression, Argument[] args, bool quiet) where T : IComparable;

        private static Dictionary<string, ExpressionHandler> _exprHandlers = new Dictionary<string, ExpressionHandler>();

        private static ScriptExecutionState _activeScript = null;

        private enum ExecutionState
        {
            RUNNING,
            PAUSED,
            TIMING_OUT
        };

        public delegate bool TimeoutCallback();

        private static ExecutionState _executionState = ExecutionState.RUNNING;
        private static long _pauseTimeout = long.MaxValue;
        private static TimeoutCallback _timeoutCallback = null;

        public static CultureInfo Culture;

        static Interpreter()
        {
            Culture = new CultureInfo(CultureInfo.CurrentCulture.LCID, false);
            Culture.NumberFormat.NumberDecimalSeparator = ".";
            Culture.NumberFormat.NumberGroupSeparator = ",";
        }

        public static void RegisterExpressionHandler<T>(string keyword, ExpressionHandler<T> handler) where T : IComparable
        {
            _exprHandlers[keyword] = (expression, args, quiet) => handler(expression, args, quiet);
        }

        public static ExpressionHandler GetExpressionHandler(string keyword)
        {
            _exprHandlers.TryGetValue(keyword, out var expression);

            return expression;
        }

        public static void CreateList(string name)
        {
            if (_lists.ContainsKey(name))
                return;

            _lists[name] = new List<Argument>();
        }

        public static void DestroyList(string name)
        {
            _lists.Remove(name);
        }

        public static void ClearList(string name)
        {
            if (!_lists.ContainsKey(name))
                return;

            _lists[name].Clear();
        }

        public static bool ListExists(string name)
        {
            return _lists.ContainsKey(name);
        }

        public static bool ListContains(string name, Argument arg)
        {
            if (!_lists.ContainsKey(name))
                throw new ScriptRunTimeError(null, "List does not exist");

            return _lists[name].Contains(arg);
        }

        public static int ListLength(string name)
        {
            if (!_lists.ContainsKey(name))
                throw new ScriptRunTimeError(null, "List does not exist");

            return _lists[name].Count;
        }

        public static void PushList(string name, Argument arg, bool front, bool unique)
        {
            if (!_lists.ContainsKey(name))
                throw new ScriptRunTimeError(null, "List does not exist");

            if (unique && _lists[name].Contains(arg))
                return;

            if (front)
                _lists[name].Insert(0, arg);
            else
                _lists[name].Add(arg);
        }

        public static bool PopList(string name, Argument arg)
        {
            if (!_lists.ContainsKey(name))
                throw new ScriptRunTimeError(null, "List does not exist");

            return _lists[name].Remove(arg);
        }

        public static bool PopList(string name, bool front)
        {
            if (!_lists.ContainsKey(name))
                throw new ScriptRunTimeError(null, "List does not exist");

            var idx = front ? 0 : _lists[name].Count - 1;

            _lists[name].RemoveAt(idx);

            return _lists[name].Count > 0;
        }

        public static Argument GetListValue(string name, int idx)
        {
            if (!_lists.ContainsKey(name))
                throw new ScriptRunTimeError(null, "List does not exist");

            var list = _lists[name];

            if (idx < list.Count)
                return list[idx];

            return null;
        }

        public static void CreateTimer(string name)
        {
            _timers[name] = DateTime.UtcNow;
        }

        public static TimeSpan GetTimer(string name)
        {
            if (!_timers.TryGetValue(name, out DateTime timestamp))
                throw new ScriptRunTimeError(null, "Timer does not exist");

            TimeSpan elapsed = DateTime.UtcNow - timestamp;

            return elapsed;
        }

        public static void SetTimer(string name, int elapsed)
        {
            // Setting a timer to start at a given value is equivalent to
            // starting the timer that number of milliseconds in the past.
            _timers[name] = DateTime.UtcNow.AddMilliseconds(-elapsed);
        }

        public static void RemoveTimer(string name)
        {
            _timers.Remove(name);
        }

        public static bool TimerExists(string name)
        {
            return _timers.ContainsKey(name);
        }

        public static bool StartScript(ScriptExecutionState script)
        {
            if (_activeScript != null)
                return false;

            _activeScript = script;
            _executionState = ExecutionState.RUNNING;

            ExecuteScript();

            return true;
        }

        public static void StopScript()
        {
            _activeScript = null;
            _executionState = ExecutionState.RUNNING;
        }

        public static bool ExecuteScript()
        {
            if (_activeScript == null)
                return false;

            if (_executionState == ExecutionState.PAUSED)
            {
                if (_pauseTimeout < DateTime.UtcNow.Ticks)
                    _executionState = ExecutionState.RUNNING;
                else
                    return true;
            }
            else if (_executionState == ExecutionState.TIMING_OUT)
            {
                if (_pauseTimeout < DateTime.UtcNow.Ticks)
                {
                    if (_timeoutCallback != null)
                    {
                        if (_timeoutCallback())
                        {
                            _activeScript.Advance();
                            ClearTimeout();
                        }

                        _timeoutCallback = null;
                    }

                    /* If the callback changed the state to running, continue
                     * on. Otherwise, exit.
                     */
                    if (_executionState != ExecutionState.RUNNING)
                    {
                        _activeScript = null;
                        return false;
                    }
                }
            }

            // Execute script (parsing all nodes, executing what possible and queing majority of commands)
            if (!_activeScript.ExecuteNext())
            {
                _activeScript = null;
                return false;
            }

            return true;
        }

        // Pause execution for the given number of milliseconds
        public static void Pause(long duration)
        {
            // Already paused or timing out
            if (_executionState != ExecutionState.RUNNING)
                return;

            _pauseTimeout = DateTime.UtcNow.Ticks + (duration * 10000);
            _executionState = ExecutionState.PAUSED;
        }

        // Unpause execution
        public static void Unpause()
        {
            if (_executionState != ExecutionState.PAUSED)
                return;

            _pauseTimeout = 0;
            _executionState = ExecutionState.RUNNING;
        }

        // If forward progress on the script isn't made within this
        // amount of time (milliseconds), bail
        public static void Timeout(long duration, TimeoutCallback callback)
        {
            // Don't change an existing timeout
            if (_executionState != ExecutionState.RUNNING)
                return;

            _pauseTimeout = DateTime.UtcNow.Ticks + (duration * 10000);
            _executionState = ExecutionState.TIMING_OUT;
            _timeoutCallback = callback;
        }

        // Clears any previously set timeout. Automatically
        // called any time the script advances a statement.
        public static void ClearTimeout()
        {
            if (_executionState != ExecutionState.TIMING_OUT)
                return;

            _pauseTimeout = 0;
            _executionState = ExecutionState.RUNNING;
        }
    }
}