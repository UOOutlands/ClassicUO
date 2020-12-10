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
using System.Diagnostics;
using System.IO;
using System.Linq;
using ClassicUO.Configuration;
using ClassicUO.Game.Scripting;
using ClassicUO.Resources;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Game.Managers
{
    internal class ScriptManager
    {
        readonly List<Script> _scripts = new List<Script>();

        string ScriptsDirectory
        {
            get
            {
                return Path.Combine(
                    CUOEnviroment.ExecutablePath,
                    "Data",
                    "Profiles",
                    ProfileManager.CurrentProfile.Username,
                    ProfileManager.CurrentProfile.ServerName,
                    ProfileManager.CurrentProfile.CharacterName,
                    "Scripts");
            }
        }

        Script _activeScript;

        static ScriptManager()
        {
            Aliases.Register();
            Commands.Register();
            Expressions.Register();
        }

        public void Load()
        {
            try
            {
                Stop();

                _scripts.Clear();

                EnsureScriptsDirectoryExists();
                LoadScripts();
                CreateDefaultScriptIfNoneExist();
            }
            catch (Exception ex)
            {
                Log.Error($"{nameof(ScriptManager)}::{nameof(Load)}() - \r\n{ex}");
            }
        }

        private void CreateDefaultScriptIfNoneExist()
        {
            if (!_scripts.Any())
            {
                Create("Hello UO");
            }
        }

        private void LoadScripts()
        {
            _scripts.AddRange(
                Directory.GetFiles(ScriptsDirectory, "*.uos")
                .Where(s => s.EndsWith(".uos")) // ignore .bak files
                .Select(s => new Script(s)));

            foreach (var script in _scripts)
            {
                try
                {
                    script.Load();
                }
                catch (Exception ex)
                {
                    Log.Error($"{nameof(ScriptManager)}::{nameof(Load)}() - " +
                        $"there was a problem loading script: '{script.DisplayName}': \r\n" +
                        $"{ex}");
                }
            }
        }

        public List<Script> GetAllScripts() => _scripts.ToList();

        private void EnsureScriptsDirectoryExists()
        {
            if (!Directory.Exists(ScriptsDirectory))
            {
                Directory.CreateDirectory(ScriptsDirectory);
            }
        }

        public string ValidateCreate(string name)
        {
            // name is invalid if it is empty
            if (string.IsNullOrWhiteSpace(name))
            {
                return ResGumps.ScriptsManager_Create_InvalidName;
            }

            // name is invalid if it matches an existing script
            if (_scripts.Any(s => string.Compare(name, s.DisplayName, true) == 0))
            {
                return ResGumps.ScriptsManager_Create_InvalidName;
            }

            // name is invalid if it cannot directly be turned into a file name
            foreach (var invalidCharacter in Path.GetInvalidFileNameChars())
            {
                if (name.Contains(invalidCharacter))
                {
                    return ResGumps.ScriptsManager_Create_InvalidName;
                }
            }

            return null;
        }

        public Script Create(string name)
        {
            if (ValidateCreate(name) != null)
            {
                Log.Warn($"{nameof(ScriptManager)}::{nameof(Create)}() - failed validation.");
                return null;
            }

            EnsureScriptsDirectoryExists();

            var filePath = Path.Combine(ScriptsDirectory, $"{name}.uos");

            File.WriteAllText(filePath, "msg \"Hello UO!\"");

            Script script = new Script(filePath);
            script.Load();
            _scripts.Add(script);
            return script;
        }

        public void Stop()
        {
            _activeScript = null;
            Interpreter.StopScript();
        }

        public void Run(Script script)
        {
            Stop();

            try
            {
                _activeScript = script;
                Interpreter.StartScript(new Scripting.ScriptExecutionState(script.AST));
            }
            catch (Exception ex)
            {
                var errMsg = $"There was a problem starting script: '{_activeScript.DisplayName}' \r\n" +
                    $"{ex}";

                Log.Error($"{nameof(ScriptManager)}::{nameof(Run)}() - " + errMsg);

                GameActions.Print(errMsg);

                Stop();
            }
        }

        public void Rename(Script script, string name)
        {
            var original = script.DisplayName;

            try
            {
                script.Rename(name);
            }
            catch (Exception ex)
            {
                Log.Error($"{nameof(ScriptManager)}::{nameof(Rename)}() - " +
                    $"there was a problem renaming script from: '{original}' to: '{name}' \r\n" +
                    $"{ex}");
            }
        }

        public void Delete(Script script)
        {
            try
            {
                _scripts.Remove(script);
                File.Delete(script.FilePath);
                script.Dispose();
            }
            catch (Exception ex)
            {
                Log.Error($"{nameof(ScriptManager)}::{nameof(Delete)}() - " +
                    $"there was a problem deleting script: '{script.DisplayName}': \r\n" +
                    $"{ex}");
            }
        }

        public void Update()
        {
            try
            {
                Interpreter.ExecuteScript();
            }
            catch (Exception ex)
            {
                var errMsg = $"There was a problem executing script: '{_activeScript.DisplayName}' \r\n" +
                    $"{ex}";

                Log.Error($"{nameof(ScriptManager)}::{nameof(Update)}() - " + errMsg);

                GameActions.Print(errMsg);
            }
        }

        public void Save()
        {
            foreach (var script in _scripts)
            {
                try
                {
                    script.Backup();
                }
                catch (Exception ex)
                {
                    Log.Error($"{nameof(ScriptManager)}::{nameof(Save)}() - " +
                        $"there was a problem backing up script: '{script.DisplayName}': \r\n" +
                        $"{ex}");
                }
            }

            foreach (var script in _scripts)
            {
                try
                {
                    script.Save();
                }
                catch (Exception ex)
                {
                    Log.Error($"{nameof(ScriptManager)}::{nameof(Save)}() - " +
                        $"there was a problem saving script: '{script.DisplayName}': \r\n" +
                        $"{ex}");
                }
            }
        }

        public void OpenScriptsDirectory()
        {
            try
            {
                EnsureScriptsDirectoryExists();

                Process.Start(new ProcessStartInfo
                {
                    Arguments = ScriptsDirectory,
                    FileName = "explorer.exe"
                });
            }
            catch (Exception ex)
            {
                Log.Error($"{nameof(ScriptManager)}::{nameof(OpenScriptsDirectory)}() - " +
                    $"there was a problem opening the scripts directory '{ScriptsDirectory}': \r\n" +
                    $"{ex}");
            }
        }

        public void OpenInExternalEditor(Script script)
        {
            try
            {
                EnsureScriptsDirectoryExists();

                // try to open the file by using the association for the extension
                Process.Start(new ProcessStartInfo
                {
                    FileName = script.FilePath
                });
            }
            catch
            {
                // opening by association can throw an exception if one is not found
                // so we fall back to notepad

                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        Arguments = script.FilePath,
                        FileName = "notepad.exe"
                    });
                }
                catch (Exception ex)
                {
                    Log.Error($"{nameof(ScriptManager)}::{nameof(OpenInExternalEditor)}() - " +
                        $"there was a problem opening the script in an external editor '{script.FilePath}': \r\n" +
                        $"{ex}");
                }
            }
        }

        public void OnSync(Script script)
        {
            try
            {
                script.Load();
            }
            catch (Exception ex)
            {
                Log.Error($"{nameof(ScriptManager)}::{nameof(OnSync)}() - " +
                    $"there was a problem syncing the script in an external editor '{script.FilePath}': \r\n" +
                    $"{ex}");
            }
        }
    }

    internal class Script : IDisposable
    {
        public event Action<Script> NameChangedEvent;
        public event Action<Script> HasChangesChangedEvent;

        public string BackupFilePath => FilePath.Replace($"{DisplayName}.uos", $"{DisplayName}.uos.bak");
        public string DisplayName => Path.GetFileNameWithoutExtension(FilePath);
        public string FilePath { get; private set; }

        string _text;
        public string Text

        {
            get { return _text; }
            set
            {
                if (_text == value) return;
                _text = value;
                _hasChanges = true;
                HasChangesChangedEvent?.Invoke(this);
            }
        }

        bool _hasChanges;
        public bool HasChanges
        {
            get { return _hasChanges; }
            private set
            {
                if (_hasChanges == value) return;
                _hasChanges = value;
                HasChangesChangedEvent?.Invoke(this);
            }
        }

        public Scripting.ASTNode AST { get; private set; }

        public Script(string filePath)
        {
            FilePath = filePath;
        }

        public void Rename(string newName)
        {
            var newFilePath = FilePath.Replace($"{DisplayName}.uos", $"{newName}.uos");
            var newBackupPath = BackupFilePath.Replace($"{DisplayName}.uos", $"{newName}.uos");

            File.Move(FilePath, newFilePath);

            if (File.Exists(BackupFilePath))
            {
                File.Move(BackupFilePath, newBackupPath);
            }

            FilePath = newFilePath;
            NameChangedEvent?.Invoke(this);
        }

        public void Load()
        {
            HasChanges = false;
            Text = File.ReadAllText(FilePath);
            Compile();
        }

        public void Compile()
        {
            try
            {
                var lines = Text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                AST = Lexer.Lex(lines);
            }
            catch (Exception ex)
            {
                var errMsg = $"There was a problem compiling the script '{DisplayName}': \r\n" +
                    $"{ex}";

                Log.Error($"{nameof(Script)}::{nameof(Compile)}() - " + errMsg);

                GameActions.Print(errMsg);
            }
        }

        public void Save()
        {
            if (!HasChanges) return;
            HasChanges = false;
            File.WriteAllText(FilePath, Text);
            Compile();
        }

        public void Backup()
        {
            File.Copy(FilePath, BackupFilePath, true);
        }

        public void Restore()
        {
            try
            {
                File.Copy(BackupFilePath, FilePath, true);
                Load();
                HasChanges = true;
            }
            catch (Exception ex)
            {
                Log.Error($"{nameof(Script)}::{nameof(Restore)}() - " +
                    $"there was a problem opening restoring from '{BackupFilePath}': \r\n" +
                    $"{ex}");
            }
        }

        public void Dispose()
        {
            NameChangedEvent = null;
        }
    }
}