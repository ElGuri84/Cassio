﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LeagueSharp;
using LeagueSharp.Common;
using DevCommom;
using SharpDX;

/*
 * #### DevKogMaw ####
 * 
 * InjectionDev GitHub: https://github.com/InjectionDev/LeagueSharp/
 * Script Based GitHub: https://github.com/fueledbyflux/LeagueSharp-Public/tree/master/EasyKogMaw/ - Credits to fueledbyflux
* /

/*
 * ##### DevKogMaw Mods #####
 * + Chase Enemy After Death
 * + R KillSteal
 * + Range based on Skill Level
 * + Assisted Ult
 * + Block Ult if will not hit

*/

namespace DevCassio
{
    class Program
    {
        public const string ChampionName = "KogMaw";

        public static Menu Config;
        public static Orbwalking.Orbwalker Orbwalker;
        public static List<Spell> SpellList = new List<Spell>();
        public static Obj_AI_Hero Player;
        public static Spell Q;
        public static Spell W;
        public static Spell E;
        public static Spell R;
        public static List<Obj_AI_Base> MinionList;
        public static DevCommom.SkinManager SkinManager;
        public static DevCommom.IgniteManager IgniteSpell;

        public static bool mustDebug = false;




        static void Main(string[] args)
        {
            LeagueSharp.Common.CustomEvents.Game.OnGameLoad += onGameLoad;
        }

        private static void OnTick(EventArgs args)
        {
            MinionList = MinionManager.GetMinions(ObjectManager.Player.Position, E.Range, MinionTypes.All);

            if (Config.Item("ComboActive").GetValue<KeyBind>().Active)
            {
                Combo();
            }
            if (Config.Item("HarassActive").GetValue<KeyBind>().Active)
            {
                Harass();
            }
            if (Config.Item("LaneClearActive").GetValue<KeyBind>().Active)
            {
                WaveClear();
            }
            if (Config.Item("FreezeActive").GetValue<KeyBind>().Active)
            {
                Freeze();
            }
            if (Config.Item("RKillSteal").GetValue<bool>())
            {
                KillSteal();
            }
            if (Config.Item("ChaseEnemyAfterDeath").GetValue<bool>())
            {
                ChaseEnemyAfterDeath();
            }
            

            UpdateSpellsRange();

            SkinManager.Update();
        }


        public static void Combo()
        {
            if (mustDebug)
                Game.PrintChat("Combo Start");

            var useQ = Config.Item("UseQCombo").GetValue<bool>();
            var useW = Config.Item("UseWCombo").GetValue<bool>();
            var useE = Config.Item("UseECombo").GetValue<bool>();
            var useR = Config.Item("UseRCombo").GetValue<bool>();
            var useIgnite = Config.Item("UseIgnite").GetValue<bool>();
            var packetCast = Config.Item("PacketCast").GetValue<bool>();
            var RMaxStacksCombo = Config.Item("RMaxStacksCombo").GetValue<Slider>().Value;

            var eTarget = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Magical);

            if (eTarget.IsValidTarget(Q.Range) && Q.IsReady() && useQ)
            {
                Q.CastIfHitchanceEquals(eTarget, eTarget.IsMoving ? HitChance.High : HitChance.Medium, packetCast);
                return;
            }

            if (eTarget.IsValidTarget(Player.AttackRange + W.Range) && W.IsReady() && useW)
            {
                W.Cast();
                return;
            }

            if (eTarget.IsValidTarget(E.Range) && E.IsReady() && useE)
            {
                E.CastIfHitchanceEquals(eTarget, eTarget.IsMoving ? HitChance.High : HitChance.Medium, packetCast);
                return;
            }

            if (eTarget.IsValidTarget(R.Range) && R.IsReady() && GetRStacks() < RMaxStacksCombo && useR)
            {
                R.CastIfHitchanceEquals(eTarget, eTarget.IsMoving ? HitChance.High : HitChance.Medium, packetCast);
                return;
            }

            if (IgniteSpell.CanKill(eTarget))
            {
                IgniteSpell.Cast(eTarget);
                Game.PrintChat(string.Format("Ignite Combo KS -> {0} ", eTarget.SkinName));
            }

        }


        public static void Harass()
        {
            if (mustDebug)
                Game.PrintChat("Harass Start");

            var useQ = Config.Item("UseQHarass").GetValue<bool>();
            var useW = Config.Item("UseWHarass").GetValue<bool>();
            var useE = Config.Item("UseEHarass").GetValue<bool>();
            var useR = Config.Item("UseRHarass").GetValue<bool>();
            var RMaxStacksHarass = Config.Item("RMaxStacksHarass").GetValue<Slider>().Value;
            var packetCast = Config.Item("PacketCast").GetValue<bool>();

            var eTarget = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Magical);

