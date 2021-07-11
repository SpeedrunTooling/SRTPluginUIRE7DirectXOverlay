using GameOverlay.Drawing;
using GameOverlay.Windows;
using SRTPluginBase;
using SRTPluginProviderRE7;
using SRTPluginProviderRE7.Structs;
using SRTPluginProviderRE7.Structs.GameStructs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace SRTPluginUIRE7DirectXOverlay
{
    public class SRTPluginUIRE7DirectXOverlay : PluginBase, IPluginUI
    {
        internal static PluginInfo _Info = new PluginInfo();
        public override IPluginInfo Info => _Info;
        public string RequiredProvider => "SRTPluginProviderRE7";
        private IPluginHostDelegates hostDelegates;
        private IGameMemoryRE7 gameMemory;

        // DirectX Overlay-specific.
        private OverlayWindow _window;
        private Graphics _graphics;
        private SharpDX.Direct2D1.WindowRenderTarget _device;

        private Font _consolasBold;

        private SolidBrush _black;
        private SolidBrush _white;
        private SolidBrush _grey;
        private SolidBrush _darkred;
        private SolidBrush _red;
        private SolidBrush _lightred;
        private SolidBrush _lightyellow;
        private SolidBrush _lightgreen;
        private SolidBrush _lawngreen;
        private SolidBrush _goldenrod;
        private SolidBrush _greydark;
        private SolidBrush _greydarker;
        private SolidBrush _darkgreen;
        private SolidBrush _darkyellow;

        //private IReadOnlyDictionary<ItemEnumeration, SharpDX.Mathematics.Interop.RawRectangleF> itemToImageTranslation;
        //private IReadOnlyDictionary<Weapon, SharpDX.Mathematics.Interop.RawRectangleF> weaponToImageTranslation;
        //private SharpDX.Direct2D1.Bitmap _invItemSheet1;
        //private SharpDX.Direct2D1.Bitmap _invItemSheet2;
        //private int INV_SLOT_WIDTH;
        //private int INV_SLOT_HEIGHT;
        public PluginConfiguration config;
        private Process GetProcess() => Process.GetProcessesByName("re7")?.FirstOrDefault();
        private Process gameProcess;
        private IntPtr gameWindowHandle;

        //STUFF
        SolidBrush HPBarColor;
        SolidBrush TextColor;

        private string PlayerName = "";

        [STAThread]
        public override int Startup(IPluginHostDelegates hostDelegates)
        {
            this.hostDelegates = hostDelegates;
            config = LoadConfiguration<PluginConfiguration>();

            gameProcess = GetProcess();
            if (gameProcess == default)
                return 1;
            gameWindowHandle = gameProcess.MainWindowHandle;

            DEVMODE devMode = default;
            devMode.dmSize = (short)Marshal.SizeOf<DEVMODE>();
            PInvoke.EnumDisplaySettings(null, -1, ref devMode);

            // Create and initialize the overlay window.
            _window = new OverlayWindow(0, 0, devMode.dmPelsWidth, devMode.dmPelsHeight);
            _window?.Create();

            // Create and initialize the graphics object.
            _graphics = new Graphics()
            {
                MeasureFPS = false,
                PerPrimitiveAntiAliasing = false,
                TextAntiAliasing = true,
                UseMultiThreadedFactories = false,
                VSync = false,
                Width = _window.Width,
                Height = _window.Height,
                WindowHandle = _window.Handle
            };
            _graphics?.Setup();

            // Get a refernence to the underlying RenderTarget from SharpDX. This'll be used to draw portions of images.
            _device = (SharpDX.Direct2D1.WindowRenderTarget)typeof(Graphics).GetField("_device", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(_graphics);

            _consolasBold = _graphics?.CreateFont("Consolas", 12, true);

            _black = _graphics?.CreateSolidBrush(0, 0, 0);
            _white = _graphics?.CreateSolidBrush(255, 255, 255);
            _grey = _graphics?.CreateSolidBrush(128, 128, 128);
            _greydark = _graphics?.CreateSolidBrush(64, 64, 64);
            _greydarker = _graphics?.CreateSolidBrush(24, 24, 24, 100);
            _darkred = _graphics?.CreateSolidBrush(153, 0, 0, 100);
            _darkgreen = _graphics?.CreateSolidBrush(0, 102, 0, 100);
            _darkyellow = _graphics?.CreateSolidBrush(218, 165, 32, 100);
            _red = _graphics?.CreateSolidBrush(255, 0, 0);
            _lightred = _graphics?.CreateSolidBrush(255, 183, 183);
            _lightyellow = _graphics?.CreateSolidBrush(255, 255, 0);
            _lightgreen = _graphics?.CreateSolidBrush(0, 255, 0);
            _lawngreen = _graphics?.CreateSolidBrush(124, 252, 0);
            _goldenrod = _graphics?.CreateSolidBrush(218, 165, 32);
            HPBarColor = _grey;
            TextColor = _white;

            //if (!config.NoInventory)
            //{
            //    INV_SLOT_WIDTH = 112;
            //    INV_SLOT_HEIGHT = 112;
            //
            //    _invItemSheet1 = ImageLoader.LoadBitmap(_device, Properties.Resources.ui0100_iam_texout);
            //    _invItemSheet2 = ImageLoader.LoadBitmap(_device, Properties.Resources.ui0100_wp_iam_texout);
            //    GenerateClipping();
            //}

            return 0;
        }

        public override int Shutdown()
        {
            SaveConfiguration(config);

            //weaponToImageTranslation = null;
            //itemToImageTranslation = null;
            //
            //_invItemSheet2?.Dispose();
            //_invItemSheet1?.Dispose();

            _black?.Dispose();
            _white?.Dispose();
            _grey?.Dispose();
            _greydark?.Dispose();
            _greydarker?.Dispose();
            _darkred?.Dispose();
            _darkgreen?.Dispose();
            _darkyellow?.Dispose();
            _red?.Dispose();
            _lightred?.Dispose();
            _lightyellow?.Dispose();
            _lightgreen?.Dispose();
            _lawngreen?.Dispose();
            _goldenrod?.Dispose();

            _consolasBold?.Dispose();

            _device = null; // We didn't create this object so we probably shouldn't be the one to dispose of it. Just set the variable to null so the reference isn't held.
            _graphics?.Dispose(); // This should technically be the one to dispose of the _device object since it was pulled from this instance.
            _graphics = null;
            _window?.Dispose();
            _window = null;

            gameProcess?.Dispose();
            gameProcess = null;

            return 0;
        }

        public int ReceiveData(object gameMemory)
        {
            this.gameMemory = (IGameMemoryRE7)gameMemory;
            _window?.PlaceAbove(gameWindowHandle);
            _window?.FitTo(gameWindowHandle, true);

            try
            {
                _graphics?.BeginScene();
                _graphics?.ClearScene();

                if (config.ScalingFactor != 1f)
                    _device.Transform = new SharpDX.Mathematics.Interop.RawMatrix3x2(config.ScalingFactor, 0f, 0f, config.ScalingFactor, 0f, 0f);
                else
                    _device.Transform = new SharpDX.Mathematics.Interop.RawMatrix3x2(1f, 0f, 0f, 1f, 0f, 0f);

                DrawOverlay();
            }
            catch (Exception ex)
            {
                hostDelegates.ExceptionMessage.Invoke(ex);
            }
            finally
            {
                _graphics?.EndScene();
            }

            return 0;
        }

        private void SetColors()
        {
            if (gameMemory.Player.HealthState == PlayerStatus.Fine) // Fine
            {
                HPBarColor = _darkgreen;
                TextColor = _lightgreen;
                return;
            }
            else if (gameMemory.Player.HealthState == PlayerStatus.Caution) // Caution (Yellow)
            {
                HPBarColor = _darkyellow;
                TextColor = _lightyellow; 
                return;
            }
            else if (gameMemory.Player.HealthState == PlayerStatus.Danger) // Danger (Red)
            {
                HPBarColor = _darkred; 
                 TextColor = _lightred;
                return;
            }
            else
            {
                HPBarColor = _greydarker;
                TextColor = _white;
                return;
            }
        }

        private void DrawOverlay()
        {
            float baseXOffset = config.PositionX;
            float baseYOffset = config.PositionY;

            // Player HP
            float statsXOffset = baseXOffset + 5f;
            float statsYOffset = baseYOffset + 0f;

            PlayerName = "Player: ";
            SetColors();

            if (config.ShowHPBars)
            {
                DrawHealthBar(ref statsXOffset, ref statsYOffset, gameMemory.Player.CurrentHP, gameMemory.Player.MaxHP, gameMemory.Player.Percentage);
            }
            else
            {
                string perc = float.IsNaN(gameMemory.Player.Percentage) ? "0%" : string.Format("{0:P1}", gameMemory.Player.Percentage);
                _graphics?.DrawText(_consolasBold, 20f, _red, statsXOffset, statsYOffset += 24, "Player HP");
                _graphics?.DrawText(_consolasBold, 20f, TextColor, statsXOffset + 10f, statsYOffset += 24, string.Format("{0}{1} / {2} {3:P1}", PlayerName, gameMemory.Player.CurrentHP, gameMemory.Player.MaxHP, perc));
            }

            float textOffsetX = 0f;
            //if (config.Debug)
            //{
            //}

            if (config.ShowDifficultyAdjustment)
            {
                _graphics?.DrawText(_consolasBold, 20f, _grey, config.PositionX + 15f, statsYOffset += 24, config.ScoreString);
                textOffsetX = config.PositionX + 15f + GetStringSize(config.ScoreString) + 10f;
                _graphics?.DrawText(_consolasBold, 20f, _lawngreen, textOffsetX, statsYOffset, Math.Floor(gameMemory.RankScore).ToString()); //110f
                textOffsetX += GetStringSize(gameMemory.RankScore.ToString()) + 10f;
                _graphics?.DrawText(_consolasBold, 20f, _grey, textOffsetX, statsYOffset, config.RankString); //178f
                textOffsetX += GetStringSize(config.RankString) + 10f;
                _graphics?.DrawText(_consolasBold, 20f, _lawngreen, textOffsetX, statsYOffset, gameMemory.Rank.ToString()); //261f
                textOffsetX += GetStringSize(gameMemory.Rank.ToString()) + 10f;
            }

            // Enemy HP
            var xOffset = config.EnemyHPPositionX == -1 ? statsXOffset : config.EnemyHPPositionX;
            var yOffset = config.EnemyHPPositionY == -1 ? statsYOffset : config.EnemyHPPositionY;
            _graphics?.DrawText(_consolasBold, 20f, _red, xOffset, yOffset += 24f, config.EnemyString);
            foreach (EnemyHP enemyHP in gameMemory.EnemyHealth.Where(a => a.IsAlive).OrderBy(a => a.Percentage).ThenByDescending(a => a.CurrentHP))
                if (config.ShowHPBars)
                {
                    DrawProgressBar(ref xOffset, ref yOffset, enemyHP.CurrentHP, enemyHP.MaximumHP, enemyHP.Percentage);
                }
                else
                {
                    _graphics.DrawText(_consolasBold, 20f, _white, xOffset + 10f, yOffset += 28f, string.Format("{0} / {1} {2:P1}", enemyHP.CurrentHP, enemyHP.MaximumHP, enemyHP.Percentage));
                }

            // Inventory
            //if (!config.NoInventory)
            //{
            //    float invXOffset = config.InventoryPositionX == -1 ? statsXOffset : config.InventoryPositionX;
            //    float invYOffset = config.InventoryPositionY == -1 ? yOffset + 24f : config.InventoryPositionY; // Using yOffset instead of statsYOffset to offset everything relative to the other stats Y position.
            //    if (itemToImageTranslation != null && weaponToImageTranslation != null)
            //    {
            //        for (int i = 0; i < gameMemory.PlayerInventory.Length; ++i)
            //        {
            //            // Only do logic for non-blank and non-broken items.
            //            if (gameMemory.PlayerInventory[i].SlotPosition >= 0 && gameMemory.PlayerInventory[i].SlotPosition <= 19 && !gameMemory.PlayerInventory[i].IsEmptySlot)
            //            {
            //                int slotColumn = gameMemory.PlayerInventory[i].SlotPosition % 4;
            //                int slotRow = gameMemory.PlayerInventory[i].SlotPosition / 4;
            //                float imageX = invXOffset + (slotColumn * INV_SLOT_WIDTH);
            //                float imageY = invYOffset + (slotRow * INV_SLOT_HEIGHT);
            //                //float textX = imageX + (INV_SLOT_WIDTH * options.ScalingFactor);
            //                //float textY = imageY + (INV_SLOT_HEIGHT * options.ScalingFactor);
            //                float textX = imageX + (INV_SLOT_WIDTH * 0.96f);
            //                float textY = imageY + (INV_SLOT_HEIGHT * 0.68f);
            //                SolidBrush textBrush = _white;
            //                if (gameMemory.PlayerInventory[i].Quantity == 0)
            //                    textBrush = _darkred;
            //
            //                Weapon weapon = new Weapon();
            //                if (gameMemory.PlayerInventory[i].IsWeapon)
            //                {
            //                    weapon.WeaponID = gameMemory.PlayerInventory[i].WeaponID;
            //                    weapon.Attachments = gameMemory.PlayerInventory[i].Attachments;
            //                }
            //
            //                // Get the region of the inventory sheet where this item's icon resides.
            //                SharpDX.Mathematics.Interop.RawRectangleF imageRegion;
            //                if (gameMemory.PlayerInventory[i].IsItem && itemToImageTranslation.ContainsKey(gameMemory.PlayerInventory[i].ItemID))
            //                    imageRegion = itemToImageTranslation[gameMemory.PlayerInventory[i].ItemID];
            //
            //                //FAILING TO RETURN IT CONTAINS KEY?
            //                else if (gameMemory.PlayerInventory[i].IsWeapon && weaponToImageTranslation.ContainsKey(weapon))
            //                    imageRegion = weaponToImageTranslation[weapon];
            //                else 
            //                    imageRegion = new SharpDX.Mathematics.Interop.RawRectangleF(0, 0, INV_SLOT_WIDTH, INV_SLOT_HEIGHT);
            //
            //                imageRegion.Right += imageRegion.Left;
            //                imageRegion.Bottom += imageRegion.Top;
            //
            //                // Get the region to draw our item icon to.
            //                SharpDX.Mathematics.Interop.RawRectangleF drawRegion;
            //                if (imageRegion.Right - imageRegion.Left == INV_SLOT_WIDTH * 2f)
            //                {
            //                    // Double-slot item, adjust the draw region width and text's X coordinate.
            //                    textX += INV_SLOT_WIDTH;
            //                    drawRegion = new SharpDX.Mathematics.Interop.RawRectangleF(imageX, imageY, INV_SLOT_WIDTH * 2, INV_SLOT_HEIGHT);
            //                }
            //                else // Normal-sized icon.
            //                    drawRegion = new SharpDX.Mathematics.Interop.RawRectangleF(imageX, imageY, INV_SLOT_WIDTH, INV_SLOT_HEIGHT);
            //                drawRegion.Right += drawRegion.Left;
            //                drawRegion.Bottom += drawRegion.Top;
            //
            //                if (gameMemory.PlayerInventory[i].IsItem)
            //                    _device?.DrawBitmap(_invItemSheet1, drawRegion, 1f, SharpDX.Direct2D1.BitmapInterpolationMode.Linear, imageRegion);
            //                else if (gameMemory.PlayerInventory[i].IsWeapon)
            //                    _device?.DrawBitmap(_invItemSheet2, drawRegion, 1f, SharpDX.Direct2D1.BitmapInterpolationMode.Linear, imageRegion);
            //                
            //
            //                // Draw the quantity text.
            //                _graphics?.DrawText(_consolasBold, 22f, textBrush, textX - GetStringSize(gameMemory.PlayerInventory[i].Quantity.ToString(), 22f), textY, (gameMemory.PlayerInventory[i].Quantity != -1) ? gameMemory.PlayerInventory[i].Quantity.ToString() : "∞");
            //            }
            //        }
            //    }
            //}
        }

        private float GetStringSize(string str, float size = 20f)
        {
            return (float)_graphics?.MeasureString(_consolasBold, size, str).X;
        }

        private void DrawProgressBar(ref float xOffset, ref float yOffset, float chealth, float mhealth, float percentage = 1f)
        {
            string perc = float.IsNaN(percentage) ? "0%" : string.Format("{0:P1}", percentage);
            float endOfBar = config.PositionX + 342f - GetStringSize(perc);
            _graphics.DrawRectangle(_greydark, xOffset, yOffset += 28f, xOffset + 342f, yOffset + 22f, 4f);
            _graphics.FillRectangle(_greydarker, xOffset + 1f, yOffset + 1f, xOffset + 340f, yOffset + 20f);
            _graphics.FillRectangle(_darkred, xOffset + 1f, yOffset + 1f, xOffset + (340f * percentage), yOffset + 20f);
            _graphics.DrawText(_consolasBold, 20f, _white, xOffset + 10f, yOffset - 2f, string.Format("{0} / {1}", chealth, mhealth));
            _graphics.DrawText(_consolasBold, 20f, _white, endOfBar, yOffset - 2f, perc);
        }

        private void DrawHealthBar(ref float xOffset, ref float yOffset, float chealth, float mhealth, float percentage = 1f)
        {
            string perc = float.IsNaN(percentage) ? "0%" : string.Format("{0:P1}", percentage);
            float endOfBar = config.PositionX + 342f - GetStringSize(perc);
            _graphics.DrawRectangle(_greydark, xOffset, yOffset += 28f, xOffset + 342f, yOffset + 22f, 4f);
            _graphics.FillRectangle(_greydarker, xOffset + 1f, yOffset + 1f, xOffset + 340f, yOffset + 20f);
            _graphics.FillRectangle(HPBarColor, xOffset + 1f, yOffset + 1f, xOffset + (340f * percentage), yOffset + 20f);
            _graphics.DrawText(_consolasBold, 20f, TextColor, xOffset + 10f, yOffset - 2f, string.Format("{0}{1} / {2}", PlayerName, chealth, mhealth));
            _graphics.DrawText(_consolasBold, 20f, TextColor, endOfBar, yOffset - 2f, perc);
        }

        //public void GenerateClipping()
        //{
        //    int itemColumnInc = -1;
        //    int itemRowInc = -1;
        //    itemToImageTranslation = new Dictionary<ItemEnumeration, SharpDX.Mathematics.Interop.RawRectangleF>()
        //    {
        //        { ItemEnumeration.None, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * 0, INV_SLOT_HEIGHT * 8, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //
        //        // Row 0.
        //        { ItemEnumeration.First_Aid_Spray, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * (itemColumnInc = 0), INV_SLOT_HEIGHT * ++itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //        { ItemEnumeration.Green_Herb, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //        { ItemEnumeration.Red_Herb, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //        { ItemEnumeration.Mixed_Herb_GG, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //        { ItemEnumeration.Mixed_Herb_GR, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //        { ItemEnumeration.Mixed_Herb_GGG, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //        { ItemEnumeration.Green_Herb2, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //        { ItemEnumeration.Red_Herb2, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //
        //        // Row 1.
        //        { ItemEnumeration.Handgun_Ammo, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * (itemColumnInc = 0), INV_SLOT_HEIGHT * ++itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //        { ItemEnumeration.Shotgun_Shells, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //        { ItemEnumeration.Assault_Rifle_Ammo, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //        { ItemEnumeration.MAG_Ammo, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //        { ItemEnumeration.Acid_Rounds, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //        { ItemEnumeration.Flame_Rounds, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //        { ItemEnumeration.Explosive_Rounds, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //        { ItemEnumeration.Mine_Rounds, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //        { ItemEnumeration.Gunpowder, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //        { ItemEnumeration.HighGrade_Gunpowder, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //        { ItemEnumeration.Explosive_A, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //        { ItemEnumeration.Explosive_B, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //
        //        // Row 2.
        //        { ItemEnumeration.Moderator_Handgun, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * (itemColumnInc = 0), INV_SLOT_HEIGHT * ++itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //        { ItemEnumeration.Dot_Sight_Handgun, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //        { ItemEnumeration.Extended_Magazine_Handgun, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //        { ItemEnumeration.SemiAuto_Barrel_Shotgun, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //        { ItemEnumeration.Tactical_Stock_Shotgun, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //        { ItemEnumeration.Shell_Holder_Shotgun, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //        { ItemEnumeration.Scope_Assault_Rifle, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //        { ItemEnumeration.Dual_Magazine_Assault_Rifle, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //        { ItemEnumeration.Tactical_Grip_Assault_Rifle, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //        { ItemEnumeration.Extended_Barrel_MAG, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //        { ItemEnumeration.Supply_Crate_Acid_Rounds, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //        { ItemEnumeration.Supply_Crate_Extended_Barrel_MAG, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //        { ItemEnumeration.Supply_Crate_Extended_Magazine_Handgun, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //        { ItemEnumeration.Supply_Crate_Flame_Rounds, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //        { ItemEnumeration.Supply_Crate_Moderator_Handgun, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //        { ItemEnumeration.Supply_Crate_Shotgun_Shells, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //
        //        //Row 3.
        //        { ItemEnumeration.Battery, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * (itemColumnInc = 0), INV_SLOT_HEIGHT * ++itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //        { ItemEnumeration.Safety_Deposit_Key, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //        { ItemEnumeration.Detonator_No_Battery, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //        { ItemEnumeration.Brads_ID_Card, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //        { ItemEnumeration.Detonator, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //        { ItemEnumeration.Detonator2, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //        { ItemEnumeration.Lock_Pick, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * (itemColumnInc = 8), INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //        { ItemEnumeration.Bolt_Cutters, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //
        //        // Row 4.
        //        { ItemEnumeration.Fire_Hose, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * (itemColumnInc = 0), INV_SLOT_HEIGHT * ++itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //        { ItemEnumeration.Fire_Hose2, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //        { ItemEnumeration.Kendos_Gate_Key, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //        { ItemEnumeration.Battery_Pack, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH * 2, INV_SLOT_HEIGHT) },
        //        { ItemEnumeration.Case_Lock_Pick, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * (itemColumnInc = 4), INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //        { ItemEnumeration.Green_Jewel, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //        { ItemEnumeration.Blue_Jewel, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //        { ItemEnumeration.Red_Jewel, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //        { ItemEnumeration.Fancy_Box_Green_Jewel, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //        { ItemEnumeration.Fancy_Box_Blue_Jewel, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //        { ItemEnumeration.Fancy_Box_Red_Jewel, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //
        //        // Row 5.
        //        { ItemEnumeration.Hospital_ID_Card, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * (itemColumnInc = 0), INV_SLOT_HEIGHT * ++itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //        { ItemEnumeration.Audiocassette_Tape, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //        { ItemEnumeration.Vaccine_Sample, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //        { ItemEnumeration.Fuse1, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //        { ItemEnumeration.Fuse2, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //        { ItemEnumeration.Fuse3, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //        { ItemEnumeration.Audiocassette_Tape2, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //        { ItemEnumeration.Tape_Player, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //        { ItemEnumeration.Tape_Player_Tape_Inserted, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //        { ItemEnumeration.Locker_Room_Key, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //
        //        // Row 6.
        //        { ItemEnumeration.Override_Key, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * (itemColumnInc = 0), INV_SLOT_HEIGHT * ++itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //        { ItemEnumeration.Vaccine, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //        { ItemEnumeration.Culture_Sample, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //        { ItemEnumeration.Liquidfilled_Test_Tube, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //        { ItemEnumeration.Vaccine_Base, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //
        //        // Row 7.
        //        { ItemEnumeration.Hip_Pouch, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * (itemColumnInc = 1), INV_SLOT_HEIGHT * ++itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //        { ItemEnumeration.Iron_Defense_Coin, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * (itemColumnInc = 5), INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //        { ItemEnumeration.Assault_Coin, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //        { ItemEnumeration.Recovery_Coin, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //        { ItemEnumeration.Crafting_Companion, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //        { ItemEnumeration.STARS_Field_Combat_Manual, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++itemColumnInc, INV_SLOT_HEIGHT * itemRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //
        //    };
        //
        //    int weaponColumnInc = -1;
        //    int weaponRowInc = -1;
        //    weaponToImageTranslation = new Dictionary<Weapon, SharpDX.Mathematics.Interop.RawRectangleF>()
        //    {
        //        { new Weapon() { WeaponID = WeaponEnumeration.None, Attachments = AttachmentsFlag.None }, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * 0, INV_SLOT_HEIGHT * 5, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //
        //        // Row 10.
        //        { new Weapon() { WeaponID = WeaponEnumeration.G19_Handgun, Attachments = AttachmentsFlag.None }, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * (weaponColumnInc = 0), INV_SLOT_HEIGHT * ++weaponRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //        { new Weapon() { WeaponID = WeaponEnumeration.G19_Handgun, Attachments = AttachmentsFlag.First }, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++weaponColumnInc, INV_SLOT_HEIGHT * weaponRowInc, INV_SLOT_WIDTH * 2, INV_SLOT_HEIGHT) },
        //        { new Weapon() { WeaponID = WeaponEnumeration.G19_Handgun, Attachments = AttachmentsFlag.First | AttachmentsFlag.Third }, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * (weaponColumnInc = 3), INV_SLOT_HEIGHT * weaponRowInc, INV_SLOT_WIDTH * 2, INV_SLOT_HEIGHT) },
        //        { new Weapon() { WeaponID = WeaponEnumeration.G19_Handgun, Attachments = AttachmentsFlag.First | AttachmentsFlag.Second | AttachmentsFlag.Third }, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * (weaponColumnInc = 5), INV_SLOT_HEIGHT * weaponRowInc, INV_SLOT_WIDTH * 2, INV_SLOT_HEIGHT) },
        //        { new Weapon() { WeaponID = WeaponEnumeration.G19_Handgun, Attachments = AttachmentsFlag.Third }, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * (weaponColumnInc = 7), INV_SLOT_HEIGHT * weaponRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //        { new Weapon() { WeaponID = WeaponEnumeration.G19_Handgun, Attachments = AttachmentsFlag.Second | AttachmentsFlag.Third }, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++weaponColumnInc, INV_SLOT_HEIGHT * weaponRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //        { new Weapon() { WeaponID = WeaponEnumeration.G19_Handgun, Attachments = AttachmentsFlag.Second }, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++weaponColumnInc, INV_SLOT_HEIGHT * weaponRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //        { new Weapon() { WeaponID = WeaponEnumeration.G19_Handgun, Attachments = AttachmentsFlag.First | AttachmentsFlag.Second }, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * (weaponColumnInc = 10), INV_SLOT_HEIGHT * weaponRowInc, INV_SLOT_WIDTH * 2, INV_SLOT_HEIGHT) },
        //        { new Weapon() { WeaponID = WeaponEnumeration.Samurai_Edge, Attachments = AttachmentsFlag.None }, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * (weaponColumnInc = 12), INV_SLOT_HEIGHT * weaponRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //        { new Weapon() { WeaponID = WeaponEnumeration.G18_Handgun, Attachments = AttachmentsFlag.None }, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * (weaponColumnInc = 16), INV_SLOT_HEIGHT * weaponRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //        { new Weapon() { WeaponID = WeaponEnumeration.G18_Burst_Handgun, Attachments = AttachmentsFlag.None }, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++weaponColumnInc, INV_SLOT_HEIGHT * weaponRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //
        //        // Row 11.
        //        { new Weapon() { WeaponID = WeaponEnumeration.Shotgun, Attachments = AttachmentsFlag.None }, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * (weaponColumnInc = 0), INV_SLOT_HEIGHT * ++weaponRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //        { new Weapon() { WeaponID = WeaponEnumeration.Shotgun, Attachments = AttachmentsFlag.First }, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++weaponColumnInc, INV_SLOT_HEIGHT * weaponRowInc, INV_SLOT_WIDTH * 2, INV_SLOT_HEIGHT) },
        //        { new Weapon() { WeaponID = WeaponEnumeration.Shotgun, Attachments = AttachmentsFlag.First | AttachmentsFlag.Third }, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * (weaponColumnInc = 3), INV_SLOT_HEIGHT * weaponRowInc, INV_SLOT_WIDTH * 2, INV_SLOT_HEIGHT) },
        //        { new Weapon() { WeaponID = WeaponEnumeration.Shotgun, Attachments = AttachmentsFlag.First | AttachmentsFlag.Second | AttachmentsFlag.Third }, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * (weaponColumnInc = 5), INV_SLOT_HEIGHT * weaponRowInc, INV_SLOT_WIDTH * 2, INV_SLOT_HEIGHT) },
        //        { new Weapon() { WeaponID = WeaponEnumeration.Shotgun, Attachments = AttachmentsFlag.Third }, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * (weaponColumnInc = 7), INV_SLOT_HEIGHT * weaponRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //        { new Weapon() { WeaponID = WeaponEnumeration.Shotgun, Attachments = AttachmentsFlag.Second | AttachmentsFlag.Third }, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++weaponColumnInc, INV_SLOT_HEIGHT * weaponRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //        { new Weapon() { WeaponID = WeaponEnumeration.Shotgun, Attachments = AttachmentsFlag.Second }, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++weaponColumnInc, INV_SLOT_HEIGHT * weaponRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //        { new Weapon() { WeaponID = WeaponEnumeration.Shotgun, Attachments = AttachmentsFlag.First | AttachmentsFlag.Second }, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * (weaponColumnInc = 10), INV_SLOT_HEIGHT * weaponRowInc, INV_SLOT_WIDTH * 2, INV_SLOT_HEIGHT) },
        //        { new Weapon() { WeaponID = WeaponEnumeration.Lightning_Hawk, Attachments = AttachmentsFlag.None }, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * (weaponColumnInc = 12), INV_SLOT_HEIGHT * weaponRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //        { new Weapon() { WeaponID = WeaponEnumeration.Lightning_Hawk, Attachments = AttachmentsFlag.Second }, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++weaponColumnInc, INV_SLOT_HEIGHT * weaponRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //
        //        // Row 12.
        //        { new Weapon() { WeaponID = WeaponEnumeration.CQBR_Assault_Rifle, Attachments = AttachmentsFlag.None }, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * (weaponColumnInc = 0), INV_SLOT_HEIGHT * ++weaponRowInc, INV_SLOT_WIDTH * 2, INV_SLOT_HEIGHT) },
        //        { new Weapon() { WeaponID = WeaponEnumeration.CQBR_Assault_Rifle, Attachments = AttachmentsFlag.First }, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * (weaponColumnInc = 2), INV_SLOT_HEIGHT * weaponRowInc, INV_SLOT_WIDTH * 2, INV_SLOT_HEIGHT) },
        //        { new Weapon() { WeaponID = WeaponEnumeration.CQBR_Assault_Rifle, Attachments = AttachmentsFlag.First | AttachmentsFlag.Third }, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * (weaponColumnInc = 4), INV_SLOT_HEIGHT * weaponRowInc, INV_SLOT_WIDTH * 2, INV_SLOT_HEIGHT) },
        //        { new Weapon() { WeaponID = WeaponEnumeration.CQBR_Assault_Rifle, Attachments = AttachmentsFlag.First | AttachmentsFlag.Second | AttachmentsFlag.Third }, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * (weaponColumnInc = 6), INV_SLOT_HEIGHT * weaponRowInc, INV_SLOT_WIDTH * 2, INV_SLOT_HEIGHT) },
        //        { new Weapon() { WeaponID = WeaponEnumeration.CQBR_Assault_Rifle, Attachments = AttachmentsFlag.Third }, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * (weaponColumnInc = 8), INV_SLOT_HEIGHT * weaponRowInc, INV_SLOT_WIDTH * 2, INV_SLOT_HEIGHT) },
        //        { new Weapon() { WeaponID = WeaponEnumeration.CQBR_Assault_Rifle, Attachments = AttachmentsFlag.Second | AttachmentsFlag.Third }, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * (weaponColumnInc = 10), INV_SLOT_HEIGHT * weaponRowInc, INV_SLOT_WIDTH * 2, INV_SLOT_HEIGHT) },
        //        { new Weapon() { WeaponID = WeaponEnumeration.CQBR_Assault_Rifle, Attachments = AttachmentsFlag.Second }, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * (weaponColumnInc = 12), INV_SLOT_HEIGHT * weaponRowInc, INV_SLOT_WIDTH * 2, INV_SLOT_HEIGHT) },
        //        { new Weapon() { WeaponID = WeaponEnumeration.CQBR_Assault_Rifle, Attachments = AttachmentsFlag.First | AttachmentsFlag.Second }, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * (weaponColumnInc = 14), INV_SLOT_HEIGHT * weaponRowInc, INV_SLOT_WIDTH * 2, INV_SLOT_HEIGHT) },
        //
        //
        //        // Row 13.
        //        { new Weapon() { WeaponID = WeaponEnumeration.Infinite_Rocket_Launcher, Attachments = AttachmentsFlag.None }, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * (weaponColumnInc = 6), INV_SLOT_HEIGHT * ++weaponRowInc, INV_SLOT_WIDTH * 2, INV_SLOT_HEIGHT) },
        //        { new Weapon() { WeaponID = WeaponEnumeration.Infinite_CQBR_Assault_Rifle, Attachments = AttachmentsFlag.None }, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * (weaponColumnInc = 8), INV_SLOT_HEIGHT * weaponRowInc, INV_SLOT_WIDTH * 2, INV_SLOT_HEIGHT) },
        //
        //        // Row 14.
        //        { new Weapon() { WeaponID = WeaponEnumeration.Combat_Knife_Carlos, Attachments = AttachmentsFlag.None }, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * (weaponColumnInc = 0), INV_SLOT_HEIGHT * ++weaponRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //        { new Weapon() { WeaponID = WeaponEnumeration.Survival_Knife_Jill, Attachments = AttachmentsFlag.None }, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++weaponColumnInc, INV_SLOT_HEIGHT * weaponRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //        { new Weapon() { WeaponID = WeaponEnumeration.Infinite_MUP_Handgun, Attachments = AttachmentsFlag.None }, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * (weaponColumnInc = 4), INV_SLOT_HEIGHT * weaponRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //        { new Weapon() { WeaponID = WeaponEnumeration.RAIDEN, Attachments = AttachmentsFlag.None }, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++weaponColumnInc, INV_SLOT_HEIGHT * weaponRowInc, INV_SLOT_WIDTH * 2, INV_SLOT_HEIGHT) },
        //        { new Weapon() { WeaponID = WeaponEnumeration.HOT_DOGGER, Attachments = AttachmentsFlag.None }, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * (weaponColumnInc = 7), INV_SLOT_HEIGHT * weaponRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //        { new Weapon() { WeaponID = WeaponEnumeration.Hand_Grenade, Attachments = AttachmentsFlag.None }, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * (weaponColumnInc = 9), INV_SLOT_HEIGHT * weaponRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //        { new Weapon() { WeaponID = WeaponEnumeration.Flash_Grenade, Attachments = AttachmentsFlag.None }, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++weaponColumnInc, INV_SLOT_HEIGHT * weaponRowInc, INV_SLOT_WIDTH, INV_SLOT_HEIGHT) },
        //        { new Weapon() { WeaponID = WeaponEnumeration.Grenade_Launcher, Attachments = AttachmentsFlag.None }, new SharpDX.Mathematics.Interop.RawRectangleF(INV_SLOT_WIDTH * ++weaponColumnInc, INV_SLOT_HEIGHT * weaponRowInc, INV_SLOT_WIDTH * 2, INV_SLOT_HEIGHT) },
        //
        //    };
        //}
    }
}
