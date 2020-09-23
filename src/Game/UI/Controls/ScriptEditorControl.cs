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
using ClassicUO.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Resources;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls
{
    internal class ScriptEditorControl : Control
    {
        readonly Script _script;
        readonly Rectangle _bounds;
        readonly ColorBox _bgColor;
        readonly ScrollArea _scrollArea;
        readonly StbTextBox _textBox;
        readonly NiceButton _restore;
        readonly NiceButton _openExt;
        readonly NiceButton _sync;
        readonly NiceButton _rename;
        readonly NiceButton _run;
        readonly NiceButton _stop;

        public ScriptEditorControl(Script script, Rectangle bounds)
        {
            X = bounds.X;
            Y = bounds.Y;

            _script = script;
            _bounds = bounds;

            _bgColor = new ColorBox(bounds.Width, bounds.Height - 25, 0, 0xFF2E2E2E)
            {
                Location = new Point(0, 27)
            };
            _scrollArea = new ScrollArea(0, 27, bounds.Width, bounds.Height - 25, true);

            // top buttons - for script/file management
            _restore = new NiceButton(0, 0, 75, 20, ButtonAction.Activate, ResGumps.ScriptsManager_UI_Restore) { IsSelectable = false };
            _restore.MouseUp += OnRestoreClicked;
            Add(_restore);

            _openExt = new NiceButton(75 + 5, 0, 75, 20, ButtonAction.Activate, ResGumps.ScriptsManager_UI_ExternalEditor) { IsSelectable = false };
            _openExt.MouseUp += OnOpenExternalEditorClicked;
            Add(_openExt);

            _sync = new NiceButton(75 + 75 + 5 + 5, 0, 75, 20, ButtonAction.Activate, ResGumps.ScriptsManager_UI_Sync) { IsSelectable = false };
            _sync.MouseUp += OnSyncClicked;
            Add(_sync);

            _rename = new NiceButton(75 + 75 + 75 + 5 + 5 + 5, 0, 75, 20, ButtonAction.Activate, ResGumps.ScriptsManager_UI_Rename) { IsSelectable = false };
            _rename.MouseUp += OnRenameClicked;
            Add(_rename);

            // bottom buttons - for testing / keybinding etc.
            _run = new NiceButton(0, 395, 75, 20, ButtonAction.Activate, ResGumps.ScriptsManager_UI_Run) { IsSelectable = false };
            _run.MouseUp += OnRunClicked;
            Add(_run);

            _stop = new NiceButton(75 + 5, 395, 75, 20, ButtonAction.Activate, ResGumps.ScriptsManager_UI_Stop) { IsSelectable = false };
            _stop.MouseUp += OnStopClicked;
            Add(_stop);

            bool useUnicode = Client.Version >= ClientVersion.CV_305D;
            byte unicodeFontIndex = 1;

            ushort textColor = 0xFFFF;

            _textBox = new StbTextBox(useUnicode ? unicodeFontIndex : (byte)9, -1, bounds.Width - 30, hue: textColor, isunicode: useUnicode)
            {
                X = 5,
                Y = 0,
                Width = bounds.Width - 30,
                Height = bounds.Height - 25,
                Multiline = true,
                IsEditable = true
            };
            _textBox.TextChanged += OnTextBoxTextChanged;

            _textBox.SetText(script.Text);

            _scrollArea.Add(_textBox);

            Add(_bgColor);
            Add(_scrollArea);
        }

        private void OnStopClicked(object sender, MouseEventArgs e)
        {
            Client.Game.GetScene<GameScene>()
                       .Scripts
                       .Stop();
        }

        private void OnRunClicked(object sender, MouseEventArgs e)
        {
            Client.Game.GetScene<GameScene>()
                       .Scripts
                       .Run(_script);
        }

        private void OnRenameClicked(object sender, MouseEventArgs e)
        {
            UIManager.Add(new EntryDialog
            (
                250, 150, ResGumps.ScriptsManager_UI_ScriptName, name =>
                {
                    var error = Client.Game.GetScene<GameScene>()
                      .Scripts
                      .ValidateCreate(name);

                    if (!string.IsNullOrWhiteSpace(error))
                    {
                        UIManager.Add(new MessageBoxGump(
                            250, 150,
                            error,
                            null)
                        {
                            CanCloseWithRightClick = true
                        });

                        return;
                    }

                    Client.Game
                          .GetScene<GameScene>()
                          .Scripts
                          .Rename(_script, name);
                }
            )
            {
                CanCloseWithRightClick = true
            });
        }

        private void OnSyncClicked(object sender, MouseEventArgs e)
        {
            Client.Game.GetScene<GameScene>().Scripts.OnSync(_script);
            _textBox.SetText(_script.Text);
        }

        private void OnOpenExternalEditorClicked(object sender, MouseEventArgs e)
        {
            Client.Game.GetScene<GameScene>().Scripts.OpenInExternalEditor(_script);
        }

        private void OnRestoreClicked(object sender, Input.MouseEventArgs e)
        {
            _script.Restore();
            _textBox.SetText(_script.Text);
        }

        private void OnTextBoxTextChanged(object sender, EventArgs e)
        {
            _textBox.Height = Math.Max(_bounds.Height, FontsLoader.Instance.GetHeightUnicode(1, _textBox.Text, _bounds.Width - 30, TEXT_ALIGN_TYPE.TS_LEFT, 0x0) + 5);

            foreach (Control c in _scrollArea.Children)
            {
                c.OnPageChanged();
            }

            _script.Text = _textBox.Text;
        }
    }
}