            if (mustDebug)
                Game.PrintChat("Harass Target -> " + eTarget.SkinName);

            if (eTarget.IsValidTarget(Q.Range) && R.IsReady() && useQ)
            {
                R.CastIfHitchanceEquals(eTarget, eTarget.IsMoving ? HitChance.High : HitChance.Medium, packetCast);
                return;
            }

            if (eTarget.IsValidTarget(Player.AttackRange + W.Range) && W.IsReady() && useW)
            {
                W.Cast();
                return;
            }

            if (eTarget.IsValidTarget(E.Range) && E.IsReady() && useE)
            {
                Q.CastIfHitchanceEquals(eTarget, eTarget.IsMoving ? HitChance.High : HitChance.Medium, packetCast);
                return;
            }

            if (eTarget.IsValidTarget(R.Range) && R.IsReady() && GetRStacks() < RMaxStacksHarass && useR)
            {
                R.CastIfHitchanceEquals(eTarget, eTarget.IsMoving ? HitChance.High : HitChance.Medium, packetCast);
                return;
            }

            if (mustDebug)
                Game.PrintChat("Harass Finish");
        }

        public static void WaveClear()
        {
            if (mustDebug)
                Game.PrintChat("WaveClear Start");

            if (MinionList.Count == 0)
                return;

            var useE = Config.Item("UseELaneClear").GetValue<bool>();
            var packetCast = Config.Item("PacketCast").GetValue<bool>();

            if (E.IsReady() && useE)
            {
                if (E.GetLineFarmLocation(MinionList).MinionsHit > 3)
                    E.Cast(W.GetLineFarmLocation(MinionList).Position, packetCast);
            }
        }

        public static void Freeze()
        {
            if (mustDebug)
                Game.PrintChat("Freeze Start");

            if (MinionList.Count == 0)
                return;

            var packetCast = Config.Item("PacketCast").GetValue<bool>();
            var nearestTarget = Player.GetNearestEnemy();
        }

        public static void CastAssistedUlt()
        {
            if (mustDebug)
                Game.PrintChat("CastAssistedUlt Start");

            var eTarget = SimpleTs.GetTarget(R.Range, SimpleTs.DamageType.Magical);
            var packetCast = Config.Item("PacketCast").GetValue<bool>();

            if (eTarget.IsValidTarget(R.Range) && R.IsReady())
            {
                R.CastIfWillHit(eTarget, 1, packetCast);
                Game.PrintChat(string.Format("AssistedUlt fired"));
                return;
            }

            if (mustDebug)
                Game.PrintChat("CastAssistedUlt Finish");
        }

        private static void UpdateSpellsRange()
        {
            if (W.Level > 0)
                W.Range = 110 + W.Level * 20;
            if (R.Level > 0)
                R.Range = 900 + R.Level * 300;
        }

        private static void KillSteal()
        {
            var RKillSteal = Config.Item("RKillSteal").GetValue<bool>();
            var packetCast = Config.Item("PacketCast").GetValue<bool>();

            if (RKillSteal && R.IsReady())
            {
                foreach (var enemy in DevCommom.DevCommom.GetEnemyList())
                {
                    if (enemy.IsValidTarget(R.Range) && HealthPrediction.GetHealthPrediction(enemy, (int)R.Delay * 1000) < DamageLib.getDmg(enemy, DamageLib.SpellType.R))
                    {
                        R.CastIfHitchanceEquals(enemy, enemy.IsMoving ? HitChance.High : HitChance.Medium, packetCast);
                        Game.PrintChat("R KillSteal");
                    }
                }
            }
        }

        private static void ChaseEnemyAfterDeath()
        {
            var ChaseEnemyAfterDeath = Config.Item("ChaseEnemyAfterDeath").GetValue<bool>();
            var packetCast = Config.Item("PacketCast").GetValue<bool>();
            var eTarget = SimpleTs.GetTarget(R.Range, SimpleTs.DamageType.Magical);

            if (Player.IsDead && Player.CanMove && eTarget.IsValidTarget())
            {
                Player.SendMovePacket(eTarget.ServerPosition.To2D());
                Game.PrintChat("Fuck! Chase Him!");
            }
        }

        private static int GetRStacks()
        {
            if (Player.HasBuff("KogMawLivingArtillery"))
            {
                return Player.Buffs
                    .Where(x => x.DisplayName.ToLower() == "KogMawLivingArtillery")
                    .Select(x => x.Count)
                    .First();
            }
            else
            {
                return 0;
            }
        }

        private static void onGameLoad(EventArgs args)
        {
            try
            {
                Player = ObjectManager.Player;

                if (Player.ChampionName != ChampionName)
                    return;

                InitializeSpells();

                InitializeSkinManager();

                InitializeMainMenu();

                InitializeAttachEvents();

                Game.PrintChat(string.Format("<font color='#F7A100'>DevKogMaw Loaded v{0}</font>", Assembly.GetExecutingAssembly().GetName().Version));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private static void InitializeAttachEvents()
        {
            if (mustDebug)
                Game.PrintChat("InitializeAttachEvents Start");

            Game.OnGameUpdate += OnTick;
            Game.OnGameSendPacket += Game_OnGameSendPacket;
            Game.OnWndProc += Game_OnWndProc;
            Drawing.OnDraw += OnDraw;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Interrupter.OnPosibleToInterrupt += Interrupter_OnPosibleToInterrupt;

            if (mustDebug)
                Game.PrintChat("InitializeAttachEvents Finish");
        }

        private static void InitializeSpells()
        {
            if (mustDebug)
                Game.PrintChat("InitializeSpells Start");

            IgniteSpell = new DevCommom.IgniteManager();

            Spell Q = new Spell(SpellSlot.Q, 1000);
            Q.SetSkillshot(0.25f, 70f, 1650f, true, SkillshotType.SkillshotLine);

            Spell W = new Spell(SpellSlot.W, 130);

            Spell E = new Spell(SpellSlot.E, 1200);
            E.SetSkillshot(0.25f, 120f, 1400f, false, SkillshotType.SkillshotLine);

            Spell R = new Spell(SpellSlot.R, 1200);
            R.SetSkillshot(1.5f, 225f, float.MaxValue, false, SkillshotType.SkillshotCircle);

            SpellList.Add(Q);
            SpellList.Add(W);
            SpellList.Add(E);
            SpellList.Add(R);

            if (mustDebug)
                Game.PrintChat("InitializeSpells Finish");
        }

        private static void InitializeSkinManager()
        {
            if (mustDebug)
                Game.PrintChat("InitializeSkinManager Start");

            SkinManager = new DevCommom.SkinManager();
            SkinManager.Add("Kog'Maw");
            SkinManager.Add("Caterpillar Kog'Maw");
            SkinManager.Add("Sonoran Kog'Maw");
            SkinManager.Add("Monarch Kog'Maw");
            SkinManager.Add("Reindeer Kog'Maw");
            SkinManager.Add("Lion Dance Kog'Maw");
            SkinManager.Add("Deep Sea Kog'Maw");
            SkinManager.Add("Jurassic Kog'Maw");

            if (mustDebug)
                Game.PrintChat("InitializeSkinManager Finish");
        }

        static void Game_OnGameSendPacket(GamePacketEventArgs args)
        {
            var BlockUlt = Config.Item("BlockUlt").GetValue<bool>();

            if (BlockUlt && args.PacketData[0] == Packet.C2S.Cast.Header)
            {
                var decodedPacket = Packet.C2S.Cast.Decoded(args.PacketData);
                if (decodedPacket.SourceNetworkId == Player.NetworkId && decodedPacket.Slot == SpellSlot.R)
                {
                    Vector3 vecCast = new Vector3(decodedPacket.ToX, decodedPacket.ToY, 0);
                    var query = DevCommom.DevCommom.GetEnemyList().Where(x => R.WillHit(x, vecCast));

                    if (query.Count() == 0)
                    {
                        args.Process = false;
                        Game.PrintChat(string.Format("Ult Blocked"));
                    }
                }
            }
        }

        static void Game_OnWndProc(WndEventArgs args)
        {
            if (MenuGUI.IsChatOpen)
                return;

            var UseAssistedUlt = Config.Item("UseAssistedUlt").GetValue<bool>();
            var AssistedUltKey = Config.Item("AssistedUltKey").GetValue<KeyBind>().Key;

            if (UseAssistedUlt && args.WParam == AssistedUltKey)
            {
                if (mustDebug)
                    Game.PrintChat("CastAssistedUlt");

                args.Process = false;
                CastAssistedUlt();
            }
        }

        static void Interrupter_OnPosibleToInterrupt(Obj_AI_Base unit, InterruptableSpell spell)
        {
            Game.PrintChat(string.Format("OnPosibleToInterrupt -> {0} cast {1}", unit.SkinName, spell.SpellName));
        }

        static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            Game.PrintChat(string.Format("OnEnemyGapcloser -> {0}", gapcloser.Sender.SkinName));
        }

        private static float GetRDamage(Obj_AI_Hero hero)
        {
            return (float)DamageLib.getDmg(hero, DamageLib.SpellType.R);
        }

        private static void DrawDebug()
        {
            float y = 0;

            foreach (var buff in ObjectManager.Player.Buffs)
            {
                if (buff.IsActive)
                    LeagueSharp.Drawing.DrawText(0, y, System.Drawing.Color.Wheat, buff.DisplayName);
                y += 16;
            }
        }

        private static void OnDraw(EventArgs args)
        {
            foreach (var spell in SpellList)
            {
                var menuItem = Config.Item(spell.Slot + "Range").GetValue<Circle>();
                if (menuItem.Active && spell.IsReady())
                {
                    Utility.DrawCircle(ObjectManager.Player.Position, spell.Range, menuItem.Color);
                }
            }

            if (Config.Item("RDamage").GetValue<bool>())
            {
                Utility.HpBarDamageIndicator.DamageToUnit = GetRDamage;
                Utility.HpBarDamageIndicator.Enabled = true;
            }

            if (mustDebug)
                DrawDebug();
        }

        private static void InitializeMainMenu()
        {
            if (mustDebug)
                Game.PrintChat("InitializeMainMenu Start");

            Config = new Menu("DevKogMaw", "DevKogMaw", true);

            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            SimpleTs.AddToMenu(targetSelectorMenu);
            Config.AddSubMenu(targetSelectorMenu);

            Config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));
            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));

            Config.AddSubMenu(new Menu("Combo", "Combo"));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseQCombo", "Use Q").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseWCombo", "Use W").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseECombo", "Use E").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseRCombo", "Use R").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("RMaxStacksCombo", "R Max Stacks").SetValue(new Slider(3, 1, 5)));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseIgnite", "Use Ignite").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("ComboActive", "Combo!").SetValue(new KeyBind(32, KeyBindType.Press)));

            Config.AddSubMenu(new Menu("Harass", "Harass"));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseQHarass", "Use Q").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseWHarass", "Use W").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseEHarass", "Use E").SetValue(false));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseRHarass", "Use R").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("RMaxStacksHarass", "R Max Stacks").SetValue(new Slider(1, 1, 5)));
            Config.SubMenu("Harass").AddItem(new MenuItem("HarassActive", "Harass!").SetValue(new KeyBind("V".ToCharArray()[0], KeyBindType.Press)));
            
            Config.AddSubMenu(new Menu("Freeze", "Freeze"));
            Config.SubMenu("Freeze").AddItem(new MenuItem("FreezeActive", "Freeze!").SetValue(new KeyBind("X".ToCharArray()[0], KeyBindType.Press)));

            Config.AddSubMenu(new Menu("LaneClear", "LaneClear"));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("UseELaneClear", "Use E").SetValue(false));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("LaneClearActive", "LaneClear!").SetValue(new KeyBind("C".ToCharArray()[0], KeyBindType.Press)));

            Config.AddSubMenu(new Menu("KillSteal", "KillSteal"));
            Config.SubMenu("KillSteal").AddItem(new MenuItem("RKillSteal", "R KillSteal").SetValue(true));

            Config.AddSubMenu(new Menu("Misc", "Misc"));
            Config.SubMenu("Misc").AddItem(new MenuItem("ChaseEnemyAfterDeath", "Chase Enemy After Death").SetValue(true));
            Config.SubMenu("Misc").AddItem(new MenuItem("PacketCast", "Use PacketCast").SetValue(true));

            Config.AddSubMenu(new Menu("Drawings", "Drawings"));
            Config.SubMenu("Drawings").AddItem(new MenuItem("QRange", "Q Range").SetValue(new Circle(true, System.Drawing.Color.FromArgb(255, 255, 255, 255))));
            Config.SubMenu("Drawings").AddItem(new MenuItem("WRange", "W Range").SetValue(new Circle(false, System.Drawing.Color.FromArgb(255, 255, 255, 255))));
            Config.SubMenu("Drawings").AddItem(new MenuItem("ERange", "E Range").SetValue(new Circle(false, System.Drawing.Color.FromArgb(255, 255, 255, 255))));
            Config.SubMenu("Drawings").AddItem(new MenuItem("RRange", "R Range").SetValue(new Circle(false, System.Drawing.Color.FromArgb(255, 255, 255, 255))));
            Config.SubMenu("Drawings").AddItem(new MenuItem("RDamage", "Show R Damage on HPBar").SetValue(true));


            SkinManager.AddToMenu(ref Config);

            Config.AddToMainMenu();

            if (mustDebug)
                Game.PrintChat("InitializeMainMenu Finish");
        }
    }
}