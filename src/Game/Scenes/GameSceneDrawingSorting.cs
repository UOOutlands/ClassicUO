#region license

// Copyright (c) 2021, andreakarasho
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Map;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.Scenes
{
    internal partial class GameScene
    {
        private static GameObject[] _renderList = new GameObject[10000];
        private StaticTiles _empty;
        private Rectangle _foliageHitBox;

        private sbyte _maxGroundZ;
        private int _maxZ;
        private Vector2 _minPixel, _maxPixel;
        private bool _noDrawRoofs;
        private Point _offset, _maxTile, _minTile, _last_scaled_offset;
        private int _oldPlayerX, _oldPlayerY, _oldPlayerZ;
        private int _renderListCount;


        public Point ScreenOffset => _offset;

        public void UpdateMaxDrawZ(bool force = false)
        {
            int playerX = World.Player.X;
            int playerY = World.Player.Y;
            int playerZ = World.Player.Z;

            if (playerX == _oldPlayerX && playerY == _oldPlayerY && playerZ == _oldPlayerZ && !force)
            {
                return;
            }

            _oldPlayerX = playerX;
            _oldPlayerY = playerY;
            _oldPlayerZ = playerZ;

            sbyte maxGroundZ = 127;
            _maxGroundZ = 127;
            _maxZ = 127;
            _noDrawRoofs = !ProfileManager.CurrentProfile.DrawRoofs;
            int bx = playerX;
            int by = playerY;
            Chunk chunk = World.Map.GetChunk(bx, by, false);

            if (chunk != null)
            {
                int x = playerX % 8;
                int y = playerY % 8;

                int pz14 = playerZ + 14;
                int pz16 = playerZ + 16;

                for (GameObject obj = chunk.GetHeadObject(x, y); obj != null; obj = obj.TNext)
                {
                    sbyte tileZ = obj.Z;

                    if (obj is Land l)
                    {
                        if (l.IsStretched)
                        {
                            tileZ = l.AverageZ;
                        }

                        if (pz16 <= tileZ)
                        {
                            maxGroundZ = (sbyte) pz16;
                            _maxGroundZ = (sbyte) pz16;
                            _maxZ = _maxGroundZ;

                            break;
                        }

                        continue;
                    }

                    if (obj is Mobile)
                    {
                        continue;
                    }


                    //if (obj is Item it && !it.ItemData.IsRoof || !(obj is Static) && !(obj is Multi))
                    //    continue;

                    if (tileZ > pz14 && _maxZ > tileZ)
                    {
                        ref StaticTiles itemdata = ref TileDataLoader.Instance.StaticData[obj.Graphic];

                        //if (GameObjectHelper.TryGetStaticData(obj, out var itemdata) && ((ulong) itemdata.Flags & 0x20004) == 0 && (!itemdata.IsRoof || itemdata.IsSurface))
                        if (((ulong) itemdata.Flags & 0x20004) == 0 && (!itemdata.IsRoof || itemdata.IsSurface))
                        {
                            _maxZ = tileZ;
                            _noDrawRoofs = true;
                        }
                    }
                }

                int tempZ = _maxZ;
                _maxGroundZ = (sbyte) _maxZ;
                playerX++;
                playerY++;
                bx = playerX;
                by = playerY;
                chunk = World.Map.GetChunk(bx, by, false);

                if (chunk != null)
                {
                    x = playerX % 8;
                    y = playerY % 8;

                    for (GameObject obj2 = chunk.GetHeadObject(x, y); obj2 != null; obj2 = obj2.TNext)
                    {
                        //if (obj is Item it && !it.ItemData.IsRoof || !(obj is Static) && !(obj is Multi))
                        //    continue;

                        if (obj2 is Mobile)
                        {
                            continue;
                        }

                        sbyte tileZ = obj2.Z;

                        if (tileZ > pz14 && _maxZ > tileZ)
                        {
                            if (!(obj2 is Land))
                            {
                                ref StaticTiles itemdata = ref TileDataLoader.Instance.StaticData[obj2.Graphic];

                                if (((ulong) itemdata.Flags & 0x204) == 0 && itemdata.IsRoof)
                                {
                                    _maxZ = tileZ;
                                    World.Map.ClearBockAccess();
                                    _maxGroundZ = World.Map.CalculateNearZ(tileZ, playerX, playerY, tileZ);
                                    _noDrawRoofs = true;
                                }
                            }

                            //if (GameObjectHelper.TryGetStaticData(obj2, out var itemdata) && ((ulong) itemdata.Flags & 0x204) == 0 && itemdata.IsRoof)
                            //{
                            //    _maxZ = tileZ;
                            //    World.Map.ClearBockAccess();
                            //    _maxGroundZ = World.Map.CalculateNearZ(tileZ, playerX, playerY, tileZ);
                            //    _noDrawRoofs = true;
                            //}
                        }
                    }

                    tempZ = _maxGroundZ;
                }

                _maxZ = _maxGroundZ;

                if (tempZ < pz16)
                {
                    _maxZ = pz16;
                    _maxGroundZ = (sbyte) pz16;
                }

                _maxGroundZ = maxGroundZ;
            }
        }

        private void UpdateObjectHandles(GameObject obj, bool useObjectHandles)
        {
            if (useObjectHandles && NameOverHeadManager.IsAllowed(obj as Entity))
            {
                switch (obj.ObjectHandle)
                {
                    case GameObject.ObjectHandleState.NONE:
                    case GameObject.ObjectHandleState.CLOSING:
                        obj.ObjectHandle = GameObject.ObjectHandleState.NAME_NEEDED;
                        break;
                    case GameObject.ObjectHandleState.NAME_NEEDED:
                        break;
                    case GameObject.ObjectHandleState.DISPLAYING:
                        obj.UpdateTextCoordsV();
                        break;
                }
            }
            else if (obj.ObjectHandle != GameObject.ObjectHandleState.NONE &&
                     obj.ObjectHandle != GameObject.ObjectHandleState.CLOSING)
            {
                obj.ObjectHandle = GameObject.ObjectHandleState.CLOSING;
                obj.UpdateTextCoordsV();
            }
        }

        private bool ProcessFoliage(GameObject obj, ref StaticTiles itemData)
        {
            if (!itemData.IsFoliage || itemData.IsMultiMovable || World.Season >= Season.Winter && !(obj is Multi))
            {
                return true;
            }

            if ((ProfileManager.CurrentProfile.TreeToStumps && !(obj is Multi)) ||
                (ProfileManager.CurrentProfile.HideVegetation && (obj is Multi mm && mm.IsVegetation || obj is Static st && st.IsVegetation)))
            {
                return false;
            }

            if (_alphaChanged)
            {
                bool hide = false;

                // If the player is standing behind the foliage, make it transparent
                if ((World.Player.X <= obj.X && World.Player.Y <= obj.Y) ||
                    (World.Player.Y <= obj.Y && World.Player.X <= (obj.X + 1)) ||
                    (World.Player.X <= obj.X && World.Player.Y <= (obj.Y + 1)))
                {
                    ArtTexture texture = ArtLoader.Instance.GetTexture(obj.Graphic);

                    if (texture != null)
                    {
                        _rectangleObj.X = obj.RealScreenPosition.X - (texture.Width >> 1) + texture.ImageRectangle.X;
                        _rectangleObj.Y = obj.RealScreenPosition.Y - texture.Height + texture.ImageRectangle.Y;
                        _rectangleObj.Width = texture.ImageRectangle.Width;
                        _rectangleObj.Height = texture.ImageRectangle.Height;

                        hide = Exstentions.InRect(ref _rectangleObj, ref _foliageHitBox);
                    }
                }

                if (hide)
                {
                    if (obj.AlphaHue != Constants.FOLIAGE_ALPHA)
                    {
                        obj.ProcessAlpha(Constants.FOLIAGE_ALPHA);
                    }
                }
                else if (obj.AlphaHue != 0xFF)
                {
                    obj.ProcessAlpha(0xFF);
                }
            }

            return true;
        }


        private bool ProcessAlpha(GameObject obj, ref StaticTiles itemData)
        {
            if (obj.Z >= _maxZ)
            {
                if (_alphaChanged)
                {
                    obj.ProcessAlpha(0);
                }

                return obj.AlphaHue != 0;
            }
            else if (_noDrawRoofs && itemData.IsRoof)
            {
                if (_alphaChanged)
                {
                    obj.ProcessAlpha(0);
                }

                return obj.AlphaHue != 0;
            }
            else if (itemData.IsTranslucent && obj.AlphaHue != 178)
            {
                if (_alphaChanged)
                {
                    obj.ProcessAlpha(178);
                }

                return true;
            }
            else if (!itemData.IsFoliage && obj.AlphaHue != 0xFF)
            {
                if (_alphaChanged)
                {
                    obj.ProcessAlpha(0xFF);
                }

                return true;
            }

            return true;
        }

        private bool InViewport(GameObject obj)
        {
            if (UpdateDrawPosition || obj.IsPositionChanged)
            {
                obj.UpdateRealScreenPosition(_offset.X, _offset.Y);
            }

            int drawX = obj.RealScreenPosition.X;
            int drawY = obj.RealScreenPosition.Y;

            if (drawX < _minPixel.X || drawX > _maxPixel.X)
            {
                return false;
            }

            if (drawY < _minPixel.Y || drawY > _maxPixel.Y)
            {
                return false;
            }

            return true;
        }

        private void CalculateBoundingBox(GameObject obj, ushort graphic, byte height)
        {
            ArtTexture texture = ArtLoader.Instance.GetTexture(graphic);

            int halfWidth = ((texture.Width / 2) - 22) / 44;

            obj.MinX = obj.X - halfWidth;
            obj.MinY = obj.Y - halfWidth;
            obj.MinZ = obj.Z;

            obj.MaxX = obj.X + halfWidth;
            obj.MaxY = obj.Y + halfWidth;
            obj.MaxZ = (sbyte)(obj.Z + height);
        }

        private void AddTileToRenderList(GameObject obj, int worldX, int worldY, bool useObjectHandles)
        {
            TileDataLoader loader = TileDataLoader.Instance;

            for (; obj != null; obj = obj.TNext)
            {
                if (!obj.AllowedToDraw)
                {
                    continue;
                }

                if (UpdateDrawPosition || obj.IsPositionChanged)
                {
                    obj.UpdateRealScreenPosition(_offset.X, _offset.Y);
                }

                if (!InViewport(obj))
                {
                    continue;
                }

                ref StaticTiles itemData = ref _empty;

                ushort graphic = obj.Graphic;

                switch (obj)
                {
                    case Mobile _:
                        if (obj.Z >= _maxZ)
                        {
                            continue;
                        }

                        CalculateBoundingBox(obj, graphic, Constants.DEFAULT_CHARACTER_HEIGHT);

                        if (_alphaChanged && obj.AlphaHue != 0xFF)
                        {
                            obj.ProcessAlpha(0xFF);
                        }

                        UpdateObjectHandles(obj, useObjectHandles);
                        break;

                    case Land _:
                        break;

                    case Item it:
                        if (it.IsMulti)
                        {
                            graphic = it.MultiGraphic;
                        }

                        itemData = ref loader.StaticData[graphic];

                        if (itemData.IsInternal)
                        {
                            continue;
                        }

                        if (it.IsCorpse)
                        {
                            UpdateObjectHandles(obj, useObjectHandles);
                        }

                        if (!it.IsLocked || (it.IsLocked && itemData.IsContainer))
                        {
                            UpdateObjectHandles(obj, useObjectHandles);
                        }

                        if (!ProcessFoliage(obj, ref itemData))
                        {
                            continue;
                        }

                        if (!ProcessAlpha(obj, ref itemData))
                        {
                            continue;
                        }

                        CalculateBoundingBox(obj, graphic, itemData.Height);

                        break;

                    default:
                        itemData = ref loader.StaticData[graphic];

                        if (itemData.IsInternal)
                        {
                            continue;
                        }

                        if (!ProcessFoliage(obj, ref itemData))
                        {
                            continue;
                        }

                        if (!ProcessAlpha(obj, ref itemData))
                        {
                            continue;
                        }

                        CalculateBoundingBox(obj, graphic, itemData.Height);

                        break;
                }

                if (GameObject.SomePositionChanged)
                {
                    if (_renderListCount >= _renderList.Length)
                    {
                        int newsize = _renderList.Length + 1000;
                        Array.Resize(ref _renderList, newsize);
                    }

                    _renderList[_renderListCount++] = obj;
                }
            }

            return;
        }

        private int _sortDepth;

        public class DrawDepthComparer : IComparer<GameObject>
        {
            public int Compare(GameObject x, GameObject y)
            {
                return x.DrawDepth - y.DrawDepth;
            }
        }

        private void TopologicalVisit(GameObject obj)
        {
            if (obj.Visited)
            {
                return;
            }

            obj.Visited = true;

            for (int i = 0; i < obj.BehindCount; i++)
            {
                if (obj.Behind[i] == null)
                {
                    break;
                }

                TopologicalVisit(obj.Behind[i]);
                obj.Behind[i] = null;
            }

            obj.DrawDepth = _sortDepth++;
        }

        private void TopologicalSort()
        {
            GameObject a, b;

            DrawDepthComparer comparer = new DrawDepthComparer();

            // TODO: We absolutely should not be doing this before every frame. This should be done
            // each time a mobile is created or moves instead.
            for (int i = 0; i < _renderListCount; i++)
            {
                a = _renderList[i];
                a.BehindCount = 0;
                a.Visited = false;

                for (int j = 0; j < _renderListCount; j++)
                {
                    if (i == j)
                    {
                        continue;
                    }

                    b = _renderList[j];

                    if (b.MinX < a.MaxX && b.MinY < a.MaxY && b.MinZ < a.MaxZ)
                    {
                        if (a.BehindCount >= a.Behind.Length)
                        {
                            int newsize = a.Behind.Length * 2;
                            Array.Resize(ref a.Behind, newsize);
                        }

                        a.Behind[a.BehindCount++] = b;
                    }
                }
            }

            _sortDepth = 0;
            for (int i = 0; i < _renderListCount; i++)
            {
                TopologicalVisit(_renderList[i]);
            }

            Array.Sort(_renderList, 0, _renderListCount, comparer);
        }

        private void GetViewPort()
        {
            int oldDrawOffsetX = _offset.X;
            int oldDrawOffsetY = _offset.Y;
            Point old_scaled_offset = _last_scaled_offset;

            float zoom = Camera.Zoom;

            int winGameWidth = ProfileManager.CurrentProfile.GameWindowSize.X;
            int winGameHeight = ProfileManager.CurrentProfile.GameWindowSize.Y;
            int winGameCenterX = (winGameWidth >> 1);
            int winGameCenterY = (winGameHeight >> 1) + (World.Player.Z << 2);
            winGameCenterX -= (int) World.Player.Offset.X;
            winGameCenterY -= (int) (World.Player.Offset.Y - World.Player.Offset.Z);

            int tileOffX = World.Player.X;
            int tileOffY = World.Player.Y;

            int winDrawOffsetX = (tileOffX - tileOffY) * 22 - winGameCenterX;
            int winDrawOffsetY = (tileOffX + tileOffY) * 22 - winGameCenterY;

            int winGameScaledOffsetX;
            int winGameScaledOffsetY;

            if (ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.EnableMousewheelScaleZoom)
            {
                winGameScaledOffsetX = (int)(winGameWidth * (1f - zoom));
                winGameScaledOffsetY = (int)(winGameHeight * (1f - zoom));
            }
            else
            {
                winGameScaledOffsetX = 0;
                winGameScaledOffsetY = 0;
            }

            int width = (int) ((winGameWidth / 44 + 1) * zoom);
            int height = (int) ((winGameHeight / 44 + 1) * zoom);


            if (width < height)
            {
                width = height;
            }
            else
            {
                height = width;
            }

            int realMinRangeX = tileOffX - width;

            if (realMinRangeX < 0)
            {
                realMinRangeX = 0;
            }

            int realMaxRangeX = tileOffX + width;

            int realMinRangeY = tileOffY - height;

            if (realMinRangeY < 0)
            {
                realMinRangeY = 0;
            }

            int realMaxRangeY = tileOffY + height;

            int drawOffset = (int) (44 / zoom);

            Point p = Point.Zero;
            p.X -= drawOffset;
            p.Y -= drawOffset;
            p = Camera.ScreenToWorld(p);
            int minPixelsX = p.X;
            int minPixelsY = p.Y;

            p.X = Camera.Bounds.Width + drawOffset;
            p.Y = Camera.Bounds.Height + drawOffset;
            p = Camera.ScreenToWorld(p);
            int maxPixelsX = p.X;
            int maxPixelsY = p.Y;


            if (UpdateDrawPosition || oldDrawOffsetX != winDrawOffsetX || oldDrawOffsetY != winDrawOffsetY || old_scaled_offset.X != winGameScaledOffsetX || old_scaled_offset.Y != winGameScaledOffsetY)
            {
                UpdateDrawPosition = true;

                if (_lightRenderTarget == null || _lightRenderTarget.Width != winGameWidth || _lightRenderTarget.Height != winGameHeight)
                {
                    _lightRenderTarget?.Dispose();

                    PresentationParameters pp = Client.Game.GraphicsDevice.PresentationParameters;

                    _lightRenderTarget = new RenderTarget2D
                    (
                        Client.Game.GraphicsDevice,
                        winGameWidth,
                        winGameHeight,
                        false,
                        pp.BackBufferFormat,
                        pp.DepthStencilFormat,
                        pp.MultiSampleCount,
                        pp.RenderTargetUsage
                    );
                }
            }

            _minTile.X = realMinRangeX;
            _minTile.Y = realMinRangeY;
            _maxTile.X = realMaxRangeX;
            _maxTile.Y = realMaxRangeY;

            _minPixel.X = minPixelsX;
            _minPixel.Y = minPixelsY;
            _maxPixel.X = maxPixelsX;
            _maxPixel.Y = maxPixelsY;

            _offset.X = winDrawOffsetX;
            _offset.Y = winDrawOffsetY;

            _last_scaled_offset.X = winGameScaledOffsetX;
            _last_scaled_offset.Y = winGameScaledOffsetY;


            UpdateMaxDrawZ();
        }

        private struct TreeUnion
        {
            public TreeUnion(ushort start, ushort end)
            {
                Start = start;
                End = end;
            }

            public readonly ushort Start;
            public readonly ushort End;
        }
    }
}