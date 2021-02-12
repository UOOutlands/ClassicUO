using ClassicUO.Game.Data;
using ClassicUO.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ClassicUO.Game.Scripting
{
    // An argument in the the Abstract Syntax Tree (AST)
    // For example (but not restricted to) the arguments provided to a given command in the UO Steam script
    public class Argument
    {
        // Node of the Abstract Syntax Tree
        protected ASTNode _node;

        // Execution state
        protected ScriptExecutionState _script;

        public Argument(ScriptExecutionState script, ASTNode node)
        {
            _script = script;
            _node = node;
        }

        // Generic method to interpreter an argument as the desired type T
        public virtual T As<T>(string localAlias = "")
        {
            if (_node.Lexeme == null)
                throw new ScriptRunTimeError(_node, "Cannot convert argument to " + typeof(T).FullName);

            // 1 - Try to resolve it as a scoped variable first
            var arg = _script.Lookup(_node.Lexeme);
            if (arg != null)
                return arg.As<T>();

            // 2 - Try to resolve it as a local alias (like a ushort set to 0, when a "color", should be ushort.MaxValue)
            T value = default(T);
            if (localAlias != string.Empty)
            {
                string content = (string)Convert.ChangeType(_node.Lexeme, typeof(string));
                if(Aliases.Read<T>(localAlias, content, ref value))
                    return value;
            }

            // 3 - Try to resolve it as a global alias (like every uint as 'backpack' maps to the backpack serial)
            if (Aliases.Read<T>(_node.Lexeme, ref value))
                return value;

            // 4 - If neither a scope variable or an alias, convert type based on Lexeme
            if(typeof(T) == typeof(string))
                return (T)Convert.ChangeType(_node.Lexeme, typeof(string));
            else return TypeConverter.To<T>(_node.Lexeme);
        }

        #region Comparison overrided methods
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            Argument arg = obj as Argument;

            if (arg == null)
                return false;

            return Equals(arg);
        }

        public bool Equals(Argument other)
        {
            if (other == null)
                return false;

            return (other._node.Lexeme == _node.Lexeme);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        #endregion
    }

    // A virtualization of the Argument so that arguments may be created and injected in commands on the fly
    public class VirtualArgument : Argument
    {
        // Content of this virtual argument
        protected string _value;

        public VirtualArgument(string value) : base(null, null)
        {
            _value = value;
        }

        // Generic method to interpreter an argument as the desired type T
        public override T As<T>(string localAlias = "")
        {
            // 1 - Try to resolve it as a local alias (like a ushort set to 0, when a "color", should be ushort.MaxValue)
            T value = default(T);
            if (localAlias != string.Empty)
            {
                string content = (string)Convert.ChangeType(_value, typeof(string));
                if (Aliases.Read<T>(localAlias, content, ref value))
                    return value;
            }

            // 2 - Try to resolve it as a global alias (like every uint as 'backpack' maps to the backpack serial)
            if (Aliases.Read<T>(_value, ref value))
                return value;

            // 3 - If neither a scope variable or an alias, convert type based on stored value
            if (typeof(T) == typeof(string))
                return (T)Convert.ChangeType(_value, typeof(string));
            else return TypeConverter.To<T>(_value);
        }
    }

    // Encapsulate AST arguments so that command specific definitions can be evaluated and more easly processed
    // Using term "parameter" to make difference clear and align term used by scripting languages
    public class ParameterList
    {
        // Definition (name) for the type of each AST in the list
        private string[] _definitions;

        // All AST arguments provided to this operation (not necessarily a command)
        private Argument[] _args;

        // Index used to organize traversal of arguments
        private int _index;

        public enum Expectation
        {
            Mandatory,
            Optional,
        }

        public ParameterList(Argument[] args, string[] definitions)
        {
            _args = args;
            _definitions = definitions;
            _index = -1;
        }

        // Array like interface
        public Argument this[int i]
        {

            get { _index = i; return _args[_index]; }
            set { _index = i; _args[_index] = value; }
        }
        public int Length
        {
            get { return _args.Length; }
        }

        // Generic method to read next argument in the list as the desired type T
        public T NextAs<T>(Expectation type = Expectation.Optional)
        {
            if (_args.Length > _index + 1) // Could we read one more argument?
                return _args[++_index].As<T>(_definitions[_index]); // Yes, so read it as desired type (passinf default if possible)
            else if (type == Expectation.Optional) // Nop, but this is optional
                return default(T);
            //return GetDefault<T>(); // So return default without reading anything
            else throw new ScriptRunTimeError(null, typeof(T).FullName + " argument does not exist at " + _index); // Otherwise.. kaboooom
        }

        // Retrieve an arguments that contains and array of elements
        // Attention - inner values are not mapped to alias or local variables
        public T[] NextAsArray<T>(Expectation type = Expectation.Optional)
        {
            if (_args.Length > _index + 1)
            {
                // Read array of elements and convert them to desired array type
                string arrayStr = _args[++_index].As<string>(_definitions[_index]);
                string[] array = arrayStr.Replace(" ", "").Split(',');
                if (array.Length > 1)
                    return Array.ConvertAll(array, new Converter<string, T>(TypeConverter.To<T>));
                else return new T[1] { TypeConverter.To<T>(array[0]) };
            }
            else if (type == Expectation.Optional)
                return new T[] { default(T) };
                //return new T[] { GetDefault<T>() }; // Return single element (Length == 1) for non existent optionals
            else throw new ScriptRunTimeError(null, "List of " + typeof(T).FullName + " argument does not exist at " + _index);
        }  
    }
}
