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

using System.Linq;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Resources;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps.Options
{
    internal class OptionsScriptsPage : Control
    {
        // script
        private ScriptEditorControl _scriptEditorControl;

        private enum Buttons
        {
            NewScript = 1000,
            DeleteScript = 1001,
            OpenScriptDir = 1002,
        }

        void createScriptEditorControl(Script script)
        {
            _scriptEditorControl?.Dispose();

            _scriptEditorControl = new ScriptEditorControl(script, new Rectangle
            {
                X = 350,
                Y = 20,
                Width = 340,
                Height = 385
            });

            Add(_scriptEditorControl);
        }

        int buttonY(int button) => button * 20 + (button - 1) * 12;

        NiceButton createScriptListButton(Script script, ScrollArea list)
        {
            var btn = new NiceButton(0, (list.Children.Count - 1) * 25, 130, 25, ButtonAction.Activate, script.DisplayName)
            {
                Tag = script
            };

            script.NameChangedEvent += (s) =>
            {
                btn.TextLabel.Text = s.DisplayName + (s.HasChanges ? "*" : "");
            };

            script.HasChangesChangedEvent += (s) =>
            {
                btn.TextLabel.Text = s.DisplayName + (s.HasChanges ? "*" : "");
            };

            list.Add(btn);

            // todo: implement Script buttons like Macro buttons

            //btn.DragBegin += (sss, eee) =>
            //{
            //    if (UIManager.IsDragging || Math.Max(Math.Abs(Mouse.LDroppedOffset.X), Math.Abs(Mouse.LDroppedOffset.Y)) < 5
            //                             || btn.ScreenCoordinateX > Mouse.LDropPosition.X || btn.ScreenCoordinateX < Mouse.LDropPosition.X - btn.Width
            //                             || btn.ScreenCoordinateY > Mouse.LDropPosition.Y || btn.ScreenCoordinateY + btn.Height < Mouse.LDropPosition.Y)
            //    {
            //        return;
            //    }

            //    UIManager.Gumps.OfType<MacroButtonGump>()
            //             .FirstOrDefault(s => s._macro == macro)
            //             ?.Dispose();

            //    MacroButtonGump macroButtonGump = new MacroButtonGump(macro, Mouse.LDropPosition.X, Mouse.LDropPosition.Y);
            //    UIManager.Add(macroButtonGump);
            //    UIManager.AttemptDragControl(macroButtonGump, new Point(Mouse.Position.X + (macroButtonGump.Width >> 1), Mouse.Position.Y + (macroButtonGump.Height >> 1)), true);
            //};

            btn.MouseUp += (sss, eee) =>
            {
                createScriptEditorControl(script);
            };

            return btn;
        }

        void showValidationErrorDialog(string message)
        {
            UIManager.Add(new MessageBoxGump(
                250, 150,
                message,
                null)
            {
                CanCloseWithRightClick = true
            });
        }

        void onClickAddButton(string name, ScrollArea scriptsList)
        {
            var error = Client.Game.GetScene<GameScene>()
              .Scripts
              .ValidateCreate(name);

            if (!string.IsNullOrWhiteSpace(error))
            {
                showValidationErrorDialog(error);
                return;
            }

            var script = Client.Game
                  .GetScene<GameScene>()
                  .Scripts
                  .Create(name);

            var btn = createScriptListButton(script, scriptsList);

            btn.IsSelected = true;

            createScriptEditorControl(script);
        }

        void showAddScriptDialog(ScrollArea scriptsList)
        {
            UIManager.Add(new EntryDialog
            (
                250, 150, ResGumps.ScriptsManager_UI_ScriptName, name =>
                {
                    onClickAddButton(name, scriptsList);
                }
            )
            {
                CanCloseWithRightClick = true
            });
        }

        void onClickDeleteButton(NiceButton button, Script script, ScrollArea scriptsList)
        {
            button.Dispose();

            if (_scriptEditorControl != null)
            {
                // todo: when implementing the dockable script buttons, need to mirror how Macros delete the buttons
                //UIManager.Gumps.OfType<MacroButtonGump>()
                //         .FirstOrDefault(s => s._macro == control.Macro)
                //         ?.Dispose();
            }

            Client.Game
                  .GetScene<GameScene>()
                  .Scripts
                  .Delete(script);

            var nextAvailable = scriptsList.Children
                           .Where(s => !s.IsDisposed)
                           .OfType<NiceButton>()
                           .FirstOrDefault();

            if (nextAvailable == null)
            {
                _scriptEditorControl?.Dispose();
            }
            else
            {
                nextAvailable.IsSelected = true;
                createScriptEditorControl((Script)nextAvailable.Tag);
            }
        }

        void showDeleteScriptDialog(NiceButton nb, ScrollArea scriptsList)
        {
            UIManager.Add(new QuestionGump
             (
                 ResGumps.ScriptsManager_UI_DeleteConfirmation, b =>
                 {
                     if (!b)
                     {
                         return;
                     }

                     onClickDeleteButton(nb, (Script)nb.Tag, scriptsList);
                 }
             ));
        }

        public void Build()
        {
            var scriptsList = new ScrollArea(190, buttonY(4) - 4, 150, 360 - 31, true);

            Add(scriptsList);

            foreach (var script in Client.Game.GetScene<GameScene>().Scripts.GetAllScripts())
            {
                createScriptListButton(script, scriptsList);
            }

            var first = scriptsList
                .Children
                .OfType<NiceButton>()
                .FirstOrDefault();

            if (first != null)
            {
                first.IsSelected = true;
                createScriptEditorControl((Script)first.Tag);
            }


            NiceButton addScriptButton = new NiceButton(190, buttonY(1), 130, 20, ButtonAction.Activate, ResGumps.ScriptsManager_UI_NewScript) { IsSelectable = false, ButtonParameter = (int)Buttons.NewScript };

            addScriptButton.MouseUp += (sender, e) =>
            {
                showAddScriptDialog(scriptsList);
            };

            Add(addScriptButton);

            NiceButton deleteScriptButton = new NiceButton(190, buttonY(2), 130, 20, ButtonAction.Activate, ResGumps.ScriptsManager_UI_DeleteScript) { IsSelectable = false, ButtonParameter = (int)Buttons.DeleteScript };

            deleteScriptButton.MouseUp += (ss, ee) =>
            {
                NiceButton nb = scriptsList.Children
                                           .OfType<NiceButton>()
                                           .SingleOrDefault(a => a.IsSelected);

                if (nb == null) return;

                showDeleteScriptDialog(nb, scriptsList);
            };

            Add(deleteScriptButton);

            NiceButton openScriptsDirectoryButton = new NiceButton(190, buttonY(3), 130, 20, ButtonAction.Activate, ResGumps.ScriptsManager_UI_OpenScriptsDirectory) { IsSelectable = false, ButtonParameter = (int)Buttons.OpenScriptDir };

            openScriptsDirectoryButton.MouseUp += (ss, ee) =>
            {
                Client.Game
                  .GetScene<GameScene>()
                  .Scripts
                  .OpenScriptsDirectory();
            };

            Add(openScriptsDirectoryButton);

            Add(new Line(190, buttonY(3) + 26, 150, 1, Color.Gray.PackedValue));
            Add(new Line(191 + 150, 21, 1, 418, Color.Gray.PackedValue));
        }
    }
}